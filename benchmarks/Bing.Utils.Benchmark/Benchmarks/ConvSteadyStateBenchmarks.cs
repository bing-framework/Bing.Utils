using System.Diagnostics;
using System.Text;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using Bing.Helpers;

namespace Bing.Utils.Benchmark.Benchmarks;

/// <summary>
/// 交错测量稳态字符串到长整数的转换路径。
/// </summary>
/// <remarks>
/// 基准进程启动后先完成固定次数的两条路径预热，再按样本交错执行当前实现和旧
/// <c>Convert.ChangeType</c> 方法体。逐样本结果由清理阶段写入统一证据目录。
/// </remarks>
[MemoryDiagnoser]
public sealed class ConvSteadyStateBenchmarks
{
    /// <summary>
    /// 预热阶段每条路径执行的次数。
    /// </summary>
    private const int WarmupOperations = 50_000;

    /// <summary>
    /// 每个稳态样本包含的转换次数。
    /// </summary>
    private const int OperationsPerSample = 10_000;

    /// <summary>
    /// 稳态测量样本数。
    /// </summary>
    private const int SampleCount = 20;

    /// <summary>
    /// 与历史基准保持一致的固定字符串输入。
    /// </summary>
    private readonly object _input = "12345";

    /// <summary>
    /// 防止基准循环被优化掉的校验值。
    /// </summary>
    private long _checksum;

    /// <summary>
    /// 保存当前基准调用产生的逐样本结果。
    /// </summary>
    private readonly List<SteadyStateSample> _samples = new(SampleCount);

    /// <summary>
    /// 准备稳态测量环境。
    /// </summary>
    /// <remarks>
    /// 预热两条路径后执行完整回收，为测量建立一致起点。
    /// </remarks>
    [GlobalSetup]
    public void Setup()
    {
        for (var index = 0; index < WarmupOperations; index++)
        {
            _checksum ^= Conv.To<long>(_input);
            _checksum ^= LegacyBody();
        }

        ForceFullCollection();
        _checksum = 0;
    }

    /// <summary>
    /// 交错测量当前实现和旧方法体的稳态表现。
    /// </summary>
    /// <returns>两条路径校验值的合并结果。</returns>
    [Benchmark]
    public long InterleavedSteadyState()
    {
        _samples.Clear();
        _checksum = 0;

        for (var sampleIndex = 0; sampleIndex < SampleCount; sampleIndex++)
        {
            // 交替首个路径，降低单调运行期间频率变化和 GC 时序的影响。
            var currentFirst = sampleIndex % 2 == 0;
            var current = currentFirst ? MeasureCurrent() : default;
            var legacy = currentFirst ? MeasureLegacy() : default;
            if (!currentFirst)
            {
                legacy = MeasureLegacy();
                current = MeasureCurrent();
            }

            _samples.Add(new SteadyStateSample(
                sampleIndex + 1,
                currentFirst ? "current-first" : "legacy-first",
                current,
                legacy));
        }

        GC.KeepAlive(_input);
        return _checksum;
    }

    /// <summary>
    /// 将逐样本稳态数据写入证据目录。
    /// </summary>
    [GlobalCleanup]
    public void WriteEvidence()
    {
        var directory = Environment.GetEnvironmentVariable("CONV_STEADY_EVIDENCE_DIR");
        if (string.IsNullOrWhiteSpace(directory))
        {
            var evidenceRoot = Environment.GetEnvironmentVariable("CONV_EVIDENCE_ROOT");
            if (string.IsNullOrWhiteSpace(evidenceRoot))
            {
                evidenceRoot = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Bing.Utils",
                    "Evidence");
            }
            directory = Path.Combine(
                evidenceRoot,
                "conv-steady-state",
                $"{Environment.Version}-{DateTime.UtcNow:yyyyMMdd-HHmmss}");
        }

        Directory.CreateDirectory(directory);
        var evidence = new SteadyStateEvidence(
            DateTime.UtcNow,
            Environment.Version.ToString(),
            Environment.OSVersion.ToString(),
            Process.GetCurrentProcess().Id,
            Process.GetCurrentProcess().StartTime.ToUniversalTime(),
            Environment.GetEnvironmentVariable("CONV_CANDIDATE_ID") ?? "working-tree",
            Environment.GetEnvironmentVariable("CONV_SOURCE_SHA256") ?? "unknown",
            Environment.GetEnvironmentVariable("CONV_PRODUCTION_DLL_SHA256") ?? "unknown",
            Environment.GetEnvironmentVariable("CONV_HARNESS_DLL_SHA256") ?? "unknown",
            WarmupOperations,
            OperationsPerSample,
            SampleCount,
            _checksum,
            _samples);

        var jsonPath = Path.Combine(directory, "steady-state.json");
        var csvPath = Path.Combine(directory, "steady-state.csv");
        var json = JsonSerializer.Serialize(evidence, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(jsonPath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        var csv = new StringBuilder();
        csv.AppendLine("sample,order,currentElapsedNanoseconds,currentAllocatedBytes,currentGen0,currentGen1,currentGen2,currentChecksum,legacyElapsedNanoseconds,legacyAllocatedBytes,legacyGen0,legacyGen1,legacyGen2,legacyChecksum");
        foreach (var sample in _samples)
        {
            csv.Append(sample.Index).Append(',')
                .Append(sample.Order).Append(',')
                .Append(sample.Current.ElapsedNanoseconds.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)).Append(',')
                .Append(sample.Current.AllocatedBytes).Append(',')
                .Append(sample.Current.Gen0).Append(',')
                .Append(sample.Current.Gen1).Append(',')
                .Append(sample.Current.Gen2).Append(',')
                .Append(sample.Current.Checksum).Append(',')
                .Append(sample.Legacy.ElapsedNanoseconds.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)).Append(',')
                .Append(sample.Legacy.AllocatedBytes).Append(',')
                .Append(sample.Legacy.Gen0).Append(',')
                .Append(sample.Legacy.Gen1).Append(',')
                .Append(sample.Legacy.Gen2).Append(',')
                .Append(sample.Legacy.Checksum)
                .AppendLine();
        }

        File.WriteAllText(csvPath, csv.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        Console.WriteLine($"Conv steady-state evidence: directory={directory}; processId={evidence.ProcessId}; samples={_samples.Count}; checksum={_checksum}");
    }

    /// <summary>
    /// 测量当前转换路径的一个样本。
    /// </summary>
    /// <returns>当前路径的耗时、分配、GC 和校验结果。</returns>
    private SteadyStateMeasurement MeasureCurrent()
    {
        var beforeAllocated = GC.GetAllocatedBytesForCurrentThread();
        var beforeGen0 = GC.CollectionCount(0);
        var beforeGen1 = GC.CollectionCount(1);
        var beforeGen2 = GC.CollectionCount(2);
        var start = Stopwatch.GetTimestamp();
        var checksum = 0L;
        for (var index = 0; index < OperationsPerSample; index++)
            checksum ^= Conv.To<long>(_input);
        var elapsedNanoseconds = (Stopwatch.GetTimestamp() - start) * (1_000_000_000d / Stopwatch.Frequency);

        _checksum ^= checksum;
        return new SteadyStateMeasurement(
            elapsedNanoseconds,
            GC.GetAllocatedBytesForCurrentThread() - beforeAllocated,
            GC.CollectionCount(0) - beforeGen0,
            GC.CollectionCount(1) - beforeGen1,
            GC.CollectionCount(2) - beforeGen2,
            checksum);
    }

    /// <summary>
    /// 测量旧转换方法体的一个样本。
    /// </summary>
    /// <returns>旧路径的耗时、分配、GC 和校验结果。</returns>
    private SteadyStateMeasurement MeasureLegacy()
    {
        var beforeAllocated = GC.GetAllocatedBytesForCurrentThread();
        var beforeGen0 = GC.CollectionCount(0);
        var beforeGen1 = GC.CollectionCount(1);
        var beforeGen2 = GC.CollectionCount(2);
        var start = Stopwatch.GetTimestamp();
        var checksum = 0L;
        for (var index = 0; index < OperationsPerSample; index++)
            checksum ^= LegacyBody();
        var elapsedNanoseconds = (Stopwatch.GetTimestamp() - start) * (1_000_000_000d / Stopwatch.Frequency);

        _checksum ^= checksum;
        return new SteadyStateMeasurement(
            elapsedNanoseconds,
            GC.GetAllocatedBytesForCurrentThread() - beforeAllocated,
            GC.CollectionCount(0) - beforeGen0,
            GC.CollectionCount(1) - beforeGen1,
            GC.CollectionCount(2) - beforeGen2,
            checksum);
    }

    /// <summary>
    /// 复现优化前的 invariant 长整数转换方法体。
    /// </summary>
    /// <returns>解析得到的长整数。</returns>
    private long LegacyBody()
    {
        try
        {
            return (long)Convert.ChangeType(_input, typeof(long), System.Globalization.CultureInfo.InvariantCulture);
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// 执行阻塞式完整回收，为稳态窗口建立一致起点。
    /// </summary>
    private static void ForceFullCollection()
    {
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
    }

    /// <summary>
    /// 保存一个转换路径的测量结果。
    /// </summary>
    /// <param name="ElapsedNanoseconds">经过的纳秒数。</param>
    /// <param name="AllocatedBytes">分配的字节数。</param>
    /// <param name="Gen0">第 0 代垃圾回收次数。</param>
    /// <param name="Gen1">第 1 代垃圾回收次数。</param>
    /// <param name="Gen2">第 2 代垃圾回收次数。</param>
    /// <param name="Checksum">本次测量的校验值。</param>
    private readonly record struct SteadyStateMeasurement(
        double ElapsedNanoseconds,
        long AllocatedBytes,
        int Gen0,
        int Gen1,
        int Gen2,
        long Checksum);

    /// <summary>
    /// 保存当前实现和旧实现的一组交错测量结果。
    /// </summary>
    /// <param name="Index">样本序号。</param>
    /// <param name="Order">本样本中两条路径的执行顺序。</param>
    /// <param name="Current">当前实现的测量结果。</param>
    /// <param name="Legacy">旧实现的测量结果。</param>
    private sealed record SteadyStateSample(
        int Index,
        string Order,
        SteadyStateMeasurement Current,
        SteadyStateMeasurement Legacy);

    /// <summary>
    /// 保存一次独立进程中的稳态证据元数据和样本。
    /// </summary>
    /// <param name="RecordedAtUtc">证据记录时间。</param>
    /// <param name="Runtime">运行时版本。</param>
    /// <param name="OperatingSystem">操作系统信息。</param>
    /// <param name="ProcessId">测量进程标识。</param>
    /// <param name="ProcessStartTimeUtc">测量进程启动时间。</param>
    /// <param name="CandidateId">当前候选实现标识。</param>
    /// <param name="SourceSha256">源代码 SHA-256 标识。</param>
    /// <param name="ProductionDllSha256">生产程序集 SHA-256 标识。</param>
    /// <param name="HarnessDllSha256">基准程序集 SHA-256 标识。</param>
    /// <param name="WarmupOperations">预热操作次数。</param>
    /// <param name="OperationsPerSample">每个样本的操作次数。</param>
    /// <param name="SampleCount">样本数量。</param>
    /// <param name="FinalChecksum">最终校验值。</param>
    /// <param name="Samples">逐样本测量结果。</param>
    private sealed record SteadyStateEvidence(
        DateTimeOffset RecordedAtUtc,
        string Runtime,
        string OperatingSystem,
        int ProcessId,
        DateTime ProcessStartTimeUtc,
        string CandidateId,
        string SourceSha256,
        string ProductionDllSha256,
        string HarnessDllSha256,
        int WarmupOperations,
        int OperationsPerSample,
        int SampleCount,
        long FinalChecksum,
        IReadOnlyList<SteadyStateSample> Samples);
}
