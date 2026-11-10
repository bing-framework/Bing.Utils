using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Bing.Date.Chinese;
/// <summary>
/// 验证公历与结构化农历日期的双向转换、闰月映射和日期运算。
/// </summary>
/// <remarks>
/// 固定日期依据香港天文台 2023 年和 2024 年公历农历对照表，避免只用同一实现验证自身结果。
/// </remarks>
[Trait("DateTimeUT", "ChineseDate.LunarDate")]
public class LunarDateIntegrationTest
{
    /// <summary>
    /// 验证香港天文台公布的普通二月、闰二月、三月和春节前后日期映射。
    /// </summary>
    /// <param name="year">公历年份。</param>
    /// <param name="month">公历月份。</param>
    /// <param name="day">公历日。</param>
    /// <param name="lunarYear">预期农历年份。</param>
    /// <param name="lunarMonth">预期农历显示月份。</param>
    /// <param name="lunarDay">预期农历日。</param>
    /// <param name="isLeapMonth">日期是否处于闰月。</param>
    /// <param name="calendarMonth">预期 BCL 农历月份槽位。</param>
    /// <remarks>
    /// 参考 https://www.hko.gov.hk/en/gts/time/calendar/text/files/T2023e.txt 和
    /// https://www.hko.gov.hk/en/gts/time/calendar/text/files/T2024e.txt。
    /// </remarks>
    [Theory]
    [InlineData(2023, 2, 20, 2023, 2, 1, false, 2)]
    [InlineData(2023, 3, 22, 2023, 2, 1, true, 3)]
    [InlineData(2023, 4, 20, 2023, 3, 1, false, 4)]
    [InlineData(2024, 2, 9, 2023, 12, 30, false, 13)]
    [InlineData(2024, 2, 10, 2024, 1, 1, false, 1)]
    public void FromDate_OfficialCalendarVectors_ReturnsLunarComponents(
        int year, int month, int day, int lunarYear, int lunarMonth, int lunarDay,
        bool isLeapMonth, int calendarMonth)
    {
        var actual = new DateTime(year, month, day).ToLunarDate();

        actual.IsValid.ShouldBeTrue();
        actual.Year.ShouldBe(lunarYear);
        actual.Month.ShouldBe(lunarMonth);
        actual.Day.ShouldBe(lunarDay);
        actual.IsLeapMonth.ShouldBe(isLeapMonth);
        actual.CalendarMonth.ShouldBe(calendarMonth);
    }
    /// <summary>
    /// 验证新日期类型可区分普通二月、闰二月和闰月后的三月。
    /// </summary>
    [Fact]
    public void Create_LeapMonthSequence_MapsBothDirections()
    {
        var ordinaryMonth = ChineseLunarCalendar.Create(2023, 2, 1);
        var leapMonth = ChineseLunarCalendar.Create(2023, 2, 1, isLeapMonth: true);
        var followingMonth = ChineseLunarCalendar.Create(2023, 3, 1);

        ordinaryMonth.ToDateTime().ShouldBe(new DateTime(2023, 2, 20, 0, 0, 0, DateTimeKind.Unspecified));
        leapMonth.ToDateTime().ShouldBe(new DateTime(2023, 3, 22, 0, 0, 0, DateTimeKind.Unspecified));
        followingMonth.ToDateTime().ShouldBe(new DateTime(2023, 4, 20, 0, 0, 0, DateTimeKind.Unspecified));

        ordinaryMonth.AddMonths(1).ShouldBe(leapMonth);
        leapMonth.AddMonths(1).ShouldBe(followingMonth);
        followingMonth.AddMonths(-1).ShouldBe(leapMonth);
    }
    /// <summary>
    /// 验证农历腊月与正月之间可双向移动，并符合春节前后的固定日期。
    /// </summary>
    [Fact]
    public void AddMonths_CrossesLunarYearBoundaryInBothDirections()
    {
        var newYearsEve = new DateTime(2024, 2, 9).ToLunarDate();
        var lunarNewYear = new DateTime(2024, 2, 10).ToLunarDate();
        var lastMonthFirstDay = ChineseLunarCalendar.Create(2023, 12, 1);
        var firstMonthFirstDay = ChineseLunarCalendar.Create(2024, 1, 1);

        newYearsEve.ShouldBe(ChineseLunarCalendar.Create(2023, 12, 30));
        lunarNewYear.ShouldBe(firstMonthFirstDay);
        lastMonthFirstDay.AddMonths(1).ShouldBe(firstMonthFirstDay);
        firstMonthFirstDay.AddMonths(-1).ShouldBe(lastMonthFirstDay);
    }
    /// <summary>
    /// 验证带闰月年份的 BCL 月序号仍覆盖一到十三且显示月份始终为一到十二。
    /// </summary>
    [Fact]
    public void FromDate_LeapYear_ExposesDisplayMonthAndCalendarSlotSeparately()
    {
        var expectedDisplayMonths = new[] { 1, 2, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };

        for (var calendarMonth = 1; calendarMonth <= 13; calendarMonth++)
        {
            var date = new DateTime(2023, 1, 22).AddMonths(calendarMonth - 1);
            var lunar = date.ToLunarDate();

            lunar.Year.ShouldBe(2023);
            lunar.CalendarMonth.ShouldBe(calendarMonth);
            lunar.Month.ShouldBe(expectedDisplayMonths[calendarMonth - 1]);
            lunar.IsLeapMonth.ShouldBe(calendarMonth == 3);
        }
    }
    /// <summary>
    /// 验证同一日期字段在不同 Kind 和时刻下产生相同农历日期。
    /// </summary>
    /// <param name="kind">输入日期的种类。</param>
    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void DateTimeExtension_UsesDateFieldsWithoutTimeZoneConversion(DateTimeKind kind)
    {
        var morning = new DateTime(2024, 2, 10, 1, 2, 3, kind);
        var late = new DateTime(2024, 2, 10, 23, 59, 59, kind);

        morning.ToLunarDate().ShouldBe(late.ToLunarDate());
        morning.ToLunarDate().Year.ShouldBe(2024);
        morning.ToLunarDate().Month.ShouldBe(1);
        morning.ToLunarDate().Day.ShouldBe(1);
    }
    /// <summary>
    /// 验证 DateTimeOffset 按固定 UTC+8 自然日判断农历，且相同时间点不受 Offset 表示影响。
    /// </summary>
    [Fact]
    public void DateTimeOffsetExtension_UsesBeijingMidnightBoundary()
    {
        var beijingNewYear = new DateTimeOffset(2024, 2, 9, 16, 0, 0, TimeSpan.Zero);
        var beforeBoundary = beijingNewYear.AddTicks(-1);
        var afterBoundary = beijingNewYear;

        beforeBoundary.ToLunarDate().ShouldBe(ChineseLunarCalendar.Create(2023, 12, 30));
        afterBoundary.ToLunarDate().ShouldBe(ChineseLunarCalendar.Create(2024, 1, 1));

        foreach (var offsetHours in new[] { -14, -5, 0, 8, 14 })
        {
            afterBoundary.ToOffset(TimeSpan.FromHours(offsetHours)).ToLunarDate()
                .ShouldBe(ChineseLunarCalendar.Create(2024, 1, 1));
        }
    }
    /// <summary>
    /// 验证支持范围与 BCL 农历边界一致，且最大日期当天末刻仍可转换。
    /// </summary>
    [Fact]
    public void SupportedRange_IncludesBclBoundariesAndRejectsAdjacentDays()
    {
        ChineseLunarCalendar.MinDate.ShouldBe(new DateTime(1901, 2, 19));
        ChineseLunarCalendar.MaxDate.ShouldBe(new DateTime(2101, 1, 28));

        ChineseLunarCalendar.MinDate.ToLunarDate().ToDateTime().Date.ShouldBe(ChineseLunarCalendar.MinDate);
        new DateTime(2101, 1, 28, 23, 59, 59, DateTimeKind.Local).AddTicks(9999999)
            .ToLunarDate().Year.ShouldBe(2100);

        Should.Throw<ArgumentOutOfRangeException>(() => ChineseLunarCalendar.MinDate.AddDays(-1).ToLunarDate());
        Should.Throw<ArgumentOutOfRangeException>(() => ChineseLunarCalendar.MaxDate.AddDays(1).ToLunarDate());
        Should.Throw<ArgumentOutOfRangeException>(() => DateTime.MinValue.ToLunarDate());
        Should.Throw<ArgumentOutOfRangeException>(() => DateTime.MaxValue.ToLunarDate());
    }
    /// <summary>
    /// 验证农历组件创建会拒绝范围外年月日和不存在的闰月。
    /// </summary>
    [Fact]
    public void Create_InvalidComponents_ThrowsForNamedArgument()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ChineseLunarCalendar.Create(-1, 1, 1))
            .ParamName.ShouldBe("year");
        Should.Throw<ArgumentOutOfRangeException>(() => ChineseLunarCalendar.Create(1900, 1, 1))
            .ParamName.ShouldBe("year");
        Should.Throw<ArgumentOutOfRangeException>(() => ChineseLunarCalendar.Create(2024, -1, 1))
            .ParamName.ShouldBe("month");
        Should.Throw<ArgumentOutOfRangeException>(() => ChineseLunarCalendar.Create(2024, 0, 1))
            .ParamName.ShouldBe("month");
        Should.Throw<ArgumentOutOfRangeException>(() => ChineseLunarCalendar.Create(2024, 13, 1))
            .ParamName.ShouldBe("month");
        Should.Throw<ArgumentOutOfRangeException>(() => ChineseLunarCalendar.Create(2024, 1, 0))
            .ParamName.ShouldBe("day");
        Should.Throw<ArgumentOutOfRangeException>(() => ChineseLunarCalendar.Create(2024, 1, -1))
            .ParamName.ShouldBe("day");
        Should.Throw<ArgumentOutOfRangeException>(() => ChineseLunarCalendar.Create(2024, 1, 30))
            .ParamName.ShouldBe("day");
        Should.Throw<ArgumentException>(() => ChineseLunarCalendar.Create(2024, 2, 1, isLeapMonth: true))
            .ParamName.ShouldBe("isLeapMonth");
    }
    /// <summary>
    /// 验证 Try 工厂对无效日期返回 false 和 default，而不吞掉其他异常。
    /// </summary>
    [Fact]
    public void TryFactories_InvalidInputs_ReturnFalseAndDefault()
    {
        ChineseLunarCalendar.TryFromDate(DateTime.MinValue, out var fromDate).ShouldBeFalse();
        fromDate.IsValid.ShouldBeFalse();
        fromDate.Year.ShouldBe(0);
        fromDate.Month.ShouldBe(0);
        fromDate.Day.ShouldBe(0);

        ChineseLunarCalendar.TryCreate(2024, 2, 1, true, out var fromLunar).ShouldBeFalse();
        fromLunar.ShouldBe(default(LunarDate));

        ChineseLunarCalendar.TryFromInstant(DateTimeOffset.MinValue, out var fromMinimumInstant).ShouldBeFalse();
        fromMinimumInstant.ShouldBe(default(LunarDate));
        ChineseLunarCalendar.TryFromInstant(DateTimeOffset.MaxValue, out var fromMaximumInstant).ShouldBeFalse();
        fromMaximumInstant.ShouldBe(default(LunarDate));
    }
    /// <summary>
    /// 验证有效的日期、时间点及农历组件均可通过 Try 入口转换。
    /// </summary>
    [Fact]
    public void TryFactories_ValidInputs_ReturnMatchingLunarDate()
    {
        var date = new DateTime(2023, 3, 22, 23, 59, 59, DateTimeKind.Local);
        var instant = new DateTimeOffset(2023, 3, 22, 0, 0, 0, TimeSpan.FromHours(8));
        var expected = ChineseLunarCalendar.Create(2023, 2, 1, isLeapMonth: true);

        ChineseLunarCalendar.TryFromDate(date, out var fromDate).ShouldBeTrue();
        fromDate.ShouldBe(expected);
        date.TryToLunarDate(out var fromDateExtension).ShouldBeTrue();
        fromDateExtension.ShouldBe(expected);

        ChineseLunarCalendar.TryFromInstant(instant, out var fromInstant).ShouldBeTrue();
        fromInstant.ShouldBe(expected);
        instant.TryToLunarDate(out var fromInstantExtension).ShouldBeTrue();
        fromInstantExtension.ShouldBe(expected);

        ChineseLunarCalendar.TryCreate(2023, 2, 1, true, out var fromComponents).ShouldBeTrue();
        fromComponents.ShouldBe(expected);
    }
    /// <summary>
    /// 验证 DateTimeOffset 时间点的 UTC+8 投影越过公历可表示范围时拒绝转换。
    /// </summary>
    [Fact]
    public void FromInstant_ExtremeValues_RejectsUnrepresentableUtcPlusEightProjection()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ChineseLunarCalendar.FromInstant(DateTimeOffset.MinValue));
        Should.Throw<ArgumentOutOfRangeException>(() => ChineseLunarCalendar.FromInstant(DateTimeOffset.MaxValue));
    }
    /// <summary>
    /// 验证农历支持范围按北京时间自然日包含首尾日期，精确到一个 tick。
    /// </summary>
    [Fact]
    public void FromInstant_SupportedRangeBoundaries_AreInclusiveAcrossOffsets()
    {
        var firstInstant = new DateTimeOffset(ChineseLunarCalendar.MinDate, TimeSpan.FromHours(8));
        var afterLastInstant = new DateTimeOffset(ChineseLunarCalendar.MaxDate.AddDays(1), TimeSpan.FromHours(8));
        var offsets = new[] { -14, -5, 0, 8, 14 };

        foreach (var offsetHours in offsets)
        {
            var offset = TimeSpan.FromHours(offsetHours);
            var beforeRange = firstInstant.AddTicks(-1).ToOffset(offset);
            var atRangeStart = firstInstant.ToOffset(offset);
            var atRangeEnd = afterLastInstant.AddTicks(-1).ToOffset(offset);
            var afterRange = afterLastInstant.ToOffset(offset);

            ChineseLunarCalendar.TryFromInstant(beforeRange, out var invalidStart).ShouldBeFalse();
            invalidStart.ShouldBe(default(LunarDate));
            atRangeStart.ToLunarDate().ToDateTime().ShouldBe(ChineseLunarCalendar.MinDate);
            atRangeEnd.ToLunarDate().ToDateTime().ShouldBe(ChineseLunarCalendar.MaxDate);
            ChineseLunarCalendar.TryFromInstant(afterRange, out var invalidEnd).ShouldBeFalse();
            invalidEnd.ShouldBe(default(LunarDate));
        }
    }
    /// <summary>
    /// 验证默认值可比较，但不能执行农历日期操作。
    /// </summary>
    [Fact]
    public void DefaultValue_IsInvalidAndOperationsFail()
    {
        var first = default(LunarDate);
        var second = default(LunarDate);

        first.IsValid.ShouldBeFalse();
        first.ShouldBe(second);
        first.GetHashCode().ShouldBe(second.GetHashCode());
        Should.Throw<InvalidOperationException>(() => first.ToDateTime());
        Should.Throw<InvalidOperationException>(() => first.GetDaysInMonth());
        Should.Throw<InvalidOperationException>(() => first.GetDaysInYear());
        Should.Throw<InvalidOperationException>(() => first.ToChineseString());
        Should.Throw<InvalidOperationException>(() => first.AddDays(1));
        Should.Throw<InvalidOperationException>(() => first.AddMonths(1));
    }
    /// <summary>
    /// 验证相同农历日期的值相等且哈希一致，普通月和闰月具有不同身份。
    /// </summary>
    [Fact]
    public void LunarDate_ValueEquality_IncludesLeapMonthIdentity()
    {
        var fromGregorian = new DateTime(2023, 3, 22, 22, 0, 0, DateTimeKind.Utc).ToLunarDate();
        var fromComponents = ChineseLunarCalendar.Create(2023, 2, 1, isLeapMonth: true);
        var ordinaryMonth = ChineseLunarCalendar.Create(2023, 2, 1);

        fromGregorian.ShouldBe(fromComponents);
        fromGregorian.GetHashCode().ShouldBe(fromComponents.GetHashCode());
        fromGregorian.ShouldNotBe(ordinaryMonth);
        (fromGregorian == fromComponents).ShouldBeTrue();
        (ordinaryMonth != fromComponents).ShouldBeTrue();
        new HashSet<LunarDate> { fromGregorian, fromComponents, ordinaryMonth }.Count.ShouldBe(2);
    }
    /// <summary>
    /// 验证从农历日期反向转换返回公历午夜且 Kind 为 Unspecified。
    /// </summary>
    [Fact]
    public void ToDateTime_ReturnsUnspecifiedGregorianMidnight()
    {
        var date = ChineseLunarCalendar.Create(2024, 1, 1).ToDateTime();

        date.ShouldBe(new DateTime(2024, 2, 10, 0, 0, 0, DateTimeKind.Unspecified));
        date.Kind.ShouldBe(DateTimeKind.Unspecified);
        date.Ticks.ShouldBe(new DateTime(2024, 2, 10).Ticks);
    }
    /// <summary>
    /// 验证农历日偏移包含跨月行为并拒绝超出支持范围的目标日期。
    /// </summary>
    [Fact]
    public void AddDays_MovesNaturalDaysAndRejectsRangeOverflow()
    {
        ChineseLunarCalendar.Create(2023, 2, 29).AddDays(1)
            .ShouldBe(ChineseLunarCalendar.Create(2023, 2, 30));
        ChineseLunarCalendar.Create(2023, 2, 30).AddDays(1)
            .ShouldBe(ChineseLunarCalendar.Create(2023, 2, 1, isLeapMonth: true));
        Should.Throw<ArgumentOutOfRangeException>(() => ChineseLunarCalendar.MinDate.ToLunarDate().AddDays(-1));
        Should.Throw<ArgumentOutOfRangeException>(() => ChineseLunarCalendar.MaxDate.ToLunarDate().AddDays(1));
        Should.Throw<ArgumentOutOfRangeException>(() => ChineseLunarCalendar.MinDate.ToLunarDate().AddDays(int.MinValue));
        Should.Throw<ArgumentOutOfRangeException>(() => ChineseLunarCalendar.MaxDate.ToLunarDate().AddDays(int.MaxValue));
    }
    /// <summary>
    /// 验证农历月偏移默认拒绝丢失日号，并允许调用方显式截到目标月末日。
    /// </summary>
    [Fact]
    public void AddMonths_RejectsOrClampsMissingDayAsRequested()
    {
        var thirtiethDay = ChineseLunarCalendar.Create(2023, 2, 30);

        Should.Throw<ArgumentOutOfRangeException>(() => thirtiethDay.AddMonths(1));
        thirtiethDay.AddMonths(1, LunarDayOverflow.ClampToLastDay)
            .ShouldBe(ChineseLunarCalendar.Create(2023, 2, 29, isLeapMonth: true));
        ChineseLunarCalendar.Create(2023, 1, 1).AddMonths(0)
            .ShouldBe(ChineseLunarCalendar.Create(2023, 1, 1));
        Should.Throw<ArgumentOutOfRangeException>(() => thirtiethDay.AddMonths(int.MaxValue));
        Should.Throw<ArgumentOutOfRangeException>(() => thirtiethDay.AddMonths(int.MinValue));
        Should.Throw<ArgumentOutOfRangeException>(() => ChineseLunarCalendar.MinDate.ToLunarDate().AddMonths(-1));
        Should.Throw<ArgumentOutOfRangeException>(() => ChineseLunarCalendar.MaxDate.ToLunarDate().AddMonths(1));
        Should.Throw<ArgumentOutOfRangeException>(() => thirtiethDay.AddMonths(1, (LunarDayOverflow)int.MaxValue));
    }
    /// <summary>
    /// 验证农历月数和年数使用当前日期所属农历年月计算。
    /// </summary>
    [Fact]
    public void DateParts_ReturnActualLunarMonthAndYearLengths()
    {
        var newYearEve = new DateTime(2024, 2, 9).ToLunarDate();
        var leapMonth = ChineseLunarCalendar.Create(2023, 2, 1, isLeapMonth: true);

        newYearEve.GetDaysInMonth().ShouldBe(30);
        newYearEve.GetDaysInYear().ShouldBe(384);
        leapMonth.GetDaysInMonth().ShouldBeInRange(29, 30);
    }
    /// <summary>
    /// 验证简繁体文本使用农历年份和实际月份，并且不依赖当前区域性。
    /// </summary>
    [Fact]
    public void ToChineseString_UsesLunarYearAndIsCultureInvariant()
    {
        var leapDate = ChineseLunarCalendar.Create(2023, 2, 1, isLeapMonth: true);
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;

        leapDate.ToChineseString().ShouldBe("二零二三年闰二月初一");
        leapDate.ToChineseString(traditionalChineseCharacters: true)
            .ShouldBe("貳零貳叁年閏貳月初壹");
        leapDate.ToChineseString(includeYear: false).ShouldBe("闰二月初一");
        new DateTime(2024, 2, 9).ToLunarDateString().ShouldBe("二零二三年腊月三十");
        new DateTimeOffset(2024, 2, 9, 16, 0, 0, TimeSpan.Zero).ToLunarDateString()
            .ShouldBe("二零二四年正月初一");

        try
        {
            foreach (var cultureName in new[] { "en-US", "fr-FR", "ar-SA" })
            {
                var culture = CultureInfo.GetCultureInfo(cultureName);
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;

                leapDate.ToChineseString().ShouldBe("二零二三年闰二月初一");
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }
    /// <summary>
    /// 验证十一月、初十和二十沿用现有简繁字表格式。
    /// </summary>
    [Fact]
    public void ToChineseString_FormatsEleventhMonthAndTenthTwentiethDays()
    {
        var lunarMonthStart = ChineseLunarCalendar.Create(2023, 11, 1);
        var tenthDay = ChineseLunarCalendar.Create(2023, 11, 10);
        var twentiethDay = ChineseLunarCalendar.Create(2023, 11, 20);

        lunarMonthStart.ToChineseString().ShouldBe("二零二三年十一月初一");
        lunarMonthStart.ToChineseString(traditionalChineseCharacters: true)
            .ShouldBe("貳零貳叁年拾壹月初壹");
        tenthDay.ToChineseString().ShouldBe("二零二三年十一月初十");
        tenthDay.ToChineseString(traditionalChineseCharacters: true)
            .ShouldBe("貳零貳叁年拾壹月初拾");
        twentiethDay.ToChineseString().ShouldBe("二零二三年十一月二十");
        twentiethDay.ToChineseString(traditionalChineseCharacters: true)
            .ShouldBe("貳零貳叁年拾壹月貳拾");
    }
    /// <summary>
    /// 验证公历日期在农历支持范围内逐日往返且相邻日期连续。
    /// </summary>
    [Fact]
    public void EntireSupportedRange_RoundTripsEveryGregorianDate()
    {
        var priorDate = DateTime.MinValue;
        var count = 0;

        for (var date = ChineseLunarCalendar.MinDate; date <= ChineseLunarCalendar.MaxDate; date = date.AddDays(1))
        {
            var lunar = date.ToLunarDate();
            var fromComponents = ChineseLunarCalendar.Create(lunar.Year, lunar.Month, lunar.Day, lunar.IsLeapMonth);
            var roundTrip = lunar.ToDateTime();

            lunar.IsValid.ShouldBeTrue();
            fromComponents.ShouldBe(lunar);
            roundTrip.ShouldBe(date.Date);
            roundTrip.Kind.ShouldBe(DateTimeKind.Unspecified);
            if (count > 0)
                roundTrip.Subtract(priorDate).TotalDays.ShouldBe(1);

            priorDate = roundTrip;
            count++;
        }

        count.ShouldBe((ChineseLunarCalendar.MaxDate - ChineseLunarCalendar.MinDate).Days + 1);
    }
    /// <summary>
    /// 验证共享内部历法的并发读取返回一致农历日期。
    /// </summary>
    [Fact]
    public void ConcurrentConversions_ReturnConsistentLunarDates()
    {
        const int conversionCount = 1024;
        var date = new DateTime(2023, 3, 22, 12, 0, 0, DateTimeKind.Utc);
        var expected = ChineseLunarCalendar.Create(2023, 2, 1, isLeapMonth: true);
        var results = new LunarDate[conversionCount];

        Parallel.For(0, conversionCount, index => results[index] = date.ToLunarDate());

        foreach (var result in results)
            result.ShouldBe(expected);
    }
}
