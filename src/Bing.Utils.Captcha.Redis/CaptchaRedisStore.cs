using System.Text;
using Bing.Captcha;
using StackExchange.Redis;

namespace Bing.Captcha.Redis;

/// <summary>使用 Redis 原子替换和一次性消费验证码记录。</summary>
public sealed class CaptchaRedisStore : ICaptchaStore
{
    private const int MaxRecordBytes = 512;
    private const int MaxUtf8FieldBytes = 192;
    private static readonly byte[] Header = { 0x42, 0x43, 0x52, 0x01 };
    private const string TakeScript = @"
local value = redis.call('GET', KEYS[1])
if not value then return nil end
if #value < 6 or string.sub(value, 1, 4) ~= ARGV[2] then return redis.error_reply('Unknown captcha record protocol') end
local idLength = string.byte(value, 5) + string.byte(value, 6) * 256
if idLength < 1 or idLength > 192 or #value < 6 + idLength then return redis.error_reply('Invalid captcha record id length') end
if string.sub(value, 7, 6 + idLength) ~= ARGV[1] then return nil end
redis.call('DEL', KEYS[1])
return value";

    private readonly IDatabase _database;
    private readonly string _keyPrefix;

    /// <summary>创建存储；复用但不释放传入的 Redis multiplexer。</summary>
    /// <param name="multiplexer">由调用方持有并负责释放的 Redis 多路复用器。</param>
    /// <param name="keyPrefix">用于隔离验证码键的前缀。</param>
    /// <param name="database">使用的 Redis 数据库编号；-1 表示默认数据库。</param>
    public CaptchaRedisStore(IConnectionMultiplexer multiplexer, string keyPrefix = "bing:captcha:v1:", int database = -1)
    {
        if (multiplexer == null)
            throw new ArgumentNullException(nameof(multiplexer));
        if (string.IsNullOrWhiteSpace(keyPrefix) || keyPrefix.Length > 128 || keyPrefix.Any(char.IsControl))
            throw new ArgumentException("键前缀为空白、过长或含控制字符。", nameof(keyPrefix));
        if (database < -1)
            throw new ArgumentOutOfRangeException(nameof(database));

        _database = multiplexer.GetDatabase(database);
        _keyPrefix = keyPrefix;
    }

    /// <summary>获取当前前缀；测试可用唯一前缀做定向清理，不应清空数据库。</summary>
    public string KeyPrefix => _keyPrefix;

    /// <inheritdoc />
    public async Task ReplaceAsync(CaptchaContext context, CaptchaEntry entry, CancellationToken cancellationToken = default)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));
        if (entry == null)
            throw new ArgumentNullException(nameof(entry));
        cancellationToken.ThrowIfCancellationRequested();
        var expiresAt = DateTimeOffset.FromUnixTimeMilliseconds(entry.ExpiresAt.ToUnixTimeMilliseconds());
        if (expiresAt <= DateTimeOffset.UtcNow)
            throw new CaptchaStoreException("不能存储已过期的验证码记录。");

        byte[] payload;
        try
        {
            payload = Serialize(entry);
        }
        catch (Exception exception) when (!(exception is CaptchaStoreException))
        {
            throw new CaptchaStoreException("验证码记录序列化失败。", exception);
        }

        cancellationToken.ThrowIfCancellationRequested();
        Task<bool>? operation = null;
        try
        {
            var expiry = new Expiration(expiresAt.UtcDateTime);
            operation = _database.StringSetAsync(KeyFor(context), payload, expiry, ValueCondition.Always, CommandFlags.DemandMaster);
            var stored = await AwaitWithCancellation(operation, cancellationToken).ConfigureAwait(false);
            if (!stored)
                throw new CaptchaStoreException("Redis 未能保存验证码记录。");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (CaptchaStoreException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new CaptchaStoreException("Redis 验证码写入失败。", exception);
        }
        finally
        {
            if (operation == null || operation.IsCompleted)
                Array.Clear(payload, 0, payload.Length);
            else
                ClearWhenCompleted(operation, payload);
        }
    }

    /// <inheritdoc />
    public async Task<CaptchaEntry?> TryTakeAsync(CaptchaContext context, string captchaId, CancellationToken cancellationToken = default)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));
        ValidateCaptchaId(captchaId);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var operation = _database.ScriptEvaluateAsync(
                TakeScript,
                new[] { (RedisKey)KeyFor(context) },
                new RedisValue[] { captchaId, Header },
                CommandFlags.DemandMaster);
            var result = await AwaitWithCancellation(operation, cancellationToken).ConfigureAwait(false);
            if (result.IsNull)
                return null;

            var payload = (byte[]?)result;
            if (payload == null)
                return null;
            try
            {
                return Deserialize(payload);
            }
            finally
            {
                Array.Clear(payload, 0, payload.Length);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (CaptchaStoreException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new CaptchaStoreException("Redis 验证码消费失败；操作派发后其状态可能已改变。", exception);
        }
    }

    private string KeyFor(CaptchaContext context) => _keyPrefix + context.GetStorageKey();

    private static void ValidateCaptchaId(string captchaId)
    {
        if (captchaId == null)
            throw new ArgumentNullException(nameof(captchaId));
        if (string.IsNullOrWhiteSpace(captchaId) || captchaId.Length > 64 || captchaId.Any(char.IsControl) || captchaId.Any(char.IsSurrogate))
            throw new ArgumentException("验证码 ID 为空、过长或包含不支持的字符。", nameof(captchaId));
    }

    private static byte[] Serialize(CaptchaEntry entry)
    {
        using var stream = new MemoryStream(MaxRecordBytes);
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
        {
            writer.Write(Header);
            var idBytes = Encoding.UTF8.GetBytes(entry.CaptchaId);
            if (idBytes.Length == 0 || idBytes.Length > MaxUtf8FieldBytes)
                throw new InvalidDataException("Invalid captcha id length.");
            writer.Write((ushort)idBytes.Length);
            writer.Write(idBytes);
            var hash = entry.AnswerHash;
            try
            {
                writer.Write((byte)hash.Length);
                writer.Write(hash);
            }
            finally
            {
                Array.Clear(hash, 0, hash.Length);
            }
            WriteString(writer, entry.KeyVersion, MaxUtf8FieldBytes);
            writer.Write(entry.ExpiresAt.ToUnixTimeMilliseconds());
            writer.Write(entry.IgnoreAsciiCase);
            writer.Write(entry.NormalizationVersion);
            writer.Flush();
        }
        if (stream.Length > MaxRecordBytes)
            throw new CaptchaStoreException("验证码记录超出存储大小上限。");
        return stream.ToArray();
    }

    private static CaptchaEntry Deserialize(byte[] bytes)
    {
        if (bytes.Length > MaxRecordBytes)
            throw new CaptchaStoreException("Redis 验证码记录超出大小上限。");
        try
        {
            using var stream = new MemoryStream(bytes, false);
            using var reader = new BinaryReader(stream, Encoding.UTF8, true);
            var header = reader.ReadBytes(Header.Length);
            if (!header.SequenceEqual(Header))
                throw new InvalidDataException("Invalid captcha record version.");
            var idLength = reader.ReadUInt16();
            if (idLength == 0 || idLength > MaxUtf8FieldBytes)
                throw new InvalidDataException("Invalid captcha id length.");
            var idBytes = reader.ReadBytes(idLength);
            if (idBytes.Length != idLength)
                throw new EndOfStreamException();
            var captchaId = new UTF8Encoding(false, true).GetString(idBytes);
            var hashLength = reader.ReadByte();
            if (hashLength != 32)
                throw new InvalidDataException("Invalid captcha answer hash length.");
            var hash = reader.ReadBytes(hashLength);
            try
            {
                if (hash.Length != hashLength)
                    throw new EndOfStreamException();
                var keyVersion = ReadString(reader, MaxUtf8FieldBytes);
                var expiresAt = DateTimeOffset.FromUnixTimeMilliseconds(reader.ReadInt64());
                var caseFlag = reader.ReadByte();
                if (caseFlag > 1)
                    throw new InvalidDataException("Invalid captcha case flag.");
                var ignoreAsciiCase = caseFlag == 1;
                var normalizationVersion = reader.ReadInt32();
                if (stream.Position != stream.Length)
                    throw new InvalidDataException("Unexpected trailing captcha record data.");
                return new CaptchaEntry(captchaId, hash, keyVersion, expiresAt, ignoreAsciiCase, normalizationVersion);
            }
            finally
            {
                Array.Clear(hash, 0, hash.Length);
            }
        }
        catch (Exception exception) when (exception is IOException || exception is ArgumentException)
        {
            throw new CaptchaStoreException("Redis 验证码记录损坏。", exception);
        }
    }

    private static void WriteString(BinaryWriter writer, string value, int maxBytes)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        if (bytes.Length == 0 || bytes.Length > maxBytes)
            throw new InvalidDataException("Invalid captcha record string length.");
        writer.Write((byte)bytes.Length);
        writer.Write(bytes);
    }

    private static string ReadString(BinaryReader reader, int maxBytes)
    {
        var length = reader.ReadByte();
        if (length == 0 || length > maxBytes)
            throw new InvalidDataException("Invalid captcha record string length.");
        var bytes = reader.ReadBytes(length);
        if (bytes.Length != length)
            throw new EndOfStreamException();
        return new UTF8Encoding(false, true).GetString(bytes);
    }

    private static async Task<T> AwaitWithCancellation<T>(Task<T> operation, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled)
            return await operation.ConfigureAwait(false);

        var canceled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using (cancellationToken.Register(static state => ((TaskCompletionSource<bool>)state!).TrySetResult(true), canceled))
        {
            var completed = await Task.WhenAny(operation, canceled.Task).ConfigureAwait(false);
            if (completed != operation && !operation.IsCompleted)
            {
                ObserveFailure(operation);
                throw new OperationCanceledException(cancellationToken);
            }
        }

        return await operation.ConfigureAwait(false);
    }

    private static void ObserveFailure(Task operation)
    {
        _ = operation.ContinueWith(
            static completed => { _ = completed.Exception; },
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private static void ClearWhenCompleted(Task operation, byte[] buffer)
    {
        _ = operation.ContinueWith(
            static (_, state) =>
            {
                var bytes = (byte[])state!;
                Array.Clear(bytes, 0, bytes.Length);
            },
            buffer,
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }
}
