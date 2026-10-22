using BenchmarkDotNet.Attributes;
using Bing.Helpers;

namespace Bing.Utils.Benchmark.Benchmarks;

/// <summary>
/// 测量失败转换和集合结果的分配。
/// </summary>
[MemoryDiagnoser]
public class ConvAllocationBenchmarks
{
    /// <summary>
    /// 测量枚举成功转换。
    /// </summary>
    /// <returns>转换后的枚举值。</returns>
    [Benchmark]
    public DayOfWeek EnumSuccess() => Conv.To<DayOfWeek>("Monday");
    /// <summary>
    /// 测量枚举失败转换。
    /// </summary>
    /// <returns>转换失败时返回默认枚举值。</returns>
    [Benchmark]
    public DayOfWeek EnumFailure() => Conv.To<DayOfWeek>("invalid");
    /// <summary>
    /// 测量 Guid 失败转换。
    /// </summary>
    /// <returns>转换失败时返回空 Guid。</returns>
    [Benchmark]
    public Guid GuidFailure() => Conv.To<Guid>("invalid");
    /// <summary>
    /// 测量列表拆分产生的中间分配。
    /// </summary>
    /// <returns>拆分后的字符串数组。</returns>
    [Benchmark]
    public string[] ListSplitControl() => "1,2,3,4,5,6,7,8,9,10".Split(',');
    /// <summary>
    /// 测量列表结果的存储分配。
    /// </summary>
    /// <returns>包含固定整数的列表。</returns>
    [Benchmark]
    public List<int> ListResultControl() => new(10) { 1,2,3,4,5,6,7,8,9,10 };
    /// <summary>
    /// 测量字典结果和装箱值的分配。
    /// </summary>
    /// <returns>包含固定属性的字典。</returns>
    [Benchmark]
    public Dictionary<string, object> DictionaryResultControl() => new(3) { ["Id"] = 42, ["Name"] = "baseline", ["Enabled"] = true };
}
