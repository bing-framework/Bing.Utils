using System;

namespace Bing.Date.TimeZones;
/// <summary>
/// 使用显式时区规则在时间点和本地时间之间转换。
/// </summary>
public static class TimeZoneConversion
{
    /// <summary>
    /// 将指定地区的无偏移钟面时间转换为 UTC 时间。
    /// </summary>
    /// <param name="localTime">Kind 必须为 Unspecified 的钟面时间。</param>
    /// <param name="sourceTimeZone">该钟面时间所属的时区。</param>
    /// <param name="ambiguousTimeResolution">夏令时重复时间的解析策略，默认拒绝。</param>
    /// <returns>保留原时间点及 tick 精度、Kind 为 Utc 的时间。</returns>
    /// <remarks>这是实际时区转换，与只修改 Kind 的历史 DateTime.ToUtc 扩展不同。</remarks>
    /// <exception cref="ArgumentNullException">来源时区为空。</exception>
    /// <exception cref="ArgumentException">输入 Kind、缺失时间或未消歧的重复时间无效。</exception>
    /// <exception cref="ArgumentOutOfRangeException">策略非法或 UTC 结果越界。</exception>
    public static DateTime ToUtc(DateTime localTime, TimeZoneInfo sourceTimeZone,
        AmbiguousTimeResolution ambiguousTimeResolution = AmbiguousTimeResolution.Reject) =>
        ResolveLocalTime(localTime, sourceTimeZone, ambiguousTimeResolution).UtcDateTime;
    /// <summary>
    /// 将 UTC 语义区间投影为指定时区的两个显示端点。
    /// </summary>
    /// <param name="range">输入区间；自定义端点按 DateTimeRange 规则规范化和排序。</param>
    /// <param name="destinationTimeZone">显示端点的目标时区。</param>
    /// <returns>保留同一时间点的带偏移起止端点；跨夏令时的偏移可以不同。</returns>
    /// <remarks>不修改原区间；DateTimeRange 输入不创建额外区间对象。</remarks>
    /// <exception cref="ArgumentNullException">区间或目标时区为空。</exception>
    /// <exception cref="ArgumentOutOfRangeException">端点无法在目标时区表示。</exception>
    public static (DateTimeOffset StartTime, DateTimeOffset EndTime) ToTimeZone(
        IDateTimeRange range, TimeZoneInfo destinationTimeZone)
    {
        if (range == null)
            throw new ArgumentNullException(nameof(range));
        if (destinationTimeZone == null)
            throw new ArgumentNullException(nameof(destinationTimeZone));
        var normalized = range as DateTimeRange ?? new DateTimeRange(range);
        return (ToTimeZone(new DateTimeOffset(normalized.StartTime), destinationTimeZone),
            ToTimeZone(new DateTimeOffset(normalized.EndTime), destinationTimeZone));
    }
    /// <summary>
    /// 将时间点转换为指定时区中的日期和偏移。
    /// </summary>
    /// <param name="instant">待转换的时间点。</param>
    /// <param name="destinationTimeZone">目标时区。</param>
    /// <returns>目标时区中的同一时间点及当地偏移。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="destinationTimeZone"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="ArgumentOutOfRangeException">目标时区中的本地日期超出 <see cref="DateTimeOffset"/> 的可表示范围；异常参数名为 <c>instant</c>。</exception>
    public static DateTimeOffset ToTimeZone(DateTimeOffset instant, TimeZoneInfo destinationTimeZone)
    {
        if (destinationTimeZone == null)
            throw new ArgumentNullException(nameof(destinationTimeZone));

        var offset = destinationTimeZone.GetUtcOffset(instant);
        var localTicks = instant.UtcDateTime.Ticks + offset.Ticks;
        if (localTicks < DateTime.MinValue.Ticks || localTicks > DateTime.MaxValue.Ticks)
            throw new ArgumentOutOfRangeException(nameof(instant), instant, "目标时区中的本地日期超出可表示范围。");

        return new DateTimeOffset(new DateTime(localTicks, DateTimeKind.Unspecified), offset);
    }
    /// <summary>
    /// 按指定时区规则将未指定种类的本地时间解析为明确的时间点。
    /// </summary>
    /// <param name="localTime">待解析的本地日期时间，其 <see cref="DateTime.Kind"/> 必须为 <see cref="DateTimeKind.Unspecified"/>。</param>
    /// <param name="sourceTimeZone">该本地时间所属时区。</param>
    /// <param name="ambiguousTimeResolution">遇到夏令时歧义时采用的策略，默认拒绝。</param>
    /// <returns>按指定时区规则解析出的日期时间及偏移。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sourceTimeZone"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="ArgumentException">日期时间的种类不为 <see cref="DateTimeKind.Unspecified"/>，或本地时间因夏令时跳时而无效，或歧义时间采用默认拒绝策略。</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="ambiguousTimeResolution"/> 不是定义的枚举值。</exception>
    public static DateTimeOffset ResolveLocalTime(
        DateTime localTime,
        TimeZoneInfo sourceTimeZone,
        AmbiguousTimeResolution ambiguousTimeResolution = AmbiguousTimeResolution.Reject)
    {
        if (sourceTimeZone == null)
            throw new ArgumentNullException(nameof(sourceTimeZone));
        if (localTime.Kind != DateTimeKind.Unspecified)
            throw new ArgumentException("本地时间的 Kind 必须为 Unspecified。", nameof(localTime));
        if ((int)ambiguousTimeResolution < (int)AmbiguousTimeResolution.Reject ||
            (int)ambiguousTimeResolution > (int)AmbiguousTimeResolution.Later)
            throw new ArgumentOutOfRangeException(nameof(ambiguousTimeResolution), ambiguousTimeResolution, "不支持的歧义时间解析策略。");
        if (sourceTimeZone.IsInvalidTime(localTime))
            throw new ArgumentException("该本地时间因时区夏令时跳时而无效。", nameof(localTime));

        if (!sourceTimeZone.IsAmbiguousTime(localTime))
            return new DateTimeOffset(localTime, sourceTimeZone.GetUtcOffset(localTime));

        if (ambiguousTimeResolution == AmbiguousTimeResolution.Reject)
            throw new ArgumentException("该本地时间对应多个时间点，必须显式指定歧义解析策略。", nameof(localTime));

        var offsets = sourceTimeZone.GetAmbiguousTimeOffsets(localTime);
        if (offsets.Length < 2)
            throw new InvalidOperationException("时区报告了歧义时间，但未提供足够的候选偏移。");

        var first = new DateTimeOffset(localTime, offsets[0]);
        var second = new DateTimeOffset(localTime, offsets[1]);
        var earlier = first <= second ? first : second;
        var later = first <= second ? second : first;

        return ambiguousTimeResolution == AmbiguousTimeResolution.Earlier ? earlier : later;
    }
}
