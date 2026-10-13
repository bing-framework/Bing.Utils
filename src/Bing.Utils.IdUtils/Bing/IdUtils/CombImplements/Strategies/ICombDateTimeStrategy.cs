namespace Bing.IdUtils.CombImplements.Strategies;

/// <summary>
/// COMB 日期时间策略
/// </summary>
internal interface ICombDateTimeStrategy
{
    /// <summary>
    /// 获取用于编码日期时间的字节数。
    /// </summary>
    int NumDateBytes { get; }

    /// <summary>
    /// 获取此策略可表示的最小日期时间。
    /// </summary>
    DateTime MinDateTimeValue { get; }

    /// <summary>
    /// 获取此策略可表示的最大日期时间。
    /// </summary>
    DateTime MaxDateTimeValue { get; }

    /// <summary>
    /// 将日期时间编码为字节数组。
    /// </summary>
    /// <param name="timestamp">要编码的日期时间。</param>
    /// <returns>按此策略编码的日期时间字节。</returns>
    byte[] DateTimeToBytes(DateTime timestamp);

    /// <summary>
    /// 将字节数组解码为日期时间。
    /// </summary>
    /// <param name="value">按此策略编码的日期时间字节。</param>
    /// <returns>解码得到的日期时间。</returns>
    DateTime BytesToDateTime(byte[] value);
}
