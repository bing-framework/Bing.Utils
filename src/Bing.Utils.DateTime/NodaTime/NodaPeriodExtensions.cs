using System;
using Bing.Date;

namespace NodaTime;

/// <summary>
/// Bing <see cref="Period"/> 扩展
/// </summary>
public static class NodaPeriodExtensions
{
    /// <summary>
    /// 将 <see cref="TimeSpan"/> 转换为 <see cref="Period"/>
    /// </summary>
    /// <param name="ts">时间间隔</param>
    public static Period AsPeriod(this TimeSpan ts) => Period.FromTicks(ts.Ticks);

    /// <summary>
    /// 将 <see cref="DateTimeSpan"/> 转换为 <see cref="Period"/>
    /// </summary>
    /// <param name="ts">日期时间间隔</param>
    public static Period AsPeriod(this DateTimeSpan ts) => ts;

    /// <summary>
    /// 将 <see cref="Duration"/> 转换为 <see cref="Period"/>
    /// </summary>
    /// <param name="d">持续时间</param>
    public static Period AsPeriod(this Duration d) => Period.FromTicks(d.ToTimeSpan().Ticks);

    /// <summary>
    /// 将 <see cref="Period"/> 转换为 <see cref="TimeSpan"/>
    /// </summary>
    /// <param name="p">期间</param>
    public static TimeSpan AsTimeSpan(this Period p) => TimeSpan.FromTicks(p.Ticks);

    /// <summary>
    /// 将 <see cref="Period"/> 转换为 <see cref="DateTimeSpan"/>
    /// </summary>
    /// <param name="p">期间</param>
    public static DateTimeSpan AsDateTimeSpan(this Period p) => p;

    /// <summary>
    /// 将 <see cref="Period"/> 转换为 <see cref="Duration"/>
    /// </summary>
    /// <param name="p">期间</param>
    public static Duration AsDuration(this Period p) => Duration.FromTicks(p.Ticks);

    /// <summary>
    /// 将纳秒数转换为 <see cref="Period"/>
    /// </summary>
    /// <param name="nanoseconds">纳秒数</param>
    public static Period AsPeriodOfNanoseconds(this long nanoseconds) => Period.FromNanoseconds(nanoseconds);

    /// <summary>
    /// 将毫秒数转换为 <see cref="Period"/>
    /// </summary>
    /// <param name="milliseconds">毫秒数</param>
    public static Period AsPeriodOfMilliseconds(this long milliseconds) => Period.FromMilliseconds(milliseconds);

    /// <summary>
    /// 将秒数转换为 <see cref="Period"/>
    /// </summary>
    /// <param name="seconds">秒数</param>
    public static Period AsPeriodOfSeconds(this long seconds) => Period.FromSeconds(seconds);

    /// <summary>
    /// 将分钟数转换为 <see cref="Period"/>
    /// </summary>
    /// <param name="minutes">分钟数</param>
    public static Period AsPeriodOfMinutes(this long minutes) => Period.FromMinutes(minutes);

    /// <summary>
    /// 将小时数转换为 <see cref="Period"/>
    /// </summary>
    /// <param name="hours">小时数</param>
    public static Period AsPeriodOfHours(this long hours) => Period.FromHours(hours);

    /// <summary>
    /// 将天数转换为 <see cref="Period"/>
    /// </summary>
    /// <param name="days">天数</param>
    public static Period AsPeriodOfDays(this int days) => Period.FromDays(days);

    /// <summary>
    /// 将月数转换为 <see cref="Period"/>
    /// </summary>
    /// <param name="months">月数</param>
    public static Period AsPeriodOfMonth(this int months) => Period.FromMonths(months);

    /// <summary>
    /// 将季度数转换为 <see cref="Period"/>
    /// </summary>
    /// <param name="quarters">季度数</param>
    public static Period AsPeriodOfQuarters(this int quarters) => Period.FromMonths(quarters * 3);

    /// <summary>
    /// 将年数转换为 <see cref="Period"/>
    /// </summary>
    /// <param name="years">年数</param>
    public static Period AsPeriodOfYears(this int years) => Period.FromYears(years);
}