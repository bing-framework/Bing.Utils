namespace Bing.Date;
/// <summary>
/// 日期时间工厂的创建和星期定位测试。
/// </summary>
[Trait("DateTimeUT", "DateTimeFactory")]
public class DateTimeFactoryTest
{
    /// <summary>
    /// 验证本地当前时间位于调用前后的容差范围内。
    /// </summary>
    [Fact]
    public void Now_Invoked_ReturnsValueBetweenBeforeAndAfter()
    {
        var before = DateTime.Now;
        var result = DateTimeFactory.Now();
        var after = DateTime.Now;
        result.ShouldBeGreaterThanOrEqualTo(before.AddSeconds(-1));
        result.ShouldBeLessThanOrEqualTo(after.AddSeconds(1));
    }
    /// <summary>
    /// 验证 UTC 当前时间位于调用前后的容差范围内。
    /// </summary>
    [Fact]
    public void UtcNow_Invoked_ReturnsValueBetweenBeforeAndAfter()
    {
        var before = DateTime.UtcNow;
        var result = DateTimeFactory.UtcNow();
        var after = DateTime.UtcNow;
        result.ShouldBeGreaterThanOrEqualTo(before.AddSeconds(-1));
        result.ShouldBeLessThanOrEqualTo(after.AddSeconds(1));
    }
    /// <summary>
    /// 验证闰年二月的最后一天为二十九日。
    /// </summary>
    [Fact]
    public void CreateLastDayOfMonth_LeapYearFebruary_ReturnsExpectedDate()
    {
        var result = DateTimeFactory.CreateLastDayOfMonth(2024, 2);
        result.ShouldBe(new DateTime(2024, 2, 29));
    }
    /// <summary>
    /// 验证创建月内第一个指定星期的日期。
    /// </summary>
    [Fact]
    public void CreateFirstDayOfMonth_WithDayOfWeek_ReturnsFirstMatchingDate()
    {
        var result = DateTimeFactory.CreateFirstDayOfMonth(2024, 1, DayOfWeek.Monday);
        result.ShouldBe(new DateTime(2024, 1, 1));
    }
    /// <summary>
    /// 验证创建月内最后一个指定星期的日期。
    /// </summary>
    [Fact]
    public void CreateLastDayOfMonth_WithDayOfWeek_ReturnsLastMatchingDate()
    {
        var result = DateTimeFactory.CreateLastDayOfMonth(2024, 1, DayOfWeek.Monday);
        result.ShouldBe(new DateTime(2024, 1, 29));
    }
    /// <summary>
    /// 验证创建月内第三个星期一的日期。
    /// </summary>
    [Fact]
    public void CreateByWeek_ThirdMonday_ReturnsExpectedDate()
    {
        var result = DateTimeFactory.CreateByWeek(2024, 2, DayOfWeek.Monday, 3);
        result.ShouldBe(new DateTime(2024, 2, 19));
    }
    /// <summary>
    /// 验证源日期已匹配星期时定位到下一周。
    /// </summary>
    [Fact]
    public void CreateNextDayByWeek_WhenSameWeekday_ReturnsNextWeek()
    {
        var monday = new DateTime(2024, 1, 1); // Monday
        var result = DateTimeFactory.CreateNextDayByWeek(monday, DayOfWeek.Monday);
        result.ShouldBe(new DateTime(2024, 1, 8));
    }
    /// <summary>
    /// 验证源日期已匹配星期时定位到上一周。
    /// </summary>
    [Fact]
    public void CreatePreviousDayByWeek_WhenSameWeekday_ReturnsPreviousWeek()
    {
        var monday = new DateTime(2024, 1, 8); // Monday
        var result = DateTimeFactory.CreatePreviousDayByWeek(monday, DayOfWeek.Monday);
        result.ShouldBe(new DateTime(2024, 1, 1));
    }
    /// <summary>
    /// 验证无效月份触发参数越界异常。
    /// </summary>
    [Fact]
    public void Create_InvalidMonth_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => DateTimeFactory.Create(2024, 13, 1));
    }

    /// <summary>
    /// 验证创建日期时保留毫秒和日期时间种类。
    /// </summary>
    [Fact]
    public void Create_WithMillisecondAndKind_KeepsExpectedComponents()
    {
        var result = DateTimeFactory.Create(2024, 3, 1, 12, 30, 15, 123, DateTimeKind.Utc);

        result.ShouldBe(new DateTime(2024, 3, 1, 12, 30, 15, 123, DateTimeKind.Utc));
        result.Kind.ShouldBe(DateTimeKind.Utc);
    }

    /// <summary>
    /// 验证整数星期重载与枚举重载的结果一致。
    /// </summary>
    [Fact]
    public void CreateFirstDayOfMonth_IntDayOfWeekOverload_MatchesEnumOverload()
    {
        var fromEnum = DateTimeFactory.CreateFirstDayOfMonth(2024, 2, DayOfWeek.Thursday);
        var fromInt = DateTimeFactory.CreateFirstDayOfMonth(2024, 2, (int)DayOfWeek.Thursday);

        fromInt.ShouldBe(fromEnum);
        fromInt.DayOfWeek.ShouldBe(DayOfWeek.Thursday);
    }

    /// <summary>
    /// 验证整数星期重载与枚举重载的结果一致。
    /// </summary>
    [Fact]
    public void CreateLastDayOfMonth_IntDayOfWeekOverload_MatchesEnumOverload()
    {
        var fromEnum = DateTimeFactory.CreateLastDayOfMonth(2024, 2, DayOfWeek.Thursday);
        var fromInt = DateTimeFactory.CreateLastDayOfMonth(2024, 2, (int)DayOfWeek.Thursday);

        fromInt.ShouldBe(fromEnum);
        fromInt.DayOfWeek.ShouldBe(DayOfWeek.Thursday);
    }

    /// <summary>
    /// 验证无效的月内出现序号被拒绝并报告参数名。
    /// </summary>
    [Fact]
    public void CreateByWeek_InvalidOccurrence_ThrowsArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() => DateTimeFactory.CreateByWeek(2024, 2, DayOfWeek.Monday, 0));

        ex.ParamName.ShouldBe("weekAtMonth");
    }

    /// <summary>
    /// 验证无效的整数星期被拒绝并报告参数名。
    /// </summary>
    [Fact]
    public void CreateLastDayOfMonth_InvalidIntDayOfWeek_ThrowsArgumentOutOfRangeException()
    {
        var ex = Should.Throw<ArgumentOutOfRangeException>(() => DateTimeFactory.CreateLastDayOfMonth(2024, 2, 7));

        ex.ParamName.ShouldBe("dayOfWeek");
    }
}

