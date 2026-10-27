# 传输限速、分片任务与临时文件

## 从接口创建分片下载

引用 Bing.Utils.Http，并引入 `Bing.Http`、`Bing.IO`：

```csharp
IHttpRequest<string> request = /* 已配置的请求 */;
var limiter = new TransferRateLimiter(4 * 1024 * 1024, burstBytes: 64 * 1024);
var options = new HttpSegmentedDownloadOptions
{
    SegmentSize = 4 * 1024 * 1024,
    MaxConcurrency = 4,
    MaxRetries = 2,
    RetryDelay = TimeSpan.FromSeconds(1),
    RateLimiter = limiter
};

var result = await request.DownloadFileSegmentedAsync(path, options, cancellationToken);
bool complete = result.Segments.All(segment => segment.Completed);
```

入口自动探测长度、范围支持和资源验证器，沿用已有检查点身份校验、响应/下载大小限制及原子合并。所有片段完成才提交目标文件；单片最终失败通过返回状态和 `Failure` 回调报告，调用成功返回不等同于完整下载。参数错误、探测失败和取消仍抛异常。

新增 `IHttpRequestSegmentedDownload<TResult>` 是 HTTP 包内的可选接口；旧 `IHttpRequest<TResult>` 实现无需新增成员。不支持该能力的实现调用扩展时抛 `NotSupportedException`。

## 任务生命周期

```csharp
await using var task = request.CreateSegmentedDownloadTask(path, options);
Task<HttpDownloadSegmentsState> running = task.StartAsync(cancellationToken);

// 例如由外部控制流程请求暂停。PauseAsync 等待所有在途片段收尾。
await task.PauseAsync();
try { await running; }
catch (OperationCanceledException) { }

var checkpoint = task.InspectCheckpoint();
var resumed = await task.ResumeAsync(cancellationToken);
```

`Created → Running → Completed / Incomplete / Failed / Paused`；暂停过程中为 `Pausing`。`Incomplete`、`Failed`、`Paused` 可再次启动。已完成或已丢弃的任务不能重新启动；新建任务可执行新的下载。

任务创建时复制配置，运行期间修改原 options 不影响它；共享限速器的速率仍可动态修改。请求由调用方拥有，执行时不要更改请求配置或并发用同一个请求进行其他传输。

`DisposeAsync` 取消并等待收尾、保留检查点，不释放请求或限速器。并发释放等待同一操作。原始执行异常保留在运行任务和 `LastError` 中。取消回调抛错时，暂停/释放仍先收尾再传播该错误。回调中不得等待当前任务的暂停/释放，否则会等待自身。

## 共享带宽预算

同一个 `TransferRateLimiter` 可以供多任务、多流共享。`BytesPerSecond` 必须大于零；改值会唤醒等待者。`BurstBytes` 固定，初始允许一个突发窗口，空闲最多积累一个窗口，不承诺等待公平性。

```csharp
limiter.BytesPerSecond = 1024 * 1024;
using var input = new RateLimitedStream(source, limiter, leaveOpen: true);
await StreamTransfer.CopyAsync(input, destination, cancellationToken: cancellationToken);
```

也可包装上传输入流后交给已有流正文/正文工厂入口。包装流支持同步与异步读写，不支持定位，保留底层当前位置。同步入口会阻塞等待；异步等待可取消。短读归还未使用预算，失败的 I/O 保守计费。默认关闭包装器保留底层流，`leaveOpen: false` 才接管它。共享限制器线程安全，同一包装流的 I/O 仍须由调用方串行使用。

限制的是交付给调用方或授权写入的正文，不保证网络栈预读速率。分片配置不限制探测正文和本地合并 I/O。超低速率配合较大突发窗口会使每次授权等待较长，可显式选择较小窗口。

## 查询和清理检查点

```csharp
var snapshot = HttpSegmentedDownloadFiles.Inspect(path);
var preview = HttpSegmentedDownloadFiles.CleanupExpired(
    downloadDirectory, DateTime.UtcNow.AddDays(-7)); // 默认只预览
var cleaned = HttpSegmentedDownloadFiles.CleanupExpired(
    downloadDirectory, DateTime.UtcNow.AddDays(-7), dryRun: false);
```

清理与下载共用独占目标锁。查询/显式 `Discard(path)` 遇活动锁直接抛 I/O 异常；批量清理将锁冲突和损坏检查点列入 `Errors`/`SkippedTasks`。调用方应检查结果。清理保留最终目标和锁文件，只删除所属分片文件、分片临时文件及检查点；分片删除失败会保留检查点。它不递归扫描，也不删除陌生文件或符号链接。没有清单的孤立分片可按明确目标执行 `Discard`。

## 临时存储配额

```csharp
var store = new TemporaryFileStore(dedicatedDirectory, maxReservedBytes: 512L * 1024 * 1024);
using (var lease = store.CreateFile(maxBytes: 8 * 1024 * 1024, deleteOnDispose: true))
{
    await StreamTransfer.CopyAsync(source, lease.Stream, cancellationToken: cancellationToken);
    lease.Stream.Position = 0; // 继续处理已写内容
}
var statistics = store.GetStatistics();
var preview = store.CleanupExpired(DateTime.UtcNow.AddDays(-1));
```

创建前预留最大长度，正文写入、定位空洞与 `SetLength` 都不能超过单文件限额。配额包含保留文件的预留长度；实际内容很小也不会自动退回预留额度。默认释放租约只关闭流和活动锁，保留文件；删除文件才释放额度。统计持久化在目录中，重新创建存储实例后仍可读取。

同一实例的管理操作串行执行；跨实例/进程用磁盘锁协调，冲突抛 I/O 异常，需稍后重试。所有实例应使用相同额度。过期清理默认预览，按正文/标记最后修改 UTC 时间判断，跳过活动租约、陌生内容和链接，逐项报告错误。关闭 `lease.Stream` 不等同于释放租约。

根目录应专用于此存储并由可信调用方管理。配额只覆盖正文逻辑长度，文件系统元数据、其他目录和下载旧布局不在其中；不要直接通过 `FilePath` 写入以绕过限制。临时文件租约不自动迁移分片下载的旧布局，下载残留使用上面的专用维护入口。

数据库读取器、JSON 和 NDJSON 的逐行衔接见 [数据库流指南](tabular-datareader-guide.md)。

统一进度及旧 HTTP 观察器适配见[统一进度与表格映射指南](tabular-mapping-guide.md)。Read/Write 的最终事件表示对应阶段正常完成；取消和早停不补发完成事件。`Commit` 留给实际提交结果的上层，不能仅凭传输正文结束推断文件已保存。
