using System.Threading;

namespace Bing.IO;

/// <summary>以可控时钟验证共享预算，不使用机器速度作为正确性判断。</summary>
public class TransferRateLimiterTest
{
    [Fact]
    public async Task SharedStreamsHonorBurstAndAggregateRate()
    {
        double seconds = 0; var delays = new List<TimeSpan>();
        var limiter = new TransferRateLimiter(4, 4, () => seconds, (time, _) => { delays.Add(time); seconds += time.TotalSeconds; return Task.CompletedTask; });
        using var a = new MemoryStream(); using var b = new MemoryStream();
        using var first = new RateLimitedStream(a, limiter); using var second = new RateLimitedStream(b, limiter);
        await first.WriteAsync(new byte[4], 0, 4);
        second.Write(new byte[6], 0, 6);
        Assert.Equal(1.5, seconds); Assert.Equal(2, delays.Count);
        Assert.Equal(4, a.Length); Assert.Equal(6, b.Length);
        Assert.False(first.CanSeek); Assert.Throws<NotSupportedException>(() => first.Position = 0);
    }

    [Fact]
    public async Task ShortReadReturnsUnusedBudgetAndOwnershipIsExplicit()
    {
        var limiter = new TransferRateLimiter(1, 4, () => 0, (_, _) => throw new Exception("Unexpected wait"));
        var one = new MemoryStream(new byte[] { 7 });
        using (var stream = new RateLimitedStream(one, limiter)) Assert.Equal(1, await stream.ReadAsync(new byte[4], 0, 4));
        Assert.True(one.CanRead);
        var destination = new MemoryStream();
        using (var stream = new RateLimitedStream(destination, limiter, false)) stream.Write(new byte[3], 0, 3);
        Assert.False(destination.CanWrite); one.Dispose();
        using var readonlyStream = new RateLimitedStream(new MemoryStream(Array.Empty<byte>(), false), limiter, false);
        Assert.Throws<NotSupportedException>(() => readonlyStream.WriteByte(1));
    }

    [Fact]
    public async Task ChangingRateWakesPendingWaitAndCancellationKeepsToken()
    {
        double seconds = 0; var waiting = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var limiter = new TransferRateLimiter(1, 1, () => seconds, async (_, token) => { waiting.TrySetResult(true); await Task.Delay(Timeout.Infinite, token); });
        await limiter.WaitAsync(1);
        var pending = limiter.WaitAsync(1); await waiting.Task;
        Assert.False(pending.IsCompleted);
        seconds = 1; limiter.BytesPerSecond = 10; await pending;
        using var cancellation = new CancellationTokenSource();
        var canceled = limiter.WaitAsync(1, cancellation.Token); cancellation.Cancel();
        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceled);
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Throws<ArgumentOutOfRangeException>(() => limiter.BytesPerSecond = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => new TransferRateLimiter(0));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => limiter.WaitAsync(-1));
    }

    [Fact]
    public async Task CancellationBeforeIoDoesNotCloseOrWrite()
    {
        using var target = new MemoryStream(); using var token = new CancellationTokenSource(); token.Cancel();
        var stream = new RateLimitedStream(target, new TransferRateLimiter(1));
        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => stream.WriteAsync(new byte[1], 0, 1, token.Token));
        Assert.Equal(token.Token, error.CancellationToken); Assert.Equal(0, target.Length);
        stream.Dispose(); stream.Dispose(); Assert.True(target.CanWrite);
        Assert.Throws<ObjectDisposedException>(() => stream.ReadByte());
    }
}
