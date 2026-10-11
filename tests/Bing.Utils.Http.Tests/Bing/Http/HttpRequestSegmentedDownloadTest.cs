using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using Bing.Http;
using Bing.Http.Clients;

namespace Bing.Utils.Http.Tests.Bing.Http;

/// <summary>

/// HttpRequest 显式分片入口的 fake transport 测试。

/// </summary>
public class HttpRequestSegmentedDownloadTest
{
    /// <summary>
    /// 验证探测、调度和校验分段后提交文件。
    /// </summary>
    [Fact]
    public async Task SegmentedDownload_ProbesSchedulesValidatesAndCommits()
    {
        var root = CreateDirectory();
        try
        {
            var target = Path.Combine(root, "download.bin");
            var request = new HttpRequest<string>(null, new HttpClient(new StubHandler()), HttpMethod.Get,
                "https://example.test/file");
            var state = new HttpDownloadSegmentsState
            {
                SchemaVersion = HttpDownloadSegmentStateStore.CurrentSchemaVersion,
                TotalLength = 8,
                Segments = HttpDownloadRangePlanner.Create(8, 4).Select(range =>
                    new HttpDownloadSegmentState { Start = range.Start, End = range.End }).ToList()
            };
            var probeCalls = 0;
            var downloadCalls = 0;
            var validated = false;
            request.SetResumableDownloadValidator(async (stream, token) =>
            {
                using (var reader = new StreamReader(stream, Encoding.UTF8, false, 1024, true))
                    (await reader.ReadToEndAsync()).ShouldBe("ABCDEFGH");
                validated = true;
            });

            var result = await request.DownloadFileSegmentedAsync(target, state,
                (range, token) =>
                {
                    range.ShouldBeNull();
                    probeCalls++;
                    return Task.FromResult(new HttpDownloadRangeResponseMetadata
                    {
                        StatusCode = HttpStatusCode.OK,
                        AcceptRanges = "bytes",
                        ContentLength = 8
                    });
                },
                (range, token) =>
                {
                    downloadCalls++;
                    var content = range.Start == 0 ? "ABCD" : "EFGH";
                    return Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(content)));
                }, 4, 2);

            probeCalls.ShouldBe(1);
            downloadCalls.ShouldBe(2);
            result.Segments.All(segment => segment.Completed).ShouldBeTrue();
            validated.ShouldBeTrue();
            File.ReadAllText(target).ShouldBe("ABCDEFGH");
        }
        finally { Delete(root); }
    }

    /// <summary>

    /// 验证拒绝服务端未使用范围之前写入。

    /// </summary>
    [Fact]
    public async Task SegmentedDownload_RejectsServerWithoutRangesBeforeWriting()
    {
        var root = CreateDirectory();
        try
        {
            var target = Path.Combine(root, "download.bin");
            var request = new HttpRequest<string>(null, new HttpClient(new StubHandler()), HttpMethod.Get,
                "https://example.test/file");
            var state = new HttpDownloadSegmentsState
            {
                SchemaVersion = HttpDownloadSegmentStateStore.CurrentSchemaVersion,
                TotalLength = 4,
                Segments = new List<HttpDownloadSegmentState>
                {
                    new HttpDownloadSegmentState { Start = 0, End = 3 }
                }
            };
            await Should.ThrowAsync<NotSupportedException>(() => request.DownloadFileSegmentedAsync(target, state,
                (range, token) => Task.FromResult(new HttpDownloadRangeResponseMetadata
                {
                    StatusCode = HttpStatusCode.OK,
                    ContentLength = 4
                }),
                (range, token) => Task.FromResult<Stream>(new MemoryStream(new byte[] { 1, 2, 3, 4 })), 4, 1));
            File.Exists(target).ShouldBeFalse();
        }
        finally { Delete(root); }
    }

    /// <summary>

    /// 验证拒绝状态不同时使用不同分段大小探测。

    /// </summary>
    [Fact]
    public async Task SegmentedDownload_RejectsStateWithDifferentSegmentSizeBeforeProbe()
    {
        var root = CreateDirectory();
        try
        {
            var target = Path.Combine(root, "download.bin");
            var request = new HttpRequest<string>(null, new HttpClient(new StubHandler()), HttpMethod.Get,
                "https://example.test/file");
            var probeCalls = 0;

            await Should.ThrowAsync<InvalidDataException>(() => request.DownloadFileSegmentedAsync(target, State(8, 3),
                (range, token) =>
                {
                    probeCalls++;
                    return Task.FromResult(new HttpDownloadRangeResponseMetadata
                    {
                        StatusCode = HttpStatusCode.OK,
                        AcceptRanges = "bytes",
                        ContentLength = 8
                    });
                },
                (range, token) => Task.FromResult<Stream>(new MemoryStream(new byte[range.Length])), 4, 1));

            probeCalls.ShouldBe(0);
            File.Exists(target).ShouldBeFalse();
        }
        finally { Delete(root); }
    }

    /// <summary>

    /// 验证发送范围响应头并读取206正文。

    /// </summary>
    [Fact]
    public async Task SegmentedDownloadFromRequest_SendsRangeHeadersAndReads206Bodies()
    {
        var root = CreateDirectory();
        var handler = new RangeHandler(Encoding.UTF8.GetBytes("ABCDEFGH"));
        try
        {
            var target = Path.Combine(root, "download.bin");
            using (var client = new HttpClient(handler))
            {
                var request = new HttpRequest<string>(null, client, HttpMethod.Get,
                    "https://example.test/file");
                var state = new HttpDownloadSegmentsState
                {
                    SchemaVersion = HttpDownloadSegmentStateStore.CurrentSchemaVersion,
                    TotalLength = 8,
                    Segments = HttpDownloadRangePlanner.Create(8, 4).Select(range =>
                        new HttpDownloadSegmentState { Start = range.Start, End = range.End }).ToList()
                };
                var result = await request.DownloadFileSegmentedFromRequestAsync(target, state, 4, 2);
                result.Segments.All(segment => segment.Completed).ShouldBeTrue();
            }
            File.ReadAllText(target).ShouldBe("ABCDEFGH");
            handler.RangeHeaders.OrderBy(value => value).ShouldBe(new[] { "bytes=0-3", "bytes=4-7" });
            handler.RequestCount.ShouldBe(3);
        }
        finally { Delete(root); }
    }

    [Fact]
    public async Task SegmentedDownloadFromRequest_ReplansWhenServerLengthChanges()
    {
        var root = CreateDirectory();
        var handler = new RangeHandler(Encoding.UTF8.GetBytes("ABCDEFGHIJ"));
        try
        {
            var target = Path.Combine(root, "download.bin");
            using (var client = new HttpClient(handler))
            {
                var request = new HttpRequest<string>(null, client, HttpMethod.Get,
                    "https://example.test/file");
                var result = await request.DownloadFileSegmentedFromRequestAsync(target, State(8, 4), 4, 2);
                result.TotalLength.ShouldBe(10);
                result.Segments.All(segment => segment.Completed).ShouldBeTrue();
            }

            File.ReadAllText(target).ShouldBe("ABCDEFGHIJ");
            handler.RangeHeaders.OrderBy(value => value).ShouldBe(new[] { "bytes=0-3", "bytes=4-7", "bytes=8-9" });
        }
        finally { Delete(root); }
    }

    /// <summary>

    /// 验证重试范围失败并报告进度一次。

    /// </summary>
    [Fact]
    public async Task SegmentedDownloadFromRequest_RetriesRangeFailureAndReportsProgressOnce()
    {
        var root = CreateDirectory();
        var handler = new RetryRangeHandler(Encoding.UTF8.GetBytes("ABCDEFGH"), 4, 1);
        try
        {
            var target = Path.Combine(root, "download.bin");
            var progress = new List<HttpDownloadSegmentProgress>();
            using (var client = new HttpClient(handler))
            {
                var request = new HttpRequest<string>(null, client, HttpMethod.Get,
                    "https://example.test/file");
                var result = await request.DownloadFileSegmentedFromRequestAsync(target, State(8, 4), 4, 2,
                    maxRetries: 1, progressObserver: progress.Add);
                result.Segments.All(segment => segment.Completed).ShouldBeTrue();
            }
            File.ReadAllText(target).ShouldBe("ABCDEFGH");
            progress.Select(item => item.CompletedBytes).OrderBy(value => value).ShouldBe(new long[] { 4, 8 });
            handler.RangeAttempts["bytes=4-7"].ShouldBe(2);
        }
        finally { Delete(root); }
    }

    /// <summary>

    /// 验证持久化失败保留原目标文件并报告失败。

    /// </summary>
    [Fact]
    public async Task SegmentedDownloadFromRequest_PersistentFailureKeepsOldTargetAndReportsFailure()
    {
        var root = CreateDirectory();
        var handler = new RetryRangeHandler(Encoding.UTF8.GetBytes("ABCDEFGH"), 4, int.MaxValue);
        try
        {
            var target = Path.Combine(root, "download.bin");
            File.WriteAllText(target, "old");
            var failures = new List<HttpDownloadSegmentFailure>();
            using (var client = new HttpClient(handler))
            {
                var request = new HttpRequest<string>(null, client, HttpMethod.Get,
                    "https://example.test/file");
                var result = await request.DownloadFileSegmentedFromRequestAsync(target, State(8, 4), 4, 2,
                    maxRetries: 1, failureObserver: failures.Add);
                result.Segments.Count(segment => !segment.Completed).ShouldBe(1);
            }
            File.ReadAllText(target).ShouldBe("old");
            failures.Count.ShouldBe(1);
            failures.Single().Range.ShouldBe(new HttpDownloadRange(4, 7));
            failures.Single().Attempts.ShouldBe(2);
        }
        finally { Delete(root); }
    }

    /// <summary>

    /// 验证转发重试观察器并隔离观察器异常。

    /// </summary>
    [Fact]
    public async Task SegmentedDownloadFromRequest_BridgesRetryObserverAndIsolatesObserverErrors()
    {
        var root = CreateDirectory();
        var handler = new RetryRangeHandler(Encoding.UTF8.GetBytes("ABCDEFGH"), 4, 1);
        try
        {
            var events = new List<HttpRetryScheduled>();
            var observerErrors = new List<Exception>();
            using (var client = new HttpClient(handler))
            {
                var request = new HttpRequest<string>(null, client, HttpMethod.Get,
                    "https://example.test/file");
                request.SetRetryObserver(item =>
                {
                    events.Add(item);
                    throw new InvalidOperationException("observer");
                }, observerErrors.Add);
                await request.DownloadFileSegmentedFromRequestAsync(
                    Path.Combine(root, "download.bin"), State(8, 4), 4, 2, maxRetries: 1);
            }
            events.Count.ShouldBe(1);
            events.Single().FailedAttempt.ShouldBe(1);
            observerErrors.Count.ShouldBe(1);
        }
        finally { Delete(root); }
    }

    /// <summary>

    /// 验证取消传播从处理程序。

    /// </summary>
    [Fact]
    public async Task SegmentedDownloadFromRequest_CancellationPropagatesFromHandler()
    {
        var root = CreateDirectory();
        try
        {
            using (var client = new HttpClient(new SlowRangeHandler()))
            using (var cancellation = new CancellationTokenSource())
            {
                var request = new HttpRequest<string>(null, client, HttpMethod.Get,
                    "https://example.test/file");
                var operation = request.DownloadFileSegmentedFromRequestAsync(
                    Path.Combine(root, "download.bin"), State(4, 4), 4, 1,
                    cancellationToken: cancellation.Token);
                await Task.Delay(50);
                cancellation.Cancel();
                await Should.ThrowAsync<OperationCanceledException>(() => operation);
            }
            File.Exists(Path.Combine(root, "download.bin")).ShouldBeFalse();
        }
        finally { Delete(root); }
    }

    /// <summary>

    /// 验证拒绝On发送之后使用显式原因。

    /// </summary>
    [Fact]
    public void SegmentedDownloadFromRequest_RejectsOnSendAfterWithExplicitReason()
    {
        var root = CreateDirectory();
        try
        {
            using (var client = new HttpClient(new StubHandler()))
            {
                var request = new HttpRequest<string>(null, client, HttpMethod.Get,
                    "https://example.test/file");
                request.OnSendAfter(_ => Task.FromResult("custom"));
                Should.Throw<NotSupportedException>(() => request.DownloadFileSegmentedFromRequestAsync(
                    Path.Combine(root, "download.bin"), State(4, 4), 4, 1));
            }
        }
        finally { Delete(root); }
    }

    [Fact]
    public async Task SegmentedDownload_ResumesFromPersistedCheckpointAfterFailure()
    {
        var root = CreateDirectory();
        try
        {
            var target = Path.Combine(root, "download.bin");
            File.WriteAllText(target, "old");
            var request = new HttpRequest<string>(null, new HttpClient(new StubHandler()), HttpMethod.Get,
                "https://example.test/file");
            var requestedRanges = new List<HttpDownloadRange>();

            var first = await request.DownloadFileSegmentedAsync(target, State(8, 4),
                (range, token) => Task.FromResult(ProbeMetadata(8)),
                (range, token) =>
                {
                    requestedRanges.Add(range);
                    if (range.Start == 0)
                        return Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes("ABCD")));
                    throw new IOException("temporary failure");
                }, 4, 1);

            first.Segments[0].Completed.ShouldBeTrue();
            first.Segments[1].Completed.ShouldBeFalse();
            File.ReadAllText(target).ShouldBe("old");
            HttpDownloadSegmentStateStore.Load(target + ".binghttp.segments.json").Segments[0].Completed.ShouldBeTrue();

            var second = await request.DownloadFileSegmentedAsync(target, State(8, 4),
                (range, token) => Task.FromResult(ProbeMetadata(8)),
                (range, token) =>
                {
                    requestedRanges.Add(range);
                    range.Start.ShouldBe(4);
                    return Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes("EFGH")));
                }, 4, 1);

            second.Segments.All(segment => segment.Completed).ShouldBeTrue();
            requestedRanges.Select(range => range.Start).ShouldBe(new long[] { 0, 4, 4 });
            requestedRanges.Count(range => range.Start == 0).ShouldBe(1);
            File.ReadAllText(target).ShouldBe("ABCDEFGH");
            File.Exists(target + ".binghttp.segments.json").ShouldBeFalse();
            Directory.Exists(target + ".binghttp.segments").ShouldBeFalse();
        }
        finally { Delete(root); }
    }

    [Fact]
    public async Task SegmentedDownload_UnknownStateSchemaStartsOver()
    {
        var root = CreateDirectory();
        try
        {
            var target = Path.Combine(root, "download.bin");
            File.WriteAllText(target, "old");
            File.WriteAllText(target + ".binghttp.segments.json", "{\"SchemaVersion\":999}", Encoding.UTF8);
            var request = new HttpRequest<string>(null, new HttpClient(new StubHandler()), HttpMethod.Get,
                "https://example.test/file");
            var calls = 0;

            var result = await request.DownloadFileSegmentedAsync(target, State(4, 4),
                (range, token) => Task.FromResult(ProbeMetadata(4)),
                (range, token) =>
                {
                    calls++;
                    return Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes("DATA")));
                }, 4, 1);

            calls.ShouldBe(1);
            result.Segments.Single().Completed.ShouldBeTrue();
            File.ReadAllText(target).ShouldBe("DATA");
            File.Exists(target + ".binghttp.segments.json").ShouldBeFalse();
        }
        finally { Delete(root); }
    }

    [Fact]
    public async Task SegmentedDownload_CancellationRetainsCompletedCheckpoint()
    {
        var root = CreateDirectory();
        try
        {
            var target = Path.Combine(root, "download.bin");
            File.WriteAllText(target, "old");
            var request = new HttpRequest<string>(null, new HttpClient(new StubHandler()), HttpMethod.Get,
                "https://example.test/file");
            using (var cancellation = new CancellationTokenSource())
            {
                await Should.ThrowAsync<OperationCanceledException>(() =>
                    request.DownloadFileSegmentedAsync(target, State(8, 4),
                        (range, token) => Task.FromResult(ProbeMetadata(8)),
                        (range, token) =>
                        {
                            if (range.Start == 0)
                                return Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes("ABCD")));
                            cancellation.Cancel();
                            return Task.FromCanceled<Stream>(cancellation.Token);
                        }, 4, 1, cancellationToken: cancellation.Token));
            }

            var saved = HttpDownloadSegmentStateStore.Load(target + ".binghttp.segments.json");
            saved.Segments[0].Completed.ShouldBeTrue();
            saved.Segments[1].Completed.ShouldBeFalse();
            File.Exists(Path.Combine(target + ".binghttp.segments", "segment-0-3.part")).ShouldBeTrue();
            File.ReadAllText(target).ShouldBe("old");
        }
        finally { Delete(root); }
    }

    /// <summary>

    /// 处理测试场景中的 State。

    /// </summary>
    private static HttpDownloadSegmentsState State(long totalLength, long segmentSize)
        => new HttpDownloadSegmentsState
        {
            SchemaVersion = HttpDownloadSegmentStateStore.CurrentSchemaVersion,
            TotalLength = totalLength,
            Segments = HttpDownloadRangePlanner.Create(totalLength, segmentSize).Select(range =>
                new HttpDownloadSegmentState { Start = range.Start, End = range.End }).ToList()
        };

    private static HttpDownloadRangeResponseMetadata ProbeMetadata(long totalLength)
        => new HttpDownloadRangeResponseMetadata
        {
            StatusCode = HttpStatusCode.OK,
            AcceptRanges = "bytes",
            ContentLength = totalLength
        };

    /// <summary>

    /// 始终返回成功响应的消息处理器。

    /// </summary>
    private sealed class StubHandler : HttpMessageHandler
    {
        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }

    /// <summary>

    /// 按请求范围返回正文片段并记录范围头。

    /// </summary>
    private sealed class RangeHandler : HttpMessageHandler
    {
        /// <summary>
        /// 测试请求或响应使用的字节数据。
        /// </summary>
        private readonly byte[] _body;

        /// <summary>

        /// 初始化一个<see cref="RangeHandler"/>类型的实例。

        /// </summary>
        /// <param name="body">响应正文数据。</param>
        public RangeHandler(byte[] body) { _body = body; }

        /// <summary>

        /// 记录请求中的范围头。

        /// </summary>
        public List<string> RangeHeaders { get; } = new List<string>();

        /// <summary>

        /// 记录收到的请求数量。

        /// </summary>
        public int RequestCount { get; private set; }

        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            if (!request.Headers.TryGetValues("Range", out var values))
            {
                var full = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(_body)
                };
                full.Headers.TryAddWithoutValidation("Accept-Ranges", "bytes");
                return Task.FromResult(full);
            }

            var header = values.Single();
            RangeHeaders.Add(header);
            var bounds = header.Substring("bytes=".Length).Split('-');
            var start = int.Parse(bounds[0]);
            var end = int.Parse(bounds[1]);
            var content = new ByteArrayContent(_body.Skip(start).Take(end - start + 1).ToArray());
            content.Headers.ContentRange = new ContentRangeHeaderValue(start, end, _body.Length);
            var partial = new HttpResponseMessage(HttpStatusCode.PartialContent) { Content = content };
            partial.Headers.TryAddWithoutValidation("Accept-Ranges", "bytes");
            return Task.FromResult(partial);
        }
    }

    /// <summary>

    /// 对指定范围注入失败并记录各范围尝试次数。

    /// </summary>
    private sealed class RetryRangeHandler : HttpMessageHandler
    {
        /// <summary>
        /// 测试请求或响应使用的字节数据。
        /// </summary>
        private readonly byte[] _body;
        /// <summary>
        /// 开始注入失败的范围起始位置。
        /// </summary>
        private readonly int _failureStart;
        /// <summary>
        /// 剩余要注入的失败次数。
        /// </summary>
        private readonly int _failures;
        /// <summary>
        /// 测试期间记录的调用状态。
        /// </summary>
        private readonly Dictionary<string, int> _attempts = new Dictionary<string, int>();

        /// <summary>

        /// 初始化一个<see cref="RetryRangeHandler"/>类型的实例。

        /// </summary>
        /// <param name="body">响应正文数据。</param>
        /// <param name="failureStart">开始注入失败的范围起始位置。</param>
        /// <param name="failures">要注入的失败次数。</param>
        public RetryRangeHandler(byte[] body, int failureStart, int failures)
        {
            _body = body;
            _failureStart = failureStart;
            _failures = failures;
        }

        /// <summary>

        /// 记录测试替身使用的状态。

        /// </summary>
        public IReadOnlyDictionary<string, int> RangeAttempts => _attempts;

        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (!request.Headers.TryGetValues("Range", out var values))
            {
                var full = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(_body) };
                full.Headers.TryAddWithoutValidation("Accept-Ranges", "bytes");
                return Task.FromResult(full);
            }

            var rangeHeader = values.Single();
            _attempts[rangeHeader] = _attempts.TryGetValue(rangeHeader, out var prior) ? prior + 1 : 1;
            var bounds = rangeHeader.Substring("bytes=".Length).Split('-');
            var start = int.Parse(bounds[0]);
            var end = int.Parse(bounds[1]);
            if (start == _failureStart && _attempts[rangeHeader] <= _failures)
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

            var content = new ByteArrayContent(_body.Skip(start).Take(end - start + 1).ToArray());
            content.Headers.ContentRange = new ContentRangeHeaderValue(start, end, _body.Length);
            var partial = new HttpResponseMessage(HttpStatusCode.PartialContent) { Content = content };
            partial.Headers.TryAddWithoutValidation("Accept-Ranges", "bytes");
            return Task.FromResult(partial);
        }
    }

    /// <summary>

    /// 延迟返回范围响应以验证分段调度的消息处理器。

    /// </summary>
    private sealed class SlowRangeHandler : HttpMessageHandler
    {
        /// <inheritdoc />
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (!request.Headers.Contains("Range"))
            {
                var full = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(new byte[] { 1, 2, 3, 4 })
                };
                full.Headers.TryAddWithoutValidation("Accept-Ranges", "bytes");
                return full;
            }
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.PartialContent);
        }
    }

    /// <summary>

    /// 创建测试所需的辅助对象或数据。

    /// </summary>
    private static string CreateDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "bing-http-request-segmented-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>

    /// 清理测试生成的临时资源。

    /// </summary>
    private static void Delete(string path)
    {
        if (Directory.Exists(path)) Directory.Delete(path, true);
    }
}
