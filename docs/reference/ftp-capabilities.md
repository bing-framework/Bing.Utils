# FTP 能力与配置说明

## 异步客户端

**Bing.Net.FTP.AsyncFtpClient** 基于 FluentFTP 42.0.1 的原生 **AsyncFtpClient**，直接调用带 CancellationToken 的异步方法，不通过 Task.Run 包装同步网络 I/O。

```csharp
using Bing.Net.FTP;

using var ftp = new AsyncFtpClient(new AsyncFtpClientOptions {
    Host = "ftp.example.com", UserName = userName, Password = password,
    EncryptionType = EncryptionType.Explicit,
    ConnectTimeoutMilliseconds = 15000, ReadTimeoutMilliseconds = 60000
});
await ftp.ConnectAsync(token);
bool downloaded = await ftp.DownloadFileAsync(localPath, remotePath,
    cancellationToken: token);
await ftp.DisconnectAsync(token);
```

先等待传输结束再释放客户端；`Dispose()` 会同步等待当前命令结束。传输错误和取消直接传播，不以成功结果掩盖失败。

同一客户端实例会串行执行连接、查询和传输操作，因为 FTP 客户端共享控制连接及当前目录状态。等待门闩时取消只取消排队中的操作，不会打断正在执行的命令。操作开始后，取消令牌会继续传给 FluentFTP。

流上传和下载都由调用方持有。客户端用非关闭包装流保护调用方流；传输期间调用方应保持流可读或可写。文件和流传输默认覆盖目标文件，进度通过可选的 IProgress<FtpProgress> 报告。

新选项明确支持以下 FTP 代理类型：HTTP/1.1、SOCKS4、SOCKS4a、SOCKS5、BlueCoat 和 UserAtHost。代理主机、端口和类型会在连接前校验。

FTPS 默认只接受 SslPolicyErrors.None。只有显式选择 Implicit 或 Explicit 加密模式时才注册证书验证；自定义策略可明确覆盖默认判断，策略异常会先拒绝证书再传播。

SocketKeepAlive 映射 FluentFTP 的 TCP Socket 保活设置，只影响 TCP 连接探活，不发送 FTP NOOP 命令。

## 旧同步配置支持矩阵

下表描述原 FtpClient(FtpClientConfig) 的实际映射。新增异步 API 使用 AsyncFtpClientOptions，不会自动读取或继承旧配置。

| 旧配置成员 | 旧同步封装的行为 |
| --- | --- |
| Host、Port、UserName、Password | 映射到 FluentFTP 连接属性 |
| Timeout | 映射控制连接和数据连接建立超时 |
| ReadWriteTimeout | 映射控制通道和数据通道读取超时 |
| CharsetName | 通过 Encoding.GetEncoding 设置编码 |
| UsePassive | 映射为自动被动或自动主动数据连接 |
| EncryptionType | 映射为无加密、隐式 FTPS 或显式 FTPS |
| SslCertificateValidationPolicy | 加密模式启用时执行；默认只接受无证书策略错误的证书 |
| EnableSsl | 不单独控制加密；实际模式由 EncryptionType 决定 |
| DisconnectTimeout、ReceiveTimeout、SendTimeout、ConnectCount | 未映射到底层客户端 |
| LocalDirectory、RemoteDirectory | 未作为客户端默认目录应用 |
| ProxyHost、ProxyPort、ProxyUserName、ProxyPassword | GetProxy() 可创建 WebProxy，但同步 FTP 客户端没有将其传给 FluentFTP |
| UseBinary、KeepAlive | 未映射到底层客户端 |

新异步选项新增显式上传/下载 Binary 或 Ascii 数据类型和 SocketKeepAlive 映射。旧构造函数仍保持上表行为；下述完整模式显式启用以前未接入的配置。

## 同步完整配置模式

```csharp
using var ftp = new Bing.Net.FTP.FtpClient(new FtpClientConfig {
    Host = "ftp.example.com", UserName = userName, Password = password,
    EnableSsl = true, // EncryptionType 未指定时映射为 Explicit FTPS
    LocalDirectory = localRoot, RemoteDirectory = "/incoming",
    UseBinary = true, UsePassive = true, KeepAlive = false,
    Timeout = 15000, ReadWriteTimeout = 60000,
    ConnectCount = 2 // 首次连接失败后，最多额外尝试两次
}, FtpConfigurationMode.Complete);
bool uploaded = ftp.UploadFile("report.csv", "report.csv");
```

| 配置 | Complete 模式 |
|---|---|
| `EncryptionType`、`EnableSsl` | 显式加密类型优先；None 加 EnableSsl=true 时使用 Explicit。证书仍默认严格校验 |
| `Timeout` | 控制与数据连接建立超时，单位毫秒，必须大于零 |
| `ReadWriteTimeout`、`ReceiveTimeout` | 前者提供读取超时；只有显式设置后者才覆盖。旧 ReceiveTimeout 默认 300 不会悄悄缩短读取时间 |
| `SendTimeout`、`DisconnectTimeout` | 后端没有独立同步契约；显式设置时构造抛 NotSupportedException。旧默认数值未设置时不视为配置要求 |
| `ConnectCount` | 非负，表示额外连接尝试数；只重试连接，不重放失败的文件操作。已有传输校验重试策略仍保留 |
| `ProxyHost/Port/UserName/Password`、`ProxyType` | 直接创建 FluentFTP 同步代理客户端；支持 HTTP/1.1、SOCKS4/4a/5、BlueCoat、UserAtHost，默认 HTTP/1.1 |
| `UseBinary` | 同时设置上传/下载数据类型；ASCII 不进行原始字节长度/摘要一致性检查，仍检查底层成功状态 |
| `LocalDirectory` | 本地相对路径基准；未指定时固定为客户端构造时的进程目录。绝对路径保持不变，不改变进程当前目录 |
| `RemoteDirectory` | 远程相对路径基准；绝对路径保持不变。SetCurrentDirectory 成功后更新基准，后续重新连接仍使用该基准 |
| `KeepAlive` | false 时最外层操作结束后自动断开，嵌套检查和递归操作复用当前连接；不是 TCP SocketKeepAlive |

Complete 在构造时复制配置，调用方后续修改不会影响客户端。代理主机使用主机名或 IP，端口独立指定；不在 ProxyHost 中填写 URL。显式赋值的超时属性会被记录，即使赋的是旧默认数值也属于显式设置；配置绑定/反序列化同样适用。

完整模式的 `GetCurrentDirectory()` 返回当前逻辑路径基准，因此无参列目录会应用 RemoteDirectory。显式 `Connect()` 保留连接，后续独立操作仍按 KeepAlive 断开。同步操作和 Dispose 串行化；自动断开失败不会覆盖业务结果或原异常，显式 Disconnect 的失败会传播。目录传输包含失败项时返回 false。

本地测试验证配置映射、快照隔离、路径、ASCII 校验选择、连接重试和复合操作生命周期。真实 FTP/FTPS、代理握手、服务端 ASCII 换行转换需要部署环境集成验证；本轮没有将构造测试描述为真实网络验证。
