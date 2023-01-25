

// ReSharper disable once CheckNamespace
namespace Bing.Date;

/// <summary>
/// 时间(<see cref="DateTime"/>) 扩展
/// </summary>
public static partial class DateTimeExtensions
{
    #region ToDateTimeString(yyyy-MM-dd HH:mm:ss)

    /// <summary>
    /// 获取格式化字符串，带时分秒，格式："yyyy-MM-dd HH:mm:ss"
    /// </summary>
    /// <param name="dateTime">日期</param>
    /// <param name="isRemoveSecond">是否移除秒,true:是,false:否</param>
    public static string ToDateTimeString(this in DateTime dateTime, bool isRemoveSecond = false) => dateTime.ToString(isRemoveSecond ? "yyyy-MM-dd HH:mm" : "yyyy-MM-dd HH:mm:ss");

    /// <summary>
    /// 获取格式化字符串，带时分秒，格式："yyyy-MM-dd HH:mm:ss"
    /// </summary>
    /// <param name="dateTime">日期</param>
    /// <param name="isRemoveSecond">是否移除秒,true:是,false:否</param>
    public static string ToDateTimeString(this in DateTime? dateTime, bool isRemoveSecond = false) => dateTime == null ? string.Empty : ToDateTimeString(dateTime.Value, isRemoveSecond);

    #endregion

    #region ToDateString(yyyy-MM-dd)

    /// <summary>
    /// 获取格式化字符串，不带时分秒，格式："yyyy-MM-dd"
    /// </summary>
    /// <param name="dateTime">日期</param>
    public static string ToDateString(this in DateTime dateTime) => dateTime.ToString("yyyy-MM-dd");

    /// <summary>
    /// 获取格式化字符串，不带时分秒，格式："yyyy-MM-dd"
    /// </summary>
    /// <param name="dateTime">日期</param>
    public static string ToDateString(this in DateTime? dateTime) => dateTime == null ? string.Empty : ToDateString(dateTime.Value);

    #endregion

    #region ToTimeString(HH:mm:ss)

    /// <summary>
    /// 获取格式化字符串，不带年月日，格式："HH:mm:ss"
    /// </summary>
    /// <param name="dateTime">日期</param>
    public static string ToTimeString(this in DateTime dateTime) => dateTime.ToString("HH:mm:ss");

    /// <summary>
    /// 获取格式化字符串，不带年月日，格式："HH:mm:ss"
    /// </summary>
    /// <param name="dateTime">日期</param>
    public static string ToTimeString(this in DateTime? dateTime) => dateTime == null ? string.Empty : ToTimeString(dateTime.Value);

    #endregion

    #region ToMillisecondString(yyyy-MM-dd HH:mm:ss.fff)

    /// <summary>
    /// 获取格式化字符串，带毫秒，格式："yyyy-MM-dd HH:mm:ss.fff"
    /// </summary>
    /// <param name="dateTime">日期</param>
    public static string ToMillisecondString(this in DateTime dateTime) => dateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");

    /// <summary>
    /// 获取格式化字符串，带毫秒，格式："yyyy-MM-dd HH:mm:ss.fff"
    /// </summary>
    /// <param name="dateTime">日期</param>
    public static string ToMillisecondString(this in DateTime? dateTime) => dateTime == null ? string.Empty : ToMillisecondString(dateTime.Value);

    #endregion

    #region ToChineseDateString(yyyy年MM月dd日)

    /// <summary>
    /// 获取格式化字符串，不带时分秒，格式："yyyy年MM月dd日"
    /// </summary>
    /// <param name="dateTime">日期</param>
    public static string ToChineseDateString(this in DateTime dateTime) => $"{dateTime.Year}年{dateTime.Month}月{dateTime.Day}日";

    /// <summary>
    /// 获取格式化字符串，不带时分秒，格式："yyyy年MM月dd日"
    /// </summary>
    /// <param name="dateTime">日期</param>
    public static string ToChineseDateString(this in DateTime? dateTime) => dateTime == null ? string.Empty : ToChineseDateString(dateTime.Value);

    #endregion

    #region ToChineseDateTimeString(yyyy年MM月dd日 HH时mm分)

    /// <summary>
    /// 获取格式化字符串，带时分秒，格式："yyyy年MM月dd日 HH时mm分"
    /// </summary>
    /// <param name="dateTime">日期</param>
    /// <param name="isRemoveSecond">是否移除秒</param>
    public static string ToChineseDateTimeString(this in DateTime dateTime, bool isRemoveSecond = false)
    {
        var result = new StringBuilder();
        result.AppendFormat("{0}年{1}月{2}日", dateTime.Year, dateTime.Month, dateTime.Day);
        result.AppendFormat(" {0}时{1}分", dateTime.Hour, dateTime.Minute);
        if (isRemoveSecond == false)
            result.AppendFormat("{0}秒", dateTime.Second);
        return result.ToString();
    }

    /// <summary>
    /// 获取格式化字符串，带时分秒，格式："yyyy年MM月dd日 HH时mm分"
    /// </summary>
    /// <param name="dateTime">日期</param>
    /// <param name="isRemoveSecond">是否移除秒</param>
    public static string ToChineseDateTimeString(this in DateTime? dateTime, bool isRemoveSecond = false) => dateTime == null ? string.Empty : ToChineseDateTimeString(dateTime.Value, isRemoveSecond);

    #endregion

    #region In(判断时间是否在区间内)

    /// <summary>
    /// 判断时间是否在区间内
    /// </summary>
    /// <param name="this"></param>
    /// <param name="start">开始时间</param>
    /// <param name="end">结束时间</param>
    /// <param name="mode">区间模式</param>
    public static bool In(this in DateTime @this, DateTime start, DateTime end, RangeMode mode = RangeMode.Close)
    {
        return mode switch
        {
            RangeMode.Open => start < @this && end > @this,
            RangeMode.Close => start <= @this && end >= @this,
            RangeMode.OpenClose => start < @this && end >= @this,
            RangeMode.CloseOpen => start <= @this && end > @this,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }

    #endregion
}