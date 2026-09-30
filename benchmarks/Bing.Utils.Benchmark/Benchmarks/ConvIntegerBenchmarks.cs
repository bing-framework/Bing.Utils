using BenchmarkDotNet.Attributes;
using Bing.Helpers;

namespace Bing.Utils.Benchmark.Benchmarks;

/// <summary>
/// 比较预装箱整数输入的专用转换方法体。
/// </summary>
[MemoryDiagnoser]
public class ConvIntegerBenchmarks
{
    /// <summary>
    /// 当前预装箱输入。
    /// </summary>
    private object _input = 12345L;
    /// <summary>
    /// 获取或设置输入类型名称。
    /// </summary>
    [ParamsSource(nameof(Sources))]
    public string Source { get; set; } = "long";
    /// <summary>
    /// 获取本次测量的源类型集合。
    /// </summary>
    /// <remarks>
    /// CONV_INTEGER_SOURCE 可将定向确认限制为一个类型。
    /// </remarks>
    public IEnumerable<string> Sources => Environment.GetEnvironmentVariable("CONV_INTEGER_SOURCE") is { Length: > 0 } selected
        ? new[] { selected } : new[] { "long", "double", "decimal" };
    /// <summary>
    /// 准备输入。
    /// </summary>
    [GlobalSetup]
    public void Setup() => _input = Source switch { "double" => (object)12345.625d, "decimal" => 12345.625m, _ => 12345L };
    /// <summary>
    /// 测量当前专用转换。
    /// </summary>
    /// <returns>转换结果；超出范围时返回 null。</returns>
    [Benchmark]
    public int? Current() => Conv.ToIntOrNull(_input);
    /// <summary>
    /// 测量旧文本转换方法体。
    /// </summary>
    /// <returns>转换结果；超出范围时返回 null。</returns>
    [Benchmark(Baseline = true)]
    public int? LegacyBody()
    {
        if (_input is int direct) return direct;
        var text = _input?.ToString()?.Trim() ?? string.Empty;
        if (int.TryParse(text, out var integer)) return integer;
        var rounded = _input is double floating ? Math.Round(floating, 0, MidpointRounding.AwayFromZero)
            : double.TryParse(text, out var value) ? Math.Round(value, 0, MidpointRounding.AwayFromZero) : double.NaN;
        return rounded is >= int.MinValue and <= int.MaxValue ? (int)rounded : null;
    }
}
