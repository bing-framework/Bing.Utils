using System.Collections.Generic;
using System.Linq;

namespace Bing.Date.Calendars;

/// <summary>
/// 验证预加载年度规则组成的不可变跨年业务日历。
/// </summary>
public class BusinessCalendarSetTest
{
    /// <summary>
    /// 测试快照使用的地区标识。
    /// </summary>
    private const string Region = "CN-office";

    /// <summary>
    /// 验证跨年计数采用各年度节假日和补班规则，且反向区间为负。
    /// </summary>
    [Fact]
    public void CountBusinessDays_AcrossYear_UsesAnnualRules()
    {
        var calendar = CreateSet(
            CreateYear(2023, holidays: new[] { new DateTime(2023, 12, 29) }),
            CreateYear(2024, holidays: new[] { new DateTime(2024, 1, 1) },
                overrides: new[] { new DateTime(2024, 1, 6) }));
        var start = new DateTime(2023, 12, 29, 9, 0, 0);
        var end = new DateTime(2024, 1, 8, 23, 59, 0);

        calendar.CountBusinessDays(start, end).ShouldBe(5);
        calendar.CountBusinessDays(end, start).ShouldBe(-5);
        calendar.CountBusinessDays(start, start.AddHours(10)).ShouldBe(0);
    }

    /// <summary>
    /// 验证不涉及次年元旦的空尾段无需加载下一年度。
    /// </summary>
    [Fact]
    public void CountBusinessDays_ExclusiveNewYearBoundary_DoesNotRequireExcludedYear()
    {
        var calendar = CreateSet(CreateYear(2023));

        calendar.CountBusinessDays(new DateTime(2023, 12, 29), new DateTime(2024, 1, 1))
            .ShouldBe(1);
        calendar.CountBusinessDays(new DateTime(2024, 1, 1), new DateTime(2024, 1, 1, 23, 59, 0))
            .ShouldBe(0);
    }

    /// <summary>
    /// 验证缺失年份只在实际被半开区间覆盖时拒绝。
    /// </summary>
    [Fact]
    public void CountBusinessDays_MissingYear_ThrowsOnlyWhenRangeCoversThatYear()
    {
        var calendar = CreateSet(CreateYear(2023), CreateYear(2025));

        calendar.CountBusinessDays(new DateTime(2023, 12, 31), new DateTime(2024, 1, 1))
            .ShouldBe(0);
        Should.Throw<KeyNotFoundException>(() => calendar.CountBusinessDays(
            new DateTime(2023, 12, 31), new DateTime(2024, 1, 2)));
        Should.Throw<KeyNotFoundException>(() => calendar.CountBusinessDays(
            new DateTime(2025, 1, 1), new DateTime(2026, 1, 2)));
    }

    /// <summary>
    /// 验证跨年正负偏移采用年度规则且保留完整时间精度和 Kind。
    /// </summary>
    /// <param name="kind">输入日期种类。</param>
    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void AddBusinessDays_AcrossYear_PreservesPrecisionAndKind(DateTimeKind kind)
    {
        var calendar = CreateSet(
            CreateYear(2023, overrides: new[] { new DateTime(2023, 12, 30) }),
            CreateYear(2024, holidays: new[] { new DateTime(2024, 1, 1) },
                overrides: new[] { new DateTime(2024, 1, 6) }));
        var beforeNewYear = new DateTime(2023, 12, 28, 14, 25, 30, kind).AddTicks(9876);
        var afterNewYear = new DateTime(2024, 1, 2, 14, 25, 30, kind).AddTicks(9876);

        var forward = calendar.AddBusinessDays(beforeNewYear, 3);
        var backward = calendar.AddBusinessDays(afterNewYear, -1);

        forward.ShouldBe(new DateTime(2024, 1, 2, 14, 25, 30, kind).AddTicks(9876));
        backward.ShouldBe(new DateTime(2023, 12, 30, 14, 25, 30, kind).AddTicks(9876));
        forward.Kind.ShouldBe(kind);
        backward.Kind.ShouldBe(kind);
        forward.Ticks.ShouldBe(afterNewYear.Ticks);
        calendar.AddBusinessDays(beforeNewYear, 0).ShouldBe(beforeNewYear);
    }

    /// <summary>
    /// 验证在完整无业务日年度之间跳转，并在目标年度找到首个业务日。
    /// </summary>
    [Fact]
    public void AddBusinessDays_SkipsEmptyYears_WithoutDayByDayScan()
    {
        var emptyYear = Enumerable.Range(1, 366).Select(day => new DateTime(2024, 1, 1).AddDays(day - 1));
        var calendar = CreateSet(
            CreateYear(2023, holidays: new[] { new DateTime(2023, 12, 29) }),
            CreateYear(2024, holidays: emptyYear),
            CreateYear(2025, holidays: new[] { new DateTime(2025, 1, 1) }));
        var start = new DateTime(2023, 12, 28, 8, 0, 0);

        calendar.AddBusinessDays(start, 1).ShouldBe(new DateTime(2025, 1, 2, 8, 0, 0));
        calendar.AddBusinessDays(new DateTime(2025, 1, 2, 8, 0, 0), -1).ShouldBe(start);
    }

    /// <summary>
    /// 验证缺年和超出 DateTime 边界时具有不同失败类型，并且不会无限扫描空年。
    /// </summary>
    [Fact]
    public void AddBusinessDays_MissingYearAndRepresentableLimits_FailClearly()
    {
        var onlyYear = CreateSet(CreateYear(2024));
        Should.Throw<KeyNotFoundException>(() => onlyYear.AddBusinessDays(new DateTime(2024, 12, 31), 1));
        Should.Throw<KeyNotFoundException>(() => onlyYear.AddBusinessDays(new DateTime(2024, 1, 1), -1));
        Should.Throw<ArgumentOutOfRangeException>(() => onlyYear.AddBusinessDays(new DateTime(2024, 1, 2), int.MinValue));
        var maximumYear = CreateSet(CreateYear(9999));
        var minimumYear = CreateSet(CreateYear(1));
        Should.Throw<ArgumentOutOfRangeException>(() => maximumYear.AddBusinessDays(DateTime.MaxValue.Date, 1));
        Should.Throw<ArgumentOutOfRangeException>(() => minimumYear.AddBusinessDays(DateTime.MinValue.Date, -1));
        Should.Throw<KeyNotFoundException>(() => onlyYear.AddBusinessDays(DateTime.MinValue.Date, 0));
        maximumYear.AddBusinessDays(new DateTime(9999, 12, 30), 1).ShouldBe(DateTime.MaxValue.Date);
    }

    /// <summary>
    /// 验证零业务日年份会在已加载范围内被安全跳过。
    /// </summary>
    [Fact]
    public void AddBusinessDays_AllEmptyYears_UsesConfiguredRangeAndFailsAtMissingYear()
    {
        var empty2024 = Enumerable.Range(1, 366).Select(day => new DateTime(2024, 1, 1).AddDays(day - 1));
        var empty2025 = Enumerable.Range(1, 365).Select(day => new DateTime(2025, 1, 1).AddDays(day - 1));
        var calendar = CreateSet(CreateYear(2024, holidays: empty2024), CreateYear(2025, holidays: empty2025));

        Should.Throw<KeyNotFoundException>(() => calendar.AddBusinessDays(new DateTime(2024, 6, 1), 1));
        Should.Throw<KeyNotFoundException>(() => calendar.AddBusinessDays(new DateTime(2025, 6, 1), -1));
    }

    /// <summary>
    /// 验证年度快照集合复制后不受输入集合变更影响。
    /// </summary>
    [Fact]
    public void Constructor_CopiesInputCollection()
    {
        var first = CreateYear(2023);
        var years = new List<AnnualBusinessCalendar> { first, CreateYear(2024) };
        var calendar = new BusinessCalendarSet(years);
        years.Clear();
        years.Add(CreateYear(2025));

        calendar.Region.ShouldBe(Region);
        calendar.IsBusinessDay(new DateTime(2024, 1, 2)).ShouldBeTrue();
        Should.Throw<KeyNotFoundException>(() => calendar.IsBusinessDay(new DateTime(2025, 1, 2)));
        first.Year.ShouldBe(2023);
    }

    /// <summary>
    /// 验证构造阶段拒绝空集合、null 项、重复年份和地区大小写差异。
    /// </summary>
    [Fact]
    public void Constructor_RejectsInvalidSnapshotSets()
    {
        Should.Throw<ArgumentNullException>(() => new BusinessCalendarSet(null));
        Should.Throw<ArgumentException>(() => new BusinessCalendarSet(Array.Empty<AnnualBusinessCalendar>()));
        Should.Throw<ArgumentException>(() => new BusinessCalendarSet(new AnnualBusinessCalendar[] { null }));
        Should.Throw<ArgumentException>(() => new BusinessCalendarSet(new[] { CreateYear(2024), CreateYear(2024) }));
        Should.Throw<ArgumentException>(() => new BusinessCalendarSet(new[]
        {
            CreateYear(2024, region: "CN"), CreateYear(2025, region: "cn")
        }));
    }

    /// <summary>
    /// 验证跨年计数和偏移与独立逐日参考规则一致。
    /// </summary>
    [Fact]
    public void CrossYearOperations_MatchDailyOracle()
    {
        var holiday2023 = new[] { new DateTime(2023, 12, 25), new DateTime(2023, 12, 29) };
        var override2023 = new[] { new DateTime(2023, 12, 30) };
        var holiday2024 = new[] { new DateTime(2024, 1, 1), new DateTime(2024, 2, 29) };
        var override2024 = new[] { new DateTime(2024, 1, 6) };
        var holidays = holiday2023.Concat(holiday2024).ToArray();
        var overrides = override2023.Concat(override2024).ToArray();
        var calendar = CreateSet(
            CreateYear(2023, holiday2023, override2023),
            CreateYear(2024, holiday2024, override2024));
        var restDays = new[] { DayOfWeek.Saturday, DayOfWeek.Sunday };

        for (var start = new DateTime(2023, 12, 20); start < new DateTime(2024, 2, 11); start = start.AddDays(1))
        {
            for (var end = new DateTime(2023, 12, 20); end < new DateTime(2024, 2, 11); end = end.AddDays(3))
            {
                calendar.CountBusinessDays(start, end)
                    .ShouldBe(CountOracle(start, end, holidays, overrides, restDays));
            }
        }

        for (var start = new DateTime(2023, 12, 20); start < new DateTime(2024, 2, 11); start = start.AddDays(1))
        {
            foreach (var offset in new[] { -20, -5, -2, -1, 1, 2, 5, 20 })
            {
                if (TryAddOracle(start, offset, holidays, overrides, restDays, out var expected))
                    calendar.AddBusinessDays(start, offset).ShouldBe(expected);
            }
        }
    }

    /// <summary>
    /// 使用指定年度规则创建组合日历。
    /// </summary>
    /// <param name="calendars">年度快照。</param>
    /// <returns>跨年业务日历。</returns>
    private static BusinessCalendarSet CreateSet(params AnnualBusinessCalendar[] calendars) => new(calendars);

    /// <summary>
    /// 创建单年测试规则。
    /// </summary>
    /// <param name="year">公历年份。</param>
    /// <param name="holidays">节假日。</param>
    /// <param name="overrides">补班日期。</param>
    /// <param name="region">业务地区标识。</param>
    /// <returns>指定年度的日历快照。</returns>
    private static AnnualBusinessCalendar CreateYear(int year,
        IEnumerable<DateTime> holidays = null,
        IEnumerable<DateTime> overrides = null,
        string region = Region) =>
        new(year, region, $"v-{year}", "unit-test", holidays, overrides);

    /// <summary>
    /// 通过独立的逐日规则判断业务日。
    /// </summary>
    /// <param name="date">待判断的日期。</param>
    /// <param name="holidays">节假日集合。</param>
    /// <param name="overrides">补班集合。</param>
    /// <param name="restDays">周休息日集合。</param>
    /// <returns>业务日为 true，否则为 false。</returns>
    private static bool IsBusinessDay(DateTime date,
        IReadOnlyCollection<DateTime> holidays,
        IReadOnlyCollection<DateTime> overrides,
        IReadOnlyCollection<DayOfWeek> restDays)
    {
        var day = date.Date;
        if (overrides.Contains(day))
            return true;
        return !restDays.Contains(day.DayOfWeek) && !holidays.Contains(day);
    }

    /// <summary>
    /// 用逐日扫描计算半开区间的期望业务日数。
    /// </summary>
    /// <param name="start">包含的开始日期。</param>
    /// <param name="end">不包含的结束日期。</param>
    /// <param name="holidays">节假日集合。</param>
    /// <param name="overrides">补班集合。</param>
    /// <param name="restDays">周休息日集合。</param>
    /// <returns>正向计数或反向计数的相反数。</returns>
    private static int CountOracle(DateTime start, DateTime end,
        IReadOnlyCollection<DateTime> holidays,
        IReadOnlyCollection<DateTime> overrides,
        IReadOnlyCollection<DayOfWeek> restDays)
    {
        start = start.Date;
        end = end.Date;
        if (start > end)
            return -CountOracle(end, start, holidays, overrides, restDays);

        var count = 0;
        for (var date = start; date < end; date = date.AddDays(1))
        {
            if (IsBusinessDay(date, holidays, overrides, restDays))
                count++;
        }
        return count;
    }

    /// <summary>
    /// 用逐日扫描计算固定测试年度内的期望偏移。
    /// </summary>
    /// <param name="date">起始日期。</param>
    /// <param name="offset">有符号业务日偏移。</param>
    /// <param name="holidays">节假日集合。</param>
    /// <param name="overrides">补班集合。</param>
    /// <param name="restDays">周休息日集合。</param>
    /// <param name="result">成功时返回目标日期。</param>
    /// <returns>目标仍在测试年度范围内时为 true，否则为 false。</returns>
    private static bool TryAddOracle(DateTime date, int offset,
        IReadOnlyCollection<DateTime> holidays,
        IReadOnlyCollection<DateTime> overrides,
        IReadOnlyCollection<DayOfWeek> restDays,
        out DateTime result)
    {
        var remaining = Math.Abs((long)offset);
        var direction = Math.Sign(offset);
        result = date;
        while (remaining > 0)
        {
            result = result.AddDays(direction);
            if (result.Year < 2023 || result.Year > 2024)
                return false;
            if (IsBusinessDay(result, holidays, overrides, restDays))
                remaining--;
        }
        return true;
    }
}
