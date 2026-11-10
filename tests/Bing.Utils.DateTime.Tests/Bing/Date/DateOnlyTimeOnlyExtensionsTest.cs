#if NET6_0_OR_GREATER
using System.Collections.Generic;
using Bing.Date.Calendars;
using Bing.Date.Chinese;
using Bing.Date.TimeZones;

namespace Bing.Date;
/// <summary>
/// DateOnly 和 TimeOnly 日期模块适配测试。
/// </summary>
[Trait("DateTimeUT", "DateOnlyTimeOnlyExtensions")]
public class DateOnlyTimeOnlyExtensionsTest
{
    /// <summary>
    /// 验证 DateOnly 与农历日期在闰月中双向转换。
    /// </summary>
    [Fact]
    public void LunarConversion_LeapMonth_RoundTrips()
    {
        var date = new DateOnly(2023, 3, 22);
        var lunar = date.ToLunarDate();

        lunar.ShouldBe(ChineseLunarCalendar.Create(2023, 2, 1, isLeapMonth: true));
        lunar.ToDateOnly().ShouldBe(date);
        DateOnlyTimeOnlyExtensions.TryToLunarDate(new DateOnly(1901, 2, 18), out var unsupported).ShouldBeFalse();
        unsupported.ShouldBe(default(LunarDate));
        Should.Throw<ArgumentOutOfRangeException>(() => DateOnly.MinValue.ToLunarDate());
        Should.Throw<ArgumentOutOfRangeException>(() => DateOnly.MaxValue.ToLunarDate());
        Should.Throw<InvalidOperationException>(() => default(LunarDate).ToDateOnly());
    }
    /// <summary>
    /// 验证 DateOnly ISO 周适配正确表示跨公历年周，并支持反向恢复。
    /// </summary>
    [Fact]
    public void IsoWeek_CrossYear_RoundTrips()
    {
        var date = new DateOnly(2024, 12, 30);

        date.GetIsoWeek().ShouldBe((2025, 1));
        DateOnlyTimeOnlyExtensions.GetDateFromIsoWeek(2025, 1, DayOfWeek.Monday).ShouldBe(date);
        Should.Throw<ArgumentOutOfRangeException>(
            () => DateOnlyTimeOnlyExtensions.GetDateFromIsoWeek(2021, 53, DayOfWeek.Monday));
        Should.Throw<ArgumentOutOfRangeException>(
            () => DateOnlyTimeOnlyExtensions.GetDateFromIsoWeek(9999, 52, DayOfWeek.Saturday));
    }
    /// <summary>
    /// 验证年度业务日历的 DateOnly 适配保留半开区间和例外日期规则。
    /// </summary>
    [Fact]
    public void AnnualBusinessCalendar_UsesHalfOpenDateIntervals()
    {
        var calendar = new AnnualBusinessCalendar(2024, "test", "v1", "unit-test",
            holidays: new[] { new DateTime(2024, 1, 1) },
            workingDayOverrides: new[] { new DateTime(2024, 1, 6) });

        calendar.IsBusinessDay(new DateOnly(2024, 1, 1)).ShouldBeFalse();
        calendar.IsBusinessDay(new DateOnly(2024, 1, 6)).ShouldBeTrue();
        calendar.CountBusinessDays(new DateOnly(2024, 1, 5), new DateOnly(2024, 1, 8)).ShouldBe(2);
        calendar.AddBusinessDays(new DateOnly(2024, 1, 5), 1).ShouldBe(new DateOnly(2024, 1, 6));
        calendar.CountBusinessDays(new DateOnly(2024, 1, 8), new DateOnly(2024, 1, 5)).ShouldBe(-2);
        Should.Throw<ArgumentOutOfRangeException>(() => calendar.IsBusinessDay(DateOnly.MinValue));
        Should.Throw<ArgumentOutOfRangeException>(() => calendar.AddBusinessDays(new DateOnly(2024, 1, 5), int.MaxValue));
    }
    /// <summary>
    /// 验证跨年业务日历的 DateOnly 适配按已加载年度处理区间和偏移。
    /// </summary>
    [Fact]
    public void BusinessCalendarSet_TraversesLoadedYearBoundary()
    {
        var year2023 = new AnnualBusinessCalendar(2023, "test", "v1", "unit-test");
        var year2024 = new AnnualBusinessCalendar(2024, "test", "v1", "unit-test",
            holidays: new[] { new DateTime(2024, 1, 1) });
        var calendar = new BusinessCalendarSet(new[] { year2023, year2024 });

        calendar.CountBusinessDays(new DateOnly(2023, 12, 29), new DateOnly(2024, 1, 3)).ShouldBe(2);
        calendar.AddBusinessDays(new DateOnly(2023, 12, 29), 1).ShouldBe(new DateOnly(2024, 1, 2));
        calendar.IsBusinessDay(new DateOnly(2024, 1, 1)).ShouldBeFalse();
        Should.Throw<KeyNotFoundException>(() => calendar.IsBusinessDay(new DateOnly(2025, 1, 1)));
        Should.Throw<KeyNotFoundException>(() => calendar.CountBusinessDays(new DateOnly(2023, 12, 29), new DateOnly(2025, 1, 2)));
    }
    /// <summary>
    /// 验证 TimeOnly 按固定显式偏移解析时保留 100 纳秒刻度。
    /// </summary>
    [Fact]
    public void ResolveLocalTime_FixedOffset_PreservesTicksAndDateTimeBoundaries()
    {
        var zone = TimeZoneInfo.CreateCustomTimeZone("Fixed+8-DateOnly", TimeSpan.FromHours(8), "Fixed+8", "Fixed+8");
        var date = new DateOnly(2024, 1, 1);
        var time = new TimeOnly(12, 34, 56).Add(TimeSpan.FromTicks(9876));
        var result = date.ResolveLocalTime(time, zone);
        var expectedLocal = date.ToDateTime(time, DateTimeKind.Unspecified);

        result.Offset.ShouldBe(TimeSpan.FromHours(8));
        result.DateTime.Ticks.ShouldBe(expectedLocal.Ticks);
        result.UtcDateTime.Ticks.ShouldBe(expectedLocal.Ticks - TimeSpan.FromHours(8).Ticks);
        new DateOnly(1, 1, 1).ResolveLocalTime(TimeOnly.MinValue, TimeZoneInfo.Utc)
            .UtcDateTime.ShouldBe(DateTime.MinValue);
        new DateOnly(9999, 12, 31).ResolveLocalTime(TimeOnly.MaxValue, TimeZoneInfo.Utc)
            .UtcDateTime.ShouldBe(DateTime.MaxValue);
        Should.Throw<ArgumentNullException>(() => date.ResolveLocalTime(time, null)).ParamName.ShouldBe("sourceTimeZone");
    }
    /// <summary>
    /// 验证显式时区解析沿用现有 DST 缺失和重复时间策略。
    /// </summary>
    [Fact]
    public void ResolveLocalTime_DstGapAndFold_FollowsExplicitPolicy()
    {
        var zone = CreateDaylightSavingZone();
        var gapDate = new DateOnly(2024, 3, 10);
        var foldDate = new DateOnly(2024, 11, 3);
        var foldTime = new TimeOnly(1, 30, 0).Add(TimeSpan.FromTicks(2468));

        Should.Throw<ArgumentException>(() => gapDate.ResolveLocalTime(new TimeOnly(2, 30), zone));
        Should.Throw<ArgumentException>(() => foldDate.ResolveLocalTime(foldTime, zone));

        var earlier = foldDate.ResolveLocalTime(foldTime, zone, AmbiguousTimeResolution.Earlier);
        var later = foldDate.ResolveLocalTime(foldTime, zone, AmbiguousTimeResolution.Later);

        earlier.UtcDateTime.ShouldBeLessThan(later.UtcDateTime);
        (later.UtcDateTime - earlier.UtcDateTime).ShouldBe(TimeSpan.FromHours(1));
        earlier.DateTime.Ticks.ShouldBe(foldDate.ToDateTime(foldTime).Ticks);
        later.DateTime.Ticks.ShouldBe(foldDate.ToDateTime(foldTime).Ticks);
        Should.Throw<ArgumentOutOfRangeException>(() =>
            foldDate.ResolveLocalTime(foldTime, zone, (AmbiguousTimeResolution)99));
    }
    /// <summary>
    /// 验证 DateOnly 业务日历扩展明确拒绝 null 日历。
    /// </summary>
    [Fact]
    public void BusinessCalendarAdapters_RejectNullCalendars()
    {
        var date = new DateOnly(2024, 1, 1);

        Should.Throw<ArgumentNullException>(() => DateOnlyTimeOnlyExtensions.IsBusinessDay((AnnualBusinessCalendar)null, date))
            .ParamName.ShouldBe("calendar");
        Should.Throw<ArgumentNullException>(() => DateOnlyTimeOnlyExtensions.CountBusinessDays(
            (AnnualBusinessCalendar)null, date, date)).ParamName.ShouldBe("calendar");
        Should.Throw<ArgumentNullException>(() => DateOnlyTimeOnlyExtensions.AddBusinessDays(
            (AnnualBusinessCalendar)null, date, 0)).ParamName.ShouldBe("calendar");
        Should.Throw<ArgumentNullException>(() => DateOnlyTimeOnlyExtensions.IsBusinessDay((BusinessCalendarSet)null, date))
            .ParamName.ShouldBe("calendar");
        Should.Throw<ArgumentNullException>(() => DateOnlyTimeOnlyExtensions.CountBusinessDays(
            (BusinessCalendarSet)null, date, date)).ParamName.ShouldBe("calendar");
        Should.Throw<ArgumentNullException>(() => DateOnlyTimeOnlyExtensions.AddBusinessDays(
            (BusinessCalendarSet)null, date, 0)).ParamName.ShouldBe("calendar");
    }

    private static TimeZoneInfo CreateDaylightSavingZone() => TimeZoneInfo.CreateCustomTimeZone(
        "TestDST-DateOnly", TimeSpan.FromHours(-5), "TestDST", "Standard", "Daylight", new[]
        {
            TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31),
                TimeSpan.FromHours(1),
                TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 2, 0, 0), 3, 10),
                TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 2, 0, 0), 11, 3))
        });
}
#endif
