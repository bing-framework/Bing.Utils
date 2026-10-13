namespace Bing.Net.IPv4;

/// <summary>
/// IPv4子网信息
/// </summary>
public class IPv4SubnetInfo
{
    /// <summary>
    /// 获取或设置网络地址
    /// </summary>
    public string NetworkAddress { get; set; }

    /// <summary>
    /// 获取或设置广播地址
    /// </summary>
    public string BroadcastAddress { get; set; }

    /// <summary>
    /// 获取或设置子网掩码
    /// </summary>
    public string SubnetMask { get; set; }

    /// <summary>
    /// 获取或设置CIDR前缀长度
    /// </summary>
    public int PrefixLength { get; set; }

    /// <summary>
    /// 获取或设置总主机数
    /// </summary>
    public uint TotalHosts { get; set; }

    /// <summary>
    /// 获取或设置可用主机数
    /// </summary>
    public uint AvailableHosts { get; set; }

    /// <summary>
    /// 获取或设置第一个可用IP
    /// </summary>
    public string FirstUsableIp { get; set; }

    /// <summary>
    /// 获取或设置最后一个可用IP
    /// </summary>
    public string LastUsableIp { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"网络: {NetworkAddress}/{PrefixLength}, 可用主机: {AvailableHosts}, 范围: {FirstUsableIp} - {LastUsableIp}";
}
