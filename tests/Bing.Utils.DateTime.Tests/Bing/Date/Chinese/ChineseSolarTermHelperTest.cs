using System.Globalization;
namespace Bing.Date.Chinese;
/// <summary>
/// 二十四节气名称及前后节气定位测试。
/// </summary>
[Trait("DateTimeUT", "ChineseDate.SolarTerm")]
public class ChineseSolarTermHelperTest
{
    /// <summary>
    /// 节气计算使用的农历历法。
    /// </summary>
    private readonly ChineseLunisolarCalendar _calendar = new();
    /// <summary>
    /// 验证节气的简体和繁体中文名称。
    /// </summary>
    /// <param name="term">节气枚举值。</param>
    /// <param name="traditional">是否使用繁体文字。</param>
    /// <param name="expected">预期结果。</param>
    [Theory]
    [InlineData(ChineseSolarTerms.BeginningOfSpring, false, "立春")]
    [InlineData(ChineseSolarTerms.VernalEquinox, false, "春分")]
    [InlineData(ChineseSolarTerms.QingmingFestival, true, "清明")]
    [InlineData(ChineseSolarTerms.TheWakingOfInsects, true, "驚蟄")]
    public void GetName_ValidTerms_ReturnsCorrectName(ChineseSolarTerms term, bool traditional, string expected)
    {
        // Act
        var result = ChineseSolarTermHelper.GetName(term, traditional);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 验证节气的英文名称。
    /// </summary>
    /// <param name="term">节气枚举值。</param>
    /// <param name="expected">预期结果。</param>
    [Theory]
    [InlineData(ChineseSolarTerms.BeginningOfSpring, "Beginning of Spring")]
    [InlineData(ChineseSolarTerms.VernalEquinox, "Vernal Equinox")]
    [InlineData(ChineseSolarTerms.QingmingFestival, "Qingming Festival")]
    [InlineData(ChineseSolarTerms.SummerSolstice, "Summer Solstice")]
    public void GetEnglishName_ValidTerms_ReturnsCorrectName(ChineseSolarTerms term, string expected)
    {
        // Act
        var result = ChineseSolarTermHelper.GetEnglishName(term);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 验证节气当日返回对应名称。
    /// </summary>
    /// <param name="year">公历年份。</param>
    /// <param name="month">公历月份。</param>
    /// <param name="day">公历日期。</param>
    /// <param name="traditional">是否使用繁体文字。</param>
    /// <param name="expected">预期结果。</param>
    [Theory]
    [InlineData(2023, 2, 4, false, "立春")] // 2023年立春：2月4日
    [InlineData(2023, 3, 21, false, "春分")] // 2023年春分：3月21日
    [InlineData(2023, 6, 21, false, "夏至")] // 2023年夏至：6月21日
    [InlineData(2023, 12, 22, false, "冬至")] // 2023年冬至：12月22日
    public void GetSolarTerm_TermDay_ReturnsSolarTermName(int year, int month, int day, bool traditional, string expected)
    {
        // Arrange
        var date = new DateTime(year, month, day);
        // Act
        var result = ChineseSolarTermHelper.GetSolarTerm(_calendar, date, traditional);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 验证非节气日期返回空字符串。
    /// </summary>
    [Fact]
    public void GetSolarTerm_NonTermDay_ReturnsEmptyString()
    {
        // Arrange
        var date = new DateTime(2023, 2, 10); // 非节气日
        // Act
        var result = ChineseSolarTermHelper.GetSolarTerm(_calendar, date);
        // Assert
        result.ShouldBe(string.Empty);
    }
    /// <summary>
    /// 验证上一个节气的名称及日期早于查询日期。
    /// </summary>
    /// <param name="year">公历年份。</param>
    /// <param name="month">公历月份。</param>
    /// <param name="day">公历日期。</param>
    /// <param name="expected">预期结果。</param>
    [Theory]
    [InlineData(2023, 2, 10, "立春")] // 立春后几天，上一个是立春
    [InlineData(2023, 3, 25, "春分")] // 春分后几天，上一个是春分
    public void GetLastSolarTerm_ValidDate_ReturnsLastTerm(int year, int month, int day, string expected)
    {
        // Arrange
        var date = new DateTime(year, month, day);
        // Act
        var result = ChineseSolarTermHelper.GetLastSolarTerm(_calendar, date, out var termDate);
        // Assert
        result.ShouldBe(expected);
        termDate.ShouldNotBe(default);
        termDate.ShouldBeLessThan(date);
    }
    /// <summary>
    /// 验证下一个节气的名称及日期晚于查询日期。
    /// </summary>
    /// <param name="year">公历年份。</param>
    /// <param name="month">公历月份。</param>
    /// <param name="day">公历日期。</param>
    /// <param name="expected">预期结果。</param>
    [Theory]
    [InlineData(2023, 1, 25, "立春")] // 立春前几天，下一个是立春
    [InlineData(2023, 3, 15, "春分")] // 春分前几天，下一个是春分
    public void GetNextSolarTerm_ValidDate_ReturnsNextTerm(int year, int month, int day, string expected)
    {
        // Arrange
        var date = new DateTime(year, month, day);
        // Act
        var result = ChineseSolarTermHelper.GetNextSolarTerm(_calendar, date, out var termDate);
        // Assert
        result.ShouldBe(expected);
        termDate.ShouldNotBe(default);
        termDate.ShouldBeGreaterThan(date);
    }
    /// <summary>
    /// 验证节气当日返回对应枚举值。
    /// </summary>
    /// <param name="year">公历年份。</param>
    /// <param name="month">公历月份。</param>
    /// <param name="day">公历日期。</param>
    /// <param name="expected">预期结果。</param>
    [Theory]
    [InlineData(2023, 2, 4, ChineseSolarTerms.BeginningOfSpring)] // 2023年立春
    [InlineData(2023, 3, 21, ChineseSolarTerms.VernalEquinox)] // 2023年春分
    public void GetSolarTermEnum_TermDay_ReturnsSolarTermEnum(int year, int month, int day, ChineseSolarTerms expected)
    {
        // Arrange
        var date = new DateTime(year, month, day);
        // Act
        var result = ChineseSolarTermHelper.GetSolarTermEnum(_calendar, date);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 验证上一个节气的枚举值及日期。
    /// </summary>
    [Fact]
    public void GetLastSolarTermEnum_ValidDate_ReturnsLastTermEnum()
    {
        // Arrange
        var date = new DateTime(2023, 2, 10); // 立春后几天
        // Act
        var result = ChineseSolarTermHelper.GetLastSolarTermEnum(_calendar, date, out var termDate);
        // Assert
        result.ShouldBe(ChineseSolarTerms.BeginningOfSpring);
        termDate.ShouldNotBe(default);
        termDate.ShouldBeLessThan(date);
    }
    /// <summary>
    /// 验证下一个节气的枚举值及日期。
    /// </summary>
    [Fact]
    public void GetNextSolarTermEnum_ValidDate_ReturnsNextTermEnum()
    {
        // Arrange
        var date = new DateTime(2023, 3, 15); // 春分前几天
        // Act
        var result = ChineseSolarTermHelper.GetNextSolarTermEnum(_calendar, date, out var termDate);
        // Assert
        result.ShouldBe(ChineseSolarTerms.VernalEquinox);
        termDate.ShouldNotBe(default);
        termDate.ShouldBeGreaterThan(date);
    }
    /// <summary>
    /// 验证节气查询拒绝空历法。
    /// </summary>
    [Fact]
    public void GetSolarTerm_NullCalendar_ThrowsArgumentNullException()
    {
        // Arrange
        var date = new DateTime(2023, 2, 4);
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => ChineseSolarTermHelper.GetSolarTerm(null, date));
    }
    /// <summary>
    /// 验证支持范围外的日期返回空字符串。
    /// </summary>
    /// <param name="year">公历年份。</param>
    /// <param name="month">公历月份。</param>
    /// <param name="day">公历日期。</param>
    [Theory]
    [InlineData(1800, 1, 1)] // 早于基准日期
    [InlineData(2200, 1, 1)] // 超出最大支持日期
    public void GetSolarTerm_DateOutOfRange_ReturnsEmptyString(int year, int month, int day)
    {
        // Arrange
        var date = new DateTime(year, month, day);
        // Act
        var result = ChineseSolarTermHelper.GetSolarTerm(_calendar, date);
        // Assert
        result.ShouldBe(string.Empty);
    }

    /// <summary>
    /// 验证节气按 UTC+8 自然日精确匹配，不把相邻日期算作节气日。
    /// </summary>
    [Fact]
    public void GetSolarTerm_SummerSolstice_UsesNaturalDateAndStrictNeighborSearch()
    {
        var date = new DateTime(2023, 6, 21, 23, 59, 59, DateTimeKind.Local);

        ChineseSolarTermHelper.GetSolarTerm(_calendar, date).ShouldBe("夏至");
        ChineseSolarTermHelper.GetSolarTermEnum(_calendar, date).ShouldBe(ChineseSolarTerms.SummerSolstice);
        ChineseSolarTermHelper.GetSolarTerm(_calendar, new DateTime(2023, 6, 20)).ShouldBeEmpty();
        ChineseSolarTermHelper.GetSolarTerm(_calendar, new DateTime(2023, 6, 22)).ShouldBeEmpty();

        ChineseSolarTermHelper.GetLastSolarTerm(_calendar, date, out var previousDate).ShouldBe("芒种");
        previousDate.ShouldBe(new DateTime(2023, 6, 6));
        previousDate.Kind.ShouldBe(DateTimeKind.Unspecified);

        ChineseSolarTermHelper.GetNextSolarTerm(_calendar, date, out var nextDate).ShouldBe("小暑");
        nextDate.ShouldBe(new DateTime(2023, 7, 7));
        nextDate.Kind.ShouldBe(DateTimeKind.Unspecified);
    }

    /// <summary>
    /// 验证元旦、年末及节气当天的前后查找使用固定日期并严格排除当天。
    /// </summary>
    [Fact]
    public void GetLastAndNextSolarTerm_CrossYearUsesFixedDatesAndExcludesQueryDay()
    {
        var newYear = new DateTime(2024, 1, 1);
        ChineseSolarTermHelper.GetLastSolarTerm(_calendar, newYear, out var previousYearTerm).ShouldBe("冬至");
        AssertUnspecifiedSolarTermDate(previousYearTerm, 2023, 12, 22);
        previousYearTerm.ShouldBeLessThan(newYear);

        var yearEnd = new DateTime(2024, 12, 31);
        ChineseSolarTermHelper.GetNextSolarTerm(_calendar, yearEnd, out var nextYearTerm).ShouldBe("小寒");
        AssertUnspecifiedSolarTermDate(nextYearTerm, 2025, 1, 5);
        nextYearTerm.ShouldBeGreaterThan(yearEnd);

        var winterSolstice = new DateTime(2023, 12, 22);
        ChineseSolarTermHelper.GetLastSolarTerm(_calendar, winterSolstice, out var strictlyPrevious).ShouldBe("大雪");
        AssertUnspecifiedSolarTermDate(strictlyPrevious, 2023, 12, 7);
        strictlyPrevious.ShouldBeLessThan(winterSolstice);

        ChineseSolarTermHelper.GetNextSolarTerm(_calendar, winterSolstice, out var strictlyNext).ShouldBe("小寒");
        AssertUnspecifiedSolarTermDate(strictlyNext, 2024, 1, 6);
        strictlyNext.ShouldBeGreaterThan(winterSolstice);
    }

    /// <summary>
    /// 验证完整 1900 年节气固定向量。
    /// </summary>
    [Fact]
    public void GetSolarTerm_1900_UsesOfficialBeijingCalendarDates()
    {
        var expected = new (int Month, int Day, string Name)[]
        {
            (1, 6, "小寒"), (1, 20, "大寒"), (2, 4, "立春"), (2, 19, "雨水"),
            (3, 6, "惊蛰"), (3, 21, "春分"), (4, 5, "清明"), (4, 20, "谷雨"),
            (5, 6, "立夏"), (5, 21, "小满"), (6, 6, "芒种"), (6, 22, "夏至"),
            (7, 7, "小暑"), (7, 23, "大暑"), (8, 8, "立秋"), (8, 23, "处暑"),
            (9, 8, "白露"), (9, 23, "秋分"), (10, 9, "寒露"), (10, 24, "霜降"),
            (11, 8, "立冬"), (11, 23, "小雪"), (12, 7, "大雪"), (12, 22, "冬至")
        };

        foreach (var (month, day, name) in expected)
            ChineseSolarTermHelper.GetSolarTerm(_calendar, new DateTime(1900, month, day)).ShouldBe(name);
    }

    /// <summary>
    /// 验证支持范围内每个公历年均有且只有 24 个不同节气日。
    /// </summary>
    [Fact]
    public void GetSolarTerm_AllSupportedYears_HasTwentyFourTerms()
    {
        for (var year = 1900; year <= 2100; year++)
        {
            var found = new bool[24];
            var count = 0;
            for (var month = 1; month <= 12; month++)
            {
                var daysInMonth = DateTime.DaysInMonth(year, month);
                for (var day = 1; day <= daysInMonth; day++)
                {
                    var term = ChineseSolarTermHelper.GetSolarTermEnum(_calendar, new DateTime(year, month, day));
                    if (term == null)
                        continue;

                    var index = (int)term.Value;
                    found[index].ShouldBeFalse($"Duplicate {term} on {year:D4}-{month:D2}-{day:D2}");
                    found[index] = true;
                    count++;
                }
            }

            count.ShouldBe(24, $"Unexpected number of terms in {year}");
            for (var index = 0; index < found.Length; index++)
                found[index].ShouldBeTrue($"Missing enum index {index} in {year}");
        }
    }

    /// <summary>
    /// 验证完整数据边界的相邻节气查询和输出哨兵。
    /// </summary>
    [Fact]
    public void GetLastAndNextSolarTerm_SupportedRangeBoundaries_ReturnExpectedSentinels()
    {
        var firstDate = new DateTime(1900, 1, 1);
        ChineseSolarTermHelper.GetLastSolarTerm(_calendar, firstDate, out var beforeFirst).ShouldBeEmpty();
        beforeFirst.ShouldBe(_calendar.MinSupportedDateTime);
        beforeFirst.Kind.ShouldBe(DateTimeKind.Unspecified);
        ChineseSolarTermHelper.GetNextSolarTerm(_calendar, firstDate, out var firstTerm).ShouldBe("小寒");
        firstTerm.ShouldBe(new DateTime(1900, 1, 6));
        firstTerm.Kind.ShouldBe(DateTimeKind.Unspecified);

        var lastDate = new DateTime(2100, 12, 31);
        ChineseSolarTermHelper.GetNextSolarTerm(_calendar, lastDate, out var afterLast).ShouldBeEmpty();
        afterLast.ShouldBe(_calendar.MinSupportedDateTime);
        ChineseSolarTermHelper.GetLastSolarTerm(_calendar, lastDate, out var lastTerm).ShouldBe("冬至");
        lastTerm.ShouldBe(new DateTime(2100, 12, 22));
        lastTerm.Kind.ShouldBe(DateTimeKind.Unspecified);

        foreach (var date in new[] { new DateTime(1899, 12, 31), new DateTime(2101, 1, 1) })
        {
            ChineseSolarTermHelper.GetLastSolarTerm(_calendar, date, out var previous).ShouldBeEmpty();
            previous.ShouldBe(_calendar.MinSupportedDateTime);
            ChineseSolarTermHelper.GetNextSolarTerm(_calendar, date, out var next).ShouldBeEmpty();
            next.ShouldBe(_calendar.MinSupportedDateTime);
        }
    }

    private static void AssertUnspecifiedSolarTermDate(DateTime actual, int year, int month, int day)
    {
        actual.Ticks.ShouldBe(new DateTime(year, month, day).Ticks);
        actual.Kind.ShouldBe(DateTimeKind.Unspecified);
    }
}
