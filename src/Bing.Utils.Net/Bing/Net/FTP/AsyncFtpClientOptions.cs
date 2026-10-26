using System;
using System.Text;
using FluentFTP;

namespace Bing.Net.FTP;

/// <summary>
/// 为异步 FTP 客户端提供严格校验的连接与传输配置。
/// </summary>
public sealed class AsyncFtpClientOptions
{
    /// <summary>
    /// 获取或设置 FTP 服务器主机名或 IP 地址。
    /// </summary>
    public string Host { get; set; }

    /// <summary>
    /// 获取或设置 FTP 服务器端口，默认值为 21。
    /// </summary>
    public int Port { get; set; } = 21;

    /// <summary>
    /// 获取或设置 FTP 登录用户名，默认值为匿名用户。
    /// </summary>
    public string UserName { get; set; } = "anonymous";

    /// <summary>
    /// 获取或设置 FTP 登录密码。
    /// </summary>
    public string Password { get; set; } = "";

    /// <summary>
    /// 获取或设置连接超时时长，单位为毫秒，必须大于零。
    /// </summary>
    public int ConnectTimeoutMilliseconds { get; set; } = 100_000;

    /// <summary>
    /// 获取或设置控制通道和数据通道读超时时长，单位为毫秒，必须大于零。
    /// </summary>
    public int ReadTimeoutMilliseconds { get; set; } = 300_000;

    /// <summary>
    /// 获取或设置控制与数据连接使用的文本编码。
    /// </summary>
    public Encoding Encoding { get; set; } = System.Text.Encoding.UTF8;

    /// <summary>
    /// 获取或设置是否使用自动被动数据连接；为 false 时使用自动主动模式。
    /// </summary>
    public bool UsePassive { get; set; } = true;

    /// <summary>
    /// 获取或设置上传和下载的数据类型。
    /// </summary>
    public FtpTransferType TransferType { get; set; } = FtpTransferType.Binary;

    /// <summary>
    /// 获取或设置是否启用 TCP Socket 保活。
    /// </summary>
    /// <remarks>该设置只映射 TCP Socket 保活，不代表 FTP NOOP 保活命令。</remarks>
    public bool SocketKeepAlive { get; set; } = true;

    /// <summary>
    /// 获取或设置 FTP 加密模式；证书默认只接受没有策略错误的证书。
    /// </summary>
    public EncryptionType EncryptionType { get; set; } = Bing.Net.FTP.EncryptionType.None;

    /// <summary>
    /// 获取或设置可选的 FTPS 证书验证策略。
    /// </summary>
    /// <remarks>返回 true 可接受证书；策略抛出异常时证书会被拒绝并传播异常。</remarks>
    public Func<FtpSslValidationEventArgs, bool> CertificateValidationPolicy { get; set; }

    /// <summary>
    /// 获取或设置可选代理；仅支持 <see cref="FtpAsyncProxyType"/> 中列出的类型。
    /// </summary>
    public FtpAsyncProxyOptions Proxy { get; set; }

    internal static AsyncFtpClientOptions CreateValidatedSnapshot(AsyncFtpClientOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrWhiteSpace(options.Host))
            throw new ArgumentException("必须指定 FTP 服务器主机。", nameof(options.Host));
        if (ContainsWhiteSpace(options.Host))
            throw new ArgumentException("FTP 服务器主机不能包含空白字符。", nameof(options.Host));
        if (options.Port < 1 || options.Port > 65535)
            throw new ArgumentOutOfRangeException(nameof(options.Port), options.Port, "FTP 服务器端口必须位于 1 到 65535 之间。");
        if (string.IsNullOrWhiteSpace(options.UserName))
            throw new ArgumentException("必须指定 FTP 登录用户名。", nameof(options.UserName));
        if (options.ConnectTimeoutMilliseconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(options.ConnectTimeoutMilliseconds), options.ConnectTimeoutMilliseconds, "连接超时必须大于零。");
        if (options.ReadTimeoutMilliseconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(options.ReadTimeoutMilliseconds), options.ReadTimeoutMilliseconds, "读取超时必须大于零。");
        if (options.Encoding == null)
            throw new ArgumentNullException(nameof(options.Encoding));
        if (!Enum.IsDefined(typeof(FtpTransferType), options.TransferType))
            throw new ArgumentOutOfRangeException(nameof(options.TransferType), options.TransferType, "不支持的 FTP 传输类型。");
        if (!Enum.IsDefined(typeof(EncryptionType), options.EncryptionType))
            throw new ArgumentOutOfRangeException(nameof(options.EncryptionType), options.EncryptionType, "不支持的 FTP 加密模式。");

        return new AsyncFtpClientOptions
        {
            Host = options.Host,
            Port = options.Port,
            UserName = options.UserName,
            Password = options.Password ?? "",
            ConnectTimeoutMilliseconds = options.ConnectTimeoutMilliseconds,
            ReadTimeoutMilliseconds = options.ReadTimeoutMilliseconds,
            Encoding = options.Encoding,
            UsePassive = options.UsePassive,
            TransferType = options.TransferType,
            SocketKeepAlive = options.SocketKeepAlive,
            EncryptionType = options.EncryptionType,
            CertificateValidationPolicy = options.CertificateValidationPolicy,
            Proxy = CreateProxySnapshot(options.Proxy)
        };
    }

    private static FtpAsyncProxyOptions CreateProxySnapshot(FtpAsyncProxyOptions proxy)
    {
        if (proxy == null)
            return null;
        if (!Enum.IsDefined(typeof(FtpAsyncProxyType), proxy.Type))
            throw new ArgumentOutOfRangeException(nameof(proxy.Type), proxy.Type, "不支持的 FTP 代理类型。");
        if (string.IsNullOrWhiteSpace(proxy.Host))
            throw new ArgumentException("必须指定代理服务器主机。", nameof(proxy.Host));
        if (proxy.Port < 1 || proxy.Port > 65535)
            throw new ArgumentOutOfRangeException(nameof(proxy.Port), proxy.Port, "代理服务器端口必须位于 1 到 65535 之间。");
        if (ContainsWhiteSpace(proxy.Host))
            throw new ArgumentException("代理服务器主机不能包含空白字符。", nameof(proxy.Host));
        if (string.IsNullOrWhiteSpace(proxy.UserName) && !string.IsNullOrEmpty(proxy.Password))
            throw new ArgumentException("代理密码非空时必须同时指定代理用户名。", nameof(proxy.UserName));

        return new FtpAsyncProxyOptions
        {
            Type = proxy.Type,
            Host = proxy.Host,
            Port = proxy.Port,
            UserName = proxy.UserName ?? "",
            Password = proxy.Password ?? ""
        };
    }

    private static bool ContainsWhiteSpace(string value)
    {
        foreach (var character in value)
        {
            if (char.IsWhiteSpace(character))
                return true;
        }

        return false;
    }
}
