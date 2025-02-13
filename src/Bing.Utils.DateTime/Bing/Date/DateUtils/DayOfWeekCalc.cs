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
    /// 计算两个星期几之间的间隔天数。
    /// </summary>
    /// <param name="left">起始星期几。</param>
    /// <param name="right">结束星期几。</param>
    /// <returns>返回从起始星期几到结束星期几之间的间隔天数。如果起始星期大于结束星期，则认为跨周计算。</returns>
    public static int DaysBetween(DayOfWeek left, DayOfWeek right)
    {
        var leftVal = left.CastToInt32();
        var rightVal = right.CastToInt32();

        if (leftVal <= rightVal)
            return rightVal - leftVal;
        return leftVal + 7 - rightVal;
    }

    /// <summary>
    /// 计算两个ISO星期几之间的间隔天数。
    /// </summary>
    /// <param name="left">起始ISO星期几。</param>
    /// <param name="right">结束ISO星期几。</param>
    /// <returns>返回从起始ISO星期几到结束ISO星期几之间的间隔天数。如果起始星期大于结束星期，则认为跨周计算。如果任一参数为<see cref="IsoDayOfWeek.None"/>，则返回0。</returns>
    public static int DaysBetween(IsoDayOfWeek left, IsoDayOfWeek right)
    {
        if (left == IsoDayOfWeek.None || right == IsoDayOfWeek.None)
            return 0;
        return DaysBetween(DayOfWeekHelper.ToSystemWeek(left), DayOfWeekHelper.ToSystemWeek(right));
    }

    /// <summary>
    /// 尝试获取两个星期几之间的间隔天数。
    /// </summary>
    /// <param name="left">起始星期几。</param>
    /// <param name="right">结束星期几。</param>
    /// <param name="days">间隔天数的输出参数。</param>
    /// <returns>始终返回true，表示可以获取间隔天数。</returns>
    public static bool TryDaysBetween(DayOfWeek left, DayOfWeek right, out int days)
    {
        days = DaysBetween(left, right);
        return true;
    }

    /// <summary>
    /// 尝试获取两个ISO星期几之间的间隔天数。
    /// </summary>
    /// <param name="left">起始ISO星期几。</param>
    /// <param name="right">结束ISO星期几。</param>
    /// <param name="days">间隔天数的输出参数。</param>
    /// <returns>如果起始或结束星期几为<see cref="IsoDayOfWeek.None"/>，则返回false，否则返回true。</returns>
    public static bool TryDaysBetween(IsoDayOfWeek left, IsoDayOfWeek right, out int days)
    {
        days = 0;
        if (left == IsoDayOfWeek.None || right == IsoDayOfWeek.None)
            return false;
        return TryDaysBetween(DayOfWeekHelper.ToSystemWeek(left), DayOfWeekHelper.ToSystemWeek(right), out days);
    }

    /// <summary>
    /// 给定的星期几基础上添加指定的天数。
    /// </summary>
    /// <param name="dayOfWeek">起始星期几。</param>
    /// <param name="days">要添加的天数，可以是正数或负数。</param>
    /// <returns>添加指定天数后的星期几。</returns>
    public static DayOfWeek AddDays(DayOfWeek dayOfWeek, int days)
    {
        var numberOfDays = 7;
        var result = (dayOfWeek.CastToInt32(0) + numberOfDays + days % numberOfDays) % numberOfDays;
        return (DayOfWeek)result;
    }

    /// <summary>
    /// 获取两个星期几之间的所有星期几。
    /// </summary>
    /// <param name="from">起始星期几。</param>
    /// <param name="to">结束星期几。</param>
    /// <param name="includeBoundary">是否包含边界的星期几，默认为true。</param>
    /// <returns>从起始星期几到结束星期几之间的所有星期几，根据includeBoundary参数决定是否包含起始和结束星期几。</returns>
    public static IEnumerable<DayOfWeek> GetDaysBetween(DayOfWeek from, DayOfWeek to, bool includeBoundary = true)
    {
        if (includeBoundary)
            yield return from;
        if (from != to)
        {
            var next = AddDays(from, 1);
            while (next != to)
            {
                yield return next;
                next = AddDays(next, 1);
            }
        }

        if (includeBoundary && from != to)
            yield return to;
    }
}