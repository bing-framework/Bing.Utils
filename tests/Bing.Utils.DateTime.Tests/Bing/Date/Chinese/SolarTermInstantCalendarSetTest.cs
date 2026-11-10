using Bing.Date.Chinese;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bing.Utils.DateTime.Tests.Bing.Date.Chinese;

/// <summary>
/// 验证调用方提供的年度节气时刻表组合及跨年查询。
/// </summary>
public class SolarTermInstantCalendarSetTest
{
    /// <summary>
    /// 验证构造会复制输入快照，并允许不同年度使用不同的数据来源和版本。
    /// </summary>
    [Fact]
    public void Constructor_CopiesSnapshotAndPreservesPerYearMetadata()
    {
        var first = CreateCalendar(2023, "v-2023", "source-a");
        var last = CreateCalendar(2025, "v-2025", "source-b");
        var input = new List<SolarTermInstantCalendar> { last, first };
        var calendars = new SolarTermInstantCalendarSet(input);

        calendars.MinYear.ShouldBe(2023);
        calendars.MaxYear.ShouldBe(2025);
        calendars.Count.ShouldBe(2);
        calendars.GetCalendar(2023).Version.ShouldBe("v-2023");
        calendars.GetCalendar(2023).Source.ShouldBe("source-a");
        calendars.GetCalendar(2025).Version.ShouldBe("v-2025");
        calendars.GetCalendar(2025).Source.ShouldBe("source-b");
        calendars.GetTerm(2025, ChineseSolarTerms.SlightCold)
            .ShouldBe(CreateTerms(2025)[0]);

        input.Clear();
        input.Add(CreateCalendar(2024));

        calendars.GetCalendar(2023).Year.ShouldBe(2023);
        calendars.GetCalendar(2025).Year.ShouldBe(2025);
        Should.Throw<KeyNotFoundException>(() => calendars.GetCalendar(2024));
    }

    /// <summary>
    /// 验证集合拒绝空集合、空项和重复年份。
    /// </summary>
    [Fact]
    public void Constructor_RejectsInvalidSnapshots()
    {
        Should.Throw<ArgumentNullException>(() => new SolarTermInstantCalendarSet(null));
        Should.Throw<ArgumentException>(() => new SolarTermInstantCalendarSet(Array.Empty<SolarTermInstantCalendar>()));
        Should.Throw<ArgumentException>(() => new SolarTermInstantCalendarSet(new SolarTermInstantCalendar[] { null }));
        Should.Throw<ArgumentException>(() => new SolarTermInstantCalendarSet(new[]
        {
            CreateCalendar(2024), CreateCalendar(2024, "other-version", "other-source")
        }));
    }

    /// <summary>
    /// 验证显式年度查询对缺失年份和不支持年份区分异常。
    /// </summary>
    [Fact]
    public void GetCalendarAndTerm_DistinguishMissingAndUnsupportedYears()
    {
        var calendars = CreateSet(CreateCalendar(2023), CreateCalendar(2025));

        Should.Throw<KeyNotFoundException>(() => calendars.GetCalendar(2024));
        Should.Throw<KeyNotFoundException>(() => calendars.GetTerm(2024, ChineseSolarTerms.SlightCold));
        Should.Throw<ArgumentOutOfRangeException>(() => calendars.GetCalendar(0));
        Should.Throw<ArgumentOutOfRangeException>(() => calendars.GetTerm(9999, ChineseSolarTerms.SlightCold));
        Should.Throw<ArgumentOutOfRangeException>(() => calendars.GetTerm(2023, (ChineseSolarTerms)24));
    }

    /// <summary>
    /// 验证跨年相邻查询对精确时刻使用严格和包含边界语义。
    /// </summary>
    [Fact]
    public void Neighbors_CrossYearAtExactInstantAndAdjacentTicks()
    {
        var previousYear = CreateTerms(2023);
        var currentYear = CreateTerms(2024);
        var calendars = CreateSet(CreateCalendar(2023, terms: previousYear), CreateCalendar(2024, terms: currentYear));
        var previousLast = previousYear[23];
        var currentFirst = currentYear[0];
        var nextInYear = currentYear[1];

        calendars.GetNext(previousLast.Instant.AddTicks(-1))!.Value.ShouldBe(previousLast);
        calendars.GetNext(previousLast.Instant).Value.ShouldBe(currentFirst);
        calendars.GetNext(previousLast.Instant, includeBoundary: true)!.Value.ShouldBe(previousLast);
        calendars.GetPrevious(previousLast.Instant.AddTicks(1))!.Value.ShouldBe(previousLast);
        calendars.GetPrevious(currentFirst.Instant.AddTicks(-1))!.Value.ShouldBe(previousLast);
        calendars.GetNext(currentFirst.Instant.AddTicks(-1))!.Value.ShouldBe(currentFirst);
        calendars.GetPrevious(currentFirst.Instant).Value.ShouldBe(previousLast);
        calendars.GetNext(currentFirst.Instant).Value.ShouldBe(nextInYear);
        calendars.GetPrevious(currentFirst.Instant, includeBoundary: true)!.Value.ShouldBe(currentFirst);
        calendars.GetNext(currentFirst.Instant, includeBoundary: true)!.Value.ShouldBe(currentFirst);
        calendars.GetPrevious(currentFirst.Instant.AddTicks(1))!.Value.ShouldBe(currentFirst);
        calendars.GetNext(currentFirst.Instant.AddTicks(1))!.Value.ShouldBe(nextInYear);

        var equivalentOffset = currentFirst.Instant.ToOffset(TimeSpan.FromHours(-5));
        calendars.GetPrevious(equivalentOffset, includeBoundary: true)!.Value.ShouldBe(currentFirst);
        calendars.GetNext(equivalentOffset, includeBoundary: true)!.Value.ShouldBe(currentFirst);
    }

    /// <summary>
    /// 验证查询年度按北京时间确定，而非按 DateTimeOffset 的 UTC 年份确定。
    /// </summary>
    [Fact]
    public void Neighbors_UseBeijingYearAtUtcNewYearBoundary()
    {
        var calendars = CreateSet(CreateCalendar(2024));
        var newYearInBeijing = new DateTimeOffset(2024, 12, 31, 16, 0, 0, TimeSpan.Zero);

        Should.Throw<KeyNotFoundException>(() => calendars.GetPrevious(newYearInBeijing));
        Should.Throw<KeyNotFoundException>(() => calendars.GetNext(newYearInBeijing));
    }

    /// <summary>
    /// 验证北京时间元旦零点恰好是首个节气时，两侧查询采用正确年度。
    /// </summary>
    [Fact]
    public void Neighbors_FirstTermAtBeijingNewYear_PreservesTickBoundaries()
    {
        var previous = CreateTerms(2023);
        var current = CreateTerms(2024);
        var newYear = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.FromHours(8));
        current[0] = new SolarTermInstant(ChineseSolarTerms.SlightCold, newYear);
        var calendars = CreateSet(CreateCalendar(2023, terms: previous), CreateCalendar(2024, terms: current));

        calendars.GetNext(newYear.AddTicks(-1))!.Value.ShouldBe(current[0]);
        calendars.GetPrevious(newYear)!.Value.ShouldBe(previous[23]);
        calendars.GetNext(newYear, includeBoundary: true)!.Value.ShouldBe(current[0]);
        calendars.GetPrevious(newYear.AddTicks(1))!.Value.ShouldBe(current[0]);
    }

    /// <summary>
    /// 验证相邻查询遇到集合范围内的缺年时明确失败。
    /// </summary>
    [Fact]
    public void Neighbors_ThrowWhenRequiredInteriorYearIsMissing()
    {
        var calendars = CreateSet(CreateCalendar(2023), CreateCalendar(2025));
        var lastInFirstYear = CreateTerms(2023)[23].Instant;
        var firstInLastYear = CreateTerms(2025)[0].Instant;

        Should.Throw<KeyNotFoundException>(() => calendars.GetNext(lastInFirstYear.AddTicks(1)));
        Should.Throw<KeyNotFoundException>(() => calendars.GetPrevious(firstInLastYear.AddTicks(-1)));
    }

    /// <summary>
    /// 验证集合两端之外没有前后年度时返回空值。
    /// </summary>
    [Fact]
    public void Neighbors_ReturnNullOutsideLoadedYearRange()
    {
        var firstYear = CreateTerms(2024);
        var lastYear = CreateTerms(2025);
        var calendars = CreateSet(CreateCalendar(2024, terms: firstYear), CreateCalendar(2025, terms: lastYear));
        var first = firstYear[0].Instant;
        var last = lastYear[23].Instant;

        calendars.GetPrevious(first.AddTicks(-1)).ShouldBeNull();
        calendars.GetNext(first.AddTicks(-1))!.Value.ShouldBe(firstYear[0]);
        calendars.GetPrevious(last.AddTicks(1))!.Value.ShouldBe(lastYear[23]);
        calendars.GetNext(last.AddTicks(1)).ShouldBeNull();
        calendars.GetPrevious(first, includeBoundary: true)!.Value.ShouldBe(firstYear[0]);
        calendars.GetNext(last, includeBoundary: true)!.Value.ShouldBe(lastYear[23]);
    }

    /// <summary>
    /// 验证第 1 年起点可查询，且集合前界之外没有上一节气。
    /// </summary>
    [Fact]
    public void Neighbors_HandleDateTimeOffsetMinimumWhenYearOneIsLoaded()
    {
        var terms = CreateTerms(1);
        var calendars = CreateSet(CreateCalendar(1, terms: terms));

        calendars.GetPrevious(DateTimeOffset.MinValue).ShouldBeNull();
        calendars.GetNext(DateTimeOffset.MinValue)!.Value.ShouldBe(terms[0]);
    }

    /// <summary>
    /// 验证第 9998 年北京时间最后一 tick 可查询，而进入第 9999 年即超出支持范围。
    /// </summary>
    [Fact]
    public void Neighbors_HandleLastSupportedBeijingTickAndRejectYear9999()
    {
        var terms = CreateTerms(9998);
        var calendars = CreateSet(CreateCalendar(9998, terms: terms));
        var lastBeijingTick = new DateTimeOffset(9998, 12, 31, 23, 59, 59, TimeSpan.FromHours(8))
            .AddTicks(TimeSpan.TicksPerSecond - 1);
        var firstBeijingInstantIn9999 = new DateTimeOffset(9999, 1, 1, 8, 0, 0, TimeSpan.FromHours(8));

        calendars.GetPrevious(lastBeijingTick)!.Value.ShouldBe(terms[23]);
        calendars.GetNext(lastBeijingTick).ShouldBeNull();
        Should.Throw<ArgumentOutOfRangeException>(() => calendars.GetPrevious(firstBeijingInstantIn9999))
            .ParamName.ShouldBe("instant");
        Should.Throw<ArgumentOutOfRangeException>(() => calendars.GetNext(firstBeijingInstantIn9999))
            .ParamName.ShouldBe("instant");
    }

    /// <summary>
    /// 验证无效北京时间换算范围通过 instant 参数报告。
    /// </summary>
    [Fact]
    public void Neighbors_RejectInstantsOutsideSupportedBeijingYearRange()
    {
        var calendars = CreateSet(CreateCalendar(2024));
        var year9999InBeijing = new DateTimeOffset(9998, 12, 31, 16, 0, 0, TimeSpan.Zero);

        Should.Throw<KeyNotFoundException>(() => calendars.GetPrevious(DateTimeOffset.MinValue));
        Should.Throw<ArgumentOutOfRangeException>(() => calendars.GetNext(DateTimeOffset.MaxValue))
            .ParamName.ShouldBe("instant");
        Should.Throw<ArgumentOutOfRangeException>(() => calendars.GetPrevious(year9999InBeijing))
            .ParamName.ShouldBe("instant");
        Should.Throw<ArgumentOutOfRangeException>(() => calendars.GetNext(year9999InBeijing))
            .ParamName.ShouldBe("instant");
    }

    /// <summary>
    /// 验证并发相邻查询共享不可变年度快照。
    /// </summary>
    [Fact]
    public async Task Neighbors_SupportConcurrentQueries()
    {
        var calendars = CreateSet(CreateCalendar(2023), CreateCalendar(2024), CreateCalendar(2025));
        var expected = CreateTerms(2024)[8];
        var instant = expected.Instant;

        var results = await Task.WhenAll(Enumerable.Range(0, 240).Select(_ => Task.Run(() =>
            calendars.GetPrevious(instant, includeBoundary: true))));

        foreach (var result in results)
            result!.Value.ShouldBe(expected);
    }

    /// <summary>
    /// 使用仅供行为测试的合成年度时刻表创建集合。
    /// </summary>
    /// <param name="calendars">年度数据。</param>
    /// <returns>跨年节气时刻集合。</returns>
    private static SolarTermInstantCalendarSet CreateSet(params SolarTermInstantCalendar[] calendars) => new(calendars);

    /// <summary>
    /// 创建仅供查询测试使用的合成年度数据。
    /// </summary>
    /// <param name="year">数据所属年份。</param>
    /// <param name="version">调用方数据版本。</param>
    /// <param name="source">调用方数据来源。</param>
    /// <param name="terms">可选的合成节气时刻。</param>
    /// <returns>年度节气时刻表。</returns>
    private static SolarTermInstantCalendar CreateCalendar(int year, string version = "synthetic-v1",
        string source = "synthetic test data", IEnumerable<SolarTermInstant> terms = null) =>
        new(year, version, source, TimeSpan.FromMinutes(1), terms ?? CreateTerms(year));

    /// <summary>
    /// 按小寒至冬至的顺序生成二十四个合成节气时刻，不代表真实天文数据。
    /// </summary>
    /// <param name="year">合成年份。</param>
    /// <returns>固定偏移为北京时间的合成时刻。</returns>
    private static SolarTermInstant[] CreateTerms(int year) => Enumerable.Range(0, 24)
        .Select(index => new SolarTermInstant(TermAt(index),
            new DateTimeOffset(year, index / 2 + 1, index % 2 == 0 ? 5 : 20, 12, 0, 0, TimeSpan.FromHours(8))))
        .ToArray();

    /// <summary>
    /// 将年度时序索引映射为公开节气枚举。
    /// </summary>
    /// <param name="index">从小寒开始的时序索引。</param>
    /// <returns>公开枚举值。</returns>
    private static ChineseSolarTerms TermAt(int index) => (ChineseSolarTerms)((index + 22) % 24);
}
