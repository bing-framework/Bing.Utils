using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading;
using Bing.Http;
using Bing.Http.Clients;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace Bing.Utils.Http.Tests.Bing.Http;

/// <summary>
/// HttpRequest 生命周期、请求配置和隔离注册回归测试。
/// </summary>
[Trait("Bing.Http", "HttpRequest.Reliability")]
public class HttpRequestReliabilityTest
{
    /// <summary>
    /// 可靠性测试注册和查找隔离客户端时使用的名称。
    /// </summary>
    private const string IsolationClientName = "http-reliability-isolated";

    /// <summary>
    /// 异步验证成功请求会释放请求和响应内容。
    /// </summary>
    [Fact]
    public async Task GetResultAsync_Success_DisposesRequestAndResponseContent()
    {
        var requestContent = new TrackingContent("request");
        var responseContent = new TrackingContent("ok");
        var handler = new FakeHandler((_, _) => Task.FromResult(CreateResponse(HttpStatusCode.OK, responseContent)));
        using var client = CreateClient(handler);
        var request = new TrackingRequest(client, requestContent);

        var result = await request.GetResultAsync();

        result.ShouldBe("ok");
        requestContent.IsDisposed.ShouldBeTrue();
        responseContent.IsDisposed.ShouldBeTrue();
        handler.SendCount.ShouldBe(1);
    }

    /// <summary>
    /// 异步验证失败响应触发回调并释放响应内容。
    /// </summary>
    [Fact]
    public async Task GetResultAsync_NonSuccess_InvokesFailAndDisposesResponseContent()
    {
        var responseContent = new TrackingContent("bad");
        var handler = new FakeHandler((_, _) => Task.FromResult(CreateResponse(HttpStatusCode.BadRequest, responseContent)));
        using var client = CreateClient(handler);
        HttpStatusCode? statusCode = null;
        object failContent = null;
        var completeCalled = false;

        var result = await new HttpClientService().SetHttpClient(client).Get("/failure")
            .OnFail((response, content) =>
            {
                statusCode = response.StatusCode;
                failContent = content;
            })
            .OnComplete((_, _) => completeCalled = true)
            .GetResultAsync();

        result.ShouldBeNull();
        statusCode.ShouldBe(HttpStatusCode.BadRequest);
        failContent.ShouldBe("bad");
        completeCalled.ShouldBeTrue();
        responseContent.IsDisposed.ShouldBeTrue();
    }

    /// <summary>
    /// 异步验证发送取消时释放请求内容。
    /// </summary>
    [Fact]
    public async Task GetResultAsync_WhenSendThrowsCancellation_DisposesRequestContent()
    {
        var requestContent = new TrackingContent("request");
        var handler = new FakeHandler((_, token) =>
            Task.FromException<HttpResponseMessage>(new OperationCanceledException(token)));
        using var client = CreateClient(handler);
        var request = new TrackingRequest(client, requestContent);

        await Should.ThrowAsync<OperationCanceledException>(() => request.GetResultAsync());

        requestContent.IsDisposed.ShouldBeTrue();
        handler.SendCount.ShouldBe(1);
    }

    /// <summary>
    /// 异步验证响应回调异常时释放响应内容。
    /// </summary>
    [Fact]
    public async Task GetResultAsync_WhenSendAfterThrows_DisposesResponseContent()
    {
        var responseContent = new TrackingContent("ok");
        var handler = new FakeHandler((_, _) => Task.FromResult(CreateResponse(HttpStatusCode.OK, responseContent)));
        using var client = CreateClient(handler);

        var request = new HttpClientService().SetHttpClient(client).Get("/callback")
            .OnSendAfter(_ => Task.FromException<string>(new InvalidOperationException("callback failure")));

        await Should.ThrowAsync<InvalidOperationException>(() => request.GetResultAsync());

        responseContent.IsDisposed.ShouldBeTrue();
    }

    /// <summary>
    /// 异步验证发送前取消会释放内容且不发送请求。
    /// </summary>
    [Fact]
    public async Task GetResultAsync_WhenSendBeforeRejects_DisposesMessageContentAndDoesNotSend()
    {
        var requestContent = new TrackingContent("request");
        var handler = new FakeHandler((_, _) => Task.FromResult(CreateResponse(HttpStatusCode.OK, "unexpected")));
        using var client = CreateClient(handler);
        var request = new TrackingRequest(client, requestContent);
        request.OnSendBefore(_ => false);

        var result = await request.GetResultAsync();

        result.ShouldBeNull();
        requestContent.IsDisposed.ShouldBeTrue();
        handler.SendCount.ShouldBe(0);
    }

    /// <summary>
    /// 异步验证请求完成后仍可复用注入的客户端。
    /// </summary>
    [Fact]
    public async Task InjectedClient_RemainsUsableAfterRequest()
    {
        var handler = new FakeHandler((request, _) =>
            Task.FromResult(CreateResponse(HttpStatusCode.OK, request.RequestUri.AbsolutePath)));
        using var client = CreateClient(handler);
        var service = new HttpClientService().SetHttpClient(client);

        (await service.Get("/first").GetResultAsync()).ShouldBe("/first");
        (await service.Get("/second").GetResultAsync()).ShouldBe("/second");

        handler.SendCount.ShouldBe(2);
        handler.IsDisposed.ShouldBeFalse();
    }

    /// <summary>
    /// 异步验证同名请求头忽略大小写并采用最后设置的值。
    /// </summary>
    [Fact]
    public async Task Header_IsCaseInsensitiveAndLastValueWins()
    {
        string value = null;
        var handler = new FakeHandler((request, _) =>
        {
            value = request.Headers.GetValues("X-Request-Id").Single();
            return Task.FromResult(CreateResponse(HttpStatusCode.OK, "ok"));
        });
        using var client = CreateClient(handler);

        var result = await new HttpClientService().SetHttpClient(client).Get("/headers")
            .Header("X-Request-Id", "first")
            .Header("x-request-id", "last")
            .GetResultAsync();

        result.ShouldBe("ok");
        value.ShouldBe("last");
    }

    /// <summary>
    /// 异步验证显式 Cookie 使用分号分隔。
    /// </summary>
    [Fact]
    public async Task Cookie_UsesSemicolonSeparator()
    {
        string cookie = null;
        var handler = new FakeHandler((request, _) =>
        {
            cookie = request.Headers.GetValues("Cookie").Single();
            return Task.FromResult(CreateResponse(HttpStatusCode.OK, "ok"));
        });
        using var client = CreateClient(handler);

        var result = await new HttpClientService().SetHttpClient(client).Get("/cookies")
            .Cookie("first", "one")
            .Cookie("second", "two")
            .GetResultAsync();

        result.ShouldBe("ok");
        cookie.ShouldBe("first=one; second=two");
    }

    /// <summary>
    /// 异步验证基地址和超时配置不修改共享客户端。
    /// </summary>
    [Fact]
    public async Task BaseAddressAndTimeout_ArePerRequestAndDoNotMutateClient()
    {
        var originalBaseAddress = new Uri("http://client.example/root/");
        var originalTimeout = TimeSpan.FromSeconds(42);
        Uri requestUri = null;
        var handler = new FakeHandler((request, _) =>
        {
            requestUri = request.RequestUri;
            return Task.FromResult(CreateResponse(HttpStatusCode.OK, "ok"));
        });
        using var client = new HttpClient(handler)
        {
            BaseAddress = originalBaseAddress,
            Timeout = originalTimeout
        };

        var result = await new HttpClientService().SetHttpClient(client).Get("resource")
            .BaseAddress("http://request.example/base/")
            .Timeout(TimeSpan.FromSeconds(1))
            .GetResultAsync();

        result.ShouldBe("ok");
        requestUri.ShouldBe(new Uri("http://request.example/base/resource"));
        client.BaseAddress.ShouldBe(originalBaseAddress);
        client.Timeout.ShouldBe(originalTimeout);
    }

    /// <summary>
    /// 异步验证超时配置支持无限等待并拒绝无效值。
    /// </summary>
    [Fact]
    public async Task Timeout_InfiniteIsAcceptedAndInvalidValuesAreRejected()
    {
        var handler = new FakeHandler((_, _) => Task.FromResult(CreateResponse(HttpStatusCode.OK, "ok")));
        using var client = CreateClient(handler);
        var service = new HttpClientService().SetHttpClient(client);

        var request = service.Get("/timeout");
        Should.Throw<ArgumentOutOfRangeException>(() => request.Timeout(TimeSpan.Zero));
        Should.Throw<ArgumentOutOfRangeException>(() => request.Timeout(TimeSpan.FromMilliseconds(-2)));
        Should.Throw<ArgumentOutOfRangeException>(() => request.Timeout(TimeSpan.FromMilliseconds((double)int.MaxValue + 1)));

        request.Timeout(Timeout.InfiniteTimeSpan);
        (await request.GetResultAsync()).ShouldBe("ok");
        client.Timeout.ShouldBe(TimeSpan.FromSeconds(100));
    }

    /// <summary>
    /// 异步验证未注册隔离能力时拒绝请求级传输配置。
    /// </summary>
    [Fact]
    public async Task ExplicitTransportOverrides_WithoutRegistrationFailBeforeSend()
    {
        var handler = new FakeHandler((_, _) => Task.FromResult(CreateResponse(HttpStatusCode.OK, "unexpected")));
        using var client = CreateClient(handler);
        var service = new HttpClientService().SetHttpClient(client);

        var requests = new[]
        {
            service.Get("/ignore").IgnoreSsl(),
            service.Get("/cookies").UseCookies(false),
            service.Get("/certificate").Certificate("missing.pfx", null)
        };

        foreach (var request in requests)
        {
            var exception = await Should.ThrowAsync<NotSupportedException>(() => request.GetResultAsync());
            exception.Message.ShouldContain("UseBingRequestIsolation");
        }

        handler.SendCount.ShouldBe(0);
    }

    /// <summary>
    /// 异步验证请求构建失败时释放多部分内容。
    /// </summary>
    [Fact]
    public async Task MultipartContent_IsDisposedWhenRequestCreationFails()
    {
        var handler = new FakeHandler((_, _) => Task.FromResult(CreateResponse(HttpStatusCode.OK, "unexpected")));
        using var client = CreateClient(handler);
        var request = new FailingMultipartRequest(client);

        await Should.ThrowAsync<InvalidOperationException>(() => request.GetResultAsync());

        request.Content.IsDisposed.ShouldBeTrue();
        handler.SendCount.ShouldBe(0);
    }

    /// <summary>
    /// 验证无效上传路径会抛出异常且不发送请求。
    /// </summary>
    /// <param name="filePath">用于验证的无效文件路径。</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void FileContent_InvalidPath_ThrowsAndDoesNotSend(string filePath)
    {
        var handler = new FakeHandler((_, _) => Task.FromResult(CreateResponse(HttpStatusCode.OK, "unexpected")));
        using var client = CreateClient(handler);
        var service = new HttpClientService().SetHttpClient(client);

        Should.Throw<ArgumentException>(() => service.Post("/upload").FileContent(filePath, "file"));

        handler.SendCount.ShouldBe(0);
    }

    /// <summary>
    /// 验证上传流为 null 时拒绝发送。
    /// </summary>
    [Fact]
    public void FileContent_NullStream_ThrowsAndDoesNotSend()
    {
        var handler = new FakeHandler((_, _) => Task.FromResult(CreateResponse(HttpStatusCode.OK, "unexpected")));
        using var client = CreateClient(handler);
        var service = new HttpClientService().SetHttpClient(client);

        Should.Throw<ArgumentNullException>(() =>
            service.Post("/upload").FileContent((Stream)null, "empty.bin", "file"));

        handler.SendCount.ShouldBe(0);
    }

    /// <summary>
    /// 异步验证发送前已关闭的上传流会导致请求失败。
    /// </summary>
    [Fact]
    public async Task FileContent_ClosedStreamBeforeSend_ThrowsAndDoesNotSend()
    {
        var handler = new FakeHandler((_, _) => Task.FromResult(CreateResponse(HttpStatusCode.OK, "unexpected")));
        using var client = CreateClient(handler);
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("content"));
        var request = new HttpClientService().SetHttpClient(client)
            .Post("/upload")
            .FileContent(stream, "content.txt", "file");
        stream.Dispose();

        await Should.ThrowAsync<InvalidOperationException>(() => request.GetResultAsync());

        handler.SendCount.ShouldBe(0);
    }

    /// <summary>
    /// 异步验证不可读上传流被释放且请求不发送。
    /// </summary>
    [Fact]
    public async Task FileContent_UnreadableStream_IsDisposedAndDoesNotSend()
    {
        var handler = new FakeHandler((_, _) => Task.FromResult(CreateResponse(HttpStatusCode.OK, "unexpected")));
        using var client = CreateClient(handler);
        var validStream = new MemoryStream(Encoding.UTF8.GetBytes("valid"));
        var stream = new UnreadableStream();
        var request = new HttpClientService().SetHttpClient(client)
            .Post("/upload")
            .FileContent(validStream, "valid.txt", "valid")
            .FileContent(stream, "unreadable.bin", "file");

        await Should.ThrowAsync<InvalidOperationException>(() => request.GetResultAsync());

        validStream.CanRead.ShouldBeFalse();
        stream.IsDisposed.ShouldBeTrue();
        handler.SendCount.ShouldBe(0);
    }

    /// <summary>
    /// 异步验证空流作为文件项上传并被释放。
    /// </summary>
    [Fact]
    public async Task FileContent_EmptyStream_IsSentAsMultipartPartAndDisposed()
    {
        var hasFilePart = false;
        var handler = new FakeHandler(async (request, _) =>
        {
            var body = await request.Content.ReadAsStringAsync();
            hasFilePart = body.Contains("name=\"file\"", StringComparison.Ordinal);
            return CreateResponse(HttpStatusCode.OK, "ok");
        });
        using var client = CreateClient(handler);
        var stream = new MemoryStream();

        var result = await new HttpClientService().SetHttpClient(client)
            .Post("/upload")
            .FileContent(stream, "empty.bin", "file")
            .GetResultAsync();

        result.ShouldBe("ok");
        hasFilePart.ShouldBeTrue();
        stream.CanRead.ShouldBeFalse();
        handler.SendCount.ShouldBe(1);
    }

    /// <summary>
    /// 验证隔离注册拒绝 null 客户端生成器。
    /// </summary>
    [Fact]
    public void UseBingRequestIsolation_NullBuilderThrows()
    {
        Should.Throw<ArgumentNullException>(() =>
            BingHttpClientBuilderExtensions.UseBingRequestIsolation((IHttpClientBuilder)null));
    }

    /// <summary>
    /// 验证隔离注册拒绝 null 处理器工厂。
    /// </summary>
    [Fact]
    public void UseBingRequestIsolation_NullFactoryThrows()
    {
        var builder = new ServiceCollection().AddHttpClient("isolated");

        Should.Throw<ArgumentNullException>(() =>
            BingHttpClientBuilderExtensions.UseBingRequestIsolation(builder, null));
    }

    /// <summary>
    /// 验证隔离主处理器被覆盖时拒绝客户端配置。
    /// </summary>
    [Fact]
    public void UseBingRequestIsolation_WhenPrimaryHandlerIsOverridden_RejectsConfiguration()
    {
        var services = new ServiceCollection();
        var builder = services.AddHttpClient("isolated")
            .UseBingRequestIsolation()
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler());
        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();

        var exception = Should.Throw<InvalidOperationException>(() => factory.CreateClient(builder.Name));

        exception.Message.ShouldContain("UseBingRequestIsolation");
    }

    /// <summary>
    /// 异步验证成功请求使用并释放独立处理器。
    /// </summary>
    [Fact]
    public async Task UseBingRequestIsolation_Success_UsesAndReleasesIndependentHandler()
    {
        var handlers = new List<TrackingHttpClientHandler>();
        using var provider = CreateIsolationProvider(_ =>
        {
            var handler = new TrackingHttpClientHandler();
            handlers.Add(handler);
            return handler;
        });
        var service = CreateFactoryService(provider);

        var result = await service.Get("/isolated")
            .HttpClientName(IsolationClientName)
            .GetResultAsync();

        result.ShouldBe("isolated");
        handlers.Count.ShouldBe(1);
        handlers[0].SendCount.ShouldBe(1);
        handlers[0].IsDisposed.ShouldBeTrue();
    }

    /// <summary>
    /// 异步验证发送异常时释放独立处理器。
    /// </summary>
    [Fact]
    public async Task UseBingRequestIsolation_SendException_ReleasesIndependentHandler()
    {
        var handlers = new List<TrackingHttpClientHandler>();
        using var provider = CreateIsolationProvider(_ =>
        {
            var handler = new TrackingHttpClientHandler
            {
                ExceptionFactory = _ => new InvalidOperationException("handler failure")
            };
            handlers.Add(handler);
            return handler;
        });
        var service = CreateFactoryService(provider);

        await Should.ThrowAsync<InvalidOperationException>(() => service.Get("/isolated")
            .HttpClientName(IsolationClientName)
            .GetResultAsync());

        handlers.Count.ShouldBe(1);
        handlers[0].SendCount.ShouldBe(1);
        handlers[0].IsDisposed.ShouldBeTrue();
    }

    /// <summary>
    /// 异步验证发送取消时释放独立处理器。
    /// </summary>
    [Fact]
    public async Task UseBingRequestIsolation_SendCancellation_ReleasesIndependentHandler()
    {
        var handlers = new List<TrackingHttpClientHandler>();
        using var provider = CreateIsolationProvider(_ =>
        {
            var handler = new TrackingHttpClientHandler
            {
                ExceptionFactory = token => new OperationCanceledException(token)
            };
            handlers.Add(handler);
            return handler;
        });
        var service = CreateFactoryService(provider);

        await Should.ThrowAsync<OperationCanceledException>(() => service.Get("/isolated")
            .HttpClientName(IsolationClientName)
            .GetResultAsync());

        handlers.Count.ShouldBe(1);
        handlers[0].SendCount.ShouldBe(1);
        handlers[0].IsDisposed.ShouldBeTrue();
    }

    /// <summary>
    /// 异步验证证书加载失败时释放处理器且不发送请求。
    /// </summary>
    [Fact]
    public async Task UseBingRequestIsolation_CertificateLoadFailure_ReleasesHandlerBeforeSend()
    {
        var handlers = new List<TrackingHttpClientHandler>();
        using var provider = CreateIsolationProvider(_ =>
        {
            var handler = new TrackingHttpClientHandler();
            handlers.Add(handler);
            return handler;
        });
        var service = CreateFactoryService(provider);
        var certificatePath = Path.Combine(Path.GetTempPath(), $"bing-http-missing-{Guid.NewGuid():N}.pfx");
        File.Exists(certificatePath).ShouldBeFalse();

        var exception = await Should.ThrowAsync<Exception>(() => service.Get("/certificate")
            .HttpClientName(IsolationClientName)
            .Certificate(certificatePath, "password")
            .GetResultAsync());

        (exception is FileNotFoundException || exception is CryptographicException).ShouldBeTrue();
        handlers.Count.ShouldBe(1);
        handlers[0].SendCount.ShouldBe(0);
        handlers[0].IsDisposed.ShouldBeTrue();
    }

    /// <summary>
    /// 异步验证禁用 Cookie 后复用共享处理器并在清理时释放。
    /// </summary>
    [Fact]
    public async Task UseBingRequestIsolation_DisabledCookies_ReusesSharedHandlerUntilProviderDisposed()
    {
        var handlers = new List<TrackingHttpClientHandler>();
        var provider = CreateIsolationProvider(_ =>
        {
            var handler = new TrackingHttpClientHandler();
            handlers.Add(handler);
            return handler;
        });
        try
        {
            var service = CreateFactoryService(provider);
            var first = await service.Get("/first")
                .HttpClientName(IsolationClientName)
                .UseCookies(false)
                .GetResultAsync();
            var second = await service.Get("/second")
                .HttpClientName(IsolationClientName)
                .UseCookies(false)
                .GetResultAsync();

            first.ShouldBe("isolated");
            second.ShouldBe("isolated");
            handlers.Count.ShouldBe(1);
            handlers[0].SendCount.ShouldBe(2);
            handlers[0].IsDisposed.ShouldBeFalse();
        }
        finally
        {
            DisposeIsolationRouter(provider);
            provider.Dispose();
        }

        handlers[0].IsDisposed.ShouldBeTrue();
    }

    /// <summary>
    /// 异步验证释放路由器后共享处理器被释放且不能继续发送。
    /// </summary>
    [Fact]
    public async Task UseBingRequestIsolation_DisposingRouter_ReleasesSharedHandlerAndRejectsLaterSend()
    {
        var handlers = new List<TrackingHttpClientHandler>();
        var provider = CreateIsolationProvider(_ =>
        {
            var handler = new TrackingHttpClientHandler();
            handlers.Add(handler);
            return handler;
        });
        try
        {
            var service = CreateFactoryService(provider);
            (await service.Get("/first")
                .HttpClientName(IsolationClientName)
                .UseCookies(false)
                .GetResultAsync()).ShouldBe("isolated");

            var pipeline = provider.GetRequiredService<IHttpMessageHandlerFactory>()
                .CreateHandler(IsolationClientName);
            HttpMessageHandler router = pipeline;
            while (router is DelegatingHandler delegating)
                router = delegating.InnerHandler;
            router.ShouldNotBeNull();
            router.Dispose();

            handlers.Count.ShouldBe(1);
            handlers[0].IsDisposed.ShouldBeTrue();
            await Should.ThrowAsync<ObjectDisposedException>(() => service.Get("/after-dispose")
                .HttpClientName(IsolationClientName)
                .GetResultAsync());
        }
        finally
        {
            provider.Dispose();
        }
    }

    /// <summary>
    /// 异步验证工厂客户端被释放而注入客户端仍可复用。
    /// </summary>
    [Fact]
    public async Task FactoryCreatedClient_IsDisposedButInjectedClient_RemainsUsable()
    {
        var factoryHandler = new FakeHandler((_, _) => Task.FromResult(CreateResponse(HttpStatusCode.OK, "factory")));
        using var factoryClient = new HttpClient(factoryHandler, disposeHandler: true)
        {
            BaseAddress = new Uri("http://localhost/")
        };
        var factory = new FixedHttpClientFactory(factoryClient);

        var factoryResult = await new HttpClientService(factory).Get("/factory").GetResultAsync();

        factoryResult.ShouldBe("factory");
        factoryHandler.IsDisposed.ShouldBeTrue();

        var injectedHandler = new FakeHandler((_, _) => Task.FromResult(CreateResponse(HttpStatusCode.OK, "injected")));
        using var injectedClient = new HttpClient(injectedHandler, disposeHandler: true)
        {
            BaseAddress = new Uri("http://localhost/")
        };
        var injectedFactory = new FixedHttpClientFactory(injectedClient);
        var injected = new HttpClientService(injectedFactory).SetHttpClient(injectedFactory.CreateClient("injected"));

        (await injected.Get("/first").GetResultAsync()).ShouldBe("injected");
        injectedHandler.IsDisposed.ShouldBeFalse();
        (await injected.Get("/second").GetResultAsync()).ShouldBe("injected");
        injectedHandler.SendCount.ShouldBe(2);
        injectedHandler.IsDisposed.ShouldBeFalse();
    }

    /// <summary>
    /// 创建注册隔离客户端的测试服务容器。
    /// </summary>
    /// <param name="handlerFactory">每次创建底层处理器的工厂。</param>
    /// <returns>由测试负责释放的服务容器。</returns>
    private static ServiceProvider CreateIsolationProvider(Func<IServiceProvider, HttpClientHandler> handlerFactory)
    {
        var services = new ServiceCollection();
        services.AddHttpClient(IsolationClientName, client => client.BaseAddress = new Uri("http://localhost/"))
            .UseBingRequestIsolation(handlerFactory);
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// 创建使用容器客户端工厂的 HTTP 服务。
    /// </summary>
    /// <param name="provider">提供客户端工厂的测试容器。</param>
    /// <returns>用于发起测试请求的 HTTP 服务。</returns>
    private static HttpClientService CreateFactoryService(ServiceProvider provider) =>
        new(provider.GetRequiredService<IHttpClientFactory>());

    /// <summary>
    /// 释放测试客户端管线末端的隔离路由器。
    /// </summary>
    /// <param name="provider">提供消息处理器工厂的测试容器。</param>
    private static void DisposeIsolationRouter(ServiceProvider provider)
    {
        var pipeline = provider.GetRequiredService<IHttpMessageHandlerFactory>()
            .CreateHandler(IsolationClientName);
        HttpMessageHandler router = pipeline;
        while (router is DelegatingHandler delegating)
            router = delegating.InnerHandler;
        router?.Dispose();
    }

    /// <summary>
    /// 创建使用模拟处理器的测试客户端。
    /// </summary>
    /// <param name="handler">由客户端接管的模拟处理器。</param>
    /// <returns>以 localhost 为基地址的客户端，由测试负责释放。</returns>
    private static HttpClient CreateClient(FakeHandler handler) => new(handler)
    {
        BaseAddress = new Uri("http://localhost/")
    };

    /// <summary>
    /// 创建测试响应消息。
    /// </summary>
    /// <param name="statusCode">响应状态码。</param>
    /// <param name="content">编码为可跟踪释放状态的纯文本内容。</param>
    /// <returns>包含指定内容的响应消息，由接收方负责释放。</returns>
    private static HttpResponseMessage CreateResponse(HttpStatusCode statusCode, string content) =>
        CreateResponse(statusCode, new TrackingContent(content));

    /// <summary>
    /// 创建测试响应消息。
    /// </summary>
    /// <param name="statusCode">响应状态码。</param>
    /// <param name="content">由响应消息接管的内容。</param>
    /// <returns>包含指定内容的响应消息，由接收方负责释放。</returns>
    private static HttpResponseMessage CreateResponse(HttpStatusCode statusCode, HttpContent content) =>
        new(statusCode) { Content = content };

    /// <summary>
    /// 使用可跟踪内容的测试请求。
    /// </summary>
    private sealed class TrackingRequest : HttpRequest<string>
    {
        /// <summary>
        /// 随请求消息释放、供测试观察生命周期的请求内容。
        /// </summary>
        private readonly HttpContent _content;

        /// <summary>
        /// 初始化 <see cref="TrackingRequest" /> 类的新实例。
        /// </summary>
        /// <param name="client">由测试管理的 HTTP 客户端。</param>
        /// <param name="content">供请求消息使用的内容。</param>
        public TrackingRequest(HttpClient client, HttpContent content)
            : base(null, client, HttpMethod.Post, "http://localhost/request")
        {
            _content = content;
        }

        /// <inheritdoc />
        protected override HttpContent CreateHttpContent() => _content;
    }

    /// <summary>
    /// 模拟构建失败的多部分上传请求。
    /// </summary>
    private sealed class FailingMultipartRequest : HttpRequest<string>
    {
        /// <summary>
        /// 初始化 <see cref="FailingMultipartRequest" /> 类的新实例。
        /// </summary>
        /// <param name="client">由测试管理的 HTTP 客户端。</param>
        public FailingMultipartRequest(HttpClient client)
            : base(null, client, HttpMethod.Post, "http://localhost/multipart")
        {
            Content = new TrackingMultipartContent();
        }

        /// <summary>
        /// 获取可跟踪释放状态的多部分内容。
        /// </summary>
        public TrackingMultipartContent Content { get; }

        /// <inheritdoc />
        protected override void AddHeaders(HttpRequestMessage message)
        {
            message.Content = Content;
            throw new InvalidOperationException("request construction failed");
        }
    }

    /// <summary>
    /// 由委托提供响应并跟踪生命周期的模拟处理器。
    /// </summary>
    private sealed class FakeHandler : HttpMessageHandler
    {
        /// <summary>
        /// 为测试请求生成异步响应的委托。
        /// </summary>
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _send;

        /// <summary>
        /// 初始化 <see cref="FakeHandler" /> 类的新实例。
        /// </summary>
        /// <param name="send">生成异步响应的委托，不得为 null。</param>
        public FakeHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
        {
            _send = send ?? throw new ArgumentNullException(nameof(send));
        }

        /// <summary>
        /// 获取处理器的发送次数。
        /// </summary>
        public int SendCount { get; private set; }

        /// <summary>
        /// 获取是否已释放资源。
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            SendCount++;
            return _send(request, cancellationToken);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                IsDisposed = true;
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// 始终返回指定客户端的测试工厂。
    /// </summary>
    private sealed class FixedHttpClientFactory : IHttpClientFactory
    {
        /// <summary>
        /// 工厂为测试请求返回的固定客户端实例。
        /// </summary>
        private readonly HttpClient _client;

        /// <summary>
        /// 初始化 <see cref="FixedHttpClientFactory" /> 类的新实例。
        /// </summary>
        /// <param name="client">工厂固定返回的客户端。</param>
        public FixedHttpClientFactory(HttpClient client)
        {
            _client = client;
        }

        /// <inheritdoc />
        /// <remarks>
        /// 测试工厂忽略名称，始终返回构造时指定的实例。
        /// </remarks>
        public HttpClient CreateClient(string name) => _client;
    }

    /// <summary>
    /// 跟踪发送和释放状态的隔离传输测试处理器。
    /// </summary>
    private sealed class TrackingHttpClientHandler : HttpClientHandler
    {
        /// <summary>
        /// 获取处理器的发送次数。
        /// </summary>
        public int SendCount { get; private set; }

        /// <summary>
        /// 获取是否已释放资源。
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// 获取或设置发送异常工厂。
        /// </summary>
        /// <remarks>
        /// 工厂返回非 null 异常时发送失败；未设置或返回 null 时生成成功响应。
        /// </remarks>
        public Func<CancellationToken, Exception> ExceptionFactory { get; set; }

        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            SendCount++;
            var exception = ExceptionFactory?.Invoke(cancellationToken);
            if (exception != null)
                return Task.FromException<HttpResponseMessage>(exception);
            return Task.FromResult(CreateResponse(HttpStatusCode.OK, "isolated"));
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                IsDisposed = true;
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// 用于验证上传失败清理行为的不可读流。
    /// </summary>
    private sealed class UnreadableStream : Stream
    {
        /// <summary>
        /// 获取是否已释放资源。
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <inheritdoc />
        public override bool CanRead => false;

        /// <inheritdoc />
        public override bool CanSeek => false;

        /// <inheritdoc />
        public override bool CanWrite => false;

        /// <inheritdoc />
        public override long Length => throw new NotSupportedException();

        /// <inheritdoc />
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override void Flush() => throw new NotSupportedException();

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        /// <inheritdoc />
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                IsDisposed = true;
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// 跟踪释放状态的纯文本 HTTP 内容。
    /// </summary>
    private class TrackingContent : HttpContent
    {
        /// <summary>
        /// 序列化到请求或响应流的 UTF-8 文本字节。
        /// </summary>
        private readonly byte[] _bytes;

        /// <summary>
        /// 初始化 <see cref="TrackingContent" /> 类的新实例。
        /// </summary>
        /// <param name="value">纯文本内容；为 null 时使用空字符串。</param>
        public TrackingContent(string value)
        {
            _bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
            Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        }

        /// <summary>
        /// 获取是否已释放资源。
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <inheritdoc />
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context) =>
            stream.WriteAsync(_bytes, 0, _bytes.Length);

        /// <inheritdoc />
        protected override bool TryComputeLength(out long length)
        {
            length = _bytes.Length;
            return true;
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                IsDisposed = true;
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// 跟踪释放状态的多部分表单内容。
    /// </summary>
    private sealed class TrackingMultipartContent : MultipartFormDataContent
    {
        /// <summary>
        /// 获取是否已释放资源。
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                IsDisposed = true;
            base.Dispose(disposing);
        }
    }
}
