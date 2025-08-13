using Bing.Net;

namespace Bing.Helpers;

/// <summary>
/// IP地址操作工具类
/// </summary>
/// <remarks>
/// 提供IP地址获取、验证、转换等功能，支持IPv4和IPv6地址处理。
/// 包含内网IP判断、地址格式验证、网络接口查询等实用方法。
/// </remarks>
public static class Ip
{
    #region 基础IP操作

    /// <summary>
    /// 设置当前线程的IP地址
    /// </summary>
    /// <param name="ip">IP地址字符串</param>
    /// <exception cref="ArgumentException">当IP地址格式无效时抛出</exception>
    /// <example>
    /// <code>
    /// Ip.SetIp("192.168.1.100");
    /// Ip.SetIp("2001:db8::1");  // IPv6地址
    /// </code>
    /// </example>
    public static void SetIp(string ip) => IpAddressProvider.SetIp(ip);

    /// <summary>
    /// 重置当前线程的IP地址缓存
    /// </summary>
    public static void Reset() => IpAddressProvider.Reset();

    /// <summary>
    /// 获取客户端IP地址
    /// </summary>
    /// <returns>
    /// 返回客户端IP地址。优先级：<br />
    /// 1. 手动设置的IP地址<br />
    /// 2. HTTP上下文中的远程IP地址<br />
    /// 3. 本机局域网IP地址
    /// </returns>
    /// <example>
    /// <code>
    /// string clientIp = Ip.GetIp();
    /// Console.WriteLine($"客户端IP: {clientIp}");
    /// </code>
    /// </example>
    public static string GetIp() => IpAddressProvider.GetIp();

    /// <summary>
    /// 获取本机所有IP地址
    /// </summary>
    /// <param name="includeIPv6">是否包含IPv6地址</param>
    /// <param name="includeLoopback">是否包含回环地址</param>
    /// <returns>本机IP地址列表</returns>
    /// <example>
    /// <code>
    /// var allIps = Ip.GetAllLocalIps(includeIPv6: true, includeLoopback: false);
    /// foreach (var ip in allIps)
    /// {
    ///     Console.WriteLine($"本机IP: {ip}");
    /// }
    /// </code>
    /// </example>
    public static List<string> GetAllLocalIps(bool includeIPv6 = false, bool includeLoopback = false) =>
        IpAddressProvider.GetAllLocalIps(includeIPv6, includeLoopback);

    #endregion
}