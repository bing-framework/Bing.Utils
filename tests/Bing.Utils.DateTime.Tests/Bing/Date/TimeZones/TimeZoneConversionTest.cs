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
        result.DateTime.ShouldBe(new DateTime(2023, 12, 31, 18, 0, 0, DateTimeKind.Unspecified));
        result.Offset.ShouldBe(TimeSpan.FromHours(-8));
        result.UtcTicks.ShouldBe(utc.UtcTicks);
    }

    /// <summary>
    /// 验证目标本地时间超出正负十四小时偏移的可表示范围时拒绝转换。
    /// </summary>
    /// <param name="offsetHours">自建目标时区的固定偏移小时数。</param>
    /// <param name="upperBoundary">为 true 时使用上界，否则使用下界。</param>
    [Theory]
    [InlineData(14, true)]
    [InlineData(-14, false)]
    public void ToTimeZone_RejectsLocalRepresentationOutsideDateTimeOffsetRange(int offsetHours, bool upperBoundary)
    {
        var zone = CreateFixedTimeZone(offsetHours);
        var instant = upperBoundary ? DateTimeOffset.MaxValue : DateTimeOffset.MinValue;

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => TimeZoneConversion.ToTimeZone(instant, zone));

        exception.ParamName.ShouldBe("instant");
    }

    /// <summary>
    /// 验证目标当地日期刚好位于 DateTimeOffset 边界时保留偏移和原始时间点。
    /// </summary>
    /// <param name="offsetHours">自建目标时区的固定偏移小时数。</param>
    /// <param name="upperBoundary">为 true 时检查上界，否则检查下界。</param>
    [Theory]
    [InlineData(14, true)]
    [InlineData(-14, false)]
    public void ToTimeZone_ConvertsAtRepresentableLocalBoundary(int offsetHours, bool upperBoundary)
    {
        var zone = CreateFixedTimeZone(offsetHours);
        var localBoundary = upperBoundary ? DateTime.MaxValue.AddHours(-offsetHours) : DateTime.MinValue.AddHours(-offsetHours);
        var instant = new DateTimeOffset(DateTime.SpecifyKind(localBoundary, DateTimeKind.Unspecified), TimeSpan.Zero);

        var result = TimeZoneConversion.ToTimeZone(instant, zone);
        var expectedLocal = upperBoundary ? DateTime.MaxValue : DateTime.MinValue;

        result.DateTime.Ticks.ShouldBe(expectedLocal.Ticks);
        result.Offset.ShouldBe(TimeSpan.FromHours(offsetHours));
        result.UtcTicks.ShouldBe(instant.UtcTicks);
    }

    /// <summary>
    /// 验证目标时区为空时拒绝转换。
    /// </summary>
    [Fact]
    public void ToTimeZone_RejectsNullDestinationTimeZone()
    {
        var instant = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var exception = Assert.Throws<ArgumentNullException>(() => TimeZoneConversion.ToTimeZone(instant, null));

        exception.ParamName.ShouldBe("destinationTimeZone");
    }

    /// <summary>
    /// 验证夏令时回拨时按输入时间点选择目标偏移。
    /// </summary>
    /// <param name="utcHour">输入时间点的 UTC 小时。</param>
    /// <param name="expectedOffset">目标时区的预期偏移小时数。</param>
    [Theory]
    [InlineData(5, -4)]
    [InlineData(6, -5)]
    public void ToTimeZone_AmbiguousDestinationTime_UsesInstantAndDestinationOffset(int utcHour, int expectedOffset)
    {
        var utc = new DateTimeOffset(2024, 11, 3, utcHour, 30, 0, TimeSpan.Zero);
        var input = utc.ToOffset(TimeSpan.FromMinutes(330));

        var result = TimeZoneConversion.ToTimeZone(input, CreateDaylightSavingTimeZone());

        result.UtcTicks.ShouldBe(input.UtcTicks);
        result.DateTime.ShouldBe(new DateTime(2024, 11, 3, 1, 30, 0, DateTimeKind.Unspecified));
        result.Offset.ShouldBe(TimeSpan.FromHours(expectedOffset));
    }

    /// <summary>
    /// 验证源时区为空时拒绝解析。
    /// </summary>
    [Fact]
    public void ResolveLocalTime_RejectsNullSourceTimeZone()
    {
        var localTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Unspecified);

        var exception = Assert.Throws<ArgumentNullException>(() => TimeZoneConversion.ResolveLocalTime(localTime, null));

        exception.ParamName.ShouldBe("sourceTimeZone");
    }

    /// <summary>
    /// 验证未定义的歧义时间策略被拒绝。
    /// </summary>
    /// <param name="resolution">待验证的歧义时间策略。</param>
    [Theory]
    [InlineData((AmbiguousTimeResolution)(-1))]
    [InlineData((AmbiguousTimeResolution)3)]
    [InlineData((AmbiguousTimeResolution)int.MaxValue)]
    public void ResolveLocalTime_RejectsUndefinedAmbiguousTimeResolution(AmbiguousTimeResolution resolution)
    {
        var localTime = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Unspecified);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            TimeZoneConversion.ResolveLocalTime(localTime, CreateDaylightSavingTimeZone(), resolution));

        exception.ParamName.ShouldBe("ambiguousTimeResolution");
    }

    /// <summary>
    /// 验证夏令时开始时跳过的本地时间会被拒绝。
    /// </summary>
    [Fact]
    public void ResolveLocalTime_RejectsDaylightSavingGap()
    {
        var eastern = CreateDaylightSavingTimeZone();
        var invalidLocalTime = new DateTime(2024, 3, 10, 2, 30, 0, DateTimeKind.Unspecified);

        Assert.Throws<ArgumentException>(() => TimeZoneConversion.ResolveLocalTime(invalidLocalTime, eastern));
    }

    /// <summary>
    /// 验证夏令时结束时重叠的本地时间默认拒绝隐式选择。
    /// </summary>
    [Fact]
    public void ResolveLocalTime_RejectsAmbiguousTimeByDefault()
    {
        var eastern = CreateDaylightSavingTimeZone();
        var ambiguousLocalTime = new DateTime(2024, 11, 3, 1, 30, 0, DateTimeKind.Unspecified);

        Assert.Throws<ArgumentException>(() => TimeZoneConversion.ResolveLocalTime(ambiguousLocalTime, eastern));
    }

    /// <summary>
    /// 验证 Earlier 和 Later 按候选时间点的 UTC 顺序选择，而非偏移列表顺序。
    /// </summary>
    [Fact]
    public void ResolveLocalTime_ChoosesAmbiguousCandidatesByUtcInstant()
    {
        var eastern = CreateDaylightSavingTimeZone();
        var ambiguousLocalTime = new DateTime(2024, 11, 3, 1, 30, 0, DateTimeKind.Unspecified);

        var earlier = TimeZoneConversion.ResolveLocalTime(ambiguousLocalTime, eastern, AmbiguousTimeResolution.Earlier);
        var later = TimeZoneConversion.ResolveLocalTime(ambiguousLocalTime, eastern, AmbiguousTimeResolution.Later);

        earlier.UtcDateTime.ShouldBe(new DateTime(2024, 11, 3, 5, 30, 0, DateTimeKind.Utc));
        later.UtcDateTime.ShouldBe(new DateTime(2024, 11, 3, 6, 30, 0, DateTimeKind.Utc));
        earlier.Offset.ShouldBe(TimeSpan.FromHours(-4));
        later.Offset.ShouldBe(TimeSpan.FromHours(-5));
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
        var eastern = CreateDaylightSavingTimeZone();
        var localTime = new DateTime(2024, 1, 15, 12, 0, 0, kind);

        Assert.Throws<ArgumentException>(() => TimeZoneConversion.ResolveLocalTime(localTime, eastern));
    }

    /// <summary>
    /// 验证普通本地时间使用指定时区的有效偏移。
    /// </summary>
    [Fact]
    public void ResolveLocalTime_UsesExplicitZoneForUnambiguousTime()
    {
        var eastern = CreateDaylightSavingTimeZone();
        var localTime = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Unspecified);

        var result = TimeZoneConversion.ResolveLocalTime(localTime, eastern);

        result.ShouldBe(new DateTimeOffset(2024, 1, 15, 12, 0, 0, TimeSpan.FromHours(-5)));
        result.DateTime.ShouldBe(localTime);
        result.Offset.ShouldBe(TimeSpan.FromHours(-5));
        result.UtcTicks.ShouldBe(new DateTime(2024, 1, 15, 17, 0, 0, DateTimeKind.Utc).Ticks);
    }

    /// <summary>
    /// 创建指定偏移的固定时区。
    /// </summary>
    /// <param name="offsetHours">固定 UTC 偏移小时数。</param>
    /// <returns>使用指定偏移的自建时区。</returns>
    private static TimeZoneInfo CreateFixedTimeZone(int offsetHours) =>
        TimeZoneInfo.CreateCustomTimeZone($"Fixed/{offsetHours:+0;-0}", TimeSpan.FromHours(offsetHours), "Fixed", "Fixed");

    /// <summary>
    /// 创建包含固定夏令时规则的测试时区。
    /// </summary>
    /// <returns>用于验证跳时和重复时间的自建时区。</returns>
    private static TimeZoneInfo CreateDaylightSavingTimeZone()
    {
        var start = TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 2, 0, 0), 3, 10);
        var end = TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 2, 0, 0), 11, 3);
        var rule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 31),
            TimeSpan.FromHours(1),
            start,
            end);

        return TimeZoneInfo.CreateCustomTimeZone(
            "Custom/Eastern-2024",
            TimeSpan.FromHours(-5),
            "Custom Eastern",
            "Standard",
            "Daylight",
            new[] { rule });
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
