using System.Collections.Generic;
using Bing.Helpers;

namespace Bing.Date;

/// <summary>
/// 日期时间判断测试
/// </summary>
[Trait("DateTimeUT", "DateTimeExtensions.Is")]
public class DateTimeIsTest
{
    #region IsEarlyMorning

    /// <summary>
    /// 测试 - 是否清晨
    /// </summary>
    [Theory]
    [InlineData("2024-01-12 00:00:00", true)]
    [InlineData("2024-01-12 02:00:00", true)]
    [InlineData("2024-01-12 04:00:00", true)]
    [InlineData("2024-01-12 05:59:59", true)]
    [InlineData("2024-01-12 06:00:00", false)]
    [InlineData("2024-01-12 08:00:00", false)]
    [InlineData("2024-01-12 23:59:59", false)]
    public void Test_IsEarlyMorning(string input, bool result)
    {
        var dt = Conv.ToDate(input);
        dt.IsEarlyMorning().ShouldBe(result);
    }

    #endregion

    #region IsLeapYear

    /// <summary>
    /// 测试 - 是否闰年
    /// </summary>
    [Theory]
    [InlineData("2004-01-01", true)]
    [InlineData("2008-01-01", true)]
    [InlineData("2012-01-01", true)]
    [InlineData("2016-01-01", true)]
    [InlineData("2020-01-01", true)]
    [InlineData("2021-01-01", false)]
    [InlineData("2022-01-01", false)]
    [InlineData("2023-01-01", false)]
    [InlineData("2024-01-01", true)]
    public void Test_IsLeapYear(string input, bool result)
    {
        var dt = Conv.ToDate(input);
        dt.IsLeapYear().ShouldBe(result);
    }

    #endregion

    #region IsSameDay(是否为同一天)

    /// <summary>
    /// 测试 - 判断是否同一天
    /// </summary>
    [Theory]
    [MemberData(nameof(IsSameDayValues))]
    public void Test_IsSameDay(DateTime? date, DateTime? compareDate, bool result)
    {
        Assert.Equal(result, DateJudge.IsSameDay(date, compareDate));
    }

    public static List<object[]> IsSameDayValues =>
    [
        [DateTime.Parse("2020-01-04 05:34:55"), DateTime.Parse("2020-01-04 00:00:00"), true],
        [DateTime.Parse("2020-01-04 05:34:55"), DateTime.Parse("2020-01-05 00:00:00"), false],
        [DateTime.Parse("2018-01-04 05:34:55"), DateTime.Parse("2018-01-05 00:00:00"), false],
        [DateTime.Parse("2015-07-31 05:34:55"), DateTime.Parse("2014-07-31 05:34:55"), false],
        [DateTime.Parse("2015-08-31 05:34:55"), DateTime.Parse("2015-07-31 05:34:55"), false],
        [DateTime.Parse("2015-07-30 05:34:55"), DateTime.Parse("2015-07-31 05:34:55"), false],
        [null!, null!, false],
        [DateTime.Parse("1867-12-24 05:34:55"), null!, false],
        [null!, DateTime.Parse("1867-12-24 05:34:55"), false]
    ];

    #endregion

    #region IsSameWeek(是否为同一周)

    /// <summary>
    /// 测试 - 判断是否同一周
    /// </summary>
    [Theory]
    [MemberData(nameof(IsSameWeekValues))]
    public void Test_IsSameWeek(DateTime? date, DateTime? compareDate, DayOfWeek firstDayOfWeek, bool result)
    {
        Assert.Equal(result, DateJudge.IsSameWeek(date, compareDate, firstDayOfWeek));
    }

    public static List<object[]> IsSameWeekValues =>
    [
        [DateTime.Parse("2021-09-05 05:34:55"), DateTime.Parse("2021-09-05 00:00:00"), DayOfWeek.Monday, true],
        [DateTime.Parse("2021-09-01 05:34:55"), DateTime.Parse("2021-09-05 00:00:00"), DayOfWeek.Monday, true],
        [DateTime.Parse("2021-09-01 05:34:55"), DateTime.Parse("2021-08-30 00:00:00"), DayOfWeek.Monday, true],
        [DateTime.Parse("2021-09-30 05:34:55"), DateTime.Parse("2021-10-03 00:00:00"), DayOfWeek.Monday, true],
        [DateTime.Parse("2021-09-01 05:34:55"), DateTime.Parse("2021-09-06 00:00:00"), DayOfWeek.Monday, false],
        [DateTime.Parse("2021-09-01 05:34:55"), DateTime.Parse("2021-09-05 00:00:00"), DayOfWeek.Sunday, false],
        [DateTime.Parse("2021-09-01 05:34:55"), DateTime.Parse("2021-08-30 00:00:00"), DayOfWeek.Sunday, true],
        [DateTime.Parse("2021-09-30 05:34:55"), DateTime.Parse("2021-10-03 00:00:00"), DayOfWeek.Sunday, false],
        [null!, null!, DayOfWeek.Monday, false],
        [DateTime.Parse("1867-12-24 05:34:55"), null!, DayOfWeek.Monday, false],
        [null!, DateTime.Parse("1867-12-24 05:34:55"), DayOfWeek.Monday, false]
    ];

    #endregion

    #region IsSameMonth(是否为同一月)

    /// <summary>
    /// 测试 - 判断是否为同一月
    /// </summary>
    [Theory]
    [MemberData(nameof(IsSameMonthValues))]
    public void Test_IsSameMonth(DateTime? date, DateTime? compareDate, bool result)
    {
        Assert.Equal(result, DateJudge.IsSameMonth(date, compareDate));
    }

    public static List<object[]> IsSameMonthValues =>
    [
        [DateTime.Parse("2020-01-04 05:34:55"), DateTime.Parse("2020-01-05 00:00:00"), true],
        [DateTime.Parse("2018-01-04 05:34:55"), DateTime.Parse("2018-01-05 00:00:00"), true],
        [DateTime.Parse("2015-07-31 05:34:55"), DateTime.Parse("2014-07-31 05:34:55"), false],
        [DateTime.Parse("2015-08-31 05:34:55"), DateTime.Parse("2015-07-31 05:34:55"), false],
        [null!, null!, false],
        [DateTime.Parse("1867-12-24 05:34:55"), null!, false],
        [null!, DateTime.Parse("1867-12-24 05:34:55"), false]
    ];

    #endregion

    #region IsSameYear(是否为同一年)

    /// <summary>
    /// 测试 - 判断是否为同一年
    /// </summary>
    [Theory]
    [MemberData(nameof(IsSameYearValues))]
    public void Test_IsSameYear(DateTime? date, DateTime? compareDate, bool result)
    {
        Assert.Equal(result, DateJudge.IsSameYear(date, compareDate));
    }

    public static List<object[]> IsSameYearValues =>
    [
        [DateTime.Parse("2020-01-04 05:34:55"), DateTime.Parse("2020-01-05 00:00:00"), true],
        [DateTime.Parse("2018-01-04 05:34:55"), DateTime.Parse("2018-01-05 00:00:00"), true],
        [DateTime.Parse("2015-07-31 05:34:55"), DateTime.Parse("2014-07-31 05:34:55"), false],
        [null!, null!, false],
        [DateTime.Parse("1867-12-24 05:34:55"), null!, false],
        [null!, DateTime.Parse("1867-12-24 05:34:55"), false]
    ];

    #endregion
}