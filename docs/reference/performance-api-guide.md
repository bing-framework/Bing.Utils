# 流式与 API 选择指南

本指南说明正文读取、流式压缩、正则超时和内存流的入口选择，以及这些 API 对缓冲、资源所有权和限额的约定。HTTP 默认响应上限仅作用于 Bing 请求读取路径；直接调用 `HttpClient.SendAsync` 不会应用该上限。

## HTTP 正文读取

| 场景 | 推荐入口 | 行为与边界 |
|---|---|---|
| 普通缓冲业务结果 | `GetResultAsync()` | 按既有正文读取流程处理完整响应，并使用现有转换、回调和错误处理。 |
| 需要完整原始正文 | `GetBytesAsync()` | 返回完整 `byte[]`，会把正文读入内存；`GetStreamAsync()` 是保留的旧名称，返回类型同样是 `byte[]`。 |
| 解析兼容的 UTF-8 JSON | `GetJsonStreamAsync<T>()` | 直接从响应流解析。字符串结果、非 JSON 或非 UTF-8 响应及自定义转换等情况会回退到既有正文处理；不能与 `OnSendAfter` 同时使用。 |
| 成功响应下载到已有目标流 | `DownloadToAsync(Stream)` | 成功正文逐块写入调用方持有的可写流；取消或写入失败时目标可能已有部分内容。 |
| 下载并替换目标文件 | `DownloadFileAsync(path)` | 先写入同目录临时文件，成功后替换目标；失败时保留原目标文件。 |
| 服务端将对象写成 JSON | `HttpResponse.WriteJsonStreamAsync<T>(...)` | 以 UTF-8 将 JSON 直接写入响应正文，遵守 `RequestAborted` 和显式取消令牌；预取消时不写入，且不关闭响应正文流。 |

```csharp
using Bing.Http;
using Bing.Http.Clients;

var service = new HttpClientService(httpClientFactory);
var order = await service.Get<OrderDto>(url).GetResultAsync(cancellationToken);

byte[] body = await service.Get<OrderDto>(url).GetBytesAsync(cancellationToken);
var streamedOrder = await service.Get<OrderDto>(url).GetJsonStreamAsync(cancellationToken);

await using var destination = File.Create(downloadStreamPath);
await service.Get<OrderDto>(url).DownloadToAsync(destination, cancellationToken);
await service.Get<OrderDto>(url).DownloadFileAsync(downloadPath, cancellationToken);

// response 是当前 ASP.NET Core 请求的 HttpResponse。
await response.WriteJsonStreamAsync(order, cancellationToken: cancellationToken);
```

`WriteJsonStreamAsync` 直接写入响应体，但序列化器仍可能为单个超大字符串或值分配缓冲；流式入口不等于恒定内存。

`DownloadToAsync` 不拥有或关闭目标流。若需要下载失败时保留已有文件，应使用 `DownloadFileAsync`；直接下载到流的失败不会回滚已经写入的字节。

## 响应正文大小上限

为命名或类型化客户端设置默认正文上限：

```csharp
using Microsoft.Extensions.DependencyInjection;

services.AddHttpClient("catalog")
    .WithBingDefaultMaxResponseBytes(4L * 1024 * 1024)
    .UseBingRequestIsolation();
```

请求级配置有三种状态，显式请求设置优先于客户端默认值：

```csharp
using Bing.Http;
using Bing.Http.Clients;

using var client = httpClientFactory.CreateClient("catalog");
var service = new HttpClientService().SetHttpClient(client);

var request = service.Get<OrderDto>(url); // 默认继承命名客户端的 4 MiB 上限。

// 为此请求设置更严格的 256 KiB 上限：
request.MaxResponseBytes(256 * 1024);

// 也可改为显式不限大小，覆盖客户端默认值：
// request.WithoutMaxResponseLimit();

// 或清除请求级覆盖，恢复继承客户端默认值：
// request.UseClientDefaultMaxResponseBytes();

var result = await request.GetResultAsync(cancellationToken);
```

`MaxResponseBytes(0)` 只允许空正文；负数无效。`WithoutMaxResponseLimit()` 会使该请求不受客户端默认上限约束。`UseClientDefaultMaxResponseBytes()` 恢复继承；如果客户端没有设置默认值，该请求不限制正文大小。上限适用于 Bing 的结果读取、字节读取、JSON 流解析和下载等路径。分片下载按每个实际响应检查上限，单片失败通过未完成状态及 `failureObserver` 报告，不提交最终目标；该设置不是所有分片合计的文件大小预算。

## 正则匹配超时

处理不可信输入时，使用带 `matchTimeout` 的重载，而不是依赖旧重载的运行时默认值：

```csharp
using System.Text.RegularExpressions;
using Bing.Text.RegularExpressions;

var matched = RegexPool.IsMatch(
    input,
    pattern,
    RegexOptions.CultureInvariant,
    TimeSpan.FromMilliseconds(100));
```

显式超时约束每次正则匹配；集合匹配的超时不是整个枚举操作的总时限。达到上限时会抛出 `RegexMatchTimeoutException`。除 `Regex.InfiniteMatchTimeout` 外，超时必须大于零且不超过 `2147483646` 毫秒。旧重载保留原有超时行为。

## 流式 GZip

`Compression.CompressTo`、`DecompressTo` 及异步版本从源流当前位置读取，并从目标流当前位置写入；不复位输入，也不截断目标。默认 `leaveOpen: true` 保留两个流；设为 `false` 时，参数检查和异步预取消通过后，处理开始后的成功或失败都会关闭源流与目标流。处理失败可能留下部分输出，不会回滚目标内容。

```csharp
using Bing.Helpers;
using System.IO;

using var input = File.OpenRead("input.bin");
using var compressed = new MemoryStream();
Compression.CompressTo(input, compressed); // 默认保留 input 和 compressed

compressed.Position = 0;
using var output = File.Create("output.bin");
Compression.DecompressTo(compressed, output, maxOutputBytes: 100L * 1024 * 1024);
```

异步调用可在最后一个命名参数传入 `cancellationToken`：

```csharp
// 独立示例：输入和输出应已定位到本次处理的起点。
await Compression.CompressToAsync(input, compressed, cancellationToken: cancellationToken);
compressed.Position = 0; // MemoryStream 支持复位；管道等不可定位流应由消费端直接读取。
await Compression.DecompressToAsync(
    compressed, output, maxOutputBytes: 100L * 1024 * 1024,
    cancellationToken: cancellationToken);
```

解压上限按实际写出的解压字节数计算，边界值允许通过；超过上限会抛出 `InvalidDataException`，但此前已写入的部分内容仍保留。该上限不代表 CPU 或总内存预算。

## 流的位置、释放与失败结果

| 入口 | 开始与完成位置 | 资源归属 | 失败及取消 |
|---|---|---|---|
| `FileHelper.ReadToMemoryStream[Async]` | 文件从头读；返回内存流停在末尾，读取前设 `Position = 0` | 文件由方法释放；返回流由调用方释放 | 普通失败返回 null；异步取消传播，失败的中间流释放 |
| 旧 `Compress(Stream)` / `CompressAsync(Stream)` | 可定位输入从头读，不可定位输入从当前位置读；完成后停在已消费位置 | 保留输入；返回独立数组 | 空输入返回空数组；处理错误包装，异步取消传播 |
| 旧 `Decompress(Stream)` / 异步版本 | 从当前输入位置读；返回独立数组 | 处理开始后关闭输入，可定位空流提前返回且保持打开 | 失败不返回部分数组；处理错误包装，异步取消传播；空流提前返回保留旧约定 |
| `DecompressWithLimit(Stream)` / 异步版本 | 从当前输入位置读；返回独立数组 | 处理开始后关闭输入；参数检查、预取消及可定位空流不接管输入 | 超限抛 `InvalidDataException`，不返回部分数组；取消传播 |
| `CompressTo[Async]` / `DecompressTo[Async]` | 两端均从当前位置开始；完成后停在实际消费/写入位置 | 默认两端均保持打开；`leaveOpen: false` 按前述契约释放 | 部分输出可保留；解压绝不写超预算，取消传播 |

ZIP 限额解压使用同目录临时文件：新建目标通过移动提交，覆盖已有目标通过文件替换提交。只有平台明确不支持替换时回退复制，因此回退仍有复制成本，且不是原子替换。整个归档不提供事务回滚；此前成功的条目和已经创建的目录可能保留。

## 内存流

`PooledMemoryStream.GetBuffer()` 与 `ToArray()` 返回独立的逻辑内容副本。`GetSpan()`、`GetMemory()` 和 `ToArraySegment()` 返回借用视图，不应在流扩容或释放后继续使用。使用完池化流后调用 `Dispose`。

`LargeMemoryStream` 按页面存储数据；无参构造默认每页 64 KiB，空流不租页。可根据负载显式传入页大小，布局兼容与页面视图说明见 [能力收敛指南](capability-refinement-guide.md)：

```csharp
using System.IO;
using System.Text;
using Bing.IO;

using var stream = new LargeMemoryStream(1024 * 1024); // 每页 1 MiB
var data = Encoding.UTF8.GetBytes("page sample");
stream.Write(data, 0, data.Length);
stream.Seek(-1, SeekOrigin.End); // 定位到最后一个字节
```

## Cookie 隔离与连接复用

显式调用 `UseCookies(bool)` 时，客户端必须用 `UseBingRequestIsolation()` 注册：

```csharp
using Bing.Http.Clients;
using Microsoft.Extensions.DependencyInjection;

services.AddHttpClient("api")
    .UseBingRequestIsolation();
```

```csharp
using Bing.Http.Clients;

using var client = httpClientFactory.CreateClient("api");
var service = new HttpClientService().SetHttpClient(client);

var result = await service.Get<OrderDto>(url)
    .UseCookies(false)
    .GetResultAsync(cancellationToken);
```

`UseCookies(false)` 禁用自动 Cookie。隔离路由器会让没有其他请求级 TLS、证书或 Cookie 隔离需求的请求共用无 Cookie 传输，因此可以复用其连接池。启用自动 Cookie、请求级 TLS 例外或客户端证书时，会为一次逻辑请求创建独立传输，并在该请求及其响应处理结束后释放；这类请求不应依赖跨请求的连接池复用。默认 Cookie 状态仅在该逻辑请求及其重定向、重试过程中保留。

## 随机字符串

`RandomNonceStrGenerator.Create(length, forceToAvoidRepetition)` 使用密码学随机数和拒绝采样覆盖完整 62 字符集，长度小于 16 时按 16 生成。`forceToAvoidRepetition` 为源码兼容保留，两种取值使用相同安全随机路径，不维护全局去重，也不保证唯一。较长输出按固定大小随机块生成，不按输出长度分配同等大小的随机字节缓冲区。
