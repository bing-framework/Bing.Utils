using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using Bing.Helpers;

namespace Bing.Utils.Benchmark.Benchmarks;

/// <summary>
/// Conv 高频转换路径基准。
/// </summary>
/// <remarks>
/// 正式测量由 BenchmarkDotNet 驱动，短测入口用于冻结基线。
/// </remarks>
[MemoryDiagnoser]
public class ConvBenchmarks : IDisposable
{
    /// <summary>
    /// 短测预热阶段执行的迭代次数。
    /// </summary>
    private const int ProbeWarmupIterations = 2_000;

    /// <summary>
    /// 每次短测批次执行的转换次数。
    /// </summary>
    private const int ProbeIterations = 20_000;

    /// <summary>
    /// 集合算法对照测量的重复次数。
    /// </summary>
    private const int CollectionComparisonRepetitions = 5;

    /// <summary>
    /// 数值转换基准使用的字符串输入。
    /// </summary>
    private readonly object _numericInput = "12345.67";

    /// <summary>
    /// 泛型转换基准使用的字符串输入。
    /// </summary>
    private readonly object _genericInput = "12345";

    /// <summary>
    /// 已预先装箱的数值输入。
    /// </summary>
    private readonly object _preboxedNumericInput = 12345;

    /// <summary>
    /// 由调用处传入并在调用时装箱的数值输入。
    /// </summary>
    private int _callerBoxingNumericInput = 12345;

    /// <summary>
    /// 失败路径基准使用的非法字符串输入。
    /// </summary>
    private readonly object _failureInput = "not-a-number";

    /// <summary>
    /// 自定义转换基准使用的字符串输入。
    /// </summary>
    private readonly object _customInput = "12345";

    /// <summary>
    /// 列表转换基准使用的逗号分隔输入。
    /// </summary>
    private readonly string _listInput = "1,2,3,4,5,6,7,8,9,10";

    /// <summary>
    /// 自定义转换基准使用的转换器实例。
    /// </summary>
    private readonly ConvConverter _customConverter = new ConvConverterBuilder()
        .Register<string, int>(TryParseInt)
        .Build();

    /// <summary>
    /// 字典和 JSON 基准共用的稳定输入记录。
    /// </summary>
    private readonly ProbeRecord _record = new()
    {
        Id = 42,
        Name = "baseline",
        Enabled = true
    };

    /// <summary>
    /// JSON 基准使用并由 Dispose 释放的文档。
    /// </summary>
    private readonly JsonDocument _jsonDocument = JsonDocument.Parse("{\"id\":42,\"name\":\"baseline\",\"enabled\":true}");

    /// <summary>
    /// JSON 基准使用的根元素。
    /// </summary>
    private readonly JsonElement _jsonElement;

    /// <summary>
    /// 初始化基准测试数据。
    /// </summary>
    public ConvBenchmarks()
    {
        _jsonElement = _jsonDocument.RootElement;
    }

    /// <summary>
    /// 测量字符串到 int 的数值转换。
    /// </summary>
    [Benchmark]
    public int NumericConversion() => Conv.ToInt(_numericInput);

    /// <summary>
    /// 测量字符串到 int 的泛型转换。
    /// </summary>
    [Benchmark]
    public int GenericConversion() => Conv.To<int>(_genericInput);

    /// <summary>
    /// 测量已预先装箱数值直接传入转换器的路径。
    /// </summary>
    [Benchmark(Baseline = true)]
    public int PreboxedNumericConversion() => Conv.ToInt(_preboxedNumericInput);

    /// <summary>
    /// 测量调用处将 int 字段传给 object 参数的路径。
    /// </summary>
    [Benchmark]
    public int CallerBoxingNumericConversion() => Conv.ToInt(_callerBoxingNumericInput);

    /// <summary>
    /// 测量字符串到 int 的自定义转换器路径。
    /// </summary>
    [Benchmark]
    public int CustomConverterConversion() => _customConverter.To<int>(_customInput);

    /// <summary>
    /// 测量 JsonElement 到对象的 JSON 转换。
    /// </summary>
    [Benchmark]
    public int JsonConversion() => Conv.To<ProbeRecord>(_jsonElement).Id;

    /// <summary>
    /// 测量对象到字典的转换。
    /// </summary>
    [Benchmark]
    public int DictionaryConversion() => Conv.ToDictionary(_record).Count;

    /// <summary>
    /// 测量逗号分隔字符串到列表的转换。
    /// </summary>
    [Benchmark]
    public int ListConversion() => Conv.ToList<int>(_listInput).Count;

    /// <summary>
    /// 测量非法字符串到 int 的失败转换路径。
    /// </summary>
    [Benchmark]
    public int FailureConversion() => Conv.To<int>(_failureInput);

    /// <summary>
    /// 比较当前集合转换和旧算法的性能。
    /// </summary>
    /// <remarks>
    /// 旧实现仅在基准内复现，元素转换共用当前 Conv.To&lt;T&gt;，因此不能当作完整历史版本 before/after。
    /// </remarks>
    public static void RunCollectionComparison()
    {
        using var benchmark = new ConvBenchmarks();
        Console.WriteLine($"Conv collection algorithm comparison; baselineCommit={GetCommitIdentity()}; candidate={GetCandidateIdentity()}; runtime={Environment.Version}; os={Environment.OSVersion}");
        Console.WriteLine($"warmup={ProbeWarmupIterations}; iterations={ProbeIterations}; repetitions={CollectionComparisonRepetitions}; input=shared fixed instance; allocation=GC.GetAllocatedBytesForCurrentThread; gc=GC.CollectionCount");

        CompareCollections(
            "DictionaryConversion",
            benchmark.DictionaryConversion,
            benchmark.LegacyDictionaryConversion);
        CompareCollections(
            "ListConversion",
            benchmark.ListConversion,
            benchmark.LegacyListConversion);
    }

    /// <summary>
    /// 执行固定循环短测。
    /// </summary>
    /// <remarks>
    /// 记录当前提交的时间、分配和 GC 数据，用于冻结 before 基线，不替代正式 BenchmarkDotNet 结果。
    /// </remarks>
    public static void RunBaseline()
    {
        using var benchmark = new ConvBenchmarks();
        Console.WriteLine($"Conv baseline probe; baselineCommit={GetCommitIdentity()}; candidate={GetCandidateIdentity()}; runtime={Environment.Version}; os={Environment.OSVersion}");
        Console.WriteLine($"warmup={ProbeWarmupIterations}; iterations={ProbeIterations}; allocation=GC.GetAllocatedBytesForCurrentThread; gc=GC.CollectionCount");

        Measure("NumericConversion", benchmark.NumericConversion);
        Measure("GenericConversion", benchmark.GenericConversion);
        Measure("PreboxedNumericConversion", benchmark.PreboxedNumericConversion);
        Measure("CallerBoxingNumericConversion", benchmark.CallerBoxingNumericConversion);
        Measure("CustomConverterConversion", benchmark.CustomConverterConversion);
        Measure("JsonConversion", benchmark.JsonConversion);
        Measure("DictionaryConversion", benchmark.DictionaryConversion);
        Measure("ListConversion", benchmark.ListConversion);
        Measure("FailureConversion", benchmark.FailureConversion);
    }

    /// <summary>
    /// 记录固定类型重复转换后的存活内存。
    /// </summary>
    public static void RunMemoryProbe()
    {
        using var benchmark = new ConvBenchmarks();
        for (var i = 0; i < ProbeWarmupIterations; i++)
            _ = benchmark.JsonConversion();

        ForceFullCollection();
        Console.WriteLine($"Conv memory probe; baselineCommit={GetCommitIdentity()}; candidate={GetCandidateIdentity()}; runtime={Environment.Version}; os={Environment.OSVersion}");
        Console.WriteLine($"warmup={ProbeWarmupIterations}; batchIterations={ProbeIterations}; batches=4; naturalMemory=read without explicit collection during batch; forcedMemory=read after full blocking compacting GC");

        for (var batch = 0; batch < 4; batch++)
        {
            var beforeAllocated = GC.GetAllocatedBytesForCurrentThread();
            var beforeGen0 = GC.CollectionCount(0);
            var beforeGen1 = GC.CollectionCount(1);
            var beforeGen2 = GC.CollectionCount(2);
            var beforeLive = GC.GetTotalMemory(false);
            var checksum = 0;
            for (var i = 0; i < ProbeIterations; i++)
                checksum += benchmark.JsonConversion();
            GC.KeepAlive(checksum);

            var afterNaturalAllocated = GC.GetAllocatedBytesForCurrentThread();
            var afterNaturalGen0 = GC.CollectionCount(0);
            var afterNaturalGen1 = GC.CollectionCount(1);
            var afterNaturalGen2 = GC.CollectionCount(2);
            var naturalLive = GC.GetTotalMemory(false);

            ForceFullCollection();
            var forcedLive = GC.GetTotalMemory(false);
            var afterForcedGen0 = GC.CollectionCount(0);
            var afterForcedGen1 = GC.CollectionCount(1);
            var afterForcedGen2 = GC.CollectionCount(2);

            Console.WriteLine($"batch={batch + 1}; allocatedBytes={afterNaturalAllocated - beforeAllocated}; beforeLiveBytes={beforeLive}; naturalLiveBytes={naturalLive}; forcedLiveBytes={forcedLive}; naturalGen0={afterNaturalGen0 - beforeGen0}; naturalGen1={afterNaturalGen1 - beforeGen1}; naturalGen2={afterNaturalGen2 - beforeGen2}; forcedGen0={afterForcedGen0 - afterNaturalGen0}; forcedGen1={afterForcedGen1 - afterNaturalGen1}; forcedGen2={afterForcedGen2 - afterNaturalGen2}; checksum={checksum}");
        }
        GC.KeepAlive(benchmark);
    }

    /// <summary>
    /// 测量指定操作的耗时、分配和 GC 数据。
    /// </summary>
    /// <param name="name">测量项名称。</param>
    /// <param name="operation">待测量的转换操作。</param>
    private static void Measure(string name, Func<int> operation)
    {
        for (var i = 0; i < ProbeWarmupIterations; i++)
            _ = operation();

        ForceFullCollection();

        var beforeAllocated = GC.GetAllocatedBytesForCurrentThread();
        var beforeGen0 = GC.CollectionCount(0);
        var beforeGen1 = GC.CollectionCount(1);
        var beforeGen2 = GC.CollectionCount(2);
        var checksum = 0;
        var stopwatch = Stopwatch.StartNew();
        for (var i = 0; i < ProbeIterations; i++)
            checksum ^= operation();
        stopwatch.Stop();

        var allocated = GC.GetAllocatedBytesForCurrentThread() - beforeAllocated;
        Console.WriteLine($"{name}: elapsedMs={stopwatch.Elapsed.TotalMilliseconds:F3}; allocatedBytes={allocated}; gen0={GC.CollectionCount(0) - beforeGen0}; gen1={GC.CollectionCount(1) - beforeGen1}; gen2={GC.CollectionCount(2) - beforeGen2}; checksum={checksum}");
    }

    /// <summary>
    /// 交替测量当前集合算法和旧算法并输出中位数。
    /// </summary>
    /// <param name="name">测量项名称。</param>
    /// <param name="current">当前集合转换操作。</param>
    /// <param name="legacy">旧集合转换操作。</param>
    private static void CompareCollections(string name, Func<int> current, Func<int> legacy)
    {
        for (var i = 0; i < ProbeWarmupIterations; i++)
        {
            _ = current();
            _ = legacy();
        }

        var currentMeasurements = new List<ProbeMeasurement>(CollectionComparisonRepetitions);
        var legacyMeasurements = new List<ProbeMeasurement>(CollectionComparisonRepetitions);
        for (var repetition = 0; repetition < CollectionComparisonRepetitions; repetition++)
        {
            if (repetition % 2 == 0)
            {
                currentMeasurements.Add(MeasureProbe(current));
                legacyMeasurements.Add(MeasureProbe(legacy));
            }
            else
            {
                legacyMeasurements.Add(MeasureProbe(legacy));
                currentMeasurements.Add(MeasureProbe(current));
            }
        }

        var currentMedian = Median(currentMeasurements);
        var legacyMedian = Median(legacyMeasurements);
        var elapsedRatio = legacyMedian.ElapsedMs == 0 ? double.NaN : currentMedian.ElapsedMs / legacyMedian.ElapsedMs;
        Console.WriteLine($"{name}; currentMedianMs={currentMedian.ElapsedMs:F3}; currentMedianAllocatedBytes={currentMedian.AllocatedBytes}; legacyBodyMedianMs={legacyMedian.ElapsedMs:F3}; legacyBodyMedianAllocatedBytes={legacyMedian.AllocatedBytes}; currentOverLegacyElapsedRatio={elapsedRatio:F3}");
    }

    /// <summary>
    /// 执行一次集合性能测量。
    /// </summary>
    /// <param name="operation">待测量的集合转换操作。</param>
    /// <returns>本次测量的耗时、分配和 GC 数据。</returns>
    private static ProbeMeasurement MeasureProbe(Func<int> operation)
    {
        ForceFullCollection();
        var beforeAllocated = GC.GetAllocatedBytesForCurrentThread();
        var beforeGen0 = GC.CollectionCount(0);
        var beforeGen1 = GC.CollectionCount(1);
        var beforeGen2 = GC.CollectionCount(2);
        var checksum = 0;
        var stopwatch = Stopwatch.StartNew();
        for (var i = 0; i < ProbeIterations; i++)
            checksum ^= operation();
        stopwatch.Stop();
        GC.KeepAlive(checksum);
        return new ProbeMeasurement(
            stopwatch.Elapsed.TotalMilliseconds,
            GC.GetAllocatedBytesForCurrentThread() - beforeAllocated,
            GC.CollectionCount(0) - beforeGen0,
            GC.CollectionCount(1) - beforeGen1,
            GC.CollectionCount(2) - beforeGen2);
    }

    /// <summary>
    /// 取得测量结果的中位数。
    /// </summary>
    /// <param name="measurements">待排序的测量结果。</param>
    /// <returns>按耗时排序后的中位测量结果。</returns>
    private static ProbeMeasurement Median(IReadOnlyList<ProbeMeasurement> measurements)
    {
        var ordered = measurements.OrderBy(measurement => measurement.ElapsedMs).ToArray();
        return ordered[ordered.Length / 2];
    }

    /// <summary>
    /// 执行阻塞式完整垃圾回收。
    /// </summary>
    private static void ForceFullCollection()
    {
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
    }

    /// <summary>
    /// 测量旧字典转换算法。
    /// </summary>
    private int LegacyDictionaryConversion() => LegacyToDictionary(_record).Count;

    /// <summary>
    /// 测量旧列表转换算法。
    /// </summary>
    private int LegacyListConversion() => LegacyToList<int>(_listInput).Count;

    // 复制基线 Conv 方法体，以隔离集合算法本身的开销。
    /// <summary>
    /// 按旧实现转换对象到字典。
    /// </summary>
    /// <param name="input">待转换的对象。</param>
    /// <returns>旧算法生成的字典。</returns>
    private static IDictionary<string, object> LegacyToDictionary(object input)
    {
        var result = new Dictionary<string, object>();
        if (input == null)
            return result;
        if (input is IEnumerable<KeyValuePair<string, object>> dict)
            return new Dictionary<string, object>(dict);
        foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(input))
        {
            var value = property.GetValue(input);
            result.Add(property.Name, value!);
        }
        return result;
    }

    /// <summary>
    /// 按旧实现转换逗号分隔字符串到列表。
    /// </summary>
    /// <typeparam name="T">列表元素类型。</typeparam>
    /// <param name="input">待转换的逗号分隔字符串。</param>
    /// <returns>旧算法生成的列表。</returns>
    private static List<T> LegacyToList<T>(string input)
    {
        var result = new List<T>();
        if (string.IsNullOrWhiteSpace(input))
            return result;
        var array = input.Split(',');
        result.AddRange(from each in array where !string.IsNullOrWhiteSpace(each) select Conv.To<T>(each));
        return result;
    }

    /// <summary>
    /// 按默认区域性解析 int。
    /// </summary>
    /// <param name="input">待解析的字符串。</param>
    /// <param name="result">解析得到的整数。</param>
    /// <returns>解析成功返回 true，否则返回 false。</returns>
    private static bool TryParseInt(string input, out int result) => int.TryParse(input, out result);

    /// <summary>
    /// 获取当前基准候选标识。
    /// </summary>
    /// <returns>环境变量中的候选标识，未设置时返回 working-tree。</returns>
    private static string GetCandidateIdentity()
    {
        var candidate = Environment.GetEnvironmentVariable("CONV_CANDIDATE_ID");
        return string.IsNullOrWhiteSpace(candidate) ? "working-tree" : candidate;
    }

    /// <summary>
    /// 获取当前基准基线提交标识。
    /// </summary>
    /// <returns>环境变量或 Git 返回的提交标识，无法获取时返回 unknown。</returns>
    private static string GetCommitIdentity()
    {
        var commit = Environment.GetEnvironmentVariable("CONV_BASELINE_COMMIT");
        if (!string.IsNullOrWhiteSpace(commit))
            return commit;

        try
        {
            using var process = Process.Start(new ProcessStartInfo("git", "rev-parse HEAD")
            {
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            });
            var head = process?.StandardOutput.ReadToEnd().Trim();
            process?.WaitForExit();
            return string.IsNullOrWhiteSpace(head) ? "unknown" : head;
        }
        catch
        {
            return "unknown";
        }
    }

    /// <summary>
    /// 保存一次性能测量的结果。
    /// </summary>
    /// <param name="ElapsedMs">经过的毫秒数。</param>
    /// <param name="AllocatedBytes">分配的字节数。</param>
    /// <param name="Gen0">第 0 代垃圾回收次数。</param>
    /// <param name="Gen1">第 1 代垃圾回收次数。</param>
    /// <param name="Gen2">第 2 代垃圾回收次数。</param>
    private readonly record struct ProbeMeasurement(
        double ElapsedMs,
        long AllocatedBytes,
        int Gen0,
        int Gen1,
        int Gen2);

    /// <summary>
    /// 提供字典和 JSON 场景使用的稳定输入模型。
    /// </summary>
    public sealed class ProbeRecord
    {
        /// <summary>
        /// 获取或设置记录标识。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 获取或设置记录名称。
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置记录是否启用。
        /// </summary>
        public bool Enabled { get; set; }
    }

    /// <inheritdoc />
    [GlobalCleanup]
    public void Dispose() => _jsonDocument.Dispose();
}
