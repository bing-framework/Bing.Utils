using System.Net;
using Bing.Text;
using FluentFTP;

namespace Bing.Net.FTP;

/// <summary>
/// FTP客户端配置
/// </summary>
public class FtpClientConfig
{
    /// <summary>
    /// 保存断开超时的历史默认值。
    /// </summary>
    private int _disconnectTimeout = 300_000;
    /// <summary>
    /// 保存接收超时的历史默认值。
    /// </summary>
    private int _receiveTimeout = 300;
    /// <summary>
    /// 保存发送超时的历史默认值。
    /// </summary>
    private int _sendTimeout = 300;

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
    /// 控制通道和数据通道的连接超时时长。单位：毫秒
    /// </summary>
    public int Timeout { get; set; } = 100_000;

    /// <summary>
    /// 获取或设置读写超时时长。单位：毫秒
    /// </summary>
    /// <remarks>用于控制通道和数据通道读取超时。完整配置模式下，仅显式设置的 <see cref="ReceiveTimeout"/> 会覆盖该值。</remarks>
    public int ReadWriteTimeout { get; set; } = 300_000;

    /// <summary>
    /// 获取或设置断开超时时长。单位：毫秒
    /// </summary>
    /// <remarks>完整配置模式当前不支持独立断开超时；显式设置后构造客户端会抛出 <see cref="NotSupportedException"/>。</remarks>
    public int DisconnectTimeout
    {
        get => _disconnectTimeout;
        set
        {
            _disconnectTimeout = value;
            IsDisconnectTimeoutExplicitlyConfigured = true;
        }
    }

    /// <summary>
    /// 获取是否显式设置了断开超时。
    /// </summary>
    internal bool IsDisconnectTimeoutExplicitlyConfigured { get; private set; }

    /// <summary>
    /// 获取或设置接收超时时长。单位：毫秒
    /// </summary>
    /// <remarks>完整配置模式仅在显式设置时覆盖控制通道和数据通道读取超时；默认值不会覆盖 <see cref="ReadWriteTimeout"/>。</remarks>
    public int ReceiveTimeout
    {
        get => _receiveTimeout;
        set
        {
            _receiveTimeout = value;
            IsReceiveTimeoutExplicitlyConfigured = true;
        }
    }

    /// <summary>
    /// 获取是否显式设置了接收超时。
    /// </summary>
    internal bool IsReceiveTimeoutExplicitlyConfigured { get; private set; }

    /// <summary>
    /// 获取或设置发送超时时长。单位：毫秒
    /// </summary>
    /// <remarks>完整配置模式当前不支持独立发送超时；显式设置后构造客户端会抛出 <see cref="NotSupportedException"/>。</remarks>
    public int SendTimeout
    {
        get => _sendTimeout;
        set
        {
            _sendTimeout = value;
            IsSendTimeoutExplicitlyConfigured = true;
        }
    }

    /// <summary>
    /// 获取是否显式设置了发送超时。
    /// </summary>
    internal bool IsSendTimeoutExplicitlyConfigured { get; private set; }

    /// <summary>
    /// 获取或设置额外连接重试次数
    /// </summary>
    /// <remarks>完整配置模式仅对连接失败增加重试，不会重放失败的文件操作；历史模式忽略此值。</remarks>
    public int ConnectCount { get; set; }

    /// <summary>
    /// 获取或设置本地目录
    /// </summary>
    /// <remarks>完整配置模式以此目录解析本地相对路径；未设置时使用创建客户端时的当前目录快照。</remarks>
    public string LocalDirectory { get; set; }

    /// <summary>
    /// 获取或设置远程服务器目录
    /// </summary>
    /// <remarks>完整配置模式以此目录作为远程相对路径基准。</remarks>
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
    /// 获取或设置代理类型；完整配置模式默认使用 HTTP/1.1 代理。
    /// </summary>
    public FtpAsyncProxyType ProxyType { get; set; } = FtpAsyncProxyType.Http11;

    /// <summary>
    /// 获取或设置是否启用SSL
    /// </summary>
    public bool EnableSsl { get; set; } = false;

    /// <summary>
    /// 获取或设置 FTPS 证书验证策略。
    /// </summary>
    /// <remarks>
    /// 返回 true 表示接受证书，返回 false 表示拒绝证书；未设置时仅接受没有策略错误的证书。策略仅在实际加密模式启用时调用；策略抛出异常时证书会被拒绝，异常继续传播。
    /// </remarks>
    public Func<FtpSslValidationEventArgs, bool> SslCertificateValidationPolicy { get; set; }

    /// <summary>
    /// 获取或设置是否允许二进制
    /// </summary>
    public bool UseBinary { get; set; } = true;

    /// <summary>
    /// 获取或设置是否使用被动模式；为 false 时使用主动模式。具体命令由底层自动协商。
    /// </summary>
    public bool UsePassive { get; set; } = true;

    /// <summary>
    /// 获取或设置 FTP 操作结束后是否保持控制连接
    /// </summary>
    /// <remarks>完整配置模式设为 false 时，在最外层复合操作结束后断开连接。</remarks>
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
        if (!string.IsNullOrEmpty(ProxyUserName) || !string.IsNullOrEmpty(ProxyPassword))
            proxy.Credentials = new NetworkCredential(ProxyUserName, ProxyPassword);
        return proxy;
    }

    /// <summary>
    /// 复制配置值及显式设置标记，隔离调用方后续修改。
    /// </summary>
    internal FtpClientConfig CreateSnapshot()
    {
        var snapshot = new FtpClientConfig
        {
            UserName = UserName,
            Password = Password,
            Host = Host,
            Port = Port,
            Timeout = Timeout,
            ReadWriteTimeout = ReadWriteTimeout,
            ConnectCount = ConnectCount,
            LocalDirectory = LocalDirectory,
            RemoteDirectory = RemoteDirectory,
            CharsetName = CharsetName,
            ProxyHost = ProxyHost,
            ProxyPort = ProxyPort,
            ProxyUserName = ProxyUserName,
            ProxyPassword = ProxyPassword,
            ProxyType = ProxyType,
            EnableSsl = EnableSsl,
            SslCertificateValidationPolicy = SslCertificateValidationPolicy,
            UseBinary = UseBinary,
            UsePassive = UsePassive,
            KeepAlive = KeepAlive,
            EncryptionType = EncryptionType
        };

        snapshot._disconnectTimeout = _disconnectTimeout;
        snapshot._receiveTimeout = _receiveTimeout;
        snapshot._sendTimeout = _sendTimeout;
        snapshot.IsDisconnectTimeoutExplicitlyConfigured = IsDisconnectTimeoutExplicitlyConfigured;
        snapshot.IsReceiveTimeoutExplicitlyConfigured = IsReceiveTimeoutExplicitlyConfigured;
        snapshot.IsSendTimeoutExplicitlyConfigured = IsSendTimeoutExplicitlyConfigured;
        return snapshot;
    }
}
