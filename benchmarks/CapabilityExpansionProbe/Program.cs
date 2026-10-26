using System.Data;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Bing.Http;
using Bing.Http.Clients;
using Bing.IO;
#if CAPABILITIES
using Bing.Collections;
using Bing.IdUtils;
using Bing.Serialization.SystemTextJson;
using Bing.Utils.Json;
#endif

const int seed = 20261023;
var rows = new List<object>();
var directory = Path.Combine(Path.GetTempPath(), "bing-capability-probe-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(directory);
var bytes = new byte[8 * 1024 * 1024]; new Random(seed).NextBytes(bytes);
try
{
    await Measure("file-stream-write-8MiB", async () =>
    {
        using var source = new MemoryStream(bytes);
        await FileHelper.WriteAsync(Path.Combine(directory, "stream.bin"), source);
    });
    var value = new { Id = long.MaxValue, Name = "sample", Children = new[] { 1, 2, 3 } };
    await Measure("json-default-options-10000", () =>
    {
        for (var i = 0; i < 10000; i++) _ = Bing.Helpers.Json.ToJson(value);
        return Task.CompletedTask;
    });
    foreach (var count in new[] { 1000, 10000 })
    foreach (var degree in new[] { 1, 8, 16 })
    {
        var state = State(count, 1);
        await Measure($"segment-schedule-{count}-degree-{degree}", async () =>
        {
            // 失败分片不触发磁盘提交，隔离调度任务成本；所有版本使用同样的异步失败操作。
            var scheduler = new HttpDownloadSegmentScheduler(new HttpDownloadSegmentFileStore(directory, Path.Combine(directory, "unused")), degree);
            await scheduler.DownloadAsync(state, async (_, _) => { await Task.Yield(); throw new IOException("probe"); });
        });
    }
    using var client = new HttpClient(new RangeHandler(bytes));
    await Measure("http-segment-to-file-8MiB", async () =>
    {
        var request = new HttpRequest<string>(null, client, HttpMethod.Get, "https://probe.invalid/file");
        var result = await request.DownloadFileSegmentedFromRequestAsync(Path.Combine(directory, "download.bin"), State(bytes.Length, bytes.Length), bytes.Length, 1);
        if (result.Segments.Any(s => !s.Completed)) throw new InvalidOperationException("Probe download failed.");
    });
#if CAPABILITIES
    await Measure("new-stream-transfer-sha256-8MiB", async () =>
    {
        using var source = new MemoryStream(bytes);
        await StreamTransfer.CopyAsync(source, Stream.Null, new StreamTransferOptions { ComputeSha256 = true });
    }, "NOT_APPLICABLE");
    await Measure("new-json-array-1000", () => JsonStream.WriteArrayAsync(Stream.Null, Items()), "NOT_APPLICABLE");
    await Measure("new-ndjson-1000", () => JsonStream.WriteNdjsonAsync(Stream.Null, Items(), new NdjsonOptions { MaxRecordBytes = 128 }), "NOT_APPLICABLE");
    await Measure("new-async-select-1000-degree8", async () =>
    {
        await foreach (var item in Items().SelectConcurrentAsync(async (i, _) => { await Task.Yield(); return i; }, 8)) { }
    }, "NOT_APPLICABLE");
    using var table = new DataTable("rows"); table.Columns.Add("Id", typeof(long)); table.Columns.Add("Name", typeof(string));
    for (var i = 0; i < 1000; i++) table.Rows.Add((long)i, "name");
    await Measure("new-tabular-roundtrip-1000", () =>
    {
        using var result = TabularJson.DeserializeTable(TabularJson.SerializeTable(table)); return Task.CompletedTask;
    }, "NOT_APPLICABLE");
    foreach (var length in new[] { 16, 64, 1024 })
        await Measure($"new-secure-id-{length}-1000", () =>
        {
            for (var i = 0; i < 1000; i++) _ = SecureRandomId.Create(length, "0123456789abcdef"); return Task.CompletedTask;
        }, "NOT_APPLICABLE");
#endif
}
finally { Directory.Delete(directory, true); }
SaveEvidence();

void SaveEvidence()
{
    var identity = typeof(FileHelper).Assembly;
    File.WriteAllText(args[0], JsonSerializer.Serialize(new
    {
        seed, warmups = 1, repetitions = 3, runtime = RuntimeInformation.FrameworkDescription,
        platform = RuntimeInformation.OSDescription, identity = identity.Location,
        moduleId = identity.ManifestModule.ModuleVersionId, allocation = "GC.GetTotalAllocatedBytes(true), all process threads", rows
    }, new JsonSerializerOptions { WriteIndented = true }), Encoding.UTF8);
}

async Task Measure(string name, Func<Task> action, string beforeStatus = "COMPARABLE")
{
    await action();
    var samples = new List<object>();
    for (var i = 0; i < 3; i++)
    {
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        var start = GC.GetTotalAllocatedBytes(true); var watch = Stopwatch.StartNew();
        await action(); watch.Stop();
        samples.Add(new { milliseconds = watch.Elapsed.TotalMilliseconds, allocatedBytes = GC.GetTotalAllocatedBytes(true) - start });
    }
    rows.Add(new { name, beforeStatus, samples }); SaveEvidence(); Console.WriteLine(name);
}
static HttpDownloadSegmentsState State(long total, long segment) => new()
{
    SchemaVersion = HttpDownloadSegmentStateStore.CurrentSchemaVersion, TotalLength = total,
    Segments = HttpDownloadRangePlanner.Create(total, segment).Select(r => new HttpDownloadSegmentState { Start = r.Start, End = r.End }).ToList()
};
#if CAPABILITIES
static async IAsyncEnumerable<int> Items() { for (var i = 0; i < 1000; i++) { yield return i; await Task.Yield(); } }
#endif
sealed class RangeHandler : HttpMessageHandler
{
    private readonly byte[] _bytes;
    public RangeHandler(byte[] bytes) => _bytes = bytes;
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        var range = request.Headers.Range?.Ranges.Single();
        var response = new HttpResponseMessage(range == null ? HttpStatusCode.OK : HttpStatusCode.PartialContent)
        { Content = new StreamContent(new MemoryStream(_bytes, false)) };
        response.Headers.AcceptRanges.Add("bytes"); response.Content.Headers.ContentLength = _bytes.Length;
        if (range != null) response.Content.Headers.ContentRange = new ContentRangeHeaderValue(range.From.Value, range.To.Value, _bytes.Length);
        return Task.FromResult(response);
    }
}
