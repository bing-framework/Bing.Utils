using System.Buffers;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Bing.Http;
using Bing.IO;
using Bing.Utils.Json;

const int seed = 20261023;
var records = new List<object>();
var root = Path.Combine(Path.GetTempPath(), "bing-refinement-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
var body = new byte[8 * 1024 * 1024]; new Random(seed).NextBytes(body);
try
{
    foreach (var count in new[] { 1000, 10000 })
    foreach (var degree in new[] { 1, 8, 16 })
        await Measure($"schedule-failed-{count}-degree-{degree}", async () =>
        {
            var active = 0; var peak = 0; var calls = 0;
            var scheduler = new HttpDownloadSegmentScheduler(new HttpDownloadSegmentFileStore(root, Path.Combine(root, "unused")), degree);
            await scheduler.DownloadAsync(State(count, 1), async (_, _) =>
            {
                Interlocked.Increment(ref calls); Maximum(ref peak, Interlocked.Increment(ref active));
                try { await Task.Yield(); throw new IOException("probe"); }
                finally { Interlocked.Decrement(ref active); }
            });
            return new { actualPeakConcurrency = peak, calls };
        });
    foreach (var checkpoint in new[] { false, true })
        await Measure("schedule-success-256-degree8-checkpoint-" + checkpoint, async () =>
        {
            var directory = Path.Combine(root, "success"); Directory.CreateDirectory(directory);
            var checkpointPath = Path.Combine(root, "state.json"); var saved = 0; long stateBytes = 0;
            try
            {
                var scheduler = new HttpDownloadSegmentScheduler(new HttpDownloadSegmentFileStore(directory, Path.Combine(root, "out")), 8);
                await scheduler.DownloadAsync(State(256, 1), async (_, _) => { await Task.Yield(); return new MemoryStream(new byte[] { 1 }); },
                    checkpoint: checkpoint ? async (state, token) =>
                    {
                        await HttpDownloadSegmentStateStore.SaveAsync(checkpointPath, state, token);
                        saved++; stateBytes += new FileInfo(checkpointPath).Length;
                    } : null);
                return new { checkpoints = saved, serializedStateBytes = stateBytes };
            }
            finally { Directory.Delete(directory, true); if (File.Exists(checkpointPath)) File.Delete(checkpointPath); }
        });
    await Measure("segment-write-and-merge-8MiB", async () =>
    {
        var directory = Path.Combine(root, "merge"); var target = Path.Combine(root, "merged");
        var store = new HttpDownloadSegmentFileStore(directory, target); var state = State(body.Length, body.Length);
        state.Segments[0] = await store.WriteSegmentAsync(state.Segments[0].Range, new MemoryStream(body, false));
        await store.MergeAsync(state);
        if (new FileInfo(target).Length != body.Length) throw new InvalidOperationException();
        File.Delete(target); return new { bytes = body.Length };
    });

    var pool = new RequestedPool();
    var constructor = typeof(LargeMemoryStream).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(int), typeof(ArrayPool<byte>) }, null);
    var size = (int)typeof(LargeMemoryStream).GetField("PageSize", BindingFlags.NonPublic | BindingFlags.Static).GetRawConstantValue();
    using (var stream = (Stream)constructor.Invoke(new object[] { size, pool }))
    {
        try { stream.WriteByte(1); } catch (RequestedPool.StopAllocationException) { }
    }
    records.Add(new { name = "default-page-request", requestedBytes = pool.Requested, measuredAllocation = false, note = "记录首次租用请求后受控终止，不分配旧版 GB 页面" });

    foreach (var sizeInBytes in new[] { 65536, 262144, 1048576 })
        await Measure("page-size-" + sizeInBytes + "-write-8MiB", () =>
        {
            using var stream = new LargeMemoryStream(sizeInBytes); stream.Write(body, 0, body.Length);
            stream.Position = body.Length - 1; if (stream.ReadByte() != body[^1]) throw new InvalidOperationException();
            return Task.FromResult<object>(new { pages = stream.GetWrittenSegments().Count });
        });
#if REFINED
    using var schemaTable = new DataTable("rows"); schemaTable.Columns.Add("Id", typeof(int)); schemaTable.Columns.Add("Value", typeof(string));
    var schema = TabularSchema.FromTable(schemaTable);
    foreach (var count in new[] { 1000, 10000 })
        await Measure("tabular-v2-stream-" + count, async () =>
        {
            var path = Path.Combine(root, "rows.json");
            using (var output = File.Create(path)) await TabularJson.WriteRowsAsync(output, schema, Rows(count));
            long read = 0;
            using (var input = File.OpenRead(path))
            using (var reader = await TabularJson.OpenReaderAsync(input, new TabularJsonOptions { MaxRowBytes = 4096, MaxSchemaBytes = 65536 }))
                await foreach (var row in reader.ReadRowsAsync()) read++;
            var length = new FileInfo(path).Length; File.Delete(path);
            return new { rows = read, documentBytes = length };
        }, "NOT_APPLICABLE");
#endif
}
finally { Directory.Delete(root, true); }
Save();

async Task Measure(string name, Func<Task<object>> action, string beforeStatus = "COMPARABLE")
{
    await action(); var samples = new List<object>();
    for (var i = 0; i < 3; i++)
    {
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        var allocated = GC.GetTotalAllocatedBytes(true); var timer = Stopwatch.StartNew();
        var metrics = await action(); timer.Stop();
        samples.Add(new { milliseconds = timer.Elapsed.TotalMilliseconds, allocatedBytes = GC.GetTotalAllocatedBytes(true) - allocated, metrics });
    }
    records.Add(new { name, beforeStatus, samples }); Save(); Console.WriteLine(name);
}
void Save() => File.WriteAllText(args[0], JsonSerializer.Serialize(new
{
    seed, warmups = 1, repetitions = 3, allocation = "GC.GetTotalAllocatedBytes(true): all process threads",
    runtime = RuntimeInformation.FrameworkDescription, platform = RuntimeInformation.OSDescription,
    mainMvid = typeof(LargeMemoryStream).Assembly.ManifestModule.ModuleVersionId,
    httpMvid = typeof(HttpDownloadSegmentScheduler).Assembly.ManifestModule.ModuleVersionId, records
}, new JsonSerializerOptions { WriteIndented = true }), Encoding.UTF8);
static HttpDownloadSegmentsState State(long length, long segment) => new()
{
    SchemaVersion = HttpDownloadSegmentStateStore.CurrentSchemaVersion, TotalLength = length,
    Segments = HttpDownloadRangePlanner.Create(length, segment).Select(r => new HttpDownloadSegmentState { Start = r.Start, End = r.End }).ToList()
};
static void Maximum(ref int location, int value)
{
    int old; do { old = Volatile.Read(ref location); if (old >= value) return; } while (Interlocked.CompareExchange(ref location, value, old) != old);
}
#if REFINED
static async IAsyncEnumerable<TabularRowRecord> Rows(int count)
{
    for (var i = 0; i < count; i++) yield return new TabularRowRecord(0, DataRowState.Added, null, new object[] { i, "value" });
    await Task.CompletedTask;
}
#endif
sealed class RequestedPool : ArrayPool<byte>
{
    public int Requested { get; private set; }
    public override byte[] Rent(int minimumLength) { Requested = minimumLength; throw new StopAllocationException(); }
    public override void Return(byte[] array, bool clearArray = false) => throw new InvalidOperationException();
    public sealed class StopAllocationException : Exception { }
}
