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
        Validate(start, nameof(start));
        Validate(end, nameof(end));
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
        if ((uint)start > 7)
            throw new ArgumentOutOfRangeException(nameof(start));
        if ((uint)end > 7)
            throw new ArgumentOutOfRangeException(nameof(end));
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
    /// <returns>星期合法时返回 true；非法枚举返回 false</returns>
    public static bool TryDaysBetween(DayOfWeek start, DayOfWeek end, out int days)
    {
        days = 0;
        if ((uint)start > 6 || (uint)end > 6)
            return false;
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
        if ((int)start < 1 || (int)start > 7 || (int)end < 1 || (int)end > 7)
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
        Validate(dayOfWeek, nameof(dayOfWeek));
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
        var distance = DaysBetween(start, end); // 在创建迭代器之前验证，非法输入立即失败。
        return EnumerateDays(start, distance, includeBoundary);
    }

    private static IEnumerable<DayOfWeek> EnumerateDays(DayOfWeek start, int distance, bool includeBoundary)
    {
        var first = includeBoundary ? 0 : 1;
        var last = includeBoundary ? distance : distance - 1;
        for (var i = first; i <= last; i++)
            yield return (DayOfWeek)(((int)start + i) % 7);
    }

    /// <summary>
    /// 验证星期枚举，并以调用方的公开参数名报告越界。
    /// </summary>
    /// <param name="value">待验证的星期。</param>
    /// <param name="parameterName">公开入口的参数名。</param>
    /// <exception cref="ArgumentOutOfRangeException">星期不在 0 到 6 范围内。</exception>
    internal static void Validate(DayOfWeek value, string parameterName)
    {
        if ((uint)value > 6)
            throw new ArgumentOutOfRangeException(parameterName, value, "星期必须在 Sunday 到 Saturday 之间。");
    }
}
