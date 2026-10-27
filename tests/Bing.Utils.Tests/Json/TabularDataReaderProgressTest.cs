using System.Data;
using System.Data.Common;
using System.Collections;
using System.Threading;
using Bing.Operations;
using Bing.Utils.Json;

namespace Bing.Utils.Tests.Json;

/// <summary>
/// 验证数据库表格读取进度、兼容签名和读取器所有权。
/// </summary>
public class TabularDataReaderProgressTest
{
    /// <summary>
    /// 新进度重载在成功读取的行和 EOF 处递增，未知总量保持为空。
    /// </summary>
    [Fact]
    public async Task ReadRowsReportsOrderedUnknownTotalProgress()
    {
        using var table = CreateTable();
        using var reader = table.CreateDataReader();
        var events = new List<OperationProgress>();
        var rows = new List<TabularRowRecord>();

        await foreach (var row in TabularDataReader.ReadRowsWithProgressAsync(reader,
                           new CallbackProgress<OperationProgress>(events.Add), leaveOpen: true))
        {
            rows.Add(row);
        }

        Assert.Equal(2, rows.Count);
        Assert.Equal(new long[] { 0, 1, 2 }, events.Select(item => item.Sequence));
        Assert.Equal(new long[] { 1, 2, 2 }, events.Select(item => item.Completed));
        Assert.Equal(new[] { false, false, true }, events.Select(item => item.IsFinal));
        Assert.All(events, item =>
        {
            Assert.Equal(OperationStage.Read, item.Stage);
            Assert.Equal(ProgressUnit.Record, item.Unit);
            Assert.Null(item.Total);
        });
        Assert.False(reader.IsClosed);
    }

    /// <summary>
    /// 保留原三参数签名，并在取消、早停后按所有权关闭读取器且不报告最终事件。
    /// </summary>
    [Fact]
    public async Task ReadRowsPreservesLegacySignatureAndDisposesOwnedReader()
    {
        var legacyMethod = typeof(TabularDataReader).GetMethod(nameof(TabularDataReader.ReadRowsAsync),
            new[] { typeof(DbDataReader), typeof(bool), typeof(CancellationToken) });
        Assert.NotNull(legacyMethod);

        using var table = CreateTable();
        using (var legacyReader = table.CreateDataReader())
        {
            var rows = new List<TabularRowRecord>();
            await foreach (var row in TabularDataReader.ReadRowsAsync(legacyReader, true, CancellationToken.None))
                rows.Add(row);
            Assert.Equal(2, rows.Count);
        }

        var earlyReader = table.CreateDataReader();
        var earlyEvents = new List<OperationProgress>();
        await foreach (var row in TabularDataReader.ReadRowsWithProgressAsync(earlyReader,
                           new CallbackProgress<OperationProgress>(earlyEvents.Add), leaveOpen: false))
        {
            break;
        }
        Assert.True(earlyReader.IsClosed);
        Assert.Single(earlyEvents);
        Assert.False(earlyEvents[0].IsFinal);

        var canceledReader = table.CreateDataReader();
        var canceledEvents = new List<OperationProgress>();
        using var cancellation = new CancellationTokenSource();
        var progress = new CallbackProgress<OperationProgress>(item =>
        {
            canceledEvents.Add(item);
            cancellation.Cancel();
        });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var row in TabularDataReader.ReadRowsWithProgressAsync(canceledReader, progress, leaveOpen: false,
                               cancellationToken: cancellation.Token))
            {
            }
        });
        Assert.True(canceledReader.IsClosed);
        Assert.Single(canceledEvents);
        Assert.False(canceledEvents[0].IsFinal);

        var preCanceledReader = table.CreateDataReader();
        using var preCanceled = new CancellationTokenSource();
        preCanceled.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(() => TabularDataReader.ReadRowsWithProgressAsync(preCanceledReader,
            progress: null, leaveOpen: false, cancellationToken: preCanceled.Token));
        Assert.False(preCanceledReader.IsClosed);
        preCanceledReader.Dispose();
    }

    /// <summary>
    /// 读取失败时即使释放读取器也失败，仍传播原始读取异常且不报告终态。
    /// </summary>
    [Fact]
    public async Task ReadFailureAndDisposeFailurePreserveOriginalError()
    {
        using var table = CreateTable();
        var readError = new IOException("read failed");
        var reader = new DisposeFailingReader(table.CreateDataReader(), new IOException("dispose failed"))
        {
            ThrowOnReadCall = 2,
            ReadFailure = readError
        };
        var events = new List<OperationProgress>();

        await using var enumerator = TabularDataReader.ReadRowsWithProgressAsync(reader,
            new CallbackProgress<OperationProgress>(events.Add), leaveOpen: false).GetAsyncEnumerator();

        Assert.True(await enumerator.MoveNextAsync());
        var error = await Assert.ThrowsAsync<IOException>(() => enumerator.MoveNextAsync().AsTask());

        Assert.Same(readError, error);
        Assert.True(reader.DisposeAttempted);
        Assert.True(reader.IsClosed);
        Assert.Single(events);
        Assert.Equal(1, events[0].Completed);
        Assert.False(events[0].IsFinal);
    }

    /// <summary>
    /// 进度回调取消后即使释放读取器也失败，仍传播同令牌取消且不报告终态。
    /// </summary>
    [Fact]
    public async Task CancellationAndDisposeFailurePreserveOriginalCancellation()
    {
        using var table = CreateTable();
        var reader = new DisposeFailingReader(table.CreateDataReader(), new IOException("dispose failed"));
        var events = new List<OperationProgress>();
        using var cancellation = new CancellationTokenSource();
        var progress = new CallbackProgress<OperationProgress>(item =>
        {
            events.Add(item);
            cancellation.Cancel();
        });

        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in TabularDataReader.ReadRowsWithProgressAsync(reader, progress,
                               leaveOpen: false, cancellationToken: cancellation.Token))
            {
            }
        });

        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.True(reader.DisposeAttempted);
        Assert.True(reader.IsClosed);
        Assert.Single(events);
        Assert.Equal(1, events[0].Completed);
        Assert.False(events[0].IsFinal);
    }

    private static DataTable CreateTable()
    {
        var table = new DataTable("Rows");
        table.Columns.Add("Id", typeof(int));
        table.Rows.Add(1);
        table.Rows.Add(2);
        return table;
    }

    private sealed class CallbackProgress<T> : IProgress<T>
    {
        private readonly Action<T> _callback;

        internal CallbackProgress(Action<T> callback) => _callback = callback;

        public void Report(T value) => _callback(value);
    }

    private sealed class DisposeFailingReader : DbDataReader
    {
        private readonly DbDataReader _inner;
        private readonly Exception _disposeFailure;
        private bool _closed;

        internal DisposeFailingReader(DbDataReader inner, Exception disposeFailure)
        {
            _inner = inner;
            _disposeFailure = disposeFailure;
        }

        internal int ThrowOnReadCall { get; set; }
        internal Exception ReadFailure { get; set; }
        internal int ReadCalls { get; private set; }
        internal bool DisposeAttempted { get; private set; }

        public override int Depth => _inner.Depth;
        public override int FieldCount => _inner.FieldCount;
        public override bool HasRows => _inner.HasRows;
        public override bool IsClosed => _closed || _inner.IsClosed;
        public override int RecordsAffected => _inner.RecordsAffected;
        public override object this[int ordinal] => GetValue(ordinal);
        public override object this[string name] => GetValue(GetOrdinal(name));

        public override bool Read() => _inner.Read();

        public override async Task<bool> ReadAsync(CancellationToken cancellationToken)
        {
            ReadCalls++;
            cancellationToken.ThrowIfCancellationRequested();
            if (ThrowOnReadCall == ReadCalls) throw ReadFailure;
            return await _inner.ReadAsync(cancellationToken).ConfigureAwait(false);
        }

        public override bool NextResult() => _inner.NextResult();
        public override string GetName(int ordinal) => _inner.GetName(ordinal);
        public override int GetOrdinal(string name) => _inner.GetOrdinal(name);
        public override Type GetFieldType(int ordinal) => _inner.GetFieldType(ordinal);
        public override string GetDataTypeName(int ordinal) => _inner.GetDataTypeName(ordinal);
        public override object GetValue(int ordinal) => _inner.GetValue(ordinal);
        public override int GetValues(object[] values) => _inner.GetValues(values);
        public override bool IsDBNull(int ordinal) => _inner.IsDBNull(ordinal);
        public override bool GetBoolean(int ordinal) => _inner.GetBoolean(ordinal);
        public override byte GetByte(int ordinal) => _inner.GetByte(ordinal);
        public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
            => _inner.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);
        public override char GetChar(int ordinal) => _inner.GetChar(ordinal);
        public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
            => _inner.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);
        public override DateTime GetDateTime(int ordinal) => _inner.GetDateTime(ordinal);
        public override decimal GetDecimal(int ordinal) => _inner.GetDecimal(ordinal);
        public override double GetDouble(int ordinal) => _inner.GetDouble(ordinal);
        public override float GetFloat(int ordinal) => _inner.GetFloat(ordinal);
        public override Guid GetGuid(int ordinal) => _inner.GetGuid(ordinal);
        public override short GetInt16(int ordinal) => _inner.GetInt16(ordinal);
        public override int GetInt32(int ordinal) => _inner.GetInt32(ordinal);
        public override long GetInt64(int ordinal) => _inner.GetInt64(ordinal);
        public override string GetString(int ordinal) => _inner.GetString(ordinal);
        public override DataTable GetSchemaTable() => _inner.GetSchemaTable();
        public override IEnumerator GetEnumerator() => new DbEnumerator(this, closeReader: false);

        public override void Close()
        {
            DisposeAttempted = true;
            _closed = true;
            _inner.Close();
            throw _disposeFailure;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeAttempted = true;
                _closed = true;
                _inner.Dispose();
                throw _disposeFailure;
            }
            base.Dispose(disposing);
        }

#if NET6_0_OR_GREATER
        public override ValueTask DisposeAsync()
        {
            DisposeAttempted = true;
            _closed = true;
            _inner.Dispose();
            return new ValueTask(Task.FromException(_disposeFailure));
        }
#endif
    }
}
