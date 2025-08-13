namespace Bing.Net.IPv4.Internal;

/// <summary>
/// 地址操作类
/// </summary>
internal static class AddressOperations
{
    /// <summary>
    /// 计算以2为底的对数（整数版本）
    /// </summary>
    /// <param name="value">要计算对数的值</param>
    /// <returns>以2为底的对数的整数部分</returns>
    public static int CalculateLog2(uint value)
    {
        if (value == 0)
            return 0;
        // 使用位运算快速计算 log2
        int result = 0;
        if (value >= 0x10000) { value >>= 16; result += 16; }
        if (value >= 0x100) { value >>= 8; result += 8; }
        if (value >= 0x10) { value >>= 4; result += 4; }
        if (value >= 0x4) { value >>= 2; result += 2; }
        if (value >= 0x2) { result += 1; }
        return result;
    }

    /// <summary>
    /// 计算32位整数中从左开始连续1的个数
    /// </summary>
    /// <param name="value">32位整数值</param>
    /// <returns>连续1的个数</returns>
    public static int CountLeadingOnes(uint value)
    {
        var count = 0;
        for (var i = 31; i >= 0; i--)
        {
            if ((value & 1u << i) != 0)
                count++;
            else
                break;
        }
        return count;
    }
}