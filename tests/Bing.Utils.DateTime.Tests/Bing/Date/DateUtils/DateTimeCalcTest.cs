using Bing.Tests;

namespace Bing.Date.DateUtils;

/// <summary>
/// 时间日期计算器 测试
/// </summary>
[Trait("DateTimeUT", "DateTime.Calc")]
public class DateTimeCalcTest : TestBase
{
    /// <inheritdoc />
    public DateTimeCalcTest(ITestOutputHelper output) : base(output)
    {
    }

    #region OffsetByMillisecond

    /// <summary>
    /// 测试 - OffsetByMillisecond - 正确进行毫秒偏移
    /// </summary>
    [Fact]
    public void OffsetByMillisecond_CorrectlyOffsetsTime()
    {
        // 准备
        var dt = new DateTime(2023, 1, 1, 0, 0, 0, 0);

        // 执行 - 正向偏移
        var result1 = DateTimeCalc.OffsetByMillisecond(dt, 500);

        // 验证
        result1.ShouldBe(new DateTime(2023, 1, 1, 0, 0, 0, 500));

        // 执行 - 负向偏移
        var result2 = DateTimeCalc.OffsetByMillisecond(dt, -500);

        // 验证
        result2.ShouldBe(new DateTime(2022, 12, 31, 23, 59, 59, 500));
    }

    #endregion

    #region OffsetBySeconds

    /// <summary>
    /// 测试 - OffsetBySeconds - 正确进行秒偏移
    /// </summary>
    [Fact]
    public void OffsetBySeconds_CorrectlyOffsetsTime()
    {
        // 准备
        var dt = new DateTime(2023, 1, 1, 12, 0, 0);

        // 执行 - 正向偏移
        var result1 = DateTimeCalc.OffsetBySeconds(dt, 30);

        // 验证
        result1.ShouldBe(new DateTime(2023, 1, 1, 12, 0, 30));

        // 执行 - 负向偏移
        var result2 = DateTimeCalc.OffsetBySeconds(dt, -30);

        // 验证
        result2.ShouldBe(new DateTime(2023, 1, 1, 11, 59, 30));
    }

    #endregion

    #region OffsetByMinutes

    /// <summary>
    /// 测试 - OffsetByMinutes - 正确进行分钟偏移
    /// </summary>
    [Fact]
    public void OffsetByMinutes_CorrectlyOffsetsTime()
    {
        // 准备
        var dt = new DateTime(2023, 1, 1, 12, 0, 0);

        // 执行 - 正向偏移
        var result1 = DateTimeCalc.OffsetByMinutes(dt, 30);

        // 验证
        result1.ShouldBe(new DateTime(2023, 1, 1, 12, 30, 0));

        // 执行 - 负向偏移
        var result2 = DateTimeCalc.OffsetByMinutes(dt, -30);

        // 验证
        result2.ShouldBe(new DateTime(2023, 1, 1, 11, 30, 0));
    }

    #endregion

    #region OffsetByHours

    /// <summary>
    /// 测试 - OffsetByHours - 正确进行小时偏移
    /// </summary>
    [Fact]
    public void OffsetByHours_CorrectlyOffsetsTime()
    {
        // 准备
        var dt = new DateTime(2023, 1, 1, 12, 0, 0);

        // 执行 - 正向偏移
        var result1 = DateTimeCalc.OffsetByHours(dt, 5);

        // 验证
        result1.ShouldBe(new DateTime(2023, 1, 1, 17, 0, 0));

        // 执行 - 负向偏移
        var result2 = DateTimeCalc.OffsetByHours(dt, -5);

        // 验证
        result2.ShouldBe(new DateTime(2023, 1, 1, 7, 0, 0));
    }

    #endregion

    #region OffsetByDays

    /// <summary>
    /// 测试 - OffsetByDays - 正确进行天数偏移
    /// </summary>
    [Fact]
    public void OffsetByDays_CorrectlyOffsetsDate()
    {
        // 准备
        var dt = new DateTime(2023, 1, 15, 12, 0, 0);

        // 执行 - 正向偏移
        var result1 = DateTimeCalc.OffsetByDays(dt, 5);

        // 验证
        result1.ShouldBe(new DateTime(2023, 1, 20, 12, 0, 0));

        // 执行 - 负向偏移
        var result2 = DateTimeCalc.OffsetByDays(dt, -5);

        // 验证
        result2.ShouldBe(new DateTime(2023, 1, 10, 12, 0, 0));
    }

    #endregion

    #region OffsetByWeek

    /// <summary>
    /// 测试 - OffsetByWeek - 成功获取特定星期几
    /// </summary>
    [Fact]
    public void OffsetByWeek_ReturnsCorrectDate()
    {
        // 准备 - 2023年1月第二个周二
        var expected = new DateTime(2023, 1, 10);

        // 执行
        var result = DateTimeCalc.OffsetByWeek(2023, 1, 2, DayOfWeek.Tuesday);

        // 验证
        result.ShouldBe(expected);
        result.DayOfWeek.ShouldBe(DayOfWeek.Tuesday);
    }

    /// <summary>
    /// 测试 - OffsetByWeek - 超出范围返回MinValue
    /// </summary>
    [Fact]
    public void OffsetByWeek_OutOfRange_ReturnsMinValue()
    {
        // 执行 - 例如2023年2月第5个周一，超出范围
        var result = DateTimeCalc.OffsetByWeek(2023, 2, 5, DayOfWeek.Monday);

        // 验证
        result.ShouldBe(DateTime.MinValue);
    }

    /// <summary>
    /// 测试 - OffsetByWeek - 无效参数抛出异常
    /// </summary>
    [Fact]
    public void OffsetByWeek_InvalidWeekAtMonth_ThrowsArgumentException()
    {
        // 验证 - 小于1抛出异常
        Should.Throw<ArgumentException>(() => DateTimeCalc.OffsetByWeek(2023, 1, 0, DayOfWeek.Monday));

        // 验证 - 大于5抛出异常
        Should.Throw<ArgumentException>(() => DateTimeCalc.OffsetByWeek(2023, 1, 6, DayOfWeek.Monday));
    }

    /// <summary>
    /// 测试 - TryOffsetByWeek - 成功获取特定星期几
    /// </summary>
    [Fact]
    public void TryOffsetByWeek_SuccessfulCase_ReturnsTrue()
    {
        // 准备
        DateTime result;

        // 执行
        var success = DateTimeCalc.TryOffsetByWeek(2023, 1, 2, DayOfWeek.Tuesday, out result);

        // 验证
        success.ShouldBeTrue();
        result.ShouldBe(new DateTime(2023, 1, 10));
    }

    /// <summary>
    /// 测试 - TryOffsetByWeek - 超出范围返回False
    /// </summary>
    [Fact]
    public void TryOffsetByWeek_OutOfRange_ReturnsFalse()
    {
        // 准备
        DateTime result;

        // 执行 - 超出范围
        var success = DateTimeCalc.TryOffsetByWeek(2023, 2, 5, DayOfWeek.Monday, out result);

        // 验证
        success.ShouldBeFalse();
        result.ShouldBe(DateTime.MinValue);
    }

    #endregion

    #region OffsetByWeeks

    /// <summary>
    /// 测试 - OffsetByWeeks - 正确进行周数偏移
    /// </summary>
    [Fact]
    public void OffsetByWeeks_CorrectlyOffsetsDate()
    {
        // 准备
        var dt = new DateTime(2023, 1, 1); // 2023年1月1日，星期日

        // 执行 - 向后2周
        var result1 = DateTimeCalc.OffsetByWeeks(dt, 2);

        // 验证
        result1.ShouldBe(new DateTime(2023, 1, 15)); // 1月15日，仍是星期日
        result1.DayOfWeek.ShouldBe(DayOfWeek.Sunday);

        // 执行 - 向前2周
        var result2 = DateTimeCalc.OffsetByWeeks(dt, -2);

        // 验证
        result2.ShouldBe(new DateTime(2022, 12, 18)); // 12月18日，仍是星期日
        result2.DayOfWeek.ShouldBe(DayOfWeek.Sunday);
    }

    #endregion

    #region OffsetByWeekBefore / OffsetByWeekAfter

    /// <summary>
    /// 测试 - OffsetByWeekBefore - 正确获取上一个指定星期几
    /// </summary>
    [Fact]
    public void OffsetByWeekBefore_ReturnsCorrectDate()
    {
        // 准备 - 2023年1月15日，星期日
        var dt = new DateTime(2023, 1, 15);

        // 执行 - 获取上一个星期三
        var result = DateTimeCalc.OffsetByWeekBefore(dt, DayOfWeek.Wednesday);

        // 验证 - 应当是1月11日
        result.ShouldBe(new DateTime(2023, 1, 11));
        result.DayOfWeek.ShouldBe(DayOfWeek.Wednesday);

        // 执行 - 获取上一个星期日（当前就是星期日，应返回上周日）
        var result2 = DateTimeCalc.OffsetByWeekBefore(dt, DayOfWeek.Sunday);

        // 验证 - 应当是1月8日
        result2.ShouldBe(new DateTime(2023, 1, 8));
        result2.DayOfWeek.ShouldBe(DayOfWeek.Sunday);
    }

    /// <summary>
    /// 测试 - OffsetByWeekAfter - 正确获取下一个指定星期几
    /// </summary>
    [Fact]
    public void OffsetByWeekAfter_ReturnsCorrectDate()
    {
        // 准备 - 2023年1月15日，星期日
        var dt = new DateTime(2023, 1, 15);

        // 执行 - 获取下一个星期三
        var result = DateTimeCalc.OffsetByWeekAfter(dt, DayOfWeek.Wednesday);

        // 验证 - 应当是1月18日
        result.ShouldBe(new DateTime(2023, 1, 18));
        result.DayOfWeek.ShouldBe(DayOfWeek.Wednesday);

        // 执行 - 获取下一个星期日（当前就是星期日，应返回下周日）
        var result2 = DateTimeCalc.OffsetByWeekAfter(dt, DayOfWeek.Sunday);

        // 验证 - 应当是1月22日
        result2.ShouldBe(new DateTime(2023, 1, 22));
        result2.DayOfWeek.ShouldBe(DayOfWeek.Sunday);
    }

    #endregion

    #region OffsetOfDayOfWeek

    /// <summary>
    /// 测试 - OffsetOfDayOfWeek - 正确偏移到指定星期几
    /// </summary>
    [Theory]
    [InlineData(DayOfWeek.Monday, 1, "2023-01-16")] // 下一个周一
    [InlineData(DayOfWeek.Friday, 1, "2023-01-20")] // 下一个周五
    [InlineData(DayOfWeek.Sunday, 1, "2023-01-22")] // 下一个周日
    [InlineData(DayOfWeek.Monday, -1, "2023-01-09")] // 上一个周一
    [InlineData(DayOfWeek.Friday, -1, "2023-01-13")] // 上一个周五
    [InlineData(DayOfWeek.Sunday, -1, "2023-01-08")] // 上一个周日
    [InlineData(DayOfWeek.Sunday, 0, "2023-01-15")] // 当前日期
    public void OffsetOfDayOfWeek_ReturnsCorrectDate(DayOfWeek dayOfWeek, int weekOffset, string expectedDate)
    {
        // 准备 - 2023年1月15日，星期日
        var dt = new DateTime(2023, 1, 15);
        var expected = DateTime.Parse(expectedDate);

        // 执行
        var result = DateTimeCalc.OffsetOfDayOfWeek(dt, dayOfWeek, weekOffset);

        // 验证
        result.ShouldBe(expected);
        result.DayOfWeek.ShouldBe(dayOfWeek);
    }

    #endregion

    #region OffsetByMonths

    /// <summary>
    /// 测试 - OffsetByMonths - 绝对偏移模式
    /// </summary>
    [Fact]
    public void OffsetByMonths_AbsoluteMode_CorrectlyOffsetsDate()
    {
        // 准备
        var dt = new DateTime(2023, 1, 15);

        // 执行 - 向后3个月
        var result1 = DateTimeCalc.OffsetByMonths(dt, 3, DateTimeOffsetOptions.Absolute);

        // 验证 - 使用绝对偏移模式
        result1.ShouldBe(dt.AddMonths(3));

        // 执行 - 向前3个月
        var result2 = DateTimeCalc.OffsetByMonths(dt, -3, DateTimeOffsetOptions.Absolute);

        // 验证
        result2.ShouldBe(dt.AddMonths(-3));
    }

    /// <summary>
    /// 测试 - OffsetByMonths - 相对偏移模式处理月份天数差异
    /// </summary>
    [Fact]
    public void OffsetByMonths_RelativelyMode_HandlesMonthBoundaries()
    {
        // 准备 - 2024年1月31日
        var dt = new DateTime(2024, 1, 31);

        // 执行 - 偏移到2月（2月没有31日）
        var result = DateTimeCalc.OffsetByMonths(dt, 1, DateTimeOffsetOptions.Relatively);

        // 验证 - 结果应当是2月29日（2024是闰年）
        result.ShouldBe(new DateTime(2024, 2, 29));

        // 准备 - 2023年3月30日
        var dt2 = new DateTime(2023, 3, 30);

        // 执行 - 偏移到2月（2月没有30日）
        var result2 = DateTimeCalc.OffsetByMonths(dt2, -1, DateTimeOffsetOptions.Relatively);

        // 验证 - 结果应当是2月28日（2023非闰年）
        result2.ShouldBe(new DateTime(2023, 2, 28));
    }

    /// <summary>
    /// 测试 - OffsetByMonths - 处理年份跨越
    /// </summary>
    [Fact]
    public void OffsetByMonths_HandlesYearBoundaries()
    {
        // 准备
        var dt = new DateTime(2023, 12, 15);

        // 执行 - 向后2个月
        var result1 = DateTimeCalc.OffsetByMonths(dt, 2, DateTimeOffsetOptions.Relatively);

        // 验证 - 结果应当是2024年2月15日
        result1.ShouldBe(new DateTime(2024, 2, 15));

        // 执行 - 向前2个月
        var result2 = DateTimeCalc.OffsetByMonths(dt, -14, DateTimeOffsetOptions.Relatively);

        // 验证 - 结果应当是2022年10月15日
        result2.ShouldBe(new DateTime(2022, 10, 15));
    }

    #endregion

    #region OffsetByQuarters

    /// <summary>
    /// 测试 - OffsetByQuarters - 正确进行季度偏移
    /// </summary>
    [Fact]
    public void OffsetByQuarters_CorrectlyOffsetsDate()
    {
        // 准备
        var dt = new DateTime(2023, 1, 15);

        // 执行 - 绝对偏移，向后2个季度
        var result1 = DateTimeCalc.OffsetByQuarters(dt, 2, DateTimeOffsetOptions.Absolute);

        // 验证 - 结果应当是2023年7月15日
        result1.ShouldBe(dt.AddMonths(6));

        // 执行 - 相对偏移，向后2个季度
        var result2 = DateTimeCalc.OffsetByQuarters(dt, 2, DateTimeOffsetOptions.Relatively);

        // 验证 - 结果应当是2023年7月15日
        result2.ShouldBe(new DateTime(2023, 7, 15));

        // 准备 - 有月份天数差异的日期
        var dt2 = new DateTime(2023, 1, 31);

        // 执行 - 相对偏移到第二季度
        var result3 = DateTimeCalc.OffsetByQuarters(dt2, 1, DateTimeOffsetOptions.Relatively);

        // 验证 - 4月没有31日，应当是4月30日
        result3.ShouldBe(new DateTime(2023, 4, 30));
    }

    #endregion

    #region OffsetByYears

    /// <summary>
    /// 测试 - OffsetByYears - 正确进行年份偏移
    /// </summary>
    [Fact]
    public void OffsetByYears_CorrectlyOffsetsDate()
    {
        // 准备
        var dt = new DateTime(2023, 1, 15);

        // 执行 - 绝对偏移，向后2年
        var result1 = DateTimeCalc.OffsetByYears(dt, 2, DateTimeOffsetOptions.Absolute);

        // 验证
        result1.ShouldBe(new DateTime(2025, 1, 15));

        // 执行 - 相对偏移，向前2年
        var result2 = DateTimeCalc.OffsetByYears(dt, -2, DateTimeOffsetOptions.Relatively);

        // 验证
        result2.ShouldBe(new DateTime(2021, 1, 15));
    }

    /// <summary>
    /// 测试 - OffsetByYears - 处理闰年日期
    /// </summary>
    [Fact]
    public void OffsetByYears_HandlesLeapYears()
    {
        // 准备 - 闰年2月29日
        var dt = new DateTime(2024, 2, 29);

        // 执行 - 相对偏移到非闰年
        var result = DateTimeCalc.OffsetByYears(dt, 1, DateTimeOffsetOptions.Relatively);

        // 验证 - 2025年2月没有29日，应当是2月28日
        result.ShouldBe(new DateTime(2025, 2, 28));
    }

    #endregion

    #region OffsetByDuration

    /// <summary>
    /// 测试 - OffsetByDuration - 正确应用持续时间偏移
    /// </summary>
    [Fact]
    public void OffsetByDuration_CorrectlyOffsetsDate()
    {
        // 准备
        var dt = new DateTime(2023, 1, 15, 12, 0, 0);

        // 创建NodaTime的Duration
        var duration = NodaTime.Duration.FromDays(2.5); // 2天12小时

        // 执行
        var result = DateTimeCalc.OffsetByDuration(dt, duration);

        // 验证 - 结果应当是2023年1月18日0点
        result.ShouldBe(new DateTime(2023, 1, 18, 0, 0, 0));
    }

    #endregion
}