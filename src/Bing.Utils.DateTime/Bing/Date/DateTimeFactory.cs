using Bing.Date.DateUtils;

namespace Bing.Date;

/// <summary>
/// 日期时间工厂
/// </summary>
public static class DateTimeFactory
{
    #region Now

    /// <summary>
    /// 获取当前本机本地时间。
    /// </summary>
    /// <returns>时钟表示的本机当前时间。</returns>
    public static DateTime Now() => DateTime.Now;

    /// <summary>
    /// 获取当前本机本地时间。
    /// </summary>
    /// <param name="clock">当前时间来源。</param>
    /// <returns>Kind 为 Local 的本地时间。</returns>
    /// <remarks>本地时间越界时沿用 BCL 的饱和行为；需要完整时间点时使用 UtcNow。</remarks>
    /// <exception cref="ArgumentNullException">时钟为 null。</exception>
    public static DateTime Now(Clock.IClock clock) => ClockDateTime.Instant(clock).LocalDateTime;

    /// <summary>
    /// 获取当前 UTC 时间。
    /// </summary>
    /// <returns>时钟表示的当前 UTC 时间。</returns>
    public static DateTime UtcNow() => DateTime.UtcNow;

    /// <summary>
    /// 获取当前 UTC 时间。
    /// </summary>
    /// <param name="clock">当前时间来源。</param>
    /// <returns>Kind 为 Utc 的时间。</returns>
    /// <exception cref="ArgumentNullException">时钟为 null。</exception>
    public static DateTime UtcNow(Clock.IClock clock) => ClockDateTime.Instant(clock).UtcDateTime;

    /// <summary>
    /// 获取指定时区中当前自然日的午夜。
    /// </summary>
    /// <param name="clock">当前时间来源。</param>
    /// <param name="timeZone">业务时区。</param>
    /// <returns>Kind 为 Unspecified 的当地自然日。</returns>
    /// <exception cref="ArgumentNullException">时钟或时区为 null。</exception>
    /// <exception cref="ArgumentOutOfRangeException">当前时间点不能在目标时区表示。</exception>
    public static DateTime Today(Clock.IClock clock, TimeZoneInfo timeZone) => ClockDateTime.Today(clock, timeZone);

    #endregion

    #region Create

    /// <summary>
    /// 根据指定的日期创建 <see cref="DateTime"/>
    /// </summary>
    /// <param name="year">年</param>
    /// <param name="month">月</param>
    /// <param name="day">日</param>
    /// <returns>按指定日期时间分量创建的日期时间。</returns>
    public static DateTime Create(int year, int month, int day) => new(year, month, day);

    /// <summary>
    /// 根据指定的日期创建 <see cref="DateTime"/>
    /// </summary>
    /// <param name="year">年</param>
    /// <param name="month">月</param>
    /// <param name="day">日</param>
    /// <param name="hour">时</param>
    /// <param name="minute">分</param>
    /// <param name="second">秒</param>
    /// <returns>按指定日期时间分量创建的日期时间。</returns>
    public static DateTime Create(int year, int month, int day, int hour, int minute, int second) => new(year, month, day, hour, minute, second);

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
    /// <returns>按指定日期时间分量创建的日期时间。</returns>
    public static DateTime Create(int year, int month, int day, int hour, int minute, int second, int millisecond) => new(year, month, day, hour, minute, second, millisecond);

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
    /// <returns>按指定日期时间分量创建的日期时间。</returns>
    public static DateTime Create(int year, int month, int day, int hour, int minute, int second, int millisecond, DateTimeKind kind) => new(year, month, day, hour, minute, second, millisecond, kind);

    #endregion

    #region Create Last Day Of Month

    /// <summary>
    /// 寻找一个月中的最后一个日期
    /// </summary>
    /// <param name="year">年</param>
    /// <param name="month">月</param>
    /// <returns>指定月份最后一天的日期时间。</returns>
    public static DateTime CreateLastDayOfMonth(int year, int month) => Create(year, month, DateTime.DaysInMonth(year, month));

    /// <summary>
    /// 寻找一个月中的最后一个工作日（如周一）
    /// </summary>
    /// <param name="year">年</param>
    /// <param name="month">月</param>
    /// <param name="dayOfWeek">星期几</param>
    /// <returns>对应的指定星期日期。</returns>
    /// <exception cref="ArgumentOutOfRangeException">日期、星期或星期序号越界。</exception>
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
    /// <returns>对应的指定星期日期。</returns>
    /// <exception cref="ArgumentOutOfRangeException">日期、星期或星期序号越界。</exception>
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
    /// <returns>指定月份第一天的日期时间。</returns>
    public static DateTime CreateFirstDayOfMonth(int year, int month) => Create(year, month, 1);

    /// <summary>
    /// 寻找一个月中的第一个工作日（如周一）
    /// </summary>
    /// <param name="year">年</param>
    /// <param name="month">月</param>
    /// <param name="dayOfWeek">星期几</param>
    /// <returns>对应的指定星期日期。</returns>
    /// <exception cref="ArgumentOutOfRangeException">日期、星期或星期序号越界。</exception>
    public static DateTime CreateFirstDayOfMonth(int year, int month, DayOfWeek dayOfWeek) => DateTimeCalc.OffsetByWeek(year, month, 1, dayOfWeek);

    /// <summary>
    /// 寻找一个月中的第一个工作日（如周一）
    /// </summary>
    /// <param name="year">年</param>
    /// <param name="month">月</param>
    /// <param name="dayOfWeek">星期几</param>
    /// <returns>对应的指定星期日期。</returns>
    /// <exception cref="ArgumentOutOfRangeException">日期、星期或星期序号越界。</exception>
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
    /// <returns>对应的指定星期日期。</returns>
    /// <exception cref="ArgumentOutOfRangeException">日期、星期或星期序号越界。</exception>
    public static DateTime CreateNextDayByWeek(int year, int month, int day, DayOfWeek dayOfWeek) => DateTimeCalc.OffsetByWeekAfter(Create(year, month, day), dayOfWeek);

    /// <summary>
    /// 根据指定的日期，获得下一个工作日（如周一）
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="dayOfWeek">星期几</param>
    /// <returns>对应的指定星期日期。</returns>
    /// <exception cref="ArgumentOutOfRangeException">日期、星期或星期序号越界。</exception>
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
    /// <returns>对应的指定星期日期。</returns>
    /// <exception cref="ArgumentOutOfRangeException">日期、星期或星期序号越界。</exception>
    public static DateTime CreatePreviousDayByWeek(int year, int month, int day, DayOfWeek dayOfWeek) => DateTimeCalc.OffsetByWeekBefore(Create(year, month, day), dayOfWeek);

    /// <summary>
    /// 根据指定的日期，获取上一个工作日（如周一）
    /// </summary>
    /// <param name="dt">时间</param>
    /// <param name="dayOfWeek">星期几</param>
    /// <returns>对应的指定星期日期。</returns>
    /// <exception cref="ArgumentOutOfRangeException">日期、星期或星期序号越界。</exception>
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
    /// <returns>指定星期日期；当月没有该次出现时返回 DateTime.MinValue。</returns>
    /// <exception cref="ArgumentOutOfRangeException">年份、月份、星期或出现序号越界。</exception>
    public static DateTime CreateByWeek(int year, int month, DayOfWeek dayOfWeek, int occurrence)
    {
        if (occurrence < 1 || occurrence > 5)
            throw new ArgumentOutOfRangeException(nameof(occurrence), occurrence, "出现序号必须在1到5之间。");
        return DateTimeCalc.OffsetByWeek(year, month, occurrence, dayOfWeek);
    }

    #endregion
}
