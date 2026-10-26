using System.Collections;
using System.Data;
using System.Data.Common;
using System.Threading;

namespace Bing.Utils.Json;

/// <summary>
/// 将异步表格行记录适配为标准只读数据库读取器。
/// </summary>
/// <remarks>读取器只提供一个结果集；行源按需枚举，关闭读取器会释放其异步枚举器。</remarks>
internal sealed class TabularDataDbDataReader : DbDataReader, IAsyncDisposable
{
    /// <summary>
    /// 保存独立的单表结构副本。
    /// </summary>
    private readonly DataTable _table;
    /// <summary>
    /// 保存按需提供当前版本行记录的异步源。
    /// </summary>
    private readonly IAsyncEnumerable<TabularRowRecord> _rows;
    /// <summary>
    /// 保存与读取器生命周期关联的取消源。
    /// </summary>
    private readonly CancellationTokenSource _lifetime;
    /// <summary>
    /// 保存当前数据行的异步枚举器。
    /// </summary>
    private IAsyncEnumerator<TabularRowRecord> _enumerator;
    /// <summary>
    /// 保存已预取但尚未由 Read 返回的行。
    /// </summary>
    private object[] _pendingValues;
    /// <summary>
    /// 保存当前行的值快照。
    /// </summary>
    private object[] _currentValues;
    /// <summary>
    /// 指示是否存在一行已预取数据。
    /// </summary>
    private bool _hasPending;
    // 读取到末尾后 HasRows 仍报告本结果集是否曾包含行。
    private bool _hadRows;
    /// <summary>
    /// 指示读取器已经到达行源末尾。
    /// </summary>
    private bool _endOfRows;
    /// <summary>
    /// 指示读取器是否已关闭。
    /// </summary>
    private bool _closed;

    /// <summary>
    /// 创建只读数据库读取器并复制结构。
    /// </summary>
    /// <param name="table">由结构快照创建的空表。</param>
    /// <param name="rows">异步行记录源。</param>
    /// <param name="cancellationToken">读取器生命周期取消令牌。</param>
    internal TabularDataDbDataReader(DataTable table, IAsyncEnumerable<TabularRowRecord> rows,
        CancellationToken cancellationToken)
    {
        _table = table ?? throw new ArgumentNullException(nameof(table));
        _rows = rows ?? throw new ArgumentNullException(nameof(rows));
        _lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    }

    /// <summary>
    /// 获取读取器当前是否包含或已预取至少一行。
    /// </summary>
    public override bool HasRows
    {
        get
        {
            ThrowIfClosed();
            if (_hadRows) return true;
            if (_endOfRows) return false;
            try
            {
                return EnsurePendingAsync(_lifetime.Token).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch
            {
                DisposeAfterFailureAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                throw;
            }
        }
    }

    /// <inheritdoc />
    public override int Depth => 0;

    /// <inheritdoc />
    public override int FieldCount => _table.Columns.Count;

    /// <inheritdoc />
    public override bool IsClosed => _closed;

    /// <inheritdoc />
    public override int RecordsAffected => -1;

    /// <inheritdoc />
    public override object this[int ordinal] => GetValue(ordinal);

    /// <inheritdoc />
    public override object this[string name] => GetValue(GetOrdinal(name));

    /// <summary>
    /// 判断是否存在下一张表；该读取器仅描述一个结果集。
    /// </summary>
    /// <returns>始终返回 false。</returns>
    public override bool NextResult() => NextResultAsync(CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

    /// <inheritdoc />
    public override async Task<bool> NextResultAsync(CancellationToken cancellationToken)
    {
        ThrowIfClosed(); cancellationToken.ThrowIfCancellationRequested();
        _lifetime.Token.ThrowIfCancellationRequested();
        _endOfRows = true; _hasPending = false; _hadRows = false;
        _currentValues = null; _pendingValues = null;
        var enumerator = _enumerator; _enumerator = null;
        try { if (enumerator != null) await enumerator.DisposeAsync().ConfigureAwait(false); }
        catch { await DisposeAfterFailureAsync().ConfigureAwait(false); throw; }
        return false;
    }

    /// <summary>
    /// 同步读取下一行，并等待异步行源完成读取。
    /// </summary>
    /// <returns>存在下一行时返回 true。</returns>
    public override bool Read() => ReadAsync(CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

    /// <summary>
    /// 异步读取下一行，并在行源失败时关闭读取器。
    /// </summary>
    /// <param name="cancellationToken">取消本次读取；取消读取也结束该异步行源。</param>
    /// <returns>存在下一行时返回 true。</returns>
    public override Task<bool> ReadAsync(CancellationToken cancellationToken)
        => ReadNextAsync(cancellationToken);

    /// <inheritdoc />
    public override string GetName(int ordinal)
    {
        ValidateOrdinal(ordinal);
        return _table.Columns[ordinal].ColumnName;
    }

    /// <inheritdoc />
    public override int GetOrdinal(string name)
    {
        if (name == null) throw new ArgumentNullException(nameof(name));
        for (var ordinal = 0; ordinal < FieldCount; ordinal++)
            if (string.Equals(_table.Columns[ordinal].ColumnName, name, StringComparison.Ordinal))
                return ordinal;

        var match = -1;
        for (var ordinal = 0; ordinal < FieldCount; ordinal++)
        {
            if (!string.Equals(_table.Columns[ordinal].ColumnName, name, StringComparison.OrdinalIgnoreCase))
                continue;
            if (match >= 0) throw new IndexOutOfRangeException($"字段名不唯一：{name}。");
            match = ordinal;
        }
        if (match < 0) throw new IndexOutOfRangeException($"找不到字段：{name}。");
        return match;
    }

    /// <inheritdoc />
    public override Type GetFieldType(int ordinal)
    {
        ValidateOrdinal(ordinal);
        return _table.Columns[ordinal].DataType;
    }

    /// <inheritdoc />
    public override string GetDataTypeName(int ordinal) => GetFieldType(ordinal).Name;

    /// <inheritdoc />
    public override object GetValue(int ordinal)
    {
        ValidateOrdinal(ordinal);
        EnsureCurrentRow();
        return _currentValues[ordinal];
    }

    /// <inheritdoc />
    public override int GetValues(object[] values)
    {
        if (values == null) throw new ArgumentNullException(nameof(values));
        EnsureCurrentRow();
        var count = Math.Min(values.Length, _currentValues.Length);
        Array.Copy(_currentValues, values, count);
        return count;
    }

    /// <inheritdoc />
    public override bool IsDBNull(int ordinal) => GetValue(ordinal) == DBNull.Value;

    /// <inheritdoc />
    public override bool GetBoolean(int ordinal) => (bool)GetValue(ordinal);

    /// <inheritdoc />
    public override byte GetByte(int ordinal) => (byte)GetValue(ordinal);

    /// <inheritdoc />
    public override char GetChar(int ordinal) => (char)GetValue(ordinal);

    /// <inheritdoc />
    public override DateTime GetDateTime(int ordinal) => (DateTime)GetValue(ordinal);

    /// <inheritdoc />
    public override decimal GetDecimal(int ordinal) => (decimal)GetValue(ordinal);

    /// <inheritdoc />
    public override double GetDouble(int ordinal) => (double)GetValue(ordinal);

    /// <inheritdoc />
    public override float GetFloat(int ordinal) => (float)GetValue(ordinal);

    /// <inheritdoc />
    public override Guid GetGuid(int ordinal) => (Guid)GetValue(ordinal);

    /// <inheritdoc />
    public override short GetInt16(int ordinal) => (short)GetValue(ordinal);

    /// <inheritdoc />
    public override int GetInt32(int ordinal) => (int)GetValue(ordinal);

    /// <inheritdoc />
    public override long GetInt64(int ordinal) => (long)GetValue(ordinal);

    /// <inheritdoc />
    public override string GetString(int ordinal) => (string)GetValue(ordinal);

    /// <inheritdoc />
    public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
    {
        var value = (byte[])GetValue(ordinal);
        if (dataOffset < 0) throw new ArgumentOutOfRangeException(nameof(dataOffset));
        if (buffer == null) return value.LongLength;
        ValidateCopyArguments(buffer.Length, bufferOffset, length);
        if (dataOffset >= value.LongLength || length == 0) return 0;
        var count = (int)Math.Min(length, value.LongLength - dataOffset);
        Array.Copy(value, checked((int)dataOffset), buffer, bufferOffset, count);
        return count;
    }

    /// <inheritdoc />
    public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
    {
        var value = (string)GetValue(ordinal);
        if (dataOffset < 0) throw new ArgumentOutOfRangeException(nameof(dataOffset));
        if (buffer == null) return value.Length;
        ValidateCopyArguments(buffer.Length, bufferOffset, length);
        if (dataOffset >= value.Length || length == 0) return 0;
        var count = (int)Math.Min(length, value.Length - dataOffset);
        value.CopyTo(checked((int)dataOffset), buffer, bufferOffset, count);
        return count;
    }

    /// <summary>
    /// 获取当前单表列的标准数据库 schema 描述。
    /// </summary>
    /// <returns>每列一行的独立 schema 表。</returns>
    public override DataTable GetSchemaTable()
    {
        var result = new DataTable("SchemaTable");
        result.Columns.Add("ColumnName", typeof(string));
        result.Columns.Add("ColumnOrdinal", typeof(int));
        result.Columns.Add("ColumnSize", typeof(int));
        result.Columns.Add("DataType", typeof(Type));
        result.Columns.Add("AllowDBNull", typeof(bool));
        result.Columns.Add("IsUnique", typeof(bool));
        result.Columns.Add("IsKey", typeof(bool));
        result.Columns.Add("IsAutoIncrement", typeof(bool));
        result.Columns.Add("IsReadOnly", typeof(bool));
        result.Columns.Add("IsLong", typeof(bool));
        result.Columns.Add("BaseTableName", typeof(string));
        result.Columns.Add("BaseColumnName", typeof(string));
        var primaryKey = _table.PrimaryKey;
        foreach (DataColumn column in _table.Columns)
        {
            var row = result.NewRow();
            row["ColumnName"] = column.ColumnName;
            row["ColumnOrdinal"] = column.Ordinal;
            row["ColumnSize"] = column.DataType == typeof(string) ? column.MaxLength : -1;
            row["DataType"] = column.DataType;
            row["AllowDBNull"] = column.AllowDBNull;
            row["IsUnique"] = column.Unique;
            row["IsKey"] = primaryKey.Contains(column);
            row["IsAutoIncrement"] = column.AutoIncrement;
            row["IsReadOnly"] = column.ReadOnly;
            row["IsLong"] = column.DataType == typeof(byte[]) ||
                             column.DataType == typeof(string) && column.MaxLength < 0;
            row["BaseTableName"] = _table.TableName;
            row["BaseColumnName"] = column.ColumnName;
            result.Rows.Add(row);
        }
        return result;
    }

    /// <inheritdoc />
    public override IEnumerator GetEnumerator() => new DbEnumerator(this, closeReader: false);

    /// <summary>
    /// 关闭读取器并释放异步枚举器、表结构及取消源。
    /// </summary>
    public override void Close() => CloseAsyncCore().ConfigureAwait(false).GetAwaiter().GetResult();

#if NET6_0_OR_GREATER
    /// <summary>
    /// 异步关闭读取器并释放其异步资源。
    /// </summary>
    /// <returns>表示异步关闭操作的任务。</returns>
    public override Task CloseAsync() => CloseAsyncCore();

    /// <summary>
    /// 异步关闭读取器并释放其异步资源。
    /// </summary>
    /// <returns>表示异步关闭操作的任务。</returns>
    public override ValueTask DisposeAsync() => new ValueTask(CloseAsyncCore());
#else
    /// <summary>
    /// 异步关闭读取器并释放其异步资源。
    /// </summary>
    /// <returns>表示异步关闭操作的任务。</returns>
    public ValueTask DisposeAsync() => new ValueTask(CloseAsyncCore());
#endif

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) Close();
        base.Dispose(disposing);
    }

    /// <summary>
    /// 读取下一条记录并校验其表索引、状态和列值类型。
    /// </summary>
    private async Task<bool> ReadNextAsync(CancellationToken cancellationToken)
    {
        ThrowIfClosed();
        using var registration = cancellationToken.Register(CancelLifetime);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            _lifetime.Token.ThrowIfCancellationRequested();
            if (_hasPending)
            {
                _currentValues = _pendingValues;
                _pendingValues = null;
                _hasPending = false;
                return true;
            }

            if (!await EnsurePendingAsync(cancellationToken).ConfigureAwait(false))
            {
                _currentValues = null;
                return false;
            }

            _currentValues = _pendingValues;
            _pendingValues = null;
            _hasPending = false;
            return true;
        }
        catch
        {
            await DisposeAfterFailureAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// 异步预取下一行，以支持 HasRows 而不缓存整个数据源。
    /// </summary>
    private async Task<bool> EnsurePendingAsync(CancellationToken cancellationToken)
    {
        if (_hasPending) return true;
        if (_endOfRows) return false;
        _enumerator ??= _rows.GetAsyncEnumerator(_lifetime.Token);
        var hasNext = await _enumerator.MoveNextAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        _lifetime.Token.ThrowIfCancellationRequested();
        if (!hasNext)
        {
            _endOfRows = true;
            var enumerator = _enumerator;
            _enumerator = null;
            await enumerator.DisposeAsync().ConfigureAwait(false);
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();
        _pendingValues = ValidateAndCopyRow(_enumerator.Current);
        _hasPending = true; _hadRows = true;
        return true;
    }

    /// <summary>
    /// 验证一行记录并复制其当前版本值。
    /// </summary>
    private object[] ValidateAndCopyRow(TabularRowRecord row)
    {
        if (row == null) throw new InvalidDataException("行记录为空。");
        if (row.TableIndex != 0) throw new InvalidDataException("单表读取器不能读取其他表的行记录。");
        if (row.State == DataRowState.Deleted || row.Current == null)
            throw new NotSupportedException("数据库读取器不能呈现已删除行或缺少当前版本的行。");
        if (row.Current.Count != FieldCount)
            throw new InvalidDataException("行记录的列数与表结构不一致。");

        var values = new object[FieldCount];
        for (var ordinal = 0; ordinal < values.Length; ordinal++)
        {
            var value = row.Current[ordinal] ?? DBNull.Value;
            if (value != DBNull.Value && value.GetType() != _table.Columns[ordinal].DataType)
                throw new InvalidDataException($"字段 {_table.Columns[ordinal].ColumnName} 的行值类型与 schema 不一致。");
            values[ordinal] = value is byte[] bytes ? bytes.Clone() : value;
        }
        return values;
    }

    /// <summary>
    /// 关闭异步枚举器并释放当前结构和取消源。
    /// </summary>
    private async Task CloseAsyncCore()
    {
        if (_closed) return;
        _closed = true;
        _currentValues = null;
        _pendingValues = null;
        Exception cleanupError = null;
        try { _lifetime.Cancel(); }
        catch (ObjectDisposedException) { }
        catch (Exception error) { cleanupError = error; }
        if (_enumerator != null)
        {
            var enumerator = _enumerator;
            _enumerator = null;
            try { await enumerator.DisposeAsync().ConfigureAwait(false); }
            catch (Exception error) { cleanupError ??= error; }
        }
        try { _table.Dispose(); }
        catch (Exception) when (cleanupError != null) { }
        catch (Exception error) { cleanupError = error; }
        _lifetime.Dispose();
        if (cleanupError != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(cleanupError).Throw();
    }

    /// <summary>
    /// 在读取或行验证失败后尽力关闭资源，不覆盖当前异常。
    /// </summary>
    private async Task DisposeAfterFailureAsync()
    {
        try { await CloseAsyncCore().ConfigureAwait(false); }
        catch { }
    }

    /// <summary>
    /// 取消当前读取器生命周期并忽略并发关闭造成的已释放错误。
    /// </summary>
    private void CancelLifetime()
    {
        try { _lifetime.Cancel(); }
        catch (ObjectDisposedException) { }
        catch { }
    }

    /// <summary>
    /// 验证读取器仍处于打开状态。
    /// </summary>
    private void ThrowIfClosed()
    {
        if (_closed) throw new InvalidOperationException("读取器已关闭。");
    }

    /// <summary>
    /// 验证列序号位于当前结构范围内。
    /// </summary>
    private void ValidateOrdinal(int ordinal)
    {
        if (ordinal < 0 || ordinal >= FieldCount) throw new IndexOutOfRangeException($"字段序号越界：{ordinal}。");
    }

    /// <summary>
    /// 验证读取器打开且当前已定位到有效数据行。
    /// </summary>
    private void EnsureCurrentRow()
    {
        ThrowIfClosed();
        if (_currentValues == null) throw new InvalidOperationException("读取器当前没有有效数据行。");
    }

    /// <summary>
    /// 验证块复制参数不会越过调用方缓冲区。
    /// </summary>
    private static void ValidateCopyArguments(int bufferLength, int bufferOffset, int length)
    {
        if (bufferOffset < 0) throw new ArgumentOutOfRangeException(nameof(bufferOffset));
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
        if (bufferOffset > bufferLength - length) throw new ArgumentException("目标缓冲区容量不足。");
    }

    /// <summary>
    /// 获取当前行的强类型字段值。
    /// </summary>
    public override T GetFieldValue<T>(int ordinal)
    {
        return (T)GetValue(ordinal);
    }
}
