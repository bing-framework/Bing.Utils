namespace Bing.Conversions;

/// <summary>
/// <see cref="TimeSpan"/> 转换器
/// </summary>
public static class TimeSpanConv
{
    /// <summary>
    /// 将 <see cref="TimeSpan"/> 转换为 <see cref="DateTime"/>
    /// </summary>
    /// <param name="time">需要转换的 <see cref="TimeSpan"/></param>
    /// <returns>转换后的 <see cref="DateTime"/></returns>
    /// <exception cref="ArgumentOutOfRangeException">当 TimeSpan 的 Ticks 属性小于 0 或大于 3155378975999999999 时抛出此异常</exception>
    public static DateTime ToDateTime(TimeSpan time)
    {
        var ticks = time.Ticks;
        if (ticks is < 0 or > 3155378975999999999)
            throw new ArgumentOutOfRangeException(nameof(time));
        return new(ticks);
    }
}

/// <summary>
/// <see cref="TimeSpan"/> 转换器扩展
/// </summary>
public static class TimeSpanConvExtensions
{
    /// <summary>
    /// 将 <see cref="TimeSpan"/> 转换为 <see cref="DateTime"/>
    /// </summary>
    /// <param name="time">需要转换的 <see cref="TimeSpan"/></param>
    /// <returns>转换后的 <see cref="DateTime"/></returns>
    public static DateTime CastToDateTime(this TimeSpan time) => TimeSpanConv.ToDateTime(time);
}