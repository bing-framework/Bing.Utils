using Bing.Date.DateUtils;
using NodaTime;
using System.Diagnostics.Contracts;

namespace Bing.Date;

/// <summary>
/// Bing 日期时间(<see cref="DateTime"/>) 扩展
/// </summary>
public static partial class DateTimeExtensions
{
    #region Add

    /// <summary>
    /// 添加指定数量的星期到日期时间
    /// </summary>
    /// <param name="dt">原始日期时间</param>
    /// <param name="weeks">要添加的星期数，可为负数</param>
    /// <returns>添加星期数后的新日期时间</returns>
    /// <example>
    /// <code>
    /// var date = new DateTime(2023, 1, 1);
    /// var newDate = date.AddWeeks(2); // 结果为 2023-01-15
    /// </code>
    /// </example>
    public static DateTime AddWeeks(this DateTime dt, int weeks) => DateTimeCalc.OffsetByWeeks(dt, weeks);

    /// <summary>
    /// 添加指定数量的季度到日期时间
    /// </summary>
    /// <param name="dt">原始日期时间</param>
    /// <param name="quarters">要添加的季度数，可为负数</param>
    /// <returns>添加季度数后的新日期时间</returns>
    /// <example>
    /// <code>
    /// var date = new DateTime(2023, 1, 1);
    /// var newDate = date.AddQuarters(1); // 结果为 2023-04-01
    /// </code>
    /// </example>
    public static DateTime AddQuarters(this DateTime dt, int quarters) => DateTimeCalc.OffsetByQuarters(dt, quarters);

    /// <summary>
    /// 添加指定的NodaTime持续时间到日期时间
    /// </summary>
    /// <param name="dt">原始日期时间</param>
    /// <param name="duration">要添加的NodaTime持续时间</param>
    /// <returns>添加持续时间后的新日期时间</returns>
    /// <example>
    /// <code>
    /// var date = new DateTime(2023, 1, 1);
    /// var duration = Duration.FromDays(5);
    /// var newDate = date.AddDuration(duration); // 结果为 2023-01-06
    /// </code>
    /// </example>
    public static DateTime AddDuration(this DateTime dt, Duration duration) => DateTimeCalc.OffsetByDuration(dt, duration);

    /// <summary>
    /// 添加指定数量的工作日（周一至周五）到日期时间
    /// </summary>
    /// <param name="dt">原始日期时间</param>
    /// <param name="days">要添加的工作日数，可为负数</param>
    /// <returns>添加工作日数后的新日期时间</returns>
    /// <remarks>
    /// 此方法会跳过所有的周六和周日，只计算周一至周五为工作日。
    /// </remarks>
    /// <example>
    /// <code>
    /// var friday = new DateTime(2023, 1, 6); // 周五
    /// var newDate = friday.AddBusinessDays(1); // 结果为 2023-01-09（下一个周一）
    /// </code>
    /// </example>
    public static DateTime AddBusinessDays(this DateTime dt, int days)
    {
        var sign = Math.Sign(days);
        var unsignedDays = Math.Abs(days);
        for (var i = 0; i < unsignedDays; i++)
        {
            do
            {
                dt = dt.AddDays(sign);
            } while (dt.DayOfWeek == DayOfWeek.Saturday || dt.DayOfWeek == DayOfWeek.Sunday);
        }
        return dt;
    }

    #endregion

    #region Age & Birthday

    /// <summary>
    /// 根据生日计算当前日期下的年龄
    /// </summary>
    /// <param name="birthday">生日日期</param>
    /// <returns>当前的年龄（岁）</returns>
    /// <remarks>
    /// 使用今天的日期（<see cref="DateTime.Today"/>）作为参考日期来计算年龄。
    /// </remarks>
    /// <example>
    /// <code>
    /// var birthday = new DateTime(1990, 5, 15);
    /// var age = birthday.ToCalculateAge(); // 返回当前日期下的年龄
    /// </code>
    /// </example>
    public static int ToCalculateAge(this DateTime birthday) => birthday.ToCalculateAge(DateTime.Today);

    /// <summary>
    /// 根据生日和参照日期，计算指定日期下的年龄
    /// </summary>
    /// <param name="birthday">生日日期</param>
    /// <param name="referenceDate">计算年龄的参考日期</param>
    /// <returns>参考日期下的年龄（岁）</returns>
    /// <remarks>
    /// 考虑月份和日期，如果参考日期的月/日小于生日月/日，则减一年。
    /// </remarks>
    /// <example>
    /// <code>
    /// var birthday = new DateTime(1990, 5, 15);
    /// var refDate = new DateTime(2023, 1, 1);
    /// var age = birthday.ToCalculateAge(refDate); // 结果为32（未满33岁）
    /// </code>
    /// </example>
    public static int ToCalculateAge(this DateTime birthday, DateTime referenceDate)
    {
        var years = referenceDate.Year - birthday.Year;
        if (referenceDate.Month < birthday.Month ||
            referenceDate.Month == birthday.Month && referenceDate.Day < birthday.Day)
            --years;
        return years;
    }

    #endregion

    #region Clone

    /// <summary>
    /// 创建日期时间的精确副本，保留原始的Ticks和Kind属性
    /// </summary>
    /// <param name="dt">要克隆的日期时间</param>
    /// <returns>与原始日期时间值和Kind属性完全相同的新实例</returns>
    /// <example>
    /// <code>
    /// var original = DateTime.UtcNow;
    /// var cloned = original.Clone();
    /// Console.WriteLine(cloned.Kind == original.Kind); // 输出: True
    /// </code>
    /// </example>
    public static DateTime Clone(this DateTime dt) => new(dt.Ticks, dt.Kind);

    #endregion

    #region Diff

    /// <summary>
    /// 计算两个日期时间之间相差的月份数（整数）
    /// </summary>
    /// <param name="dt1">第一个日期时间</param>
    /// <param name="dt2">第二个日期时间</param>
    /// <returns>两个日期之间的月份差异数（绝对值）</returns>
    /// <remarks>
    /// 如果较小日期的天数大于较大日期的天数，不计入一个月；
    /// 如果较小日期的天数等于较大日期的天数，则精确计算月份差。
    /// </remarks>
    /// <example>
    /// <code>
    /// var date1 = new DateTime(2023, 1, 15);
    /// var date2 = new DateTime(2023, 5, 10);
    /// var months = date1.GetMonthDiff(date2); // 结果为 3（因为 15 > 10）
    /// </code>
    /// </example>
    public static int GetMonthDiff(this DateTime dt1, DateTime dt2)
    {
        var l = dt1 < dt2 ? dt1 : dt2;
        var r = dt1 >= dt2 ? dt1 : dt2;
        return (l.Day == r.Day ? 0 : l.Day > r.Day ? 0 : 1)
               + (l.Month == r.Month ? 0 : r.Month - l.Month)
               + (l.Year == r.Year ? 0 : (r.Year - l.Year) * 12);
    }

    /// <summary>
    /// 计算两个日期时间之间相差的确切月份数（含小数）
    /// </summary>
    /// <param name="dt1">第一个日期时间</param>
    /// <param name="dt2">第二个日期时间</param>
    /// <returns>两个日期之间的月份差异数（带小数部分）</returns>
    /// <remarks>
    /// 此方法考虑了不同月份的天数差异，提供更精确的计算结果。
    /// 小数部分表示不足一个月的比例。
    /// </remarks>
    /// <example>
    /// <code>
    /// var date1 = new DateTime(2023, 1, 15);
    /// var date2 = new DateTime(2023, 5, 20);
    /// var months = date1.GetTotalMonthDiff(date2); // 结果约为 4.16
    /// </code>
    /// </example>
    public static double GetTotalMonthDiff(this DateTime dt1, DateTime dt2)
    {
        var l = dt1 < dt2 ? dt1 : dt2;
        var r = dt1 >= dt2 ? dt1 : dt2;
        var lDfm = DateTime.DaysInMonth(l.Year, l.Month);
        var rDfm = DateTime.DaysInMonth(r.Year, r.Month);

        var dayFixOne = l.Day == r.Day
            ? 0d
            : l.Day > r.Day
                ? r.Day * 1d / rDfm - l.Day * 1d / lDfm
                : (r.Day - l.Day) * 1d / rDfm;

        return dayFixOne
               + (l.Month == r.Month ? 0 : r.Month - l.Month)
               + (l.Year == r.Year ? 0 : (r.Year - l.Year) * 12);
    }

    #endregion

    #region Elapsed

    /// <summary>
    /// 计算当前系统时间与指定时间之间的时间差
    /// </summary>
    /// <param name="dt">参照时间</param>
    /// <returns>时间差的TimeSpan对象</returns>
    /// <remarks>
    /// 使用<see cref="DateTime.Now"/>作为当前系统时间进行计算。
    /// 如果dt早于当前时间，返回的TimeSpan为正值，否则为负值。
    /// </remarks>
    /// <example>
    /// <code>
    /// var pastTime = DateTime.Now.AddHours(-2);
    /// var elapsed = pastTime.ElapsedTime();
    /// Console.WriteLine(elapsed.TotalHours); // 输出约为: 2
    /// </code>
    /// </example>
    public static TimeSpan ElapsedTime(this DateTime dt) => DateTime.Now - dt;

    /// <summary>
    /// 计算当前系统时间与指定时间之间的时间差（毫秒）
    /// </summary>
    /// <param name="dt">参照时间</param>
    /// <returns>时间差的总毫秒数（整数）</returns>
    /// <remarks>
    /// 使用<see cref="DateTime.Now"/>作为当前系统时间进行计算。
    /// 如果dt早于当前时间，返回值为正，否则为负。
    /// </remarks>
    /// <example>
    /// <code>
    /// var pastTime = DateTime.Now.AddSeconds(-5);
    /// var elapsed = pastTime.ElapsedMilliseconds(); // 结果约为5000毫秒
    /// </code>
    /// </example>
    public static int ElapsedMilliseconds(this DateTime dt) => (int)(DateTime.Now - dt).TotalMilliseconds;

    #endregion

    #region Is

    /// <summary>
    /// 判断日期时间是否在指定的时间范围内
    /// </summary>
    /// <param name="dt">要判断的日期时间</param>
    /// <param name="from">范围起始时间</param>
    /// <param name="to">范围结束时间</param>
    /// <param name="includeBoundary">是否包含边界值（起始和结束时间）</param>
    /// <returns>如果在范围内则为true，否则为false</returns>
    /// <remarks>
    /// 默认情况下包含边界值，即 dt >= from &amp;&amp; dt 	&lt;= to。
    /// 如果 includeBoundary = false，则使用开区间: dt > from &amp;&amp; dt &lt; to。
    /// </remarks>
    /// <example>
    /// <code>
    /// var start = new DateTime(2023, 1, 1);
    /// var end = new DateTime(2023, 12, 31);
    /// var date = new DateTime(2023, 6, 15);
    /// var isInRange = date.IsBetween(start, end); // 结果为true
    /// </code>
    /// </example>
    public static bool IsBetween(this DateTime dt, DateTime from, DateTime to, bool includeBoundary = true)
    {
        return includeBoundary
            ? dt >= from && dt <= to
            : dt > from && dt < to;
    }

    /// <summary>
    /// 判断日期是否在指定的日期范围内（闭区间，包含边界日期）
    /// </summary>
    /// <param name="dt">要判断的日期</param>
    /// <param name="min">范围起始日期</param>
    /// <param name="max">范围结束日期</param>
    /// <returns>如果在范围内则为true，否则为false</returns>
    /// <remarks>
    /// 此方法考虑整个日期，max日期会被视为整天（到次日凌晨前），
    /// 因此实际是判断 dt 大于等于 min 且 dt 小于 max.AddDays(1)
    /// </remarks>
    /// <example>
    /// <code>
    /// var start = new DateTime(2023, 1, 1);
    /// var end = new DateTime(2023, 1, 31);
    /// var date = new DateTime(2023, 1, 31, 23, 59, 59);
    /// var isInRange = date.IsDateBetweenWithBoundary(start, end); // 结果为true
    /// </code>
    /// </example>
    public static bool IsDateBetweenWithBoundary(this DateTime dt, DateTime min, DateTime max) =>
        dt.IsBetween(min, max.AddDays(+1), false);

    /// <summary>
    /// 判断日期是否在可为空的日期范围内（闭区间，包含边界日期）
    /// </summary>
    /// <param name="dt">要判断的日期</param>
    /// <param name="min">可为空的范围起始日期</param>
    /// <param name="max">可为空的范围结束日期</param>
    /// <returns>如果在范围内则为true，否则为false</returns>
    /// <remarks>
    /// 如果min和max都为空，返回true；
    /// 如果只有min有值，判断 dt 大于等于 min；
    /// 如果只有max有值，判断 dt 小于 max.AddDays(1)。
    /// </remarks>
    /// <example>
    /// <code>
    /// var date = new DateTime(2023, 6, 15);
    /// DateTime? minDate = new DateTime(2023, 1, 1);
    /// DateTime? maxDate = null;
    /// var isInRange = date.IsDateBetweenWithBoundary(minDate, maxDate); // 结果为true，因为date >= minDate
    /// </code>
    /// </example>
    public static bool IsDateBetweenWithBoundary(this DateTime dt, DateTime? min, DateTime? max)
    {
        if (min.HasValue && max.HasValue)
            return dt.IsDateBetweenWithBoundary(min.Value, max.Value);
        if (min.HasValue)
            return dt >= min.Value;
        if (max.HasValue)
            return dt < max.Value.AddDays(+1);
        return true;
    }

    /// <summary>
    /// 判断日期是否在指定的日期范围内（开区间，不包含边界日期）
    /// </summary>
    /// <param name="dt">要判断的日期</param>
    /// <param name="min">范围起始日期</param>
    /// <param name="max">范围结束日期</param>
    /// <returns>如果在开区间范围内则为true，否则为false</returns>
    /// <remarks>
    /// 此方法使用严格的开区间，即 dt 大于 min 且 dt 小于 max
    /// </remarks>
    /// <example>
    /// <code>
    /// var start = new DateTime(2023, 1, 1);
    /// var end = new DateTime(2023, 12, 31);
    /// var date = new DateTime(2023, 1, 1);
    /// var isInRange = date.IsDateBetweenWithoutBoundary(start, end); // 结果为false，因为date=start
    /// </code>
    /// </example>
    public static bool IsDateBetweenWithoutBoundary(this DateTime dt, DateTime min, DateTime max) =>
        dt.IsBetween(min, max, false);

    /// <summary>
    /// 判断给定时间是否为今天（当前日期）
    /// </summary>
    /// <param name="dt">要判断的时间</param>
    /// <returns>如果是今天则为true，否则为false</returns>
    /// <remarks>
    /// 只比较日期部分，忽略时间部分
    /// </remarks>
    /// <example>
    /// <code>
    /// var today = DateTime.Now;
    /// var isToday = today.IsToday(); // 结果为true
    /// 
    /// var tomorrow = DateTime.Today.AddDays(1);
    /// var isTodayTomorrow = tomorrow.IsToday(); // 结果为false
    /// </code>
    /// </example>
    public static bool IsToday(this DateTime dt) => dt.Date == DateTime.Today;

    /// <summary>
    /// 判断给定可空时间是否为今天（当前日期）
    /// </summary>
    /// <param name="dt">要判断的可空时间</param>
    /// <returns>如果是今天则为true，否则为false</returns>
    /// <remarks>
    /// 如果dt为null，将使用默认值进行比较（结果通常为false）
    /// </remarks>
    /// <example>
    /// <code>
    /// DateTime? nullDate = null;
    /// var isToday = nullDate.IsToday(); // 结果为false
    /// 
    /// DateTime? today = DateTime.Today;
    /// var isTodayValid = today.IsToday(); // 结果为true
    /// </code>
    /// </example>
    public static bool IsToday(this DateTime? dt) => dt.GetValueOrDefault().Date == DateTime.Today;

    /// <summary>
    /// 判断给定时间是否处于清晨时段（0:00-5:59）
    /// </summary>
    /// <param name="dt">要判断的时间</param>
    /// <returns>如果是清晨则为true，否则为false</returns>
    /// <example>
    /// <code>
    /// var earlyMorning = new DateTime(2023, 1, 1, 4, 30, 0);
    /// var isEarlyMorning = earlyMorning.IsEarlyMorning(); // 结果为true
    /// </code>
    /// </example>
    public static bool IsEarlyMorning(this DateTime dt)
    {
        var hour = dt.Hour;
        return hour is >= 0 and < 6;
    }

    /// <summary>
    /// 判断给定时间是否处于早晨时段（6:00-11:59）
    /// </summary>
    /// <param name="dt">要判断的时间</param>
    /// <returns>如果是早晨则为true，否则为false</returns>
    /// <example>
    /// <code>
    /// var morning = new DateTime(2023, 1, 1, 8, 30, 0);
    /// var isMorning = morning.IsMorning(); // 结果为true
    /// </code>
    /// </example>
    public static bool IsMorning(this DateTime dt)
    {
        var hour = dt.Hour;
        return hour is >= 6 and < 12;
    }

    /// <summary>
    /// 判断给定时间是否处于下午时段（12:00-17:59）
    /// </summary>
    /// <param name="dt">要判断的时间</param>
    /// <returns>如果是下午则为true，否则为false</returns>
    /// <example>
    /// <code>
    /// var afternoon = new DateTime(2023, 1, 1, 14, 30, 0);
    /// var isAfternoon = afternoon.IsAfternoon(); // 结果为true
    /// </code>
    /// </example>
    public static bool IsAfternoon(this DateTime dt)
    {
        var hour = dt.Hour;
        return hour is >= 12 and < 18;
    }

    /// <summary>
    /// 判断给定时间是否处于黄昏时段（16:00-18:59）
    /// </summary>
    /// <param name="dt">要判断的时间</param>
    /// <returns>如果是黄昏则为true，否则为false</returns>
    /// <remarks>
    /// 黄昏时段与下午有部分重叠（16:00-17:59）
    /// </remarks>
    /// <example>
    /// <code>
    /// var dusk = new DateTime(2023, 1, 1, 17, 30, 0);
    /// var isDusk = dusk.IsDusk(); // 结果为true
    /// </code>
    /// </example>
    public static bool IsDusk(this DateTime dt)
    {
        var hour = dt.Hour;
        return hour is >= 16 and < 19;
    }

    /// <summary>
    /// 判断给定时间是否处于夜晚时段（18:00-23:59或0:00-5:59）
    /// </summary>
    /// <param name="dt">要判断的时间</param>
    /// <returns>如果是夜晚则为true，否则为false</returns>
    /// <remarks>
    /// 夜晚时段与清晨时段重叠（0:00-5:59）
    /// </remarks>
    /// <example>
    /// <code>
    /// var evening1 = new DateTime(2023, 1, 1, 22, 30, 0);
    /// var isEvening1 = evening1.IsEvening(); // 结果为true
    /// 
    /// var evening2 = new DateTime(2023, 1, 1, 2, 30, 0);
    /// var isEvening2 = evening2.IsEvening(); // 结果为true
    /// </code>
    /// </example>
    public static bool IsEvening(this DateTime dt)
    {
        var hour = dt.Hour;
        return hour is >= 18 and < 24 or >= 0 and < 6;
    }

    /// <summary>
    /// 判断给定时间是否为上午（AM，0:00-11:59）
    /// </summary>
    /// <param name="dt">要判断的时间</param>
    /// <returns>如果是上午则为true，否则为false</returns>
    /// <example>
    /// <code>
    /// var morning = new DateTime(2023, 1, 1, 9, 30, 0);
    /// var isAM = morning.IsAM(); // 结果为true
    /// </code>
    /// </example>
    // ReSharper disable once InconsistentNaming
    public static bool IsAM(this DateTime dt) => dt.Hour < 12;

    /// <summary>
    /// 判断给定时间是否为下午（PM，12:00-23:59）
    /// </summary>
    /// <param name="dt">要判断的时间</param>
    /// <returns>如果是下午则为true，否则为false</returns>
    /// <example>
    /// <code>
    /// var afternoon = new DateTime(2023, 1, 1, 15, 30, 0);
    /// var isPM = afternoon.IsPM(); // 结果为true
    /// </code>
    /// </example>
    // ReSharper disable once InconsistentNaming
    public static bool IsPM(this DateTime dt) => dt.Hour >= 12;

    /// <summary>
    /// 判断指定时间是否早于比较时间
    /// </summary>
    /// <param name="dt">要判断的日期时间</param>
    /// <param name="toCompareWith">比较的基准时间</param>
    /// <param name="includeBoundary">是否将相等的时间视为早于（默认为false）</param>
    /// <returns>如果早于比较时间则为true，否则为false</returns>
    /// <remarks>
    /// 默认情况下，如果两个时间相等，返回false。
    /// 如果includeBoundary=true，则相等时也返回true。
    /// </remarks>
    /// <example>
    /// <code>
    /// var earlier = new DateTime(2023, 1, 1);
    /// var later = new DateTime(2023, 6, 1);
    /// var isEarlier = earlier.IsBefore(later); // 结果为true
    /// 
    /// var same1 = new DateTime(2023, 1, 1);
    /// var same2 = new DateTime(2023, 1, 1);
    /// var isSameEarlier = same1.IsBefore(same2); // 结果为false
    /// var isSameEarlierWithBoundary = same1.IsBefore(same2, true); // 结果为true
    /// </code>
    /// </example>
    [Pure]
    public static bool IsBefore(this DateTime dt, DateTime toCompareWith, bool includeBoundary = false) =>
        includeBoundary ? dt <= toCompareWith : dt < toCompareWith;

    /// <summary>
    /// 判断指定可空时间是否早于比较时间
    /// </summary>
    /// <param name="dt">要判断的可空日期时间</param>
    /// <param name="toCompareWith">比较的基准可空时间</param>
    /// <param name="includeBoundary">是否将相等的时间视为早于（默认为false）</param>
    /// <returns>如果早于比较时间则为true，否则为false</returns>
    /// <remarks>
    /// 如果任何一个参数为null，返回false
    /// </remarks>
    /// <example>
    /// <code>
    /// DateTime? earlier = new DateTime(2023, 1, 1);
    /// DateTime? later = new DateTime(2023, 6, 1);
    /// var isEarlier = earlier.IsBefore(later); // 结果为true
    /// 
    /// DateTime? nullDate = null;
    /// var isNullEarlier = nullDate.IsBefore(later); // 结果为false
    /// </code>
    /// </example>
    [Pure]
    public static bool IsBefore(this DateTime? dt, DateTime? toCompareWith, bool includeBoundary = false)
    {
        if (!dt.HasValue || !toCompareWith.HasValue)
            return false;
        return dt.Value.IsBefore(toCompareWith.Value, includeBoundary);
    }

    /// <summary>
    /// 判断指定时间是否晚于比较时间
    /// </summary>
    /// <param name="dt">要判断的日期时间</param>
    /// <param name="toCompareWith">比较的基准时间</param>
    /// <param name="includeBoundary">是否将相等的时间视为晚于（默认为false）</param>
    /// <returns>如果晚于比较时间则为true，否则为false</returns>
    /// <remarks>
    /// 默认情况下，如果两个时间相等，返回false。
    /// 如果includeBoundary=true，则相等时也返回true。
    /// </remarks>
    /// <example>
    /// <code>
    /// var earlier = new DateTime(2023, 1, 1);
    /// var later = new DateTime(2023, 6, 1);
    /// var isLater = later.IsAfter(earlier); // 结果为true
    /// 
    /// var same1 = new DateTime(2023, 1, 1);
    /// var same2 = new DateTime(2023, 1, 1);
    /// var isSameLater = same1.IsAfter(same2); // 结果为false
    /// var isSameLaterWithBoundary = same1.IsAfter(same2, true); // 结果为true
    /// </code>
    /// </example>
    [Pure]
    public static bool IsAfter(this DateTime dt, DateTime toCompareWith, bool includeBoundary = false) =>
        includeBoundary ? dt >= toCompareWith : dt > toCompareWith;

    /// <summary>
    /// 判断指定可空时间是否晚于比较时间
    /// </summary>
    /// <param name="dt">要判断的可空日期时间</param>
    /// <param name="toCompareWith">比较的基准可空时间</param>
    /// <param name="includeBoundary">是否将相等的时间视为晚于（默认为false）</param>
    /// <returns>如果晚于比较时间则为true，否则为false</returns>
    /// <remarks>
    /// 如果任何一个参数为null，返回false
    /// </remarks>
    /// <example>
    /// <code>
    /// DateTime? earlier = new DateTime(2023, 1, 1);
    /// DateTime? later = new DateTime(2023, 6, 1);
    /// var isLater = later.IsAfter(earlier); // 结果为true
    /// 
    /// DateTime? nullDate = null;
    /// var isNullLater = nullDate.IsAfter(earlier); // 结果为false
    /// </code>
    /// </example>
    [Pure]
    public static bool IsAfter(this DateTime? dt, DateTime? toCompareWith, bool includeBoundary = false)
    {
        if (!dt.HasValue || !toCompareWith.HasValue)
            return false;
        return dt.Value.IsAfter(toCompareWith.Value, includeBoundary);
    }

    /// <summary>
    /// 判断指定时间是否在当前系统时间之后（未来）
    /// </summary>
    /// <param name="dt">要判断的日期时间</param>
    /// <param name="includeBoundary">是否将相等的时间视为未来（默认为false）</param>
    /// <returns>如果在当前时间之后则为true，否则为false</returns>
    /// <remarks>
    /// 使用<see cref="DateTime.Now"/>作为比较基准
    /// </remarks>
    /// <example>
    /// <code>
    /// var future = DateTime.Now.AddDays(1);
    /// var isFuture = future.IsInTheFuture(); // 结果为true
    /// 
    /// var past = DateTime.Now.AddDays(-1);
    /// var isPastFuture = past.IsInTheFuture(); // 结果为false
    /// </code>
    /// </example>
    public static bool IsInTheFuture(this DateTime dt, bool includeBoundary = false) =>
        dt.IsAfter(DateTime.Now, includeBoundary);

    /// <summary>
    /// 判断指定可空时间是否在当前系统时间之后（未来）
    /// </summary>
    /// <param name="dt">要判断的可空日期时间</param>
    /// <param name="includeBoundary">是否将相等的时间视为未来（默认为false）</param>
    /// <returns>如果在当前时间之后则为true，否则为false</returns>
    /// <remarks>
    /// 如果dt为null，返回false
    /// </remarks>
    /// <example>
    /// <code>
    /// DateTime? future = DateTime.Now.AddDays(1);
    /// var isFuture = future.IsInTheFuture(); // 结果为true
    /// 
    /// DateTime? nullDate = null;
    /// var isNullFuture = nullDate.IsInTheFuture(); // 结果为false
    /// </code>
    /// </example>
    public static bool IsInTheFuture(this DateTime? dt, bool includeBoundary = false)
    {
        if (!dt.HasValue)
            return false;
        return dt.Value.IsInTheFuture(includeBoundary);
    }

    /// <summary>
    /// 判断指定时间是否在当前系统时间之前（过去）
    /// </summary>
    /// <param name="dt">要判断的日期时间</param>
    /// <param name="includeBoundary">是否将相等的时间视为过去（默认为false）</param>
    /// <returns>如果在当前时间之前则为true，否则为false</returns>
    /// <remarks>
    /// 使用<see cref="DateTime.Now"/>作为比较基准
    /// </remarks>
    /// <example>
    /// <code>
    /// var past = DateTime.Now.AddDays(-1);
    /// var isPast = past.IsInThePast(); // 结果为true
    /// 
    /// var future = DateTime.Now.AddDays(1);
    /// var isFuturePast = future.IsInThePast(); // 结果为false
    /// </code>
    /// </example>
    public static bool IsInThePast(this DateTime dt, bool includeBoundary = false) =>
        dt.IsBefore(DateTime.Now, includeBoundary);

    /// <summary>
    /// 判断指定可空时间是否在当前系统时间之前（过去）
    /// </summary>
    /// <param name="dt">要判断的可空日期时间</param>
    /// <param name="includeBoundary">是否将相等的时间视为过去（默认为false）</param>
    /// <returns>如果在当前时间之前则为true，否则为false</returns>
    /// <remarks>
    /// 如果dt为null，返回false
    /// </remarks>
    /// <example>
    /// <code>
    /// DateTime? past = DateTime.Now.AddDays(-1);
    /// var isPast = past.IsInThePast(); // 结果为true
    /// 
    /// DateTime? nullDate = null;
    /// var isNullPast = nullDate.IsInThePast(); // 结果为false
    /// </code>
    /// </example>
    public static bool IsInThePast(this DateTime? dt, bool includeBoundary = false)
    {
        if (!dt.HasValue)
            return false;
        return dt.Value.IsInThePast(includeBoundary);
    }

    /// <summary>
    /// 判断指定日期是否为工作日（周一至周五）
    /// </summary>
    /// <param name="dt">要判断的日期</param>
    /// <returns>如果是工作日则为true，否则为false</returns>
    /// <remarks>
    /// 工作日定义为周一至周五，与周末（周六日）相反
    /// </remarks>
    /// <example>
    /// <code>
    /// var monday = new DateTime(2023, 1, 2); // 周一
    /// var isWeekday = monday.IsWeekday(); // 结果为true
    /// 
    /// var saturday = new DateTime(2023, 1, 7); // 周六
    /// var isSaturdayWeekday = saturday.IsWeekday(); // 结果为false
    /// </code>
    /// </example>
    public static bool IsWeekday(this DateTime dt) => !dt.IsWeekend();

    /// <summary>
    /// 判断指定可空日期是否为工作日（周一至周五）
    /// </summary>
    /// <param name="dt">要判断的可空日期</param>
    /// <returns>如果是工作日则为true，否则为false</returns>
    /// <remarks>
    /// 如果dt为null，将使用默认值判断
    /// </remarks>
    /// <example>
    /// <code>
    /// DateTime? monday = new DateTime(2023, 1, 2); // 周一
    /// var isWeekday = monday.IsWeekday(); // 结果为true
    /// 
    /// DateTime? nullDate = null;
    /// var isNullWeekday = nullDate.IsWeekday(); // 根据默认日期判断结果
    /// </code>
    /// </example>
    public static bool IsWeekday(this DateTime? dt) => dt.GetValueOrDefault().IsWeekday();

    /// <summary>
    /// 判断指定日期是否为周末（周六或周日）
    /// </summary>
    /// <param name="dt">要判断的日期</param>
    /// <returns>如果是周末则为true，否则为false</returns>
    /// <example>
    /// <code>
    /// var sunday = new DateTime(2023, 1, 1); // 周日
    /// var isWeekend = sunday.IsWeekend(); // 结果为true
    /// 
    /// var wednesday = new DateTime(2023, 1, 4); // 周三
    /// var isWednesdayWeekend = wednesday.IsWeekend(); // 结果为false
    /// </code>
    /// </example>
    public static bool IsWeekend(this DateTime dt) => dt.DayOfWeek == DayOfWeek.Sunday || dt.DayOfWeek == DayOfWeek.Saturday;

    /// <summary>
    /// 判断指定可空日期是否为周末（周六或周日）
    /// </summary>
    /// <param name="dt">要判断的可空日期</param>
    /// <returns>如果是周末则为true，否则为false</returns>
    /// <remarks>
    /// 如果dt为null，将使用默认值判断
    /// </remarks>
    /// <example>
    /// <code>
    /// DateTime? saturday = new DateTime(2023, 1, 7); // 周六
    /// var isWeekend = saturday.IsWeekend(); // 结果为true
    /// 
    /// DateTime? nullDate = null;
    /// var isNullWeekend = nullDate.IsWeekend(); // 根据默认日期判断结果
    /// </code>
    /// </example>
    public static bool IsWeekend(this DateTime? dt) => dt.GetValueOrDefault().IsWeekend();

    /// <summary>
    /// 判断两个日期是否为同一天
    /// </summary>
    /// <param name="dt">第一个日期</param>
    /// <param name="date">第二个日期</param>
    /// <returns>如果是同一天则为true，否则为false</returns>
    /// <remarks>
    /// 比较年、月、日是否相等，忽略时间部分
    /// </remarks>
    /// <example>
    /// <code>
    /// var date1 = new DateTime(2023, 1, 1, 10, 30, 0);
    /// var date2 = new DateTime(2023, 1, 1, 22, 15, 0);
    /// var isSameDay = date1.IsSameDay(date2); // 结果为true
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSameDay(this DateTime dt, DateTime date) => DateJudge.IsSameDay(dt, date);

    /// <summary>
    /// 判断两个可空日期是否为同一天
    /// </summary>
    /// <param name="dt">第一个可空日期</param>
    /// <param name="date">第二个可空日期</param>
    /// <returns>如果是同一天则为true，否则为false</returns>
    /// <remarks>
    /// 如果任何一个日期为null，返回false
    /// </remarks>
    /// <example>
    /// <code>
    /// DateTime? date1 = new DateTime(2023, 1, 1, 10, 30, 0);
    /// DateTime? date2 = new DateTime(2023, 1, 1, 22, 15, 0);
    /// var isSameDay = date1.IsSameDay(date2); // 结果为true
    /// 
    /// DateTime? nullDate = null;
    /// var isNullSameDay = date1.IsSameDay(nullDate); // 结果为false
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSameDay(this DateTime? dt, DateTime? date) => DateJudge.IsSameDay(dt, date);

    /// <summary>
    /// 判断两个日期是否为同一月
    /// </summary>
    /// <param name="dt">第一个日期</param>
    /// <param name="date">第二个日期</param>
    /// <returns>如果是同一月则为true，否则为false</returns>
    /// <remarks>
    /// 比较年和月是否相等，忽略日和时间部分
    /// </remarks>
    /// <example>
    /// <code>
    /// var date1 = new DateTime(2023, 6, 1);
    /// var date2 = new DateTime(2023, 6, 30);
    /// var isSameMonth = date1.IsSameMonth(date2); // 结果为true
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSameMonth(this DateTime dt, DateTime date) => DateJudge.IsSameMonth(dt, date);

    /// <summary>
    /// 判断两个可空日期是否为同一月
    /// </summary>
    /// <param name="dt">第一个可空日期</param>
    /// <param name="date">第二个可空日期</param>
    /// <returns>如果是同一月则为true，否则为false</returns>
    /// <remarks>
    /// 如果任何一个日期为null，返回false
    /// </remarks>
    /// <example>
    /// <code>
    /// DateTime? date1 = new DateTime(2023, 6, 1);
    /// DateTime? date2 = new DateTime(2023, 6, 30);
    /// var isSameMonth = date1.IsSameMonth(date2); // 结果为true
    /// 
    /// DateTime? nullDate = null;
    /// var isNullSameMonth = date1.IsSameMonth(nullDate); // 结果为false
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSameMonth(this DateTime? dt, DateTime? date) => DateJudge.IsSameMonth(dt, date);

    /// <summary>
    /// 判断两个日期是否为同一年
    /// </summary>
    /// <param name="dt">第一个日期</param>
    /// <param name="date">第二个日期</param>
    /// <returns>如果是同一年则为true，否则为false</returns>
    /// <remarks>
    /// 只比较年份是否相等，忽略月、日和时间部分
    /// </remarks>
    /// <example>
    /// <code>
    /// var date1 = new DateTime(2023, 1, 15);
    /// var date2 = new DateTime(2023, 12, 31);
    /// var isSameYear = date1.IsSameYear(date2); // 结果为true
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSameYear(this DateTime dt, DateTime date) => DateJudge.IsSameYear(dt, date);

    /// <summary>
    /// 判断两个可空日期是否为同一年
    /// </summary>
    /// <param name="dt">第一个可空日期</param>
    /// <param name="date">第二个可空日期</param>
    /// <returns>如果是同一年则为true，否则为false</returns>
    /// <remarks>
    /// 如果任何一个日期为null，返回false
    /// </remarks>
    /// <example>
    /// <code>
    /// DateTime? date1 = new DateTime(2023, 1, 15);
    /// DateTime? date2 = new DateTime(2023, 12, 31);
    /// var isSameYear = date1.IsSameYear(date2); // 结果为true
    /// 
    /// DateTime? nullDate = null;
    /// var isNullSameYear = date1.IsSameYear(nullDate); // 结果为false
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSameYear(this DateTime? dt, DateTime? date) => DateJudge.IsSameYear(dt, date);

    /// <summary>
    /// 判断两个日期的日期部分是否相等（忽略时间部分）
    /// </summary>
    /// <param name="dt">第一个日期</param>
    /// <param name="date">第二个日期</param>
    /// <returns>如果日期部分相等则为true，否则为false</returns>
    /// <remarks>
    /// 这是IsSameDay方法的别名，用于语义上更清晰地表示日期相等
    /// </remarks>
    /// <example>
    /// <code>
    /// var date1 = new DateTime(2023, 1, 1, 10, 30, 0);
    /// var date2 = new DateTime(2023, 1, 1, 22, 15, 0);
    /// var isDateEqual = date1.IsDateEqual(date2); // 结果为true
    /// </code>
    /// </example>
    public static bool IsDateEqual(this DateTime dt, DateTime date) => dt.IsSameDay(date);

    /// <summary>
    /// 判断两个日期时间的时间部分是否相等（忽略日期部分）
    /// </summary>
    /// <param name="dt">第一个日期时间</param>
    /// <param name="date">第二个日期时间</param>
    /// <returns>如果时间部分相等则为true，否则为false</returns>
    /// <remarks>
    /// 比较时、分、秒、毫秒是否相等，忽略年、月、日
    /// </remarks>
    /// <example>
    /// <code>
    /// var date1 = new DateTime(2023, 1, 1, 10, 30, 15);
    /// var date2 = new DateTime(2023, 6, 15, 10, 30, 15);
    /// var isTimeEqual = date1.IsTimeEqual(date2); // 结果为true
    /// </code>
    /// </example>
    public static bool IsTimeEqual(this DateTime dt, DateTime date) => dt.TimeOfDay == date.TimeOfDay;

    /// <summary>
    /// 判断给定日期所在年份是否为闰年
    /// </summary>
    /// <param name="dt">要判断的日期</param>
    /// <returns>如果是闰年则为true，否则为false</returns>
    /// <remarks>
    /// 闰年的定义：
    /// 1. 能被4整除但不能被100整除的年份
    /// 2. 能被400整除的年份
    /// </remarks>
    /// <example>
    /// <code>
    /// var date2020 = new DateTime(2020, 1, 1);
    /// var isLeap2020 = date2020.IsLeapYear(); // 结果为true（2020是闰年）
    /// 
    /// var date2023 = new DateTime(2023, 1, 1);
    /// var isLeap2023 = date2023.IsLeapYear(); // 结果为false（2023不是闰年）
    /// </code>
    /// </example>
    public static bool IsLeapYear(this DateTime dt) => DateTime.IsLeapYear(dt.Year);

    #endregion

    #region Round

    /// <summary>
    /// 将日期时间四舍五入到指定精度
    /// </summary>
    /// <param name="dt">要四舍五入的日期时间</param>
    /// <param name="rt">四舍五入的精度</param>
    /// <returns>四舍五入后的日期时间</returns>
    /// <remarks>
    /// 四舍五入规则：<br />
    /// - 舍入到秒：毫秒>=500则进一秒 <br />
    /// - 舍入到分钟：秒>=30则进一分钟 <br />
    /// - 舍入到小时：分钟>=30则进一小时 <br />
    /// - 舍入到天：小时>=12则进一天
    /// </remarks>
    /// <example>
    /// <code>
    /// var time = new DateTime(2023, 1, 1, 12, 29, 45, 600);
    /// 
    /// var roundToSecond = time.Round(RoundTo.Second); // 2023-01-01 12:29:46
    /// var roundToMinute = time.Round(RoundTo.Minute); // 2023-01-01 12:30:00
    /// var roundToHour = time.Round(RoundTo.Hour); // 2023-01-01 12:00:00
    /// var roundToDay = time.Round(RoundTo.Day); // 2023-01-02 00:00:00（因为12>=12）
    /// </code>
    /// </example>
    /// <exception cref="ArgumentOutOfRangeException">当rt参数不是有效的RoundTo枚举值时抛出</exception>
    public static DateTime Round(this DateTime dt, RoundTo rt)
    {
        DateTime rounded;
        switch (rt)
        {
            case RoundTo.Second:
            {
                rounded = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Kind);
                if (dt.Millisecond >= 500)
                    rounded = rounded.AddSeconds(1);
                break;
            }
            case RoundTo.Minute:
            {
                rounded = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0, dt.Kind);
                if (dt.Second >= 30)
                    rounded = rounded.AddMinutes(1);
                break;
            }
            case RoundTo.Hour:
            {
                rounded = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0, dt.Kind);
                if (dt.Minute >= 30)
                    rounded = rounded.AddHours(1);
                break;
            }
            case RoundTo.Day:
            {
                rounded = new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0, dt.Kind);
                if (dt.Hour >= 12)
                    rounded = rounded.AddDays(1);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(rt));
        }
        return rounded;
    }

    #endregion

    #region To

    /// <summary>
    /// 将日期时间转换为UTC时间格式（不改变值，只改变Kind属性）
    /// </summary>
    /// <param name="dt">要转换的日期时间</param>
    /// <returns>具有相同时间值但Kind属性为Utc的新日期时间实例</returns>
    /// <remarks>
    /// 此方法不进行时区转换，仅将Kind属性设置为Utc
    /// </remarks>
    /// <example>
    /// <code>
    /// var localTime = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Local);
    /// var utcTime = localTime.ToUtc();
    /// 
    /// Console.WriteLine(utcTime.Kind); // 输出: Utc
    /// Console.WriteLine(utcTime); // 输出: 2023-01-01 12:00:00（相同的时间值）
    /// </code>
    /// </example>
    public static DateTime ToUtc(this DateTime dt) => new(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, DateTimeKind.Utc);

    /// <summary>
    /// 获取当前时刻的中国标准时间（CST，UTC+8）
    /// </summary>
    /// <param name="dt">参数未使用，仅作为扩展方法使用</param>
    /// <returns>当前的中国标准时间</returns>
    /// <remarks>
    /// 此方法特别适用于在Linux环境下获取正确的中国时间。
    /// 使用NodaTime库来确保时区转换的正确性。
    /// </remarks>
    /// <example>
    /// <code>
    /// var now = DateTime.Now;
    /// var cstTime = now.ToCst();
    /// Console.WriteLine($"CST时间: {cstTime}"); // 输出北京时间
    /// </code>
    /// </example>
    public static DateTime ToCst(this DateTime dt)
    {
        var now = SystemClock.Instance.GetCurrentInstant();
        var shanghaiZone = DateTimeZoneProviders.Tzdb["Asia/Shanghai"];
        return now.InZone(shanghaiZone).ToDateTimeUnspecified();
    }

    /// <summary>
    /// 计算当前时间与Unix纪元（1970-01-01）之间的时间间隔
    /// </summary>
    /// <param name="dt">要计算的日期时间</param>
    /// <returns>与Unix纪元之间的TimeSpan</returns>
    /// <remarks>
    /// Unix纪元定义为1970年1月1日 00:00:00 UTC
    /// </remarks>
    /// <example>
    /// <code>
    /// var date = new DateTime(2023, 1, 1);
    /// var epochSpan = date.ToEpochTimeSpan();
    /// Console.WriteLine(epochSpan.TotalDays); // 输出从1970-01-01到2023-01-01的总天数
    /// </code>
    /// </example>
    public static TimeSpan ToEpochTimeSpan(this DateTime dt) => dt.Subtract(DateTimeFactory.Create(1970, 1, 1));

    /// <summary>
    /// 将DateTime转换为NodaTime的 <see cref="LocalDateTime"/>
    /// </summary>
    /// <param name="dt">要转换的日期时间</param>
    /// <returns>等效的NodaTime LocalDateTime</returns>
    /// <remarks>
    /// LocalDateTime不包含时区信息，仅表示本地日期和时间
    /// </remarks>
    /// <example>
    /// <code>
    /// var dateTime = new DateTime(2023, 1, 1, 12, 30, 0);
    /// var localDateTime = dateTime.ToLocalDateTime();
    /// Console.WriteLine(localDateTime); // 输出: 2023-01-01T12:30:00
    /// </code>
    /// </example>
    public static LocalDateTime ToLocalDateTime(this DateTime dt) => LocalDateTime.FromDateTime(dt);

    /// <summary>
    /// 将DateTime转换为NodaTime的 <see cref="LocalDate"/>（仅日期部分）
    /// </summary>
    /// <param name="dt">要转换的日期时间</param>
    /// <returns>等效的NodaTime LocalDate（只包含年月日）</returns>
    /// <example>
    /// <code>
    /// var dateTime = new DateTime(2023, 1, 1, 12, 30, 0);
    /// var localDate = dateTime.ToLocalDate();
    /// Console.WriteLine(localDate); // 输出: 2023-01-01
    /// </code>
    /// </example>
    public static LocalDate ToLocalDate(this DateTime dt) => LocalDate.FromDateTime(dt);

    /// <summary>
    /// 将DateTime转换为NodaTime的 <see cref="LocalTime"/>（仅时间部分）
    /// </summary>
    /// <param name="dt">要转换的日期时间</param>
    /// <returns>等效的NodaTime LocalTime（只包含时分秒毫秒）</returns>
    /// <remarks>
    /// 此方法提取日期时间中的时间部分，创建不含日期信息的NodaTime LocalTime对象
    /// </remarks>
    /// <example>
    /// <code>
    /// var dateTime = new DateTime(2023, 1, 1, 12, 30, 45, 500);
    /// var nodaTime = dateTime.ToNodaLocalTime();
    /// Console.WriteLine(nodaTime); // 输出: 12:30:45.500
    /// </code>
    /// </example>
    public static LocalTime ToNodaLocalTime(this DateTime dt) => new(dt.Hour, dt.Minute, dt.Second, dt.Millisecond);

    /// <summary>
    /// 将日期时间转换为字节数组
    /// </summary>
    /// <param name="dt">要转换的日期时间</param>
    /// <returns>表示日期时间的字节数组</returns>
    /// <remarks>
    /// 使用 <see cref="DateTime.ToBinary"/> 方法将日期时间转换为长整数，
    /// 然后使用 <see cref="BitConverter.GetBytes(long)"/> 将长整数转换为字节数组
    /// </remarks>
    public static byte[] ToBytes(this DateTime dt) => BitConverter.GetBytes(dt.ToBinary());

    #endregion
}