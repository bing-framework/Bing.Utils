using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Bing.Collections;
using Bing.Helpers;
using Bing.Http;
using Bing.IdUtils;
using Bing.IO;
using Bing.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

// 固定输入、同进程全线程分配计数；不使用单线程分配计数测量异步任务。
const int seed = 42;
const int warmups = 2;
const int repetitions = 5;
var rows = new List<object>();
// 独立调度资源探针：不把观测计数器加入既有耗时/分配对照。
if (args.Contains("--queue-probe"))
{
    foreach (var count in new[] { 1000, 10000 })
    foreach (var degree in new[] { 1, 8, 16 })
    {
        var pending = 0; var maxPending = 0; var active = 0; var maxActive = 0;
        void Maximum(ref int target, int value)
        {
            int prior;
            do { prior = Volatile.Read(ref target); if (value <= prior) return; }
            while (Interlocked.CompareExchange(ref target, value, prior) != prior);
        }
        IEnumerable<int> Source()
        {
            for (var i = 0; i < count; i++)
            {
                Maximum(ref maxPending, Interlocked.Increment(ref pending));
                yield return i;
            }
        }
        await Source().ForEachAsync(async _ =>
        {
            Interlocked.Decrement(ref pending);
            Maximum(ref maxActive, Interlocked.Increment(ref active));
            await Task.Yield();
            Interlocked.Decrement(ref active);
        }, degree);
        rows.Add(new { count, requestedConcurrency = degree, maxActualParallelism = maxActive,
            maxQueuedRequests = maxPending, finalPending = pending });
    }
    File.WriteAllText(args[0], JsonSerializer.Serialize(new { queueDefinition = "已从源领取但尚未进入操作委托的元素；不代表线程池全局队列", rows },
        new JsonSerializerOptions { WriteIndented = true }), Encoding.UTF8);
    return;
}
async Task Measure(string name, Func<Task> action, Func<object> details = null, string beforeStatus = "COMPARABLE")
{
    for (var i = 0; i < warmups; i++) await action();
    var samples = new List<object>();
    for (var i = 0; i < repetitions; i++)
    {
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        var watch = new Stopwatch();
        var allocated = GC.GetTotalAllocatedBytes(true);
        watch.Start();
        await action();
        watch.Stop();
        var bytes = GC.GetTotalAllocatedBytes(true) - allocated;
        samples.Add(new { milliseconds = watch.Elapsed.TotalMilliseconds, allocatedBytes = bytes });
    }
    rows.Add(new { name, beforeStatus, samples, details = details?.Invoke() });
    Console.WriteLine(name);
}

// 在相同旧入口上记录行为差异，不为基线伪造新增 API。
using var canceled = new CancellationTokenSource(); canceled.Cancel();
var calls = 0; string enumerationStatus;
try { await new[] { 1, 2, 3 }.ForEachAsync(_ => { calls++; return Task.CompletedTask; }, cancellationToken: canceled.Token); enumerationStatus = "completed"; }
catch (OperationCanceledException) { enumerationStatus = "canceled"; }
var errorCalls = 0;
try { await new[] { 1, 2, 3 }.ForEachAsync(_ => { errorCalls++; throw new FormatException(); }, 1, (_, _) => false); }
catch (AggregateException) { }
var path = Path.Combine(Path.GetTempPath(), "bing-probe-" + Guid.NewGuid());
Directory.CreateDirectory(path);
try
{
    var file = Path.Combine(path, "read.txt");
    File.WriteAllText(file, "test", Encoding.UTF8);
    string readStatus;
    try { using var result = await FileHelper.ReadToMemoryStreamAsync(file, canceled.Token); readStatus = result == null ? "null" : "stream"; }
    catch (OperationCanceledException) { readStatus = "canceled"; }
    rows.Add(new { name = "behavior-regressions", enumerationStatus, calls, errorCalls, readStatus });

    foreach (var size in new[] { 1024, 1024 * 1024, 8 * 1024 * 1024 })
    foreach (var random in new[] { false, true })
    {
        var data = new byte[size];
        if (random) new Random(seed).NextBytes(data); else Array.Fill(data, (byte)65);
        byte[] result = null;
        await Measure($"compress-{size}-{(random ? "random" : "repeated")}", () =>
        {
            using var input = new MemoryStream(data, false);
            result = Compression.Compress(input);
            return Task.CompletedTask;
        }, () => new { inputBytes = size, outputBytes = result.Length });
        if (!Compression.Decompress(result).SequenceEqual(data)) throw new Exception("Compression changed data");
#if CANDIDATE
        using var sink = new CountingSink();
        await Measure($"compress-to-{size}-{random}", async () =>
        {
            sink.Count = 0;
            using var input = new MemoryStream(data, false);
            await Compression.CompressToAsync(input, sink);
        }, () => new { inputBytes = size, outputBytes = sink.Count }, "NOT_APPLICABLE");
        await Measure($"decompress-to-{size}-{random}", () =>
        {
            sink.Count = 0;
            using var input = new MemoryStream(result, false);
            Compression.DecompressTo(input, sink, size);
            if (sink.Count != size) throw new Exception("Invalid output size");
            return Task.CompletedTask;
        }, () => new { outputBytes = sink.Count }, "NOT_APPLICABLE");
#endif
    }

    foreach (var count in new[] { 1000, 10000 })
    foreach (var degree in new[] { 1, 8, 16 })
    {
        var active = 0; var maximum = 0; var observed = 0;
        await Measure($"foreach-{count}-{degree}", async () =>
        {
            active = maximum = observed = 0;
            await Enumerable.Range(0, count).ForEachAsync(async _ =>
            {
                var current = Interlocked.Increment(ref active);
                int previous;
                do { previous = Volatile.Read(ref maximum); if (current <= previous) break; }
                while (Interlocked.CompareExchange(ref maximum, current, previous) != previous);
                await Task.Yield();
                Interlocked.Decrement(ref active);
                Interlocked.Increment(ref observed);
            }, degree);
            if (observed != count || maximum > degree) throw new Exception("Invalid scheduling");
        }, () => new { requestedConcurrency = degree, maxActualParallelism = maximum, maxQueuedRequests = "not instrumented", completed = observed });
    }

    RegexPool.Clear(); RegexPool.GetOrCreate("^a+$", RegexOptions.None);
    await Measure("regex-hot-10000", () =>
    {
        for (var i = 0; i < 10000; i++) RegexPool.GetOrCreate("^a+$", RegexOptions.None);
        return Task.CompletedTask;
    });
    await Measure("regex-cold-same-key-16", async () =>
    {
        RegexPool.Clear();
        using var barrier = new Barrier(16);
        var pattern = "^(?:" + string.Join("|", Enumerable.Range(0, 200).Select(i => "token" + i)) + ")+$";
        await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => Task.Factory.StartNew(() =>
        {
            if (!barrier.SignalAndWait(TimeSpan.FromSeconds(10))) throw new TimeoutException();
            return RegexPool.GetOrCreate(pattern, RegexOptions.None);
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)));
    }, () => new { RegexPool.Count, RegexPool.HitCount, RegexPool.MissCount });
    await Measure("regex-many-1100", () =>
    {
        RegexPool.Clear();
        for (var i = 0; i < 1100; i++) RegexPool.GetOrCreate("^token" + i + "$", RegexOptions.None);
        return Task.CompletedTask;
    }, () => new { RegexPool.Count, RegexPool.HitCount, RegexPool.MissCount });

    foreach (var length in new[] { 16, 64, 1024 })
        await Measure($"nonce-{length}-1000", () =>
        {
            for (var i = 0; i < 1000; i++) if (RandomNonceStrGenerator.Create(length).Length != length) throw new Exception("Nonce length");
            return Task.CompletedTask;
        });

    foreach (var entryCount in new[] { 1, 128 })
    {
        var data = new byte[entryCount == 1 ? 8 * 1024 * 1024 : 1024]; new Random(seed).NextBytes(data);
        var zipPath = Path.Combine(path, "zip-" + entryCount + ".zip");
        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            for (var i = 0; i < entryCount; i++)
            {
                using var output = archive.CreateEntry(i + ".bin", CompressionLevel.NoCompression).Open();
                output.Write(data, 0, data.Length);
            }
        var target = Path.Combine(path, "extracted-" + entryCount);
        await Measure($"zip-overwrite-{entryCount}-{data.Length}", () =>
        {
            Compression.UnZipWithLimit(zipPath, target, (long)entryCount * data.Length);
            return Task.CompletedTask;
        }, () => new { entryCount, inputBytes = (long)entryCount * data.Length,
#if CANDIDATE
            commitPath = "File.Replace (Windows existing destination; fallback separately covered by tests)"
#else
            commitPath = "File.Copy overwrite"
#endif
        });
        if (!File.ReadAllBytes(Path.Combine(target, "0.bin")).SequenceEqual(data)) throw new Exception("ZIP changed data");
    }

    var payload = new Payload { Text = new string('a', 1024 * 1024) };
    var expected = Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(payload));
    using var body = new CountingSink();
    var response = new DefaultHttpContext().Response;
    response.Body = body;
    await Measure("http-json-buffered-1MiB", async () =>
    {
        body.Count = 0;
        await response.WriteJsonAsync(payload);
        if (body.Count != expected) throw new Exception("JSON output size");
    }, () => new { outputBytes = body.Count });
#if CANDIDATE
    await Measure("http-json-streamed-1MiB", async () =>
    {
        body.Count = 0;
        await response.WriteJsonStreamAsync(payload);
        if (body.Count != expected) throw new Exception("JSON stream output size");
    }, () => new { outputBytes = body.Count }, "NOT_APPLICABLE");
    foreach (var pageSize in new[] { 65536, 1048576 })
    {
        var rented = 0;
        await Measure($"large-stream-small-write-page-{pageSize}", () =>
        {
            using var stream = new LargeMemoryStream(pageSize);
            stream.WriteByte(1);
            rented = stream.GetSpan()[0].Length;
            return Task.CompletedTask;
        }, () => new { pageSize, rentedBytes = rented }, "NOT_APPLICABLE");
    }
#endif
}
finally { Directory.Delete(path, true); }

var assemblies = new[] { typeof(Compression).Assembly, typeof(RandomNonceStrGenerator).Assembly, typeof(HttpResponseExtensions).Assembly }
    .Select(a => new { name = a.GetName().Name, sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(a.Location))) }).ToArray();
var outputPath = args.Length == 0 ? "results.json" : args[0];
File.WriteAllText(outputPath, JsonSerializer.Serialize(new { seed, warmups, repetitions,
    runtime = RuntimeInformation.FrameworkDescription, os = RuntimeInformation.OSDescription,
    allocationMeasurement = "GC.GetTotalAllocatedBytes(true), whole isolated process",
    assemblies, rows }, new JsonSerializerOptions { WriteIndented = true }), Encoding.UTF8);

sealed class Payload { public string Text { get; set; } }
sealed class CountingSink : Stream
{
    public long Count;
    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => Count;
    public override long Position { get => Count; set => throw new NotSupportedException(); }
    public override void Write(byte[] buffer, int offset, int count) => Count += count;
    public override void Write(ReadOnlySpan<byte> buffer) => Count += buffer.Length;
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken token) { token.ThrowIfCancellationRequested(); Count += count; return Task.CompletedTask; }
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken token = default) { token.ThrowIfCancellationRequested(); Count += buffer.Length; return default; }
    public override void Flush() { }
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
}
