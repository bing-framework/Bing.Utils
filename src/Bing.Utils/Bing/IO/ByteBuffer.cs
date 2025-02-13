namespace Bing.IO;

/**
 * Reference to 
 *      https://github.com/XiaZengming/ByteBuffer/blob/master/ByteBuffer.cs
 *      Author: XiaZengming
 *      https://github.com/chenrensong/SS.Toolkit/blob/master/SS.Toolkit/IO/ByteBuffer.cs
 *      Author: chenrensong
 */

/// <summary>
/// 字节缓冲区。
/// </summary>
[Serializable]
public class ByteBuffer
{
    #region 字段

    /// <summary>
    /// 字节缓存区，需保证数组中都是大端模式数据，否则容易出错
    /// </summary>
    private byte[] _buffer;

    /// <summary>
    /// 读取索引
    /// </summary>
    private int _readIndex;

    /// <summary>
    /// 写入索引
    /// </summary>
    private int _writeIndex;

    /// <summary>
    /// 读取索引标记
    /// </summary>
    private int _markReadIndex;

    /// <summary>
    /// 写入索引标记
    /// </summary>
    private int _markWriteIndex;

    /// <summary>
    /// 缓存区字节数组长度
    /// </summary>
    private int _capacity;

    /// <summary>
    /// 对象池
    /// </summary>
    private static Queue<ByteBuffer> _pool = new();

    /// <summary>
    /// 对象池最大容量
    /// </summary>
    private const int PoolMaxCount = 200;

    /// <summary>
    /// 此对象是否池化
    /// </summary>
    private bool IsPool { get; set; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化一个<see cref="ByteBuffer"/>类型的实例
    /// </summary>
    /// <param name="capacity">初始容量。</param>
    private ByteBuffer(int capacity)
    {
        _buffer = new byte[capacity];
        _capacity = capacity;
        _readIndex = 0;
        _writeIndex = 0;
    }

    /// <summary>
    /// 初始化一个<see cref="ByteBuffer"/>类型的实例
    /// </summary>
    /// <param name="bytes">复制此数据并初始化。</param>
    private ByteBuffer(byte[] bytes)
    {
        _buffer = new byte[bytes.Length];
        Array.Copy(bytes, _buffer, bytes.Length);
        _capacity = _buffer.Length;
        _readIndex = 0;
        _writeIndex = bytes.Length;
    }

    #endregion

    #region 属性

    /// <summary>
    /// 设置/获取读指针位置
    /// </summary>
    public int ReaderIndex
    {
        get => _readIndex;
        set
        {
            if (value < 0)
                return;
            _readIndex = value;
        }
    }

    /// <summary>
    /// 设置/获取写指针位置
    /// </summary>
    public int WriterIndex
    {
        get => _writeIndex;
        set
        {
            if (value < 0)
                return;
            _writeIndex = value;
        }
    }

    /// <summary>
    /// 获取可读的有效字节数
    /// </summary>
    public int ReadableBytes => _writeIndex - _readIndex;

    /// <summary>
    /// 获取缓冲区容量大小
    /// </summary>
    public int Capacity => _capacity;

    #endregion

    #region Allocate

    /// <summary>
    /// 构建一个指定初始容量的字节缓冲区<see cref="ByteBuffer"/>对象
    /// </summary>
    /// <param name="capacity">初始容量</param>
    /// <param name="fromPool">
    /// true表示获取一个池化的<see cref="ByteBuffer"/>对象，池化的对象必须在调用Dispose后才回推入池中，此方法为线程安全的。
    /// 当为true时，从池中获取的对象的实际<paramref name="capacity"/>值。
    /// </param>
    /// <returns><see cref="ByteBuffer"/> 对象。</returns>
    public static ByteBuffer Allocate(int capacity, bool fromPool = false)
    {
        if (!fromPool)
            return new ByteBuffer(capacity);
        lock (_pool)
        {
            if (_pool.Count == 0)
                return new ByteBuffer(capacity) { IsPool = true };
            var buffer = _pool.Dequeue();
            buffer.IsPool = true;
            return buffer;
        }
    }

    /// <summary>
    /// 构建缓冲区
    /// </summary>
    /// <param name="bytes">源数据</param>
    /// <param name="fromPool">
    /// true表示从对象池获取，必须调用Disposable后才会推入池中，此方法线程安全。
    /// </param>
    /// <returns><see cref="ByteBuffer"/> 对象。</returns>
    public static ByteBuffer Allocate(byte[] bytes, bool fromPool = false)
    {
        var buffer = Allocate(bytes.Length, fromPool);
        buffer.WriteBytes(bytes);
        return buffer;
    }

    #endregion

    #region WriteBytes

    /// <summary>
    /// 将字节数组从指定起始索引到指定长度的字节写入到缓冲区
    /// </summary>
    /// <param name="bytes">字节数组。</param>
    /// <param name="startIndex">字节数组的起始索引。</param>
    /// <param name="length">要写入的字节数长度。</param>
    public void WriteBytes(byte[] bytes, int startIndex, int length)
    {
        if (length < 0)
            return;
        var total = _writeIndex + length;
        FixSizeAndReset(_buffer.Length, total);
        Array.Copy(bytes, startIndex, _buffer, _writeIndex, length);
        _writeIndex = total;
    }

    /// <summary>
    /// 确定内部字节缓存数组的大小。
    /// </summary>
    /// <param name="currentLength">当前长度。</param>
    /// <param name="futureLength">将来长度。</param>
    /// <returns>将来长度。</returns>
    private int FixSizeAndReset(int currentLength, int futureLength)
    {
        if (futureLength > currentLength)
        {
            // 以较大的当前长度和将来长度为基准，确定内部字节缓存区大小
            var size = Math.Max(FixLength(currentLength), FixLength(futureLength)) * 2;
            var newBuffer = new byte[size];
            Array.Copy(_buffer, 0, newBuffer, 0, currentLength);
            _buffer = newBuffer;
            _capacity = newBuffer.Length;
        }

        return futureLength;
    }

    /// <summary>
    /// 将长度修正为不小于指定值的最近的大于或等于2的幂，如length=7，则返回值为8；length=12，则返回16。
    /// </summary>
    /// <param name="length">输入值。</param>
    /// <returns>修正后的长度。</returns>
    private int FixLength(int length)
    {
        if (length == 0)
            return 1;
        var powerOfTwo = 2; // 2的幂等
        var bitIndex = 2;   // 位索引
        while (powerOfTwo < length)
        {
            powerOfTwo = 1 << bitIndex; // 计算2的幂
            bitIndex++;                 // 递增位索引
        }
        return powerOfTwo;
    }

    /// <summary>
    /// 将字节数组从0到指定长度的元素写入缓冲区
    /// </summary>
    /// <param name="bytes">字节数组。</param>
    /// <param name="length">要写入的字节数长度。</param>
    public void WriteBytes(byte[] bytes, int length) => WriteBytes(bytes, 0, length);

    /// <summary>
    /// 将字节数组全部写入到缓冲区
    /// </summary>
    /// <param name="bytes">字节数组。</param>
    public void WriteBytes(byte[] bytes) => WriteBytes(bytes, bytes.Length);

    #endregion

    #region WriteByte

    /// <summary>
    /// 写入一个 <see cref="byte"/> 数据。
    /// </summary>
    /// <param name="value"><see cref="byte"/> 数据。</param>
    public void WriteByte(int value)
    {
        var b = (byte)value;
        WriteByte(b);
    }

    /// <summary>
    /// 写入一个 <see cref="byte"/> 数据。
    /// </summary>
    /// <param name="value"><see cref="byte"/> 数据。</param>
    public void WriteByte(byte value)
    {
        var afterLength = _writeIndex + 1;
        var length = _buffer.Length;
        FixSizeAndReset(length, afterLength);
        _buffer[_writeIndex] = value;
        _writeIndex = afterLength;
    }

    #endregion

    #region Write

    /// <summary>
    /// 将一个 <see cref="ByteBuffer"/> 的有效字节区写入到此缓冲区中。
    /// </summary>
    /// <param name="buffer">待写入的字节缓存区</param>
    /// <param name="isLittleEndian">是否使用小端顺序。</param>
    public void Write(ByteBuffer buffer, bool isLittleEndian = false)
    {
        if (buffer == null)
            return;
        if(buffer.ReadableBytes<=0)
            return;
        var bytes = buffer.ToArray();
        if(isLittleEndian)
            Array.Reverse(bytes);
        WriteBytes(bytes);
    }

    /// <summary>
    /// 写入一个 <see cref="ushort"/> 数据。
    /// </summary>
    /// <param name="value"><see cref="ushort"/> 数据。</param>
    /// <param name="isLittleEndian">是否使用小端顺序。</param>
    public void WriteUInt16(ushort value, bool isLittleEndian = false) => WriteBytes(Flip(BitConverter.GetBytes(value), isLittleEndian));

    /// <summary>
    /// 翻转字节数组。如果本地字节序列为低字节序列，则进行翻转以转换为高字节序列（大小端）
    /// </summary>
    /// <param name="bytes">字节数组。</param>
    /// <param name="isLittleEndian">是否使用小端顺序。</param>
    private byte[] Flip(byte[] bytes, bool isLittleEndian = false)
    {
        if (BitConverter.IsLittleEndian)
        {
            // 系统为小端，这时候需要的是大段则转换
            if (!isLittleEndian)
                Array.Reverse(bytes);
        }
        else
        {
            // 系统为大端，这时候需要的是小端则转换
            if (isLittleEndian)
                Array.Reverse(bytes);
        }

        return bytes;
    }

    /// <summary>
    /// 写入一个 <see cref="short"/> 数据。
    /// </summary>
    /// <param name="value"><see cref="short"/> 数据。</param>
    /// <param name="isLittleEndian">是否使用小端顺序。</param>
    public void WriteInt16(short value, bool isLittleEndian = false) => WriteBytes(Flip(BitConverter.GetBytes(value), isLittleEndian));

    /// <summary>
    /// 写入一个 <see cref="uint"/> 数据。
    /// </summary>
    /// <param name="value"><see cref="uint"/> 数据。</param>
    /// <param name="isLittleEndian">是否使用小端顺序。</param>
    public void WriteUInt32(uint value, bool isLittleEndian = false) => WriteBytes(Flip(BitConverter.GetBytes(value), isLittleEndian));

    /// <summary>
    /// 写入一个 <see cref="int"/> 数据。
    /// </summary>
    /// <param name="value"><see cref="int"/> 数据。</param>
    /// <param name="isLittleEndian">是否使用小端顺序。</param>
    public void WriteInt32(int value, bool isLittleEndian = false) => WriteBytes(Flip(BitConverter.GetBytes(value), isLittleEndian));

    /// <summary>
    /// 写入一个 <see cref="ulong"/> 数据。
    /// </summary>
    /// <param name="value"><see cref="ulong"/> 数据。</param>
    /// <param name="isLittleEndian">是否使用小端顺序。</param>
    public void WriteUInt64(ulong value, bool isLittleEndian = false) => WriteBytes(Flip(BitConverter.GetBytes(value), isLittleEndian));

    /// <summary>
    /// 写入一个 <see cref="long"/> 数据。
    /// </summary>
    /// <param name="value"><see cref="long"/> 数据。</param>
    /// <param name="isLittleEndian">是否使用小端顺序。</param>
    public void WriteInt64(long value, bool isLittleEndian = false) => WriteBytes(Flip(BitConverter.GetBytes(value), isLittleEndian));

    /// <summary>
    /// 写入一个 <see cref="float"/> 数据。
    /// </summary>
    /// <param name="value"><see cref="float"/> 数据。</param>
    /// <param name="isLittleEndian">是否使用小端顺序。</param>
    public void WriteSingle(float value, bool isLittleEndian = false) => WriteBytes(Flip(BitConverter.GetBytes(value), isLittleEndian));

    /// <summary>
    /// 写入一个 <see cref="double"/> 数据。
    /// </summary>
    /// <param name="value"><see cref="double"/> 数据。</param>
    /// <param name="isLittleEndian">是否使用小端顺序。</param>
    public void WriteDouble(double value, bool isLittleEndian = false) => WriteBytes(Flip(BitConverter.GetBytes(value), isLittleEndian));

    /// <summary>
    /// 写入一个 <see cref="char"/> 数据。
    /// </summary>
    /// <param name="value"><see cref="char"/> 数据。</param>
    /// <param name="isLittleEndian">是否使用小端顺序。</param>
    public void WriteChar(char value, bool isLittleEndian = false) => WriteBytes(Flip(BitConverter.GetBytes(value), isLittleEndian));

    /// <summary>
    /// 写入一个 <see cref="bool"/> 数据。
    /// </summary>
    /// <param name="value"><see cref="bool"/> 数据。</param>
    /// <param name="isLittleEndian">是否使用小端顺序。</param>
    public void WriteBoolean(bool value, bool isLittleEndian = false) => WriteBytes(Flip(BitConverter.GetBytes(value), isLittleEndian));

    #endregion

    #region ReadByte

    /// <summary>
    /// 从当前读取索引处读取一个字节，并将读取索引向前移动。
    /// </summary>
    /// <returns>读取的字节。</returns>
    public byte ReadByte()
    {
        var b = _buffer[_readIndex];
        _readIndex++;
        return b;
    }

    #endregion

    #region ReadBytes

    /// <summary>
    /// 从当前读取索引处读取指定长度的字节数组，并将读取索引向前移动。
    /// </summary>
    /// <param name="targetBytes">目标字节数组。</param>
    /// <param name="targetStartIndex">目标字节数组的起始索引。</param>
    /// <param name="length">要读取的字节数。</param>
    public void ReadBytes(byte[] targetBytes, int targetStartIndex, int length)
    {
        var size = targetStartIndex + length;
        for (var i = targetStartIndex; i < size; i++)
            targetBytes[i] = ReadByte();
    }

    #endregion

    #region Read

    /// <summary>
    /// 读取一个 <see cref="ushort"/> 数据。
    /// </summary>
    /// <param name="isLittleEndian">是否使用小端顺序。</param>
    /// <returns><see cref="ushort"/> 数据。</returns>
    public ushort ReadUInt16(bool isLittleEndian = false) => BitConverter.ToUInt16(Read(2, isLittleEndian), 0);

    /// <summary>
    /// 从当前读取索引处读取指定长度的字节数组，并将读取索引向前移动。
    /// </summary>
    /// <param name="length">要读取的字节数。</param>
    /// <param name="isLittleEndian">是否使用小端顺序。</param>
    /// <returns>读取的字节数组。</returns>
    private byte[] Read(int length, bool isLittleEndian = false)
    {
        var bytes = Get(_readIndex, length);
        bytes = Flip(bytes, isLittleEndian);
        _readIndex += length;
        return bytes;
    }

    /// <summary>
    /// 从指定索引处获取指定长度的字节数组。
    /// </summary>
    /// <param name="index">开始复制的源字节数组的索引。</param>
    /// <param name="length">要复制的字节数。</param>
    /// <returns>复制的字节数组。</returns>
    private byte[] Get(int index, int length)
    {
        var bytes = new byte[length];
        Array.Copy(_buffer, index, bytes, 0, length);
        return bytes;
    }

    /// <summary>
    /// 读取一个 <see cref="short"/> 数据。
    /// </summary>
    /// <param name="isLittleEndian">是否使用小端顺序。</param>
    /// <returns><see cref="short"/> 数据。</returns>
    public short ReadInt16(bool isLittleEndian = false) => BitConverter.ToInt16(Read(2, isLittleEndian), 0);

    /// <summary>
    /// 读取一个 <see cref="uint"/> 数据。
    /// </summary>
    /// <param name="isLittleEndian">是否使用小端顺序。</param>
    /// <returns><see cref="uint"/> 数据。</returns>
    public uint ReadUInt32(bool isLittleEndian = false) => BitConverter.ToUInt32(Read(4, isLittleEndian), 0);

    /// <summary>
    /// 读取一个 <see cref="int"/> 数据。
    /// </summary>
    /// <param name="isLittleEndian">是否使用小端顺序。</param>
    /// <returns><see cref="int"/> 数据。</returns>
    public int ReadInt32(bool isLittleEndian = false) => BitConverter.ToInt32(Read(4, isLittleEndian), 0);

    /// <summary>
    /// 读取一个 <see cref="ulong"/> 数据。
    /// </summary>
    /// <param name="isLittleEndian">是否使用小端顺序。</param>
    /// <returns><see cref="ulong"/> 数据。</returns>
    public ulong ReadUInt64(bool isLittleEndian = false) => BitConverter.ToUInt64(Read(8, isLittleEndian), 0);

    /// <summary>
    /// 读取一个 <see cref="long"/> 数据。
    /// </summary>
    /// <param name="isLittleEndian">是否使用小端顺序。</param>
    /// <returns><see cref="long"/> 数据。</returns>
    public long ReadInt64(bool isLittleEndian = false) => BitConverter.ToInt64(Read(8, isLittleEndian), 0);

    /// <summary>
    /// 读取一个 <see cref="float"/> 数据。
    /// </summary>
    /// <param name="isLittleEndian">是否使用小端顺序。</param>
    /// <returns><see cref="float"/> 数据。</returns>
    public float ReadSingle(bool isLittleEndian = false) => BitConverter.ToSingle(Read(4, isLittleEndian), 0);

    /// <summary>
    /// 读取一个 <see cref="double"/> 数据。
    /// </summary>
    /// <param name="isLittleEndian">是否使用小端顺序。</param>
    /// <returns><see cref="double"/> 数据。</returns>
    public double ReadDouble(bool isLittleEndian = false) => BitConverter.ToDouble(Read(8, isLittleEndian), 0);

    /// <summary>
    /// 读取一个 <see cref="char"/> 数据。
    /// </summary>
    /// <param name="isLittleEndian">是否使用小端顺序。</param>
    /// <returns><see cref="char"/> 数据。</returns>
    public char ReadChar(bool isLittleEndian = false) => BitConverter.ToChar(Read(2, isLittleEndian), 0);

    /// <summary>
    /// 读取一个 <see cref="bool"/> 数据。
    /// </summary>
    /// <param name="isLittleEndian">是否使用小端顺序。</param>
    /// <returns><see cref="bool"/> 数据。</returns>
    public bool ReadBoolean(bool isLittleEndian = false) => BitConverter.ToBoolean(Read(1, isLittleEndian), 0);

    #endregion

    #region GetByte

    /// <summary>
    /// 获取指定索引处的字节值。
    /// </summary>
    /// <param name="index">要获取字节的索引。</param>
    /// <returns>指定索引处的字节值。</returns>
    public byte GetByte(int index) => _buffer[index];

    /// <summary>
    /// 获取当前读取索引处的字节值，并将读取索引递增。
    /// </summary>
    /// <returns>当前读取索引处的字节值。</returns>
    public byte GetByte() => GetByte(_readIndex);


    #endregion

    #region Get

    /// <summary>
    /// 从指定索引处读取一个 <see cref="ushort"/> 数据。
    /// </summary>
    /// <param name="index">要读取的起始索引。</param>
    /// <returns><see cref="ushort"/> 数据。</returns>
    public ushort GetUInt16(int index) => BitConverter.ToUInt16(Get(index, 2), 0);

    /// <summary>
    /// 从当前读取索引处读取一个 <see cref="ushort"/> 数据。
    /// </summary>
    /// <returns><see cref="ushort"/> 数据。</returns>
    public ushort GetUInt16() => GetUInt16(_readIndex);

    /// <summary>
    /// 从指定索引处读取一个 <see cref="short"/> 数据。
    /// </summary>
    /// <param name="index">要读取的起始索引。</param>
    /// <returns><see cref="short"/> 数据。</returns>
    public short GetInt16(int index) => BitConverter.ToInt16(Get(index, 2), 0);

    /// <summary>
    /// 从当前读取索引处读取一个 <see cref="short"/> 数据。
    /// </summary>
    /// <returns><see cref="short"/> 数据。</returns>
    public short GetInt16() => GetInt16(_readIndex);

    /// <summary>
    /// 从指定索引处读取一个 <see cref="uint"/> 数据。
    /// </summary>
    /// <param name="index">要读取的起始索引。</param>
    /// <returns><see cref="uint"/> 数据。</returns>
    public uint GetUInt32(int index) => BitConverter.ToUInt32(Get(index, 4), 0);

    /// <summary>
    /// 从当前读取索引处读取一个 <see cref="uint"/> 数据。
    /// </summary>
    /// <returns><see cref="uint"/> 数据。</returns>
    public uint GetUInt32() => GetUInt32(_readIndex);

    /// <summary>
    /// 从指定索引处读取一个 <see cref="int"/> 数据。
    /// </summary>
    /// <param name="index">要读取的起始索引。</param>
    /// <returns><see cref="int"/> 数据。</returns>
    public int GetInt32(int index) => BitConverter.ToInt32(Get(index, 4), 0);

    /// <summary>
    /// 从当前读取索引处读取一个 <see cref="int"/> 数据。
    /// </summary>
    /// <returns><see cref="int"/> 数据。</returns>
    public int GetInt32() => GetInt32(_readIndex);

    /// <summary>
    /// 从指定索引处读取一个 <see cref="ulong"/> 数据。
    /// </summary>
    /// <param name="index">要读取的起始索引。</param>
    /// <returns><see cref="ulong"/> 数据。</returns>
    public ulong GetUInt64(int index) => BitConverter.ToUInt64(Get(index, 8), 0);

    /// <summary>
    /// 从当前读取索引处读取一个 <see cref="ulong"/> 数据。
    /// </summary>
    /// <returns><see cref="ulong"/> 数据。</returns>
    public ulong GetUInt64() => GetUInt64(_readIndex);

    /// <summary>
    /// 从指定索引处读取一个 <see cref="long"/> 数据。
    /// </summary>
    /// <param name="index">要读取的起始索引。</param>
    /// <returns><see cref="long"/> 数据。</returns>
    public long GetInt64(int index) => BitConverter.ToInt32(Get(index, 8), 0);

    /// <summary>
    /// 从当前读取索引处读取一个 <see cref="long"/> 数据。
    /// </summary>
    /// <returns><see cref="long"/> 数据。</returns>
    public long GetInt64() => GetInt64(_readIndex);

    /// <summary>
    /// 从指定索引处读取一个 <see cref="float"/> 数据。
    /// </summary>
    /// <param name="index">要读取的起始索引。</param>
    /// <returns><see cref="float"/> 数据。</returns>
    public float GetSingle(int index) => BitConverter.ToSingle(Get(index, 4), 0);

    /// <summary>
    /// 从当前读取索引处读取一个 <see cref="float"/> 数据。
    /// </summary>
    /// <returns><see cref="float"/> 数据。</returns>
    public float GetSingle() => GetSingle(_readIndex);

    /// <summary>
    /// 从指定索引处读取一个 <see cref="double"/> 数据。
    /// </summary>
    /// <param name="index">要读取的起始索引。</param>
    /// <returns><see cref="double"/> 数据。</returns>
    public double GetDouble(int index) => BitConverter.ToDouble(Get(index, 8), 0);

    /// <summary>
    /// 从当前读取索引处读取一个 <see cref="double"/> 数据。
    /// </summary>
    /// <returns><see cref="double"/> 数据。</returns>
    public double GetDouble() => GetDouble(_readIndex);

    /// <summary>
    /// 从指定索引处读取一个 <see cref="char"/> 数据。
    /// </summary>
    /// <param name="index">要读取的起始索引。</param>
    /// <returns><see cref="char"/> 数据。</returns>
    public char GetChar(int index) => BitConverter.ToChar(Get(index, 2), 0);

    /// <summary>
    /// 从当前读取索引处读取一个 <see cref="char"/> 数据。
    /// </summary>
    /// <returns><see cref="char"/> 数据。</returns>
    public char GetChar() => GetChar(_readIndex);

    /// <summary>
    /// 从指定索引处读取一个 <see cref="bool"/> 数据。
    /// </summary>
    /// <param name="index">要读取的起始索引。</param>
    /// <returns><see cref="bool"/> 数据。</returns>
    public bool GetBoolean(int index) => BitConverter.ToBoolean(Get(index, 1), 0);

    /// <summary>
    /// 从当前读取索引处读取一个 <see cref="bool"/> 数据。
    /// </summary>
    /// <returns><see cref="bool"/> 数据。</returns>
    public bool GetBoolean() => GetBoolean(_readIndex);

    #endregion

    #region Index

    /// <summary>
    /// 标记读取的索引位置
    /// </summary>
    public void MarkReaderIndex() => _markReadIndex = _readIndex;

    /// <summary>
    /// 标记写入的索引位置
    /// </summary>
    public void MarkWriterIndex() => _markWriteIndex = _writeIndex;

    /// <summary>
    /// 将读取的索引位置重置为标记的读取索引位置
    /// </summary>
    public void ResetReaderIndex() => _readIndex = _markReadIndex;

    /// <summary>
    /// 将写入的索引位置重置为标记的写入索引位置
    /// </summary>
    public void ResetWriterIndex() => _writeIndex = _markWriteIndex;

    #endregion

    #region ToArray

    /// <summary>
    /// 将缓冲区中的有效数据复制到新的字节数组中。
    /// </summary>
    /// <returns>包含缓冲区中有效数据的新字节数组。</returns>
    public byte[] ToArray()
    {
        var bytes = new byte[_writeIndex];
        Array.Copy(_buffer, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    #endregion

    #region CopyRest

    /// <summary>
    /// 复制当前 <see cref="ByteBuffer"/> 对象中未读取部分，不改变原对象的数据，包括已读数据。
    /// </summary>
    /// <returns>复制的 <see cref="ByteBuffer"/> 对象。</returns>
    public ByteBuffer CopyRest()
    {
        if (_buffer == null || _readIndex >= _writeIndex)
            return new ByteBuffer(16);
        // 计算未读取部分的长度，并复制到新的字节数组中
        var length = _writeIndex - _readIndex;
        var newBytes = new byte[length];
        Array.Copy(_buffer, _readIndex, newBytes, 0, newBytes.Length);
        return new ByteBuffer(newBytes)
        {
            IsPool = IsPool
        };
    }

    #endregion

    #region Clone

    /// <summary>
    /// 克隆当前 <see cref="ByteBuffer"/> 对象，具有与原始对象相同的数据，不改变原对象的数据，包括已读数据。
    /// </summary>
    /// <returns>克隆的 <see cref="ByteBuffer"/> 对象。</returns>
    public ByteBuffer Clone()
    {
        if (_buffer == null)
            return new ByteBuffer(16);
        return new ByteBuffer(_buffer)
        {
            _capacity = _capacity,
            _readIndex = _readIndex,
            _writeIndex = _writeIndex,
            _markReadIndex = _markReadIndex,
            _markWriteIndex = _markWriteIndex,
            IsPool = IsPool
        };
    }

    #endregion

    #region ForEach

    /// <summary>
    /// 遍历所有的字节数据
    /// </summary>
    /// <param name="action">操作</param>
    public void ForEach(Action<byte> action)
    {
        for (var i = 0; i < ReadableBytes; i++)
            action.Invoke(_buffer[i]);
    }

    #endregion

    #region Clear And Dispose

    /// <summary>
    /// 丢弃已读取的字节，将未读取的字节移到缓冲区的起始位置。
    /// </summary>
    public void DiscardReadBytes()
    {
        if (_readIndex <= 0)
            return;
        // 计算未读取字节的长度
        var remainingLength = _buffer.Length - _readIndex;
        // 创建新的缓冲区，将未读取的字节复制到起始位置
        var newBuffer = new byte[remainingLength];
        Array.Copy(_buffer, _readIndex, newBuffer, 0, remainingLength);
        // 更新缓冲区和相关索引
        _buffer = newBuffer;
        _writeIndex -= _readIndex;
        _markReadIndex -= _readIndex;
        // 重新计算标记的读写索引
        if (_markReadIndex < 0)
            _markReadIndex = 0; // _markReadIndex = _readIndex
        _markWriteIndex -= _readIndex;
        if (_markWriteIndex < 0 || _markWriteIndex < _readIndex || _markWriteIndex < _markReadIndex)
            _markWriteIndex = _writeIndex;
        // 重置读索引
        _readIndex = 0;
    }

    /// <summary>
    /// 清空此对象，但保留字节缓存数组（空数组）
    /// </summary>
    public void Clear()
    {
#if NET6_0_OR_GREATER
        Array.Clear(_buffer);
#else
        Array.Clear(_buffer, 0, _buffer.Length);
#endif
        _readIndex = 0;
        _writeIndex = 0;
        _markReadIndex = 0;
        _markWriteIndex = 0;
        _capacity = _buffer.Length;
    }

    /// <summary>
    /// 释放对象，清楚字节缓存数组。如果此对象可池化，则推入到对象池。
    /// </summary>
    public void Dispose()
    {
        if (IsPool)
        {
            lock (_pool)
            {
                if (_pool.Count < PoolMaxCount)
                {
                    Clear();
                    _pool.Enqueue(this);
                }
            }
        }
        _readIndex = 0;
        _writeIndex = 0;
        _markReadIndex = 0;
        _markWriteIndex = 0;
        _capacity = 0;
        _buffer = null;
    }

    #endregion
}