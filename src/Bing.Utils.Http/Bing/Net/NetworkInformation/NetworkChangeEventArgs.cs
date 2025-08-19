using System.Net.NetworkInformation;

namespace Bing.Net.NetworkInformation;

/// <summary>
/// 网络变化事件参数
/// </summary>
public class NetworkChangeEventArgs : EventArgs
{
    /// <summary>
    /// 变化列表
    /// </summary>
    public List<NetworkChange> Changes { get; set; } = new();

    /// <summary>
    /// 变化时间戳
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// 当前网络接口列表
    /// </summary>
    public List<NetworkInterfaceInfo> CurrentInterfaces { get; set; } = new();
}