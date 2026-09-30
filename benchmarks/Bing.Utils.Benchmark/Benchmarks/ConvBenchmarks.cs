using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using Bing.Conversions;
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
    /// 泛型数值解析使用的固定输入。
    /// </summary>
    private readonly string _longInput = "922337203685477580";

    /// <summary>
    /// 泛型浮点解析使用的固定输入。
    /// </summary>
    private readonly string _doubleInput = "12345.625";

    /// <summary>
    /// 泛型十进制解析使用的固定输入。
    /// </summary>
    private readonly string _decimalInput = "12345.625";

    /// <summary>
    /// 泛型布尔解析使用的固定输入。
    /// </summary>
    private readonly string _boolInput = "True";

    /// <summary>
    /// 数值跨类型转换使用的预装箱输入。
    /// </summary>
    private readonly object _preboxedDoubleInput = 12345.625;

    /// <summary>
    /// 同类型泛型转换使用的预装箱长整数。
    /// </summary>
    private readonly object _preboxedLongInput = 1234567890123L;

    /// <summary>
    /// 同类型泛型转换使用的预装箱十进制数。
    /// </summary>
    private readonly object _preboxedDecimalInput = 12345.625m;

    /// <summary>
    /// 同类型泛型转换使用的预装箱布尔值。
    /// </summary>
    private readonly object _preboxedBoolInput = true;

    /// <summary>
    /// 同类型泛型转换使用的 Guid。
    /// </summary>
    private readonly object _guidInput = Guid.Parse("f94d5b93-4c4b-4fca-bf97-a306db4257c5");

    /// <summary>
    /// 同类型泛型转换使用的日期。
    /// </summary>
    private readonly object _dateInput = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc);

    /// <summary>
    /// 类型化入口使用的值类型输入。
    /// </summary>
    private int _typedNumericInput = 12345;

    /// <summary>
    /// 类型化入口使用的自定义转换器。
    /// </summary>
    private readonly ConvConverter _typedCustomConverter = new ConvConverterBuilder()
        .Register<int, long>(TryConvertIntToLong)
        .Build();

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
    /// 供旧入口复用的预装箱 JSON 元素。
    /// </summary>
    private readonly object _preboxedJsonElement;

    /// <summary>
    /// 初始化基准测试数据。
    /// </summary>
    public ConvBenchmarks()
    {
        _jsonElement = _jsonDocument.RootElement;
        _preboxedJsonElement = _jsonElement;
    }

    /// <summary>
    /// 测量字符串到 int 的数值转换。
    /// </summary>
    /// <returns>转换后的整数。</returns>
    [Benchmark]
    [BenchmarkCategory("NumericString")]
    public int NumericConversion() => Conv.ToInt(_numericInput);

    /// <summary>
    /// 测量字符串到 int 的泛型转换。
    /// </summary>
    /// <returns>转换后的整数。</returns>
    [Benchmark]
    [BenchmarkCategory("GenericString")]
    public int GenericConversion() => Conv.To<int>(_genericInput);

    /// <summary>
    /// 测量已预先装箱数值直接传入转换器的路径。
    /// </summary>
    /// <returns>转换后的整数。</returns>
    [Benchmark]
    [BenchmarkCategory("Boxing")]
    public int PreboxedNumericConversion() => Conv.ToInt(_preboxedNumericInput);

    /// <summary>
    /// 测量调用处将 int 字段传给 object 参数的路径。
    /// </summary>
    /// <returns>转换后的整数。</returns>
    [Benchmark]
    [BenchmarkCategory("Boxing")]
    public int CallerBoxingNumericConversion() => Conv.ToInt(_callerBoxingNumericInput);

    /// <summary>
    /// 测量字符串到 int 的自定义转换器路径。
    /// </summary>
    /// <returns>转换后的整数。</returns>
    [Benchmark]
    [BenchmarkCategory("Custom")]
    public int CustomConverterConversion() => _customConverter.To<int>(_customInput);

    /// <summary>
    /// 测量 JsonElement 到对象的 JSON 转换。
    /// </summary>
    /// <returns>转换后记录的标识。</returns>
    [Benchmark]
    [BenchmarkCategory("Json")]
    public int JsonConversion() => Conv.To<ProbeRecord>(_jsonElement).Id;

    /// <summary>
    /// 测量对象到字典的转换。
    /// </summary>
    /// <returns>转换后字典的项数。</returns>
    [Benchmark]
    [BenchmarkCategory("Collection")]
    public int DictionaryConversion() => Conv.ToDictionary(_record).Count;

    /// <summary>
    /// 测量逗号分隔字符串到列表的转换。
    /// </summary>
    /// <returns>转换后列表的项数。</returns>
    [Benchmark]
    [BenchmarkCategory("Collection")]
    public int ListConversion() => Conv.ToList<int>(_listInput).Count;

    /// <summary>
    /// 测量非法字符串到 int 的失败转换路径。
    /// </summary>
    /// <returns>转换失败时返回默认整数值。</returns>
    [Benchmark]
    [BenchmarkCategory("Failure")]
    public int FailureConversion() => Conv.To<int>(_failureInput);

    /// <summary>
    /// 测量泛型字符串到长整数的转换。
    /// </summary>
    /// <returns>转换后的长整数。</returns>
    [Benchmark]
    [BenchmarkCategory("GenericString")]
    public long GenericLongConversion() => Conv.To<long>(_longInput);

    /// <summary>
    /// 测量泛型字符串到浮点数的转换。
    /// </summary>
    /// <returns>转换后的双精度浮点数。</returns>
    [Benchmark]
    [BenchmarkCategory("GenericString")]
    public double GenericDoubleConversion() => Conv.To<double>(_doubleInput);

    /// <summary>
    /// 测量泛型字符串到十进制数的转换。
    /// </summary>
    /// <returns>转换后的十进制数。</returns>
    [Benchmark]
    [BenchmarkCategory("GenericString")]
    public decimal GenericDecimalConversion() => Conv.To<decimal>(_decimalInput);

    /// <summary>
    /// 测量泛型字符串到布尔值的转换。
    /// </summary>
    /// <returns>转换后的布尔值。</returns>
    [Benchmark]
    [BenchmarkCategory("GenericString")]
    public bool GenericBoolConversion() => Conv.To<bool>(_boolInput);

    /// <summary>
    /// 测量泛型字符串到可空长整数的转换。
    /// </summary>
    /// <returns>转换后的长整数；失败时返回 null。</returns>
    [Benchmark]
    [BenchmarkCategory("GenericNullable")]
    public long? GenericNullableLongConversion() => Conv.To<long?>(_longInput);

    /// <summary>
    /// 对照可空长整数的直接解析与装箱转换。
    /// </summary>
    /// <returns>解析后的长整数；解析失败时返回 null。</returns>
    [Benchmark]
    [BenchmarkCategory("GenericNullable")]
    public long? NullableLongParseCastControl()
    {
        if (!long.TryParse(_longInput, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            return null;
        return (long?)(object)value;
    }

    /// <summary>
    /// 对照旧的可空长整数 Convert.ChangeType 路径。
    /// </summary>
    /// <returns>转换后的长整数。</returns>
    [Benchmark]
    [BenchmarkCategory("GenericNullable")]
    public long? NullableLongChangeTypeControl()
    {
        var target = Common.GetType<long?>();
        return (long?)System.Convert.ChangeType(_longInput, target, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 对照可空目标的类型解析开销。
    /// </summary>
    /// <returns>目标类型为长整数时返回 1，否则返回 0。</returns>
    [Benchmark]
    [BenchmarkCategory("GenericNullable")]
    public int NullableLongTypeControl() => Common.GetType<long?>() == typeof(long) ? 1 : 0;

    /// <summary>
    /// 测量泛型同类型长整数转换。
    /// </summary>
    /// <returns>转换后的长整数。</returns>
    [Benchmark]
    [BenchmarkCategory("GenericSameType")]
    public long GenericLongSameType() => Conv.To<long>(_preboxedLongInput);

    /// <summary>
    /// 测量泛型同类型浮点数转换。
    /// </summary>
    /// <returns>转换后的双精度浮点数。</returns>
    [Benchmark]
    [BenchmarkCategory("GenericSameType")]
    public double GenericDoubleSameType() => Conv.To<double>(_preboxedDoubleInput);

    /// <summary>
    /// 测量泛型同类型十进制数转换。
    /// </summary>
    /// <returns>转换后的十进制数。</returns>
    [Benchmark]
    [BenchmarkCategory("GenericSameType")]
    public decimal GenericDecimalSameType() => Conv.To<decimal>(_preboxedDecimalInput);

    /// <summary>
    /// 测量泛型同类型布尔值转换。
    /// </summary>
    /// <returns>转换后的布尔值。</returns>
    [Benchmark]
    [BenchmarkCategory("GenericSameType")]
    public bool GenericBoolSameType() => Conv.To<bool>(_preboxedBoolInput);

    /// <summary>
    /// 测量浮点数到整数的专用转换。
    /// </summary>
    /// <returns>转换后的整数。</returns>
    [Benchmark]
    [BenchmarkCategory("NumericCrossType")]
    public int NumericDoubleToInt() => Conv.ToInt(_preboxedDoubleInput);

    /// <summary>
    /// 测量 Guid 同类型转换。
    /// </summary>
    /// <returns>转换后的 Guid。</returns>
    [Benchmark]
    [BenchmarkCategory("GenericSameType")]
    public Guid GuidSameType() => Conv.To<Guid>(_guidInput);

    /// <summary>
    /// 测量日期同类型转换。
    /// </summary>
    /// <returns>转换后的日期。</returns>
    [Benchmark]
    [BenchmarkCategory("GenericSameType")]
    public DateTime DateSameType() => Conv.To<DateTime>(_dateInput);

    /// <summary>
    /// 测量非法长整数的转换。
    /// </summary>
    /// <returns>转换失败时返回默认长整数值。</returns>
    [Benchmark]
    [BenchmarkCategory("Failure")]
    public long LongFailure() => Conv.To<long>(_failureInput);

    /// <summary>
    /// 测量类型化同类型整数转换。
    /// </summary>
    /// <returns>转换后的整数。</returns>
    [Benchmark]
    [BenchmarkCategory("Boxing")]
    public int TypedSameTypeInt() => Conv.To<int, int>(_typedNumericInput);

    /// <summary>
    /// 测量旧入口的预装箱整数自定义转换。
    /// </summary>
    /// <returns>转换后的长整数。</returns>
    [Benchmark]
    [BenchmarkCategory("TypedCustom")]
    public long PreboxedCustomIntToLong() => _typedCustomConverter.To<long>(_preboxedNumericInput);

    /// <summary>
    /// 测量旧入口的调用端装箱整数自定义转换。
    /// </summary>
    /// <returns>转换后的长整数。</returns>
    [Benchmark]
    [BenchmarkCategory("TypedCustom")]
    public long CallerBoxingCustomIntToLong() => _typedCustomConverter.To<long>(_typedNumericInput);

    /// <summary>
    /// 测量类型化整数自定义转换。
    /// </summary>
    /// <returns>转换后的长整数。</returns>
    [Benchmark]
    [BenchmarkCategory("TypedCustom")]
    public long TypedCustomIntToLong() => _typedCustomConverter.To<int, long>(_typedNumericInput);

    /// <summary>
    /// 测量旧入口的预装箱 JSON 元素转换。
    /// </summary>
    /// <returns>转换后记录的标识。</returns>
    [Benchmark]
    [BenchmarkCategory("Json")]
    public int PreboxedJsonConversion() => Conv.To<ProbeRecord>(_preboxedJsonElement).Id;

    /// <summary>
    /// 测量类型化 JSON 元素转换。
    /// </summary>
    /// <returns>转换后记录的标识。</returns>
    [Benchmark]
    [BenchmarkCategory("Json")]
    public int TypedJsonConversion() => Conv.To<JsonElement, ProbeRecord>(_jsonElement).Id;

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
        Measure("GenericLongConversion", () => benchmark.GenericLongConversion().GetHashCode());
        Measure("GenericDoubleConversion", () => benchmark.GenericDoubleConversion().GetHashCode());
        Measure("GenericDecimalConversion", () => benchmark.GenericDecimalConversion().GetHashCode());
        Measure("GenericBoolConversion", () => benchmark.GenericBoolConversion() ? 1 : 0);
        Measure("GenericNullableLongConversion", () => benchmark.GenericNullableLongConversion().GetHashCode());
        Measure("NullableLongParseCastControl", () => benchmark.NullableLongParseCastControl().GetHashCode());
        Measure("NullableLongChangeTypeControl", () => benchmark.NullableLongChangeTypeControl().GetHashCode());
        Measure("NullableLongTypeControl", benchmark.NullableLongTypeControl);
        Measure("NumericDoubleToInt", benchmark.NumericDoubleToInt);
        Measure("TypedSameTypeInt", benchmark.TypedSameTypeInt);
        Measure("PreboxedCustomIntToLong", () => benchmark.PreboxedCustomIntToLong().GetHashCode());
        Measure("CallerBoxingCustomIntToLong", () => benchmark.CallerBoxingCustomIntToLong().GetHashCode());
        Measure("TypedCustomIntToLong", () => benchmark.TypedCustomIntToLong().GetHashCode());
        Measure("PreboxedJsonConversion", benchmark.PreboxedJsonConversion);
        Measure("TypedJsonConversion", benchmark.TypedJsonConversion);
    }

    /// <summary>
    /// 记录单进程内各转换路径的首次调用开销。
    /// </summary>
    /// <param name="scenario">单独测量的场景名称；为空时按顺序测量所有场景。</param>
    /// <remarks>
    /// 使用独立进程选择一个场景，可避免前一个场景的共享初始化影响。
    /// </remarks>
    public static void RunFirstCallProbe(string? scenario = null)
    {
        using var benchmark = new ConvBenchmarks();
        var scenarios = new (string Name, Func<int> Operation)[]
        {
            ("JsonConversion", benchmark.JsonConversion),
            ("GenericLongConversion", () => benchmark.GenericLongConversion().GetHashCode()),
            ("NumericConversion", benchmark.NumericConversion),
            ("TypedSameTypeInt", benchmark.TypedSameTypeInt),
            ("CustomConverterConversion", benchmark.CustomConverterConversion),
            ("DictionaryConversion", benchmark.DictionaryConversion),
            ("ListConversion", benchmark.ListConversion)
        };
        if (scenario != null && !scenarios.Any(item => item.Name == scenario))
            throw new ArgumentException("未知的首次调用场景。", nameof(scenario));
        Console.WriteLine($"Conv first call; candidate={GetCandidateIdentity()}; runtime={Environment.Version}; setup=benchmark-constructor-excluded; scenario={scenario ?? "all-sequential"}");
        foreach (var item in scenarios)
        {
            if (scenario == null || item.Name == scenario)
                MeasureFirstCall(item.Name, item.Operation);
        }
    }

    /// <summary>
    /// 测量一个路径的第一次调用。
    /// </summary>
    private static void MeasureFirstCall(string name, Func<int> operation)
    {
        var stopwatch = new Stopwatch();
        var beforeAllocated = GC.GetAllocatedBytesForCurrentThread();
        stopwatch.Start();
        var value = operation();
        stopwatch.Stop();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - beforeAllocated;
        Console.WriteLine($"{name}: elapsedMs={stopwatch.Elapsed.TotalMilliseconds:F3}; allocatedBytes={allocated}; value={value}");
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
    /// 记录连续转换期间的自然 GC 与回收后存活内存。
    /// </summary>
    public static void RunSustainedGcProbe(bool listOnly = false)
    {
        using var benchmark = new ConvBenchmarks();
        Console.WriteLine($"Conv sustained GC; commit={GetCommitIdentity()}; candidate={GetCandidateIdentity()}; runtime={Environment.Version}; serverGC={GCSettings.IsServerGC}; latencyMode={GCSettings.LatencyMode}; durationSeconds=10");
        if (listOnly)
        {
            MeasureSustainedGc("ListConversion", benchmark.ListConversion);
            return;
        }
        MeasureSustainedGc("PreboxedNumericConversion", benchmark.PreboxedNumericConversion);
        MeasureSustainedGc("CallerBoxingNumericConversion", benchmark.CallerBoxingNumericConversion);
        MeasureSustainedGc("JsonConversion", benchmark.JsonConversion);
        MeasureSustainedGc("DictionaryConversion", benchmark.DictionaryConversion);
        MeasureSustainedGc("ListConversion", benchmark.ListConversion);
    }

    /// <summary>
    /// 测量单个场景的连续分配和暂停。
    /// </summary>
    private static void MeasureSustainedGc(string name, Func<int> operation)
    {
        for (var i = 0; i < ProbeWarmupIterations; i++)
            _ = operation();
        using var pauses = new GcPauseListener();
        ForceFullCollection();

        var stopwatch = new Stopwatch();
        var beforeAllocated = GC.GetTotalAllocatedBytes(precise: true);
        var beforeGen0 = GC.CollectionCount(0);
        var beforeGen1 = GC.CollectionCount(1);
        var beforeGen2 = GC.CollectionCount(2);
        var beforeLive = GC.GetTotalMemory(false);
#if NET7_0_OR_GREATER
        var beforeGcPause = GC.GetTotalPauseDuration();
#endif
        var checksum = 0;
        var count = 0L;
        pauses.Start();
        stopwatch.Start();
        do
        {
            checksum ^= operation();
            count++;
        } while (stopwatch.Elapsed < TimeSpan.FromSeconds(10));
        stopwatch.Stop();
        pauses.Stop();

        var allocated = GC.GetTotalAllocatedBytes(precise: true) - beforeAllocated;
        var naturalLive = GC.GetTotalMemory(false);
        var lastGcHeap = GC.GetGCMemoryInfo().HeapSizeBytes;
        var gen0 = GC.CollectionCount(0) - beforeGen0;
        var gen1 = GC.CollectionCount(1) - beforeGen1;
        var gen2 = GC.CollectionCount(2) - beforeGen2;
#if NET7_0_OR_GREATER
        var gcPause = GC.GetTotalPauseDuration() - beforeGcPause;
#endif
        ForceFullCollection();
        var forcedLive = GC.GetTotalMemory(false);
        var pauseSnapshot = pauses.Snapshot();
#if NET7_0_OR_GREATER
        var gcPauseText = $"; gcPauseRuntimeMs={gcPause.TotalMilliseconds:F3}";
#else
        var gcPauseText = "; gcPauseRuntimeMs=unavailable-net6";
#endif
        Console.WriteLine($"{name}: durationSeconds={stopwatch.Elapsed.TotalSeconds:F3}; iterations={count}; allocatedBytes={allocated}; bytesPerOperation={(double)allocated / count:F2}; allocationBytesPerSecond={allocated / stopwatch.Elapsed.TotalSeconds:F0}; beforeLiveBytes={beforeLive}; naturalLiveBytes={naturalLive}; lastGcHeapBytes={lastGcHeap}; forcedLiveBytes={forcedLive}; naturalGen0={gen0}; naturalGen1={gen1}; naturalGen2={gen2}; eventsDrained={pauseSnapshot.Drained}; gcSuspensionEvents={pauseSnapshot.Count}; suspensionTotalMs={pauseSnapshot.Total.TotalMilliseconds:F3}; suspensionMaxMs={pauseSnapshot.Maximum.TotalMilliseconds:F3}{gcPauseText}; checksum={checksum}");
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

        var stopwatch = new Stopwatch();
        var beforeAllocated = GC.GetAllocatedBytesForCurrentThread();
        var beforeGen0 = GC.CollectionCount(0);
        var beforeGen1 = GC.CollectionCount(1);
        var beforeGen2 = GC.CollectionCount(2);
        var checksum = 0;
        stopwatch.Start();
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
        var stopwatch = new Stopwatch();
        var beforeAllocated = GC.GetAllocatedBytesForCurrentThread();
        var beforeGen0 = GC.CollectionCount(0);
        var beforeGen1 = GC.CollectionCount(1);
        var beforeGen2 = GC.CollectionCount(2);
        var checksum = 0;
        stopwatch.Start();
        for (var i = 0; i < ProbeIterations; i++)
            checksum ^= operation();
        stopwatch.Stop();
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
    /// <returns>旧算法生成的字典项数。</returns>
    private int LegacyDictionaryConversion() => LegacyToDictionary(_record).Count;

    /// <summary>
    /// 测量旧列表转换算法。
    /// </summary>
    /// <returns>旧算法生成的列表项数。</returns>
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
    /// 将整数转换成长整数。
    /// </summary>
    /// <param name="input">待转换的整数。</param>
    /// <param name="result">转换后的长整数。</param>
    /// <returns>始终返回 true。</returns>
    private static bool TryConvertIntToLong(int input, out long result)
    {
        result = input;
        return true;
    }

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
