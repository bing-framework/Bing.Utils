using System;

namespace Bing.Date
{
    // 时间点 - 更新
    public static partial class DateTimeOffsetExtensions
    {
        #region At

        /// <summary>
        /// 时间点，修改它的时分
        /// </summary>
        /// <param name="dto">时间点</param>
        /// <param name="hour">时</param>
        /// <param name="minute">分</param>
        public static DateTimeOffset At(this DateTimeOffset dto, int hour, int minute) => dto.SetTime(hour, minute);

        /// <summary>
        /// 时间点，修改它的时分秒
        /// </summary>
        /// <param name="dto">时间点</param>
        /// <param name="hour">时</param>
        /// <param name="minute">分</param>
        /// <param name="second">秒</param>
        public static DateTimeOffset At(this DateTimeOffset dto, int hour, int minute, int second) => dto.SetTime(hour, minute, second);

        /// <summary>
        /// 时间点，修改它的时分秒，以及毫秒
        /// </summary>
        /// <param name="dto">时间点</param>
        /// <param name="hour">时</param>
        /// <param name="minute">分</param>
        /// <param name="second">秒</param>
        /// <param name="milliseconds">毫秒</param>
        public static DateTimeOffset At(this DateTimeOffset dto, int hour, int minute, int second, int milliseconds) => dto.SetTime(hour, minute, second, milliseconds);

        #endregion

        #region On

        /// <summary>
        /// 时间点，修改它的年月日。
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="year">年</param>
        /// <param name="month">月</param>
        /// <param name="day">日</param>
        public static DateTimeOffset On(this DateTimeOffset dt, int year, int month, int day) => dt.SetDate(year, month, day);

        #endregion

        #region Set

        /// <summary>
        /// 设置时间
        /// </summary>
        /// <param name="originalDate">时间点</param>
        /// <param name="hour">时</param>
        public static DateTimeOffset SetTime(this DateTimeOffset originalDate, int hour) =>
            new(originalDate.Year, originalDate.Month, originalDate.Day, hour, originalDate.Minute, originalDate.Second, originalDate.Millisecond, originalDate.Offset);

        /// <summary>
        /// 设置时间
        /// </summary>
        /// <param name="originalDate">时间点</param>
        /// <param name="hour">时</param>
        /// <param name="minute">分</param>
        public static DateTimeOffset SetTime(this DateTimeOffset originalDate, int hour, int minute) =>
            new(originalDate.Year, originalDate.Month, originalDate.Day, hour, minute, originalDate.Second, originalDate.Millisecond, originalDate.Offset);

        /// <summary>
        /// 设置时间
        /// </summary>
        /// <param name="originalDate">时间点</param>
        /// <param name="hour">时</param>
        /// <param name="minute">分</param>
        /// <param name="second">秒</param>
        public static DateTimeOffset SetTime(this DateTimeOffset originalDate, int hour, int minute, int second) =>
            new(originalDate.Year, originalDate.Month, originalDate.Day, hour, minute, second, originalDate.Millisecond, originalDate.Offset);

        /// <summary>
        /// 设置时间
        /// </summary>
        /// <param name="originalDate">时间点</param>
        /// <param name="hour">时</param>
        /// <param name="minute">分</param>
        /// <param name="second">秒</param>
        /// <param name="millisecond">毫秒</param>
        public static DateTimeOffset SetTime(this DateTimeOffset originalDate, int hour, int minute, int second, int millisecond) =>
            new(originalDate.Year, originalDate.Month, originalDate.Day, hour, minute, second, millisecond, originalDate.Offset);

        /// <summary>
        /// 设置时间 - 小时
        /// </summary>
        /// <param name="originalDate">时间点</param>
        /// <param name="hour">时</param>
        public static DateTimeOffset SetHour(this DateTimeOffset originalDate, int hour) =>
            new(originalDate.Year, originalDate.Month, originalDate.Day, hour, originalDate.Minute, originalDate.Second, originalDate.Millisecond, originalDate.Offset);

        /// <summary>
        /// 设置时间 - 分钟
        /// </summary>
        /// <param name="originalDate">时间点</param>
        /// <param name="minute">分</param>
        public static DateTimeOffset SetMinute(this DateTimeOffset originalDate, int minute) =>
            new(originalDate.Year, originalDate.Month, originalDate.Day, originalDate.Hour, minute, originalDate.Second, originalDate.Millisecond, originalDate.Offset);

        /// <summary>
        /// 设置时间 - 秒
        /// </summary>
        /// <param name="originalDate">时间点</param>
        /// <param name="second">秒</param>
        public static DateTimeOffset SetSecond(this DateTimeOffset originalDate, int second) =>
            new(originalDate.Year, originalDate.Month, originalDate.Day, originalDate.Hour, originalDate.Minute, second, originalDate.Millisecond, originalDate.Offset);

        /// <summary>
        /// 设置时间 - 毫秒
        /// </summary>
        /// <param name="originalDate">时间点</param>
        /// <param name="millisecond">毫秒</param>
        public static DateTimeOffset SetMillisecond(this DateTimeOffset originalDate, int millisecond) =>
            new(originalDate.Year, originalDate.Month, originalDate.Day, originalDate.Hour, originalDate.Minute, originalDate.Second, millisecond, originalDate.Offset);

        /// <summary>
        /// 设置时间点为凌晨0点（午夜）
        /// </summary>
        /// <param name="dto">时间点</param>
        public static DateTimeOffset Midnight(this DateTimeOffset dto) => dto.BeginningOfDay();

        /// <summary>
        /// 设置时间点为中午12点（正午）
        /// </summary>
        /// <param name="dto">时间点</param>
        public static DateTimeOffset Noon(this DateTimeOffset dto) => dto.SetTime(12, 0, 0, 0);

        /// <summary>
        /// 设置日期
        /// </summary>
        /// <param name="originalDate">时间点</param>
        /// <param name="year">年</param>
        public static DateTimeOffset SetDate(this DateTimeOffset originalDate, int year) =>
            new(year, originalDate.Month, originalDate.Day, originalDate.Hour, originalDate.Minute, originalDate.Second, originalDate.Millisecond, originalDate.Offset);

        /// <summary>
        /// 设置日期
        /// </summary>
        /// <param name="originalDate">时间点</param>
        /// <param name="year">年</param>
        /// <param name="month">月</param>
        public static DateTimeOffset SetDate(this DateTimeOffset originalDate, int year, int month) =>
            new(year, month, originalDate.Day, originalDate.Hour, originalDate.Minute, originalDate.Second, originalDate.Millisecond, originalDate.Offset);

        /// <summary>
        /// 设置日期
        /// </summary>
        /// <param name="originalDate">时间点</param>
        /// <param name="year">年</param>
        /// <param name="month">月</param>
        /// <param name="day">日</param>
        public static DateTimeOffset SetDate(this DateTimeOffset originalDate, int year, int month, int day) =>
            new(year, month, day, originalDate.Hour, originalDate.Minute, originalDate.Second, originalDate.Millisecond, originalDate.Offset);

        /// <summary>
        /// 设置日期 - 年
        /// </summary>
        /// <param name="originalDate">时间点</param>
        /// <param name="year">年</param>
        public static DateTimeOffset SetYear(this DateTimeOffset originalDate, int year) =>
            new(year, originalDate.Month, originalDate.Day, originalDate.Hour, originalDate.Minute, originalDate.Second, originalDate.Millisecond, originalDate.Offset);

        /// <summary>
        /// 设置日期 - 月
        /// </summary>
        /// <param name="originalDate">时间点</param>
        /// <param name="month">月</param>
        public static DateTimeOffset SetMonth(this DateTimeOffset originalDate, int month) =>
            new(originalDate.Year, month, originalDate.Day, originalDate.Hour, originalDate.Minute, originalDate.Second, originalDate.Millisecond, originalDate.Offset);

        /// <summary>
        /// 设置日期 - 日
        /// </summary>
        /// <param name="originalDate">时间点</param>
        /// <param name="day">日</param>
        public static DateTimeOffset SetDay(this DateTimeOffset originalDate, int day) =>
            new(originalDate.Year, originalDate.Month, day, originalDate.Hour, originalDate.Minute, originalDate.Second, originalDate.Millisecond, originalDate.Offset);

        #endregion
    }
}
