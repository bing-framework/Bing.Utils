using System;

namespace Bing.Date.TimeZones;

/// <summary>
/// 显式时区转换和夏令时歧义处理测试。
/// </summary>
public class TimeZoneConversionTest
{
    /// <summary>
    /// 验证带偏移时间点转换到目标时区时可以跨越日历日期。
    /// </summary>
    [Fact]
    public void ToTimeZone_ConvertsInstantAcrossDateBoundary()
    {
        var utc = new DateTimeOffset(2024, 1, 1, 2, 0, 0, TimeSpan.Zero);
        var pacific = FindTimeZone("America/Los_Angeles", "Pacific Standard Time");

        var result = TimeZoneConversion.ToTimeZone(utc, pacific);

        result.ShouldBe(new DateTimeOffset(2023, 12, 31, 18, 0, 0, TimeSpan.FromHours(-8)));
    }

    /// <summary>
    /// 验证夏令时开始时跳过的本地时间会被拒绝。
    /// </summary>
    [Fact]
    public void ResolveLocalTime_RejectsDaylightSavingGap()
    {
        var eastern = FindTimeZone("America/New_York", "Eastern Standard Time");
        var invalidLocalTime = new DateTime(2024, 3, 10, 2, 30, 0, DateTimeKind.Unspecified);

        Assert.Throws<ArgumentException>(() => TimeZoneConversion.ResolveLocalTime(invalidLocalTime, eastern));
    }

    /// <summary>
    /// 验证夏令时结束时重叠的本地时间默认拒绝隐式选择。
    /// </summary>
    [Fact]
    public void ResolveLocalTime_RejectsAmbiguousTimeByDefault()
    {
        var eastern = FindTimeZone("America/New_York", "Eastern Standard Time");
        var ambiguousLocalTime = new DateTime(2024, 11, 3, 1, 30, 0, DateTimeKind.Unspecified);

        Assert.Throws<ArgumentException>(() => TimeZoneConversion.ResolveLocalTime(ambiguousLocalTime, eastern));
    }

    /// <summary>
    /// 验证 Earlier 和 Later 按候选时间点的 UTC 顺序选择，而非偏移列表顺序。
    /// </summary>
    [Fact]
    public void ResolveLocalTime_ChoosesAmbiguousCandidatesByUtcInstant()
    {
        var eastern = FindTimeZone("America/New_York", "Eastern Standard Time");
        var ambiguousLocalTime = new DateTime(2024, 11, 3, 1, 30, 0, DateTimeKind.Unspecified);

        var earlier = TimeZoneConversion.ResolveLocalTime(ambiguousLocalTime, eastern, AmbiguousTimeResolution.Earlier);
        var later = TimeZoneConversion.ResolveLocalTime(ambiguousLocalTime, eastern, AmbiguousTimeResolution.Later);

        earlier.UtcDateTime.ShouldBe(new DateTime(2024, 11, 3, 5, 30, 0, DateTimeKind.Utc));
        later.UtcDateTime.ShouldBe(new DateTime(2024, 11, 3, 6, 30, 0, DateTimeKind.Utc));
        (earlier < later).ShouldBeTrue();
    }

    /// <summary>
    /// 验证解析本地时间只接受 Unspecified 种类，避免隐式套用机器本地时区。
    /// </summary>
    /// <param name="kind">待拒绝的日期时间种类。</param>
    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Utc)]
    public void ResolveLocalTime_RejectsImplicitDateTimeKind(DateTimeKind kind)
    {
        var eastern = FindTimeZone("America/New_York", "Eastern Standard Time");
        var localTime = new DateTime(2024, 1, 15, 12, 0, 0, kind);

        Assert.Throws<ArgumentException>(() => TimeZoneConversion.ResolveLocalTime(localTime, eastern));
    }

    /// <summary>
    /// 验证普通本地时间使用指定时区的有效偏移。
    /// </summary>
    [Fact]
    public void ResolveLocalTime_UsesExplicitZoneForUnambiguousTime()
    {
        var eastern = FindTimeZone("America/New_York", "Eastern Standard Time");
        var localTime = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Unspecified);

        var result = TimeZoneConversion.ResolveLocalTime(localTime, eastern);

        result.ShouldBe(new DateTimeOffset(2024, 1, 15, 12, 0, 0, TimeSpan.FromHours(-5)));
    }

    /// <summary>
    /// 在系统安装的时区数据库中查找时区，并兼容 Windows 与 IANA 标识。
    /// </summary>
    /// <param name="ianaId">IANA 时区标识。</param>
    /// <param name="windowsId">Windows 时区标识。</param>
    /// <returns>查找到的系统时区。</returns>
    private static TimeZoneInfo FindTimeZone(string ianaId, string windowsId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(ianaId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById(windowsId);
        }
    }
}
