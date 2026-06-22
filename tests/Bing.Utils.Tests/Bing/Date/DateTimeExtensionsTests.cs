using Bing.Date;

namespace Bing.Utils.Tests.Bing.Date;

/// <summary>
/// <see cref="DateTimeExtensions"/> 单元测试
/// </summary>
public class DateTimeExtensionsTests
{
    private static readonly DateTime Sample = new DateTime(2024, 3, 15, 9, 5, 7, 123);

    // ─────────────────────────────────────────────────────────────────
    // ToDateTimeString
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ToDateTimeString_DefaultFormat() =>
        Sample.ToDateTimeString().ShouldBe("2024-03-15 09:05:07");

    [Fact]
    public void ToDateTimeString_RemoveSecond() =>
        Sample.ToDateTimeString(isRemoveSecond: true).ShouldBe("2024-03-15 09:05");

    [Fact]
    public void ToDateTimeString_NullableNull_ReturnsEmpty() =>
        ((DateTime?)null).ToDateTimeString().ShouldBe(string.Empty);

    [Fact]
    public void ToDateTimeString_NullableWithValue_FormatsCorrectly() =>
        ((DateTime?)Sample).ToDateTimeString().ShouldBe("2024-03-15 09:05:07");

    // ─────────────────────────────────────────────────────────────────
    // ToDateString
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ToDateString_FormatsDate() =>
        Sample.ToDateString().ShouldBe("2024-03-15");

    [Fact]
    public void ToDateString_NullableNull_ReturnsEmpty() =>
        ((DateTime?)null).ToDateString().ShouldBe(string.Empty);

    // ─────────────────────────────────────────────────────────────────
    // ToTimeString
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ToTimeString_FormatsTime() =>
        Sample.ToTimeString().ShouldBe("09:05:07");

    [Fact]
    public void ToTimeString_NullableNull_ReturnsEmpty() =>
        ((DateTime?)null).ToTimeString().ShouldBe(string.Empty);

    // ─────────────────────────────────────────────────────────────────
    // ToMillisecondString
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ToMillisecondString_FormatsWithMilliseconds() =>
        Sample.ToMillisecondString().ShouldBe("2024-03-15 09:05:07.123");

    [Fact]
    public void ToMillisecondString_NullableNull_ReturnsEmpty() =>
        ((DateTime?)null).ToMillisecondString().ShouldBe(string.Empty);

    // ─────────────────────────────────────────────────────────────────
    // ToChineseDateString / ToChineseDateTimeString
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ToChineseDateString_FormatsCorrectly() =>
        Sample.ToChineseDateString().ShouldBe("2024年3月15日");

    [Fact]
    public void ToChineseDateString_NullableNull_ReturnsEmpty() =>
        ((DateTime?)null).ToChineseDateString().ShouldBe(string.Empty);

    [Fact]
    public void ToChineseDateTimeString_WithSecond_FormatsCorrectly()
    {
        var result = Sample.ToChineseDateTimeString();
        result.ShouldContain("2024年3月15日");
        result.ShouldContain("9时5分");
        result.ShouldContain("7秒");
    }

    [Fact]
    public void ToChineseDateTimeString_RemoveSecond_OmitsSecond()
    {
        var result = Sample.ToChineseDateTimeString(isRemoveSecond: true);
        result.ShouldNotContain("秒");
    }

    [Fact]
    public void ToChineseDateTimeString_NullableNull_ReturnsEmpty() =>
        ((DateTime?)null).ToChineseDateTimeString().ShouldBe(string.Empty);

    // ─────────────────────────────────────────────────────────────────
    // ToUniqueString
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ToUniqueString_WithoutMillisecond_Returns9Chars()
    {
        var s = Sample.ToUniqueString();
        s.ShouldNotBeNullOrEmpty();
        s.Length.ShouldBeLessThanOrEqualTo(12);
    }

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

    [Fact]
    public void In_Close_InsideRange_ReturnsTrue()
    {
        var dt = new DateTime(2024, 6, 15);
        dt.In(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)).ShouldBeTrue();
    }

    [Fact]
    public void In_Close_AtBoundary_ReturnsTrue()
    {
        var dt = new DateTime(2024, 1, 1);
        dt.In(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)).ShouldBeTrue();
    }

    [Fact]
    public void In_Open_AtBoundary_ReturnsFalse()
    {
        var dt = new DateTime(2024, 1, 1);
        dt.In(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), RangeMode.Open).ShouldBeFalse();
    }

    [Fact]
    public void In_Outside_ReturnsFalse()
    {
        var dt = new DateTime(2025, 1, 1);
        dt.In(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)).ShouldBeFalse();
    }

    // ─────────────────────────────────────────────────────────────────
    // SetTime
    // ─────────────────────────────────────────────────────────────────

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

    [Fact]
    public void CompareInterval_Seconds_ReturnsCorrectDifference()
    {
        var begin = new DateTime(2024, 1, 1, 0, 0, 0);
        var end = new DateTime(2024, 1, 1, 0, 0, 10);
        begin.CompareInterval(end, "s").ShouldBe(-10L);
    }

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

    [Fact]
    public void IsBetweenTime_InsideRange_ReturnsTrue()
    {
        var now = new DateTime(2024, 1, 1, 10, 0, 0);
        var begin = new DateTime(2024, 1, 1, 8, 0, 0);
        var end = new DateTime(2024, 1, 1, 18, 0, 0);
        now.IsBetweenTime(begin, end).ShouldBeTrue();
    }

    [Fact]
    public void IsBetweenTime_OutsideRange_ReturnsFalse()
    {
        var now = new DateTime(2024, 1, 1, 20, 0, 0);
        var begin = new DateTime(2024, 1, 1, 8, 0, 0);
        var end = new DateTime(2024, 1, 1, 18, 0, 0);
        now.IsBetweenTime(begin, end).ShouldBeFalse();
    }

    [Fact]
    public void IsBetweenDate_InsideRange_ReturnsTrue()
    {
        var now = new DateTime(2024, 6, 15);
        now.IsBetweenDate(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)).ShouldBeTrue();
    }

    [Fact]
    public void IsBetweenDate_OutsideRange_ReturnsFalse()
    {
        var now = new DateTime(2025, 1, 1);
        now.IsBetweenDate(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)).ShouldBeFalse();
    }

    // ─────────────────────────────────────────────────────────────────
    // IsValid
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void IsValid_NormalDate_ReturnsTrue() =>
        new DateTime(2024, 1, 1).IsValid().ShouldBeTrue();

    [Fact]
    public void IsValid_MinValue_ReturnsFalse() =>
        DateTime.MinValue.IsValid().ShouldBeFalse();

    [Fact]
    public void IsValid_MaxValue_ReturnsFalse() =>
        DateTime.MaxValue.IsValid().ShouldBeFalse();
}
