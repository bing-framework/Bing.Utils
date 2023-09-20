using Bing.Date;

namespace Bing.Utils.Timing;

/// <summary>
/// 日期时间辅助扩展操作
/// </summary>
public static class DateTimeExtensions
{
    #region ToUniqueString(获取时间相对唯一字符串)

    /// <summary>
    /// 获取时间相对唯一字符串
    /// </summary>
    /// <param name="dateTime">时间点</param>
    /// <param name="milsec">是否使用毫秒</param>
    /// <returns></returns>
    public static string ToUniqueString(this DateTime dateTime, bool milsec = false)
    {
        int sedonds = dateTime.Hour * 3600 + dateTime.Minute * 60 + dateTime.Second;
        string value = string.Format("{0}{1}{2}", dateTime.ToString("yy"), dateTime.DayOfWeek, sedonds);
        return milsec ? value + dateTime.ToString("fff") : value;
    }

    #endregion

    #region ToJsGetTime(将时间转换为Js时间格式)

    /// <summary>
    /// 将时间转换为Js时间格式（Date.getTiem()）
    /// </summary>
    /// <param name="dateTime">时间点</param>
    public static string ToJsGetTime(this DateTime dateTime, bool milsec = true)
    {
        DateTime utc = dateTime.ToUniversalTime();
        TimeSpan span = utc.Subtract(new DateTime(1970, 1, 1));
        return Math.Round(milsec ? span.TotalMilliseconds : span.TotalSeconds).ToString();
    }

    #endregion

    #region SetTime(设置时间)

    /// <summary>
    /// 设置时间，设置时间间隔
    /// </summary>
    /// <param name="date">时间</param>
    /// <param name="time">时间间隔</param>
    /// <returns>返回设置后的时间</returns>
    public static DateTime SetTime(this DateTime date, TimeSpan time)
    {
        return date.Date.Add(time);
    }

    #endregion

    #region GetMillisecondsSince1970(获取当前毫秒数)

    /// <summary>
    /// 获取当前毫秒数，毫秒数=1970年1月1日-当前时间，UNIX
    /// </summary>
    /// <param name="datetime">当前时间</param>
    /// <returns>毫秒数</returns>
    public static long GetMillisecondsSince1970(this DateTime datetime)
    {
        var ts = datetime.Subtract(TimeOptions.Date1970);
        return (long)ts.TotalMilliseconds;
    }

    #endregion

    #region CompareInterval(计算两个时间的间隔)

    /// <summary>
    /// 计算两个时间的间隔
    /// </summary>
    /// <param name="begin">开始时间</param>
    /// <param name="end">结束时间</param>
    /// <param name="dateFormat">间隔格式(y:年,M:月,d:天,h:小时,m:分钟,s:秒,fff:毫秒)</param>
    /// <returns></returns>
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

    #region ToTimeStamp(将时间转换为时间戳)

    /// <summary>
    /// 将时间转换为时间戳
    /// </summary>
    /// <param name="time">时间</param>
    /// <returns></returns>
    public static int ToTimeStamp(this DateTime time)
    {
        return (int)(time.ToUniversalTime().Ticks / 10000000 - 62135596800);
    }

    #endregion

    #region CsharpTime2JavascriptTime(将C#时间转换为Javascript时间)

    /// <summary>
    /// 将C#时间转换为Javascript时间
    /// </summary>
    /// <param name="dateTime">时间</param>
    /// <returns></returns>
    public static long CsharpTime2JavascriptTime(this DateTime dateTime)
    {
        return (long)new TimeSpan(dateTime.Ticks - TimeOptions.Date1970.Ticks).TotalMilliseconds;
    }

    #endregion

    #region PhpTime2CsharpTime(将PHP时间转换为C#时间)

    /// <summary>
    /// 将PHP时间转换为C#时间
    /// </summary>
    /// <param name="dateTime">时间</param>
    /// <param name="time">PHP的时间</param>
    /// <returns></returns>
    public static DateTime PhpTime2CsharpTime(this DateTime dateTime, long time)
    {
        long t = (time + 8 * 60 * 60) * 10000000 + TimeOptions.Date1970.Ticks;
        return new DateTime(t);
    }

    #endregion

    #region CsharpTime2PhpTime(将C#时间转换为PHP时间)

    /// <summary>
    /// 将C#时间转换为PHP时间
    /// </summary>
    /// <param name="dateTime">时间</param>
    /// <returns></returns>
    public static long CsharpTime2PhpTime(this DateTime dateTime)
    {
        return (DateTime.UtcNow.Ticks - TimeOptions.Date1970.Ticks) / 10000000;
    }

    #endregion

    #region ConvertToTimeZone(将当前时间转换为特定时区的时间)

    /// <summary>
    /// 将当前时间转换为特定时区的时间
    /// </summary>
    /// <param name="dateTime">时间</param>
    /// <param name="timeZone">时区</param>
    /// <returns></returns>
    public static DateTime ConvertToTimeZone(this DateTime dateTime, TimeZoneInfo timeZone)
    {
        return TimeZoneInfo.ConvertTime(dateTime, timeZone);
    }

    #endregion
}