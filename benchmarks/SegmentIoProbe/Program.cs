using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Bing.Http;
using Bing.IO;

// 参数：结果文件、样本数、可选 raw。每个场景先预热一次，准备与清理不计入测量。
const int seed = 20261023;
var resultPath = Path.GetFullPath(args[0]);
var repeats = int.Parse(args[1]);
var root = Path.Combine(Path.GetDirectoryName(resultPath)!, "scratch-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
var body = new byte[8 * 1024 * 1024]; new Random(seed).NextBytes(body);
var records = new List<object>();
try
{
    foreach (var size in new[] { 1, 8192, body.Length })
        await Measure("write-" + size, directory =>
        {
            var store = new HttpDownloadSegmentFileStore(directory, Path.Combine(root, "unused"));
            return async () =>
            {
                using var input = new MemoryStream(body, 0, size, false);
                var part = await store.WriteSegmentAsync(new HttpDownloadRange(0, size - 1), input);
                return new { part.BytesWritten, part.Sha256 };
            };
        });
    foreach (var size in new[] { 8192, 1024 * 1024, body.Length })
        await Measure("merge-8MiB-parts-" + body.Length / size, directory =>
        {
            var staging = Path.Combine(directory, "parts"); Directory.CreateDirectory(staging);
            var state = State(body.Length, size);
            foreach (var part in state.Segments)
            {
                // 独立准备输入，避免把生产分片写入的耗时混进合并样本。
                File.WriteAllBytes(Path.Combine(staging, $"segment-{part.Start}-{part.End}.part"), body.AsSpan((int)part.Start, size).ToArray());
                part.BytesWritten = size; part.Completed = true;
                part.Sha256 = Convert.ToHexString(SHA256.HashData(body.AsSpan((int)part.Start, size))).ToLowerInvariant();
            }
            var target = Path.Combine(directory, "target");
            var store = new HttpDownloadSegmentFileStore(staging, target);
            return async () =>
            {
                await store.MergeAsync(state);
                if (new FileInfo(target).Length != body.Length) throw new InvalidDataException();
                return new { bytes = body.Length, parts = state.Segments.Count };
            };
        });
    foreach (var checkpoint in new[] { false, true })
        await Measure("write-256-degree8-checkpoint-" + checkpoint, directory =>
        {
            var scheduler = new HttpDownloadSegmentScheduler(new HttpDownloadSegmentFileStore(Path.Combine(directory, "parts"), Path.Combine(directory, "target")), 8);
            return async () =>
            {
                var saved = 0;
                await scheduler.DownloadAsync(State(256, 1), async (_, _) => { await Task.Yield(); return new MemoryStream(new byte[] { 1 }); },
                    checkpoint: checkpoint ? async (state, token) =>
                    {
                        await HttpDownloadSegmentStateStore.SaveAsync(Path.Combine(directory, "state.json"), state, token); saved++;
                    } : null);
                return new { checkpoints = saved };
            };
        });
    if (args.Contains("raw"))
        foreach (var bufferSize in new[] { 81920, 262144, 1048576 })
        foreach (var asyncInput in new[] { false, true })
            await Measure($"raw-copy-8MiB-buffer-{bufferSize}-async-{asyncInput}", directory =>
            {
                var path = Path.Combine(directory, "input"); File.WriteAllBytes(path, body);
                return async () =>
                {
                    using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, asyncInput ? 1 : 4096, asyncInput);
                    using var output = new FileStream(Path.Combine(directory, "output"), FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, true);
                    var transfer = await StreamTransfer.CopyAsync(input, output, new StreamTransferOptions { BufferSize = bufferSize, MaxBytes = body.Length, ComputeSha256 = true });
                    output.Flush(true);
                    return new { transfer.BytesTransferred };
                };
            });
}
finally { Directory.Delete(root, true); }
Save();

async Task Measure(string name, Func<string, Func<Task<object>>> prepare)
{
    var samples = new List<object>();
    for (var i = -1; i < repeats; i++)
    {
        var directory = Path.Combine(root, Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            var action = prepare(directory);
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            var allocated = GC.GetTotalAllocatedBytes(true); var timer = Stopwatch.StartNew();
            var metrics = await action(); timer.Stop();
            if (i >= 0) samples.Add(new { milliseconds = timer.Elapsed.TotalMilliseconds, allocatedBytes = GC.GetTotalAllocatedBytes(true) - allocated, metrics });
        }
        finally { Directory.Delete(directory, true); }
    }
    records.Add(new { name, samples }); Save(); Console.WriteLine(name);
}
void Save() => File.WriteAllText(resultPath, JsonSerializer.Serialize(new
{
    seed, warmups = 1, repetitions = repeats, allocation = "GC.GetTotalAllocatedBytes(true): all process threads",
    runtime = RuntimeInformation.FrameworkDescription, platform = RuntimeInformation.OSDescription,
    volume = Path.GetPathRoot(root), httpMvid = typeof(HttpDownloadSegmentFileStore).Assembly.ManifestModule.ModuleVersionId,
    records
}, new JsonSerializerOptions { WriteIndented = true }), Encoding.UTF8);
static HttpDownloadSegmentsState State(long length, long size) => new()
{
    SchemaVersion = HttpDownloadSegmentStateStore.CurrentSchemaVersion, TotalLength = length,
    Segments = HttpDownloadRangePlanner.Create(length, size).Select(r => new HttpDownloadSegmentState { Start = r.Start, End = r.End }).ToList()
};
