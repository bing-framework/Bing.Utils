using System.Diagnostics;

namespace Bing.Http;

/// <summary>
/// 可跨并发传输共享的累积诊断，不包含 URL、正文或认证信息。
/// </summary>
/// <remarks>耗时为各操作累计，重叠任务的累计耗时可以超过墙钟时间；字节包含失败和重试中已读取的正文。
/// 属性是分别原子读取的快照，不保证多个属性之间的事务一致性。</remarks>
public sealed class HttpTransferMetrics
{
    private long _bytes, _queueTicks, _throttleTicks, _checkpointTicks, _mergeTicks;
    /// <summary>
    /// 获取从分片响应读取的正文总字节数。
    /// </summary>
    public long BytesRead => Interlocked.Read(ref _bytes);
    /// <summary>
    /// 获取下载进入队列至实际开始的累计时间。
    /// </summary>
    public TimeSpan QueueWaitTime => Elapsed(Interlocked.Read(ref _queueTicks));
    /// <summary>
    /// 获取累计等待限速授权的时间，不包含网络和磁盘读取。
    /// </summary>
    public TimeSpan ThrottleWaitTime => TimeSpan.FromTicks(Interlocked.Read(ref _throttleTicks));
    /// <summary>
    /// 获取持久化检查点的累计时间，包含失败操作。
    /// </summary>
    public TimeSpan CheckpointTime => Elapsed(Interlocked.Read(ref _checkpointTicks));
    /// <summary>
    /// 获取合并、校验和提交的累计时间，包含失败操作。
    /// </summary>
    public TimeSpan MergeTime => Elapsed(Interlocked.Read(ref _mergeTicks));
    internal void AddBytes(long bytes) => Interlocked.Add(ref _bytes, bytes);
    internal void AddQueueTime(long start) => Interlocked.Add(ref _queueTicks, Stopwatch.GetTimestamp() - start);
    internal void AddThrottleTime(TimeSpan time) => Interlocked.Add(ref _throttleTicks, time.Ticks);
    internal void AddCheckpointTime(long start) => Interlocked.Add(ref _checkpointTicks, Stopwatch.GetTimestamp() - start);
    internal void AddMergeTime(long start) => Interlocked.Add(ref _mergeTicks, Stopwatch.GetTimestamp() - start);
    private static TimeSpan Elapsed(long ticks) => TimeSpan.FromSeconds((double)ticks / Stopwatch.Frequency);
}

/// <summary>
/// 对显式启用诊断的响应记录交付字节，不接管底层流。
/// </summary>
internal sealed class HttpTransferReadStream : Stream
{
    private readonly Stream _inner;
    private readonly HttpTransferMetrics _metrics;
    internal HttpTransferReadStream(Stream inner, HttpTransferMetrics metrics) { _inner = inner; _metrics = metrics; }
    public override int Read(byte[] buffer, int offset, int count)
    { var read = _inner.Read(buffer, offset, count); _metrics.AddBytes(read); return read; }
    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    { var read = await _inner.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false); _metrics.AddBytes(read); return read; }
    public override bool CanRead => _inner.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
