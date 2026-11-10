using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Bing.Date;

Console.OutputEncoding = new UTF8Encoding(false);
// 参数限定工作量，避免诊断工具意外启动无限负载；没有业务性能阈值。
var workers = args.Length > 0 ? int.Parse(args[0]) : 1;
var operations = args.Length > 1 ? int.Parse(args[1]) : 200000;
var samples = args.Length > 2 ? int.Parse(args[2]) : 7;
var mode = args.Length > 3 ? args[3] : "single";
if (workers is < 1 or > 64 || operations < workers || operations > 10000000 || samples is < 1 or > 30 || mode is not ("single" or "batch"))
    throw new ArgumentException("参数：workers 1..64；operations workers..10000000；samples 1..30；mode single|batch。");

// 小规模预热独立于测量结果，单值/批量均走被测工厂入口。
Run(workers, Math.Min(operations, 40000), mode);
var results = new List<object>(samples);
for (var index = 0; index < samples; index++)
    results.Add(Run(workers, operations, mode));

var assembly = typeof(NoRepeatTimeStampFactory).Assembly.Location;
using var assemblyStream = File.OpenRead(assembly);
var hash = Convert.ToHexString(SHA256.HashData(assemblyStream));
Console.WriteLine(JsonSerializer.Serialize(new
{
    schemaVersion = 1,
    environmentClass = "UNATTESTED",
    runtime = RuntimeInformation.FrameworkDescription,
    os = RuntimeInformation.OSDescription,
    architecture = RuntimeInformation.ProcessArchitecture.ToString(),
    logicalProcessors = Environment.ProcessorCount,
    serverGc = System.Runtime.GCSettings.IsServerGC,
    assembly, assemblySha256 = hash,
    requestedWorkers = workers, totalOperationsPerSample = operations, mode,
    note = "包含线程启动、调度、同步与批量数组；maxActiveWorkers 是重叠工作的线程数，不是锁内并行度；peakWorkingSetBytes 是进程生命周期高水位。非稳定环境证明。",
    results
}, new JsonSerializerOptions { WriteIndented = true }));

// 工厂锁内最多一个调用执行；不增加逐项探针计数，避免改变锁竞争成本。
static object Run(int workers, int operations, string mode)
{
    var factory = new NoRepeatTimeStampFactory(0.0001);
    var origin = new DateTime(2090, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    using var ready = new CountdownEvent(workers);
    using var start = new ManualResetEventSlim();
    var threads = new Thread[workers];
    var lastTicks = new long[workers];
    var errors = new Exception?[workers];
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
                long previous = 0;
                for (var generated = 0; generated < count;)
                {
                    if (mode == "single")
                    {
                        var value = factory.GetTimeStamp(origin);
                        if (value.Ticks <= previous || value.Kind != DateTimeKind.Utc)
                            throw new InvalidOperationException("单线程观察到序列不递增或 Kind 错误。");
                        previous = value.Ticks;
                        generated++;
                    }
                    else
                    {
                        var batch = factory.GetTimeStamps(Math.Min(1000, count - generated), useUtc: true);
                        foreach (var value in batch)
                        {
                            if (value.Ticks <= previous || value.Kind != DateTimeKind.Utc)
                                throw new InvalidOperationException("批量序列不递增或 Kind 错误。");
                            previous = value.Ticks;
                        }
                        generated += batch.Length;
                    }
                }
                lastTicks[slot] = previous;
            }
            catch (Exception error) { errors[slot] = error; }
            finally { Interlocked.Decrement(ref active); }
        });
    }

    using var process = Process.GetCurrentProcess();
    var allocatedBefore = GC.GetTotalAllocatedBytes(true);
    var gcBefore = Enumerable.Range(0, 3).Select(GC.CollectionCount).ToArray();
    var cpuBefore = process.TotalProcessorTime;
    var timer = Stopwatch.StartNew();
    foreach (var thread in threads) thread.Start();
    ready.Wait();
    start.Set();
    foreach (var thread in threads) thread.Join();
    timer.Stop();
    var cpuMs = (process.TotalProcessorTime - cpuBefore).TotalMilliseconds;
    var allocated = GC.GetTotalAllocatedBytes(true) - allocatedBefore;
    var gcCounts = Enumerable.Range(0, 3).Select(g => GC.CollectionCount(g) - gcBefore[g]).ToArray();
    process.Refresh();
    if (errors.Any(error => error != null))
        throw new AggregateException(errors.Where(error => error != null)!);
    return new
    {
        elapsedMilliseconds = timer.Elapsed.TotalMilliseconds,
        operationsPerSecond = operations / timer.Elapsed.TotalSeconds,
        cpuMilliseconds = cpuMs,
        processCpuPercentOfOneCore = cpuMs / timer.Elapsed.TotalMilliseconds * 100,
        allocatedBytes = allocated, allocatedBytesPerOperation = (double)allocated / operations,
        gcCounts, maxActiveWorkers = maxActive,
        peakWorkingSetBytes = process.PeakWorkingSet64,
        workingSetBytesAfter = process.WorkingSet64,
        lastTick = lastTicks.Max()
    };
}
