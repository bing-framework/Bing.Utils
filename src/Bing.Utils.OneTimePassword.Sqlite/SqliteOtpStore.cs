using System.Globalization;
using Bing.OneTimePassword;
using Microsoft.Data.Sqlite;

namespace Bing.OneTimePassword.Sqlite;

/// <summary>
/// 使用本地 SQLite 文件保存 OTP 权威状态的事务存储。
/// </summary>
/// <remarks>每次调用独立连接；同步 SQLite I/O 不使用 Task.Run。请在启动时显式 InitializeAsync，不在网络文件系统共享数据库。</remarks>
public sealed class SqliteOtpStore : IOtpStore
{
    /// <summary>
    /// 固定五秒锁等待且不持有连接池的连接串。
    /// </summary>
    private readonly string _connectionString;

    /// <summary>
    /// 创建本地文件存储。
    /// </summary>
    /// <param name="connectionString">本地数据库连接串；不支持每连接独立的内存数据库。</param>
    public SqliteOtpStore(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentException("必须指定 SQLite 文件。", nameof(connectionString));
        var builder = new SqliteConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(builder.DataSource) || builder.DataSource == ":memory:" || builder.Mode == SqliteOpenMode.Memory || builder.Mode == SqliteOpenMode.ReadOnly)
            throw new ArgumentException("OTP 状态需要可写的持久化文件。", nameof(connectionString));
        builder.DefaultTimeout = 5;
        builder.ForeignKeys = true;
        builder.Pooling = false;
        _connectionString = builder.ToString();
    }

    /// <summary>
    /// 显式创建版本一表结构；未知或不完整版本拒绝使用。
    /// </summary>
    /// <param name="cancellationToken">提交前取消回滚。</param>
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            using var connection = Open();
            using (var wal = connection.CreateCommand())
            {
                wal.CommandText = "PRAGMA journal_mode=WAL;";
                if (!string.Equals(Convert.ToString(wal.ExecuteScalar(), CultureInfo.InvariantCulture), "wal", StringComparison.OrdinalIgnoreCase))
                    throw new OtpStoreException("SQLite 文件不支持 WAL。");
            }
            using var transaction = connection.BeginTransaction(deferred: false);
            using var exists = Command(connection, transaction, "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name LIKE 'otp_%';");
            var count = Convert.ToInt64(exists.ExecuteScalar(), CultureInfo.InvariantCulture);
            if (count == 0)
            {
                using var schema = Command(connection, transaction, Schema);
                schema.ExecuteNonQuery();
            }
            EnsureVersion(connection, transaction);
            cancellationToken.ThrowIfCancellationRequested();
            transaction.Commit();
            return Task.CompletedTask;
        }
        catch (SqliteException e) { throw new OtpStoreException("SQLite OTP 初始化失败。", e); }
    }

    /// <inheritdoc />
    public Task<T> ExecuteAsync<T>(string subjectId, Func<OtpAccountState, T> operation, CancellationToken cancellationToken = default)
    {
        ValidateSubject(subjectId);
        if (operation == null) throw new ArgumentNullException(nameof(operation));
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            using var connection = Open();
            using var transaction = connection.BeginTransaction(deferred: false);
            EnsureVersion(connection, transaction);
            var state = Load(connection, transaction, subjectId);
            cancellationToken.ThrowIfCancellationRequested();
            var result = operation(state);
            if (result is Task) throw new InvalidOperationException("OTP 存储回调必须同步完成。");
            Save(connection, transaction, state);
            cancellationToken.ThrowIfCancellationRequested();
            transaction.Commit();
            return Task.FromResult(result);
        }
        catch (SqliteException e) { throw new OtpStoreException("SQLite OTP 事务失败，提交结果未知时不能自动重试。", e); }
    }

    /// <inheritdoc />
    public Task<int> CleanupAsync(DateTimeOffset now, int maxRecords = 256, CancellationToken cancellationToken = default)
    {
        if (maxRecords < 1 || maxRecords > 10000) throw new ArgumentOutOfRangeException(nameof(maxRecords));
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            using var connection = Open();
            using var transaction = connection.BeginTransaction(deferred: false);
            EnsureVersion(connection, transaction);
            var total = 0;
            foreach (var sql in new[]
            {
                "DELETE FROM otp_transactions WHERE rowid IN (SELECT rowid FROM otp_transactions WHERE consumed=1 OR expires_at <= $now LIMIT $limit);",
                "DELETE FROM otp_credentials WHERE rowid IN (SELECT rowid FROM otp_credentials WHERE status=3 OR (status=1 AND pending_expires <= $now) LIMIT $limit);",
                "DELETE FROM otp_accounts WHERE rowid IN (SELECT a.rowid FROM otp_accounts a WHERE (a.fail_start IS NULL OR a.fail_start <= $old) AND (a.locked_until IS NULL OR a.locked_until <= $now) AND NOT EXISTS(SELECT 1 FROM otp_credentials c WHERE c.subject_id=a.subject_id) AND NOT EXISTS(SELECT 1 FROM otp_transactions t WHERE t.subject_id=a.subject_id) LIMIT $limit);"
            })
            {
                if (total >= maxRecords) break;
                using var command = Command(connection, transaction, sql);
                command.Parameters.AddWithValue("$now", now.ToUnixTimeMilliseconds());
                command.Parameters.AddWithValue("$old", now.AddDays(-1).ToUnixTimeMilliseconds());
                command.Parameters.AddWithValue("$limit", maxRecords - total);
                total += command.ExecuteNonQuery();
                cancellationToken.ThrowIfCancellationRequested();
            }
            transaction.Commit();
            return Task.FromResult(total);
        }
        catch (SqliteException e) { throw new OtpStoreException("SQLite OTP 清理失败。", e); }
    }

    /// <summary>
    /// 打开连接并固定持久化和外键设置。
    /// </summary>
    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        try
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA foreign_keys=ON; PRAGMA synchronous=FULL;";
            command.ExecuteNonQuery();
            return connection;
        }
        catch { connection.Dispose(); throw; }
    }

    /// <summary>
    /// 验证唯一 schema 版本及必要表，禁止静默迁移。
    /// </summary>
    private static void EnsureVersion(SqliteConnection connection, SqliteTransaction transaction)
    {
        using var command = Command(connection, transaction, "SELECT version FROM otp_schema;");
        using var reader = command.ExecuteReader();
        if (!reader.Read() || reader.GetInt32(0) != 1 || reader.Read()) throw new OtpStoreException("不支持的 OTP 数据库版本。");
        reader.Close();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name IN ('otp_accounts','otp_credentials','otp_transactions');";
        if (Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) != 3) throw new OtpStoreException("OTP 表结构不完整。");
    }

    /// <summary>
    /// 在写事务中完整读取当前账号的有界状态。
    /// </summary>
    private static OtpAccountState Load(SqliteConnection connection, SqliteTransaction transaction, string subject)
    {
        try
        {
            var state = new OtpAccountState(subject);
            using (var command = Command(connection, transaction, "SELECT fail_start,fail_count,locked_until FROM otp_accounts WHERE subject_id=$subject;", subject))
            using (var reader = command.ExecuteReader())
                if (reader.Read()) state.Failure = ReadFailure(reader, 0);
            using (var command = Command(connection, transaction, @"SELECT credential_id,display_name,type,algorithm,digits,period,protected_secret,status,next_counter,counter_exhausted,last_step,created_at,last_used,pending_expires,fail_start,fail_count,locked_until FROM otp_credentials WHERE subject_id=$subject LIMIT 201;", subject))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (state.Credentials.Count >= 200) throw new OtpStoreException("OTP 凭据记录超出存储边界。");
                    var status = (OtpCredentialStatus)reader.GetInt32(7);
                    if (!Enum.IsDefined(typeof(OtpCredentialStatus), status)) throw new OtpStoreException("凭据状态损坏。");
                    var cipherLength = reader.GetBytes(6, 0, null, 0, 0);
                    if (cipherLength > 16384 || cipherLength < (status == OtpCredentialStatus.Revoked ? 0 : 1)) throw new OtpStoreException("OTP 密文长度损坏。");
                    var counterBytes = (byte[])reader.GetValue(8);
                    if (counterBytes.Length != 8) throw new OtpStoreException("OTP 计数器损坏。");
                    var lastStep = reader.GetInt64(10);
                    if (lastStep < -1) throw new OtpStoreException("OTP 时间步损坏。");
                    var credential = new OtpCredentialState
                    {
                        CredentialId = ReadText(reader, 0, 64), DisplayName = ReadText(reader, 1, 128),
                        Parameters = new OtpParameters((OtpType)reader.GetInt32(2), (OtpHashAlgorithm)reader.GetInt32(3), reader.GetInt32(4), reader.GetInt32(5)),
                        ProtectedSecret = (byte[])reader.GetValue(6), Status = status, NextCounter = DecodeCounter(counterBytes),
                        CounterExhausted = ReadBoolean(reader, 9), LastAcceptedStep = lastStep,
                        CreatedAt = ReadTime(reader, 11)!.Value, LastUsedAt = ReadTime(reader, 12), PendingExpiresAt = ReadTime(reader, 13),
                        Failure = ReadFailure(reader, 14)
                    };
                    if (status == OtpCredentialStatus.Pending && !credential.PendingExpiresAt.HasValue) throw new OtpStoreException("待确认凭据缺少有效期。");
                    state.Credentials.Add(credential);
                }
            }
            using (var command = Command(connection, transaction, "SELECT transaction_id,purpose,binding_hash,credential_id,kind,expires_at,fail_count,consumed FROM otp_transactions WHERE subject_id=$subject LIMIT 201;", subject))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (state.Transactions.Count >= 200) throw new OtpStoreException("OTP 事务记录超出存储边界。");
                    var kind = (OtpTransactionKind)reader.GetInt32(4);
                    var failures = reader.GetInt32(6);
                    if (!Enum.IsDefined(typeof(OtpTransactionKind), kind) || failures < 0 || failures > 10000) throw new OtpStoreException("OTP 事务记录损坏。");
                    state.Transactions.Add(new OtpTransactionState
                    {
                        TransactionId = ReadText(reader, 0, 64), Purpose = ReadText(reader, 1, 64), BindingHash = ReadText(reader, 2, 64),
                        CredentialId = reader.IsDBNull(3) ? null : ReadText(reader, 3, 64), Kind = kind,
                        ExpiresAt = ReadTime(reader, 5)!.Value, FailureCount = failures, Consumed = ReadBoolean(reader, 7)
                    });
                }
            }
            return state;
        }
        catch (Exception e) when (e is ArgumentException || e is InvalidCastException || e is OverflowException || e is InvalidOperationException)
        { throw new OtpStoreException("OTP 持久化记录格式损坏。", e); }
    }

    /// <summary>
    /// 在同一写事务内替换有界集合并保存账号失败状态。
    /// </summary>
    private static void Save(SqliteConnection connection, SqliteTransaction transaction, OtpAccountState state)
    {
        if (state.Credentials.Count > 200 || state.Transactions.Count > 200) throw new OtpStoreException("OTP 状态超出存储边界。");
        using (var account = Command(connection, transaction, @"INSERT INTO otp_accounts(subject_id,fail_start,fail_count,locked_until,revision) VALUES($subject,$start,$count,$locked,1)
ON CONFLICT(subject_id) DO UPDATE SET fail_start=$start,fail_count=$count,locked_until=$locked,revision=revision+1;", state.SubjectId))
        { AddFailure(account, state.Failure); account.ExecuteNonQuery(); }
        using (var clear = Command(connection, transaction, "DELETE FROM otp_transactions WHERE subject_id=$subject; DELETE FROM otp_credentials WHERE subject_id=$subject;", state.SubjectId)) clear.ExecuteNonQuery();
        foreach (var c in state.Credentials)
        {
            using var command = Command(connection, transaction, @"INSERT INTO otp_credentials(subject_id,credential_id,display_name,type,algorithm,digits,period,protected_secret,status,next_counter,counter_exhausted,last_step,created_at,last_used,pending_expires,fail_start,fail_count,locked_until)
VALUES($subject,$id,$name,$type,$algorithm,$digits,$period,$secret,$status,$counter,$exhausted,$step,$created,$used,$pending,$start,$count,$locked);", state.SubjectId);
            command.Parameters.AddWithValue("$id", c.CredentialId);
            command.Parameters.AddWithValue("$name", c.DisplayName);
            command.Parameters.AddWithValue("$type", (int)c.Parameters.Type);
            command.Parameters.AddWithValue("$algorithm", (int)c.Parameters.Algorithm);
            command.Parameters.AddWithValue("$digits", c.Parameters.Digits);
            command.Parameters.AddWithValue("$period", c.Parameters.PeriodSeconds);
            command.Parameters.AddWithValue("$secret", c.ProtectedSecret);
            command.Parameters.AddWithValue("$status", (int)c.Status);
            command.Parameters.AddWithValue("$counter", EncodeCounter(c.NextCounter));
            command.Parameters.AddWithValue("$exhausted", c.CounterExhausted ? 1 : 0);
            command.Parameters.AddWithValue("$step", c.LastAcceptedStep);
            command.Parameters.AddWithValue("$created", c.CreatedAt.ToUnixTimeMilliseconds());
            command.Parameters.AddWithValue("$used", TimeValue(c.LastUsedAt));
            command.Parameters.AddWithValue("$pending", TimeValue(c.PendingExpiresAt));
            AddFailure(command, c.Failure);
            command.ExecuteNonQuery();
        }
        foreach (var t in state.Transactions)
        {
            using var command = Command(connection, transaction, @"INSERT INTO otp_transactions(subject_id,transaction_id,purpose,binding_hash,credential_id,kind,expires_at,fail_count,consumed)
VALUES($subject,$id,$purpose,$binding,$credential,$kind,$expires,$failures,$consumed);", state.SubjectId);
            command.Parameters.AddWithValue("$id", t.TransactionId);
            command.Parameters.AddWithValue("$purpose", t.Purpose);
            command.Parameters.AddWithValue("$binding", t.BindingHash);
            command.Parameters.AddWithValue("$credential", (object?)t.CredentialId ?? DBNull.Value);
            command.Parameters.AddWithValue("$kind", (int)t.Kind);
            command.Parameters.AddWithValue("$expires", t.ExpiresAt.ToUnixTimeMilliseconds());
            command.Parameters.AddWithValue("$failures", t.FailureCount);
            command.Parameters.AddWithValue("$consumed", t.Consumed ? 1 : 0);
            command.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// 创建绑定连接与事务的参数化命令。
    /// </summary>
    private static SqliteCommand Command(SqliteConnection connection, SqliteTransaction transaction, string sql, string? subject = null)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction; command.CommandText = sql; command.CommandTimeout = 5;
        if (subject != null) command.Parameters.AddWithValue("$subject", subject);
        return command;
    }

    /// <summary>
    /// 绑定失败窗口参数。
    /// </summary>
    private static void AddFailure(SqliteCommand command, OtpFailureState failure)
    {
        command.Parameters.AddWithValue("$start", TimeValue(failure.WindowStart));
        command.Parameters.AddWithValue("$count", failure.Count);
        command.Parameters.AddWithValue("$locked", TimeValue(failure.LockedUntil));
    }

    /// <summary>
    /// 读取合法的失败窗口。
    /// </summary>
    private static OtpFailureState ReadFailure(SqliteDataReader reader, int index)
    {
        var count = reader.GetInt32(index + 1);
        if (count < 0 || count > 10000) throw new OtpStoreException("OTP 失败次数损坏。");
        return new OtpFailureState { WindowStart = ReadTime(reader, index), Count = count, LockedUntil = ReadTime(reader, index + 2) };
    }

    /// <summary>
    /// 读取毫秒精度 UTC 时间。
    /// </summary>
    private static DateTimeOffset? ReadTime(SqliteDataReader reader, int index) => reader.IsDBNull(index) ? null : DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(index));

    /// <summary>
    /// 把可空 UTC 时间映射为数据库参数。
    /// </summary>
    private static object TimeValue(DateTimeOffset? time) => time.HasValue ? (object)time.Value.ToUnixTimeMilliseconds() : DBNull.Value;

    /// <summary>
    /// 读取受控的布尔字段。
    /// </summary>
    private static bool ReadBoolean(SqliteDataReader reader, int index)
    {
        var value = reader.GetInt32(index);
        if (value != 0 && value != 1) throw new OtpStoreException("OTP 布尔字段损坏。");
        return value == 1;
    }

    /// <summary>
    /// 校验数据库文本字段的边界。
    /// </summary>
    private static string ReadText(SqliteDataReader reader, int index, int max)
    {
        var value = reader.GetString(index);
        if (string.IsNullOrWhiteSpace(value) || value.Length > max || value.Any(x => char.IsControl(x) || char.IsSurrogate(x))) throw new OtpStoreException("OTP 文本字段损坏。");
        return value;
    }

    /// <summary>
    /// 以八字节大端序编码完整无符号计数器。
    /// </summary>
    private static byte[] EncodeCounter(ulong value)
    {
        var bytes = new byte[8];
        for (var i = 7; i >= 0; i--) { bytes[i] = (byte)value; value >>= 8; }
        return bytes;
    }

    /// <summary>
    /// 解码完整无符号计数器。
    /// </summary>
    private static ulong DecodeCounter(byte[] bytes)
    {
        ulong result = 0;
        foreach (var b in bytes) result = (result << 8) | b;
        return result;
    }

    /// <summary>
    /// 拒绝空、超长或不支持字符的账号标识。
    /// </summary>
    private static void ValidateSubject(string subject)
    {
        if (subject == null) throw new ArgumentNullException(nameof(subject));
        if (string.IsNullOrWhiteSpace(subject) || subject.Length > 256 || subject.Any(c => char.IsControl(c) || char.IsSurrogate(c))) throw new ArgumentException("账号格式无效。", nameof(subject));
    }

    /// <summary>
    /// 版本一结构；不保存验证码或明文共享密钥。
    /// </summary>
    private const string Schema = @"
CREATE TABLE otp_schema(version INTEGER NOT NULL);
INSERT INTO otp_schema VALUES(1);
CREATE TABLE otp_accounts(
 subject_id TEXT PRIMARY KEY, fail_start INTEGER NULL, fail_count INTEGER NOT NULL CHECK(fail_count>=0),
 locked_until INTEGER NULL, revision INTEGER NOT NULL);
CREATE TABLE otp_credentials(
 subject_id TEXT NOT NULL REFERENCES otp_accounts(subject_id) ON DELETE CASCADE,
 credential_id TEXT PRIMARY KEY, display_name TEXT NOT NULL,
 type INTEGER NOT NULL CHECK(type IN (1,2)), algorithm INTEGER NOT NULL CHECK(algorithm IN (1,2,3)),
 digits INTEGER NOT NULL CHECK(digits IN (6,8)), period INTEGER NOT NULL CHECK(period IN (30,60)),
 protected_secret BLOB NOT NULL CHECK(length(protected_secret)<=16384),
 status INTEGER NOT NULL CHECK(status IN (1,2,3)), next_counter BLOB NOT NULL CHECK(length(next_counter)=8),
 counter_exhausted INTEGER NOT NULL CHECK(counter_exhausted IN (0,1)), last_step INTEGER NOT NULL CHECK(last_step>=-1),
 created_at INTEGER NOT NULL, last_used INTEGER NULL, pending_expires INTEGER NULL,
 fail_start INTEGER NULL, fail_count INTEGER NOT NULL CHECK(fail_count>=0), locked_until INTEGER NULL);
CREATE INDEX ix_otp_credentials_subject ON otp_credentials(subject_id);
CREATE INDEX ix_otp_credentials_expiry ON otp_credentials(status,pending_expires);
CREATE TABLE otp_transactions(
 subject_id TEXT NOT NULL REFERENCES otp_accounts(subject_id) ON DELETE CASCADE,
 transaction_id TEXT PRIMARY KEY, purpose TEXT NOT NULL, binding_hash TEXT NOT NULL,
 credential_id TEXT NULL, kind INTEGER NOT NULL CHECK(kind IN (1,2,3)), expires_at INTEGER NOT NULL,
 fail_count INTEGER NOT NULL CHECK(fail_count>=0), consumed INTEGER NOT NULL CHECK(consumed IN (0,1)));
CREATE INDEX ix_otp_transactions_subject ON otp_transactions(subject_id);
CREATE INDEX ix_otp_transactions_expiry ON otp_transactions(expires_at);";
}
