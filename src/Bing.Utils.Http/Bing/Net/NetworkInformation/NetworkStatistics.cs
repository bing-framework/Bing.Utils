namespace Bing.Net.NetworkInformation;

/// <summary>
/// 网络统计信息
/// </summary>
public class NetworkStatistics
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
    /// 活动接口数量
    /// </summary>
    public int ActiveInterfaceCount { get; set; }

    /// <summary>
    /// 总流量（发送+接收）
    /// </summary>
    public long TotalBytes => BytesSent + BytesReceived;

    /// <summary>
    /// 总数据包数（发送+接收）
    /// </summary>
    public long TotalPackets => PacketsSent + PacketsReceived;

    /// <summary>
    /// 返回格式化的字符串表示
    /// </summary>
    /// <returns>格式化的统计信息</returns>
    public override string ToString()
    {
        return $"发送: {FormatBytes(BytesSent)}, 接收: {FormatBytes(BytesReceived)}, " +
               $"数据包: {PacketsSent + PacketsReceived:N0}, 接口数: {ActiveInterfaceCount}";
    }

    /// <summary>
    /// 格式化字节数为人类可读的格式
    /// </summary>
    /// <param name="bytes">字节数</param>
    /// <returns>格式化的字符串</returns>
    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}