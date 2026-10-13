namespace Bing.Net.NetworkInformation;

/// <summary>
/// 网络连接质量信息
/// </summary>
public class NetworkQuality
{
    /// <summary>
    /// 获取或设置是否连接到互联网
    /// </summary>
    public bool IsConnected { get; set; }

    /// <summary>
    /// 获取或设置成功率（0-1）
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// 获取或设置平均延迟（毫秒）
    /// </summary>
    public long AverageLatency { get; set; }

    /// <summary>
    /// 获取或设置最小延迟（毫秒）
    /// </summary>
    public long MinLatency { get; set; }

    /// <summary>
    /// 获取或设置最大延迟（毫秒）
    /// </summary>
    public long MaxLatency { get; set; }

    /// <summary>
    /// 获取或设置丢包率（0-1）
    /// </summary>
    public double PacketLoss { get; set; }

    /// <summary>
    /// 获取或设置连接质量等级
    /// </summary>
    public NetworkQualityLevel QualityLevel { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        if (!IsConnected)
            return "无网络连接";

        return $"质量: {QualityLevel}, 成功率: {SuccessRate:P1}, 平均延迟: {AverageLatency}ms, 丢包率: {PacketLoss:P1}";
    }
}
