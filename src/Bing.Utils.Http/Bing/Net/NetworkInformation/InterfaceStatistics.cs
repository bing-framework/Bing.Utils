namespace Bing.Net.NetworkInformation;

/// <summary>
/// 接口统计信息
/// </summary>
public class InterfaceStatistics
{
    /// <summary>
    /// 获取或设置发送的字节数
    /// </summary>
    public long BytesSent { get; set; }

    /// <summary>
    /// 获取或设置接收的字节数
    /// </summary>
    public long BytesReceived { get; set; }

    /// <summary>
    /// 获取或设置发送的数据包数
    /// </summary>
    public long PacketsSent { get; set; }

    /// <summary>
    /// 获取或设置接收的数据包数
    /// </summary>
    public long PacketsReceived { get; set; }

    /// <summary>
    /// 获取或设置错误数据包数
    /// </summary>
    public long IncomingPacketsWithErrors { get; set; }

    /// <summary>
    /// 获取或设置丢弃的数据包数
    /// </summary>
    public long IncomingPacketsDiscarded { get; set; }
}
