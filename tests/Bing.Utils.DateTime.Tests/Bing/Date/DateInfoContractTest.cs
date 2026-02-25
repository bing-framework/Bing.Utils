using NodaTime;

namespace Bing.Date;

/// <summary>
/// 测试类：覆盖 `DateInfo` 相关行为。
/// </summary>
[Trait("DateTimeUT", "DateInfo.Contract")]
public class DateInfoContractTest
{
    /// <summary>
    /// 测试用例：验证 `DateInfo` 在 `ConstructWithDateTime` 场景下，结果为 `TruncatesTimePart`。
    /// </summary>
    [Fact]
    public void DateInfo_ConstructWithDateTime_TruncatesTimePart()
    {
        var source = new DateTime(2024, 7, 8, 9, 10, 11, 123, DateTimeKind.Local);

        var info = new DateInfo(source);
        var result = info.ToDateTime();

        result.ShouldBe(new DateTime(2024, 7, 8, 0, 0, 0, 0, DateTimeKind.Local));
    }

    /// <summary>
    /// 测试用例：验证 `DateInfo` 在 `ConstructWithYearMonthDay` 场景下，结果为 `StoresExpectedDate`。
    /// </summary>
    [Fact]
    public void DateInfo_ConstructWithYearMonthDay_StoresExpectedDate()
    {
        var info = new DateInfo(2020, 2, 29);

        info.Year.ShouldBe(2020);
        info.Month.ShouldBe(2);
        info.Day.ShouldBe(29);
    }

    /// <summary>
    /// 测试用例：验证 `Setters` 在 `ValidDateChange` 场景下，结果为 `UpdatesDateFields`。
    /// </summary>
    [Fact]
    public void Setters_ValidDateChange_UpdatesDateFields()
    {
        var info = new DateInfo(2024, 1, 15);

        info.Year = 2025;
        info.Month = 2;
        info.Day = 20;

        var result = info.ToDateTime();
        result.ShouldBe(new DateTime(2025, 2, 20));
    }

    /// <summary>
    /// 测试用例：验证 `MonthSetter` 在 `OutOfRangeValue` 场景下，结果为 `ThrowsArgumentOutOfRangeException`。
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void MonthSetter_OutOfRangeValue_ThrowsArgumentOutOfRangeException(int month)
    {
        var info = new DateInfo(2024, 1, 1);

        Should.Throw<ArgumentOutOfRangeException>(() => info.Month = month);
    }

    /// <summary>
    /// 测试用例：验证 `DaySetter` 在 `InvalidDayInMonth` 场景下，结果为 `ThrowsArgumentOutOfRangeException`。
    /// </summary>
    [Fact]
    public void DaySetter_InvalidDayInMonth_ThrowsArgumentOutOfRangeException()
    {
        var info = new DateInfo(2024, 4, 1);

        Should.Throw<ArgumentOutOfRangeException>(() => info.Day = 31);
    }

    /// <summary>
    /// 测试用例：验证 `AddDays` 在 `ChainCall` 场景下，结果为 `MutatesSelfAndReturnsSameReference`。
    /// </summary>
    [Fact]
    public void AddDays_ChainCall_MutatesSelfAndReturnsSameReference()
    {
        var info = new DateInfo(2024, 1, 1);

        var returned = info.AddDays(3).AddDays(-1);

        returned.ShouldBeSameAs(info);
        info.ToDateTime().ShouldBe(new DateTime(2024, 1, 3));
    }

    /// <summary>
    /// 测试用例：验证 `ToLocalDateAndToLocalDateTime` 在 `NormalDate` 场景下，结果为 `ReturnsEquivalentNodaValues`。
    /// </summary>
    [Fact]
    public void ToLocalDateAndToLocalDateTime_NormalDate_ReturnsEquivalentNodaValues()
    {
        var info = new DateInfo(2024, 12, 31);

        var localDate = info.ToLocalDate();
        var localDateTime = info.ToLocalDateTime();

        localDate.ShouldBe(new LocalDate(2024, 12, 31));
        localDateTime.ShouldBe(new LocalDateTime(2024, 12, 31, 0, 0, 0));
    }
}
