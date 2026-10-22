# Bing.Utils.Net

## 功能说明

`Bing.Utils.Net` 当前封装 FluentFTP，提供同步 FTP/FTPS 连接、文件上传下载、文件和目录增删改查、批量传输及工作目录操作。当前没有 SFTP、TCP/UDP 或通用网络客户端能力。

## 核心实现与关键文件

源码根目录为 `src/Bing.Utils.Net/Bing/Net/FTP`。

- `IFtpClient.cs`：公开同步客户端契约并继承 `IDisposable`。
- `FtpClient.cs`：把文件、目录和连接操作委托给 FluentFTP；上传下载会校验远端与本地长度，底层 FTP 异常包装为 `FtpClientException`。
- `FtpClientConfig.cs`：主机、端口、凭据、编码、FTPS 模式、代理和超时等配置模型。
- `EncryptionType.cs`：`None`、`Implicit`、`Explicit` 三种 FTP 加密模式。
- 单元测试位于 `tests/Bing.Utils.Net.Tests`；真实服务器验证位于 `tests/Bing.Utils.Net.Tests.Integration`。

## 对外接口与用法

```csharp
using Bing.Net.FTP;

var config = new FtpClientConfig {
    Host = "ftp.example.com",
    Port = 21,
    UserName = "user",
    Password = "secret",
    CharsetName = "UTF-8",
    Timeout = 30_000,
    ReadWriteTimeout = 120_000,
    UsePassive = true,
    EnableSsl = true,
    EncryptionType = EncryptionType.Explicit
};

using var client = new FtpClient(config);
client.Connect();
client.UploadFile(@"C:\data\report.csv", "/incoming/report.csv");
```

## 依赖与配置

- 依赖 `Bing.Utils` 和 `FluentFTP 42.0.1`。
- 目标框架继承 `common.props`。
- 构造时接入主机、端口、凭据、字符集、加密模式，以及下表配置。构造不会自动连接；配置值在构造时读取，修改原配置对象不会重新设置底层客户端。
- 底层 `RetryAttempts` 固定为 3，含义是上传/下载校验失败的重试次数，不是 `ConnectCount` 的连接重试次数。

| 配置项 | 实际映射 | 默认值及单位 |
| --- | --- | --- |
| `Timeout` | `ConnectTimeout`、`DataConnectionConnectTimeout` | 100,000 毫秒，控制/数据连接建立超时 |
| `ReadWriteTimeout` | `ReadTimeout`、`DataConnectionReadTimeout` | 300,000 毫秒，仅控制/数据通道读取超时，未设置独立发送超时 |
| `UsePassive` | true：`AutoPassive`；false：`AutoActive` | true；具体 FTP 命令由 FluentFTP 协商 |

这些字段此前未生效，底层使用其默认值；现在默认配置会实际采用上述超时。升级时应核对调用方的超时预期。

配置映射回归见 `tests/Bing.Utils.Net.Tests/FtpClientConfigurationTest.cs`，不访问 FTP 服务器；真实连接和传输行为仍需集成验收。

## 已知限制与注意事项

- **安全注意**：启用 SSL 时，当前证书验证回调无条件接受服务器证书，不能抵御中间人攻击；在修复前不要把它用于不可信网络或高敏感传输。
- **待实现/待确认**：`DisconnectTimeout`、`ReceiveTimeout`、`SendTimeout` 在当前 FluentFTP 配置中没有同义映射；`ConnectCount` 的连接重试策略尚未接入，不等同于校验重试。
- **待实现/待确认**：`KeepAlive` 的请求后保持连接语义不等同于 `SocketKeepAlive` 的 TCP 保活；`UseBinary` 未接入模式切换，ASCII 转换可能与当前严格字节长度校验冲突，需要先确定验收契约。
- **待实现/待确认**：`LocalDirectory`、`RemoteDirectory` 未用作默认传输路径，调用方应继续显式传入路径。
- `FtpClientConfig.GetProxy()` 没有接入客户端；其代理凭据判断还要求密码为空，行为与常规配置预期不符，属于**待实现/待确认**。
- API 仅提供同步调用，不支持 `CancellationToken`。
- `CopyFile` 会把整个远端文件读入内存，不适合大文件。
- `UploadDirectory` 和 `DownloadDirectory` 当前忽略 FluentFTP 返回的逐项结果，只要未抛异常就返回 `true`，不能据此确认每个文件均传输成功。
- `DownloadStream` 与 `DownloadBytes` 会把长度为 0 的远端文件视为未找到；零字节文件场景需由调用方规避。
- 递归删除目录时只显式遍历子目录，目录内普通文件的处理依赖 FluentFTP 最终删除行为，非空目录结果需在目标服务器上验证。
- 集成测试需要外部 FTP 服务及凭据，本地单元测试不能替代真实网络验证。
- 仓库集成测试通过 `RUN_INTEGRATION_TESTS=true` 启用，并读取 `BING_FTP_HOST`、`BING_FTP_USERNAME`、`BING_FTP_PASSWORD` 和可选的 `BING_FTP_PORT`。
