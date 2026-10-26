using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using Bing.Http;
using Bing.Http.Clients;
using Microsoft.Extensions.DependencyInjection;

namespace Bing.Utils.Http.Tests.Integration.Http;

[Trait("Category", "PerformanceApiReview")]
public class HttpResponseLimitDefaultsIntegrationTest
{
    [Fact]
    public async Task RequestOverride_DisableAndReset_RespectLastConfiguration()
    {
        using var provider = CreateProvider(3);
        var request = Request<string>(provider);
        await Assert.ThrowsAsync<HttpResponseSizeLimitException>(() => request.GetBytesAsync());
        Assert.Equal(5, (await request.MaxResponseBytes(5).GetBytesAsync()).Length);
        await Assert.ThrowsAsync<HttpResponseSizeLimitException>(() => request.MaxResponseBytes(1).GetBytesAsync());
        Assert.Equal(5, (await request.WithoutMaxResponseLimit().GetBytesAsync()).Length);
        await Assert.ThrowsAsync<HttpResponseSizeLimitException>(() => request.UseClientDefaultMaxResponseBytes().GetBytesAsync());
        Assert.Equal(5, (await request.WithoutMaxResponseLimit().MaxResponseBytes(5).GetBytesAsync()).Length);
    }

    [Fact]
    public async Task Defaults_AreIsolatedAcrossContainersAndClientNames()
    {
        using var small = CreateProvider(1);
        using var large = CreateProvider(8);
        await Assert.ThrowsAsync<HttpResponseSizeLimitException>(() => Request<string>(small).GetBytesAsync());
        Assert.Equal(5, (await Request<string>(large).GetBytesAsync()).Length);
        var request = Request<string>(small);
        Assert.Equal(5, (await request.HttpClientName("unlimited").GetBytesAsync()).Length);
        await Assert.ThrowsAsync<HttpResponseSizeLimitException>(() => request.HttpClientName("limited").GetBytesAsync());
        Assert.Equal(5, (await request.HttpClientName("unlimited").GetBytesAsync()).Length);
    }

    [Fact]
    public async Task Limit_IsSnapshotForCurrentExecution()
    {
        using var provider = CreateProvider(1);
        var request = Request<string>(provider);
        request.OnSendBefore(_ => { request.WithoutMaxResponseLimit(); return true; });
        await Assert.ThrowsAsync<HttpResponseSizeLimitException>(() => request.GetBytesAsync());
        Assert.Equal(5, (await request.GetBytesAsync()).Length);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DefaultLimit_AppliesToKnownAndUnknownLength(bool unknown)
    {
        using var provider = CreateProvider(3, () => unknown ? new UnknownContent("12345") : new StringContent("12345"));
        await Assert.ThrowsAsync<HttpResponseSizeLimitException>(() => Request<string>(provider).GetBytesAsync());
        await Assert.ThrowsAsync<HttpResponseSizeLimitException>(() => Request<string>(provider).GetResultAsync());
        using var destination = new MemoryStream();
        await Assert.ThrowsAsync<HttpResponseSizeLimitException>(() => Request<string>(provider).DownloadToAsync(destination));
        Assert.True(destination.Length <= 3);
    }

    [Fact]
    public async Task DefaultLimit_AppliesToStreamingJsonAndArray()
    {
        using var provider = CreateProvider(2, () => new StringContent("{\"Value\":\"hello\"}", Encoding.UTF8, "application/json"));
        await Assert.ThrowsAsync<HttpResponseSizeLimitException>(() => Request<Payload>(provider).GetJsonStreamAsync());
        Assert.Equal("hello", (await Request<Payload>(provider).WithoutMaxResponseLimit().GetJsonStreamAsync()).Value);
        using var arrays = CreateProvider(2, () => new StringContent("[{\"Value\":\"hello\"}]", Encoding.UTF8, "application/json"));
        await Assert.ThrowsAsync<HttpResponseSizeLimitException>(async () =>
        {
            await foreach (var item in Request<Payload>(arrays).GetJsonArrayStreamAsync()) { }
        });
    }

    [Fact]
    public async Task ZeroAllowsEmpty_AndPreCancellationPropagates()
    {
        using var empty = CreateProvider(0, () => new ByteArrayContent(Array.Empty<byte>()));
        Assert.Empty(await Request<string>(empty).GetBytesAsync());
        using var full = CreateProvider(0);
        await Assert.ThrowsAsync<HttpResponseSizeLimitException>(() => Request<string>(full).GetBytesAsync());
        using var cts = new CancellationTokenSource(); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Request<string>(empty).GetBytesAsync(cts.Token));
    }

    [Fact]
    public async Task LastBuilderRegistrationWins_AndNegativeIsRejected()
    {
        var services = new ServiceCollection();
        var builder = services.AddHttpClient("limited").ConfigurePrimaryHttpMessageHandler(() => new FixedHandler(() => new StringContent("12345")));
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.WithBingDefaultMaxResponseBytes(-1));
        builder.WithBingDefaultMaxResponseBytes(1).WithBingDefaultMaxResponseBytes(5);
        using var provider = services.BuildServiceProvider();
        Assert.Equal(5, (await Request<string>(provider).GetBytesAsync()).Length);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task SegmentedDownload_EnforcesLimit_BeforeFullBuffering(bool unknown, bool requestOverride)
    {
        var directory = Path.Combine(Path.GetTempPath(), "bing-range-limit-" + Guid.NewGuid());
        Directory.CreateDirectory(directory);
        try
        {
            var handler = new RangeLimitHandler(unknown);
            var services = new ServiceCollection();
            services.AddHttpClient("limited").ConfigurePrimaryHttpMessageHandler(() => handler)
                .WithBingDefaultMaxResponseBytes(requestOverride ? 300000 : 3);
            using var provider = services.BuildServiceProvider();
            var request = new HttpRequest<string>(provider.GetRequiredService<IHttpClientFactory>(), null, HttpMethod.Get, "https://local.test/");
            request.HttpClientName("limited");
            if (requestOverride) request.MaxResponseBytes(3);
            var state = new HttpDownloadSegmentsState
            {
                SchemaVersion = HttpDownloadSegmentStateStore.CurrentSchemaVersion,
                TotalLength = 200000,
                Segments = HttpDownloadRangePlanner.Create(200000, 200000)
                    .Select(range => new HttpDownloadSegmentState { Start = range.Start, End = range.End }).ToList()
            };
            var target = Path.Combine(directory, "file.bin");
            File.WriteAllText(target, "old", Encoding.UTF8);
            Exception observed = null;
            var result = await request.DownloadFileSegmentedFromRequestAsync(target, state, 200000, 1,
                failureObserver: failure => observed = failure.Error);
            Assert.IsType<HttpResponseSizeLimitException>(observed);
            Assert.False(result.Segments.Single().Completed);
            Assert.Equal("old", File.ReadAllText(target, Encoding.UTF8));
            // 未知长度最多读取限额加一个探测字节；已知超限正文在读取前拒绝。
            Assert.Equal(unknown ? 4 : 0, handler.RangeContent.ReadBytes);
            Assert.True(handler.RangeContent.Disposed);
        }
        finally { Directory.Delete(directory, true); }
    }

    private sealed class RangeLimitHandler : HttpMessageHandler
    {
        private readonly bool _unknown;
        public CountedContent RangeContent;
        public RangeLimitHandler(bool unknown) => _unknown = unknown;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            var range = request.Headers.Range != null;
            var content = new CountedContent(_unknown && range);
            var response = new HttpResponseMessage(range ? HttpStatusCode.PartialContent : HttpStatusCode.OK) { Content = content };
            response.Headers.TryAddWithoutValidation("Accept-Ranges", "bytes");
            if (range)
            {
                content.Headers.ContentRange = new ContentRangeHeaderValue(0, 199999, 200000);
                RangeContent = content;
            }
            return Task.FromResult(response);
        }
    }
    private sealed class CountedContent : HttpContent
    {
        private readonly bool _unknown;
        public int ReadBytes;
        public bool Disposed;
        public CountedContent(bool unknown) => _unknown = unknown;
        protected override bool TryComputeLength(out long length) { length = 200000; return !_unknown; }
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context) => throw new NotSupportedException();
        protected override Task<Stream> CreateContentReadStreamAsync() => Task.FromResult<Stream>(new CountedStream(this));
        protected override void Dispose(bool disposing) { Disposed = true; base.Dispose(disposing); }
        private sealed class CountedStream : MemoryStream
        {
            private readonly CountedContent _owner;
            public CountedStream(CountedContent owner) : base(new byte[200000]) => _owner = owner;
            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                var read = base.Read(buffer, offset, count);
                _owner.ReadBytes += read;
                return Task.FromResult(read);
            }
        }
    }

    private static ServiceProvider CreateProvider(long maxBytes, Func<HttpContent> content = null)
    {
        content ??= () => new StringContent("12345");
        var services = new ServiceCollection();
        services.AddHttpClient("limited").ConfigurePrimaryHttpMessageHandler(() => new FixedHandler(content)).WithBingDefaultMaxResponseBytes(maxBytes);
        services.AddHttpClient("unlimited").ConfigurePrimaryHttpMessageHandler(() => new FixedHandler(content));
        return services.BuildServiceProvider();
    }

    private static IHttpRequest<T> Request<T>(IServiceProvider provider) where T : class
        => new HttpRequest<T>(provider.GetRequiredService<IHttpClientFactory>(), null, HttpMethod.Get, "https://local.test/").HttpClientName("limited");

    public sealed class Payload { public string Value { get; set; } }
    private sealed class FixedHandler : HttpMessageHandler
    {
        private readonly Func<HttpContent> _content;
        public FixedHandler(Func<HttpContent> content) => _content = content;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = _content() });
        }
    }
    private sealed class UnknownContent : HttpContent
    {
        private readonly byte[] _bytes;
        public UnknownContent(string text) => _bytes = Encoding.UTF8.GetBytes(text);
        protected override bool TryComputeLength(out long length) { length = 0; return false; }
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context) => stream.WriteAsync(_bytes, 0, _bytes.Length);
        protected override Task<Stream> CreateContentReadStreamAsync() => Task.FromResult<Stream>(new MemoryStream(_bytes));
    }
}
