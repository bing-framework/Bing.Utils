using System.Globalization;
namespace Bing.Date;
/// <summary>
/// 日期时间跨度的运算、比较和解析测试。
/// </summary>
[Trait("DateTimeUT", "DateTimeSpan.Core")]
public class DateTimeSpanCoreTest
{
    /// <summary>
    /// 验证构造函数保存年、月和时间跨度分量。
    /// </summary>
    [Fact]
    public void Constructor_WithAllParts_SetsProperties()
    {
        var span = new DateTimeSpan(1, 2, TimeSpan.FromHours(3));
        span.Years.ShouldBe(1);
        span.Months.ShouldBe(2);
        span.TimeSpan.ShouldBe(TimeSpan.FromHours(3));
    }
    /// <summary>
    /// 验证转换时按每年三百六十五天、每月三十天折算。
    /// </summary>
    [Fact]
    public void ImplicitCastToTimeSpan_IncludesYearsAndMonthsAsDayOffsets()
    {
        var span = new DateTimeSpan(1, 2, TimeSpan.FromHours(3));
        TimeSpan timeSpan = span;
        timeSpan.ShouldBe(TimeSpan.FromDays(365 + 60).Add(TimeSpan.FromHours(3)));
    }
    /// <summary>
    /// 验证日期时间跨度逐分量相加减。
    /// </summary>
    [Fact]
    public void AddAndSubtract_WithDateTimeSpan_ReturnsExpected()
    {
        var left = new DateTimeSpan(1, 2, TimeSpan.FromHours(3));
        var right = new DateTimeSpan(2, 3, TimeSpan.FromMinutes(30));
        var added = left + right;
        var subtracted = added - right;
        added.Years.ShouldBe(3);
        added.Months.ShouldBe(5);
        added.TimeSpan.ShouldBe(TimeSpan.FromHours(3.5));
        subtracted.ShouldBe(left);
    }
    /// <summary>
    /// 验证时间跨度的加减保留月份分量。
    /// </summary>
    [Fact]
    public void AddAndSubtract_WithTimeSpan_ReturnsExpected()
    {
        var span = new DateTimeSpan(0, 1, TimeSpan.FromHours(2));
        var added = span + TimeSpan.FromMinutes(45);
        var subtracted = added - TimeSpan.FromMinutes(45);
        added.Months.ShouldBe(1);
        added.TimeSpan.ShouldBe(TimeSpan.FromHours(2.75));
        subtracted.ShouldBe(span);
    }
    /// <summary>
    /// 验证取负时将全部分量折算为时间跨度。
    /// </summary>
    [Fact]
    public void UnaryMinus_CurrentBehavior_DropsYearsAndMonths()
    {
        var span = new DateTimeSpan(1, 2, TimeSpan.FromHours(1));
        var negated = -span;
        negated.Years.ShouldBe(0);
        negated.Months.ShouldBe(0);
        negated.TimeSpan.ShouldBe(-((TimeSpan)span));
    }
    /// <summary>
    /// 验证大小比较及与时间跨度的相等判断。
    /// </summary>
    [Fact]
    public void CompareOperators_WithTimeSpan_ReturnExpected()
    {
        var shortSpan = new DateTimeSpan(0, 0, TimeSpan.FromHours(1));
        var longSpan = new DateTimeSpan(0, 0, TimeSpan.FromHours(2));
        (shortSpan < longSpan).ShouldBeTrue();
        (shortSpan <= longSpan).ShouldBeTrue();
        (longSpan > shortSpan).ShouldBeTrue();
        (longSpan >= shortSpan).ShouldBeTrue();
        (shortSpan == TimeSpan.FromHours(1)).ShouldBeTrue();
        (shortSpan != TimeSpan.FromHours(2)).ShouldBeTrue();
    }
    /// <summary>
    /// 验证不兼容的比较对象被拒绝并报告参数名。
    /// </summary>
    [Fact]
    public void CompareTo_WithInvalidObject_ThrowsArgumentException()
    {
        var span = new DateTimeSpan(0, 0, TimeSpan.FromHours(1));
        var ex = Should.Throw<ArgumentException>(() => span.CompareTo((object)"invalid"));
        ex.ParamName.ShouldBe("value");
    }
    /// <summary>
    /// 验证克隆结果与原值相等且哈希码一致。
    /// </summary>
    [Fact]
    public void Clone_ReturnsEquivalentCopy()
    {
        var span = new DateTimeSpan(1, 2, TimeSpan.FromHours(3));
        var clone = (DateTimeSpan)span.Clone();
        clone.ShouldBe(span);
        clone.GetHashCode().ShouldBe(span.GetHashCode());
    }
    /// <summary>
    /// 验证时间跨度解析的成功结果及失败默认值。
    /// </summary>
    [Fact]
    public void TryParse_ValidAndInvalidInput_ReturnsExpected()
    {
        var parsed = DateTimeSpan.TryParse("01:30:00", out var successValue);
        var failed = DateTimeSpan.TryParse("not-a-timespan", out var failValue);
        parsed.ShouldBeTrue();
        successValue.TimeSpan.ShouldBe(TimeSpan.FromHours(1.5));
        failed.ShouldBeFalse();
        failValue.ShouldBe(default(DateTimeSpan));
    }
    /// <summary>
    /// 验证按单个和多个格式精确解析时间跨度。
    /// </summary>
    [Fact]
    public void TryParseExact_WithSingleAndMultipleFormats_ReturnsExpected()
    {
        var provider = CultureInfo.InvariantCulture;
        var single = DateTimeSpan.TryParseExact("01:02:03", "c", provider, out var singleValue);
        var multiple = DateTimeSpan.TryParseExact("1:02:03", new[] { "g", "G" }, provider, out var multipleValue);
        single.ShouldBeTrue();
        multiple.ShouldBeTrue();
        singleValue.TimeSpan.ShouldBe(TimeSpan.FromHours(1).Add(TimeSpan.FromMinutes(2)).Add(TimeSpan.FromSeconds(3)));
        multipleValue.TimeSpan.ShouldBe(TimeSpan.FromHours(1).Add(TimeSpan.FromMinutes(2)).Add(TimeSpan.FromSeconds(3)));
    }
    /// <summary>
    /// 验证当前复数年份扩展映射到月份分量。
    /// </summary>
    [Fact]
    public void NumberExtensions_CurrentBehavior_YearsPluralMapsToMonths()
    {
        var pluralYears = 2.Years();
        var singularYear = 1.Year();
        pluralYears.Years.ShouldBe(0);
        pluralYears.Months.ShouldBe(2);
        singularYear.Years.ShouldBe(1);
        singularYear.Months.ShouldBe(0);
    }
    /// <summary>
    /// 验证以日期时间为基准向前和向后偏移。
    /// </summary>
    [Fact]
    public void BeforeAndFrom_WithExplicitDateTime_ReturnExpected()
    {
        var origin = new DateTime(2024, 5, 10, 12, 0, 0, DateTimeKind.Unspecified);
        var span = new DateTimeSpan(1, 2, TimeSpan.FromDays(3));
        var before = span.Before(origin);
        var from = span.From(origin);
        before.ShouldBe(new DateTime(2023, 3, 7, 12, 0, 0, DateTimeKind.Unspecified));
        from.ShouldBe(new DateTime(2025, 7, 13, 12, 0, 0, DateTimeKind.Unspecified));
    }
    /// <summary>
    /// 验证以带偏移日期时间为基准向前和向后偏移。
    /// </summary>
    [Fact]
    public void BeforeAndFrom_WithExplicitDateTimeOffset_ReturnExpected()
    {
        var origin = new DateTimeOffset(2024, 5, 10, 12, 0, 0, TimeSpan.FromHours(8));
        var span = new DateTimeSpan(0, 1, TimeSpan.FromHours(5));
        var before = span.Before(origin);
        var from = span.From(origin);
        before.ShouldBe(new DateTimeOffset(2024, 4, 10, 7, 0, 0, TimeSpan.FromHours(8)));
        from.ShouldBe(new DateTimeOffset(2024, 6, 10, 17, 0, 0, TimeSpan.FromHours(8)));
    }
}

