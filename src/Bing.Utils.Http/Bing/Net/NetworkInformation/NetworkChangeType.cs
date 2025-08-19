namespace Bing.Net.NetworkInformation;

/// <summary>
/// 网络变化类型
/// </summary>
public enum NetworkChangeType
{
    /// <summary>
    /// 接口新增
    /// </summary>
    InterfaceAdded,

    /// <summary>
    /// 接口移除
    /// </summary>
    InterfaceRemoved,

    /// <summary>
    /// IP地址变化
    /// </summary>
    IpAddressChanged,

    /// <summary>
    /// 连接状态变化
    /// </summary>
    ConnectionStatusChanged
}