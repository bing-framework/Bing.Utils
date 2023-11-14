using Bing.Helpers;

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

    #region ToUnixTimestamp(转换为Unix时间戳)

    /// <summary>
    /// 将 <see cref="DateTime"/> 转换为Unix时间戳
    /// </summary>
    /// <param name="time">时间</param>
    /// <remarks>当前时间必须是 <see cref="TimeZoneInfo.Local"/></remarks>
    public static long ToUnixTimestamp(this DateTime time) => Time.GetUnixTimestamp(time);

    #endregion

    #region ToUniqueString(获取时间相对唯一字符串)

    /// <summary>
    /// 获取时间相对唯一字符串
    /// </summary>
    /// <param name="dateTime">时间点</param>
    /// <param name="isContainMillisecond">是否包含毫秒</param>
    public static string ToUniqueString(this DateTime dateTime, bool isContainMillisecond = false)
    {
        var sedonds = dateTime.Hour * 3600 + dateTime.Minute * 60 + dateTime.Second;
        var value = $"{dateTime:yy}{dateTime.DayOfYear}{sedonds}";
        return isContainMillisecond ? value + dateTime.ToString("fff") : value;
    }

    #endregion

    #region ToJsGetTime(将时间转换为JS时间格式)

    /// <summary>
    /// 将时间转换为JS时间格式（Date.getTime()）
    /// </summary>
    /// <param name="dateTime">时间点</param>
    /// <param name="isContainMillisecond">是否包含毫秒</param>
    public static string ToJsGetTime(this DateTime dateTime, bool isContainMillisecond = true)
    {
        var utc = dateTime.ToUniversalTime();
        var span = utc.Subtract(TimeOptions.Date1970);
        return Math.Round(isContainMillisecond ? span.TotalMilliseconds : span.TotalSeconds).ToString();
    }

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

    #region SetTime(设置时间)

    /// <summary>
    /// 设置时间，设置时间间隔
    /// </summary>
    /// <param name="date">时间</param>
    /// <param name="time">时间间隔</param>
    /// <returns>返回设置后的时间</returns>
    public static DateTime SetTime(this DateTime date, TimeSpan time) => date.Date.Add(time);

    #endregion

    #region CompareInterval(计算两个时间的间隔)

    /// <summary>
    /// 计算两个时间的间隔
    /// </summary>
    /// <param name="begin">开始时间</param>
    /// <param name="end">结束时间</param>
    /// <param name="dateFormat">间隔格式(y:年,M:月,d:天,h:小时,m:分钟,s:秒,fff:毫秒)</param>
    public static long CompareInterval(this DateTime begin, DateTime end, string dateFormat)
    {
        long interval = begin.Ticks - end.Ticks;
        DateTime dt1;
        DateTime dt2;
        switch (dateFormat)
        {
            case "fff":
                interval /= 10000;
                break;

            case "s":
                interval /= 10000000;
                break;

            case "m":
                interval /= 600000000;
                break;

            case "h":
                interval /= 36000000000;
                break;

            case "d":
                interval /= 864000000000;
                break;

            case "M":
                dt1 = (begin.CompareTo(end) >= 0) ? end : begin;
                dt2 = (begin.CompareTo(end) >= 0) ? begin : end;
                interval = -1;
                while (dt2.CompareTo(dt1) >= 0)
                {
                    interval++;
                    dt1 = dt1.AddMonths(1);
                }
                break;

            case "y":
                dt1 = (begin.CompareTo(end) >= 0) ? end : begin;
                dt2 = (begin.CompareTo(end) >= 0) ? begin : end;
                interval = -1;
                while (dt2.CompareTo(dt1) >= 0)
                {
                    interval++;
                    dt1 = dt1.AddMonths(1);
                }

                interval /= 12;
                break;
        }

        return interval;
    }

    #endregion

    #region ConvertToTimeZone(将当前时间转换为特定时区的时间)

    /// <summary>
    /// 将当前时间转换为特定时区的时间
    /// </summary>
    /// <param name="dateTime">时间</param>
    /// <param name="timeZone">时区</param>
    public static DateTime ConvertToTimeZone(this DateTime dateTime, TimeZoneInfo timeZone) => TimeZoneInfo.ConvertTime(dateTime, timeZone);

    #endregion

    #region IsBetweenTime(判断当前时间是否在指定时间段内)

    /// <summary>
    /// 判断当前时间是否在指定时间段内，格式：hh:mm:ss
    /// </summary>
    /// <param name="currentTime">当前时间</param>
    /// <param name="beginTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <returns></returns>
    public static bool IsBetweenTime(this DateTime currentTime, DateTime beginTime, DateTime endTime)
    {
        var am = beginTime.TimeOfDay;
        var pm = endTime.TimeOfDay;

        var now = currentTime.TimeOfDay;
        if (pm < am)//截止时间小于开始时间，表示跨天
        {
            if (now <= pm || now >= am)
            {
                return true;
            }
        }

        if (now >= am && now <= pm)
        {
            return true;
        }

        return false;
    }

    #endregion

    #region IsBetweenDate(判断当前时间是否在指定日期时间段内)

    /// <summary>
    /// 判断当前时间是否在指定日期时间段内，格式：yyyy-MM-dd
    /// </summary>
    /// <param name="currentDate">当前日期</param>
    /// <param name="beginDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <returns></returns>
    public static bool IsBetweenDate(this DateTime currentDate, DateTime beginDate, DateTime endDate)
    {
        var begin = beginDate.Date;
        var end = endDate.Date;
        var now = currentDate.Date;

        return now >= begin && now <= end;
    }

    #endregion

    #region IsValid(是否有效时间)

    /// <summary>
    /// 是否有效时间
    /// </summary>
    /// <param name="value">值</param>
    /// <returns></returns>
    public static bool IsValid(this DateTime value)
    {
        return (value >= TimeOptions.MinDate) && (value <= TimeOptions.MaxDate);
    }

    #endregion
}