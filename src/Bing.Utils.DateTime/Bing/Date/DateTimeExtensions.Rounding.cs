namespace Bing.Date;

/// <summary>
/// 日期时间取整扩展
/// </summary>
public static partial class DateTimeExtensions
{
    /// <summary>
    /// 将日期时间向上取整到从 <see cref="DateTime.MinValue"/> 起算的指定时间间隔边界。
    /// </summary>
    /// <param name="dateTime">要取整的日期时间。</param>
    /// <param name="interval">取整间隔，必须大于零。</param>
    /// <returns>大于或等于 <paramref name="dateTime"/> 的最近间隔边界。</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="interval"/> 小于或等于零，或向上取整结果超出 <see cref="DateTime.MaxValue"/> 时抛出。</exception>
    /// <remarks>
    /// 已位于边界的值保持不变。该方法仅按 Tick 取整，不执行时区转换，并保留原始 <see cref="DateTime.Kind"/>。
    /// </remarks>
    /// <example>
    /// <code>
    /// var value = new DateTime(2024, 1, 1, 10, 1, 1).RoundUp(TimeSpan.FromMinutes(5));
    /// // 2024-01-01 10:05:00
    /// </code>
    /// </example>
    public static DateTime RoundUp(this DateTime dateTime, TimeSpan interval)
    {
        ValidateInterval(interval);

        var remainder = dateTime.Ticks % interval.Ticks;
        if (remainder == 0)
            return dateTime;

        var ticksToAdd = interval.Ticks - remainder;
        if (dateTime.Ticks > DateTime.MaxValue.Ticks - ticksToAdd)
            throw new ArgumentOutOfRangeException(nameof(interval), "向上取整结果超出 DateTime 的有效范围。");
        return new DateTime(dateTime.Ticks + ticksToAdd, dateTime.Kind);
    }

    /// <summary>
    /// 将日期时间向下取整到从 <see cref="DateTime.MinValue"/> 起算的指定时间间隔边界。
    /// </summary>
    /// <param name="dateTime">要取整的日期时间。</param>
    /// <param name="interval">取整间隔，必须大于零。</param>
    /// <returns>小于或等于 <paramref name="dateTime"/> 的最近间隔边界。</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="interval"/> 小于或等于零时抛出。</exception>
    /// <remarks>
    /// 已位于边界的值保持不变。该方法仅按 Tick 取整，不执行时区转换，并保留原始 <see cref="DateTime.Kind"/>。
    /// </remarks>
    /// <example>
    /// <code>
    /// var value = new DateTime(2024, 1, 1, 10, 4, 59).RoundDown(TimeSpan.FromMinutes(5));
    /// // 2024-01-01 10:00:00
    /// </code>
    /// </example>
    public static DateTime RoundDown(this DateTime dateTime, TimeSpan interval)
    {
        ValidateInterval(interval);
        return new DateTime(dateTime.Ticks - dateTime.Ticks % interval.Ticks, dateTime.Kind);
    }

    /// <summary>
    /// 验证取整间隔。
    /// </summary>
    /// <param name="interval">取整间隔。</param>
    /// <exception cref="ArgumentOutOfRangeException">当间隔小于或等于零时抛出。</exception>
    private static void ValidateInterval(TimeSpan interval)
    {
        if (interval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(interval), "取整间隔必须大于零。");
    }
}