# Bing.Utils.Net

## 功能说明

`Bing.Utils.Net` 基于 FluentFTP 提供同步与原生异步 FTP/FTPS 客户端，包括文件传输、目录操作、代理配置及连接管理。异步客户端支持取消令牌；同步 API 保留兼容边界且不接受取消令牌。本模块不提供 SFTP、TCP/UDP 或通用网络客户端。

## 核心类型

源码位于 `src/Bing.Utils.Net/Bing/Net/FTP`。

- `IFtpClient`、`FtpClient`：旧同步客户端契约及其实现。
- `FtpConfigurationMode`：选择兼容配置或完整配置模式。单参数 `FtpClient(config)` 仍使用 `Legacy`。
- `AsyncFtpClient`、`IFtpAsyncClient`：基于 FluentFTP 原生异步客户端的操作入口。
- `IFtpAsyncDirectoryClient`：可选的异步目录遍历、传输、校验和预算能力。
- `FtpClientConfig`、`AsyncFtpClientOptions`：同步与异步客户端分别使用的配置模型，不会互相自动转换。

## 用法

旧同步构造保持兼容：

```csharp
using Bing.Net.FTP;

var config = new FtpClientConfig {
    Host = "ftp.example.com", UserName = "user", Password = "secret"
};
using var ftp = new FtpClient(config);
ftp.Connect();
bool uploaded = ftp.UploadFile(@"C:\data\report.csv", "/incoming/report.csv");
```

需要应用完整同步配置时显式选择 `Complete`：

```csharp
using Bing.Net.FTP;

var config = new FtpClientConfig {
    Host = "ftp.example.com", UserName = "user", Password = "secret",
    LocalDirectory = @"C:\data", RemoteDirectory = "/incoming"
};
using var ftp = new FtpClient(config, FtpConfigurationMode.Complete);
bool uploaded = ftp.UploadFile("report.csv", "report.csv");
```

原生异步客户端直接调用 FluentFTP 异步 API：

```csharp
using System.Threading;
using Bing.Net.FTP;

using var ftp = new AsyncFtpClient(new AsyncFtpClientOptions {
    Host = "ftp.example.com",
    UserName = "user",
    Password = "secret",
    EncryptionType = EncryptionType.Explicit
});

using var cancellation = new CancellationTokenSource();
await ftp.ConnectAsync(cancellation.Token);
bool downloaded = await ftp.DownloadFileAsync(@"C:\data\report.csv", "/incoming/report.csv",
    cancellationToken: cancellation.Token);
```

## 安全、配置与生命周期

启用 FTPS 时，同步与异步客户端默认只接受 `SslPolicyErrors.None` 的证书。显式提供的证书策略可以决定是否接受证书；策略抛异常时先拒绝证书，再传播异常。不要用无条件接受策略绕过证书验证。

单参数同步构造使用 `Legacy`，为兼容保留旧配置映射。该模式会应用主机、端口、凭据、字符集、`EncryptionType`，以及连接/读取超时和主动/被动模式；它不应用 `EnableSsl` 回退、代理、数据类型、默认目录或连接生命周期等完整配置。`FtpClientConfig.GetProxy()` 只创建 `WebProxy`，Legacy 不会把该代理交给 FluentFTP。新增或迁移调用方应显式选择 `Complete`，并检查默认超时变化。

| 配置 | 同步 `Complete` 模式 |
| --- | --- |
| `Timeout` | 控制与数据连接建立超时，默认 100,000 毫秒 |
| `ReadWriteTimeout` | 控制与数据通道读取超时，默认 300,000 毫秒；显式设置的 `ReceiveTimeout` 会覆盖它 |
| `UsePassive`、`UseBinary` | 选择自动主动/被动连接及上传/下载 Binary/Ascii 类型 |
| `EncryptionType`、`EnableSsl` | 显式加密类型优先；类型为 None 且 `EnableSsl=true` 时回退到 Explicit FTPS |
| `ConnectCount` | 首次连接失败后的额外连接尝试数；不会重放文件操作 |
| 代理字段 | 映射到受支持的 FluentFTP 同步代理客户端及类型 |
| `LocalDirectory`、`RemoteDirectory` | 分别作为本地和远程相对路径基准；不改变进程当前目录 |
| `KeepAlive` | false 时在最外层操作结束后断开控制连接；不是 TCP Socket 保活 |

完整配置会在构造时复制并校验配置。当前后端不支持独立同步 `SendTimeout` 或 `DisconnectTimeout`；在 `Complete` 模式中显式设置任一字段会抛出 `NotSupportedException`。没有显式设置的历史默认数值不会被当作用户要求。`FtpClientConfig.GetProxy()` 返回 `WebProxy`，但同步完整模式使用独立的 FluentFTP 代理配置，不是把 `WebProxy` 直接传入后端。

异步客户端使用独立的 `AsyncFtpClientOptions`，支持原生异步连接、文件/流操作和 `CancellationToken`，并支持 FTP 代理与 TCP Socket 保活。单个客户端实例上的命令会串行执行；排队时取消不会中断正在执行的另一条命令，开始执行后令牌会传给 FluentFTP。异步目录预算、路径和失败结果契约见[异步 FTP 目录指南](../reference/ftp-directory-guide.md)及[FTP 能力矩阵](../reference/ftp-capabilities.md)。

## 已知限制

- 旧同步 API 不接收 `CancellationToken`；需要取消语义时使用 `AsyncFtpClient`。
- 同步 `CopyFile` 会先把完整远端文件载入 `MemoryStream` 再上传，不适用于大文件。
- 旧 `Legacy` 模式有意保留原配置行为；不得将 `Complete` 模式新增映射推断为旧构造也启用了所有配置。
- 同步 `DownloadStream` 与 `DownloadBytes` 在二进制校验路径上仍把空结果按未找到处理，零字节远端文件不能通过该路径可靠下载。
- 新配置、代理握手、服务器对 ASCII 换行的处理，以及真实 FTPS 证书链仍需隔离 FTP/FTPS 服务验证；无网络单元测试不代表真实服务器已验收。

## 测试与记录

本地测试位于 `tests/Bing.Utils.Net.Tests`，覆盖证书策略、完整同步配置、原生异步后端、取消和异步目录行为。真实 FTP/FTPS 服务及代理验收属于外部验证。

新能力和配置映射的实现状态与本地证据见[工具能力扩展执行记录](../quality/capability-expansion-execution.md)；模块能力概览见[FTP 能力矩阵](../reference/ftp-capabilities.md)。
