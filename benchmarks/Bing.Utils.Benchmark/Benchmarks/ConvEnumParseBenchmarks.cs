using BenchmarkDotNet.Attributes;
using Bing.Helpers;

namespace Bing.Utils.Benchmark.Benchmarks;

/// <summary>
/// 比较枚举转换与泛型非异常解析的成本。
/// </summary>
[MemoryDiagnoser]
public class ConvEnumParseBenchmarks
{
    /// <summary>
    /// 选择枚举文本输入。
    /// </summary>
    [Params("Ready", "42", "invalid")]
    public string Input { get; set; } = "Ready";

    /// <summary>
    /// 测量当前泛型转换入口。
    /// </summary>
    /// <returns>转换是否成功。</returns>
    [Benchmark(Baseline = true)]
    public bool Current() => Conv.TryTo<SampleState>(Input, out _);

    /// <summary>
    /// 测量泛型枚举解析本身。
    /// </summary>
    /// <returns>解析是否成功。</returns>
    [Benchmark]
    public bool GenericTryParse() => System.Enum.TryParse<SampleState>(Input, true, out _);

    /// <summary>
    /// 测量原有枚举解析方法体。
    /// </summary>
    /// <returns>解析是否成功。</returns>
    [Benchmark]
    public bool LegacyEnumParse()
    {
        try
        {
            _ = Enums.Parse<SampleState>(Input);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 提供枚举基准的状态值。
    /// </summary>
    public enum SampleState
    {
        /// <summary>
        /// 就绪状态。
        /// </summary>
        Ready = 1
    }
}
