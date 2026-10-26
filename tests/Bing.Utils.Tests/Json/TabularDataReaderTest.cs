using System.Collections;
using System.Threading;
using System.Data;
using System.Data.Common;
using System.Text.Json;
using Bing.Utils.Json;
using Xunit;

namespace Bing.Utils.Tests.Json;

/// <summary>
/// 验证数据库读取器与逐行表格格式之间的流式适配。
/// </summary>
public class TabularDataReaderTest
{
    /// <summary>
    /// 创建包含精确数值、二进制和数据库空值的单行表。
    /// </summary>
    private static DataTable CreateTable(string name = "Result")
    {
        var table = new DataTable(name);
        table.Columns.Add("Id", typeof(long));
        table.Columns.Add("Amount", typeof(decimal));
        table.Columns.Add("Payload", typeof(byte[]));
        table.Columns.Add("OccurredAt", typeof(DateTimeOffset));
        table.Columns.Add("Optional", typeof(string));
        table.Rows.Add(long.MaxValue, 123456789012345.6789m, new byte[] { 4, 5, 6 },
            new DateTimeOffset(2026, 10, 23, 11, 12, 13, TimeSpan.FromHours(8)), DBNull.Value);
        return table;
    }

    /// <summary>
    /// 读取异步行记录并在结束时执行资源清理回调。
    /// </summary>
    private static async IAsyncEnumerable<TabularRowRecord> Rows(IEnumerable<TabularRowRecord> rows,
        Action onDispose = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        try
        {
            foreach (var row in rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return row;
                await Task.Yield();
            }
        }
        finally { onDispose?.Invoke(); }
    }

    /// <summary>
    /// 创建包含一行数据的 Added 记录。
    /// </summary>
    private static TabularRowRecord Record(params object[] values)
        => new(0, DataRowState.Added, null, values);

    /// <summary>
    /// 创建带唯一列名的反向读取器结构。
    /// </summary>
    private static TabularSchema CreateSchema()
    {
        using var table = CreateTable();
        return TabularSchema.FromTable(table);
    }

    /// <summary>
    /// 创建只记录是否关闭、可控制读取失败的读取器包装器。
    /// </summary>
    private sealed class TrackingReader : DbDataReader
    {
        /// <summary>
        /// 保存底层数据库读取器。
        /// </summary>
        private readonly DbDataReader _inner;
        /// <summary>
        /// 保存可选的元数据列名覆盖。
        /// </summary>
        private readonly string[] _names;
        /// <summary>
        /// 记录包装器是否已经释放。
        /// </summary>
        private bool _closed;

        /// <summary>
        /// 创建包装器并可替换列名以测试非法元数据。
        /// </summary>
        public TrackingReader(DbDataReader inner, string[] names = null)
        {
            _inner = inner;
            _names = names;
        }

        /// <summary>
        /// 获取是否在读取时注入异常。
        /// </summary>
        public bool ThrowOnRead { get; set; }
        public Action AfterRead { get; set; }

        /// <summary>
        /// 获取已尝试读取的行数。
        /// </summary>
        public int ReadCalls { get; private set; }

        /// <inheritdoc />
        public override int Depth => _inner.Depth;
        /// <inheritdoc />
        public override int FieldCount => _inner.FieldCount;
        /// <inheritdoc />
        public override bool HasRows => _inner.HasRows;
        /// <inheritdoc />
        public override bool IsClosed => _closed || _inner.IsClosed;
        /// <inheritdoc />
        public override int RecordsAffected => _inner.RecordsAffected;
        /// <inheritdoc />
        public override object this[int ordinal] => GetValue(ordinal);
        /// <inheritdoc />
        public override object this[string name] => GetValue(GetOrdinal(name));

        /// <inheritdoc />
        public override bool Read()
        {
            ReadCalls++;
            if (ThrowOnRead) throw new IOException("read failed");
            return _inner.Read();
        }

        /// <inheritdoc />
        public override async Task<bool> ReadAsync(CancellationToken cancellationToken)
        {
            ReadCalls++;
            cancellationToken.ThrowIfCancellationRequested();
            if (ThrowOnRead) throw new IOException("read failed");
            var hasNext = await _inner.ReadAsync(cancellationToken).ConfigureAwait(false);
            AfterRead?.Invoke(); return hasNext;
        }

        /// <inheritdoc />
        public override bool NextResult() => _inner.NextResult();
        /// <inheritdoc />
        public override string GetName(int ordinal) => _names == null ? _inner.GetName(ordinal) : _names[ordinal];
        /// <inheritdoc />
        public override int GetOrdinal(string name)
        {
            for (var i = 0; i < FieldCount; i++)
                if (string.Equals(GetName(i), name, StringComparison.Ordinal)) return i;
            throw new IndexOutOfRangeException(name);
        }
        /// <inheritdoc />
        public override Type GetFieldType(int ordinal) => _inner.GetFieldType(ordinal);
        /// <inheritdoc />
        public override string GetDataTypeName(int ordinal) => _inner.GetDataTypeName(ordinal);
        /// <inheritdoc />
        public override object GetValue(int ordinal) => _inner.GetValue(ordinal);
        /// <inheritdoc />
        public override int GetValues(object[] values) => _inner.GetValues(values);
        /// <inheritdoc />
        public override bool IsDBNull(int ordinal) => _inner.IsDBNull(ordinal);
        /// <inheritdoc />
        public override bool GetBoolean(int ordinal) => _inner.GetBoolean(ordinal);
        /// <inheritdoc />
        public override byte GetByte(int ordinal) => _inner.GetByte(ordinal);
        /// <inheritdoc />
        public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
            => _inner.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);
        /// <inheritdoc />
        public override char GetChar(int ordinal) => _inner.GetChar(ordinal);
        /// <inheritdoc />
        public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
            => _inner.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);
        /// <inheritdoc />
        public override DateTime GetDateTime(int ordinal) => _inner.GetDateTime(ordinal);
        /// <inheritdoc />
        public override decimal GetDecimal(int ordinal) => _inner.GetDecimal(ordinal);
        /// <inheritdoc />
        public override double GetDouble(int ordinal) => _inner.GetDouble(ordinal);
        /// <inheritdoc />
        public override float GetFloat(int ordinal) => _inner.GetFloat(ordinal);
        /// <inheritdoc />
        public override Guid GetGuid(int ordinal) => _inner.GetGuid(ordinal);
        /// <inheritdoc />
        public override short GetInt16(int ordinal) => _inner.GetInt16(ordinal);
        /// <inheritdoc />
        public override int GetInt32(int ordinal) => _inner.GetInt32(ordinal);
        /// <inheritdoc />
        public override long GetInt64(int ordinal) => _inner.GetInt64(ordinal);
        /// <inheritdoc />
        public override string GetString(int ordinal) => _inner.GetString(ordinal);
        /// <inheritdoc />
        public override DataTable GetSchemaTable() => _inner.GetSchemaTable();
        /// <inheritdoc />
        public override IEnumerator GetEnumerator() => new DbEnumerator(this, closeReader: false);

        /// <summary>
        /// 关闭底层读取器并记录关闭状态。
        /// </summary>
        public override void Close()
        {
            _closed = true;
            _inner.Close();
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing) Close();
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// 记录目标流是否已释放。
    /// </summary>
    private class TrackingStream : MemoryStream
    {
        /// <summary>
        /// 获取目标是否已释放。
        /// </summary>
        public bool WasDisposed { get; private set; }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }

        /// <inheritdoc />
        public override ValueTask DisposeAsync()
        {
            WasDisposed = true;
            return base.DisposeAsync();
        }
    }

    /// <summary>
    /// 在任意写入入口抛出指定异常的目标流。
    /// </summary>
    private sealed class FailingStream : TrackingStream
    {
        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count) => throw new IOException("write failed");
        /// <inheritdoc />
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => Task.FromException(new IOException("write failed"));
        /// <inheritdoc />
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            => new ValueTask(Task.FromException(new IOException("write failed")));
    }

    /// <summary>
    /// 创建结构时不读取数据行，并拒绝空列名和重复列名。
    /// </summary>
    [Fact]
    public void CreateSchemaUsesMetadataOnlyAndRejectsInvalidNames()
    {
        using var table = CreateTable();
        using var reader = new TrackingReader(table.CreateDataReader());
        var schema = TabularDataReader.CreateSchema(reader, "Snapshot");
        Assert.Equal(0, reader.ReadCalls);
        Assert.Equal(1, schema.TableCount);
        using var copied = schema.GetTableSchema(0);
        Assert.Equal("Snapshot", copied.TableName);
        Assert.Equal(0, copied.Rows.Count);
        Assert.Equal(typeof(long), copied.Columns[0].DataType);

        using var namesTable = new DataTable();
        namesTable.Columns.Add("A", typeof(int));
        namesTable.Columns.Add("B", typeof(int));
        using var duplicateReader = new TrackingReader(namesTable.CreateDataReader(), new[] { "A", "A" });
        Assert.Throws<ArgumentException>(() => TabularDataReader.CreateSchema(duplicateReader));
        using var emptyReader = new TrackingReader(namesTable.CreateDataReader(), new[] { "", "B" });
        Assert.Throws<ArgumentException>(() => TabularDataReader.CreateSchema(emptyReader));
    }

    /// <summary>
    /// 单次异步枚举逐行返回 Added 记录并复制二进制单元格。
    /// </summary>
    [Fact]
    public async Task ReadRowsAsyncStreamsIndependentCurrentRows()
    {
        using var table = CreateTable();
        using var reader = new TrackingReader(table.CreateDataReader());
        var rows = TabularDataReader.ReadRowsAsync(reader);
        await using var enumerator = rows.GetAsyncEnumerator();
        Assert.True(await enumerator.MoveNextAsync());
        var record = enumerator.Current;
        Assert.Equal(0, record.TableIndex);
        Assert.Equal(DataRowState.Added, record.State);
        Assert.Equal(long.MaxValue, record.Current[0]);
        Assert.Equal(123456789012345.6789m, record.Current[1]);
        Assert.NotSame(reader.GetValue(2), record.Current[2]);
        Assert.Same(DBNull.Value, record.Current[4]);
        Assert.False(await enumerator.MoveNextAsync());
        Assert.False(reader.IsClosed);
        Assert.Throws<InvalidOperationException>(() => rows.GetAsyncEnumerator());
    }

    /// <summary>
    /// 提前释放、读取异常和预取消按读取器所有权契约收尾。
    /// </summary>
    [Fact]
    public async Task ReadRowsAsyncHonorsEarlyDisposalFailureAndCancellation()
    {
        using var table = CreateTable();
        var closedReader = new TrackingReader(table.CreateDataReader());
        var enumerator = TabularDataReader.ReadRowsAsync(closedReader, leaveOpen: false).GetAsyncEnumerator();
        Assert.True(await enumerator.MoveNextAsync());
        await enumerator.DisposeAsync();
        Assert.True(closedReader.IsClosed);

        using var openReader = new TrackingReader(table.CreateDataReader());
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(() =>
            TabularDataReader.ReadRowsAsync(openReader, leaveOpen: false, canceled.Token));
        Assert.False(openReader.IsClosed);

        using var canceledDuringRead = new CancellationTokenSource();
        using var canceledReader = new TrackingReader(table.CreateDataReader());
        var canceledRows = TabularDataReader.ReadRowsAsync(canceledReader, leaveOpen: false);
        var canceledEnumerator = canceledRows.GetAsyncEnumerator(canceledDuringRead.Token);
        canceledDuringRead.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await canceledEnumerator.MoveNextAsync());
        Assert.True(canceledReader.IsClosed);

        using var failedReader = new TrackingReader(table.CreateDataReader()) { ThrowOnRead = true };
        var failedEnumerator = TabularDataReader.ReadRowsAsync(failedReader, leaveOpen: false).GetAsyncEnumerator();
        var error = await Assert.ThrowsAsync<IOException>(async () => await failedEnumerator.MoveNextAsync());
        Assert.Equal("read failed", error.Message);
        Assert.True(failedReader.IsClosed);
    }

    /// <summary>
    /// v2 写入仅处理当前结果集并保留大整数、decimal 和空值。
    /// </summary>
    [Fact]
    public async Task WriteTabularAsyncWritesOnlyCurrentResultSet()
    {
        using var dataSet = new DataSet("multiple");
        dataSet.Tables.Add(CreateTable("First"));
        var second = new DataTable("Second");
        second.Columns.Add("Other", typeof(string));
        second.Rows.Add("later");
        dataSet.Tables.Add(second);
        using var reader = new TrackingReader(dataSet.CreateDataReader());
        using var output = new MemoryStream();
        await TabularDataReader.WriteTabularAsync(output, reader,
            new TabularJsonOptions { Format = TabularJsonFormat.Version2 });
        Assert.False(reader.IsClosed);
        Assert.True(reader.NextResult());

        output.Position = 0;
        using var restored = await TabularJson.ReadTableAsync(output,
            new TabularJsonOptions { Format = TabularJsonFormat.Version2 });
        Assert.Equal("Result", restored.TableName);
        Assert.Single(restored.Rows);
        Assert.Equal(long.MaxValue, restored.Rows[0][0]);
        Assert.Equal(123456789012345.6789m, restored.Rows[0][1]);
        Assert.Same(DBNull.Value, restored.Rows[0][4]);
    }

    /// <summary>
    /// 普通 JSON 数组和 NDJSON 保留数值精度并将数据库空值写为 null。
    /// </summary>
    [Fact]
    public async Task JsonArrayAndNdjsonWriteExactValuesAndNulls()
    {
        using var table = CreateTable();
        using var arrayReader = new TrackingReader(table.CreateDataReader());
        using var arrayOutput = new MemoryStream();
        await TabularDataReader.WriteJsonArrayAsync(arrayOutput, arrayReader);
        using (var document = JsonDocument.Parse(arrayOutput.ToArray()))
        {
            var row = document.RootElement[0];
            Assert.Equal(long.MaxValue, row.GetProperty("Id").GetInt64());
            Assert.Equal(123456789012345.6789m, row.GetProperty("Amount").GetDecimal());
            Assert.Equal(JsonValueKind.Null, row.GetProperty("Optional").ValueKind);
        }

        using var ndjsonReader = new TrackingReader(table.CreateDataReader());
        using var ndjsonOutput = new MemoryStream();
        await TabularDataReader.WriteNdjsonAsync(ndjsonOutput, ndjsonReader);
        using var line = JsonDocument.Parse(ndjsonOutput.ToArray().AsMemory(0, checked((int)ndjsonOutput.Length - 1)));
        Assert.Equal(long.MaxValue, line.RootElement.GetProperty("Id").GetInt64());
        Assert.Equal(JsonValueKind.Null, line.RootElement.GetProperty("Optional").ValueKind);
    }

    /// <summary>
    /// 参数预检不接管资源，处理阶段按两个独立所有权选项释放资源。
    /// </summary>
    [Fact]
    public async Task WritersRespectResourceOwnershipAndPreserveWriteFailure()
    {
        using var table = CreateTable();
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        using var untouchedReader = new TrackingReader(table.CreateDataReader());
        var untouchedStream = new TrackingStream();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => TabularDataReader.WriteJsonArrayAsync(
            untouchedStream, untouchedReader, leaveReaderOpen: false, leaveStreamOpen: false,
            cancellationToken: canceled.Token));
        Assert.False(untouchedReader.IsClosed);
        Assert.False(untouchedStream.WasDisposed);

        using var reader = new TrackingReader(table.CreateDataReader());
        var failingStream = new FailingStream();
        var error = await Assert.ThrowsAsync<IOException>(() => TabularDataReader.WriteJsonArrayAsync(
            failingStream, reader, leaveReaderOpen: false, leaveStreamOpen: false));
        Assert.Equal("write failed", error.Message);
        Assert.True(reader.IsClosed);
        Assert.True(failingStream.WasDisposed);
    }

    /// <summary>
    /// 反向读取器支持标准字段访问、字节块读取和 schema 元数据。
    /// </summary>
    [Fact]
    public async Task CreateReaderExposesTypedValuesAndSchema()
    {
        using var table = CreateTable();
        var schema = TabularSchema.FromTable(table);
        var row = Record(long.MaxValue, 123456789012345.6789m, new byte[] { 4, 5, 6 },
            new DateTimeOffset(2026, 10, 23, 11, 12, 13, TimeSpan.FromHours(8)), DBNull.Value);
        var disposed = false;
        await using var reader = TabularDataReader.CreateReader(schema, Rows(new[] { row }, () => disposed = true));
        Assert.True(reader.HasRows);
        Assert.Equal("Id", reader.GetName(0));
        Assert.Equal(0, reader.GetOrdinal("id"));
        Assert.Equal(typeof(long), reader.GetFieldType(0));
        var metadata = reader.GetSchemaTable();
        Assert.Equal("Id", metadata.Rows[0]["ColumnName"]);
        Assert.True(await reader.ReadAsync());
        Assert.Equal(long.MaxValue, reader.GetInt64(0));
        Assert.Equal(123456789012345.6789m, reader.GetDecimal(1));
        Assert.Equal(new DateTimeOffset(2026, 10, 23, 11, 12, 13, TimeSpan.FromHours(8)),
            reader.GetFieldValue<DateTimeOffset>(3));
        Assert.True(reader.IsDBNull(4));
        Assert.Same(DBNull.Value, reader.GetFieldValue<object>(4));
        Assert.Equal(3, reader.GetBytes(2, 0, null, 0, 0));
        var bytes = new byte[2];
        Assert.Equal(2, reader.GetBytes(2, 1, bytes, 0, bytes.Length));
        Assert.Equal(new byte[] { 5, 6 }, bytes);
        var charsSchema = TabularSchema.FromTable(new DataTable("chars") { Columns = { { "Text", typeof(string) } } });
        using var charReader = TabularDataReader.CreateReader(charsSchema,
            Rows(new[] { Record("abcdef") }));
        Assert.True(await charReader.ReadAsync());
        Assert.Equal(6, charReader.GetChars(0, 0, null, 0, 0));
        var chars = new char[3];
        Assert.Equal(3, charReader.GetChars(0, 2, chars, 0, chars.Length));
        Assert.Equal("cde", new string(chars));
        Assert.False(await reader.ReadAsync());
        Assert.True(disposed);
        Assert.True(reader.HasRows);
        Assert.False(await reader.NextResultAsync(CancellationToken.None));
    }

    /// <summary>
    /// 空行源与提前关闭都会释放异步枚举器且不缓冲后续行。
    /// </summary>
    [Fact]
    public async Task CreateReaderHandlesEmptySourceAndEarlyClose()
    {
        using var table = CreateTable();
        var emptyDisposed = false;
        await using (var empty = TabularDataReader.CreateReader(TabularSchema.FromTable(table),
                         Rows(Array.Empty<TabularRowRecord>(), () => emptyDisposed = true)))
        {
            Assert.False(empty.HasRows);
            Assert.False(await empty.ReadAsync());
        }
        Assert.True(emptyDisposed);

        var earlyDisposed = false;
        await using (var early = TabularDataReader.CreateReader(TabularSchema.FromTable(table),
                         Rows(new[]
                         {
                             Record(long.MaxValue, 123456789012345.6789m, new byte[] { 4 }, DateTimeOffset.UtcNow, DBNull.Value),
                             Record(long.MaxValue - 1, 1m, new byte[] { 5 }, DateTimeOffset.UtcNow, DBNull.Value)
                         }, () => earlyDisposed = true)))
        {
            Assert.True(await early.ReadAsync());
            Assert.Equal(long.MaxValue, early.GetInt64(0));
        }
        Assert.True(earlyDisposed);
    }

    /// <summary>
    /// 反向读取器拒绝多表结构和没有 Current 版本的已删除行。
    /// </summary>
    [Fact]
    public async Task CreateReaderRejectsMultipleTablesAndDeletedRows()
    {
        using var dataSet = new DataSet();
        dataSet.Tables.Add(CreateTable("One"));
        dataSet.Tables.Add(CreateTable("Two"));
        Assert.Throws<NotSupportedException>(() => TabularDataReader.CreateReader(
            TabularSchema.FromDataSet(dataSet), Rows(Array.Empty<TabularRowRecord>())));

        using var table = CreateTable();
        await using var reader = TabularDataReader.CreateReader(TabularSchema.FromTable(table), Rows(new[]
        {
            new TabularRowRecord(0, DataRowState.Deleted,
                new object[] { long.MaxValue, 123456789012345.6789m, new byte[] { 1 }, DateTimeOffset.UtcNow, DBNull.Value }, null)
        }));
        await Assert.ThrowsAsync<NotSupportedException>(() => reader.ReadAsync(CancellationToken.None));
        Assert.True(reader.IsClosed);
    }

    /// <summary>
    /// 取消正在挂起的异步源会关闭读取器并释放行枚举器。
    /// </summary>
    [Fact]
    public async Task CreateReaderCancellationDisposesSource()
    {
        var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var disposed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        async IAsyncEnumerable<TabularRowRecord> WaitingRows(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            try
            {
                started.TrySetResult(true);
                await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
                yield break;
            }
            finally { disposed.TrySetResult(true); }
        }

        using var table = CreateTable();
        await using var reader = TabularDataReader.CreateReader(TabularSchema.FromTable(table), WaitingRows());
        using var canceled = new CancellationTokenSource();
        var read = reader.ReadAsync(canceled.Token);
        await started.Task;
        canceled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => read);
        await disposed.Task;
        Assert.True(reader.IsClosed);

        using var alreadyCanceled = new CancellationTokenSource();
        alreadyCanceled.Cancel();
        await using var preCanceledReader = TabularDataReader.CreateReader(
            TabularSchema.FromTable(table), Rows(Array.Empty<TabularRowRecord>()));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            preCanceledReader.ReadAsync(alreadyCanceled.Token));
        Assert.True(preCanceledReader.IsClosed);
    }
    [Fact]
    public async Task LifetimeCancellationAppliesToPrefetchedRows()
    {
        using var table = new DataTable("T"); table.Columns.Add("N", typeof(int));
        using var token = new CancellationTokenSource();
        using var reader = TabularDataReader.CreateReader(TabularSchema.FromTable(table), Rows(new[] { Record(1) }), token.Token);
        Assert.True(reader.HasRows); token.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reader.ReadAsync());
        Assert.True(reader.IsClosed);
    }

    [Fact]
    public async Task NextResultDiscardsRemainingRowsAndDisposesEnumerator()
    {
        using var table = new DataTable("T"); table.Columns.Add("N", typeof(int));
        var disposed = false;
        using var reader = TabularDataReader.CreateReader(TabularSchema.FromTable(table), Rows(new[] { Record(1), Record(2) }, () => disposed = true));
        Assert.True(await reader.ReadAsync());
        Assert.False(await reader.NextResultAsync()); Assert.True(disposed);
        Assert.False(reader.HasRows); Assert.False(await reader.ReadAsync());
    }
    [Fact]
    public async Task NextResultFailureClosesReaderAndPreservesError()
    {
        async IAsyncEnumerable<TabularRowRecord> FailingCleanup()
        {
            try { yield return Record(1); await Task.CompletedTask; }
            finally { throw new IOException("cleanup failed"); }
        }
        using var table = new DataTable("T"); table.Columns.Add("N", typeof(int));
        using var reader = TabularDataReader.CreateReader(TabularSchema.FromTable(table), FailingCleanup());
        Assert.True(await reader.ReadAsync());
        var error = await Assert.ThrowsAsync<IOException>(() => reader.NextResultAsync());
        Assert.Equal("cleanup failed", error.Message); Assert.True(reader.IsClosed);
    }

    [Fact]
    public async Task SourceThatIgnoresCancellationCannotDeliverAnotherRow()
    {
        using var token = new CancellationTokenSource();
        async IAsyncEnumerable<TabularRowRecord> IgnoresCancellation()
        {
            token.Cancel(); yield return Record(1); await Task.CompletedTask;
        }
        using var table = new DataTable("T"); table.Columns.Add("N", typeof(int));
        using var reader = TabularDataReader.CreateReader(TabularSchema.FromTable(table), IgnoresCancellation(), token.Token);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reader.ReadAsync()); Assert.True(reader.IsClosed);
    }

    [Fact]
    public async Task CancellationOnDatabaseEofIsNotReportedAsSuccess()
    {
        using var table = new DataTable("T"); table.Columns.Add("N", typeof(int));
        using var token = new CancellationTokenSource();
        using var reader = new TrackingReader(table.CreateDataReader()) { AfterRead = token.Cancel };
        await using var enumerator = TabularDataReader.ReadRowsAsync(reader, leaveOpen: false, cancellationToken: token.Token).GetAsyncEnumerator();
        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => enumerator.MoveNextAsync().AsTask());
        Assert.Equal(token.Token, error.CancellationToken); Assert.True(reader.IsClosed);
    }
}
