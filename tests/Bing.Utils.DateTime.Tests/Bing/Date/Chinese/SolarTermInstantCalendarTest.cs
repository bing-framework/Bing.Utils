using Bing.Date.Chinese;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bing.Utils.DateTime.Tests.Bing.Date.Chinese;

/// <summary>
/// 验证调用方提供的年度节气真实时间点日历。
/// </summary>
public class SolarTermInstantCalendarTest
{
    /// <summary>
    /// 验证枚举映射、UTC 归一化和数据元信息。
    /// </summary>
    [Fact]
    public void Constructor_MapsTermsAndPreservesMetadata()
    {
        var terms = Enumerable.Reverse(CreateTerms(2024)).ToArray();
        var calendar = new SolarTermInstantCalendar(2024, "test-v1", "licensed synthetic source", TimeSpan.FromMinutes(1), terms);

        calendar.Year.ShouldBe(2024);
        calendar.Version.ShouldBe("test-v1");
        calendar.Source.ShouldBe("licensed synthetic source");
        calendar.Resolution.ShouldBe(TimeSpan.FromMinutes(1));
        for (var index = 0; index < 24; index++)
        {
            var expectedTerm = TermAt(index);
            var actual = calendar.GetTerm(expectedTerm);
            actual.Term.ShouldBe(expectedTerm);
            actual.Instant.ShouldBe(ExpectedInstant(2024, index));
            actual.Instant.Offset.ShouldBe(TimeSpan.Zero);
        }
    }

    /// <summary>
    /// 验证相邻项按 UTC 时间点比较，并支持严格和包含边界两种模式。
    /// </summary>
    [Fact]
    public void Neighbors_CompareInstantsAndHandleBoundaries()
    {
        var terms = CreateTerms(2024);
        var calendar = CreateCalendar(terms: terms);
        const int targetIndex = 2;
        var exact = terms[targetIndex].Instant;

        calendar.GetPrevious(exact.AddTicks(-1))!.Value.Term.ShouldBe(TermAt(targetIndex - 1));
        calendar.GetNext(exact.AddTicks(-1))!.Value.Term.ShouldBe(TermAt(targetIndex));
        calendar.GetPrevious(exact).Value.Term.ShouldBe(TermAt(targetIndex - 1));
        calendar.GetNext(exact).Value.Term.ShouldBe(TermAt(targetIndex + 1));
        calendar.GetPrevious(exact, includeBoundary: true)!.Value.Term.ShouldBe(TermAt(targetIndex));
        calendar.GetNext(exact, includeBoundary: true)!.Value.Term.ShouldBe(TermAt(targetIndex));
        calendar.GetPrevious(exact.AddTicks(1))!.Value.Term.ShouldBe(TermAt(targetIndex));
        calendar.GetNext(exact.AddTicks(1))!.Value.Term.ShouldBe(TermAt(targetIndex + 1));

        var equivalentOffset = exact.ToOffset(TimeSpan.FromHours(-5));
        calendar.GetPrevious(equivalentOffset, includeBoundary: true)!.Value.Instant.ShouldBe(exact);
        calendar.GetNext(equivalentOffset, includeBoundary: true)!.Value.Instant.ShouldBe(exact);
    }

    /// <summary>
    /// 验证单个独立参考向量：美国海军天文台公布的 2026 年夏至为 08:24 UTC。
    /// </summary>
    /// <remarks>其他节气仍为合成数据，此测试不声明完整年度数据通过天文精度验收。</remarks>
    [Fact]
    public void SuppliedSummerSolstice_PreservesPublishedMinuteAndInstantBoundaries()
    {
        var terms = CreateTerms(2026);
        var published = new DateTimeOffset(2026, 6, 21, 8, 24, 0, TimeSpan.Zero);
        terms[11] = new SolarTermInstant(ChineseSolarTerms.SummerSolstice, published);
        var calendar = new SolarTermInstantCalendar(2026, "test", "USNO single vector; remaining synthetic", TimeSpan.FromMinutes(1), terms);

        calendar.GetTerm(ChineseSolarTerms.SummerSolstice).Instant.ShouldBe(published);
        calendar.GetNext(published.AddTicks(-1)).Value.Term.ShouldBe(ChineseSolarTerms.SummerSolstice);
        calendar.GetPrevious(published.AddTicks(1)).Value.Term.ShouldBe(ChineseSolarTerms.SummerSolstice);
        SolarTermCalendar.GetTerm(published).Value.Term.ShouldBe(ChineseSolarTerms.SummerSolstice);
    }

    /// <summary>
    /// 验证年度查询边界不会隐式读取前一年或后一年。
    /// </summary>
    [Fact]
    public void Neighbors_ReturnNullOutsideAnnualTerms()
    {
        var terms = CreateTerms(2024);
        var calendar = CreateCalendar(terms: terms);
        var first = terms[0].Instant;
        var last = terms[23].Instant;

        calendar.GetPrevious(DateTimeOffset.MinValue).ShouldBeNull();
        calendar.GetNext(DateTimeOffset.MaxValue).ShouldBeNull();
        calendar.GetPrevious(first).ShouldBeNull();
        calendar.GetPrevious(first, includeBoundary: true)!.Value.Instant.ShouldBe(first);
        calendar.GetNext(DateTimeOffset.MinValue)!.Value.Instant.ShouldBe(first);
        calendar.GetNext(last).ShouldBeNull();
        calendar.GetNext(last, includeBoundary: true)!.Value.Instant.ShouldBe(last);
        calendar.GetPrevious(DateTimeOffset.MaxValue)!.Value.Instant.ShouldBe(last);
    }

    /// <summary>
    /// 验证构造时复制输入集合且并发查询只读。
    /// </summary>
    [Fact]
    public async Task Calendar_CopiesInputAndSupportsConcurrentReads()
    {
        var input = CreateTerms(2024).ToList();
        var expected = input[0];
        var calendar = CreateCalendar(terms: input);
        input[0] = input[1];

        calendar.GetTerm(ChineseSolarTerms.SlightCold).ShouldBe(expected);
        var results = await Task.WhenAll(Enumerable.Range(0, 240).Select(index => Task.Run(() =>
        {
            var termIndex = index % 24;
            return calendar.GetTerm(TermAt(termIndex));
        })));

        for (var index = 0; index < results.Length; index++)
            results[index].ShouldBe(calendar.GetTerm(TermAt(index % 24)));
    }

    /// <summary>
    /// 验证年份、元数据和分辨率的输入边界。
    /// </summary>
    [Fact]
    public void Constructor_RejectsInvalidYearMetadataAndResolution()
    {
        var terms = CreateTerms(2024);

        Should.Throw<ArgumentOutOfRangeException>(() => CreateCalendar(year: 0, terms: terms));
        Should.Throw<ArgumentOutOfRangeException>(() => CreateCalendar(year: 9999, terms: terms));
        Should.Throw<ArgumentException>(() => new SolarTermInstantCalendar(2024, " ", "source", TimeSpan.FromMinutes(1), terms));
        Should.Throw<ArgumentException>(() => new SolarTermInstantCalendar(2024, "version", "\t", TimeSpan.FromMinutes(1), terms));
        Should.Throw<ArgumentException>(() => new SolarTermInstantCalendar(2024, null!, "source", TimeSpan.FromMinutes(1), terms));
        Should.Throw<ArgumentException>(() => new SolarTermInstantCalendar(2024, "version", null!, TimeSpan.FromMinutes(1), terms));
        Should.Throw<ArgumentOutOfRangeException>(() => CreateCalendar(resolution: TimeSpan.Zero, terms: terms));
        Should.Throw<ArgumentOutOfRangeException>(() => CreateCalendar(resolution: TimeSpan.FromDays(1).Add(TimeSpan.FromTicks(1)), terms: terms));
        Should.Throw<ArgumentNullException>(() => new SolarTermInstantCalendar(2024, "version", "source",
            TimeSpan.FromMinutes(1), null!));
        Should.Throw<ArgumentOutOfRangeException>(() => new SolarTermInstant((ChineseSolarTerms)24, DateTimeOffset.UtcNow));
        Should.Throw<ArgumentOutOfRangeException>(() => CreateCalendar().GetTerm((ChineseSolarTerms)24));

        var dailyResolutionTerms = Enumerable.Range(0, 24).Select(index => new SolarTermInstant(TermAt(index),
            new DateTimeOffset(2024, index / 2 + 1, index % 2 == 0 ? 5 : 20, 0, 0, 0, TimeSpan.Zero)));
        new SolarTermInstantCalendar(2024, "test-v1", "synthetic test vector", TimeSpan.FromDays(1), dailyResolutionTerms)
            .Resolution.ShouldBe(TimeSpan.FromDays(1));

        CreateCalendar(year: 1).Year.ShouldBe(1);
        CreateCalendar(year: 9998).Year.ShouldBe(9998);
    }

    /// <summary>
    /// 验证集合数量、重复节气、月份、年份、时间顺序和刻度对齐校验。
    /// </summary>
    [Fact]
    public void Constructor_RejectsInvalidTermData()
    {
        var valid = CreateTerms(2024);
        Should.Throw<ArgumentException>(() => CreateCalendar(terms: valid.Take(23)));
        Should.Throw<ArgumentException>(() => CreateCalendar(terms: valid.Concat(new[] { valid[0] })));

        var duplicate = valid.ToArray();
        duplicate[1] = new SolarTermInstant(duplicate[0].Term, duplicate[1].Instant);
        Should.Throw<ArgumentException>(() => CreateCalendar(terms: duplicate));

        var wrongMonth = valid.ToArray();
        wrongMonth[0] = new SolarTermInstant(wrongMonth[0].Term, BeijingTime(2024, 2, 4));
        Should.Throw<ArgumentException>(() => CreateCalendar(terms: wrongMonth));

        var wrongYear = valid.ToArray();
        wrongYear[0] = new SolarTermInstant(wrongYear[0].Term, BeijingTime(2025, 1, 5));
        Should.Throw<ArgumentException>(() => CreateCalendar(terms: wrongYear));

        var wrongOrder = valid.ToArray();
        (wrongOrder[0], wrongOrder[1]) = (new SolarTermInstant(wrongOrder[0].Term, wrongOrder[1].Instant),
            new SolarTermInstant(wrongOrder[1].Term, wrongOrder[0].Instant));
        Should.Throw<ArgumentException>(() => CreateCalendar(terms: wrongOrder));

        var misaligned = valid.ToArray();
        misaligned[4] = new SolarTermInstant(misaligned[4].Term, misaligned[4].Instant.AddTicks(1));
        Should.Throw<ArgumentException>(() => CreateCalendar(terms: misaligned));
    }

    /// <summary>
    /// 按测试日历默认参数创建合成年度节气时间表。
    /// </summary>
    /// <param name="year">数据年份。</param>
    /// <param name="version">版本标识。</param>
    /// <param name="source">来源说明。</param>
    /// <param name="resolution">数据分辨率。</param>
    /// <param name="terms">节气集合；未提供时生成合成向量。</param>
    /// <returns>通过输入校验的日历。</returns>
    private static SolarTermInstantCalendar CreateCalendar(int year = 2024, string version = "test-v1",
        string source = "licensed synthetic source", TimeSpan? resolution = null,
        IEnumerable<SolarTermInstant> terms = null) =>
        new(year, version, source, resolution ?? TimeSpan.FromMinutes(1), terms ?? CreateTerms(year));

    /// <summary>
    /// 生成测试用的二十四个严格递增节气时刻，不包含任何真实官方数据。
    /// </summary>
    /// <param name="year">合成数据年份。</param>
    /// <returns>从小寒开始按年度顺序排列的节气。</returns>
    private static SolarTermInstant[] CreateTerms(int year) => Enumerable.Range(0, 24)
        .Select(index => new SolarTermInstant(TermAt(index), ExpectedInstant(year, index)))
        .ToArray();

    /// <summary>
    /// 将年度时序索引映射到公开枚举值。
    /// </summary>
    /// <param name="index">从小寒开始的索引。</param>
    /// <returns>公开节气枚举。</returns>
    private static ChineseSolarTerms TermAt(int index) => (ChineseSolarTerms)((index + 22) % 24);

    /// <summary>
    /// 为合成测试向量生成整分北京时间并转换为 UTC。
    /// </summary>
    /// <param name="year">年份。</param>
    /// <param name="index">从小寒开始的索引。</param>
    /// <returns>指定年度索引的合成时间点。</returns>
    private static DateTimeOffset ExpectedInstant(int year, int index) =>
        BeijingTime(year, index / 2 + 1, index % 2 == 0 ? 5 : 20);

    /// <summary>
    /// 创建合成数据使用的固定 UTC+8 时间点。
    /// </summary>
    /// <param name="year">年份。</param>
    /// <param name="month">月份。</param>
    /// <param name="day">日期。</param>
    /// <returns>北京时间对应的时间点。</returns>
    private static DateTimeOffset BeijingTime(int year, int month, int day) =>
        new(year, month, day, 12, 0, 0, TimeSpan.FromHours(8));
}
