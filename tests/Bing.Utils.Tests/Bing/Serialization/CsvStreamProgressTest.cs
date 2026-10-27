using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Bing.Diagnostics;
using Bing.Operations;
using Bing.Serialization.Csv;

namespace Bing.Utils.Tests.Bing.Serialization;

/// <summary>
/// 验证 CSV 统一进度的计数、最终事件和资源生命周期。
/// </summary>
public class CsvStreamProgressTest
{
    /// <summary>
    /// 读取和写入进度按数据记录计数，预算不作为总量且最终事件仅在 EOF 发出。
    /// </summary>
    [Fact]
    public async Task ReadAndWriteProgressUsesUnknownTotalAndOrderedFinalEvents()
    {
        var readEvents = new List<OperationProgress>();
        using var source = new MemoryStream(Encoding.UTF8.GetBytes("id\n1\n2\n"));
        var readRows = new List<CsvRow>();
        await foreach (var row in CsvStream.ReadAsync(source, new CsvOptions
                       {
                           MaxRows = 2,
                           OperationProgress = new CallbackProgress<OperationProgress>(readEvents.Add)
                       }))
        {
            readRows.Add(row);
        }

        Assert.Equal(2, readRows.Count);
        Assert.Equal(new long[] { 0, 1, 2 }, readEvents.Select(item => item.Sequence));
        Assert.Equal(new long[] { 1, 2, 2 }, readEvents.Select(item => item.Completed));
        Assert.Equal(new[] { false, false, true }, readEvents.Select(item => item.IsFinal));
        Assert.All(readEvents, item =>
        {
            Assert.Equal(OperationStage.Read, item.Stage);
            Assert.Equal(ProgressUnit.Record, item.Unit);
            Assert.Null(item.Total);
        });

        var writeEvents = new List<OperationProgress>();
        using var destination = new MemoryStream();
        await CsvStream.WriteAsync(destination, Rows(), new CsvOptions
        {
            MaxRows = 2,
            NewLine = "\n",
            OperationProgress = new CallbackProgress<OperationProgress>(writeEvents.Add)
        });

        Assert.Equal(new long[] { 0, 1, 2 }, writeEvents.Select(item => item.Sequence));
        Assert.Equal(new long[] { 1, 2, 2 }, writeEvents.Select(item => item.Completed));
        Assert.Equal(new[] { false, false, true }, writeEvents.Select(item => item.IsFinal));
        Assert.All(writeEvents, item =>
        {
            Assert.Equal(OperationStage.Write, item.Stage);
            Assert.Equal(ProgressUnit.Record, item.Unit);
            Assert.Null(item.Total);
        });
        Assert.DoesNotContain(writeEvents, item => item.Stage == OperationStage.Commit);
    }

    /// <summary>
    /// 读取早停或回调取消不会报告最终事件，且枚举启动后按所有权释放源流。
    /// </summary>
    [Fact]
    public async Task ReadEarlyStopAndCancellationSkipFinalAndDisposeOwnedSource()
    {
        var earlyEvents = new List<OperationProgress>();
        var earlySource = new MemoryStream(Encoding.UTF8.GetBytes("id\n1\n2\n"));
        await foreach (var row in CsvStream.ReadAsync(earlySource, new CsvOptions
                       {
                           OperationProgress = new CallbackProgress<OperationProgress>(earlyEvents.Add)
                       }, leaveOpen: false))
        {
            break;
        }

        Assert.False(earlySource.CanRead);
        Assert.Single(earlyEvents);
        Assert.False(earlyEvents[0].IsFinal);

        var canceledSource = new MemoryStream(Encoding.UTF8.GetBytes("id\n1\n2\n"));
        var canceledEvents = new List<OperationProgress>();
        using var cancellation = new CancellationTokenSource();
        var progress = new CallbackProgress<OperationProgress>(item =>
        {
            canceledEvents.Add(item);
            cancellation.Cancel();
        });
        var deliveredRows = 0;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var row in CsvStream.ReadAsync(canceledSource,
                               new CsvOptions { OperationProgress = progress }, leaveOpen: false,
                               cancellationToken: cancellation.Token))
            {
                deliveredRows++;
            }
        });

        Assert.Equal(0, deliveredRows);
        Assert.False(canceledSource.CanRead);
        Assert.Single(canceledEvents);
        Assert.False(canceledEvents[0].IsFinal);
    }

    /// <summary>
    /// 枚举第一次移动前取消，以及读取 EOF 时取消，都不会发送成功完成事件。
    /// </summary>
    [Fact]
    public async Task ReadStartAndEofCancellationDoNotReportCompletion()
    {
        var beforeMoveSource = new MemoryStream(Encoding.UTF8.GetBytes("id\n1\n"));
        var beforeMoveEvents = new List<OperationProgress>();
        using (var cancellation = new CancellationTokenSource())
        {
            var rows = CsvStream.ReadAsync(beforeMoveSource, new CsvOptions
            {
                OperationProgress = new CallbackProgress<OperationProgress>(beforeMoveEvents.Add)
            }, leaveOpen: false, cancellationToken: cancellation.Token);
            await using var enumerator = rows.GetAsyncEnumerator();
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => enumerator.MoveNextAsync().AsTask());
        }
        Assert.False(beforeMoveSource.CanRead);
        Assert.Empty(beforeMoveEvents);

        using var eofCancellation = new CancellationTokenSource();
        var eofSource = new CancelOnEofStream(Encoding.UTF8.GetBytes("id\n1\n"), eofCancellation);
        var eofEvents = new List<OperationProgress>();
        var deliveredRows = 0;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var row in CsvStream.ReadAsync(eofSource, new CsvOptions
                           {
                               OperationProgress = new CallbackProgress<OperationProgress>(eofEvents.Add)
                           }, leaveOpen: false, cancellationToken: eofCancellation.Token))
            {
                deliveredRows++;
            }
        });

        Assert.Equal(1, deliveredRows);
        Assert.False(eofSource.CanRead);
        Assert.Single(eofEvents);
        Assert.Equal(1, eofEvents[0].Completed);
        Assert.False(eofEvents[0].IsFinal);
    }

    /// <summary>
    /// 写入取消或异常时不报告最终事件，并释放已接管的目标流。
    /// </summary>
    [Fact]
    public async Task WriteCancellationAndFailureSkipFinalAndHonorOwnership()
    {
        var canceledDestination = new TrackingStream();
        var canceledEvents = new List<OperationProgress>();
        using var cancellation = new CancellationTokenSource();
        var progress = new CallbackProgress<OperationProgress>(item =>
        {
            canceledEvents.Add(item);
            cancellation.Cancel();
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CsvStream.WriteAsync(canceledDestination,
            Rows(), new CsvOptions
            {
                NewLine = "\n",
                OperationProgress = progress
            }, leaveOpen: false, cancellationToken: cancellation.Token));

        Assert.True(canceledDestination.WasDisposed);
        Assert.Single(canceledEvents);
        Assert.False(canceledEvents[0].IsFinal);

        var failedDestination = new FailingStream();
        var failedEvents = new List<OperationProgress>();
        await Assert.ThrowsAsync<IOException>(() => CsvStream.WriteAsync(failedDestination, Rows(),
            new CsvOptions
            {
                NewLine = "\n",
                OperationProgress = new CallbackProgress<OperationProgress>(failedEvents.Add)
            }, leaveOpen: false));

        Assert.True(failedDestination.WasDisposed);
        Assert.Empty(failedEvents);
    }

    /// <summary>
    /// CSV 行转换异常的位置与流读取出的逻辑记录、列序号和表头名称一致。
    /// </summary>
    [Fact]
    public async Task CsvRowConversionExceptionExposesLogicalLocation()
    {
        using var source = new MemoryStream(Encoding.UTF8.GetBytes("Id,Name,Age\n1,Ada,18\n2,\"Lin\nX\",not-an-int\n"));
        CsvFormatException conversionError = null;

        await foreach (var row in CsvStream.ReadAsync(source))
        {
            if (row.RowNumber == 3)
                conversionError = Assert.Throws<CsvFormatException>(() => row.Get<int>("Age"));
        }

        Assert.NotNull(conversionError);
        Assert.Equal(3, conversionError.RowNumber);
        Assert.Equal(3, conversionError.ColumnNumber);
        Assert.Equal("Age", conversionError.ColumnName);
        var location = Assert.IsType<CsvLocation>(conversionError.Location);
        Assert.Equal(3, location.RecordNumber);
        Assert.Equal(3, location.ColumnNumber);
        Assert.Equal("Age", location.ColumnName);
    }

    private static async IAsyncEnumerable<CsvRow> Rows(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return new CsvRow(new[] { "1" }, new[] { "id" });
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        yield return new CsvRow(new[] { "2" }, new[] { "id" });
    }

    private sealed class CallbackProgress<T> : IProgress<T>
    {
        private readonly Action<T> _callback;

        internal CallbackProgress(Action<T> callback) => _callback = callback;

        public void Report(T value) => _callback(value);
    }

    private class TrackingStream : MemoryStream
    {
        internal bool WasDisposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }

    private sealed class FailingStream : TrackingStream
    {
        public override void Write(byte[] buffer, int offset, int count) => throw new IOException("write failed");

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => Task.FromException(new IOException("write failed"));

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            => new(Task.FromException(new IOException("write failed")));
    }

    private sealed class CancelOnEofStream : MemoryStream
    {
        private readonly CancellationTokenSource _cancellation;

        internal CancelOnEofStream(byte[] content, CancellationTokenSource cancellation) : base(content)
            => _cancellation = cancellation;

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count,
            CancellationToken cancellationToken)
        {
            var read = await base.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
            if (read == 0) _cancellation.Cancel();
            return read;
        }
    }
}
