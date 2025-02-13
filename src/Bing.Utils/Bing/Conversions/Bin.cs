using Bing.Conversions.Internals;

namespace Bing.Conversions;

/// <summary>
/// 二进制工具
/// </summary>
public static class Bin
{
    /// <summary>
    /// 高低位交换
    /// </summary>
    /// <param name="bin">二进制字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Reverse(string bin) => ScaleRevHelper.Reverse(bin, 8);
}