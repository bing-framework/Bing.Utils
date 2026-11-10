using System.Globalization;
using BenchmarkDotNet.Attributes;
using Bing.Date;
using Bing.Date.Calendars;
using Bing.Date.Chinese;
using Bing.Date.TimeZones;

namespace Bing.Utils.Benchmark.Benchmarks;

/// <summary>
/// 测量默认周号计算成本。
/// </summary>
/// <remarks>所有运行使用相同日期和固定区域文化。</remarks>
[MemoryDiagnoser]
public class DateTimeWeekBenchmarks
{
    /// <summary>
    /// 基准重复使用的固定输入日期。
    /// </summary>
    private readonly DateTime _date = new(2024, 12, 31);

    /// <summary>
    /// 设置基准使用的固定区域文化。
    /// </summary>
    [GlobalSetup]
    public void Setup() => CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");

    /// <summary>
    /// 获取固定日期的默认周号。
    /// </summary>
    /// <returns>固定日期对应的周号。</returns>
    [Benchmark]
    public int DefaultWeek() => _date.GetWeekOfYear();
}

/// <summary>
/// 测量中国节气查询成本。
/// </summary>
/// <remarks>使用官方日期表查询结果作为基准输入。</remarks>
[MemoryDiagnoser]
public class DateTimeSolarBenchmarks
{
    /// <summary>
    /// 基准重复使用的中国农历实例。
    /// </summary>
    private readonly ChineseLunisolarCalendar _calendar = new();
    /// <summary>
    /// 基准重复使用的固定输入日期。
    /// </summary>
    private readonly DateTime _date = new(2023, 12, 31);

    /// <summary>
    /// 获取固定日期之后的下一节气。
    /// </summary>
    /// <returns>固定日期之后的下一节气名称。</returns>
    [Benchmark]
    public string NextTermAcrossYear() => ChineseSolarTermHelper.GetNextSolarTerm(_calendar, _date);

    /// <summary>
    /// 获取固定日期对应的节气。
    /// </summary>
    /// <returns>固定日期对应的节气名称。</returns>
    [Benchmark]
    public string CurrentTerm() => ChineseSolarTermHelper.GetSolarTerm(_calendar, new DateTime(2023, 6, 21));
}

/// <summary>
/// 测量时间戳工厂的并发生成成本。
/// </summary>
/// <remarks>各线程配置使用相同的总生成次数，测量结果包含并行调度开销。</remarks>
[MemoryDiagnoser]
public class DateTimeTimestampBenchmarks
{
    /// <summary>
    /// 每组参数执行的时间戳生成总次数。
    /// </summary>
    private const int OperationCount = 40000;
    /// <summary>
    /// 基准重复使用的时间戳工厂。
    /// </summary>
    private readonly NoRepeatTimeStampFactory _factory = new(0.0001);
    /// <summary>
    /// 基准生成时间戳的固定起始时间。
    /// </summary>
    private readonly DateTime _origin = new(2090, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// 获取或设置并发工作线程数。
    /// </summary>
    [Params(1, 4)]
    public int Workers { get; set; }

    /// <summary>
    /// 生成固定总量的时间戳。
    /// </summary>
    [Benchmark(OperationsPerInvoke = OperationCount)]
    public void Generate()
    {
        if (Workers == 1)
        {
            for (var i = 0; i < OperationCount; i++)
                _factory.GetTimeStamp(_origin);
            return;
        }
        Parallel.For(0, Workers, worker =>
        {
            for (var i = worker; i < OperationCount; i += Workers)
                _factory.GetTimeStamp(_origin);
        });
    }
}

/// <summary>
/// 测量不同区间长度及例外日期密度下的业务日历运算成本。
/// </summary>
[MemoryDiagnoser]
public class BusinessCalendarBenchmarks
{
    /// <summary>
    /// 当前参数组合使用的业务日历。
    /// </summary>
    private BusinessCalendar _calendar = null!;

    /// <summary>
    /// 待测日期区间的起点。
    /// </summary>
    private DateTime _start;

    /// <summary>
    /// 待测日期区间的不包含终点。
    /// </summary>
    private DateTime _end;

    /// <summary>
    /// 待测业务日偏移量。
    /// </summary>
    private int _businessDayOffset;

    /// <summary>
    /// 获取或设置待测区间：短区间、跨年区间或五十年跨度。
    /// </summary>
    [Params("Short", "CrossYear", "LongSpan")]
    public string Scenario { get; set; } = "Short";

    /// <summary>
    /// 获取或设置是否为工作日历配置节假日和补班日期。
    /// </summary>
    [Params(false, true)]
    public bool WithExceptions { get; set; }

    /// <summary>
    /// 按基准参数构造确定的区间和例外日期集合。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        switch (Scenario)
        {
            case "Short":
                _start = new DateTime(2024, 3, 1);
                _end = _start.AddDays(14);
                _businessDayOffset = 9;
                break;
            case "CrossYear":
                _start = new DateTime(2024, 12, 20);
                _end = new DateTime(2025, 1, 15);
                _businessDayOffset = 17;
                break;
            case "LongSpan":
                _start = new DateTime(1970, 1, 1);
                _end = new DateTime(2020, 1, 1);
                _businessDayOffset = 12000;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(Scenario), Scenario, "不支持的业务日历基准区间。");
        }

        var holidays = new List<DateTime>();
        var workingDayOverrides = new List<DateTime>();
        if (WithExceptions)
        {
            for (var date = _start.AddDays(3); date < _end; date = date.AddDays(30))
                holidays.Add(date);

            for (var date = _start.Date; date < _end; date = date.AddDays(180))
            {
                if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                    workingDayOverrides.Add(date);
            }

            var firstWeekend = _start.Date;
            while (firstWeekend < _end && firstWeekend.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
                firstWeekend = firstWeekend.AddDays(1);
            if (firstWeekend < _end)
                workingDayOverrides.Add(firstWeekend);
        }

        _calendar = new BusinessCalendar(holidays: holidays, workingDayOverrides: workingDayOverrides);
    }

    /// <summary>
    /// 测量从固定起点移动一段确定业务日偏移的成本。
    /// </summary>
    /// <returns>移动后的日期。</returns>
    [Benchmark]
    public DateTime AddBusinessDays() => _calendar.AddBusinessDays(_start, _businessDayOffset);

    /// <summary>
    /// 测量统计固定半开区间内业务日数量的成本。
    /// </summary>
    /// <returns>区间内的业务日数量。</returns>
    [Benchmark]
    public int CountBusinessDays() => _calendar.CountBusinessDays(_start, _end);
}

/// <summary>
/// 测量固定时间点转换到自建固定偏移时区的成本。
/// </summary>
[MemoryDiagnoser]
public class TimeZoneConversionBenchmarks
{
    /// <summary>
    /// 各目标偏移共用的固定输入时间点。
    /// </summary>
    private static readonly DateTimeOffset Instant = new(2024, 3, 10, 7, 30, 0, TimeSpan.Zero);

    /// <summary>
    /// 当前参数组合使用的自建固定偏移时区。
    /// </summary>
    private TimeZoneInfo _destinationTimeZone = null!;

    /// <summary>
    /// 获取或设置自建目标时区的固定 UTC 偏移小时数。
    /// </summary>
    [Params(-8, 5.5, 14)]
    public double OffsetHours { get; set; }

    /// <summary>
    /// 按参数构造自建固定偏移时区，不访问系统时区数据库。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        var offset = TimeSpan.FromHours(OffsetHours);
        var zoneId = $"Benchmark/Fixed/{OffsetHours:+0.##;-0.##}";
        _destinationTimeZone = TimeZoneInfo.CreateCustomTimeZone(zoneId, offset, zoneId, zoneId);
    }

    /// <summary>
    /// 将固定时间点转换到当前参数指定的自建时区。
    /// </summary>
    /// <returns>目标时区的日期时间及偏移。</returns>
    [Benchmark]
    public DateTimeOffset ConvertInstant() => TimeZoneConversion.ToTimeZone(Instant, _destinationTimeZone);
}
