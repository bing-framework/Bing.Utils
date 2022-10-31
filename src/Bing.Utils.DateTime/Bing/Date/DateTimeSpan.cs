using System;
using System.Runtime.InteropServices;
using NodaTime;

/*
 * Reference to:
 *     FluentDateTime
 *     Author: Simon Cropp
 *     GitHub: https://github.com/FluentDateTime/FluentDateTime/blob/master/src/FluentDateTime/DateTimeSpan.cs
 *     NO LICENSE
 */

namespace Bing.Date;

/// <summary>
/// 日期时间间隔
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public partial struct DateTimeSpan : IEquatable<DateTimeSpan>, IComparable<TimeSpan>, IComparable<DateTimeSpan>
{
    /// <summary>
    /// 每年天数
    /// </summary>
    public const int DAYS_PER_YEAR = 365;

    /// <summary>
    /// 年
    /// </summary>
    public int Years { get; set; }

    /// <summary>
    /// 月
    /// </summary>
    public int Months { get; set; }

    /// <summary>
    /// 时间间隔
    /// </summary>
    public TimeSpan TimeSpan { get; set; }

    /// <summary>
    /// 相等
    /// </summary>
    /// <param name="other">对象</param>
    public bool Equals(DateTimeSpan other) => this == other;

    /// <summary>
    /// 添加
    /// </summary>
    /// <param name="number">日期时间间隔</param>
    public DateTimeSpan Add(DateTimeSpan number) => AddInternal(this, number);

    /// <summary>
    /// 添加
    /// </summary>
    /// <param name="timeSpan">时间间隔</param>
    public DateTimeSpan Add(TimeSpan timeSpan) => AddInternal(this, timeSpan);

    /// <summary>
    /// 减去
    /// </summary>
    /// <param name="dateTimeSpan">日期时间间隔</param>
    public DateTimeSpan Subtract(DateTimeSpan dateTimeSpan) => SubtractInternal(this, dateTimeSpan);

    #region Operators

    /// <summary>
    /// +
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static DateTimeSpan operator +(DateTimeSpan left, DateTimeSpan right) => AddInternal(left, right);

    /// <summary>
    /// +
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static DateTimeSpan operator +(DateTimeSpan left, TimeSpan right) => AddInternal(left, right);

    /// <summary>
    /// +
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static DateTimeSpan operator +(TimeSpan left, DateTimeSpan right) => AddInternal(left, right);

    /// <summary>
    /// +
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static DateTimeSpan operator +(DateTimeSpan left, Period right) => AddInternal(left, right);

    /// <summary>
    /// +
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static DateTimeSpan operator +(Period left, DateTimeSpan right) => AddInternal(left, right);

    /// <summary>
    /// +
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static DateTimeSpan operator +(DateTimeSpan left, Duration right) => AddInternal(left, right);

    /// <summary>
    /// +
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static DateTimeSpan operator +(Duration left, DateTimeSpan right) => AddInternal(left, right);

    /// <summary>
    /// -
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static DateTimeSpan operator -(DateTimeSpan left, DateTimeSpan right) => SubtractInternal(left, right);

    /// <summary>
    /// -
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static DateTimeSpan operator -(DateTimeSpan left, TimeSpan right) => SubtractInternal(left, right);

    /// <summary>
    /// -
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static DateTimeSpan operator -(TimeSpan left, DateTimeSpan right) => SubtractInternal(left, right);

    /// <summary>
    /// -
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static DateTimeSpan operator -(DateTimeSpan left, Period right) => SubtractInternal(left, right);

    /// <summary>
    /// -
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static DateTimeSpan operator -(Period left, DateTimeSpan right) => SubtractInternal(left, right);

    /// <summary>
    /// -
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static DateTimeSpan operator -(DateTimeSpan left, Duration right) => SubtractInternal(left, right);

    /// <summary>
    /// -
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static DateTimeSpan operator -(Duration left, DateTimeSpan right) => SubtractInternal(left, right);

    /// <summary>
    /// ==
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static bool operator ==(DateTimeSpan left, DateTimeSpan right) => left.Years == right.Years && left.Months == right.Months && left.TimeSpan == right.TimeSpan;

    /// <summary>
    /// ==
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static bool operator ==(DateTimeSpan left, TimeSpan right) => left == (DateTimeSpan)right;

    /// <summary>
    /// ==
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static bool operator ==(TimeSpan left, DateTimeSpan right) => (DateTimeSpan)left == right;

    /// <summary>
    /// ==
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static bool operator ==(DateTimeSpan left, Period right) => left == (DateTimeSpan)right;

    /// <summary>
    /// ==
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static bool operator ==(Period left, DateTimeSpan right) => (DateTimeSpan)left == right;

    /// <summary>
    /// ==
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static bool operator ==(DateTimeSpan left, Duration right) => left == (DateTimeSpan)right;

    /// <summary>
    /// ==
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static bool operator ==(Duration left, DateTimeSpan right) => (DateTimeSpan)left == right;

    /// <summary>
    /// !=
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static bool operator !=(DateTimeSpan left, DateTimeSpan right) => !(left == right);

    /// <summary>
    /// !=
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static bool operator !=(DateTimeSpan left, TimeSpan right) => !(left == right);

    /// <summary>
    /// !=
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static bool operator !=(TimeSpan left, DateTimeSpan right) => !(left == right);

    /// <summary>
    /// !=
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static bool operator !=(DateTimeSpan left, Period right) => !(left == right);

    /// <summary>
    /// !=
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static bool operator !=(Period left, DateTimeSpan right) => !(left == right);

    /// <summary>
    /// !=
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static bool operator !=(DateTimeSpan left, Duration right) => !(left == right);

    /// <summary>
    /// !=
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static bool operator !=(Duration left, DateTimeSpan right) => !(left == right);

    /// <summary>
    /// -
    /// </summary>
    /// <param name="value">值</param>
    public static DateTimeSpan operator -(DateTimeSpan value) => value.Negate();

    /// <summary>
    /// &lt;
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static bool operator <(DateTimeSpan left, DateTimeSpan right) => (TimeSpan)left < (TimeSpan)right;

    /// <summary>
    /// &lt;
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static bool operator <(DateTimeSpan left, TimeSpan right) => (TimeSpan)left < right;

    /// <summary>
    /// &lt;
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static bool operator <(TimeSpan left, DateTimeSpan right) => left < (TimeSpan)right;

    /// <summary>
    /// &lt;
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static bool operator <(DateTimeSpan left, Period right) => (TimeSpan)left < right.AsTimeSpan();

    /// <summary>
    /// &lt;
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static bool operator <(Period left, DateTimeSpan right) => left.AsTimeSpan() < (TimeSpan)right;

    /// <summary>
    /// &lt;
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static bool operator <(DateTimeSpan left, Duration right) => (TimeSpan)left < right.ToTimeSpan();

    /// <summary>
    /// &lt;
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static bool operator <(Duration left, DateTimeSpan right) => left.ToTimeSpan() < (TimeSpan)right;

    /// <summary>
    /// &lt;=
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static bool operator <=(DateTimeSpan left, DateTimeSpan right) => (TimeSpan)left <= (TimeSpan)right;

    /// <summary>
    /// &lt;=
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static bool operator <=(DateTimeSpan left, TimeSpan right) => (TimeSpan)left <= right;

    /// <summary>
    /// &lt;=
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static bool operator <=(TimeSpan left, DateTimeSpan right) => left <= (TimeSpan)right;

    /// <summary>
    /// &lt;=
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static bool operator <=(DateTimeSpan left, Period right) => (TimeSpan)left <= right.AsTimeSpan();

    /// <summary>
    /// &lt;=
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static bool operator <=(Period left, DateTimeSpan right) => left.AsTimeSpan() <= (TimeSpan)right;

    /// <summary>
    /// &lt;=
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static bool operator <=(DateTimeSpan left, Duration right) => (TimeSpan)left <= right.ToTimeSpan();

    /// <summary>
    /// &lt;=
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static bool operator <=(Duration left, DateTimeSpan right) => left.ToTimeSpan() <= (TimeSpan)right;

    /// <summary>
    /// &gt;
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static bool operator >(DateTimeSpan left, DateTimeSpan right) => (TimeSpan)left > (TimeSpan)right;

    /// <summary>
    /// &gt;
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static bool operator >(DateTimeSpan left, TimeSpan right) => (TimeSpan)left > right;

    /// <summary>
    /// &gt;
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static bool operator >(TimeSpan left, DateTimeSpan right) => left > (TimeSpan)right;

    /// <summary>
    /// &gt;
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static bool operator >(DateTimeSpan left, Period right) => (TimeSpan)left > right.AsTimeSpan();

    /// <summary>
    /// &gt;
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static bool operator >(Period left, DateTimeSpan right) => left.AsTimeSpan() > (TimeSpan)right;

    /// <summary>
    /// &gt;
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static bool operator >(DateTimeSpan left, Duration right) => (TimeSpan)left > right.ToTimeSpan();

    /// <summary>
    /// &gt;
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static bool operator >(Duration left, DateTimeSpan right) => left.ToTimeSpan() > (TimeSpan)right;

    /// <summary>
    /// &gt;=
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static bool operator >=(DateTimeSpan left, DateTimeSpan right) => (TimeSpan)left >= (TimeSpan)right;

    /// <summary>
    /// &gt;=
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static bool operator >=(DateTimeSpan left, TimeSpan right) => (TimeSpan)left >= right;

    /// <summary>
    /// &gt;=
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static bool operator >=(TimeSpan left, DateTimeSpan right) => left >= (TimeSpan)right;

    /// <summary>
    /// &gt;=
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static bool operator >=(DateTimeSpan left, Period right) => (TimeSpan)left >= right.AsTimeSpan();

    /// <summary>
    /// &gt;=
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static bool operator >=(Period left, DateTimeSpan right) => left.AsTimeSpan() >= (TimeSpan)right;

    /// <summary>
    /// &gt;=
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static bool operator >=(DateTimeSpan left, Duration right) => (TimeSpan)left >= right.ToTimeSpan();

    /// <summary>
    /// &gt;=
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static bool operator >=(Duration left, DateTimeSpan right) => left.ToTimeSpan() >= (TimeSpan)right;

    /// <summary>
    /// 将 <see cref="DateTimeSpan"/> 显式转换为 <see cref="System.TimeSpan"/>
    /// </summary>
    /// <param name="dateTimeSpan">日期时间间隔</param>
    public static implicit operator TimeSpan(DateTimeSpan dateTimeSpan)
    {
        var daysFromYears = DAYS_PER_YEAR * dateTimeSpan.Years;
        var daysFromMonths = 30 * dateTimeSpan.Months;
        var days = daysFromMonths + daysFromYears;
        return new TimeSpan(days, 0, 0, 0) + dateTimeSpan.TimeSpan;
    }

    /// <summary>
    /// 将 <see cref="System.TimeSpan"/> 显式转换为 <see cref="DateTimeSpan"/>
    /// </summary>
    /// <param name="timeSpan">时间间隔</param>
    public static implicit operator DateTimeSpan(TimeSpan timeSpan) => new DateTimeSpan { TimeSpan = timeSpan };

    /// <summary>
    /// 将 <see cref="DateTimeSpan"/> 显式转换为 <see cref="Period"/>
    /// </summary>
    /// <param name="dateTimeSpan">日期时间间隔</param>
    public static implicit operator Period(DateTimeSpan dateTimeSpan) => Period.FromTicks(dateTimeSpan.Ticks);

    /// <summary>
    /// 将 <see cref="Period"/> 显式转换为 <see cref="DateTimeSpan"/>
    /// </summary>
    /// <param name="period">期间</param>
    public static implicit operator DateTimeSpan(Period period) =>
        new DateTimeSpan
        {
            Years = period.Years,
            Months = period.Months,
            TimeSpan = TimeSpan.FromTicks(period.Ticks)
        };

    /// <summary>
    /// 将 <see cref="DateTimeSpan"/> 显式转换为 <see cref="Duration"/>
    /// </summary>
    /// <param name="dateTimeSpan">日期时间间隔</param>
    public static implicit operator Duration(DateTimeSpan dateTimeSpan) => Duration.FromTimeSpan(dateTimeSpan.TimeSpan);

    /// <summary>
    /// 将 <see cref="Duration"/> 显式转换为 <see cref="DateTimeSpan"/>
    /// </summary>
    /// <param name="duration">持续时间</param>
    public static implicit operator DateTimeSpan(Duration duration) =>
        new DateTimeSpan
        {
            TimeSpan = duration.ToTimeSpan()
        };

    #endregion

    #region Override Methods

    /// <inheritdoc/>
    public override string ToString() => ((TimeSpan)this).ToString();

    /// <summary>
    /// 转换为字符串
    /// </summary>
    /// <param name="format">格式</param>
    public string ToString(string format) => ((TimeSpan)this).ToString(format);

    /// <summary>
    /// 转换为字符串
    /// </summary>
    /// <param name="format">格式</param>
    /// <param name="provider">格式提供程序</param>
    public string ToString(string format, IFormatProvider provider) => ((TimeSpan)this).ToString(format, provider);

    /// <inheritdoc/>
    public override bool Equals(object obj)
    {
        if (obj is null)
            return false;
        var type = obj.GetType();
        if (type == typeof(DateTimeSpan))
            return this == (DateTimeSpan)obj;
        if (type == typeof(TimeSpan))
            return this == (TimeSpan)obj;
        return false;
    }

    /// <inheritdoc/>
    // ReSharper disable NonReadonlyMemberInGetHashCode
    public override int GetHashCode() => Months.GetHashCode() ^ Years.GetHashCode() ^ TimeSpan.GetHashCode();

    #endregion

    #region DateTimeSpan ops others

    /// <summary>
    /// 内部相加
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    static DateTimeSpan AddInternal(DateTimeSpan left, TimeSpan right) =>
        new DateTimeSpan
        {
            Years = left.Years,
            Months = left.Months,
            TimeSpan = left.TimeSpan + right
        };

    /// <summary>
    /// 内部相减
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    static DateTimeSpan SubtractInternal(DateTimeSpan left, TimeSpan right) =>
        new DateTimeSpan
        {
            Years = left.Years,
            Months = left.Months,
            TimeSpan = left.TimeSpan - right
        };

    /// <summary>
    /// 内部相加
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    static DateTimeSpan AddInternal(DateTimeSpan left, Period right) => left + right.AsDateTimeSpan();

    /// <summary>
    /// 内部相减
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    static DateTimeSpan SubtractInternal(DateTimeSpan left, Period right) => left - right.AsDateTimeSpan();

    /// <summary>
    /// 内部相加
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    static DateTimeSpan AddInternal(DateTimeSpan left, Duration right) => left + right.ToTimeSpan();

    /// <summary>
    /// 内部相减
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    static DateTimeSpan SubtractInternal(DateTimeSpan left, Duration right) => left - right.ToTimeSpan();

    #endregion

    #region Others ops DateTimeSpan

    /// <summary>
    /// 内部相减
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    internal static DateTimeSpan SubtractInternal(TimeSpan left, DateTimeSpan right) =>
        new DateTimeSpan
        {
            Years = -right.Years,
            Months = -right.Months,
            TimeSpan = left - right.TimeSpan
        };

    /// <summary>
    /// 内部相减
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    static DateTimeSpan SubtractInternal(Period left, DateTimeSpan right) => left.AsDateTimeSpan() - right;

    /// <summary>
    /// 内部相减
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    static DateTimeSpan SubtractInternal(Duration left, DateTimeSpan right) => left.ToTimeSpan() - right;

    #endregion

    #region DateTimeSpan ops DateTimeSpan

    /// <summary>
    /// 内部相加
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    static DateTimeSpan AddInternal(DateTimeSpan left, DateTimeSpan right) =>
        new DateTimeSpan
        {
            Years = left.Years + right.Years,
            Months = left.Months + right.Months,
            TimeSpan = left.TimeSpan + right.TimeSpan
        };

    /// <summary>
    /// 内部相减
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    static DateTimeSpan SubtractInternal(DateTimeSpan left, DateTimeSpan right) =>
        new DateTimeSpan
        {
            Years = left.Years - right.Years,
            Months = left.Months - right.Months,
            TimeSpan = left.TimeSpan - right.TimeSpan,
        };

    #endregion

    #region Fields Getters

    /// <summary>
    /// 刻度数
    /// </summary>
    public long Ticks => ((TimeSpan)this).Ticks;

    /// <summary>
    /// 天数
    /// </summary>
    public int Days => ((TimeSpan)this).Days;

    /// <summary>
    /// 小时数
    /// </summary>
    public int Hours => ((TimeSpan)this).Hours;

    /// <summary>
    /// 分钟数
    /// </summary>
    public int Minutes => ((TimeSpan)this).Minutes;

    /// <summary>
    /// 秒数
    /// </summary>
    public int Seconds => ((TimeSpan)this).Seconds;

    /// <summary>
    /// 毫秒数
    /// </summary>
    public int Milliseconds => ((TimeSpan)this).Milliseconds;

    /// <summary>
    /// 总天数
    /// </summary>
    public double TotalDays => ((TimeSpan)this).TotalDays;

    /// <summary>
    /// 总小时数
    /// </summary>
    public double TotalHours => ((TimeSpan)this).TotalHours;

    /// <summary>
    /// 总分钟数
    /// </summary>
    public double TotalMinutes => ((TimeSpan)this).TotalMinutes;

    /// <summary>
    /// 总秒数
    /// </summary>
    public double TotalSeconds => ((TimeSpan)this).TotalSeconds;

    /// <summary>
    /// 总毫秒数
    /// </summary>
    public double TotalMilliseconds => ((TimeSpan)this).TotalMilliseconds;

    #endregion

    #region CompareTo

    /// <summary>
    /// 比较
    /// </summary>
    /// <param name="value">对象</param>
    public int CompareTo(TimeSpan value) => ((TimeSpan)this).CompareTo(value);

    /// <summary>
    /// 比较
    /// </summary>
    /// <param name="value">对象</param>
    /// <exception cref="ArgumentException"></exception>
    public int CompareTo(object value)
    {
        if (value is TimeSpan timeSpan)
            return ((TimeSpan)this).CompareTo(timeSpan);
        throw new ArgumentException("Value must be a TimeSpan", nameof(value));
    }

    /// <summary>
    /// 比较
    /// </summary>
    /// <param name="value">对象</param>
    public int CompareTo(DateTimeSpan value) => ((TimeSpan)this).CompareTo(value);

    #endregion

    /// <summary>
    /// 克隆
    /// </summary>
    public object Clone() =>
        new DateTimeSpan
        {
            TimeSpan = TimeSpan,
            Months = Months,
            Years = Years
        };

    /// <summary>
    /// 取反
    /// </summary>
    public TimeSpan Negate() =>
        new DateTimeSpan
        {
            Years = -Years,
            Months = -Months,
            TimeSpan = -TimeSpan
        };
}