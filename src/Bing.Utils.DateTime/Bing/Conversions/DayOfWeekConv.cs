namespace Bing.Conversions;

/// <summary>
/// 周转换器
/// </summary>
public static class DayOfWeekConv
{
    /// <summary>
    /// 将 <see cref="DayOfWeek"/> 转换为 <see cref="int"/>
    /// </summary>
    /// <param name="week">星期几</param>
    public static int ToInt32(DayOfWeek week) => ToInt32(week, 1);

    /// <summary>
    /// 将 <see cref="DayOfWeek"/> 转换为 <see cref="int"/>
    /// </summary>
    /// <param name="week">星期几</param>
    /// <param name="offset">偏移量</param>
    public static int ToInt32(DayOfWeek week, int offset) =>
        offset + week switch
        {
            DayOfWeek.Sunday => 0,
            DayOfWeek.Monday => 1,
            DayOfWeek.Tuesday => 2,
            DayOfWeek.Wednesday => 3,
            DayOfWeek.Thursday => 4,
            DayOfWeek.Friday => 5,
            DayOfWeek.Saturday => 6,
            _ => 0
        };
}

/// <summary>
/// 周转换器 扩展
/// </summary>
public static class DayOfWeekConvExtensions
{
    /// <summary>
    /// 将 <see cref="DayOfWeek"/> 转换为 <see cref="int"/>
    /// </summary>
    /// <param name="week">星期几</param>
    public static int CastToInt32(this DayOfWeek week) => DayOfWeekConv.ToInt32(week);

    /// <summary>
    /// 将 <see cref="DayOfWeek"/> 转换为 <see cref="int"/>
    /// </summary>
    /// <param name="week">星期几</param>
    /// <param name="offset">偏移量</param>
    public static int CastToInt32(this DayOfWeek week, int offset) => DayOfWeekConv.ToInt32(week, offset);
}