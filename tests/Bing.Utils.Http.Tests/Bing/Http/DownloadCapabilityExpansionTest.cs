using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using Bing.Http;
using Bing.Http.Clients;

namespace Bing.Utils.Http.Tests.Bing.Http;

/// <summary>
/// 验证总下载限额和直接分片存储，不依赖外部网络。
/// </summary>
public class DownloadCapabilityExpansionTest
{
    /// <summary>
    /// 已知和未知正文长度均在写入超额数据前失败；普通缓冲请求不受下载预算影响。
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DownloadLimitIsIndependentOfResponseLimit(bool knownLength)
    {
        using var client = new HttpClient(new Handler(_ =>
        {
            var content = new StreamContent(new ShortSource(Encoding.UTF8.GetBytes("123456")));
            if (knownLength) content.Headers.ContentLength = 6;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        }));
        var request = new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/file");
        request.MaxDownloadBytes(5);
        using var target = new MemoryStream();
        await Assert.ThrowsAsync<HttpDownloadSizeLimitException>(() => request.DownloadToAsync(target));
        Assert.True(target.Length <= 5);
        var bytes = await request.GetBytesAsync();
        Assert.Equal("123456", Encoding.UTF8.GetString(bytes));
        request.MaxDownloadBytes(6); target.SetLength(0);
        await request.DownloadToAsync(target); Assert.Equal(6, target.Length);
        request.WithoutMaxDownloadLimit(); target.SetLength(0);
        await request.DownloadToAsync(target); Assert.Equal(6, target.Length);
    }

    /// <summary>
    /// 读第二块前已存在临时分片文件，证明没有缓存完整响应；总预算先于下载生效。
    /// </summary>
    [Fact]
    public async Task SegmentedResponseWritesDirectlyAndKeepsOldTargetOnLimitFailure()
    {
        var root = Path.Combine(Path.GetTempPath(), "bing-direct-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        var target = Path.Combine(root, "target.bin"); var checkedDirect = false;
        var data = new byte[200000]; new Random(42).NextBytes(data);
        try
        {
            File.WriteAllText(target, "old", Encoding.UTF8);
            using var client = new HttpClient(new Handler(message =>
            {
                var range = message.Headers.Range?.Ranges.Single();
                if (range == null)
                {
                    var probe = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Array.Empty<byte>()) };
                    probe.Headers.AcceptRanges.Add("bytes"); probe.Content.Headers.ContentLength = data.Length; return probe;
                }
                var source = new ShortSource(data, () =>
                {
                    Assert.True(Directory.Exists(target + ".binghttp.segments"));
                    Assert.NotEmpty(Directory.GetFiles(target + ".binghttp.segments", "*.tmp")); checkedDirect = true;
                });
                var response = new HttpResponseMessage(HttpStatusCode.PartialContent) { Content = new StreamContent(source) };
                response.Content.Headers.ContentRange = new ContentRangeHeaderValue(0, data.Length - 1, data.Length);
                response.Content.Headers.ContentLength = data.Length; return response;
            }));
            var request = new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/file");
            var state = new HttpDownloadSegmentsState { SchemaVersion = HttpDownloadSegmentStateStore.CurrentSchemaVersion, TotalLength = data.Length, Segments = new List<HttpDownloadSegmentState> { new() { Start = 0, End = data.Length - 1 } } };
            request.MaxDownloadBytes(data.Length - 1);
            await Assert.ThrowsAsync<HttpDownloadSizeLimitException>(() => request.DownloadFileSegmentedFromRequestAsync(target, state, data.Length, 1));
            Assert.Equal("old", File.ReadAllText(target, Encoding.UTF8)); Assert.False(checkedDirect);
            request.MaxDownloadBytes(data.Length);
            var result = await request.DownloadFileSegmentedFromRequestAsync(target, state, data.Length, 1);
            Assert.All(result.Segments, segment => Assert.True(segment.Completed));
            Assert.True(checkedDirect); Assert.Equal(data, File.ReadAllBytes(target));
        }
        finally { Directory.Delete(root, true); }
    }

    /// <summary>
    /// 进度回调的致命错误立即停止领取后续分片。
    /// </summary>
    [Fact]
    public async Task SchedulerStopsClaimingOnFatalFailure()
    {
        var root = Path.Combine(Path.GetTempPath(), "bing-workers-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        try
        {
            var calls = 0;
            var state = new HttpDownloadSegmentsState { SchemaVersion = HttpDownloadSegmentStateStore.CurrentSchemaVersion, TotalLength = 100, Segments = HttpDownloadRangePlanner.Create(100, 1).Select(r => new HttpDownloadSegmentState { Start = r.Start, End = r.End }).ToList() };
            var scheduler = new HttpDownloadSegmentScheduler(new HttpDownloadSegmentFileStore(root, Path.Combine(root, "out")), 1,
                progressObserver: _ => throw new InvalidOperationException("observer"));
            await Assert.ThrowsAsync<InvalidOperationException>(() => scheduler.DownloadAsync(state, (_, _) => { calls++; return Task.FromResult<Stream>(new MemoryStream(new byte[1])); }));
            Assert.Equal(1, calls);
        }
        finally { Directory.Delete(root, true); }
    }

    /// <summary>
    /// 返回受控 HTTP 响应。
    /// </summary>
    private sealed class Handler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;
        public Handler(Func<HttpRequestMessage, HttpResponseMessage> respond) => _respond = respond;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(_respond(request));
    }

    /// <summary>
    /// 不可定位短读正文，第二次读时检查直接输出。
    /// </summary>
    private sealed class ShortSource : MemoryStream
    {
        private readonly Action _onSecondRead; private int _reads;
        public ShortSource(byte[] data, Action onSecondRead = null) : base(data) => _onSecondRead = onSecondRead;
        public override bool CanSeek => false;
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (++_reads == 2) _onSecondRead?.Invoke();
            return base.ReadAsync(buffer, offset, Math.Min(count, _onSecondRead == null ? 2 : 81920), cancellationToken);
        }
    }
}
