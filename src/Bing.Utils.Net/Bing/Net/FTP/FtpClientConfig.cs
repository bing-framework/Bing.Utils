using System.Net;
using Bing.Text;

namespace Bing.Net.FTP;

/// <summary>
/// FTP客户端配置
/// </summary>
public class FtpClientConfig
{
    /// <summary>
    /// 初始化一个 <see cref="FtpClientConfig"/> 类型的实例。
    /// </summary>
    public FtpClientConfig()
    {
    }

    /// <summary>
    /// 获取或设置用户名
    /// </summary>
    public string UserName { get; set; }

    /// <summary>
    /// 获取或设置密码
    /// </summary>
    public string Password { get; set; }

    /// <summary>
    /// 获取或设置主机
    /// </summary>
    public string Host { get; set; }

    /// <summary>
    /// 获取或设置端口。默认：21
    /// </summary>
    public int Port { get; set; } = 21;

    /// <summary>
    /// 获取或设置连接超时时长。单位：毫秒
    /// </summary>
    public int Timeout { get; set; } = 100_000;

    /// <summary>
    /// 获取或设置读写超时时长。单位：毫秒
    /// </summary>
    public int ReadWriteTimeout { get; set; } = 300_000;

    /// <summary>
    /// 获取或设置断开超时时长。单位：毫秒
    /// </summary>
    public int DisconnectTimeout { get; set; } = 300_000;

    /// <summary>
    /// 获取或设置接收超时时长。单位：毫秒
    /// </summary>
    public int ReceiveTimeout { get; set; } = 300;

    /// <summary>
    /// 获取或设置发送超时时长。单位：毫秒
    /// </summary>
    public int SendTimeout { get; set; } = 300;

    /// <summary>
    /// 获取或设置重试连接次数
    /// </summary>
    public int ConnectCount { get; set; }

    /// <summary>
    /// 获取或设置本地目录
    /// </summary>
    public string LocalDirectory { get; set; }

    /// <summary>
    /// 获取或设置远程服务器目录
    /// </summary>
    public string RemoteDirectory { get; set; } = "/";

    /// <summary>
    /// 获取或设置编码名称
    /// </summary>
    public string CharsetName { get; set; } = "UTF-8";

    /// <summary>
    /// 获取或设置代理地址
    /// </summary>
    public string ProxyHost { get; set; }

    /// <summary>
    /// 获取或设置代理端口
    /// </summary>
    public int ProxyPort { get; set; }

    /// <summary>
    /// 获取或设置代理账号
    /// </summary>
    public string ProxyUserName { get; set; }

    /// <summary>
    /// 获取或设置代理密码
    /// </summary>
    public string ProxyPassword { get; set; }

    /// <summary>
    /// 获取或设置是否启用SSL
    /// </summary>
    public bool EnableSsl { get; set; } = false;

    /// <summary>
    /// 获取或设置是否允许二进制
    /// </summary>
    public bool UseBinary { get; set; } = true;

    /// <summary>
    /// 获取或设置是否允许被动式
    /// </summary>
    public bool UsePassive { get; set; } = true;

    /// <summary>
    /// 获取或设置是否请求完成保持链接
    /// </summary>
    public bool KeepAlive { get; set; } = true;

    /// <summary>
    /// 获取或设置加密方式
    /// </summary>
    public EncryptionType EncryptionType { get; set; } = EncryptionType.None;

    /// <summary>
    /// 获取代理
    /// </summary>
    public WebProxy GetProxy()
    {
        if (string.IsNullOrEmpty(ProxyHost))
            return null;
        var proxy = new WebProxy();
        if (StringJudge.IsWebUrl(ProxyHost))
            proxy.Address = new Uri(ProxyHost);
        if (ProxyPort > 0)
            proxy = new WebProxy(ProxyHost, ProxyPort);
        if (!string.IsNullOrEmpty(ProxyUserName) && string.IsNullOrEmpty(ProxyPassword))
            proxy.Credentials = new NetworkCredential(ProxyUserName, ProxyPassword);
        return proxy;
    }
}
