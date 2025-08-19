namespace Bing.Net.NetworkInformation;

/// <summary>
/// 网络变化信息
/// </summary>
public class NetworkChange
{
    /// <summary>
    /// 变化类型
    /// </summary>
    public NetworkChangeType Type { get; set; }

    /// <summary>
    /// 接口名称
    /// </summary>
    public string InterfaceName { get; set; }

    /// <summary>
    /// 变化描述
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// 旧值
    /// </summary>
    public string OldValue { get; set; }

    /// <summary>
    /// 新值
    /// </summary>
    public string NewValue { get; set; }
}