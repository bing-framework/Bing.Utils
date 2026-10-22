using System.Globalization;
using BenchmarkDotNet.Attributes;
using Bing.Helpers;

namespace Bing.Utils.Benchmark.Benchmarks;

/// <summary>
/// 比较日期文本转换与 ChangeType 控制路径。
/// </summary>
/// <remarks>
/// 两条路径均采用 invariant 区域性，并在格式错误时返回默认日期。
/// 控制路径用于隔离转换成本，不代表历史完整 Conv 实现。
/// </remarks>
[MemoryDiagnoser]
public class ConvDateParsingBenchmarks
{
    /// <summary>
    /// 当前测量共用的有效或无效日期文本。
    /// </summary>
    private object _input = string.Empty;

    /// <summary>
    /// 获取或设置是否使用格式错误的日期文本。
    /// </summary>
    [Params(false, true)]
    public bool Invalid { get; set; }

    /// <summary>
    /// 准备日期文本。
    /// </summary>
    /// <remarks>
    /// 初始化后验证当前路径与控制路径的结果一致。
    /// </remarks>
    [GlobalSetup]
    public void Setup()
    {
        _input = Invalid ? "not-a-date" : "2024-01-02T03:04:05";
        var current = GenericConversion();
        if (current != ChangeTypeControl() || Invalid != (current == default))
            throw new InvalidOperationException("日期转换基准输入未满足预期契约。");
    }

    /// <summary>
    /// 测量泛型日期转换。
    /// </summary>
    /// <returns>解析日期；格式错误时返回默认日期。</returns>
    [Benchmark]
    public DateTime GenericConversion() => Conv.To<DateTime>(_input);

    /// <summary>
    /// 测量可空日期转换。
    /// </summary>
    /// <returns>解析日期；格式错误时返回空值。</returns>
    [Benchmark]
    public DateTime? NullableGenericConversion() => Conv.To<DateTime?>(_input);

    /// <summary>
    /// 测量 ChangeType 日期转换。
    /// </summary>
    /// <returns>解析日期；格式错误时返回默认日期。</returns>
    [Benchmark(Baseline = true)]
    public DateTime ChangeTypeControl()
    {
        try
        {
            return (DateTime)Convert.ChangeType(_input, typeof(DateTime), CultureInfo.InvariantCulture);
        }
        catch (FormatException)
        {
            return default;
        }
    }

    /// <summary>
    /// 测量旧可空日期的 ChangeType 方法体。
    /// </summary>
    /// <returns>解析日期；格式错误时返回空值。</returns>
    [Benchmark]
    public DateTime? NullableChangeTypeControl()
    {
        try
        {
            return (DateTime?)Convert.ChangeType(_input, typeof(DateTime), CultureInfo.InvariantCulture);
        }
        catch (FormatException)
        {
            return default;
        }
    }
}
