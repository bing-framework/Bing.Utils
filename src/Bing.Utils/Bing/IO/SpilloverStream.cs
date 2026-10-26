namespace Bing.IO;

/// <summary>
/// 超过内存阈值后转入受配额管理临时文件的可定位流。
/// </summary>
/// <remarks>不支持并发操作。首次溢写预留 maxLength 字节，之后不自动退回内存；释放时删除临时文件。
/// 内容长度或定位位置超过阈值即进入磁盘；阈值不是 CLR 分配上限。调用方拥有存储，流只拥有其创建的租约。</remarks>
public sealed class SpilloverStream : Stream, IAsyncDisposable
{
    /// <summary>
    /// 获取临时存储。
    /// </summary>
    private readonly TemporaryFileStore _store;
    /// <summary>
    /// 内存逻辑长度阈值。
    /// </summary>
    private readonly int _threshold;
    /// <summary>
    /// 最大逻辑长度，包含边界。
    /// </summary>
    private readonly long _maximum;
    /// <summary>
    /// 当前正文存储。
    /// </summary>
    private Stream _inner = new MemoryStream();
    /// <summary>
    /// 溢写后持有的活动租约。
    /// </summary>
    private TemporaryFileLease _lease;
    /// <summary>
    /// 是否已经释放。
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// 创建从空内存开始的流，阈值为零时首次非空写入进入磁盘。
    /// </summary>
    public SpilloverStream(TemporaryFileStore store, int memoryThresholdBytes = 1048576, long maxLength = 67108864)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        if (memoryThresholdBytes < 0) throw new ArgumentOutOfRangeException(nameof(memoryThresholdBytes));
        if (maxLength < 0) throw new ArgumentOutOfRangeException(nameof(maxLength));
        _threshold = memoryThresholdBytes; _maximum = maxLength;
    }

    /// <summary>
    /// 获取是否已经转入磁盘，释放后仍保留该状态。
    /// </summary>
    public bool HasSpilled => _lease != null;
    /// <inheritdoc />
    public override bool CanRead => !_disposed;
    /// <inheritdoc />
    public override bool CanWrite => !_disposed;
    /// <inheritdoc />
    public override bool CanSeek => !_disposed;
    /// <inheritdoc />
    public override long Length { get { CheckOpen(); return _inner.Length; } }
    /// <inheritdoc />
    public override long Position
    {
        get { CheckOpen(); return _inner.Position; }
        set { CheckLength(value); EnsureStorage(value); _inner.Position = value; }
    }
    /// <inheritdoc />
    public override void Flush() { CheckOpen(); _inner.Flush(); }
    /// <inheritdoc />
    public override Task FlushAsync(CancellationToken cancellationToken) { CheckOpen(); return _inner.FlushAsync(cancellationToken); }
    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count) { CheckOpen(); return _inner.Read(buffer, offset, count); }
    /// <inheritdoc />
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    { CheckOpen(); return _inner.ReadAsync(buffer, offset, count, cancellationToken); }
    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin)
    {
        var start = origin == SeekOrigin.Begin ? 0 : origin == SeekOrigin.Current ? Position : origin == SeekOrigin.End ? Length : throw new ArgumentOutOfRangeException(nameof(origin));
        Position = checked(start + offset); return Position;
    }
    /// <inheritdoc />
    public override void SetLength(long value) { CheckLength(value); EnsureStorage(value); _inner.SetLength(value); }
    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
    {
        ValidateWrite(buffer, offset, count); EnsureStorage(checked(Position + count)); _inner.Write(buffer, offset, count);
    }
    /// <inheritdoc />
    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ValidateWrite(buffer, offset, count); cancellationToken.ThrowIfCancellationRequested();
        await EnsureStorageAsync(checked(Position + count), cancellationToken).ConfigureAwait(false);
        await _inner.WriteAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 检查释放状态和最大长度。
    /// </summary>
    private void CheckLength(long value)
    {
        CheckOpen();
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
        if (value > _maximum) throw new IOException("The spill stream length limit would be exceeded.");
    }
    /// <summary>
    /// 检查数组边界与写入预算。
    /// </summary>
    private void ValidateWrite(byte[] buffer, int offset, int count)
    {
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));
        if (offset < 0 || count < 0 || offset > buffer.Length - count) throw new ArgumentOutOfRangeException(nameof(count));
        CheckOpen();
        if (Position > _maximum - count) throw new IOException("The spill stream length limit would be exceeded.");
    }
    /// <summary>
    /// 同步溢写，复制完成前不替换原存储。
    /// </summary>
    private void EnsureStorage(long length)
    {
        if (_lease != null || length <= _threshold) return;
        var lease = _store.CreateFile(_maximum, true);
        var position = _inner.Position;
        try
        {
            _inner.Position = 0; _inner.CopyTo(lease.Stream); lease.Stream.Position = position;
            _inner.Dispose(); _inner = lease.Stream; _lease = lease;
        }
        catch { _inner.Position = position; lease.Dispose(); throw; }
    }
    /// <summary>
    /// 异步溢写，取消和复制失败保持原存储可用。
    /// </summary>
    private async Task EnsureStorageAsync(long length, CancellationToken token)
    {
        if (_lease != null || length <= _threshold) return;
        var lease = _store.CreateFile(_maximum, true);
        var position = _inner.Position;
        try
        {
            _inner.Position = 0;
            await _inner.CopyToAsync(lease.Stream, 81920, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested(); lease.Stream.Position = position;
            _inner.Dispose(); _inner = lease.Stream; _lease = lease;
        }
        catch { _inner.Position = position; lease.Dispose(); throw; }
    }
    /// <summary>
    /// 拒绝释放后访问。
    /// </summary>
    private void CheckOpen() { if (_disposed) throw new ObjectDisposedException(nameof(SpilloverStream)); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _disposed = true;
            if (_lease != null) _lease.Dispose(); else _inner.Dispose();
        }
        base.Dispose(disposing);
    }
    /// <summary>
    /// 关闭正文并释放临时文件；删除失败由存储清理器重试。
    /// </summary>
#if NETSTANDARD2_0
    public ValueTask DisposeAsync()
#else
    public override ValueTask DisposeAsync()
#endif
    { Dispose(); return default; }
}
