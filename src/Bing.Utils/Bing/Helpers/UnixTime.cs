﻿namespace Bing.Helpers;

/// <summary>
/// Unix时间操作
/// </summary>
public static partial class UnixTime
{
    /// <summary>
    /// Unix纪元时间
    /// </summary>
    public static DateTime EpochTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// 转换为Unix时间戳
    /// </summary>
    /// <param name="isContainMillisecond">是否包含毫秒</param>
    public static long ToTimestamp(bool isContainMillisecond = true) => ToTimestamp(DateTime.Now, isContainMillisecond);

    /// <summary>
    /// 转换为Unix时间戳
    /// </summary>
    /// <param name="dateTime">时间</param>
    /// <param name="isContainMillisecond">是否包含毫秒</param>
    public static long ToTimestamp(DateTime dateTime, bool isContainMillisecond = true) =>
        dateTime.Kind == DateTimeKind.Utc
            ? Convert.ToInt64((dateTime - EpochTime).TotalMilliseconds / (isContainMillisecond ? 1 : 1000))
            : Convert.ToInt64((TimeZoneInfo.ConvertTimeToUtc(dateTime) - EpochTime).TotalMilliseconds /
                              (isContainMillisecond ? 1 : 1000));

    /// <summary>
    /// 转换为DateTime对象
    /// </summary>
    /// <param name="timestamp">时间戳</param>
    public static DateTime ToDateTime(long timestamp) => EpochTime.AddMilliseconds(timestamp).ToLocalTime();
}