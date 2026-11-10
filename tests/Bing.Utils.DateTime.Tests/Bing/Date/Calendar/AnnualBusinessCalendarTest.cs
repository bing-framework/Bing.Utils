using System;
using System.Collections.Generic;
using System.Linq;

namespace Bing.Date.Calendars;
/// <summary>
/// 验证年度业务日历的边界、不可变性及前缀索引结果。
/// </summary>
public class AnnualBusinessCalendarTest
{
    /// <summary>
    /// 测试用日历的地区标识。
    /// </summary>
    private const string Region = "CN";
    /// <summary>
    /// 测试用日历的版本标识。
    /// </summary>
    private const string Version = "2026.1";
    /// <summary>
    /// 测试用日历的数据来源说明。
    /// </summary>
    private const string Source = "unit-test";
    /// <summary>
    /// 验证闰年全年度查询、区间统计和偏移与独立逐日算法一致。
    /// </summary>
    [Fact]
    public void Calendar_LeapYearQueries_MatchIndependentDailyOracle()
    {
        const int year = 2024;
        var holidays = new[]
        {
            new DateTime(year, 1, 1, 9, 0, 0),
            new DateTime(year, 2, 29, 15, 30, 0),
            new DateTime(year, 6, 15, 0, 0, 0)
        };
        var workingDayOverrides = new[]
        {
            new DateTime(year, 2, 29, 7, 0, 0),
            new DateTime(year, 6, 15, 12, 0, 0)
        };
        var restDays = new[] { DayOfWeek.Saturday, DayOfWeek.Sunday };
        var calendar = Create(year, holidays, workingDayOverrides, restDays);

        var expectedTotal = 0;
        for (var date = new DateTime(year, 1, 1); date.Year == year; date = date.AddDays(1))
        {
            var expected = IsReferenceBusinessDay(date, holidays, workingDayOverrides, restDays);
            calendar.IsBusinessDay(date.AddHours(13)).ShouldBe(expected);
            if (expected)
                expectedTotal++;
        }

        calendar.TotalBusinessDays.ShouldBe(expectedTotal);

        var random = new Random(173);
        for (var iteration = 0; iteration < 200; iteration++)
        {
            var firstDay = random.Next(1, 367);
            var secondDay = random.Next(1, 368);
            var start = new DateTime(year, 1, 1).AddDays(firstDay - 1);
            var end = new DateTime(year, 1, 1).AddDays(secondDay - 1);
            calendar.CountBusinessDays(start, end)
                .ShouldBe(CountReferenceBusinessDays(start, end, holidays, workingDayOverrides, restDays));
        }

        for (var date = new DateTime(year, 1, 1); date.Year == year; date = date.AddDays(1))
        {
            foreach (var offset in new[] { -20, -5, -2, -1, 1, 2, 5, 20 })
            {
                var expected = TryAddReferenceBusinessDays(date, offset, holidays, workingDayOverrides, restDays, out var expectedDate);
                if (expected)
                    calendar.AddBusinessDays(date, offset).ShouldBe(expectedDate);
                else
                    Should.Throw<ArgumentOutOfRangeException>(() => calendar.AddBusinessDays(date, offset));
            }
        }
    }
    /// <summary>
    /// 验证每个星期都可以作为自定义休息日并得到对应的年度总数。
    /// </summary>
    [Fact]
    public void Calendar_EachWeekdayCanBeConfiguredAsRestDay()
    {
        const int year = 2025;
        foreach (DayOfWeek restDay in Enum.GetValues(typeof(DayOfWeek)))
        {
            var restDays = new[] { restDay };
            var calendar = Create(year, restDays: restDays);
            var expectedTotal = 0;

            for (var date = new DateTime(year, 1, 1); date.Year == year; date = date.AddDays(1))
            {
                var expected = date.DayOfWeek != restDay;
                calendar.IsBusinessDay(date).ShouldBe(expected);
                if (expected)
                    expectedTotal++;
            }

            calendar.TotalBusinessDays.ShouldBe(expectedTotal);
        }
    }
    /// <summary>
    /// 验证日历建成后不受调用方修改输入集合影响。
    /// </summary>
    [Fact]
    public void Constructor_NormalizesAndCopiesInputCollections()
    {
        var holiday = new DateTime(2024, 1, 8);
        var saturday = new DateTime(2024, 1, 6);
        var holidays = new List<DateTime> { holiday.AddHours(8), holiday.AddHours(19) };
        var workingDayOverrides = new List<DateTime> { holiday.AddHours(13), saturday.AddHours(9) };
        var restDays = new List<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday };
        var calendar = Create(2024, holidays, workingDayOverrides, restDays);
        var total = calendar.TotalBusinessDays;

        holidays.Clear();
        holidays.Add(new DateTime(2024, 1, 9));
        workingDayOverrides.Clear();
        restDays.Clear();
        restDays.Add(DayOfWeek.Monday);

        calendar.IsBusinessDay(holiday).ShouldBeTrue();
        calendar.IsBusinessDay(saturday).ShouldBeTrue();
        calendar.IsBusinessDay(new DateTime(2024, 1, 9)).ShouldBeTrue();
        calendar.TotalBusinessDays.ShouldBe(total);
        calendar.Region.ShouldBe(Region);
        calendar.Version.ShouldBe(Version);
        calendar.Source.ShouldBe(Source);
    }
    /// <summary>
    /// 验证区间统计支持次年元旦哨兵、逆序和自然日半开语义。
    /// </summary>
    [Fact]
    public void CountBusinessDays_UsesHalfOpenDatesAndNextYearSentinel()
    {
        var calendar = Create(2024);
        var start = new DateTime(2024, 1, 1, 23, 59, 0, DateTimeKind.Utc);
        var end = new DateTime(2025, 1, 1, 18, 0, 0, DateTimeKind.Local);

        calendar.CountBusinessDays(start, end).ShouldBe(calendar.TotalBusinessDays);
        calendar.CountBusinessDays(end, start).ShouldBe(-calendar.TotalBusinessDays);
        calendar.CountBusinessDays(start, start.AddHours(-10)).ShouldBe(0);
        calendar.CountBusinessDays(end, end).ShouldBe(0);
    }
    /// <summary>
    /// 验证年度末日的总数可从年内可表示范围推导，且无次年哨兵可用。
    /// </summary>
    [Fact]
    public void Year9999_UsesTotalForFullYearAndRejectsUnrepresentableNextYear()
    {
        var calendar = Create(9999);
        var first = new DateTime(9999, 1, 1);
        var last = DateTime.MaxValue.Date;
        var countBeforeLast = calendar.CountBusinessDays(first, last);

        (countBeforeLast + (calendar.IsBusinessDay(last) ? 1 : 0)).ShouldBe(calendar.TotalBusinessDays);
        calendar.CountBusinessDays(first, DateTime.MaxValue).ShouldBe(countBeforeLast);
        Should.Throw<ArgumentOutOfRangeException>(() => calendar.AddBusinessDays(last, 1));
    }
    /// <summary>
    /// 验证全部自然日设为节假日时无业务日且任意非零偏移均快速失败。
    /// </summary>
    [Fact]
    public void Calendar_WhenEveryDateIsHoliday_HasNoBusinessDays()
    {
        const int year = 2024;
        var holidays = new List<DateTime>();
        for (var date = new DateTime(year, 1, 1); date.Year == year; date = date.AddDays(1))
            holidays.Add(date);

        var calendar = Create(year, holidays);

        calendar.TotalBusinessDays.ShouldBe(0);
        calendar.IsBusinessDay(new DateTime(year, 2, 1)).ShouldBeFalse();
        Should.Throw<ArgumentOutOfRangeException>(() =>
            calendar.AddBusinessDays(new DateTime(year, 2, 1), 1));
        Should.Throw<ArgumentOutOfRangeException>(() =>
            calendar.AddBusinessDays(new DateTime(year, 2, 1), -1));
    }
    /// <summary>
    /// 验证最早可表示年份可统计到次年元旦并执行年内偏移。
    /// </summary>
    [Fact]
    public void Year1_SupportsCalendarBoundaries()
    {
        var calendar = Create(1);
        var first = DateTime.MinValue;
        var nextYear = new DateTime(2, 1, 1);

        calendar.IsBusinessDay(first).ShouldBeTrue();
        calendar.CountBusinessDays(first, nextYear).ShouldBe(calendar.TotalBusinessDays);
        calendar.AddBusinessDays(first, 1).Date.ShouldBe(new DateTime(1, 1, 2));
        Should.Throw<ArgumentOutOfRangeException>(() =>
            calendar.AddBusinessDays(first, -1));
    }
    /// <summary>
    /// 验证正负偏移不计起点，并保留时分秒、亚毫秒 ticks 和 Kind。
    /// </summary>
    /// <param name="kind">测试日期使用的 <see cref="DateTimeKind"/>。</param>
    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void AddBusinessDays_PreservesTimePrecisionAndKind(DateTimeKind kind)
    {
        var calendar = Create(2024, holidays: new[] { new DateTime(2024, 1, 8) });
        var friday = new DateTime(2024, 1, 5, 14, 25, 30, kind).AddTicks(9876);
        var tuesday = new DateTime(2024, 1, 9, 14, 25, 30, kind).AddTicks(9876);

        var forward = calendar.AddBusinessDays(friday, 1);
        var backward = calendar.AddBusinessDays(tuesday, -1);

        forward.ShouldBe(tuesday);
        backward.ShouldBe(friday);
        forward.Kind.ShouldBe(kind);
        backward.Kind.ShouldBe(kind);
        (forward.Ticks % TimeSpan.TicksPerMillisecond).ShouldBe(9876);
        (backward.Ticks % TimeSpan.TicksPerMillisecond).ShouldBe(9876);
        calendar.AddBusinessDays(friday, 0).ShouldBe(friday);
    }
    /// <summary>
    /// 验证年度外日期、输入日期和极值偏移均快速拒绝。
    /// </summary>
    [Fact]
    public void Calendar_RejectsOutOfYearDatesAndOffsets()
    {
        var calendar = Create(2024);

        Should.Throw<ArgumentOutOfRangeException>(() => calendar.IsBusinessDay(new DateTime(2023, 12, 31)));
        Should.Throw<ArgumentOutOfRangeException>(() => calendar.CountBusinessDays(
            new DateTime(2024, 1, 1), new DateTime(2025, 1, 2)));
        Should.Throw<ArgumentOutOfRangeException>(() => calendar.CountBusinessDays(
            new DateTime(2024, 1, 1), new DateTime(2026, 1, 1)));
        Should.Throw<ArgumentOutOfRangeException>(() => calendar.AddBusinessDays(
            new DateTime(2025, 1, 1), 0));
        Should.Throw<ArgumentOutOfRangeException>(() => calendar.AddBusinessDays(
            new DateTime(2024, 1, 2), int.MinValue));
        Should.Throw<ArgumentOutOfRangeException>(() => calendar.AddBusinessDays(
            new DateTime(2024, 12, 31), int.MaxValue));
    }
    /// <summary>
    /// 验证节假日与补班日期必须属于配置年度。
    /// </summary>
    [Fact]
    public void Constructor_RejectsHolidayOrOverrideFromAnotherYear()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => Create(2024,
            holidays: new[] { new DateTime(2023, 12, 31) }));
        Should.Throw<ArgumentOutOfRangeException>(() => Create(2024,
            workingDayOverrides: new[] { new DateTime(2025, 1, 1) }));
    }
    /// <summary>
    /// 验证年度和日历元数据的无效输入被拒绝。
    /// </summary>
    [Fact]
    public void Constructor_RejectsInvalidYearAndBlankMetadata()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new AnnualBusinessCalendar(0, Region, Version, Source));
        Should.Throw<ArgumentOutOfRangeException>(() => new AnnualBusinessCalendar(10000, Region, Version, Source));
        Should.Throw<ArgumentException>(() => new AnnualBusinessCalendar(2024, null, Version, Source));
        Should.Throw<ArgumentException>(() => new AnnualBusinessCalendar(2024, " ", Version, Source));
        Should.Throw<ArgumentException>(() => new AnnualBusinessCalendar(2024, Region, null, Source));
        Should.Throw<ArgumentException>(() => new AnnualBusinessCalendar(2024, Region, "", Source));
        Should.Throw<ArgumentException>(() => new AnnualBusinessCalendar(2024, Region, Version, null));
        Should.Throw<ArgumentException>(() => new AnnualBusinessCalendar(2024, Region, Version, "\t"));
    }
    /// <summary>
    /// 创建使用测试元数据的年度日历。
    /// </summary>
    /// <param name="year">日历年度。</param>
    /// <param name="holidays">节假日集合。</param>
    /// <param name="workingDayOverrides">补班日期集合。</param>
    /// <param name="restDays">每周休息日集合。</param>
    /// <returns>使用测试元数据构造的年度日历。</returns>
    private static AnnualBusinessCalendar Create(
        int year,
        IEnumerable<DateTime> holidays = null,
        IEnumerable<DateTime> workingDayOverrides = null,
        IEnumerable<DayOfWeek> restDays = null) =>
        new(year, Region, Version, Source, holidays, workingDayOverrides, restDays);
    /// <summary>
    /// 使用简单的逐日规则判断给定自然日是否为业务日。
    /// </summary>
    /// <param name="date">待判断日期。</param>
    /// <param name="holidays">节假日集合。</param>
    /// <param name="workingDayOverrides">优先于休息日和节假日的补班集合。</param>
    /// <param name="restDays">每周休息日集合。</param>
    /// <returns>按参考规则为业务日时为 <see langword="true"/>。</returns>
    private static bool IsReferenceBusinessDay(
        DateTime date,
        IEnumerable<DateTime> holidays,
        IEnumerable<DateTime> workingDayOverrides,
        IEnumerable<DayOfWeek> restDays)
    {
        var day = date.Date;
        if (workingDayOverrides.Any(value => value.Date == day))
            return true;
        return !restDays.Contains(day.DayOfWeek) && !holidays.Any(value => value.Date == day);
    }
    /// <summary>
    /// 使用逐日参考规则统计半开日期区间内的业务日数量。
    /// </summary>
    /// <param name="startInclusive">包含的起始日期。</param>
    /// <param name="endExclusive">不包含的结束日期。</param>
    /// <param name="holidays">节假日集合。</param>
    /// <param name="workingDayOverrides">补班日期集合。</param>
    /// <param name="restDays">每周休息日集合。</param>
    /// <returns>正向区间的业务日数量，反向区间返回其相反数。</returns>
    private static int CountReferenceBusinessDays(
        DateTime startInclusive,
        DateTime endExclusive,
        IEnumerable<DateTime> holidays,
        IEnumerable<DateTime> workingDayOverrides,
        IEnumerable<DayOfWeek> restDays)
    {
        var start = startInclusive.Date;
        var end = endExclusive.Date;
        if (start > end)
            return -CountReferenceBusinessDays(end, start, holidays, workingDayOverrides, restDays);

        var count = 0;
        for (var date = start; date < end; date = date.AddDays(1))
        {
            if (IsReferenceBusinessDay(date, holidays, workingDayOverrides, restDays))
                count++;
        }

        return count;
    }
    /// <summary>
    /// 用逐日扫描的独立参考规则尝试执行年度内业务日偏移。
    /// </summary>
    /// <param name="start">起始日期，不计入偏移。</param>
    /// <param name="businessDays">有符号业务日偏移量。</param>
    /// <param name="holidays">节假日集合。</param>
    /// <param name="workingDayOverrides">补班日期集合。</param>
    /// <param name="restDays">每周休息日集合。</param>
    /// <param name="result">成功时的目标日期；越界时为导致失败的日期。</param>
    /// <returns>目标仍在起始年度内时为 <see langword="true"/>。</returns>
    private static bool TryAddReferenceBusinessDays(
        DateTime start,
        int businessDays,
        IEnumerable<DateTime> holidays,
        IEnumerable<DateTime> workingDayOverrides,
        IEnumerable<DayOfWeek> restDays,
        out DateTime result)
    {
        result = start;
        var direction = Math.Sign(businessDays);
        var remaining = Math.Abs((long)businessDays);
        while (remaining > 0)
        {
            result = result.AddDays(direction);
            if (result.Year != start.Year)
                return false;
            if (IsReferenceBusinessDay(result, holidays, workingDayOverrides, restDays))
                remaining--;
        }

        return true;
    }
}
