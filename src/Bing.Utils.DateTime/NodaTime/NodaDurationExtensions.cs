using System;
using Bing.Date;

namespace NodaTime;

/// <summary>
/// Bing <see cref="Duration"/> 扩展
/// </summary>
public static class NodaDurationExtensions
{
    /// <summary>
    /// 将 <see cref="TimeSpan"/> 转换为 <see cref="Duration"/>
    /// </summary>
    /// <param name="ts">时间间隔</param>
    public static Duration AsDuration(this TimeSpan ts) => Duration.FromTimeSpan(ts);

    /// <summary>
    /// 将 <see cref="DateTimeSpan"/> 转换为 <see cref="Duration"/>
    /// </summary>
    /// <param name="dateTimeSpan">日期时间间隔</param>
    public static Duration AsDuration(this DateTimeSpan dateTimeSpan) => Duration.FromTimeSpan(dateTimeSpan);

    /// <summary>
    /// 将毫秒数转换为 <see cref="Duration"/>
    /// </summary>
    /// <param name="milliseconds">毫秒数</param>
    public static Duration AsDurationOfMilliseconds(this long milliseconds) => Duration.FromMilliseconds(milliseconds);

    /// <summary>
    /// 将毫秒数转换为 <see cref="Duration"/>
    /// </summary>
    /// <param name="milliseconds">毫秒数</param>
    public static Duration AsDurationOfMilliseconds(this double milliseconds) => Duration.FromMilliseconds(milliseconds);

    /// <summary>
    /// 将秒数转换为 <see cref="Duration"/>
    /// </summary>
    /// <param name="seconds">秒数</param>
    public static Duration AsDurationOfSeconds(this long seconds) => Duration.FromSeconds(seconds);

    /// <summary>
    /// 将秒数转换为 <see cref="Duration"/>
    /// </summary>
    /// <param name="seconds">秒数</param>
    public static Duration AsDurationOfSeconds(this double seconds) => Duration.FromSeconds(seconds);

    /// <summary>
    /// 将分钟数转换为 <see cref="Duration"/>
    /// </summary>
    /// <param name="minutes">分钟数</param>
    public static Duration AsDurationOfMinutes(this long minutes) => Duration.FromMinutes(minutes);

    /// <summary>
    /// 将分钟数转换为 <see cref="Duration"/>
    /// </summary>
    /// <param name="minutes">分钟数</param>
    public static Duration AsDurationOfMinutes(this double minutes) => Duration.FromMinutes(minutes);

    /// <summary>
    /// 将小时数转换为 <see cref="Duration"/>
    /// </summary>
    /// <param name="hours">小时数</param>
    public static Duration AsDurationOfHours(this long hours) => Duration.FromHours(hours);

    /// <summary>
    /// 将小时数转换为 <see cref="Duration"/>
    /// </summary>
    /// <param name="hours">小时数</param>
    public static Duration AsDurationOfHours(this double hours) => Duration.FromHours(hours);

    /// <summary>
    /// 将天数转换为 <see cref="Duration"/>
    /// </summary>
    /// <param name="days">天数</param>
    public static Duration AsDurationOfDays(this long days) => Duration.FromDays(days);

    /// <summary>
    /// 将天数转换为 <see cref="Duration"/>
    /// </summary>
    /// <param name="days">天数</param>
    public static Duration AsDurationOfDays(this double days) => Duration.FromDays(days);

    /// <summary>
    /// 将周数转换为 <see cref="Duration"/>
    /// </summary>
    /// <param name="weeks">周数</param>
    public static Duration AsDurationOfWeeks(this int weeks) => Duration.FromDays(weeks * 7);
}