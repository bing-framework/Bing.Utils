namespace Bing.Date;

/// <summary>
/// 测试类：覆盖 `DateTimeExtensions` 的工作日、区间、年龄与舍入行为。
/// </summary>
[Trait("DateTimeUT", "DateTimeExtensions.BusinessAndRound")]
public class DateTimeExtensionsBusinessAndRoundTest
{
    /// <summary>
    /// 测试用例：验证 `AddBusinessDays` 在 `FridayAddOneBusinessDay` 场景下，结果为 `ReturnsNextMonday`。
    /// </summary>
    [Fact]
    public void AddBusinessDays_FridayAddOneBusinessDay_ReturnsNextMonday()
    {
        var friday = new DateTime(2024, 1, 5); // Friday

        var result = friday.AddBusinessDays(1);

        result.ShouldBe(new DateTime(2024, 1, 8));
    }

    /// <summary>
    /// 测试用例：验证 `AddBusinessDays` 在 `MondayMinusOneBusinessDay` 场景下，结果为 `ReturnsPreviousFriday`。
    /// </summary>
    [Fact]
    public void AddBusinessDays_MondayMinusOneBusinessDay_ReturnsPreviousFriday()
    {
        var monday = new DateTime(2024, 1, 8); // Monday

        var result = monday.AddBusinessDays(-1);

        result.ShouldBe(new DateTime(2024, 1, 5));
    }

    /// <summary>
    /// 测试用例：验证 `AddBusinessDays` 在 `ZeroDays` 场景下，结果为 `ReturnsSameDate`。
    /// </summary>
    [Fact]
    public void AddBusinessDays_ZeroDays_ReturnsSameDate()
    {
        var date = new DateTime(2024, 1, 10);

        var result = date.AddBusinessDays(0);

        result.ShouldBe(date);
    }

    /// <summary>
    /// 测试用例：验证 `ToCalculateAge` 在 `ReferenceDateBeforeBirthday` 场景下，结果为 `SubtractsOneYear`。
    /// </summary>
    [Fact]
    public void ToCalculateAge_ReferenceDateBeforeBirthday_SubtractsOneYear()
    {
        var birthday = new DateTime(2000, 12, 31);
        var reference = new DateTime(2024, 1, 1);

        var age = birthday.ToCalculateAge(reference);

        age.ShouldBe(23);
    }

    /// <summary>
    /// 测试用例：验证 `IsDateBetweenWithBoundary` 在 `NullableMinMax` 场景下，结果为 `ReturnsExpectedByBoundaryRules`。
    /// </summary>
    [Fact]
    public void IsDateBetweenWithBoundary_NullableMinMax_ReturnsExpectedByBoundaryRules()
    {
        var target = new DateTime(2024, 6, 15, 23, 59, 59);

        target.IsDateBetweenWithBoundary((DateTime?)null, (DateTime?)null).ShouldBeTrue();
        target.IsDateBetweenWithBoundary(new DateTime(2024, 6, 1), (DateTime?)null).ShouldBeTrue();
        target.IsDateBetweenWithBoundary((DateTime?)null, new DateTime(2024, 6, 15)).ShouldBeTrue();
        target.IsDateBetweenWithBoundary((DateTime?)null, new DateTime(2024, 6, 14)).ShouldBeFalse();
    }

    /// <summary>
    /// 测试用例：验证 `Round` 在 `SecondBoundary` 场景下，结果为 `RoundsByMillisecondsAndPreservesKind`。
    /// </summary>
    [Theory]
    [InlineData(499, 30, 15)]
    [InlineData(500, 30, 16)]
    public void Round_SecondBoundary_RoundsByMillisecondsAndPreservesKind(int millisecond, int expectedMinute, int expectedSecond)
    {
        var source = new DateTime(2024, 1, 1, 8, 30, 15, millisecond, DateTimeKind.Utc);

        var rounded = source.Round(RoundTo.Second);

        rounded.Minute.ShouldBe(expectedMinute);
        rounded.Second.ShouldBe(expectedSecond);
        rounded.Kind.ShouldBe(DateTimeKind.Utc);
    }

    /// <summary>
    /// 测试用例：验证 `Round` 在 `InvalidRoundTo` 场景下，结果为 `ThrowsArgumentOutOfRangeException`。
    /// </summary>
    [Fact]
    public void Round_InvalidRoundTo_ThrowsArgumentOutOfRangeException()
    {
        var source = new DateTime(2024, 1, 1, 8, 30, 15, 500, DateTimeKind.Local);

        var ex = Should.Throw<ArgumentOutOfRangeException>(() => source.Round((RoundTo)999));

        ex.ParamName.ShouldBe("rt");
    }
}
