# 异步 FTP 目录操作

目录能力通过可选的 `IFtpAsyncDirectoryClient` 提供，因此旧版 `IFtpAsyncClient` 自定义实现不需要新增成员。`AsyncFtpClient` 同时实现该接口；使用旧接口变量时，可调用 `DirectoryOperations()` 获取能力接口，或使用对应扩展方法。

```csharp
using var client = new AsyncFtpClient(new AsyncFtpClientOptions { Host = "ftp.example.com" });
await client.ConnectAsync(cancellationToken);

var entries = await client.GetDirectoryEntriesAsync("/reports", cancellationToken);
foreach (var entry in entries)
{
    Console.WriteLine($"{entry.Type}: {entry.Name}, {entry.Size}, {entry.ModifiedTime}");
}

var result = await client.DownloadDirectoryAsync(
    "./reports",
    "/reports",
    FtpConflictPolicy.Resume,
    progress,
    cancellationToken);
```

`GetDirectoryEntriesAsync` 返回直接子项的名称、完整路径、类型、可用大小和修改时间。FTP 服务器可能无法提供大小或时间；未知大小映射为 `null`，零字节文件仍返回 `0`。`GetFileChecksumAsync` 使用服务器支持的 hash 命令；默认算法 `NONE` 选择服务器可用算法。若服务器未提供有效校验值，方法抛出 `NotSupportedException`，FTP 命令本身的网络错误仍按原异常传播。

目录上传和下载逐层枚举，目录按合并处理，冲突策略应用于文件：

- `Error`：遇到已存在文件时抛出 `IOException`；传输前检查并要求后端以跳过模式防止检查后的覆盖竞态。
- `Skip`：保留已有文件，并将跳过路径放入结果。
- `Overwrite`：覆盖目标文件。
- `Resume`：让 FluentFTP 从现有目标长度续传；服务器必须支持 REST/续传语义。

返回的 `FtpDirectoryTransferResult` 汇总传输成功、跳过和后端报告失败的路径，以及创建的目录数。底层 I/O 异常和取消会直接抛出；此时已完成的文件不会回滚。传入的 `IProgress<FtpProgress>` 会原样传给每个文件传输。

远程目录项名称在组合路径前必须是单一片段；空名称、`.`、`..`（包括 Windows 会规范化为这两种名称的尾随空格/点形式）、绝对路径和包含斜杠或反斜杠的名称会被拒绝。远端链接和未知项不会递归。下载目标通过 `Path.GetFullPath` 验证仍位于调用方指定的本地根目录；本地 reparse point 不会被跟随，根目录本身是 reparse point 时操作失败。上传的本地源根目录或下载的本地目标根目录及其相关祖先必须由可信调用方控制，并确保在整个操作期间不会被其他进程替换。现有路径规范化和 reparse point 检查可防止静态链接导致的路径越界，但不提供基于句柄的安全隔离，无法抵御恶意本地并发替换。

目录移动支持 `Error`、`Skip` 和 `Overwrite`；`Resume` 不适用于目录并抛出 `NotSupportedException`。目录删除始终递归删除远端目录内容。FTP 服务端对覆盖目录的支持各异，服务器拒绝时原样传播错误。

单个客户端上的目录操作与普通 FTP 命令共用一个异步 gate。取消等待 gate 的调用不会干扰当前命令；获得 gate 后，取消令牌会传给后端 listing、传输、校验、移动或删除操作。上传会跳过本地 reparse point；下载会跳过远程链接，避免递归离开约定根目录。

## 显式目录预算

```csharp
var result = await client.DownloadDirectoryWithLimitsAsync(
    "./reports", "/reports",
    new FtpDirectoryTransferLimits
    {
        MaxFiles = 1000,
        MaxDepth = 8,
        MaxTotalFileBytes = 512L * 1024 * 1024
    },
    conflictPolicy: FtpConflictPolicy.Skip,
    cancellationToken: cancellationToken);
```

预算通过可选的 `IFtpAsyncDirectoryLimitClient` 实现；旧 `IFtpAsyncClient` 变量可使用同名扩展方法，不支持能力时抛 `NotSupportedException`，不会退回无预算操作。旧目录入口保持无限制；新入口复制选项，均为非负、包含边界的可空值。

- `MaxFiles`：遍历到的普通文件数，包括因冲突而跳过的文件，链接不计。零拒绝任何普通文件。
- `MaxDepth`：根目录为零。零允许根目录中的文件，在创建或遍历第一级子目录前失败。
- `MaxTotalFileBytes`：实际选中传输文件的完整大小累计，续传也按完整大小计算，跳过文件不计。启用后未知或负大小在文件传输前失败；后端报告失败的尝试不退回预算。

超限抛 `InvalidDataException` 并停止后续传输；已创建的目录及完成的文件不会回滚。大小来源是本地 `FileInfo` 或远程 listing，文件在检查后增长、服务器错误声明、协议开销及底层重试不受该值约束，因此它是传输准入预算，不是磁盘或网络实际字节硬上限。listing 仍由服务器按目录返回集合，不保证超大单目录常量内存。校验续传和下载原子提交保留后续专项。
