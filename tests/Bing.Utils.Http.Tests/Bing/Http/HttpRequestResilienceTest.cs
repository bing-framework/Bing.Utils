using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Bing.Http;
using Bing.Http.Clients;

namespace Bing.Utils.Http.Tests.Bing.Http;



/// <summary>
/// HTTP 请求隔离、资源释放与显式弹性策略回归测试。
/// </summary>
public class HttpRequestResilienceTest
{
    /// <summary>
    /// 验证请求不会修改注入的客户端。
    /// </summary>
    [Fact]
    public async Task RequestOptions_DoNotMutateInjectedClient()
    {
        Uri received = null;
        using var client = CreateClient((request, _) =>
        {
            received = request.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") };
        });
        client.BaseAddress = new Uri("https://original.example/");
        client.Timeout = TimeSpan.FromSeconds(33);

        var result = await new HttpRequest<string>(null, client, HttpMethod.Get, "items")
            .BaseAddress("https://override.example/api/")
            .Timeout(2)
            .GetResultAsync();

        result.ShouldBe("ok");
        received.ShouldBe(new Uri("https://override.example/api/items"));
        client.BaseAddress.ShouldBe(new Uri("https://original.example/"));
        client.Timeout.ShouldBe(TimeSpan.FromSeconds(33));
    }



    /// <summary>
    /// 验证是否拒绝之前发送。
    /// </summary>
    [Fact]
    public void SharedHandlerOptions_AreRejectedBeforeSend()
    {
        using var client = CreateClient((_, _) => new HttpResponseMessage(HttpStatusCode.OK));
        var request = new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/");
        Should.Throw<NotSupportedException>(() => request.IgnoreSsl());
        Should.Throw<NotSupportedException>(() => request.Certificate("test.pfx", "password"));
        Should.Throw<NotSupportedException>(() => request.UseCookies(false));
    }



    /// <summary>
    /// 验证不修改真实处理程序属性。
    /// </summary>
    [Fact]
    public void SharedHandlerOptions_DoNotMutateRealHandlerProperties()
    {
        using var rsa = RSA.Create(2048);
        var certificateRequest = new CertificateRequest(
            "CN=http-request-test",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        using var certificate = certificateRequest.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow.AddMinutes(1));

        var cookieContainer = new CookieContainer();
        var cookieUri = new Uri("https://example.test/");
        cookieContainer.Add(cookieUri, new Cookie("session", "before"));
        Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> certificateValidator =
            (_, _, _, _) => true;
        using var handler = new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = cookieContainer,
            ServerCertificateCustomValidationCallback = certificateValidator
        };
        var clientCertificates = handler.ClientCertificates;
        clientCertificates.Add(certificate);
        using var client = new HttpClient(handler);
        var request = new HttpRequest<string>(null, client, HttpMethod.Get, cookieUri.ToString());

        Should.Throw<NotSupportedException>(() => request.IgnoreSsl());
        Should.Throw<NotSupportedException>(() => request.Certificate("test.pfx", "password"));
        Should.Throw<NotSupportedException>(() => request.UseCookies(false));

        handler.UseCookies.ShouldBeTrue();
        ReferenceEquals(handler.CookieContainer, cookieContainer).ShouldBeTrue();
        cookieContainer.GetCookieHeader(cookieUri).ShouldBe("session=before");
        ReferenceEquals(handler.ServerCertificateCustomValidationCallback, certificateValidator).ShouldBeTrue();
        ReferenceEquals(handler.ClientCertificates, clientCertificates).ShouldBeTrue();
        handler.ClientCertificates.Count.ShouldBe(1);
        ReferenceEquals(handler.ClientCertificates[0], certificate).ShouldBeTrue();
    }



    /// <summary>
    /// 验证释放响应并执行最终回调。
    /// </summary>
    [Fact]
    public async Task Result_DisposesResponseAndRunsFinalCallbacks()
    {
        var content = new TrackingContent("done");
        var completed = false;
        using var client = CreateClient((_, _) => new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        var result = await new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
            .OnComplete((_, _) => completed = true)
            .GetResultAsync();
        result.ShouldBe("done");
        completed.ShouldBeTrue();
        content.IsDisposed.ShouldBeTrue();
    }



    /// <summary>
    /// 验证释放调用方拥有的上传流。
    /// </summary>
    [Fact]
    public async Task SendBeforeRejectsRequest_DisposesOwnedUploadStream()
    {
        var source = new MemoryStream(new byte[] { 1, 2, 3 });
        using var client = CreateClient((_, _) => throw new InvalidOperationException("The request should not be sent."));
        var result = await new HttpRequest<string>(null, client, HttpMethod.Post, "https://example.test/")
            .FileContent(source, "upload.bin")
            .OnSendBefore(_ => false)
            .GetResultAsync();
        result.ShouldBeNull();
        source.CanRead.ShouldBeFalse();
    }



    /// <summary>
    /// 验证释放此前已拥有的流。
    /// </summary>
    [Fact]
    public async Task ReplacingUpload_DisposesPreviouslyOwnedStream()
    {
        var previous = new MemoryStream(new byte[] { 1 });
        using var replacement = new MemoryStream(new byte[] { 2 });
        using var client = CreateClient((_, _) => new HttpResponseMessage(HttpStatusCode.OK)
        { Content = new StringContent("ok") });
        var request = new HttpRequest<string>(null, client, HttpMethod.Post, "https://example.test/");

        request.FileContent(previous, "old.bin");
        request.FileContent(replacement, "new.bin", "file", leaveOpen: true);
        previous.CanRead.ShouldBeFalse();
        await request.GetResultAsync();
        replacement.CanRead.ShouldBeTrue();
    }



    /// <summary>
    /// 验证释放调用方拥有的上传流。
    /// </summary>
    [Fact]
    public async Task InvalidDownloadDestination_DisposesOwnedUploadStream()
    {
        var source = new MemoryStream(new byte[] { 1 });
        using var client = CreateClient((_, _) => throw new InvalidOperationException("No send expected."));
        var request = new HttpRequest<string>(null, client, HttpMethod.Post, "https://example.test/");
        request.FileContent(source, "upload.bin");

        await Should.ThrowAsync<ArgumentNullException>(() => request.DownloadToAsync(null));
        source.CanRead.ShouldBeFalse();
    }



    /// <summary>
    /// 验证调用现有受保护的响应重写方法。
    /// </summary>
    [Fact]
    public async Task GetResultAsync_InvokesExistingProtectedResponseOverride()
    {
        using var client = CreateClient((_, _) => new HttpResponseMessage(HttpStatusCode.OK)
        { Content = new StringContent("ignored") });

        var result = await new OverridingResponseRequest(client).GetResultAsync();

        result.ShouldBe("overridden");
    }



    /// <summary>
    /// 验证释放调用方拥有的上传流。
    /// </summary>
    [Fact]
    public async Task RetryRejectsNonSeekableCallerStream_DisposesOwnedUploadStream()
    {
        var source = new NonSeekableStream(new byte[] { 1, 2, 3 });
        using var client = CreateClient((_, _) => throw new InvalidOperationException("The request should not be sent."));
        var options = new HttpRetryOptions();
        options.AllowedMethods.Add(HttpMethod.Post);
        await Should.ThrowAsync<NotSupportedException>(async () =>
            await new HttpRequest<string>(null, client, HttpMethod.Post, "https://example.test/")
                .FileContent(source, "upload.bin")
                .Retry(options).GetResultAsync());
        source.CanRead.ShouldBeFalse();
    }



    /// <summary>
    /// 验证重放调用方提供的可定位流流。
    /// </summary>
    [Fact]
    public async Task Retry_ReplaysSeekableCallerStream()
    {
        var source = new MemoryStream(new byte[] { 1, 2, 3 });
        var bodies = new List<byte[]>();
        var attempts = 0;
        using var client = CreateClient((request, _) =>
        {
            var multipart = (MultipartFormDataContent)request.Content;
            bodies.Add(multipart.Single().ReadAsByteArrayAsync().GetAwaiter().GetResult());
            attempts++;
            return new HttpResponseMessage(attempts == 1 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK)
            { Content = new StringContent("ok") };
        });
        var options = new HttpRetryOptions { BaseDelay = TimeSpan.Zero, MaxDelay = TimeSpan.Zero, UseJitter = false };
        options.AllowedMethods.Add(HttpMethod.Post);

        var result = await new HttpRequest<string>(null, client, HttpMethod.Post, "https://example.test/")
            .FileContent(source, "upload.bin")
            .Retry(options)
            .GetResultAsync();

        result.ShouldBe("ok");
        attempts.ShouldBe(2);
        bodies.Count.ShouldBe(2);
        bodies[0].ShouldBe(bodies[1]);
        source.CanRead.ShouldBeFalse();
    }



    /// <summary>
    /// 验证释放响应。
    /// </summary>
    [Fact]
    public async Task SendAfterThrows_DisposesResponse()
    {
        var content = new TrackingContent("response");
        var completed = 0;
        using var client = CreateClient((_, _) => new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
                .OnSendAfter(_ => throw new InvalidOperationException("callback"))
                .OnComplete((_, _) => completed++)
                .GetResultAsync());
        content.IsDisposed.ShouldBeTrue();
        completed.ShouldBe(1);
    }



    /// <summary>
    /// 验证取消保留响应在回调完成前保持有效完成。
    /// </summary>
    [Fact]
    public async Task SendAfter_CancellationKeepsResponseAliveUntilCallbackCompletes()
    {
        var content = new TrackingContent("response");
        var callbackStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var callbackSawLiveResponse = false;
        var callbackSawCancellation = false;
        var completed = false;
        var completedBeforeDispose = false;
        using var cancellation = new CancellationTokenSource();
        using var client = CreateClient((_, _) => new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        var request = new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
            .OnSendAfter(async (response, token) =>
            {
                callbackSawLiveResponse = !content.IsDisposed &&
                    await response.Content.ReadAsStringAsync() == "response";
                callbackStarted.TrySetResult(true);
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, token);
                    return "unexpected";
                }
                finally
                {
                    callbackSawCancellation = token.IsCancellationRequested;
                }
            })
            .OnComplete((_, _) =>
            {
                completed = true;
                completedBeforeDispose = !content.IsDisposed;
            });

        var execution = request.GetResultAsync(cancellation.Token);
        await callbackStarted.Task;
        cancellation.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() => execution);
        callbackSawLiveResponse.ShouldBeTrue();
        callbackSawCancellation.ShouldBeTrue();
        completed.ShouldBeTrue();
        completedBeforeDispose.ShouldBeTrue();
        content.IsDisposed.ShouldBeTrue();
    }



    /// <summary>
    /// 验证安全方法重试瞬时故障状态不安全方法是否不。
    /// </summary>
    [Fact]
    public async Task Retry_SafeMethodRetriesTransientStatus_UnsafeMethodDoesNot()
    {
        var attempts = 0;
        using var client = CreateClient((_, _) =>
        {
            attempts++;
            return new HttpResponseMessage(attempts == 1 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK)
            { Content = new StringContent("ok") };
        });
        var options = new HttpRetryOptions { BaseDelay = TimeSpan.Zero, MaxDelay = TimeSpan.Zero, UseJitter = false };
        var get = await new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
            .Retry(options).GetResultAsync();
        get.ShouldBe("ok");
        attempts.ShouldBe(2);

        attempts = 0;
        var post = await new HttpRequest<string>(null, client, HttpMethod.Post, "https://example.test/")
            .Retry(options).GetResultAsync();
        post.ShouldBeNull();
        attempts.ShouldBe(1);
    }

    /// <summary>
    /// 验证宿主 Handler 已声明负责重试时，请求级重试会在发送前失败。
    /// </summary>
    [Fact]
    public async Task Retry_HostHandlerOwnershipFailsBeforeFirstSend()
    {
        var attempts = 0;
        using var client = CreateClient((_, _) =>
        {
            attempts++;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") };
        });
        var options = new HttpRetryOptions
        {
            HostHandlerOwnsRetries = true,
            BaseDelay = TimeSpan.Zero,
            MaxDelay = TimeSpan.Zero,
            UseJitter = false
        };

        var error = await Should.ThrowAsync<InvalidOperationException>(() =>
            new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
                .Retry(options).GetResultAsync());

        error.Message.ShouldContain("handler");
        attempts.ShouldBe(0);
    }

    /// <summary>
    /// 验证宿主 Handler 重试声明默认为关闭并保留请求级重试行为。
    /// </summary>
    [Fact]
    public async Task Retry_HostHandlerOwnershipDefaultsToFalse()
    {
        var attempts = 0;
        using var client = CreateClient((_, _) =>
        {
            attempts++;
            return new HttpResponseMessage(attempts == 1 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK)
            { Content = new StringContent("ok") };
        });

        var result = await new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
            .Retry(new HttpRetryOptions { BaseDelay = TimeSpan.Zero, MaxDelay = TimeSpan.Zero, UseJitter = false })
            .GetResultAsync();

        result.ShouldBe("ok");
        attempts.ShouldBe(2);
    }

    /// <summary>
    /// 验证启用幂等键约束时，缺少请求头会在发送前失败。
    /// </summary>
    [Fact]
    public async Task Retry_UnsafeMethodWithRequiredIdempotencyKeyFailsBeforeSendWhenHeaderMissing()
    {
        var attempts = 0;
        using var client = CreateClient((_, _) =>
        {
            attempts++;
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        });
        var options = new HttpRetryOptions
        {
            BaseDelay = TimeSpan.Zero,
            MaxDelay = TimeSpan.Zero,
            UseJitter = false,
            RequireIdempotencyKeyForUnsafeMethods = true
        };
        options.AllowedMethods.Add(HttpMethod.Post);

        var error = await Should.ThrowAsync<InvalidOperationException>(() =>
            new HttpRequest<string>(null, client, HttpMethod.Post, "https://example.test/")
                .Retry(options).GetResultAsync());

        error.Message.ShouldContain("Idempotency-Key");
        attempts.ShouldBe(0);
    }

    /// <summary>
    /// 验证带幂等键的非安全方法可以重试且所有尝试复用同一个键。
    /// </summary>
    [Fact]
    public async Task Retry_UnsafeMethodWithRequiredIdempotencyKeyRetriesAndPreservesHeader()
    {
        var attempts = 0;
        var keys = new List<string>();
        using var client = CreateClient((request, _) =>
        {
            attempts++;
            keys.Add(request.Headers.GetValues("Idempotency-Key").Single());
            return new HttpResponseMessage(attempts == 1 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK)
            { Content = new StringContent("ok") };
        });
        var options = new HttpRetryOptions
        {
            BaseDelay = TimeSpan.Zero,
            MaxDelay = TimeSpan.Zero,
            UseJitter = false,
            RequireIdempotencyKeyForUnsafeMethods = true
        };
        options.AllowedMethods.Add(HttpMethod.Post);

        var result = await new HttpRequest<string>(null, client, HttpMethod.Post, "https://example.test/")
            .Header("Idempotency-Key", "order-123")
            .Retry(options)
            .GetResultAsync();

        result.ShouldBe("ok");
        attempts.ShouldBe(2);
        keys.ShouldBe(new[] { "order-123", "order-123" });
    }

    /// <summary>
    /// 验证未启用幂等键约束时，显式允许的非安全方法保持原有重试行为。
    /// </summary>
    [Fact]
    public async Task Retry_UnsafeMethodWithoutRequirementKeepsExistingOptInBehavior()
    {
        var attempts = 0;
        using var client = CreateClient((_, _) =>
        {
            attempts++;
            return new HttpResponseMessage(attempts == 1 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK)
            { Content = new StringContent("ok") };
        });
        var options = new HttpRetryOptions { BaseDelay = TimeSpan.Zero, MaxDelay = TimeSpan.Zero, UseJitter = false };
        options.AllowedMethods.Add(HttpMethod.Post);

        var result = await new HttpRequest<string>(null, client, HttpMethod.Post, "https://example.test/")
            .Retry(options)
            .GetResultAsync();

        result.ShouldBe("ok");
        attempts.ShouldBe(2);
    }

    /// <summary>
    /// 验证调用方设置的关联 ID 在请求重试的每次尝试中保持不变。
    /// </summary>
    [Fact]
    public async Task Retry_PreservesCallerCorrelationIdAcrossAttempts()
    {
        var attempts = 0;
        var correlationIds = new List<string>();
        using var client = CreateClient((request, _) =>
        {
            attempts++;
            correlationIds.Add(request.Headers.GetValues("X-Correlation-Id").Single());
            return new HttpResponseMessage(attempts == 1 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK)
            { Content = new StringContent("ok") };
        });
        var options = new HttpRetryOptions { BaseDelay = TimeSpan.Zero, MaxDelay = TimeSpan.Zero, UseJitter = false };

        var result = await new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
            .Header("X-Correlation-Id", "trace-42")
            .Retry(options)
            .GetResultAsync();

        result.ShouldBe("ok");
        attempts.ShouldBe(2);
        correlationIds.ShouldBe(new[] { "trace-42", "trace-42" });
    }



    /// <summary>
    /// 验证配置的重试次数和状态码。
    /// </summary>
    /// <param name="statusCode">测试输入值。</param>
    [Theory]
    [InlineData(408)]
    [InlineData(429)]
    [InlineData(500)]
    [InlineData(502)]
    [InlineData(503)]
    [InlineData(504)]
    public async Task Retry_RetriesConfiguredStatusCodes(int statusCode)
    {
        var attempts = 0;
        using var client = CreateClient((_, _) =>
        {
            attempts++;
            return new HttpResponseMessage(attempts == 1 ? (HttpStatusCode)statusCode : HttpStatusCode.OK)
            { Content = new StringContent("ok") };
        });
        var options = new HttpRetryOptions
        {
            MaxRetries = 1,
            BaseDelay = TimeSpan.Zero,
            MaxDelay = TimeSpan.Zero,
            UseJitter = false
        };

        var result = await new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
            .Retry(options)
            .GetResultAsync();

        result.ShouldBe("ok");
        attempts.ShouldBe(2);
    }



    /// <summary>
    /// 验证是否不重试时发送之前不会影响安全性方法到不安全方法。
    /// </summary>
    [Fact]
    public async Task Retry_DoesNotRetryWhenSendBeforeChangesSafeMethodToUnsafeMethod()
    {
        var attempts = 0;
        using var client = CreateClient((_, _) =>
        {
            attempts++;
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            { Content = new StringContent("retry must not repeat this write") };
        });
        var options = new HttpRetryOptions { BaseDelay = TimeSpan.Zero, MaxDelay = TimeSpan.Zero, UseJitter = false };

        var result = await new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
            .Retry(options)
            .OnSendBefore(message =>
            {
                message.Method = HttpMethod.Post;
                return true;
            })
            .GetResultAsync();

        result.ShouldBeNull();
        attempts.ShouldBe(1);
    }



    /// <summary>
    /// 验证使用重试之后增量或日期。
    /// </summary>
    /// <param name="useDate">测试输入值。</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Retry_UsesRetryAfterDeltaOrDate(bool useDate)
    {
        var now = new DateTimeOffset(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);
        var attempts = 0;
        using var client = CreateClient((_, _) =>
        {
            attempts++;
            if (attempts > 1)
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") };

            var response = new HttpResponseMessage((HttpStatusCode)429) { Content = new StringContent("wait") };
            response.Headers.RetryAfter = useDate
                ? new System.Net.Http.Headers.RetryConditionHeaderValue(now.AddSeconds(7))
                : new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(3));
            return response;
        });
        var request = new RetryTimingRequest(client, now);
        var options = new HttpRetryOptions { BaseDelay = TimeSpan.Zero, MaxDelay = TimeSpan.Zero, UseJitter = false };

        var result = await request.Run(options);

        result.ShouldBe("ok");
        attempts.ShouldBe(2);
        request.Delays.ShouldBe(new[] { useDate ? TimeSpan.FromSeconds(7) : TimeSpan.FromSeconds(3) });
    }



    /// <summary>
    /// 验证停止之后已配置重试数量。
    /// </summary>
    [Fact]
    public async Task Retry_StopsAfterConfiguredRetryCount()
    {
        var attempts = 0;
        using var client = CreateClient((_, _) =>
        {
            attempts++;
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { Content = new StringContent("retry") };
        });
        var request = new RetryTimingRequest(client, DateTimeOffset.UtcNow);
        var options = new HttpRetryOptions
        {
            MaxRetries = 2,
            BaseDelay = TimeSpan.Zero,
            MaxDelay = TimeSpan.Zero,
            UseJitter = false
        };

        var result = await request.Run(options);

        result.ShouldBeNull();
        attempts.ShouldBe(3);
        request.Delays.Count.ShouldBe(2);
    }



    /// <summary>
    /// 验证使用确定性抖动并限制指数退避时长。
    /// </summary>
    [Fact]
    public async Task Retry_UsesDeterministicJitterAndCapsExponentialBackoff()
    {
        var attempts = 0;
        using var client = CreateClient((_, _) =>
        {
            attempts++;
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { Content = new StringContent("retry") };
        });
        var request = new RetryTimingRequest(client, DateTimeOffset.UtcNow, retryJitter: 0.5);
        var options = new HttpRetryOptions
        {
            MaxRetries = 4,
            BaseDelay = TimeSpan.FromMilliseconds(100),
            MaxDelay = TimeSpan.FromMilliseconds(250),
            UseJitter = true
        };

        var result = await request.Retry(options).GetResultAsync();

        result.ShouldBeNull();
        attempts.ShouldBe(5);
        request.Delays.ShouldBe(new[]
        {
            TimeSpan.FromMilliseconds(75),
            TimeSpan.FromMilliseconds(150),
            TimeSpan.FromMilliseconds(187.5),
            TimeSpan.FromMilliseconds(187.5)
        });
    }



    /// <summary>
    /// 验证停止时重试之后超过剩余限制超时预算。
    /// </summary>
    [Fact]
    public async Task Retry_StopsWhenRetryAfterExceedsRemainingTimeoutBudget()
    {
        var now = new DateTimeOffset(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);
        var attempts = 0;
        using var client = CreateClient((_, _) =>
        {
            attempts++;
            var response = new HttpResponseMessage((HttpStatusCode)429) { Content = new StringContent("wait") };
            response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(6));
            return response;
        });
        var request = new RetryTimingRequest(client, now);
        var options = new HttpRetryOptions
        {
            BaseDelay = TimeSpan.FromSeconds(1),
            MaxDelay = TimeSpan.FromSeconds(1),
            UseJitter = false
        };

        var result = await request.Retry(options).Timeout(TimeSpan.FromSeconds(5)).GetResultAsync();

        result.ShouldBeNull();
        attempts.ShouldBe(1);
        request.Delays.ShouldBeEmpty();
    }



    /// <summary>
    /// 验证使用一个超时所有尝试共用的预算。
    /// </summary>
    [Fact]
    public async Task Retry_UsesOneTimeoutBudgetAcrossAllAttempts()
    {
        var now = new DateTimeOffset(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);
        var attempts = 0;
        using var client = CreateClient((_, _) =>
        {
            attempts++;
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { Content = new StringContent("retry") };
        });
        var request = new RetryTimingRequest(client, now);
        var options = new HttpRetryOptions
        {
            MaxRetries = 5,
            BaseDelay = TimeSpan.FromSeconds(4),
            MaxDelay = TimeSpan.FromSeconds(4),
            UseJitter = false
        };

        var result = await request.Retry(options).Timeout(TimeSpan.FromSeconds(10)).GetResultAsync();

        result.ShouldBeNull();
        attempts.ShouldBe(3);
        request.Delays.ShouldBe(new[] { TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(4) });
    }



    /// <summary>
    /// 验证无限超时允许已配置退避策略。
    /// </summary>
    [Fact]
    public async Task Retry_InfiniteTimeoutAllowsConfiguredBackoff()
    {
        var attempts = 0;
        using var client = CreateClient((_, _) =>
        {
            attempts++;
            return new HttpResponseMessage(attempts == 1 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK)
            { Content = new StringContent("ok") };
        });
        var request = new RetryTimingRequest(client, DateTimeOffset.UtcNow);
        var options = new HttpRetryOptions
        {
            MaxRetries = 1,
            BaseDelay = TimeSpan.FromSeconds(1),
            MaxDelay = TimeSpan.FromSeconds(1),
            UseJitter = false
        };

        var result = await request.Retry(options).Timeout(Timeout.InfiniteTimeSpan).GetResultAsync();

        result.ShouldBe("ok");
        attempts.ShouldBe(2);
        request.Delays.ShouldBe(new[] { TimeSpan.FromSeconds(1) });
    }



    /// <summary>
    /// 验证接受厂商 JSON 媒体类型。
    /// </summary>
    [Fact]
    public async Task JsonContent_AcceptsVendorJsonMediaType()
    {
        string body = null;
        string mediaType = null;
        using var client = CreateClient((request, _) =>
        {
            body = request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            mediaType = request.Content.Headers.ContentType.MediaType;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") };
        });

        await new HttpRequest<string>(null, client, HttpMethod.Post, "https://example.test/")
            .JsonContent(new { Name = "sample" })
            .ContentType("application/vnd.example+json")
            .GetResultAsync();

        mediaType.ShouldBe("application/vnd.example+json");
        body.ShouldContain("sample");
    }



    /// <summary>
    /// 验证接受厂商 JSON 媒体类型使用字符集参数。
    /// </summary>
    [Fact]
    public async Task JsonContent_AcceptsVendorJsonMediaTypeWithCharsetParameter()
    {
        string body = null;
        string mediaType = null;
        string charset = null;
        using var client = CreateClient((request, _) =>
        {
            body = request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            mediaType = request.Content.Headers.ContentType.MediaType;
            charset = request.Content.Headers.ContentType.CharSet;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") };
        });

        await new HttpRequest<string>(null, client, HttpMethod.Post, "https://example.test/")
            .JsonContent(new { Name = "sample" })
            .ContentType("application/vnd.example+json; charset=utf-8")
            .GetResultAsync();

        mediaType.ShouldBe("application/vnd.example+json");
        charset.ShouldBe("utf-8");
        body.ShouldContain("sample");
    }



    /// <summary>
    /// 验证拒绝未知长度响应。
    /// </summary>
    [Fact]
    public async Task Limit_RejectsUnknownLengthResponse()
    {
        using var client = CreateClient((_, _) => new HttpResponseMessage(HttpStatusCode.OK)
        { Content = new UnknownLengthContent(new byte[11]) });
        await Should.ThrowAsync<HttpResponseSizeLimitException>(async () =>
            await new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
                .MaxResponseBytes(10).GetStreamAsync());
    }



    /// <summary>
    /// 验证拒绝过大的自定义回调正文并完成。
    /// </summary>
    [Fact]
    public async Task Limit_RejectsOversizedCustomCallbackBodyAndCompletes()
    {
        var completed = 0;
        var invoked = false;
        using var client = CreateClient((_, _) => new HttpResponseMessage(HttpStatusCode.OK)
        { Content = new UnknownLengthContent(new byte[11]) });

        await Should.ThrowAsync<HttpResponseSizeLimitException>(async () =>
            await new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
                .MaxResponseBytes(10)
                .OnSendAfter(_ =>
                {
                    invoked = true;
                    return Task.FromResult("unexpected");
                })
                .OnComplete((_, _) => completed++)
                .GetResultAsync());

        invoked.ShouldBeFalse();
        completed.ShouldBe(1);
    }



    /// <summary>
    /// 验证不能将调用方拥有的流作为响应返回。
    /// </summary>
    [Fact]
    public async Task SendAfter_CannotReturnResponseOwnedStream()
    {
        using var client = CreateClient((_, _) => new HttpResponseMessage(HttpStatusCode.OK)
        { Content = new ByteArrayContent(new byte[] { 1 }) });

        await Should.ThrowAsync<NotSupportedException>(async () =>
            await new HttpRequest<Stream>(null, client, HttpMethod.Get, "https://example.test/")
                .OnSendAfter(response => response.Content.ReadAsStreamAsync())
                .GetResultAsync());
    }



    /// <summary>
    /// 验证允许返回独立流。
    /// </summary>
    [Fact]
    public async Task SendAfter_AllowsReturningIndependentStream()
    {
        var content = new TrackingContent("response");
        using var client = CreateClient((_, _) => new HttpResponseMessage(HttpStatusCode.OK) { Content = content });

        var result = await new HttpRequest<Stream>(null, client, HttpMethod.Get, "https://example.test/")
            .OnSendAfter(_ => Task.FromResult<Stream>(new MemoryStream(new byte[] { 4, 5, 6 })))
            .GetResultAsync();

        content.IsDisposed.ShouldBeTrue();
        result.CanRead.ShouldBeTrue();
        using (result)
        using (var copy = new MemoryStream())
        {
            await result.CopyToAsync(copy);
            copy.ToArray().ShouldBe(new byte[] { 4, 5, 6 });
        }
    }



    /// <summary>
    /// 验证保持目标流保持打开。
    /// </summary>
    [Fact]
    public async Task DownloadToAsync_LeavesDestinationOpen()
    {
        using var client = CreateClient((_, _) => new HttpResponseMessage(HttpStatusCode.OK)
        { Content = new ByteArrayContent(new byte[] { 1, 2, 3 }) });
        using var destination = new MemoryStream();
        await new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
            .DownloadToAsync(destination);
        destination.CanWrite.ShouldBeTrue();
        destination.ToArray().ShouldBe(new byte[] { 1, 2, 3 });
    }



    /// <summary>
    /// 验证处理空正文。
    /// </summary>
    [Fact]
    public async Task DownloadToAsync_HandlesEmptyBody()
    {
        using var client = CreateClient((_, _) => new HttpResponseMessage(HttpStatusCode.OK)
        { Content = new ByteArrayContent(Array.Empty<byte>()) });
        using var destination = new MemoryStream();

        await new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
            .DownloadToAsync(destination);

        destination.CanWrite.ShouldBeTrue();
        destination.ToArray().ShouldBeEmpty();
    }



    /// <summary>
    /// 验证复制较短内容读取从不可定位源流。
    /// </summary>
    [Fact]
    public async Task DownloadToAsync_CopiesShortReadsFromNonSeekableSource()
    {
        var bytes = Enumerable.Range(1, 257).Select(x => (byte)x).ToArray();
        using var client = CreateClient((_, _) => new HttpResponseMessage(HttpStatusCode.OK)
        { Content = new StreamContent(new ChunkedNonSeekableStream(bytes, maxChunkSize: 3)) });
        using var destination = new MemoryStream();

        await new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
            .DownloadToAsync(destination);

        destination.ToArray().ShouldBe(bytes);
    }



    /// <summary>
    /// 验证遵循重试后的延迟时间。
    /// </summary>
    [Fact]
    public async Task Retry_RespectsRetryAfterDelay()
    {
        var attempts = 0;
        using var client = CreateClient((_, _) =>
        {
            attempts++;
            if (attempts != 1) return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") };
            var response = new HttpResponseMessage((HttpStatusCode)429) { Content = new StringContent("wait") };
            response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromMilliseconds(80));
            return response;
        });
        var options = new HttpRetryOptions { BaseDelay = TimeSpan.Zero, MaxDelay = TimeSpan.Zero, UseJitter = false };
        var timer = Stopwatch.StartNew();
        var result = await new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
            .Retry(options).GetResultAsync();
        timer.Stop();
        result.ShouldBe("ok");
        attempts.ShouldBe(2);
        timer.ElapsedMilliseconds.ShouldBeGreaterThanOrEqualTo(65);
    }



    /// <summary>
    /// 验证取消期间退避策略停止后续尝试。
    /// </summary>
    [Fact]
    public async Task Retry_CancelDuringBackoffStopsFurtherAttempts()
    {
        var attempts = 0;
        using var client = CreateClient((_, _) =>
        {
            attempts++;
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { Content = new StringContent("wait") };
        });
        using var cancellation = new CancellationTokenSource();
        var retryScheduled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var options = new HttpRetryOptions { BaseDelay = TimeSpan.FromSeconds(1), MaxDelay = TimeSpan.FromSeconds(1), UseJitter = false };
        var execution = new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
            .ObserveRetry(_ =>
            {
                retryScheduled.TrySetResult(true);
                cancellation.Cancel();
            })
            .Retry(options)
            .GetResultAsync(cancellation.Token);

        await Task.WhenAny(retryScheduled.Task, execution);
        retryScheduled.Task.IsCompleted.ShouldBeTrue();
        await Should.ThrowAsync<OperationCanceledException>(() => execution);
        attempts.ShouldBe(1);
    }



    /// <summary>
    /// 验证是否不重试嵌套证书失败。
    /// </summary>
    [Fact]
    public async Task Retry_DoesNotRetryNestedCertificateFailure()
    {
        var attempts = 0;
        using var client = CreateClient((_, _) =>
        {
            attempts++;
            throw new HttpRequestException("TLS failed", new IOException("Handshake failed",
                new System.Security.Authentication.AuthenticationException("Invalid certificate")));
        });
        var options = new HttpRetryOptions { BaseDelay = TimeSpan.Zero, MaxDelay = TimeSpan.Zero, UseJitter = false };
        await Should.ThrowAsync<HttpRequestException>(async () =>
            await new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/")
                .Retry(options).GetResultAsync());
        attempts.ShouldBe(1);
    }



    /// <summary>
    /// 验证显式 POST 请求会为每次尝试重新创建正文。
    /// </summary>
    [Fact]
    public async Task Retry_ExplicitPostRebuildsBodyForEachAttempt()
    {
        var attempts = 0;
        var bodies = new List<string>();
        using var client = CreateClient((request, _) =>
        {
            bodies.Add(request.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            attempts++;
            return new HttpResponseMessage(attempts == 1 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK)
            { Content = new StringContent("ok") };
        });
        var options = new HttpRetryOptions { BaseDelay = TimeSpan.Zero, MaxDelay = TimeSpan.Zero, UseJitter = false };
        options.AllowedMethods.Add(HttpMethod.Post);
        var result = await new HttpRequest<string>(null, client, HttpMethod.Post, "https://example.test/")
            .JsonContent(new { Name = "repeat" }).Retry(options).GetResultAsync();
        result.ShouldBe("ok");
        bodies.Count.ShouldBe(2);
        bodies[0].ShouldBe(bodies[1]);
    }



    /// <summary>
    /// 验证并发请求保留各自的地址和响应头。
    /// </summary>
    [Fact]
    public async Task SharedClient_ConcurrentRequestsKeepUrisAndHeadersSeparate()
    {
        var received = new System.Collections.Concurrent.ConcurrentBag<string>();
        using var client = CreateClient((request, _) =>
        {
            received.Add(request.RequestUri + ":" + request.Headers.GetValues("X-Request").Single());
            Thread.Sleep(20);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") };
        });
        var first = Task.Run(() => new HttpRequest<string>(null, client, HttpMethod.Get, "item")
            .BaseAddress("https://one.example/").Header("X-Request", "one").GetResultAsync());
        var second = Task.Run(() => new HttpRequest<string>(null, client, HttpMethod.Get, "item")
            .BaseAddress("https://two.example/").Header("X-Request", "two").GetResultAsync());
        await Task.WhenAll(first, second);
        received.OrderBy(x => x).ShouldBe(new[]
        {
            "https://one.example/item:one", "https://two.example/item:two"
        });
        client.BaseAddress.ShouldBeNull();
    }



    /// <summary>
    /// 验证并发请求使用各自的超时时间。
    /// </summary>
    [Fact]
    public async Task SharedClient_ConcurrentRequestsUseIndependentTimeouts()
    {
        using var client = new HttpClient(new DelayedHandler(TimeSpan.FromMilliseconds(300)));
        var shortRequest = new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/short")
            .Timeout(TimeSpan.FromMilliseconds(120)).GetResultAsync();
        var longRequest = new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/long")
            .Timeout(TimeSpan.FromSeconds(2)).GetResultAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => shortRequest);
        (await longRequest).ShouldBe("ok");
    }



    /// <summary>
    /// 验证复用处理程序未使用变化It。
    /// </summary>
    [Fact]
    public async Task NamedFactoryClient_ReusesHandlerWithoutChangingIt()
    {
        var handler = new StubHandler((request, _) =>
            new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent(request.Headers.GetValues("X-Request").Single()) });
        using (handler)
        {
            var factory = new NamedFactory(handler);
            var service = new HttpClientService(factory);
            var first = await service.Get("https://example.test/").HttpClientName("named")
                .Header("X-Request", "first").GetResultAsync();
            var second = await service.Get("https://example.test/").HttpClientName("named")
                .Header("X-Request", "second").GetResultAsync();
            first.ShouldBe("first");
            second.ShouldBe("second");
            factory.CreatedNames.ShouldBe(new[] { "named", "named" });
        }
    }



    /// <summary>
    /// 验证遵循所有权。
    /// </summary>
    /// <param name="leaveOpen">测试输入值。</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UploadStream_RespectsOwnership(bool leaveOpen)
    {
        var source = new MemoryStream(new byte[] { 1, 2, 3 });
        using var client = CreateClient((request, _) =>
        {
            request.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult().Length.ShouldBeGreaterThan(0);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") };
        });
        await new HttpRequest<string>(null, client, HttpMethod.Post, "https://example.test/")
            .FileContent(source, "upload.bin", "file", leaveOpen)
            .GetResultAsync();
        source.CanRead.ShouldBe(leaveOpen);
        if (leaveOpen) source.Dispose();
    }



    /// <summary>
    /// 验证继续之后释放失败并保留发送异常。
    /// </summary>
    [Fact]
    public async Task UploadCleanup_ContinuesAfterDisposeFailureAndPreservesSendException()
    {
        var cleanupError = new InvalidOperationException("upload dispose failed");
        var sendError = new HttpRequestException("send failed");
        var throwingStream = new ThrowingDisposeStream(cleanupError);
        var otherStream = new TrackingUploadStream();
        using var client = CreateClient((_, _) => throw sendError);
        var request = new HttpRequest<string>(null, client, HttpMethod.Post, "https://example.test/")
            .FileContent(throwingStream, "first.bin", "first")
            .FileContent(otherStream, "second.bin", "second");

        var error = await Should.ThrowAsync<HttpRequestException>(() => request.GetResultAsync());

        error.ShouldBeSameAs(sendError);
        throwingStream.DisposeCalls.ShouldBeGreaterThan(0);
        otherStream.IsDisposed.ShouldBeTrue();
    }



    /// <summary>
    /// 验证抛出清理失败之后成功请求。
    /// </summary>
    [Fact]
    public async Task UploadCleanup_ThrowsCleanupFailureAfterSuccessfulRequest()
    {
        var cleanupError = new InvalidOperationException("upload dispose failed");
        var throwingStream = new ThrowingDisposeStream(cleanupError);
        var otherStream = new TrackingUploadStream();
        using var client = CreateClient((_, _) => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("ok")
        });
        var request = new HttpRequest<string>(null, client, HttpMethod.Post, "https://example.test/")
            .FileContent(throwingStream, "first.bin", "first")
            .FileContent(otherStream, "second.bin", "second");

        var error = await Should.ThrowAsync<InvalidOperationException>(() => request.GetResultAsync());

        error.ShouldBeSameAs(cleanupError);
        throwingStream.DisposeCalls.ShouldBeGreaterThan(0);
        otherStream.IsDisposed.ShouldBeTrue();
    }



    /// <summary>
    /// 验证已调度等待期间先释放响应，再进入等待。
    /// </summary>
    [Fact]
    public async Task ObserveRetry_ReportsOneScheduledWaitAfterResponseDisposalAndBeforeWaiting()
    {
        var sends = 0;
        var steps = new List<string>();
        var responseContent = new TrackingContent("wait", () => steps.Add("disposed"));
        using var client = CreateClient((_, _) => new HttpResponseMessage(++sends == 1
            ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK)
        { Content = sends == 1 ? responseContent : new StringContent("ok") });
        var events = new List<HttpRetryScheduled>();
        var completions = new List<HttpRequestCompletion>();
        var disposedAtNotification = false;
        var request = new RetryTimingRequest(client, DateTimeOffset.UtcNow)
        {
            WaitHandler = (_, _) => { steps.Add("wait"); return Task.CompletedTask; }
        };

        var result = await request.ObserveRetry(item =>
            {
                events.Add(item);
                disposedAtNotification = responseContent.IsDisposed;
                steps.Add("event");
            }).ObserveCompletion(item => { completions.Add(item); steps.Add("completion"); })
            .Retry(RetryObservationOptions()).GetResultAsync();

        result.ShouldBe("ok");
        sends.ShouldBe(2);
        disposedAtNotification.ShouldBeTrue();
        steps.ShouldBe(new[] { "disposed", "event", "wait", "completion" });
        events.Count.ShouldBe(1);
        events[0].FailedAttempt.ShouldBe(1);
        events[0].StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        events[0].Delay.ShouldBe(TimeSpan.FromMilliseconds(200));
        events[0].Exception.ShouldBeNull();
        request.Delays.ShouldBe(new[] { events[0].Delay });
        completions.Count.ShouldBe(1);
        completions[0].Outcome.ShouldBe(HttpRequestOutcome.Success);
        completions[0].Attempts.ShouldBe(2);
    }



    /// <summary>
    /// 验证报告最终重试之后增量或使用日期而非封顶抖动。
    /// </summary>
    /// <param name="useDate">测试输入值。</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ObserveRetry_ReportsFinalRetryAfterDeltaOrDateInsteadOfCappedJitter(bool useDate)
    {
        var now = new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);
        var sends = 0;
        using var client = CreateClient((_, _) =>
        {
            if (++sends > 1) return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") };
            var response = new HttpResponseMessage((HttpStatusCode)429) { Content = new StringContent("wait") };
            response.Headers.RetryAfter = useDate
                ? new System.Net.Http.Headers.RetryConditionHeaderValue(now.AddSeconds(7))
                : new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(3));
            return response;
        });
        var events = new List<HttpRetryScheduled>();
        var request = new RetryTimingRequest(client, now, retryJitter: 0.5);
        var options = RetryObservationOptions();
        options.UseJitter = true;

        (await request.ObserveRetry(events.Add).Retry(options).GetResultAsync()).ShouldBe("ok");

        events.Count.ShouldBe(1);
        events[0].Delay.ShouldBe(useDate ? TimeSpan.FromSeconds(7) : TimeSpan.FromSeconds(3));
        events[0].StatusCode.ShouldBe((HttpStatusCode)429);
        events[0].FailedAttempt.ShouldBe(1);
        request.Delays.ShouldBe(new[] { events[0].Delay });
    }



    /// <summary>
    /// 验证报告确定性抖动并封顶的指数延迟。
    /// </summary>
    [Fact]
    public async Task ObserveRetry_ReportsDeterministicJitterAndCappedExponentialDelays()
    {
        using var client = CreateClient((_, _) => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        { Content = new StringContent("wait") });
        var events = new List<HttpRetryScheduled>();
        var request = new RetryTimingRequest(client, DateTimeOffset.UtcNow, retryJitter: 0.5);
        var options = new HttpRetryOptions
        {
            MaxRetries = 4,
            BaseDelay = TimeSpan.FromMilliseconds(100),
            MaxDelay = TimeSpan.FromMilliseconds(250),
            UseJitter = true
        };

        (await request.ObserveRetry(events.Add).Retry(options).GetResultAsync()).ShouldBeNull();

        events.Select(item => item.FailedAttempt).ShouldBe(new[] { 1, 2, 3, 4 });
        events.Select(item => item.Delay).ShouldBe(new[]
        {
            TimeSpan.FromMilliseconds(75), TimeSpan.FromMilliseconds(150),
            TimeSpan.FromMilliseconds(187.5), TimeSpan.FromMilliseconds(187.5)
        });
        request.Delays.ShouldBe(events.Select(item => item.Delay));
    }



    /// <summary>
    /// 验证网络失败保留原始异常引用。
    /// </summary>
    [Fact]
    public async Task ObserveRetry_NetworkFailurePreservesOriginalExceptionReference()
    {
        var sends = 0;
        var expected = new HttpRequestException("transient send failed");
        using var client = CreateClient((_, _) =>
        {
            if (++sends == 1) throw expected;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") };
        });
        var events = new List<HttpRetryScheduled>();
        var request = new RetryTimingRequest(client, DateTimeOffset.UtcNow);

        (await request.ObserveRetry(events.Add).Retry(RetryObservationOptions()).GetResultAsync()).ShouldBe("ok");

        sends.ShouldBe(2);
        events.Count.ShouldBe(1);
        events[0].Exception.ShouldBeSameAs(expected);
        events[0].StatusCode.ShouldBeNull();
        events[0].FailedAttempt.ShouldBe(1);
        events[0].Delay.ShouldBe(TimeSpan.FromMilliseconds(200));
    }



    /// <summary>
    /// 验证是否不报告时无等待是否接受。
    /// </summary>
    /// <param name="boundary">测试输入值。</param>
    [Theory]
    [InlineData("no-retries")]
    [InlineData("unsafe-method")]
    [InlineData("timeout-budget")]
    [InlineData("delay-overflow")]
    [InlineData("non-retryable-status")]
    public async Task ObserveRetry_DoesNotReportWhenNoWaitIsAccepted(string boundary)
    {
        var sends = 0;
        var content = new TrackingContent("failed");
        using var client = CreateClient((_, _) =>
        {
            sends++;
            var response = new HttpResponseMessage(boundary == "non-retryable-status"
                ? HttpStatusCode.BadRequest : HttpStatusCode.ServiceUnavailable)
            { Content = content };
            if (boundary == "timeout-budget" || boundary == "delay-overflow")
                response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(
                    boundary == "timeout-budget" ? TimeSpan.FromSeconds(6) : TimeSpan.FromDays(60));
            return response;
        });
        var events = new List<HttpRetryScheduled>();
        var request = new RetryTimingRequest(client, DateTimeOffset.UtcNow);
        var options = RetryObservationOptions(boundary == "no-retries" ? 0 : 1);
        request.ObserveRetry(events.Add).Retry(options);
        if (boundary == "timeout-budget") request.Timeout(TimeSpan.FromSeconds(5));
        if (boundary == "unsafe-method")
            request.OnSendBefore(message => { message.Method = HttpMethod.Post; return true; });

        (await request.GetResultAsync()).ShouldBeNull();

        sends.ShouldBe(1);
        events.ShouldBeEmpty();
        request.Delays.ShouldBeEmpty();
        content.IsDisposed.ShouldBeTrue();
    }



    /// <summary>
    /// 验证网络不足重试预算保留失败未使用事件。
    /// </summary>
    [Fact]
    public async Task ObserveRetry_InsufficientNetworkRetryBudgetPreservesFailureWithoutEvent()
    {
        var expected = new HttpRequestException("no time to retry");
        var sends = 0;
        using var client = CreateClient((_, _) => { sends++; throw expected; });
        var events = new List<HttpRetryScheduled>();
        var request = new RetryTimingRequest(client, DateTimeOffset.UtcNow);
        var options = RetryObservationOptions();
        options.BaseDelay = options.MaxDelay = TimeSpan.FromSeconds(6);

        var error = await Should.ThrowAsync<HttpRequestException>(() => request.ObserveRetry(events.Add)
            .Retry(options).Timeout(TimeSpan.FromSeconds(5)).GetResultAsync());

        error.ShouldBeSameAs(expected);
        sends.ShouldBe(1);
        events.ShouldBeEmpty();
        request.Delays.ShouldBeEmpty();
    }



    /// <summary>
    /// 验证证书失败是否不调度等待。
    /// </summary>
    [Fact]
    public async Task ObserveRetry_CertificateFailureDoesNotScheduleWait()
    {
        var expected = new HttpRequestException("TLS failed", new IOException("Handshake failed",
            new System.Security.Authentication.AuthenticationException("Invalid certificate")));
        var sends = 0;
        using var client = CreateClient((_, _) => { sends++; throw expected; });
        var events = new List<HttpRetryScheduled>();
        var request = new RetryTimingRequest(client, DateTimeOffset.UtcNow);

        var error = await Should.ThrowAsync<HttpRequestException>(() => request.ObserveRetry(events.Add)
            .Retry(RetryObservationOptions()).GetResultAsync());

        error.ShouldBeSameAs(expected);
        sends.ShouldBe(1);
        events.ShouldBeEmpty();
        request.Delays.ShouldBeEmpty();
    }



    /// <summary>
    /// 验证耗尽重试数量报告仅进入零延迟等待。
    /// </summary>
    [Fact]
    public async Task ObserveRetry_ExhaustedRetryCountReportsOnlyEnteredZeroDelayWaits()
    {
        var sends = 0;
        using var client = CreateClient((_, _) =>
        {
            sends++;
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { Content = new StringContent("wait") };
        });
        var events = new List<HttpRetryScheduled>();
        var completions = new List<HttpRequestCompletion>();
        var request = new RetryTimingRequest(client, DateTimeOffset.UtcNow);
        var options = new HttpRetryOptions
        { MaxRetries = 2, BaseDelay = TimeSpan.Zero, MaxDelay = TimeSpan.Zero, UseJitter = false };

        (await request.ObserveRetry(events.Add).ObserveCompletion(completions.Add).Retry(options).GetResultAsync()).ShouldBeNull();

        sends.ShouldBe(3);
        events.Select(item => item.FailedAttempt).ShouldBe(new[] { 1, 2 });
        events.All(item => item.Delay == TimeSpan.Zero).ShouldBeTrue();
        request.Delays.Count.ShouldBe(2);
        completions.Count.ShouldBe(1);
        completions[0].Outcome.ShouldBe(HttpRequestOutcome.HttpFailure);
        completions[0].Attempts.ShouldBe(3);
    }



    /// <summary>
    /// 验证等待期间取消时保留事件并阻止下一次发送。
    /// </summary>
    [Fact]
    public async Task ObserveRetry_CancellationDuringWaitKeepsEventButPreventsNextSend()
    {
        var sends = 0;
        var content = new TrackingContent("wait");
        using var client = CreateClient((_, _) =>
        {
            sends++;
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { Content = content };
        });
        using var cancellation = new CancellationTokenSource();
        var waitStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var events = new List<HttpRetryScheduled>();
        var completions = new List<HttpRequestCompletion>();
        var request = new RetryTimingRequest(client, DateTimeOffset.UtcNow)
        {
            WaitHandler = async (_, token) =>
            {
                waitStarted.TrySetResult(true);
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
            }
        };
        var execution = request.ObserveRetry(events.Add).ObserveCompletion(completions.Add)
            .Retry(RetryObservationOptions()).GetResultAsync(cancellation.Token);
        await waitStarted.Task;
        var disposedBeforeCancel = content.IsDisposed;
        cancellation.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() => execution);

        disposedBeforeCancel.ShouldBeTrue();
        sends.ShouldBe(1);
        events.Count.ShouldBe(1);
        request.Delays.Count.ShouldBe(1);
        completions.Count.ShouldBe(1);
        completions[0].Outcome.ShouldBe(HttpRequestOutcome.Cancelled);
    }



    /// <summary>
    /// 验证取消期间释放响应会阻止事件并取消等待。
    /// </summary>
    [Fact]
    public async Task ObserveRetry_CancellationDuringResponseDisposalPreventsEventAndWait()
    {
        using var cancellation = new CancellationTokenSource();
        var content = new TrackingContent("wait", cancellation.Cancel);
        var sends = 0;
        using var client = CreateClient((_, _) =>
        {
            sends++;
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { Content = content };
        });
        var events = new List<HttpRetryScheduled>();
        var completions = new List<HttpRequestCompletion>();
        var request = new RetryTimingRequest(client, DateTimeOffset.UtcNow);

        await Should.ThrowAsync<OperationCanceledException>(() => request.ObserveRetry(events.Add)
            .ObserveCompletion(completions.Add).Retry(RetryObservationOptions()).GetResultAsync(cancellation.Token));

        content.IsDisposed.ShouldBeTrue();
        sends.ShouldBe(1);
        events.ShouldBeEmpty();
        request.Delays.ShouldBeEmpty();
        completions.Count.ShouldBe(1);
        completions[0].Outcome.ShouldBe(HttpRequestOutcome.Cancelled);
    }



    /// <summary>
    /// 验证观察器并异常处理程序异常是否隔离。
    /// </summary>
    /// <param name="errorHandling">测试输入值。</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task ObserveRetry_ObserverAndErrorHandlerExceptionsAreIsolated(int errorHandling)
    {
        var expected = new InvalidOperationException("retry observer failed");
        var observedErrors = new List<Exception>();
        var sends = 0;
        var observerCalls = 0;
        using var client = CreateClient((_, _) => new HttpResponseMessage(++sends == 1
            ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK)
        { Content = new StringContent("ok") });
        var request = new RetryTimingRequest(client, DateTimeOffset.UtcNow);
        Action<Exception> onError = errorHandling == 0 ? null : error =>
        {
            observedErrors.Add(error);
            if (errorHandling == 2) throw new ApplicationException("retry error handler failed");
        };

        var result = await request.ObserveRetry(_ => { observerCalls++; throw expected; }, onError)
            .Retry(RetryObservationOptions()).GetResultAsync();

        result.ShouldBe("ok");
        sends.ShouldBe(2);
        observerCalls.ShouldBe(1);
        request.Delays.Count.ShouldBe(1);
        if (errorHandling == 0) observedErrors.ShouldBeEmpty();
        else observedErrors.Single().ShouldBeSameAs(expected);
    }



    /// <summary>
    /// 验证注册并异常处理程序快照变化仅下一次执行。
    /// </summary>
    [Fact]
    public async Task ObserveRetry_RegistrationAndErrorHandlerSnapshotChangesOnlyNextExecution()
    {
        var sends = 0;
        var original = new List<HttpRetryScheduled>();
        var replacement = new List<HttpRetryScheduled>();
        var originalErrors = new List<Exception>();
        var replacementErrors = new List<Exception>();
        var originalError = new InvalidOperationException("original observer failed");
        var replacementError = new InvalidOperationException("replacement observer failed");
        var completions = new List<HttpRequestCompletion>();
        RetryTimingRequest request = null;
        using var client = CreateClient((_, _) =>
        {
            sends++;
            if (sends == 1)
                request.ObserveRetry(item => { replacement.Add(item); throw replacementError; }, replacementErrors.Add);
            return new HttpResponseMessage(sends % 3 == 0 ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable)
            { Content = new StringContent("ok") };
        });
        request = new RetryTimingRequest(client, DateTimeOffset.UtcNow);
        request.ObserveRetry(item => { original.Add(item); throw originalError; }, originalErrors.Add)
            .ObserveCompletion(completions.Add).Retry(RetryObservationOptions(2));

        (await request.GetResultAsync()).ShouldBe("ok");
        original.Select(item => item.FailedAttempt).ShouldBe(new[] { 1, 2 });
        originalErrors.Count.ShouldBe(2);
        originalErrors.All(error => ReferenceEquals(error, originalError)).ShouldBeTrue();
        replacement.ShouldBeEmpty();
        replacementErrors.ShouldBeEmpty();

        (await request.GetResultAsync()).ShouldBe("ok");
        original.Count.ShouldBe(2);
        originalErrors.Count.ShouldBe(2);
        replacement.Select(item => item.FailedAttempt).ShouldBe(new[] { 1, 2 });
        replacementErrors.Count.ShouldBe(2);
        replacementErrors.All(error => ReferenceEquals(error, replacementError)).ShouldBeTrue();
        sends.ShouldBe(6);
        request.Delays.Count.ShouldBe(4);
        completions.Count.ShouldBe(2);
        completions.All(item => item.Attempts == 3 && item.Outcome == HttpRequestOutcome.Success).ShouldBeTrue();
    }



    /// <summary>
    /// 创建禁用抖动的重试观察配置。
    /// </summary>
    /// <param name="maxRetries">允许的最大重试次数，默认为一次。</param>
    /// <returns>基础延迟为二百毫秒且最大延迟为一秒的重试配置。</returns>
    private static HttpRetryOptions RetryObservationOptions(int maxRetries = 1) => new HttpRetryOptions
    {
        MaxRetries = maxRetries,
        BaseDelay = TimeSpan.FromMilliseconds(200),
        MaxDelay = TimeSpan.FromSeconds(1),
        UseJitter = false
    };



    /// <summary>
    /// 创建使用指定同步响应回调的 HTTP 客户端。
    /// </summary>
    /// <param name="send">根据请求及取消令牌返回响应的测试委托。</param>
    /// <returns>使用回调消息处理器的客户端，调用方负责释放。</returns>
    private static HttpClient CreateClient(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> send)
        => new HttpClient(new StubHandler(send));



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
        /// 初始化一个 <see cref="StubHandler"/> 类型的实例。
        /// </summary>
        /// <param name="send">根据请求生成响应的委托。</param>
        public StubHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> send) => _send = send;
        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_send(request, cancellationToken));
    }



    /// <summary>
    /// 延迟指定时间后返回成功响应的消息处理器。
    /// </summary>
    private sealed class DelayedHandler : HttpMessageHandler
    {
        /// <summary>
        /// 发送响应前等待的时长。
        /// </summary>
        private readonly TimeSpan _delay;



        /// <summary>
        /// 初始化一个 <see cref="DelayedHandler"/> 类型的实例。
        /// </summary>
        /// <param name="delay">发送响应前等待的时长。</param>
        public DelayedHandler(TimeSpan delay) => _delay = delay;

        /// <inheritdoc />
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(_delay, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") };
        }
    }



    /// <summary>
    /// 允许测试指定重试时钟和抖动值的请求实现。
    /// </summary>
    private sealed class RetryTimingRequest : HttpRequest<string>
    {
        /// <summary>
        /// 测试使用的当前时间。
        /// </summary>
        private DateTimeOffset _now;
        /// <summary>
        /// 重试延迟使用的抖动比例。
        /// </summary>
        private readonly double _retryJitter;



        /// <summary>
        /// 初始化一个 <see cref="RetryTimingRequest"/> 类型的实例。
        /// </summary>
        /// <param name="client">用于发送测试请求的 HTTP 客户端。</param>
        /// <param name="now">测试使用的当前时间。</param>
        /// <param name="retryJitter">重试延迟使用的抖动比例。</param>
        public RetryTimingRequest(HttpClient client, DateTimeOffset now, double retryJitter = 0)
            : base(null, client, HttpMethod.Get, "https://example.test/")
        {
            _now = now;
            _retryJitter = retryJitter;
        }



        /// <summary>
        /// 获取按调用顺序记录的重试等待时长。
        /// </summary>
        public List<TimeSpan> Delays { get; } = new List<TimeSpan>();



        /// <summary>
        /// 获取或设置重试等待时调用的自定义异步操作。
        /// </summary>
        public Func<TimeSpan, CancellationToken, Task> WaitHandler { get; set; }



        /// <summary>
        /// 应用重试配置并异步获取响应结果。
        /// </summary>
        /// <param name="options">本次执行使用的重试策略。</param>
        /// <returns>请求最终返回的字符串结果。</returns>
        public Task<string> Run(HttpRetryOptions options) => Retry(options).GetResultAsync();

        /// <inheritdoc />
        protected override DateTimeOffset GetUtcNow() => _now;

        /// <inheritdoc />
        protected override double GetRetryJitter() => _retryJitter;

        /// <inheritdoc />
        protected override async Task WaitBeforeRetryAsync(TimeSpan delay, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Delays.Add(delay);
            if (WaitHandler != null) await WaitHandler(delay, token);
            _now += delay;
        }
    }



    /// <summary>
    /// 通过重写响应处理返回固定结果的请求实现。
    /// </summary>
    private sealed class OverridingResponseRequest : HttpRequest<string>
    {
        /// <summary>
        /// 初始化一个 <see cref="OverridingResponseRequest"/> 类型的实例。
        /// </summary>
        /// <param name="client">用于发送测试请求的 HTTP 客户端。</param>
        public OverridingResponseRequest(HttpClient client)
            : base(null, client, HttpMethod.Get, "https://example.test/") { }

        /// <inheritdoc />
        protected override Task<string> SendAfterAsync(HttpResponseMessage message)
            => Task.FromResult("overridden");
    }



    /// <summary>
    /// 记录客户端名称并使用指定处理器创建客户端的工厂。
    /// </summary>
    private sealed class NamedFactory : IHttpClientFactory
    {
        /// <summary>
        /// 模拟 HTTP 请求处理的消息处理器。
        /// </summary>
        private readonly HttpMessageHandler _handler;
        /// <summary>
        /// 获取工厂按调用顺序记录的客户端名称。
        /// </summary>
        public List<string> CreatedNames { get; } = new List<string>();
        /// <summary>
        /// 初始化一个 <see cref="NamedFactory"/> 类型的实例。
        /// </summary>
        /// <param name="handler">供工厂创建客户端时使用的消息处理器。</param>
        public NamedFactory(HttpMessageHandler handler) => _handler = handler;
        /// <inheritdoc />
        public HttpClient CreateClient(string name)
        {
            CreatedNames.Add(name);
            return new HttpClient(_handler, disposeHandler: false);
        }
    }



    /// <summary>
    /// 记录 HTTP 内容是否已释放。
    /// </summary>
    private sealed class TrackingContent : StringContent
    {
        /// <summary>
        /// 内容释放时执行的回调。
        /// </summary>
        private readonly Action _onDispose;
        /// <summary>
        /// 初始化一个 <see cref="TrackingContent"/> 类型的实例。
        /// </summary>
        /// <param name="content">测试响应正文文本。</param>
        /// <param name="onDispose">内容释放时执行的回调。</param>
        public TrackingContent(string content, Action onDispose = null) : base(content) => _onDispose = onDispose;
        /// <summary>
        /// 获取当前测试对象是否已被释放。
        /// </summary>
        public bool IsDisposed { get; private set; }
        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing && !IsDisposed)
            {
                IsDisposed = true;
                _onDispose?.Invoke();
            }
            base.Dispose(disposing);
        }
    }



    /// <summary>
    /// 记录上传流的释放状态。
    /// </summary>
    private sealed class TrackingUploadStream : MemoryStream
    {
        /// <summary>
        /// 获取当前测试对象是否已被释放。
        /// </summary>
        public bool IsDisposed { get; private set; }
        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing) IsDisposed = true;
            base.Dispose(disposing);
        }
    }



    /// <summary>
    /// 在释放时抛出指定异常的内存流。
    /// </summary>
    private sealed class ThrowingDisposeStream : MemoryStream
    {
        /// <summary>
        /// 测试异常传播行为的异常实例。
        /// </summary>
        private readonly Exception _error;
        /// <summary>
        /// 获取已记录的 Dispose 调用次数。
        /// </summary>
        public int DisposeCalls { get; private set; }



        /// <summary>
        /// 初始化一个 <see cref="ThrowingDisposeStream"/> 类型的实例。
        /// </summary>
        /// <param name="error">测试期间抛出的异常。</param>
        public ThrowingDisposeStream(Exception error) => _error = error;

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeCalls++;
                throw _error;
            }
            base.Dispose(disposing);
        }
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
        /// 初始化一个 <see cref="UnknownLengthContent"/> 类型的实例。
        /// </summary>
        /// <param name="bytes">响应正文的字节数据。</param>
        public UnknownLengthContent(byte[] bytes) => _bytes = bytes;
        /// <inheritdoc />
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
            => stream.WriteAsync(_bytes, 0, _bytes.Length);
        /// <inheritdoc />
        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }



    /// <summary>
    /// 禁用定位能力的内存流。
    /// </summary>
    private sealed class NonSeekableStream : MemoryStream
    {
        /// <summary>
        /// 初始化一个 <see cref="NonSeekableStream"/> 类型的实例。
        /// </summary>
        /// <param name="bytes">响应正文的字节数据。</param>
        public NonSeekableStream(byte[] bytes) : base(bytes) { }
        /// <inheritdoc />
        public override bool CanSeek => false;
        /// <inheritdoc />
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin loc) => throw new NotSupportedException();
    }



    /// <summary>
    /// 禁用定位并限制单次读取字节数的流。
    /// </summary>
    private sealed class ChunkedNonSeekableStream : Stream
    {
        /// <summary>
        /// 测试请求或响应使用的字节数据。
        /// </summary>
        private readonly byte[] _bytes;
        /// <summary>
        /// 单次读取允许返回的最大字节数。
        /// </summary>
        private readonly int _maxChunkSize;
        /// <summary>
        /// 测试期间记录的调用状态。
        /// </summary>
        private int _position;



        /// <summary>
        /// 初始化一个 <see cref="ChunkedNonSeekableStream"/> 类型的实例。
        /// </summary>
        /// <param name="bytes">响应正文的字节数据。</param>
        /// <param name="maxChunkSize">单次读取允许返回的最大字节数。</param>
        public ChunkedNonSeekableStream(byte[] bytes, int maxChunkSize)
        {
            _bytes = bytes;
            _maxChunkSize = maxChunkSize;
        }

        /// <inheritdoc />
        public override bool CanRead => true;
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
        public override int Read(byte[] buffer, int offset, int count)
        {
            var length = Math.Min(Math.Min(count, _maxChunkSize), _bytes.Length - _position);
            Array.Copy(_bytes, _position, buffer, offset, length);
            _position += length;
            return length;
        }

        /// <inheritdoc />
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Read(buffer, offset, count));
        }

        /// <inheritdoc />
        public override void Flush() { }
        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        /// <inheritdoc />
        public override void SetLength(long value) => throw new NotSupportedException();
        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
