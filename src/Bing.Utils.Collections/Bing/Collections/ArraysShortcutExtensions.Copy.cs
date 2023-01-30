namespace Bing.Collections;

/// <summary>
/// 数组捷径扩展
/// </summary>
public static partial class ArraysShortcutExtensions
{
    /// <summary>
    /// 克隆。<br />
    /// 将一个 Array 的一部分元素复制到另一个 Array 中，并根据需要执行类型转换和装箱。
    /// </summary>
    /// <param name="sourceArray">源数组</param>
    /// <param name="destinationArray">目标数组</param>
    /// <param name="length">长度</param>
    public static void Copy(this Array sourceArray, Array destinationArray, int length) => Array.Copy(sourceArray, destinationArray, length);

    /// <summary>
    /// 克隆。<br />
    /// 将一个 Array 的一部分元素复制到另一个 Array 中，并根据需要执行类型转换和装箱。
    /// </summary>
    /// <param name="sourceArray">源数组</param>
    /// <param name="sourceIndex">源数组索引</param>
    /// <param name="destinationArray">目标数组</param>
    /// <param name="destinationIndex">目标数组索引</param>
    /// <param name="length">长度</param>
    public static void Copy(this Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length) => Array.Copy(sourceArray, sourceIndex, destinationArray, destinationIndex, length);

    /// <summary>
    /// 克隆。<br />
    /// 将一个 Array 的一部分元素复制到另一个 Array 中，并根据需要执行类型转换和装箱。
    /// </summary>
    /// <param name="sourceArray">源数组</param>
    /// <param name="destinationArray">目标数组</param>
    /// <param name="length">长度</param>
    public static void Copy(this Array sourceArray, Array destinationArray, long length) => Array.Copy(sourceArray, destinationArray, length);

    /// <summary>
    /// 克隆。<br />
    /// 将一个 Array 的一部分元素复制到另一个 Array 中，并根据需要执行类型转换和装箱。
    /// </summary>
    /// <param name="sourceArray">源数组</param>
    /// <param name="sourceIndex">源数组索引</param>
    /// <param name="destinationArray">目标数组</param>
    /// <param name="destinationIndex">目标数组索引</param>
    /// <param name="length">长度</param>
    public static void Copy(this Array sourceArray, long sourceIndex, Array destinationArray, long destinationIndex, long length) => Array.Copy(sourceArray, sourceIndex, destinationArray, destinationIndex, length);

    /// <summary>
    /// 克隆。<br />
    /// 从指定源索引开始的 <see cref="T:System.Array" /> 复制一系列元素，并将它们粘贴到另一个 <see cref="T:System.Array" /> 从指定目标索引开始 . 如果复制没有完全成功，则保证所有更改都将被撤消。
    /// </summary>
    /// <param name="sourceArray">源数组</param>
    /// <param name="sourceIndex">源数组索引</param>
    /// <param name="destinationArray">目标数组</param>
    /// <param name="destinationIndex">目标数组索引</param>
    /// <param name="length">长度</param>
    public static void ConstrainedCopy(this Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length) => Array.ConstrainedCopy(sourceArray, sourceIndex, destinationArray, destinationIndex, length);

    /// <summary>
    /// 块复制。<br />
    /// 将指定数量的字节从以特定偏移量开始的源数组复制到以特定偏移量开始的目标数组。
    /// </summary>
    /// <param name="src">源数组</param>
    /// <param name="srcOffset">源数组偏移量</param>
    /// <param name="dst">目标数组</param>
    /// <param name="dstOffset">目标数组偏移量</param>
    /// <param name="count">计数</param>
    public static void BlockCopy(this Array src, int srcOffset, Array dst, int dstOffset, int count) => Buffer.BlockCopy(src, srcOffset, dst, dstOffset, count);
}