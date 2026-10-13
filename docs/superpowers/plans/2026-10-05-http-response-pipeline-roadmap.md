# HTTP 响应管线优化与能力扩展 Implementation Plan

> 历史记录：本文是 2026-10-06 本地候选的路线图与收尾记录。表格中的 `CLOSED`、测试数量和构建结论仅属于当时候选，本次文档整理未重跑这些门禁。当前能力、未完成项和发布状态应以现有源码及最新验证为准。

> **For agentic workers:** REQUIRED SUB-SKILL: Use executing-plans to implement this plan task-by-task.

**Goal:** 保持现有公共契约和多目标框架兼容性，减少文本响应的重复内存复制，补齐流式 JSON 元数据、上传进度和 .NET 8 集成验证。

**Architecture:** 继续通过可选能力接口扩展 `HttpRequest<TResult>`，不向 `IHttpRequest<TResult>` 增加必需成员。先整理响应读取内部职责，再复用现有执行、限额、回调和重试语义。

**Tech Stack:** C#、HttpClient、System.Text.Json、xUnit、Shouldly 和仓库内自定义 .NET 控制台基准程序。生产目标：`net8.0;net7.0;net6.0;netstandard2.0`。

**Historical status:** 2026-10-06 当时的本地验收记录为完成。Task 1 至 Task 6 在该候选中实现并通过对应矩阵；外部 CI 与不可比性能基线按当时边界记录。

## 审计结论

该候选的收尾记录为：已具备任意 HTTP 方法、响应元数据、拦截器、完成观测、安全重试、Retry-After、重试观测、响应限额、流式正文、下载进度、原子文件保存和完整对象流式 JSON。

| 状态 | 当前项 | 处理结果 |
| --- | --- | --- |
| CLOSED | 文本缓冲重复复制、流式 JSON 元数据、上传进度 | 已实现并通过对应测试矩阵 |
| CLOSED | JSON 数组逐项消费 | 已通过 netstandard2.0 兼容实现和枚举生命周期测试 |
| CLOSED | 断点续传下载 | 已通过 206、200、416 和失败状态集成测试 |
| CLOSED | net8.0 集成目标与相关 partial 拆分 | 已完成并通过五目标框架矩阵 |
| CLOSED | 宿主集成示例和命名 Handler 回归 | 本轮补充文档与集成测试 |
| CLOSED | SHA-256 续传校验、显式内存/文件缓存及缓存接入 | 已实现并通过专项测试；默认不自动启用 |
| CLOSED | 性能基线采集、比较和可选回归门禁 | 已提供离线 JSON 入口；缺少历史同候选基线时不启用门禁 |
| BLOCKED_EXTERNAL | 远程 CI、发布环境 API baseline | 需要外部流水线或发布环境 |
| NOT_APPLICABLE | 性能前后门禁 | 当前没有同候选、同负载的旧基线 |
| DEFERRED | 自动缓存接入、分布式缓存一致性、并发分片续传、核心库自建 Resilience/OpenTelemetry | 保持显式调用和架构边界，另立计划 |
不在核心请求对象内自建缓存、熔断、限流、对冲或底层 HTTP span。跨请求策略优先使用宿主 HttpClientFactory、Microsoft.Extensions.Http.Resilience 和运行时遥测。

## Global Constraints

- 保留公开成员、protected 扩展点、异常优先级和资源所有权。
- 不升级依赖、不调整生产目标、不修改版本号。
- 正文限额按实际读取字节执行，不能只信任 Content-Length。
- 保留工作区现有未提交修改，只增量修改相关文件。

## Task 1：修复基线并锁定回归

**Files:**
- Modify: `docs/superpowers/plans/2026-10-05-http-next-enhancements.md`
- Modify: `docs/modules/http.md`
- Modify: `docs/modules/http-optimization-migration.md`
- Test: `tests/Bing.Utils.Http.Tests/Bing/Http/HttpCapabilityExtensionsTest.cs`

- [x] 将重试观测和文本缓冲复用均标为已完成，并链接本轮证据。
- [x] 在实现前修正文档中的错误完成声明。
- [x] 补齐 BOM-only、UTF-16 BOM、charset、零字节、HEAD/204、回调、限额、取消和读取异常回归。
- [x] 运行 net8.0 定向回归并记录本轮证据。

## Task 2：文本响应复用有界缓冲

**Files:**
- Create: `src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.ResponseReading.cs`
- Modify: `src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs`
- Modify: `benchmarks/Bing.Utils.Http.Benchmark/Program.cs`
- Create: `docs/quality/http-response-buffer-benchmark-2026-10-05.md`

- [x] 为 metadata-large、raw-large 记录当前候选基准；完全可比的旧基线不存在，已标记 NOT_APPLICABLE。
- [x] 新增内部 Buffer 读取和文本解码方法，继续复用 `CopyResponseAsync`。
- [x] `GetResponseAsync` 与默认 `SendAfterAsync` 在同一 MemoryStream 上解码。
- [x] 保留两入口空正文转换差异和 BOM-only 行为；byte[] 返回及自定义 OnSendAfter 保持不变。
- [x] 运行回归与优化后基准，报告实际分配、正文大小和耗时。
- [x] 验收后更新文档为已实现。

## Task 3：流式 JSON 响应元数据

**Files:**
- Modify: `src/Bing.Utils/Bing/Http/IHttpRequestCapabilities.cs`
- Modify: `src/Bing.Utils/Bing/Http/HttpTransferCapabilityExtensions.cs`
- Modify: `src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs`
- Test: `tests/Bing.Utils.Http.Tests/Bing/Http/HttpTransferCapabilitiesTest.cs`

- [x] 新增可选流式 JSON 响应结果入口，返回响应元数据快照。
- [x] UTF-8 成功响应直接解析并快照状态和响应头，RawContent 为空。
- [x] 失败、非 UTF-8、正文回调或 string 结果回退现有元数据路径，回调只执行一次。
- [x] 覆盖旧实现、HEAD/204、空正文、错误 JSON、限额、取消和回调组合。

## Task 4：上传进度

**Files:**
- Create: `src/Bing.Utils/Bing/Http/HttpUploadProgress.cs`
- Modify: `src/Bing.Utils/Bing/Http/HttpTransferCapabilityExtensions.cs`
- Create: `src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.UploadProgress.cs`
- Modify: `src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs`
- Test: `tests/Bing.Utils.Http.Tests/Bing/Http/HttpTransferCapabilitiesTest.cs`

- [x] 事件包含 Attempt、BytesWritten、TotalBytes、IsCompleted，表示写入传输流，不表示服务器持久化。
- [x] 包装每次发送的最终 HttpContent，保留内容头和资源所有权。
- [x] 每次重试从零计数；总长度未知时返回 null。
- [x] 覆盖 byte[]、流、ContentFactory、multipart、重试、取消、观察异常和通知阈值。

## Task 5：集成矩阵和维护性拆分

**Files:**
- Modify: `tests/Bing.Utils.Http.Tests.Integration/Bing.Utils.Http.Tests.Integration.csproj`
- Modify: `src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs`

- [x] 增加 net8.0 集成目标和对应条件依赖，保留旧目标。
- [x] 只移动本轮相关私有代码，不移动 public/protected 扩展点。
- [x] 对比拆分前后公共 API 和测试结果，并记录到 docs/quality/http-api-surface-comparison-2026-10-06.md。

## Task 6：验收

- [x] 单元测试运行 net8.0/net7.0/net6.0/net5.0/netcoreapp3.1。
- [x] 集成测试运行同一矩阵；外部网络/网卡用例仍受 RUN_INTEGRATION_TESTS 控制。
- [x] Release 构建四个生产目标，运行基准并保存环境和前后结果。
- [x] 执行 git diff --check、XML 文档和 UTF-8 检查。
- [x] 写回实际证据；只有 OPEN_ACTIONABLE > 0 才进入自动修复。

## 后续独立计划

- JSON 数组逐项消费：已完成兼容实现；后续仅考虑现代框架的额外适配，不改变当前 netstandard2.0 实现。
- 并发分片断点续传：在现有单流续传基础上另行设计分片状态、并发限制、失败恢复和最终合并协议。
- 宿主集成：提供 Microsoft.Extensions.Http.Resilience 与 OpenTelemetry 示例，避免双重重试、超时叠加和重复 span。

## 官方依据

- [DeserializeAsyncEnumerable](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonserializer.deserializeasyncenumerable)
- [.NET HTTP resilience](https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience)
- [.NET networking tracing](https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/telemetry/tracing)


- 宿主集成文档：docs/modules/http-optimization-migration.md，说明命名客户端、Handler Resilience、OpenTelemetry 和双重重试边界。
- 命名 Handler 集成测试：NamedHttpClient_SelectsTheConfiguredHandlerByRequestName。

## 本轮执行证据

- HTTP net8.0 定向测试：流式响应结果 6/6、上传进度 12/12 通过；共享能力回归 197/197 通过，0 失败。
- 性能报告：docs/quality/http-response-buffer-benchmark-2026-10-05.md；当前候选结果已保存，旧版本无可比同构基线，BEFORE_STATUS=NOT_APPLICABLE。
- 缓存专项：内存/文件缓存、跨进程锁、TTL、容量、失效和显式请求接入已覆盖；自动缓存与分布式一致性保持 DEFERRED。
- 四个生产目标 Release 构建通过且无告警；单元测试矩阵与集成测试矩阵均通过；外部 CI 属于 BLOCKED_EXTERNAL，性能前后门禁因无可比基线属于 NOT_APPLICABLE。


## 2026-10-07 Web 全局上下文并发读取验证

- Completed：新增启动期完成 Web.HttpContextAccessor 配置后的并发只读回归，覆盖 HttpContext、Request、Response 和 RequestType，不改变并发覆盖配置仍不受支持的边界。
- Verified：新增测试定向执行 1/1；WebTest 类在 net8.0 执行 99/99；git diff --check 和 UTF-8 读取校验通过。
- CLOSED：模块文档中“全局静态上下文并发读取缺少测试”的本地 TODO。
- TODO（DEFERRED）：并发写入全局静态配置仍不支持；如需运行期多租户或多上下文隔离，应由宿主改用请求作用域服务或显式实例化入口。


## 2026-10-07 HTTP 公开注释与响应头写入收口

- Completed：为重试时间、抖动和等待三个受保护扩展点补充准确 XML 注释。
- Completed：响应缓存头写入改用 IHeaderDictionary 索引器，保持已有 Cache-Control/Pragma 不覆盖语义，消除 ASP0019 警告。
- Completed：新增响应头扩展单元测试，覆盖缺失头设置、无缓存头设置和已有头保护。
- Verified：新增测试 3/3；HTTP 项目自身 net8.0 编译 0 错误且无新增 HTTP 警告；依赖的 Bing.Utils 仍有既有弃用、文档和分析器警告；UTF-8 与 git diff --check 通过。
- CLOSED：本轮公开 API 文档和响应头分析器警告缺口。


## 2026-10-07 显式递归续传侧车清理

- Completed：新增 CleanupRecursiveAsync，按目录树逐层扫描续传侧车文件；每个目标仍在本目录独占 .binghttp.lock，并在锁内重新扫描和删除。
- Completed：保留 CleanupAsync 的默认顶层扫描行为，不启动后台任务，不删除锁文件。
- Verified：续传清理专项测试 9/9；默认顶层行为、嵌套目录清理、锁保留和既有注入文件系统测试均通过。
- CLOSED：递归目录扫描的显式调用能力。
- TODO（DEFERRED）：自动后台清理调度、锁文件回收、并发分片续传和同候选性能基线。


## 2026-10-07 文件响应缓存显式递归维护

- Completed：新增 `FileHttpResponseCache.CleanupRecursiveAsync`，按目录树清理过期 `.cache` 和遗留 `.tmp` 文件；每个目录分别取得容量锁和 key 锁。
- Completed：保留 `CleanupAsync` 顶层扫描默认行为，锁文件和容量锁文件始终保留，不改变后台维护器的显式启动边界。
- Verified：文件缓存专项测试 `27/27` 通过，新增嵌套目录、默认入口隔离及锁保留断言。
- CLOSED：文件响应缓存缺少显式目录树维护入口。
- DEFERRED：跨节点分布式一致性、自动 HTTP 缓存协议语义和锁文件回收仍需独立协议与发布决策。
