using System.Diagnostics;

namespace Bing.IO;

/// <summary>
/// 可供多个传输共享的线程安全字节令牌桶，允许动态调整速率。
/// </summary>
/// <remarks>初始允许一个突发窗口；空闲时最多积累 BurstBytes。等待可取消，不保证等待者的 FIFO 顺序。
/// 统计已授权的流字节，不限制操作系统或网络栈预读。共享同一实例即可共享预算。</remarks>
public sealed class TransferRateLimiter
{
    private readonly object _gate = new();
    private readonly Func<double> _seconds;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;
    private TaskCompletionSource<bool> _changed = NewSignal();
    private long _rate;
    private double _tokens;
    private double _last;

    /// <summary>创建限制器；速率和突发字节数必须大于零。</summary>
    public TransferRateLimiter(long bytesPerSecond, int burstBytes = 65536)
        : this(bytesPerSecond, burstBytes, () => (double)Stopwatch.GetTimestamp() / Stopwatch.Frequency, Task.Delay) { }

    /// <summary>内部时钟与延迟接缝，用于无真实等待的预算验证。</summary>
    internal TransferRateLimiter(long rate, int burst, Func<double> seconds, Func<TimeSpan, CancellationToken, Task> delay)
    {
        if (rate <= 0) throw new ArgumentOutOfRangeException(nameof(rate));
        if (burst <= 0) throw new ArgumentOutOfRangeException(nameof(burst));
        _rate = rate; BurstBytes = burst; _tokens = burst;
        _seconds = seconds; _delay = delay; _last = seconds();
    }

    /// <summary>获取最大突发字节数，设置后固定。</summary>
    public int BurstBytes { get; }

    /// <summary>获取或修改每秒允许的字节数；必须大于零，修改后唤醒等待者重新计算。</summary>
    public long BytesPerSecond
    {
        get { lock (_gate) return _rate; }
        set
        {
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
            lock (_gate)
            {
                Refill(); _rate = value;
                var previous = _changed; _changed = NewSignal(); previous.TrySetResult(true);
            }
        }
    }

    /// <summary>等待并扣除指定数量字节的预算；取消后不退回此前已授权的部分。</summary>
    public async Task WaitAsync(int byteCount, CancellationToken cancellationToken = default)
    {
        if (byteCount < 0) throw new ArgumentOutOfRangeException(nameof(byteCount));
        cancellationToken.ThrowIfCancellationRequested();
        while (byteCount > 0) byteCount -= await ReserveAsync(byteCount, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>一次最多授权一个突发窗口，调用方短读时归还未使用额度。</summary>
    internal async Task<int> ReserveAsync(int requested, CancellationToken token)
    {
        var count = Math.Min(requested, BurstBytes);
        while (true)
        {
            token.ThrowIfCancellationRequested();
            Task changed; TimeSpan wait;
            lock (_gate)
            {
                Refill();
                if (_tokens >= count) { _tokens -= count; return count; }
                wait = TimeSpan.FromMilliseconds(Math.Max(1, Math.Min(int.MaxValue - 1d, Math.Ceiling((count - _tokens) * 1000 / _rate))));
                changed = _changed.Task;
            }
            using var delayCancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
            var delay = _delay(wait, delayCancellation.Token);
            try
            {
                if (await Task.WhenAny(delay, changed).ConfigureAwait(false) == delay)
                    await delay.ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                token.ThrowIfCancellationRequested();
                throw;
            }
            finally { delayCancellation.Cancel(); }
        }
    }

    internal void Refund(int count)
    {
        if (count == 0) return;
        lock (_gate)
        {
            Refill(); _tokens = Math.Min(BurstBytes, _tokens + count);
            var previous = _changed; _changed = NewSignal(); previous.TrySetResult(true);
        }
    }

    private void Refill()
    {
        var now = _seconds();
        _tokens = Math.Min(BurstBytes, _tokens + Math.Max(0, now - _last) * _rate);
        _last = now;
    }

    private static TaskCompletionSource<bool> NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
