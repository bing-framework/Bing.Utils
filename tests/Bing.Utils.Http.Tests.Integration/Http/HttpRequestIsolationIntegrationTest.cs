using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using Bing.Http.Clients;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;

namespace Bing.Utils.Http.Tests.Integration.Http;

/// <summary>
/// 验证请求级传输隔离行为。
/// </summary>
[Trait("Bing.Http", "HttpRequest.IsolationIntegration")]
public sealed class HttpRequestIsolationIntegrationTest
{
    /// <summary>
    /// 请求隔离测试使用的命名客户端名称。
    /// </summary>
    private const string ClientName = "isolated";

    /// <summary>
    /// 验证忽略证书校验只作用于当前请求。
    /// </summary>
    [Fact]
    public async Task IgnoreSsl_IsScopedToOneRequest()
    {
        await using var server = await IsolationTestServer.StartAsync("tls");
        using var provider = CreateProvider(server.HttpAddress, TimeSpan.FromSeconds(30));
        var client = CreateClient(provider);

        var ignoredResult = await client.Get("/identity")
            .HttpClientName(ClientName)
            .BaseAddress(server.HttpsAddress.AbsoluteUri)
            .IgnoreSsl()
            .GetResultAsync();

        ignoredResult.ShouldBe("tls");
        await Should.ThrowAsync<HttpRequestException>(() => client.Get("/identity")
            .HttpClientName(ClientName)
            .BaseAddress(server.HttpsAddress.AbsoluteUri)
            .GetResultAsync());
    }

    /// <summary>
    /// 验证自动 Cookie 在重定向内保持且不会泄漏到下一次执行。
    /// </summary>
    [Fact]
    public async Task Cookies_AreKeptForRedirect_AndAreNotSharedBetweenExecutions()
    {
        await using var server = await IsolationTestServer.StartAsync("cookies");
        using var provider = CreateProvider(server.HttpAddress, TimeSpan.FromSeconds(30));
        var client = CreateClient(provider);

        var redirected = await client.Get("/cookie/redirect")
            .HttpClientName(ClientName)
            .GetResultAsync();
        var nextExecution = await client.Get("/cookie/check")
            .HttpClientName(ClientName)
            .GetResultAsync();
        var disabled = await client.Get("/cookie/redirect")
            .HttpClientName(ClientName)
            .UseCookies(false)
            .GetResultAsync();

        redirected.ShouldBe("present");
        nextExecution.ShouldBe("missing");
        disabled.ShouldBe("missing");
    }

    /// <summary>
    /// 验证并发请求各自保留自动 Cookie 值。
    /// </summary>
    [Fact]
    public async Task CookiesEnabled_ConcurrentRequests_KeepPerRequestValues()
    {
        await using var server = await IsolationTestServer.StartAsync("cookies-concurrent");
        using var provider = CreateProvider(server.HttpAddress, TimeSpan.FromSeconds(30));
        var client = CreateClient(provider);

        var requestA = client.Get("/cookie/isolation/a")
            .HttpClientName(ClientName)
            .GetResultAsync();
        var requestB = client.Get("/cookie/isolation/b")
            .HttpClientName(ClientName)
            .GetResultAsync();

        var results = await Task.WhenAll(requestA, requestB);

        results[0].ShouldBe("A");
        results[1].ShouldBe("B");
    }

    /// <summary>
    /// 验证关闭自动 Cookie 时复用无状态共享处理器。
    /// </summary>
    [Fact]
    public async Task CookiesDisabled_ReuseSharedHandlerWithoutCookieState()
    {
        await using var server = await IsolationTestServer.StartAsync("cookies-shared");
        var factoryCalls = 0;
        using var provider = CreateProviderWithHandlerFactory(server.HttpAddress, TimeSpan.FromSeconds(30),
            _ =>
            {
                Interlocked.Increment(ref factoryCalls);
                return new HttpClientHandler();
            });
        var client = CreateClient(provider);

        var results = await Task.WhenAll(
            client.Get("/cookie/redirect").HttpClientName(ClientName).UseCookies(false).GetResultAsync(),
            client.Get("/cookie/redirect").HttpClientName(ClientName).UseCookies(false).GetResultAsync());

        results.ShouldAllBe(value => value == "missing");
        factoryCalls.ShouldBe(1);
    }

    /// <summary>
    /// 验证处理器工厂复用实例时拒绝第二次创建独立上下文。
    /// </summary>
    [Fact]
    public async Task HandlerFactory_ReusingHandler_IsRejected()
    {
        await using var server = await IsolationTestServer.StartAsync("reused-handler");
        var sharedHandler = new HttpClientHandler();
        using var provider = CreateProviderWithHandlerFactory(server.HttpAddress, TimeSpan.FromSeconds(30), _ => sharedHandler);
        var client = CreateClient(provider);

        var first = await client.Get("/identity").HttpClientName(ClientName).GetResultAsync();
        first.ShouldBe("reused-handler");

        await Should.ThrowAsync<InvalidOperationException>(() => client.Get("/identity")
            .HttpClientName(ClientName)
            .GetResultAsync());
    }

    /// <summary>
    /// 验证处理器工厂返回 null 时发送请求失败。
    /// </summary>
    [Fact]
    public async Task HandlerFactory_ReturningNull_IsRejected()
    {
        await using var server = await IsolationTestServer.StartAsync("null-handler");
        using var provider = CreateProviderWithHandlerFactory(server.HttpAddress, TimeSpan.FromSeconds(30), _ => null);
        var client = CreateClient(provider);

        await Should.ThrowAsync<InvalidOperationException>(() => client.Get("/identity")
            .HttpClientName(ClientName)
            .GetResultAsync());
    }

    /// <summary>
    /// 验证并发请求使用各自的客户端证书。
    /// </summary>
    [Fact]
    public async Task ClientCertificates_AreIsolatedBetweenConcurrentRequests()
    {
        await using var server = await IsolationTestServer.StartAsync("mTLS", requireClientCertificate: true);
        using var clientCertificateA = ClientCertificateFile.Create("client-a");
        using var clientCertificateB = ClientCertificateFile.Create("client-b");
        using var provider = CreateProvider(server.HttpAddress, TimeSpan.FromSeconds(30));
        var client = CreateClient(provider);

        var requestA = client.Get("/certificate")
            .HttpClientName(ClientName)
            .BaseAddress(server.HttpsAddress.AbsoluteUri)
            .IgnoreSsl()
            .Certificate(clientCertificateA.Path, clientCertificateA.Password)
            .GetResultAsync();
        var requestB = client.Get("/certificate")
            .HttpClientName(ClientName)
            .BaseAddress(server.HttpsAddress.AbsoluteUri)
            .IgnoreSsl()
            .Certificate(clientCertificateB.Path, clientCertificateB.Password)
            .GetResultAsync();

        var results = await Task.WhenAll(requestA, requestB);

        results[0].ShouldBe("client-a");
        results[1].ShouldBe("client-b");
    }

    /// <summary>
    /// 验证复制请求元数据后重试可复用隔离上下文。
    /// </summary>
    [Fact]
    public async Task RetryHandler_CopyingRequestMetadata_CanRetryThroughIsolationRouter()
    {
        await using var server = await IsolationTestServer.StartAsync("retry");
        var retryHandler = new RetryCloneHandler(copyMetadata: true);
        using var provider = CreateProvider(server.HttpAddress, TimeSpan.FromSeconds(30), retryHandler);
        var client = CreateClient(provider);
        var key = Guid.NewGuid().ToString("N");

        var result = await client.Get($"/retry/{key}")
            .HttpClientName(ClientName)
            .GetResultAsync();

        result.ShouldBe("ok:2:present");
        retryHandler.SendCount.ShouldBe(2);
        server.GetRetryRequestCount(key).ShouldBe(2);
    }

    /// <summary>
    /// 验证未复制隔离元数据时在第二次发送前拒绝请求。
    /// </summary>
    [Fact]
    public async Task RetryHandler_WithoutRequestMetadata_IsRejectedBeforeSecondSend()
    {
        await using var server = await IsolationTestServer.StartAsync("retry-no-metadata");
        var retryHandler = new RetryCloneHandler(copyMetadata: false);
        using var provider = CreateProvider(server.HttpAddress, TimeSpan.FromSeconds(30), retryHandler);
        var client = CreateClient(provider);
        var key = Guid.NewGuid().ToString("N");

        await Should.ThrowAsync<InvalidOperationException>(() => client.Get($"/retry/{key}")
            .HttpClientName(ClientName)
            .GetResultAsync());

        retryHandler.SendCount.ShouldBe(2);
        server.GetRetryRequestCount(key).ShouldBe(1);
    }

    /// <summary>
    /// 验证请求独立使用地址和超时且不改变命名客户端默认配置。
    /// </summary>
    [Fact]
    public async Task BaseAddressAndTimeout_AreScopedToEachRequest()
    {
        await using var serverA = await IsolationTestServer.StartAsync("server-a");
        await using var serverB = await IsolationTestServer.StartAsync("server-b");
        using var provider = CreateProvider(serverA.HttpAddress, TimeSpan.FromSeconds(30));
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var client = new HttpClientService(factory);

        using (var baseline = factory.CreateClient(ClientName))
        {
            baseline.BaseAddress.AbsoluteUri.ShouldBe(serverA.HttpAddress.AbsoluteUri);
            baseline.Timeout.ShouldBe(TimeSpan.FromSeconds(30));
        }

        var serial = await client.Get("/identity")
            .HttpClientName(ClientName)
            .BaseAddress(serverA.HttpAddress.AbsoluteUri)
            .Timeout(TimeSpan.FromSeconds(2))
            .GetResultAsync();
        var concurrentA = client.Get("/identity")
            .HttpClientName(ClientName)
            .BaseAddress(serverA.HttpAddress.AbsoluteUri)
            .Timeout(TimeSpan.FromSeconds(3))
            .GetResultAsync();
        var concurrentB = client.Get("/identity")
            .HttpClientName(ClientName)
            .BaseAddress(serverB.HttpAddress.AbsoluteUri)
            .Timeout(TimeSpan.FromSeconds(4))
            .GetResultAsync();
        var concurrentResults = await Task.WhenAll(concurrentA, concurrentB);

        serial.ShouldBe("server-a");
        concurrentResults[0].ShouldBe("server-a");
        concurrentResults[1].ShouldBe("server-b");

        using var after = factory.CreateClient(ClientName);
        after.BaseAddress.AbsoluteUri.ShouldBe(serverA.HttpAddress.AbsoluteUri);
        after.Timeout.ShouldBe(TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// 创建包含隔离注册和可选外层处理器的测试服务提供程序。
    /// </summary>
    /// <param name="baseAddress">测试服务基础地址。</param>
    /// <param name="timeout">命名客户端默认超时时间。</param>
    /// <param name="handlers">可选的外层处理器。</param>
    /// <returns>配置完成的服务提供程序。</returns>
    private static ServiceProvider CreateProvider(Uri baseAddress, TimeSpan timeout, params DelegatingHandler[] handlers)
    {
        return CreateProviderWithHandlerFactory(baseAddress, timeout, _ => new HttpClientHandler(), handlers);
    }

    /// <summary>
    /// 创建使用指定底层处理器工厂的测试服务提供程序。
    /// </summary>
    /// <param name="baseAddress">测试服务基础地址。</param>
    /// <param name="timeout">命名客户端默认超时时间。</param>
    /// <param name="handlerFactory">创建底层 HTTP 处理器的工厂。</param>
    /// <param name="handlers">可选的外层处理器。</param>
    /// <returns>配置完成的服务提供程序。</returns>
    private static ServiceProvider CreateProviderWithHandlerFactory(Uri baseAddress, TimeSpan timeout,
        Func<IServiceProvider, HttpClientHandler> handlerFactory, params DelegatingHandler[] handlers)
    {
        var services = new ServiceCollection();
        var builder = services.AddHttpClient(ClientName, client =>
        {
            client.BaseAddress = baseAddress;
            client.Timeout = timeout;
        });

        foreach (var handler in handlers ?? Array.Empty<DelegatingHandler>())
            builder.AddHttpMessageHandler(() => handler);
        builder.UseBingRequestIsolation(handlerFactory);
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// 创建 Bing HTTP 客户端服务。
    /// </summary>
    /// <param name="provider">测试服务提供程序。</param>
    /// <returns>绑定到测试服务提供程序的 HTTP 客户端服务。</returns>
    private static HttpClientService CreateClient(ServiceProvider provider) =>
        new(provider.GetRequiredService<IHttpClientFactory>());

    /// <summary>
    /// 用于验证外层重试处理器是否复制隔离元数据。
    /// </summary>
    private sealed class RetryCloneHandler : DelegatingHandler
    {
        /// <summary>
        /// 指示克隆请求时是否复制隔离元数据。
        /// </summary>
        private readonly bool _copyMetadata;

        /// <summary>
        /// 初始化 <see cref="RetryCloneHandler" /> 类的新实例。
        /// </summary>
        /// <param name="copyMetadata">是否复制请求隔离元数据。</param>
        public RetryCloneHandler(bool copyMetadata)
        {
            _copyMetadata = copyMetadata;
        }

        /// <summary>
        /// 获取已发送的请求数。
        /// </summary>
        public int SendCount { get; private set; }

        /// <inheritdoc />
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            SendCount++;
            if (SendCount != 1)
                return await base.SendAsync(request, cancellationToken);

            var response = await base.SendAsync(request, cancellationToken);
            if (response.StatusCode != HttpStatusCode.InternalServerError)
                return response;
            response.Dispose();

            var clone = Clone(request, _copyMetadata);
            try
            {
                SendCount++;
                return await base.SendAsync(clone, cancellationToken);
            }
            finally
            {
                clone.Dispose();
            }
        }

        /// <summary>
        /// 克隆 HTTP 请求及其可选元数据。
        /// </summary>
        /// <param name="source">源请求。</param>
        /// <param name="copyMetadata">是否复制隔离元数据。</param>
        /// <returns>克隆后的 HTTP 请求。</returns>
        private static HttpRequestMessage Clone(HttpRequestMessage source, bool copyMetadata)
        {
            var clone = new HttpRequestMessage(source.Method, source.RequestUri)
            {
                Version = source.Version
            };
            foreach (var header in source.Headers)
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

            if (!copyMetadata)
                return clone;

            foreach (var property in source.Properties)
                clone.Properties[property.Key] = property.Value;
#if NET5_0_OR_GREATER
            foreach (var option in source.Options)
                clone.Options.Set(new HttpRequestOptionsKey<object>(option.Key), option.Value);
#endif
            return clone;
        }
    }

    /// <summary>
    /// 表示提供 HTTP 和自签名 HTTPS 端口的本地 Kestrel 服务。
    /// </summary>
    private sealed class IsolationTestServer : IAsyncDisposable
    {
        /// <summary>
        /// 托管本地测试服务的主机。
        /// </summary>
        private readonly IHost _host;

        /// <summary>
        /// 本地 HTTPS 服务使用的服务器证书。
        /// </summary>
        private readonly X509Certificate2 _serverCertificate;

        /// <summary>
        /// 保存请求处理状态的服务对象。
        /// </summary>
        private readonly ServerState _state;

        /// <summary>
        /// 初始化 <see cref="IsolationTestServer" /> 类的新实例。
        /// </summary>
        /// <param name="host">已启动的测试主机。</param>
        /// <param name="serverCertificate">服务器证书。</param>
        /// <param name="state">请求处理状态。</param>
        /// <param name="httpAddress">HTTP 监听地址。</param>
        /// <param name="httpsAddress">HTTPS 监听地址。</param>
        private IsolationTestServer(IHost host, X509Certificate2 serverCertificate, ServerState state, Uri httpAddress, Uri httpsAddress)
        {
            _host = host;
            _serverCertificate = serverCertificate;
            _state = state;
            HttpAddress = httpAddress;
            HttpsAddress = httpsAddress;
        }

        /// <summary>
        /// 获取 HTTP 服务地址。
        /// </summary>
        public Uri HttpAddress { get; }

        /// <summary>
        /// 获取 HTTPS 服务地址。
        /// </summary>
        public Uri HttpsAddress { get; }

        /// <summary>
        /// 启动本地测试服务。
        /// </summary>
        /// <param name="responseLabel">默认响应文本。</param>
        /// <param name="requireClientCertificate">是否要求客户端证书。</param>
        /// <returns>异步返回已启动的测试服务。</returns>
        public static async Task<IsolationTestServer> StartAsync(string responseLabel, bool requireClientCertificate = false)
        {
            var serverCertificate = CertificateFactory.CreateServerCertificate();
            var state = new ServerState(responseLabel);

            var host = new HostBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseKestrel(options =>
                    {
                        options.Listen(IPAddress.Loopback, 0);
                        options.Listen(IPAddress.Loopback, 0, listenOptions =>
                        {
                            listenOptions.UseHttps(serverCertificate, httpsOptions =>
                            {
                                if (!requireClientCertificate)
                                    return;
                                httpsOptions.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                                httpsOptions.ClientCertificateValidation = (_, _, _) => true;
                            });
                        });
                    });
                    webBuilder.Configure(app => app.Run(state.HandleAsync));
                })
                .Build();

            try
            {
                await host.StartAsync();
                var addresses = host.Services.GetRequiredService<IServer>()
                    .Features.Get<IServerAddressesFeature>()?.Addresses
                    ?? throw new InvalidOperationException("Kestrel未暴露监听地址。");
                var httpAddress = addresses
                    .Select(address => new Uri(address))
                    .Single(address => address.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase));
                var httpsAddress = addresses
                    .Select(address => new Uri(address))
                    .Single(address => address.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase));
                return new IsolationTestServer(
                    host,
                    serverCertificate,
                    state,
                    httpAddress,
                    httpsAddress);
            }
            catch
            {
                host.Dispose();
                serverCertificate.Dispose();
                throw;
            }
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            try
            {
                await _host.StopAsync();
            }
            finally
            {
                _host.Dispose();
                _serverCertificate.Dispose();
            }
        }

        /// <summary>
        /// 获取指定重试键的请求次数。
        /// </summary>
        /// <param name="key">重试请求键。</param>
        /// <returns>已记录的请求次数。</returns>
        public int GetRetryRequestCount(string key) => _state.GetRetryRequestCount(key);
    }

    /// <summary>
    /// 保存 Kestrel 请求处理状态。
    /// </summary>
    private sealed class ServerState
    {
        /// <summary>
        /// 默认响应文本。
        /// </summary>
        private readonly string _responseLabel;

        /// <summary>
        /// 按键记录重试请求次数。
        /// </summary>
        private readonly ConcurrentDictionary<string, int> _retryCounts = new();

        /// <summary>
        /// 协调并发 Cookie 重定向的屏障。
        /// </summary>
        private readonly CookieIsolationBarrier _cookieIsolationBarrier = new(2, TimeSpan.FromSeconds(5));

        /// <summary>
        /// 初始化 <see cref="ServerState" /> 类的新实例。
        /// </summary>
        /// <param name="responseLabel">默认响应文本。</param>
        public ServerState(string responseLabel)
        {
            _responseLabel = responseLabel;
        }

        /// <summary>
        /// 获取指定重试键的请求次数。
        /// </summary>
        /// <param name="key">重试请求键。</param>
        /// <returns>已记录的请求次数。</returns>
        public int GetRetryRequestCount(string key) =>
            _retryCounts.TryGetValue(key, out var count) ? count : 0;

        /// <summary>
        /// 处理本地测试服务请求。
        /// </summary>
        /// <param name="context">当前 HTTP 请求上下文。</param>
        public async Task HandleAsync(Microsoft.AspNetCore.Http.HttpContext context)
        {
            var path = context.Request.Path.Value ?? "/";
            if (path.Equals("/cookie/redirect", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status302Found;
                context.Response.Headers["Location"] = "/cookie/check";
                context.Response.Headers["Set-Cookie"] = "bing-isolation=present; Path=/";
                return;
            }

            if (path.Equals("/cookie/check", StringComparison.OrdinalIgnoreCase))
            {
                await WriteTextAsync(context, context.Request.Cookies.ContainsKey("bing-isolation") ? "present" : "missing");
                return;
            }

            if (path.StartsWith("/cookie/isolation/", StringComparison.OrdinalIgnoreCase))
            {
                var value = path.Substring("/cookie/isolation/".Length);
                if (value.Equals("check", StringComparison.OrdinalIgnoreCase))
                {
                    context.Request.Cookies.TryGetValue("bing-concurrent", out var cookie);
                    await WriteTextAsync(context, cookie ?? "missing");
                    return;
                }

                var segments = value.Split('/');
                if (segments.Length == 2 && segments[1].Equals("middle", StringComparison.OrdinalIgnoreCase) &&
                    (segments[0].Equals("a", StringComparison.OrdinalIgnoreCase) || segments[0].Equals("b", StringComparison.OrdinalIgnoreCase)))
                {
                    // 此时两个客户端都已经收到并处理了各自首次重定向中的Set-Cookie。
                    await _cookieIsolationBarrier.WaitForPeersAsync(context.RequestAborted);
                    context.Response.StatusCode = StatusCodes.Status302Found;
                    context.Response.Headers["Location"] = "/cookie/isolation/check";
                    return;
                }

                if (segments.Length == 1 &&
                    (value.Equals("a", StringComparison.OrdinalIgnoreCase) || value.Equals("b", StringComparison.OrdinalIgnoreCase)))
                {
                    context.Response.Headers["Set-Cookie"] = $"bing-concurrent={value.ToUpperInvariant()}; Path=/";
                    context.Response.StatusCode = StatusCodes.Status302Found;
                    context.Response.Headers["Location"] = $"/cookie/isolation/{value}/middle";
                    return;
                }
            }

            if (path.Equals("/certificate", StringComparison.OrdinalIgnoreCase))
            {
                var certificate = await context.Connection.GetClientCertificateAsync();
                var name = certificate?.GetNameInfo(X509NameType.SimpleName, false) ?? "none";
                await WriteTextAsync(context, name);
                return;
            }

            if (path.StartsWith("/retry/", StringComparison.OrdinalIgnoreCase))
            {
                var key = path.Substring("/retry/".Length);
                var attempt = _retryCounts.AddOrUpdate(key, 1, (_, value) => value + 1);
                if (attempt == 1)
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.Headers["Set-Cookie"] = "bing-retry=present; Path=/";
                    await WriteTextAsync(context, "transient");
                    return;
                }

                var cookie = context.Request.Cookies.ContainsKey("bing-retry") ? "present" : "missing";
                await WriteTextAsync(context, $"ok:{attempt}:{cookie}");
                return;
            }

            await WriteTextAsync(context, _responseLabel);
        }

        /// <summary>
        /// 向响应写入 UTF-8 文本。
        /// </summary>
        /// <param name="context">当前 HTTP 请求上下文。</param>
        /// <param name="value">待写入文本。</param>
        private static async Task WriteTextAsync(Microsoft.AspNetCore.Http.HttpContext context, string value)
        {
            value ??= string.Empty;
            context.Response.ContentType = "text/plain";
            context.Response.ContentLength = Encoding.UTF8.GetByteCount(value);
            await context.Response.WriteAsync(value, Encoding.UTF8);
        }
    }

    /// <summary>
    /// 协调并发 Cookie 请求的重定向进度。
    /// </summary>
    private sealed class CookieIsolationBarrier
    {
        /// <summary>
        /// 参与同步的请求数。
        /// </summary>
        private readonly int _participantCount;

        /// <summary>
        /// 等待其他请求的最长时间。
        /// </summary>
        private readonly TimeSpan _timeout;

        /// <summary>
        /// 所有参与请求到达后释放的任务源。
        /// </summary>
        private readonly TaskCompletionSource<bool> _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>
        /// 已到达屏障的请求数。
        /// </summary>
        private int _arrivals;

        /// <summary>
        /// 初始化 <see cref="CookieIsolationBarrier" /> 类的新实例。
        /// </summary>
        /// <param name="participantCount">参与同步的请求数。</param>
        /// <param name="timeout">等待其他请求的最长时间。</param>
        public CookieIsolationBarrier(int participantCount, TimeSpan timeout)
        {
            _participantCount = participantCount;
            _timeout = timeout;
        }

        /// <summary>
        /// 等待所有参与请求到达或超时。
        /// </summary>
        /// <param name="requestAborted">请求取消令牌。</param>
        public async Task WaitForPeersAsync(CancellationToken requestAborted)
        {
            if (Interlocked.Increment(ref _arrivals) == _participantCount)
                _release.TrySetResult(true);

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(requestAborted);
            timeout.CancelAfter(_timeout);
            var cancellation = Task.Delay(Timeout.InfiniteTimeSpan, timeout.Token);
            var completed = await Task.WhenAny(_release.Task, cancellation);
            if (!ReferenceEquals(completed, _release.Task))
                throw new TimeoutException("并发Cookie测试屏障等待超时。");
        }
    }

    /// <summary>
    /// 生成测试用服务端和客户端证书。
    /// </summary>
    private static class CertificateFactory
    {
        /// <summary>
        /// 测试证书导出的密码。
        /// </summary>
        private const string CertificatePassword = "bing-http-server";

        /// <summary>
        /// 创建测试用服务器证书。
        /// </summary>
        /// <returns>自签名服务器证书。</returns>
        public static X509Certificate2 CreateServerCertificate()
        {
            using var rsa = RSA.Create(2048);
            var request = CreateRequest(rsa, "CN=localhost", "1.3.6.1.5.5.7.3.1", X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment);
            var san = new SubjectAlternativeNameBuilder();
            san.AddDnsName("localhost");
            san.AddIpAddress(IPAddress.Loopback);
            request.CertificateExtensions.Add(san.Build());
            using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddMinutes(20));
            var pfx = certificate.Export(X509ContentType.Pfx, CertificatePassword);
            return new X509Certificate2(pfx, CertificatePassword, X509KeyStorageFlags.DefaultKeySet);
        }

        /// <summary>
        /// 创建测试用客户端证书。
        /// </summary>
        /// <param name="name">证书主题名称。</param>
        /// <returns>自签名客户端证书。</returns>
        public static X509Certificate2 CreateClientCertificate(string name)
        {
            using var rsa = RSA.Create(2048);
            var request = CreateRequest(rsa, $"CN={name}", "1.3.6.1.5.5.7.3.2", X509KeyUsageFlags.DigitalSignature);
            return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddMinutes(20));
        }

        /// <summary>
        /// 创建包含指定用途扩展的证书请求。
        /// </summary>
        /// <param name="rsa">证书使用的 RSA 密钥。</param>
        /// <param name="subject">证书主题。</param>
        /// <param name="enhancedKeyUsageOid">增强型密钥用途 OID。</param>
        /// <param name="keyUsage">密钥用途标志。</param>
        /// <returns>配置完成的证书请求。</returns>
        private static CertificateRequest CreateRequest(RSA rsa, string subject, string enhancedKeyUsageOid, X509KeyUsageFlags keyUsage)
        {
            var request = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
            request.CertificateExtensions.Add(new X509KeyUsageExtension(keyUsage, false));
            var oids = new OidCollection { new Oid(enhancedKeyUsageOid) };
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(oids, false));
            return request;
        }
    }

    /// <summary>
    /// 表示供客户端证书请求使用的临时 PFX 文件。
    /// </summary>
    private sealed class ClientCertificateFile : IDisposable
    {
        /// <summary>
        /// 客户端证书文件密码。
        /// </summary>
        private const string CertificatePasswordValue = "bing-http-isolation";

        /// <summary>
        /// 初始化 <see cref="ClientCertificateFile" /> 类的新实例。
        /// </summary>
        /// <param name="path">临时 PFX 文件路径。</param>
        private ClientCertificateFile(string path)
        {
            Path = path;
        }

        /// <summary>
        /// 获取临时 PFX 文件路径。
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// 获取客户端证书文件密码。
        /// </summary>
        public string Password => CertificatePasswordValue;

        /// <summary>
        /// 创建包含客户端证书的临时 PFX 文件。
        /// </summary>
        /// <param name="name">证书主题名称。</param>
        /// <returns>临时证书文件。</returns>
        public static ClientCertificateFile Create(string name)
        {
            using var certificate = CertificateFactory.CreateClientCertificate(name);
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"bing-http-{Guid.NewGuid():N}.pfx");
            File.WriteAllBytes(path, certificate.Export(X509ContentType.Pfx, CertificatePasswordValue));
            return new ClientCertificateFile(path);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            try
            {
                if (File.Exists(Path))
                    File.Delete(Path);
            }
            catch (IOException)
            {
                // Windows可能短暂占用PFX文件，测试主体结果不应被清理失败覆盖。
            }
        }
    }
}
