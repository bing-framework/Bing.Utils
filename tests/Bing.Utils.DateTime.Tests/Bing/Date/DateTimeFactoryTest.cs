namespace Bing.Date;
/// <summary>
/// 测试类：覆盖 `DateTimeFactory` 相关行为。
/// </summary>
[Trait("DateTimeUT", "DateTimeFactory")]
public class DateTimeFactoryTest
{
    /// <summary>
    /// 测试用例：验证 `Now` 在 `Invoked` 场景下，结果为 `ReturnsValueBetweenBeforeAndAfter`。
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
    /// 测试用例：验证 `UtcNow` 在 `Invoked` 场景下，结果为 `ReturnsValueBetweenBeforeAndAfter`。
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
    /// 测试用例：验证 `CreateLastDayOfMonth` 在 `LeapYearFebruary` 场景下，结果为 `ReturnsExpectedDate`。
    /// </summary>
    [Fact]
    public void CreateLastDayOfMonth_LeapYearFebruary_ReturnsExpectedDate()
    {
        var result = DateTimeFactory.CreateLastDayOfMonth(2024, 2);
        result.ShouldBe(new DateTime(2024, 2, 29));
    }
    /// <summary>
    /// 测试用例：验证 `CreateFirstDayOfMonth` 在 `WithDayOfWeek` 场景下，结果为 `ReturnsFirstMatchingDate`。
    /// </summary>
    [Fact]
    public void CreateFirstDayOfMonth_WithDayOfWeek_ReturnsFirstMatchingDate()
    {
        var result = DateTimeFactory.CreateFirstDayOfMonth(2024, 1, DayOfWeek.Monday);
        result.ShouldBe(new DateTime(2024, 1, 1));
    }
    /// <summary>
    /// 测试用例：验证 `CreateLastDayOfMonth` 在 `WithDayOfWeek` 场景下，结果为 `ReturnsLastMatchingDate`。
    /// </summary>
    [Fact]
    public void CreateLastDayOfMonth_WithDayOfWeek_ReturnsLastMatchingDate()
    {
        var result = DateTimeFactory.CreateLastDayOfMonth(2024, 1, DayOfWeek.Monday);
        result.ShouldBe(new DateTime(2024, 1, 29));
    }
    /// <summary>
    /// 测试用例：验证 `CreateByWeek` 在 `ThirdMonday` 场景下，结果为 `ReturnsExpectedDate`。
    /// </summary>
    [Fact]
    public void CreateByWeek_ThirdMonday_ReturnsExpectedDate()
    {
        var result = DateTimeFactory.CreateByWeek(2024, 2, DayOfWeek.Monday, 3);
        result.ShouldBe(new DateTime(2024, 2, 19));
    }
    /// <summary>
    /// 测试用例：验证 `CreateNextDayByWeek` 在 `WhenSameWeekday` 场景下，结果为 `ReturnsNextWeek`。
    /// </summary>
    [Fact]
    public void CreateNextDayByWeek_WhenSameWeekday_ReturnsNextWeek()
    {
        var monday = new DateTime(2024, 1, 1); // Monday
        var result = DateTimeFactory.CreateNextDayByWeek(monday, DayOfWeek.Monday);
        result.ShouldBe(new DateTime(2024, 1, 8));
    }
    /// <summary>
    /// 测试用例：验证 `CreatePreviousDayByWeek` 在 `WhenSameWeekday` 场景下，结果为 `ReturnsPreviousWeek`。
    /// </summary>
    [Fact]
    public void CreatePreviousDayByWeek_WhenSameWeekday_ReturnsPreviousWeek()
    {
        var monday = new DateTime(2024, 1, 8); // Monday
        var result = DateTimeFactory.CreatePreviousDayByWeek(monday, DayOfWeek.Monday);
        result.ShouldBe(new DateTime(2024, 1, 1));
    }
    /// <summary>
    /// 测试用例：验证 `Create` 在 `InvalidMonth` 场景下，结果为 `ThrowsArgumentOutOfRangeException`。
    /// </summary>
    [Fact]
    public void Create_InvalidMonth_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => DateTimeFactory.Create(2024, 13, 1));
    }

    /// <summary>
    /// 测试用例：验证 `Create` 在 `WithMillisecondAndKind` 场景下，结果为 `KeepsExpectedComponents`。
    /// </summary>
    [Fact]
    public void Create_WithMillisecondAndKind_KeepsExpectedComponents()
    {
        var result = DateTimeFactory.Create(2024, 3, 1, 12, 30, 15, 123, DateTimeKind.Utc);

        result.ShouldBe(new DateTime(2024, 3, 1, 12, 30, 15, 123, DateTimeKind.Utc));
        result.Kind.ShouldBe(DateTimeKind.Utc);
    }

    /// <summary>
    /// 测试用例：验证 `CreateFirstDayOfMonth` 在 `IntDayOfWeekOverload` 场景下，结果与 `DayOfWeek` 重载一致。
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
    /// 测试用例：验证 `CreateLastDayOfMonth` 在 `IntDayOfWeekOverload` 场景下，结果与 `DayOfWeek` 重载一致。
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
    /// 测试用例：验证 `CreateByWeek` 在 `InvalidOccurrence` 场景下，结果为 `ThrowsArgumentException`。
    /// </summary>
    [Fact]
    public void CreateByWeek_InvalidOccurrence_ThrowsArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() => DateTimeFactory.CreateByWeek(2024, 2, DayOfWeek.Monday, 0));

        ex.ParamName.ShouldBe("weekAtMonth");
    }

    /// <summary>
    /// 测试用例：验证 `CreateLastDayOfMonth` 在 `InvalidIntDayOfWeek` 场景下，结果为 `ThrowsArgumentOutOfRangeException`。
    /// </summary>
    [Fact]
    public void CreateLastDayOfMonth_InvalidIntDayOfWeek_ThrowsArgumentOutOfRangeException()
    {
        var ex = Should.Throw<ArgumentOutOfRangeException>(() => DateTimeFactory.CreateLastDayOfMonth(2024, 2, 7));

        ex.ParamName.ShouldBe("dayOfWeek");
    }
}

