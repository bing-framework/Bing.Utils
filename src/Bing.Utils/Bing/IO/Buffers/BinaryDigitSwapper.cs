namespace Bing.IO.Buffers;

/// <summary>
/// 字节交换器
/// </summary>
public class BinaryDigitSwapper
{
    /// <summary>
    /// 交换 <see cref="short"/> 前后 8 位的值
    /// </summary>
    /// <param name="v">要交换字节顺序的 <see cref="short"/> 值。</param>
    /// <returns>交换字节顺序后的 <see cref="short"/> 值。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short SwapInt16(short v) =>
        (short)(((v & 0xff) << 8) | ((v >> 8) & 0xff));

    /// <summary>
    /// 交换 <see cref="ushort"/> 前后 8 位的值
    /// </summary>
    /// <param name="v">要交换字节顺序的 <see cref="ushort"/> 值。</param>
    /// <returns>交换字节顺序后的 <see cref="ushort"/> 值。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort SwapUInt16(ushort v) =>
        (ushort)(((v & 0xff) << 8) | ((v >> 8) & 0xff));

    /// <summary>
    /// 交换 <see cref="int"/> 前后 16 位的值
    /// </summary>
    /// <param name="v">要交换字节顺序的 <see cref="int"/> 值。</param>
    /// <returns>交换字节顺序后的 <see cref="int"/> 值。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int SwapInt32(int v) =>
        ((SwapInt16((short)v) & 0xffff) << 0x10) | (SwapInt16((short)(v >> 0x10)) & 0xffff);

    /// <summary>
    /// 交换 <see cref="uint"/> 前后 16 位的值
    /// </summary>
    /// <param name="v">要交换字节顺序的 <see cref="uint"/> 值。</param>
    /// <returns>交换字节顺序后的 <see cref="uint"/> 值。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint SwapUInt32(uint v) =>
        (uint)(((SwapUInt16((ushort)v) & 0xffff) << 0x10) | (SwapUInt16((ushort)(v >> 0x10)) & 0xffff));

    /// <summary>
    /// 交换 <see cref="long"/> 前后 32 位的值
    /// </summary>
    /// <param name="v">要交换字节顺序的 <see cref="long"/> 值。</param>
    /// <returns>交换字节顺序后的 <see cref="long"/> 值。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long SwapInt64(long v) =>
        ((SwapInt32((int)v) & 0xffffffffL) << 0x20) | (SwapInt32((int)(v >> 0x20)) & 0xffffffffL);

    /// <summary>
    /// 交换 <see cref="ulong"/> 前后 32 位的值
    /// </summary>
    /// <param name="v">要交换字节顺序的 <see cref="ulong"/> 值。</param>
    /// <returns>交换字节顺序后的 <see cref="ulong"/> 值。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong SwapUInt64(ulong v) =>
        (ulong)(((SwapUInt32((uint)v) & 0xffffffffL) << 0x20) | (SwapUInt32((uint)(v >> 0x20)) & 0xffffffffL));
}