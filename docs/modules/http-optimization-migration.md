# HTTP 优化迁移说明

本阶段的通用请求、完整响应与异步拦截器使用可选能力接口，不再给现有接口增加必需成员。

## 安全配置

`Certificate(...)`、`IgnoreSsl()` 和 `UseCookies(...)` 不再修改工厂复用的 Handler；显式调用会抛 `NotSupportedException`。在注册命名 `HttpClient` 时配置客户端证书、TLS 验证和 `UseCookies`，然后通过 `.HttpClientName(name)` 选择该客户端。`IgnoreSsl()` 只能用于受控测试环境；生产环境应使用正常证书验证。手动 `.Cookie(key, value)` 仍设置单次请求的 Cookie 头。对于每用户独立 Cookie，使用隔离的客户端/Handler，避免共享 `CookieContainer`。

```csharp
services.AddHttpClient("private-api")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        UseCookies = false
    });

var result = await client.Get("https://example.com/data")
    .HttpClientName("private-api")
    .GetResultAsync();
```

客户端证书和 TLS 验证策略也应在对应命名客户端的 Handler 注册阶段配置。

注入的 `HttpClient` 由调用方释放。HTTP 模块会释放自己创建的请求、响应及工厂创建的客户端包装实例。`OnSendAfter`、`OnFail`、`OnComplete` 回调执行期间响应有效；回调返回后响应由模块释放，直接返回响应流会抛异常，也不要把该流封装在其他返回对象中。回调创建的独立流可以返回，由调用方负责释放。`BaseAddress` 和 `Timeout` 现在是单次请求配置，不修改注入的客户端。已有 `HttpClient.Timeout` 仍可能更早终止发送。

旧版 `OnSendAfter(Func<HttpResponseMessage, Task<TResult>>)` 没有取消令牌参数。库会等待回调结束后才释放响应，因此长期运行的回调需要自行处理取消。前阶段新增的 `OnSendAfter(Func<HttpResponseMessage, CancellationToken, Task<TResult>>)` 向回调传递请求取消令牌，第三方 `IHttpRequest<TResult>` 实现需补充此重载。

## 宿主弹性与遥测集成

HTTP 模块支持通过 IHttpClientFactory 使用宿主配置的命名 HttpClient 和 Handler。核心包不引用 Microsoft.Extensions.Http.Resilience、Polly 或 OpenTelemetry；这些策略应在应用宿主注册，避免把宿主版本和依赖传递给所有调用方。

```csharp
services.AddHttpClient("orders", client =>
    {
        client.BaseAddress = new Uri("https://orders.example/");
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    // 需要时在宿主项目引用 Microsoft.Extensions.Http.Resilience：
    .AddStandardResilienceHandler();

services.AddTransient<IHttpClient>(provider =>
    new HttpClientService(provider.GetRequiredService<IHttpClientFactory>()));

var result = await client.Get<OrderDto>("orders")
    .HttpClientName("orders")
    .GetJsonStreamAsync(cancellationToken);
```

弹性策略只能在一个层级负责同一类重试。若命名客户端已经通过 Handler 配置重试、超时或熔断，则请求链不要再调用 .Retry(...)；若业务必须使用请求级 .Retry(...) 和 ObserveRetry(...)，应关闭该命名客户端的同层 Handler 重试，避免尝试次数相乘。请求级重试仍适合按单次请求动态决定允许的方法、响应码和预算。

如果共享请求配置需要显式记录由宿主负责重试，可设置 `HttpRetryOptions.HostHandlerOwnsRetries = true`。这不是 Handler 探测开关：当同一请求继续调用 `.Retry(...)` 时，模块会在首次发送前抛出 `InvalidOperationException`，并且不会发出请求。该属性默认为 `false`，现有请求级重试行为不变；应用应选择一个层级负责重试。

```csharp
var request = client.Get<OrderDto>(url)
    .Retry(new HttpRetryOptions { HostHandlerOwnsRetries = true });
// request.GetResultAsync() 在发送前失败；移除 Retry 或关闭宿主重试后再选择对应层级。
```

OpenTelemetry 同样由宿主配置。例如应用已经引用 OpenTelemetry.Extensions.Hosting 和 OpenTelemetry.Instrumentation.Http 时：

```csharp
services.AddOpenTelemetry()
    .WithTracing(builder => builder.AddHttpClientInstrumentation());
```

该配置采集 HttpClient 运行时产生的标准 HTTP span；Bing HTTP 模块不额外创建第二条 span。需要补充请求业务结果时，可使用 ObserveCompletion 或 ObserveRetry 写入应用自己的计数器/日志，但应通过闭包提供业务标识，并避免记录完整 URL、认证头和正文。ObserveRetry 的异常处理器只处理观测委托自身异常，不改变请求结果。

HttpClientName(name) 的命名 Handler 选择由集成测试覆盖。证书、TLS、Cookie、代理和宿主弹性策略均应在命名客户端注册阶段配置；Certificate(...)、IgnoreSsl() 等请求级入口不会修改工厂复用的 Handler。

## 传输与弹性能力

```csharp
using Bing.Http;

var bytes = await client.Get("https://example.com/data")
    .MaxResponseBytes(1024 * 1024)
    .Retry(new HttpRetryOptions())
    .GetStreamAsync(cancellationToken);

await client.Get("https://example.com/file")
    .MaxResponseBytes(64L * 1024 * 1024)
    .DownloadFileAsync(filePath, cancellationToken);
```

`DownloadToAsync` 保留目标流打开；失败或取消时目标流可能已有部分内容。`DownloadFileAsync` 在同目录写临时文件，仅成功时替换目标文件。下载在临时文件关闭后、替换或移动目标前检查调用方取消令牌；取消会保留旧目标并清理临时文件。请求 `Timeout` 覆盖 HTTP 发送和正文读取，不覆盖最终同步文件提交；提交开始后不可中断，取消检查到提交之间存在短暂竞态。`GetStreamAsync` 仍返回 `byte[]`；旧 `WriteAsync` 在无需正文回调时使用流式文件路径。配置 `OnComplete`/`OnFail` 正文回调时，旧 `WriteAsync` 会先缓冲整个响应，再直接覆盖目标文件；写入失败或取消可能破坏已有文件。需要保护原文件时使用 `DownloadFileWithResultAsync`。上传路径以 `StreamContent` 流式发送，不会先将整个文件读入内存；旧 `FileContent(Stream,...)` 由模块释放流，新重载的 `leaveOpen: true` 保留调用方的流。

新版本可通过可选能力 `IHttpRequestDownloadResult<TResult>` 使用带元数据的下载扩展，不会要求既有 `IHttpRequest<TResult>` 实现增加成员：

~~~csharp
var result = await client.Get<CustomerDto>(fileUrl)
    .DownloadFileWithResultAsync(filePath, cancellationToken);

if (result is not null)
    logger.LogInformation("下载状态 {StatusCode}，写入 {BytesWritten} 字节，保存={IsFileSaved}",
        result.StatusCode, result.BytesWritten, result.IsFileSaved);
~~~

`DownloadToWithResultAsync(Stream, CancellationToken)` 和 `DownloadFileWithResultAsync(string, CancellationToken)` 返回 `HttpDownloadResult`，包含 `StatusCode`、`IsSuccessStatusCode`、独立的 `Headers`/`ContentHeaders` 快照、`BytesWritten` 和 `IsFileSaved`。下载到流时目标流仍归调用方所有且保持打开；取消或读取异常时可能已有部分内容写入。文件下载先写同目录临时文件，仅成功后替换目标；非成功 HTTP 状态会返回结果、字节数为 0、`IsFileSaved` 为 false，拒绝发送时结果为 null。取消、网络、读取、超限或文件操作异常仍会抛出。`HttpDownloadResult` 的公共构造函数供第三方可选实现创建同契约结果；响应头和内容头会防御性复制为大小写不敏感的只读快照，值列表也会独立复制。旧 `DownloadToAsync`/`DownloadFileAsync` 签名及行为继续可用。

取消提交前边界同样适用于旧 `DownloadFileAsync` 和带结果的 `DownloadFileWithResultAsync`：下载正文成功后关闭临时文件，再检查调用方令牌；取消时原目标保持不变。令牌检查通过后，最终文件替换或移动是同步操作，开始后无法由 `Timeout` 或取消令牌中断。

整次请求观测通过可选接口 `IHttpRequestObservationRegistration<TResult>` 注册：

~~~csharp
var request = client.Get<CustomerDto>(resourceUrl)
    .ObserveCompletion(completion =>
    {
        logger.LogInformation("HTTP {StatusCode} {Outcome}，尝试 {Attempts} 次，耗时 {ElapsedMs} ms",
            completion.StatusCode, completion.Outcome, completion.Attempts, completion.Elapsed.TotalMilliseconds);
    }, error => logger.LogWarning("观测处理失败：{ErrorType}", error.GetType().Name));

var response = await request.GetResponseAsync(cancellationToken: cancellationToken);
~~~

`ObserveCompletion` 返回原请求对象；同一请求重复调用时后一次注册覆盖前一次。观测包含状态码（无响应时为空）、实际发送尝试数、包含资源清理的总耗时、`Success`/`HttpFailure`/`Rejected`/`Cancelled`/`TimedOut`/`Faulted` 分类，以及传播给调用方的异常。`HttpRequestCompletion` 的公共构造函数可供第三方观测能力实现创建相同契约的数据。观测回调是同步委托，在清理完成后、请求任务完成前运行；长时间处理会延迟请求完成，建议将轻量观测数据投递到宿主队列异步处理。回调抛出的异常交给可选错误处理回调；未提供该回调时忽略观测异常，错误处理回调自身异常也会被隔离，两者都不会改变请求结果。`WriteAsync` 的完成观测包括文件写入结束。观测对象不包含 URL、头或正文。日志应避免正文、认证/签名头、query 参数及异常消息。

请求正文处理发生异常时，模块仍继续清理其拥有的请求、响应和正文资源；清理异常不会覆盖原始处理异常。此前没有处理异常时，传播首个清理异常，同时继续执行其余清理操作。


断点续传是独立的可选能力 IHttpRequestResumableDownload<TResult>，调用 DownloadFileResumableWithResultAsync 可在同目录 .binghttp.lock/.binghttp.part/.binghttp.resume.json 上恢复。它只允许 GET，并要求 ETag 或 Last-Modified 作为 If-Range 验证器；同一目标文件的并发续传由独占锁串行化，冲突请求快速失败。206 追加、200 回退覆盖、匹配的 416 提交，失败时保留可验证片段，成功后删除状态文件。需要一次性清理目录树中的遗留侧车文件时，可显式调用 CleanupRecursiveAsync；默认 CleanupAsync 仍只扫描当前目录顶层。失败结果的 BytesWritten 是保留片段长度；匹配的 416 仍保留 416 状态码，但 IsFileSaved=true 表示本地片段已提交。状态同时保存请求头 SHA-256 指纹和响应 Content-Encoding；关键请求头或编码变化时拒绝追加。旧下载 API 不会自动改变为断点续传。

HEAD 和 204 响应在普通结果、字节读取及下载路径都不读取正文，也不以描述性 `Content-Length` 触发 `MaxResponseBytes`。普通结果入口返回 null 并向成功回调传入 null；若调用方曾用模拟 HEAD 正文取得非空结果，需要改为读取状态或响应头。

重试默认关闭。启用后默认最多重试两次，仅适用于 GET、HEAD、OPTIONS 的 408、429、500、502、503、504 和发送阶段的连接异常；使用指数退避、抖动并遵守 `Retry-After`。写请求必须显式加入 `AllowedMethods`，且正文可重放。对于需要服务端幂等保障的非安全方法，可设置 `RequireIdempotencyKeyForUnsafeMethods = true`，此时每次重试前都必须存在非空的 `Idempotency-Key` 请求头；库不会自动生成该键，调用方应为同一逻辑操作复用同一个键。文件路径和可定位的调用方流可以重试；不可定位流或 `OnSendBefore` 替换正文时会拒绝自动重试。发送前回调如改变 HTTP 方法，按实际方法重新检查允许名单；重试期间不得再次更改方法。每次尝试以实际发送前的方法固定重试资格，后置拦截器修改请求消息不会放开该次尝试的重试。已有命名客户端重试 Handler 时只选择一层负责重试，避免重复次数相乘。

关联请求 ID 使用现有 Header API 显式设置，例如 `.Header("X-Correlation-Id", correlationId)`。请求级重试每次都会重新构造请求消息，但会复制该 Header；断点续传的初次请求和后续 Range 请求也会保留同一个 Header。模块不会自动生成、覆盖或从响应推断关联 ID，因此同一逻辑操作应由调用方传入同一个非空值。`ObserveRetry` 和 `ObserveCompletion` 事件不暴露头部，观测回调需要通过闭包保留业务关联 ID；宿主 Handler 自行重试时，应另行确认其是否保留自定义头部。

断点续传默认不校验文件摘要。需要在提交目标文件前校验内容时，可使用 `.ValidateResumableDownloadSha256(expectedSha256)`，或传入读取完整临时文件的 `.ValidateResumableDownload(stream => ...)` 校验器。摘要不匹配会抛出 `IOException`，目标文件不会被替换；当前已有 ETag/Last-Modified 验证器时，片段和状态文件按既有失败语义保留以便再次续传。校验成功后才提交并删除状态文件。

条件请求可使用 `.IfNoneMatch("\"v1\"")` 或 `.IfModifiedSince(lastModified)` 设置强类型的 `If-None-Match`/`If-Modified-Since` 请求头。收到 304 时请求仍返回普通响应结果，不会自动读取缓存或重复发送；可通过 `HttpResponseResult.IsNotModified` 或 `HttpDownloadResult.IsNotModified` 判断。无效实体标签会在发送前抛出 `ArgumentException`。

```csharp
var result = await client.Post<OrderResult>(url)
    .Header("Idempotency-Key", operationId)
    .Retry(new HttpRetryOptions
    {
        AllowedMethods = new HashSet<HttpMethod> { HttpMethod.Post },
        RequireIdempotencyKeyForUnsafeMethods = true
    })
    .GetResultAsync(cancellationToken);
```

`MaxResponseBytes` 默认不设限。设置后检查响应长度和实际读取字节；自定义 `OnSendAfter` 使用有界缓冲后接收响应。超限抛 `HttpResponseSizeLimitException`。

### 接口迁移范围

前阶段已向 `Bing.Http.IHttpRequest<TResult>` 增加必需成员：`Retry`、`MaxResponseBytes`、`DownloadToAsync`、`DownloadFileAsync`、带 `leaveOpen` 的 `FileContent` 重载，以及带取消令牌的 `OnSendAfter` 重载。自定义请求实现者需要实现这些签名；若这些更改尚未纳入已发布版本，升级时应同时更新实现并重新编译。

本阶段的 `Request`/PATCH/HEAD/OPTIONS、完整响应结果、异步拦截器、带结果的下载和整次请求观测通过独立可选能力接口提供：`IHttpRequestFactory`、`IHttpRequestResponse<TResult>`、`IHttpRequestInterceptorRegistration<TResult>`、`IHttpRequestDownloadResult<TResult>` 和 `IHttpRequestObservationRegistration<TResult>`。它们不再向 `IHttpClient` 或 `IHttpRequest<TResult>` 增加必需成员。自定义实现可按需实现能力接口；调用方对不支持的实现调用对应扩展时会收到明确的 `NotSupportedException`。详见[HTTP 模块能力与使用示例](http.md#13-通用请求完整响应与拦截器)。

`OnSendBefore` 返回 false 时，`GetResponseAsync` 返回 null，且不执行响应回调。响应处理与 `OnComplete` 同时抛异常时，现在保留原始处理异常；只有完成回调失败时仍传播其异常。`HttpRequestExtensions.IsMobileBrowser` 对缺失或少于四字符的 User-Agent 返回 false；此前短值可能抛异常，依赖该异常的调用方应改为检查返回值。

## 服务端辅助方法

`Web.IsLocal` 在远端 IP 缺失时返回 false。`Web.Host` 在 HTTP 上下文中返回连接远端 IP；代理部署应先配置可信 ForwardedHeaders 中间件。`Web.Write(FileStream)` 已标记过时，使用 `await Web.WriteAsync(stream)`；两个方法都不删除输入文件、不关闭输入流或响应流。`Web.DownloadFileAsync` 与 `Web.DownloadAsync(Stream,...)` 采用流式复制。大请求体使用 `Web.GetBodyAsync(maxBytes, cancellationToken)`，无参重载保留旧的无限额行为。

通用 Cookie 默认值未改变；敏感 Cookie 可用现有 `Web.SetCookie(key, value, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax })` 显式配置属性。

## 二进制正文、进度与流式 JSON

重复执行二进制请求时，默认拥有的流在首次执行后已释放，需要重新配置正文。使用 `leaveOpen: true` 时调用方负责最终释放，可定位流每次从注册位置发送；不可定位流不能自动复原。顺序重复发送也可使用字节数组或每次返回全新正文的工厂，请求实例不支持并发执行或执行中修改正文。

新增的 `BinaryContent`、`ContentFactory`、`ObserveUploadProgress`、`ObserveDownloadProgress`、`GetJsonArrayStreamAsync` 和 `GetJsonStreamAsync` 分别由可选接口 `IHttpRequestRawContentRegistration<TResult>`、`IHttpRequestUploadProgressRegistration<TResult>`、`IHttpRequestDownloadProgressRegistration<TResult>`、`IHttpRequestJsonArrayStreaming<TResult>` 和 `IHttpRequestStreamingJson<TResult>` 承载，不会给既有 `IHttpRequest<TResult>` 实现增加必需成员。旧实现调用未支持的能力扩展时收到 `NotSupportedException`。

`BinaryContent(byte[], contentType)` 在注册时复制字节数组。`BinaryContent(Stream, contentType, leaveOpen)` 在注册时记录流的起始位置，每次尝试都从该位置重放；默认流由请求配置拥有，`leaveOpen: true` 保留给调用方。`ContentFactory(Func<HttpContent>)` 必须在每次尝试返回新的正文对象，工厂正文由请求释放。自动重试按实际 HTTP 方法检查；不可定位流不能用于允许重试的方法。原始二进制正文与对象/参数或 multipart 正文配置冲突，配置在创建正文时失败。适合重试的正文应从保存位置重放，或由工厂逐次创建。

### 逐项 JSON 数组消费

GetJsonArrayStreamAsync 返回 IAsyncEnumerable<TResult>，只接受 UTF-8 的顶层 JSON 数组（application/json、+json 或未声明媒体类型）。它使用响应头完成模式和增量 Utf8JsonReader，数组元素在解析完成后立即产生，调用方不需要先构造完整集合。

该枚举只能消费一次；HTTP/JSON/读取异常通常在第一次或后续 MoveNextAsync 时抛出。开始消费后不执行自动重试，避免已经产生的元素与重试响应混合。取消令牌、提前 DisposeAsync、解析失败和大小限额异常都会释放响应、正文和底层流。MaxResponseBytes 仍限制实际读取的响应字节数，单个巨大元素仍可能占用与元素大小相当的内存。

为避免完整响应回调在“响应头已返回但正文尚未消费完”时产生歧义，该入口不与 OnSuccess、OnConvert、OnFail、OnComplete、OnSendAfter 或完成观测同时使用。

示例：await foreach (var item in request.GetJsonArrayStreamAsync(cancellationToken)) { await HandleItemAsync(item); }

### 上传进度

新增可选接口 `IHttpRequestUploadProgressRegistration<TResult>` 和扩展 `ObserveUploadProgress`，旧版 `IHttpRequest<TResult>` 实现无需增加必需成员；不支持该能力的实现调用扩展时收到 `NotSupportedException`。进度包装最终发送的 `HttpContent`，按实际写入传输流的字节计数，保留正文 headers、长度计算和正文所有权语义；它表示本地写入，不表示服务端已接收或持久化。

每次发送尝试都会从零开始报告 `BytesWritten`，`Attempt` 从 1 开始。已知长度填充 `TotalBytes`，未知长度为 null；每次尝试都有零字节初始通知和成功写完后的 `IsCompleted=true` 最终通知。默认中间通知阈值为 65,536 字节和 100 毫秒，两个阈值同时满足。重试、`ContentFactory` 和已有流正文会按原有正文生命周期运行，包装器在每次尝试独立创建并释放。

上传回调为同步委托；回调异常、取消或正文写入失败会终止发送并传播，未完成写入不会报告完成事件。回调应保持轻量，不要记录 URL、query、认证头或正文。

上传正文写完只表示本地发送完成，不代表服务端已确认业务成功。需要显式检查响应时，可使用 `GetUploadConfirmedResponseAsync`：它只发送一次请求，校验器返回 false 时抛出 `HttpUploadConfirmationException<TResult>`，异常的 `Response` 属性保留状态码、响应头和转换结果。该入口默认不启用，不自动重试、不缓存响应；`ContentFactory` 和 `ObserveUploadProgress` 仍按各自正文重建及本地写入契约工作。

~~~csharp
var response = await client.Post<UploadAck>(endpoint, payload)
    .GetUploadConfirmedResponseAsync(result =>
        result.IsSuccessStatusCode && result.Data?.Accepted == true,
        cancellationToken: cancellationToken);
~~~

`ObserveDownloadProgress` 使用同步回调，仅跟踪成功下载实际写入目标流的字节。默认中间通知要求累计至少 65,536 字节且间隔至少 100 毫秒；初始与 EOF 最终通知不受中间限频条件影响。`IsCompleted` 表示读到正文 EOF，不表示下载文件已最终保存；文件路径下载应另外检查带结果 API 的 `HttpDownloadResult.IsFileSaved`。未知总长度为 null，HEAD/204 总量为 0。进度回调异常会中止下载并传播；正文未到 EOF 时发生异常或取消不通知完成，EOF 后保存失败或取消则可能已有完成进度。回调为同步执行，建议仅记录必要的字节数和状态；不要记录 URL、query、敏感头或正文。

`GetJsonStreamAsync` 仅在成功 UTF-8 JSON、非 string 结果且未配置 `OnConvert`、`OnFail`、`OnComplete` 正文回调时直接从响应流解析。识别 `application/json` 与 `+json`，支持 UTF-8 BOM，并使用现有 JSON 序列化选项。UTF-8 以外编码、这些回调、自定义转换、string 结果和非成功响应均回退到原正文读取与转换逻辑，保留既有编码、转换和失败处理语义；成功的非 JSON 响应也不被强制解析成 JSON。直接解析的 HEAD、204、空或空白正文返回 null；回退路径仍允许 string 保留空字符串、OnConvert 自定义处理空正文。若调用方需要保留解码原文，继续使用 `GetResponseAsync(includeRawContent: true)`。

进度只用于新旧流式下载及带结果入口；旧 WriteAsync 配置正文回调后走缓冲写入路径，不通知下载进度。

进度委托在下载流程中同步调用，不保证调用方线程或 UI 线程，也不自动切换调度器。更新桌面 UI 时，由调用方通过 Dispatcher 或 SynchronizationContext 调度；异步投递后的更新可能晚于下载任务完成，其异常由调用方处理。同步回调中的耗时工作会延长下载时间。

流式 JSON 入口仍返回完整 `TResult`，减少的是完整正文的字节数组和字符串中间缓冲。大型数组对应的集合及元素仍会整体构造；`MaxResponseBytes` 限制读取的正文大小，不限制对象图的内存。需要逐项消费时使用上方的 GetJsonArrayStreamAsync；单个巨大元素仍可能占用与元素大小相当的内存。

## 文本读取优化与重试等待观测

普通 `GetResultAsync` 文本处理及 `GetResponseAsync` 复用同一个 `MemoryStream` 完成解码，减少 `ToArray` 复制完整正文数组的中间分配。现有字符集、BOM、零字节判断、转换、回调、限额和取消行为保持不变。这些入口仍保留完整正文缓冲、字符串和转换结果，不提供恒定内存；`GetStreamAsync` 仍返回独立 `byte[]`，自定义 `OnSendAfter` 的缓冲适配保持原有契约。

新增 `ObserveRetry` 通过可选接口 `IHttpRequestRetryObservationRegistration<TResult>` 注册，不要求旧请求接口实现增加成员。对不支持该能力的实现调用扩展会收到 `NotSupportedException`；null 请求或观察委托会收到 `ArgumentNullException`。观察注册不会自动启用重试，须另行配置 `Retry`：

~~~csharp
var result = await client.Get<CustomerDto>(endpoint)
    .Retry(new HttpRetryOptions { MaxRetries = 2 })
    .ObserveRetry(retry => logger.LogInformation(
        "重试计划：失败尝试 {FailedAttempt}，等待 {DelayMs} ms，状态 {StatusCode}，异常类型 {ErrorType}",
        retry.FailedAttempt, retry.Delay.TotalMilliseconds, retry.StatusCode,
        retry.Exception?.GetType().Name),
        error => logger.LogWarning("重试观测失败：{ErrorType}", error.GetType().Name))
    .GetResultAsync(cancellationToken);
~~~

`HttpRetryScheduled` 为不可变事件。`FailedAttempt` 从 1 开始，表示刚失败的发送尝试；`Delay` 是最终计划时长，含现有退避、抖动及 `Retry-After` 的决策结果，不是实际耗时。状态码重试仅携带状态码，发送异常重试仅携带原始异常引用。响应成功释放并检查取消后、等待前通知一次；事件不保证再次发送，等待中取消可阻止下一次发送。重试次数、实际方法、证书失败、预算或延迟范围导致不进入等待时无事件；释放失败或已观察到取消时也无事件。

每次执行开始固定观察者和错误处理回调快照，重新注册同时覆盖两者，仅影响下一次执行。回调异常交给可选错误处理；未提供时忽略观察异常，错误处理自身异常也被隔离，不改变请求结果。`ObserveCompletion` 仍在整次执行结束时通知一次。

观察者为同步委托，不保证调用方线程或 UI 线程，调用方负责调度界面更新。回调耗时计入整次请求耗时并延长调用时间，应保持轻量。未注册时不创建事件对象。事件不包含 URI、头或正文；原异常对象可能包含底层信息，记录日志时由调用方选取字段，示例只记录异常类型。


文件响应缓存的 `CleanupAsync` 默认只扫描当前目录；需要一次性维护目录树时可显式调用 `CleanupRecursiveAsync`，它为每个目录分别取得容量锁和 key 锁并保留锁文件。该维护入口仍是显式调用，不会改变缓存的 HTTP 协议语义或自动接入请求管线。
