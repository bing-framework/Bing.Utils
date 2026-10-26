namespace Bing.Net.FTP;

/// <summary>
/// 指定 FluentFTP 42.0.1 异步客户端支持的代理类型。
/// </summary>
public enum FtpAsyncProxyType
{
    /// <summary>
    /// HTTP/1.1 代理。
    /// </summary>
    Http11 = 0,

    /// <summary>
    /// SOCKS4 代理。
    /// </summary>
    Socks4 = 1,

    /// <summary>
    /// SOCKS4a 代理。
    /// </summary>
    Socks4a = 2,

    /// <summary>
    /// SOCKS5 代理。
    /// </summary>
    Socks5 = 3,

    /// <summary>
    /// BlueCoat 代理。
    /// </summary>
    BlueCoat = 4,

    /// <summary>
    /// UserAtHost 代理。
    /// </summary>
    UserAtHost = 5
}
