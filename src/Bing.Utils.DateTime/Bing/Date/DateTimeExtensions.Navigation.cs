using System;
using System.Globalization;

namespace Bing.Date
{
    // 日期时间 - 导航
    public static partial class DateTimeExtensions
    {
        #region Offset

        /// <summary>
        /// 偏移指定值
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="offsetVal">偏移值</param>
        /// <param name="styles">时间偏移样式</param>
        public static DateTime OffsetBy(this DateTime dt, int offsetVal, DateTimeOffsetStyles styles) =>
            styles switch
            {
                DateTimeOffsetStyles.Day => DateTimeCalc.OffsetByDays(dt, offsetVal),
                DateTimeOffsetStyles.Week => DateTimeCalc.OffsetByWeeks(dt, offsetVal),
                DateTimeOffsetStyles.Month => DateTimeCalc.OffsetByMonths(dt, offsetVal, DateTimeOffsetOptions.Relatively),
                DateTimeOffsetStyles.Quarters => DateTimeCalc.OffsetByQuarters(dt, offsetVal, DateTimeOffsetOptions.Relatively),
                DateTimeOffsetStyles.Year => DateTimeCalc.OffsetByYears(dt, offsetVal, DateTimeOffsetOptions.Relatively),
                _ => DateTimeCalc.OffsetByDays(dt, offsetVal)
            };

        #endregion

        #region Begin

        /// <summary>
        /// 获取一天的开始时间。
        /// </summary>
        /// <param name="dt">时间</param>
        public static DateTime BeginningOfDay(this DateTime dt) => DateTimeFactory.Create(dt.Year, dt.Month, dt.Day, 0, 0, 0, 0, dt.Kind);

        /// <summary>
        /// 获取一天的开始时间。
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="timeZoneOffset">时区偏移量</param>
        public static DateTime BeginningOfDay(this DateTime dt, int timeZoneOffset) => DateTimeFactory.Create(dt.Year, dt.Month, dt.Day, 0, 0, 0, 0, dt.Kind).AddHours(timeZoneOffset);

        /// <summary>
        /// 获取一周的开始时间
        /// </summary>
        /// <param name="dt">时间</param>
        public static DateTime BeginningOfWeek(this DateTime dt) => dt.FirstDayOfWeek().BeginningOfDay();

        /// <summary>
        /// 获取一周的开始时间
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="timeZoneOffset">时区偏移量</param>
        public static DateTime BeginningOfWeek(this DateTime dt, int timeZoneOffset) => dt.FirstDayOfWeek().BeginningOfDay(timeZoneOffset);

        /// <summary>
        /// 获取一个月的开始时间
        /// </summary>
        /// <param name="dt">时间</param>
        public static DateTime BeginningOfMonth(this DateTime dt) => dt.FirstDayOfMonth().BeginningOfDay();

        /// <summary>
        /// 获取一个月的开始时间
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="timeZoneOffset">时区偏移量</param>
        public static DateTime BeginningOfMonth(this DateTime dt, int timeZoneOffset) => dt.FirstDayOfMonth().BeginningOfDay(timeZoneOffset);

        /// <summary>
        /// 获取一个季度的开始时间
        /// </summary>
        /// <param name="dt">时间</param>
        public static DateTime BeginningOfQuarter(this DateTime dt) => dt.FirstDayOfQuarter().BeginningOfDay();

        /// <summary>
        /// 获取一个季度的开始时间
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="timeZoneOffset">时区偏移量</param>
        public static DateTime BeginningOfQuarter(this DateTime dt, int timeZoneOffset) => dt.FirstDayOfQuarter().BeginningOfDay(timeZoneOffset);

        /// <summary>
        /// 获取一年的开始时间
        /// </summary>
        /// <param name="dt">时间</param>
        public static DateTime BeginningOfYear(this DateTime dt) => dt.FirstDayOfYear().BeginningOfDay();

        /// <summary>
        /// 获取一年的开始时间
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="timeZoneOffset">时区偏移量</param>
        public static DateTime BeginningOfYear(this DateTime dt, int timeZoneOffset) => dt.FirstDayOfYear().BeginningOfDay(timeZoneOffset);

        #endregion

        #region End

        /// <summary>
        /// 获取一天的结束时间
        /// </summary>
        /// <param name="dt">时间</param>
        public static DateTime EndOfDay(this DateTime dt) => DateTimeFactory.Create(dt.Year, dt.Month, dt.Day, 23, 59, 59, 999, dt.Kind);

        /// <summary>
        /// 获取一天的结束时间
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="timeZoneOffset">时区偏移量</param>
        public static DateTime EndOfDay(this DateTime dt, int timeZoneOffset) => DateTimeFactory.Create(dt.Year, dt.Month, dt.Day, 23, 59, 59, 999, dt.Kind).AddHours(timeZoneOffset);

        /// <summary>
        /// 获取一周的结束时间
        /// </summary>
        /// <param name="dt">时间</param>
        public static DateTime EndOfWeek(this DateTime dt) => dt.LastDayOfWeek().EndOfDay();

        /// <summary>
        /// 获取一周的结束时间
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="timeZoneOffset">时区偏移量</param>
        public static DateTime EndOfWeek(this DateTime dt, int timeZoneOffset) => dt.LastDayOfWeek().EndOfDay(timeZoneOffset);

        /// <summary>
        /// 获取一个月的结束时间
        /// </summary>
        /// <param name="dt">时间</param>
        public static DateTime EndOfMonth(this DateTime dt) => dt.LastDayOfMonth().EndOfDay();

        /// <summary>
        /// 获取一个月的结束时间
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="timeZoneOffset">时区偏移量</param>
        public static DateTime EndOfMonth(this DateTime dt, int timeZoneOffset) => dt.LastDayOfMonth().EndOfDay(timeZoneOffset);

        /// <summary>
        /// 获取一个季度的结束时间
        /// </summary>
        /// <param name="dt">时间</param>
        public static DateTime EndOfQuarter(this DateTime dt) => dt.LastDayOfQuarter().EndOfDay();

        /// <summary>
        /// 获取一个季度的结束时间
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="timeZoneOffset">时区偏移量</param>
        public static DateTime EndOfQuarter(this DateTime dt, int timeZoneOffset) => dt.LastDayOfQuarter().EndOfDay(timeZoneOffset);

        /// <summary>
        /// 获取一年的结束时间
        /// </summary>
        /// <param name="dt">时间</param>
        public static DateTime EndOfYear(this DateTime dt) => dt.LastDayOfYear().EndOfDay();

        /// <summary>
        /// 获取一年的结束时间
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="timeZoneOffset">时区偏移量</param>
        public static DateTime EndOfYear(this DateTime dt, int timeZoneOffset) => dt.LastDayOfYear().EndOfDay(timeZoneOffset);

        #endregion

        #region Get

        /// <summary>
        /// 获取两个时间之间的间隔
        /// </summary>
        /// <param name="leftDt">时间</param>
        /// <param name="rightDt">时间</param>
        public static TimeSpan GetTimeSpan(this DateTime leftDt, DateTime rightDt) => rightDt - leftDt;

        /// <summary>
        /// 获取指定年份的第一天
        /// </summary>
        /// <param name="dt">时间</param>
        public static DateTime FirstDayOfYear(this DateTime dt) => dt.SetDate(dt.Year, 1, 1);

        /// <summary>
        /// 获取指定年份的第一个星期指定的某一天
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="dayOfWeek">星期几</param>
        public static DateTime FirstDayOfYear(this DateTime dt, DayOfWeek dayOfWeek)
        {
            var ret = dt.FirstDayOfYear();
            return DayOfWeekCalc.TryDaysBetween(ret.DayOfWeek, dayOfWeek, out var day) && day > 0
                ? ret.AddDays(day)
                : ret;
        }

        /// <summary>
        /// 获取指定季度的第一天
        /// </summary>
        /// <param name="dt">时间</param>
        public static DateTime FirstDayOfQuarter(this DateTime dt)
        {
            var currentQuarter = dt.QuarterOfMonth();
            var month = 3 * currentQuarter - 2;
            return DateTimeFactory.Create(dt.Year, month, 1);
        }

        /// <summary>
        /// 获取指定季度的第一个星期指定的某一天
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="dayOfWeek">星期几</param>
        public static DateTime FirstDayOfQuarter(this DateTime dt, DayOfWeek dayOfWeek)
        {
            var ret = dt.FirstDayOfQuarter();
            return DayOfWeekCalc.TryDaysBetween(ret.DayOfWeek, dayOfWeek, out var day) && day > 0
                ? ret.AddDays(day)
                : ret;
        }

        /// <summary>
        /// 获取指定月份的第一天
        /// </summary>
        /// <param name="dt">时间</param>
        public static DateTime FirstDayOfMonth(this DateTime dt) => dt.SetDay(1);

        /// <summary>
        /// 获取指定月份的第一个星期指定的某一天
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="dayOfWeek">星期几</param>
        public static DateTime FirstDayOfMonth(this DateTime dt, DayOfWeek dayOfWeek)
        {
            var ret = dt.FirstDayOfMonth();
            return DayOfWeekCalc.TryDaysBetween(ret.DayOfWeek, dayOfWeek, out var day) && day > 0
                ? ret.AddDays(day)
                : ret;
        }

        /// <summary>
        /// 获取指定星期的第一天
        /// </summary>
        /// <param name="dt">时间</param>
        public static DateTime FirstDayOfWeek(this DateTime dt) => dt.FirstDayOfWeek(null);

        /// <summary>
        /// 获取指定星期的第一天
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="cultureInfo">区域信息</param>
        public static DateTime FirstDayOfWeek(this DateTime dt, CultureInfo cultureInfo)
        {
            cultureInfo ??= CultureInfo.CurrentCulture;
            var firstDayOfWeek = cultureInfo.DateTimeFormat.FirstDayOfWeek;
            var offset = dt.DayOfWeek - firstDayOfWeek < 0 ? 7 : 0;
            var numberOfDaysSinceBeginningOfTheWeek = dt.DayOfWeek + offset - firstDayOfWeek;
            return dt.AddDays(-numberOfDaysSinceBeginningOfTheWeek);
        }

        /// <summary>
        /// 获取指定年份的最后一天
        /// </summary>
        /// <param name="dt">时间</param>
        public static DateTime LastDayOfYear(this DateTime dt) => dt.SetDate(dt.Year, 12, 31);

        /// <summary>
        /// 获取指定年份的最后一个星期指定的某一天
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="dayOfWeek">星期几</param>
        public static DateTime LastDayOfYear(this DateTime dt, DayOfWeek dayOfWeek)
        {
            var ret = dt.LastDayOfYear();
            return DayOfWeekCalc.TryDaysBetween(ret.DayOfWeek, dayOfWeek, out var day) && day == 0
                ? ret
                : ret.AddDays(-day);
        }

        /// <summary>
        /// 获取指定季度的最后一天
        /// </summary>
        /// <param name="dt">时间</param>
        public static DateTime LastDayOfQuarter(this DateTime dt)
        {
            var currentQuarter = dt.QuarterOfMonth();
            var month = 3 * currentQuarter;
            var day = DateTime.DaysInMonth(dt.Year, month);
            return DateTimeFactory.Create(dt.Year, month, day);
        }

        /// <summary>
        /// 获取指定季度的最后一个星期指定的某一天
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="dayOfWeek">星期几</param>
        public static DateTime LastDayOfQuarter(this DateTime dt, DayOfWeek dayOfWeek)
        {
            var ret = dt.LastDayOfQuarter();
            return DayOfWeekCalc.TryDaysBetween(ret.DayOfWeek, dayOfWeek, out var day) && day == 0
                ? ret
                : ret.AddDays(-day);
        }

        /// <summary>
        /// 获取指定月份的最后一天
        /// </summary>
        /// <param name="dt">时间</param>
        public static DateTime LastDayOfMonth(this DateTime dt) => dt.SetDay(dt.DaysInMonth());

        /// <summary>
        /// 获取指定月份的最后一个星期指定的某一天
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="dayOfWeek">星期几</param>
        public static DateTime LastDayOfMonth(this DateTime dt, DayOfWeek dayOfWeek)
        {
            var ret = dt.LastDayOfMonth();
            return DayOfWeekCalc.TryDaysBetween(ret.DayOfWeek, dayOfWeek, out var day) && day == 0
                ? ret
                : ret.AddDays(-day);
        }

        /// <summary>
        /// 获取指定星期的最后一天
        /// </summary>
        /// <param name="dt">时间</param>
        public static DateTime LastDayOfWeek(this DateTime dt) => dt.LastDayOfWeek(null);

        /// <summary>
        /// 获取指定星期的最后一天
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="cultureInfo">区域信息</param>
        public static DateTime LastDayOfWeek(this DateTime dt, CultureInfo cultureInfo) => dt.FirstDayOfWeek(cultureInfo).AddDays(6);

        /// <summary>
        /// 获取指定月份中的天数
        /// </summary>
        /// <param name="dt">时间</param>
        public static int DaysInMonth(this DateTime dt) => DateTime.DaysInMonth(dt.Year, dt.Month);

        /// <summary>
        /// 获取指定年份中的天数
        /// </summary>
        /// <param name="dt">时间</param>
        public static int DaysInYear(this DateTime dt) => DateTime.IsLeapYear(dt.Year) ? 366 : 365;

        /// <summary>
        /// 获取指定月份是第几个季度
        /// </summary>
        /// <param name="dt">时间</param>
        public static int QuarterOfMonth(this DateTime dt)=> (dt.Month - 1) / 3 + 1;

        /// <summary>
        /// 获取指定的星期是一年中的第几个星期
        /// </summary>
        /// <param name="dt">时间</param>
        public static int GetWeekOfYear(this DateTime dt) => GetWeekOfYear(dt, new DateTimeFormatInfo().CalendarWeekRule, new DateTimeFormatInfo().FirstDayOfWeek);

        /// <summary>
        /// 获取指定的星期是一年中的第几个星期
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="weekRule">星期规则</param>
        public static int GetWeekOfYear(this DateTime dt, CalendarWeekRule weekRule) => GetWeekOfYear(dt, weekRule, new DateTimeFormatInfo().FirstDayOfWeek);

        /// <summary>
        /// 获取指定的星期是一年中的第几个星期
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="firstDayOfWeek">星期几</param>
        public static int GetWeekOfYear(this DateTime dt, DayOfWeek firstDayOfWeek) => GetWeekOfYear(dt, new DateTimeFormatInfo().CalendarWeekRule, firstDayOfWeek);

        /// <summary>
        /// 获取指定的星期是一年中的第几个星期
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="weekRule">星期规则</param>
        /// <param name="firstDayOfWeek">星期几</param>
        public static int GetWeekOfYear(this DateTime dt, CalendarWeekRule weekRule, DayOfWeek firstDayOfWeek) => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(dt, weekRule, firstDayOfWeek);

        #endregion

        #region Navigation

        /// <summary>
        /// 下一年（明年）
        /// </summary>
        /// <param name="dt">时间</param>
        public static DateTime NextYear(this DateTime dt) => dt.OffsetBy(1, DateTimeOffsetStyles.Year);

        /// <summary>
        /// 上一年（去年）
        /// </summary>
        /// <param name="dt">时间</param>
        public static DateTime PreviousYear(this DateTime dt) => dt.OffsetBy(-1, DateTimeOffsetStyles.Year);

        /// <summary>
        /// 下个季度
        /// </summary>
        /// <param name="dt">时间</param>
        public static DateTime NextQuarter(this DateTime dt) => dt.OffsetBy(1, DateTimeOffsetStyles.Quarters);

        /// <summary>
        /// 上个季度
        /// </summary>
        /// <param name="dt">时间</param>
        public static DateTime PreviousQuarter(this DateTime dt) => dt.OffsetBy(-1, DateTimeOffsetStyles.Quarters);

        /// <summary>
        /// 下个月
        /// </summary>
        /// <param name="dt">时间</param>
        public static DateTime NextMonth(this DateTime dt) => dt.OffsetBy(1, DateTimeOffsetStyles.Month);

        /// <summary>
        /// 上个月
        /// </summary>
        /// <param name="dt">时间</param>
        public static DateTime PreviousMonth(this DateTime dt) => dt.OffsetBy(-1, DateTimeOffsetStyles.Month);

        /// <summary>
        /// 下个星期
        /// </summary>
        /// <param name="dt">时间</param>
        public static DateTime NextWeek(this DateTime dt) => dt.OffsetBy(1, DateTimeOffsetStyles.Week);

        /// <summary>
        /// 上个星期
        /// </summary>
        /// <param name="dt">时间</param>
        public static DateTime PreviousWeek(this DateTime dt) => dt.OffsetBy(-1, DateTimeOffsetStyles.Week);

        /// <summary>
        /// 下一天（明天）
        /// </summary>
        /// <param name="dt">时间</param>
        public static DateTime NextDay(this DateTime dt) => dt.OffsetBy(1, DateTimeOffsetStyles.Day);

        /// <summary>
        /// 上一天（昨天）
        /// </summary>
        /// <param name="dt">时间</param>
        public static DateTime PreviousDay(this DateTime dt) => dt.OffsetBy(-1, DateTimeOffsetStyles.Day);

        /// <summary>
        /// 明天
        /// </summary>
        /// <param name="dt">时间</param>
        public static DateTime Tomorrow(this DateTime dt) => dt.NextDay();

        /// <summary>
        /// 昨天
        /// </summary>
        /// <param name="dt">时间</param>
        public static DateTime Yesterday(this DateTime dt) => dt.NextDay();

        /// <summary>
        /// 下个星期几
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="dayOfWeek">星期几</param>
        public static DateTime NextDayOfWeek(this DateTime dt, DayOfWeek dayOfWeek) => DateTimeCalc.OffsetOfDayOfWeek(dt, dayOfWeek, 1);

        /// <summary>
        /// 上个星期几
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="dayOfWeek">星期几</param>
        public static DateTime PreviousDayOfWeek(this DateTime dt, DayOfWeek dayOfWeek) => DateTimeCalc.OffsetOfDayOfWeek(dt, dayOfWeek, -1);

        #endregion
    }
}
