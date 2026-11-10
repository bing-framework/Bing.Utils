namespace Bing.Date;
/// <summary>
/// ISO 8601 周历转换测试。
/// </summary>
[Trait("DateTimeUT", "IsoWeekCalendar")]
public class IsoWeekCalendarTest
{
    /// <summary>
    /// 验证跨公历年边界的 ISO 周所属年份和周号。
    /// </summary>
    /// <param name="year">公历年份。</param>
    /// <param name="month">公历月份。</param>
    /// <param name="day">公历日。</param>
    /// <param name="weekYear">预期 ISO 周所属年份。</param>
    /// <param name="weekNumber">预期 ISO 周号。</param>
    [Theory]
    [InlineData(2005, 1, 1, 2004, 53)]
    [InlineData(2007, 12, 31, 2008, 1)]
    [InlineData(2021, 1, 1, 2020, 53)]
    [InlineData(2021, 1, 4, 2021, 1)]
    [InlineData(2024, 12, 30, 2025, 1)]
    public void GetIsoWeek_ReturnsIsoWeekYearAndNumber(int year, int month, int day, int weekYear, int weekNumber)
    {
        IsoWeekCalendar.GetIsoWeek(new DateTime(year, month, day)).ShouldBe((weekYear, weekNumber));
    }
    /// <summary>
    /// 验证 ISO 周年包含 52 周或 53 周。
    /// </summary>
    /// <param name="year">ISO 周所属年份。</param>
    /// <param name="expectedWeeks">预期周数。</param>
    [Theory]
    [InlineData(2015, 53)]
    [InlineData(2020, 53)]
    [InlineData(2021, 52)]
    [InlineData(2024, 52)]
    public void GetWeeksInYear_ReturnsExpectedWeekCount(int year, int expectedWeeks)
    {
        IsoWeekCalendar.GetWeeksInYear(year).ShouldBe(expectedWeeks);
    }
    /// <summary>
    /// 验证从 ISO 年周星期恢复公历日期，并明确输出 Kind。
    /// </summary>
    /// <param name="weekYear">ISO 周所属年份。</param>
    /// <param name="weekNumber">ISO 周号。</param>
    /// <param name="dayOfWeek">星期值。</param>
    /// <param name="year">预期公历年份。</param>
    /// <param name="month">预期公历月份。</param>
    /// <param name="day">预期公历日。</param>
    [Theory]
    [InlineData(2020, 53, DayOfWeek.Monday, 2020, 12, 28)]
    [InlineData(2020, 53, DayOfWeek.Thursday, 2020, 12, 31)]
    [InlineData(2020, 53, DayOfWeek.Sunday, 2021, 1, 3)]
    [InlineData(2021, 1, DayOfWeek.Sunday, 2021, 1, 10)]
    public void GetDate_ReturnsMatchingGregorianDate(int weekYear, int weekNumber, DayOfWeek dayOfWeek, int year, int month, int day)
    {
        var result = IsoWeekCalendar.GetDate(weekYear, weekNumber, dayOfWeek);

        result.ShouldBe(new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Unspecified));
        result.Kind.ShouldBe(DateTimeKind.Unspecified);
        IsoWeekCalendar.GetIsoWeek(result).ShouldBe((weekYear, weekNumber));
    }
    /// <summary>
    /// 验证无效周年份、周号及星期值被拒绝。
    /// </summary>
    [Fact]
    public void InvalidComponents_AreRejected()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => IsoWeekCalendar.GetWeeksInYear(0)).ParamName.ShouldBe("weekYear");
        Should.Throw<ArgumentOutOfRangeException>(() => IsoWeekCalendar.GetWeeksInYear(10000)).ParamName.ShouldBe("weekYear");
        Should.Throw<ArgumentOutOfRangeException>(() => IsoWeekCalendar.GetDate(2021, 0, DayOfWeek.Monday)).ParamName.ShouldBe("weekNumber");
        Should.Throw<ArgumentOutOfRangeException>(() => IsoWeekCalendar.GetDate(2021, 53, DayOfWeek.Monday)).ParamName.ShouldBe("weekNumber");
        Should.Throw<ArgumentOutOfRangeException>(() => IsoWeekCalendar.GetDate(2020, 54, DayOfWeek.Monday)).ParamName.ShouldBe("weekNumber");
        Should.Throw<ArgumentOutOfRangeException>(() => IsoWeekCalendar.GetDate(2021, 1, (DayOfWeek)7)).ParamName.ShouldBe("dayOfWeek");
        Should.Throw<ArgumentOutOfRangeException>(() => IsoWeekCalendar.GetDate(2021, 1, (DayOfWeek)(-1))).ParamName.ShouldBe("dayOfWeek");
    }
    /// <summary>
    /// 验证 DateTime 可表示边界在 ISO 周转换中不会发生算术溢出。
    /// </summary>
    [Fact]
    public void DateTimeBoundaries_AreHandledExplicitly()
    {
        IsoWeekCalendar.GetIsoWeek(DateTime.MinValue).ShouldBe((1, 1));
        IsoWeekCalendar.GetIsoWeek(DateTime.MaxValue).ShouldBe((9999, 52));
        IsoWeekCalendar.GetDate(1, 1, DayOfWeek.Monday).ShouldBe(DateTime.MinValue);
        IsoWeekCalendar.GetDate(9999, 52, DayOfWeek.Friday)
            .ShouldBe(new DateTime(9999, 12, 31, 0, 0, 0, DateTimeKind.Unspecified));
        Should.Throw<ArgumentOutOfRangeException>(() => IsoWeekCalendar.GetDate(9999, 52, DayOfWeek.Saturday));
        Should.Throw<ArgumentOutOfRangeException>(() => IsoWeekCalendar.GetDate(9999, 52, DayOfWeek.Sunday));
    }

#if NET6_0_OR_GREATER
    /// <summary>
    /// 以 BCL ISO 周历核对完整的 400 年公历周期。
    /// </summary>
    [Fact]
    public void GregorianCycle_MatchesBclAndRoundTrips()
    {
        var end = new DateTime(2300, 1, 1);
        for (var date = new DateTime(1900, 1, 1); date < end; date = date.AddDays(1))
        {
            var actual = IsoWeekCalendar.GetIsoWeek(date);
            var expectedYear = System.Globalization.ISOWeek.GetYear(date);
            var expectedWeek = System.Globalization.ISOWeek.GetWeekOfYear(date);
            if (actual.WeekYear != expectedYear || actual.WeekNumber != expectedWeek ||
                IsoWeekCalendar.GetDate(actual.WeekYear, actual.WeekNumber, date.DayOfWeek) != date)
                throw new InvalidOperationException($"ISO 周转换不一致: {date:yyyy-MM-dd}");
        }
    }
#endif
}
