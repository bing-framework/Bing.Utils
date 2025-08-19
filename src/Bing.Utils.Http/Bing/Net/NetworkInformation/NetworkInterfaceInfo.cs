using Bing.Net.IPv6;
using Bing.Net.Mac;
using Bing.Text;
using System.Net.NetworkInformation;
using System.Text;

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

    /// <summary>
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
    /// 是否为主要网络接口（有网关且速度最快）
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// 接口统计信息
    /// </summary>
    public InterfaceStatistics Statistics { get; set; }

    /// <summary>
    /// 子网掩码（IPv4）
    /// </summary>
    public string SubnetMask { get; set; }

    /// <summary>
    /// 是否为无线接口
    /// </summary>
    public bool IsWireless => Type == NetworkInterfaceType.Wireless80211 ||
                              Description.Contains("Wi-Fi", StringComparison.OrdinalIgnoreCase) ||
                              Description.Contains("Wireless", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 是否为以太网接口
    /// </summary>
    public bool IsEthernet => Type == NetworkInterfaceType.Ethernet;

    /// <summary>
    /// 格式化的速度显示
    /// </summary>
    public string FormattedSpeed
    {
        get
        {
            if (Speed <= 0) return "未知";

            var speedInMbps = Speed / 1_000_000.0;
            if (speedInMbps >= 1000)
            {
                return $"{speedInMbps / 1000:F1} Gbps";
            }
            return $"{speedInMbps:F0} Mbps";
        }
    }

    /// <summary>
    /// 返回详细的格式化字符串表示
    /// </summary>
    /// <returns>详细的接口信息</returns>
    public string ToDetailedString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"接口名称: {Name}");
        sb.AppendLine($"描述: {Description}");
        sb.AppendLine($"类型: {Type}");
        sb.AppendLine($"状态: {Status}");
        sb.AppendLine($"速度: {FormattedSpeed}");
        sb.AppendLine($"MAC地址: {FormattedMacAddress}");
        sb.AppendLine($"IPv4地址: {string.Join(", ", IPv4Addresses)}");
        sb.AppendLine($"IPv6地址: {string.Join(", ", IPv6Addresses)}");
        sb.AppendLine($"网关: {string.Join(", ", GatewayAddresses)}");
        sb.AppendLine($"DNS: {string.Join(", ", DnsAddresses)}");
        sb.AppendLine($"支持IPv6: {SupportsIPv6}");
        return sb.ToString();
    }

    /// <summary>
    /// 返回格式化的字符串表示
    /// </summary>
    public override string ToString() => $"{Name} ({Type}) - IPv4: {IpAddress}, IPv6: {IPv6Address} - {FormattedMacAddress}";
}