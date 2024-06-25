using System.Diagnostics;

namespace Bing.Date;

/// <summary>
/// 时间范围
/// </summary>
public interface IDateTimeRange
{
    /// <summary>
    /// 获取或设置 起始时间
    /// </summary>
    DateTime StartTime { get; }

    /// <summary>
    /// 获取或设置 结束时间
    /// </summary>
    DateTime EndTime { get; }
}

/// <summary>
/// 时间范围
/// </summary>
[Serializable]
[DebuggerDisplay("{StartTime} {DefaultSeparator} {EndTime}")]
public class DateTimeRange : IDateTimeRange, IEquatable<DateTimeRange>, IComparable<DateTimeRange>
{
    #region 常量

    /// <summary>
    /// 空实例
    /// </summary>
    public static readonly DateTimeRange Empty = new(DateTime.MinValue, DateTime.MaxValue);

    /// <summary>
    /// 默认分隔符
    /// </summary>
    public const string DefaultSeparator = " - ";

    #endregion

    #region 字段

    /// <summary>
    /// 当前时间
    /// </summary>
    private static DateTime Now => DateTime.Now;

    /// <summary>
    /// 周列表
    /// </summary>
    private static DayOfWeek[] Weeks =>
    [
        DayOfWeek.Sunday,
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday,
        DayOfWeek.Saturday
    ];

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化一个<see cref="DateTimeRange"/>类型的实例
    /// </summary>
    public DateTimeRange() : this(DateTime.MinValue, DateTime.MaxValue)
    {
    }

    /// <summary>
    /// 初始化一个<see cref="DateTimeRange"/>类型的实例
    /// </summary>
    /// <param name="start">起始时间</param>
    /// <param name="end">结束时间</param>
    public DateTimeRange(DateTime start, DateTime end)
    {
        StartTime = start < end ? start : end;
        UtcStartTime = StartTime != DateTime.MinValue ? StartTime.ToUniversalTime() : StartTime;
        EndTime = end > start ? end : start;
        UtcEndTime = EndTime != DateTime.MaxValue ? EndTime.ToUniversalTime() : EndTime;
    }

    /// <summary>
    /// 初始化一个<see cref="DateTimeRange"/>类型的实例
    /// </summary>
    /// <param name="start">起始时间</param>
    /// <param name="end">结束时间</param>
    public DateTimeRange(DateTimeOffset start, DateTimeOffset end)
    {
        StartTime = (start = start < end ? start : end).DateTime;
        UtcStartTime = start.UtcDateTime;
        EndTime = (end = end > start ? end : start).DateTime;
        UtcEndTime = end.UtcDateTime;
    }

    /// <summary>
    /// 初始化一个<see cref="DateTimeRange"/>类型的实例
    /// </summary>
    /// <param name="dateTimeRange">事件范围</param>
    public DateTimeRange(IDateTimeRange dateTimeRange) : this(dateTimeRange.StartTime, dateTimeRange.EndTime)
    {
    }

    #endregion

    #region 属性

    /// <summary>
    /// 获取或设置 起始时间
    /// </summary>
    public DateTime StartTime { get; }

    /// <summary>
    /// 获取或设置 结束时间
    /// </summary>
    public DateTime EndTime { get; }

    /// <summary>
    /// 获取 UTC起始时间
    /// </summary>
    public DateTime UtcStartTime { get; }

    /// <summary>
    /// 获取 UTC结束时间
    /// </summary>
    public DateTime UtcEndTime { get; }

    /// <summary>
    /// 相差时间
    /// </summary>
    public TimeSpan TimeSpan => EndTime - StartTime;

    /// <summary>
    /// 总天数
    /// </summary>
    public double TotalDays => TimeSpan.TotalDays;

    /// <summary>
    /// 总小时数
    /// </summary>
    public double TotalHours => TimeSpan.TotalHours;

    /// <summary>
    /// 总分钟数
    /// </summary>
    public double TotalMinutes => TimeSpan.TotalMinutes;

    /// <summary>
    /// 总秒数
    /// </summary>
    public double TotalSeconds => TimeSpan.TotalSeconds;

    /// <summary>
    /// 总毫秒数
    /// </summary>
    public double TotalMilliseconds => TimeSpan.TotalMilliseconds;

    #endregion

    #region Yesterday(昨天时间范围)

    /// <summary>
    /// 获取 昨天的时间范围
    /// </summary>
    public static DateTimeRange Yesterday
    {
        get
        {
            var now = Now;
            return new DateTimeRange(now.Date.AddDays(-1), now.Date.AddMilliseconds(-1));
        }
    }

    #endregion

    #region Today(今天时间范围)

    /// <summary>
    /// 获取 今天的时间范围
    /// </summary>
    public static DateTimeRange Today
    {
        get
        {
            var now = Now;
            return new DateTimeRange(now.Date.Date, now.Date.AddDays(1).AddMilliseconds(-1));
        }
    }

    #endregion

    #region Tomorrow(明天时间范围)

    /// <summary>
    /// 获取 明天的时间范围
    /// </summary>
    public static DateTimeRange Tomorrow
    {
        get
        {
            var now = Now;
            return new DateTimeRange(now.Date.AddDays(1), now.Date.AddDays(2).AddMilliseconds(-1));
        }
    }

    #endregion

    #region LastWeek(上周范围)

    /// <summary>
    /// 获取 上周的时间范围
    /// </summary>
    public static DateTimeRange LastWeek
    {
        get
        {
            var now = Now;
            var index = Array.IndexOf(Weeks, now.DayOfWeek);
            return new DateTimeRange(now.Date.AddDays(-index - 7), now.Date.AddDays(-index).AddMilliseconds(-1));
        }
    }

    #endregion

    #region ThisWeek(本周时间范围)

    /// <summary>
    /// 获取 本周的时间范围
    /// </summary>
    public static DateTimeRange ThisWeek
    {
        get
        {
            var now = Now;
            var index = Array.IndexOf(Weeks, now.DayOfWeek);
            return new DateTimeRange(now.Date.AddDays(-index), now.Date.AddDays(7 - index).AddMilliseconds(-1));
        }
    }

    #endregion

    #region NextWeek(下周时间范围)

    /// <summary>
    /// 获取 下周的时间范围
    /// </summary>
    public static DateTimeRange NextWeek
    {
        get
        {
            var now = Now;
            var index = Array.IndexOf(Weeks, now.DayOfWeek);
            return new DateTimeRange(now.Date.AddDays(-index + 7), now.Date.AddDays(14 - index).AddMilliseconds(-1));
        }
    }

    #endregion

    #region LastMonth(上月时间范围)

    /// <summary>
    /// 获取 上个月的时间范围
    /// </summary>
    public static DateTimeRange LastMonth
    {
        get
        {
            var now = Now;
            var startTime = now.Date.AddDays(-now.Day + 1).AddMonths(-1);
            var endTime = startTime.AddMonths(1).AddMilliseconds(-1);
            return new DateTimeRange(startTime, endTime);
        }
    }

    #endregion

    #region ThisMonth(本月时间范围)

    /// <summary>
    /// 获取 本月的时间范围
    /// </summary>
    public static DateTimeRange ThisMonth
    {
        get
        {
            var now = Now;
            var startTime = now.Date.AddDays(-now.Day + 1);
            var endTime = startTime.AddMonths(1).AddMilliseconds(-1);
            return new DateTimeRange(startTime, endTime);
        }
    }

    #endregion

    #region NextMonth(下月时间范围)

    /// <summary>
    /// 获取 下个月的时间范围
    /// </summary>
    public static DateTimeRange NextMonth
    {
        get
        {
            var now = Now;
            var startTime = now.Date.AddDays(-now.Day + 1).AddMonths(1);
            var endTime = startTime.AddMonths(1).AddMilliseconds(-1);
            return new DateTimeRange(startTime, endTime);
        }
    }

    #endregion

    #region LastYear(去年时间范围)

    /// <summary>
    /// 获取 上一年的时间范围
    /// </summary>
    public static DateTimeRange LastYear
    {
        get
        {
            var now = Now;
            return new DateTimeRange(new DateTime(now.Year - 1, 1, 1), new DateTime(now.Year, 1, 1).AddMilliseconds(-1));
        }
    }

    #endregion

    #region ThisYear(今年时间范围)

    /// <summary>
    /// 获取 本年的时间范围
    /// </summary>
    public static DateTimeRange ThisYear
    {
        get
        {
            var now = Now;
            return new DateTimeRange(new DateTime(now.Year, 1, 1), new DateTime(now.Year + 1, 1, 1).AddMilliseconds(-1));
        }
    }

    #endregion

    #region NextYear(明年时间范围)

    /// <summary>
    /// 获取 下一年的时间范围
    /// </summary>
    public static DateTimeRange NextYear
    {
        get
        {
            var now = Now;
            return new DateTimeRange(new DateTime(now.Year + 1, 1, 1), new DateTime(now.Year + 2, 1, 1).AddMilliseconds(-1));
        }
    }

    #endregion

    #region Last7Days(过去7天时间范围)

    /// <summary>
    /// 获取 相对于当前时间过去7天的时间范围
    /// </summary>
    public static DateTimeRange Last7Days
    {
        get
        {
            var now = Now;
            return new DateTimeRange(now.AddDays(-7), now);
        }
    }

    #endregion

    #region Last30Days(过去30天时间范围)

    /// <summary>
    /// 获取 相对于当前时间过去30天的时间范围
    /// </summary>
    public static DateTimeRange Last30Days
    {
        get
        {
            var now = Now;
            return new DateTimeRange(now.AddDays(-30), now);
        }
    }

    #endregion

    #region Last7DaysExceptToday(截止昨天最近7天时间范围)

    /// <summary>
    /// 获取 截止到昨天的最近7天的天数范围
    /// </summary>
    public static DateTimeRange Last7DaysExceptToday
    {
        get
        {
            var now = Now;
            return new DateTimeRange(now.Date.AddDays(-7), now.Date.AddMilliseconds(-1));
        }
    }

    #endregion

    #region Last30DaysExceptToday(截止昨天最近30天时间范围)

    /// <summary>
    /// 获取 截止到昨天的最近30天的天数范围
    /// </summary>
    public static DateTimeRange Last30DaysExceptToday
    {
        get
        {
            var now = Now;
            return new DateTimeRange(now.Date.AddDays(-30), now.Date.AddMilliseconds(-1));
        }
    }

    #endregion

    #region GetDays(获取相差天数)

    /// <summary>
    /// 获取两个时间之间的天数
    /// </summary>
    public int GetDays() => Convert.ToInt32(EndTime.Subtract(StartTime).TotalDays);

    #endregion

    #region GetHours(获取相差小时数)

    /// <summary>
    /// 获取两个时间之间的小时数
    /// </summary>
    public int GetHours() => Convert.ToInt32(EndTime.Subtract(StartTime).TotalHours);

    #endregion

    #region GetMinutes(获取相差分钟数)

    /// <summary>
    /// 获取两个时间之间的分钟数
    /// </summary>
    public int GetMinutes() => Convert.ToInt32(EndTime.Subtract(StartTime).TotalMinutes);

    #endregion

    #region GetSeconds(获取相差秒数)

    /// <summary>
    /// 获取两个时间之间的秒数
    /// </summary>
    public int GetSeconds() => Convert.ToInt32(EndTime.Subtract(StartTime).TotalSeconds);

    #endregion

    #region GetMilliseconds(获取相差毫秒数)

    /// <summary>
    /// 获取两个时间之间的毫秒数
    /// </summary>
    public int GetMilliseconds() => Convert.ToInt32(EndTime.Subtract(StartTime).TotalMilliseconds);

    #endregion

    #region HasIntersect(是否与指定时间范围相交)

    /// <summary>
    /// 是否与指定时间范围相交
    /// </summary>
    /// <param name="range">时间范围</param>
    public bool HasIntersect(IDateTimeRange range)
    {
        return StartTime.In(range.StartTime, range.EndTime) || EndTime.In(range.StartTime, range.EndTime);
    }

    /// <summary>
    /// 是否与指定时间范围相交
    /// </summary>
    /// <param name="start">开始时间</param>
    /// <param name="end">结束时间</param>
    public bool HasIntersect(DateTime start, DateTime end) => HasIntersect(new DateTimeRange(start, end));

    #endregion

    #region Contains(是否包含指定时间范围)

    /// <summary>
    /// 判断指定的时间是否在此时间范围内
    /// </summary>
    /// <param name="time">需要判断的时间</param>
    /// <returns>如果指定的时间在此时间范围内，返回 true，否则返回 false。</returns>
    public bool Contains(DateTime time)
    {
        if (time.Kind == DateTimeKind.Utc)
            return time >= UtcStartTime && time <= UtcEndTime;
        return time >= StartTime && time <= EndTime;
    }

    /// <summary>
    /// 是否包含指定时间范围
    /// </summary>
    /// <param name="range">时间范围</param>
    public bool Contains(IDateTimeRange range)
    {
        return range.StartTime.In(StartTime, EndTime) && range.EndTime.In(StartTime, EndTime);
    }

    /// <summary>
    /// 是否包含指定时间范围
    /// </summary>
    /// <param name="start">开始时间</param>
    /// <param name="end">结束时间</param>
    public bool Contains(DateTime start, DateTime end) => Contains(new DateTimeRange(start, end));

    #endregion

    #region In(是否在指定时间范围内)

    /// <summary>
    /// 是否在指定时间范围内
    /// </summary>
    /// <param name="range">时间范围</param>
    public bool In(IDateTimeRange range)
    {
        return StartTime.In(range.StartTime, range.EndTime) && EndTime.In(range.StartTime, range.EndTime);
    }

    /// <summary>
    /// 是否在指定时间范围内
    /// </summary>
    /// <param name="start">开始时间</param>
    /// <param name="end">结束时间</param>
    public bool In(DateTime start, DateTime end) => In(new DateTimeRange(start, end));

    #endregion

    #region Intersect(获取相交时间范围)

    /// <summary>
    /// 获取相交时间范围
    /// </summary>
    /// <param name="range">时间范围</param>
    public (bool intersected, DateTimeRange range) Intersect(IDateTimeRange range)
    {
        if (HasIntersect(range.StartTime, range.EndTime))
        {
            var list = new List<DateTime> { StartTime, range.StartTime, EndTime, range.EndTime };
            list.Sort();
            return (true, new DateTimeRange(list[1], list[2]));
        }

        return (false, null);
    }

    /// <summary>
    /// 获取相交时间范围
    /// </summary>
    /// <param name="start">开始时间</param>
    /// <param name="end">结束时间</param>
    /// <returns></returns>
    public (bool intersected, DateTimeRange range) Intersect(DateTime start, DateTime end)
    {
        return Intersect(new DateTimeRange(start, end));
    }

    #endregion

    #region Union(合并时间范围)

    /// <summary>
    /// 合并时间范围
    /// </summary>
    /// <param name="range">时间范围</param>
    public DateTimeRange Union(IDateTimeRange range)
    {
        if (HasIntersect(range))
        {
            var list = new List<DateTime> { StartTime, range.StartTime, EndTime, range.EndTime };
            list.Sort();
            return new DateTimeRange(list[0], list[3]);
        }

        throw new ArgumentException("不相交的时间段无法合并", nameof(range));
    }

    /// <summary>
    /// 合并时间范围
    /// </summary>
    /// <param name="start">开始时间</param>
    /// <param name="end">结束时间</param>
    public DateTimeRange Union(DateTime start, DateTime end) => Union(new DateTimeRange(start, end));

    #endregion

    #region ToString(输出字符串)

    /// <summary>
    /// 输出字符串
    /// </summary>
    /// <returns>yyyy-MM-dd HH:mm:ss - yyyy-MM-dd HH:mm:ss</returns>
    public override string ToString() => ToString("yyyy-MM-dd HH:mm:ss");

    /// <summary>
    /// 输出字符串
    /// </summary>
    /// <param name="format">格式</param>
    /// <returns>yyyy-MM-dd HH:mm:ss - yyyy-MM-dd HH:mm:ss</returns>
    public string ToString(string format) => ToString(format, DefaultSeparator);

    /// <summary>
    /// 输出字符串
    /// </summary>
    /// <param name="format">格式</param>
    /// <param name="separator">分隔符</param>
    public string ToString(string format, string separator) => $"{StartTime.ToString(format)}{separator}{EndTime.ToString(format)}";

    /// <summary>
    /// 输出字符串
    /// </summary>
    /// <param name="format">格式</param>
    /// <param name="formatProvider">格式化提供程序</param>
    public string ToString(string format, IFormatProvider formatProvider) => ToString(format, DefaultSeparator, formatProvider);

    /// <summary>
    /// 输出字符串
    /// </summary>
    /// <param name="format">格式</param>
    /// <param name="separator">分隔符</param>
    /// <param name="formatProvider">格式化提供程序</param>
    public string ToString(string format, string separator, IFormatProvider formatProvider) => $"{StartTime.ToString(format, formatProvider)}{separator}{EndTime.ToString(format, formatProvider)}";

    #endregion

    /// <summary>
    /// 判断两个 DateTimeRange 实例是否相等
    /// </summary>
    /// <param name="left">第一个 DateTimeRange 实例</param>
    /// <param name="right">第二个 DateTimeRange 实例</param>
    /// <returns>如果两个 DateTimeRange 实例的开始时间和结束时间都相等，返回 true，否则返回 false。</returns>
    public static bool operator ==(DateTimeRange left, DateTimeRange right)
    {
        if (ReferenceEquals(left, right))
            return true;
        if ((object)left == null || (object)right == null)
            return false;
        return left.StartTime == right.StartTime && left.EndTime == right.EndTime;
    }

    /// <summary>
    /// 判断两个 DateTimeRange 实例是否不相等
    /// </summary>
    /// <param name="left">第一个 DateTimeRange 实例</param>
    /// <param name="right">第二个 DateTimeRange 实例</param>
    /// <returns>如果两个 DateTimeRange 实例的开始时间和结束时间都不相等，返回 true，否则返回 false。</returns>
    public static bool operator !=(DateTimeRange left, DateTimeRange right) => !(left == right);

    /// <inheritdoc />
    public int CompareTo(DateTimeRange other)
    {
        if (other == null)
            return 1;
        if (Equals(other))
            return 0;
        return StartTime.CompareTo(other.EndTime);
    }

    /// <inheritdoc />
    public override bool Equals(object obj)
    {
        if (obj == null)
            return false;
        var other = obj as DateTimeRange;
        if ((object)other == null)
            return false;
        return (StartTime == other.StartTime) && (EndTime == other.EndTime);
    }

    /// <inheritdoc />
    public bool Equals(DateTimeRange other)
    {
        if ((object)other == null)
            return false;
        return StartTime == other.StartTime && EndTime == other.EndTime;
    }

    /// <inheritdoc />
    public override int GetHashCode() => (StartTime.Ticks + EndTime.Ticks).GetHashCode();
}