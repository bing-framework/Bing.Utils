using System.Globalization;
using BenchmarkDotNet.Attributes;
using Bing.Date;
using Bing.Date.Chinese;

namespace Bing.Utils.Benchmark.Benchmarks;

/// <summary>默认周数计算的可重复基准；修复前后使用相同输入、文化和入口。</summary>
[MemoryDiagnoser]
public class DateTimeWeekBenchmarks
{
    private readonly DateTime _date = new(2024, 12, 31);

    [GlobalSetup]
    public void Setup() => CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");

    [Benchmark]
    public int DefaultWeek() => _date.GetWeekOfYear();
}

/// <summary>官方日期表查询的当前成本；不与返回错误日期的旧公式比较加速比。</summary>
[MemoryDiagnoser]
public class DateTimeSolarBenchmarks
{
    private readonly ChineseLunisolarCalendar _calendar = new();
    private readonly DateTime _date = new(2023, 12, 31);

    [Benchmark]
    public string NextTermAcrossYear() => ChineseSolarTermHelper.GetNextSolarTerm(_calendar, _date);

    [Benchmark]
    public string CurrentTerm() => ChineseSolarTermHelper.GetSolarTerm(_calendar, new DateTime(2023, 6, 21));
}

/// <summary>相同总工作量下单线程与多线程的时间戳工厂成本，包含 Parallel 调度开销。</summary>
[MemoryDiagnoser]
public class DateTimeTimestampBenchmarks
{
    private const int OperationCount = 40000;
    private readonly NoRepeatTimeStampFactory _factory = new(0.0001);
    private readonly DateTime _origin = new(2090, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Params(1, 4)]
    public int Workers { get; set; }

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
