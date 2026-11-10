using BenchmarkDotNet.Attributes;
using Bing.Date.Calendars;

namespace Bing.Utils.Benchmark.Benchmarks;

/// <summary>
/// 比较相同年度和规则下逐日算法与年度索引的查询成本。
/// </summary>
/// <remarks>这是等价查询的实现对比，不是将新功能伪装成历史版本基线。</remarks>
[MemoryDiagnoser]
public class AnnualBusinessCalendarBenchmarks
{
    /// <summary>
    /// 通用逐日计算器。
    /// </summary>
    private BusinessCalendar _daily = null!;

    /// <summary>
    /// 可复用年度前缀索引。
    /// </summary>
    private AnnualBusinessCalendar _annual = null!;

    /// <summary>
    /// 固定查询起点。
    /// </summary>
    private readonly DateTime _start = new(2024, 1, 1);

    /// <summary>
    /// 获取或设置区间跨度。
    /// </summary>
    [Params(7, 180, 366)]
    public int Days { get; set; }

    /// <summary>
    /// 构建相同规则，并在测量前验证两种实现的结果一致。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        var holidays = new[] { new DateTime(2024, 1, 1), new DateTime(2024, 2, 29) };
        var overrides = new[] { new DateTime(2024, 1, 6) };
        _daily = new BusinessCalendar(holidays: holidays, workingDayOverrides: overrides);
        _annual = new AnnualBusinessCalendar(2024, "demo", "v1", "benchmark", holidays, overrides);
        if (DailyCount() != IndexedCount()) throw new InvalidOperationException("计数契约不一致。");
    }

    /// <summary>
    /// 逐日计算相同区间的业务日数。
    /// </summary>
    /// <returns>业务日数。</returns>
    [Benchmark(Baseline = true)]
    public int DailyCount() => _daily.CountBusinessDays(_start, _start.AddDays(Days));

    /// <summary>
    /// 通过索引计算相同区间的业务日数。
    /// </summary>
    /// <returns>业务日数。</returns>
    [Benchmark]
    public int IndexedCount() => _annual.CountBusinessDays(_start, _start.AddDays(Days));
}
