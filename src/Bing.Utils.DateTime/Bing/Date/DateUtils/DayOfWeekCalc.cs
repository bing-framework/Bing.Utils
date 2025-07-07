using Bing.Conversions;
using NodaTime;
using NodaTime.Helpers;

namespace Bing.Date.DateUtils;

/// <summary>
/// 星期计算
/// </summary>
public static class DayOfWeekCalc
{
    /// <summary>
    /// 计算两个星期几之间的间隔天数
    /// </summary>
    /// <param name="start">起始星期几</param>
    /// <param name="end">结束星期几</param>
    /// <returns>从起始星期几到结束星期几之间的间隔天数。如果起始星期大于结束星期，则按跨周计算</returns>
    public static int DaysBetween(DayOfWeek start, DayOfWeek end)
    {
        var startValue = start.CastToInt32();
        var endValue = end.CastToInt32();

        if (startValue <= endValue)
            return endValue - startValue;
        return 7 - (startValue - endValue);
    }

    /// <summary>
    /// 计算两个ISO星期几之间的间隔天数。
    /// </summary>
    /// <param name="start">起始ISO星期几</param>
    /// <param name="end">结束ISO星期几</param>
    /// <returns>返回从起始ISO星期几到结束ISO星期几之间的间隔天数。如果起始星期大于结束星期，则认为跨周计算。如果任一参数为<see cref="IsoDayOfWeek.None"/>，则返回0。</returns>
    public static int DaysBetween(IsoDayOfWeek start, IsoDayOfWeek end)
    {
        if (start == IsoDayOfWeek.None || end == IsoDayOfWeek.None)
            return 0;
        return DaysBetween(DayOfWeekHelper.ToSystemWeek(start), DayOfWeekHelper.ToSystemWeek(end));
    }

    /// <summary>
    /// 尝试获取两个星期几之间的间隔天数
    /// </summary>
    /// <param name="start">起始星期几</param>
    /// <param name="end">结束星期几</param>
    /// <param name="days">间隔天数的输出参数</param>
    /// <returns>始终返回true，表示可以获取间隔天数</returns>
    public static bool TryDaysBetween(DayOfWeek start, DayOfWeek end, out int days)
    {
        days = DaysBetween(start, end);
        return true;
    }

    /// <summary>
    /// 尝试获取两个ISO星期几之间的间隔天数
    /// </summary>
    /// <param name="start">起始ISO星期几</param>
    /// <param name="end">结束ISO星期几</param>
    /// <param name="days">间隔天数的输出参数</param>
    /// <returns>如果起始或结束星期几为<see cref="IsoDayOfWeek.None"/>，则返回false，否则返回true</returns>
    public static bool TryDaysBetween(IsoDayOfWeek start, IsoDayOfWeek end, out int days)
    {
        days = 0;
        if (start == IsoDayOfWeek.None || end == IsoDayOfWeek.None)
            return false;
        days = DaysBetween(start, end);
        return true;
    }

    /// <summary>
    /// 在给定星期几的基础上添加指定天数
    /// </summary>
    /// <param name="dayOfWeek">起始星期几</param>
    /// <param name="days">要添加的天数，可以是正数或负数</param>
    /// <returns>添加指定天数后的星期几</returns>
    public static DayOfWeek AddDays(DayOfWeek dayOfWeek, int days)
    {
        const int daysInWeek = 7;
        var result = ((int)dayOfWeek + daysInWeek + days % daysInWeek) % daysInWeek;
        return (DayOfWeek)result;
    }

    /// <summary>
    /// 获取两个星期几之间的所有星期几
    /// </summary>
    /// <param name="start">起始星期几</param>
    /// <param name="end">结束星期几</param>
    /// <param name="includeBoundary">是否包含边界的星期几，默认为true</param>
    /// <returns>从起始星期几到结束星期几之间的所有星期几</returns>
    public static IEnumerable<DayOfWeek> GetDaysBetween(DayOfWeek start, DayOfWeek end, bool includeBoundary = true)
    {
        if (includeBoundary)
            yield return start;

        if (start != end)
        {
            var current = AddDays(start, 1);
            while (current != end)
            {
                yield return current;
                current = AddDays(current, 1);
            }
        }

        if (includeBoundary && start != end)
            yield return end;
    }
}