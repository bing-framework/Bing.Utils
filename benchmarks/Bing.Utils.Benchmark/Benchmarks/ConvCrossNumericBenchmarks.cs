using System.Globalization;
using BenchmarkDotNet.Attributes;
using Bing.Helpers;

namespace Bing.Utils.Benchmark.Benchmarks;

/// <summary>
/// 比较泛型跨数值类型转换的中间分配。
/// </summary>
/// <remarks>
/// 输入在数据准备阶段预装箱，以隔离结果装箱和失败异常成本。
/// 成功与越界失败分别测量；控制路径与泛型入口均使用 invariant 区域性并在失败时返回零。
/// </remarks>
[MemoryDiagnoser]
public class ConvCrossNumericBenchmarks
{
    /// <summary>
    /// 本次测量共用的预装箱数值输入。
    /// </summary>
    private object _input = 12345L;

    /// <summary>
    /// 获取或设置源数值类型名称。
    /// </summary>
    [Params("long", "double", "decimal")]
    public string Source { get; set; } = "long";

    /// <summary>
    /// 获取或设置是否使用超出 int 范围的输入。
    /// </summary>
    [Params(false, true)]
    public bool Invalid { get; set; }

    /// <summary>
    /// 准备成功或越界失败输入。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _input = Source switch
        {
            "long" => (object)(Invalid ? long.MaxValue : 12345L),
            "double" => (object)(Invalid ? double.MaxValue : 12345.625d),
            "decimal" => (object)(Invalid ? decimal.MaxValue : 12345.625m),
            _ => throw new NotSupportedException($"未配置数值类型：{Source}")
        };
    }

    /// <summary>
    /// 测量泛型跨数值转换。
    /// </summary>
    /// <returns>转换后的整数；越界时返回零。</returns>
    [Benchmark]
    public int GenericConversion() => Conv.To<int>(_input);

    /// <summary>
    /// 测量 ChangeType 的同语义控制路径。
    /// </summary>
    /// <returns>转换后的整数；越界时返回零。</returns>
    /// <remarks>
    /// 只隔离 ChangeType 转换成本，不代表历史完整 Conv 方法体。
    /// </remarks>
    [Benchmark(Baseline = true)]
    public int ChangeTypeControl()
    {
        try
        {
            return (int)Convert.ChangeType(_input, typeof(int), CultureInfo.InvariantCulture);
        }
        catch (OverflowException)
        {
            return 0;
        }
    }
}
