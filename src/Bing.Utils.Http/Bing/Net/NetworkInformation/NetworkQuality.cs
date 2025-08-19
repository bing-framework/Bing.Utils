namespace Bing.Net.NetworkInformation;

/// <summary>
/// 网络连接质量信息
/// </summary>
public class NetworkQuality
{
    /// <summary>
    /// 是否连接到互联网
    /// </summary>
    public bool IsConnected { get; set; }

    /// <summary>
    /// 成功率（0-1）
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// 平均延迟（毫秒）
    /// </summary>
    public long AverageLatency { get; set; }

    /// <summary>
    /// 最小延迟（毫秒）
    /// </summary>
    public long MinLatency { get; set; }

    /// <summary>
    /// 最大延迟（毫秒）
    /// </summary>
    public long MaxLatency { get; set; }

    /// <summary>
    /// 丢包率（0-1）
    /// </summary>
    public double PacketLoss { get; set; }

    /// <summary>
    /// 连接质量等级
    /// </summary>
    public NetworkQualityLevel QualityLevel { get; set; }

    /// <summary>
    /// 返回格式化的质量描述
    /// </summary>
    public override string ToString()
    {
        if (!IsConnected)
            return "无网络连接";

        return $"质量: {QualityLevel}, 成功率: {SuccessRate:P1}, 平均延迟: {AverageLatency}ms, 丢包率: {PacketLoss:P1}";
    }
}