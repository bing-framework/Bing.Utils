using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Bing.Date.Chinese;

namespace Bing.Utils.Benchmark.Benchmarks;
/// <summary>
/// 比较相同日期字段输出的旧农历对象与新农历日期值。
/// </summary>
[MemoryDiagnoser]
public class LunarCalendarBenchmarks
{
    /// <summary>
    /// 旧、新入口共用的闰月日期。
    /// </summary>
    private readonly DateTime _date = new(2023, 3, 22);
    /// <summary>
    /// 确认两个入口在测量中提取相同的农历字段。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        if (LegacyParts() != ValueParts())
            throw new InvalidOperationException("农历字段不一致，不能比较。");
    }
    /// <summary>
    /// 构造旧农历对象并读取年份、BCL 月序号、日期和闰月标志。
    /// </summary>
    /// <returns>农历字段组合。</returns>
    [Benchmark(Baseline = true)]
    public int LegacyParts()
    {
        var info = new ChineseDateInfo(_date);
        return Combine(info.ChineseYear, info.ChineseMonth, info.ChineseDay, info.IsLeapMonth());
    }
    /// <summary>
    /// 转换新农历日期值并读取同样的字段。
    /// </summary>
    /// <returns>农历字段组合。</returns>
    [Benchmark]
    public int ValueParts()
    {
        var date = _date.ToLunarDate();
        return Combine(date.Year, date.CalendarMonth, date.Day, date.IsLeapMonth);
    }
    /// <summary>
    /// 组合测量中共同读取的农历字段。
    /// </summary>
    /// <param name="year">农历年。</param>
    /// <param name="month">BCL 农历月序号。</param>
    /// <param name="day">农历日。</param>
    /// <param name="leap">是否为闰月。</param>
    /// <returns>可比较的字段组合。</returns>
    private static int Combine(int year, int month, int day, bool leap) =>
        ((year * 14 + month) * 31 + day) * 2 + (leap ? 1 : 0);
}
/// <summary>
/// 分别测量农历反向转换、文本和月份偏移。
/// </summary>
[MemoryDiagnoser]
public class LunarCalendarOperationsBenchmarks
{
    /// <summary>
    /// 预先创建的农历闰二月日期。
    /// </summary>
    private readonly LunarDate _date = ChineseLunarCalendar.Create(2023, 2, 1, true);
    /// <summary>
    /// 从农历闰月分量构造日期。
    /// </summary>
    /// <returns>农历日期值。</returns>
    [Benchmark]
    public LunarDate ReverseLeapMonth() => ChineseLunarCalendar.Create(2023, 2, 1, true);
    /// <summary>
    /// 将已创建的农历日期格式化为简体中文。
    /// </summary>
    /// <returns>农历日期文本。</returns>
    [Benchmark]
    public string FormatChinese() => _date.ToChineseString();
    /// <summary>
    /// 按农历月份顺序移动一个月。
    /// </summary>
    /// <returns>移动后的农历日期。</returns>
    [Benchmark]
    public LunarDate AddLunarMonth() => _date.AddMonths(1);
}
/// <summary>
/// 在固定总工作量下测量并发日期转换及锁竞争。
/// </summary>
[MemoryDiagnoser]
public class LunarCalendarConcurrencyBenchmarks
{
    /// <summary>
    /// 每次测量固定转换总次数。
    /// </summary>
    private const int OperationCount = 10000;
    /// <summary>
    /// 并发转换使用的固定公历日期。
    /// </summary>
    private readonly DateTime _date = new(2023, 3, 22);
    /// <summary>
    /// 获取或设置并发工作线程请求数。
    /// </summary>
    [Params(1, 4, 8)]
    public int Workers { get; set; }
    /// <summary>
    /// 运行固定总次数的转换并合并各线程结果。
    /// </summary>
    /// <returns>所有转换结果的年和。</returns>
    [Benchmark(OperationsPerInvoke = OperationCount)]
    public long ConvertDates()
    {
        var sums = new long[Workers];
        Parallel.For(0, Workers, worker =>
        {
            long sum = 0;
            for (var index = worker; index < OperationCount; index += Workers)
                sum += _date.ToLunarDate().Year;
            sums[worker] = sum;
        });
        return sums.Sum();
    }
}
/// <summary>
/// 在新基准进程中观察首次农历转换成本。
/// </summary>
/// <remarks>
/// 不包含进程创建耗时，首次调用包含相关 JIT 和历法初始化。
/// </remarks>
[MemoryDiagnoser]
[SimpleJob(RunStrategy.ColdStart, launchCount: 3, warmupCount: 0, iterationCount: 1, invocationCount: 1)]
public class LunarCalendarColdStartBenchmarks
{
    /// <summary>
    /// 调用进程中的首次农历日期转换。
    /// </summary>
    /// <returns>转换后的农历日期。</returns>
    [Benchmark]
    public LunarDate FirstConversion() => new DateTime(2023, 3, 22).ToLunarDate();
}
