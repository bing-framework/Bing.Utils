using System.Globalization;
using Bing.Date.DateUtils;

namespace Bing.Date;

// 时间点 - 导航
public static partial class DateTimeOffsetExtensions
{
    #region Begin

    /// <summary>
    /// 获取指定日期时间的当天起始时间（零点零分零秒）。
    /// </summary>
    /// <param name="dto">时间点</param>
    /// <returns>指定日期时间的当天起始时间。</returns>
    public static DateTimeOffset BeginningOfDay(this DateTimeOffset dto) => new(dto.Year, dto.Month, dto.Day, 0, 0, 0, dto.Offset);

    #endregion

    #region End

    /// <summary>
    /// 获取指定日期时间的当天结束时间（23:59:59.9999999，最后一个 tick）。
    /// </summary>
    /// <param name="dto">时间点</param>
    /// <returns>指定日期时间的当天结束时间。</returns>
    public static DateTimeOffset EndOfDay(this DateTimeOffset dto) => new DateTimeOffset(dto.DateTime.EndOfDay(), dto.Offset);

    #endregion

    #region Get

    /// <summary>
    /// 获取指定年份的第一天
    /// </summary>
    /// <param name="dto">时间点</param>
    public static DateTimeOffset FirstDayOfYear(this DateTimeOffset dto) => dto.SetDate(dto.Year, 1, 1);

    /// <summary>
    /// 获取指定季度的第一天
    /// </summary>
    /// <param name="dto">时间点</param>
    public static DateTimeOffset FirstDayOfQuarter(this DateTimeOffset dto)
    {
        var currentQuarter = (dto.Month - 1) / 3 + 1;
        var month = 3 * currentQuarter - 2;
        return dto.SetDate(dto.Year, month, 1);
    }

    /// <summary>
    /// 获取指定月份的第一天
    /// </summary>
    /// <param name="dto">时间点</param>
    public static DateTimeOffset FirstDayOfMonth(this DateTimeOffset dto) => dto.SetDay(1);

    /// <summary>
    /// 获取指定星期的第一天
    /// </summary>
    /// <param name="dto">时间点</param>
    /// <param name="cultureInfo">区域信息</param>
    public static DateTimeOffset FirstDayOfWeek(this DateTimeOffset dto, CultureInfo cultureInfo = null)
    {
        cultureInfo ??= CultureInfo.CurrentCulture;
        var firstDayOfWeek = cultureInfo.DateTimeFormat.FirstDayOfWeek;
        var offset = dto.DayOfWeek - firstDayOfWeek < 0 ? 7 : 0;
        var numberOfDaysSinceBeginningOfTheWeek = dto.DayOfWeek + offset - firstDayOfWeek;
        return dto.AddDays(-numberOfDaysSinceBeginningOfTheWeek);
    }

    /// <summary>
    /// 获取指定年份的最后一天
    /// </summary>
    /// <param name="dto">时间点</param>
    public static DateTimeOffset LastDayOfYear(this DateTimeOffset dto) => dto.SetDate(dto.Year, 12, 31);

    /// <summary>
    /// 获取指定季度的最后一天
    /// </summary>
    /// <param name="dto">时间点</param>
    public static DateTimeOffset LastDayOfQuarter(this DateTimeOffset dto)
    {
        var currentQuarter = (dto.Month - 1) / 3 + 1;
        var month = 3 * currentQuarter;
        var firstDay = dto.SetDate(dto.Year, 3 * currentQuarter - 2, 1);
        return firstDay.SetMonth(firstDay.Month + 2).LastDayOfMonth();
    }

    /// <summary>
    /// 获取指定月份的最后一天
    /// </summary>
    /// <param name="dto">时间点</param>
    public static DateTimeOffset LastDayOfMonth(this DateTimeOffset dto) => dto.SetDay(DateTime.DaysInMonth(dto.Year, dto.Month));

    /// <summary>
    /// 获取指定星期的最后一天
    /// </summary>
    /// <param name="dto">时间点</param>
    public static DateTimeOffset LastDayOfWeek(this DateTimeOffset dto) => new(dto.DateTime.LastDayOfWeek(), dto.Offset);

    /// <summary>
    /// 获取一周前的时间点
    /// </summary>
    /// <param name="dto">时间点</param>
    public static DateTimeOffset WeekBefore(this DateTimeOffset dto) => dto - 1.Weeks();

    /// <summary>
    /// 获取一周后的时间点
    /// </summary>
    /// <param name="dto">时间点</param>
    public static DateTimeOffset WeekAfter(this DateTimeOffset dto) => dto + 1.Weeks();

    #endregion

    #region Navigation

    /// <summary>
    /// 下一年（明年）
    /// </summary>
    /// <param name="dto">时间点</param>
    public static DateTimeOffset NextYear(this DateTimeOffset dto) => dto.AddYears(1);

    /// <summary>
    /// 上一年（去年）
    /// </summary>
    /// <param name="dto">时间点</param>
    public static DateTimeOffset PreviousYear(this DateTimeOffset dto) => dto.AddYears(-1);

    /// <summary>
    /// 下个季度
    /// </summary>
    /// <param name="dto">时间点</param>
    public static DateTimeOffset NextQuarter(this DateTimeOffset dto) => dto.AddMonths(3);

    /// <summary>
    /// 上个季度
    /// </summary>
    /// <param name="dto">时间点</param>
    public static DateTimeOffset PreviousQuarter(this DateTimeOffset dto) => dto.AddMonths(-3);

    /// <summary>
    /// 下个月
    /// </summary>
    /// <param name="dto">时间点</param>
    public static DateTimeOffset NextMonth(this DateTimeOffset dto) => dto.AddMonths(1);

    /// <summary>
    /// 上个月
    /// </summary>
    /// <param name="dto">时间点</param>
    public static DateTimeOffset PreviousMonth(this DateTimeOffset dto) => dto.AddMonths(-1);

    /// <summary>
    /// 下个星期
    /// </summary>
    /// <param name="dto">时间点</param>
    public static DateTimeOffset NextWeek(this DateTimeOffset dto) => dto.AddDays(7);

    /// <summary>
    /// 上个星期
    /// </summary>
    /// <param name="dto">时间点</param>
    public static DateTimeOffset PreviousWeek(this DateTimeOffset dto) => dto.AddDays(-7);

    /// <summary>
    /// 下一天（明天）
    /// </summary>
    /// <param name="dto">时间点</param>
    public static DateTimeOffset NextDay(this DateTimeOffset dto) => dto.AddDays(1);

    /// <summary>
    /// 上一天（昨天）
    /// </summary>
    /// <param name="dto">时间点</param>
    public static DateTimeOffset PreviousDay(this DateTimeOffset dto) => dto.AddDays(-1);

    /// <summary>
    /// 明天
    /// </summary>
    /// <param name="dto">时间点</param>
    public static DateTimeOffset Tomorrow(this DateTimeOffset dto) => dto.NextDay();

    /// <summary>
    /// 昨天
    /// </summary>
    /// <param name="dto">时间点</param>
    public static DateTimeOffset Yesterday(this DateTimeOffset dto) => dto.PreviousDay();

    /// <summary>
    /// 下个星期几
    /// </summary>
    /// <param name="dto">时间点</param>
    /// <param name="dayOfWeek">星期几</param>
    public static DateTimeOffset NextDayOfWeek(this DateTimeOffset dto, DayOfWeek dayOfWeek) => new DateTimeOffset(DateTimeCalc.OffsetOfDayOfWeek(dto.DateTime, dayOfWeek, 1), dto.Offset);

    /// <summary>
    /// 上个星期几
    /// </summary>
    /// <param name="dto">时间点</param>
    /// <param name="dayOfWeek">星期几</param>
    public static DateTimeOffset PreviousDayOfWeek(this DateTimeOffset dto, DayOfWeek dayOfWeek) => new DateTimeOffset(DateTimeCalc.OffsetOfDayOfWeek(dto.DateTime, dayOfWeek, -1), dto.Offset);

    #endregion
}
