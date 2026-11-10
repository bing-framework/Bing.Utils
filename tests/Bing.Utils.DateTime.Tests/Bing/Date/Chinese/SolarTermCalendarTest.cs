using System.Globalization;

namespace Bing.Date.Chinese;
/// <summary>
/// 验证类型化节气查询与北京时间日期边界。
/// </summary>
public class SolarTermCalendarTest
{
    /// <summary>
    /// 验证 UTC+8 午夜边界及同一时间点的不同偏移表示。
    /// </summary>
    [Fact]
    public void Instant_UsesBeijingDateAndIgnoresRepresentationOffset()
    {
        var instant = new DateTimeOffset(2023, 6, 20, 16, 0, 0, TimeSpan.Zero);
        SolarTermCalendar.GetTerm(instant.AddTicks(-1)).ShouldBeNull();
        foreach (var hours in new[] { -14, -5, 0, 8, 14 })
        {
            var result = SolarTermCalendar.GetTerm(instant.ToOffset(TimeSpan.FromHours(hours))).Value;
            result.Term.ShouldBe(ChineseSolarTerms.SummerSolstice);
            result.Date.Ticks.ShouldBe(new DateTime(2023, 6, 21).Ticks);
            result.Date.Kind.ShouldBe(DateTimeKind.Unspecified);
        }
        SolarTermCalendar.GetTerm(instant.AddDays(1)).ShouldBeNull();
        SolarTermCalendar.GetPrevious(instant).Value.Term.ShouldBe(ChineseSolarTerms.GrainInEar);
        SolarTermCalendar.GetNext(instant).Value.Term.ShouldBe(ChineseSolarTerms.SlightHeat);
    }
    /// <summary>
    /// 验证日期字段入口不受 Kind 及时间部分影响。
    /// </summary>
    /// <param name="kind">日期种类。</param>
    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void DateFields_IgnoreKindAndTime(DateTimeKind kind) =>
        SolarTermCalendar.GetTerm(new DateTime(2023, 6, 21, 23, 59, 59, kind)).Value.Term
            .ShouldBe(ChineseSolarTerms.SummerSolstice);
    /// <summary>
    /// 验证全表映射一致，避免枚举起点和节气表顺序混淆。
    /// </summary>
    [Fact]
    public void AllSupportedYears_MatchExistingTableAndHave24Terms()
    {
        var lunar = new ChineseLunisolarCalendar();
        for (var year = 1900; year <= 2100; year++)
        {
            var count = 0;
            for (var date = new DateTime(year, 1, 1); date.Year == year; date = date.AddDays(1))
            {
                var item = SolarTermCalendar.GetTerm(date);
                (item?.Term).ShouldBe(ChineseSolarTermHelper.GetSolarTermEnum(lunar, date));
                if (item.HasValue) count++;
            }
            count.ShouldBe(24);
        }
    }
    /// <summary>
    /// 验证范围外拒绝与范围内无相邻结果可以区分。
    /// </summary>
    [Fact]
    public void RangeAndNeighbors_AreExplicit()
    {
        SolarTermCalendar.GetPrevious(SolarTermCalendar.MinDate).ShouldBeNull();
        SolarTermCalendar.GetNext(SolarTermCalendar.MaxDate).ShouldBeNull();
        SolarTermCalendar.GetPrevious(new DateTime(2024, 1, 1)).Value.Date.ShouldBe(new DateTime(2023, 12, 22));
        SolarTermCalendar.GetNext(new DateTime(2024, 12, 31)).Value.Date.ShouldBe(new DateTime(2025, 1, 5));
        Should.Throw<ArgumentOutOfRangeException>(() => SolarTermCalendar.GetTerm(new DateTime(1899, 12, 31)));
        Should.Throw<ArgumentOutOfRangeException>(() => SolarTermCalendar.GetNext(new DateTime(2101, 1, 1)));
        Should.Throw<ArgumentOutOfRangeException>(() => SolarTermCalendar.GetPrevious(DateTimeOffset.MaxValue));
        Should.Throw<ArgumentOutOfRangeException>(() => SolarTermCalendar.GetTerm(DateTimeOffset.MinValue));
        var first = new DateTimeOffset(1899, 12, 31, 16, 0, 0, TimeSpan.Zero);
        SolarTermCalendar.GetTerm(first).ShouldBeNull();
        Should.Throw<ArgumentOutOfRangeException>(() => SolarTermCalendar.GetTerm(first.AddTicks(-1)));
        var end = new DateTimeOffset(2100, 12, 31, 16, 0, 0, TimeSpan.Zero);
        SolarTermCalendar.GetNext(end.AddTicks(-1)).ShouldBeNull();
        Should.Throw<ArgumentOutOfRangeException>(() => SolarTermCalendar.GetTerm(end));
    }
}
