namespace Bing.IO.Buffers;

/// <summary>
/// 字节写入器
/// </summary>
public class BinaryDigitWriter
{
	#region Span

    /// <summary>
    /// 将 <see cref="short"/> 值写入指定的字节数组
    /// </summary>
    /// <param name="buffer">要写入的字节数组。</param>
    /// <param name="value">要写入的 <see cref="short"/> 值。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(Span<byte> buffer, short value)
    {
        buffer[0] = (byte)value;
        buffer[1] = (byte)(value >> 8);
    }

    /// <summary>
    /// 将 <see cref="ushort"/> 值写入指定的字节数组
    /// </summary>
    /// <param name="buffer">要写入的字节数组。</param>
    /// <param name="value">要写入的 <see cref="ushort"/> 值。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(Span<byte> buffer, ushort value)
    {
        buffer[0] = (byte)value;
        buffer[1] = (byte)(value >> 8);
    }

    /// <summary>
    /// 将 <see cref="int"/> 值写入指定的字节数组
    /// </summary>
    /// <param name="buffer">要写入的字节数组。</param>
    /// <param name="value">要写入的 <see cref="int"/> 值。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(Span<byte> buffer, int value)
    {
        buffer[0] = (byte)value;
        buffer[1] = (byte)(value >> 8);
        buffer[2] = (byte)(value >> 16);
        buffer[3] = (byte)(value >> 24);
    }

    /// <summary>
    /// 将 <see cref="uint"/> 值写入指定的字节数组
    /// </summary>
    /// <param name="buffer">要写入的字节数组。</param>
    /// <param name="value">要写入的 <see cref="uint"/> 值。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(Span<byte> buffer, uint value)
    {
        buffer[0] = (byte)value;
        buffer[1] = (byte)(value >> 8);
        buffer[2] = (byte)(value >> 16);
        buffer[3] = (byte)(value >> 24);
    }

    /// <summary>
    /// 将 <see cref="long"/> 值写入指定的字节数组
    /// </summary>
    /// <param name="buffer">要写入的字节数组。</param>
    /// <param name="value">要写入的 <see cref="long"/> 值。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(Span<byte> buffer, long value)
    {
        buffer[0] = (byte)value;
        buffer[1] = (byte)(value >> 8);
        buffer[2] = (byte)(value >> 16);
        buffer[3] = (byte)(value >> 24);
        buffer[4] = (byte)(value >> 32);
        buffer[5] = (byte)(value >> 40);
        buffer[6] = (byte)(value >> 48);
        buffer[7] = (byte)(value >> 56);
    }

    /// <summary>
    /// 将 <see cref="ulong"/> 值写入指定的字节数组
    /// </summary>
    /// <param name="buffer">要写入的字节数组。</param>
    /// <param name="value">要写入的 <see cref="ulong"/> 值。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(Span<byte> buffer, ulong value)
    {
        buffer[0] = (byte)value;
        buffer[1] = (byte)(value >> 8);
        buffer[2] = (byte)(value >> 16);
        buffer[3] = (byte)(value >> 24);
        buffer[4] = (byte)(value >> 32);
        buffer[5] = (byte)(value >> 40);
        buffer[6] = (byte)(value >> 48);
        buffer[7] = (byte)(value >> 56);
    }

    #endregion

    #region Bytes

    /// <summary>
    /// 将 <see cref="short"/> 值写入指定位置的字节数组。
    /// </summary>
    /// <param name="buffer">要写入的字节数组。</param>
    /// <param name="position">写入的起始位置。</param>
    /// <param name="value">要写入的 <see cref="short"/> 值。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(byte[] buffer, int position, short value)
    {
        buffer[position + 0] = (byte)value;
        buffer[position + 1] = (byte)(value >> 8);
    }

    /// <summary>
    /// 将 <see cref="ushort"/> 值写入指定位置的字节数组。
    /// </summary>
    /// <param name="buffer">要写入的字节数组。</param>
    /// <param name="position">写入的起始位置。</param>
    /// <param name="value">要写入的 <see cref="ushort"/> 值。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(byte[] buffer, int position, ushort value)
    {
        buffer[position + 0] = (byte)value;
        buffer[position + 1] = (byte)(value >> 8);
    }

    /// <summary>
    /// 将 <see cref="int"/> 值写入指定位置的字节数组。
    /// </summary>
    /// <param name="buffer">要写入的字节数组。</param>
    /// <param name="position">写入的起始位置。</param>
    /// <param name="value">要写入的 <see cref="int"/> 值。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(byte[] buffer, int position, int value)
    {
        buffer[position + 0] = (byte)value;
        buffer[position + 1] = (byte)(value >> 8);
        buffer[position + 2] = (byte)(value >> 16);
        buffer[position + 3] = (byte)(value >> 24);
    }

    /// <summary>
    /// 将 <see cref="uint"/> 值写入指定位置的字节数组。
    /// </summary>
    /// <param name="buffer">要写入的字节数组。</param>
    /// <param name="position">写入的起始位置。</param>
    /// <param name="value">要写入的 <see cref="uint"/> 值。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(byte[] buffer, int position, uint value)
    {
        buffer[position + 0] = (byte)value;
        buffer[position + 1] = (byte)(value >> 8);
        buffer[position + 2] = (byte)(value >> 16);
        buffer[position + 3] = (byte)(value >> 24);
    }

    /// <summary>
    /// 将 <see cref="long"/> 值写入指定位置的字节数组。
    /// </summary>
    /// <param name="buffer">要写入的字节数组。</param>
    /// <param name="position">写入的起始位置。</param>
    /// <param name="value">要写入的 <see cref="long"/> 值。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(byte[] buffer, int position, long value)
    {
        buffer[position + 0] = (byte)value;
        buffer[position + 1] = (byte)(value >> 8);
        buffer[position + 2] = (byte)(value >> 16);
        buffer[position + 3] = (byte)(value >> 24);
        buffer[position + 4] = (byte)(value >> 32);
        buffer[position + 5] = (byte)(value >> 40);
        buffer[position + 6] = (byte)(value >> 48);
        buffer[position + 7] = (byte)(value >> 56);
    }

    /// <summary>
    /// 将 <see cref="ulong"/> 值写入指定位置的字节数组。
    /// </summary>
    /// <param name="buffer">要写入的字节数组。</param>
    /// <param name="position">写入的起始位置。</param>
    /// <param name="value">要写入的 <see cref="ulong"/> 值。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(byte[] buffer, int position, ulong value)
    {
        buffer[position + 0] = (byte)value;
        buffer[position + 1] = (byte)(value >> 8);
        buffer[position + 2] = (byte)(value >> 16);
        buffer[position + 3] = (byte)(value >> 24);
        buffer[position + 4] = (byte)(value >> 32);
        buffer[position + 5] = (byte)(value >> 40);
        buffer[position + 6] = (byte)(value >> 48);
        buffer[position + 7] = (byte)(value >> 56);
    }

    #endregion
}