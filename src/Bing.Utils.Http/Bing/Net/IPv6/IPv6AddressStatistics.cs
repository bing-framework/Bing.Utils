using System.Numerics;

namespace Bing.Net.IPv6;

/// <summary>
/// IPv6地址统计信息
/// </summary>
public class IPv6AddressStatistics
{
    /// <summary>
    /// 获取或设置总计数
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 获取或设置有效计数
    /// </summary>
    public int ValidCount { get; set; }

    /// <summary>
    /// 获取或设置无效计数
    /// </summary>
    public int InvalidCount { get; set; }

    /// <summary>
    /// 获取或设置最小地址
    /// </summary>
    public string SmallestAddress { get; set; }

    /// <summary>
    /// 获取或设置最大地址
    /// </summary>
    public string LargestAddress { get; set; }

    /// <summary>
    /// 获取或设置地址范围
    /// </summary>
    public BigInteger AddressRange { get; set; }

    /// <summary>
    /// 获取或设置地址类型统计
    /// </summary>
    public Dictionary<IPv6AddressType, int> AddressTypes { get; set; } = new();

    /// <inheritdoc />
    public override string ToString() => $"Total: {TotalCount}, Valid: {ValidCount}, Invalid: {InvalidCount}, Range: {AddressRange}";
}
