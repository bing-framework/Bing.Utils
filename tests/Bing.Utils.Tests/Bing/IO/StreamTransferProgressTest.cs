using System.Threading;
using Bing.Operations;

namespace Bing.IO;

/// <summary>
/// 验证流传输的统一进度只反映成功读取和写入的字节。
/// </summary>
public class StreamTransferProgressTest
{
    /// <summary>
    /// 旧字节进度保持兼容，新进度按读写阶段递增且不猜测总量。
    /// </summary>
    [Fact]
    public async Task CopyReportsSuccessfulReadWriteCountsAndUnknownTotal()
    {
        using var source = new ShortReadStream(new byte[] { 1, 2, 3, 4, 5 }, 2);
        using var destination = new MemoryStream();
        var legacyEvents = new List<long>();
        var operationEvents = new List<OperationProgress>();
        var options = new StreamTransferOptions
        {
            MaxBytes = 5,
            BufferSize = 4,
            Progress = new CallbackProgress<long>(legacyEvents.Add),
            OperationProgress = new CallbackProgress<OperationProgress>(operationEvents.Add)
        };

        var result = await StreamTransfer.CopyAsync(source, destination, options);

        Assert.Equal(5, result.BytesTransferred);
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, destination.ToArray());
        Assert.Equal(new long[] { 2, 4, 5 }, legacyEvents);
        Assert.Equal(new[]
        {
            OperationStage.Read, OperationStage.Write,
            OperationStage.Read, OperationStage.Write,
            OperationStage.Read, OperationStage.Write,
            OperationStage.Read, OperationStage.Write
        }, operationEvents.Select(item => item.Stage));
        Assert.Equal(new long[] { 0, 1, 2, 3, 4, 5, 6, 7 }, operationEvents.Select(item => item.Sequence));
        Assert.All(operationEvents, item =>
        {
            Assert.Equal(ProgressUnit.Byte, item.Unit);
            Assert.Null(item.Total);
        });
        Assert.Equal(new long[] { 2, 2, 4, 4, 5, 5, 5, 5 }, operationEvents.Select(item => item.Completed));
        Assert.Equal(new[] { false, false, false, false, false, false, true, true },
            operationEvents.Select(item => item.IsFinal));
    }

    /// <summary>
    /// 写入异常只报告已成功读取的字节，不伪造写入或最终事件。
    /// </summary>
    [Fact]
    public async Task CopyWriteFailureReportsReadButNotWriteOrFinal()
    {
        using var source = new MemoryStream(new byte[] { 1, 2, 3 });
        using var destination = new FailingWriteStream();
        var events = new List<OperationProgress>();

        await Assert.ThrowsAsync<IOException>(() => StreamTransfer.CopyAsync(source, destination,
            new StreamTransferOptions { OperationProgress = new CallbackProgress<OperationProgress>(events.Add) }));

        var read = Assert.Single(events);
        Assert.Equal(OperationStage.Read, read.Stage);
        Assert.Equal(3, read.Completed);
        Assert.False(read.IsFinal);
        Assert.DoesNotContain(events, item => item.Stage == OperationStage.Write || item.IsFinal);
    }

    /// <summary>
    /// 读进度回调取消后不会写入数据或发送最终事件，并按 leaveOpen=false 释放流。
    /// </summary>
    [Fact]
    public async Task CopyCancellationAfterReadHonorsOwnershipAndSkipsFinalEvents()
    {
        var source = new ShortReadStream(new byte[] { 1, 2, 3 }, 3);
        var destination = new MemoryStream();
        var events = new List<OperationProgress>();
        using var cancellation = new CancellationTokenSource();
        var progress = new CallbackProgress<OperationProgress>(item =>
        {
            events.Add(item);
            if (item.Stage == OperationStage.Read) cancellation.Cancel();
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => StreamTransfer.CopyAsync(source, destination,
            new StreamTransferOptions { OperationProgress = progress }, leaveOpen: false,
            cancellationToken: cancellation.Token));

        Assert.Empty(destination.ToArray());
        Assert.False(source.CanRead);
        Assert.False(destination.CanWrite);
        Assert.Single(events);
        Assert.Equal(OperationStage.Read, events[0].Stage);
        Assert.False(events[0].IsFinal);
    }

    private sealed class CallbackProgress<T> : IProgress<T>
    {
        private readonly Action<T> _callback;

        internal CallbackProgress(Action<T> callback) => _callback = callback;

        public void Report(T value) => _callback(value);
    }

    private sealed class ShortReadStream : MemoryStream
    {
        private readonly int _maximumRead;

        internal ShortReadStream(byte[] bytes, int maximumRead) : base(bytes) => _maximumRead = maximumRead;

        public override bool CanSeek => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
            => base.Read(buffer, offset, Math.Min(count, _maximumRead));

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Read(buffer, offset, count));
        }
    }

    private sealed class FailingWriteStream : MemoryStream
    {
        public override void Write(byte[] buffer, int offset, int count) => throw new IOException("write failed");

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => Task.FromException(new IOException("write failed"));
    }
}
