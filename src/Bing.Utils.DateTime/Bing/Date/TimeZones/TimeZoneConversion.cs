using System;

namespace Bing.Date.TimeZones;

/// <summary>
/// 使用显式时区规则在时间点和本地时间之间转换。
/// </summary>
public static class TimeZoneConversion
{
    /// <summary>
    /// 将时间点转换为指定时区中的日期和偏移。
    /// </summary>
    /// <param name="instant">待转换的时间点。</param>
    /// <param name="destinationTimeZone">目标时区。</param>
    /// <returns>目标时区中的同一时间点。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="destinationTimeZone"/> 为 <see langword="null"/>。</exception>
    public static DateTimeOffset ToTimeZone(DateTimeOffset instant, TimeZoneInfo destinationTimeZone)
    {
        if (destinationTimeZone == null)
            throw new ArgumentNullException(nameof(destinationTimeZone));

        return TimeZoneInfo.ConvertTime(instant, destinationTimeZone);
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
