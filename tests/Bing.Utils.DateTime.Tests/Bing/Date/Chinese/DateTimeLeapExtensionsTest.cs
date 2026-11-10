using System.Globalization;
using System.Threading.Tasks;

namespace Bing.Date.Chinese;
/// <summary>
/// 农历闰年、闰月和闰日扩展测试。
/// </summary>
/// <remarks>日期判断基于传入日期对应的农历年月日分量。</remarks>
[Trait("DateTimeUT", "ChineseDate.LeapExtensions")]
public class DateTimeLeapExtensionsTest
{
    /// <summary>
    /// 闰年、闰月和闰日判断使用的农历历法。
    /// </summary>
    private readonly ChineseLunisolarCalendar _calendar = new();

    #region IsLeapYear (with calendar)
    /// <summary>
    /// 验证二〇二〇年被判断为农历闰年。
    /// </summary>
    [Fact]
    public void IsLeapYear_LeapYear2020_ReturnsTrue()
    {
        var dt = new DateTime(2020, 6, 1);
        dt.IsLeapYear(_calendar).ShouldBeTrue();
    }
    /// <summary>
    /// 验证二〇二一年被判断为农历平年。
    /// </summary>
    [Fact]
    public void IsLeapYear_NonLeapYear2021_ReturnsFalse()
    {
        var dt = new DateTime(2021, 6, 1);
        dt.IsLeapYear(_calendar).ShouldBeFalse();
    }
    /// <summary>
    /// 验证二〇二三年被判断为农历闰年。
    /// </summary>
    [Fact]
    public void IsLeapYear_LeapYear2023_ReturnsTrue()
    {
        var dt = new DateTime(2023, 5, 1);
        dt.IsLeapYear(_calendar).ShouldBeTrue();
    }
    /// <summary>
    /// 验证春节前的公历日期仍按前一个农历年判断闰年。
    /// </summary>
    [Fact]
    public void IsLeapYear_BeforeLunarNewYear_UsesPreviousLunarYear()
    {
        new DateTime(2024, 1, 31).IsLeapYear(_calendar).ShouldBeTrue();
    }
    /// <summary>
    /// 验证无历法重载在春节、普通月和闰月边界按农历分量判断。
    /// </summary>
    /// <param name="year">公历年份。</param>
    /// <param name="month">公历月份。</param>
    /// <param name="day">公历日。</param>
    /// <param name="leapYear">是否为农历闰年。</param>
    /// <param name="leapMonth">是否为农历闰月。</param>
    [Theory]
    [InlineData(2023, 2, 20, true, false)]
    [InlineData(2023, 3, 21, true, false)]
    [InlineData(2023, 3, 22, true, true)]
    [InlineData(2023, 4, 19, true, true)]
    [InlineData(2023, 4, 20, true, false)]
    [InlineData(2024, 2, 9, true, false)]
    [InlineData(2024, 2, 10, false, false)]
    /// <summary>
    /// 验证无历法参数的重载使用农历日期分量。
    /// </summary>
    public void NoCalendarOverloads_UseLunarDateParts(int year, int month, int day, bool leapYear, bool leapMonth)
    {
        var dt = new DateTime(year, month, day);

        DateTimeLeapExtensions.IsLeapYear(dt).ShouldBe(leapYear);
        dt.IsLeapMonth().ShouldBe(leapMonth);
        dt.IsLeapDay().ShouldBe(leapMonth);
    }
    /// <summary>
    /// 验证显式指定空历法仍会拒绝，避免混淆默认历法与无效参数。
    /// </summary>
    [Fact]
    public void ExplicitNullCalendar_IsRejected()
    {
        var dt = new DateTime(2023, 3, 22);

        Should.Throw<ArgumentNullException>(() => DateTimeLeapExtensions.IsLeapYear(dt, null)).ParamName.ShouldBe("calendar");
        Should.Throw<ArgumentNullException>(() => dt.IsLeapMonth(null)).ParamName.ShouldBe("calendar");
        Should.Throw<ArgumentNullException>(() => dt.IsLeapDay(null)).ParamName.ShouldBe("calendar");
    }
    /// <summary>
    /// 验证无历法重载的日期范围和显式历法保持一致。
    /// </summary>
    [Fact]
    public void NoCalendarOverloads_RespectSupportedDateBoundaries()
    {
        foreach (var date in new[] { _calendar.MinSupportedDateTime, _calendar.MaxSupportedDateTime })
        {
            DateTimeLeapExtensions.IsLeapYear(date).ShouldBe(date.IsLeapYear(_calendar));
            date.IsLeapMonth().ShouldBe(date.IsLeapMonth(_calendar));
            date.IsLeapDay().ShouldBe(date.IsLeapDay(_calendar));
        }

        foreach (var date in new[] { _calendar.MinSupportedDateTime.AddTicks(-1), _calendar.MaxSupportedDateTime.AddTicks(1) })
        {
            Should.Throw<ArgumentOutOfRangeException>(() => DateTimeLeapExtensions.IsLeapYear(date)).ParamName.ShouldBe("dt");
            Should.Throw<ArgumentOutOfRangeException>(() => date.IsLeapMonth()).ParamName.ShouldBe("dt");
            Should.Throw<ArgumentOutOfRangeException>(() => date.IsLeapDay()).ParamName.ShouldBe("dt");
        }
    }
    /// <summary>
    /// 验证默认农历判断读取日期字段，不根据 Kind 换算时区。
    /// </summary>
    /// <param name="kind">输入日期的时间类型。</param>
    [Theory]
    [InlineData(DateTimeKind.Unspecified)]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Local)]
    public void NoCalendarOverloads_UseDateFieldsRegardlessOfKind(DateTimeKind kind)
    {
        var date = new DateTime(2023, 3, 22, 23, 59, 59, kind).AddTicks(9999999);

        DateTimeLeapExtensions.IsLeapYear(date).ShouldBeTrue();
        date.IsLeapMonth().ShouldBeTrue();
        date.IsLeapDay().ShouldBeTrue();
    }
    /// <summary>
    /// 验证共享默认历法支持并发判断。
    /// </summary>
    [Fact]
    public void NoCalendarOverloads_ConcurrentCalls_ReturnConsistentResults()
    {
        Parallel.For(0, 1024, index =>
        {
            var isLeapMonth = index % 2 == 0;
            var dt = isLeapMonth ? new DateTime(2023, 3, 22) : new DateTime(2024, 2, 10);

            DateTimeLeapExtensions.IsLeapYear(dt).ShouldBe(isLeapMonth);
            dt.IsLeapMonth().ShouldBe(isLeapMonth);
            dt.IsLeapDay().ShouldBe(isLeapMonth);
        });
    }

    #endregion

    #region IsLeapMonth (with calendar)
    /// <summary>
    /// 验证二〇二〇年五月的当前闰月映射结果。
    /// </summary>
    [Fact]
    public void IsLeapMonth_WithDateInLeapMonth2020_ReturnsTrue()
    {
        // 2020-05-25 对应农历年2020的闰四月
        var dt = new DateTime(2020, 5, 25);
        dt.IsLeapMonth(_calendar).ShouldBeTrue();
    }
    /// <summary>
    /// 验证公历六月仍能识别农历闰四月。
    /// </summary>
    [Fact]
    public void IsLeapMonth_WithGregorianJuneInLunarLeapMonth2020_ReturnsTrue()
    {
        new DateTime(2020, 6, 1).IsLeapMonth(_calendar).ShouldBeTrue();
    }
    /// <summary>
    /// 验证二〇二三年三月的当前闰月映射结果。
    /// </summary>
    [Fact]
    public void IsLeapMonth_WithDateInLeapMonth2023_ReturnsTrue()
    {
        var dt = new DateTime(2023, 3, 25);
        dt.IsLeapMonth(_calendar).ShouldBeTrue();
    }
    /// <summary>
    /// 验证非闰月日期返回否定结果。
    /// </summary>
    [Fact]
    public void IsLeapMonth_WithDateNotInLeapMonth_ReturnsFalse()
    {
        var dt = new DateTime(2023, 1, 22);
        dt.IsLeapMonth(_calendar).ShouldBeFalse();
    }

    #endregion

    #region IsLeapDay (with calendar)
    /// <summary>
    /// 验证当前闰月映射下的日期被判断为闰日。
    /// </summary>
    [Fact]
    public void IsLeapDay_WithDayInLeapMonth_ReturnsTrue()
    {
        // 2020-05-25 属于农历年2020的闰四月
        var dt = new DateTime(2020, 5, 25);
        dt.IsLeapDay(_calendar).ShouldBeTrue();
    }
    /// <summary>
    /// 验证闰日判断使用农历月份，而非公历月份。
    /// </summary>
    [Fact]
    public void IsLeapDay_WithGregorianJuneInLunarLeapMonth2020_ReturnsTrue()
    {
        new DateTime(2020, 6, 1).IsLeapDay(_calendar).ShouldBeTrue();
    }
    /// <summary>
    /// 验证普通农历日期不属于闰日。
    /// </summary>
    [Fact]
    public void IsLeapDay_WithDayNotInLeapMonth_ReturnsFalse()
    {
        var dt = new DateTime(2023, 1, 22);
        dt.IsLeapDay(_calendar).ShouldBeFalse();
    }

    #endregion
}
