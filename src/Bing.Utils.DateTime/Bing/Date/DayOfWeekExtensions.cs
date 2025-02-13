using Bing.Date.DateUtils;

namespace Bing.Date;

/// <summary>
/// 周(<see cref="DayOfWeek"/>)) 扩展
/// </summary>
public static class DayOfWeekExtensions
{
    /// <summary>
    /// 将<see cref="DayOfWeek"/>枚举转换为中文表示形式，使用默认前缀“星期”。
    /// </summary>
    /// <param name="week">星期枚举值。</param>
    /// <returns>返回星期的中文表示形式，前缀为“星期”。</returns>
    public static string ToChinese(this DayOfWeek week) => ToChinese(week, "星期");

    /// <summary>
    /// 将<see cref="DayOfWeek"/>枚举转换为中文表示形式。
    /// </summary>
    /// <param name="week">星期枚举值。</param>
    /// <param name="weekNamePre">星期名称前缀，允许自定义前缀以适应不同的显示需求。</param>
    /// <returns>返回星期的中文表示形式，包含自定义前缀。</returns>
    public static string ToChinese(this DayOfWeek week, string weekNamePre) =>
        week switch
        {
            DayOfWeek.Sunday => $"{weekNamePre}日",
            DayOfWeek.Monday => $"{weekNamePre}一",
            DayOfWeek.Tuesday => $"{weekNamePre}二",
            DayOfWeek.Wednesday => $"{weekNamePre}三",
            DayOfWeek.Thursday => $"{weekNamePre}四",
            DayOfWeek.Friday => $"{weekNamePre}五",
            DayOfWeek.Saturday => $"{weekNamePre}六",
            _ => null
        };

    /// <summary>
    /// 给定的星期几基础上添加指定的天数。
    /// </summary>
    /// <param name="dayOfWeek">起始星期几。</param>
    /// <param name="days">要添加的天数，可以是正数或负数。</param>
    /// <returns>添加指定天数后的星期几。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DayOfWeek AddDays(this DayOfWeek dayOfWeek, int days) => DayOfWeekCalc.AddDays(dayOfWeek, days);

    /// <summary>
    /// 获取两个星期几之间的所有星期几。
    /// </summary>
    /// <param name="from">起始星期几。</param>
    /// <param name="to">结束星期几。</param>
    /// <param name="includeBoundary">是否包含边界的星期几，默认为true。</param>
    /// <returns>从起始星期几到结束星期几之间的所有星期几，根据includeBoundary参数决定是否包含起始和结束星期几。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<DayOfWeek> GetDaysBetween(this DayOfWeek from, DayOfWeek to, bool includeBoundary = true) => DayOfWeekCalc.GetDaysBetween(from, to, includeBoundary);
}