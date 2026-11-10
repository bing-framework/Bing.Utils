using System.Diagnostics.Contracts;
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
/// <remarks>转换为固定时长及比较大小时，按一年 365 天、一月 30 天换算；该换算不表示真实日历跨度。</remarks>
[StructLayout(LayoutKind.Sequential)]
public partial struct DateTimeSpan : IEquatable<DateTimeSpan>, IComparable<TimeSpan>, IComparable<DateTimeSpan>
{
    /// <summary>
    /// 固定时长换算使用的每年天数，不表示实际日历年长度。
    /// </summary>
    public const int DAYS_PER_YEAR = 365;
    /// <summary>
    /// 获取或设置年
    /// </summary>
    public int Years { get; set; }
    /// <summary>
    /// 获取或设置月
    /// </summary>
    public int Months { get; set; }
    /// <summary>
    /// 获取或设置时间间隔
    /// </summary>
    public TimeSpan TimeSpan { get; set; }

    #region Ctor
    /// <summary>
    /// 初始化一个 <see cref="DateTimeSpan"/> 类型的实例。
    /// </summary>
    /// <param name="years">年</param>
    /// <param name="months">月</param>
    /// <param name="timeSpan">时间间隔</param>
    public DateTimeSpan(int years, int months, TimeSpan timeSpan)
    {
        Years = years;
        Months = months;
        TimeSpan = timeSpan;
    }

    #endregion
    /// <inheritdoc />
    public bool Equals(DateTimeSpan other) => this == other;
    /// <summary>
    /// 计算时间间隔之和。
    /// </summary>
    /// <param name="number">日期时间间隔</param>
    /// <returns>相加后的时间间隔。</returns>
    [Pure]
    public DateTimeSpan Add(DateTimeSpan number) => AddInternal(this, number);
    /// <summary>
    /// 计算时间间隔之和。
    /// </summary>
    /// <param name="timeSpan">时间间隔</param>
    /// <returns>相加后的时间间隔。</returns>
    [Pure]
    public DateTimeSpan Add(TimeSpan timeSpan) => AddInternal(this, timeSpan);
    /// <summary>
    /// 计算时间间隔之差。
    /// </summary>
    /// <param name="dateTimeSpan">日期时间间隔</param>
    /// <returns>相减后的时间间隔。</returns>
    [Pure]
    public DateTimeSpan Subtract(DateTimeSpan dateTimeSpan) => SubtractInternal(this, dateTimeSpan);

    #region Operators
    /// <summary>
    /// 计算时间间隔之和。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>相加后的时间间隔。</returns>
    [Pure]
    public static DateTimeSpan operator +(DateTimeSpan left, DateTimeSpan right) => AddInternal(left, right);
    /// <summary>
    /// 计算时间间隔之和。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>相加后的时间间隔。</returns>
    [Pure]
    public static DateTimeSpan operator +(DateTimeSpan left, TimeSpan right) => AddInternal(left, right);
    /// <summary>
    /// 计算时间间隔之和。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>相加后的时间间隔。</returns>
    [Pure]
    public static DateTimeSpan operator +(TimeSpan left, DateTimeSpan right) => AddInternal(left, right);
    /// <summary>
    /// 计算时间间隔之和。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>相加后的时间间隔。</returns>
    [Pure]
    public static DateTimeSpan operator +(DateTimeSpan left, Period right) => AddInternal(left, right);
    /// <summary>
    /// 计算时间间隔之和。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>相加后的时间间隔。</returns>
    [Pure]
    public static DateTimeSpan operator +(Period left, DateTimeSpan right) => AddInternal(left, right);
    /// <summary>
    /// 计算时间间隔之和。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>相加后的时间间隔。</returns>
    [Pure]
    public static DateTimeSpan operator +(DateTimeSpan left, Duration right) => AddInternal(left, right);
    /// <summary>
    /// 计算时间间隔之和。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>相加后的时间间隔。</returns>
    [Pure]
    public static DateTimeSpan operator +(Duration left, DateTimeSpan right) => AddInternal(left, right);
    /// <summary>
    /// 计算时间间隔之差。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>相减后的时间间隔。</returns>
    [Pure]
    public static DateTimeSpan operator -(DateTimeSpan left, DateTimeSpan right) => SubtractInternal(left, right);
    /// <summary>
    /// 计算时间间隔之差。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>相减后的时间间隔。</returns>
    [Pure]
    public static DateTimeSpan operator -(DateTimeSpan left, TimeSpan right) => SubtractInternal(left, right);
    /// <summary>
    /// 计算时间间隔之差。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>相减后的时间间隔。</returns>
    [Pure]
    public static DateTimeSpan operator -(TimeSpan left, DateTimeSpan right) => SubtractInternal(left, right);
    /// <summary>
    /// 计算时间间隔之差。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>相减后的时间间隔。</returns>
    [Pure]
    public static DateTimeSpan operator -(DateTimeSpan left, Period right) => SubtractInternal(left, right);
    /// <summary>
    /// 计算时间间隔之差。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>相减后的时间间隔。</returns>
    [Pure]
    public static DateTimeSpan operator -(Period left, DateTimeSpan right) => SubtractInternal(left, right);
    /// <summary>
    /// 计算时间间隔之差。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>相减后的时间间隔。</returns>
    [Pure]
    public static DateTimeSpan operator -(DateTimeSpan left, Duration right) => SubtractInternal(left, right);
    /// <summary>
    /// 计算时间间隔之差。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>相减后的时间间隔。</returns>
    [Pure]
    public static DateTimeSpan operator -(Duration left, DateTimeSpan right) => SubtractInternal(left, right);
    /// <summary>
    /// 判断两个时间间隔是否相等。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>两个时间间隔相等时为 true，否则为 false。</returns>
    [Pure]
    public static bool operator ==(DateTimeSpan left, DateTimeSpan right) => left.Years == right.Years && left.Months == right.Months && left.TimeSpan == right.TimeSpan;
    /// <summary>
    /// 判断两个时间间隔是否相等。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>两个时间间隔相等时为 true，否则为 false。</returns>
    [Pure]
    public static bool operator ==(DateTimeSpan left, TimeSpan right) => left == (DateTimeSpan)right;
    /// <summary>
    /// 判断两个时间间隔是否相等。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>两个时间间隔相等时为 true，否则为 false。</returns>
    [Pure]
    public static bool operator ==(TimeSpan left, DateTimeSpan right) => (DateTimeSpan)left == right;
    /// <summary>
    /// 判断两个时间间隔是否相等。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>两个时间间隔相等时为 true，否则为 false。</returns>
    [Pure]
    public static bool operator ==(DateTimeSpan left, Period right) => left == (DateTimeSpan)right;
    /// <summary>
    /// 判断两个时间间隔是否相等。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>两个时间间隔相等时为 true，否则为 false。</returns>
    [Pure]
    public static bool operator ==(Period left, DateTimeSpan right) => (DateTimeSpan)left == right;
    /// <summary>
    /// 判断两个时间间隔是否相等。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>两个时间间隔相等时为 true，否则为 false。</returns>
    [Pure]
    public static bool operator ==(DateTimeSpan left, Duration right) => left == (DateTimeSpan)right;
    /// <summary>
    /// 判断两个时间间隔是否相等。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>两个时间间隔相等时为 true，否则为 false。</returns>
    [Pure]
    public static bool operator ==(Duration left, DateTimeSpan right) => (DateTimeSpan)left == right;
    /// <summary>
    /// 判断两个时间间隔是否不相等。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>两个时间间隔不相等时为 true，否则为 false。</returns>
    [Pure]
    public static bool operator !=(DateTimeSpan left, DateTimeSpan right) => !(left == right);
    /// <summary>
    /// 判断两个时间间隔是否不相等。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>两个时间间隔不相等时为 true，否则为 false。</returns>
    [Pure]
    public static bool operator !=(DateTimeSpan left, TimeSpan right) => !(left == right);
    /// <summary>
    /// 判断两个时间间隔是否不相等。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>两个时间间隔不相等时为 true，否则为 false。</returns>
    [Pure]
    public static bool operator !=(TimeSpan left, DateTimeSpan right) => !(left == right);
    /// <summary>
    /// 判断两个时间间隔是否不相等。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>两个时间间隔不相等时为 true，否则为 false。</returns>
    [Pure]
    public static bool operator !=(DateTimeSpan left, Period right) => !(left == right);
    /// <summary>
    /// 判断两个时间间隔是否不相等。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>两个时间间隔不相等时为 true，否则为 false。</returns>
    [Pure]
    public static bool operator !=(Period left, DateTimeSpan right) => !(left == right);
    /// <summary>
    /// 判断两个时间间隔是否不相等。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>两个时间间隔不相等时为 true，否则为 false。</returns>
    [Pure]
    public static bool operator !=(DateTimeSpan left, Duration right) => !(left == right);
    /// <summary>
    /// 判断两个时间间隔是否不相等。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>两个时间间隔不相等时为 true，否则为 false。</returns>
    [Pure]
    public static bool operator !=(Duration left, DateTimeSpan right) => !(left == right);
    /// <summary>
    /// 计算时间间隔之差。
    /// </summary>
    /// <param name="value">值</param>
    /// <returns>相减后的时间间隔。</returns>
    public static DateTimeSpan operator -(DateTimeSpan value) =>
        new(checked(-value.Years), checked(-value.Months), -value.TimeSpan);
    /// <summary>
    /// 判断左侧时间间隔是否小于右侧。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>左侧小于右侧时为 true，否则为 false。</returns>
    public static bool operator <(DateTimeSpan left, DateTimeSpan right) => (TimeSpan)left < (TimeSpan)right;
    /// <summary>
    /// 判断左侧时间间隔是否小于右侧。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>左侧小于右侧时为 true，否则为 false。</returns>
    [Pure]
    public static bool operator <(DateTimeSpan left, TimeSpan right) => (TimeSpan)left < right;
    /// <summary>
    /// 判断左侧时间间隔是否小于右侧。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>左侧小于右侧时为 true，否则为 false。</returns>
    [Pure]
    public static bool operator <(TimeSpan left, DateTimeSpan right) => left < (TimeSpan)right;
    /// <summary>
    /// 判断左侧时间间隔是否小于右侧。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>左侧小于右侧时为 true，否则为 false。</returns>
    public static bool operator <(DateTimeSpan left, Period right) => (TimeSpan)left < right.AsTimeSpan();
    /// <summary>
    /// 判断左侧时间间隔是否小于右侧。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>左侧小于右侧时为 true，否则为 false。</returns>
    public static bool operator <(Period left, DateTimeSpan right) => left.AsTimeSpan() < (TimeSpan)right;
    /// <summary>
    /// 判断左侧时间间隔是否小于右侧。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>左侧小于右侧时为 true，否则为 false。</returns>
    public static bool operator <(DateTimeSpan left, Duration right) => (TimeSpan)left < right.ToTimeSpan();
    /// <summary>
    /// 判断左侧时间间隔是否小于右侧。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>左侧小于右侧时为 true，否则为 false。</returns>
    public static bool operator <(Duration left, DateTimeSpan right) => left.ToTimeSpan() < (TimeSpan)right;
    /// <summary>
    /// 判断左侧时间间隔是否小于等于右侧。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>左侧小于或等于右侧时为 true，否则为 false。</returns>
    [Pure]
    public static bool operator <=(DateTimeSpan left, DateTimeSpan right) => (TimeSpan)left <= (TimeSpan)right;
    /// <summary>
    /// 判断左侧时间间隔是否小于等于右侧。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>左侧小于或等于右侧时为 true，否则为 false。</returns>
    [Pure]
    public static bool operator <=(DateTimeSpan left, TimeSpan right) => (TimeSpan)left <= right;
    /// <summary>
    /// 判断左侧时间间隔是否小于等于右侧。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>左侧小于或等于右侧时为 true，否则为 false。</returns>
    [Pure]
    public static bool operator <=(TimeSpan left, DateTimeSpan right) => left <= (TimeSpan)right;
    /// <summary>
    /// 判断左侧时间间隔是否小于等于右侧。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>左侧小于或等于右侧时为 true，否则为 false。</returns>
    public static bool operator <=(DateTimeSpan left, Period right) => (TimeSpan)left <= right.AsTimeSpan();
    /// <summary>
    /// 判断左侧时间间隔是否小于等于右侧。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>左侧小于或等于右侧时为 true，否则为 false。</returns>
    public static bool operator <=(Period left, DateTimeSpan right) => left.AsTimeSpan() <= (TimeSpan)right;
    /// <summary>
    /// 判断左侧时间间隔是否小于等于右侧。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>左侧小于或等于右侧时为 true，否则为 false。</returns>
    public static bool operator <=(DateTimeSpan left, Duration right) => (TimeSpan)left <= right.ToTimeSpan();
    /// <summary>
    /// 判断左侧时间间隔是否小于等于右侧。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>左侧小于或等于右侧时为 true，否则为 false。</returns>
    public static bool operator <=(Duration left, DateTimeSpan right) => left.ToTimeSpan() <= (TimeSpan)right;
    /// <summary>
    /// 判断左侧时间间隔是否大于右侧。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>左侧大于右侧时为 true，否则为 false。</returns>
    [Pure]
    public static bool operator >(DateTimeSpan left, DateTimeSpan right) => (TimeSpan)left > (TimeSpan)right;
    /// <summary>
    /// 判断左侧时间间隔是否大于右侧。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>左侧大于右侧时为 true，否则为 false。</returns>
    [Pure]
    public static bool operator >(DateTimeSpan left, TimeSpan right) => (TimeSpan)left > right;
    /// <summary>
    /// 判断左侧时间间隔是否大于右侧。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>左侧大于右侧时为 true，否则为 false。</returns>
    [Pure]
    public static bool operator >(TimeSpan left, DateTimeSpan right) => left > (TimeSpan)right;
    /// <summary>
    /// 判断左侧时间间隔是否大于右侧。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>左侧大于右侧时为 true，否则为 false。</returns>
    public static bool operator >(DateTimeSpan left, Period right) => (TimeSpan)left > right.AsTimeSpan();
    /// <summary>
    /// 判断左侧时间间隔是否大于右侧。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>左侧大于右侧时为 true，否则为 false。</returns>
    public static bool operator >(Period left, DateTimeSpan right) => left.AsTimeSpan() > (TimeSpan)right;
    /// <summary>
    /// 判断左侧时间间隔是否大于右侧。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>左侧大于右侧时为 true，否则为 false。</returns>
    public static bool operator >(DateTimeSpan left, Duration right) => (TimeSpan)left > right.ToTimeSpan();
    /// <summary>
    /// 判断左侧时间间隔是否大于右侧。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>左侧大于右侧时为 true，否则为 false。</returns>
    public static bool operator >(Duration left, DateTimeSpan right) => left.ToTimeSpan() > (TimeSpan)right;
    /// <summary>
    /// 判断左侧时间间隔是否大于等于右侧。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>左侧大于或等于右侧时为 true，否则为 false。</returns>
    [Pure]
    public static bool operator >=(DateTimeSpan left, DateTimeSpan right) => (TimeSpan)left >= (TimeSpan)right;
    /// <summary>
    /// 判断左侧时间间隔是否大于等于右侧。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>左侧大于或等于右侧时为 true，否则为 false。</returns>
    [Pure]
    public static bool operator >=(DateTimeSpan left, TimeSpan right) => (TimeSpan)left >= right;
    /// <summary>
    /// 判断左侧时间间隔是否大于等于右侧。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>左侧大于或等于右侧时为 true，否则为 false。</returns>
    [Pure]
    public static bool operator >=(TimeSpan left, DateTimeSpan right) => left >= (TimeSpan)right;
    /// <summary>
    /// 判断左侧时间间隔是否大于等于右侧。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>左侧大于或等于右侧时为 true，否则为 false。</returns>
    public static bool operator >=(DateTimeSpan left, Period right) => (TimeSpan)left >= right.AsTimeSpan();
    /// <summary>
    /// 判断左侧时间间隔是否大于等于右侧。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>左侧大于或等于右侧时为 true，否则为 false。</returns>
    public static bool operator >=(Period left, DateTimeSpan right) => left.AsTimeSpan() >= (TimeSpan)right;
    /// <summary>
    /// 判断左侧时间间隔是否大于等于右侧。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>左侧大于或等于右侧时为 true，否则为 false。</returns>
    public static bool operator >=(DateTimeSpan left, Duration right) => (TimeSpan)left >= right.ToTimeSpan();
    /// <summary>
    /// 判断左侧时间间隔是否大于等于右侧。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>左侧大于或等于右侧时为 true，否则为 false。</returns>
    public static bool operator >=(Duration left, DateTimeSpan right) => left.ToTimeSpan() >= (TimeSpan)right;
    /// <summary>
    /// 将 <see cref="DateTimeSpan"/> 隐式转换为 <see cref="System.TimeSpan"/>
    /// </summary>
    /// <param name="dateTimeSpan">日期时间间隔</param>
    /// <returns>转换后的 <see cref="TimeSpan"/> 值。</returns>
    public static implicit operator TimeSpan(DateTimeSpan dateTimeSpan)
    {
        // 兼容固定换算：一年 365 天、一月 30 天；不表示真实日历跨度。
        var days = (long)DAYS_PER_YEAR * dateTimeSpan.Years + 30L * dateTimeSpan.Months;
        return TimeSpan.FromTicks(checked(days * TimeSpan.TicksPerDay)) + dateTimeSpan.TimeSpan;
    }
    /// <summary>
    /// 将 <see cref="System.TimeSpan"/> 隐式转换为 <see cref="DateTimeSpan"/>
    /// </summary>
    /// <param name="timeSpan">时间间隔</param>
    /// <returns>转换后的 <see cref="DateTimeSpan"/> 值。</returns>
    public static implicit operator DateTimeSpan(TimeSpan timeSpan) => new() { TimeSpan = timeSpan };
    /// <summary>
    /// 将 <see cref="DateTimeSpan"/> 隐式转换为 <see cref="Period"/>
    /// </summary>
    /// <param name="dateTimeSpan">日期时间间隔</param>
    /// <returns>转换后的 <see cref="Period"/> 值。</returns>
    public static implicit operator Period(DateTimeSpan dateTimeSpan) =>
        new PeriodBuilder { Years = dateTimeSpan.Years, Months = dateTimeSpan.Months, Ticks = dateTimeSpan.TimeSpan.Ticks }.Build();
    /// <summary>
    /// 将 <see cref="Period"/> 隐式转换为 <see cref="DateTimeSpan"/>
    /// </summary>
    /// <param name="period">期间</param>
    /// <returns>转换后的 <see cref="DateTimeSpan"/> 值。</returns>
    public static implicit operator DateTimeSpan(Period period)
    {
        if (period is null)
            throw new ArgumentNullException(nameof(period));
        var fixedPart = period.ToBuilder();
        fixedPart.Years = 0;
        fixedPart.Months = 0;
        return new DateTimeSpan(period.Years, period.Months, fixedPart.Build().ToDuration().ToTimeSpan());
    }
    /// <summary>
    /// 将 <see cref="DateTimeSpan"/> 隐式转换为 <see cref="Duration"/>
    /// </summary>
    /// <param name="dateTimeSpan">日期时间间隔</param>
    /// <returns>转换后的 <see cref="Duration"/> 值。</returns>
    public static implicit operator Duration(DateTimeSpan dateTimeSpan) => Duration.FromTimeSpan((TimeSpan)dateTimeSpan);
    /// <summary>
    /// 将 <see cref="Duration"/> 隐式转换为 <see cref="DateTimeSpan"/>
    /// </summary>
    /// <param name="duration">持续时间</param>
    /// <returns>转换后的 <see cref="DateTimeSpan"/> 值。</returns>
    public static implicit operator DateTimeSpan(Duration duration) =>
        new()
        {
            TimeSpan = duration.ToTimeSpan()
        };

    #endregion

    #region Override Methods
    /// <inheritdoc />
    public override string ToString() => ((TimeSpan)this).ToString();
    /// <summary>
    /// 转换为字符串
    /// </summary>
    /// <param name="format">格式</param>
    /// <returns>按指定格式生成的时间文本。</returns>
    public string ToString(string format) => ((TimeSpan)this).ToString(format);
    /// <summary>
    /// 转换为字符串
    /// </summary>
    /// <param name="format">格式</param>
    /// <param name="provider">格式提供程序</param>
    /// <returns>按指定格式生成的时间文本。</returns>
    public string ToString(string format, IFormatProvider provider) => ((TimeSpan)this).ToString(format, provider);
    /// <inheritdoc />
    /// <remarks>对象相等性只比较相同类型；与 <see cref="TimeSpan"/> 的跨类型比较仍可使用运算符。</remarks>
    public override bool Equals(object obj) => obj is DateTimeSpan other && Equals(other);
    /// <inheritdoc />
    public override int GetHashCode() => Months.GetHashCode() ^ Years.GetHashCode() ^ TimeSpan.GetHashCode();

    #endregion

    #region DateTimeSpan ops others
    /// <summary>
    /// 计算时间间隔之和。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>按分量相加后的时间间隔。</returns>
    static DateTimeSpan AddInternal(DateTimeSpan left, TimeSpan right) =>
        new()
        {
            Years = left.Years,
            Months = left.Months,
            TimeSpan = left.TimeSpan + right
        };
    /// <summary>
    /// 计算时间间隔之差。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>按分量相减后的时间间隔。</returns>
    static DateTimeSpan SubtractInternal(DateTimeSpan left, TimeSpan right) =>
        new()
        {
            Years = left.Years,
            Months = left.Months,
            TimeSpan = left.TimeSpan - right
        };
    /// <summary>
    /// 计算时间间隔之和。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>按分量相加后的时间间隔。</returns>
    static DateTimeSpan AddInternal(DateTimeSpan left, Period right) => left + right.AsDateTimeSpan();
    /// <summary>
    /// 计算时间间隔之差。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>按分量相减后的时间间隔。</returns>
    static DateTimeSpan SubtractInternal(DateTimeSpan left, Period right) => left - right.AsDateTimeSpan();
    /// <summary>
    /// 计算时间间隔之和。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>按分量相加后的时间间隔。</returns>
    static DateTimeSpan AddInternal(DateTimeSpan left, Duration right) => left + right.ToTimeSpan();
    /// <summary>
    /// 计算时间间隔之差。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>按分量相减后的时间间隔。</returns>
    static DateTimeSpan SubtractInternal(DateTimeSpan left, Duration right) => left - right.ToTimeSpan();

    #endregion

    #region Others ops DateTimeSpan
    /// <summary>
    /// 计算时间间隔之差。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>按分量相减后的时间间隔。</returns>
    internal static DateTimeSpan SubtractInternal(TimeSpan left, DateTimeSpan right) =>
        new()
        {
            Years = checked(-right.Years),
            Months = checked(-right.Months),
            TimeSpan = left - right.TimeSpan
        };
    /// <summary>
    /// 计算时间间隔之差。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>按分量相减后的时间间隔。</returns>
    static DateTimeSpan SubtractInternal(Period left, DateTimeSpan right) => left.AsDateTimeSpan() - right;
    /// <summary>
    /// 计算时间间隔之差。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>按分量相减后的时间间隔。</returns>
    static DateTimeSpan SubtractInternal(Duration left, DateTimeSpan right) => left.ToTimeSpan() - right;

    #endregion

    #region DateTimeSpan ops DateTimeSpan
    /// <summary>
    /// 计算时间间隔之和。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>按分量相加后的时间间隔。</returns>
    static DateTimeSpan AddInternal(DateTimeSpan left, DateTimeSpan right) =>
        new()
        {
            Years = checked(left.Years + right.Years),
            Months = checked(left.Months + right.Months),
            TimeSpan = left.TimeSpan + right.TimeSpan
        };
    /// <summary>
    /// 计算时间间隔之差。
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    /// <returns>按分量相减后的时间间隔。</returns>
    static DateTimeSpan SubtractInternal(DateTimeSpan left, DateTimeSpan right) =>
        new()
        {
            Years = checked(left.Years - right.Years),
            Months = checked(left.Months - right.Months),
            TimeSpan = left.TimeSpan - right.TimeSpan,
        };

    #endregion

    #region Fields Getters
    /// <summary>
    /// 获取刻度数
    /// </summary>
    public long Ticks => ((TimeSpan)this).Ticks;
    /// <summary>
    /// 获取天数
    /// </summary>
    public int Days => ((TimeSpan)this).Days;
    /// <summary>
    /// 获取小时数
    /// </summary>
    public int Hours => ((TimeSpan)this).Hours;
    /// <summary>
    /// 获取分钟数
    /// </summary>
    public int Minutes => ((TimeSpan)this).Minutes;
    /// <summary>
    /// 获取秒数
    /// </summary>
    public int Seconds => ((TimeSpan)this).Seconds;
    /// <summary>
    /// 获取毫秒数
    /// </summary>
    public int Milliseconds => ((TimeSpan)this).Milliseconds;
    /// <summary>
    /// 获取总天数
    /// </summary>
    public double TotalDays => ((TimeSpan)this).TotalDays;
    /// <summary>
    /// 获取总小时数
    /// </summary>
    public double TotalHours => ((TimeSpan)this).TotalHours;
    /// <summary>
    /// 获取总分钟数
    /// </summary>
    public double TotalMinutes => ((TimeSpan)this).TotalMinutes;
    /// <summary>
    /// 获取总秒数
    /// </summary>
    public double TotalSeconds => ((TimeSpan)this).TotalSeconds;
    /// <summary>
    /// 获取总毫秒数
    /// </summary>
    public double TotalMilliseconds => ((TimeSpan)this).TotalMilliseconds;

    #endregion

    #region CompareTo
    /// <inheritdoc />
    public int CompareTo(TimeSpan value) => ((TimeSpan)this).CompareTo(value);
    /// <summary>
    /// 比较
    /// </summary>
    /// <param name="value">对象</param>
    /// <exception cref="ArgumentException">比较值不是 TimeSpan 时抛出。</exception>
    /// <returns>小于比较值时为负数，相等时为零，大于比较值时为正数。</returns>
    public int CompareTo(object value)
    {
        if (value is TimeSpan timeSpan)
            return ((TimeSpan)this).CompareTo(timeSpan);
        throw new ArgumentException("Value must be a TimeSpan", nameof(value));
    }
    /// <inheritdoc />
    public int CompareTo(DateTimeSpan value) => ((TimeSpan)this).CompareTo(value);

    #endregion
    /// <summary>
    /// 克隆
    /// </summary>
    /// <returns>与当前值具有相同分量的副本。</returns>
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
    /// <returns>各分量取反后的时间间隔。</returns>
    [Pure]
    public TimeSpan Negate() =>
        new DateTimeSpan
        {
            Years = checked(-Years),
            Months = checked(-Months),
            TimeSpan = -TimeSpan
        };
}
