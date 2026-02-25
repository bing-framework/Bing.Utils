using System.Globalization;
namespace Bing.Date;
/// <summary>
/// 测试类：覆盖 `DateTimeSpanCore` 相关行为。
/// </summary>
[Trait("DateTimeUT", "DateTimeSpan.Core")]
public class DateTimeSpanCoreTest
{
    /// <summary>
    /// 测试用例：验证 `Constructor` 在 `WithAllParts` 场景下，结果为 `SetsProperties`。
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
    /// 测试用例：验证 `ImplicitCastToTimeSpan` 在 `IncludesYearsAndMonthsAsDayOffsets` 场景下的行为。
    /// </summary>
    [Fact]
    public void ImplicitCastToTimeSpan_IncludesYearsAndMonthsAsDayOffsets()
    {
        var span = new DateTimeSpan(1, 2, TimeSpan.FromHours(3));
        TimeSpan timeSpan = span;
        timeSpan.ShouldBe(TimeSpan.FromDays(365 + 60).Add(TimeSpan.FromHours(3)));
    }
    /// <summary>
    /// 测试用例：验证 `AddAndSubtract` 在 `WithDateTimeSpan` 场景下，结果为 `ReturnsExpected`。
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
    /// 测试用例：验证 `AddAndSubtract` 在 `WithTimeSpan` 场景下，结果为 `ReturnsExpected`。
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
    /// 测试用例：验证 `UnaryMinus` 在 `CurrentBehavior` 场景下，结果为 `DropsYearsAndMonths`。
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
    /// 测试用例：验证 `CompareOperators` 在 `WithTimeSpan` 场景下，结果为 `ReturnExpected`。
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
    /// 测试用例：验证 `CompareTo` 在 `WithInvalidObject` 场景下，结果为 `ThrowsArgumentException`。
    /// </summary>
    [Fact]
    public void CompareTo_WithInvalidObject_ThrowsArgumentException()
    {
        var span = new DateTimeSpan(0, 0, TimeSpan.FromHours(1));
        var ex = Should.Throw<ArgumentException>(() => span.CompareTo((object)"invalid"));
        ex.ParamName.ShouldBe("value");
    }
    /// <summary>
    /// 测试用例：验证 `Clone` 在 `ReturnsEquivalentCopy` 场景下的行为。
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
    /// 测试用例：验证 `TryParse` 在 `ValidAndInvalidInput` 场景下，结果为 `ReturnsExpected`。
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
    /// 测试用例：验证 `TryParseExact` 在 `WithSingleAndMultipleFormats` 场景下，结果为 `ReturnsExpected`。
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
    /// 测试用例：验证 `NumberExtensions` 在 `CurrentBehavior` 场景下，结果为 `YearsPluralMapsToMonths`。
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
    /// 测试用例：验证 `BeforeAndFrom` 在 `WithExplicitDateTime` 场景下，结果为 `ReturnExpected`。
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
    /// 测试用例：验证 `BeforeAndFrom` 在 `WithExplicitDateTimeOffset` 场景下，结果为 `ReturnExpected`。
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

