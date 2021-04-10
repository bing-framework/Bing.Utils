using System;

namespace Bing.Date
{
    /// <summary>
    /// Bing <see cref="DateTimeSpan"/> 扩展
    /// </summary>
    public static class DateTimeSpanExtensions
    {
        #region Before

        /// <summary>
        /// 之前
        /// </summary>
        /// <param name="ts">日期时间间隔</param>
        public static DateTime Before(this DateTimeSpan ts) => ts.Before(DateTime.Now);

        /// <summary>
        /// 之前
        /// </summary>
        /// <param name="ts">日期时间间隔</param>
        /// <param name="originalValue">原值</param>
        public static DateTime Before(this DateTimeSpan ts, DateTime originalValue) => originalValue.AddMonths(-ts.Months).AddYears(-ts.Years).Add(-ts.TimeSpan);

        /// <summary>
        /// 之前
        /// </summary>
        /// <param name="ts">日期时间间隔</param>
        public static DateTimeOffset OffsetBefore(this DateTimeSpan ts) => ts.Before(DateTimeOffset.Now);

        /// <summary>
        /// 之前
        /// </summary>
        /// <param name="ts">日期时间间隔</param>
        /// <param name="originalValue">原值</param>
        public static DateTimeOffset Before(this DateTimeSpan ts, DateTimeOffset originalValue) => originalValue.AddMonths(-ts.Months).AddYears(-ts.Years).Add(-ts.TimeSpan);

        #endregion

        #region From

        /// <summary>
        /// 从现在开始
        /// </summary>
        /// <param name="ts">日期时间间隔</param>
        public static DateTime FromNow(this DateTimeSpan ts) => ts.From(DateTime.Now);

        /// <summary>
        /// 从当前时间开始的之后一段日期时间间隔
        /// </summary>
        /// <param name="ts">日期时间间隔</param>
        /// <param name="originalValue">原值</param>
        public static DateTime From(this DateTimeSpan ts, DateTime originalValue) => originalValue.AddMonths(ts.Months).AddYears(ts.Years).Add(ts.TimeSpan);

        /// <summary>
        /// 从现在开始
        /// </summary>
        /// <param name="ts">日期时间间隔</param>
        public static DateTimeOffset OffsetFromNow(this DateTimeSpan ts) => ts.From(DateTimeOffset.Now);

        /// <summary>
        /// 从当前时间开始的之后一段日期时间间隔
        /// </summary>
        /// <param name="ts">日期时间间隔</param>
        /// <param name="originalValue">原值</param>
        public static DateTimeOffset From(this DateTimeSpan ts, DateTimeOffset originalValue) => originalValue.AddMonths(ts.Months).AddYears(ts.Years).Add(ts.TimeSpan);

        #endregion

        #region Number

        /// <summary>
        /// 创建指定年数的日期时间间隔
        /// </summary>
        /// <param name="years">年数</param>
        public static DateTimeSpan Years(this int years) => new DateTimeSpan {Years = years};

        /// <summary>
        /// 创建指定季度数的日期时间间隔
        /// </summary>
        /// <param name="quarters">季度数</param>
        public static DateTimeSpan Quarters(this int quarters) => new DateTimeSpan { Months = quarters*3 };

        /// <summary>
        /// 创建指定月数的日期时间间隔
        /// </summary>
        /// <param name="months">月数</param>
        public static DateTimeSpan Months(this int months) => new DateTimeSpan { Months = months };

        /// <summary>
        /// 创建指定周数的日期时间间隔
        /// </summary>
        /// <param name="weeks">周数</param>
        public static DateTimeSpan Weeks(this int weeks) => new DateTimeSpan { TimeSpan = TimeSpan.FromDays(weeks*7) };

        /// <summary>
        /// 创建指定周数的日期时间间隔
        /// </summary>
        /// <param name="weeks">周数</param>
        public static DateTimeSpan Weeks(this double weeks) => new DateTimeSpan { TimeSpan = TimeSpan.FromDays(weeks * 7) };

        /// <summary>
        /// 创建指定天数的日期时间间隔
        /// </summary>
        /// <param name="days">天数</param>
        public static DateTimeSpan Days(this int days) => new DateTimeSpan { TimeSpan = TimeSpan.FromDays(days) };

        /// <summary>
        /// 创建指定天数的日期时间间隔
        /// </summary>
        /// <param name="days">天数</param>
        public static DateTimeSpan Days(this double days) => new DateTimeSpan { TimeSpan = TimeSpan.FromDays(days) };

        /// <summary>
        /// 创建指定小时数的日期时间间隔
        /// </summary>
        /// <param name="hours">小时数</param>
        public static DateTimeSpan Hours(this int hours) => new DateTimeSpan { TimeSpan = TimeSpan.FromHours(hours) };

        /// <summary>
        /// 创建指定小时数的日期时间间隔
        /// </summary>
        /// <param name="hours">小时数</param>
        public static DateTimeSpan Hours(this double hours) => new DateTimeSpan { TimeSpan = TimeSpan.FromHours(hours) };

        /// <summary>
        /// 创建指定分钟数的日期时间间隔
        /// </summary>
        /// <param name="minutes">分钟数</param>
        public static DateTimeSpan Minutes(this int minutes) => new DateTimeSpan { TimeSpan = TimeSpan.FromMinutes(minutes) };

        /// <summary>
        /// 创建指定分钟数的日期时间间隔
        /// </summary>
        /// <param name="minutes">分钟数</param>
        public static DateTimeSpan Minutes(this double minutes) => new DateTimeSpan { TimeSpan = TimeSpan.FromMinutes(minutes) };

        /// <summary>
        /// 创建指定秒数的日期时间间隔
        /// </summary>
        /// <param name="seconds">秒数</param>
        public static DateTimeSpan Seconds(this int seconds) => new DateTimeSpan { TimeSpan = TimeSpan.FromSeconds(seconds) };

        /// <summary>
        /// 创建指定秒数的日期时间间隔
        /// </summary>
        /// <param name="seconds">秒数</param>
        public static DateTimeSpan Seconds(this double seconds) => new DateTimeSpan { TimeSpan = TimeSpan.FromSeconds(seconds) };

        /// <summary>
        /// 创建指定毫秒数的日期时间间隔
        /// </summary>
        /// <param name="milliseconds">毫秒数</param>
        public static DateTimeSpan Milliseconds(this int milliseconds) => new DateTimeSpan { TimeSpan = TimeSpan.FromMilliseconds(milliseconds) };

        /// <summary>
        /// 创建指定毫秒数的日期时间间隔
        /// </summary>
        /// <param name="milliseconds">毫秒数</param>
        public static DateTimeSpan Milliseconds(this double milliseconds) => new DateTimeSpan { TimeSpan = TimeSpan.FromMilliseconds(milliseconds) };

        /// <summary>
        /// 创建指定刻度数的日期时间间隔
        /// </summary>
        /// <param name="ticks">刻度数</param>
        public static DateTimeSpan Ticks(this int ticks) => new DateTimeSpan { TimeSpan = TimeSpan.FromTicks(ticks) };

        /// <summary>
        /// 创建指定刻度数的日期时间间隔
        /// </summary>
        /// <param name="ticks">刻度数</param>
        public static DateTimeSpan Ticks(this long ticks) => new DateTimeSpan { TimeSpan = TimeSpan.FromTicks(ticks) };

        #endregion

    }
}
