namespace Bing.Net.IPv6.Internal;

/// <summary>
/// 地址操作类
/// </summary>
internal static class AddressOperations
{
    /// <summary>
    /// 计算字节中从左开始连续1的个数
    /// </summary>
    /// <param name="value">字节值</param>
    /// <returns>连续1的个数</returns>
    internal static int CountLeadingOnes(byte value)
    {
        var count = 0;
        for (var i = 7; i >= 0; i--)
        {
            if ((value & 1 << i) != 0)
                count++;
            else
                break;
        }
        return count;
    }
}