# Http 安全与可靠性修复报告

> 2026-10-15 后续进度：质量报告 P1-1 上传补测、覆盖计划 P2-1 失败链路均已完成，当前 net6/net8 集成各 65/65 通过；质量报告 P1-2 的 nightly 实际结果待验证。最新证据见文末「后续 TODO 验收」及 [TODO 执行记录](todo-execution.md)。原修复批次的证据和边界保留如下。

## 当前结果

已完成批准计划的 8 类修复，现有环境验证通过。基线提交：`b87b706126eaa1f63001e2e89ca1a814b52b4b48`；候选为本次工作区改动，未提交、未推送、未修改依赖或版本。

| 问题 | 状态 | 实现与证据 |
|---|---|---|
| 共享 TLS/客户端证书/Cookie 配置污染 | CLOSED | 新增 UseBingRequestIsolation；10 项本地隔离集成用例覆盖真实 TLS、并发 mTLS、串行与并发自动 Cookie 隔离及重试 |
| 失败下载破坏原文件 | CLOSED | 临时文件写完后移动/替换；HTTP 失败、拒绝、取消、占用失败保留原文件 |
| Session 泛型写入递归 | CLOSED | 使用框架 SetString，5 项 Session 用例 |
| 请求、响应与传输资源释放 | CLOSED | 外围 using/构造异常清理、独立传输上下文；共享处理器创建与释放加锁 |
| 共享客户端超时与基地址竞争 | CLOSED | 独立取消令牌与请求 URI 合并，客户端属性保持不变 |
| Cookie 分隔符与请求头大小写 | CLOSED | Cookie 使用分号；请求头大小写不敏感覆盖 |
| 上传文件缺失导致静默漏传 | CLOSED | 缺失文件、空白/null 路径、null/不可读/已关闭流均明确异常且不发送；空文件/空流仍可上传 |
| JSON 媒体类型子串误判 | CLOSED | 严格解析媒体类型，保留合法类型和 Accept 回退 |

资源回归用例还覆盖工厂客户端与注入客户端的所有权、证书加载失败、释放后的路由器拒绝请求。最终代码经主代理检查，并由 Luna 对释放路径做独立只读检查；发现的共享处理器创建/释放竞态已修正。

## 影响分析

- ChangedFiles/Projects：Http 客户端、隔离注册与内部传输、Session/请求扩展、Http 两个测试项目、模块文档。
- ChangedPublicContracts：新增注册扩展的两个重载；现有接口签名不变。显式传输覆盖要求注册隔离能力；回调后的响应由模块释放；失败 WriteAsync 明确抛异常。
- ChangedRuntimePaths：请求构建/发送/响应、TLS/mTLS/Cookie、超时取消、上传、下载文件提交、Session 和媒体类型判断。
- ChangedProviders：IHttpClientFactory 与直接注入 HttpClient 路径。AffectedDependents：Http 单元/集成消费者。
- ChangedTFMs/BuildPackaging/BenchmarkHarness：无目标框架、构建打包、基准框架变更。ChangedDocsOnly：否。RiskLevel：HIGH。

## 验证

生产项目 Release 构建通过：`net8.0;net7.0;net6.0;netstandard2.0`，0 错误。以下为上一阶段完整测试证据，均无失败或跳过；本轮仅复用其中未受影响范围，上传及并发 Cookie 以随后列出的新证据为准：

| 验证 | 结果 | 本地 TRX 文件 |
|---|---|---|
| net8.0 单元全量 | 1523/1523 通过 | `http-unit-full-net8.trx` |
| net7.0 集成全量 | 59/59 通过 | `http-integration-final-net7.trx` |
| net7.0 单元定向 | 57/57 通过 | `http-unit-compat-net7.trx` |
| net6.0 单元定向 | 57/57 通过 | `http-unit-compat-net6.trx` |
| net6.0 集成定向 | 19/19 通过 | `http-integration-final-net6.trx` |

证据目录：`TestResults/http-reliability/`（构建产物，不纳入源码提交）。初次诊断产生的失败 TRX 仅保留为历史，不属于上表最终证据。

执行入口：

```powershell
dotnet build src/Bing.Utils.Http/Bing.Utils.Http.csproj -c Release
dotnet test tests/Bing.Utils.Http.Tests/Bing.Utils.Http.Tests.csproj -c Release -f net8.0
dotnet test tests/Bing.Utils.Http.Tests.Integration/Bing.Utils.Http.Tests.Integration.csproj -c Release -f net7.0
git diff --check
```

兼容单元过滤器：`FullyQualifiedName~HttpRequestReliabilityTest|FullyQualifiedName~SessionExtensionsTest|FullyQualifiedName~HttpRequestExtensionsTest`，分别在 net6.0/net7.0 执行。net6.0 集成过滤器覆盖隔离、文件可靠性以及现有超时/取消用例。

L0：多目标编译、diff 与 UTF-8 检查。L1：精准回归通过。L2：1523 项单元全量通过。L3：59 项集成全量及兼容回归通过。L4/L5：按批准范围不执行全解决方案测试或性能基准，不声明性能提升。

## 审查漏项修复（2026-09-25）

上轮将上传缺失和并发 Cookie 验收提前标为全部关闭。本轮针对两个明确漏项完成修正，并以新增用例验证，不将旧 TRX 当作新用例证据。

| Finding | 修复及验收标准 | 状态 |
|---|---|---|
| HTTP-FIX-01 上传无效输入被静默跳过 | 空白/null 路径和 null 流明确拒绝；构建时拒绝不可读/已关闭流；已有合法附件被消费后遇到无效流仍不发送请求；有效空流/空文件保持合法 | CLOSED |
| HTTP-FIX-02 缺少并发自动 Cookie 证据 | A/B 请求各自接收同名不同值 Cookie，经中间重定向屏障确认都已设置，再读取并分别断言 A/B；屏障超时受控 | CLOSED |

本轮影响：生产代码仅改变上传参数和读取前校验；公开签名、依赖、目标框架与传输隔离实现均未变化。单元测试归属 Http.Tests，真实文件及本地 HTTP 验证归属 Http.Tests.Integration。风险级别 MEDIUM。L0 多目标 Release 构建通过（0 错误、3 警告）；L1/L3 如下，均无跳过：

| 新验证 | 结果 | TRX |
|---|---|---|
| net8.0 请求可靠性单元 | 29/29 | `http-review-fix-unit-net8.trx` |
| net7.0 请求可靠性单元 | 29/29 | `http-review-fix-unit-net7.trx` |
| net6.0 请求可靠性单元 | 29/29 | `http-review-fix-unit-net6.trx` |
| net7.0 隔离、下载及上传集成 | 24/24 | `http-review-fix-integration-net7.trx` |
| net6.0 隔离、下载及上传集成 | 24/24 | `http-review-fix-integration-net6.trx` |

独立只读复核已核对源码及上述 TRX 中新增用例的 Passed 状态，两项 finding 均 CLOSED。

单元过滤器：`FullyQualifiedName~HttpRequestReliabilityTest`。集成过滤器：`FullyQualifiedName~HttpRequestIsolationIntegrationTest|FullyQualifiedName~HttpDownloadReliabilityTest|FullyQualifiedName~FileContent`。证据目录同上。本轮未重复执行全量单元/集成；未受影响的 Session、媒体类型等结果按范围复用上轮证据。没有新增性能或发布要求。

## 兼容性与边界

迁移说明见 [Http 模块文档](../modules/http.md#请求级安全隔离与可靠性契约)。请求级隔离需增加一次 `AddHttpClient(...).UseBingRequestIsolation(...)` 注册；隔离客户端通过 Bing HttpRequest 使用，不直接调用 HttpClient.SendAsync。自动 Cookie 仅在同次执行的重定向/重试内保留。未接入的普通客户端沿用其原有传输配置。

Windows Schannel 和 netstandard2.0 使用 DefaultKeySet 兼容加载请求证书，其他现代平台使用 EphemeralKeySet；不设置 PersistKeySet，证书对象由本次传输释放。真实 TLS 与文件占用用例已在当前 Windows 环境验证，未将此结果表述为其他操作系统的实测结果。

## 执行状态与 TODO

- Task：http-security-reliability；Round：审查漏项修复及精准兼容回归。
- Provider/model：Codex 主代理；Luna 负责独立辅助修复、测试及释放路径检查。
- Implementation：8/8。验证门禁：5/6（构建、单元全量、集成全量、兼容回归、静态检查通过；旧运行时门禁阻塞）。
- Completed：原 8 类修复、两项审查漏项修正、迁移说明及本轮精准回归。
- Open Actionable：none。
- Blocked Approval：none。
- Blocked External：本机缺少 .NET 5/3.1 运行时，相关测试未运行；netstandard2.0 仅声明编译通过。
- Not Applicable：无批准的性能阈值或基准任务，不执行重型基准。
- Accepted Limitations：请求构建器不支持同实例并发修改；响应完整缓冲；隔离自动 Cookie 不保留跨请求会话；文件替换需要目标文件系统支持。
- Verified Boundaries：none。
- Deferred：按计划暂不更改 Token 旧格式兼容、通用 Cookie 默认属性和代理头信任策略，不纳入本次修复验收；后续可单独开展认证与代理信任边界审计。
- Reused Evidence：上轮全量结果仅复用未受影响范围；上传与并发 Cookie 使用本轮新 TRX。只改报告不重复运行测试。
- No-Progress Check：CHANGED；Implementation Status：PASS；现代运行时 Test Status：PASS；旧运行时 Gate：BLOCKED_EXTERNAL。
- Next Action：STOP。Goal Status：STOPPED_BLOCKED（仅旧运行时验证，未进行发布）。

## 后续 TODO 验收（2026-10-15）

此前按 [测试质量报告](test-quality-report.md) P1 原顺序完成本地实现，本轮接续 [覆盖计划](test-coverage-map.md) P2-1：

| 条目 | 实现 | 状态 |
| --- | --- | --- |
| P1-1 上传集成补测 | `HttpUploadReliabilityTest` 验证无目录文件名、不同字段下的重复文件名、两个独立请求同时到达时的同名文件内容隔离；均核对实际落盘字节 | CLOSED |
| P1-2 net6/net8 持续验收 | 集成项目增加 net8 目标，复用既有依赖版本；既有 `.github/workflows/dotnet.yml` 增加每日 net6/net8 完整集成测试入口 | PARTIAL；本地实现完成，实际 nightly 结果待验证 |
| 覆盖 P2-1 HTTP 失败链路 | 共享测试宿主捕获受控异常并返回既有 ErrorResponse；补强完整错误体及回调契约断言 | CLOSED；本轮完成 |

`Test6Controller` 改为从当前请求异步读取表单，以每文件独立临时目录保存并在 `finally` 清理。旧上传路由响应格式保持不变，新增 `inspect` 仅用于测试回执。生产 HTTP、I/O 实现及公开 API 未变化。`FileContent` 原本按字段名替换附件，因此同一请求中的重复文件名用例使用不同字段名，不引入同字段多附件的新契约。

上传批次的 net6/net8 默认完整集成套件各 64/64 通过，证据为 `TestResults/todo-http-upload/http-full-net6.trx`、`http-full-net8.trx`。其中初次 `http-upload-net7.trx` 含一项与既有字段替换契约不符的额外断言失败，仅留作诊断记录；该额外断言已移除，原计划三类场景全部保留。本轮共享宿主变更后，以以下新证据为当前结果。

P2-1 的实现只位于集成项目：`Startup.ConfigureHost` 注册测试用 `UseExceptionHandler`，复用 MVC JSON 配置与 `Test7Controller.ErrorResponse`；`Test7Controller.Throw` 提供受控异常路由；`HttpClientServiceTest.Pipeline` 补齐断言。没有新增生产异常协议或更改客户端行为。

| 原验收场景 | 测试函数及断言 |
| --- | --- |
| 非 2xx | `Test_Get_OnFail_For4xxAnd5xx_1` 验证 400/500、完整 code/message、空结果及不调用成功回调 |
| 超时 | `Test_Get_Timeout_ThrowsOperationCanceledException_1` 验证取消异常且不调用响应失败/完成回调 |
| 取消令牌 | `Test_Get_CancellationToken_ThrowsOperationCanceledException_1` 验证取消异常且不调用响应失败/完成回调 |
| 异常中间件返回体一致性 | `Test_Get_Failure_ExceptionMiddleware_PreservesErrorBody_1` 比较异常路由与显式 500 的完整响应体；`AssertFailureResponseAsync` 验证失败/完成回调体相同，且按顺序各执行一次 |

发送阶段超时/取消没有 HTTP 响应体，遵循 `HttpRequest.GetResultAsync/SendAfterAsync` 既有行为，不把取消异常伪装成统一 JSON 错误体。

当前验证：net6.0、net8.0 Release 编译通过；net8.0 定向 5/5，两个框架默认完整集成套件各 65/65，无失败、无跳过。构建仍有既有警告。最终证据位于 `TestResults/todo-http-failures/`：`http-failure-targeted-net8.trx`、`http-failure-full-net8-authorized.trx`、`http-failure-full-net6.trx`，定向用例属于完整套件的子集。

沙箱内诊断 `http-failure-full-net8.trx` 的 11 项失败均为证书导入或文件替换权限问题；同一测试产物经权限流程复核后 65/65 通过。失败证据保留，未跳过测试或修改业务实现。本轮未改工作流、依赖、基线及生产代码，Luna 独立只读复核未发现可行动问题。

本地复现命令（每个框架各执行一次）：

```powershell
dotnet test tests/Bing.Utils.Http.Tests.Integration/Bing.Utils.Http.Tests.Integration.csproj -c Release -f net6.0 -p:TargetFrameworks=net6.0
dotnet test tests/Bing.Utils.Http.Tests.Integration/Bing.Utils.Http.Tests.Integration.csproj -c Release -f net8.0 -p:TargetFrameworks=net8.0
```

本机因现有 `obj` 写权限及共享产物问题，按权限流程执行，并加 `-m:1 -nr:false -p:UseSharedCompilation=false -p:BuildInParallel=false` 串行构建；未改项目构建配置或禁用测试。

nightly 仅复用既有 verify job 的 Windows 执行器，顺序测试两个框架，不新增 workflow、job、矩阵组合或基线，不执行 publish。验证 SDK 固定为仓库 `global.json` 指定的 8.0.425。未启用 `RUN_INTEGRATION_TESTS` 的外部网络/主机配置用例，不将本地 Windows 结果表述为其他平台或实际 CI 已通过。
