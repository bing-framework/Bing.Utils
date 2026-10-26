namespace Bing.Net.FTP;

/// <summary>
/// 配置 FTP 异步客户端连接使用的代理。
/// </summary>
public sealed class FtpAsyncProxyOptions
{
    /// <summary>
    /// 获取或设置代理协议类型。
    /// </summary>
    public FtpAsyncProxyType Type { get; set; } = FtpAsyncProxyType.Http11;

    /// <summary>
    /// 获取或设置代理服务器主机名或 IP 地址。
    /// </summary>
    public string Host { get; set; }

    /// <summary>
    /// 获取或设置代理服务器端口。
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// 获取或设置代理认证用户名；代理不要求认证时留空。
    /// </summary>
    public string UserName { get; set; } = "";

    /// <summary>
    /// 获取或设置代理认证密码；代理不要求认证时留空。
    /// </summary>
    public string Password { get; set; } = "";
}
