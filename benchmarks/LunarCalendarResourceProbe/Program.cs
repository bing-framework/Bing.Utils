using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Bing.Date.Chinese;

Console.OutputEncoding = new UTF8Encoding(false);
var workers = args.Length > 0 ? int.Parse(args[0]) : 1;
var operations = args.Length > 1 ? int.Parse(args[1]) : 50000;
var samples = args.Length > 2 ? int.Parse(args[2]) : 5;
if (workers is < 1 or > 64 || operations < workers || operations > 5000000 || samples is < 1 or > 30)
    throw new ArgumentException("参数：workers 1..64；operations workers..5000000；samples 1..30。");

var input = new DateTime(2023, 3, 22);
var coldBytesBefore = GC.GetAllocatedBytesForCurrentThread();
var coldWatch = Stopwatch.StartNew();
var first = input.ToLunarDate();
coldWatch.Stop();
var coldBytes = GC.GetAllocatedBytesForCurrentThread() - coldBytesBefore;
if (first.Year != 2023 || first.Month != 2 || first.Day != 1 || !first.IsLeapMonth)
    throw new InvalidOperationException("首次转换结果不符合固定向量。");

Run(workers, Math.Min(operations, 10000), input);
var results = new List<object>(samples);
for (var index = 0; index < samples; index++)
    results.Add(Run(workers, operations, input));

var assembly = typeof(LunarDate).Assembly.Location;
using var assemblyStream = File.OpenRead(assembly);
Console.WriteLine(JsonSerializer.Serialize(new
{
    schemaVersion = 1,
    environmentClass = "UNATTESTED",
    runtime = RuntimeInformation.FrameworkDescription,
    os = RuntimeInformation.OSDescription,
    architecture = RuntimeInformation.ProcessArchitecture.ToString(),
    logicalProcessors = Environment.ProcessorCount,
    serverGc = System.Runtime.GCSettings.IsServerGC,
    assembly,
    assemblySha256 = Convert.ToHexString(SHA256.HashData(assemblyStream)),
    requestedWorkers = workers,
    totalOperationsPerSample = operations,
    coldFirstCallMilliseconds = coldWatch.Elapsed.TotalMilliseconds,
    coldFirstCallThreadAllocatedBytes = coldBytes,
    note = "首次调用包含 JIT 和历法初始化但不含进程启动；样本包含线程启动、调度和同步。活动线程数不代表锁内并行；峰值工作集为进程生命周期高水位。",
    results
}, new JsonSerializerOptions { WriteIndented = true }));

// 工作线程只计数活动重叠，不在农历锁内部增加探针。
static object Run(int workers, int operations, DateTime input)
{
    using var ready = new CountdownEvent(workers);
    using var start = new ManualResetEventSlim();
    using var process = Process.GetCurrentProcess();
    var threads = new Thread[workers];
    var errors = new Exception?[workers];
    var sums = new long[workers];
    var active = 0;
    var maxActive = 0;
    for (var worker = 0; worker < workers; worker++)
    {
        var slot = worker;
        var count = (int)((long)operations * (slot + 1) / workers - (long)operations * slot / workers);
        threads[slot] = new Thread(() =>
        {
            ready.Signal();
            start.Wait();
            var current = Interlocked.Increment(ref active);
            int observed;
            do { observed = Volatile.Read(ref maxActive); }
            while (current > observed && Interlocked.CompareExchange(ref maxActive, current, observed) != observed);
            try
            {
                long sum = 0;
                for (var index = 0; index < count; index++)
                {
                    var lunar = input.ToLunarDate();
                    sum += lunar.Year + lunar.Month + lunar.Day + (lunar.IsLeapMonth ? 1 : 0);
                }
                sums[slot] = sum;
            }
            catch (Exception error) { errors[slot] = error; }
            finally { Interlocked.Decrement(ref active); }
        });
    }

    var allocatedBefore = GC.GetTotalAllocatedBytes(true);
    var cpuBefore = process.TotalProcessorTime;
    var gc0 = GC.CollectionCount(0);
    var gc1 = GC.CollectionCount(1);
    var gc2 = GC.CollectionCount(2);
    var watch = Stopwatch.StartNew();
    foreach (var thread in threads) thread.Start();
    ready.Wait();
    start.Set();
    foreach (var thread in threads) thread.Join();
    watch.Stop();
    process.Refresh();
    foreach (var error in errors)
        if (error != null) throw new InvalidOperationException("工作线程转换失败。", error);
    if (sums.Sum() != 2027L * operations)
        throw new InvalidOperationException("农历转换结果不符合固定向量。");

    return new
    {
        wallMilliseconds = watch.Elapsed.TotalMilliseconds,
        cpuMilliseconds = (process.TotalProcessorTime - cpuBefore).TotalMilliseconds,
        allocatedBytes = GC.GetTotalAllocatedBytes(true) - allocatedBefore,
        gen0 = GC.CollectionCount(0) - gc0,
        gen1 = GC.CollectionCount(1) - gc1,
        gen2 = GC.CollectionCount(2) - gc2,
        maxActiveWorkers = maxActive,
        peakWorkingSetBytes = process.PeakWorkingSet64,
        endWorkingSetBytes = process.WorkingSet64
    };
}
