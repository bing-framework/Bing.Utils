using System.Buffers;
using System.Collections;

namespace Bing.IO;

/// <summary>
/// 提供一个基于内存池的流实现，旨在减少频繁内存分配和回收造成的性能开销。
/// </summary>
/// <remarks>
/// PooledMemoryStream使用ArrayPool来管理内存的分配，这有助于提高应用程序性能，
/// 尤其是在需要频繁创建和销毁大量内存流的场景中。这个类确保了内存的有效重用，
/// 并提供了与System.IO.Stream相似的API接口，使其可以方便地用于现有代码中。
/// </remarks>
public sealed class PooledMemoryStream : Stream, IEnumerable<byte>
{
    /// <summary>
    /// 当内部缓冲区需要扩展时使用的过扩展因子，决定新缓冲区大小是当前所需容量的倍数。
    /// </summary>
    /// <remarks>
    /// 设置过扩展因子为2意味着，每当内存流的容量需要增加时，新的容量将是所需容量的两倍。
    /// 这有助于减少因频繁扩容而导致的内存分配次数，从而提高性能。
    /// </remarks>
    private const float OverExpansionFactor = 2;

    /// <summary>
    /// 内部用于存储流数据的缓冲区。
    /// </summary>
    /// <remarks>
    /// 该缓冲区从一个ArrayPool中租借而来，用于临时存储流中的数据。
    /// 使用ArrayPool可以减少GC压力和提高内存利用率。
    /// </remarks>
    private byte[] _data = [];

    /// <summary>
    /// 当前流中数据的实际长度。
    /// </summary>
    /// <remarks>
    /// 与_data数组的长度不同，_length表示流中当前存储的有效数据长度。
    /// </remarks>
    private int _length;

    /// <summary>
    /// 用于管理缓冲区内存的ArrayPool实例。
    /// </summary>
    /// <remarks>
    /// 提供了一种机制，通过重用大型数组来减少显著的内存分配和回收。
    /// </remarks>
    private readonly ArrayPool<byte> _pool;

    /// <summary>
    /// 指示当前流实例是否已被释放。
    /// </summary>
    /// <remarks>
    /// 在Dispose方法调用后，此字段被设置为true，标记流不再可用。
    /// </remarks>
    private bool _isDisposed;

    /// <summary>
    /// 初始化一个<see cref="PooledMemoryStream"/>类型的实例
    /// </summary>
    public PooledMemoryStream() : this(ArrayPool<byte>.Shared) { }

    /// <summary>
    /// 初始化一个<see cref="PooledMemoryStream"/>类型的实例
    /// </summary>
    /// <param name="buffer">流的初始内容。</param>
    public PooledMemoryStream(byte[] buffer) : this(ArrayPool<byte>.Shared, buffer.Length)
    {
        Buffer.BlockCopy(buffer, 0, _data, 0, buffer.Length);
        _length = buffer.Length;
    }

    /// <summary>
    /// 初始化一个<see cref="PooledMemoryStream"/>类型的实例
    /// </summary>
    /// <param name="arrayPool">用于分配缓冲区的内存池。</param>
    /// <param name="capacity">流的初始容量。</param>
    /// <exception cref="ArgumentNullException">如果 <paramref name="arrayPool"/> 为 null，则抛出此异常。</exception>
    public PooledMemoryStream(ArrayPool<byte> arrayPool, int capacity = 0)
    {
        _pool = arrayPool ?? throw new ArgumentNullException(nameof(arrayPool));
        if (capacity > 0)
            _data = _pool.Rent(capacity);
    }

    /// <summary>
    /// 可读
    /// </summary>
    public override bool CanRead => true;

    /// <summary>
    /// 可查找
    /// </summary>
    public override bool CanSeek => true;

    /// <summary>
    /// 科协
    /// </summary>
    public override bool CanWrite => true;

    /// <summary>
    /// 长度
    /// </summary>
    public override long Length => _length;

    /// <summary>
    /// 位置
    /// </summary>
    public override long Position { get; set; }

    /// <summary>
    /// 获取当前流的容量，即流中可以存储的字节数。
    /// </summary>
    public long Capacity => _data?.Length ?? 0;

    /// <summary>
    /// 获取表示流当前内容的Span。
    /// </summary>
    /// <returns>包含流数据的Span。</returns>
    public Span<byte> GetSpan() => _data.AsSpan(0, _length);

    /// <summary>
    /// 获取表示流当前内容的Memory。
    /// </summary>
    /// <returns>包含流数据的Memory。</returns>
    public Memory<byte> GetMemory() => _data.AsMemory(0, _length);

    /// <summary>
    /// 将流内容转换为ArraySegment。
    /// </summary>
    /// <returns>包含流数据的ArraySegment。</returns>
    public ArraySegment<byte> ToArraySegment() => new(_data, 0, (int)Length);

    /// <summary>
    /// 清理所有缓冲的数据。
    /// </summary>
    /// <remarks>
    /// 此方法主要执行了资源是否已释放的检查。
    /// 尝试在已释放的流上调用此方法将引发ObjectDisposedException。
    /// </remarks>
    public override void Flush() => AssertNotDisposed();

    /// <summary>
    /// 从当前流中读取字节序列到指定的缓冲区，并根据读取的字节数向前移动流的当前位置。
    /// </summary>
    /// <param name="buffer">目标缓冲区数组，从当前流中读取的数据将被存放在这个数组中。</param>
    /// <param name="offset">buffer 中的字节偏移量，从此处开始存放从当前流中读取的数据。</param>
    /// <param name="count">最大读取的字节数量。</param>
    /// <returns>实际读取的字节数量。如果流的末尾已到达，则可能少于请求的字节数量。</returns>
    public override int Read(byte[] buffer, int offset, int count)
    {
        AssertNotDisposed();
        if (count == 0)
            return 0;
        var available = Math.Min(count, Length - Position);
        Array.Copy(_data, Position, buffer, offset, available);
        Position += available;
        return (int)available;
    }

    /// <summary>
    /// 设置当前流中的位置。
    /// </summary>
    /// <param name="offset">相对于<paramref name="origin"/>参数所指定的位置的偏移量。</param>
    /// <param name="origin">一个<see cref="SeekOrigin"/>枚举值，指示用于获取新位置的参考点。</param>
    /// <returns>当前流中的新位置。</returns>
    /// <exception cref="ObjectDisposedException">如果流已被释放，则抛出此异常。</exception>
    /// <exception cref="ArgumentOutOfRangeException">如果计算得出的新位置超出了流的有效容量范围，则抛出此异常。</exception>
    /// <remarks>
    /// 此方法允许将流的当前位置向前或向后移动，也可以将位置设置为流的开始或结束位置附近的一个指定偏移量。
    /// 它首先验证新计算的位置是否有效（即不小于0且不大于流的容量）。
    /// 如果指定的新位置有效，方法将更新流的当前位置和长度（如果新位置超出了当前长度）。
    /// 特别地，如果是向流的末尾之后寻求，将会扩展流的容量以容纳新位置。
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override long Seek(long offset, SeekOrigin origin)
    {
        AssertNotDisposed();
        switch (origin)
        {
            case SeekOrigin.Current:
                if (Position + offset < 0 || Position + offset > Capacity)
                    throw new ArgumentOutOfRangeException(nameof(offset));
                Position += offset;
                _length = (int)Math.Max(Position, _length);
                return Position;

            case SeekOrigin.Begin:
                if (offset < 0 || offset > Capacity)
                    throw new ArgumentOutOfRangeException(nameof(offset));
                Position = offset;
                _length = (int)Math.Max(Position, _length);
                return Position;

            case SeekOrigin.End:
                if (Length + offset < 0)
                    throw new ArgumentOutOfRangeException(nameof(offset));
                if (Length + offset > Capacity)
                    SetCapacity((int)(Length + offset));
                Position = Length + offset;
                _length = (int)Math.Max(Position, _length);
                return Position;

            default:
                throw new ArgumentOutOfRangeException(nameof(origin));
        }
    }

    /// <summary>
    /// 设置当前流的长度。
    /// </summary>
    /// <param name="value">流的新长度。</param>
    /// <exception cref="ObjectDisposedException">如果流已被释放，则抛出此异常。</exception>
    /// <exception cref="ArgumentOutOfRangeException">如果<paramref name="value"/>小于0，则抛出此异常。</exception>
    /// <remarks>
    /// 此方法用于调整流的长度。如果指定的新长度大于当前流的容量，将扩展流的容量以适应新长度。
    /// 如果新长度小于当前流的长度，流将被截断，丢弃超出新长度部分的数据。
    /// 如果当前流的位置超出了新设置的长度，流的位置将调整为新长度的末尾。
    /// </remarks>
    public override void SetLength(long value)
    {
        AssertNotDisposed();
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value));
        if (value > Capacity)
            SetCapacity((int)value);
        _length = (int)value;
        if (Position > Length)
            Position = Length;
    }

    /// <summary>
    /// 将指定缓冲区中的数据写入当前流。
    /// </summary>
    /// <param name="buffer">源数据的缓冲区。</param>
    /// <param name="offset">buffer 中开始写入数据的起始位置。</param>
    /// <param name="count">要写入的字节数。</param>
    /// <exception cref="ObjectDisposedException">如果流已被释放，则抛出此异常。</exception>
    /// <remarks>
    /// 此方法从 buffer 参数指定的缓冲区复制 count 个字节到当前流中，从 offset 指定的位置开始读取。
    /// 如果写入的数据量会超出当前流的容量，则自动扩展流的容量以容纳新数据，扩展策略基于 OverExpansionFactor 因子。
    /// 写入操作完成后，流的当前位置会增加实际写入的字节数量，并且可能更新流的长度，如果写入操作扩展了流的末尾。
    /// </remarks>
    public override void Write(byte[] buffer, int offset, int count)
    {
        AssertNotDisposed();
        if (count == 0)
            return;
        if (Capacity - Position < count)
            SetCapacity((int)(OverExpansionFactor * (Position + count)));
        Array.Copy(buffer, offset, _data, Position, count);
        Position += count;
        _length = (int)Math.Max(Position, _length);
    }

    /// <summary>
    /// 将流内容写入另一个流。
    /// </summary>
    /// <param name="stream">要写入的目标流。</param>
    public void WriteTo(Stream stream)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        AssertNotDisposed();
        stream.Write(_data, 0, (int)Length);
    }

    /// <summary>
    /// 获取流的内部缓冲区的副本。
    /// </summary>
    /// <returns>流数据的字节数组。</returns>
    public byte[] GetBuffer()
    {
        AssertNotDisposed();
        if (_data.Length == Length)
            return _data;
        var buffer = new byte[Length];
        Buffer.BlockCopy(_data, 0, buffer, 0, buffer.Length);
        return buffer;
    }

    /// <summary>
    /// 将流内容转换为字节数组。
    /// </summary>
    /// <returns>流数据的字节数组。</returns>
    public byte[] ToArray() => GetBuffer();

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _isDisposed = true;
            Position = 0;
            _length = 0;
            if (_data != null)
            {
                _pool.Return(_data);
                _data = null;
            }
        }
        base.Dispose(disposing);
    }

    /// <summary>
    /// 调整内部缓冲区的容量至指定的大小。
    /// </summary>
    /// <param name="newCapacity">缓冲区新的容量。</param>
    /// <remarks>
    /// 如果新的容量大于当前缓冲区的大小，此方法会从内存池中租借一个更大的缓冲区，
    /// 并将当前缓冲区的内容复制到新的缓冲区中。完成复制后，原有的缓冲区将被返回到内存池。
    /// 这个操作确保了`PooledMemoryStream`能够动态调整其存储容量，以适应不断变化的数据大小，
    /// 同时通过重用内存池中的缓冲区来减少内存分配的开销。
    /// </remarks>
    private void SetCapacity(int newCapacity)
    {
        var newData = _pool.Rent(newCapacity);
        if (_data != null)
        {
            Array.Copy(_data, 0, newData, 0, Position);
            _pool.Return(_data);
        }
        _data = newData;
    }

    /// <summary>
    /// 检查当前流实例是否已被释放。
    /// </summary>
    /// <exception cref="ObjectDisposedException">当尝试操作一个已经被释放的流时抛出。</exception>
    /// <remarks>
    /// 在进行任何读写或设置操作之前调用此方法，确保流实例处于有效状态。
    /// 如果流已被释放（即_isDisposed字段为true），则抛出ObjectDisposedException异常，
    /// 阻止对已释放资源的进一步操作。这是一种常见的模式，用于管理可释放资源的类，
    /// 保护类的方法不在资源被释放后被调用，从而避免潜在的运行时错误。
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AssertNotDisposed()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(PooledMemoryStream));
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// 获取枚举器
    /// </summary>
    public IEnumerator<byte> GetEnumerator()
    {
        for (var i = 0; i < Length; i++)
            yield return _data[i];
    }

    /// <summary>
    /// PooledMemoryStream 析构函数，确保资源被正确释放。
    /// </summary>
    ~PooledMemoryStream() => Dispose(true);
}