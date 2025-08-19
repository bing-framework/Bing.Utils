namespace Bing.Net.NetworkInformation;

/// <summary>
/// 接口统计信息
/// </summary>
public class InterfaceStatistics
{
    /// <summary>
    /// 发送的字节数
    /// </summary>
    public long BytesSent { get; set; }

    /// <summary>
    /// 接收的字节数
    /// </summary>
    public long BytesReceived { get; set; }

    /// <summary>
    /// 发送的数据包数
    /// </summary>
    public long PacketsSent { get; set; }

    /// <summary>
    /// 接收的数据包数
    /// </summary>
    public long PacketsReceived { get; set; }

    /// <summary>
    /// 错误数据包数
    /// </summary>
    public long IncomingPacketsWithErrors { get; set; }

    /// <summary>
    /// 丢弃的数据包数
    /// </summary>
    public long IncomingPacketsDiscarded { get; set; }
}