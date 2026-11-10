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
    /// <returns>增加指定年月及固定时长后的日期时间。</returns>
    public static DateTimeOffset AddDateTimeSpan(this DateTimeOffset dto, DateTimeSpan timeSpan) =>
        dto.AddMonths(timeSpan.Months)
            .AddYears(timeSpan.Years)
            .Add(timeSpan.TimeSpan);
    /// <summary>
    /// 减去 <see cref="DateTimeSpan"/>
    /// </summary>
    /// <param name="dto">时间点</param>
    /// <param name="timeSpan">日期时间间隔</param>
    /// <returns>减去指定年月及固定时长后的日期时间。</returns>
    public static DateTimeOffset SubtractDateTimeSpan(this DateTimeOffset dto, DateTimeSpan timeSpan) =>
        dto.AddMonths(checked(-timeSpan.Months))
            .AddYears(checked(-timeSpan.Years))
            .Subtract(timeSpan.TimeSpan);
    /// <summary>
    /// 添加工作日
    /// </summary>
    /// <param name="dto">时间点</param>
    /// <param name="days">天数</param>
    /// <returns>增加指定工作日数后的日期时间。</returns>
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
    /// <returns>减去指定工作日数后的日期时间。</returns>
    public static DateTimeOffset SubtractBusinessDays(this DateTimeOffset dto, int days) => AddBusinessDays(dto, -days);

    #endregion

    #region Is
    /// <summary>
    /// 判断给定时间点是否今天
    /// </summary>
    /// <param name="dto">时间点</param>
    /// <returns>属于当前自然日时为 true，否则为 false。</returns>
    public static bool IsToday(this DateTimeOffset dto) => dto.Date == DateTime.Today;
    /// <summary>
    /// 判断给定时间点是否今天
    /// </summary>
    /// <param name="dto">时间点</param>
    /// <returns>属于当前自然日时为 true，否则为 false。</returns>
    public static bool IsToday(this DateTimeOffset? dto) => dto.GetValueOrDefault().Date == DateTime.Today;
    /// <summary>
    /// 判断指定时间点是否相对给定时间点的过去
    /// </summary>
    /// <param name="current">时间点</param>
    /// <param name="toCompareWith">比较时间点</param>
    /// <returns>早于比较时间时为 true，否则为 false。</returns>
    public static bool IsBefore(this DateTimeOffset current, DateTimeOffset toCompareWith) => current < toCompareWith;
    /// <summary>
    /// 判断指定时间点是否相对给定时间点的未来
    /// </summary>
    /// <param name="current">时间点</param>
    /// <param name="toCompareWith">比较时间点</param>
    /// <returns>晚于比较时间时为 true，否则为 false。</returns>
    public static bool IsAfter(this DateTimeOffset current, DateTimeOffset toCompareWith) => current > toCompareWith;
    /// <summary>
    /// 判断指定时间点是否相对当前时间点的未来
    /// </summary>
    /// <param name="dto">时间点</param>
    /// <returns>晚于当前时间时为 true，否则为 false。</returns>
    public static bool IsInTheFuture(this DateTimeOffset dto) => dto > DateTimeOffset.Now;
    /// <summary>
    /// 判断指定时间点是否相对当前时间点的过去
    /// </summary>
    /// <param name="dto">时间点</param>
    /// <returns>早于当前时间时为 true，否则为 false。</returns>
    public static bool IsInThePast(this DateTimeOffset dto) => dto < DateTimeOffset.Now;
    /// <summary>
    /// 判断是否同一天
    /// </summary>
    /// <param name="current">时间点</param>
    /// <param name="date">日期</param>
    /// <returns>年月日相同时为 true，否则为 false。</returns>
    public static bool IsSameDay(this DateTimeOffset current, DateTimeOffset date) => current.Date == date.Date;
    /// <summary>
    /// 判断是否同一个月
    /// </summary>
    /// <param name="current">时间点</param>
    /// <param name="date">日期</param>
    /// <returns>年份和月份相同时为 true，否则为 false。</returns>
    public static bool IsSameMonth(this DateTimeOffset current, DateTimeOffset date) => current.Year == date.Year && current.Month == date.Month;
    /// <summary>
    /// 判断是否同一年
    /// </summary>
    /// <param name="current">时间点</param>
    /// <param name="date">日期</param>
    /// <returns>年份相同时为 true，否则为 false。</returns>
    public static bool IsSameYear(this DateTimeOffset current, DateTimeOffset date) => current.Year == date.Year;

    #endregion

    #region Round
    /// <summary>
    /// 将 <see cref="DateTimeOffset"/> 对象按指定精度进行舍入。
    /// </summary>
    /// <param name="dto">时间点</param>
    /// <param name="roundTo">舍入精度</param>
    /// <returns>按钟面时间舍入并保持原 Offset 的值。</returns>
    /// <remarks>先计算最终钟面时间，再校验带偏移的时间点，避免中间截断值越界而拒绝合法结果。</remarks>
    /// <exception cref="ArgumentOutOfRangeException">精度非法或最终的钟面时间、UTC 时间点超出支持范围。</exception>
    public static DateTimeOffset Round(this DateTimeOffset dto, RoundTo roundTo)
    {
        if (roundTo < RoundTo.Second || roundTo > RoundTo.Day)
            throw new ArgumentOutOfRangeException(nameof(roundTo));
        return new DateTimeOffset(dto.DateTime.Round(roundTo), dto.Offset);
    }

    #endregion
}
