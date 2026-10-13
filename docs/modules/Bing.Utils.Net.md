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
- 构造时实际接入主机、端口、凭据、字符集和加密模式；底层重试次数固定为 3。

## 已知限制与注意事项

- **安全注意**：启用 SSL 时，当前证书验证回调无条件接受服务器证书，不能抵御中间人攻击；在修复前不要把它用于不可信网络或高敏感传输。
- `FtpClientConfig` 中代理、超时、主动/被动模式、二进制和 KeepAlive 等字段当前未在 `FtpClient` 构造逻辑中应用，属于**待实现/待确认**。
- `FtpClientConfig.GetProxy()` 没有接入客户端；其代理凭据判断还要求密码为空，行为与常规配置预期不符，属于**待实现/待确认**。
- API 仅提供同步调用，不支持 `CancellationToken`。
- `CopyFile` 会把整个远端文件读入内存，不适合大文件。
- `UploadDirectory` 和 `DownloadDirectory` 当前忽略 FluentFTP 返回的逐项结果，只要未抛异常就返回 `true`，不能据此确认每个文件均传输成功。
- `DownloadStream` 与 `DownloadBytes` 会把长度为 0 的远端文件视为未找到；零字节文件场景需由调用方规避。
- 递归删除目录时只显式遍历子目录，目录内普通文件的处理依赖 FluentFTP 最终删除行为，非空目录结果需在目标服务器上验证。
- 集成测试需要外部 FTP 服务及凭据，本地单元测试不能替代真实网络验证。
- 仓库集成测试通过 `RUN_INTEGRATION_TESTS=true` 启用，并读取 `BING_FTP_HOST`、`BING_FTP_USERNAME`、`BING_FTP_PASSWORD` 和可选的 `BING_FTP_PORT`。
