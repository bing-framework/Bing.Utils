using System;
using Bing.Conversions;

namespace Bing.Date;

/// <summary>
/// 日期时间工厂
/// </summary>
public static class DateTimeFactory
{
    #region Now

    /// <summary>
    /// 此刻。获取当前本地时间
    /// </summary>
    public static DateTime Now() => DateTime.Now;

    /// <summary>
    /// 此刻。获取当前UTC时间
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

    #region Create Last Day Of Month

    /// <summary>
    /// 寻找一个月中的最后一个日期
    /// </summary>
    /// <param name="year">年</param>
    /// <param name="month">月</param>
    public static DateTime CreateLastDayOfMonth(int year, int month) => Create(year, month, DateTime.DaysInMonth(year, month));

    /// <summary>
    /// 寻找一个月中的最后一个工作日（如周一）
    /// </summary>
    /// <param name="year">年</param>
    /// <param name="month">月</param>
    /// <param name="dayOfWeek">星期几</param>
    public static DateTime CreateLastDayOfMonth(int year, int month, DayOfWeek dayOfWeek) =>
        DateTimeCalc.TryOffsetByWeek(year, month, 5, dayOfWeek, out var resultedDay)
            ? resultedDay
            : DateTimeCalc.OffsetByWeek(year, month, 4, dayOfWeek);

    /// <summary>
    /// 寻找一个月中的最后一个工作日（如周一）
    /// </summary>
    /// <param name="year">年</param>
    /// <param name="month">月</param>
    /// <param name="dayOfWeek">星期几</param>
    public static DateTime CreateLastDayOfMonth(int year, int month, int dayOfWeek) =>
        DateTimeCalc.TryOffsetByWeek(year, month, 5, dayOfWeek, out var resultedDay)
            ? resultedDay
            : DateTimeCalc.OffsetByWeek(year, month, 4, dayOfWeek);

    #endregion

    #region Create First Day Of Month

    /// <summary>
    /// 寻找一个月中的第一个日期
    /// </summary>
    /// <param name="year">年</param>
    /// <param name="month">月</param>
    public static DateTime CreateFirstDayOfMonth(int year, int month) => CreateFirstDayOfMonth(year, month, 1);

    /// <summary>
    /// 寻找一个月中的第一个工作日（如周一）
    /// </summary>
    /// <param name="year">年</param>
    /// <param name="month">月</param>
    /// <param name="dayOfWeek">星期几</param>
    public static DateTime CreateFirstDayOfMonth(int year, int month, DayOfWeek dayOfWeek) => DateTimeCalc.OffsetByWeek(year, month, 1, dayOfWeek);

    /// <summary>
    /// 寻找一个月中的第一个工作日（如周一）
    /// </summary>
    /// <param name="year">年</param>
    /// <param name="month">月</param>
    /// <param name="dayOfWeek">星期几</param>
    public static DateTime CreateFirstDayOfMonth(int year, int month, int dayOfWeek) => DateTimeCalc.OffsetByWeek(year, month, 1, dayOfWeek);

    #endregion

    #region Create Next Day by Week

    /// <summary>
    /// 根据指定的日期，获得下一个工作日（如周一）
    /// </summary>
    /// <param name="year">年</param>
    /// <param name="month">月</param>
    /// <param name="day">日</param>
    /// <param name="dayOfWeek">星期几</param>
    public static DateTime CreateNextDayByWeek(int year, int month, int day, DayOfWeek dayOfWeek) => DateTimeCalc.OffsetByWeekAfter(Create(year, month, day), dayOfWeek);

    /// <summary>
    /// 根据指定的日期，获得下一个工作日（如周一）
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="dayOfWeek">星期几</param>
    public static DateTime CreateNextDayByWeek(DateTime dt, DayOfWeek dayOfWeek) => DateTimeCalc.OffsetByWeekAfter(dt, dayOfWeek);

    #endregion

    #region Create Previous Day by Week

    /// <summary>
    /// 根据指定的日期，获取上一个工作日（如周一）
    /// </summary>
    /// <param name="year">年</param>
    /// <param name="month">月</param>
    /// <param name="day">日</param>
    /// <param name="dayOfWeek">星期几</param>
    public static DateTime CreatePreviousDayByWeek(int year, int month, int day, DayOfWeek dayOfWeek) => DateTimeCalc.OffsetByWeekBefore(Create(year, month, day), dayOfWeek);

    /// <summary>
    /// 根据指定的日期，获取上一个工作日（如周一）
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="dayOfWeek">星期几</param>
    public static DateTime CreatePreviousDayByWeek(DateTime dt, DayOfWeek dayOfWeek) => DateTimeCalc.OffsetByWeekBefore(dt, dayOfWeek);

    #endregion

    #region Create Day by Week

    /// <summary>
    /// 寻找指定的日期（如一个月的第三个周一）
    /// </summary>
    /// <param name="year">年</param>
    /// <param name="month">月</param>
    /// <param name="dayOfWeek">星期几</param>
    /// <param name="occurrence">第几个星期</param>
    public static DateTime CreateByWeek(int year, int month, DayOfWeek dayOfWeek, int occurrence) => DateTimeCalc.OffsetByWeek(year, month, occurrence, dayOfWeek.CastToInt(0));

    #endregion
}