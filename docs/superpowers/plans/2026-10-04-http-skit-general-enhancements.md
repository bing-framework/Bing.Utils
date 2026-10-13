# HTTP 通用能力增强：SKIT 对比增量收尾

> 历史记录：本文保留当时与 SKIT 的对比、实现决策及候选验证数据。文中“已验证”和测试数量不是当前工作树的新鲜证据，也不覆盖后续实现状态。

## 1. 对比结论与范围

参考 SKIT 固定提交 `dbe4826ac6db60135a2cf27be49f1dc3c89d1c60`。本文件记录既有增强及本轮增量收尾：补齐契约测试、兼容说明和验证证据。

## 计划收尾时状态

- 已验证实现：通用请求方法及 PATCH/HEAD/OPTIONS、完整响应结果、异步拦截器与可选能力接口。
- 前阶段安全与弹性能力已有实现记录；其接口迁移要求见 [HTTP 优化迁移说明](../../modules/http-optimization-migration.md)。
- 本阶段不再向既有 `IHttpClient` 或 `IHttpRequest<TResult>` 增加必需成员；新增功能由 `IHttpRequestFactory`、`IHttpRequestResponse<TResult>`、`IHttpRequestInterceptorRegistration<TResult>` 提供。
- 没有两库同负载性能证据，不宣称性能优于 SKIT。

| 对比点 | SKIT 的做法 | 改动前 Bing 情况 | 本轮决策 |
|---|---|---|---|
| 响应信息 | 响应基类提供状态码、响应头、原始字节和业务成功判定 | 普通请求主要返回 DTO 或 `null`，响应元数据依赖回调 | 新增独立响应结果对象 |
| 拦截器 | 多个异步拦截器，请求顺序执行、响应逆序执行 | 单个发送前回调为同步，重复注册覆盖 | 新增可组合异步拦截器 |
| 请求方法 | 支持指定任意 HTTP 方法 | 服务入口只有 GET、POST、PUT、DELETE | 补通用入口及 PATCH、HEAD、OPTIONS |
| 序列化 | 可配置 JSON、表单序列化器及多种转换器 | 已有 JSON 配置、数字及日期转换能力 | 复用现有能力，暂不增加序列化框架 |
| 客户端复用 | 支持注入 `HttpClient`、工厂集成 | 已支持命名客户端和工厂复用 | 沿用现有机制 |

这些增强解决功能与使用体验差异；没有两库同负载测试，不宣称性能优于 SKIT。

## 2. 实现内容与公共契约

### 完整响应结果

- 新增 `HttpResponseResult<TResult>`，包含状态码、成功标记、响应头、内容头、转换后的数据和可选原文。
- 响应头采用不区分大小写、保留多值的独立只读快照；请求结束后无需持有或释放响应对象。
- 新增可选 `IHttpRequestResponse<TResult>.GetResponseAsync(bool includeRawContent = false, CancellationToken cancellationToken = default)` 能力。
- HTTP 非成功状态返回结果对象，数据为 null；开启原文保留后可读取错误正文。网络异常、取消、解析错误和超限继续抛异常。
- HEAD、204 和空正文返回 null 数据；启用原文保留时原文为空字符串。默认不保留原文。
- 正文只读取一次，共用现有限额、编码和转换逻辑。现有结果、下载路径及回调行为保持原契约。
- 新响应入口继续使用转换、成功/失败及完成回调；与自定义发送后回调同时配置时，在发送前明确拒绝。

### 异步签名与观测拦截器

- 新增 `HttpRequestInterceptor`，提供可重写的异步发送前、发送后方法。
- 通过 `IHttpRequestInterceptorRegistration<TResult>.AddInterceptor(HttpRequestInterceptor interceptor)` 追加实例；每次执行开始时快照注册列表。
- 上下文提供请求、从 1 开始的尝试次数、请求级 Items；后置阶段提供状态码、响应头快照、发送耗时和发送异常，不暴露响应正文流。
- 每次重试都重新执行前置拦截器。顺序为构建消息、既有发送前回调、前置拦截器顺序执行、重放安全检查、发送、后置拦截器逆序执行、重试判断。
- 既有发送前回调拒绝发送时不执行拦截器。前置链中途失败时，对已成功进入的拦截器逆序执行后置操作。
- 后置阶段在发送成功、发送异常和取消时均执行；传入取消令牌。已有发送异常优先传播；没有原始异常时传播后置异常。拦截器异常不触发自动重试。
- 请求方法、正文替换继续服从现有重试安全检查。最终转换及业务回调只处理最终响应。
- 每个前置拦截器替换请求正文后，立即释放被替换的正文；链式替换及前置异常时也释放中间正文。请求结束时由请求消息释放最终正文。
- 不内置请求正文日志或敏感头日志；示例只记录方法、去除查询参数的地址、状态、尝试次数和耗时。

### 请求方法与兼容扩展

- 新增可选 `IHttpRequestFactory.CreateRequest<TResult>(HttpMethod method, string url)` 能力，由 `HttpClientService` 实现。
- 为既有客户端提供通用请求、PATCH、HEAD、OPTIONS 扩展入口，统一创建请求。
- PATCH 默认仍不重试；HEAD、OPTIONS 沿用已有安全方法重试策略。
- 本阶段不向既有客户端及请求接口增加必需成员。前阶段 `IHttpRequest<TResult>` 的必需成员迁移要求见迁移说明；本阶段新增能力由独立接口提供，缺少能力时明确抛出 `NotSupportedException`。

## 前轮验证结果

- [x] 新能力及扩展精准测试：`net8.0` 共 56/56，覆盖 JSON/`OnConvert`、原文开关、字符集、头快照、限额/取消/回调异常后的释放、HEAD/204、拦截器链式正文替换、逐次签名、异常优先级、方法重试策略及短/缺失 User-Agent。
- [x] 具体 `IHttpClient` 与 `IHttpRequest<TResult>` 旧接口实现均可编译；缺少可选能力时明确抛出 `NotSupportedException`。
- [x] TestServer 精准测试在全部 4 个集成框架各 3/3，通过 PATCH 方法、内容类型与 JSON 正文、HEAD/OPTIONS 和错误响应。
- [x] HTTP 单元矩阵：`netcoreapp3.1/net5.0/net6.0/net7.0/net8.0` 各 1573/1573；集成矩阵：`netcoreapp3.1/net5.0/net6.0/net7.0` 各 52/52。本轮重新运行全部矩阵。
- [x] 生产项目全部目标框架 Release 构建成功，0 错误；`git diff --check` 通过。
- [x] 前置拦截器的正文释放路径已由回归测试覆盖；已测量正常、不替换正文的拦截器开销，以及完整响应原文开关。本轮单次头快照复制后复测 `metadata`/`raw`/`json`，各自分配中位数为 6.66/6.66/6.19 MiB（1000 次）；此前候选的 `metadata`/`raw` 为 7.10 MiB。各候选程序集版本及原始样本见[HTTP 优化性能记录](../../quality/http-optimization-benchmark-2026-10-04.md)。基准不测量正文替换路径，也没有与 SKIT 同负载的对比。

## 后续契约收尾

- [x] `OnSendBefore(false)` 的完整响应入口返回 null，且跳过响应回调；补充回归测试和 API 说明。
- [x] 响应处理与 `OnComplete` 同时失败时保留原始处理异常；完成回调单独失败仍传播其异常。
- [x] `HttpRequestInterceptorContext.Elapsed` 明确只计发送阶段，不包含拦截器前后处理或正文转换。
- [x] 完整响应结果的内部路径只复制一次头快照；公开构造函数继续防御性复制调用方字典。
- [x] 短或缺失 User-Agent 返回 false，并补齐边界测试与迁移说明。

## 本轮 TODO 收尾

- [x] 冻结实际发送方法的重试资格，防止后置拦截器修改请求消息后放开写请求的自动重试；覆盖响应状态和发送异常。
- [x] 字节读取与下载处理异常和 `OnComplete` 同时失败时保留原异常；完成回调单独失败仍由现有处理逻辑传播。
- [x] 少于四字符的 User-Agent 即使匹配移动端正则也返回 false，并补充反例。
- [x] 公开 `HttpResponseResult` 构造函数验证调用方原始字典和值数组后续修改不影响快照。
- [x] 签名示例使用绝对地址，并说明相对地址解析和自动重定向时的签名边界。

上节测试与性能数据为前一候选的验收证据。本轮 `net8.0` 精准筛选 `HttpCapabilityExtensionsTest|HttpRequestExtensionsTest` 为 66/66；HTTP 单元测试五个目标框架各 1583/1583，集成测试四个目标框架各 52/52。生产项目全部目标框架 Release 构建成功，0 警告、0 错误。发送与读取路径的本地基准复测记录在[HTTP 优化性能记录](../../quality/http-optimization-benchmark-2026-10-04.md)，按其候选程序集哈希与前轮区分。

## 实现与证据边界

### HEAD 跨入口与签名边界收尾

- [x] 公共读取层对 HEAD/204 在长度检查前跳过正文；普通结果、字节及下载入口验证空结果和完成回调。普通结果的成功回调收到 null；响应由既有请求管线释放。
- [x] 请求级 `BaseAddress` 签名地址与实际发送地址一致；仅注入客户端基地址时，前置拦截器看到相对 URI。真实本地 Kestrel 自动重定向验证只在原始请求运行一次签名拦截器。

本轮 `net8.0` HEAD/签名精准单测 50/50，真实重定向集成精准测试 1/1；HTTP 单元测试 `netcoreapp3.1/net5.0/net6.0/net7.0/net8.0` 各 1591/1591，集成测试 `netcoreapp3.1/net5.0/net6.0/net7.0` 各 53/53。生产项目全部现有目标框架 Release 构建成功。正文读取基准的候选程序集身份和样本见[HTTP 优化性能记录](../../quality/http-optimization-benchmark-2026-10-04.md)。上文 66/66、1583/1583 和 52/52 属于前一候选。

测试证据位于 `tests/Bing.Utils.Http.Tests/Bing/Http/HttpCapabilityExtensionsTest.cs`、`tests/Bing.Utils.Http.Tests/Bing/Http/HttpRequestResilienceTest.cs`、`tests/Bing.Utils.Http.Tests.Integration/Http/HttpCapabilityExtensionsIntegrationTest.cs` 和 `tests/Bing.Utils.Http.Tests.Integration/Http/HttpRequestFactoryAndDownloadIntegrationTest.cs`。本轮修复拦截器正文所有权并补齐测试和文档；外部 CI 和真实网络吞吐未由本地测试验证。

保持当前目标框架、包版本、命名客户端管道及 UTF-8；不引入 Flurl/SKIT 依赖，不提交或发布代码。完整 SDK 基类、业务错误码框架、双 JSON 引擎、响应正文改写、熔断、缓存与限流留作独立需求。
