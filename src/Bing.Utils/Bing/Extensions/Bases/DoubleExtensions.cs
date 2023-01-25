﻿

// ReSharper disable once CheckNamespace
namespace Bing.Extensions;

/// <summary>
/// 双精度浮点型(<see cref="Double"/>) 扩展
/// </summary>
public static class DoubleExtensions
{
    #region InRange(判断值是否在指定范围内)

    /// <summary>
    /// 判断当前值是否在指定范围内
    /// </summary>
    /// <param name="value">double</param>
    /// <param name="minValue">最小值</param>
    /// <param name="maxValue">最大值</param>
    public static bool InRange(this double value, double minValue, double maxValue) => value >= minValue && value <= maxValue;

    /// <summary>
    /// 判断值是否在指定范围内，否则返回默认值
    /// </summary>
    /// <param name="value">double</param>
    /// <param name="minValue">最小值</param>
    /// <param name="maxValue">最大值</param>
    /// <param name="defaultValue">默认值</param>
    public static double InRange(this double value, double minValue, double maxValue, double defaultValue) => value.InRange(minValue, maxValue) ? value : defaultValue;

    #endregion

    #region Days(获取日期间隔)

    /// <summary>
    /// 获取日期间隔，根据数值获取时间间隔
    /// </summary>
    /// <param name="days">double</param>
    public static TimeSpan Days(this double days) => TimeSpan.FromDays(days);

    #endregion

    #region Hours(获取小时间隔)

    /// <summary>
    /// 获取小时间隔，根据数值获取时间间隔
    /// </summary>
    /// <param name="hours">double</param>
    public static TimeSpan Hours(this double hours) => TimeSpan.FromHours(hours);

    #endregion

    #region Minutes(获取分钟间隔)

    /// <summary>
    /// 获取分钟间隔，根据数值获取时间间隔
    /// </summary>
    /// <param name="minutes">double</param>
    public static TimeSpan Minutes(this double minutes) => TimeSpan.FromMinutes(minutes);

    #endregion

    #region Seconds(获取秒间隔)

    /// <summary>
    /// 获取秒间隔，根据数值获取时间间隔
    /// </summary>
    /// <param name="seconds">double</param>
    public static TimeSpan Seconds(this double seconds) => TimeSpan.FromSeconds(seconds);

    #endregion

    #region Milliseconds(获取毫秒间隔)

    /// <summary>
    /// 获取毫秒间隔，根据数值获取时间间隔
    /// </summary>
    /// <param name="milliseconds">double</param>
    public static TimeSpan Milliseconds(this double milliseconds) => TimeSpan.FromMilliseconds(milliseconds);

    #endregion

    #region Round(精确小数位数)

    /// <summary>
    /// 精确小数位数
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="length">小数点后位数</param>
    /// <param name="multiple">倍数。如果是百分制则乘以100</param>
    public static double Round(this double value, int length, int multiple = 1) => Math.Round(value * multiple, length);

    #endregion

    #region FixValue(修复当被除数为0时的异常)

    /// <summary>
    /// 修复double类型当被除数为0时的异常
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="defValue">默认值</param>
    public static double FixValue(this double value, double defValue = 0D)
    {
        if (double.IsNaN(value))
            return defValue;
        if (double.IsInfinity(value))
            return defValue;
        return value;
    }

    #endregion
}