using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using Bing.Http;
using Bing.Http.Clients;

namespace Bing.Utils.Http.Tests.Bing.Http;

/// <summary>
/// 验证 JSON 数组流式读取的解析、边界和资源释放行为。
/// </summary>
public class HttpJsonArrayStreamingTest
{
    /// <summary>
    /// 验证 JSON 数组流按项解析嵌套数据并处理空数组。
    /// </summary>
    [Fact]
    public async Task GetJsonArrayStreamAsync_ParsesChunkedNestedValuesAndEmptyArray()
    {
        var payload = Encoding.UTF8.GetBytes("[{\"name\":\"one\"},{\"name\":\"two\",\"state\":7}]");
        using var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent(payload) }));

        var values = new List<Item>();
        await foreach (var item in new HttpRequest<Item>(null, client, HttpMethod.Get, "https://example.test/items")
            .GetJsonArrayStreamAsync())
            values.Add(item);

        values.Count.ShouldBe(2);
        values[0].Name.ShouldBe("one");
        values[1].Name.ShouldBe("two");
        values[1].State.ShouldBe(State.Ready);

        using var emptyClient = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent(Encoding.UTF8.GetBytes("[]")) }));
        var empty = new List<Item>();
        await foreach (var item in new HttpRequest<Item>(null, emptyClient, HttpMethod.Get, "https://example.test/empty")
            .GetJsonArrayStreamAsync())
            empty.Add(item);
        empty.ShouldBeEmpty();
    }

    /// <summary>
    /// 验证在响应正文结束前产出首个数组项，并在提前停止时释放正文。
    /// </summary>
    [Fact]
    public async Task GetJsonArrayStreamAsync_YieldsFirstItemBeforeBodyEofAndDisposesOnEarlyStop()
    {
        var body = new PrefixThenBlockingStream(Encoding.UTF8.GetBytes("[{\"name\":\"first\"},"));
        using var client = new HttpClient(new StubHandler((_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(body) };
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            return response;
        }));

        var enumerator = new HttpRequest<Item>(null, client, HttpMethod.Get, "https://example.test/early")
            .GetJsonArrayStreamAsync().GetAsyncEnumerator();
        var move = enumerator.MoveNextAsync().AsTask();
        (await Task.WhenAny(move, Task.Delay(TimeSpan.FromSeconds(2)))).ShouldBe(move);
        (await move).ShouldBeTrue();
        enumerator.Current.Name.ShouldBe("first");

        await enumerator.DisposeAsync();
        body.IsDisposed.ShouldBeTrue();
    }



    /// <summary>
    /// 验证响应正文超过配置上限时抛出大小限制异常。
    /// </summary>
    [Fact]
    public async Task GetJsonArrayStreamAsync_EnforcesResponseSizeLimit()
    {
        using var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent(Encoding.UTF8.GetBytes("[{\"name\":\"long\"}]"))
            }));

        await Should.ThrowAsync<HttpResponseSizeLimitException>(async () =>
        {
            await foreach (var item in new HttpRequest<Item>(null, client, HttpMethod.Get, "https://example.test/limit")
                .MaxResponseBytes(4).GetJsonArrayStreamAsync())
            {
            }
        });
    }

    /// <summary>
    /// 验证读取过程中取消操作会释放响应正文。
    /// </summary>
    [Fact]
    public async Task GetJsonArrayStreamAsync_CancellationDuringReadDisposesBody()
    {
        var body = new PrefixThenBlockingStream(Encoding.UTF8.GetBytes("[{\"name\":\"first\"},"));
        using var client = new HttpClient(new StubHandler((_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(body) };
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            return response;
        }));
        using var cancellation = new CancellationTokenSource();
        var enumerator = new HttpRequest<Item>(null, client, HttpMethod.Get, "https://example.test/cancel")
            .GetJsonArrayStreamAsync().GetAsyncEnumerator(cancellation.Token);

        (await enumerator.MoveNextAsync()).ShouldBeTrue();
        var pending = enumerator.MoveNextAsync().AsTask();
        await Task.Delay(50);
        cancellation.Cancel();
        await Should.ThrowAsync<OperationCanceledException>(() => pending);
        body.IsDisposed.ShouldBeTrue();
    }

    /// <summary>
    /// 验证拒绝无效 JSON 尾部，并且请求失败时不重试。
    /// </summary>
    [Fact]
    public async Task GetJsonArrayStreamAsync_RejectsInvalidTailAndDoesNotRetry()
    {
        var content = new TrackingContent(Encoding.UTF8.GetBytes("[{\"name\":\"one\"}] trailing"));
        var sends = 0;
        using var client = new HttpClient(new StubHandler((_, _) =>
        {
            sends++;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        }));

        var request = new HttpRequest<Item>(null, client, HttpMethod.Get, "https://example.test/invalid-tail");
        var error = await Should.ThrowAsync<JsonException>(async () =>
        {
            await foreach (var item in request.GetJsonArrayStreamAsync())
            {
            }
        });
        error.ShouldNotBeNull();
        content.IsDisposed.ShouldBeTrue();

        using var failedClient = new HttpClient(new StubHandler((_, _) =>
        {
            sends++;
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("failed")
            };
        }));
        var retry = new HttpRetryOptions
        {
            MaxRetries = 2,
            BaseDelay = TimeSpan.Zero,
            MaxDelay = TimeSpan.Zero,
            UseJitter = false
        };
        retry.AllowedMethods.Add(HttpMethod.Get);
        await Should.ThrowAsync<HttpRequestException>(async () =>
        {
            await foreach (var item in new HttpRequest<Item>(null, failedClient, HttpMethod.Get, "https://example.test/failure")
                .Retry(retry).GetJsonArrayStreamAsync())
            {
            }
        });
        sends.ShouldBe(2);
    }

    /// <summary>
    /// 验证旧版请求不支持回调时会拒绝该操作。
    /// </summary>
    [Fact]
    public void GetJsonArrayStreamAsync_RejectsCallbacksAndLegacyRequests()
    {
        using var client = new HttpClient(new StubHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent(Encoding.UTF8.GetBytes("[]")) }));
        var request = new HttpRequest<Item>(null, client, HttpMethod.Get, "https://example.test/callback");
        Should.Throw<NotSupportedException>(() => request.OnSuccess(_ => { }).GetJsonArrayStreamAsync());
        Should.Throw<NotSupportedException>(() => request.ObserveCompletion(_ => { }).GetJsonArrayStreamAsync());

        Should.Throw<NotSupportedException>(() =>
            new Mock<IHttpRequest<Item>>().Object.GetJsonArrayStreamAsync());
    }

    /// <summary>
    /// 创建 JSON 媒体类型的 HTTP 内容。
    /// </summary>
    /// <param name="bytes">要作为 JSON 正文发送的 UTF-8 字节。</param>
    /// <returns>内容类型为 application/json 的 HTTP 内容。</returns>
    private static HttpContent JsonContent(byte[] bytes)
    {
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        return content;
    }

    /// <summary>
    /// 根据同步回调生成 HTTP 响应的消息处理器。
    /// </summary>
    private sealed class StubHandler : HttpMessageHandler
    {
        /// <summary>
        /// 根据 HTTP 请求生成测试响应的委托。
        /// </summary>
        private readonly Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> _send;
        /// <summary>
        /// 初始化一个 <see cref="StubHandler"/> 类型的实例。
        /// </summary>
        /// <param name="send">根据请求生成响应的委托。</param>
        public StubHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> send) => _send = send;
        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
            => Task.FromResult(_send(request, token));
    }

    /// <summary>
    /// 表示 JSON 数组流测试中的数据项。
    /// </summary>
    public sealed class Item
    {
        /// <summary>
        /// 获取或设置数组项的名称。
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 获取或设置数组项的状态。
        /// </summary>
        public State State { get; set; }
    }

    /// <summary>
    /// 标识 JSON 数组流测试的数据状态。
    /// </summary>
    public enum State
    {
        /// <summary>
        /// 表示未指定状态。
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// 表示数组项已就绪。
        /// </summary>
        Ready = 7
    }

    /// <summary>
    /// 保存测试响应正文并记录内容释放状态的 HTTP 内容。
    /// </summary>
    private sealed class TrackingContent : HttpContent
    {
        /// <summary>
        /// 测试请求或响应使用的字节数据。
        /// </summary>
        private readonly byte[] _bytes;
        /// <summary>
        /// 指示测试对象是否已释放。
        /// </summary>
        public bool IsDisposed { get; private set; }
        /// <summary>
        /// 初始化一个 <see cref="TrackingContent"/> 类型的实例。
        /// </summary>
        /// <param name="bytes">响应正文的字节数据。</param>
        public TrackingContent(byte[] bytes) => _bytes = bytes;
        /// <inheritdoc />
        protected override Task SerializeToStreamAsync(Stream stream, System.Net.TransportContext context)
            => stream.WriteAsync(_bytes, 0, _bytes.Length);
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
    /// 先返回指定前缀再等待后续数据的流。
    /// </summary>
    private sealed class PrefixThenBlockingStream : Stream
    {
        /// <summary>
        /// 存储流开始时返回的字节前缀。
        /// </summary>
        private readonly byte[] _prefix;
        /// <summary>
        /// 记录已从前缀流返回的字节数。
        /// </summary>
        private int _offset;
        /// <summary>
        /// 指示测试对象是否已释放。
        /// </summary>
        public bool IsDisposed { get; private set; }
        /// <summary>
        /// 初始化一个 <see cref="PrefixThenBlockingStream"/> 类型的实例。
        /// </summary>
        /// <param name="prefix">存储流开始时返回的字节前缀。</param>
        public PrefixThenBlockingStream(byte[] prefix) => _prefix = prefix;
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
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token)
        {
            if (_offset == 0)
            {
                var length = Math.Min(count, _prefix.Length);
                Buffer.BlockCopy(_prefix, 0, buffer, offset, length);
                _offset = length;
                return length;
            }
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
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
}
