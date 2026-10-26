# 工具能力扩展使用指南

本轮新增 API 保留原有入口。严格错误传播、预算、结构化执行等行为均通过独立入口选择。目标框架仍为 net8.0/net7.0/net6.0/netstandard2.0。

## 文件和流

```csharp
using Bing.IO;

var transfer = await StreamTransfer.CopyAsync(input, output,
    new StreamTransferOptions {
        MaxBytes = 32 * 1024 * 1024,
        ComputeSha256 = true,
        ExpectedSha256 = expectedHash // 可选，64 位十六进制
    }, cancellationToken: token);

// 同目录临时文件写入并验证，成功后 Move/Replace；父目录必须存在。
await AtomicFile.WriteAsync(path, input,
    new StreamTransferOptions { MaxBytes = 32 * 1024 * 1024 },
    cancellationToken: token);

// 错误传播给调用方；需要旧 null/空值错误模型时继续使用旧入口。
byte[] bytes = await FileHelper.ReadBytesStrictAsync(path,
    maxBytes: 1024 * 1024, cancellationToken: token);
```

| 入口 | 输入与完成位置 | 资源归属 | 失败/取消 |
|---|---|---|---|
| `StreamTransfer.Copy/CopyAsync` | 源当前位置到 EOF，向目标当前位置写；不复位、不截断 | 默认保留两流；`leaveOpen=false` 在处理阶段接管 | 可能有部分输出，但不写超额字节；可额外读取 1 字节判断超限；取消传播 |
| `AtomicFile.Write/WriteAsync` | 从源当前位置读取，新文件内容完整替代目标 | 默认保留源；内部临时文件由方法管理 | 提交前失败保留原目标、清理临时文件；不做非原子复制降级；成功提交后不回滚 |
| `FileHelper.ReadBytesStrict*` | 读取完整文件，返回独立数组 | 方法管理文件流 | 文件、权限、I/O、限额及取消异常传播 |
| `FileHelper.Write(path, stream)` / `WriteAsync` | 可定位源先到 0，成功后停在末尾；不可定位源从当前位置读取 | 保留源，方法管理目标文件 | 保留旧入口错误约定；直接复制，不再创建完整中间数组 |
| `LargeMemoryStream.GetWrittenSegments()` | 不改变 Position，仅返回 Length 内有效数据 | 借用底层页，不转移数组所有权 | 修改、扩容、缩容或释放后不可继续使用旧视图 |

`StreamTransfer` 可计算 SHA-256、验证预期摘要和报告累计字节。`IProgress<long>` 的调度方式由调用方实现决定；`Progress<T>` 可能异步派发，不代表已完成文件提交。

`AtomicFile` 保证单文件提交边界，不承诺断电持久性或目录事务。`LargeMemoryStream` 推荐按实际负载显式选择页大小，如 `new LargeMemoryStream(1024 * 1024)`；`ToArray()` 是需要独立快照时的选择。`PooledMemoryStream.GetBuffer()` 保持返回副本的安全语义。

## ZIP 和正则

```csharp
await ZipHelper.UnZipWithOptionsAsync(zipPath, targetDirectory,
    new ZipExtractionOptions {
        MaxEntries = 1000, MaxDirectoryDepth = 8,
        MaxEntryBytes = 16 * 1024 * 1024,
        MaxTotalBytes = 64 * 1024 * 1024,
        OverwriteFiles = false
    }, token);

bool matched = Regexs.IsMatchWithTimeout(text, pattern, TimeSpan.FromMilliseconds(200));
```

ZIP 先预检路径与声明大小，再逐文件检查实际读取量并临时写入。目录条目计入条目数；根目录文件深度为 0。整个归档不具备事务回滚，取消保留已经提交的文件。覆盖使用文件替换，仅平台明确不支持替换时沿用复制降级；调用方应控制目标目录，避免并发修改目录结构。

`Regexs` 新增 `IsMatchWithTimeout/MatchWithTimeout/MatchesWithTimeout/ReplaceWithTimeout/SplitWithTimeout/GetValueWithTimeout/GetValuesWithTimeout`。旧入口默认行为不变；不可信输入推荐显式超时。缓存仍区分文化、选项、模式和超时。

## 异步序列、JSON 数组与 NDJSON

```csharp
await events.ForEachAwaitAsync(ProcessAsync, maxDegreeOfParallelism: 8,
    cancellationToken: token);

await foreach (var result in events.SelectConcurrentAsync(TransformAsync,
    maxDegreeOfParallelism: 8, preserveOrder: true, cancellationToken: token)) {
    // 一次最多保留 8 个在途操作/重排结果，并允许一个源读取。
}

await JsonStream.WriteArrayAsync(output, events, flushEveryItems: 100,
    cancellationToken: token);
await JsonStream.WriteNdjsonAsync(output, events,
    new NdjsonOptions { MaxRecordBytes = 1024 * 1024, FlushEveryRecords = 100 },
    cancellationToken: token);
await foreach (var item in JsonStream.ReadNdjsonAsync<MyEvent>(input,
    new NdjsonOptions { MaxRecordBytes = 1024 * 1024 }, cancellationToken: token)) {
}
```

异步源只枚举一次，不并行调用 `MoveNextAsync`。失败、外部取消和消费者提前结束都会停止领取并等待在途操作；内部停止令牌传给操作和源。操作应合作响应取消；不可取消的操作可能延长收尾。默认有序转换可能等待较早的慢操作，`preserveOrder=false` 返回已完成结果，同批完成顺序不保证。

JSON 数组按元素序列化，不收集完整序列；单个大对象仍有序列化器自身内存成本。NDJSON 每次缓存一条记录，写入目标前验证该记录字节预算。预算按 UTF-8 计算，不包括 CRLF/LF 和文件开头 BOM；支持末尾无换行；默认拒绝空行，可显式忽略。无效记录报告行号。输出默认不缩进，NDJSON 明确拒绝缩进选项。失败可能留下先前记录或部分当前输出，流默认保持打开。

服务端使用 `response.WriteJsonArrayStreamAsync(source, ...)` 或 `response.WriteNdjsonStreamAsync(source, ...)`。两者同时遵守显式取消和 `RequestAborted`，设置对应 UTF-8 内容类型并保留响应体。旧 `WriteJsonAsync(string)` 继续写原文；`WriteJsonStreamAsync(string)` 写 JSON 字符串值。

## DataTable/DataSet 双向 JSON

```csharp
var json = TabularJson.SerializeDataSet(dataSet);
using var restored = TabularJson.DeserializeDataSet(json,
    new TabularJsonOptions {
        MaxInputBytes = 16 * 1024 * 1024, MaxTables = 32,
        MaxColumnsPerTable = 128, MaxRowsPerTable = 100000, MaxDepth = 64
    });

// 旧 Rows/Tables 不含类型，读取必须提供 schema；schema 不被修改。
using var oldTable = TabularJson.DeserializeTable(oldJson,
    new TabularJsonOptions { TableSchema = existingSchema });
```

新入口默认使用 `$format: "bing.tabular"`、`$version: 1`、`kind: "table"/"dataset"`。表模型包含 `name/namespace/prefix/locale/caseSensitive/columns/uniqueConstraints/foreignKeys/rows`；数据集还包含 `enforceConstraints/tables/relations`。行按列顺序编码为数组；列顺序保留。

列保存名称、类型、空值许可、标题、命名空间、映射、字符串长度、默认值、只读、自增种子/步长及日期模式。唯一约束记录是否为主键；外键保存关联列及删除/更新/接受拒绝规则；关系保存名称、关联列和嵌套状态。先在新数据集中装载所有结构和行，再恢复约束；成功才返回。

| 类型 | v1 编码 |
|---|---|
| string、bool、byte/sbyte/short/ushort/int/uint | JSON 对应标量 |
| char | 一个非代理 UTF-16 字符的字符串 |
| long、ulong、decimal、float、double | 不变区域字符串，避免数字精度损失；浮点支持非有限值 |
| DateTime、DateTimeOffset | `O` 往返格式 |
| TimeSpan、Guid、byte[] | `c`、`D`、Base64 |
| DBNull | JSON null |

不从 JSON 类型名称加载 CLR 类型。表达式列、自定义对象列、非空 ExtendedProperties 明确拒绝。默认 v1 保存当前快照，拒绝 Deleted 行，不保存行编辑历史；恢复后的行状态为 Unchanged。显式 v2 支持表内行编辑状态，见 [收敛指南](capability-refinement-guide.md)。自增内部计数不保证与原表完全一致。

`DataTableConverter/DataSetConverter` 无参构造仍写旧 Rows/Tables 格式；传入 `TabularJsonOptions` 可显式选择 v1/v2。已有 `JsonReader` 无法准确统计原始输入字节，转换器遇到 `MaxInputBytes` 会明确拒绝；需要字节预算时使用 `TabularJson` 文本/流入口。默认表格流读取缓冲整个预算内的 JSON；显式 Version2 或 OpenReaderAsync 可逐行解析结构先行的文档。

## HTTP 下载和诊断

```csharp
request.MaxResponseBytes(4 * 1024 * 1024) // 每个响应
       .MaxDownloadBytes(128 * 1024 * 1024); // 最终下载逻辑总量

using var diagnostics = new HttpDiagnostics(e => Log(e.Kind, e.Outcome));
request.UseInterceptor(diagnostics.CreateInterceptor())
       .ObserveCompletion(diagnostics.RecordCompletion)
       .ObserveRetry(diagnostics.RecordRetry);
```

下载预算包含续传已有字节，重试不重复累计；普通字节/JSON 请求仍只受响应限额约束。`WithoutMaxDownloadLimit()` 单独取消下载预算。普通文件下载、续传和分片均在提交最终文件前执行限额检查。未知长度正文在读取时检查，绝不写超限字节；流目标可能保留部分内容。

当前请求的分片入口直接把响应写入分片临时文件；响应保持打开直至写入结束。调度器仅创建并发度数量的工作任务，状态清单仍随分片数增长。SHA-256 在分片写入中计算，恢复/最终合并仍重新验证已有文件。

`DownloadFileSegmentedFromRequestAsync` 将请求 URI/请求头指纹、内容编码和远端验证器绑定到恢复状态；优先使用强 ETag，其次使用秒级精度的 Last-Modified，并在每个范围请求中发送 `If-Range`。资源或请求身份变化会清除旧分片并重新下载；单次下载中的验证器或编码变化会使该片失败，未完成前不会替换目标文件。若服务端不提供强 ETag 或 Last-Modified，自动入口仍可完成本次下载，但每次调用都会从头获取分片；服务端也没有验证器时，客户端无法识别同长度内容变化。显式委托入口不控制实际 HTTP 请求，调用方负责验证所有分片来自同一资源版本。

`HttpDiagnostics` 不记录 URL、头部、正文或异常文本。net6+ 发布 ActivitySource/Meter（默认名 `Bing.Utils.Http`）；netstandard2.0 提供相同回调。发送 span 到收到响应头结束，`bing.http.duration` 包含完成回调所覆盖的正文与收尾时间。指标为 `bing.http.requests/retries/duration`，标签只含固定结果分类和状态码。可在已有回调中调用 `RecordCompletion/RecordRetry`，避免替换已有观察器；适配器应在所有请求结束后释放。

字节需求选 `GetBytesAsync`；对象需求选普通缓冲或 `GetJsonStreamAsync`；顶层数组选 `GetJsonArrayStreamAsync`；大文件选下载 API。无状态客户端可显式 `UseCookies(false)`；请求隔离/Cookie 配置可能影响底层连接复用，默认配置保持不变。

## 命令、时间与随机 ID

```csharp
var result = await CommandRunner.RunAsync(new CommandOptions {
    FileName = "dotnet", Arguments = new[] { "--info" },
    Timeout = TimeSpan.FromSeconds(10),
    MaxStandardOutputChars = 65536, MaxStandardErrorChars = 65536
}, token);
result.EnsureSuccess();

var calendar = new BusinessCalendar(holidays: holidays, workingDayOverrides: makeupDays);
var due = calendar.AddBusinessDays(start, 5);
var instant = TimeZoneConversion.ResolveLocalTime(localUnspecified, timeZone,
    AmbiguousTimeResolution.Earlier);
var clock = new FixedClock(instant);
string id = SecureRandomId.Create(24, SecureRandomId.AlphaNumericAlphabet);
```

命令参数逐项传递，不经过 shell；要运行脚本时明确指定脚本解释器。双路管道持续读取，即使达到捕获上限也不会停止消费；截断标记和 `OutputComplete` 区分捕获上限与收尾超时。非零退出码通过结果返回，只有 `EnsureSuccess()` 显式抛错。取消保留调用方令牌，超时抛 TimeoutException；netstandard2.0 只能保证终止直接子进程，现代目标可终止进程树。

时钟接口位于 `Bing.Date.Clock`，时区工具位于 `Bing.Date.TimeZones`，日历位于 `Bing.Date.Calendars`。时区转换显式传入 TimeZoneInfo，不依赖系统本地时区；跳时产生的无效时间拒绝，歧义默认拒绝，可选择按 UTC 排序的较早/较晚时间点。补班优先于节假日与周休息日；工作日偏移保持时分秒和 Kind，零返回原值；计数使用半开区间，反向返回负数。日历不内置会随年份变化的法定假期数据。

`SecureRandomId` 按指定正长度生成，字符集允许 2–256 个不重复、非代理 UTF-16 字符；固定 256 字节随机块和拒绝采样避免偏差，不承诺全局唯一。原 `RandomNonceStrGenerator` 仍保持最小长度 16 和兼容参数语义。

FTP 的新异步入口、配置映射及未支持项见 [FTP 能力矩阵](ftp-capabilities.md)。
