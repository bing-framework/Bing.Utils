namespace Bing.Conversions;

/// <summary>
/// 字节数组(<see cref="byte"/>[]) 扩展
/// </summary>
public static class ByteArrayExtensions
{
    /// <summary>
    /// 将 <see cref="byte"/>[] 转换为 <see cref="MemoryStream"/>。
    /// </summary>
    /// <param name="this">要转换的字节数组。</param>
    /// <returns>基于原字节数组内容的<see cref="MemoryStream"/>实例。</returns>
    /// <remarks>
    /// 这个方法创建了一个<see cref="MemoryStream"/>实例，其内容是传入的字节数组。
    /// 这对于需要将字节数组直接用作内存流的场景非常有用，比如在文件读写、网络传输等情况下。
    /// 使用这种方式可以避免手动创建<see cref="MemoryStream"/>实例并写入字节数组的步骤，简化代码。
    /// </remarks>
    public static MemoryStream CastToMemoryStream(this byte[] @this) => new(@this);
}