using System.Linq;
namespace Bing.Date;
/// <summary>
/// 验证星期文本、星期导航、区间枚举及日期克隆。
/// </summary>
[Trait("DateTimeUT", "DayOfWeek.Extensions")]
public class DayOfWeekAndSystemDateTimeExtensionsTest
{
    /// <summary>
    /// 验证星期转换使用默认中文前缀。
    /// </summary>
    /// <param name="dayOfWeek">待转换的星期值。</param>
    /// <param name="expected">预期中文星期文本。</param>
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
    /// 验证星期转换使用指定前缀。
    /// </summary>
    [Fact]
    public void ToChinese_WithCustomPrefix_ReturnsExpectedText()
    {
        DayOfWeek.Monday.ToChinese("周").ShouldBe("周一");
    }
    /// <summary>
    /// 验证未定义星期值的中文转换结果为空。
    /// </summary>
    [Fact]
    public void ToChinese_WithInvalidEnum_ReturnsNull()
    {
        var result = ((DayOfWeek)999).ToChinese();
        result.ShouldBeNull();
    }
    /// <summary>
    /// 验证星期偏移跨越一周边界。
    /// </summary>
    [Fact]
    public void AddDays_Extension_ShouldDelegateToCalculator()
    {
        DayOfWeek.Friday.AddDays(3).ShouldBe(DayOfWeek.Monday);
        DayOfWeek.Monday.AddDays(-2).ShouldBe(DayOfWeek.Saturday);
    }
    /// <summary>
    /// 验证星期区间枚举按选项包含或排除端点。
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
    /// 验证日期克隆保留时钟周期数与时间种类。
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
