namespace Bing.Net.IPv4;

/// <summary>
/// IPv4子网信息
/// </summary>
public class IPv4SubnetInfo
{
    /// <summary>
    /// 网络地址
    /// </summary>
    public string NetworkAddress { get; set; }

    /// <summary>
    /// 广播地址
    /// </summary>
    public string BroadcastAddress { get; set; }

    /// <summary>
    /// 子网掩码
    /// </summary>
    public string SubnetMask { get; set; }

    /// <summary>
    /// CIDR前缀长度
    /// </summary>
    public int PrefixLength { get; set; }

    /// <summary>
    /// 总主机数
    /// </summary>
    public uint TotalHosts { get; set; }

    /// <summary>
    /// 可用主机数
    /// </summary>
    public uint AvailableHosts { get; set; }

    /// <summary>
    /// 第一个可用IP
    /// </summary>
    public string FirstUsableIp { get; set; }

    /// <summary>
    /// 最后一个可用IP
    /// </summary>
    public string LastUsableIp { get; set; }

    /// <summary>
    /// 返回格式化的字符串表示
    /// </summary>
    public override string ToString() => $"网络: {NetworkAddress}/{PrefixLength}, 可用主机: {AvailableHosts}, 范围: {FirstUsableIp} - {LastUsableIp}";
}