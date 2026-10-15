using NodaTime;

namespace Bing.Date;

/// <summary>
/// 验证日期对象的构造、分量设置、日期偏移及 NodaTime 转换。
/// </summary>
[Trait("DateTimeUT", "DateInfo.Contract")]
public class DateInfoContractTest
{
    /// <summary>
    /// 验证日期对象构造时截断时间并保留时间种类。
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
    /// 验证按年月日构造日期对象时保存闰日。
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
    /// 验证日期分量设置后返回更新的日期。
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
    /// 验证月份设置拒绝超出一至十二月的数值。
    /// </summary>
    /// <param name="month">用于验证范围检查的无效月份。</param>
    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void MonthSetter_OutOfRangeValue_ThrowsArgumentOutOfRangeException(int month)
    {
        var info = new DateInfo(2024, 1, 1);

        Should.Throw<ArgumentOutOfRangeException>(() => info.Month = month);
    }

    /// <summary>
    /// 验证日分量设置拒绝目标月份中不存在的日期。
    /// </summary>
    [Fact]
    public void DaySetter_InvalidDayInMonth_ThrowsArgumentOutOfRangeException()
    {
        var info = new DateInfo(2024, 4, 1);

        Should.Throw<ArgumentOutOfRangeException>(() => info.Day = 31);
    }

    /// <summary>
    /// 验证连续日期偏移修改同一对象并返回原引用。
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
    /// 验证日期对象转换为等价的 NodaTime 日期及午夜时间。
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
