namespace Bing.Date;

/// <summary>
/// Bing 时间点(<see cref="DateTimeOffset"/>) 扩展
/// </summary>
public static partial class DateTimeOffsetExtensions
{
    #region Add

    /// <summary>
    /// 添加 <see cref="DateTimeSpan"/>
    /// </summary>
    /// <param name="dto">时间点</param>
    /// <param name="timeSpan">日期时间间隔</param>
    public static DateTimeOffset AddDateTimeSpan(this DateTimeOffset dto, DateTimeSpan timeSpan) =>
        dto.AddMonths(timeSpan.Months)
            .AddYears(timeSpan.Years)
            .Add(timeSpan.TimeSpan);

    /// <summary>
    /// 减去 <see cref="DateTimeSpan"/>
    /// </summary>
    /// <param name="dto">时间点</param>
    /// <param name="timeSpan">日期时间间隔</param>
    public static DateTimeOffset SubtractDateTimeSpan(this DateTimeOffset dto, DateTimeSpan timeSpan) =>
        dto.AddMonths(-timeSpan.Months)
            .AddYears(-timeSpan.Years)
            .Add(timeSpan.TimeSpan);

    /// <summary>
    /// 添加工作日
    /// </summary>
    /// <param name="dto">时间点</param>
    /// <param name="days">天数</param>
    public static DateTimeOffset AddBusinessDays(this DateTimeOffset dto, int days)
    {
        var sign = Math.Sign(days);
        var unsignedDays = Math.Abs(days);
        for (var i = 0; i < unsignedDays; i++)
        {
            do
            {
                dto = dto.AddDays(sign);
            } while (dto.DayOfWeek == DayOfWeek.Saturday || dto.DayOfWeek == DayOfWeek.Sunday);
        }

        return dto;
    }

    /// <summary>
    /// 减去工作日
    /// </summary>
    /// <param name="dto">时间点</param>
    /// <param name="days">天数</param>
    public static DateTimeOffset SubtractBusinessDays(this DateTimeOffset dto, int days) => AddBusinessDays(dto, -days);

    #endregion

    #region Is

    /// <summary>
    /// 判断给定时间点是否今天
    /// </summary>
    /// <param name="dto">时间点</param>
    public static bool IsToday(this DateTimeOffset dto) => dto.Date == DateTime.Today;

    /// <summary>
    /// 判断给定时间点是否今天
    /// </summary>
    /// <param name="dto">时间点</param>
    public static bool IsToday(this DateTimeOffset? dto) => dto.GetValueOrDefault().Date == DateTime.Today;

    /// <summary>
    /// 判断指定时间点是否相对给定时间点的过去
    /// </summary>
    /// <param name="current">时间点</param>
    /// <param name="toCompareWith">比较时间点</param>
    public static bool IsBefore(this DateTimeOffset current, DateTimeOffset toCompareWith) => current < toCompareWith;

    /// <summary>
    /// 判断指定时间点是否相对给定时间点的未来
    /// </summary>
    /// <param name="current">时间点</param>
    /// <param name="toCompareWith">比较时间点</param>
    public static bool IsAfter(this DateTimeOffset current, DateTimeOffset toCompareWith) => current > toCompareWith;

    /// <summary>
    /// 判断指定时间点是否相对当前时间点的未来
    /// </summary>
    /// <param name="dto">时间点</param>
    public static bool IsInTheFuture(this DateTimeOffset dto) => dto > DateTimeOffset.Now;

    /// <summary>
    /// 判断指定时间点是否相对当前时间点的过去
    /// </summary>
    /// <param name="dto">时间点</param>
    public static bool IsInThePast(this DateTimeOffset dto) => dto < DateTimeOffset.Now;

    /// <summary>
    /// 判断是否同一天
    /// </summary>
    /// <param name="current">时间点</param>
    /// <param name="date">日期</param>
    public static bool IsSameDay(this DateTimeOffset current, DateTimeOffset date) => current.Date == date.Date;

    /// <summary>
    /// 判断是否同一个月
    /// </summary>
    /// <param name="current">时间点</param>
    /// <param name="date">日期</param>
    public static bool IsSameMonth(this DateTimeOffset current, DateTimeOffset date) => current.Month == date.Month;

    /// <summary>
    /// 判断是否同一年
    /// </summary>
    /// <param name="current">时间点</param>
    /// <param name="date">日期</param>
    public static bool IsSameYear(this DateTimeOffset current, DateTimeOffset date) => current.Year == date.Year;

    #endregion

    #region Round

    /// <summary>
    /// 将时间点四舍五入到最接近给定精度的值
    /// </summary>
    /// <param name="dto">时间点</param>
    /// <param name="rt">舍入精度</param>
    public static DateTimeOffset Round(this DateTimeOffset dto, RoundTo rt)
    {
        DateTimeOffset rounded;
        switch (rt)
        {
            case RoundTo.Second:
            {
                rounded = new DateTimeOffset(dto.Year, dto.Month, dto.Day, dto.Hour, dto.Minute, dto.Second, dto.Offset);
                if (dto.Millisecond >= 500)
                    rounded = rounded.AddSeconds(1);
                break;
            }
            case RoundTo.Minute:
            {
                rounded = new DateTimeOffset(dto.Year, dto.Month, dto.Day, dto.Hour, dto.Minute, 0, dto.Offset);
                if (dto.Second >= 30)
                    rounded = rounded.AddMinutes(1);
                break;
            }
            case RoundTo.Hour:
            {
                rounded = new DateTimeOffset(dto.Year, dto.Month, dto.Day, dto.Hour, 0, 0, dto.Offset);
                if (dto.Minute >= 30)
                    rounded = rounded.AddHours(1);
                break;
            }
            case RoundTo.Day:
            {
                rounded = new DateTimeOffset(dto.Year, dto.Month, dto.Day, 0, 0, 0, dto.Offset);
                if (dto.Hour >= 12)
                    rounded = rounded.AddDays(1);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(rt));
        }
        return rounded;
    }

    #endregion
}