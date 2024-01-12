namespace Bing.Date;

/// <summary>
/// 日期时间帮助类
/// </summary>
public static partial class DateTimeHelper
{
    #region BusinessDateFormat(业务时间格式化)

    /// <summary>
    /// 业务时间格式化，返回:大于60天-"yyyy-MM-dd",31~60天-1个月前，15~30天-2周前,8~14天-1周前,1~7天-x天前 ,大于1小时-x小时前,x秒前
    /// </summary>
    /// <param name="dateTime">时间</param>
    public static string BusinessDateFormat(DateTime dateTime)
    {
        var span = (DateTime.Now - dateTime).Duration();
        if (span.TotalDays > 60)
        {
            return dateTime.ToString("yyyy-MM-dd");
        }
        if (span.TotalDays > 30)
        {
            return "1个月前";
        }
        if (span.TotalDays > 14)
        {
            return "2周前";
        }
        if (span.TotalDays > 7)
        {
            return "1周前";
        }
        if (span.TotalDays > 1)
        {
            return $"{(int)Math.Floor(span.TotalDays)}天前";
        }
        if (span.TotalHours > 1)
        {
            return $"{(int)Math.Floor(span.TotalHours)}小时前";
        }
        if (span.TotalMinutes > 1)
        {
            return $"{(int)Math.Floor(span.TotalMinutes)}秒前";
        }
        return "1秒前";
    }

    /// <summary>
    /// 获取时间字符串(小于5分-刚刚、5~60分-x分钟前、1~24小时-x小时前、1~60天-x天前、yyyy-MM-dd HH:mm:ss)
    /// </summary>
    /// <param name="dt"></param>
    /// <param name="defaultFormat"></param>
    public static string BusinessDateFormat(DateTime dt, string defaultFormat = "yyyy-MM-dd HH:mm:ss")
    {
        var timeSpan = DateTime.Now - dt;
        string result;

        if (timeSpan.TotalMinutes < 5)
            result = string.Format("刚刚");
        else if (timeSpan.TotalMinutes < 60)
            result = $"{(int)timeSpan.TotalMinutes}分钟前";
        else if (timeSpan.TotalMinutes < 60 * 24)
            result = $"{(int)timeSpan.TotalHours}小时前";
        else if (timeSpan.TotalMinutes <= 60 * 24 * 7)
            result = $"{(int)timeSpan.TotalDays}天前";
        else
            result = dt.ToString(defaultFormat);

        return result;
    }

    #endregion

    #region GetWeekDay(计算当前为星期几)

    /// <summary>
    /// 根据当前日期确定当前是星期几
    /// </summary>
    /// <param name="strDate">The string date.</param>
    /// <returns>System.String.</returns>
    /// <exception cref="Exception"></exception>
    public static DayOfWeek GetWeekDay(string strDate)
    {
        try
        {
            //需要判断的时间
            DateTime dTime = Convert.ToDateTime(strDate);
            return GetWeekDay(dTime);
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    /// <summary>
    /// 根据当前日期确定当前是星期几
    /// </summary>
    /// <param name="dTime">The d time.</param>
    /// <returns>System.String.</returns>
    /// <exception cref="Exception"></exception>
    public static DayOfWeek GetWeekDay(DateTime dTime)
    {
        try
        {
            //确定星期几
            int index = (int)dTime.DayOfWeek;
            return GetWeekDay(index);
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    /// <summary>
    /// 转换星期的表示方法
    /// </summary>
    /// <param name="index">The index.</param>
    /// <returns>System.String.</returns>
    private static DayOfWeek GetWeekDay(int index)
    {
        return (DayOfWeek)index;
    }

    /// <summary>
    /// 转换星期的表示方法
    /// </summary>
    /// <param name="dayOfWeek">The index.</param>
    /// <returns>System.String.</returns>
    public static string GetChineseWeekDay(DayOfWeek dayOfWeek)
    {
        string retVal = string.Empty;

        switch (dayOfWeek)
        {
            case DayOfWeek.Sunday:
                retVal = "星期日";
                break;

            case DayOfWeek.Monday:
                retVal = "星期一";
                break;

            case DayOfWeek.Tuesday:
                retVal = "星期二";
                break;

            case DayOfWeek.Wednesday:
                retVal = "星期三";
                break;

            case DayOfWeek.Thursday:
                retVal = "星期四";
                break;

            case DayOfWeek.Friday:
                retVal = "星期五";
                break;

            case DayOfWeek.Saturday:
                retVal = "星期六";
                break;

            default:
                break;
        }

        return retVal;
    }

    #endregion

    #region GetMaxWeekOfYear(计算当前年的最大周数)

    /// <summary>
    /// 获取当前年的最大周数
    /// </summary>
    /// <param name="year">The year.</param>
    /// <returns>System.Int32.</returns>
    /// <exception cref="Exception"></exception>
    public static int GetMaxWeekOfYear(int year)
    {
        try
        {
            var tempDate = new DateTime(year, 12, 31);
            int tempDayOfWeek = (int)tempDate.DayOfWeek;
            if (tempDayOfWeek != 0)
            {
                tempDate = tempDate.Date.AddDays(-tempDayOfWeek);
            }
            return GetWeekIndex(tempDate);
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    /// <summary>
    /// 获取当前年的最大周数
    /// </summary>
    /// <param name="dTime">The d time.</param>
    /// <returns>System.Int32.</returns>
    /// <exception cref="Exception"></exception>
    public static int GetMaxWeekOfYear(DateTime dTime)
    {
        try
        {
            return GetMaxWeekOfYear(dTime.Year);
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    #endregion

    #region GetWeekIndex(计算当前是第几周)

    /// <summary>
    /// 根据时间获取当前是第几周
    /// </summary>
    /// <param name="dTime">The d time.</param>
    /// <returns>System.Int32.</returns>
    /// <exception cref="Exception"></exception>
    public static int GetWeekIndex(DateTime dTime)
    {
        //如果12月31号与下一年的1月1好在同一个星期则算下一年的第一周
        try
        {
            //确定此时间在一年中的位置
            int dayOfYear = dTime.DayOfYear;

            //当年第一天
            var tempDate = new DateTime(dTime.Year, 1, 1);

            //确定当年第一天
            int tempDayOfWeek = (int)tempDate.DayOfWeek;
            tempDayOfWeek = tempDayOfWeek == 0 ? 7 : tempDayOfWeek;

            //确定星期几
            int index = (int)dTime.DayOfWeek;
            index = index == 0 ? 7 : index;

            //当前周的范围
            var retStartDay = dTime.AddDays(-(index - 1));
            var retEndDay = dTime.AddDays(7 - index);

            //确定当前是第几周
            int weekIndex = (int)Math.Ceiling(((double)dayOfYear + tempDayOfWeek - 1) / 7);

            if (retStartDay.Year < retEndDay.Year)
            {
                weekIndex = 1;
            }

            return weekIndex;
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    /// <summary>
    /// 根据时间获取当前是第几周
    /// </summary>
    /// <param name="strDate">The string date.</param>
    /// <returns>System.Int32.</returns>
    /// <exception cref="Exception"></exception>
    public static int GetWeekIndex(string strDate)
    {
        try
        {
            //需要判断的时间
            var dTime = Convert.ToDateTime(strDate);
            return GetWeekIndex(dTime);
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    #endregion

    #region GetWeekRange(计算周范围)

    /// <summary>
    /// 根据时间取周的日期范围
    /// </summary>
    /// <param name="strDate">The string date.</param>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <returns>System.String.</returns>
    /// <exception cref="Exception"></exception>
    public static void GetWeekRange(string strDate, out DateTime startDate, out DateTime endDate)
    {
        try
        {
            //需要判断的时间
            var dTime = Convert.ToDateTime(strDate);
            GetWeekRange(dTime, out startDate, out endDate);
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    /// <summary>
    /// 根据时间取周的日期范围
    /// </summary>
    /// <param name="dTime">The d time.</param>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <returns>System.String.</returns>
    /// <exception cref="Exception"></exception>
    public static void GetWeekRange(DateTime dTime, out DateTime startDate, out DateTime endDate)
    {
        try
        {
            int index = (int)dTime.DayOfWeek;
            index = index == 0 ? 7 : index;

            startDate = dTime.AddDays(-(index - 1));
            endDate = dTime.AddDays(7 - index);
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    /// <summary>
    /// 根据时间取周的日期范围
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="weekIndex">Index of the week.</param>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <returns>System.String.</returns>
    /// <exception cref="Exception">
    /// 请输入大于0的整数
    /// or
    /// 今年没有第 + weekIndex + 周。
    /// or
    /// </exception>
    public static void GetWeekRange(int year, int weekIndex, out DateTime startDate, out DateTime endDate)
    {
        if (weekIndex < 1)
        {
            throw new Exception("请输入大于0的整数");
        }

        int allDays = (weekIndex - 1) * 7;

        //确定当年第一天
        var firstDate = new DateTime(year, 1, 1);
        int firstDayOfWeek = (int)firstDate.DayOfWeek;
        firstDayOfWeek = firstDayOfWeek == 0 ? 7 : firstDayOfWeek;

        //周开始日
        int startAddDays = allDays + (1 - firstDayOfWeek);
        var weekRangeStart = firstDate.AddDays(startAddDays);

        //周结束日
        int endAddDays = allDays + (7 - firstDayOfWeek);
        var weekRangeEnd = firstDate.AddDays(endAddDays);

        if (weekRangeStart.Year > year ||
            weekRangeStart.Year == year && weekRangeEnd.Year > year)
        {
            throw new Exception("今年没有第" + weekIndex + "周。");
        }

        startDate = weekRangeStart;
        endDate = weekRangeEnd;
    }

    /// <summary>
    /// 根据时间取周的日期范围
    /// </summary>
    /// <param name="weekIndex">Index of the week.</param>
    /// <param name="startDate">输出开始日期</param>
    /// <param name="endDate">输出结束日期</param>
    /// <returns>System.String.</returns>
    /// <exception cref="Exception"></exception>
    public static void GetWeekRange(int weekIndex, out DateTime startDate, out DateTime endDate)
    {
        try
        {
            GetWeekRange(DateTime.Now.Year, weekIndex, out startDate, out endDate);
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    #endregion

    #region GetDateRange(计算当前时间范围)

    /// <summary>
    /// 获取当前的时间范围
    /// </summary>
    /// <param name="range"></param>
    /// <param name="startDate"></param>
    /// <param name="endDate"></param>
    public static void GetDateRange(DateRangeEnum range, out DateTime startDate, out DateTime endDate)
    {
        GetDateRange(DateTime.Now, range, out startDate, out endDate);
    }

    /// <summary>
    /// 获取当前时间范围
    /// </summary>
    /// <param name="date">当前日期</param>
    /// <param name="range">日期范围</param>
    /// <param name="startDate">输出开始日期</param>
    /// <param name="endDate">输出结束日期</param>
    public static void GetDateRange(DateTime date, DateRangeEnum range, out DateTime startDate, out DateTime endDate)
    {
        switch (range)
        {
            case DateRangeEnum.Week:

                startDate = date.AddDays(-(int)date.DayOfWeek).Date;
                endDate = date.AddDays(6 - (int)date.DayOfWeek + 1).Date.AddSeconds(-1);
                break;

            case DateRangeEnum.Month:
                startDate = new DateTime(date.Year, date.Month, 1);
                endDate = startDate.AddMonths(1).Date.AddSeconds(-1);
                break;

            case DateRangeEnum.Quarter:
                if (date.Month <= 3)
                {
                    startDate = new DateTime(date.Year, 1, 1);
                }
                else if (date.Month <= 6)
                {
                    startDate = new DateTime(date.Year, 4, 1);
                }
                else if (date.Month <= 9)
                {
                    startDate = new DateTime(date.Year, 7, 1);
                }
                else
                {
                    startDate = new DateTime(date.Year, 10, 1);
                }
                endDate = startDate.AddMonths(3).AddSeconds(-1);
                break;

            case DateRangeEnum.HalfYear:
                if (date.Month <= 6)
                {
                    startDate = new DateTime(date.Year, 1, 1);
                }
                else
                {
                    startDate = new DateTime(date.Year, 7, 1);
                }
                endDate = startDate.AddMonths(6).AddSeconds(-1);
                break;

            case DateRangeEnum.Year:
                startDate = new DateTime(date.Year, 1, 1);
                endDate = startDate.AddYears(1).AddSeconds(-1);
                break;

            default:
                startDate = DateTime.MinValue;
                endDate = DateTime.MinValue;
                break;
        }
    }

    #endregion

    #region FormatTime(格式化时间)

    /// <summary>
    /// 格式化毫秒数为可读的时间格式，包括天、小时、分钟、秒和毫秒。
    /// </summary>
    /// <param name="ms">毫秒数</param>
    /// <param name="isContainMillisecond">是否包含毫秒</param>
    /// <returns>格式化后的时间字符串</returns>
    public static string FormatTime(long ms, bool isContainMillisecond = false)
    {
        const int millisecondsPerSecond = 1000;
        const int secondsPerMinute = 60;
        const int minutesPerHour = 60;
        const int hoursPerDay = 24;

        var totalSeconds = ms / millisecondsPerSecond;

        var days = totalSeconds / (secondsPerMinute * minutesPerHour * hoursPerDay);
        var hours = (totalSeconds % (secondsPerMinute * minutesPerHour * hoursPerDay)) / (secondsPerMinute * minutesPerHour);
        var minutes = (totalSeconds % (secondsPerMinute * minutesPerHour)) / secondsPerMinute;
        var seconds = (totalSeconds % secondsPerMinute);
        var milliseconds = ms % millisecondsPerSecond;

        var formattedTime = $"{days:D2} 天 {hours:D2} 小时 {minutes:D2} 分 {seconds:D2} 秒";

        if (isContainMillisecond)
            formattedTime += $" {milliseconds:D3} 毫秒";

        return formattedTime;
    }

    #endregion

    #region ConvertDateTimeToUnixTime(将DateTime转换为Unix时间戳)

    /// <summary>
    /// 将 <see cref="DateTime"/> 转换为 Unix 时间戳。
    /// </summary>
    /// <param name="dateTime">要转换的日期时间。</param>
    /// <param name="digit">时间戳的精度（秒或毫秒）。</param>
    /// <returns>Unix 时间戳。</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static long ConvertDateTimeToUnixTime(DateTime dateTime, TimestampDigit digit = TimestampDigit.Millisecond)
    {
        var dateTimeUtc = dateTime.Kind != DateTimeKind.Utc ? dateTime.ToUniversalTime() : dateTime;
        if (dateTimeUtc <= GetUnixEpoch())
            return 0;
        return digit switch
        {
            TimestampDigit.Second => new DateTimeOffset(dateTimeUtc).ToUnixTimeSeconds(),
            TimestampDigit.Millisecond => new DateTimeOffset(dateTimeUtc).ToUnixTimeMilliseconds(),
            _ => throw new ArgumentOutOfRangeException(nameof(digit), @"不支持的时间戳精度"),
        };
    }

    /// <summary>
    /// 获取 Unix纪元
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static DateTime GetUnixEpoch()
    {
#if NET6_0_OR_GREATER
        return DateTime.UnixEpoch;
#else
        return TimeOptions.UnixEpoch;
#endif
    }

    #endregion

    #region ConvertUnixTimestampToDateTime(将Unix时间戳转换为本地时间)

    /// <summary>
    /// 将 Unix 时间戳转换为本地时间。
    /// </summary>
    /// <param name="unixTimestamp">Unix 时间戳。</param>
    /// <param name="digit">时间戳的精度。</param>
    /// <returns>转换后的本地时间。</returns>
    public static DateTime ConvertUnixTimestampToDateTime(long unixTimestamp, TimestampDigit digit = TimestampDigit.Millisecond)
    {
        // 从 Unix 时间戳创建 DateTime 对象。
        var convertedTime = digit switch
        {
            TimestampDigit.Second => GetUnixEpoch().AddSeconds(unixTimestamp),
            TimestampDigit.Millisecond => GetUnixEpoch().AddMilliseconds(unixTimestamp),
            _ => GetUnixEpoch().AddMilliseconds(unixTimestamp),
        };

        // 转换为本地时间（GMT+8时区）。
        return TimeZoneInfo.ConvertTimeFromUtc(convertedTime, TimeOptions.GMT8);
    }

    #endregion

    #region Current(获取当前时间的时间戳)

    /// <summary>
    /// 获取当前时间的时间戳
    /// </summary>
    /// <returns>当前时间毫秒数</returns>
    public static long Current() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    /// <summary>
    /// 获取当前时间的时间戳（秒）
    /// </summary>
    /// <returns>当前时间秒数</returns>
    public static long CurrentSeconds() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    #endregion
}

/// <summary>
/// 时间戳精度
/// </summary>
public enum TimestampDigit
{
    /// <summary>
    /// 无
    /// </summary>
    None = 0,

    /// <summary>
    /// 精确到 秒。返回时间戳长度为：10
    /// </summary>
    Second = 16,

    /// <summary>
    /// 精确到 毫秒。返回时间戳长度为：13
    /// </summary>
    Millisecond = 32,
}