using Bing.Date;

namespace Bing.Utils.Tests.Bing.Date;

/// <summary>
/// <see cref="DateTimeOutputExtensions"/> / <see cref="DateTimeOutputHelper"/> 单元测试
/// </summary>
public class DateTimeOutputExtensionsTests
{
    private static readonly DateTime Sample = new DateTime(2024, 3, 15, 9, 5, 7, 123);

    // ─────────────────────────────────────────────────────────────────
    // ToString(DateTimeOutputStyles)
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ToString_DateTime_FormatsCorrectly()
    {
        Sample.ToString(DateTimeOutputStyles.DateTime).ShouldBe("2024-03-15 09:05:07");
    }

    [Fact]
    public void ToString_DateTime_RemoveSecond_FormatsCorrectly()
    {
        Sample.ToString(DateTimeOutputStyles.DateTime, isRemoveSecond: true).ShouldBe("2024-03-15 09:05");
    }

    [Fact]
    public void ToString_Date_FormatsCorrectly()
    {
        Sample.ToString(DateTimeOutputStyles.Date).ShouldBe("2024-03-15");
    }

    [Fact]
    public void ToString_Time_FormatsCorrectly()
    {
        Sample.ToString(DateTimeOutputStyles.Time).ShouldBe("09:05:07");
    }

    [Fact]
    public void ToString_Time_RemoveSecond_FormatsCorrectly()
    {
        Sample.ToString(DateTimeOutputStyles.Time, isRemoveSecond: true).ShouldBe("09:05");
    }

    [Fact]
    public void ToString_Millisecond_FormatsCorrectly()
    {
        Sample.ToString(DateTimeOutputStyles.Millisecond).ShouldBe("2024-03-15 09:05:07.123");
    }

    [Fact]
    public void ToString_LongDate_IsNotEmpty()
    {
        Sample.ToString(DateTimeOutputStyles.LongDate).ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void ToString_LongTime_IsNotEmpty()
    {
        Sample.ToString(DateTimeOutputStyles.LongTime).ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void ToString_ShortDate_IsNotEmpty()
    {
        Sample.ToString(DateTimeOutputStyles.ShortDate).ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void ToString_ShortTime_IsNotEmpty()
    {
        Sample.ToString(DateTimeOutputStyles.ShortTime).ShouldNotBeNullOrEmpty();
    }

    // ─────────────────────────────────────────────────────────────────
    // ToString(DateTime?)
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ToString_NullableDateTime_NullValue_ReturnsEmpty()
    {
        DateTime? dt = null;
        dt.ToString(DateTimeOutputStyles.Date).ShouldBe(string.Empty);
    }

    [Fact]
    public void ToString_NullableDateTime_WithValue_FormatsCorrectly()
    {
        DateTime? dt = Sample;
        dt.ToString(DateTimeOutputStyles.Date).ShouldBe("2024-03-15");
    }

    // ─────────────────────────────────────────────────────────────────
    // ToDateTimeOffset
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ToDateTimeOffset_PreservesDate()
    {
        var offset = Sample.ToDateTimeOffset();
        offset.Date.ShouldBe(Sample.Date);
    }

    [Fact]
    public void ToDateTimeOffset_PreservesTimeOfDay()
    {
        var offset = Sample.ToDateTimeOffset();
        offset.TimeOfDay.ShouldBe(Sample.TimeOfDay);
    }

    [Fact]
    public void ToDateTimeOffset_WithExplicitLocalTimeZone_SameResult()
    {
        var local = TimeZoneInfo.Local;
        var a = Sample.ToDateTimeOffset();
        var b = Sample.ToDateTimeOffset(local);
        a.ShouldBe(b);
    }

    // ─────────────────────────────────────────────────────────────────
    // ToLocalDateTime
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ToLocalDateTime_RoundTrip_PreservesDateTime()
    {
        var dt = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Unspecified);
        var offset = dt.ToDateTimeOffset(TimeZoneInfo.Local);
        var back = offset.ToLocalDateTime(TimeZoneInfo.Local);
        back.Date.ShouldBe(dt.Date);
        back.TimeOfDay.ShouldBe(dt.TimeOfDay);
    }

    [Fact]
    public void ToLocalDateTime_NoTimeZoneArg_UsesLocalZone()
    {
        var dt = new DateTime(2024, 1, 1, 8, 0, 0, DateTimeKind.Unspecified);
        var offset = dt.ToDateTimeOffset(TimeZoneInfo.Local);
        var back = offset.ToLocalDateTime();
        back.Date.ShouldBe(dt.Date);
    }
}
