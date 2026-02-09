# 模块：Bing.Utils.Http

## 1. 模块定位
- 目标：提供 HTTP 客户端的轻量封装（Get/Post/Put/Delete + 链式请求构建 + 统一解析/回调/超时/证书/忽略 SSL 等），并补充 ASP.NET Core 请求/响应/会话的常用扩展与 Web 辅助能力。
    - 证据：src/Bing.Utils.Http/Bing/Http/Clients/HttpClientService.cs | Bing.Http.Clients.HttpClientService.Get/Post/Put/Delete
    - 证据：src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs | Bing.Http.Clients.HttpRequest<TResult>（链式构建 + GetResultAsync/GetStreamAsync/WriteAsync）
    - 证据：src/Bing.Utils.Http/Bing/Http/Extensions/Extensions.HttpRequest.cs | Bing.Http.HttpRequestExtensions
    - 证据：src/Bing.Utils.Http/Bing/Http/Extensions/Extensions.HttpResponse.cs | Bing.Http.HttpResponseExtensions
    - 证据：src/Bing.Utils.Http/Bing/Helpers/Web.cs | Bing.Helpers.Web（HttpContext/参数/上传文件/Url 编码等）
- 非目标：不提供完整“API SDK 生成/管理平台”、不提供请求重试/熔断/限流等网关治理能力（当前实现更聚焦于请求构建与结果解析/回调）。
    - 证据：src/Bing.Utils.Http/Bing.Utils.Http.csproj | Description=Http操作类库（未声明治理/编排能力）
    - 证据：src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs | SendAsync 直接调用 HttpClient.SendAsync（未见重试/熔断实现）
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
    - 需要严格的移动端识别与健壮性保障（`IsMobileBrowser` 对 UserAgent 长度做了 `Substring(0, 4)`，未见长度 guard，可能抛异常，见边界说明）。
        - 证据：src/Bing.Utils.Http/Bing/Http/Extensions/Extensions.HttpRequest.cs | HttpRequestExtensions.IsMobileBrowser

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
| `IHttpRequest<TResult>.Cookie(...)` / `UseCookies(...)` | 设置 Cookie（以请求头 Cookie 发送）/是否自动携带 cookie | `key/value` / `isUseCookies` | `IHttpRequest<TResult>` |  | 
| `IHttpRequest<TResult>.Content(...)` | 添加请求内容参数（表单/JSON/XML 的数据来源） | `key/value` / `IDictionary` / `object` | `IHttpRequest<TResult>` |  | 
| `IHttpRequest<TResult>.JsonContent(...)` / `XmlContent(...)` | 设置内容类型并写入内容 | `object`/`string` | `IHttpRequest<TResult>` |  | 
| `IHttpRequest<TResult>.FileContent(...)` | 设置 multipart/form-data 并添加文件（路径或 Stream） | `filePath/name` 或 `stream/fileName/name` | `IHttpRequest<TResult>` |  | 
| `IHttpRequest<TResult>.Timeout(...)` | 设置 HttpClient 超时 | `int`（秒）/`TimeSpan` | `IHttpRequest<TResult>` |  | 
| `IHttpRequest<TResult>.Certificate(...)` / `IgnoreSsl()` | 客户端证书与忽略 SSL 验证 | `path/password` | `IHttpRequest<TResult>` |  | 
| `IHttpRequest<TResult>.OnSendBefore/OnSendAfter/OnConvert/OnSuccess/OnFail/OnComplete` | 生命周期回调（发送前/后、转换、成功/失败/完成） | delegates | `IHttpRequest<TResult>` |  | 
| `IHttpRequest<TResult>.GetResultAsync()` | 发送请求并获取结果（字符串或 JSON 反序列化） | `CancellationToken` | `Task<TResult>` |  | 
| `IHttpRequest<TResult>.GetStreamAsync()` / `WriteAsync(...)` | 以字节数组读取响应或写入文件 | `CancellationToken` / `filePath` | `Task<byte[]>` / `Task` |  | 
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
    3) `SendAsync(...)`：获取 HttpClient，初始化 BaseAddress/Timeout，并执行 `HttpClient.SendAsync`；
    4) `SendAfterAsync(...)`：读取响应字符串；成功则 `ConvertTo(...)`，失败则触发 `OnFail` 并返回 null；最后执行 `OnComplete`。
    - 证据：src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs | GetResultAsync/CreateMessage/SendAsync/SendAfterAsync/SuccessHandlerAsync/ConvertTo/FailHandler/CompleteHandler

- 文件上传（multipart/form-data）：
    - 普通参数：`ByteArrayContent(UTF8.GetBytes(value))` 加入表单；
    - 文件：支持 `Stream` 或文件路径；内部会把 Stream 读取为 byte[] 再加入表单。
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
    - `TResult == string` 直接返回响应内容；否则仅当 `contentType`（MediaType）为 `application/json` 时尝试 `Json.ToObject<TResult>`，否则返回 null。
        - 证据：src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs | ConvertTo(...)
        - 证据：src/Bing.Utils.Http/Bing/Http/Extensions/HttpResponseMessageExtensions.cs | GetContentType(...)

- `OnSendBefore` 触发条件疑点（待确认）：
    - `SendBefore(HttpRequestMessage)` 内部判断使用了 `SendAfterAction`（而非 `SendBeforeAction`）；这意味着“仅设置 OnSendBefore，但未设置 OnSendAfter”时，发送前回调可能不会执行（无法取消发送）。
        - 证据：src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs | SendBefore(...)

- 文件上传的资源与错误路径：
    - `FileContent(Stream...)` 上传路径中对 `file.Stream` 使用 `using var fileStream = file.Stream;`，会在请求构建时 Dispose 该 Stream（调用方需注意）。
        - 证据：src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs | AddFileData(...)
    - 文件路径不存在时 `AddFileData` 会 `return;`（提前退出，后续文件不会继续处理，行为待确认是否期望）。
        - 证据：src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs | AddFileData(...)

- ASP.NET Core 扩展的边界：
    - `HttpRequestExtensions.Query<T>/Form<T>`：转换失败（`InvalidCastException`）返回默认值而非抛出。
        - 证据：src/Bing.Utils.Http/Bing/Http/Extensions/Extensions.HttpRequest.cs | Query<T>/Form<T>
    - `HttpRequestExtensions.IsMobileBrowser`：对 `userAgent.Substring(0, 4)` 未见长度检查，userAgent 太短可能抛异常（待确认调用方是否保证长度）。
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
    - JSON 序列化与文件上传会产生字符串与 byte[] 分配（文件会读入内存再上传）。
        - 证据：src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs | CreateJsonContent/AddFileData(ReadToBytes/ToArray)
- 大数据量表现：
    - 大文件上传会把 Stream/文件读入内存（`ReadToBytes`/`ToArray`），对超大文件不友好（待确认是否有流式上传计划）。
        - 证据：src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs | AddFileData(...)
- Benchmark 链接：待补（benchmarks/Bing.Utils.Benchmark/ 中未检索到 Http 模块专项用例，待确认）。

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
    - 已包含 MicroMessenger 误识别回归测试；其余（如 `OnSendBefore` 触发疑点、短 UserAgent 的 `IsMobileBrowser`）建议补充。
        - 证据：tests/Bing.Utils.Http.Tests/Bing/Helpers/UserAgentHelperTest.cs | GetOperatingSystemName_MicroMessengerBug_FixedCorrectly
        - 证据：src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs | SendBefore(...)
        - 证据：src/Bing.Utils.Http/Bing/Http/Extensions/Extensions.HttpRequest.cs | IsMobileBrowser

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
    - 若使用 `Certificate(...)`/`IgnoreSsl()`：依赖底层 `HttpClientHandler` 可被 `IHttpClientFactory` 的 handlerFactory 获取并修改；若工厂实现不支持 `IHttpMessageHandlerFactory`，相关初始化可能无法生效（待确认运行时行为）。
        - 证据：src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs | CreateHttpClientHandler/InitHttpClientHandler/InitCertificate/IgnoreSsl

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
- [ ] 为 `HttpRequestExtensions.IsMobileBrowser` 增加短 UserAgent 的边界测试，并评估是否需要长度 guard。
    - 证据：src/Bing.Utils.Http/Bing/Http/Extensions/Extensions.HttpRequest.cs | IsMobileBrowser（Substring(0,4)）
- [ ] 为 `HttpRequest<TResult>.OnSendBefore` 补充集成/单测验证：仅设置 OnSendBefore 时是否能取消发送（当前实现判断疑点）。
    - 证据：src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs | SendBefore(...)
- [ ] 评估文件上传是否需要支持真正的流式上传（避免把大文件读入内存）。
    - 证据：src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs | AddFileData(...ReadToBytes/ToArray)
- [ ] 补充 Http 模块 benchmark（如请求构建/文件上传内存分配评估）。

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
