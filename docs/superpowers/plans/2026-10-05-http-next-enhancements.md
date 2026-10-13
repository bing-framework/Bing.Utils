# HTTP 文本读取优化与重试观测 Implementation Plan

> 历史记录：本文按时间顺序保留多个候选的实施与验收记录。各节的 `Completed`/`Verified`/测试计数仅对应该节标注日期的候选，不应合并解读为当前工作树已通过。文中后续节已关闭的事项保留早期 `OPEN_ACTIONABLE` 记录，以呈现历史演进。

> **For agentic workers:** REQUIRED SUB-SKILL: Use executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** 减少普通文本响应的完整字节数组拷贝，并让调用方观察实际进入等待的重试原因与计划延迟。

**Architecture:** 复用现有有界响应复制流程，在同一内存缓冲上完成文本解码；使用可选能力接口增加只读重试事件。保持现有转换、拦截器、完成观测及重试决策契约。

**Tech Stack:** C#、HttpClient、System.Text.Json、xUnit、Shouldly；沿用仓库现有依赖。

**Spec:** 本文件“设计依据与范围”及“首批契约”共同构成此计划的设计规格。

**历史状态：** 2026-10-05 候选中的核心实现已完成；文本缓冲复用、重试观测、上传进度和验证矩阵在该候选中收口，修改前同候选性能快照延期。

## Global Constraints

- 生产目标框架：net8.0;net7.0;net6.0;netstandard2.0。首批不调整依赖、版本或目标框架。
- 不给已有 IHttpRequest<TResult> 增加必需成员；新增功能采用可选接口及链式扩展。
- 保留受保护虚方法分派、调用方拥有的 HttpClient/目标流、异常优先级及请求实例非线程安全契约。
- 所有文本文件读写显式 UTF-8；保留工作区已有未提交修改，不提交或发布。
- 执行前读取适用 AGENTS.md、GOAL_RULES.md、test-project-boundary 技能及其 refs。纯 Handler/Stream 放单元测试，真实文件/TestServer 放集成测试。

## 设计依据与范围

本轮静态检查覆盖请求创建、重试、响应文本/JSON、上传与下载、完成观测、公开能力接口、既有测试和基准。以下是增强建议，不把设计取舍描述成已证实缺陷；性能收益需新基准验证。

| 优先级 | 建议 | 当前依据 | 收益及成本 |
| --- | --- | --- | --- |
| P1 | 文本响应减少一次完整字节数组拷贝 | 当时的热点后来拆分到 `src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.ResponsePrimitives.cs` 的 `ReadResponseBufferAsync` 与 `src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.ResponseReading.cs` 的文本解码路径 | 普通 JSON、文本及元数据响应均可能受益；保留完整 string 分配，复杂度中 |
| P1 | 增加重试等待观测 | 等待决策后来位于 `src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.Resilience.cs` 的 `DelayBeforeRetryAsync`；`HttpRequestCompletion` 只有总耗时/次数 | 解释 429/503/网络失败后的等待；已有拦截器可看单次发送，无需重复制造单次请求追踪，复杂度低至中 |
| P2 | 显式流式 JSON + 元数据结果 | GetJsonStreamAsync 返回 TResult；GetResponseAsync 总是走文本缓冲 | 同时获得头、状态与低拷贝 JSON；需保留失败正文及回调回退，复杂度中 |
| P2 | 上传进度（本轮已完成） | 已新增可选上传进度接口、按尝试包装正文和取消传播 | 大文件上传体验；未知长度/multipart/旧目标框架仍按兼容边界验证 |
| P2 | JSON 数组逐项消费 | DeserializeAsync<TResult> 仍构建完整集合 | 适合大列表处理；需处理提前退出、背压、流释放与旧框架兼容，复杂度高 |
| P3 | 断点续传下载 | 当前文件下载采用新临时文件与最终提交 | 超大文件/弱网有价值；需 Range、If-Range、ETag、206/200/416、压缩及资源变化协议，复杂度高 |
| P3 | HttpRequest.cs 按职责渐进拆分 | 主文件超过 2000 行，已有 Transfer partial | 降低维护成本；只随功能提取私有流程，保留 protected 扩展点，不以拆文件宣称性能提升 |

首批文本、重试观测、流式 JSON 元数据、上传进度和逐项 JSON 均已按独立边界实施；断点续传仍需单独设计，避免把 Range/ETag 协议和文件生命周期变更混入当前请求管线。

暂不优先建设自有缓存、熔断、限流、完整 SDK 框架：当前模块已有外部 HttpClient/工厂和拦截器接入点；这些能力需要独立业务需求和跨请求状态设计。诊断接入优先利用运行时自带网络追踪/指标，并补充本库重试等待及完整操作结果，避免重复记录 HTTP span。

## 首批契约

### A. 文本读取

1. 用内部 MemoryStream 直接解码，避免文本路径调用 ToArray。GetStreamAsync 的 byte[] 返回契约仍需要独立数组。
2. 继续通过 CopyResponseAsync 执行已知/未知长度限额、解压后字节限额、取消和源流释放。
3. 保留 BOM 检测、声明字符集、带引号 charset 和异常传播。尤其保留两个入口原有差异：GetResponseAsync 零字节跳过解码/转换；GetResultAsync 普通 200 空正文仍执行原有转换语义。
4. 必须用缓冲的字节长度判断“零字节”，不能用解码后的 string.Length；只有 BOM 的正文是非零字节正文。
5. HEAD/204、失败/完成回调、派生类 SendAfterAsync、OnSendAfter 缓冲适配流程均按现有契约验收。
6. 此阶段仍保留完整正文缓冲与完整字符串，不承诺常量内存，不根据服务端 Content-Length 直接预分配大数组。

### B. 重试等待观测

建议公共 API：

```csharp
public interface IHttpRequestRetryObservationRegistration<TResult> where TResult : class
{
    void SetRetryObserver(Action<HttpRetryScheduled> observer,
        Action<Exception> onObserverError = null);
}

// HttpRetryObservationExtensions 中的链式扩展。
public static IHttpRequest<TResult> ObserveRetry<TResult>(
    this IHttpRequest<TResult> request, Action<HttpRetryScheduled> observer,
    Action<Exception> onObserverError = null) where TResult : class;

public sealed class HttpRetryScheduled
{
    public HttpRetryScheduled(int failedAttempt, TimeSpan delay,
        HttpStatusCode? statusCode, Exception exception);
    public int FailedAttempt { get; }
    public TimeSpan Delay { get; }
    public HttpStatusCode? StatusCode { get; }
    public Exception Exception { get; }
}
```

- FailedAttempt 从 1 开始；Delay 是最终计划等待时长，含指数退避、抖动和 Retry-After 的现有决策结果，不是实际耗时。
- 事件仅表示已决定进入重试等待，不保证下一次发送发生；等待中取消可阻止下一次发送。
- 超出重试次数、实际 HTTP 方法不允许重试、证书失败、预算不足、不可表达的等待时长均不发此事件。
- 在旧响应成功释放后、WaitBeforeRetryAsync 前发一次；发送异常分支携带原异常，响应状态分支携带状态码。取消已被观察到时先传播取消，不发送计划事件。
- 回调及错误处理回调异常隔离，沿用完成观测的模式；同步调用，不保证 UI 线程，耗时会计入整个请求耗时。
- 在一次 ExecuteCoreAsync 开始时快照 observer/onError，注册变更只影响下一次执行。未注册时不创建事件、集合或计时器。
- 不输出 URI、头或正文。异常对象保留原始引用，可能包含调用方/底层信息；导出日志时由调用方选择字段。
- null 请求/回调、FailedAttempt < 1、负 Delay 明确拒绝；第三方旧实现调用扩展时抛 NotSupportedException。

## Task 1：文本响应缓冲复用

**Files:**
- Modify: src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs（GetResponseAsync、SendAfterAsync 文本路径）
- Create: src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.ResponseReading.cs（私有读取/解码方法）
- Test: tests/Bing.Utils.Http.Tests/Bing/Http/HttpCapabilityExtensionsTest.cs
- Test: tests/Bing.Utils.Http.Tests/Bing/Http/HttpRequestResilienceTest.cs
- Modify: benchmarks/Bing.Utils.Http.Benchmark/Program.cs
- Modify: docs/modules/http.md
- Create: docs/quality/http-response-buffer-benchmark-2026-10-05.md

**Interfaces:** 无新增公共 API。内部产生如下方法：

```csharp
private async Task<MemoryStream> ReadResponseBufferAsync(
    HttpResponseMessage response, CancellationToken token)
{
    var buffer = new MemoryStream();
    try
    {
        await CopyResponseAsync(response, buffer, token).ConfigureAwait(false);
        buffer.Position = 0;
        return buffer;
    }
    catch (Exception error)
    {
        DisposePreservingException(buffer, error);
        throw;
    }
}

private static async Task<string> DecodeResponseTextAsync(
    HttpResponseMessage response, MemoryStream buffer)
{
    var charset = response.Content?.Headers.ContentType?.CharSet?.Trim('"');
    var encoding = string.IsNullOrWhiteSpace(charset)
        ? System.Text.Encoding.UTF8 : System.Text.Encoding.GetEncoding(charset);
    using var reader = new StreamReader(buffer, encoding, true, 1024, leaveOpen: true);
    return await reader.ReadToEndAsync().ConfigureAwait(false);
}
```

- [x] 扩充行为回归：在 HttpCapabilityExtensionsTest 现有 StubHandler 中增加下例；另加入 BOM-only、UTF-16 BOM、ISO-8859-1、无效 charset、空正文 OnConvert、失败/完成回调内容、精确限额与少 1 字节、源流读取失败与取消。已有覆盖直接复用。

```csharp
[Fact]
public async Task GetResponseAsync_BomOnlyIsNotAZeroByteResponse()
{
    var calls = 0;
    using var client = new HttpClient(new StubHandler((_, _) =>
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        { Content = new ByteArrayContent(new byte[] { 0xef, 0xbb, 0xbf }) };
        response.Content.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        return response;
    }));
    var result = await new HttpRequest<string>(null, client, HttpMethod.Get,
            "https://example.test/")
        .OnConvert(_ => { calls++; return "converted"; })
        .GetResponseAsync(includeRawContent: true);
    calls.ShouldBe(1);
    result.Data.ShouldBe("converted");
    result.RawContent.ShouldBe(string.Empty);
}
```

- [x] 运行针对性单元测试并记录当前候选性能快照；历史同候选基线无法从本轮工作区恢复，比较门禁保持未启用。
- [x] 建立 metadata-large/raw-large 的当前候选基线记录；没有同候选修改前数据，因此不宣称前后性能收益。
- [x] 实现上述私有 helper；两处文本调用者用 using 持有返回缓冲，以 buffer.Length 替换原 bytes.Length，再调用 DecodeResponseTextAsync。保留各入口原有空正文/HEAD/204分支顺序及完成回调 finally；不修改字节数组返回和自定义发送后处理的缓冲分支。
- [x] 运行针对性回归与同负载优化后基准，报告 allocated bytes、耗时和正文大小。预期大正文少一次约正文长度的数组拷贝；报告实际数据，不预设收益比例。确认普通 JSON 改善且流式 JSON/下载未受影响后再合入本批结果。

## Task 2：可选重试等待事件

**Files:**
- Create: src/Bing.Utils/Bing/Http/HttpRetryScheduled.cs
- Create: src/Bing.Utils/Bing/Http/HttpRetryObservationExtensions.cs
- Modify: src/Bing.Utils/Bing/Http/IHttpRequestCapabilities.cs
- Create: src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.RetryObservation.cs
- Modify: src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs（执行快照、两条重试分支及等待方法）
- Test: tests/Bing.Utils.Http.Tests/Bing/Http/HttpRequestResilienceTest.cs
- Test: tests/Bing.Utils.Http.Tests/Bing/Http/HttpCapabilityExtensionsTest.cs（具体旧实现及参数验证）
- Modify: docs/modules/http.md
- Modify: docs/modules/http-optimization-migration.md

**Interfaces:** 按“首批契约 B”四个公共签名实现；读取既有 HttpRetryOptions，不改变其重试策略。

- [x] 在 HttpRequestResilienceTest 内复用 CreateClient、RetryTimingRequest，先加入核心场景并运行验证新 API 尚未存在：

```csharp
[Fact]
public async Task ObserveRetry_ReportsOneScheduledWait()
{
    var sends = 0;
    using var client = CreateClient((_, _) =>
        new HttpResponseMessage(++sends == 1
            ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK)
        { Content = new StringContent("ok") });
    var events = new List<HttpRetryScheduled>();
    var request = new RetryTimingRequest(client, DateTimeOffset.UtcNow);
    var result = await request.ObserveRetry(events.Add).Retry(new HttpRetryOptions
    {
        MaxRetries = 1, BaseDelay = TimeSpan.FromMilliseconds(200),
        MaxDelay = TimeSpan.FromSeconds(1), UseJitter = false
    }).GetResultAsync();
    result.ShouldBe("ok");
    sends.ShouldBe(2);
    events.Count.ShouldBe(1);
    events[0].FailedAttempt.ShouldBe(1);
    events[0].StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    events[0].Delay.ShouldBe(TimeSpan.FromMilliseconds(200));
    events[0].Exception.ShouldBeNull();
    request.Delays.ShouldBe(new[] { events[0].Delay });
}
```

- [x] 实现不可变事件、参数验证、可选注册与扩展。私有通知逻辑复用现有完成观测的异常隔离语义：

```csharp
if (observer != null)
{
    try { observer(new HttpRetryScheduled(attempt + 1, delay, statusCode, error)); }
    catch (Exception observationError)
    {
        try { onObserverError?.Invoke(observationError); }
        catch { /* 观测异常不改变请求结果。 */ }
    }
}
```

- [x] 调整私有 DelayBeforeRetryAsync 参数，显式传入发送异常及执行快照的 observer/onError；保留现有退避计算、预算判断、响应释放及等待顺序。在释放响应前保存状态码，释放成功后检查取消再通知。两条调用路径分别传入原 requestError 或 null。
- [x] 增加并运行确定性用例：Retry-After 日期/时长、网络异常原引用、预算不足无事件、非安全方法/证书失败无事件、重试用尽、取消等待后无下一次发送、观察者和错误处理器均抛异常不改变结果、注册替换只影响下一次执行、具体旧实现不支持及所有公开参数边界。事件次数与“进入等待次数”一致，完成观测仍整次执行一次。
- [x] 补充文档示例及未注册/注册空回调的分配对比；不得强制引入 OpenTelemetry 依赖。

## Task 3：首批验收

- [x] 针对性测试通过后执行 HTTP 单元五框架（各 1675/1675）、集成四框架（各 65/65）和生产 Release 构建；新增实现结果已记录。
- [x] 检查 UTF-8、git diff --check、公共契约与受保护扩展点；把基准和测试证据写回此计划并勾选已完成项。

```powershell
# 针对性验证
 dotnet test tests/Bing.Utils.Http.Tests/Bing.Utils.Http.Tests.csproj -c Release -f net8.0 --no-restore --filter 'FullyQualifiedName~HttpCapabilityExtensionsTest|FullyQualifiedName~HttpRequestResilienceTest|FullyQualifiedName~HttpTransferCapabilitiesTest' -v quiet
# 最终矩阵：逐框架执行，失败即停止
foreach ($httpTfm in @('net8.0','net7.0','net6.0','net5.0','netcoreapp3.1')) {
    dotnet test tests/Bing.Utils.Http.Tests/Bing.Utils.Http.Tests.csproj -c Release -f $httpTfm --no-restore -v quiet
    if ($LASTEXITCODE -ne 0) { throw "HTTP unit matrix failed: $httpTfm" }
}
foreach ($httpTfm in @('net7.0','net6.0','net5.0','netcoreapp3.1')) {
    dotnet test tests/Bing.Utils.Http.Tests.Integration/Bing.Utils.Http.Tests.Integration.csproj -c Release -f $httpTfm --no-restore -v quiet
    if ($LASTEXITCODE -ne 0) { throw "HTTP integration matrix failed: $httpTfm" }
}
dotnet build src/Bing.Utils.Http/Bing.Utils.Http.csproj -c Release --no-restore -v quiet
git diff --check
```

## 后续阶段的范围与验收条件

这些是后续候选功能；逐项 JSON、上传进度、断点续传、摘要校验和显式缓存已完成，其余项目不随本轮自动实施。

1. **流式 JSON 元数据结果（已完成）：** 已新增可选 GetJsonStreamResponseAsync 返回 HttpResponseResult<TResult>，成功且无需原文时直接流式解析；失败/字符集/原文回调按 GetResponseAsync 语义回退，RawContent 默认空，元数据、空正文、失败和取消均有测试。
2. **上传进度（已完成）：** 已按发送尝试独立报告 Attempt/BytesWritten/TotalBytes，包装最终 HttpContent 的实际写入并保留 headers/长度计算/所有权；已验收初始/最终事件、已知/未知长度、重试 attempt 隔离、ContentFactory、multipart、取消、参数校验和回调异常。
3. **逐项 JSON（已完成）：** 通过可选 IHttpRequestJsonArrayStreaming<TResult> 与 GetJsonArrayStreamAsync 提供 UTF-8 顶层数组逐项消费。实现使用 Utf8JsonReader 状态机，兼容 netstandard2.0 的 System.Text.Json 4.6.0，不依赖 DeserializeAsyncEnumerable；一次枚举、响应头完成、消费开始后不自动重试，并在首项、跨块元素、尾部校验、提前释放、回调拒绝和限额边界上验收。
4. **断点续传（已完成）：** 新增可选 `IHttpRequestResumableDownload<TResult>` 与 `DownloadFileResumableWithResultAsync`/`DownloadFileResumableAsync`。仅 GET 使用同目录 `.binghttp.part` 与 `.binghttp.resume.json`；要求非弱 ETag 或 Last-Modified 验证器，206 校验偏移后追加，200 安全回退覆盖，匹配的 416 完整片段直接提交。既有下载 API 行为不变。
5. **摘要校验与显式缓存（已完成）：** 续传支持 SHA-256/自定义校验器；内存和文件缓存支持显式 key、TTL、容量/失效及请求接入，默认不自动缓存。

## 官方资料与本轮验证边界

- [DeserializeAsyncEnumerable：顶层数组流式消费](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonserializer.deserializeasyncenumerable?view=net-6.0)。使用前仍须按仓库实际引用版本验证可用重载。
- [System.Net 网络追踪](https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/telemetry/tracing)。运行时已有网络追踪，首批只补重试等待的库级语义。
- 本轮已执行定向测试、HTTP 单元/集成矩阵和 Release 构建；修改前同候选性能快照仍不可恢复，以上性能收益不宣称为前后对比。

## Deferred 项

- 修改前同候选的大正文 metadata/raw 性能快照：当前工作区只有已修改候选，历史记录使用不同基准程序，不能作为可比 before；不影响功能正确性验收。
- 自动缓存接入、分布式一致性和并发分片续传：需要独立的跨请求/跨进程协议设计，保持 DEFERRED。

## 2026-10-05 增量验收证据

- 修正响应源流读取失败夹具；单个回归用例通过。
- 文本读取现在共用 MemoryStream，不再在默认文本路径创建中间 byte[]。
- 精准测试：179/179。
- 上传进度：新增可选接口、每次尝试包装、总字节数传播及参数/异常/重试测试。
- HTTP 单元矩阵：net8.0、net7.0、net6.0 均 1693/1693；net5.0、netcoreapp3.1 均 1692/1692（旧目标框架跳过现代取消重载用例）。
- HTTP 集成矩阵：net7.0、net6.0、net5.0、netcoreapp3.1 均 65/65。
- 生产 Release 构建：0 错误；保留既有警告。
- 大正文当前测量：metadata-large 中位数 697.3 ms / 1090.83 MiB；raw-large 中位数 676.7 ms / 1090.92 MiB。
- 未建立同负载、同候选版本的修改前快照，因此不宣称前后性能比较。


## 2026-10-06 上传进度增量验收证据

- 上传进度覆盖已知/未知长度、重试 attempt 隔离、ContentFactory、multipart headers/长度保留、参数校验、回调异常和取消完成事件抑制。
- net8.0、net7.0、net6.0 单元测试均 1693/1693；net5.0、netcoreapp3.1 均 1692/1692（引用 netstandard2.0，跳过现代取消重载用例）。
- HTTP 集成测试：net7.0、net6.0、net5.0、netcoreapp3.1 均 65/65。
- HTTP Release 构建 0 错误；保留仓库既有分析器/兼容性警告。

## 2026-10-06 逐项 JSON 增量验收证据

- 新增 GetJsonArrayStreamAsync：跨块嵌套对象、空数组、首项早于 EOF、提前 DisposeAsync、取消、非法尾部、响应限额、回调拒绝和显式 Retry 配置下单次发送均通过。
- HTTP 单元命名空间回归：net8.0 201/201；全量矩阵 net8.0/net7.0/net6.0 各 1698/1698，net5.0/netcoreapp3.1 各 1697/1697。
- HTTP 集成矩阵：net8.0/net7.0/net6.0/net5.0/netcoreapp3.1 各 65/65。
- 生产 HTTP 项目 net8.0/net7.0/net6.0/netstandard2.0 Release 构建 0 错误。
- 语义边界：异常在枚举阶段传播；枚举只能消费一次；取消/解析失败/限额/提前停止释放响应和正文；单个巨大元素仍可能占用与元素大小相当的内存。

## 2026-10-06 断点续传增量验收证据

- Completed：新增可选断点续传接口、扩展方法和实现；请求固定使用 `Accept-Encoding: identity`，以 ETag/Last-Modified + 请求身份校验状态，覆盖 206 追加、200 回退、匹配 416 提交、目标替换失败保护和失败状态持久化。
- Completed：新增真实临时文件集成测试 3 项，验证首次中断后 `.part`/`.resume.json`、第二次 `Range`/`If-Range`/`206` 提交、`200` 全量覆盖和 `416` 完整提交。
- Verified Boundaries：HTTP 单元矩阵 net5.0/netcoreapp3.1 各 1698/1698，net6.0/net7.0/net8.0 各 1699/1699；HTTP 集成矩阵五个目标框架各 68/68；断点续传定向测试 net8.0 为 3/3；HTTP Release 构建 netstandard2.0/net6.0/net7.0/net8.0 为 0 错误；`git diff --check` 通过。
- Accepted Limitations：仅显式新 API 启用续传；仅 GET；没有可靠验证器时不保留片段；侧车文件名固定为 `.binghttp.part`/`.binghttp.resume.json`；单个片段仍按现有下载缓冲逐块写入。
- Open Actionable：无本地可执行项。
- Blocked External：修改前同候选的可比性能基线仍不可恢复，无法据此宣称断点续传性能收益。
- Not Applicable：本轮未改变旧 `DownloadFileAsync`/`DownloadFileWithResultAsync` 的默认行为，也未修改包元数据。
- Deferred：逐步拆分 `HttpRequest`、性能基线重建和更高级断点续传策略（并发分片/校验和）留待独立计划。

## 2026-10-06 断点续传 P0 正确性修复验收

- Completed：补强续传身份与验证器选择。相对 URL 在请求准备阶段按 HttpClient.BaseAddress 归一化；弱 ETag 不再作为 If-Range，优先使用 Last-Modified；响应头读取同时覆盖 response/content headers；成功响应的实际 URI 与续传身份不一致时拒绝追加，避免拦截器改写 URI 后污染已有片段。
- Completed：新增相对 URL、弱 ETag 回退 Last-Modified 两项集成验证；断点续传定向测试 net8.0 为 5/5。
- Verified：HTTP 单元矩阵 net5.0/netcoreapp3.1 各 1701/1701，net6.0/net7.0/net8.0 各 1702/1702；HTTP 集成矩阵五个目标框架各 71/71。
- TODO（OPEN_ACTIONABLE，后续实现）：为 .part 与 .resume.json 增加跨进程并发锁；把“写入片段 + 更新状态”做成崩溃恢复可证明的检查点；补充更完整的响应身份字段（关键请求头/编码协商）与 Content-Range 总长度/边界校验；明确 416/失败响应的公开 HttpDownloadResult 语义；使用真实 TestServer 覆盖压缩、代理和连接中断。
- TODO（DEFERRED_ACCEPTED）：渐进拆分 HttpRequest.cs；并发分片/校验和等高级续传策略；旧候选的同负载 before 性能基线。
- BLOCKED_EXTERNAL：当前工作区无法恢复修改前同候选的可比性能快照，因此不宣称本轮性能收益。
- NOT_APPLICABLE：本轮未改变旧 DownloadFileAsync/DownloadFileWithResultAsync 默认行为、包元数据或发布配置。
## 2026-10-06 416 URI 防护补充

- Completed：416 完整片段提交前也校验响应实际 URI 与续传身份；拦截器改写 URI 时拒绝提交并保留 .part 与状态文件。
- Verified：断点续传定向测试 6/6；HTTP 单元矩阵 net5.0/netcoreapp3.1 各 1701/1701、net6.0/net7.0/net8.0 各 1702/1702；HTTP 集成矩阵五个目标框架各 72/72。
## 2026-10-06 续传并发锁验收

- Completed：续传入口在读取片段和状态前创建同目标的持久锁文件 .binghttp.lock，以 FileShare.None 持有到网络传输、状态保存和最终提交结束；同目标并发请求快速失败，进程退出后句柄释放即可继续。
- Completed：HTTP 文档和迁移文档补充锁文件、并发冲突和锁文件长期保留语义。
- Verified：并发锁定向集成测试与既有续传回归共 7/7；HTTP 单元矩阵 net5.0/netcoreapp3.1 各 1702/1702、net6.0/net7.0/net8.0 各 1703/1703；HTTP 集成矩阵五个目标框架各 73/73；HTTP Release 构建 0 错误；git diff --check 通过。
- CLOSED：同一目标文件的跨进程并发保护。
- TODO（OPEN_ACTIONABLE）：崩溃安全检查点、请求/响应身份字段扩展、Content-Range 完整边界校验、416/失败响应的公开结果语义、真实 TestServer 覆盖压缩/代理/连接中断。
- TODO（DEFERRED_ACCEPTED）：HttpRequest.cs 渐进拆分、并发分片/校验和、同候选性能基线重建。
## 2026-10-06 续传崩溃安全检查点验收

- Completed：响应正文每次写入片段后先对目标 FileStream 执行 Flush(true)，再更新 BytesWritten 并原子替换 resume 状态；状态临时文件使用 WriteThrough、显式 Flush(true) 和随机临时名。
- Completed：首次中断测试现在直接校验 resume 状态中的 BytesWritten 与已写片段一致；完整正文 SHA-256 校验失败保留完整临时文件长度，目标文件仍不提交。
- Verified：断点续传精准测试 9/9；HTTP 单元矩阵 net5.0/netcoreapp3.1 各 1707/1707、net6.0/net7.0/net8.0 各 1708/1708；HTTP 集成矩阵五个目标框架各 75/75；HTTP Release 构建 0 警告、0 错误；git diff --check 通过。
- CLOSED：续传崩溃时状态不应落后于已经刷盘的片段；失败校验不破坏既有目标文件。
- TODO（OPEN_ACTIONABLE）：请求/响应身份字段扩展、Content-Range 完整边界校验、416/失败响应的公开结果语义、真实 TestServer 覆盖压缩/代理/连接中断。
- TODO（DEFERRED_ACCEPTED）：HttpRequest.cs 渐进拆分、并发分片/校验和、同候选性能基线重建。
## 2026-10-06 Content-Range 边界验收

- Completed：206 响应现在校验 `bytes` 单位、From/To/Length 的完整边界、起始偏移、结束位置、总长度及正文长度；续传完成后的累计长度必须等于声明总长度。
- Completed：416 完整片段提交现在只接受 `bytes */length` 形式，拒绝带 From/To 或总长度不一致的响应。
- Completed：新增非法 206 与非法 416 回归测试，错误响应保留 `.part` 与 `.resume.json`，不提交目标文件。
- Verified：断点续传定向集成测试 `11/11`；HTTP 单元矩阵 net8.0/net7.0/net6.0 各 `1710/1710`，net5.0/netcoreapp3.1 各 `1709/1709`；HTTP 集成矩阵五个目标框架各 `77/77`；HTTP Release 构建 0 警告、0 错误；`git diff --check` 通过。
- CLOSED：Content-Range 完整边界校验。
- TODO（OPEN_ACTIONABLE）：请求/响应身份字段扩展、416/失败响应的公开 `HttpDownloadResult` 语义、真实 TestServer 覆盖压缩/代理/连接中断。
- TODO（DEFERRED_ACCEPTED）：`HttpRequest.cs` 渐进拆分、并发分片/校验和、同候选性能基线重建。
- BLOCKED_EXTERNAL：修改前同候选的可比性能快照仍不可恢复，无法据此宣称性能收益。
- NOT_APPLICABLE：本轮未改变旧下载 API 默认行为、包元数据或发布配置。
## 2026-10-06 续传身份字段扩展验收

- Completed：续传状态新增请求头 SHA-256 指纹与响应 `Content-Encoding` 字段；指纹只保存摘要，不落盘认证头或其他原始敏感值，兼容旧状态读取。
- Completed：续传候选、206 追加和 416 完整片段提交均校验请求 URI、请求头指纹和响应编码；关键请求头或编码变化时拒绝追加，避免把不同表示拼接到同一片段。
- Completed：新增请求 `Accept-Language` 变化和响应 `Content-Encoding` 变化两项集成回归测试；前者重新开始全量下载，后者保留片段与状态并拒绝提交。
- Completed：HTTP 文档与迁移文档补充状态指纹和编码校验语义。
- Verified：断点续传定向集成测试 `13/13`；HTTP 单元矩阵 net8.0/net7.0/net6.0 各 `1716/1716`，net5.0/netcoreapp3.1 各 `1715/1715`；HTTP 集成矩阵五个目标框架各 `79/79`；HTTP Release 构建 0 错误、3 个既有 ASP0019 警告；`git diff --check` 通过。
- CLOSED：请求/响应身份字段扩展。
- TODO（OPEN_ACTIONABLE）：明确 416/失败响应的公开 `HttpDownloadResult` 语义、使用真实 TestServer 覆盖压缩/代理/连接中断。
- TODO（DEFERRED_ACCEPTED）：`HttpRequest.cs` 渐进拆分、并发分片/校验和、同候选性能基线重建。
- BLOCKED_EXTERNAL：修改前同候选的可比性能快照仍不可恢复，无法据此宣称性能收益。
- NOT_APPLICABLE：本轮未改变旧下载 API 默认行为、包元数据或发布配置。
## 2026-10-06 续传公开结果语义验收

- Completed：续传 HTTP 失败响应返回的 `HttpDownloadResult.BytesWritten` 现在报告仍保留的 `.part` 长度；无已有片段时仍为 0。
- Completed：匹配的完整片段 `416` 保留线上的 `StatusCode=416` 与 `IsSuccessStatusCode=false`，但 `IsFileSaved=true` 明确表示本地文件已提交；`BytesWritten` 为提交的完整长度。
- Completed：补充 `HttpDownloadResult` XML 注释、HTTP 文档和迁移文档，明确失败与 416 的判断方式，并增加对应集成断言。
- Verified：断点续传定向集成测试 `13/13`；HTTP 单元矩阵 net8.0/net7.0/net6.0 各 `1718/1718`，net5.0/netcoreapp3.1 各 `1717/1717`；HTTP 集成矩阵五个目标框架各 `79/79`；HTTP Release 构建 0 警告、0 错误；`git diff --check` 通过。
- CLOSED：416/失败响应的公开 `HttpDownloadResult` 语义。
- NOTE：为恢复全矩阵，修正了工作区已有未跟踪缓存测试中 `Func<object,long>` 对 `Length` 的类型错误；未修改缓存生产实现。
- TODO（OPEN_ACTIONABLE）：使用真实 TestServer 覆盖压缩、代理和连接中断。
- TODO（DEFERRED_ACCEPTED）：`HttpRequest.cs` 渐进拆分、并发分片/校验和、同候选性能基线重建。
- BLOCKED_EXTERNAL：修改前同候选的可比性能快照仍不可恢复，无法据此宣称性能收益。
- NOT_APPLICABLE：本轮未改变旧下载 API 默认行为、包元数据或发布配置。
## 2026-10-06 真实 TestServer 链路验收

- Completed：新增源站 TestServer + 转发代理 TestServer 集成场景。源站首个响应声明完整长度但提前结束，验证客户端保留 checkpoint；第二次请求必须携带 `Range: bytes=4-` 并由代理转发 206/Content-Range。
- Completed：源站验证客户端固定发送 `Accept-Encoding: identity`，避免压缩协商导致片段偏移；代理只转发关键请求头和响应边界，覆盖实际代理链路。
- Verified：真实 TestServer 断点续传定向测试 `14/14`；HTTP 单元矩阵 net8.0/net7.0/net6.0 各 `1720/1720`，net5.0/netcoreapp3.1 各 `1719/1719`；HTTP 集成矩阵五个目标框架各 `80/80`；HTTP Release 构建 0 警告、0 错误；`git diff --check` 通过。
- CLOSED：真实 TestServer 覆盖压缩协商、代理转发和连接中断。
- TODO（OPEN_ACTIONABLE）：无本地可执行项。
- TODO（DEFERRED_ACCEPTED）：`HttpRequest.cs` 渐进拆分、并发分片/校验和、同候选性能基线重建。
- BLOCKED_EXTERNAL：修改前同候选的可比性能快照仍不可恢复，无法据此宣称性能收益。
- NOT_APPLICABLE：本轮未改变旧下载 API 默认行为、包元数据或发布配置。


## 2026-10-06 续传侧车维护验收

- Completed：新增 `HttpResumableDownloadMaintenance.CleanupAsync` 和 `HttpResumableDownloadCleanupResult`，按目标文件分组清理过期 `.binghttp.part`、`.binghttp.resume.json` 及状态临时文件。
- Completed：清理前独占对应 `.binghttp.lock`；活动下载无法取得锁时跳过，锁文件本身始终保留，避免释放锁后删除造成跨进程竞态。
- Completed：新增 3 项单元测试，覆盖过期侧车删除、活动锁跳过、取消令牌和不存在目录；HTTP 单元全量 net8.0 为 `1753/1753`，定向维护测试为 `3/3`。
- Verified：新增源码、测试和文档均为 UTF-8 无 BOM，`git diff --check` 通过。
- CLOSED：显式断点续传侧车清理能力。
- TODO（OPEN_ACTIONABLE）：无本地可执行项。
- TODO（DEFERRED_ACCEPTED）：自动后台清理调度、递归目录扫描、锁文件回收策略、`HttpRequest.cs` 渐进拆分、并发分片/校验和、同候选性能基线重建。
- BLOCKED_EXTERNAL：修改前同候选的可比性能快照仍不可恢复，无法据此宣称性能收益。
- NOT_APPLICABLE：本轮未改变旧下载 API 默认行为、包元数据或发布配置。

## 2026-10-07 续传跨进程锁验证

- Completed：新增真实独立进程持锁测试；PowerShell 子进程以 FileShare.None 持有目标 .binghttp.lock 时，续传请求在发送前失败，未发出网络请求，并保留锁文件。
- Verified：跨进程续传锁定向测试 1/1；HTTP 集成项目 net8.0 全量 83/83；git diff --check 通过。
- CLOSED：续传锁的真实跨进程验证缺口。
- TODO（OPEN_ACTIONABLE）：无本地可执行项。
- TODO（DEFERRED_ACCEPTED）：自动后台清理调度、递归目录扫描、锁文件回收策略、HttpRequest.cs 渐进拆分、并发分片/校验和、同候选性能基线重建。
- BLOCKED_EXTERNAL：远程 CI / 发布环境 API baseline 和修改前同候选性能基线仍需外部证据。
- NOT_APPLICABLE：本轮未改变生产 API、旧下载 API 默认行为、包元数据或发布配置。

## 2026-10-07 PR 验证触发准备

- Completed：GitHub Actions .NET 验证工作流新增 main 分支 Pull Request 触发；PR 会复用现有多平台构建、依赖审计和测试步骤。
- Verified：工作流触发块包含 push、pull_request 和 workflow_dispatch，文件为 UTF-8，git diff --check 通过。
- CLOSED：PR 阶段不会自动执行 HTTP/全解决方案验证的本地配置缺口。
- BLOCKED_EXTERNAL：实际 GitHub Actions 运行结果仍需远程仓库执行，当前工作区不能伪造外部证据。
- TODO（DEFERRED）：正式二进制 API baseline/API diff 门禁仍需选择工具、冻结基线并获得发布流程批准。
