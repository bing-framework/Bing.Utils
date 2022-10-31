using System;

namespace Bing.IdUtils.CombImplements.Strategies;

/// <summary>
/// COMB 日期时间策略
/// </summary>
internal interface ICombDateTimeStrategy
{
    /// <summary>
    /// 日期字节数
    /// </summary>
    int NumDateBytes { get; }

    /// <summary>
    /// 最小日期时间
    /// </summary>
    DateTime MinDateTimeValue { get; }

    /// <summary>
    /// 最大日期时间
    /// </summary>
    DateTime MaxDateTimeValue { get; }

    /// <summary>
    /// 将 <see cref="DateTime"/> 转换为 byte[]
    /// </summary>
    /// <param name="timestamp">时间戳</param>
    byte[] DateTimeToBytes(DateTime timestamp);

    /// <summary>
    /// 将 byte[] 转换为 <see cref="DateTime"/>
    /// </summary>
    /// <param name="value">字节数组</param>
    DateTime BytesToDateTime(byte[] value);
}