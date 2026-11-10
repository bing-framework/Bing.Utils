using System.Globalization;

namespace Bing.Date;
/// <summary>
/// 验证历史日期转换入口的精度和边界。
/// </summary>
public class DateTimeLegacyConversionRegressionTest
{
    /// <summary>
    /// 验证最大日期与自身的月数或年数间隔为零。
    /// </summary>
    /// <param name="unit">间隔单位。</param>
    [Theory]
    [InlineData("M")]
    [InlineData("y")]
    public void CompareInterval_EqualMaxValue_ReturnsZero(string unit)
    {
        DateTime.MaxValue.CompareInterval(DateTime.MaxValue, unit).ShouldBe(0L);
    }
    /// <summary>
    /// 验证以最大日期为端点的月数或年数间隔可双向计算。
    /// </summary>
    /// <param name="unit">间隔单位。</param>
    /// <param name="expected">预期间隔。</param>
    [Theory]
    [InlineData("M", 12L)]
    [InlineData("y", 1L)]
    public void CompareInterval_RangeEndingAtMaxValue_IsSymmetric(string unit, long expected)
    {
        var start = new DateTime(9998, 12, 31, 23, 59, 59);

        start.CompareInterval(DateTime.MaxValue, unit).ShouldBe(expected);
        DateTime.MaxValue.CompareInterval(start, unit).ShouldBe(expected);
    }
    /// <summary>
    /// 验证月底日期保持逐月推进的历史规则。
    /// </summary>
    [Fact]
    public void CompareInterval_MonthEnd_PreservesSequentialMonthStepping()
    {
        var start = new DateTime(2024, 1, 31);
        var end = new DateTime(2024, 3, 30);

        start.CompareInterval(end, "M").ShouldBe(2L);
    }
    /// <summary>
    /// 验证纪元前后的亚毫秒时间戳向零截断。
    /// </summary>
    /// <param name="ticks">相对于纪元的 ticks。</param>
    /// <param name="expected">预期的毫秒文本。</param>
    [Theory]
    [InlineData(16000L, "1")]
    [InlineData(-16000L, "-1")]
    [InlineData(1L, "0")]
    [InlineData(-1L, "0")]
    public void ToJsGetTime_FractionalMilliseconds_TruncatesTowardZero(long ticks, string expected)
    {
        var utc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(ticks);

        utc.ToJsGetTime().ShouldBe(expected);
    }
    /// <summary>
    /// 验证最大日期的整数毫秒不会发生浮点舍入。
    /// </summary>
    [Fact]
    public void ToJsGetTime_MaxValue_PreservesIntegerMilliseconds()
    {
        var utc = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
        var expected = ((utc.Ticks - new DateTime(1970, 1, 1).Ticks) / TimeSpan.TicksPerMillisecond)
            .ToString(CultureInfo.InvariantCulture);

        utc.ToJsGetTime().ShouldBe(expected);
    }
    /// <summary>
    /// 验证毫秒文本的负号不受当前文化影响。
    /// </summary>
    [Fact]
    public void ToJsGetTime_Milliseconds_UsesInvariantNegativeSign()
    {
        var original = CultureInfo.CurrentCulture;
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.NegativeSign = "~";
        try
        {
            CultureInfo.CurrentCulture = culture;
            var utc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(-16000);
            utc.ToJsGetTime().ShouldBe("-1");
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
