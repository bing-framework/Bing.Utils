// ReSharper disable once CheckNamespace
namespace Bing.Collections;

/// <summary>
/// 字节数组(<see cref="byte"/>[]) 扩展
/// </summary>
public static class ByteArrayExtensions
{
    public static DateTime ToDateTime(this byte[] bytes, int startIndex = 0) =>
        DateTime.FromBinary(BitConverter.ToInt64(bytes, startIndex));
}