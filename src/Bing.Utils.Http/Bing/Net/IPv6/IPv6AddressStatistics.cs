using System.Numerics;

namespace Bing.Net.IPv6;

/// <summary>
/// IPv6地址统计信息
/// </summary>
public class IPv6AddressStatistics
{
    /// <summary>
    /// 总计数
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 有效计数
    /// </summary>
    public int ValidCount { get; set; }

    /// <summary>
    /// 无效计数
    /// </summary>
    public int InvalidCount { get; set; }

    /// <summary>
    /// 最小地址
    /// </summary>
    public string SmallestAddress { get; set; }

    /// <summary>
    /// 最大地址
    /// </summary>
    public string LargestAddress { get; set; }

    /// <summary>
    /// 地址范围
    /// </summary>
    public BigInteger AddressRange { get; set; }

    /// <summary>
    /// 地址类型统计
    /// </summary>
    public Dictionary<IPv6AddressType, int> AddressTypes { get; set; } = new();

    /// <summary>
    /// 返回统计信息的字符串表示
    /// </summary>
    /// <returns>格式化的统计信息</returns>
    public override string ToString() => $"Total: {TotalCount}, Valid: {ValidCount}, Invalid: {InvalidCount}, Range: {AddressRange}";
}