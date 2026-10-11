using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using Bing.Http;
using Bing.Http.Clients;

namespace Bing.Utils.Http.Tests.Bing.Http;

/// <summary>

/// 验证 HttpJsonArrayStreamingTest 相关行为的测试。

/// </summary>
public class HttpJsonArrayStreamingTest
{
    /// <summary>
    /// 验证解析分块嵌套值并空数组。
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

    /// 验证产出首次项目之前正文正文末尾并释放提前停止时。

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



    /// 验证限制响应大小上限。



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

    /// 验证取消期间读取释放正文。

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

    /// 验证拒绝无效尾部并是否不重试。

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

    /// 处理测试场景中的 JsonContent。

    /// </summary>
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
        /// 测试处理流程使用的委托。
        /// </summary>
        private readonly Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> _send;
        /// <summary>
        /// 初始化一个<see cref="StubHandler"/>类型的实例。
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
        /// 保存测试数据对象的名称。
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 保存测试数据对象的状态。
        /// </summary>
        public State State { get; set; }
    }

    /// <summary>

    /// 标识 JSON 数组流测试的数据状态。

    /// </summary>
    public enum State { Unknown = 0, Ready = 7 }

    /// <summary>

    /// 记录正文序列化次数和释放状态的 HTTP 内容。

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
        /// 初始化一个<see cref="TrackingContent"/>类型的实例。
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
        /// 流开头先返回的字节数据。
        /// </summary>
        private readonly byte[] _prefix;
        /// <summary>
        /// 测试期间记录的调用状态。
        /// </summary>
        private int _offset;
        /// <summary>
        /// 指示测试对象是否已释放。
        /// </summary>
        public bool IsDisposed { get; private set; }
        /// <summary>
        /// 初始化一个<see cref="PrefixThenBlockingStream"/>类型的实例。
        /// </summary>
        /// <param name="prefix">流开头先返回的字节数据。</param>
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
