using Bing.Date;

namespace Bing.Utils.Tests.Bing.Date;

/// <summary>
/// 验证日期时间扩展的格式化、计算与区间行为。
/// </summary>
public class DateTimeExtensionsTests
{
    /// <summary>
    /// 日期时间格式化和区间测试使用的固定样例。
    /// </summary>
    private static readonly DateTime Sample = new DateTime(2024, 3, 15, 9, 5, 7, 123);

    // ─────────────────────────────────────────────────────────────────
    // ToDateTimeString
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证默认日期时间格式包含秒且不包含毫秒。
    /// </summary>
    [Fact]
    public void ToDateTimeString_DefaultFormat() =>
        Sample.ToDateTimeString().ShouldBe("2024-03-15 09:05:07");

    /// <summary>
    /// 验证省略秒的日期时间格式只保留到分钟。
    /// </summary>
    [Fact]
    public void ToDateTimeString_RemoveSecond() =>
        Sample.ToDateTimeString(isRemoveSecond: true).ShouldBe("2024-03-15 09:05");

    /// <summary>
    /// 验证空日期时间的格式化文本为空。
    /// </summary>
    [Fact]
    public void ToDateTimeString_NullableNull_ReturnsEmpty() =>
        ((DateTime?)null).ToDateTimeString().ShouldBe(string.Empty);

    /// <summary>
    /// 验证可空日期时间有值时按默认格式输出。
    /// </summary>
    [Fact]
    public void ToDateTimeString_NullableWithValue_FormatsCorrectly() =>
        ((DateTime?)Sample).ToDateTimeString().ShouldBe("2024-03-15 09:05:07");

    // ─────────────────────────────────────────────────────────────────
    // ToDateString
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证日期格式只输出年月日。
    /// </summary>
    [Fact]
    public void ToDateString_FormatsDate() =>
        Sample.ToDateString().ShouldBe("2024-03-15");

    /// <summary>
    /// 验证空日期的日期格式文本为空。
    /// </summary>
    [Fact]
    public void ToDateString_NullableNull_ReturnsEmpty() =>
        ((DateTime?)null).ToDateString().ShouldBe(string.Empty);

    // ─────────────────────────────────────────────────────────────────
    // ToTimeString
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证时间格式只输出时分秒。
    /// </summary>
    [Fact]
    public void ToTimeString_FormatsTime() =>
        Sample.ToTimeString().ShouldBe("09:05:07");

    /// <summary>
    /// 验证空日期的时间格式文本为空。
    /// </summary>
    [Fact]
    public void ToTimeString_NullableNull_ReturnsEmpty() =>
        ((DateTime?)null).ToTimeString().ShouldBe(string.Empty);

    // ─────────────────────────────────────────────────────────────────
    // ToMillisecondString
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证毫秒格式保留三位毫秒。
    /// </summary>
    [Fact]
    public void ToMillisecondString_FormatsWithMilliseconds() =>
        Sample.ToMillisecondString().ShouldBe("2024-03-15 09:05:07.123");

    /// <summary>
    /// 验证空日期的毫秒格式文本为空。
    /// </summary>
    [Fact]
    public void ToMillisecondString_NullableNull_ReturnsEmpty() =>
        ((DateTime?)null).ToMillisecondString().ShouldBe(string.Empty);

    // ─────────────────────────────────────────────────────────────────
    // ToChineseDateString / ToChineseDateTimeString
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证日期按中文年月日格式输出。
    /// </summary>
    [Fact]
    public void ToChineseDateString_FormatsCorrectly() =>
        Sample.ToChineseDateString().ShouldBe("2024年3月15日");

    /// <summary>
    /// 验证空日期的中文日期文本为空。
    /// </summary>
    [Fact]
    public void ToChineseDateString_NullableNull_ReturnsEmpty() =>
        ((DateTime?)null).ToChineseDateString().ShouldBe(string.Empty);

    /// <summary>
    /// 验证中文日期时间包含日期、时分和秒。
    /// </summary>
    [Fact]
    public void ToChineseDateTimeString_WithSecond_FormatsCorrectly()
    {
        var result = Sample.ToChineseDateTimeString();
        result.ShouldContain("2024年3月15日");
        result.ShouldContain("9时5分");
        result.ShouldContain("7秒");
    }

    /// <summary>
    /// 验证配置省略秒时中文日期时间不含秒单位。
    /// </summary>
    [Fact]
    public void ToChineseDateTimeString_RemoveSecond_OmitsSecond()
    {
        var result = Sample.ToChineseDateTimeString(isRemoveSecond: true);
        result.ShouldNotContain("秒");
    }

    /// <summary>
    /// 验证空日期的中文日期时间文本为空。
    /// </summary>
    [Fact]
    public void ToChineseDateTimeString_NullableNull_ReturnsEmpty() =>
        ((DateTime?)null).ToChineseDateTimeString().ShouldBe(string.Empty);

    // ─────────────────────────────────────────────────────────────────
    // ToUniqueString
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证不含毫秒的唯一日期文本非空且长度不超过十二。
    /// </summary>
    [Fact]
    public void ToUniqueString_WithoutMillisecond_Returns9Chars()
    {
        var s = Sample.ToUniqueString();
        s.ShouldNotBeNullOrEmpty();
        s.Length.ShouldBeLessThanOrEqualTo(12);
    }

    /// <summary>
    /// 验证包含毫秒时唯一日期文本增加三位。
    /// </summary>
    [Fact]
    public void ToUniqueString_WithMillisecond_IsLonger()
    {
        var without = Sample.ToUniqueString(false);
        var with = Sample.ToUniqueString(true);
        with.Length.ShouldBe(without.Length + 3);
    }

    // ─────────────────────────────────────────────────────────────────
    // In (range check)
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证闭区间内日期满足范围判断。
    /// </summary>
    [Fact]
    public void In_Close_InsideRange_ReturnsTrue()
    {
        var dt = new DateTime(2024, 6, 15);
        dt.In(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)).ShouldBeTrue();
    }

    /// <summary>
    /// 验证闭区间包含边界日期。
    /// </summary>
    [Fact]
    public void In_Close_AtBoundary_ReturnsTrue()
    {
        var dt = new DateTime(2024, 1, 1);
        dt.In(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)).ShouldBeTrue();
    }

    /// <summary>
    /// 验证开区间排除边界日期。
    /// </summary>
    [Fact]
    public void In_Open_AtBoundary_ReturnsFalse()
    {
        var dt = new DateTime(2024, 1, 1);
        dt.In(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), RangeMode.Open).ShouldBeFalse();
    }

    /// <summary>
    /// 验证范围外日期不满足区间判断。
    /// </summary>
    [Fact]
    public void In_Outside_ReturnsFalse()
    {
        var dt = new DateTime(2025, 1, 1);
        dt.In(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)).ShouldBeFalse();
    }

    // ─────────────────────────────────────────────────────────────────
    // SetTime
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证设置时间部分时保留日期。
    /// </summary>
    [Fact]
    public void SetTime_SetsTimePortionCorrectly()
    {
        var dt = new DateTime(2024, 3, 15);
        var result = dt.SetTime(new TimeSpan(10, 30, 0));
        result.Hour.ShouldBe(10);
        result.Minute.ShouldBe(30);
        result.Date.ShouldBe(dt.Date);
    }

    // ─────────────────────────────────────────────────────────────────
    // CompareInterval
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证秒间隔采用起始日期减去结束日期的方向。
    /// </summary>
    [Fact]
    public void CompareInterval_Seconds_ReturnsCorrectDifference()
    {
        var begin = new DateTime(2024, 1, 1, 0, 0, 0);
        var end = new DateTime(2024, 1, 1, 0, 0, 10);
        begin.CompareInterval(end, "s").ShouldBe(-10L);
    }

    /// <summary>
    /// 验证天间隔采用起始日期减去结束日期的方向。
    /// </summary>
    [Fact]
    public void CompareInterval_Days_ReturnsCorrectDifference()
    {
        var begin = new DateTime(2024, 1, 1);
        var end = new DateTime(2024, 1, 4);
        begin.CompareInterval(end, "d").ShouldBe(-3L);
    }

    // ─────────────────────────────────────────────────────────────────
    // IsBetweenTime / IsBetweenDate
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证时间部分位于区间内时返回肯定结果。
    /// </summary>
    [Fact]
    public void IsBetweenTime_InsideRange_ReturnsTrue()
    {
        var now = new DateTime(2024, 1, 1, 10, 0, 0);
        var begin = new DateTime(2024, 1, 1, 8, 0, 0);
        var end = new DateTime(2024, 1, 1, 18, 0, 0);
        now.IsBetweenTime(begin, end).ShouldBeTrue();
    }

    /// <summary>
    /// 验证时间部分位于区间外时返回否定结果。
    /// </summary>
    [Fact]
    public void IsBetweenTime_OutsideRange_ReturnsFalse()
    {
        var now = new DateTime(2024, 1, 1, 20, 0, 0);
        var begin = new DateTime(2024, 1, 1, 8, 0, 0);
        var end = new DateTime(2024, 1, 1, 18, 0, 0);
        now.IsBetweenTime(begin, end).ShouldBeFalse();
    }

    /// <summary>
    /// 验证日期位于区间内时返回肯定结果。
    /// </summary>
    [Fact]
    public void IsBetweenDate_InsideRange_ReturnsTrue()
    {
        var now = new DateTime(2024, 6, 15);
        now.IsBetweenDate(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)).ShouldBeTrue();
    }

    /// <summary>
    /// 验证日期位于区间外时返回否定结果。
    /// </summary>
    [Fact]
    public void IsBetweenDate_OutsideRange_ReturnsFalse()
    {
        var now = new DateTime(2025, 1, 1);
        now.IsBetweenDate(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)).ShouldBeFalse();
    }

    // ─────────────────────────────────────────────────────────────────
    // IsValid
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证普通日期被视为有效日期。
    /// </summary>
    [Fact]
    public void IsValid_NormalDate_ReturnsTrue() =>
        new DateTime(2024, 1, 1).IsValid().ShouldBeTrue();

    /// <summary>
    /// 验证日期最小值不被视为有效日期。
    /// </summary>
    [Fact]
    public void IsValid_MinValue_ReturnsFalse() =>
        DateTime.MinValue.IsValid().ShouldBeFalse();

    /// <summary>
    /// 验证日期最大值不被视为有效日期。
    /// </summary>
    [Fact]
    public void IsValid_MaxValue_ReturnsFalse() =>
        DateTime.MaxValue.IsValid().ShouldBeFalse();
}
