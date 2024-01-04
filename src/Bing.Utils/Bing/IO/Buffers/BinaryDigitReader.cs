namespace Bing.IO.Buffers;

/// <summary>
/// 字节读取器
/// </summary>
public class BinaryDigitReader
{
    #region Span

    /// <summary>
    /// 从字节数组中读取2个字节，转换为 <see cref="short"/> 类型。
    /// </summary>
    /// <param name="buffer">包含数据的字节数组。</param>
    /// <returns>转换后的 <see cref="short"/> 值。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short ReadInt16(Span<byte> buffer) => (short)(buffer[0] | buffer[1] << 8);

    /// <summary>
    /// 从字节数组中读取2个字节，转换为 <see cref="ushort"/> 类型。
    /// </summary>
    /// <param name="buffer">包含数据的字节数组。</param>
    /// <returns>转换后的 <see cref="ushort"/> 值。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort ReadUInt16(Span<byte> buffer) => (ushort)(buffer[0] | buffer[1] << 8);

    /// <summary>
    /// 从字节数组中读取4个字节，转换为 <see cref="int"/> 类型。
    /// </summary>
    /// <param name="buffer">包含数据的字节数组。</param>
    /// <returns>转换后的 <see cref="int"/> 值。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ReadInt32(Span<byte> buffer) => buffer[0] | buffer[1] << 8 | buffer[2] << 16 | buffer[3] << 24;

    /// <summary>
    /// 从字节数组中读取4个字节，转换为 <see cref="uint"/> 类型。
    /// </summary>
    /// <param name="buffer">包含数据的字节数组。</param>
    /// <returns>转换后的 <see cref="uint"/> 值。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ReadUInt32(Span<byte> buffer) => (uint)(buffer[0] | buffer[1] << 8 | buffer[2] << 16 | buffer[3] << 24);

    /// <summary>
    /// 从字节数组中读取8个字节，转换为 <see cref="long"/> 类型。
    /// </summary>
    /// <param name="buffer">包含数据的字节数组。</param>
    /// <returns>转换后的 <see cref="long"/> 值。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long ReadInt64(Span<byte> buffer)
    {
        var num = (uint)(buffer[0] | buffer[1] << 8 | buffer[2] << 16 | buffer[3] << 24);
        var num2 = (uint)(buffer[4] | buffer[5] << 8 | buffer[6] << 16 | buffer[7] << 24);
        return (long)((ulong)num2 << 32 | num);
    }

    /// <summary>
    /// 从字节数组中读取8个字节，转换为 <see cref="ulong"/> 类型。
    /// </summary>
    /// <param name="buffer">包含数据的字节数组。</param>
    /// <returns>转换后的 <see cref="ulong"/> 值。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ReadUInt64(Span<byte> buffer)
    {
        var num = (uint)(buffer[0] | buffer[1] << 8 | buffer[2] << 16 | buffer[3] << 24);
        var num2 = (uint)(buffer[4] | buffer[5] << 8 | buffer[6] << 16 | buffer[7] << 24);
        return (ulong)num2 << 32 | num;
    }

    #endregion

    #region Bytes

    /// <summary>
    /// 从字节数组的指定位置读取2个字节，转换为 <see cref="short"/> 类型。
    /// </summary>
    /// <param name="buffer">包含数据的字节数组。</param>
    /// <param name="position">读取的起始位置。</param>
    /// <returns>转换后的 <see cref="short"/> 值。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short ReadInt16(byte[] buffer, int position) =>
        (short)(buffer[position + 0] | buffer[position + 1] << 8);

    /// <summary>
    /// 从字节数组的指定位置读取2个字节，转换为 <see cref="ushort"/> 类型。
    /// </summary>
    /// <param name="buffer">包含数据的字节数组。</param>
    /// <param name="position">读取的起始位置。</param>
    /// <returns>转换后的 <see cref="ushort"/> 值。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort ReadUInt16(byte[] buffer, int position) =>
        (ushort)(buffer[position + 0] | buffer[position + 1] << 8);

    /// <summary>
    /// 从字节数组的指定位置读取4个字节，转换为 <see cref="int"/> 类型。
    /// </summary>
    /// <param name="buffer">包含数据的字节数组。</param>
    /// <param name="position">读取的起始位置。</param>
    /// <returns>转换后的 <see cref="int"/> 值。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ReadInt32(byte[] buffer, int position) =>
        buffer[position + 0] | buffer[position + 1] << 8 | buffer[position + 2] << 16 | buffer[position + 3] << 24;

    /// <summary>
    /// 从字节数组的指定位置读取4个字节，转换为 <see cref="uint"/> 类型。
    /// </summary>
    /// <param name="buffer">包含数据的字节数组。</param>
    /// <param name="position">读取的起始位置。</param>
    /// <returns>转换后的 <see cref="uint"/> 值。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ReadUInt32(byte[] buffer, int position) =>
        (uint)(buffer[position + 0] | buffer[position + 1] << 8 | buffer[position + 2] << 16 | buffer[position + 3] << 24);

    /// <summary>
    /// 从字节数组的指定位置读取8个字节，转换为 <see cref="long"/> 类型。
    /// </summary>
    /// <param name="buffer">包含数据的字节数组。</param>
    /// <param name="position">读取的起始位置。</param>
    /// <returns>转换后的 <see cref="long"/> 值。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long ReadInt64(byte[] buffer, int position)
    {
        var num = (uint)(buffer[position + 0] | buffer[position + 1] << 8 | buffer[position + 2] << 16 | buffer[position + 3] << 24);
        var num2 = (uint)(buffer[position + 4] | buffer[position + 5] << 8 | buffer[position + 6] << 16 | buffer[position + 7] << 24);
        return (long)((ulong)num2 << 32 | num);
    }

    /// <summary>
    /// 从字节数组的指定位置读取8个字节，转换为 <see cref="ulong"/> 类型。
    /// </summary>
    /// <param name="buffer">包含数据的字节数组。</param>
    /// <param name="position">读取的起始位置。</param>
    /// <returns>转换后的 <see cref="ulong"/> 值。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ReadUInt64(byte[] buffer, int position)
    {
        var num = (uint)(buffer[position + 0] | buffer[position + 1] << 8 | buffer[position + 2] << 16 | buffer[position + 3] << 24);
        var num2 = (uint)(buffer[position + 4] | buffer[position + 5] << 8 | buffer[position + 6] << 16 | buffer[position + 7] << 24);
        return (ulong)num2 << 32 | num;
    }

    #endregion
}