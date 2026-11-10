using System;
using Bing.Date.Clock;
using Bing.Date.TimeZones;

namespace Bing.Date;
/// <summary>
/// 接受显式时钟和业务时区的日期判断。
/// </summary>
public static partial class DateTimeExtensions
{
    /// <summary>
    /// 判断日期字段是否等于指定时钟在业务时区中的当天。
    /// </summary>
    /// <param name="date">只读取年月日，不按 Kind 转换时区。</param>
    /// <param name="clock">当前时间来源。</param>
    /// <param name="timeZone">业务时区。</param>
    /// <returns>日期字段与业务时区当天一致时为 true。</returns>
    public static bool IsToday(this DateTime date, IClock clock, TimeZoneInfo timeZone) =>
        date.Date == ClockDateTime.Today(clock, timeZone);
    /// <summary>
    /// 判断可空日期字段是否等于业务时区中的当天。
    /// </summary>
    /// <param name="date">只读取年月日，null 返回 false。</param>
    /// <param name="clock">当前时间来源。</param>
    /// <param name="timeZone">业务时区。</param>
    /// <returns>日期不为空且等于当天时为 true。</returns>
    public static bool IsToday(this DateTime? date, IClock clock, TimeZoneInfo timeZone)
    {
        var today = ClockDateTime.Today(clock, timeZone);
        return date.HasValue && date.Value.Date == today;
    }
    /// <summary>
    /// 根据指定时钟和业务时区计算年龄。
    /// </summary>
    /// <param name="birthday">生日日期，仅使用年月日。</param>
    /// <param name="clock">当前时间来源。</param>
    /// <param name="timeZone">业务时区。</param>
    /// <returns>业务时区当天的年龄。</returns>
    public static int ToCalculateAge(this DateTime birthday, IClock clock, TimeZoneInfo timeZone) =>
        birthday.ToCalculateAge(ClockDateTime.Today(clock, timeZone));
}
/// <summary>
/// 接受显式时钟和业务时区的时间点判断。
/// </summary>
public static partial class DateTimeOffsetExtensions
{
    /// <summary>
    /// 判断时间点在业务时区中的日期是否为当天。
    /// </summary>
    /// <param name="instant">待判断的时间点。</param>
    /// <param name="clock">当前时间来源。</param>
    /// <param name="timeZone">业务时区。</param>
    /// <returns>两个时间点处于同一业务自然日时为 true。</returns>
    public static bool IsToday(this DateTimeOffset instant, IClock clock, TimeZoneInfo timeZone)
    {
        var today = ClockDateTime.Today(clock, timeZone);
        return TimeZoneConversion.ToTimeZone(instant, timeZone).Date == today;
    }
    /// <summary>
    /// 判断可空时间点在业务时区中的日期是否为当天。
    /// </summary>
    /// <param name="instant">待判断的时间点；null 返回 false。</param>
    /// <param name="clock">当前时间来源。</param>
    /// <param name="timeZone">业务时区。</param>
    /// <returns>时间点不为空且处于当天时为 true。</returns>
    public static bool IsToday(this DateTimeOffset? instant, IClock clock, TimeZoneInfo timeZone)
    {
        var today = ClockDateTime.Today(clock, timeZone);
        return instant.HasValue && TimeZoneConversion.ToTimeZone(instant.Value, timeZone).Date == today;
    }
    /// <summary>
    /// 判断时间点是否严格晚于指定时钟。
    /// </summary>
    /// <param name="instant">待判断的时间点。</param>
    /// <param name="clock">当前时间来源。</param>
    /// <returns>时间点严格晚于时钟时为 true。</returns>
    public static bool IsInTheFuture(this DateTimeOffset instant, IClock clock) => instant > ClockDateTime.Instant(clock);
    /// <summary>
    /// 判断时间点是否严格早于指定时钟。
    /// </summary>
    /// <param name="instant">待判断的时间点。</param>
    /// <param name="clock">当前时间来源。</param>
    /// <returns>时间点严格早于时钟时为 true。</returns>
    public static bool IsInThePast(this DateTimeOffset instant, IClock clock) => instant < ClockDateTime.Instant(clock);
}
/// <summary>
/// 统一获取时钟时间及其业务时区日期。
/// </summary>
internal static class ClockDateTime
{
    /// <summary>
    /// 从指定时钟获取 UTC 时间点。
    /// </summary>
    /// <param name="clock">当前时间来源。</param>
    /// <returns>时钟表示的当前 UTC 时间点。</returns>
    internal static DateTimeOffset Instant(IClock clock) =>
        (clock ?? throw new ArgumentNullException(nameof(clock))).UtcNow.ToUniversalTime();
    /// <summary>
    /// 获取指定业务时区中当前日期的午夜。
    /// </summary>
    /// <param name="clock">当前时间来源。</param>
    /// <param name="timeZone">业务时区。</param>
    /// <returns>业务时区内当前自然日的午夜，Kind 为 Unspecified。</returns>
    internal static DateTime Today(IClock clock, TimeZoneInfo timeZone)
    {
        if (clock == null)
            throw new ArgumentNullException(nameof(clock));
        if (timeZone == null)
            throw new ArgumentNullException(nameof(timeZone));
        return TimeZoneConversion.ToTimeZone(Instant(clock), timeZone).Date;
    }
}
