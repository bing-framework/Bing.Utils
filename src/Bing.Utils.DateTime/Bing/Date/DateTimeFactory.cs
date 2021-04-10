using System;

namespace Bing.Date
{
    /// <summary>
    /// 日期时间工厂
    /// </summary>
    public static class DateTimeFactory
    {
        #region Now

        /// <summary>
        /// 此刻
        /// </summary>
        public static DateTime Now() => DateTime.Now;

        /// <summary>
        /// 此刻
        /// </summary>
        public static DateTime UtcNow() => DateTime.UtcNow;

        #endregion

        #region Create

        /// <summary>
        /// 根据指定的日期创建 <see cref="DateTime"/>
        /// </summary>
        /// <param name="year">年</param>
        /// <param name="month">月</param>
        /// <param name="day">日</param>
        public static DateTime Create(int year, int month, int day) => new DateTime(year, month, day);

        /// <summary>
        /// 根据指定的日期创建 <see cref="DateTime"/>
        /// </summary>
        /// <param name="year">年</param>
        /// <param name="month">月</param>
        /// <param name="day">日</param>
        /// <param name="hour">时</param>
        /// <param name="minute">分</param>
        /// <param name="second">秒</param>
        public static DateTime Create(int year, int month, int day, int hour, int minute, int second) => new DateTime(year, month, day, hour, minute, second);

        /// <summary>
        /// 根据指定的日期创建 <see cref="DateTime"/>
        /// </summary>
        /// <param name="year">年</param>
        /// <param name="month">月</param>
        /// <param name="day">日</param>
        /// <param name="hour">时</param>
        /// <param name="minute">分</param>
        /// <param name="second">秒</param>
        /// <param name="millisecond">毫秒</param>
        public static DateTime Create(int year, int month, int day, int hour, int minute, int second, int millisecond) => new DateTime(year, month, day, hour, minute, second, millisecond);

        /// <summary>
        /// 根据指定的日期创建 <see cref="DateTime"/>
        /// </summary>
        /// <param name="year">年</param>
        /// <param name="month">月</param>
        /// <param name="day">日</param>
        /// <param name="hour">时</param>
        /// <param name="minute">分</param>
        /// <param name="second">秒</param>
        /// <param name="millisecond">毫秒</param>
        /// <param name="kind">种类</param>
        public static DateTime Create(int year, int month, int day, int hour, int minute, int second, int millisecond, DateTimeKind kind) => new DateTime(year, month, day, hour, minute, second, millisecond, kind);

        #endregion

        #region CreateLastDayOfMonth

        /// <summary>
        /// 获取一个月中的最后一个日期
        /// </summary>
        /// <param name="year">年</param>
        /// <param name="month">月</param>
        public static DateTime CreateLastDayOfMonth(int year, int month) => Create(year, month, DateTime.DaysInMonth(year, month));

        //public static DateTime CreateLastDayOfMonth(int year, int month, DayOfWeek dayOfWeek)
        //{
        //}

        #endregion
    }
}
