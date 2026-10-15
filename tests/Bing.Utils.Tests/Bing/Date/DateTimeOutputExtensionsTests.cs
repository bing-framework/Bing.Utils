using Bing.Date;

namespace Bing.Utils.Tests.Bing.Date;

/// <summary>
/// <see cref="DateTimeOutputExtensions"/> / <see cref="DateTimeOutputHelper"/> 单元测试
/// </summary>
public class DateTimeOutputExtensionsTests
{
    /// <summary>
    /// 日期时间输出测试使用的固定样例。
    /// </summary>
    private static readonly DateTime Sample = new DateTime(2024, 3, 15, 9, 5, 7, 123);

    // ─────────────────────────────────────────────────────────────────
    // ToString(DateTimeOutputStyles)
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证完整日期时间输出包含秒。
    /// </summary>
    [Fact]
    public void ToString_DateTime_FormatsCorrectly()
    {
        Sample.ToString(DateTimeOutputStyles.DateTime).ShouldBe("2024-03-15 09:05:07");
    }

    /// <summary>
    /// 验证完整日期时间可配置省略秒。
    /// </summary>
    [Fact]
    public void ToString_DateTime_RemoveSecond_FormatsCorrectly()
    {
        Sample.ToString(DateTimeOutputStyles.DateTime, isRemoveSecond: true).ShouldBe("2024-03-15 09:05");
    }

    /// <summary>
    /// 验证日期输出包含年月日。
    /// </summary>
    [Fact]
    public void ToString_Date_FormatsCorrectly()
    {
        Sample.ToString(DateTimeOutputStyles.Date).ShouldBe("2024-03-15");
    }

    /// <summary>
    /// 验证时间输出包含时分秒。
    /// </summary>
    [Fact]
    public void ToString_Time_FormatsCorrectly()
    {
        Sample.ToString(DateTimeOutputStyles.Time).ShouldBe("09:05:07");
    }

    /// <summary>
    /// 验证时间输出可配置省略秒。
    /// </summary>
    [Fact]
    public void ToString_Time_RemoveSecond_FormatsCorrectly()
    {
        Sample.ToString(DateTimeOutputStyles.Time, isRemoveSecond: true).ShouldBe("09:05");
    }

    /// <summary>
    /// 验证毫秒输出包含三位毫秒。
    /// </summary>
    [Fact]
    public void ToString_Millisecond_FormatsCorrectly()
    {
        Sample.ToString(DateTimeOutputStyles.Millisecond).ShouldBe("2024-03-15 09:05:07.123");
    }

    /// <summary>
    /// 验证长日期输出非空。
    /// </summary>
    [Fact]
    public void ToString_LongDate_IsNotEmpty()
    {
        Sample.ToString(DateTimeOutputStyles.LongDate).ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// 验证长时间输出非空。
    /// </summary>
    [Fact]
    public void ToString_LongTime_IsNotEmpty()
    {
        Sample.ToString(DateTimeOutputStyles.LongTime).ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// 验证短日期输出非空。
    /// </summary>
    [Fact]
    public void ToString_ShortDate_IsNotEmpty()
    {
        Sample.ToString(DateTimeOutputStyles.ShortDate).ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// 验证短时间输出非空。
    /// </summary>
    [Fact]
    public void ToString_ShortTime_IsNotEmpty()
    {
        Sample.ToString(DateTimeOutputStyles.ShortTime).ShouldNotBeNullOrEmpty();
    }

    // ─────────────────────────────────────────────────────────────────
    // ToString(DateTime?)
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证可空日期为空时输出空文本。
    /// </summary>
    [Fact]
    public void ToString_NullableDateTime_NullValue_ReturnsEmpty()
    {
        DateTime? dt = null;
        dt.ToString(DateTimeOutputStyles.Date).ShouldBe(string.Empty);
    }

    /// <summary>
    /// 验证可空日期有值时按指定样式输出。
    /// </summary>
    [Fact]
    public void ToString_NullableDateTime_WithValue_FormatsCorrectly()
    {
        DateTime? dt = Sample;
        dt.ToString(DateTimeOutputStyles.Date).ShouldBe("2024-03-15");
    }

    // ─────────────────────────────────────────────────────────────────
    // ToDateTimeOffset
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证转换为带偏移日期时间时保留日期。
    /// </summary>
    [Fact]
    public void ToDateTimeOffset_PreservesDate()
    {
        var offset = Sample.ToDateTimeOffset();
        offset.Date.ShouldBe(Sample.Date);
    }

    /// <summary>
    /// 验证转换为带偏移日期时间时保留日内时间。
    /// </summary>
    [Fact]
    public void ToDateTimeOffset_PreservesTimeOfDay()
    {
        var offset = Sample.ToDateTimeOffset();
        offset.TimeOfDay.ShouldBe(Sample.TimeOfDay);
    }

    /// <summary>
    /// 验证显式指定本地时区与默认转换结果一致。
    /// </summary>
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

    /// <summary>
    /// 验证按本地时区往返转换保留日期和时间。
    /// </summary>
    [Fact]
    public void ToLocalDateTime_RoundTrip_PreservesDateTime()
    {
        var dt = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Unspecified);
        var offset = dt.ToDateTimeOffset(TimeZoneInfo.Local);
        var back = offset.ToLocalDateTime(TimeZoneInfo.Local);
        back.Date.ShouldBe(dt.Date);
        back.TimeOfDay.ShouldBe(dt.TimeOfDay);
    }

    /// <summary>
    /// 验证未指定时区的本地转换保留所选日期。
    /// </summary>
    [Fact]
    public void ToLocalDateTime_NoTimeZoneArg_UsesLocalZone()
    {
        var dt = new DateTime(2024, 1, 1, 8, 0, 0, DateTimeKind.Unspecified);
        var offset = dt.ToDateTimeOffset(TimeZoneInfo.Local);
        var back = offset.ToLocalDateTime();
        back.Date.ShouldBe(dt.Date);
    }
}
