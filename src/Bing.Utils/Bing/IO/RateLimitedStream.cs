namespace Bing.IO;

/// <summary>对源的读取和目标的写入应用共享字节限速；默认保留底层流。</summary>
/// <remarks>从底层当前位置操作，不支持定位；同步入口会阻塞等待预算，异步入口可取消。
/// 短读归还未用预算，失败的 I/O 保守计费；不保证同一流实例并发读写安全。</remarks>
public sealed class RateLimitedStream : Stream
{
    private readonly Stream _inner;
    private readonly TransferRateLimiter _limiter;
    private readonly bool _leaveOpen;
    private bool _disposed;
    private readonly bool _measureWait;
    private long _waitTicks;

    /// <summary>包装流；限制器可在多个包装流间共享。</summary>
    public RateLimitedStream(Stream inner, TransferRateLimiter limiter, bool leaveOpen = true)
        : this(inner, limiter, leaveOpen, false) { }

    /// <summary>
    /// 包装流并按需启用限速等待计时；不包含底层 I/O 耗时。
    /// </summary>
    public RateLimitedStream(Stream inner, TransferRateLimiter limiter, bool leaveOpen, bool measureWaitTime)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _limiter = limiter ?? throw new ArgumentNullException(nameof(limiter));
        _leaveOpen = leaveOpen;
        _measureWait = measureWaitTime;
    }

    /// <summary>
    /// 获取已启用计时的累计限速授权等待；未启用时为零。
    /// </summary>
    public TimeSpan ThrottleWaitTime => TimeSpan.FromSeconds((double)Interlocked.Read(ref _waitTicks) / System.Diagnostics.Stopwatch.Frequency);

    private Task<int> Reserve(int count, CancellationToken token) => _measureWait ? ReserveMeasured(count, token) : _limiter.ReserveAsync(count, token);
    private async Task<int> ReserveMeasured(int count, CancellationToken token)
    {
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        try { return await _limiter.ReserveAsync(count, token).ConfigureAwait(false); }
        finally { Interlocked.Add(ref _waitTicks, System.Diagnostics.Stopwatch.GetTimestamp() - start); }
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        Validate(buffer, offset, count); if (!CanRead) throw new NotSupportedException("The stream is not readable."); if (count == 0) return 0;
        var allowed = Reserve(count, CancellationToken.None).GetAwaiter().GetResult();
        var read = _inner.Read(buffer, offset, allowed);
        _limiter.Refund(allowed - read); return read;
    }

    /// <inheritdoc />
    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        Validate(buffer, offset, count); if (!CanRead) throw new NotSupportedException("The stream is not readable."); cancellationToken.ThrowIfCancellationRequested(); if (count == 0) return 0;
        var allowed = await Reserve(count, cancellationToken).ConfigureAwait(false);
        var read = await _inner.ReadAsync(buffer, offset, allowed, cancellationToken).ConfigureAwait(false);
        _limiter.Refund(allowed - read); return read;
    }

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
    {
        Validate(buffer, offset, count); if (!CanWrite) throw new NotSupportedException("The stream is not writable.");
        while (count > 0)
        {
            var allowed = Reserve(count, CancellationToken.None).GetAwaiter().GetResult();
            _inner.Write(buffer, offset, allowed); offset += allowed; count -= allowed;
        }
    }

    /// <inheritdoc />
    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        Validate(buffer, offset, count); if (!CanWrite) throw new NotSupportedException("The stream is not writable."); cancellationToken.ThrowIfCancellationRequested();
        while (count > 0)
        {
            var allowed = await Reserve(count, cancellationToken).ConfigureAwait(false);
            await _inner.WriteAsync(buffer, offset, allowed, cancellationToken).ConfigureAwait(false);
            offset += allowed; count -= allowed;
        }
    }

    private void Validate(byte[] buffer, int offset, int count)
    {
        ThrowIfDisposed();
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));
        if (offset < 0 || count < 0 || offset > buffer.Length - count) throw new ArgumentOutOfRangeException(nameof(count));
    }
    private void ThrowIfDisposed() { if (_disposed) throw new ObjectDisposedException(nameof(RateLimitedStream)); }
    /// <inheritdoc />
    public override void Flush() { ThrowIfDisposed(); _inner.Flush(); }
    /// <inheritdoc />
    public override Task FlushAsync(CancellationToken cancellationToken) { ThrowIfDisposed(); return _inner.FlushAsync(cancellationToken); }
    /// <inheritdoc />
    public override bool CanRead => !_disposed && _inner.CanRead;
    /// <inheritdoc />
    public override bool CanWrite => !_disposed && _inner.CanWrite;
    /// <inheritdoc />
    public override bool CanSeek => false;
    /// <inheritdoc />
    public override long Length => throw new NotSupportedException();
    /// <inheritdoc />
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    /// <inheritdoc />
    public override void SetLength(long value) => throw new NotSupportedException();
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (!_disposed) { _disposed = true; if (disposing && !_leaveOpen) _inner.Dispose(); }
        base.Dispose(disposing);
    }
}
