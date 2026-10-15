namespace Bing.Date;

/// <summary>
/// 验证工作日偏移、年龄、日期区间及时间舍入。
/// </summary>
[Trait("DateTimeUT", "DateTimeExtensions.BusinessAndRound")]
public class DateTimeExtensionsBusinessAndRoundTest
{
    /// <summary>
    /// 验证周五增加一个工作日后到达下周一。
    /// </summary>
    [Fact]
    public void AddBusinessDays_FridayAddOneBusinessDay_ReturnsNextMonday()
    {
        var friday = new DateTime(2024, 1, 5); // Friday

        var result = friday.AddBusinessDays(1);

        result.ShouldBe(new DateTime(2024, 1, 8));
    }

    /// <summary>
    /// 验证周一减去一个工作日后到达上周五。
    /// </summary>
    [Fact]
    public void AddBusinessDays_MondayMinusOneBusinessDay_ReturnsPreviousFriday()
    {
        var monday = new DateTime(2024, 1, 8); // Monday

        var result = monday.AddBusinessDays(-1);

        result.ShouldBe(new DateTime(2024, 1, 5));
    }

    /// <summary>
    /// 验证零工作日偏移保留原日期。
    /// </summary>
    [Fact]
    public void AddBusinessDays_ZeroDays_ReturnsSameDate()
    {
        var date = new DateTime(2024, 1, 10);

        var result = date.AddBusinessDays(0);

        result.ShouldBe(date);
    }

    /// <summary>
    /// 验证参考日期早于当年生日时年龄减一。
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
    /// 验证可空日期区间允许缺省边界且包含上限日期。
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
    /// 验证按秒舍入以五百毫秒为边界并保留时间种类。
    /// </summary>
    /// <param name="millisecond">舍入前的毫秒分量。</param>
    /// <param name="expectedMinute">预期舍入后的分钟分量。</param>
    /// <param name="expectedSecond">预期舍入后的秒分量。</param>
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
    /// 验证舍入拒绝未定义的目标单位。
    /// </summary>
    [Fact]
    public void Round_InvalidRoundTo_ThrowsArgumentOutOfRangeException()
    {
        var source = new DateTime(2024, 1, 1, 8, 30, 15, 500, DateTimeKind.Local);

        var ex = Should.Throw<ArgumentOutOfRangeException>(() => source.Round((RoundTo)999));

        ex.ParamName.ShouldBe("rt");
    }
}
