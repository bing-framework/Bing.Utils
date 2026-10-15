using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Bing.Http;
using Bing.Http.Clients;

namespace Bing.Utils.Http.Tests.Bing.Http;



/// <summary>
/// 验证 HTTP 二进制正文、下载进度和流式 JSON 可选能力。
/// </summary>
public class HttpTransferCapabilitiesTest
{
    /// <summary>
    /// 验证旧版请求未实现可选能力时会抛出异常。
    /// </summary>
    [Fact]
    public void TransferExtensions_ThrowForLegacyRequestsWithoutOptionalCapabilities()
    {
        var request = new Mock<IHttpRequest<string>>().Object;
        Should.Throw<NotSupportedException>(() => request.BinaryContent(new byte[] { 1 }));
        Should.Throw<NotSupportedException>(() => request.ContentFactory(() => new ByteArrayContent(new byte[0])));
        Should.Throw<NotSupportedException>(() => request.ObserveDownloadProgress(_ => { }));
        Should.Throw<NotSupportedException>(() => request.ObserveUploadProgress(_ => { }));
        Should.Throw<NotSupportedException>(() => request.GetJsonStreamAsync());
        Should.Throw<NotSupportedException>(() => request.GetJsonStreamResponseAsync());
    }



    /// <summary>
    /// 验证 PUT 请求发送防御性复制的原始字节。
    /// </summary>
    [Fact]
    public async Task BinaryContent_PutSendsDefensiveCopyAsRawBytes()
    {
        var source = new byte[] { 1, 2, 3, 4 };
        byte[] sent = null;
        string contentType = null;
        using var client = new HttpClient(new StubHandler(async (request, _) =>
        {
            request.Method.ShouldBe(HttpMethod.Put);
            sent = await request.Content.ReadAsByteArrayAsync();
            contentType = request.Content.Headers.ContentType.ToString();
            return Ok("ok");
        }));
        var request = new HttpRequest<string>(null, client, HttpMethod.Put, "https://example.test/raw")
            .BinaryContent(source, "application/octet-stream; profile=binary");
        source[0] = 99;
        (await request.GetResultAsync()).ShouldBe("ok");
        sent.ShouldBe(new byte[] { 1, 2, 3, 4 });
        contentType.ShouldBe("application/octet-stream; profile=binary");
    }



    /// <summary>
    /// 验证空字节数组仍发送非空的空正文内容。
    /// </summary>
    [Fact]
    public async Task BinaryContent_EmptyByteArraySendsNonNullEmptyContent()
    {
        using var client = new HttpClient(new StubHandler(async (request, _) =>
        {
            request.Content.ShouldNotBeNull();
            (await request.Content.ReadAsByteArrayAsync()).ShouldBeEmpty();
            request.Content.Headers.ContentLength.ShouldBe(0L);
            return Ok("ok");
        }));

        var result = await new HttpRequest<string>(null, client, HttpMethod.Put, "https://example.test/empty")
            .BinaryContent(Array.Empty<byte>()).GetResultAsync();

        result.ShouldBe("ok");
    }



    /// <summary>
    /// 验证无效参数会抛出异常并保留先前配置。
    /// </summary>
    [Fact]
    public async Task TransferRegistration_InvalidArgumentsThrowAndPreservePreviousConfiguration()
    {
        var body = new List<HttpDownloadProgress>();
        var sends = 0;
        using var client = new HttpClient(new StubHandler(async (request, _) =>
        {
            sends++;
            request.Content.Headers.ContentType.MediaType.ShouldBe("application/original");
            (await request.Content.ReadAsByteArrayAsync()).ShouldBe(new byte[] { 7 });
            return Ok("ok");
        }));
        var request = new HttpRequest<string>(null, client, HttpMethod.Put, "https://example.test/invalid-registration")
            .BinaryContent(new byte[] { 7 }, "application/original")
            .ObserveDownloadProgress(body.Add, minimumBytes: 1, minimumInterval: TimeSpan.Zero);

        Should.Throw<ArgumentNullException>(() => request.BinaryContent((byte[])null));
        Should.Throw<ArgumentNullException>(() => request.BinaryContent((Stream)null));
        Should.Throw<ArgumentNullException>(() => request.ContentFactory(null));
        Should.Throw<ArgumentNullException>(() => request.ObserveDownloadProgress(null));
        Should.Throw<ArgumentOutOfRangeException>(() => request.ObserveDownloadProgress(body.Add, minimumBytes: -1));
        Should.Throw<ArgumentOutOfRangeException>(() => request.ObserveDownloadProgress(body.Add, minimumInterval: TimeSpan.FromTicks(-1)));
        Should.Throw<FormatException>(() => request.BinaryContent(new byte[] { 9 }, "invalid media type"));
        sends.ShouldBe(0);

        using var target = new MemoryStream();
        await request.DownloadToWithResultAsync(target);
        sends.ShouldBe(1);
        body.Select(item => item.BytesWritten).ShouldBe(new long[] { 0, 2, 2 });
        body.Select(item => item.IsCompleted).ShouldBe(new[] { false, false, true });
    }



    /// <summary>
    /// 验证空请求会抛出参数异常。
    /// </summary>
    [Fact]
    public void TransferExtensions_NullRequestThrowsArgumentNullException()
    {
        IHttpRequest<string> request = null;
        Should.Throw<ArgumentNullException>(() => request.BinaryContent(Array.Empty<byte>()));
        Should.Throw<ArgumentNullException>(() => request.BinaryContent(new MemoryStream()));
        Should.Throw<ArgumentNullException>(() => request.ContentFactory(() => new ByteArrayContent(Array.Empty<byte>())));
        Should.Throw<ArgumentNullException>(() => request.ObserveDownloadProgress(_ => { }));
        Should.Throw<ArgumentNullException>(() => request.ObserveUploadProgress(_ => { }));
        Should.Throw<ArgumentNullException>(() => request.GetJsonStreamAsync());
    }



    /// <summary>
    /// 验证构造函数接受有效值并拒绝负长度。
    /// </summary>
    [Fact]
    public void HttpDownloadProgress_ConstructorAcceptsValidValuesAndRejectsNegativeLengths()
    {
        var progress = new HttpDownloadProgress(4, 8, false);
        progress.BytesWritten.ShouldBe(4);
        progress.TotalBytes.ShouldBe(8);
        progress.IsCompleted.ShouldBeFalse();

        Should.Throw<ArgumentOutOfRangeException>(() => new HttpDownloadProgress(-1, 8, false));
        Should.Throw<ArgumentOutOfRangeException>(() => new HttpDownloadProgress(0, -1, false));
    }



    /// <summary>
    /// 验证流重放从注册时的位置并遵循所有权。
    /// </summary>
    /// <param name="leaveOpen">测试输入值。</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BinaryContent_StreamReplaysFromRegistrationPositionAndHonorsOwnership(bool leaveOpen)
    {
        var source = new TrackingMemoryStream(new byte[] { 9, 8, 1, 2, 3, 4 });
        source.Position = 2;
        var bodies = new List<byte[]>();
        var sends = 0;
        using var client = new HttpClient(new StubHandler(async (request, _) =>
        {
            bodies.Add(await request.Content.ReadAsByteArrayAsync());
            sends++;
            return sends == 1 ? Status(HttpStatusCode.ServiceUnavailable) : Ok("ok");
        }));
        var request = new HttpRequest<string>(null, client, HttpMethod.Put, "https://example.test/upload")
            .BinaryContent(source, "application/custom", leaveOpen)
            .Retry(RetryPut());
        (await request.GetResultAsync()).ShouldBe("ok");
        sends.ShouldBe(2);
        bodies.Count.ShouldBe(2);
        bodies[0].ShouldBe(new byte[] { 1, 2, 3, 4 });
        bodies[1].ShouldBe(bodies[0]);
        source.IsDisposed.ShouldBe(!leaveOpen);
    }



    /// <summary>
    /// 验证不可定位流在实际发送前拒绝需要重试的请求。
    /// </summary>
    [Fact]
    public async Task BinaryContent_NonSeekableStreamRejectsRetryForActualWriteMethodBeforeSend()
    {
        var source = new NonSeekableStream(new byte[] { 1, 2, 3 });
        var sends = 0;
        using var client = new HttpClient(new StubHandler((_, _) =>
        {
            sends++;
            return Ok("ok");
        }));
        await Should.ThrowAsync<NotSupportedException>(() =>
            new HttpRequest<string>(null, client, HttpMethod.Put, "https://example.test/upload")
                .BinaryContent(source, leaveOpen: true)
                .Retry(RetryPut())
                .GetResultAsync());
        sends.ShouldBe(0);
        source.CanRead.ShouldBeTrue();
    }



    /// <summary>
    /// 验证不可定位流可以发送一次，但 PUT 请求不能重试。
    /// </summary>
    [Fact]
    public async Task BinaryContent_NonSeekableStreamCanSendOnceWhenPutRetriesAreNotAllowed()
    {
        var source = new NonSeekableStream(new byte[] { 3, 2, 1 });
        var sends = 0;
        using var client = new HttpClient(new StubHandler(async (request, _) =>
        {
            sends++;
            (await request.Content.ReadAsByteArrayAsync()).ShouldBe(new byte[] { 3, 2, 1 });
            return Ok("ok");
        }));

        var result = await new HttpRequest<string>(null, client, HttpMethod.Put, "https://example.test/no-put-retry")
            .BinaryContent(source)
            .Retry(new HttpRetryOptions { MaxRetries = 2, BaseDelay = TimeSpan.Zero, MaxDelay = TimeSpan.Zero, UseJitter = false })
            .GetResultAsync();

        result.ShouldBe("ok");
        sends.ShouldBe(1);
        source.CanRead.ShouldBeFalse();
    }



    /// <summary>
    /// 验证调用方拥有的流是否释放之后发送准备或取消失败。
    /// </summary>
    [Fact]
    public async Task BinaryContent_OwnedStreamIsReleasedAfterSendPreparationOrCancellationFailure()
    {
        var sendStream = new TrackingMemoryStream(new byte[] { 1 });
        using (var sendClient = new HttpClient(new StubHandler((_, _) =>
            Task.FromException<HttpResponseMessage>(new HttpRequestException("send failed")))))
        {
            await Should.ThrowAsync<HttpRequestException>(() =>
                new HttpRequest<string>(null, sendClient, HttpMethod.Put, "https://example.test/send-failure")
                    .BinaryContent(sendStream).GetResultAsync());
        }
        sendStream.IsDisposed.ShouldBeTrue();

        var prepareStream = new TrackingMemoryStream(new byte[] { 2 });
        var prepareError = new InvalidOperationException("prepare failed");
        using (var prepareClient = new HttpClient(new StubHandler((_, _) => Ok("unused"))))
        {
            var error = await Should.ThrowAsync<InvalidOperationException>(() =>
                new HttpRequest<string>(null, prepareClient, HttpMethod.Put, "https://example.test/prepare-failure")
                    .BinaryContent(prepareStream).OnSendBefore(_ => throw prepareError).GetResultAsync());
            error.ShouldBeSameAs(prepareError);
        }
        prepareStream.IsDisposed.ShouldBeTrue();

        var cancelledStream = new TrackingMemoryStream(new byte[] { 3 });
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        using var cancelClient = new HttpClient(new StubHandler((_, _) => Ok("unused")));
        await Should.ThrowAsync<OperationCanceledException>(() =>
            new HttpRequest<string>(null, cancelClient, HttpMethod.Put, "https://example.test/cancelled")
                .BinaryContent(cancelledStream).GetResultAsync(cancellation.Token));
        cancelledStream.IsDisposed.ShouldBeTrue();
    }



    /// <summary>
    /// 验证替换或拒绝请求时释放由调用方拥有的流。
    /// </summary>
    [Fact]
    public async Task BinaryContent_ReplacementAndRequestRejectionReleaseOwnedStreams()
    {
        var replaced = new TrackingMemoryStream(new byte[] { 1 });
        var current = new TrackingMemoryStream(new byte[] { 2 });
        using var client = new HttpClient(new StubHandler((_, _) => Ok("ok")));
        var request = new HttpRequest<string>(null, client, HttpMethod.Put, "https://example.test/replaced");
        request.BinaryContent(replaced);
        request.BinaryContent(current);
        replaced.IsDisposed.ShouldBeTrue();
        (await request.GetResultAsync()).ShouldBe("ok");
        current.IsDisposed.ShouldBeTrue();

        var rejectedStream = new TrackingMemoryStream(new byte[] { 3 });
        var rejected = new HttpRequest<string>(null, client, HttpMethod.Put, "https://example.test/rejected")
            .BinaryContent(rejectedStream).OnSendBefore(_ => false);
        (await rejected.GetResultAsync()).ShouldBeNull();
        rejectedStream.IsDisposed.ShouldBeTrue();
    }



    /// <summary>
    /// 验证每次重试都会创建并释放新的请求正文。
    /// </summary>
    [Fact]
    public async Task ContentFactory_CreatesAndDisposesFreshContentForEachRetry()
    {
        var contents = new List<TrackingContent>();
        var bodies = new List<byte[]>();
        var sends = 0;
        using var client = new HttpClient(new StubHandler(async (request, _) =>
        {
            bodies.Add(await request.Content.ReadAsByteArrayAsync());
            sends++;
            return sends == 1 ? Status(HttpStatusCode.ServiceUnavailable) : Ok("ok");
        }));
        var request = new HttpRequest<string>(null, client, HttpMethod.Put, "https://example.test/factory")
            .ContentFactory(() =>
            {
                var content = new TrackingContent(new byte[] { 5, 6, 7 });
                contents.Add(content);
                return content;
            })
            .Retry(RetryPut());
        (await request.GetResultAsync()).ShouldBe("ok");
        sends.ShouldBe(2);
        contents.Count.ShouldBe(2);
        ReferenceEquals(contents[0], contents[1]).ShouldBeFalse();
        contents.All(content => content.IsDisposed).ShouldBeTrue();
        bodies[0].ShouldBe(new byte[] { 5, 6, 7 });
        bodies[1].ShouldBe(bodies[0]);
    }



    /// <summary>
    /// 验证拒绝空并重复实例之前其次发送。
    /// </summary>
    [Fact]
    public async Task ContentFactory_RejectsNullAndRepeatedInstancesBeforeSecondSend()
    {
        using var nullClient = new HttpClient(new StubHandler((_, _) => Ok("unused")));
        await Should.ThrowAsync<InvalidOperationException>(() =>
            new HttpRequest<string>(null, nullClient, HttpMethod.Get, "https://example.test/null")
                .ContentFactory(() => null).GetResultAsync());
        var sends = 0;
        var sharedContent = new TrackingContent(new byte[] { 1 });
        using var retryClient = new HttpClient(new StubHandler((_, _) =>
        {
            sends++;
            return Status(HttpStatusCode.ServiceUnavailable);
        }));
        await Should.ThrowAsync<InvalidOperationException>(() =>
            new HttpRequest<string>(null, retryClient, HttpMethod.Put, "https://example.test/reused")
                .ContentFactory(() => sharedContent).Retry(RetryPut()).GetResultAsync());
        sends.ShouldBe(1);
        sharedContent.IsDisposed.ShouldBeTrue();
    }



    /// <summary>
    /// 验证失败On重试释放之前正文并保留工厂异常。
    /// </summary>
    [Fact]
    public async Task ContentFactory_FailureOnRetryDisposesPreviousContentAndPreservesFactoryError()
    {
        var content = new TrackingContent(new byte[] { 1 });
        var sends = 0;
        var factoryCalls = 0;
        var expected = new ApplicationException("content factory failed");
        using var client = new HttpClient(new StubHandler((_, _) =>
        {
            sends++;
            return Status(HttpStatusCode.ServiceUnavailable);
        }));
        var error = await Should.ThrowAsync<ApplicationException>(() =>
            new HttpRequest<string>(null, client, HttpMethod.Put, "https://example.test/factory-fails")
                .ContentFactory(() => ++factoryCalls == 1 ? content : throw expected)
                .Retry(RetryPut()).GetResultAsync());
        error.ShouldBeSameAs(expected);
        factoryCalls.ShouldBe(2);
        sends.ShouldBe(1);
        content.IsDisposed.ShouldBeTrue();
    }



    /// <summary>
    /// 验证拒绝混用结构化正文和原始正文，并允许发送后的回调。
    /// </summary>
    [Fact]
    public async Task BinaryContent_RejectsStructuredBodyMixAndAllowsOnSendAfter()
    {
        using var client = new HttpClient(new StubHandler((_, _) => Ok("unused")));
        await Should.ThrowAsync<NotSupportedException>(() =>
            new HttpRequest<string>(null, client, HttpMethod.Post, "https://example.test/mix")
                .Content(new { Value = 1 }).BinaryContent(new byte[] { 1 }).GetResultAsync());
        (await new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/after")
            .BinaryContent(new byte[] { 1 })
            .OnSendAfter((_, _) => Task.FromResult("custom")).GetResultAsync()).ShouldBe("custom");
    }



    /// <summary>
    /// 验证已知长度报告快照使用总数字节。
    /// </summary>
    [Fact]
    public async Task UploadProgress_KnownLengthReportsSnapshotsWithTotalBytes()
    {
        var payload = Enumerable.Range(0, 12).Select(value => (byte)value).ToArray();
        var progress = new List<HttpUploadProgress>();
        byte[] sent = null;
        using var client = new HttpClient(new StubHandler(async (request, _) =>
        {
            sent = await request.Content.ReadAsByteArrayAsync();
            return Ok("ok");
        }));

        var result = await new HttpRequest<string>(null, client, HttpMethod.Put, "https://example.test/upload-progress")
            .BinaryContent(payload)
            .ObserveUploadProgress(progress.Add, minimumBytes: 2, minimumInterval: TimeSpan.Zero)
            .GetResultAsync();

        result.ShouldBe("ok");
        sent.ShouldBe(payload);
        progress.First().Attempt.ShouldBe(1);
        progress.First().BytesWritten.ShouldBe(0);
        progress.First().TotalBytes.ShouldBe((long?)payload.Length);
        progress.First().IsCompleted.ShouldBeFalse();
        progress.Last().Attempt.ShouldBe(1);
        progress.Last().BytesWritten.ShouldBe(payload.Length);
        progress.Last().TotalBytes.ShouldBe((long?)payload.Length);
        progress.Last().IsCompleted.ShouldBeTrue();
        progress.Any(item => !item.IsCompleted && item.BytesWritten > 0).ShouldBeTrue();
        progress.All(item => item.TotalBytes == payload.Length).ShouldBeTrue();
    }



    /// <summary>
    /// 验证重试开始新的尝试并报告每次完成。
    /// </summary>
    [Fact]
    public async Task UploadProgress_RetryStartsFreshAttemptAndReportsEachCompletion()
    {
        var payload = new byte[] { 1, 2, 3, 4 };
        var progress = new List<HttpUploadProgress>();
        var sends = 0;
        using var client = new HttpClient(new StubHandler(async (request, _) =>
        {
            (await request.Content.ReadAsByteArrayAsync()).ShouldBe(payload);
            sends++;
            return sends == 1 ? Status(HttpStatusCode.ServiceUnavailable) : Ok("ok");
        }));

        var result = await new HttpRequest<string>(null, client, HttpMethod.Put, "https://example.test/upload-retry")
            .BinaryContent(payload)
            .ObserveUploadProgress(progress.Add, minimumBytes: long.MaxValue, minimumInterval: TimeSpan.FromHours(1))
            .Retry(RetryPut())
            .GetResultAsync();

        result.ShouldBe("ok");
        sends.ShouldBe(2);
        progress.Select(item => (item.Attempt, item.BytesWritten, item.TotalBytes, item.IsCompleted)).ShouldBe(new[]
        {
            (1, 0L, (long?)payload.Length, false),
            (1, (long)payload.Length, (long?)payload.Length, true),
            (2, 0L, (long?)payload.Length, false),
            (2, (long)payload.Length, (long?)payload.Length, true)
        });
    }



    /// <summary>
    /// 验证无效参数会抛出异常，且旧版请求会拒绝该能力。
    /// </summary>
    [Fact]
    public void UploadProgress_InvalidArgumentsThrowAndLegacyRequestsRejectCapability()
    {
        var legacy = new Mock<IHttpRequest<string>>().Object;
        Should.Throw<NotSupportedException>(() => legacy.ObserveUploadProgress(_ => { }));

        using var client = new HttpClient(new StubHandler((_, _) => Ok("ok")));
        var request = new HttpRequest<string>(null, client, HttpMethod.Put, "https://example.test/upload-validation");
        Should.Throw<ArgumentNullException>(() => request.ObserveUploadProgress(null));
        Should.Throw<ArgumentOutOfRangeException>(() => request.ObserveUploadProgress(_ => { }, minimumBytes: 0));
        Should.Throw<ArgumentOutOfRangeException>(() => request.ObserveUploadProgress(_ => { }, minimumInterval: TimeSpan.FromTicks(-1)));
    }



    /// <summary>
    /// 验证不可定位流的长度未知时不报告总字节数。
    /// </summary>
    [Fact]
    public async Task UploadProgress_UnknownLengthReportsNullTotalForNonSeekableStream()
    {
        var payload = new byte[] { 9, 8, 7 };
        var progress = new List<HttpUploadProgress>();
        using var source = new NonSeekableStream(payload);
        using var client = new HttpClient(new StubHandler(async (request, _) =>
        {
            (await request.Content.ReadAsByteArrayAsync()).ShouldBe(payload);
            return Ok("ok");
        }));

        var result = await new HttpRequest<string>(null, client, HttpMethod.Put, "https://example.test/upload-unknown")
            .BinaryContent(source, leaveOpen: true)
            .ObserveUploadProgress(progress.Add, minimumBytes: long.MaxValue, minimumInterval: TimeSpan.FromHours(1))
            .GetResultAsync();

        result.ShouldBe("ok");
        progress.Count.ShouldBe(2);
        progress[0].TotalBytes.ShouldBeNull();
        progress[0].BytesWritten.ShouldBe(0);
        progress[1].TotalBytes.ShouldBeNull();
        progress[1].BytesWritten.ShouldBe(payload.Length);
        progress[1].IsCompleted.ShouldBeTrue();
    }



    /// <summary>
    /// 验证正文工厂会包装并释放每次重试的正文。
    /// </summary>
    [Fact]
    public async Task UploadProgress_ContentFactoryWrapsAndDisposesEachRetryContent()
    {
        var payload = new byte[] { 5, 6, 7 };
        var progress = new List<HttpUploadProgress>();
        var contents = new List<TrackingContent>();
        var sends = 0;
        using var client = new HttpClient(new StubHandler(async (request, _) =>
        {
            (await request.Content.ReadAsByteArrayAsync()).ShouldBe(payload);
            sends++;
            return sends == 1 ? Status(HttpStatusCode.ServiceUnavailable) : Ok("ok");
        }));

        var result = await new HttpRequest<string>(null, client, HttpMethod.Put, "https://example.test/upload-factory")
            .ContentFactory(() =>
            {
                var content = new TrackingContent(payload);
                contents.Add(content);
                return content;
            })
            .ObserveUploadProgress(progress.Add, minimumBytes: long.MaxValue, minimumInterval: TimeSpan.FromHours(1))
            .Retry(RetryPut())
            .GetResultAsync();

        result.ShouldBe("ok");
        sends.ShouldBe(2);
        contents.Count.ShouldBe(2);
        contents.All(content => content.IsDisposed).ShouldBeTrue();
        progress.Count.ShouldBe(4);
        progress.Where(item => item.IsCompleted).Select(item => item.Attempt).ShouldBe(new[] { 1, 2 });
    }



    /// <summary>
    /// 验证观察器失败传播并是否不报告完成。
    /// </summary>
    [Fact]
    public async Task UploadProgress_ObserverFailurePropagatesAndDoesNotReportCompletion()
    {
        var expected = new InvalidOperationException("upload progress failed");
        var progressCalls = 0;
        using var client = new HttpClient(new StubHandler(async (request, _) =>
        {
            await request.Content.ReadAsByteArrayAsync();
            return Ok("ok");
        }));

        var error = await Should.ThrowAsync<InvalidOperationException>(() =>
            new HttpRequest<string>(null, client, HttpMethod.Put, "https://example.test/upload-progress-error")
                .BinaryContent(new byte[] { 1, 2, 3 })
                .ObserveUploadProgress(item =>
                {
                    progressCalls++;
                    throw expected;
                }, minimumBytes: 1, minimumInterval: TimeSpan.Zero)
                .GetResultAsync());

        error.ShouldBeSameAs(expected);
        progressCalls.ShouldBe(1);
    }



    /// <summary>
    /// 验证流遵循所有权。
    /// </summary>
    /// <param name="leaveOpen">测试输入值。</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UploadProgress_StreamHonorsOwnership(bool leaveOpen)
    {
        var payload = Enumerable.Range(0, 17).Select(value => (byte)value).ToArray();
        var source = new TrackingMemoryStream(payload);
        var progress = new List<HttpUploadProgress>();
        byte[] sent = null;
        using var client = new HttpClient(new StubHandler(async (request, _) =>
        {
            sent = await request.Content.ReadAsByteArrayAsync();
            return Ok("ok");
        }));

        var result = await new HttpRequest<string>(null, client, HttpMethod.Put, "https://example.test/upload-stream")
            .BinaryContent(source, leaveOpen: leaveOpen)
            .ObserveUploadProgress(progress.Add, minimumBytes: 1, minimumInterval: TimeSpan.Zero)
            .GetResultAsync();

        result.ShouldBe("ok");
        sent.ShouldBe(payload);
        progress.Last().BytesWritten.ShouldBe(payload.Length);
        progress.Last().TotalBytes.ShouldBe((long?)payload.Length);
        progress.Last().IsCompleted.ShouldBeTrue();
        source.IsDisposed.ShouldBe(!leaveOpen);
    }



    /// <summary>
    /// 验证不可定位流报告未知长度。
    /// </summary>
    [Fact]
    public async Task UploadProgress_NonSeekableStreamReportsUnknownLength()
    {
        var payload = Enumerable.Range(0, 9).Select(value => (byte)value).ToArray();
        var source = new NonSeekableStream(payload);
        var progress = new List<HttpUploadProgress>();
        using var client = new HttpClient(new StubHandler(async (request, _) =>
        {
            (await request.Content.ReadAsByteArrayAsync()).ShouldBe(payload);
            return Ok("ok");
        }));

        await new HttpRequest<string>(null, client, HttpMethod.Put, "https://example.test/upload-unknown")
            .BinaryContent(source, leaveOpen: true)
            .ObserveUploadProgress(progress.Add, minimumBytes: 1, minimumInterval: TimeSpan.Zero)
            .GetResultAsync();

        progress.First().TotalBytes.ShouldBeNull();
        progress.Last().TotalBytes.ShouldBeNull();
        progress.Last().BytesWritten.ShouldBe(payload.Length);
        progress.Last().IsCompleted.ShouldBeTrue();
        source.CanRead.ShouldBeTrue();
        source.Dispose();
    }



    /// <summary>
    /// 验证正文工厂创建独立尝试进度。
    /// </summary>
    [Fact]
    public async Task UploadProgress_ContentFactoryCreatesIndependentAttemptProgress()
    {
        var payload = new byte[] { 3, 1, 4, 1, 5 };
        var created = new List<TrackingContent>();
        var progress = new List<HttpUploadProgress>();
        var sends = 0;
        using var client = new HttpClient(new StubHandler(async (request, _) =>
        {
            (await request.Content.ReadAsByteArrayAsync()).ShouldBe(payload);
            sends++;
            return sends == 1 ? Status(HttpStatusCode.ServiceUnavailable) : Ok("ok");
        }));

        var result = await new HttpRequest<string>(null, client, HttpMethod.Put, "https://example.test/upload-factory")
            .ContentFactory(() =>
            {
                var content = new TrackingContent(payload);
                created.Add(content);
                return content;
            })
            .ObserveUploadProgress(progress.Add, minimumBytes: long.MaxValue, minimumInterval: TimeSpan.FromHours(1))
            .Retry(RetryPut())
            .GetResultAsync();

        result.ShouldBe("ok");
        created.Count.ShouldBe(2);
        created.All(content => content.IsDisposed).ShouldBeTrue();
        progress.Select(item => (item.Attempt, item.BytesWritten, item.IsCompleted)).ShouldBe(new[]
        {
            (1, 0L, false), (1, (long)payload.Length, true),
            (2, 0L, false), (2, (long)payload.Length, true)
        });
    }



    /// <summary>
    /// 验证多部分内容会包装完成正文并保留响应头。
    /// </summary>
    [Fact]
    public async Task UploadProgress_MultipartWrapsCompleteContentAndPreservesHeaders()
    {
        var source = new MemoryStream(new byte[] { 8, 6, 7, 5, 3, 0, 9 });
        var progress = new List<HttpUploadProgress>();
        long? contentLength = null;
        string mediaType = null;
        long bodyLength = 0;
        using var client = new HttpClient(new StubHandler(async (request, _) =>
        {
            mediaType = request.Content.Headers.ContentType?.MediaType;
            contentLength = request.Content.Headers.ContentLength;
            bodyLength = (await request.Content.ReadAsByteArrayAsync()).LongLength;
            return Ok("ok");
        }));

        var result = await new HttpRequest<string>(null, client, HttpMethod.Post, "https://example.test/upload-multipart")
            .FileContent(source, "payload.bin", "file", leaveOpen: true)
            .ObserveUploadProgress(progress.Add, minimumBytes: long.MaxValue, minimumInterval: TimeSpan.FromHours(1))
            .GetResultAsync();

        result.ShouldBe("ok");
        mediaType.ShouldBe("multipart/form-data");
        bodyLength.ShouldBeGreaterThan(0);
        progress.Last().BytesWritten.ShouldBe(bodyLength);
        progress.Last().TotalBytes.ShouldBe(contentLength);
        progress.Last().IsCompleted.ShouldBeTrue();
        source.CanRead.ShouldBeTrue();
        source.Dispose();
    }

#if NET6_0_OR_GREATER
    /// <summary>
    /// 验证取消是否不报告完成。
    /// </summary>
    [Fact]
    public async Task UploadProgress_CancellationDoesNotReportCompletion()
    {
        var cancellation = new CancellationTokenSource();
        var progress = new List<HttpUploadProgress>();
        using var client = new HttpClient(new StubHandler(async (request, token) =>
        {
            await request.Content.CopyToAsync(Stream.Null, null, token);
            return Ok("should not complete");
        }));

        var execution = new HttpRequest<string>(null, client, HttpMethod.Put, "https://example.test/upload-cancel")
            .BinaryContent(new byte[] { 1, 2, 3, 4 })
            .ObserveUploadProgress(item =>
            {
                progress.Add(item);
                if (!item.IsCompleted)
                    cancellation.Cancel();
            }, minimumBytes: 1, minimumInterval: TimeSpan.Zero)
            .GetResultAsync(cancellation.Token);

        await Should.ThrowAsync<OperationCanceledException>(() => execution);
        progress.Any(item => item.IsCompleted).ShouldBeFalse();
        cancellation.Dispose();
    }
#endif
    /// <summary>
    /// 验证已知和未知长度均报告初始进度及正文结束快照。
    /// </summary>
    [Fact]
    public async Task DownloadProgress_KnownAndUnknownLengthsReportInitialAndEofSnapshots()
    {
        var bytes = new byte[] { 1, 2, 3, 4, 5 };
        var known = new List<HttpDownloadProgress>();
        using var knownClient = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) }));
        using var knownTarget = new MemoryStream();
        await new HttpRequest<string>(null, knownClient, HttpMethod.Get, "https://example.test/known")
            .ObserveDownloadProgress(known.Add, long.MaxValue, TimeSpan.FromHours(1))
            .DownloadToWithResultAsync(knownTarget);
        known.Select(item => (item.BytesWritten, item.TotalBytes, item.IsCompleted)).ShouldBe(new[]
        {
            (0L, (long?)bytes.Length, false), ((long)bytes.Length, (long?)bytes.Length, true)
        });
        knownTarget.ToArray().ShouldBe(bytes);

        var unknown = new List<HttpDownloadProgress>();
        using var unknownClient = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new NonSeekableStream(bytes)) }));
        using var unknownTarget = new MemoryStream();
        await new HttpRequest<string>(null, unknownClient, HttpMethod.Get, "https://example.test/unknown")
            .ObserveDownloadProgress(unknown.Add, long.MaxValue, TimeSpan.FromHours(1))
            .DownloadToWithResultAsync(unknownTarget);
        unknown.Select(item => (item.BytesWritten, item.TotalBytes, item.IsCompleted)).ShouldBe(new[]
        {
            (0L, (long?)null, false), ((long)bytes.Length, (long?)null, true)
        });
    }



    /// <summary>
    /// 验证中间进度通知必须同时满足两个阈值。
    /// </summary>
    [Fact]
    public async Task DownloadProgress_IntermediateNotificationsRequireBothThresholds()
    {
        var progress = new List<HttpDownloadProgress>();
        using var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StreamContent(new ChunkedMemoryStream(Enumerable.Range(0, 12).Select(x => (byte)x).ToArray(), 2)) }));
        using var destination = new MemoryStream();
        await new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/thresholds")
            .ObserveDownloadProgress(progress.Add, minimumBytes: 2, minimumInterval: TimeSpan.FromHours(1))
            .DownloadToWithResultAsync(destination);
        progress.First().IsCompleted.ShouldBeFalse();
        progress.First().BytesWritten.ShouldBe(0);
        progress.Last().IsCompleted.ShouldBeTrue();
        progress.Last().BytesWritten.ShouldBe(12);
        progress.Count.ShouldBe(2);
    }



    /// <summary>
    /// 验证零间隔报告字节阈值并注册快照变化下一次执行。
    /// </summary>
    [Fact]
    public async Task DownloadProgress_ZeroIntervalReportsByteThresholdAndRegistrationSnapshotChangesNextRun()
    {
        var original = new List<HttpDownloadProgress>();
        var replacement = new List<HttpDownloadProgress>();
        HttpRequest<string> request = null;
        using var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StreamContent(new ChunkedMemoryStream(Enumerable.Range(0, 12).Select(x => (byte)x).ToArray(), 2)) }));
        request = new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/snapshot")
            .ObserveDownloadProgress(item =>
            {
                original.Add(item);
                if (original.Count == 1)
                    request.ObserveDownloadProgress(replacement.Add, minimumBytes: 2, minimumInterval: TimeSpan.Zero);
            }, minimumBytes: 2, minimumInterval: TimeSpan.Zero) as HttpRequest<string>;

        using (var firstTarget = new MemoryStream())
            await request.DownloadToWithResultAsync(firstTarget);
        original.Any(item => !item.IsCompleted && item.BytesWritten > 0).ShouldBeTrue();
        original.Last().IsCompleted.ShouldBeTrue();
        replacement.ShouldBeEmpty();

        using var secondTarget = new MemoryStream();
        await request.DownloadToWithResultAsync(secondTarget);
        replacement.Count.ShouldBeGreaterThan(1);
        replacement.First().IsCompleted.ShouldBeFalse();
        replacement.Last().IsCompleted.ShouldBeTrue();
    }



    /// <summary>
    /// 验证空成功响应报告零字节正文末尾。
    /// </summary>
    /// <param name="status">测试输入值。</param>
    /// <param name="method">测试输入值。</param>
    [Theory]
    [InlineData(HttpStatusCode.NoContent, "GET")]
    [InlineData(HttpStatusCode.OK, "HEAD")]
    public async Task DownloadProgress_EmptySuccessfulResponsesReportZeroByteEof(HttpStatusCode status, string method)
    {
        var progress = new List<HttpDownloadProgress>();
        using var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(status) { Content = new ByteArrayContent(Array.Empty<byte>()) }));
        using var destination = new MemoryStream();
        await new HttpRequest<string>(null, client, new HttpMethod(method), "https://example.test/empty")
            .ObserveDownloadProgress(progress.Add).DownloadToWithResultAsync(destination);
        progress.Count.ShouldBe(2);
        progress.All(item => item.BytesWritten == 0 && item.TotalBytes == 0).ShouldBeTrue();
        progress[0].IsCompleted.ShouldBeFalse();
        progress[1].IsCompleted.ShouldBeTrue();
    }



    /// <summary>
    /// 验证 HTTP 失败或观察器异常时不报告成功结束。
    /// </summary>
    [Fact]
    public async Task DownloadProgress_HttpFailureAndObserverErrorDoNotReportSuccessfulEof()
    {
        var failedProgress = new List<HttpDownloadProgress>();
        using var failedClient = new HttpClient(new StubHandler((_, _) => Status(HttpStatusCode.BadGateway)));
        using var failedTarget = new MemoryStream();
        await new HttpRequest<string>(null, failedClient, HttpMethod.Get, "https://example.test/fail")
            .ObserveDownloadProgress(failedProgress.Add).DownloadToWithResultAsync(failedTarget);
        failedProgress.ShouldBeEmpty();

        var progressError = new InvalidOperationException("progress failed");
        var trackingContent = new TrackingContent(new byte[] { 1, 2, 3 });
        using var errorClient = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = trackingContent }));
        using var errorTarget = new MemoryStream();
        var error = await Should.ThrowAsync<InvalidOperationException>(() =>
            new HttpRequest<string>(null, errorClient, HttpMethod.Get, "https://example.test/progress-error")
                .ObserveDownloadProgress(_ => throw progressError).DownloadToWithResultAsync(errorTarget));
        error.ShouldBeSameAs(progressError);
        trackingContent.IsDisposed.ShouldBeTrue();
    }



    /// <summary>
    /// 验证取消是否不报告正文读取结束。
    /// </summary>
    [Fact]
    public async Task DownloadProgress_CancellationDoesNotReportCompletedEof()
    {
        var body = new BlockingReadStream();
        var progress = new List<HttpDownloadProgress>();
        using var cancellation = new CancellationTokenSource();
        using var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(body) }));
        using var destination = new MemoryStream();
        var execution = new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/cancel-progress")
            .ObserveDownloadProgress(progress.Add, long.MaxValue, TimeSpan.Zero)
            .DownloadToWithResultAsync(destination, cancellation.Token);
        await body.ReadStarted;
        cancellation.Cancel();
        await Should.ThrowAsync<OperationCanceledException>(() => execution);

        progress.Count.ShouldBe(1);
        progress[0].IsCompleted.ShouldBeFalse();
        body.IsDisposed.ShouldBeTrue();
    }



    /// <summary>
    /// 验证使用已配置选项和字节顺序标记解析 JSON 后缀。
    /// </summary>
    [Fact]
    public async Task GetJsonStreamAsync_ParsesJsonSuffixWithConfiguredOptionsAndBom()
    {
        var json = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes("{\"state\":\"ready\"}")).ToArray();
        using var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent(json, "application/problem+json") }));
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new JsonStringEnumConverter());
        var result = await new HttpRequest<TransferDto>(null, client, HttpMethod.Get, "https://example.test/json")
            .JsonSerializerOptions(options).GetJsonStreamAsync();
        result.State.ShouldBe(TransferState.Ready);
    }



    /// <summary>
    /// 验证 UTF-8 字节顺序标记跨单字节读取时仍可正确解析。
    /// </summary>
    [Fact]
    public async Task GetJsonStreamAsync_ParsesUtf8BomSplitAcrossSingleByteReads()
    {
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes("{\"Name\":\"fragmented\"}")).ToArray();
        var body = new ChunkedMemoryStream(bytes, 1);
        var content = new StreamContent(body);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        using var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = content }));

        var result = await new HttpRequest<TransferDto>(null, client, HttpMethod.Get, "https://example.test/fragmented-bom")
            .GetJsonStreamAsync();

        result.Name.ShouldBe("fragmented");
        body.CanRead.ShouldBeFalse();
    }



    /// <summary>
    /// 验证 Unicode 空白字符跨读取边界时仍能识别空前缀。
    /// </summary>
    /// <param name="chunkSize">测试输入值。</param>
    [Theory]
    [InlineData(1)]
    [InlineData(4096)]
    public async Task GetJsonStreamAsync_UnicodeWhitespaceAcrossReadAndPrefixBoundariesReturnsNull(int chunkSize)
    {
        var whitespace = new string(' ', 4095) + string.Concat(Enumerable.Repeat("\u2003", 1500)) + "\r\n";
        var body = new ChunkedMemoryStream(Encoding.UTF8.GetBytes(whitespace), chunkSize);
        var content = new StreamContent(body);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        var successCalls = 0;
        using var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = content }));

        var result = await new HttpRequest<TransferDto>(null, client, HttpMethod.Get, "https://example.test/fragmented-whitespace")
            .OnSuccess(_ => successCalls++).GetJsonStreamAsync();

        result.ShouldBeNull();
        successCalls.ShouldBe(1);
        body.CanRead.ShouldBeFalse();
    }



    /// <summary>
    /// 验证已知并未知长度接受准确上限并拒绝一个字节更少。
    /// </summary>
    /// <param name="knownLength">测试输入值。</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetJsonStreamAsync_KnownAndUnknownLengthsAcceptExactLimitAndRejectOneByteLess(bool knownLength)
    {
        var name = new string('a', 8000);
        var bytes = Encoding.UTF8.GetBytes("{\"Name\":\"" + name + "\"}");
        var acceptedContent = new TrackingContent(bytes, knownLength);
        acceptedContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        using (var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = acceptedContent })))
        {
            var result = await new HttpRequest<TransferDto>(null, client, HttpMethod.Get, "https://example.test/exact-limit")
                .MaxResponseBytes(bytes.Length).GetJsonStreamAsync();
            result.Name.ShouldBe(name);
        }
        acceptedContent.IsDisposed.ShouldBeTrue();

        var rejectedContent = new TrackingContent(bytes, knownLength);
        rejectedContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        using (var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = rejectedContent })))
        {
            await Should.ThrowAsync<HttpResponseSizeLimitException>(() =>
                new HttpRequest<TransferDto>(null, client, HttpMethod.Get, "https://example.test/under-limit")
                    .MaxResponseBytes(bytes.Length - 1).GetJsonStreamAsync());
        }
        rejectedContent.IsDisposed.ShouldBeTrue();
    }



    /// <summary>
    /// 验证空字符串响应保留旧格式空字符串。
    /// </summary>
    [Fact]
    public async Task GetJsonStreamAsync_EmptyStringResponseKeepsLegacyEmptyString()
    {
        using var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent(Array.Empty<byte>(), "application/json") }));

        var result = await new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/empty-string")
            .GetJsonStreamAsync();

        result.ShouldBe(string.Empty);
    }



    /// <summary>
    /// 验证空响应调用自定义转换器一次。
    /// </summary>
    [Fact]
    public async Task GetJsonStreamAsync_EmptyResponseInvokesCustomConverterOnce()
    {
        var expected = new TransferDto { Name = "custom-empty" };
        var conversionCalls = 0;
        using var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent(Array.Empty<byte>(), "application/json") }));

        var result = await new HttpRequest<TransferDto>(null, client, HttpMethod.Get, "https://example.test/empty-converter")
            .OnConvert(content =>
            {
                conversionCalls++;
                content.ShouldBe(string.Empty);
                return expected;
            }).GetJsonStreamAsync();

        result.ShouldBeSameAs(expected);
        conversionCalls.ShouldBe(1);
    }



    /// <summary>
    /// 验证空白正文、HEAD 请求或无正文响应返回空并执行成功回调。
    /// </summary>
    [Fact]
    public async Task GetJsonStreamAsync_WhitespaceAndHeadOrNoContentReturnNullAndRunSuccessCallback()
    {
        var successCalls = 0;
        using var blankClient = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent(" \r\n ", Encoding.UTF8, "application/json") }));
        var blank = await new HttpRequest<TransferDto>(null, blankClient, HttpMethod.Get, "https://example.test/blank")
            .OnSuccess(_ => successCalls++).GetJsonStreamAsync();
        blank.ShouldBeNull();
        successCalls.ShouldBe(1);
        foreach (var response in new[] { (HttpMethod.Head, HttpStatusCode.OK), (HttpMethod.Get, HttpStatusCode.NoContent) })
        {
            using var client = new HttpClient(new StubHandler((_, _) =>
                new HttpResponseMessage(response.Item2) { Content = new ByteArrayContent(Array.Empty<byte>()) }));
            var value = await new HttpRequest<TransferDto>(null, client, response.Item1, "https://example.test/empty")
                .OnSuccess(_ => successCalls++).GetJsonStreamAsync();
            value.ShouldBeNull();
        }
        successCalls.ShouldBe(3);
    }



    /// <summary>
    /// 验证空白控制字符仍保持空。
    /// </summary>
    [Fact]
    public async Task GetJsonStreamAsync_WhitespaceControlCharactersRemainNull()
    {
        using var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent("\v\f", Encoding.UTF8, "application/json") }));

        var result = await new HttpRequest<TransferDto>(null, client, HttpMethod.Get, "https://example.test/control-blank")
            .GetJsonStreamAsync();

        result.ShouldBeNull();
    }



    /// <summary>
    /// 验证字符串响应使用旧格式回退。
    /// </summary>
    [Fact]
    public async Task GetJsonStreamAsync_StringResponseUsesLegacyFallback()
    {
        using var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent("plain response", Encoding.UTF8, "text/plain") }));

        var result = await new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/string-fallback")
            .GetJsonStreamAsync();

        result.ShouldBe("plain response");
    }



    /// <summary>
    /// 验证 Unicode 空白字符正文返回空内容。
    /// </summary>
    /// <param name="whitespace">测试输入值。</param>
    [Theory]
    [InlineData("\u00A0")]
    [InlineData(" \u2003\r\n")]
    public async Task GetJsonStreamAsync_UnicodeWhitespaceOnlyReturnsNull(string whitespace)
    {
        using var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent(whitespace, Encoding.UTF8, "application/json") }));

        var result = await new HttpRequest<TransferDto>(null, client, HttpMethod.Get, "https://example.test/unicode-blank")
            .GetJsonStreamAsync();

        result.ShouldBeNull();
    }



    /// <summary>
    /// 验证 JSON 前缀前出现 Unicode 空白字符时仍判定为无效。
    /// </summary>
    [Fact]
    public async Task GetJsonStreamAsync_UnicodeWhitespacePrefixBeforeJsonRemainsInvalid()
    {
        using var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent("\u00A0{}", Encoding.UTF8, "application/json") }));

        await Should.ThrowAsync<JsonException>(() =>
            new HttpRequest<TransferDto>(null, client, HttpMethod.Get, "https://example.test/unicode-prefix")
                .GetJsonStreamAsync());
    }



    /// <summary>
    /// 验证非 UTF-8 响应回退到原有转换流程并保留转换回调。
    /// </summary>
    [Fact]
    public async Task GetJsonStreamAsync_FallsBackForNonUtf8AndPreservesConversionCallbacks()
    {
        var latinJson = Encoding.GetEncoding("iso-8859-1").GetBytes("{\"name\":\"café\"}");
        using var latinClient = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK)
            { Content = JsonContent(latinJson, "application/json; charset=iso-8859-1") }));
        var latin = await new HttpRequest<TransferDto>(null, latinClient, HttpMethod.Get, "https://example.test/latin")
            .GetJsonStreamAsync();
        latin.Name.ShouldBe("café");

        var completeCount = 0;
        using var callbackClient = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent("{\"name\":\"raw\"}", Encoding.UTF8, "application/json") }));
        var converted = await new HttpRequest<TransferDto>(null, callbackClient, HttpMethod.Get, "https://example.test/convert")
            .OnConvert(_ => new TransferDto { Name = "converted" })
            .OnComplete((_, _) => completeCount++).GetJsonStreamAsync();
        converted.Name.ShouldBe("converted");
        completeCount.ShouldBe(1);
    }



    /// <summary>
    /// 验证限制未知长度上限并保留解析失败观察结果。
    /// </summary>
    [Fact]
    public async Task GetJsonStreamAsync_EnforcesUnknownLengthLimitAndPreservesParseFailureObservation()
    {
        var oversized = new TrackingContent(Encoding.UTF8.GetBytes("{\"name\":\"long\"}"), knownLength: false);
        oversized.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        using var limitClient = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = oversized }));
        var limitedCompletion = new List<HttpRequestCompletion>();
        await Should.ThrowAsync<HttpResponseSizeLimitException>(() =>
            new HttpRequest<TransferDto>(null, limitClient, HttpMethod.Get, "https://example.test/limit")
                .MaxResponseBytes(4).ObserveCompletion(limitedCompletion.Add).GetJsonStreamAsync());
        oversized.IsDisposed.ShouldBeTrue();
        limitedCompletion.Single().Outcome.ShouldBe(HttpRequestOutcome.Faulted);

        var parseContent = new TrackingContent(Encoding.UTF8.GetBytes("{"));
        parseContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        using var parseClient = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = parseContent }));
        var parseCompletions = new List<HttpRequestCompletion>();
        var parseError = await Should.ThrowAsync<JsonException>(() =>
            new HttpRequest<TransferDto>(null, parseClient, HttpMethod.Get, "https://example.test/parse")
                .ObserveCompletion(parseCompletions.Add).GetJsonStreamAsync());
        parseContent.IsDisposed.ShouldBeTrue();
        parseCompletions.Single().Exception.ShouldBeSameAs(parseError);
    }



    /// <summary>
    /// 验证取消期间读取是否观察到一次并释放正文。
    /// </summary>
    [Fact]
    public async Task GetJsonStreamAsync_CancellationDuringReadIsObservedOnceAndDisposesBody()
    {
        var body = new BlockingReadStream();
        var content = new StreamContent(body);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        var completions = new List<HttpRequestCompletion>();
        using var cancellation = new CancellationTokenSource();
        using var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = content }));
        var execution = new HttpRequest<TransferDto>(null, client, HttpMethod.Get, "https://example.test/json-cancel")
            .ObserveCompletion(completions.Add).GetJsonStreamAsync(cancellation.Token);
        await body.ReadStarted;
        cancellation.Cancel();
        OperationCanceledException error = null;
        try
        {
            await execution;
        }
        catch (OperationCanceledException exception)
        {
            error = exception;
        }

        error.ShouldNotBeNull();
        body.IsDisposed.ShouldBeTrue();
        completions.Count.ShouldBe(1);
        completions[0].Outcome.ShouldBe(HttpRequestOutcome.Cancelled);
        completions[0].Exception.ShouldBeSameAs(error);
    }



    /// <summary>
    /// 验证成功回调失败传播并释放响应。
    /// </summary>
    [Fact]
    public async Task GetJsonStreamAsync_SuccessCallbackFailurePropagatesAndDisposesResponse()
    {
        var content = new TrackingContent(Encoding.UTF8.GetBytes("{\"name\":\"value\"}"));
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        var callbackError = new InvalidOperationException("success callback failed");
        var completions = new List<HttpRequestCompletion>();
        using var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = content }));

        var error = await Should.ThrowAsync<InvalidOperationException>(() =>
            new HttpRequest<TransferDto>(null, client, HttpMethod.Get, "https://example.test/callback")
                .OnSuccess(_ => throw callbackError)
                .ObserveCompletion(completions.Add).GetJsonStreamAsync());

        error.ShouldBeSameAs(callbackError);
        content.IsDisposed.ShouldBeTrue();
        completions.Single().Outcome.ShouldBe(HttpRequestOutcome.Faulted);
        completions.Single().Exception.ShouldBeSameAs(callbackError);
    }



    /// <summary>
    /// 验证发送后拒绝该能力，并对 HTTP 失败响应回退到原有流程。
    /// </summary>
    [Fact]
    public async Task GetJsonStreamAsync_RejectsOnSendAfterAndFallsBackForHttpFailure()
    {
        using var conflictClient = new HttpClient(new StubHandler((_, _) => Ok("unused")));
        await Should.ThrowAsync<NotSupportedException>(() =>
            new HttpRequest<TransferDto>(null, conflictClient, HttpMethod.Get, "https://example.test/conflict")
                .OnSendAfter((_, _) => Task.FromResult(new TransferDto())).GetJsonStreamAsync());
        var failCalls = 0;
        var completeCalls = 0;
        using var failedClient = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.BadRequest)
            { Content = new StringContent("{\"error\":true}", Encoding.UTF8, "application/json") }));
        var value = await new HttpRequest<TransferDto>(null, failedClient, HttpMethod.Get, "https://example.test/failure")
            .OnFail((_, _) => failCalls++).OnComplete((_, _) => completeCalls++).GetJsonStreamAsync();
        value.ShouldBeNull();
        failCalls.ShouldBe(1);
        completeCalls.ShouldBe(1);
    }



    /// <summary>
    /// 验证返回元数据并释放响应正文。
    /// </summary>
    [Fact]
    public async Task GetJsonStreamResponseAsync_ReturnsMetadataAndDisposesResponseBody()
    {
        var content = new TrackingContent(Encoding.UTF8.GetBytes("{\"name\":\"snapshot\"}"));
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        using var client = new HttpClient(new StubHandler((request, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.Created) { Content = content };
            response.Headers.TryAddWithoutValidation("X-Trace", "trace-1");
            return response;
        }));

        var result = await new HttpRequest<TransferDto>(null, client, HttpMethod.Get, "https://example.test/response-result")
            .GetJsonStreamResponseAsync();

        result.StatusCode.ShouldBe(HttpStatusCode.Created);
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.Data.Name.ShouldBe("snapshot");
        result.RawContent.ShouldBeNull();
        result.Headers["X-Trace"].Single().ShouldBe("trace-1");
        result.ContentHeaders["Content-Type"].Single().ShouldContain("application/json");
        content.IsDisposed.ShouldBeTrue();
    }



    /// <summary>
    /// 验证非 UTF-8 响应回退到原有流程且只执行一次回调。
    /// </summary>
    [Fact]
    public async Task GetJsonStreamResponseAsync_FallsBackForNonUtf8AndRunsCallbacksOnce()
    {
        var latinJson = Encoding.GetEncoding("iso-8859-1").GetBytes("{\"name\":\"café\"}");
        var content = new TrackingContent(latinJson);
        content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(
            "application/json; charset=iso-8859-1");
        var completeCalls = 0;
        using var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = content }));

        var result = await new HttpRequest<TransferDto>(null, client, HttpMethod.Get, "https://example.test/response-fallback")
            .OnConvert(_ => new TransferDto { Name = "converted" })
            .OnComplete((_, _) => completeCalls++)
            .GetJsonStreamResponseAsync();

        result.Data.Name.ShouldBe("converted");
        result.RawContent.ShouldBeNull();
        completeCalls.ShouldBe(1);
        content.IsDisposed.ShouldBeTrue();
    }



    /// <summary>
    /// 验证空响应保留元数据。
    /// </summary>
    /// <param name="methodName">测试输入值。</param>
    /// <param name="statusCode">测试输入值。</param>
    [Theory]
    [InlineData("HEAD", 200)]
    [InlineData("GET", 204)]
    public async Task GetJsonStreamResponseAsync_EmptyResponseKeepsMetadata(string methodName, int statusCode)
    {
        var method = new HttpMethod(methodName);
        var status = (HttpStatusCode)statusCode;
        var successCalls = 0;
        using var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(status) { Content = new ByteArrayContent(Array.Empty<byte>()) }));

        var result = await new HttpRequest<TransferDto>(null, client, method, "https://example.test/response-empty")
            .OnSuccess(_ => successCalls++)
            .GetJsonStreamResponseAsync();

        result.StatusCode.ShouldBe(status);
        result.Data.ShouldBeNull();
        result.RawContent.ShouldBeNull();
        successCalls.ShouldBe(1);
    }
    /// <summary>
    /// 验证传播解析并上限失败。
    /// </summary>
    [Fact]
    public async Task GetJsonStreamResponseAsync_PropagatesParseAndLimitFailures()
    {
        var invalid = new TrackingContent(Encoding.UTF8.GetBytes("{"));
        invalid.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        using var invalidClient = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = invalid }));

        await Should.ThrowAsync<JsonException>(() =>
            new HttpRequest<TransferDto>(null, invalidClient, HttpMethod.Get, "https://example.test/response-parse")
                .GetJsonStreamResponseAsync());
        invalid.IsDisposed.ShouldBeTrue();

        var oversized = new TrackingContent(Encoding.UTF8.GetBytes("{\"name\":\"long\"}"), knownLength: false);
        oversized.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        using var limitedClient = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = oversized }));

        await Should.ThrowAsync<HttpResponseSizeLimitException>(() =>
            new HttpRequest<TransferDto>(null, limitedClient, HttpMethod.Get, "https://example.test/response-limit")
                .MaxResponseBytes(4).GetJsonStreamResponseAsync());
        oversized.IsDisposed.ShouldBeTrue();
    }



    /// <summary>
    /// 验证取消释放正文。
    /// </summary>
    [Fact]
    public async Task GetJsonStreamResponseAsync_CancellationDisposesBody()
    {
        var body = new BlockingReadStream();
        var content = new StreamContent(body);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        using var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = content }));
        using var cancellation = new CancellationTokenSource();

        var execution = new HttpRequest<TransferDto>(null, client, HttpMethod.Get, "https://example.test/response-cancel")
            .GetJsonStreamResponseAsync(cancellation.Token);
        await body.ReadStarted;
        cancellation.Cancel();

        var error = await Should.ThrowAsync<OperationCanceledException>(() => execution);
        error.ShouldNotBeNull();
        body.IsDisposed.ShouldBeTrue();
    }
    /// <summary>
    /// 创建允许 PUT 请求重试一次且无需等待的配置。
    /// </summary>
    /// <returns>禁用抖动、延迟为零并允许 PUT 重试的策略。</returns>
    private static HttpRetryOptions RetryPut()
    {
        var options = new HttpRetryOptions
        {
            MaxRetries = 1,
            BaseDelay = TimeSpan.Zero,
            MaxDelay = TimeSpan.Zero,
            UseJitter = false
        };
        options.AllowedMethods.Add(HttpMethod.Put);
        return options;
    }



    /// <summary>
    /// 创建携带指定字符串正文的成功响应。
    /// </summary>
    /// <param name="value">成功响应的字符串正文。</param>
    /// <returns>状态码为 200 且包含指定正文的 HTTP 响应。</returns>
    private static HttpResponseMessage Ok(string value) => new HttpResponseMessage(HttpStatusCode.OK)
    { Content = new StringContent(value) };
    /// <summary>
    /// 创建携带固定失败文本的指定状态响应。
    /// </summary>
    /// <param name="status">响应使用的 HTTP 状态码。</param>
    /// <returns>正文为 <c>failed</c> 的 HTTP 响应。</returns>
    private static HttpResponseMessage Status(HttpStatusCode status) => new HttpResponseMessage(status)
    { Content = new StringContent("failed") };



    /// <summary>
    /// 创建带有指定媒体类型的字节正文。
    /// </summary>
    /// <param name="bytes">作为正文载荷的原始字节。</param>
    /// <param name="contentType">可解析的 Content-Type 响应头值。</param>
    /// <returns>包含指定字节及媒体类型的 HTTP 内容，调用方负责释放。</returns>
    private static HttpContent JsonContent(byte[] bytes, string contentType)
    {
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(contentType);
        return content;
    }



    /// <summary>
    /// 根据异步回调生成 HTTP 响应的消息处理器。
    /// </summary>
    private sealed class StubHandler : HttpMessageHandler
    {
        /// <summary>
        /// 测试处理流程使用的委托。
        /// </summary>
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _send;
        /// <summary>
        /// 初始化一个 <see cref="StubHandler"/> 类型的实例。
        /// </summary>
        /// <param name="send">根据请求生成响应的委托。</param>
        public StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) => _send = send;
        /// <summary>
        /// 初始化一个 <see cref="StubHandler"/> 类型的实例。
        /// </summary>
        /// <param name="send">根据请求生成响应的委托。</param>
        public StubHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> send)
            : this((request, token) => Task.FromResult(send(request, token))) { }
        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => _send(request, cancellationToken);
    }



    /// <summary>
    /// 表示传输能力测试使用的 JSON 数据。
    /// </summary>
    private sealed class TransferDto
    {
        /// <summary>
        /// 获取或设置传输 JSON 样例的名称。
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 获取或设置传输 JSON 样例的枚举状态。
        /// </summary>
        public TransferState State { get; set; }
    }
    /// <summary>
    /// 标识传输能力测试的数据状态。
    /// </summary>
    private enum TransferState
    {
        /// <summary>
        /// 表示样例状态未知，数值为零。
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// 表示样例已就绪，数值为七。
        /// </summary>
        Ready = 7
    }



    /// <summary>
    /// 记录内存流的释放状态。
    /// </summary>
    private sealed class TrackingMemoryStream : MemoryStream
    {
        /// <summary>
        /// 获取当前测试对象是否已被释放。
        /// </summary>
        public bool IsDisposed { get; private set; }
        /// <summary>
        /// 初始化一个 <see cref="TrackingMemoryStream"/> 类型的实例。
        /// </summary>
        /// <param name="bytes">响应正文的字节数据。</param>
        public TrackingMemoryStream(byte[] bytes) : base(bytes) { }
        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing) IsDisposed = true;
            base.Dispose(disposing);
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
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        /// <inheritdoc />
        public override long Length => throw new NotSupportedException();
    }



    /// <summary>
    /// 限制单次读取字节数的内存流。
    /// </summary>
    private sealed class ChunkedMemoryStream : MemoryStream
    {
        /// <summary>
        /// 单次读取允许返回的最大字节数。
        /// </summary>
        private readonly int _chunkSize;
        /// <summary>
        /// 初始化一个 <see cref="ChunkedMemoryStream"/> 类型的实例。
        /// </summary>
        /// <param name="bytes">响应正文的字节数据。</param>
        /// <param name="chunkSize">单次读取允许返回的最大字节数。</param>
        public ChunkedMemoryStream(byte[] bytes, int chunkSize) : base(bytes) => _chunkSize = chunkSize;
        /// <inheritdoc />
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => base.ReadAsync(buffer, offset, Math.Min(count, _chunkSize), cancellationToken);
    }



    /// <summary>
    /// 通知读取开始并等待测试信号的流。
    /// </summary>
    private sealed class BlockingReadStream : Stream
    {
        /// <summary>
        /// 通知测试读取操作已开始的信号。
        /// </summary>
        private readonly TaskCompletionSource<bool> _readStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        /// <summary>
        /// 获取在首次异步读取开始时完成的通知任务。
        /// </summary>
        public Task ReadStarted => _readStarted.Task;
        /// <summary>
        /// 获取当前测试对象是否已被释放。
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
        public override void Flush() { }
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
    /// 记录 HTTP 内容释放状态并控制长度报告。
    /// </summary>
    private sealed class TrackingContent : HttpContent
    {
        /// <summary>
        /// 测试请求或响应使用的字节数据。
        /// </summary>
        private readonly byte[] _bytes;
        /// <summary>
        /// 是否向调用方报告正文长度。
        /// </summary>
        private readonly bool _knownLength;
        /// <summary>
        /// 获取当前测试对象是否已被释放。
        /// </summary>
        public bool IsDisposed { get; private set; }
        /// <summary>
        /// 初始化一个 <see cref="TrackingContent"/> 类型的实例。
        /// </summary>
        /// <param name="bytes">响应正文的字节数据。</param>
        /// <param name="knownLength">是否向调用方报告正文长度。</param>
        public TrackingContent(byte[] bytes, bool knownLength = true) { _bytes = bytes; _knownLength = knownLength; }
        /// <inheritdoc />
        protected override Task SerializeToStreamAsync(Stream stream, System.Net.TransportContext context)
            => stream.WriteAsync(_bytes, 0, _bytes.Length);
        /// <inheritdoc />
        protected override bool TryComputeLength(out long length)
        {
            length = _bytes.Length;
            return _knownLength;
        }
        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing) IsDisposed = true;
            base.Dispose(disposing);
        }
    }
}
