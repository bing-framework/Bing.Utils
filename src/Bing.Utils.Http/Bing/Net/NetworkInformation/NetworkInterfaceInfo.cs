using System.Net.NetworkInformation;
using Bing.Net.IPv6;
using Bing.Net.Mac;

namespace Bing.Net.NetworkInformation;

/// <summary>
/// 网络接口信息
/// </summary>
public class NetworkInterfaceInfo
{
    /// <summary>
    /// 接口名称
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 接口描述
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// 接口类型
    /// </summary>
    public NetworkInterfaceType Type { get; set; }

    /// <summary>
    /// 运行状态
    /// </summary>
    public OperationalStatus Status { get; set; }

    /// <summary>
    /// 所有IP地址列表
    /// </summary>
    public List<string> IpAddresses { get; set; } = new();

    /// <summary>
    /// IPv4地址列表
    /// </summary>
    public List<string> IPv4Addresses { get; set; } = new();

    /// <summary>
    /// IPv6地址列表
    /// </summary>
    public List<string> IPv6Addresses { get; set; } = new();

    //// <summary>
    /// 主要IP地址（第一个IPv4地址）
    /// </summary>
    public string IpAddress => IPv4Addresses.FirstOrDefault() ?? IpAddresses.FirstOrDefault();

    /// <summary>
    /// 主要IPv6地址（第一个全局单播地址）
    /// </summary>
    public string IPv6Address => IPv6Addresses.FirstOrDefault(IPv6Validator.IsGlobalUnicast) ??
                                 IPv6Addresses.FirstOrDefault();

    /// <summary>
    /// MAC地址
    /// </summary>
    public string MacAddress { get; set; }

    /// <summary>
    /// 格式化的MAC地址
    /// </summary>
    public string FormattedMacAddress => MacAddressHelper.Format(MacAddress);

    /// <summary>
    /// 是否有网关
    /// </summary>
    public bool HasGateway { get; set; }

    /// <summary>
    /// 网关地址列表
    /// </summary>
    public List<string> GatewayAddresses { get; set; } = new();

    /// <summary>
    /// DNS服务器地址列表
    /// </summary>
    public List<string> DnsAddresses { get; set; } = new();

    /// <summary>
    /// 网络速度（bps）
    /// </summary>
    public long Speed { get; set; }

    /// <summary>
    /// 是否支持IPv6
    /// </summary>
    public bool SupportsIPv6 { get; set; }

    /// <summary>
    /// 返回格式化的字符串表示
    /// </summary>
    public override string ToString() => $"{Name} ({Type}) - IPv4: {IpAddress}, IPv6: {IPv6Address} - {FormattedMacAddress}";
}