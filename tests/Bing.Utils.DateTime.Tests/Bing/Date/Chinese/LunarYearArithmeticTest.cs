using System;

namespace Bing.Date.Chinese;
/// <summary>
/// 农历按年偏移及周年日期规则。
/// </summary>
public class LunarYearArithmeticTest
{
    /// <summary>
    /// 验证普通农历周年偏移跨年后保留月份和日期。
    /// </summary>
    [Fact]
    public void OrdinaryYearOffset_PreservesMonthAndDayAcrossNewYear()
    {
        var firstDay = ChineseLunarCalendar.Create(2023, 1, 1);
        var anniversary = firstDay.AddYears(1);

        anniversary.ShouldBe(ChineseLunarCalendar.Create(2024, 1, 1));
        anniversary.ToDateTime().ShouldBe(new DateTime(2024, 2, 10));
        anniversary.AddYears(-1).ShouldBe(firstDay);
        firstDay.InYear(2024).ShouldBe(anniversary);
        firstDay.AddYears(0).ShouldBe(firstDay);
    }

    /// <summary>
    /// 验证目标年缺少同名闰月时按指定策略拒绝或转为平月。
    /// </summary>
    [Fact]
    public void LeapMonth_RejectsMissingMonthUnlessRegularMonthRequested()
    {
        var leapDay = ChineseLunarCalendar.Create(2023, 2, 1, isLeapMonth: true);

        Should.Throw<ArgumentOutOfRangeException>(() => leapDay.AddYears(1)).ParamName.ShouldBe("years");
        Should.Throw<ArgumentOutOfRangeException>(() => leapDay.InYear(2024)).ParamName.ShouldBe("year");
        leapDay.AddYears(1, LunarLeapMonthOverflow.UseRegularMonth)
            .ShouldBe(ChineseLunarCalendar.Create(2024, 2, 1));
        leapDay.InYear(2042).ShouldBe(ChineseLunarCalendar.Create(2042, 2, 1, isLeapMonth: true));
        leapDay.InYear(2023).ShouldBe(leapDay);
    }

    /// <summary>
    /// 验证目标月份缺少原日期时可拒绝或夹取末日，且与闰月策略独立。
    /// </summary>
    [Fact]
    public void MissingDay_RejectsOrClampsIndependentlyOfLeapMonthPolicy()
    {
        var dayThirty = ChineseLunarCalendar.Create(2023, 2, 30);
        Should.Throw<ArgumentOutOfRangeException>(() => dayThirty.InYear(2025));
        dayThirty.InYear(2025, missingDay: LunarDayOverflow.ClampToLastDay)
            .ShouldBe(ChineseLunarCalendar.Create(2025, 2, 29));

        var leapDay = ChineseLunarCalendar.Create(2023, 2, 29, isLeapMonth: true);
        leapDay.InYear(2025, LunarLeapMonthOverflow.UseRegularMonth)
            .ShouldBe(ChineseLunarCalendar.Create(2025, 2, 29));

        var leapThirty = ChineseLunarCalendar.Create(1914, 5, 30, isLeapMonth: true);
        Should.Throw<ArgumentOutOfRangeException>(() => leapThirty.InYear(1915, LunarLeapMonthOverflow.UseRegularMonth));
        leapThirty.InYear(1915, LunarLeapMonthOverflow.UseRegularMonth, LunarDayOverflow.ClampToLastDay)
            .ShouldBe(ChineseLunarCalendar.Create(1915, 5, 29));
    }

    /// <summary>
    /// 验证无效日期、策略和超出支持范围的年份均被拒绝。
    /// </summary>
    [Fact]
    public void InvalidInputs_RejectPoliciesAndOutOfRangeYears()
    {
        var date = ChineseLunarCalendar.Create(2023, 1, 1);
        Should.Throw<InvalidOperationException>(() => default(LunarDate).AddYears(1));
        Should.Throw<InvalidOperationException>(() => default(LunarDate).InYear(2024));
        Should.Throw<ArgumentOutOfRangeException>(() => date.AddYears(int.MaxValue)).ParamName.ShouldBe("years");
        Should.Throw<ArgumentOutOfRangeException>(() => date.AddYears(int.MinValue)).ParamName.ShouldBe("years");
        Should.Throw<ArgumentOutOfRangeException>(() => date.InYear(1900)).ParamName.ShouldBe("year");
        Should.Throw<ArgumentOutOfRangeException>(() => date.InYear(2101)).ParamName.ShouldBe("year");
        Should.Throw<ArgumentOutOfRangeException>(() => date.AddYears(0, (LunarLeapMonthOverflow)99))
            .ParamName.ShouldBe("missingLeapMonth");
        Should.Throw<ArgumentOutOfRangeException>(() => date.AddYears(0, missingDay: (LunarDayOverflow)99))
            .ParamName.ShouldBe("missingDay");
    }

    /// <summary>
    /// 验证首末支持年份内的运算有效，超出边界时抛出异常。
    /// </summary>
    [Fact]
    public void FirstAndLastSupportedYears_AcceptInRangeResults()
    {
        var first = ChineseLunarCalendar.MinDate.ToLunarDate();
        var last = ChineseLunarCalendar.MaxDate.ToLunarDate();

        first.InYear(first.Year).ShouldBe(first);
        last.InYear(last.Year).ShouldBe(last);
        Should.Throw<ArgumentOutOfRangeException>(() => first.AddYears(-1));
        Should.Throw<ArgumentOutOfRangeException>(() => last.AddYears(1));
    }
}
