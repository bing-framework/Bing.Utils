namespace Bing.IO;

/// <summary>持有检测结果及完整内容缓存的生命周期对象。</summary>
/// <remarks>
/// Content 从位置零开始，可读取和定位但不能写入。缓存内容可能位于 TemporaryFileStore 管理的临时文件中；
/// 释放本对象会关闭 Content 并清理缓存租约，但不会释放调用方提供的 TemporaryFileStore。
/// </remarks>
public sealed class FileContentBuffer : IDisposable, IAsyncDisposable
{
    private readonly SpilloverStream _storage;
    private readonly ReadOnlySeekableStream _content;
    private bool _disposed;

    /// <summary>获取文件内容检测结论；释放对象后仍可查询。</summary>
    public FileDetectionResult Result { get; }

    /// <summary>获取从位置零开始的只读可定位完整内容流；其生命周期属于本对象。</summary>
    public Stream Content => _content;

    /// <summary>接管已完成的缓存存储并重置到文件内容起点。</summary>
    internal FileContentBuffer(FileDetectionResult result, SpilloverStream storage)
    {
        Result = result ?? throw new ArgumentNullException(nameof(result));
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _storage.Position = 0;
        _content = new ReadOnlySeekableStream(_storage);
    }

    /// <summary>关闭读取流并清理内存或临时文件缓存；重复释放无副作用。</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _content.Dispose(); }
        finally { _storage.Dispose(); }
    }

    /// <summary>异步关闭内容读取流并释放底层缓存租约。</summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        try { _content.Dispose(); }
        finally { await _storage.DisposeAsync().ConfigureAwait(false); }
    }

    /// <summary>限制为只读访问，同时保留缓存的定位能力。</summary>
    private sealed class ReadOnlySeekableStream : Stream
    {
        private readonly Stream _inner;
        private bool _disposed;

        internal ReadOnlySeekableStream(Stream inner) => _inner = inner;

        public override bool CanRead => !_disposed && _inner.CanRead;
        public override bool CanSeek => !_disposed && _inner.CanSeek;
        public override bool CanWrite => false;
        public override long Length { get { ThrowIfDisposed(); return _inner.Length; } }
        public override long Position
        {
            get { ThrowIfDisposed(); return _inner.Position; }
            set { ThrowIfDisposed(); _inner.Position = value; }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ThrowIfDisposed();
            return _inner.Read(buffer, offset, count);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return _inner.ReadAsync(buffer, offset, count, cancellationToken);
        }

#if !NETSTANDARD2_0
        public override int Read(Span<byte> buffer)
        {
            ThrowIfDisposed();
            return _inner.Read(buffer);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return _inner.ReadAsync(buffer, cancellationToken);
        }
#endif

        public override long Seek(long offset, SeekOrigin origin)
        {
            ThrowIfDisposed();
            return _inner.Seek(offset, origin);
        }

        public override void Flush() => ThrowIfDisposed();
        public override void SetLength(long value) => throw new NotSupportedException("缓存内容为只读流。");
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException("缓存内容为只读流。");

        protected override void Dispose(bool disposing)
        {
            if (disposing) _disposed = true;
            base.Dispose(disposing);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ReadOnlySeekableStream));
        }
    }
}
