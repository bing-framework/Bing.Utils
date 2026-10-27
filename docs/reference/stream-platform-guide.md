# 流处理与资源管理

本页对应 S01–S15。新增能力保留原有接口、目标框架和默认行为；适合明确选择流式处理、资源预算或任务调度的调用方。

## 选择入口

| 需要 | 入口 | 内存及资源边界 |
|---|---|---|
| 稳定停止文件监控、溢出恢复 | `FileWatcher.StopAsync/DisposeAsync` | 可选有界 debounce；重扫只恢复快照差异 |
| 管理可恢复的临时正文 | `TemporaryFileStore` | 持久化逻辑额度，活动租约独占 |
| 小正文在内存、大正文落盘 | `SpilloverStream` | 内容或位置越过阈值后预留最大长度，释放删除 |
| 下载任务按主机排队 | `HttpDownloadManager` | 队列和活动任务有界，每个下载的分片并发另行配置 |
| 明确下载是否完成 | `DownloadFileSegmentedResultAsync` | 未完成结果包含失败范围及恢复位置 |
| 从 JSON 数组逐项读取 | `JsonStream.ReadArrayAsync<T>` | 单值缓存、固定预读、单值及总输入预算 |
| CSV/TSV 逐行读写 | `CsvStream` | 单行缓存、字段和行预算，支持带引号的多行字段 |
| 流式序列批量消费 | `PrefetchAsync/BatchAsync` | 有界队列、单一在途源读取 |
| HTTP 长连接事件 | `GetServerSentEventsAsync` | SSE 单事件及单连接输入预算，显式重连 |
| 数据库大字段、多结果 | `TabularDataReader` | 固定缓冲，不通过 `GetValue` 加载完整字段 |
| 子进程实时交互 | `CommandOptions` 的 IO 委托 | 每管道串行背压，捕获量独立受限 |
| 带时间信息的 ID | `Uuid7.Generate` | UUIDv7；不承诺同毫秒内单调 |

文件监控详见[监控指南](file-watcher-guide.md)，CSV 详见[CSV/TSV 指南](csv-stream-guide.md)，FTP 详见[异步目录指南](ftp-directory-guide.md)，ID 详见[UUIDv7 指南](uuid7-guide.md)。

## 临时存储、溢写与提交

```csharp
var store = new TemporaryFileStore(Path.GetFullPath("work/tmp"), maxReservedBytes: 512L * 1024 * 1024);
string id;
using (var lease = store.CreateFile(maxBytes: 64L * 1024 * 1024))
{
    id = lease.Id;
    await input.CopyToAsync(lease.Stream);
    lease.Resize(lease.Stream.Length); // 不能小于长度或当前位置
}
using (var lease = store.OpenFile(id)) // 位置为零，独占活动租约
    lease.Promote(Path.GetFullPath("work/final.bin"), overwrite: true);

await using var spill = new SpilloverStream(store, memoryThresholdBytes: 1024 * 1024,
    maxLength: 64L * 1024 * 1024);
await input.CopyToAsync(spill);
spill.Position = 0;
await spill.CopyToAsync(output);
```

存储和最终目录应事先配置在同一文件系统。`Promote` 提交完整正文；不提供跨卷复制回退。新目标在 Windows 使用禁止复制的移动，在 Linux/macOS 使用原子硬链接提交再清理源；文件系统不支持时失败并保留可重试租约。覆盖已有目标使用文件替换。提交成功后的元数据清理失败仍会抛错，此时目标已经提交，可用 `ListFiles/DeleteFile` 清理残余租约。并发修改目录布局不属于其事务保证。相关底层约束见 [MoveFileEx](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-movefileexw) 和 [POSIX link](https://pubs.opengroup.org/onlinepubs/9799919799/functions/link.html)。

`ListFiles` 是快照；`OpenFile` 后才取得活动锁。`SetRetention(reason)` 在关闭租约后仍阻止过期清理，适合正文位于存储外的下载任务；`SetRetention(null)` 解除保护。保护不覆盖显式 `Delete/DeleteFile` 或 `deleteOnDispose`。`CleanupExpiredAsync` 支持取消；取消前已清理的条目不会回滚。不得绕过租约流按 `FilePath` 写入。

已知一批正文预算时使用 `CreateFiles(new long[] { ... })`：一次扫描和根锁内验证总额度。调用方负责释放每个返回租约。批量失败尝试回滚，清理失败的残余仍计入额度。这里没有进程内缓存配额，所以其他进程创建的租约仍能被检查到；独立 `CreateFile` 保持每次扫描的行为。

`SpilloverStream` 不能并发读写；首次溢写预留 `maxLength`，内存阈值并不是 CLR 分配量上限。定位位置越过阈值也会转入磁盘，之后不会自动转回内存。存储不足或溢写复制失败不会丢掉原有内存正文。

## 下载调度、结果与诊断

```csharp
await using var manager = new HttpDownloadManager(maxConcurrentDownloads: 4,
    maxDownloadsPerHost: 2, queueCapacity: 32);
var metrics = new HttpTransferMetrics();
var options = new HttpSegmentedDownloadOptions
{
    SegmentSize = 4L * 1024 * 1024,
    MaxConcurrency = 2,
    TemporaryStore = store,
    Metrics = metrics
};
var result = await manager.EnqueueAsync(new Uri(url), request, targetPath, options, token);
if (!result.IsComplete)
{
    Console.WriteLine(result.RecoveryPath);
    foreach (var range in result.FailedSegments) Console.WriteLine(range);
}
// 明确放弃暂停中的任务，清理正文后释放额度：
await HttpSegmentedDownloadFiles.DiscardAsync(targetPath, store, token);
```

`origin` 必须对应请求实际来源，用于主机分组；重定向后的主机不重新分组。管理器限制下载任务数，单任务分片并发另外由 `MaxConcurrency` 控制。队列满时 `EnqueueAsync` 等待，取消排队不会发起请求。不要同时复用具有可变配置的同一个请求。管理器不拥有请求或存储；异步释放会取消并等待任务收尾。进度回调可能并发，管理器自身的观察回调异常被隔离。

启用 `TemporaryStore` 后，下载前为分片及合并目标预留正文总长的两倍；旧残留更多时按残留量提高预留，完成清理后才写入新正文。这个预算不包括已有最终文件、状态元数据、文件系统块开销，也不是磁盘空闲空间保证。恢复时保留原存储关联，不能切换存储或绕过额度继续。上次失败的分片/合并正文清理失败会阻止继续写入。旧同步 `Discard` 和通用过期清理不会替外部下载任务释放保留额度；应使用上述带存储的 `DiscardAsync`。

`Metrics` 可跨恢复调用累积：`BytesRead` 是成功读取的传输正文（含重试），`ThrottleWaitTime` 是限速器等待，`QueueWaitTime` 是管理器排队等待，`CheckpointTime/MergeTime` 是对应阶段耗时。并发阶段累计时间可能超过墙钟时间。管理器聚合字节按每次执行计算，恢复提交另算一次；不能视为全局不重复的业务文件总量。

`Result.IsComplete` 为 false 时目标未合并，`FailedSegments/Failures` 描述未完成范围和已捕获失败。路径探测、取消、状态格式、提交失败仍抛异常。旧返回状态的入口保持不变。

## JSON、异步批次与数据库

```csharp
var rows = JsonStream.ReadArrayAsync<MyRecord>(input,
    new JsonArrayReadOptions { MaxItemBytes = 1024 * 1024, MaxInputBytes = 128L * 1024 * 1024 },
    leaveOpen: true, cancellationToken: token);
await foreach (var batch in rows.PrefetchAsync(8, token)
    .BatchAsync(100, TimeSpan.FromMilliseconds(250), token))
    await SaveBatchAsync(batch, token);

await TabularDataReader.ForEachResultAsync(reader, async (index, current, ct) =>
{
    var columns = TabularDataReader.ReadSchema(current);
    while (await current.ReadAsync(ct))
        await TabularDataReader.CopyBinaryFieldAsync(current, 0, output, maxBytes: 32L * 1024 * 1024, cancellationToken: ct);
}, leaveOpen: true, cancellationToken: token);
```

JSON 数组只缓存一个值；预算按原始 UTF-8 字节计算，不限制反序列化后的对象图。支持开头 BOM 和可选尾逗号，不支持注释；格式错误包含元素编号及字节位置。提前结束不验证剩余数组，预读可能已越过最后交付值。选项及输入流不能在执行中被其他调用修改。

预取按容量背压，保持顺序，已入队项交付后传播源错误。批次时间从第一项开始，空源不产生空批次；时间到达时允许保留一次在途读取。提前结束会取消并等待源收尾，因此源必须合作响应取消。

数据库提供程序应使用 `CommandBehavior.SequentialAccess` 并按列顺序访问。`GetBytes/GetChars` 是同步提供程序操作，取消在调用前后及块写入时观察；文本预算以 UTF-16 字符数计。未知元数据保留为 null；`CreateDetailedSchema` 只应用可映射的 null/只读/字符串长度信息，不把提供程序元数据伪装成完整约束恢复。多结果回调不能自行调用 `NextResult` 或在返回后使用读取器。

## SSE 与命令实时 IO

```csharp
await foreach (var item in client.GetServerSentEventsAsync(new Uri(eventsUrl),
    new HttpSseOptions { MaxReconnects = 3, ReadOptions = new SseReadOptions { MaxEventBytes = 64 * 1024 } }, token))
    Console.WriteLine($"{item.Id}: {item.Data}");

await response.WriteServerSentEventsAsync(eventSource, maxEventBytes: 64 * 1024, cancellationToken: token);

var result = await CommandRunner.RunAsync(new CommandOptions
{
    FileName = executable,
    StandardInput = async (writer, ct) => { ct.ThrowIfCancellationRequested(); await writer.WriteLineAsync("input"); },
    OnStandardOutput = (chunk, ct) => ConsumeOutputAsync(chunk, ct),
    OnStandardError = (chunk, ct) => ConsumeErrorAsync(chunk, ct)
}, token);
```

SSE 按 [WHATWG Server-sent events](https://html.spec.whatwg.org/multipage/server-sent-events.html) 处理 UTF-8、多行 data、ID 重置、retry 和 CR/LF。默认不重连；显式配置后 EOF 或网络 IO 失败可以重连，携带已提交的 `Last-Event-ID`。204 停止；其他状态、内容类型错误、预算超限及取消不重试。读取单事件预算包含字段及注释，不含 CR/LF；写入预算包含完整帧。总输入预算每次连接重新计算。HTTP 响应逐条刷新，代理缓冲须由部署配置决定。

命令标准输入是无 BOM UTF-8，委托结束后刷新并关闭以发送 EOF。stdout/stderr 各自以最多 4096 字符的块交付，不能假设完整行；两个管道回调可能并发。回调慢时对该管道形成背压，捕获上限不会阻止实时回调继续接收。委托抛错会终止进程；取消和超时等待委托收尾，委托必须响应传入令牌，不得在返回后继续操作流。

## 共同生命周期契约

流复制、CSV 和数据库行读取现在可接入 `OperationProgress`，配合 `TabularMap<T>` 复用中文字段映射与错误定位，详见[统一进度与表格映射指南](tabular-mapping-guide.md)。`Total` 未知时保持 null；资源预算不充当总量。普通流的写入完成只结束 Write 阶段，不报告文件已提交。

SSE 可通过 `new HttpSseOptions { IdleTimeout = TimeSpan.FromSeconds(30), MaxReconnects = 3 }` 限制等待正文新字节的时间。默认 `null` 不限制，必须大于零。每次底层读取返回字节后重新计算，注释心跳同样有效；消费者处理事件或暂停枚举的时间不计入。超时关闭当前响应并按已有重连配置重试，次数耗尽抛 `TimeoutException`；显式取消优先传播原令牌，不触发重连。超时通过可取消读取实现，不遗留后台读取，底层自定义流须响应取消。头等待仍由 `HttpClient` 的超时控制。

`BatchAsync` 到期时优先交付现有批次，即使下一次读取也已经完成；该元素留给下一批。同步阻塞的 `MoveNextAsync` 无法被抢占，返回后通过单调时钟检查到期。消费者处理批次的时间仍不计入下一批。

| API | 起点/终点 | 资源归属 | 失败与取消 |
|---|---|---|---|
| JSON/CSV/SSE 流读取 | 当前输入位置，可能预读 | `leaveOpen` 默认 true | 开始前取消不接管；枚举开始后遵守关闭选项；已交付项不撤回 |
| SSE/CSV 写入、DB 字段复制 | 目标当前位置，不复位、不截断 | SSE/DB 保持目标打开；CSV 遵守 `leaveOpen` | 允许部分输出，不写入超过明确预算的正文 |
| 临时租约 | 新建和重开均位置零 | 调用方释放租约，不能只关闭其流 | 默认关闭后保留；显式删除或 `deleteOnDispose` 清理 |
| 溢写流 | 初始空流，可定位 | 流拥有内存/租约；不拥有 store | 释放时删除临时正文 |
| SSE HTTP 客户端 | 每次新响应 | 拥有每次请求/响应；不拥有 HttpClient | 提前结束释放当前响应；重连由明确配置决定 |

这些新增流式 API 没有可对照的旧同名入口，性能基线记为 `NOT_APPLICABLE`。临时存储批量预留的同环境数据和本轮验证见[实施记录](../quality/stream-platform-execution.md)。
