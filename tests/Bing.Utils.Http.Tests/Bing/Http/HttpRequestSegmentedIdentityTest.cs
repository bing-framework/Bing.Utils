using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using Bing.Http;
using Bing.Http.Clients;

namespace Bing.Utils.Http.Tests.Bing.Http;

/// <summary>
/// 验证自动分片下载会把恢复状态绑定到实际请求和远端资源版本。
/// </summary>
public class HttpRequestSegmentedIdentityTest
{
    [Fact]
    public async Task SameLengthVersionChange_DiscardsCompletedOldSegments()
    {
        var root = CreateDirectory();
        try
        {
            var target = Path.Combine(root, "download.bin");
            var handler = new VersionedRangeHandler("ABCDEFGH", "\"v1\"")
            {
                FailureStart = 4,
                FailuresRemaining = 1
            };
            using var client = new HttpClient(handler);

            var first = await Request(client).DownloadFileSegmentedFromRequestAsync(target, State(), 4, 1);

            Assert.True(first.Segments[0].Completed);
            Assert.False(first.Segments[1].Completed);
            handler.SetResource("12345678", "\"v2\"");
            handler.FailureStart = -1;

            var second = await Request(client).DownloadFileSegmentedFromRequestAsync(target, State(), 4, 1);

            Assert.All(second.Segments, segment => Assert.True(segment.Completed));
            Assert.Equal("12345678", File.ReadAllText(target, Encoding.UTF8));
            Assert.Equal(2, handler.RangeCounts["bytes=0-3"]);
            Assert.Contains("\"v2\"", handler.IfRangeValues);
        }
        finally { Delete(root); }
    }

    [Fact]
    public async Task StableVersion_ResumesOnlyMissingSegments()
    {
        var root = CreateDirectory();
        try
        {
            var target = Path.Combine(root, "download.bin");
            var handler = new VersionedRangeHandler("ABCDEFGH", "\"stable\"")
            {
                FailureStart = 4,
                FailuresRemaining = 1
            };
            using var client = new HttpClient(handler);

            var first = await Request(client).DownloadFileSegmentedFromRequestAsync(target, State(), 4, 1);
            Assert.False(first.Segments[1].Completed);
            handler.FailureStart = -1;

            var second = await Request(client).DownloadFileSegmentedFromRequestAsync(target, State(), 4, 1);

            Assert.All(second.Segments, segment => Assert.True(segment.Completed));
            Assert.Equal("ABCDEFGH", File.ReadAllText(target, Encoding.UTF8));
            Assert.Equal(1, handler.RangeCounts["bytes=0-3"]);
            Assert.Equal(2, handler.RangeCounts["bytes=4-7"]);
            Assert.All(handler.IfRangeValues, value => Assert.Equal("\"stable\"", value));
        }
        finally { Delete(root); }
    }

    [Fact]
    public async Task VersionChangeBetweenRanges_DoesNotMergeMixedContent()
    {
        var root = CreateDirectory();
        try
        {
            var target = Path.Combine(root, "download.bin");
            File.WriteAllText(target, "existing", Encoding.UTF8);
            var handler = new VersionedRangeHandler("ABCDEFGH", "\"v1\"")
            {
                SwitchResourceAtRangeStart = 4,
                SwitchedBody = "12345678",
                SwitchedETag = "\"v2\""
            };
            using var client = new HttpClient(handler);

            var result = await Request(client).DownloadFileSegmentedFromRequestAsync(target, State(), 4, 1);

            Assert.False(result.Segments[1].Completed);
            Assert.Equal("existing", File.ReadAllText(target, Encoding.UTF8));
            Assert.All(handler.IfRangeValues, value => Assert.Equal("\"v1\"", value));
        }
        finally { Delete(root); }
    }

    [Fact]
    public async Task MissingValidator_DoesNotReuseCompletedSegmentsAcrossCalls()
    {
        var root = CreateDirectory();
        try
        {
            var target = Path.Combine(root, "download.bin");
            var handler = new VersionedRangeHandler("ABCDEFGH", null)
            {
                FailureStart = 4,
                FailuresRemaining = 1
            };
            using var client = new HttpClient(handler);

            var first = await Request(client).DownloadFileSegmentedFromRequestAsync(target, State(), 4, 1);
            Assert.True(first.Segments[0].Completed);
            handler.FailureStart = -1;

            var second = await Request(client).DownloadFileSegmentedFromRequestAsync(target, State(), 4, 1);

            Assert.All(second.Segments, segment => Assert.True(segment.Completed));
            Assert.Equal(2, handler.RangeCounts["bytes=0-3"]);
            Assert.All(handler.IfRangeValues, value => Assert.Null(value));
            Assert.Equal("ABCDEFGH", File.ReadAllText(target, Encoding.UTF8));
        }
        finally { Delete(root); }
    }

    [Fact]
    public async Task RequestUriChange_DoesNotReuseCompletedSegments()
    {
        var root = CreateDirectory();
        try
        {
            var target = Path.Combine(root, "download.bin");
            var handler = new VersionedRangeHandler("ABCDEFGH", "\"same\"")
            {
                FailureStart = 4,
                FailuresRemaining = 1
            };
            using var client = new HttpClient(handler);

            var first = await Request(client, "https://example.test/first").DownloadFileSegmentedFromRequestAsync(target, State(), 4, 1);
            Assert.True(first.Segments[0].Completed);
            handler.FailureStart = -1;

            var second = await Request(client, "https://example.test/second").DownloadFileSegmentedFromRequestAsync(target, State(), 4, 1);

            Assert.All(second.Segments, segment => Assert.True(segment.Completed));
            Assert.Equal(2, handler.RangeCounts["bytes=0-3"]);
        }
        finally { Delete(root); }
    }

    [Fact]
    public async Task WeakETag_UsesLastModifiedForIfRange()
    {
        var root = CreateDirectory();
        try
        {
            var target = Path.Combine(root, "download.bin");
            var modified = new DateTimeOffset(2026, 10, 22, 12, 13, 14, TimeSpan.Zero);
            var handler = new VersionedRangeHandler("ABCDEFGH", "W/\"weak\"")
            {
                LastModifiedDate = modified,
                FailureStart = 4,
                FailuresRemaining = 1
            };
            using var client = new HttpClient(handler);

            var first = await Request(client).DownloadFileSegmentedFromRequestAsync(target, State(), 4, 1);
            Assert.False(first.Segments[1].Completed);
            handler.FailureStart = -1;

            var second = await Request(client).DownloadFileSegmentedFromRequestAsync(target, State(), 4, 1);

            Assert.All(second.Segments, segment => Assert.True(segment.Completed));
            Assert.All(handler.IfRangeValues,
                value => Assert.Equal(modified.ToString("R", System.Globalization.CultureInfo.InvariantCulture), value));
        }
        finally { Delete(root); }
    }

    private static HttpRequest<string> Request(HttpClient client, string url = "https://example.test/file")
        => new(null, client, HttpMethod.Get, url);

    private static HttpDownloadSegmentsState State()
    {
        const long length = 8;
        return new HttpDownloadSegmentsState
        {
            SchemaVersion = HttpDownloadSegmentStateStore.CurrentSchemaVersion,
            TotalLength = length,
            Segments = HttpDownloadRangePlanner.Create(length, 4)
                .Select(range => new HttpDownloadSegmentState { Start = range.Start, End = range.End }).ToList()
        };
    }

    private static string CreateDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "bing-http-segment-identity-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void Delete(string path)
    {
        if (Directory.Exists(path)) Directory.Delete(path, true);
    }

    private sealed class VersionedRangeHandler : HttpMessageHandler
    {
        private byte[] _body;
        private string _etag;
        private readonly Dictionary<string, int> _rangeCounts = new(StringComparer.Ordinal);

        public VersionedRangeHandler(string body, string etag)
        {
            _body = Encoding.UTF8.GetBytes(body);
            _etag = etag;
        }

        public DateTimeOffset? LastModifiedDate { get; set; }
        public int FailureStart { get; set; } = -1;
        public int FailuresRemaining { get; set; }
        public int SwitchResourceAtRangeStart { get; set; } = -1;
        public string SwitchedBody { get; set; }
        public string SwitchedETag { get; set; }
        public IReadOnlyDictionary<string, int> RangeCounts => _rangeCounts;
        public List<string> IfRangeValues { get; } = new();

        public void SetResource(string body, string etag)
        {
            _body = Encoding.UTF8.GetBytes(body);
            _etag = etag;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!request.Headers.TryGetValues("Range", out var values))
                return Task.FromResult(CreateResponse(request, HttpStatusCode.OK, _body, null, _etag));

            var rangeHeader = values.Single();
            IfRangeValues.Add(request.Headers.TryGetValues("If-Range", out var ifRange) ? ifRange.Single() : null);
            var bounds = rangeHeader.Substring("bytes=".Length).Split('-');
            var start = int.Parse(bounds[0], System.Globalization.CultureInfo.InvariantCulture);
            var end = int.Parse(bounds[1], System.Globalization.CultureInfo.InvariantCulture);
            _rangeCounts[rangeHeader] = _rangeCounts.TryGetValue(rangeHeader, out var count) ? count + 1 : 1;

            if (start == FailureStart && FailuresRemaining > 0)
            {
                FailuresRemaining--;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
            }
            if (start == SwitchResourceAtRangeStart)
            {
                SwitchResourceAtRangeStart = -1;
                SetResource(SwitchedBody, SwitchedETag);
            }

            var content = _body.Skip(start).Take(end - start + 1).ToArray();
            return Task.FromResult(CreateResponse(request, HttpStatusCode.PartialContent, content,
                new ContentRangeHeaderValue(start, end, _body.Length), _etag));
        }

        private HttpResponseMessage CreateResponse(HttpRequestMessage request, HttpStatusCode status, byte[] body,
            ContentRangeHeaderValue contentRange, string etag)
        {
            var content = new ByteArrayContent(body);
            if (contentRange != null) content.Headers.ContentRange = contentRange;
            if (LastModifiedDate.HasValue) content.Headers.LastModified = LastModifiedDate.Value;
            var response = new HttpResponseMessage(status) { Content = content, RequestMessage = request };
            response.Headers.TryAddWithoutValidation("Accept-Ranges", "bytes");
            if (etag != null) response.Headers.ETag = EntityTagHeaderValue.Parse(etag);
            return response;
        }
    }
}
