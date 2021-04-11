using System;

namespace Bing.Date
{
    public static partial class DateTimeExtensions
    {
        #region At

        /// <summary>
        /// 时间，修改它的时分秒
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="hour">时</param>
        /// <param name="minute">分</param>
        /// <param name="second">秒</param>
        public static DateTime At(this DateTime dt, int hour, int minute, int second) => dt.SetTime(hour, minute, second);

        /// <summary>
        /// 时间，修改它的时分秒，以及毫秒
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="hour">时</param>
        /// <param name="minute">分</param>
        /// <param name="second">秒</param>
        /// <param name="milliseconds">毫秒</param>
        public static DateTime At(this DateTime dt, int hour, int minute, int second, int milliseconds) => dt.SetTime(hour, minute, second, milliseconds);

        #endregion

        #region On

        /// <summary>
        /// 日期，修改它的年月日。
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="year">年</param>
        /// <param name="month">月</param>
        /// <param name="day">日</param>
        public static DateTime On(this DateTime dt, int year, int month, int day) => dt.SetDate(year, month, day);

        #endregion

        #region Set

        /// <summary>
        /// 设置时间
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="hour">时</param>
        public static DateTime SetTime(this DateTime dt, int hour) => DateTimeFactory.Create(dt.Year, dt.Month, dt.Day, hour, dt.Minute, dt.Second, dt.Millisecond, dt.Kind);

        /// <summary>
        /// 设置时间
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="hour">时</param>
        /// <param name="minute">分</param>
        public static DateTime SetTime(this DateTime dt, int hour, int minute) => DateTimeFactory.Create(dt.Year, dt.Month, dt.Day, hour, minute, dt.Second, dt.Millisecond, dt.Kind);

        /// <summary>
        /// 设置时间
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="hour">时</param>
        /// <param name="minute">分</param>
        /// <param name="second">秒</param>
        public static DateTime SetTime(this DateTime dt, int hour, int minute, int second) => DateTimeFactory.Create(dt.Year, dt.Month, dt.Day, hour, minute, second, dt.Millisecond, dt.Kind);

        /// <summary>
        /// 设置时间
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="hour">时</param>
        /// <param name="minute">分</param>
        /// <param name="second">秒</param>
        /// <param name="millisecond">毫秒</param>
        public static DateTime SetTime(this DateTime dt, int hour, int minute, int second, int millisecond) => DateTimeFactory.Create(dt.Year, dt.Month, dt.Day, hour, minute, second, millisecond, dt.Kind);

        /// <summary>
        /// 设置时间 - 小时
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="hour">时</param>
        public static DateTime SetHour(this DateTime dt, int hour) => DateTimeFactory.Create(dt.Year, dt.Month, dt.Day, hour, dt.Minute, dt.Second, dt.Millisecond, dt.Kind);

        /// <summary>
        /// 设置时间 - 分钟
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="minute">分</param>
        public static DateTime SetMinute(this DateTime dt, int minute) => DateTimeFactory.Create(dt.Year, dt.Month, dt.Day, dt.Hour, minute, dt.Second, dt.Millisecond, dt.Kind);

        /// <summary>
        /// 设置时间 - 秒
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="second">秒</param>
        public static DateTime SetSecond(this DateTime dt, int second) => DateTimeFactory.Create(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, second, dt.Millisecond, dt.Kind);

        /// <summary>
        /// 设置时间 - 毫秒
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="millisecond">毫秒</param>
        public static DateTime SetMillisecond(this DateTime dt, int millisecond) => DateTimeFactory.Create(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, millisecond, dt.Kind);

        /// <summary>
        /// 设置时间为凌晨0点（午夜）
        /// </summary>
        /// <param name="dt">时间</param>
        public static DateTime Midnight(this DateTime dt) => dt.BeginningOfDay();

        /// <summary>
        /// 设置时间为中午12点（正午）
        /// </summary>
        /// <param name="dt">时间</param>
        public static DateTime Noon(this DateTime dt) => dt.SetTime(12, 0, 0, 0);

        /// <summary>
        /// 设置日期
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="year">年</param>
        public static DateTime SetDate(this DateTime dt, int year) => DateTimeFactory.Create(year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond, dt.Kind);

        /// <summary>
        /// 设置日期
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="year">年</param>
        /// <param name="month">月</param>
        public static DateTime SetDate(this DateTime dt, int year, int month) => DateTimeFactory.Create(year, month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond, dt.Kind);

        /// <summary>
        /// 设置日期
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="year">年</param>
        /// <param name="month">月</param>
        /// <param name="day">日</param>
        public static DateTime SetDate(this DateTime dt, int year, int month, int day) => DateTimeFactory.Create(year, month, day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond, dt.Kind);

        /// <summary>
        /// 设置日期 - 年
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="year">年</param>
        public static DateTime SetYear(this DateTime dt, int year) => DateTimeFactory.Create(year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond, dt.Kind);

        /// <summary>
        /// 设置日期 - 月
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="month">月</param>
        public static DateTime SetMonth(this DateTime dt, int month) => DateTimeFactory.Create(dt.Year, month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond, dt.Kind);

        /// <summary>
        /// 设置日期 - 日
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="day">日</param>
        public static DateTime SetDay(this DateTime dt, int day) => DateTimeFactory.Create(dt.Year, dt.Month, day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond, dt.Kind);

        /// <summary>
        /// 设置日期种类。本地/UTC
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="kind">日期种类</param>
        public static DateTime SetKind(this DateTime dt, DateTimeKind kind) => DateTimeFactory.Create(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond, kind);

        #endregion
    }
}
