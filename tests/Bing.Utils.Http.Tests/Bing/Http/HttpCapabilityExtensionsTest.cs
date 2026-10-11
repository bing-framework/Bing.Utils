using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using Bing.Http;
using Bing.Http.Clients;

namespace Bing.Utils.Http.Tests.Bing.Http;

/// <summary>

/// 验证可选 HTTP 能力扩展及其兼容边界。

/// </summary>
public class HttpCapabilityExtensionsTest
{
    /// <summary>
    /// 验证返回响应时服务端确认。
    /// </summary>
    [Fact]
    public async Task UploadConfirmation_ReturnsResponseWhenServerAcknowledges()
    {
        var sends = 0;
        using var client = new HttpClient(new StubHandler((_, _) =>
        {
            sends++;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("accepted") };
        }));

        var response = await new HttpRequest<string>(null, client, HttpMethod.Post, "https://example.test/upload")
            .GetUploadConfirmedResponseAsync(result => result.IsSuccessStatusCode && result.Data == "accepted");

        response.Data.ShouldBe("accepted");
        sends.ShouldBe(1);
    }

    /// <summary>

    /// 验证抛出使用响应未使用发送再次。

    /// </summary>
    [Fact]
    public async Task UploadConfirmation_ThrowsWithResponseWithoutSendingAgain()
    {
        var sends = 0;
        using var client = new HttpClient(new StubHandler((_, _) =>
        {
            sends++;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("rejected") };
        }));

        var error = await Should.ThrowAsync<HttpUploadConfirmationException<string>>(() =>
            new HttpRequest<string>(null, client, HttpMethod.Post, "https://example.test/upload")
                .GetUploadConfirmedResponseAsync(result => result.Data == "accepted"));

        error.Response.StatusCode.ShouldBe(HttpStatusCode.OK);
        error.Response.Data.ShouldBe("rejected");
        sends.ShouldBe(1);
    }

    /// <summary>

    /// 验证条件请求头匹配时报告资源未修改。

    /// </summary>
    [Fact]
    public async Task ConditionalRequest_SetsTypedHeadersAndReportsNotModified()
    {
        HttpRequestMessage received = null;
        using var client = new HttpClient(new StubHandler((request, _) =>
        {
            received = request;
            return new HttpResponseMessage(HttpStatusCode.NotModified);
        }));
        var lastModified = new DateTimeOffset(2026, 10, 6, 12, 34, 56, TimeSpan.FromHours(9));

        var result = await new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/item")
            .IfNoneMatch("W/\"v1\"")
            .IfModifiedSince(lastModified)
            .GetResponseAsync();

        received.Headers.GetValues("If-None-Match").Single().ShouldBe("W/\"v1\"");
        received.Headers.GetValues("If-Modified-Since").Single().ShouldBe("Tue, 06 Oct 2026 03:34:56 GMT");
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.IsNotModified.ShouldBeTrue();
    }

    /// <summary>

    /// 验证拒绝无效实体标签之前发送。

    /// </summary>
    [Fact]
    public void IfNoneMatch_RejectsInvalidEntityTagBeforeSending()
    {
        using var client = new HttpClient(new StubHandler((_, _) =>
            throw new InvalidOperationException("The request should not be sent.")));
        var request = new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/item");

        Should.Throw<ArgumentException>(() => request.IfNoneMatch("invalid tag"));
    }

    /// <summary>

    /// 验证拒绝格式错误的预期哈希值。

    /// </summary>
    [Fact]
    public void ValidateResumableDownloadSha256_RejectsMalformedExpectedHash()
    {
        using var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("unused") }));
        var request = new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/");

        Should.Throw<ArgumentException>(() => request.ValidateResumableDownloadSha256("not-a-sha256"));
    }

    /// <summary>

    /// 验证构造响应结果时会复制响应头快照。

    /// </summary>
    [Fact]
    public void PublicResultTypes_CanBeConstructedAndCopyHeaderSnapshots()
    {
        var headerValues = new List<string> { "one", "two" };
        var headers = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            ["X-Trace"] = headerValues
        };
        var contentHeaders = new Dictionary<string, IReadOnlyList<string>>
        {
            ["Content-Type"] = new[] { "application/json" }
        };

        var result = new HttpDownloadResult(HttpStatusCode.OK, headers, contentHeaders, 12, true);
        var completion = new HttpRequestCompletion(HttpStatusCode.OK, 2, TimeSpan.FromMilliseconds(15),
            HttpRequestOutcome.Success, null);

        headerValues.Add("three");
        headers["X-New"] = new[] { "value" };
        contentHeaders["Content-Type"] = new[] { "text/plain" };

        result.IsSuccessStatusCode.ShouldBeTrue();
        result.IsFileSaved.ShouldBeTrue();
        result.BytesWritten.ShouldBe(12);
        result.Headers["x-trace"].ShouldBe(new[] { "one", "two" });
        result.ContentHeaders["content-type"].ShouldBe(new[] { "application/json" });
        result.Headers.ContainsKey("X-New").ShouldBeFalse();
        Should.Throw<NotSupportedException>(() =>
            ((IList<string>)result.Headers["x-trace"]).Add("four"));
        Should.Throw<NotSupportedException>(() =>
            ((IDictionary<string, IReadOnlyList<string>>)result.Headers).Add("X-Other", new[] { "value" }));

        completion.StatusCode.ShouldBe(HttpStatusCode.OK);
        completion.Attempts.ShouldBe(2);
        completion.Outcome.ShouldBe(HttpRequestOutcome.Success);
        completion.Exception.ShouldBeNull();
        Should.Throw<ArgumentOutOfRangeException>(() =>
            new HttpDownloadResult(HttpStatusCode.OK, null, null, -1, false));
        Should.Throw<ArgumentOutOfRangeException>(() =>
            new HttpRequestCompletion(null, -1, TimeSpan.Zero, HttpRequestOutcome.Faulted, null));
    }

    /// <summary>

    /// 验证创建任意方法并保留响应。

    /// </summary>
    [Fact]
    public async Task RequestExtensions_CreateArbitraryMethodsAndPreserveResponse()
    {
        var methods = new List<string>();
        var patchHasContent = false;
        using var client = new HttpClient(new StubHandler((request, _) =>
        {
            methods.Add(request.Method.Method);
            if (request.Method.Method == "PATCH")
                patchHasContent = request.Content != null;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") };
        }))
        { BaseAddress = new Uri("https://example.test/") };
        var service = new HttpClientService().SetHttpClient(client);

        (await service.Patch("/patch", new { Value = 1 }).GetResultAsync()).ShouldBe("ok");
        (await service.Head("/head").GetResultAsync()).ShouldBeNull();
        (await service.Options("/options").GetResultAsync()).ShouldBe("ok");
        (await service.Request<string>(new HttpMethod("PROPFIND"), "https://example.test/custom").GetResultAsync()).ShouldBe("ok");

        methods.ShouldBe(new[] { "PATCH", "HEAD", "OPTIONS", "PROPFIND" });
        patchHasContent.ShouldBeTrue();
    }

    /// <summary>

    /// 验证旧版实现不支持该能力时明确失败。

    /// </summary>
    [Fact]
    public void OptionalExtensions_FailClearlyForLegacyImplementations()
    {
        using var httpClient = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("legacy") }));
        httpClient.BaseAddress = new Uri("https://example.test/");
        IHttpClient legacyClient = new LegacyHttpClient(httpClient);
        IHttpRequest<string> request = new LegacyHttpRequest();

        legacyClient.Get("/legacy").GetResultAsync().GetAwaiter().GetResult().ShouldBe("legacy");
        Should.Throw<NotSupportedException>(() => legacyClient.Patch("/resource"));
        Should.Throw<NotSupportedException>(() => legacyClient.Request<string>(new HttpMethod("PROPFIND"), "/resource"));
        Should.Throw<NotSupportedException>(() => request.GetResponseAsync());
        Should.Throw<NotSupportedException>(() => request.UseInterceptor(new NoopInterceptor()));
        using var destination = new MemoryStream();
        Should.Throw<NotSupportedException>(() => request.DownloadToWithResultAsync(destination));
        Should.Throw<NotSupportedException>(() => request.ObserveCompletion(_ => { }));
        Should.Throw<NotSupportedException>(() => request.BinaryContent(new byte[] { 1 }));
        Should.Throw<NotSupportedException>(() => request.ContentFactory(() => new ByteArrayContent(new byte[] { 1 })));
        Should.Throw<NotSupportedException>(() => request.ObserveDownloadProgress(_ => { }));
        Should.Throw<NotSupportedException>(() => request.GetJsonStreamAsync());
    }

    /// <summary>

    /// 验证复制响应头时保留不区分大小写的多值内容。

    /// </summary>
    [Fact]
    public void HttpResponseResult_CopiesCaseInsensitiveMultiValueHeaders()
    {
        IReadOnlyDictionary<string, IReadOnlyList<string>> headers =
            new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["X-Trace"] = new[] { "one", "two" }
            };
        var result = new HttpResponseResult<string>(HttpStatusCode.OK, headers,
            new Dictionary<string, IReadOnlyList<string>>(), "data", null);

        result.IsSuccessStatusCode.ShouldBeTrue();
        result.Headers["x-trace"].ShouldBe(new[] { "one", "two" });
        Should.Throw<NotSupportedException>(() =>
            ((IList<string>)result.Headers["X-Trace"]).Add("three"));
        result.RawContent.ShouldBeNull();
    }

    /// <summary>

    /// 验证公开构造函数会防御性复制响应头并值。

    /// </summary>
    [Fact]
    public void HttpResponseResult_PublicConstructorDefensivelyCopiesHeadersAndValues()
    {
        var responseValues = new[] { "one", "two" };
        var contentValues = new[] { "text/plain" };
        var responseHeaders = new Dictionary<string, IReadOnlyList<string>>
        {
            ["X-Trace"] = responseValues
        };
        var contentHeaders = new Dictionary<string, IReadOnlyList<string>>
        {
            ["Content-Type"] = contentValues
        };
        var result = new HttpResponseResult<string>(HttpStatusCode.OK, responseHeaders,
            contentHeaders, "data", null);

        responseValues[0] = "changed";
        contentValues[0] = "changed";
        responseHeaders["X-Trace"] = new[] { "replaced" };
        contentHeaders.Clear();

        result.Headers["x-trace"].ShouldBe(new[] { "one", "two" });
        result.ContentHeaders["content-type"].ShouldBe(new[] { "text/plain" });
        Should.Throw<NotSupportedException>(() =>
            ((IList<string>)result.ContentHeaders["Content-Type"]).Add("extra"));
    }

    /// <summary>

    /// 验证 HEAD 或空正文跳过正文限制并执行成功回调。

    /// </summary>
    [Fact]
    public async Task GetResponseAsync_HeadNoContentAndEmptyBodySkipLimitsAndRunSuccessCallback()
    {
        using var client = new HttpClient(new StubHandler((request, _) =>
        {
            if (request.Method == HttpMethod.Head)
            {
                var head = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Array.Empty<byte>()) };
                head.Content.Headers.ContentLength = 64;
                return head;
            }
            return request.RequestUri.AbsolutePath == "/no-content"
                ? new HttpResponseMessage(HttpStatusCode.NoContent) { Content = new StringContent("ignored") }
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };
        }));
        var callbackCount = 0;

        var head = await new HttpRequest<string>(null, client, HttpMethod.Head, "https://example.test/head")
            .MaxResponseBytes(0).OnSuccess(_ => callbackCount++).GetResponseAsync(includeRawContent: true);
        var noContent = await new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/no-content")
            .MaxResponseBytes(0).OnSuccess(_ => callbackCount++).GetResponseAsync(includeRawContent: true);
        var empty = await new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/empty")
            .MaxResponseBytes(0).OnSuccess(_ => callbackCount++).GetResponseAsync(includeRawContent: true);

        foreach (var result in new[] { head, noContent, empty })
        {
            result.Data.ShouldBeNull();
            result.RawContent.ShouldBe(string.Empty);
        }
        callbackCount.ShouldBe(3);
    }

    /// <summary>

    /// 验证读取结果时忽略声明长度并执行回调。

    /// </summary>
    /// <param name="head">测试输入值。</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task NoBodyResponse_GetResultAsyncSkipsDeclaredLengthAndRunsCallbacks(bool head)
    {
        using var client = new HttpClient(new StubHandler((_, _) => CreateNoBodyResponse(head)));
        string success = "not called";
        object completed = null;
        var completionCount = 0;

        var result = await new HttpRequest<string>(null, client,
                head ? HttpMethod.Head : HttpMethod.Get, "https://example.test/")
            .MaxResponseBytes(0)
            .OnSuccess(value => success = value)
            .OnComplete((_, content) => { completed = content; completionCount++; })
            .GetResultAsync();

        result.ShouldBeNull();
        success.ShouldBeNull();
        completed.ShouldBe(string.Empty);
        completionCount.ShouldBe(1);
    }

    /// <summary>

    /// 验证获取流跳过声明的长度并执行完成。

    /// </summary>
    /// <param name="head">测试输入值。</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task NoBodyResponse_GetStreamAsyncSkipsDeclaredLengthAndRunsComplete(bool head)
    {
        using var client = new HttpClient(new StubHandler((_, _) => CreateNoBodyResponse(head)));
        object completed = null;
        var completionCount = 0;

        var bytes = await new HttpRequest<string>(null, client,
                head ? HttpMethod.Head : HttpMethod.Get, "https://example.test/")
            .MaxResponseBytes(0)
            .OnComplete((_, content) => { completed = content; completionCount++; })
            .GetStreamAsync();

        bytes.ShouldBeEmpty();
        ((byte[])completed).ShouldBeEmpty();
        completionCount.ShouldBe(1);
    }

    /// <summary>

    /// 验证下载到跳过声明的长度并执行完成。

    /// </summary>
    /// <param name="head">测试输入值。</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task NoBodyResponse_DownloadToAsyncSkipsDeclaredLengthAndRunsComplete(bool head)
    {
        using var client = new HttpClient(new StubHandler((_, _) => CreateNoBodyResponse(head)));
        using var destination = new MemoryStream();
        object completed = new object();
        var completionCount = 0;

        await new HttpRequest<string>(null, client,
                head ? HttpMethod.Head : HttpMethod.Get, "https://example.test/")
            .MaxResponseBytes(0)
            .OnComplete((_, content) => { completed = content; completionCount++; })
            .DownloadToAsync(destination);

        destination.ToArray().ShouldBeEmpty();
        completed.ShouldBeNull();
        completionCount.ShouldBe(1);
    }

    /// <summary>

    /// 验证观察到请求地址根据到基址所有者。

    /// </summary>
    /// <param name="requestBaseAddress">测试输入值。</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Interceptor_SeesRequestUriAccordingToBaseAddressOwner(bool requestBaseAddress)
    {
        Uri before = null;
        Uri sent = null;
        using var client = new HttpClient(new StubHandler((request, _) =>
        {
            sent = request.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") };
        }))
        { BaseAddress = new Uri("https://example.test/api/") };

        var request = new HttpRequest<string>(null, client, HttpMethod.Get, "resource")
            .UseInterceptor(new CallbackInterceptor(context => before = context.Request.RequestUri));
        if (requestBaseAddress)
            request.BaseAddress("https://example.test/api/");

        (await request.GetResultAsync()).ShouldBe("ok");
        sent.ShouldBe(new Uri("https://example.test/api/resource"));
        before.IsAbsoluteUri.ShouldBe(requestBaseAddress);
        if (requestBaseAddress)
            before.ShouldBe(sent);
        else
            before.OriginalString.ShouldBe("resource");
    }

    /// <summary>

    /// 创建测试所需的辅助对象或数据。

    /// </summary>
    private static HttpResponseMessage CreateNoBodyResponse(bool head)
    {
        var response = new HttpResponseMessage(head ? HttpStatusCode.OK : HttpStatusCode.NoContent)
        {
            Content = new ByteArrayContent(Encoding.UTF8.GetBytes("unexpected"))
        };
        response.Content.Headers.ContentLength = 64;
        return response;
    }

    /// <summary>

    /// 验证原始正文是否显式启用并异常正文使用声明的字符集。

    /// </summary>
    [Fact]
    public async Task GetResponseAsync_RawContentIsOptInAndErrorBodyUsesDeclaredCharset()
    {
        using var client = new HttpClient(new StubHandler((request, _) =>
        {
            var content = new ByteArrayContent(Encoding.GetEncoding("iso-8859-1").GetBytes("café"));
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
            content.Headers.ContentType.CharSet = "iso-8859-1";
            return new HttpResponseMessage(HttpStatusCode.BadRequest) { RequestMessage = request, Content = content };
        }));

        var defaultResult = await new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/").GetResponseAsync();
        var rawResult = await new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
            .GetResponseAsync(includeRawContent: true);

        defaultResult.Data.ShouldBeNull();
        defaultResult.RawContent.ShouldBeNull();
        rawResult.IsSuccessStatusCode.ShouldBeFalse();
        rawResult.Data.ShouldBeNull();
        rawResult.RawContent.ShouldBe("café");
    }

    /// <summary>

    /// 验证仅含字节顺序标记的正文按空内容转换。

    /// </summary>
    [Fact]
    public async Task GetResponseAsync_BomOnlyBodyIsNonEmptyForConversion()
    {
        var convertCalls = 0;
        using var client = new HttpClient(new StubHandler((_, _) =>
        {
            var content = new ByteArrayContent(Encoding.UTF8.GetPreamble());
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        }));
        var result = await new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/bom-only")
            .OnConvert(_ => { convertCalls++; return "converted"; })
            .GetResponseAsync(includeRawContent: true);
        convertCalls.ShouldBe(1);
        result.Data.ShouldBe("converted");
        result.RawContent.ShouldBe(string.Empty);
    }

    /// <summary>

    /// 验证根据 UTF-16 字节顺序标记和带引号的 ISO 字符集解码正文。

    /// </summary>
    [Fact]
    public async Task GetResponseAsync_DetectsUtf16BomAndQuotedIsoCharset()
    {
        using var client = new HttpClient(new StubHandler((request, _) =>
        {
            if (request.RequestUri.AbsolutePath == "/utf16")
            {
                var bytes = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes("snowman \u2603")).ToArray();
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) };
            }
            var iso = Encoding.GetEncoding("iso-8859-1").GetBytes("café");
            var content = new ByteArrayContent(iso);
            content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(
                "text/plain; charset=\"iso-8859-1\"");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        }));
        var utf16 = await new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/utf16")
            .GetResponseAsync(includeRawContent: true);
        var quotedCharset = await new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/iso")
            .GetResponseAsync(includeRawContent: true);
        utf16.Data.ShouldBe("snowman \u2603");
        utf16.RawContent.ShouldBe("snowman \u2603");
        quotedCharset.Data.ShouldBe("café");
        quotedCharset.RawContent.ShouldBe("café");
    }

    /// <summary>

    /// 验证无效字符集传播并释放正文。

    /// </summary>
    [Fact]
    public async Task GetResponseAsync_InvalidCharsetPropagatesAndDisposesContent()
    {
        var content = new CountingContent("payload");
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain")
        { CharSet = "no-such-encoding" };
        using var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = content }));
        await Should.ThrowAsync<ArgumentException>(() =>
            new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/invalid-charset")
                .GetResponseAsync());
        content.IsDisposed.ShouldBeTrue();
    }

    /// <summary>

    /// 验证空正文跳过转换期间获取结果转换空字符串。

    /// </summary>
    [Fact]
    public async Task GetResponseAsync_EmptyBodySkipsConversionWhileGetResultConvertsEmptyString()
    {
        var convertCalls = 0;
        using var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Array.Empty<byte>()) }));
        var response = await new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/empty-response")
            .OnConvert(value => { convertCalls++; return "converted:" + value; })
            .GetResponseAsync(includeRawContent: true);
        response.Data.ShouldBeNull();
        response.RawContent.ShouldBe(string.Empty);
        convertCalls.ShouldBe(0);
        var result = await new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/empty-result")
            .OnConvert(value => { convertCalls++; return "converted:" + value; })
            .GetResultAsync();
        result.ShouldBe("converted:");
        convertCalls.ShouldBe(1);
    }

    /// <summary>

    /// 验证失败和完成回调均接收解码后的正文。

    /// </summary>
    [Fact]
    public async Task GetResponseAsync_FailAndCompleteCallbacksReceiveDecodedBody()
    {
        object failedContent = null;
        object completedContent = null;
        using var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.BadRequest)
            { Content = new StringContent("rejected", Encoding.UTF8, "text/plain") }));
        var result = await new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/failure-callbacks")
            .OnFail((_, body) => failedContent = body)
            .OnComplete((_, body) => completedContent = body)
            .GetResponseAsync(includeRawContent: true);
        result.Data.ShouldBeNull();
        result.RawContent.ShouldBe("rejected");
        failedContent.ShouldBe("rejected");
        completedContent.ShouldBe("rejected");
    }

    /// <summary>

    /// 验证限制准确响应字节上限。

    /// </summary>
    /// <param name="limit">测试输入值。</param>
    /// <param name="succeeds">测试输入值。</param>
    [Theory]
    [InlineData(4, true)]
    [InlineData(3, false)]
    public async Task GetResponseAsync_EnforcesExactResponseByteLimit(long limit, bool succeeds)
    {
        var content = new CountingContent("four");
        using var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = content }));
        var request = new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/limit")
            .MaxResponseBytes(limit);
        if (succeeds)
        {
            var result = await request.GetResponseAsync(includeRawContent: true);
            result.Data.ShouldBe("four");
            result.RawContent.ShouldBe("four");
        }
        else
            await Should.ThrowAsync<HttpResponseSizeLimitException>(() => request.GetResponseAsync());
        content.IsDisposed.ShouldBeTrue();
    }

    /// <summary>

    /// 验证正文读取失败时保留原始异常并释放正文。

    /// </summary>
    [Fact]
    public async Task GetResponseAsync_SourceReadFailurePreservesExceptionAndDisposesContent()
    {
        var sourceError = new IOException("response source failed");
        var content = new FailingReadContent(sourceError);
        using var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = content }));
        var error = await Should.ThrowAsync<IOException>(() =>
            new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/read-failure")
                .GetResponseAsync());
        error.ShouldBeSameAs(sourceError);
        content.IsDisposed.ShouldBeTrue();
    }

    /// <summary>

    /// 验证读取正文一次并释放响应正文。

    /// </summary>
    [Fact]
    public async Task GetResponseAsync_ReadsBodyOnceAndDisposesResponseContent()
    {
        var content = new CountingContent("payload");
        using var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = content }));

        var result = await new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
            .GetResponseAsync(includeRawContent: true);

        result.Data.ShouldBe("payload");
        result.RawContent.ShouldBe("payload");
        content.SerializeCount.ShouldBe(1);
        content.IsDisposed.ShouldBeTrue();
    }

    /// <summary>

    /// 验证快照响应头并释放正文时转换失败。

    /// </summary>
    [Fact]
    public async Task GetResponseAsync_SnapshotsHeadersAndDisposesContentWhenConversionFails()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.Add("X-Multi", new[] { "one", "two" });
        var content = new CountingContent("{");
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        response.Content = content;
        var completed = false;
        using var client = new HttpClient(new StubHandler((_, _) => response));

        await Should.ThrowAsync<System.Text.Json.JsonException>(() =>
            new HttpRequest<Dictionary<string, string>>(null, client, HttpMethod.Get, "https://example.test/")
                .OnComplete((_, _) => completed = true)
                .GetResponseAsync());

        completed.ShouldBeTrue();
        content.IsDisposed.ShouldBeTrue();
        response.Headers.Remove("X-Multi");
        response.Headers.Add("X-Multi", "changed");
    }

    /// <summary>

    /// 验证转换异常取得优先于完成回调完成异常。

    /// </summary>
    [Fact]
    public async Task GetResponseAsync_ConversionErrorTakesPrecedenceOverOnCompleteError()
    {
        using var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{", Encoding.UTF8, "application/json")
            }));

        var error = await Should.ThrowAsync<System.Text.Json.JsonException>(() =>
            new HttpRequest<Dictionary<string, string>>(null, client, HttpMethod.Get, "https://example.test/")
                .OnComplete((_, _) => throw new InvalidOperationException("complete callback failed"))
                .GetResponseAsync());

        error.ShouldBeOfType<System.Text.Json.JsonException>();
    }

    /// <summary>

    /// 验证解析 JSON 并执行转换回调。

    /// </summary>
    [Fact]
    public async Task GetResponseAsync_ConvertsJsonAndHonorsOnConvert()
    {
        using var client = new HttpClient(new StubHandler((_, _) =>
        {
            var content = new StringContent("{\"name\":\"Ada\"}", Encoding.UTF8, "application/json");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        }));
        var json = await new HttpRequest<Dictionary<string, string>>(null, client, HttpMethod.Get, "https://example.test/")
            .GetResponseAsync(includeRawContent: true);
        var converted = await new HttpRequest<Dictionary<string, string>>(null, client, HttpMethod.Get, "https://example.test/")
            .OnConvert(_ => new Dictionary<string, string> { ["name"] = "converted" })
            .GetResponseAsync();

        json.Data["name"].ShouldBe("Ada");
        json.RawContent.ShouldBe("{\"name\":\"Ada\"}");
        converted.Data["name"].ShouldBe("converted");
    }

    /// <summary>

    /// 验证超过响应上限释放正文。

    /// </summary>
    [Fact]
    public async Task GetResponseAsync_ExceedingResponseLimitDisposesContent()
    {
        var content = new CountingContent("larger than limit");
        using var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = content }));

        await Should.ThrowAsync<HttpResponseSizeLimitException>(() =>
            new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
                .MaxResponseBytes(2)
                .GetResponseAsync());

        content.IsDisposed.ShouldBeTrue();
    }

    /// <summary>

    /// 验证取消期间正文读取释放响应流。

    /// </summary>
    [Fact]
    public async Task GetResponseAsync_CancelDuringBodyReadDisposesResponseStream()
    {
        var readStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var stream = new CancellationStream(readStarted);
        using var cancellation = new CancellationTokenSource();
        using var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(stream) }));
        var execution = new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
            .GetResponseAsync(cancellationToken: cancellation.Token);

        await readStarted.Task;
        cancellation.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() => execution);
        stream.IsDisposed.ShouldBeTrue();
    }

    /// <summary>

    /// 验证On成功失败仍释放响应正文。

    /// </summary>
    [Fact]
    public async Task GetResponseAsync_OnSuccessFailureStillDisposesResponseContent()
    {
        var content = new CountingContent("ok");
        using var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = content }));

        await Should.ThrowAsync<InvalidOperationException>(() =>
            new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
                .OnSuccess(_ => throw new InvalidOperationException("success callback failed"))
                .GetResponseAsync());

        content.IsDisposed.ShouldBeTrue();
    }

    /// <summary>

    /// 验证On完成失败仍释放响应正文。

    /// </summary>
    [Fact]
    public async Task GetResponseAsync_OnCompleteFailureStillDisposesResponseContent()
    {
        var content = new CountingContent("ok");
        using var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = content }));

        await Should.ThrowAsync<InvalidOperationException>(() =>
            new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
                .OnComplete((_, _) => throw new InvalidOperationException("complete callback failed"))
                .GetResponseAsync());

        content.IsDisposed.ShouldBeTrue();
    }

    /// <summary>

    /// 验证响应回调运行期间拒绝发送新请求。

    /// </summary>
    [Fact]
    public async Task GetResponseAsync_SendBeforeRejectsWithoutRunningResponseCallbacks()
    {
        var sends = 0;
        var callbacks = 0;
        using var client = new HttpClient(new StubHandler((_, _) =>
        {
            sends++;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("unexpected") };
        }));

        var result = await new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
            .OnSendBefore(_ => false)
            .OnSuccess(_ => callbacks++)
            .OnFail((_, _) => callbacks++)
            .OnComplete((_, _) => callbacks++)
            .GetResponseAsync();

        result.ShouldBeNull();
        sends.ShouldBe(0);
        callbacks.ShouldBe(0);
    }

    /// <summary>

    /// 验证返回元数据并实际写入量字节。

    /// </summary>
    [Fact]
    public async Task DownloadToWithResultAsync_ReturnsMetadataAndActualWrittenBytes()
    {
        var bytes = new byte[] { 2, 4, 6, 8 };
        var response = new HttpResponseMessage(HttpStatusCode.PartialContent)
        {
            Content = new ByteArrayContent(bytes)
        };
        response.Headers.Add("X-Trace", new[] { "one", "two" });
        response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        using var client = new HttpClient(new StubHandler((_, _) => response));
        using var destination = new MemoryStream();

        var result = await new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
            .DownloadToWithResultAsync(destination);

        result.StatusCode.ShouldBe(HttpStatusCode.PartialContent);
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.Headers["x-trace"].ShouldBe(new[] { "one", "two" });
        result.ContentHeaders["content-type"].Single().ShouldContain("application/octet-stream");
        result.BytesWritten.ShouldBe(bytes.Length);
        result.IsFileSaved.ShouldBeFalse();
        destination.ToArray().ShouldBe(bytes);
        response.Headers.Remove("X-Trace");
        response.Headers.Add("X-Trace", "changed");
        result.Headers["X-Trace"].ShouldBe(new[] { "one", "two" });
    }

    /// <summary>

    /// 验证 HTTP 失败响应仍返回元数据且不写入正文。

    /// </summary>
    [Fact]
    public async Task DownloadToWithResultAsync_HttpFailureReturnsMetadataWithoutWritingBody()
    {
        using var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("missing") }));
        using var destination = new MemoryStream();

        var result = await new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
            .DownloadToWithResultAsync(destination);

        result.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.BytesWritten.ShouldBe(0);
        result.IsFileSaved.ShouldBeFalse();
        destination.ToArray().ShouldBeEmpty();
    }

    /// <summary>

    /// 验证 HEAD 请求的正文长度未知时遵循正文规则。

    /// </summary>
    [Fact]
    public async Task DownloadToWithResultAsync_HeadNoContentAndUnknownLengthRespectBodyRules()
    {
        using var client = new HttpClient(new StubHandler((request, _) =>
        {
            if (request.Method == HttpMethod.Head)
            {
                var head = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Array.Empty<byte>()) };
                head.Content.Headers.ContentLength = 100;
                return head;
            }
            if (request.RequestUri.AbsolutePath == "/empty")
            {
                var noContent = new HttpResponseMessage(HttpStatusCode.NoContent) { Content = new ByteArrayContent(Array.Empty<byte>()) };
                noContent.Content.Headers.ContentLength = 100;
                return noContent;
            }
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new UnknownLengthContent("body") };
        }));
        using var headDestination = new MemoryStream();
        using var emptyDestination = new MemoryStream();
        using var unknownLengthDestination = new MemoryStream();

        var headResult = await new HttpRequest<string>(null, client, HttpMethod.Head, "https://example.test/head")
            .MaxResponseBytes(0).DownloadToWithResultAsync(headDestination);
        var emptyResult = await new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/empty")
            .MaxResponseBytes(0).DownloadToWithResultAsync(emptyDestination);
        var unknownLengthResult = await new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/body")
            .MaxResponseBytes(4).DownloadToWithResultAsync(unknownLengthDestination);

        headResult.StatusCode.ShouldBe(HttpStatusCode.OK);
        emptyResult.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        headResult.BytesWritten.ShouldBe(0);
        emptyResult.BytesWritten.ShouldBe(0);
        headDestination.ToArray().ShouldBeEmpty();
        emptyDestination.ToArray().ShouldBeEmpty();
        unknownLengthResult.BytesWritten.ShouldBe(4);
        unknownLengthDestination.ToArray().ShouldBe(Encoding.UTF8.GetBytes("body"));
    }

    /// <summary>

    /// 验证拒绝On发送之后并保留目标所有权。

    /// </summary>
    [Fact]
    public void DownloadToWithResultAsync_RejectsOnSendAfterAndPreservesDestinationOwnership()
    {
        using var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("unused") }));
        using var destination = new MemoryStream();
        var request = new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
            .OnSendAfter(_ => Task.FromResult("custom"));

        Should.Throw<NotSupportedException>(() => request.DownloadToWithResultAsync(destination));

        destination.CanWrite.ShouldBeTrue();
        destination.WriteByte(1);
        destination.ToArray().ShouldBe(new byte[] { 1 });
    }

    /// <summary>

    /// 验证超限或取消时释放响应并保持目标流打开。

    /// </summary>
    [Fact]
    public async Task DownloadToWithResultAsync_LimitAndCancellationDisposeResponseButKeepDestinationOpen()
    {
        var content = new CountingContent("too large");
        using var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = content }));
        using var destination = new MemoryStream();

        await Should.ThrowAsync<HttpResponseSizeLimitException>(() =>
            new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
                .MaxResponseBytes(2).DownloadToWithResultAsync(destination));

        content.IsDisposed.ShouldBeTrue();
        destination.CanWrite.ShouldBeTrue();

        var readStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var bodyStream = new CancellationStream(readStarted);
        using var cancelClient = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(bodyStream) }));
        using var cancellation = new CancellationTokenSource();
        var cancelDestination = new MemoryStream();
        var execution = new HttpRequest<string>(null, cancelClient, HttpMethod.Get, "https://example.test/")
            .DownloadToWithResultAsync(cancelDestination, cancellation.Token);
        await readStarted.Task;
        cancellation.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() => execution);
        bodyStream.IsDisposed.ShouldBeTrue();
        cancelDestination.CanWrite.ShouldBeTrue();
        cancelDestination.Dispose();
    }

    /// <summary>

    /// 验证保留解析并业务回调异常。

    /// </summary>
    [Fact]
    public async Task ObserveCompletion_PreservesParsingAndBusinessCallbackErrors()
    {
        using var jsonClient = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent("{", Encoding.UTF8, "application/json") }));
        var parseCompletion = new List<HttpRequestCompletion>();
        var parseError = await Should.ThrowAsync<System.Text.Json.JsonException>(() =>
            new HttpRequest<Dictionary<string, string>>(null, jsonClient, HttpMethod.Get, "https://example.test/")
                .ObserveCompletion(parseCompletion.Add).GetResultAsync());

        parseCompletion.Single().Outcome.ShouldBe(HttpRequestOutcome.Faulted);
        parseCompletion.Single().Exception.ShouldBeSameAs(parseError);

        using var callbackClient = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") }));
        var callbackCompletion = new List<HttpRequestCompletion>();
        var callbackError = new InvalidOperationException("success callback failed");
        var error = await Should.ThrowAsync<InvalidOperationException>(() =>
            new HttpRequest<string>(null, callbackClient, HttpMethod.Get, "https://example.test/")
                .OnSuccess(_ => throw callbackError)
                .OnComplete((_, _) => throw new ApplicationException("complete callback failed"))
                .ObserveCompletion(callbackCompletion.Add).GetResultAsync());

        error.ShouldBeSameAs(callbackError);
        callbackCompletion.Single().Outcome.ShouldBe(HttpRequestOutcome.Faulted);
        callbackCompletion.Single().Exception.ShouldBeSameAs(callbackError);
    }

    /// <summary>

    /// 验证报告成功并重试尝试一次。

    /// </summary>
    [Fact]
    public async Task ObserveCompletion_ReportsSuccessAndRetryAttemptsOnce()
    {
        var sends = 0;
        var completions = new List<HttpRequestCompletion>();
        using var client = new HttpClient(new StubHandler((_, _) =>
        {
            sends++;
            return new HttpResponseMessage(sends == 1 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK)
            { Content = new StringContent("ok") };
        }));
        var options = new HttpRetryOptions { MaxRetries = 1, BaseDelay = TimeSpan.Zero, MaxDelay = TimeSpan.Zero, UseJitter = false };
        var request = new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/").Retry(options);
        var returnedRequest = request.ObserveCompletion(completions.Add);

        ReferenceEquals(request, returnedRequest).ShouldBeTrue();
        (await request.GetResultAsync()).ShouldBe("ok");

        completions.Count.ShouldBe(1);
        completions[0].StatusCode.ShouldBe(HttpStatusCode.OK);
        completions[0].Attempts.ShouldBe(2);
        completions[0].Elapsed.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
        completions[0].Outcome.ShouldBe(HttpRequestOutcome.Success);
        completions[0].Exception.ShouldBeNull();
    }

    /// <summary>

    /// 验证观察器并异常处理程序失败时不变化请求结果。

    /// </summary>
    [Fact]
    public async Task ObserveCompletion_ObserverAndErrorHandlerFailuresDoNotChangeRequestResult()
    {
        var observerCalls = 0;
        var errorHandlerCalls = 0;
        Exception observedError = null;
        var observerError = new InvalidOperationException("observer failed");
        using var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") }));

        var result = await new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
            .ObserveCompletion(_ =>
            {
                observerCalls++;
                throw observerError;
            }, error =>
            {
                errorHandlerCalls++;
                observedError = error;
                throw new ApplicationException("observer error handler failed");
            })
            .GetResultAsync();

        result.ShouldBe("ok");
        observerCalls.ShouldBe(1);
        errorHandlerCalls.ShouldBe(1);
        observedError.ShouldBeSameAs(observerError);
    }

    /// <summary>

    /// 验证发送失败保留异常并报告尝试。

    /// </summary>
    [Fact]
    public async Task ObserveCompletion_SendFailurePreservesExceptionAndReportsAttempt()
    {
        var completions = new List<HttpRequestCompletion>();
        var expectedError = new HttpRequestException("send failed");
        using var client = new HttpClient(new StubHandler((_, _) => throw expectedError));
        var options = new HttpRetryOptions
        {
            MaxRetries = 3,
            BaseDelay = TimeSpan.Zero,
            MaxDelay = TimeSpan.Zero,
            UseJitter = false
        };

        var error = await Should.ThrowAsync<HttpRequestException>(() =>
            new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
                .Retry(options)
                .ObserveCompletion(completions.Add)
                .GetResultAsync());

        error.ShouldBeSameAs(expectedError);
        completions.Single().StatusCode.ShouldBeNull();
        completions.Single().Attempts.ShouldBe(4);
        completions.Single().Outcome.ShouldBe(HttpRequestOutcome.Faulted);
        completions.Single().Exception.ShouldBeSameAs(expectedError);
    }

    /// <summary>

    /// 验证报告 HTTP 失败并拒绝该请求。

    /// </summary>
    [Fact]
    public async Task ObserveCompletion_ReportsHttpFailureAndRejectedRequest()
    {
        var completions = new List<HttpRequestCompletion>();
        using var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.BadGateway) { Content = new StringContent("failed") }));
        var failedRequest = new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
            .ObserveCompletion(completions.Add);

        (await failedRequest.GetResultAsync()).ShouldBeNull();

        completions.Single().StatusCode.ShouldBe(HttpStatusCode.BadGateway);
        completions.Single().Attempts.ShouldBe(1);
        completions.Single().Outcome.ShouldBe(HttpRequestOutcome.HttpFailure);
        completions.Single().Exception.ShouldBeNull();

        completions.Clear();
        var rejectedRequest = new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
            .OnSendBefore(_ => false)
            .ObserveCompletion(completions.Add);
        (await rejectedRequest.GetResultAsync()).ShouldBeNull();

        completions.Single().StatusCode.ShouldBeNull();
        completions.Single().Attempts.ShouldBe(0);
        completions.Single().Outcome.ShouldBe(HttpRequestOutcome.Rejected);
        completions.Single().Exception.ShouldBeNull();
    }

    /// <summary>

    /// 验证分别报告取消和超时结果。

    /// </summary>
    [Fact]
    public async Task ObserveCompletion_ReportsCancellationAndTimeoutSeparately()
    {
        var cancelledCompletion = new List<HttpRequestCompletion>();
        var cancelEntered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellation = new CancellationTokenSource();
        using var cancelClient = new HttpClient(new AsyncStubHandler(async (_, token) =>
        {
            cancelEntered.TrySetResult(true);
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }));
        var cancelled = new HttpRequest<string>(null, cancelClient, HttpMethod.Get, "https://example.test/")
            .ObserveCompletion(cancelledCompletion.Add);
        var cancelledExecution = cancelled.GetResultAsync(cancellation.Token);
        await cancelEntered.Task;
        cancellation.Cancel();
        await Should.ThrowAsync<OperationCanceledException>(() => cancelledExecution);

        cancelledCompletion.Single().StatusCode.ShouldBeNull();
        cancelledCompletion.Single().Attempts.ShouldBe(1);
        cancelledCompletion.Single().Outcome.ShouldBe(HttpRequestOutcome.Cancelled);
        cancelledCompletion.Single().Exception.ShouldBeAssignableTo<OperationCanceledException>();

        var timedOutCompletion = new List<HttpRequestCompletion>();
        using var timeoutClient = new HttpClient(new AsyncStubHandler(async (_, token) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }));
        await Should.ThrowAsync<OperationCanceledException>(() =>
            new HttpRequest<string>(null, timeoutClient, HttpMethod.Get, "https://example.test/")
                .Timeout(TimeSpan.FromMilliseconds(50))
                .ObserveCompletion(timedOutCompletion.Add)
                .GetResultAsync());

        timedOutCompletion.Single().Attempts.ShouldBe(1);
        timedOutCompletion.Single().Outcome.ShouldBe(HttpRequestOutcome.TimedOut);
        timedOutCompletion.Single().Exception.ShouldBeAssignableTo<OperationCanceledException>();
    }

    /// <summary>

    /// 验证报告观察器异常未使用变化请求结果。

    /// </summary>
    [Fact]
    public async Task ObserveCompletion_ReportsObserverErrorWithoutChangingRequestResult()
    {
        var observerError = new InvalidOperationException("observer failed");
        var reportedErrors = new List<Exception>();
        using var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") }));
        var request = new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
            .ObserveCompletion(_ => throw observerError, reportedErrors.Add);

        (await request.GetResultAsync()).ShouldBe("ok");

        reportedErrors.ShouldBe(new[] { observerError });
    }

    /// <summary>

    /// 验证响应处理异常取得优先于完成回调完成异常。

    /// </summary>
    [Fact]
    public async Task GetResultAsync_ResponseProcessingErrorTakesPrecedenceOverOnCompleteError()
    {
        var content = new CountingContent("response");
        using var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = content }));

        var error = await Should.ThrowAsync<InvalidOperationException>(() =>
            new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
                .OnSendAfter(_ => Task.FromException<string>(new InvalidOperationException("response processing failed")))
                .OnComplete((_, _) => throw new InvalidOperationException("complete callback failed"))
                .GetResultAsync());

        error.Message.ShouldBe("response processing failed");
        content.IsDisposed.ShouldBeTrue();
    }

    /// <summary>

    /// 验证读取上限异常取得优先于完成回调完成异常。

    /// </summary>
    [Fact]
    public async Task GetStreamAsync_ReadLimitErrorTakesPrecedenceOverOnCompleteError()
    {
        using var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new CountingContent("long body") }));
        var completed = 0;

        var error = await Should.ThrowAsync<HttpResponseSizeLimitException>(() =>
            new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
                .MaxResponseBytes(2)
                .OnComplete((_, _) => { completed++; throw new InvalidOperationException("complete failed"); })
                .GetStreamAsync());

        error.ShouldBeOfType<HttpResponseSizeLimitException>();
        completed.ShouldBe(1);
    }

    /// <summary>

    /// 验证写入异常取得优先于完成回调完成异常。

    /// </summary>
    [Fact]
    public async Task DownloadToAsync_WriteErrorTakesPrecedenceOverOnCompleteError()
    {
        using var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("body") }));
        using var destination = new ThrowingDestinationStream();
        var completed = 0;

        var error = await Should.ThrowAsync<IOException>(() =>
            new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
                .OnComplete((_, _) => { completed++; throw new InvalidOperationException("complete failed"); })
                .DownloadToAsync(destination));

        error.Message.ShouldBe("destination write failed");
        completed.ShouldBe(1);
    }

    /// <summary>

    /// 验证取消取得优先于完成回调完成异常。

    /// </summary>
    [Fact]
    public async Task DownloadToAsync_CancellationTakesPrecedenceOverOnCompleteError()
    {
        var readStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var stream = new CancellationStream(readStarted);
        using var cancellation = new CancellationTokenSource();
        using var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(stream) }));
        using var destination = new MemoryStream();
        var completed = 0;
        var execution = new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
            .OnComplete((_, _) => { completed++; throw new InvalidOperationException("complete failed"); })
            .DownloadToAsync(destination, cancellation.Token);

        await readStarted.Task;
        cancellation.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() => execution);
        completed.ShouldBe(1);
        stream.IsDisposed.ShouldBeTrue();
    }

    /// <summary>

    /// 验证响应头快照在响应修改和释放后仍保持不变。

    /// </summary>
    [Fact]
    public async Task GetResponseAsync_ReturnedHeaderSnapshotSurvivesResponseMutationAndDisposal()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.Add("X-Multi", new[] { "one", "two" });
        var content = new StringContent("ok");
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        response.Content = content;
        using var client = new HttpClient(new StubHandler((_, _) => response));

        var result = await new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
            .GetResponseAsync();

        response.Headers.Remove("X-Multi");
        response.Headers.Add("X-Multi", "changed");
        response.Content.Headers.ContentType.MediaType = "application/json";
        result.Headers["x-multi"].ShouldBe(new[] { "one", "two" });
        result.ContentHeaders["content-type"].Single().ShouldContain("text/plain");
        Should.Throw<NotSupportedException>(() =>
            ((IList<string>)result.ContentHeaders["content-type"]).Add("changed"));
    }

    /// <summary>

    /// 验证拒绝任一发送之后重载之前发送。

    /// </summary>
    /// <param name="withCancellation">测试输入值。</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetResponseAsync_RejectsEitherSendAfterOverloadBeforeSending(bool withCancellation)
    {
        var sends = 0;
        using var client = new HttpClient(new StubHandler((_, _) =>
        {
            sends++;
            return new HttpResponseMessage(HttpStatusCode.OK);
        }));
        var request = new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/");
        if (withCancellation)
            request.OnSendAfter((_, _) => Task.FromResult("custom"));
        else
            request.OnSendAfter(_ => Task.FromResult("custom"));

        await Should.ThrowAsync<NotSupportedException>(() => request.GetResponseAsync());
        sends.ShouldBe(0);
    }

    /// <summary>

    /// 验证执行按顺序并按相反顺序执行前后处理发送。

    /// </summary>
    [Fact]
    public async Task Interceptors_RunInOrderAndReverseOrderAroundSend()
    {
        var calls = new List<string>();
        using var client = new HttpClient(new StubHandler((request, _) =>
        {
            calls.Add("send");
            request.Headers.GetValues("X-Signed").Single().ShouldBe("signed");
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("created")
            };
        }));
        var request = new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
            .UseInterceptor(new RecordingInterceptor("first", calls))
            .UseInterceptor(new RecordingInterceptor("second", calls));

        var result = await request.GetResponseAsync();

        result.StatusCode.ShouldBe(HttpStatusCode.Created);
        result.Data.ShouldBe("created");
        calls.ShouldBe(new[] { "first-before", "second-before", "send", "second-after", "first-after" });
    }

    /// <summary>

    /// 验证执行On发送失败并接收异常。

    /// </summary>
    [Fact]
    public async Task InterceptorAfter_RunsOnSendFailureAndReceivesException()
    {
        var afterCalled = false;
        Exception observed = null;
        using var client = new HttpClient(new StubHandler((_, _) => throw new HttpRequestException("send failed")));
        var request = new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
            .UseInterceptor(new HttpRequestInterceptorWithAfter(context =>
            {
                afterCalled = true;
                observed = context.Exception;
            }));

        await Should.ThrowAsync<HttpRequestException>(() => request.GetResultAsync());

        afterCalled.ShouldBeTrue();
        observed.ShouldBeOfType<HttpRequestException>();
    }

    /// <summary>

    /// 验证每次重试都会执行相应操作。

    /// </summary>
    [Fact]
    public async Task InterceptorBefore_RunsForEveryRetryAttempt()
    {
        var sends = 0;
        var attempts = new List<int>();
        var signatures = new List<string>();
        using var client = new HttpClient(new StubHandler((request, _) =>
        {
            sends++;
            signatures.Add(request.Headers.GetValues("X-Attempt-Signature").Single());
            return new HttpResponseMessage(sends == 1 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK)
            { Content = new StringContent("ok") };
        }));
        var retry = new HttpRetryOptions
        {
            MaxRetries = 1,
            BaseDelay = TimeSpan.Zero,
            MaxDelay = TimeSpan.Zero,
            UseJitter = false
        };

        var result = await new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
            .Retry(retry)
            .UseInterceptor(new AttemptInterceptor(attempts))
            .GetResultAsync();

        result.ShouldBe("ok");
        sends.ShouldBe(2);
        attempts.ShouldBe(new[] { 1, 2 });
        signatures.ShouldBe(new[] { "1", "2" });
    }

    /// <summary>

    /// 验证共享项目跨重试过程并隔离独立执行次数。

    /// </summary>
    [Fact]
    public async Task Interceptors_ShareItemsAcrossRetriesAndIsolateSeparateExecutions()
    {
        var sends = 0;
        var contexts = new List<HttpRequestInterceptorContext>();
        using var client = new HttpClient(new StubHandler((_, _) =>
        {
            sends++;
            return new HttpResponseMessage(sends == 1 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK)
            { Content = new StringContent("ok") };
        }));
        var options = new HttpRetryOptions { MaxRetries = 1, BaseDelay = TimeSpan.Zero, MaxDelay = TimeSpan.Zero, UseJitter = false };
        var request = new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
            .Retry(options)
            .UseInterceptor(new ItemsInterceptor(contexts));

        (await request.GetResultAsync()).ShouldBe("ok");
        (await request.GetResultAsync()).ShouldBe("ok");

        contexts.Select(context => context.Attempt).ShouldBe(new[] { 1, 2, 1 });
        ReferenceEquals(contexts[0].Items, contexts[1].Items).ShouldBeTrue();
        ReferenceEquals(contexts[0].Items, contexts[2].Items).ShouldBeFalse();
    }

    /// <summary>

    /// 验证仅逆序处理已进入的拦截器并是否不重试。

    /// </summary>
    [Fact]
    public async Task InterceptorBeforeFailure_OnlyUnwindsEnteredInterceptorsAndDoesNotRetry()
    {
        var events = new List<string>();
        var sends = 0;
        using var client = new HttpClient(new StubHandler((_, _) =>
        {
            sends++;
            return new HttpResponseMessage(HttpStatusCode.OK);
        }));
        var options = new HttpRetryOptions { MaxRetries = 2, BaseDelay = TimeSpan.Zero, MaxDelay = TimeSpan.Zero, UseJitter = false };

        await Should.ThrowAsync<InvalidOperationException>(() => new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
            .Retry(options)
            .UseInterceptor(new EventInterceptor("first", events))
            .UseInterceptor(new ThrowingBeforeInterceptor(events))
            .GetResultAsync());

        events.ShouldBe(new[] { "first-before", "throwing-before", "first-after" });
        sends.ShouldBe(0);
    }

    /// <summary>

    /// 验证清理失败时保留原始异常且不重试。

    /// </summary>
    [Fact]
    public async Task InterceptorBeforeFailure_PreservesOriginalErrorWhenCleanupFailsAndDoesNotRetry()
    {
        var events = new List<string>();
        var sends = 0;
        using var client = new HttpClient(new StubHandler((_, _) =>
        {
            sends++;
            return new HttpResponseMessage(HttpStatusCode.OK);
        }));
        var options = new HttpRetryOptions { MaxRetries = 2, BaseDelay = TimeSpan.Zero, MaxDelay = TimeSpan.Zero, UseJitter = false };
        var request = new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
            .Retry(options)
            .UseInterceptor(new CallbackInterceptor(
                _ => events.Add("cleanup-before"),
                _ => { events.Add("cleanup-after"); throw new InvalidOperationException("cleanup failed"); }))
            .UseInterceptor(new ThrowingBeforeInterceptor(events));

        var error = await Should.ThrowAsync<InvalidOperationException>(() => request.GetResultAsync());

        error.Message.ShouldBe("before failed");
        events.ShouldBe(new[] { "cleanup-before", "throwing-before", "cleanup-after" });
        sends.ShouldBe(0);
    }

    /// <summary>

    /// 验证继续逆序处理并发送失败取得优先级。

    /// </summary>
    [Fact]
    public async Task InterceptorAfterFailure_ContinuesUnwindAndSendFailureTakesPrecedence()
    {
        var events = new List<string>();
        using var client = new HttpClient(new StubHandler((_, _) => throw new HttpRequestException("send failed")));

        await Should.ThrowAsync<HttpRequestException>(() => new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
            .UseInterceptor(new EventInterceptor("first", events))
            .UseInterceptor(new ThrowingAfterInterceptor(events))
            .GetResultAsync());

        events.ShouldBe(new[] { "first-before", "throwing-after", "first-after" });
    }

    /// <summary>

    /// 验证传播首次相反顺序失败并继续逆序处理。

    /// </summary>
    [Fact]
    public async Task InterceptorAfterFailures_PropagatesFirstReverseOrderFailureAndContinuesUnwind()
    {
        var events = new List<string>();
        using var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") }));
        var request = new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
            .UseInterceptor(new CallbackInterceptor(
                _ => events.Add("first-before"),
                _ => { events.Add("first-after"); throw new InvalidOperationException("first after"); }))
            .UseInterceptor(new CallbackInterceptor(
                _ => events.Add("second-before"),
                _ => { events.Add("second-after"); throw new InvalidOperationException("second after"); }))
            .UseInterceptor(new EventInterceptor("last", events));

        var error = await Should.ThrowAsync<InvalidOperationException>(() => request.GetResultAsync());

        error.Message.ShouldBe("second after");
        events.ShouldBe(new[]
        {
            "first-before", "second-before", "last-before",
            "last-after", "second-after", "first-after"
        });
    }

    /// <summary>

    /// 验证可重试响应不会触发自动重试。

    /// </summary>
    [Fact]
    public async Task InterceptorAfterFailure_DoesNotRetryRetryableResponse()
    {
        var sends = 0;
        using var client = new HttpClient(new StubHandler((_, _) =>
        {
            sends++;
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        }));
        var options = new HttpRetryOptions { MaxRetries = 2, BaseDelay = TimeSpan.Zero, MaxDelay = TimeSpan.Zero, UseJitter = false };
        var request = new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
            .Retry(options)
            .UseInterceptor(new CallbackInterceptor(
                _ => { },
                _ => throw new InvalidOperationException("after failed")));

        var error = await Should.ThrowAsync<InvalidOperationException>(() => request.GetResultAsync());

        error.Message.ShouldBe("after failed");
        sends.ShouldBe(1);
    }

    /// <summary>

    /// 验证之后拦截器修改写入方法不能使当前尝试变为可重试。

    /// </summary>
    /// <param name="method">测试输入值。</param>
    /// <param name="sendThrows">测试输入值。</param>
    [Theory]
    [InlineData("POST", false)]
    [InlineData("POST", true)]
    [InlineData("PATCH", false)]
    [InlineData("PATCH", true)]
    public async Task Retry_AfterInterceptorChangingWriteMethodCannotMakeAttemptRetryable(
        string method, bool sendThrows)
    {
        var sends = 0;
        using var client = new HttpClient(new StubHandler((request, _) =>
        {
            request.Method.Method.ShouldBe(method);
            sends++;
            if (sendThrows) throw new HttpRequestException("send failed");
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        }));
        var options = new HttpRetryOptions
        {
            MaxRetries = 2,
            BaseDelay = TimeSpan.Zero,
            MaxDelay = TimeSpan.Zero,
            UseJitter = false
        };
        var request = new HttpRequest<string>(null, client, new HttpMethod(method), "https://example.test/")
            .Retry(options)
            .UseInterceptor(new CallbackInterceptor(_ => { }, context =>
                context.Request.Method = HttpMethod.Get));

        if (sendThrows)
            await Should.ThrowAsync<HttpRequestException>(() => request.GetResultAsync());
        else
            (await request.GetResultAsync()).ShouldBeNull();

        sends.ShouldBe(1);
    }

    /// <summary>

    /// 验证新增拦截器在下一次执行时生效。

    /// </summary>
    [Fact]
    public async Task Interceptors_AddedDuringExecutionApplyStartingWithNextExecution()
    {
        var events = new List<string>();
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") }));
        var request = new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
            .UseInterceptor(new RegistrationBarrierInterceptor(events, entered, release));

        var activeExecution = request.GetResultAsync();
        await entered.Task;
        request.UseInterceptor(new EventInterceptor("late", events));
        release.TrySetResult(true);

        (await activeExecution).ShouldBe("ok");
        events.ShouldBe(new[] { "initial-before", "initial-after" });

        (await request.GetResultAsync()).ShouldBe("ok");
        events.ShouldBe(new[]
        {
            "initial-before", "initial-after",
            "initial-before", "late-before", "late-after", "initial-after"
        });
    }

    /// <summary>

    /// 验证执行之后并传播取消。

    /// </summary>
    [Fact]
    public async Task InterceptorCancellation_RunsAfterAndPropagatesCancellation()
    {
        var enteredSend = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var afterCalled = false;
        using var cancellation = new CancellationTokenSource();
        using var client = new HttpClient(new AsyncStubHandler(async (_, token) =>
        {
            enteredSend.TrySetResult(true);
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }));
        var request = new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
            .UseInterceptor(new CallbackInterceptor(_ => { }, context =>
            {
                afterCalled = true;
                context.Exception.ShouldBeAssignableTo<OperationCanceledException>();
            }));

        var execution = request.GetResultAsync(cancellation.Token);
        await enteredSend.Task;
        cancellation.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() => execution);
        afterCalled.ShouldBeTrue();
    }

    /// <summary>

    /// 验证拒绝拦截器正文替换之前发送。

    /// </summary>
    [Fact]
    public async Task Retry_RejectsInterceptorBodyReplacementBeforeSending()
    {
        var sends = 0;
        var replacement = new CountingContent("replacement");
        using var client = new HttpClient(new StubHandler((_, _) =>
        {
            sends++;
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        }));
        var options = new HttpRetryOptions { MaxRetries = 1, BaseDelay = TimeSpan.Zero, MaxDelay = TimeSpan.Zero, UseJitter = false };
        options.AllowedMethods.Add(HttpMethod.Get);

        await Should.ThrowAsync<NotSupportedException>(() => new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
            .Retry(options)
            .UseInterceptor(new CallbackInterceptor(context => context.Request.Content = replacement))
            .GetResultAsync());

        sends.ShouldBe(0);
        replacement.IsDisposed.ShouldBeTrue();
    }

    /// <summary>

    /// 验证释放替换请求正文。

    /// </summary>
    [Fact]
    public async Task InterceptorSendCancellation_DisposesReplacedRequestContent()
    {
        var enteredSend = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var replacement = new CountingContent("replacement");
        using var cancellation = new CancellationTokenSource();
        using var client = new HttpClient(new AsyncStubHandler(async (_, token) =>
        {
            enteredSend.TrySetResult(true);
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }));
        var request = new HttpRequest<string>(null, client, HttpMethod.Post, "https://example.test/")
            .UseInterceptor(new CallbackInterceptor(context => context.Request.Content = replacement));
        var execution = request.GetResultAsync(cancellation.Token);

        await enteredSend.Task;
        cancellation.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() => execution);
        replacement.IsDisposed.ShouldBeTrue();
    }

    /// <summary>

    /// 验证替换请求正文释放每次之前正文。

    /// </summary>
    [Fact]
    public async Task InterceptorChain_ReplacingRequestBodyDisposesEachPreviousContent()
    {
        var original = new CountingContent("original");
        var intermediate = new CountingContent("intermediate");
        var final = new CountingContent("final");
        using var client = new HttpClient(new StubHandler((request, _) =>
        {
            request.Content.ShouldBeSameAs(final);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") };
        }));

        var result = await new HttpRequest<string>(null, client, HttpMethod.Post, "https://example.test/")
            .OnSendBefore(message => { message.Content = original; return true; })
            .UseInterceptor(new CallbackInterceptor(context => context.Request.Content = intermediate))
            .UseInterceptor(new CallbackInterceptor(context => context.Request.Content = final))
            .GetResultAsync();

        result.ShouldBe("ok");
        original.IsDisposed.ShouldBeTrue();
        intermediate.IsDisposed.ShouldBeTrue();
        final.IsDisposed.ShouldBeTrue();
    }

    /// <summary>

    /// 验证之前失败释放替换请求正文。

    /// </summary>
    [Fact]
    public async Task InterceptorChain_BeforeFailureDisposesReplacedRequestContent()
    {
        var original = new CountingContent("original");
        var replacement = new CountingContent("replacement");
        var sends = 0;
        using var client = new HttpClient(new StubHandler((_, _) =>
        {
            sends++;
            return new HttpResponseMessage(HttpStatusCode.OK);
        }));

        await Should.ThrowAsync<InvalidOperationException>(() =>
            new HttpRequest<string>(null, client, HttpMethod.Post, "https://example.test/")
                .OnSendBefore(message => { message.Content = original; return true; })
                .UseInterceptor(new CallbackInterceptor(context => context.Request.Content = replacement))
                .UseInterceptor(new ThrowingBeforeInterceptor(new List<string>()))
                .GetResultAsync());

        sends.ShouldBe(0);
        original.IsDisposed.ShouldBeTrue();
        replacement.IsDisposed.ShouldBeTrue();
    }

    /// <summary>

    /// 验证默认不对 PATCH、HEAD 和 OPTIONS 请求重试。

    /// </summary>
    [Fact]
    public async Task Retry_PatchIsExcludedByDefaultWhileHeadAndOptionsRetry()
    {
        var attemptsByMethod = new Dictionary<string, int>();
        using var client = new HttpClient(new StubHandler((request, _) =>
        {
            var method = request.Method.Method;
            attemptsByMethod.TryGetValue(method, out var attempts);
            attemptsByMethod[method] = ++attempts;
            return new HttpResponseMessage(attempts == 1 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK)
            { Content = new StringContent("ok") };
        }))
        { BaseAddress = new Uri("https://example.test/") };
        var service = new HttpClientService().SetHttpClient(client);
        var retry = new HttpRetryOptions { MaxRetries = 1, BaseDelay = TimeSpan.Zero, MaxDelay = TimeSpan.Zero, UseJitter = false };

        (await service.Patch("/patch").Retry(retry).GetResultAsync()).ShouldBeNull();
        (await service.Head("/head").Retry(retry).GetResultAsync()).ShouldBeNull();
        (await service.Options("/options").Retry(retry).GetResultAsync()).ShouldBe("ok");

        attemptsByMethod["PATCH"].ShouldBe(1);
        attemptsByMethod["HEAD"].ShouldBe(2);
        attemptsByMethod["OPTIONS"].ShouldBe(2);
    }

    /// <summary>

    /// 根据同步回调生成 HTTP 响应的消息处理器。

    /// </summary>
    private sealed class StubHandler : HttpMessageHandler
    {
        /// <summary>
        /// 测试处理流程使用的委托。
        /// </summary>
        private readonly Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> _send;
        /// <summary>
        /// 初始化一个<see cref="StubHandler"/>类型的实例。
        /// </summary>
        /// <param name="send">根据请求生成响应的委托。</param>
        public StubHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> send) => _send = send;
        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_send(request, cancellationToken));
    }

    /// <summary>

    /// 根据异步回调生成 HTTP 响应的消息处理器。

    /// </summary>
    private sealed class AsyncStubHandler : HttpMessageHandler
    {
        /// <summary>
        /// 测试处理流程使用的委托。
        /// </summary>
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _send;
        /// <summary>
        /// 初始化一个<see cref="AsyncStubHandler"/>类型的实例。
        /// </summary>
        /// <param name="send">根据请求生成响应的委托。</param>
        public AsyncStubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) => _send = send;
        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => _send(request, cancellationToken);
    }

    /// <summary>

    /// 通过 HttpClient 实现旧版 HTTP 客户端接口的适配器。

    /// </summary>
    private sealed class LegacyHttpClient : IHttpClient
    {
        /// <summary>
        /// 用于发送测试请求的 HTTP 客户端。
        /// </summary>
        private readonly HttpClient _client;
        /// <summary>
        /// 初始化一个<see cref="LegacyHttpClient"/>类型的实例。
        /// </summary>
        /// <param name="client">用于发送测试请求的 HTTP 客户端。</param>
        public LegacyHttpClient(HttpClient client) => _client = client;
        /// <inheritdoc />
        public IHttpRequest<string> Get(string url) => Create<string>(HttpMethod.Get, url);
        /// <inheritdoc />
        public IHttpRequest<string> Get(string url, object queryString) => Get(url);
        /// <inheritdoc />
        public IHttpRequest<TResult> Get<TResult>(string url) where TResult : class => Create<TResult>(HttpMethod.Get, url);
        /// <inheritdoc />
        public IHttpRequest<TResult> Get<TResult>(string url, object queryString) where TResult : class => Get<TResult>(url);
        /// <inheritdoc />
        public IHttpRequest<string> Post(string url) => Create<string>(HttpMethod.Post, url);
        /// <inheritdoc />
        public IHttpRequest<string> Post(string url, object content) => Create<string>(HttpMethod.Post, url).Content(content);
        /// <inheritdoc />
        public IHttpRequest<TResult> Post<TResult>(string url) where TResult : class => Create<TResult>(HttpMethod.Post, url);
        /// <inheritdoc />
        public IHttpRequest<TResult> Post<TResult>(string url, object content) where TResult : class => Create<TResult>(HttpMethod.Post, url).Content(content);
        /// <inheritdoc />
        public IHttpRequest<string> Put(string url) => Create<string>(HttpMethod.Put, url);
        /// <inheritdoc />
        public IHttpRequest<string> Put(string url, object content) => Create<string>(HttpMethod.Put, url).Content(content);
        /// <inheritdoc />
        public IHttpRequest<TResult> Put<TResult>(string url) where TResult : class => Create<TResult>(HttpMethod.Put, url);
        /// <inheritdoc />
        public IHttpRequest<TResult> Put<TResult>(string url, object content) where TResult : class => Create<TResult>(HttpMethod.Put, url).Content(content);
        /// <inheritdoc />
        public IHttpRequest<string> Delete(string url) => Create<string>(HttpMethod.Delete, url);
        /// <inheritdoc />
        public IHttpRequest<TResult> Delete<TResult>(string url) where TResult : class => Create<TResult>(HttpMethod.Delete, url);
        /// <summary>
        /// 创建指定方法和地址的测试请求。
        /// </summary>
        private IHttpRequest<TResult> Create<TResult>(HttpMethod method, string url) where TResult : class
            => new HttpRequest<TResult>(null, _client, method, new Uri(_client.BaseAddress, url).ToString());
    }

    /// <summary>

    /// 提供旧版 HTTP 请求接口的测试实现。

    /// </summary>
    private sealed class LegacyHttpRequest : IHttpRequest<string>
    {
        /// <inheritdoc />
        public IHttpRequest<string> HttpClientName(string name) => this;
        /// <inheritdoc />
        public IHttpRequest<string> BaseAddress(string baseAddress) => this;
        /// <inheritdoc />
        public IHttpRequest<string> Encoding(string encoding) => this;
        /// <inheritdoc />
        public IHttpRequest<string> Encoding(System.Text.Encoding encoding) => this;
        /// <inheritdoc />
        public IHttpRequest<string> BearerToken(string token) => this;
        /// <inheritdoc />
        public IHttpRequest<string> ContentType(HttpContentType contentType) => this;
        /// <inheritdoc />
        public IHttpRequest<string> ContentType(string contentType) => this;
        /// <inheritdoc />
        public IHttpRequest<string> Certificate(string path, string password) => this;
        /// <inheritdoc />
        public IHttpRequest<string> IgnoreSsl() => this;
        /// <inheritdoc />
        public IHttpRequest<string> JsonSerializerOptions(System.Text.Json.JsonSerializerOptions options) => this;
        /// <inheritdoc />
        public IHttpRequest<string> Timeout(int timeout) => this;
        /// <inheritdoc />
        public IHttpRequest<string> Timeout(TimeSpan timeout) => this;
        /// <inheritdoc />
        public IHttpRequest<string> Header(string key, string value) => this;
        /// <inheritdoc />
        public IHttpRequest<string> Header(IDictionary<string, string> headers) => this;
        /// <inheritdoc />
        public IHttpRequest<string> QueryString(string key, string value) => this;
        /// <inheritdoc />
        public IHttpRequest<string> QueryString(IDictionary<string, string> queryString) => this;
        /// <inheritdoc />
        public IHttpRequest<string> QueryString(object queryString) => this;
        /// <inheritdoc />
        public IHttpRequest<string> UseCookies(bool isUseCookies = true) => this;
        /// <inheritdoc />
        public IHttpRequest<string> Cookie(string key, string value) => this;
        /// <inheritdoc />
        public IHttpRequest<string> Cookie(IDictionary<string, string> cookies) => this;
        /// <inheritdoc />
        public IHttpRequest<string> Content(string key, object value) => this;
        /// <inheritdoc />
        public IHttpRequest<string> Content(IDictionary<string, object> parameters) => this;
        /// <inheritdoc />
        public IHttpRequest<string> Content(object value) => this;
        /// <inheritdoc />
        public IHttpRequest<string> JsonContent(object value) => this;
        /// <inheritdoc />
        public IHttpRequest<string> XmlContent(string value) => this;
        /// <inheritdoc />
        public IHttpRequest<string> FileContent(Stream stream, string fileName, string name = "file") => this;
        /// <inheritdoc />
        public IHttpRequest<string> FileContent(Stream stream, string fileName, string name, bool leaveOpen) => this;
        /// <inheritdoc />
        public IHttpRequest<string> FileContent(string filePath, string name = "file") => this;
        /// <inheritdoc />
        public IHttpRequest<string> FileParameterQuotes(bool isQuote = true) => this;
        /// <inheritdoc />
        public IHttpRequest<string> Retry(HttpRetryOptions options) => this;
        /// <inheritdoc />
        public IHttpRequest<string> MaxResponseBytes(long maxBytes) => this;
        /// <inheritdoc />
        public IHttpRequest<string> OnSendBefore(Func<HttpRequestMessage, bool> action) => this;
        /// <inheritdoc />
        public IHttpRequest<string> OnSendAfter(Func<HttpResponseMessage, Task<string>> action) => this;
        /// <inheritdoc />
        public IHttpRequest<string> OnSendAfter(Func<HttpResponseMessage, CancellationToken, Task<string>> action) => this;
        /// <inheritdoc />
        public IHttpRequest<string> OnConvert(Func<string, string> action) => this;
        /// <inheritdoc />
        public IHttpRequest<string> OnSuccess(Action<string> action) => this;
        /// <inheritdoc />
        public IHttpRequest<string> OnSuccess(Func<string, Task> action) => this;
        /// <inheritdoc />
        public IHttpRequest<string> OnFail(Action<HttpResponseMessage, object> action) => this;
        /// <inheritdoc />
        public IHttpRequest<string> OnComplete(Action<HttpResponseMessage, object> action) => this;
        /// <inheritdoc />
        public Task<string> GetResultAsync(CancellationToken cancellationToken = default) => Task.FromResult("legacy");
        /// <inheritdoc />
        public Task<byte[]> GetStreamAsync(CancellationToken cancellationToken = default) => Task.FromResult(Array.Empty<byte>());
        /// <inheritdoc />
        public Task DownloadToAsync(Stream destination, CancellationToken cancellationToken = default) => Task.CompletedTask;
        /// <inheritdoc />
        public Task DownloadFileAsync(string filePath, CancellationToken cancellationToken = default) => Task.CompletedTask;
        /// <inheritdoc />
        public Task WriteAsync(string filePath, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    /// <summary>

    /// 记录正文序列化次数和释放状态的 HTTP 内容。

    /// </summary>
    private sealed class CountingContent : HttpContent
    {
        /// <summary>
        /// 测试请求或响应使用的字节数据。
        /// </summary>
        private readonly byte[] _bytes;
        /// <summary>
        /// 初始化一个<see cref="CountingContent"/>类型的实例。
        /// </summary>
        /// <param name="value">要作为正文返回的文本。</param>
        public CountingContent(string value) => _bytes = Encoding.UTF8.GetBytes(value);
        /// <summary>
        /// 记录正文序列化次数。
        /// </summary>
        public int SerializeCount { get; private set; }
        /// <summary>
        /// 指示测试对象是否已释放。
        /// </summary>
        public bool IsDisposed { get; private set; }
        /// <inheritdoc />
        protected override async Task SerializeToStreamAsync(System.IO.Stream stream, System.Net.TransportContext context)
        {
            SerializeCount++;
            await stream.WriteAsync(_bytes, 0, _bytes.Length);
        }
        /// <inheritdoc />
        protected override bool TryComputeLength(out long length)
        {
            length = _bytes.Length;
            return true;
        }
        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing) IsDisposed = true;
            base.Dispose(disposing);
        }
    }

    /// <summary>

    /// 创建读取时抛出指定异常的 HTTP 内容。

    /// </summary>
    private sealed class FailingReadContent : HttpContent
    {
        /// <summary>
        /// 测试异常传播行为的异常实例。
        /// </summary>
        private readonly Exception _error;
        /// <summary>
        /// 指示测试对象是否已释放。
        /// </summary>
        public bool IsDisposed { get; private set; }
        /// <summary>
        /// 初始化一个<see cref="FailingReadContent"/>类型的实例。
        /// </summary>
        /// <param name="error">测试期间抛出的异常。</param>
        public FailingReadContent(Exception error) => _error = error;
        /// <inheritdoc />
        protected override Task SerializeToStreamAsync(System.IO.Stream stream, System.Net.TransportContext context)
            => Task.CompletedTask;
        /// <inheritdoc />
        protected override Task<Stream> CreateContentReadStreamAsync()
            => Task.FromResult<Stream>(new ThrowingReadStream(_error));
        /// <inheritdoc />
        protected override bool TryComputeLength(out long length)
        {
            length = 1;
            return true;
        }
        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing) IsDisposed = true;
            base.Dispose(disposing);
        }
    }

    /// <summary>

    /// 每次读取都抛出指定异常的流。

    /// </summary>
    private sealed class ThrowingReadStream : Stream
    {
        /// <summary>
        /// 测试异常传播行为的异常实例。
        /// </summary>
        private readonly Exception _error;
        /// <summary>
        /// 初始化一个<see cref="ThrowingReadStream"/>类型的实例。
        /// </summary>
        /// <param name="error">测试期间抛出的异常。</param>
        public ThrowingReadStream(Exception error) => _error = error;
        /// <inheritdoc />
        public override bool CanRead => true;
        /// <inheritdoc />
        public override bool CanSeek => false;
        /// <inheritdoc />
        public override bool CanWrite => false;
        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count) => throw _error;
        /// <inheritdoc />
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => Task.FromException<int>(_error);
        /// <inheritdoc />
        public override void Flush() => throw new NotSupportedException();
        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        /// <inheritdoc />
        public override void SetLength(long value) => throw new NotSupportedException();
        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        /// <inheritdoc />
        public override long Length => throw new NotSupportedException();
        /// <inheritdoc />
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    }

    /// <summary>

    /// 发送字节正文但不报告其长度的 HTTP 内容。

    /// </summary>
    private sealed class UnknownLengthContent : HttpContent
    {
        /// <summary>
        /// 测试请求或响应使用的字节数据。
        /// </summary>
        private readonly byte[] _bytes;
        /// <summary>
        /// 初始化一个<see cref="UnknownLengthContent"/>类型的实例。
        /// </summary>
        /// <param name="value">要作为正文返回的文本。</param>
        public UnknownLengthContent(string value) => _bytes = Encoding.UTF8.GetBytes(value);
        /// <inheritdoc />
        protected override Task SerializeToStreamAsync(System.IO.Stream stream, System.Net.TransportContext context)
            => stream.WriteAsync(_bytes, 0, _bytes.Length);
        /// <inheritdoc />
        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }

    /// <summary>

    /// 通知读取开始并支持取消测试的流。

    /// </summary>
    private sealed class CancellationStream : Stream
    {
        /// <summary>
        /// 通知测试读取已开始的信号。
        /// </summary>
        private readonly TaskCompletionSource<bool> _readStarted;
        /// <summary>
        /// 初始化一个<see cref="CancellationStream"/>类型的实例。
        /// </summary>
        /// <param name="readStarted">通知测试读取已开始的信号。</param>
        public CancellationStream(TaskCompletionSource<bool> readStarted) => _readStarted = readStarted;
        /// <summary>
        /// 指示测试对象是否已释放。
        /// </summary>
        public bool IsDisposed { get; private set; }
        /// <inheritdoc />
        public override bool CanRead => !IsDisposed;
        /// <inheritdoc />
        public override bool CanSeek => false;
        /// <inheritdoc />
        public override bool CanWrite => false;
        /// <inheritdoc />
        public override long Length => throw new NotSupportedException();
        /// <inheritdoc />
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        /// <inheritdoc />
        public override void Flush() => throw new NotSupportedException();
        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        /// <inheritdoc />
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            _readStarted.TrySetResult(true);
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return 0;
        }
        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        /// <inheritdoc />
        public override void SetLength(long value) => throw new NotSupportedException();
        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing) IsDisposed = true;
            base.Dispose(disposing);
        }
    }

    /// <summary>

    /// 在写入时抛出指定异常的内存流。

    /// </summary>
    private sealed class ThrowingDestinationStream : MemoryStream
    {
        /// <inheritdoc />
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => Task.FromException(new IOException("destination write failed"));
    }

    /// <summary>

    /// 不修改请求且不执行额外操作的拦截器。

    /// </summary>
    private sealed class NoopInterceptor : HttpRequestInterceptor { }

    /// <summary>

    /// 记录请求尝试次数并添加尝试签名头的拦截器。

    /// </summary>
    private sealed class AttemptInterceptor : HttpRequestInterceptor
    {
        /// <summary>
        /// 测试期间记录的调用状态。
        /// </summary>
        private readonly IList<int> _attempts;
        /// <summary>
        /// 初始化一个<see cref="AttemptInterceptor"/>类型的实例。
        /// </summary>
        /// <param name="attempts">记录的请求尝试次数。</param>
        public AttemptInterceptor(IList<int> attempts) => _attempts = attempts;
        /// <inheritdoc />
        public override Task BeforeSendAsync(HttpRequestInterceptorContext context, CancellationToken token)
        {
            _attempts.Add(context.Attempt);
            context.Request.Headers.TryAddWithoutValidation("X-Attempt-Signature", context.Attempt.ToString());
            return Task.CompletedTask;
        }
    }

    /// <summary>

    /// 通过请求上下文记录拦截器执行次数。

    /// </summary>
    private sealed class ItemsInterceptor : HttpRequestInterceptor
    {
        /// <summary>
        /// 接收并记录每次请求拦截上下文的列表。
        /// </summary>
        private readonly IList<HttpRequestInterceptorContext> _contexts;
        /// <summary>
        /// 初始化一个<see cref="ItemsInterceptor"/>类型的实例。
        /// </summary>
        /// <param name="contexts">记录请求拦截上下文的列表。</param>
        public ItemsInterceptor(IList<HttpRequestInterceptorContext> contexts) => _contexts = contexts;
        /// <inheritdoc />
        public override Task BeforeSendAsync(HttpRequestInterceptorContext context, CancellationToken token)
        {
            context.Items.TryGetValue("signatures", out var previous);
            var signature = (int?)previous + 1 ?? 1;
            context.Items["signatures"] = signature;
            _contexts.Add(context);
            return Task.CompletedTask;
        }
    }

    /// <summary>

    /// 按执行阶段记录拦截器调用顺序。

    /// </summary>
    private sealed class EventInterceptor : HttpRequestInterceptor
    {
        /// <summary>
        /// 拦截器名称。
        /// </summary>
        private readonly string _name;
        /// <summary>
        /// 测试期间记录的调用状态。
        /// </summary>
        private readonly IList<string> _events;
        /// <summary>
        /// 初始化一个<see cref="EventInterceptor"/>类型的实例。
        /// </summary>
        /// <param name="name">拦截器名称。</param>
        /// <param name="events">记录拦截器调用顺序的列表。</param>
        public EventInterceptor(string name, IList<string> events) { _name = name; _events = events; }
        /// <inheritdoc />
        public override Task BeforeSendAsync(HttpRequestInterceptorContext context, CancellationToken token)
        {
            _events.Add(_name + "-before");
            return Task.CompletedTask;
        }
        /// <inheritdoc />
        public override Task AfterSendAsync(HttpRequestInterceptorContext context, CancellationToken token)
        {
            _events.Add(_name + "-after");
            return Task.CompletedTask;
        }
    }

    /// <summary>

    /// 记录发送前调用后抛出异常的拦截器。

    /// </summary>
    private sealed class ThrowingBeforeInterceptor : HttpRequestInterceptor
    {
        /// <summary>
        /// 测试期间记录的调用状态。
        /// </summary>
        private readonly IList<string> _events;
        /// <summary>
        /// 初始化一个<see cref="ThrowingBeforeInterceptor"/>类型的实例。
        /// </summary>
        /// <param name="events">记录拦截器调用顺序的列表。</param>
        public ThrowingBeforeInterceptor(IList<string> events) => _events = events;
        /// <inheritdoc />
        public override Task BeforeSendAsync(HttpRequestInterceptorContext context, CancellationToken token)
        {
            _events.Add("throwing-before");
            throw new InvalidOperationException("before failed");
        }
    }

    /// <summary>

    /// 发送后记录调用并抛出异常的拦截器。

    /// </summary>
    private sealed class ThrowingAfterInterceptor : HttpRequestInterceptor
    {
        /// <summary>
        /// 测试期间记录的调用状态。
        /// </summary>
        private readonly IList<string> _events;
        /// <summary>
        /// 初始化一个<see cref="ThrowingAfterInterceptor"/>类型的实例。
        /// </summary>
        /// <param name="events">记录拦截器调用顺序的列表。</param>
        public ThrowingAfterInterceptor(IList<string> events) => _events = events;
        /// <inheritdoc />
        public override Task BeforeSendAsync(HttpRequestInterceptorContext context, CancellationToken token) => Task.CompletedTask;
        /// <inheritdoc />
        public override Task AfterSendAsync(HttpRequestInterceptorContext context, CancellationToken token)
        {
            _events.Add("throwing-after");
            throw new InvalidOperationException("after failed");
        }
    }

    /// <summary>

    /// 在请求发送前后调用指定回调的拦截器。

    /// </summary>
    private sealed class CallbackInterceptor : HttpRequestInterceptor
    {
        /// <summary>
        /// 发送请求前执行的回调。
        /// </summary>
        private readonly Action<HttpRequestInterceptorContext> _before;
        /// <summary>
        /// 发送请求后执行的回调。
        /// </summary>
        private readonly Action<HttpRequestInterceptorContext> _after;
        /// <summary>
        /// 初始化一个<see cref="CallbackInterceptor"/>类型的实例。
        /// </summary>
        /// <param name="before">发送请求前执行的回调。</param>
        /// <param name="after">发送请求后执行的回调。</param>
        public CallbackInterceptor(Action<HttpRequestInterceptorContext> before, Action<HttpRequestInterceptorContext> after = null)
        {
            _before = before;
            _after = after;
        }
        /// <inheritdoc />
        public override Task BeforeSendAsync(HttpRequestInterceptorContext context, CancellationToken token)
        {
            _before(context);
            return Task.CompletedTask;
        }
        /// <inheritdoc />
        public override Task AfterSendAsync(HttpRequestInterceptorContext context, CancellationToken token)
        {
            _after?.Invoke(context);
            return Task.CompletedTask;
        }
    }

    /// <summary>

    /// 通过进入与释放信号协调拦截器注册时序。

    /// </summary>
    private sealed class RegistrationBarrierInterceptor : HttpRequestInterceptor
    {
        /// <summary>
        /// 测试期间记录的调用状态。
        /// </summary>
        private readonly IList<string> _events;
        /// <summary>
        /// 通知测试已进入屏障的信号。
        /// </summary>
        private readonly TaskCompletionSource<bool> _entered;
        /// <summary>
        /// 控制屏障继续执行的信号。
        /// </summary>
        private readonly TaskCompletionSource<bool> _release;
        /// <summary>
        /// 测试期间记录的调用状态。
        /// </summary>
        private int _waitOnce;
        /// <summary>
        /// 初始化一个<see cref="RegistrationBarrierInterceptor"/>类型的实例。
        /// </summary>
        /// <param name="events">记录拦截器调用顺序的列表。</param>
        /// <param name="entered">通知测试已进入屏障的信号。</param>
        /// <param name="release">控制屏障继续执行的信号。</param>
        public RegistrationBarrierInterceptor(IList<string> events, TaskCompletionSource<bool> entered,
            TaskCompletionSource<bool> release)
        {
            _events = events;
            _entered = entered;
            _release = release;
        }
        /// <inheritdoc />
        public override async Task BeforeSendAsync(HttpRequestInterceptorContext context, CancellationToken token)
        {
            _events.Add("initial-before");
            if (Interlocked.Exchange(ref _waitOnce, 1) == 0)
            {
                _entered.TrySetResult(true);
                await _release.Task;
            }
        }
        /// <inheritdoc />
        public override Task AfterSendAsync(HttpRequestInterceptorContext context, CancellationToken token)
        {
            _events.Add("initial-after");
            return Task.CompletedTask;
        }
    }

    /// <summary>

    /// 记录发送前后事件并维护测试签名头的拦截器。

    /// </summary>
    private sealed class RecordingInterceptor : HttpRequestInterceptor
    {
        /// <summary>
        /// 拦截器名称。
        /// </summary>
        private readonly string _name;
        /// <summary>
        /// 测试期间记录的调用状态。
        /// </summary>
        private readonly IList<string> _calls;
        /// <summary>
        /// 初始化一个<see cref="RecordingInterceptor"/>类型的实例。
        /// </summary>
        /// <param name="name">拦截器名称。</param>
        /// <param name="calls">记录拦截器调用顺序的列表。</param>
        public RecordingInterceptor(string name, IList<string> calls) { _name = name; _calls = calls; }
        /// <inheritdoc />
        public override Task BeforeSendAsync(HttpRequestInterceptorContext context, CancellationToken token)
        {
            _calls.Add(_name + "-before");
            context.Request.Headers.Remove("X-Signed");
            context.Request.Headers.TryAddWithoutValidation("X-Signed", "signed");
            return Task.CompletedTask;
        }
        /// <inheritdoc />
        public override Task AfterSendAsync(HttpRequestInterceptorContext context, CancellationToken token)
        {
            context.StatusCode.ShouldBe(HttpStatusCode.Created);
            context.Elapsed.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
            _calls.Add(_name + "-after");
            return Task.CompletedTask;
        }
    }

    /// <summary>

    /// 仅在发送后执行指定回调的拦截器。

    /// </summary>
    private sealed class HttpRequestInterceptorWithAfter : HttpRequestInterceptor
    {
        /// <summary>
        /// 发送请求后执行的回调。
        /// </summary>
        private readonly Action<HttpRequestInterceptorContext> _after;
        /// <summary>
        /// 初始化一个<see cref="HttpRequestInterceptorWithAfter"/>类型的实例。
        /// </summary>
        /// <param name="after">发送请求后执行的回调。</param>
        public HttpRequestInterceptorWithAfter(Action<HttpRequestInterceptorContext> after) => _after = after;
        /// <inheritdoc />
        public override Task AfterSendAsync(HttpRequestInterceptorContext context, CancellationToken token)
        {
            _after(context);
            return Task.CompletedTask;
        }
    }
}
