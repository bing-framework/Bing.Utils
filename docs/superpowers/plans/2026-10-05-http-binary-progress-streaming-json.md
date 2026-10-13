# HTTP 二进制正文、下载进度与流式 JSON Implementation Plan

> 历史记录：本文是 2026-10-05 候选的执行与验收快照。勾选项、程序集标识、基准数据和测试计数仅属于该历史候选；本次文档整理未重跑代码测试或性能基准。

> **For agentic workers:** Use executing-plans to implement this plan task-by-task. 主代理实现发送、读取与所有权；luna_worker 编写独立测试，文档子代理同步中文契约。

**Goal:** 完成最新 TODO 的三项产品能力及其重放、所有权、进度和解析兼容契约。

**Architecture:** 在 Bing.Utils 的可选能力接口与扩展方法中声明 API；HttpRequest 使用独立 partial 文件实现能力，共用已有发送、重试、清理和完成观测流程。正文限额包装流只用于显式流式 JSON，进度只在下载执行中创建。

**Tech Stack:** 现有 C#/.NET、HttpClient、System.Text.Json；现有全部目标框架。

**Spec:** 本文件的接口与行为契约对应会话最新三项能力 TODO 及三项设计 TODO。

## Global Constraints

- 不新增依赖、目标框架或旧接口必需成员；不提交、不发布。保留现有未提交工作及 UTF-8。
- 所有能力缺失的第三方实现通过扩展方法明确抛 NotSupportedException。
- 真实文件系统及 TestServer 测试属于 HTTP 集成项目；纯 Handler/Stream 测试属于 HTTP 单元项目。
- 本计划将先前三项 DEFERRED 转为本轮 OPEN_ACTIONABLE；仅真实可执行发现可触发修复。

## Task 1: 原始二进制正文及正文工厂

Files: 新增 src/Bing.Utils/Bing/Http/HttpTransferCapabilities.cs 和 HttpTransferCapabilityExtensions.cs；新增 src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.Transfer.cs；修改 HttpRequest.cs。

API：IHttpRequestRawContentRegistration<TResult> 的 SetBinaryContent(byte[], string)、SetBinaryContent(Stream, string, bool)、SetContentFactory(Func<HttpContent>)；链式扩展 BinaryContent(byte[], contentType="application/octet-stream")、BinaryContent(Stream, contentType="application/octet-stream", leaveOpen=false)、ContentFactory(Func<HttpContent>)。

- [x] 注册时验证参数；字节数组防御复制。二进制与对象/参数/multipart 正文混用明确拒绝，后配置冲突在创建正文时同样拒绝。
- [x] 每次尝试创建新 HttpContent；流保存注册时 Position，可定位流每次回到该位置；不可定位流在实际允许重试的方法上发送前拒绝。默认拥有流，leaveOpen 保留流；替换配置时释放先前拥有的流，执行成功、拒绝、取消、准备或发送失败都清理。
- [x] 工厂每次尝试调用，返回非空、独立正文；同一执行内重复返回实例发送前拒绝。工厂正文随请求释放，异常不掩盖清理；实际方法允许名单及拦截器正文替换限制保持。
- [x] 单元覆盖 PUT 非 multipart 原样发送、源数组修改、起始位置重放、非定位拒绝、工厂重试/复用拒绝、所有权和失败；具体旧实现缺能力明确失败。

实现要点：CreateHttpContent 首先调用可选正文创建器；DisposeUploadStreams 统一纳入拥有的二进制流；重试资格按现有实际方法检查；工厂历史仅在该次执行分配。

## Task 2: 下载进度

Files: 同上可选接口/扩展与 partial；新增 src/Bing.Utils/Bing/Http/HttpDownloadProgress.cs；修改 CopyResponseAsync 和 DownloadToDetailedAsync。

API：IHttpRequestDownloadProgressRegistration<TResult>.SetDownloadProgressObserver(Action<HttpDownloadProgress>, long minimumBytes, TimeSpan minimumInterval)；ObserveDownloadProgress(observer, minimumBytes=65536, TimeSpan? minimumInterval=null)，默认间隔 100ms。信息为 BytesWritten、可空 TotalBytes 和 IsCompleted。

- [x] 每次下载固定注册快照；仅成功响应发初始和最终通知，中间同时满足字节与时间阈值才通知。未知长度为 null，HEAD/204 总量为 0；实际成功写入后才增加字节数。
- [x] IsCompleted 仅表示正文达到 EOF，文件保存状态由下载结果提供；EOF 前异常/取消无成功终止通知，EOF 后文件保存失败或取消则可能已经通知完成。进度回调异常传播并终止下载，清理仍执行；不向进度附加 URL、头或正文。
- [x] 不启用时不创建进度对象或计时器；普通正文读取及 HTTP 错误正文读取不通知。新旧下载及文件下载共用逻辑，不重复通知。
- [x] 单元覆盖已知/未知长度、限频、回调异常、零正文、失败/取消；集成覆盖进度完成后文件提交失败/取消时的区别。

## Task 3: 可选流式 JSON

Files: 可选接口/扩展与 partial；新增 src/Bing.Utils.Http/Bing/Http/Clients/Internal/ResponseBodyReadStream.cs。

API：IHttpRequestStreamingJson<TResult>.GetJsonStreamAsync(CancellationToken)；同名扩展返回 Task<TResult>。

- [x] 显式入口拒绝 OnSendAfter；UTF-8 application/json 或 +json、无 OnConvert/正文回调、非 string 结果使用 JsonSerializer.DeserializeAsync 和现有选项，从源流直接解析，不构建完整字节数组和字符串。
- [x] 非 UTF-8、OnConvert、OnFail/OnComplete、string 结果及非成功响应回退现有 SendAfterAsync，保留编码、原文及回调语义；原文保留仍用既有 GetResponseAsync。非 JSON 成功响应也沿用现有转换语义，不额外猜测格式。
- [x] 直接解析时 HEAD/204/空正文返回 null，成功回调执行一次；UTF-8 BOM 支持，空白正文按现有 JSON 转换语义处理。回退模式保留 string 空字符串和 OnConvert 自定义空正文语义。包装读取流检查已知长度与实际字节、取消和不可修改/不可定位行为，解析、读取、回调异常保留并清理。
- [x] 单元覆盖 JSON/+json、自定义选项、空正文、BOM、非 UTF-8 回退、转换/正文回调、HTTP 失败、限额/未知长度、取消、解析失败、释放与一次完成观测；集成覆盖本地 JSON 链路。

## Task 4: 验证与交付

- [x] net8.0 新能力与弹性精准测试，现有 HTTP 单元五框架、HTTP 集成四框架矩阵；生产项目全部现有框架 Release 构建、diff 和严格 UTF-8 检查。
- [x] 对普通 JSON、流式 JSON、大 JSON 及进度路径做同负载小型分配测量，记录负载/程序集/运行时及局限；不新增门槛，不宣称优于 SKIT。
- [x] 更新模块、迁移契约、示例和本计划证据；将旧计划三项延期链接到此计划。

Change Impact Analysis：变更 Bing.Utils 可选 API 与 Bing.Utils.Http 发送/下载/解析路径、专属测试和文档；无 Provider、框架、包或构建图变化。风险 MEDIUM，受影响依赖为所有 HTTP 消费方。L1 后按公共 API 和阶段收尾扩大到 HTTP 矩阵；不扩展到其他模块。

完成审计：三项能力、三项设计契约、兼容测试、资源/异常/取消验证、矩阵、构建、测量与文档逐项有当前证据后才完成 Goal。

## 最终检查后的增量收尾

- [x] 修正参数验证用例的三次进度通知断言，以及文件集成测试的显式 UTF-8 读取。
- [x] 统一流式 JSON 返回值 XML 契约，补分段 BOM、跨缓冲空白、恰好命中限额及空正文回退测试。
- [x] 说明二进制流的重复执行条件；完成精准测试、受影响矩阵、构建与编码检查，再关闭全部清单。

影响分析：本次只变更专属测试、XML 注释和文档，不改变生产执行路径、依赖、目标框架或性能基准。风险 LOW；先执行 net8.0 能力与弹性精准集合，再执行计划要求的 HTTP 矩阵及 Release 构建。已有同负载分配测量复用，不因注释或测试修改重测。


## 最终验收证据（2026-10-05）

当前生产实现已包含原始二进制正文与逐次正文工厂、下载进度、显式流式 JSON 三项能力。原计划 15/15 项与本次增量收尾 3/3 项均完成。上一检查发现的参数验证用例误断言已修正；此后新增边界与所有现有 HTTP 测试全部通过，不将旧候选成绩作为本次最终成绩。

| 要求 | 实现与权威验证 |
|---|---|
| 二进制及工厂 API、参数、混用拒绝、数组复制 | HttpTransferCapabilities.cs / HttpTransferCapabilityExtensions.cs / HttpRequest.Transfer.cs；HttpTransferCapabilitiesTest 的原始 PUT、空数组、非法注册保持原配置、结构化正文冲突用例 |
| 重放、实际方法策略、流及正文所有权 | CreateRawHttpContent / ExecuteCoreAsync / DisposeUploadStreams；注册位置与 leaveOpen、替换/拒绝/取消/准备失败、非定位拒绝、工厂逐次新实例/复用/异常用例；既有拦截器替换与安全方法回归 |
| 下载初始、中间及 EOF 进度、快照与零额外状态 | CopyResponseAsync / DownloadProgressState；已知/未知长度、两个阈值、零间隔、注册快照、HEAD/204、HTTP 失败、取消及回调异常用例；未配置进度不创建状态 |
| EOF 与最终文件提交区别 | HttpTransferCapabilitiesIntegrationTest：EOF 后提交失败、响应清理后取消、原目标与临时文件保护、一次最终观测；文件提交仍由共同 DownloadFileCoreAsync 管线完成 |
| 直接 JSON、选项、编码与正文回调回退 | CanStreamJson / ReadJsonStreamAsync / ResponseBodyReadStream；JSON/+json、配置选项、非 UTF-8、OnConvert/正文回调/string/HTTP 失败、HEAD/204/空白、解析与成功回调异常用例 |
| 流式读取边界及限额 | 新增单字节分段 BOM、跨 4096 前缀缓冲与多字节边界的 Unicode 全空白；已知/未知长度 8011 字节 JSON 恰好限额成功、少一字节失败并释放；已有取消、未知长度超限与一次观测用例 |
| 第三方兼容、命名客户端及旧下载回归 | HttpCapabilityExtensionsTest 的具体 LegacyHttpRequest；新可选接口缺失抛 NotSupportedException；现有命名工厂、流式文件下载及响应/拦截器测试进入最终矩阵 |
| 中文契约、迁移与可用示例 | http.md 第 14 节与 http-optimization-migration.md；说明 EOF、JSON 回退、同步进度异常、二进制流重复执行与所有权；接口返回值 XML 与实际回退规则一致 |

### 当前验证记录

- L1：net8.0 能力、传输与弹性精准集合 152/152，0 失败、0 跳过；本次补充 5 个方法、7 个执行用例。
- L2：HTTP 单元 net8.0/net7.0/net6.0/net5.0/netcoreapp3.1 各 1648/1648，0 失败、0 跳过。
- L3：HTTP 集成 net7.0/net6.0/net5.0/netcoreapp3.1 各 65/65，0 失败、0 跳过。
- L0：dotnet build src/Bing.Utils.Http/Bing.Utils.Http.csproj -c Release --no-restore -v quiet 成功，覆盖生产 net8.0/net7.0/net6.0/netstandard2.0 与所引用核心项目；0 错误、57 个既有核心警告。测试编译仍有既有 xUnit2020/xUnit1041 提示。
- L0：git diff --check 通过；全部 41 个当前改动及未跟踪文本文件严格 UTF-8、替换字符和新增文件尾部空白检查通过。
- L5：复用[性能报告的当前能力测量](../../quality/http-optimization-benchmark-2026-10-04.md#二进制正文下载进度与流式-json2026-10-05)。报告包含本地 Windows/.NET 8.0.30/Release 的 6 种同负载模式、每模式 3 轮、负载与测量程序集哈希；本次只修改测试、注释和文档，没有运行路径变化，不重测。直接大 JSON 的分配中位数为 204.35 MiB/100 次，缓冲同负载为 1190.89 MiB/100 次；不代表网络吞吐，不与 SKIT 比较。
- 复现精准测试：dotnet test tests/Bing.Utils.Http.Tests/Bing.Utils.Http.Tests.csproj -c Release -f net8.0 --no-restore --filter 'FullyQualifiedName~HttpTransferCapabilitiesTest|FullyQualifiedName~HttpCapabilityExtensionsTest|FullyQualifiedName~HttpRequestResilienceTest' -v quiet。
- 矩阵命令：分别对上述各框架执行 dotnet test tests/Bing.Utils.Http.Tests/Bing.Utils.Http.Tests.csproj -c Release -f <TFM> --no-restore -v quiet，以及集成项目同等命令。

### 完成状态与边界

- Completed：计划 18/18，当前 5 项必修 TODO 与两项建议均关闭；Implementation/Test/Performance Evidence 均 PASS。必需本地门禁 6/6（精准、单元矩阵、集成矩阵、生产构建、静态编码/差异、同负载测量）。
- Open Actionable / Blocked Approval / Blocked External：0。没有未实现的本计划能力。发布不属于本任务；未提交、未发布，不新增依赖、框架或旧接口必需成员。
- Accepted Limitations：EOF 进度不等于文件保存；同步提交不可中断且检查与提交之间存在取消竞态；JSON 不满足流式条件时缓冲回退；默认拥有的流执行后释放，顺序重复执行需重新配置或采用保留流/正文工厂，请求实例不支持并发修改。
- Not Applicable：无等价历史 API 的三项新增能力不建立虚构 before；本任务不要求 SKIT 同负载比较或性能门槛。
- Verified Boundaries：指定正文超限正确拒绝、资源清理和取消行为由当前精准用例验证。
- Deferred：完整 SDK、缓存、熔断、限流、双 JSON 引擎仍属独立产品需求，不是本计划未完事项。外部 CI 与真实网络吞吐不是本地验收门禁。
- No-Progress Check：CHANGED；本次修复测试与契约并取得当前矩阵证据。Next Action：STOP；Goal Status：COMPLETED。

## 文档说明补充（2026-10-05）

- [x] 模块与迁移文档说明流式 JSON 返回完整对象，大型数组仍整体构造；正文限额不等于对象图内存限额，当前接口不提供逐项异步枚举或恒定内存保证。
- [x] 模块与迁移文档说明下载进度同步执行且不保证 UI 线程；调用方负责调度，并处理异步投递后的异常及更新时序。

本次两项审查 TODO 均为文档补充，只修改上述两份说明与此记录。已对照 DeserializeAsync<TResult> 和直接进度委托调用核对契约；严格 UTF-8 与 git diff --check 通过。生产行为未变化，复用前述测试与性能证据。
