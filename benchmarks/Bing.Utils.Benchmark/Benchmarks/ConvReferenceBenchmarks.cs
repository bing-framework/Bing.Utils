using System.ComponentModel;
using BenchmarkDotNet.Attributes;
using Bing.Helpers;

namespace Bing.Utils.Benchmark.Benchmarks;

/// <summary>
/// 比较 JSON、自定义转换和集合的同语义路径。
/// </summary>
/// <remarks>
/// JSON、自定义场景比较当前预装箱入口与类型化入口；集合比较旧算法方法体。
/// 控制路径不是统一的历史完整版本。
/// </remarks>
[MemoryDiagnoser]
public class ConvReferenceBenchmarks : IDisposable
{
    /// <summary>
    /// 持有测量期间使用的 JSON 文档和转换器。
    /// </summary>
    private ConvBenchmarks _benchmark = null!;

    /// <summary>
    /// 在测量区外创建的固定三属性对象。
    /// </summary>
    private readonly ConvBenchmarks.ProbeRecord _record = new() { Id = 42, Name = "baseline", Enabled = true };

    /// <summary>
    /// 集合对照双方共用的十项输入。
    /// </summary>
    private const string ListInput = "1,2,3,4,5,6,7,8,9,10";

    /// <summary>
    /// 获取或设置测量场景。
    /// </summary>
    [Params("Json", "Custom", "Dictionary", "List")]
    public string Scenario { get; set; } = "Json";

    /// <summary>
    /// 准备测量场景。
    /// </summary>
    /// <remarks>
    /// 初始化后核对当前路径与控制路径的返回结果一致。
    /// </remarks>
    [GlobalSetup]
    public void Setup()
    {
        _benchmark = new ConvBenchmarks();
        if (Current() != Control())
            throw new InvalidOperationException("对照路径返回结果不一致。");
    }

    /// <summary>
    /// 测量当前转换路径。
    /// </summary>
    /// <returns>结果标识或集合项数。</returns>
    [Benchmark]
    public long Current() => Scenario switch
    {
        "Json" => _benchmark.TypedJsonConversion(),
        "Custom" => _benchmark.TypedCustomIntToLong(),
        "Dictionary" => Conv.ToDictionary(_record).Count,
        "List" => Conv.ToList<int>(ListInput).Count,
        _ => throw new InvalidOperationException("未知的测量场景。")
    };

    /// <summary>
    /// 测量同语义控制路径。
    /// </summary>
    /// <returns>结果标识或集合项数。</returns>
    [Benchmark(Baseline = true)]
    public long Control()
    {
        switch (Scenario)
        {
            case "Json":
                return _benchmark.PreboxedJsonConversion();
            case "Custom":
                return _benchmark.PreboxedCustomIntToLong();
            case "Dictionary":
                var dictionary = new Dictionary<string, object>();
                foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(_record))
                    dictionary.Add(property.Name, property.GetValue(_record)!);
                return dictionary.Count;
            case "List":
                var list = new List<int>();
                var items = ListInput.Split(',');
                list.AddRange(from item in items where !string.IsNullOrWhiteSpace(item) select Conv.To<int>(item));
                return list.Count;
            default:
                throw new InvalidOperationException("未知的测量场景。");
        }
    }

    /// <inheritdoc />
    [GlobalCleanup]
    public void Dispose() => _benchmark?.Dispose();
}
