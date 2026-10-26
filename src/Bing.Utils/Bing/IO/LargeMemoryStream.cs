using System.Buffers;

namespace Bing.IO;

/// <summary>
/// 大型内存流，最大可支持1TB数据，推荐当数据流大于2GB时使用。
/// </summary>
public class LargeMemoryStream : Stream
{
    /// <summary>
    /// 默认单页大小。
    /// </summary>
    private const int PageSize = 64 * 1024;

    /// <summary>
    /// 初始页表可容纳的页面数量。
    /// </summary>
    private const int InitialPageTableCapacity = 4;

    /// <summary>
    /// 当前实例的页大小。
    /// </summary>
    private readonly int _pageSize;

    /// <summary>
    /// 负责页面租用和归还的缓冲池。
    /// </summary>
    private readonly ArrayPool<byte> _pool;

    /// <summary>
    /// 使用默认页大小创建大型内存流。
    /// </summary>
    public LargeMemoryStream() : this(PageSize, ArrayPool<byte>.Shared) { }

    /// <summary>
    /// 使用指定页大小创建大型内存流。
    /// </summary>
    /// <param name="pageSize">每页容量，必须大于零。</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="pageSize"/> 小于或等于零。</exception>
    public LargeMemoryStream(int pageSize) : this(pageSize, ArrayPool<byte>.Shared) { }

    /// <summary>
    /// 使用默认页大小和指定缓冲池创建流。
    /// </summary>
    /// <param name="pool">用于租用和归还页面的缓冲池。</param>
    /// <exception cref="ArgumentNullException"><paramref name="pool"/> 为 null。</exception>
    internal LargeMemoryStream(ArrayPool<byte> pool) : this(PageSize, pool) { }

    /// <summary>
    /// 使用指定页大小和缓冲池创建流。
    /// </summary>
    /// <param name="pageSize">每页容量，必须大于零。</param>
    /// <param name="pool">用于租用和归还页面的缓冲池。</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="pageSize"/> 小于或等于零。</exception>
    /// <exception cref="ArgumentNullException"><paramref name="pool"/> 为 null。</exception>
    internal LargeMemoryStream(int pageSize, ArrayPool<byte> pool)
    {
        if (pageSize <= 0) throw new ArgumentOutOfRangeException(nameof(pageSize));
        _pool = pool ?? throw new ArgumentNullException(nameof(pool));
        _pageSize = pageSize;
    }

    /// <summary>
    /// 二维字节数组，用作数据存储的页。
    /// </summary>
    private byte[][] _streamBuffers = Array.Empty<byte[]>();

    /// <summary>
    /// 当前分配的页数。
    /// </summary>
    private int _pageCount;

    /// <summary>
    /// 当前分配的总字节数。
    /// </summary>
    private long _allocatedBytes;

    /// <summary>
    /// 流中的当前位置。
    /// </summary>
    private long _position;

    /// <summary>
    /// 流的当前长度。
    /// </summary>
    private long _length;

    /// <summary>
    /// 指示流实例是否已被释放。
    /// </summary>
    private bool _isDisposed;

    /// <inheritdoc />
    public override bool CanRead => true;

    /// <inheritdoc />
    public override bool CanSeek => true;

    /// <inheritdoc />
    public override bool CanWrite => true;

    /// <inheritdoc />
    public override long Length => _length;

    /// <inheritdoc />
    public override long Position
    {
        get => _position;
        set
        {
            if (value > _length)
                throw new InvalidOperationException("Position > Length");
            if (value < 0)
                throw new InvalidOperationException("Position < 0");
            _position = value;
        }
    }

    /// <summary>
    /// 获取表包含所有字节缓冲区的Span。
    /// </summary>
    /// <returns>包含所有字节缓冲区的Span。</returns>
    /// <remarks>返回借用的页面视图；页面仅在对应流未缩容、清空或释放期间有效，逻辑数据以 Length 为准。</remarks>
    public Span<byte[]> GetSpan() => _streamBuffers.AsSpan(0, _streamBuffers.Length);

    /// <summary>
    /// 获取包含所有字节缓冲区的Memory。
    /// </summary>
    /// <returns>包含所有字节缓冲区的Memory。</returns>
    /// <remarks>返回借用的页面视图；页面仅在对应流未缩容、清空或释放期间有效，逻辑数据以 Length 为准。</remarks>
    public Memory<byte[]> GetMemory() => _streamBuffers.AsMemory(0, _streamBuffers.Length);

    /// <summary>
    /// 将包含所有字节缓冲区转换为ArraySegment。
    /// </summary>
    /// <returns>包含所有字节缓冲区的ArraySegment。</returns>
    /// <remarks>返回借用的页面视图；页面仅在对应流未缩容、清空或释放期间有效，逻辑数据以 Length 为准。</remarks>
    public ArraySegment<byte[]> ToArraySegment() => new(_streamBuffers, 0, _streamBuffers.Length);

    /// <summary>
    /// 获取仅覆盖逻辑长度的只读页面片段，不改变当前位置。
    /// </summary>
    /// <returns>顺序拼接后恰好为 Length 字节的页面视图；空流返回空列表。</returns>
    /// <remarks>片段借用池内存，不是快照；修改、扩容、缩容或释放流后不可继续依赖其内容，流不支持并发访问。</remarks>
    public IReadOnlyList<ReadOnlyMemory<byte>> GetWrittenSegments()
    {
        AssertNotDisposed();
        var count = checked((int)(_length / _pageSize + (_length % _pageSize == 0 ? 0 : 1)));
        var result = new ReadOnlyMemory<byte>[count];
        var remaining = _length;
        for (var page = 0; page < count; page++)
        {
            var length = (int)Math.Min(remaining, _pageSize);
            result[page] = new ReadOnlyMemory<byte>(_streamBuffers[page], 0, length);
            remaining -= length;
        }
        return result;
    }

    /// <inheritdoc />
    /// <remarks>
    /// 此方法主要执行了资源是否已释放的检查。
    /// 尝试在已释放的流上调用此方法将引发ObjectDisposedException。
    /// </remarks>
    public override void Flush() => AssertNotDisposed();

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        AssertNotDisposed();
        ValidateBuffer(buffer, offset, count);
        var currentPage = (int)(_position / _pageSize);
        var currentOffset = (int)(_position % _pageSize);
        var currentLength = _pageSize - currentOffset;
        var startPosition = _position;
        if (startPosition + count > _length)
            count = (int)(_length - startPosition);
        while (count != 0 && _position < _length)
        {
            if (currentLength > count)
                currentLength = count;
            Buffer.BlockCopy(_streamBuffers[currentPage++], currentOffset, buffer, offset, currentLength);
            offset += currentLength;
            _position += currentLength;
            count -= currentLength;
            currentOffset = 0;
            currentLength = _pageSize;
        }
        return (int)(_position - startPosition);
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin)
    {
        AssertNotDisposed();
        switch (origin)
        {
            case SeekOrigin.Begin:
                break;

            case SeekOrigin.Current:
                offset = checked(offset + _position);
                break;

            case SeekOrigin.End:
                offset = checked(_length + offset);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(origin));
        }

        return Position = offset;
    }

    /// <inheritdoc />
    public override void SetLength(long value)
    {
        AssertNotDisposed();
        if (value < 0)
            throw new InvalidOperationException("SetLength < 0");
        var currentPageCount = GetPageCount(_allocatedBytes);
        var neededPageCount = GetPageCount(value);
        while (currentPageCount > neededPageCount)
        {
            _pool.Return(_streamBuffers[--currentPageCount], true);
            _streamBuffers[currentPageCount] = null;
            _allocatedBytes = (long)currentPageCount * _pageSize;
        }
        AllocSpaceIfNeeded(value);
        if (value > _length)
            ClearRange(_length, value - _length);
        _length = value;
        if (_position > value)
            _position = value;
        if (value == 0)
        {
            _streamBuffers = Array.Empty<byte[]>();
            _pageCount = 0;
        }
    }

    /// <summary>
    /// 根据给定的长度计算所需的页数。
    /// </summary>
    /// <param name="length">需要存储的数据长度。</param>
    /// <returns>根据给定长度计算出的页数。</returns>
    private int GetPageCount(long length)
    {
        return checked((int)(length / _pageSize + (length % _pageSize == 0 ? 0 : 1)));
    }

    /// <summary>
    /// 根据需要的空间量分配足够的页。
    /// </summary>
    /// <param name="value">期望分配的空间量。</param>
    /// <exception cref="InvalidOperationException">如果期望的空间量小于0，抛出此异常。</exception>
    private void AllocSpaceIfNeeded(long value)
    {
        switch (value)
        {
            case < 0:
                throw new InvalidOperationException("AllocSpaceIfNeeded < 0");
            case 0:
                return;
        }

        var currentPageCount = GetPageCount(_allocatedBytes);
        var neededPageCount = GetPageCount(value);
        while (currentPageCount < neededPageCount)
        {
            if (currentPageCount == _pageCount)
                ExtendPages();

            _streamBuffers[currentPageCount++] = _pool.Rent(_pageSize);
            // 每次租用后记录，异常时也能释放已获得的页面。
            _allocatedBytes = (long)currentPageCount * _pageSize;
        }

        _allocatedBytes = (long)currentPageCount * _pageSize;
    }

    /// <summary>
    /// 扩展存储页的数组，以容纳更多的页。
    /// </summary>
    /// <exception cref="OverflowException">页表容量翻倍时超出可表示范围。</exception>
    private void ExtendPages()
    {
        var capacity = _streamBuffers.Length == 0
            ? InitialPageTableCapacity
            : checked(_streamBuffers.Length * 2);
        var streamBuffers = new byte[capacity][];
        Array.Copy(_streamBuffers, streamBuffers, _streamBuffers.Length);
        _streamBuffers = streamBuffers;
        _pageCount = capacity;
    }

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
    {
        AssertNotDisposed();
        var currentPage = (int)(_position / _pageSize);
        var currentOffset = (int)(_position % _pageSize);
        var currentLength = _pageSize - currentOffset;
        ValidateBuffer(buffer, offset, count);
        var end = checked(_position + count);
        AllocSpaceIfNeeded(end);
        while (count != 0)
        {
            if (currentLength > count)
                currentLength = count;
            Buffer.BlockCopy(buffer, offset, _streamBuffers[currentPage++], currentOffset, currentLength);
            offset += currentLength;
            _position += currentLength;
            count -= currentLength;
            currentOffset = 0;
            currentLength = _pageSize;
        }
        _length = Math.Max(_length, _position);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override void Dispose(bool disposing)
    {
        if (disposing && !_isDisposed)
        {
            _isDisposed = true;
            _position = 0;
            _length = 0;
            if (_streamBuffers != null)
            {
                foreach (var bytes in _streamBuffers)
                {
                    if (bytes != null)
                        _pool.Return(bytes);
                }
                _streamBuffers = Array.Empty<byte[]>();
                _allocatedBytes = 0;
                _pageCount = 0;
            }
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// 清零新增的逻辑内容，避免初始化整页的额外开销。
    /// </summary>
    private void ClearRange(long start, long count)
    {
        while (count > 0)
        {
            var page = (int)(start / _pageSize);
            var offset = (int)(start % _pageSize);
            var length = (int)Math.Min(count, _pageSize - offset);
            Array.Clear(_streamBuffers[page], offset, length);
            start += length;
            count -= length;
        }
    }

    /// <summary>
    /// 在操作页面前验证调用方缓冲区范围。
    /// </summary>
    private static void ValidateBuffer(byte[] buffer, int offset, int count)
    {
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));
        if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        if (offset > buffer.Length - count) throw new ArgumentException("缓冲区范围无效。");
    }

    /// <summary>
    /// 检查当前流实例是否已被释放。
    /// </summary>
    /// <exception cref="ObjectDisposedException">当尝试操作一个已经被释放的流时抛出。</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AssertNotDisposed()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(LargeMemoryStream));
    }

    /// <summary>
    /// LargeMemoryStream 析构函数，确保资源被正确释放。
    /// </summary>
    ~LargeMemoryStream() => Dispose(true);
}
