# 模块：Bing.Utils.Http

> HTTP 客户端安全配置和新增流式/重试 API 的迁移方式见 [HTTP 优化迁移说明](http-optimization-migration.md)。下文的历史行为说明以迁移说明为准。

## 1. 模块定位
- 目标：提供 HTTP 客户端的链式请求构建、响应解析、流式传输与显式重试，并补充 ASP.NET Core 请求/响应/会话的常用扩展与 Web 辅助能力。
    - 证据：src/Bing.Utils.Http/Bing/Http/Clients/HttpClientService.cs | Bing.Http.Clients.HttpClientService.Get/Post/Put/Delete
    - 证据：src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs | Bing.Http.Clients.HttpRequest<TResult>（链式构建 + GetResultAsync/GetStreamAsync/WriteAsync）
    - 证据：src/Bing.Utils.Http/Bing/Http/Extensions/Extensions.HttpRequest.cs | Bing.Http.HttpRequestExtensions
    - 证据：src/Bing.Utils.Http/Bing/Http/Extensions/Extensions.HttpResponse.cs | Bing.Http.HttpResponseExtensions
    - 证据：src/Bing.Utils.Http/Bing/Helpers/Web.cs | Bing.Helpers.Web（HttpContext/参数/上传文件/Url 编码等）
- 非目标：不提供完整“API SDK 生成/管理平台”、熔断或限流等网关治理能力；请求重试为显式启用的有限能力。
    - 证据：src/Bing.Utils.Http/Bing.Utils.Http.csproj | Description=Http操作类库（未声明治理/编排能力）
    - 证据：src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs | ExecuteAsync/DelayBeforeRetryAsync（显式重试）
- 适用场景：
    - 在应用内通过 `IHttpClient` 发起基础 REST 调用，并通过链式方法配置 QueryString/Header/Cookie/Timeout/Content。
        - 证据：src/Bing.Utils/Bing/Http/IHttpClient.cs | Bing.Http.IHttpClient
        - 证据：src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs | QueryString/Header/Cookie/Timeout/Content/GetResultAsync
        - 证据：tests/Bing.Utils.Http.Tests.Integration/Http/HttpClientServiceTest.Get.cs | Test_Get_QueryString_2/Test_Get_Header_1/Test_Get_Cookie_2
    - 通过 `JsonContent(...)` 发送 JSON，并在响应 `Content-Type` 为 `application/json` 时自动反序列化为泛型结果。
        - 证据：src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs | JsonContent(...)/ConvertTo(...)
        - 证据：tests/Bing.Utils.Http.Tests.Integration/Http/HttpClientServiceTest.Get.cs | Test_Get_3（Get<List<CustomerDto>>）
        - 证据：tests/Bing.Utils.Http.Tests.Integration/Http/HttpClientServiceTest.Put.cs | Test_Put_2（Put<CustomerDto>）
    - 上传文件（multipart/form-data），支持文件路径或 Stream，并可与普通参数混发。
        - 证据：src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs | FileContent(...)/CreateFileUploadContent/AddFileData/AddFileParameters
        - 证据：tests/Bing.Utils.Http.Tests.Integration/Http/HttpClientServiceTest.Post.cs | Test_Post_FileContent_1/2/3/4
        - 证据：tests/Bing.Utils.Http.Tests.Integration/Controllers/Test6Controller.cs | Upload/MultiUpload（Web.GetFile/Web.GetFiles/Web.GetParam）
- 不适用场景：
    - 需要“强一致的失败抛异常策略/统一错误模型”的场景（默认在非成功状态码时返回 null，并通过 OnFail/OnComplete 回调）。
        - 证据：src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs | SendAfterAsync(...)

## 2. 目录结构
    src/Bing.Utils.Http/
        Bing/Http/Clients/（HttpClientService/HttpRequest 等）
        Bing/Http/Extensions/（Request/Response/ResponseMessage/Session 扩展）
        Bing/Helpers/（Web/Cookie/Ip/UserAgent 等）
        Bing/Net/（IpAddressProvider/IpValidator/IPv4/IPv6/NetworkInformation 等）
        Bing/Parameters/（UrlParameterBuilder 扩展）
    tests/Bing.Utils.Http.Tests/（单元测试）
    tests/Bing.Utils.Http.Tests.Integration/（集成测试，基于 TestServer）
    - 证据：src/Bing.Utils.Http/Bing/Http/Clients/HttpClientService.cs | namespace Bing.Http.Clients
    - 证据：src/Bing.Utils.Http/Bing/Helpers/Web.cs | namespace Bing.Helpers
    - 证据：src/Bing.Utils.Http/Bing/Net/IpAddressProvider.cs | namespace Bing.Net
    - 证据：tests/Bing.Utils.Http.Tests.Integration/Startup.cs | UseTestServer + 注入 IHttpClient

## 3. 对外 API
| API | 说明 | 参数 | 返回 | 异常 |
|---|---|---|---|---|
| `IHttpClient.Get/Post/Put/Delete(...)` | HTTP 方法入口，返回“可链式配置的请求对象” | `url` + 可选 `queryString/content` | `IHttpRequest<TResult>` |  | 
| `IHttpRequest<TResult>.QueryString(...)` | 添加查询字符串（字典/对象/键值） | `key/value` 或 `object` | `IHttpRequest<TResult>` |  | 
| `IHttpRequest<TResult>.Header(...)` | 设置请求头（重复 key 会覆盖） | `key/value` 或 `IDictionary` | `IHttpRequest<TResult>` |  | 
| `IHttpRequest<TResult>.Cookie(...)` / `UseCookies(...)` | 设置单次请求 Cookie；自动 Cookie 模式需在命名客户端注册时配置 | `key/value` / `isUseCookies` | `IHttpRequest<TResult>` | `UseCookies` 显式调用抛异常 |
| `IHttpRequest<TResult>.Content(...)` | 添加请求内容参数（表单/JSON/XML 的数据来源） | `key/value` / `IDictionary` / `object` | `IHttpRequest<TResult>` |  | 
| `IHttpRequest<TResult>.JsonContent(...)` / `XmlContent(...)` | 设置内容类型并写入内容 | `object`/`string` | `IHttpRequest<TResult>` |  | 
| `IHttpRequest<TResult>.FileContent(...)` | 设置 multipart/form-data 并添加文件；流重载可指定 `leaveOpen` | `filePath/name` 或 `stream/fileName/name/leaveOpen` | `IHttpRequest<TResult>` |  |
| `IHttpRequest<TResult>.Timeout(...)` | 设置单次请求总超时（发送、等待和正文读取） | `int`（秒）/`TimeSpan` | `IHttpRequest<TResult>` |  |
| `IHttpRequest<TResult>.Retry(...)` / `MaxResponseBytes(...)` | 显式启用重试、限制响应正文大小 | `HttpRetryOptions` / 字节数 | `IHttpRequest<TResult>` | 超限时 `HttpResponseSizeLimitException` |
| `IHttpRequest<TResult>.Certificate(...)` / `IgnoreSsl()` | 兼容旧签名；客户端证书与 TLS 策略需在命名客户端注册时配置 | `path/password` | `IHttpRequest<TResult>` | 显式调用抛异常 |
| `IHttpRequest<TResult>.OnSendBefore/OnSendAfter/OnConvert/OnSuccess/OnFail/OnComplete` | 生命周期回调（发送前/后、转换、成功/失败/完成） | delegates | `IHttpRequest<TResult>` |  | 
| `IHttpRequest<TResult>.GetResultAsync()` | 发送请求并获取结果（字符串或 JSON 反序列化） | `CancellationToken` | `Task<TResult>` |  | 
| `IHttpRequest<TResult>.GetStreamAsync()` / `WriteAsync(...)` | 以字节数组读取响应或写入文件 | `CancellationToken` / `filePath` | `Task<byte[]>` / `Task` |  | 
| `IHttpRequest<TResult>.DownloadToAsync(...)` / `DownloadFileAsync(...)` | 将响应流式写入目标流或文件 | `Stream` / `filePath` / `CancellationToken` | `Task` |  |
| `HttpRequestExtensions.Query<T>/Form<T>/Params/IsAjaxRequest/IsJsonContentType/IsMobileBrowser` | ASP.NET Core `HttpRequest` 常用扩展 | 见方法 | 见方法 | `ArgumentNullException`（部分方法对 request 做 null guard） | 
| `HttpResponseExtensions.WriteJsonAsync/WriteHtmlAsync/SetCache/SetNoCache` | ASP.NET Core `HttpResponse` 写入与缓存头设置 | 见方法 | `Task`/`void` |  | 
| `Web.GetFiles/GetFile/GetParam/UrlEncode/...` | Web 上下文常用辅助（上传文件/参数/编码等） | 见方法 | 见方法 | `ArgumentException`/`InvalidOperationException`（部分方法） |

> API 证据：
> - src/Bing.Utils/Bing/Http/IHttpClient.cs | Bing.Http.IHttpClient
> - src/Bing.Utils/Bing/Http/IHttpRequest.cs | Bing.Http.IHttpRequest<TResult>
> - src/Bing.Utils.Http/Bing/Http/Clients/HttpClientService.cs | HttpClientService.Get/Post/Put/Delete
> - src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs | HttpRequest<TResult> 的链式配置与 GetResultAsync/GetStreamAsync/WriteAsync
> - src/Bing.Utils.Http/Bing/Http/Extensions/Extensions.HttpRequest.cs | HttpRequestExtensions.*
> - src/Bing.Utils.Http/Bing/Http/Extensions/Extensions.HttpResponse.cs | HttpResponseExtensions.*
> - src/Bing.Utils.Http/Bing/Helpers/Web.cs | Web.GetFiles/GetFile/GetParam/UrlEncode/Body/Url

## 4. 核心类型
| 类型 | 职责 | 线程安全 | 备注 |
|---|---|---|---|
| `Bing.Http.IHttpClient` | Http 客户端契约（Get/Post/Put/Delete） | 取决于实现 | 定义在核心包 `Bing.Utils` 中 | 
| `Bing.Http.IHttpRequest<TResult>` | Http 请求契约（链式配置 + 异步执行） | 取决于实现 | 定义在核心包 `Bing.Utils` 中 | 
| `Bing.Http.Clients.HttpClientService` | `IHttpClient` 的实现，生成 `HttpRequest<TResult>` | 取决于 HttpClient/Factory | 支持注入 `HttpClient` 或 `IHttpClientFactory` | 
| `Bing.Http.Clients.HttpRequest<TResult>` | 请求构建器：Query/Header/Cookie/Content/File/证书/超时/回调/解析 | 每实例非线程安全（可变状态） | 默认内容类型为 JSON；非成功状态码默认返回 null | 
| `Bing.Helpers.Web` | ASP.NET Core `HttpContext` 访问与常用 Web helper | 静态状态（依赖 HttpContextAccessor） | `Url` 在 NETSTANDARD2_1 下不支持；`Body` 可能抛 `InvalidOperationException` | 
| `Bing.Helpers.CookieHelper` | Cookie 读写/清空 | 静态无状态（依赖 context） | 对 `name/context` 做 guard 并抛异常 | 
| `Bing.Net.IpAddressProvider` / `Bing.Net.IpValidator` | IP 获取/校验（含公网 IP 获取） | 静态（AsyncLocal 缓存 IP） | 公网 IP 通过外部服务获取，可能受网络/超时影响 | 
| `Bing.Helpers.UserAgentHelper` | UserAgent 识别（操作系统/浏览器/微信） | 读写字典需调用方自担 | 提供可修改字典与 `AddBrowserSupport` | 

> 核心类型证据：
> - src/Bing.Utils/Bing/Http/IHttpClient.cs | IHttpClient
> - src/Bing.Utils/Bing/Http/IHttpRequest.cs | IHttpRequest<TResult>
> - src/Bing.Utils.Http/Bing/Http/Clients/HttpClientService.cs | HttpClientService
> - src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs | HttpRequest<TResult>
> - src/Bing.Utils.Http/Bing/Helpers/Web.cs | Web
> - src/Bing.Utils.Http/Bing/Helpers/CookieHelper.cs | CookieHelper
> - src/Bing.Utils.Http/Bing/Net/IpAddressProvider.cs | IpAddressProvider
> - src/Bing.Utils.Http/Bing/Net/IpValidator.cs | IpValidator
> - src/Bing.Utils.Http/Bing/Helpers/UserAgentHelper.cs | UserAgentHelper

## 5. 依赖关系
- 直接依赖：
    - `Bing.Utils`（项目引用，提供 `IHttpClient/IHttpRequest/HttpContentType` 等契约与枚举）
        - 证据：src/Bing.Utils.Http/references.props | ProjectReference ..\\Bing.Utils\\Bing.Utils.csproj
        - 证据：src/Bing.Utils/Bing/Http/IHttpClient.cs | IHttpClient
        - 证据：src/Bing.Utils/Bing/Http/IHttpRequest.cs | IHttpRequest<TResult>
        - 证据：src/Bing.Utils/Bing/Http/HttpContentType.cs | HttpContentType
    - `System.Net.Http`（HttpClient/HttpRequestMessage 等）
        - 证据：src/Bing.Utils.Http/Bing/Http/Clients/HttpClientService.cs | using System.Net.Http
        - 证据：src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs | HttpClient/HttpRequestMessage/HttpContent
- 可选依赖（按目标框架条件引用 ASP.NET Core 组件）：
    - netstandard2.0：`Microsoft.Extensions.Http`、`Microsoft.AspNetCore.Http`、`Microsoft.AspNetCore.Http.Extensions`、`Microsoft.AspNetCore.Hosting`
    - netstandard2.1：`Microsoft.Extensions.Http`、`Microsoft.AspNetCore.Http`
    - netcoreapp3.1/net5+/net6+/net7+/net8+：`FrameworkReference Microsoft.AspNetCore.App`
        - 证据：src/Bing.Utils.Http/dependency.props | ItemGroup Condition=TargetFramework
- 禁止依赖：待确认（仓库未看到针对本模块的“禁止依赖”显式约束；如需补充请以架构规则或代码引用为证据）。

## 6. 关键实现说明
### 6.1 算法/流程
- 请求构建（`HttpRequest<TResult>`）：
    - QueryString：通过 `QueryHelpers.AddQueryString(url, QueryParams)` 合并查询参数。
        - 证据：src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs | GetUrl(...)
    - Cookie：将 `Cookies` 组装为多个 `CookieHeaderValue`，最终写入请求头 `Cookie`。
        - 证据：src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs | AddCookies(...)
    - Header：遍历 `HeaderParams` 并调用 `message.Headers.Add(key,value)`。
        - 证据：src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs | AddHeaders(...)
    - Content：按 `HttpContentType` 分派创建内容：
        - `application/x-www-form-urlencoded` -> `FormUrlEncodedContent`
        - `application/json` -> `StringContent(json)`
        - `text/xml` -> `StringContent(Param.SafeString())`
        - `multipart/form-data` -> `MultipartFormDataContent`（边界 + 参数 + 文件）
        - 证据：src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs | CreateHttpContent/CreateFormContent/CreateJsonContent/CreateXmlContent/CreateFileUploadContent

- JSON 序列化配置（默认 options）：
    - `PropertyNameCaseInsensitive = true`；NET5+ 使用 `DefaultIgnoreCondition.WhenWritingNull` 与 `JsonNumberHandling.AllowReadingFromString`；并注册 `DateTimeJsonConverter/NullableDateTimeJsonConverter`。
        - 证据：src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs | GetJsonSerializerOptions(...)

- 发送与解析（`GetResultAsync`）：
    1) `CreateMessage()`：创建请求消息并挂载 cookies/headers/content；
    2) `SendBefore(...)`：允许“发送前取消”；
    3) 发送请求：获取 HttpClient，将 BaseAddress 用于本次 URI 解析、Timeout 用于本次取消令牌，并执行 `HttpClient.SendAsync`；
    4) `SendAfterAsync(...)`：读取响应字符串；成功则 `ConvertTo(...)`，失败则触发 `OnFail` 并返回 null；最后执行 `OnComplete`。
    - 证据：src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs | GetResultAsync/CreateMessage/SendAsync/SendAfterAsync/SuccessHandlerAsync/ConvertTo/FailHandler/CompleteHandler

- 文件上传（multipart/form-data）：
    - 普通参数：`ByteArrayContent(UTF8.GetBytes(value))` 加入表单；
    - 文件：支持 `Stream` 或文件路径；通过 `StreamContent` 发送，不预先缓冲整个文件。
        - 证据：src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs | AddFileParameters/AddFileData/AddFileData(...)
    - 集成用例验证：单文件、多文件、文件+参数。
        - 证据：tests/Bing.Utils.Http.Tests.Integration/Http/HttpClientServiceTest.Post.cs | Test_Post_FileContent_1/2/3/4

### 6.2 边界与异常处理
- `HttpRequest<TResult>` 构造 guard：
    - `httpClientFactory == null && httpClient == null`：抛 `ArgumentNullException(nameof(httpClientFactory))`。
    - `url` 为空/空白：抛 `ArgumentNullException(nameof(url))`。
        - 证据：src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs | HttpRequest(..ctor..)

- Header/QueryString/Cookie/Content 的空 key/value 行为：
    - key 为空白时直接返回 this（不添加）。
    - Header/QueryString/Cookie 同 key 会先 Remove 再 Add（覆盖）。
        - 证据：src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs | Header/QueryString/Cookie/Content

- 非成功状态码的默认行为：
    - `SendAfterAsync` 在 `IsSuccessStatusCode==false` 时执行 `FailHandler` 并返回 null；`CompleteHandler` 在 finally 中始终执行。
        - 证据：src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs | SendAfterAsync(...)

- 结果转换规则（无自定义 `OnConvert` 时）：
    - `TResult == string` 直接返回响应内容；否则在 `application/json` 或 `application/*+json` 时尝试 `Json.ToObject<TResult>`，否则返回 null。
        - 证据：src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs | ConvertTo(...)
        - 证据：src/Bing.Utils.Http/Bing/Http/Extensions/HttpResponseMessageExtensions.cs | GetContentType(...)

- `OnSendBefore`：
    - 设置后在每次发送尝试前执行；返回 false 会取消发送，`GetResponseAsync` 返回 null，且不执行响应回调。
        - 证据：src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs | SendBefore(...)

- 文件上传的资源与错误路径：
    - `FileContent(Stream...)` 默认在请求结束后释放流；需要保留流时使用 `FileContent(stream, fileName, name, leaveOpen: true)`。
        - 证据：src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs | AddFileData(...)
    - 文件路径不存在时抛 `FileNotFoundException`，不发送不完整的 multipart 请求。
        - 证据：src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs | AddFileData(...)

- ASP.NET Core 扩展的边界：
    - `HttpRequestExtensions.Query<T>/Form<T>`：转换失败（`InvalidCastException`）返回默认值而非抛出。
        - 证据：src/Bing.Utils.Http/Bing/Http/Extensions/Extensions.HttpRequest.cs | Query<T>/Form<T>
    - `HttpRequestExtensions.IsMobileBrowser`：User-Agent 缺失或不足四个字符时返回 false。
        - 证据：src/Bing.Utils.Http/Bing/Http/Extensions/Extensions.HttpRequest.cs | IsMobileBrowser
    - `Web.Body`：读取请求体失败会抛 `InvalidOperationException("无法读取请求正文", ex)`。
        - 证据：src/Bing.Utils.Http/Bing/Helpers/Web.cs | Body
    - `Web.Url`：NETSTANDARD2_1 下直接抛 `NotSupportedException`。
        - 证据：src/Bing.Utils.Http/Bing/Helpers/Web.cs | Url（#if NETSTANDARD2_1）
    - `SessionExtensions.Get<T>`：NETSTANDARD2_1 下 `value` 固定为空字符串，返回 default（等价于不可用）。
        - 证据：src/Bing.Utils.Http/Bing/Http/Extensions/Extensions.Session.cs | SessionExtensions.Get<T>

## 7. 性能与复杂度
- 时间复杂度：
    - 构建请求（Query/Header/Cookie/参数合并）：通常为 O(n)（n=参数/头/文件数量）。
        - 证据：src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs | GetParameters/AddHeaders/AddCookies/AddFileData
    - 发送请求：主要由网络 IO 决定。
        - 证据：src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs | HttpClient.SendAsync
- 空间复杂度：
    - JSON 序列化仍会产生字符串分配；文件上传使用 StreamContent，不再整包读入内存。
        - 证据：src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs | CreateJsonContent/AddFileData/StreamContent
- 大数据量表现：
    - 大文件上传采用流式传输；调用方应明确上传流的所有权和重试限制。
        - 证据：src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs | AddFileData(...)
- Benchmark 链接：[HTTP 优化性能报告](../quality/http-optimization-benchmark-2026-10-04.md)；报告说明了基准程序、测量版本和适用范围。

HTTP 基准程序默认继续输出兼容历史记录的 CSV 行。需要离线保存可审计基线时，可使用可选的 `--baseline` 输出路径；该模式不访问网络，写入 JSON 中的运行时、操作系统、目标框架、处理器数、输入规模、重复次数、耗时、托管分配、峰值堆、实际并行度和队列峰值均可直接复用：

```powershell
dotnet run --project benchmarks/Bing.Utils.Http.Benchmark/Bing.Utils.Http.Benchmark.csproj -c Release -- metadata-large 1 1 3 --baseline artifacts/http-baseline.json
```

报告中的耗时和分配反映本机测量，不自动设定回归阈值；比较时应固定模式、负载、目标框架、Release 配置和环境。`metadata`/`raw` 及其大正文变体仍由同一基准程序提供，默认入口和 BenchmarkDotNet 基准工程不受影响。

两份 JSON 可用离线比较入口生成结构化差异；比较前会拒绝模式、目标框架、输入规模、并发、重复次数或迭代次数不一致的报告：

```powershell
dotnet run --project benchmarks/Bing.Utils.Http.Benchmark/Bing.Utils.Http.Benchmark.csproj -c Release -- --compare before.json after.json --output artifacts/http-baseline-diff.json
```

比较结果按重复编号记录耗时、总托管分配、按该轮实际操作数折算的请求级分配和托管堆峰值的前后值、绝对变化及百分比变化，同时保留两侧实际并行度和队列峰值。比较还会拒绝操作系统、架构、Runtime、处理器数量或 Server GC 模式不同的基线。比较只读取本地 JSON，不访问网络。

可选回归门禁只在显式传入时启用；超过阈值返回退出码 1，并在报告的 `RegressionFailures` 中列出重复编号、指标和百分比。默认检查三项，也可指定单项：

```powershell
dotnet run --project benchmarks/Bing.Utils.Http.Benchmark/Bing.Utils.Http.Benchmark.csproj -c Release -- --compare before.json after.json --output artifacts/http-baseline-diff.json --fail-on-regression 10 --metric time --percentile p95
```

`--metric request-allocated` 可单独门禁请求级分配；需要按多轮样本的分布而非单轮值设门禁时，可追加 `--percentile p50` 或 `--percentile p95`。比较结果同时输出两种分位数，未指定时仍按原有逐轮样本检查。

CI 可显式调用 `build/Invoke-HttpBenchmarkBaseline.ps1` 复用同一套比较器。脚本不会下载或生成历史基线：`-BaselinePath` 不存在时输出 `SKIPPED_BASELINE_MISSING` 并返回 0；基线存在时先生成当前 JSON，再校验运行时、OS、架构、处理器数、Server GC 和 Benchmark 维度，环境不兼容或超过 `-FailOnRegression` 阈值时返回非零。示例：

```powershell
./build/Invoke-HttpBenchmarkBaseline.ps1 -BaselinePath artifacts/http-baseline.json `
  -Mode metadata-large -SizeMiB 1 -Concurrency 1 -Repetitions 3 `
  -FailOnRegression 10 -Metric time -Percentile p95 -NoBuild
```

基线值为零时百分比变化为 `null`，该指标不会触发门禁；调用方应结合绝对变化判断零基线场景。

HTTP 响应缓存是独立的显式能力，不会改变 `HttpRequest` 的默认行为。`InMemoryHttpResponseCache<TResult>` 使用调用方定义的 key 和 TTL 保存已转换的 `HttpResponseResult<TResult>` 快照；同一 key 的并发未命中会共享一个回源任务。缓存可选实现 `IHttpResponseCacheStatistics`，通过 `GetStatistics()` 获取进程内累计的命中、未命中、淘汰等快照；文件缓存还提供损坏记录、格式迁移、锁等待和清理计数。统计是诊断数据，不代表跨进程或跨节点聚合结果。请求带认证信息时，必须同时传入 `requestContainsAuthorization: true` 和 `allowAuthenticated: true`，否则缓存会拒绝写入或回源缓存。需要条件回源时，可读取缓存快照中的 `ETag`/`Last-Modified`，通过现有 `.IfNoneMatch(...)`/`.IfModifiedSince(...)` 发起请求，并由调用方处理 `IsNotModified`。

可通过 `new HttpResponseCacheOptions { MaxEntries = 256 }` 设置条目容量；默认不限制。超限时先清理已过期条目，再淘汰最久未访问条目。未配置 `MaxBytes` 时缓存不估算响应对象的总字节数。

需要字节预算时，同时配置 `MaxBytes` 和 `SizeEstimator`；没有估算器会明确失败，估算结果必须为非负数，单条响应超过上限不会进入缓存。字节和条目容量同时配置时，两者任一超限都会按相同的过期优先、LRU 规则淘汰。

缓存失效必须由调用方显式触发。`RemoveAsync(key)` 移除单个键，`RemoveByPrefix(prefix)` 按大小写敏感的 `StringComparison.Ordinal` 前缀匹配批量移除并返回数量；两者都不会自动回源或取消其他请求。

需要清空一个缓存实例时可调用 `ClearAsync()`；它返回实际移除数量，内存缓存同时重置容量计数，文件缓存只处理当前根目录的 `.cache` 文件，保留 `.tmp`/`.lock` 文件并忽略记录损坏条目。文件缓存还提供显式的 `CleanupAsync(staleAfter, cancellationToken)`：清理当前目录顶层的过期 `.cache` 和超过保留时间的 `.tmp`，逐条目取得 key 锁后重新读取确认状态，返回 `FileHttpResponseCacheCleanupResult` 统计；锁等待响应取消令牌。需要一次性维护目录树时可调用 `CleanupRecursiveAsync`，它按目录使用同目录容量锁和 key 锁；默认入口仍不递归。为避免释放锁后删除产生跨进程竞态，`.lock` 和容量锁文件不会自动删除，也不会启动后台定时器。

```csharp
var removed = cache.RemoveByPrefix("orders:");
```

需要跨进程共享的 byte[] 响应可显式使用 `FileHttpResponseCache(directory)`。它拒绝空 key 和路径穿越，使用 key 哈希文件名、独占 lock 文件和临时文件原子替换；读取到过期或损坏文件时按未命中处理。也可通过 `FileHttpResponseCacheOptions` 设置本地目录的最大条目数或文件总字节数，超限时优先清理过期项，再按最久未访问顺序淘汰；容量治理只在当前缓存目录内提供，不代表跨节点一致性。文件格式当前写入 `BINGHTTP2` 并带显式版本号，读取仍兼容 `BINGHTTP1`；成功命中旧格式时会在键锁内原子迁移，未知版本按损坏文件处理并视为未命中。该缓存只保存 `HttpResponseResult<byte[]>`，不会自动接入 `HttpRequest`。

文件缓存测试会在 Windows 上启动独立 PowerShell 进程持有 lock 文件，验证跨进程双检和原子发布；没有可用 PowerShell 的目标会明确跳过该跨进程用例，同进程锁、TTL、损坏文件和失败回源仍独立验证。

需要中断文件缓存锁等待时，可直接调用 `FileHttpResponseCache.GetOrCreateAsync` 的 `CancellationToken` 重载，或对 `byte[]` 请求使用对应的 `GetCachedAsync` 重载；取消会抛出 `OperationCanceledException`，回源取消不会留下缓存文件。旧的无令牌接口保持原有行为。

需要由多个进程或节点共享响应时，可由宿主实现 `IDistributedHttpResponseStore`。该契约使用 `DistributedHttpResponseSnapshot` 表达状态码、头快照、`byte[]` 正文、可选原始正文和不透明版本令牌，并通过 TTL、`CompareAndSetAsync` 和带版本移除表达过期与并发写保护。契约不内置 Redis、数据库、网络协议、租约续期或故障恢复；一致性、时钟、序列化和跨节点可见性由宿主存储实现负责。

`InMemoryDistributedHttpResponseStore` 只提供进程内契约测试和单进程适配，不能作为分布式存储。`FileHttpResponseCache` 仍是本地文件缓存，即使多个进程可以通过 lock 文件协调，也不提供跨节点分布式一致性保证。上述存储均不会自动接入 `HttpRequest`，必须由调用方显式选择。对 `byte[]` 响应回源可使用分布式存储的 `GetCachedAsync` 或 `ExecuteCachedAsync`；这两个入口只保存不可释放的响应快照。实现 `IDistributedHttpResponseLeaseStore` 的存储可为显式 `ExecuteCachedAsync` 提供按键回源租约，扩展会在租约持有者写入后重新读取快照；旧存储安全回退到原有可能重复回源的行为。租约释放、续期、跨节点一致性和故障恢复仍由宿主实现负责，`InMemoryDistributedHttpResponseStore` 的租约仅用于进程内测试。
实现 `IDistributedHttpResponseNamespaceStore` 的存储还可为显式缓存入口提供 namespace 版本隔离；`InvalidateNamespaceAsync` 推进版本后旧快照不会命中，`CleanupNamespaceAsync` 可在保留当前版本的前提下物理清理旧版本。namespace 失效和清理仍由调用方显式触发，旧存储未实现该接口时不会自动获得此语义。

```csharp
var cache = new InMemoryHttpResponseCache<OrderDto>();
var cached = await cache.GetOrCreateAsync(
    "orders:42", TimeSpan.FromMinutes(1),
    () => client.Get<OrderDto>(url).GetResponseAsync());
```

需要把缓存接入请求时，调用方可显式使用 `GetCachedAsync`；它只缓存 `HttpResponseResult<TResult>` 的状态码、头快照、转换结果和可选正文，不保存可释放的 `HttpResponseMessage`。任意回源逻辑也可使用 `ExecuteCachedAsync`。认证请求必须显式设置 `allowAuthenticated: true`，失败或取消的回源不会写入缓存：

```csharp
var cached = await client.Get<OrderDto>(url).GetCachedAsync(
    cache, "orders:42", TimeSpan.FromMinutes(1));
var generated = await cache.ExecuteCachedAsync(
    "orders:43", TimeSpan.FromMinutes(1),
    () => client.Get<OrderDto>(otherUrl).GetResponseAsync());
```

## 8. 测试策略
- 单测覆盖点（tests/Bing.Utils.Http.Tests）：
    - `CookieHelper`：Get/Write/Clear 的正常/异常路径（name/context guard）。
        - 证据：tests/Bing.Utils.Http.Tests/Bing/Helpers/CookieHelperTest.cs | GetCookie_ShouldThrowArgumentException_WhenNameIsNullOrWhiteSpace / WriteCookie_ShouldThrowArgumentNullException_WhenHttpContextIsNull
    - `Web`：HttpContext 访问、AccessToken 解析、LocalIpAddress 等（大量覆盖点）。
        - 证据：tests/Bing.Utils.Http.Tests/Bing/Helpers/WebTest.cs | AccessToken_ShouldReturnToken_WhenBearerAuthorizationExists / LocalIpAddress_ShouldReturnLoopback_WhenExceptionOccurs
    - `IpAddressProvider/IpValidator`：SetIp/Reset/回退策略、IPv4/IPv6 验证与边界。
        - 证据：tests/Bing.Utils.Http.Tests/Bing/Net/IpAddressProviderTest.cs | SetIp_InvalidIp_ThrowsArgumentException / GetIp_WithManuallySetIp_ReturnsSetIp
        - 证据：tests/Bing.Utils.Http.Tests/Bing/Net/IPv4/IPv4ValidatorTest.cs | IsValid_LeadingZerosCases_ReturnsExpected
        - 证据：tests/Bing.Utils.Http.Tests/Bing/Net/IPv6/IPv6ConverterTest.cs | ToBytes_InvalidIPv6_ThrowsArgumentException
    - `UserAgentHelper`：OS/浏览器识别与空值异常、以及 MicroMessenger 误识别修复用例。
        - 证据：tests/Bing.Utils.Http.Tests/Bing/Helpers/UserAgentHelperTest.cs | GetOperatingSystemName_NullOrWhiteSpace_ThrowsArgumentException / GetOperatingSystemName_MicroMessengerBug_FixedCorrectly

- 集成测试覆盖点（tests/Bing.Utils.Http.Tests.Integration）：
    - `HttpClientService`：Get/Post/Put/Delete 基本调用与泛型结果反序列化。
        - 证据：tests/Bing.Utils.Http.Tests.Integration/Http/HttpClientServiceTest.Get.cs | Test_Get_1/Test_Get_3
        - 证据：tests/Bing.Utils.Http.Tests.Integration/Http/HttpClientServiceTest.Post.cs | Test_Post_1/Test_Post_2
        - 证据：tests/Bing.Utils.Http.Tests.Integration/Http/HttpClientServiceTest.Put.cs | Test_Put_1/Test_Put_2
        - 证据：tests/Bing.Utils.Http.Tests.Integration/Http/HttpClientServiceTest.Delete.cs | Test_Delete_1/Test_Delete_2
    - 请求构建链式能力：Header/QueryString/Cookie/Content/FileContent。
        - 证据：tests/Bing.Utils.Http.Tests.Integration/Http/HttpClientServiceTest.Get.cs | Test_Get_Header_1/Test_Get_QueryString_2/Test_Get_Cookie_2
        - 证据：tests/Bing.Utils.Http.Tests.Integration/Http/HttpClientServiceTest.Post.cs | Test_Post_Content_1/Test_Post_FileContent_3
    - 测试宿主：使用 `TestServer` 并注入 `HttpClientService` + `GetTestClient()`。
        - 证据：tests/Bing.Utils.Http.Tests.Integration/Startup.cs | UseTestServer/SetHttpClient/GetTestClient

- 边界用例：
    - 参数空值/空白的 guard（CookieHelper/UserAgentHelper/Web.Body 等）。
        - 证据：tests/Bing.Utils.Http.Tests/Bing/Helpers/CookieHelperTest.cs | GetCookie_ShouldThrowArgumentException_WhenNameIsNullOrWhiteSpace
        - 证据：tests/Bing.Utils.Http.Tests/Bing/Helpers/UserAgentHelperTest.cs | GetOperatingSystemName_NullOrWhiteSpace_ThrowsArgumentException
    - IPv4 前导零/段数验证。
        - 证据：tests/Bing.Utils.Http.Tests/Bing/Net/IPv4/IPv4ValidatorTest.cs | IsValid_InvalidIPv4Addresses_ReturnsFalse

- 回归用例：
    - 已覆盖 MicroMessenger 误识别、`OnSendBefore` 拒绝发送，以及短/缺失 User-Agent。
        - 证据：tests/Bing.Utils.Http.Tests/Bing/Helpers/UserAgentHelperTest.cs | GetOperatingSystemName_MicroMessengerBug_FixedCorrectly
        - 证据：tests/Bing.Utils.Http.Tests/Bing/Http/HttpCapabilityExtensionsTest.cs | GetResponseAsync_SendBeforeRejectsWithoutRunningResponseCallbacks
        - 证据：tests/Bing.Utils.Http.Tests/Bing/Http/HttpRequestExtensionsTest.cs | IsMobileBrowser

## 9. 版本与兼容性
- 当前版本：1.5.0
    - 证据：version.props | VersionMajor/VersionMinor/VersionPatch
- 破坏性变更：待确认（本文件仅基于当前仓库状态，未对历史版本进行比对）。
- 升级建议：
    - 目标框架差异：
        - `Web.Url` 在 NETSTANDARD2_1 下不支持（抛 `NotSupportedException`）。
            - 证据：src/Bing.Utils.Http/Bing/Helpers/Web.cs | Url（#if NETSTANDARD2_1）
        - `SessionExtensions.Get<T>` 在 NETSTANDARD2_1 下返回 default（等价不可用）。
            - 证据：src/Bing.Utils.Http/Bing/Http/Extensions/Extensions.Session.cs | Get<T>（#if NETSTANDARD2_1）
    - `Certificate(...)`、`IgnoreSsl()`、`UseCookies(...)` 现在显式抛 `NotSupportedException`；在注册命名客户端时配置对应 Handler。

## 10. 使用示例
```csharp
using Bing.Http;

// IHttpClient 的具体注入方式取决于宿主（示例：在 ASP.NET Core 中注入实现为 HttpClientService）
IHttpClient client = /* resolve from DI */ null;

// 1) GET + QueryString
var list = await client
    .Get<List<CustomerDto>>("/api/test1/list")
    .QueryString(new { Code = "a", Name = "b" })
    .GetResultAsync();

// 2) POST JSON + Bearer
var created = await client
    .Post<CustomerDto>("/api/test2", new CustomerDto { Code = "a" })
    .BearerToken("token")
    .GetResultAsync();

// 3) 上传文件（multipart/form-data）
var result = await client
    .Post("/api/test6")
    .FileContent("c:/temp/a.png", "file1")
    .Content("util", "core")
    .GetResultAsync();

// 示例证据：
// - tests/Bing.Utils.Http.Tests.Integration/Http/HttpClientServiceTest.Get.cs | Test_Get_3
// - tests/Bing.Utils.Http.Tests.Integration/Http/HttpClientServiceTest.Post.cs | Test_Post_2/Test_Post_FileContent_4
```

## 11. 待办与改进
- [x] `HttpRequestExtensions.IsMobileBrowser` 对短或缺失 User-Agent 返回 false，并有边界测试。
    - 证据：src/Bing.Utils.Http/Bing/Http/Extensions/Extensions.HttpRequest.cs | IsMobileBrowser（Substring(0,4)）；tests/Bing.Utils.Http.Tests/Bing/Http/HttpRequestExtensionsTest.cs
- [x] 仅设置 `HttpRequest<TResult>.OnSendBefore` 时可取消发送，并释放请求拥有的上传流。
    - 证据：tests/Bing.Utils.Http.Tests/Bing/Http/HttpRequestResilienceTest.cs | SendBeforeRejectsRequest_DisposesOwnedUploadStream
- [x] 文件上传支持流式传输，避免整包读入内存。
    - 证据：src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs | AddFileData/StreamContent
- [x] 补充 Http 模块流式上传与 JSON 响应基准；具体负载和测量版本见 [HTTP 优化性能报告](../quality/http-optimization-benchmark-2026-10-04.md)。

- [x] 断点续传侧车文件支持显式过期清理，并在锁内重新扫描侧车文件；活动下载跳过，锁文件始终保留。
    - 证据：src/Bing.Utils/Bing/Http/HttpResumableDownloadMaintenance.cs | CleanupAsync；tests/Bing.Utils.Http.Tests/Bing/Http/HttpResumableDownloadMaintenanceTest.cs
- [x] 断点续传支持显式递归清理；默认 CleanupAsync 仍只扫描传入目录顶层，不启动后台调度，锁文件始终保留。
    - 证据：src/Bing.Utils/Bing/Http/HttpResumableDownloadMaintenance.cs | CleanupRecursiveAsync；tests/Bing.Utils.Http.Tests/Bing/Http/HttpResumableDownloadMaintenanceTest.cs
- [ ] 断点续传自动后台清理和锁文件回收策略暂不启用；当前由宿主按计划调用清理 API。
- [x] 文件响应缓存支持显式递归清理；默认 `CleanupAsync` 仍只扫描当前目录，递归入口保留各目录容量锁、key 锁及临时锁文件。
    - 证据：src/Bing.Utils.Http/Bing/Http/Caching/FileHttpResponseCache.cs | CleanupRecursiveAsync；tests/Bing.Utils.Http.Tests/Bing/Http/FileHttpResponseCacheTest.cs
- [x] GitHub Actions 在 main 分支 Pull Request 阶段自动运行构建、依赖审计和测试。
    - 证据：.github/workflows/dotnet.yml | pull_request
- [ ] 正式二进制 API baseline/API diff 门禁仍需在发布流程中选定工具并冻结基线。

## 12. 证据定位（汇总）
- src/Bing.Utils.Http/Bing.Utils.Http.csproj | 包描述（Http操作类库）
- src/Bing.Utils.Http/references.props | ProjectReference -> Bing.Utils
- src/Bing.Utils.Http/dependency.props | 按 TargetFramework 条件引用 ASP.NET Core 依赖
- src/Bing.Utils/Bing/Http/IHttpClient.cs | IHttpClient
- src/Bing.Utils/Bing/Http/IHttpRequest.cs | IHttpRequest<TResult>
- src/Bing.Utils/Bing/Http/HttpContentType.cs | HttpContentType
- src/Bing.Utils.Http/Bing/Http/Clients/HttpClientService.cs | HttpClientService.Get/Post/Put/Delete
- src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs | HttpRequest<TResult>（构建/发送/解析/文件上传/证书/忽略SSL/回调）
- src/Bing.Utils.Http/Bing/Http/Extensions/Extensions.HttpRequest.cs | HttpRequestExtensions
- src/Bing.Utils.Http/Bing/Http/Extensions/Extensions.HttpResponse.cs | HttpResponseExtensions
- src/Bing.Utils.Http/Bing/Http/Extensions/HttpResponseMessageExtensions.cs | HttpResponseMessageExtensions.GetContentType
- src/Bing.Utils.Http/Bing/Http/Extensions/Extensions.Session.cs | SessionExtensions
- src/Bing.Utils.Http/Bing/Helpers/Web.cs | Web（HttpContext/Body/Url/GetFile/GetFiles/GetParam/UrlEncode 等）
- src/Bing.Utils.Http/Bing/Helpers/CookieHelper.cs | CookieHelper
- src/Bing.Utils.Http/Bing/Helpers/Ip.cs | Ip（委托 IpAddressProvider）
- src/Bing.Utils.Http/Bing/Net/IpAddressProvider.cs | IpAddressProvider（AsyncLocal + 公网 IP 获取）
- src/Bing.Utils.Http/Bing/Net/IpValidator.cs | IpValidator（IPv4/IPv6 通用验证）
- src/Bing.Utils.Http/Bing/Helpers/UserAgentHelper.cs | UserAgentHelper（OS/Browser/Wechat + 字典与扩展）
- src/Bing.Utils.Http/Bing/Parameters/UrlParameterBuilderExtensions.cs | UrlParameterBuilderExtensions.LoadForm/LoadQuery
- tests/Bing.Utils.Http.Tests.Integration/Startup.cs | TestServer + IHttpClient 注入
- tests/Bing.Utils.Http.Tests.Integration/Http/HttpClientServiceTest.*.cs | Get/Post/Put/Delete 集成用例
- tests/Bing.Utils.Http.Tests/Bing/Helpers/*.cs | Web/Cookie/UserAgent 单测
- tests/Bing.Utils.Http.Tests/Bing/Net/**/*.cs | IpAddressProvider/IPv4/IPv6/NetworkInformation 单测

## 13. 通用请求、完整响应与拦截器

通用方法入口通过可选的 `IHttpRequestFactory` 创建请求，另提供 PATCH、HEAD 和 OPTIONS 快捷方法。既有 `IHttpClient` 实现无需为此增加成员；未实现该能力时调用对应扩展会抛出 `NotSupportedException`。前阶段已在 `IHttpRequest<TResult>` 增加必需成员，第三方请求实现者须按[迁移说明](http-optimization-migration.md)补齐；本阶段的响应和拦截器 API 使用可选能力接口，不再增加必需成员。

```csharp
var response = await httpClient
    .Patch<CustomerDto>("/api/customer/42", new { Name = "Ada" })
    .GetResponseAsync(cancellationToken: cancellationToken);

if (!response.IsSuccessStatusCode)
    logger.LogWarning("HTTP {StatusCode}", response.StatusCode);
```

`HttpResponseResult<TResult>` 提供状态码、成功状态、响应头与内容头快照、转换后的 `Data` 和可选的 `RawContent`。原始正文默认不保留；HTTP 非成功状态返回结果并将 `Data` 设为 `null`，取消、网络错误、解析失败及响应大小超限仍以异常表示。头快照在响应释放后仍可读取。若 `OnSendBefore` 返回 false，`GetResponseAsync` 返回 null，且不执行响应回调。需要排查错误正文时可显式设置 `includeRawContent: true`，但不要把正文写入常规日志。新入口复用现有转换和回调；处理异常与 `OnComplete` 同时失败时优先传播处理异常。请求正文处理已有异常时，后续资源清理仍会继续，清理异常不会替换原始处理异常；没有更早异常时传播首个清理异常。与自定义 `OnSendAfter` 同时配置会在发送前抛出 `NotSupportedException`。

HEAD 和 204 在各正文读取及下载入口都跳过正文和描述性 `Content-Length` 限额检查。普通 `GetResultAsync` 返回 null，并向成功回调传入 null；`GetStreamAsync` 返回空字节数组，`DownloadToAsync` 不写目标流。`OnComplete` 仍执行，收到的正文为 `string.Empty`（普通结果）或空字节数组（字节入口）。

拦截器可异步添加签名或鉴权，并观察发送状态。发送前按注册顺序执行，发送后按逆序执行；重试时会重新执行发送前逻辑。上下文提供实际请求、尝试次数、状态码、响应头快照、耗时及发送异常，不提供响应正文流。日志示例只输出方法、去除 query 的路径、状态、尝试次数和耗时：

拦截器上下文中的 `Elapsed` 从发送前检查开始计时，到取得响应头或发送失败为止，不包含前置/后置拦截器和响应正文读取、解析及业务回调。需要整次请求耗时时，使用下文的 `ObserveCompletion`。

拦截器替换 `context.Request.Content` 时，请求管线会在每次替换后释放旧正文，并在本次尝试结束时释放最终正文。替换后的正文由请求管线持有，拦截器不能在回调结束后继续使用它；启用自动重试时，替换正文会因无法保证重放而在发送前被拒绝。

流式下载可使用带结果的可选能力，取得响应状态、响应头快照、实际写入字节数和文件保存状态：

~~~csharp
var download = await httpClient.Get<CustomerDto>(fileUrl)
    .MaxResponseBytes(64L * 1024 * 1024)
    .DownloadFileWithResultAsync(filePath, cancellationToken);

if (download is { IsSuccessStatusCode: true, IsFileSaved: true })
    logger.LogInformation("下载完成：{StatusCode}，写入 {BytesWritten} 字节",
        download.StatusCode, download.BytesWritten);
else if (download is not null)
    logger.LogWarning("下载未成功：{StatusCode}", download.StatusCode);
~~~

`DownloadToWithResultAsync(Stream, CancellationToken)` 将成功响应写入调用方提供的可写流并返回相同元数据；流由调用方持有，方法不会关闭它。`DownloadFileWithResultAsync(string, CancellationToken)` 通过同目录临时文件保存，成功后替换目标文件；非成功 HTTP 状态返回结果且 `IsFileSaved` 为 false，发送前拒绝时返回 null。网络、取消、读取或文件操作异常仍通过任务抛出。头快照在响应释放后仍可用，`BytesWritten` 是实际写入量；不要把响应正文或敏感头写入日志。


### 断点续传文件下载

维护清理取得目标锁后会重新扫描并确认片段、状态和临时文件仍然过期，再执行删除；因此清理扫描期间恢复或更新的侧车文件会被保留。清理等待目标锁时响应传入的取消令牌，活动下载和锁文件本身均保留。

需要跨进程或跨请求恢复大文件时，可使用新增的可选能力：

```csharp
var result = await request.DownloadFileResumableWithResultAsync(filePath, cancellationToken);
```

该入口只接受 GET，通过目标文件同目录的 .binghttp.lock、.binghttp.part 和 .binghttp.resume.json 保存锁、片段及状态，不改变既有 DownloadFileAsync/DownloadFileWithResultAsync 行为。可由宿主按计划调用 `HttpResumableDownloadMaintenance.CleanupAsync(directory, staleAfter, cancellationToken)` 清理崩溃遗留的侧车文件；它会先独占目标锁，活动下载会被跳过，锁文件本身始终保留。同一目标文件的另一个续传请求会因独占锁快速失败；锁文件可长期保留，进程退出后文件句柄释放即可再次续传。真实独立进程持锁场景已有集成测试覆盖。状态仅在响应提供强验证器（非弱 ETag 或 Last-Modified）时保留；缺少安全验证器、状态损坏或目标身份变化时会删除片段并从头下载。恢复请求固定发送 Accept-Encoding: identity、Range 和 If-Range，服务端返回 206 时追加，返回 200 时安全地从头覆盖，完整片段收到匹配的 416 时直接提交。状态还保存请求头指纹（仅 SHA-256，不落盘头值）和响应 Content-Encoding，并带有 `SchemaVersion`；缺失该字段的旧状态按版本 0 兼容读取，成功写回时升级为当前版本，未知或未来版本会删除侧车并从头下载。关键请求头或编码变化时拒绝追加。下载异常会保留可验证片段供下次调用，成功提交后删除状态文件；结果的 IsFileSaved 表示目标文件已提交。失败 HTTP 响应返回的 BytesWritten 表示保留的 .part 长度；匹配的 416 会保留 StatusCode=416 和 IsSuccessStatusCode=false，但 IsFileSaved=true 表示本地提交成功。

文件下载在关闭同目录临时文件后、替换或移动目标前检查调用方取消令牌；此时取消会抛出 `OperationCanceledException`、保留已有目标并清理临时文件。请求 `Timeout` 覆盖 HTTP 发送和正文读取，不覆盖最终同步文件提交。提交开始后不可中断，提交前的取消检查与实际替换/移动之间也存在很短的竞态。

旧 `WriteAsync` 未配置 `OnComplete`/`OnFail` 正文回调时使用流式临时文件路径；配置正文回调后会先将响应缓冲为 `byte[]`，再直接写入目标文件。该路径发生写入失败或取消时可能已截断或破坏原文件；需要在失败或提交前取消时保护原文件，请使用 `DownloadFileWithResultAsync`。

整次请求结束观测使用可选注册扩展。回调只接收状态码、实际发送尝试数、耗时、结果分类和传播给调用方的异常，不包含 URL、请求头或正文；回调失败可由第二个委托接收，观测及错误处理委托抛出的异常都不会替换请求结果：

~~~csharp
var request = httpClient.Get<CustomerDto>(resourceUrl)
    .ObserveCompletion(completion =>
    {
        logger.LogInformation("HTTP {StatusCode} {Outcome}，{Attempts} 次尝试，耗时 {ElapsedMs} ms",
            completion.StatusCode, completion.Outcome, completion.Attempts, completion.Elapsed.TotalMilliseconds);
    }, observerError => logger.LogWarning("HTTP 观测回调失败：{ErrorType}", observerError.GetType().Name));

var response = await request.GetResponseAsync(cancellationToken: cancellationToken);
~~~

`ObserveCompletion` 返回原请求对象，可继续链式配置。同一请求重复注册时后一次注册会替换前一次的观测回调和错误处理回调，不会累加。观测是同步委托，在请求清理结束后、返回任务完成前执行；耗时或阻塞的观测会延长调用方看到的完成时间，建议只做轻量数据封装并投递到宿主队列，再异步写日志或指标。观测回调抛出的异常传给 `onObserverError`；未提供错误处理委托时忽略该异常，错误处理委托自身异常也会被隔离，不改变请求结果。`Elapsed` 覆盖请求准备、发送/重试、响应处理和资源清理；`WriteAsync` 的耗时还包括文件最终写入。`Attempts` 只计实际开始的发送次数。`Outcome` 为 `Success`、`HttpFailure`、`Rejected`、`Cancelled`、`TimedOut` 或 `Faulted`；未收到响应时 `StatusCode` 为空，正常完成时 `Exception` 为空。日志示例只输出结构化状态，不记录正文、认证/签名头、query 参数或异常消息。

`HttpDownloadResult` 和 `HttpRequestCompletion` 提供公共构造函数，第三方实现可按相同契约创建可选能力结果。构造 `HttpDownloadResult` 时会防御性复制响应头和内容头：字典大小写不敏感且只读，值列表也独立复制并只读；后续修改传入字典或列表不会改变结果快照。

```csharp
using Bing.Http;
using System.Net.Http;
using Microsoft.Extensions.Logging;

// 这些依赖由应用的 DI 容器提供。
var request = httpClient.Get<CustomerDto>("https://api.example.com/api/customer/42")
    .UseInterceptor(new RequestSignatureInterceptor(apiSigner))
    .UseInterceptor(new RequestObservationInterceptor(logger));
var response = await request.GetResponseAsync(cancellationToken: cancellationToken);

public interface IApiSigner
{
    Task<string> SignAsync(HttpMethod method, Uri requestUri, CancellationToken token);
}

public sealed class RequestSignatureInterceptor : HttpRequestInterceptor
{
    private readonly IApiSigner _signer;

    public RequestSignatureInterceptor(IApiSigner signer) => _signer = signer;

    public override async Task BeforeSendAsync(HttpRequestInterceptorContext context, CancellationToken token)
    {
        var requestUri = context.Request.RequestUri ?? throw new InvalidOperationException("The request URI is missing.");
        var signature = await _signer.SignAsync(context.Request.Method, requestUri, token);
        context.Request.Headers.TryAddWithoutValidation("X-Signature", signature);
    }
}

public sealed class RequestObservationInterceptor : HttpRequestInterceptor
{
    private readonly ILogger<RequestObservationInterceptor> _logger;

    public RequestObservationInterceptor(ILogger<RequestObservationInterceptor> logger) => _logger = logger;

    public override Task AfterSendAsync(HttpRequestInterceptorContext context, CancellationToken token)
    {
        var path = context.Request.RequestUri is { IsAbsoluteUri: true } uri ? uri.AbsolutePath : null;
        _logger.LogInformation("HTTP {Method} {Path} {StatusCode} attempt {Attempt} in {ElapsedMs} ms",
            context.Request.Method, path, context.StatusCode, context.Attempt, context.Elapsed.TotalMilliseconds);
        return Task.CompletedTask;
    }
}
```

`IApiSigner` 是宿主提供的签名服务；其规范化规则和密钥管理应遵循目标 API 的要求。示例使用绝对 URL，使签名阶段取得最终请求地址；如果只给注入的 `HttpClient.BaseAddress` 和相对 URL，发送前拦截器看到的仍是相对地址。签名依赖绝对地址时，可给请求设置 `BaseAddress(...)`，或由签名服务按相同规则解析地址。自动重定向发生在 Handler 内，不会再次运行本请求的签名拦截器；本地 Kestrel 重定向测试验证了这点。需要逐跳签名的请求应关闭自动重定向并由调用方显式处理。观测日志不得包含正文、认证/签名头、query 参数或密钥。扩展方法运行时检查可选能力接口 `IHttpRequestFactory`、`IHttpRequestResponse<TResult>`、`IHttpRequestInterceptorRegistration<TResult>`、`IHttpRequestDownloadResult<TResult>`、`IHttpRequestObservationRegistration<TResult>` 和 `IHttpRequestUploadProgressRegistration<TResult>`；未实现相应可选接口时，调用扩展会明确抛出 `NotSupportedException`。

### 性能证据

HTTP 基准的提交、候选程序集、运行时、负载、样本和限制统一记录在[HTTP 优化性能报告](../quality/http-optimization-benchmark-2026-10-04.md)。报告分别记录前阶段的流式文件上传与 JSON 响应解析、普通拦截器路径、原文保留开关及响应头快照单次复制的复测；各结果应按报告中的测量版本解读。测试基于 Windows、.NET 8.0.30、Release 与本地 Handler，不代表真实网络吞吐，也未用于设定性能门槛或与 SKIT 比较。

## 14. 二进制正文、上传/下载进度与流式 JSON

以下 API 分别由可选接口 `IHttpRequestRawContentRegistration<TResult>`、`IHttpRequestUploadProgressRegistration<TResult>`、`IHttpRequestDownloadProgressRegistration<TResult>` 和 `IHttpRequestStreamingJson<TResult>` 提供；旧版 `IHttpRequest<TResult>` 实现无需增加必需成员。具体实现不具备对应能力时，扩展方法会抛出 `NotSupportedException`。

### 二进制请求正文

`BinaryContent` 设置原始二进制请求正文，不会封装成 multipart。字节数组在注册时复制，之后修改源数组不会改变请求正文。流重载在注册时记录当前 `Position`；每次发送尝试前都从该位置读取。默认 `leaveOpen: false`，请求配置拥有该流并在替换、拒绝发送、成功、取消或失败清理时释放；`leaveOpen: true` 时调用方继续拥有流。

~~~csharp
var payload = new byte[] { 0x01, 0x02, 0x03 };
var request = client.Put<UploadReceipt>(endpoint)
    .BinaryContent(payload, "application/octet-stream");

payload[0] = 0xFF; // 已注册正文仍保留原字节
var receipt = await request.GetResultAsync(cancellationToken);
~~~

也可用 `ContentFactory(Func<HttpContent>)` 为每次发送尝试创建独立正文：

~~~csharp
var body = new byte[] { 0x01, 0x02, 0x03 };
var request = client.Put<UploadReceipt>(endpoint)
    .ContentFactory(() => new ByteArrayContent(body));
~~~

工厂每次必须返回新的、非 null 的 `HttpContent`；单次执行中重复返回同一实例会在发送前被拒绝。工厂创建的正文由请求管线释放。自动重试资格按实际 HTTP 方法判断；只有允许重试的方法会重放正文。不可定位流在实际方法允许重试时会于发送前被拒绝。二进制正文与对象/参数正文或 multipart 文件正文不能混用，设置冲突会在创建请求正文时明确失败。需要重试的二进制内容应使用可从记录位置重置的流或每次返回新内容的工厂。

重复执行同一请求时，默认拥有的二进制流在第一次执行清理后已释放，应重新设置正文；若需保留流，可使用 `leaveOpen: true` 并由调用方最终释放。可定位流在每次执行前回到注册位置，不可定位流不会自动复原。字节数组正文或每次返回全新内容的正文工厂也适用于顺序重复执行；请求实例包含可变配置，不能并发执行或在执行中修改正文配置。

### 上传进度

使用 `ObserveUploadProgress` 注册同步上传进度回调。进度包装最终发送的 `HttpContent`，因此保留原正文 headers 和长度计算，并按实际写入传输目标流的字节计数；它表示本地写入进度，不表示服务器已接收或持久化。每次发送尝试都有独立的 `Attempt` 和从零开始的 `BytesWritten`，自动重试不会复用上一尝试的累计值。

默认每累计至少 65,536 字节且间隔至少 100 毫秒才发送一次中间通知；两个阈值需同时满足。每次尝试都会先通知零字节初始状态，正文成功写完后通知 `IsCompleted=true` 的最终状态；已知正文长度会填充 `TotalBytes`，未知长度为 `null`。空正文仍会收到初始和最终两个零字节通知。

~~~csharp
var request = client.Put<UploadReceipt>(endpoint)
    .BinaryContent(fileBytes)
    .ObserveUploadProgress(progress =>
    {
        logger.LogInformation("上传尝试 {Attempt}: {BytesWritten}/{TotalBytes}，完成={IsCompleted}",
            progress.Attempt, progress.BytesWritten, progress.TotalBytes, progress.IsCompleted);
    }, minimumBytes: 256 * 1024, minimumInterval: TimeSpan.FromMilliseconds(250));

var receipt = await request.GetResultAsync(cancellationToken);
~~~

进度委托同步执行，异常会终止当前发送并通过请求任务传播；模块仍会释放包装正文和原正文。取消、正文读取失败或回调异常不会发送完成通知。使用 `ContentFactory` 时，工厂正文照常按尝试创建并释放，上传进度包装在每次实际发送时重新创建。回调应保持轻量，日志只记录尝试号、字节数和完成状态，不写 URL、query、认证头或正文。

### 下载进度

使用 `ObserveDownloadProgress` 注册同步进度回调。默认每累计至少 65,536 字节且间隔至少 100 毫秒才发送一次中间通知；两个阈值需同时满足。成功下载会通知初始进度和正文 EOF 的最终进度；未知总长度时 `TotalBytes` 为 null，HEAD/204 的总量为 0。仅在目标流成功写入后增加 `BytesWritten`。

进度委托在下载流程中直接同步执行，库不保证回调位于调用方线程或 UI 线程，也不负责切换到 UI 调度器。桌面应用更新界面时，应由调用方通过对应的 Dispatcher 或 SynchronizationContext 调度；投递后的 UI 更新可能晚于下载任务完成。回调应只做轻量处理，耗时工作会延长下载时间；异步投递后的异常由调用方处理，不属于下载回调的同步异常传播范围。

~~~csharp
var request = client.Get<byte[]>(fileUrl)
    .ObserveDownloadProgress(progress =>
    {
        logger.LogInformation("下载进度 {BytesWritten}/{TotalBytes}，EOF={IsCompleted}",
            progress.BytesWritten, progress.TotalBytes, progress.IsCompleted);
    }, minimumBytes: 256 * 1024, minimumInterval: TimeSpan.FromMilliseconds(250));

var result = await request.DownloadFileWithResultAsync(filePath, cancellationToken);
if (result is { IsFileSaved: true })
    logger.LogInformation("文件已保存，共 {BytesWritten} 字节", result.BytesWritten);
~~~

`IsCompleted` 只表示响应正文已读到 EOF，不表示临时文件已提交为目标文件；文件下载是否完成应检查 `HttpDownloadResult.IsFileSaved`。进度回调异常会中止下载并通过任务传播，模块仍会执行资源清理；正文未到 EOF 时发生异常或取消不会通知 `IsCompleted=true`，EOF 后的文件保存失败或取消则可能已经发出该通知。错误响应正文和普通正文读取不发下载进度。未注册进度时不会创建进度状态或计时器。回调应保持轻量；日志只记录字节数和状态，不写 URL、query、敏感头或正文。

### 逐项 JSON 数组消费

GetJsonArrayStreamAsync 返回 IAsyncEnumerable<TResult>，只接受 UTF-8 的顶层 JSON 数组（application/json、+json 或未声明媒体类型）。它使用响应头完成模式和增量 Utf8JsonReader，数组元素在解析完成后立即产生，调用方不需要先构造完整集合。

该枚举只能消费一次；HTTP/JSON/读取异常通常在第一次或后续 MoveNextAsync 时抛出。开始消费后不执行自动重试，避免已经产生的元素与重试响应混合。取消令牌、提前 DisposeAsync、解析失败和大小限额异常都会释放响应、正文和底层流。MaxResponseBytes 仍限制实际读取的响应字节数，单个巨大元素仍可能占用与元素大小相当的内存。

为避免完整响应回调在“响应头已返回但正文尚未消费完”时产生歧义，该入口不与 OnSuccess、OnConvert、OnFail、OnComplete、OnSendAfter 或完成观测同时使用。

示例：await foreach (var item in request.GetJsonArrayStreamAsync(cancellationToken)) { await HandleItemAsync(item); }

### 流式 JSON 响应

`GetJsonStreamAsync` 是显式的直接流式 JSON 入口：对于成功的 UTF-8 `application/json` 或 `+json` 响应，在结果类型不是 `string` 且没有 `OnConvert`、`OnFail`、`OnComplete` 正文回调时，直接从响应流异步反序列化，沿用现有 `JsonSerializerOptions`，支持 UTF-8 BOM。请求选项和正文限额继续生效，不需要先构造完整字节数组或字符串。

该入口返回完整的 `TResult` 对象，节省的是完整响应字节数组和字符串的中间缓冲。大型 JSON 数组反序列化为集合时，集合及其元素仍会全部构造并占用内存；`MaxResponseBytes` 限制读取的正文大小，不限制对象图的内存占用。需要逐项处理时使用上方的 GetJsonArrayStreamAsync；单个巨大元素仍可能占用与元素大小相当的内存。

~~~csharp
var value = await client.Get<CustomerDto>(endpoint)
    .GetJsonStreamAsync(cancellationToken);
~~~

以下情况回退到既有正文读取与转换路径：响应不是 UTF-8 JSON、配置了 `OnConvert`/`OnFail`/`OnComplete`、结果类型为 `string`，或收到非成功状态。回退保留原编码处理、错误回调和转换行为；成功但非 JSON 的响应也按既有转换规则处理，不推测其格式。直接解析时 HEAD、204、空正文和空白正文均返回 null；回退时继续保留原转换规则，例如 string 结果保留空字符串，自定义转换仍可处理空正文。需要保留解码后的原始正文时仍使用 `GetResponseAsync(includeRawContent: true)`；该选项不会与直接流式解析合并。

下载进度用于新旧 `DownloadToAsync`/`DownloadFileAsync` 及带结果入口；配置正文回调的旧 `WriteAsync` 采用缓冲后写文件的兼容路径，不产生下载进度。

## 15. 文本响应读取与重试等待观测

### 文本响应缓冲复用

`GetResultAsync` 的默认文本处理和 `GetResponseAsync` 现在直接在同一个 `MemoryStream` 缓冲上解码，减少了原先 `ToArray` 生成完整正文数组后重新包装的复制。字符集、BOM、零字节正文、转换和正文回调语义保持不变，正文复制仍检查 `MaxResponseBytes`、取消和实际读取量。

这些入口仍缓冲完整响应，并构造完整字符串及转换结果；此次调整不提供恒定内存或逐项 JSON 消费。`GetStreamAsync` 的 `byte[]` 返回契约保持不变，自定义 `OnSendAfter` 的缓冲适配也沿用既有行为。

### 重试等待观测

通过可选接口 `IHttpRequestRetryObservationRegistration<TResult>` 提供的 `ObserveRetry` 可以观察已经接受的重试等待计划，不向旧 `IHttpRequest<TResult>` 增加必需成员。不支持该能力的实现调用扩展时抛出 `NotSupportedException`；请求对象或观察委托为 null 时抛出 `ArgumentNullException`。注册观察者不会启用重试，仍须显式调用 `Retry` 配置策略。

~~~csharp
using Bing.Http;

var value = await client.Get<CustomerDto>(endpoint)
    .Retry(new HttpRetryOptions { MaxRetries = 2 })
    .ObserveRetry(retry =>
    {
        logger.LogInformation("HTTP 第 {FailedAttempt} 次发送后计划等待 {DelayMs} ms，状态 {StatusCode}，异常类型 {ErrorType}",
            retry.FailedAttempt, retry.Delay.TotalMilliseconds, retry.StatusCode,
            retry.Exception?.GetType().Name);
    }, error => logger.LogWarning("重试观测回调失败：{ErrorType}", error.GetType().Name))
    .GetResultAsync(cancellationToken);
~~~

不可变的 `HttpRetryScheduled.FailedAttempt` 是刚失败的发送尝试序号，从 1 开始。`Delay` 是现有指数退避、抖动及 `Retry-After` 决策后的最终计划等待时间，不是实际等待耗时；零延迟等待也可产生事件。状态码重试携带 `StatusCode`，`Exception` 为 null；发送异常重试携带原始异常引用，`StatusCode` 为 null。

响应成功释放且取消检查通过后，库在进入等待之前同步通知一次。事件只表示已接受这次等待，不保证下一次发送会发生；等待中取消可以阻止后续发送。重试次数用尽、实际方法不允许重试、证书失败、剩余超时预算不足或等待时长不可表达时均不通知；响应释放失败或已经观察到取消时也不会通知。

一次执行开始时固定观察者与错误处理回调的快照，执行中重新注册只影响下一次执行。同一请求重复注册会覆盖原观察者和错误处理回调，不累加。观察者异常交给可选 `onObserverError`；未配置错误处理时忽略观察者异常，错误处理自身异常也被隔离，两者都不改变请求结果。整次操作的最终结果仍可通过 `ObserveCompletion` 观察一次。

回调直接同步执行，不保证调用方线程或 UI 线程；UI 更新由调用方自行调度。耗时回调会延长请求时间，其耗时包含在整次请求观测中，建议只做轻量记录或投递。未注册时不创建重试事件对象。事件没有 URI、响应头或正文；异常保留原始对象引用，可能含有底层信息，导出日志时由调用方选择字段，示例只记录异常类型。
