using System.Runtime.CompilerServices;
using System.Threading;
using Bing.Operations;
using Bing.Serialization.Csv;

namespace Bing.IO;

/// <summary>验证流释放失败不能掩盖原始取消，同时仍释放所有已接管资源。</summary>
public class StreamingCancellationCleanupTest
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CopyPreservesCancellationWhenBothDisposalsThrow(bool asynchronous)
    {
        var source = new ThrowingDisposeStream(new byte[] { 1, 2 });
        var target = new ThrowingDisposeStream(Array.Empty<byte>());
        using var token = new CancellationTokenSource();
        var config = new StreamTransferOptions { OperationProgress = new Callback(_ => token.Cancel()) };
        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            if (asynchronous) await StreamTransfer.CopyAsync(source, target, config, false, token.Token);
            else StreamTransfer.Copy(source, target, config, false, token.Token);
        });
        Assert.Equal(token.Token, error.CancellationToken);
        Assert.True(source.Disposed);
        Assert.True(target.Disposed);
    }

    [Fact]
    public async Task CsvReadAndWritePreserveCancellationThroughDisposalFailure()
    {
        var input = new ThrowingDisposeStream(Encoding.UTF8.GetBytes("Id\n1\n"));
        using var readToken = new CancellationTokenSource();
        var config = new CsvOptions { OperationProgress = new Callback(_ => readToken.Cancel()) };
        var readError = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in CsvStream.ReadAsync(input, config, false, readToken.Token)) { }
        });
        Assert.Equal(readToken.Token, readError.CancellationToken);
        Assert.True(input.Disposed);

        var output = new ThrowingDisposeStream(Array.Empty<byte>());
        using var writeToken = new CancellationTokenSource();
        var writeError = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CsvStream.WriteAsync(output, Rows(),
            new CsvOptions { OperationProgress = new Callback(_ => writeToken.Cancel()) }, false, writeToken.Token));
        Assert.Equal(writeToken.Token, writeError.CancellationToken);
        Assert.True(output.Disposed);
    }

    [Fact]
    public async Task CsvEarlyStopStillReportsIndependentCleanupFailure()
    {
        var input = new ThrowingDisposeStream(Encoding.UTF8.GetBytes("Id\n1\n2\n"));
        var enumerator = CsvStream.ReadAsync(input, leaveOpen: false).GetAsyncEnumerator();
        Assert.True(await enumerator.MoveNextAsync());
        await Assert.ThrowsAsync<IOException>(() => enumerator.DisposeAsync().AsTask());
        Assert.True(input.Disposed);
    }

    [Fact]
    public async Task BudgetProbeIsReportedAsReadButNeverWritten()
    {
        using var input = new MemoryStream(new byte[] { 1, 2, 3 });
        using var output = new MemoryStream();
        var events = new List<OperationProgress>();
        await Assert.ThrowsAsync<InvalidDataException>(() => StreamTransfer.CopyAsync(input, output,
            new StreamTransferOptions { MaxBytes = 2, BufferSize = 2, OperationProgress = new Callback(events.Add) }));
        Assert.Equal(3, events.Last(p => p.Stage == OperationStage.Read).Completed);
        Assert.Equal(2, events.Last(p => p.Stage == OperationStage.Write).Completed);
        Assert.Equal(2, output.Length);
        Assert.DoesNotContain(events, p => p.IsFinal);
    }

    private static async IAsyncEnumerable<CsvRow> Rows([EnumeratorCancellation] CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        yield return new CsvRow(new[] { "1" }, new[] { "Id" });
        await Task.CompletedTask;
    }

    private sealed class ThrowingDisposeStream : MemoryStream
    {
        internal ThrowingDisposeStream(byte[] content)
        {
            Write(content, 0, content.Length);
            Position = 0;
        }
        internal bool Disposed { get; private set; }
        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
            throw new IOException("cleanup failed");
        }
    }

    private sealed class Callback : IProgress<OperationProgress>
    {
        private readonly Action<OperationProgress> _action;
        internal Callback(Action<OperationProgress> action) => _action = action;
        public void Report(OperationProgress value) => _action(value);
    }
}
