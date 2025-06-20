using System.Diagnostics.Contracts;

namespace Bing.Date;

/// <summary>
/// Bing <see cref="DateTimeSpan"/> 扩展
/// </summary>
public static class DateTimeSpanExtensions
{
    #region Before

    /// <summary>
    /// 计算当前时间之前的指定日期时间间隔
    /// </summary>
    /// <param name="ts">日期时间间隔</param>
    /// <returns>当前时间减去指定的日期时间间隔后的时间</returns>
    /// <example>
    /// <code>
    /// // 5天前
    /// var fiveDaysAgo = 5.Days().Before(); 
    /// </code>
    /// </example>
    public static DateTime Before(this DateTimeSpan ts) => ts.Before(DateTime.Now);

    /// <summary>
    /// 计算指定时间之前的指定日期时间间隔
    /// </summary>
    /// <param name="ts">日期时间间隔</param>
    /// <param name="originalValue">原始时间</param>
    /// <returns>原始时间减去指定的日期时间间隔后的时间</returns>
    /// <example>
    /// <code>
    /// // 指定日期的5天前
    /// var date = new DateTime(2023, 6, 20);
    /// var fiveDaysBefore = 5.Days().Before(date); // 2023年6月15日
    /// </code>
    /// </example>
    public static DateTime Before(this DateTimeSpan ts, DateTime originalValue) => originalValue.AddMonths(-ts.Months).AddYears(-ts.Years).Add(-ts.TimeSpan);

    /// <summary>
    /// 计算当前时间偏移量之前的指定日期时间间隔
    /// </summary>
    /// <param name="ts">日期时间间隔</param>
    /// <returns>当前时间偏移量减去指定的日期时间间隔后的时间偏移量</returns>
    /// <example>
    /// <code>
    /// // 3个月前
    /// var threeMonthsAgo = 3.Months().OffsetBefore();
    /// </code>
    /// </example>
    public static DateTimeOffset OffsetBefore(this DateTimeSpan ts) => ts.Before(DateTimeOffset.Now);

    /// <summary>
    /// 计算指定时间偏移量之前的指定日期时间间隔
    /// </summary>
    /// <param name="ts">日期时间间隔</param>
    /// <param name="originalValue">原始时间偏移量</param>
    /// <returns>原始时间偏移量减去指定的日期时间间隔后的时间偏移量</returns>
    /// <example>
    /// <code>
    /// // 指定日期的3个月前
    /// var date = new DateTimeOffset(2023, 6, 20, 0, 0, 0, TimeSpan.Zero);
    /// var threeMonthsBefore = 3.Months().Before(date); // 2023年3月20日
    /// </code>
    /// </example>
    public static DateTimeOffset Before(this DateTimeSpan ts, DateTimeOffset originalValue) => originalValue.AddMonths(-ts.Months).AddYears(-ts.Years).Add(-ts.TimeSpan);

    #endregion

    #region From

    /// <summary>
    /// 计算从当前时间开始，向后偏移指定日期时间间隔的时间
    /// </summary>
    /// <param name="ts">日期时间间隔</param>
    /// <returns>当前时间加上指定的日期时间间隔后的时间</returns>
    /// <example>
    /// <code>
    /// // 从现在起5天后
    /// var fiveDaysLater = 5.Days().FromNow();
    /// </code>
    /// </example>
    public static DateTime FromNow(this DateTimeSpan ts) => ts.From(DateTime.Now);

    /// <summary>
    /// 计算从指定时间开始，向后偏移指定日期时间间隔的时间
    /// </summary>
    /// <param name="ts">日期时间间隔</param>
    /// <param name="originalValue">原始时间</param>
    /// <returns>原始时间加上指定的日期时间间隔后的时间</returns>
    /// <example>
    /// <code>
    /// // 指定日期的5天后
    /// var date = new DateTime(2023, 6, 20);
    /// var fiveDaysAfter = 5.Days().From(date); // 2023年6月25日
    /// </code>
    /// </example>
    public static DateTime From(this DateTimeSpan ts, DateTime originalValue) => originalValue.AddMonths(ts.Months).AddYears(ts.Years).Add(ts.TimeSpan);

    /// <summary>
    /// 计算从当前时间偏移量开始，向后偏移指定日期时间间隔的时间偏移量
    /// </summary>
    /// <param name="ts">日期时间间隔</param>
    /// <returns>当前时间偏移量加上指定的日期时间间隔后的时间偏移量</returns>
    /// <example>
    /// <code>
    /// // 从现在起3个月后
    /// var threeMonthsLater = 3.Months().OffsetFromNow();
    /// </code>
    /// </example>
    public static DateTimeOffset OffsetFromNow(this DateTimeSpan ts) => ts.From(DateTimeOffset.Now);

    /// <summary>
    /// 计算从指定时间偏移量开始，向后偏移指定日期时间间隔的时间偏移量
    /// </summary>
    /// <param name="ts">日期时间间隔</param>
    /// <param name="originalValue">原始时间偏移量</param>
    /// <returns>原始时间偏移量加上指定的日期时间间隔后的时间偏移量</returns>
    /// <example>
    /// <code>
    /// // 指定日期的3个月后
    /// var date = new DateTimeOffset(2023, 6, 20, 0, 0, 0, TimeSpan.Zero);
    /// var threeMonthsAfter = 3.Months().From(date); // 2023年9月20日
    /// </code>
    /// </example>
    public static DateTimeOffset From(this DateTimeSpan ts, DateTimeOffset originalValue) => originalValue.AddMonths(ts.Months).AddYears(ts.Years).Add(ts.TimeSpan);

    #endregion

    #region Number

    /// <summary>
    /// 创建指定年数的日期时间间隔
    /// </summary>
    /// <param name="years">年数</param>
    /// <returns>表示指定年数的日期时间间隔</returns>
    /// <example>
    /// <code>
    /// // 2年的时间间隔
    /// var twoYears = 2.Years();
    /// </code>
    /// </example>
    [Pure]
    public static DateTimeSpan Years(this int years) => new(0, years, TimeSpan.Zero);

    /// <summary>
    /// 创建指定季度数的日期时间间隔
    /// </summary>
    /// <param name="quarters">季度数</param>
    /// <returns>表示指定季度数的日期时间间隔</returns>
    /// <example>
    /// <code>
    /// // 2个季度的时间间隔（即6个月）
    /// var twoQuarters = 2.Quarters();
    /// </code>
    /// </example>
    [Pure]
    public static DateTimeSpan Quarters(this int quarters) => new(0, quarters * 3, TimeSpan.Zero);

    /// <summary>
    /// 创建指定月数的日期时间间隔
    /// </summary>
    /// <param name="months">月数</param>
    /// <returns>表示指定月数的日期时间间隔</returns>
    /// <example>
    /// <code>
    /// // 3个月的时间间隔
    /// var threeMonths = 3.Months();
    /// </code>
    /// </example>
    [Pure]
    public static DateTimeSpan Months(this int months) => new(0, months, TimeSpan.Zero);

    /// <summary>
    /// 创建指定周数的日期时间间隔
    /// </summary>
    /// <param name="weeks">周数</param>
    /// <returns>表示指定周数的日期时间间隔</returns>
    /// <example>
    /// <code>
    /// // 2周的时间间隔
    /// var twoWeeks = 2.Weeks();
    /// </code>
    /// </example>
    [Pure]
    public static DateTimeSpan Weeks(this int weeks) => new(0, 0, TimeSpan.FromDays(weeks * 7));

    /// <summary>
    /// 创建指定周数的日期时间间隔（浮点数版本）
    /// </summary>
    /// <param name="weeks">周数</param>
    /// <returns>表示指定周数的日期时间间隔</returns>
    [Pure]
    public static DateTimeSpan Weeks(this double weeks) => new(0, 0, TimeSpan.FromDays(weeks * 7));

    /// <summary>
    /// 创建指定天数的日期时间间隔
    /// </summary>
    /// <param name="days">天数</param>
    /// <returns>表示指定天数的日期时间间隔</returns>
    /// <example>
    /// <code>
    /// // 5天的时间间隔
    /// var fiveDays = 5.Days();
    /// </code>
    /// </example>
    [Pure]
    public static DateTimeSpan Days(this int days) => new(0, 0, TimeSpan.FromDays(days));

    /// <summary>
    /// 创建指定天数的日期时间间隔（浮点数版本）
    /// </summary>
    /// <param name="days">天数</param>
    /// <returns>表示指定天数的日期时间间隔</returns>
    [Pure]
    public static DateTimeSpan Days(this double days) => new(0, 0, TimeSpan.FromDays(days));

    /// <summary>
    /// 创建指定小时数的日期时间间隔
    /// </summary>
    /// <param name="hours">小时数</param>
    /// <returns>表示指定小时数的日期时间间隔</returns>
    /// <example>
    /// <code>
    /// // 12小时的时间间隔
    /// var twelveHours = 12.Hours();
    /// </code>
    /// </example>
    [Pure]
    public static DateTimeSpan Hours(this int hours) => new(0, 0, TimeSpan.FromHours(hours));

    /// <summary>
    /// 创建指定小时数的日期时间间隔（浮点数版本）
    /// </summary>
    /// <param name="hours">小时数</param>
    /// <returns>表示指定小时数的日期时间间隔</returns>
    [Pure]
    public static DateTimeSpan Hours(this double hours) => new(0, 0, TimeSpan.FromHours(hours));

    /// <summary>
    /// 创建指定分钟数的日期时间间隔
    /// </summary>
    /// <param name="minutes">分钟数</param>
    /// <returns>表示指定分钟数的日期时间间隔</returns>
    /// <example>
    /// <code>
    /// // 30分钟的时间间隔
    /// var thirtyMinutes = 30.Minutes();
    /// </code>
    /// </example>
    [Pure]
    public static DateTimeSpan Minutes(this int minutes) => new(0, 0, TimeSpan.FromMinutes(minutes));

    /// <summary>
    /// 创建指定分钟数的日期时间间隔（浮点数版本）
    /// </summary>
    /// <param name="minutes">分钟数</param>
    /// <returns>表示指定分钟数的日期时间间隔</returns>
    [Pure]
    public static DateTimeSpan Minutes(this double minutes) => new(0, 0, TimeSpan.FromMinutes(minutes));

    /// <summary>
    /// 创建指定秒数的日期时间间隔
    /// </summary>
    /// <param name="seconds">秒数</param>
    /// <returns>表示指定秒数的日期时间间隔</returns>
    /// <example>
    /// <code>
    /// // 45秒的时间间隔
    /// var fortyFiveSeconds = 45.Seconds();
    /// </code>
    /// </example>
    [Pure]
    public static DateTimeSpan Seconds(this int seconds) => new(0, 0, TimeSpan.FromSeconds(seconds));

    /// <summary>
    /// 创建指定秒数的日期时间间隔（浮点数版本）
    /// </summary>
    /// <param name="seconds">秒数</param>
    /// <returns>表示指定秒数的日期时间间隔</returns>
    [Pure]
    public static DateTimeSpan Seconds(this double seconds) => new(0, 0, TimeSpan.FromSeconds(seconds));

    /// <summary>
    /// 创建指定毫秒数的日期时间间隔
    /// </summary>
    /// <param name="milliseconds">毫秒数</param>
    /// <returns>表示指定毫秒数的日期时间间隔</returns>
    /// <example>
    /// <code>
    /// // 500毫秒的时间间隔
    /// var fiveHundredMs = 500.Milliseconds();
    /// </code>
    /// </example>
    [Pure]
    public static DateTimeSpan Milliseconds(this int milliseconds) => new(0, 0, TimeSpan.FromMilliseconds(milliseconds));

    /// <summary>
    /// 创建指定毫秒数的日期时间间隔（浮点数版本）
    /// </summary>
    /// <param name="milliseconds">毫秒数</param>
    /// <returns>表示指定毫秒数的日期时间间隔</returns>
    [Pure]
    public static DateTimeSpan Milliseconds(this double milliseconds) => new(0, 0, TimeSpan.FromMilliseconds(milliseconds));

    /// <summary>
    /// 创建指定刻度数的日期时间间隔
    /// </summary>
    /// <param name="ticks">刻度数</param>
    /// <returns>表示指定刻度数的日期时间间隔</returns>
    [Pure]
    public static DateTimeSpan Ticks(this int ticks) => new(0, 0, TimeSpan.FromTicks(ticks));

    /// <summary>
    /// 创建指定刻度数的日期时间间隔
    /// </summary>
    /// <param name="ticks">刻度数</param>
    /// <returns>表示指定刻度数的日期时间间隔</returns>
    /// <example>
    /// <code>
    /// // 10000刻度的时间间隔（1毫秒）
    /// var oneMillisecond = 10000L.Ticks();
    /// </code>
    /// </example>
    [Pure]
    public static DateTimeSpan Ticks(this long ticks) => new(0, 0, TimeSpan.FromTicks(ticks));

    #endregion

    #region Singular

    /// <summary>
    /// 创建1年的日期时间间隔
    /// </summary>
    /// <param name="_">int扩展方法的接收器（忽略）</param>
    /// <returns>表示1年的日期时间间隔</returns>
    /// <example>
    /// <code>
    /// var oneYear = 1.Year();
    /// </code>
    /// </example>
    [Pure]
    public static DateTimeSpan Year(this int _) => new(1, 0, TimeSpan.Zero);

    /// <summary>
    /// 创建1个季度的日期时间间隔（3个月）
    /// </summary>
    /// <param name="_">int扩展方法的接收器（忽略）</param>
    /// <returns>表示1个季度的日期时间间隔</returns>
    /// <example>
    /// <code>
    /// var oneQuarter = 1.Quarter();
    /// </code>
    /// </example>
    [Pure]
    public static DateTimeSpan Quarter(this int _) => new(0, 3, TimeSpan.Zero);

    /// <summary>
    /// 创建1个月的日期时间间隔
    /// </summary>
    /// <param name="_">int扩展方法的接收器（忽略）</param>
    /// <returns>表示1个月的日期时间间隔</returns>
    /// <example>
    /// <code>
    /// var oneMonth = 1.Month();
    /// </code>
    /// </example>
    [Pure]
    public static DateTimeSpan Month(this int _) => new(0, 1, TimeSpan.Zero);

    /// <summary>
    /// 创建1周的日期时间间隔
    /// </summary>
    /// <param name="_">int扩展方法的接收器（忽略）</param>
    /// <returns>表示1周的日期时间间隔</returns>
    /// <example>
    /// <code>
    /// var oneWeek = 1.Week();
    /// </code>
    /// </example>
    [Pure]
    public static DateTimeSpan Week(this int _) => new(0, 0, TimeSpan.FromDays(7));

    /// <summary>
    /// 创建1天的日期时间间隔
    /// </summary>
    /// <param name="_">int扩展方法的接收器（忽略）</param>
    /// <returns>表示1天的日期时间间隔</returns>
    /// <example>
    /// <code>
    /// var oneDay = 1.Day();
    /// </code>
    /// </example>
    [Pure]
    public static DateTimeSpan Day(this int _) => new(0, 0, TimeSpan.FromDays(1));

    /// <summary>
    /// 创建1小时的日期时间间隔
    /// </summary>
    /// <param name="_">int扩展方法的接收器（忽略）</param>
    /// <returns>表示1小时的日期时间间隔</returns>
    /// <example>
    /// <code>
    /// var oneHour = 1.Hour();
    /// </code>
    /// </example>
    [Pure]
    public static DateTimeSpan Hour(this int _) => new(0, 0, TimeSpan.FromHours(1));

    /// <summary>
    /// 创建1分钟的日期时间间隔
    /// </summary>
    /// <param name="_">int扩展方法的接收器（忽略）</param>
    /// <returns>表示1分钟的日期时间间隔</returns>
    /// <example>
    /// <code>
    /// var oneMinute = 1.Minute();
    /// </code>
    /// </example>
    [Pure]
    public static DateTimeSpan Minute(this int _) => new(0, 0, TimeSpan.FromMinutes(1));

    /// <summary>
    /// 创建1秒的日期时间间隔
    /// </summary>
    /// <param name="_">int扩展方法的接收器（忽略）</param>
    /// <returns>表示1秒的日期时间间隔</returns>
    /// <example>
    /// <code>
    /// var oneSecond = 1.Second();
    /// </code>
    /// </example>
    [Pure]
    public static DateTimeSpan Second(this int _) => new(0, 0, TimeSpan.FromSeconds(1));

    /// <summary>
    /// 创建1毫秒的日期时间间隔
    /// </summary>
    /// <param name="_">int扩展方法的接收器（忽略）</param>
    /// <returns>表示1毫秒的日期时间间隔</returns>
    /// <example>
    /// <code>
    /// var oneMillisecond = 1.Millisecond();
    /// </code>
    /// </example>
    [Pure]
    public static DateTimeSpan Millisecond(this int _) => new(0, 0, TimeSpan.FromMilliseconds(1));

    #endregion

}