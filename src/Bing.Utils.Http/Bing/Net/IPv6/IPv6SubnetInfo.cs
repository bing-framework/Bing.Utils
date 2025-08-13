namespace Bing.Net.IPv6;

/// <summary>
/// IPv6子网信息
/// </summary>
public class IPv6SubnetInfo
{
    /// <summary>
    /// 网络前缀
    /// </summary>
    public string NetworkPrefix { get; set; }

    /// <summary>
    /// 前缀长度
    /// </summary>
    public int PrefixLength { get; set; }

    /// <summary>
    /// 子网掩码
    /// </summary>
    public string SubnetMask { get; set; }

    /// <summary>
    /// 总地址数（对于大型子网可能为null）
    /// </summary>
    public ulong? TotalAddresses { get; set; }

    /// <summary>
    /// 可用地址数（排除网络地址和广播地址，对于IPv6通常等于总地址数）
    /// </summary>
    public ulong? AvailableAddresses { get; set; }

    /// <summary>
    /// 地址类型
    /// </summary>
    public IPv6AddressType AddressType { get; set; }

    /// <summary>
    /// 第一个可用地址
    /// </summary>
    public string FirstUsableAddress { get; set; }

    /// <summary>
    /// 最后一个可用地址
    /// </summary>
    public string LastUsableAddress { get; set; }

    /// <summary>
    /// 主机位数
    /// </summary>
    public int HostBits { get; set; }


    /// <summary>
    /// 是否为大型网段（主机位数>=64）
    /// </summary>
    public bool IsLargeSubnet => HostBits >= 64;

    /// <summary>
    /// 子网大小描述
    /// </summary>
    public string SizeDescription
    {
        get
        {
            if (TotalAddresses.HasValue)
                return TotalAddresses.Value.ToString("N0");
            return $"2^{HostBits} (约 {Math.Pow(2, Math.Min(HostBits, 50)):E2} 个地址)";
        }
    }

    /// <summary>
    /// 返回格式化的字符串表示
    /// </summary>
    public override string ToString()
    {
        var addressDesc = IsLargeSubnet ? $"2^{HostBits}" : TotalAddresses?.ToString("N0") ?? "0";
        return $"网络: {NetworkPrefix}/{PrefixLength}, 类型: {AddressType}, 地址数: {addressDesc}";
    }
}