using System.Buffers;

namespace Bing.IO;

/// <summary>
/// 大型内存流，最大可支持1TB数据，推荐当数据流大于2GB时使用。
/// </summary>
public class LargeMemoryStream : Stream
{
    /// <summary>
    /// 单页大小，用于定义每个字节数组的最大长度。
    /// </summary>
    private const int PageSize = 1024000000;

    /// <summary>
    /// 分配步长，用于在扩展_streamBuffers数组时增加的数组数量。
    /// </summary>
    private const int AllocStep = 1024;

    /// <summary>
    /// 二维字节数组，用作数据存储的页。
    /// </summary>
    private byte[][] _streamBuffers;

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

    /// <summary>
    /// 可读
    /// </summary>
    public override bool CanRead => true;

    /// <summary>
    /// 可查找
    /// </summary>
    public override bool CanSeek => true;

    /// <summary>
    /// 可写
    /// </summary>
    public override bool CanWrite => true;

    /// <summary>
    /// 长度
    /// </summary>
    public override long Length => _length;

    /// <summary>
    /// 位置
    /// </summary>
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
    public Span<byte[]> GetSpan() => _streamBuffers.AsSpan(0, _streamBuffers.Length);

    /// <summary>
    /// 获取包含所有字节缓冲区的Memory。
    /// </summary>
    /// <returns>包含所有字节缓冲区的Memory。</returns>
    public Memory<byte[]> GetMemory() => _streamBuffers.AsMemory(0, _streamBuffers.Length);

    /// <summary>
    /// 将包含所有字节缓冲区转换为ArraySegment。
    /// </summary>
    /// <returns>包含所有字节缓冲区的ArraySegment。</returns>
    public ArraySegment<byte[]> ToArraySegment() => new(_streamBuffers, 0, _streamBuffers.Length);

    /// <summary>
    /// 清理所有缓冲的数据。
    /// </summary>
    /// <remarks>
    /// 此方法主要执行了资源是否已释放的检查。
    /// 尝试在已释放的流上调用此方法将引发ObjectDisposedException。
    /// </remarks>
    public override void Flush() => AssertNotDisposed();

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        AssertNotDisposed();
        var currentPage = (int)(_position / PageSize);
        var currentOffset = (int)(_position % PageSize);
        var currentLength = PageSize - currentOffset;
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
            currentLength = PageSize;
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
                offset += _position;
                break;

            case SeekOrigin.End:
                offset = _length - offset;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(origin));
        }

        return Position = offset;
    }

    /// <inheritdoc />
    public override void SetLength(long value)
    {
        switch (value)
        {
            case < 0:
                throw new InvalidOperationException("SetLength < 0");
            case 0:
                _streamBuffers = null;
                _allocatedBytes = _position = _length = 0;
                _pageCount = 0;
                return;
        }

        var currentPageCount = GetPageCount(_allocatedBytes);
        var neededPageCount = GetPageCount(value);
        while (currentPageCount > neededPageCount)
        {
            ArrayPool<byte>.Shared.Return(_streamBuffers[--currentPageCount], true);
            _streamBuffers[currentPageCount] = null;
        }

        AllocSpaceIfNeeded(value);
        if (_position > (_length = value))
            _position = _length;
    }

    /// <summary>
    /// 根据给定的长度计算所需的页数。
    /// </summary>
    /// <param name="length">需要存储的数据长度。</param>
    /// <returns>根据给定长度计算出的页数。</returns>
    private int GetPageCount(long length)
    {
        var pageCount = (int)(length / PageSize) + 1;
        if (length % PageSize == 0)
            pageCount--;

        return pageCount;
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

            _streamBuffers[currentPageCount++] = ArrayPool<byte>.Shared.Rent(PageSize);
        }

        _allocatedBytes = (long)currentPageCount * PageSize;
        value = Math.Max(value, _length);
        if (_position > (_length = value))
            _position = _length;
    }

    /// <summary>
    /// 扩展存储页的数组，以容纳更多的页。
    /// </summary>
    private void ExtendPages()
    {
        if (_streamBuffers == null)
        {
            _streamBuffers = new byte[AllocStep][];
        }
        else
        {
            var streamBuffers = new byte[_streamBuffers.Length + AllocStep][];
            Buffer.BlockCopy(_streamBuffers, 0, streamBuffers, 0, _streamBuffers.Length);
            _streamBuffers = streamBuffers;
        }

        _pageCount = _streamBuffers.Length;
    }

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
    {
        AssertNotDisposed();
        var currentPage = (int)(_position / PageSize);
        var currentOffset = (int)(_position % PageSize);
        var currentLength = PageSize - currentOffset;
        AllocSpaceIfNeeded(_position + count);
        while (count != 0)
        {
            if (currentLength > count)
                currentLength = count;
            Buffer.BlockCopy(buffer, offset, _streamBuffers[currentPage++], currentOffset, currentLength);
            offset += currentLength;
            _position += currentLength;
            count -= currentLength;
            currentOffset = 0;
            currentLength = PageSize;
        }
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _isDisposed = true;
            Position = 0;
            _length = 0;
            if (_streamBuffers != null)
            {
                foreach (var bytes in _streamBuffers)
                {
                    if (bytes != null)
                        ArrayPool<byte>.Shared.Return(bytes);
                }
                _streamBuffers = null;
            }
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// 检查当前流实例是否已被释放。
    /// </summary>
    /// <exception cref="ObjectDisposedException">当尝试操作一个已经被释放的流时抛出。</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AssertNotDisposed()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(PooledMemoryStream));
    }

    /// <summary>
    /// LargeMemoryStream 析构函数，确保资源被正确释放。
    /// </summary>
    ~LargeMemoryStream() => Dispose(true);
}