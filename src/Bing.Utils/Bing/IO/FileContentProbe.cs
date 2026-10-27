namespace Bing.IO;

/// <summary>持有检测结果和完整内容的只读前缀重放流。</summary>
/// <remarks>
/// Content 从探测前源流的当前位置开始，先返回已读前缀，再读取剩余正文。
/// 成功探测后必须使用 Content，不能继续直接读源。Content 不支持定位、写入或并发读取。
/// 释放本对象等价于释放 Content；是否释放底层源由 Probe 的 leaveOpen 控制。
/// </remarks>
public sealed class FileContentProbe : IDisposable
{
    /// <summary>获取不可变检测结果；释放探测租约后仍可使用。</summary>
    public FileDetectionResult Result { get; }
    /// <summary>获取包含已探测前缀的完整内容流；其生命周期属于本租约。</summary>
    public Stream Content { get; }

    /// <summary>接管重放流。</summary>
    internal FileContentProbe(FileDetectionResult result, Stream content)
    {
        Result = result;
        Content = content;
    }

    /// <summary>释放重放缓冲并按 leaveOpen 契约处理底层源；可重复调用。</summary>
    public void Dispose() => Content.Dispose();
}

/// <summary>先读取检测缓冲，再消费源剩余内容的只读流。</summary>
internal sealed class FilePrefixReplayStream : Stream
{
    private readonly Stream _source;
    private readonly bool _leaveOpen;
    private byte[] _prefix;
    private readonly int _length;
    private int _offset;
    private bool _disposed;

    /// <summary>接管独占前缀缓冲；不再次读取源。</summary>
    internal FilePrefixReplayStream(Stream source, byte[] prefix, int length, bool leaveOpen)
    {
        _source = source;
        _prefix = prefix;
        _length = length;
        _leaveOpen = leaveOpen;
    }

    /// <inheritdoc />
    public override bool CanRead => !_disposed && _source.CanRead;
    /// <inheritdoc />
    public override bool CanSeek => false;
    /// <inheritdoc />
    public override bool CanWrite => false;
    /// <inheritdoc />
    public override long Length => throw new NotSupportedException();
    /// <inheritdoc />
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    /// <summary>验证读操作，避免零长度读取消费前缀或触发源 IO。</summary>
    private void ValidateRead(byte[] buffer, int offset, int count)
    {
        ThrowIfDisposed();
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));
        if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        if (offset > buffer.Length - count) throw new ArgumentException("读取范围超出缓冲区。");
    }

    /// <summary>检查生命周期，不让已释放缓冲被继续读取。</summary>
    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(FilePrefixReplayStream));
    }

    /// <summary>复制尚未消费的前缀，允许返回短读。</summary>
    private int ReadPrefix(Span<byte> destination)
    {
        var count = Math.Min(destination.Length, _length - _offset);
        new ReadOnlySpan<byte>(_prefix, _offset, count).CopyTo(destination);
        _offset += count;
        return count;
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        ValidateRead(buffer, offset, count);
        if (count == 0) return 0;
        return _offset < _length ? ReadPrefix(new Span<byte>(buffer, offset, count)) : _source.Read(buffer, offset, count);
    }

    /// <inheritdoc />
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ValidateRead(buffer, offset, count);
        if (cancellationToken.IsCancellationRequested) return Task.FromCanceled<int>(cancellationToken);
        if (count == 0) return Task.FromResult(0);
        return _offset < _length ? Task.FromResult(ReadPrefix(new Span<byte>(buffer, offset, count)))
            : _source.ReadAsync(buffer, offset, count, cancellationToken);
    }

#if !NETSTANDARD2_0
    /// <inheritdoc />
    public override int Read(Span<byte> buffer)
    {
        ThrowIfDisposed();
        if (buffer.Length == 0) return 0;
        return _offset < _length ? ReadPrefix(buffer) : _source.Read(buffer);
    }

    /// <inheritdoc />
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (cancellationToken.IsCancellationRequested) return new ValueTask<int>(Task.FromCanceled<int>(cancellationToken));
        if (buffer.Length == 0) return new ValueTask<int>(0);
        return _offset < _length ? new ValueTask<int>(ReadPrefix(buffer.Span)) : _source.ReadAsync(buffer, cancellationToken);
    }
#endif

    /// <inheritdoc />
    public override int ReadByte()
    {
        ThrowIfDisposed();
        return _offset < _length ? _prefix[_offset++] : _source.ReadByte();
    }

    /// <inheritdoc />
    public override void Flush() => ThrowIfDisposed();
    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    /// <inheritdoc />
    public override void SetLength(long value) => throw new NotSupportedException();
    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    /// <summary>释放前缀，并且只在接管源时释放源；重复释放不会重复关闭源。</summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _disposed = true;
            Array.Clear(_prefix, 0, _prefix.Length);
            _prefix = null;
            try { if (!_leaveOpen) _source.Dispose(); }
            finally { base.Dispose(disposing); }
            return;
        }
        base.Dispose(disposing);
    }
}
