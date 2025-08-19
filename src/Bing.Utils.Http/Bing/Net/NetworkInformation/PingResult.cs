namespace Bing.Net.NetworkInformation;

/// <summary>
/// Ping测试结果
/// </summary>
public class PingResult
{
    /// <summary>
    /// 目标主机
    /// </summary>
    public string TargetHost { get; set; }

    /// <summary>
    /// 发送的数据包数
    /// </summary>
    public int PacketsSent { get; set; }

    /// <summary>
    /// 接收的数据包数
    /// </summary>
    public int PacketsReceived { get; set; }

    /// <summary>
    /// 丢包率（0-1）
    /// </summary>
    public double PacketLoss { get; set; }

    /// <summary>
    /// 平均往返时间（毫秒）
    /// </summary>
    public long AverageRoundtripTime { get; set; }

    /// <summary>
    /// 最小往返时间（毫秒）
    /// </summary>
    public long MinRoundtripTime { get; set; }

    /// <summary>
    /// 最大往返时间（毫秒）
    /// </summary>
    public long MaxRoundtripTime { get; set; }

    /// <summary>
    /// 网络抖动（毫秒）
    /// </summary>
    public double Jitter { get; set; }

    /// <summary>
    /// 返回格式化的Ping结果
    /// </summary>
    public override string ToString()
    {
        if (PacketsReceived == 0)
            return $"Ping {TargetHost}: 请求超时";

        return $"Ping {TargetHost}: 发送={PacketsSent}, 接收={PacketsReceived}, " +
               $"丢失={PacketsSent - PacketsReceived} ({PacketLoss:P0}), " +
               $"平均={AverageRoundtripTime}ms, 抖动={Jitter:F1}ms";
    }
}