using BenchmarkDotNet.Attributes;
using Bing.Helpers;

namespace Bing.Utils.Benchmark.Benchmarks;

/// <summary>
/// 比较整数到专用浮点和十进制入口的当前与文本路径。
/// </summary>
[MemoryDiagnoser]
public class ConvDedicatedFloatingBenchmarks
{
    /// <summary>
    /// 选择整数源类型。
    /// </summary>
    [Params("Int32", "Int64", "UInt64")]
    public string Source { get; set; } = "Int32";

    /// <summary>
    /// 测量用的预装箱输入。
    /// </summary>
    private object _input = default!;

    /// <summary>
    /// 初始化测量输入。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _input = Source switch
        {
            "Int32" => 16_777_217,
            "Int64" => long.MaxValue,
            "UInt64" => ulong.MaxValue,
            _ => throw new InvalidOperationException(Source)
        };
        if (CurrentFloat() != TextFloat() || CurrentDouble() != TextDouble() ||
            CurrentDecimal() != TextDecimal())
            throw new InvalidOperationException("测量路径的转换结果不一致。");
    }

    /// <summary>
    /// 测量当前单精度转换。
    /// </summary>
    /// <returns>转换结果。</returns>
    [Benchmark]
    public float? CurrentFloat() => Conv.ToFloatOrNull(_input);

    /// <summary>
    /// 测量原有文本路径的单精度转换。
    /// </summary>
    /// <returns>转换结果。</returns>
    [Benchmark]
    public float? TextFloat() => float.TryParse(_input.ToString(), out var value) ? value : null;

    /// <summary>
    /// 测量当前双精度转换。
    /// </summary>
    /// <returns>转换结果。</returns>
    [Benchmark]
    public double? CurrentDouble() => Conv.ToDoubleOrNull(_input);

    /// <summary>
    /// 测量原有文本路径的双精度转换。
    /// </summary>
    /// <returns>转换结果。</returns>
    [Benchmark]
    public double? TextDouble() => double.TryParse(_input.ToString(), out var value) ? value : null;

    /// <summary>
    /// 测量当前十进制转换。
    /// </summary>
    /// <returns>转换结果。</returns>
    [Benchmark]
    public decimal? CurrentDecimal() => Conv.ToDecimalOrNull(_input);

    /// <summary>
    /// 测量原有文本路径的十进制转换。
    /// </summary>
    /// <returns>转换结果。</returns>
    [Benchmark]
    public decimal? TextDecimal() => decimal.TryParse(_input.ToString(), out var value) ? value : null;
}
