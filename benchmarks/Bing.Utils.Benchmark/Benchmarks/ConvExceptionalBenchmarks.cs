using System.ComponentModel;
using BenchmarkDotNet.Attributes;
using Bing.Helpers;

namespace Bing.Utils.Benchmark.Benchmarks;

/// <summary>
/// 比较 Guid 和枚举的成功及失败转换成本。
/// </summary>
/// <remarks>
/// 控制方法复现原有的类型转换器和 Enum.Parse 路径。
/// </remarks>
[MemoryDiagnoser]
public class ConvExceptionalBenchmarks
{
    /// <summary>
    /// 获取或设置是否使用无效字符串。
    /// </summary>
    [Params(false, true)]
    public bool Invalid { get; set; }

    /// <summary>
    /// 测量当前 Guid 转换。
    /// </summary>
    /// <returns>解析结果；失败时为空 Guid。</returns>
    [Benchmark]
    public Guid CurrentGuid() => Conv.To<Guid>(Invalid ? "invalid" : "f94d5b93-4c4b-4fca-bf97-a306db4257c5");

    /// <summary>
    /// 测量旧 Guid 类型转换器方法体。
    /// </summary>
    /// <returns>解析结果；失败时为空 Guid。</returns>
    [Benchmark]
    public Guid LegacyGuid()
    {
        try
        {
            return (Guid)TypeDescriptor.GetConverter(typeof(Guid))
                .ConvertFromInvariantString(Invalid ? "invalid" : "f94d5b93-4c4b-4fca-bf97-a306db4257c5")!;
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// 测量当前枚举转换。
    /// </summary>
    /// <returns>解析结果；失败时为默认枚举值。</returns>
    [Benchmark]
    public DayOfWeek CurrentEnum() => Conv.To<DayOfWeek>(Invalid ? "invalid" : "Monday");

    /// <summary>
    /// 测量旧 Enum.Parse 方法体。
    /// </summary>
    /// <returns>解析结果；失败时为默认枚举值。</returns>
    [Benchmark]
    public DayOfWeek LegacyEnum()
    {
        try
        {
            return (DayOfWeek)System.Enum.Parse(typeof(DayOfWeek), Invalid ? "invalid" : "Monday", true);
        }
        catch
        {
            return default;
        }
    }
}
