using System.Linq;
namespace Bing.Date;
/// <summary>
/// 测试类：覆盖 `DayOfWeekAndSystemDateTimeExtensions` 相关行为。
/// </summary>
[Trait("DateTimeUT", "DayOfWeek.Extensions")]
public class DayOfWeekAndSystemDateTimeExtensionsTest
{
    /// <summary>
    /// 测试用例：验证 `ToChinese` 在 `WithDefaultPrefix` 场景下，结果为 `ReturnsExpectedText`。
    /// </summary>
    [Theory]
    [InlineData(DayOfWeek.Sunday, "星期日")]
    [InlineData(DayOfWeek.Monday, "星期一")]
    [InlineData(DayOfWeek.Tuesday, "星期二")]
    [InlineData(DayOfWeek.Wednesday, "星期三")]
    [InlineData(DayOfWeek.Thursday, "星期四")]
    [InlineData(DayOfWeek.Friday, "星期五")]
    [InlineData(DayOfWeek.Saturday, "星期六")]
    public void ToChinese_WithDefaultPrefix_ReturnsExpectedText(DayOfWeek dayOfWeek, string expected)
    {
        dayOfWeek.ToChinese().ShouldBe(expected);
    }
    /// <summary>
    /// 测试用例：验证 `ToChinese` 在 `WithCustomPrefix` 场景下，结果为 `ReturnsExpectedText`。
    /// </summary>
    [Fact]
    public void ToChinese_WithCustomPrefix_ReturnsExpectedText()
    {
        DayOfWeek.Monday.ToChinese("周").ShouldBe("周一");
    }
    /// <summary>
    /// 测试用例：验证 `ToChinese` 在 `WithInvalidEnum` 场景下，结果为 `ReturnsNull`。
    /// </summary>
    [Fact]
    public void ToChinese_WithInvalidEnum_ReturnsNull()
    {
        var result = ((DayOfWeek)999).ToChinese();
        result.ShouldBeNull();
    }
    /// <summary>
    /// 测试用例：验证 `AddDays` 在 `Extension` 场景下，结果为 `ShouldDelegateToCalculator`。
    /// </summary>
    [Fact]
    public void AddDays_Extension_ShouldDelegateToCalculator()
    {
        DayOfWeek.Friday.AddDays(3).ShouldBe(DayOfWeek.Monday);
        DayOfWeek.Monday.AddDays(-2).ShouldBe(DayOfWeek.Saturday);
    }
    /// <summary>
    /// 测试用例：验证 `GetDaysBetween` 在 `WithBoundaryAndWithoutBoundary` 场景下，结果为 `ReturnsExpected`。
    /// </summary>
    [Fact]
    public void GetDaysBetween_WithBoundaryAndWithoutBoundary_ReturnsExpected()
    {
        var withBoundary = DayOfWeek.Monday.GetDaysBetween(DayOfWeek.Thursday).ToArray();
        var withoutBoundary = DayOfWeek.Monday.GetDaysBetween(DayOfWeek.Thursday, includeBoundary: false).ToArray();
        withBoundary.ShouldBe(new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday });
        withoutBoundary.ShouldBe(new[] { DayOfWeek.Tuesday, DayOfWeek.Wednesday });
    }
    /// <summary>
    /// 测试用例：验证 `Clone` 在 `DateTimeExtension` 场景下，结果为 `PreservesTicksAndKind`。
    /// </summary>
    [Fact]
    public void Clone_DateTimeExtension_PreservesTicksAndKind()
    {
        var original = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var clone = original.Clone();
        clone.Ticks.ShouldBe(original.Ticks);
        clone.Kind.ShouldBe(DateTimeKind.Utc);
        clone.ShouldBe(original);
    }
}
