using System;
using System.Globalization;

namespace Bing.Date.Chinese;

/// <summary>
/// 提供公历自然日与中国农历日期之间的转换。
/// </summary>
/// <remarks>
/// 日期字段入口不转换时区；时间点入口按固定 UTC+08:00 确定自然日。
/// 内部历法访问由锁保护，不向调用方暴露可变历法实例。
/// </remarks>
public static class ChineseLunarCalendar
{
    /// <summary>
    /// 保护内部历法实例的同步对象。
    /// </summary>
    private static readonly object CalendarLock = new();

    /// <summary>
    /// 仅在内部使用的中国农历历法实例。
    /// </summary>
    private static readonly ChineseLunisolarCalendar Calendar = new();

    /// <summary>
    /// 当前历法支持的最早公历自然日。
    /// </summary>
    private static readonly DateTime Minimum = Calendar.MinSupportedDateTime.Date;

    /// <summary>
    /// 当前历法支持的最晚公历自然日。
    /// </summary>
    private static readonly DateTime Maximum = Calendar.MaxSupportedDateTime.Date;

    /// <summary>
    /// 当前历法支持的最小农历年份。
    /// </summary>
    private static readonly int MinimumYear = Calendar.GetYear(Minimum);

    /// <summary>
    /// 当前历法支持的最大农历年份。
    /// </summary>
    private static readonly int MaximumYear = Calendar.GetYear(Maximum);

    /// <summary>
    /// 北京时间与 UTC 的固定偏移 ticks。
    /// </summary>
    private const long BeijingOffsetTicks = 8 * TimeSpan.TicksPerHour;

    /// <summary>
    /// 获取支持的最早公历日期，时间为午夜且 Kind 为 Unspecified。
    /// </summary>
    public static DateTime MinDate => Minimum;

    /// <summary>
    /// 获取支持的最晚公历日期，时间为午夜且 Kind 为 Unspecified。
    /// </summary>
    public static DateTime MaxDate => Maximum;

    /// <summary>
    /// 按输入的年月日字段转换农历日期。
    /// </summary>
    /// <param name="date">公历日期；忽略时间和 Kind，不作时区转换。</param>
    /// <returns>对应的农历日期。</returns>
    /// <exception cref="ArgumentOutOfRangeException">公历日期超出历法支持范围。</exception>
    public static LunarDate FromDate(DateTime date)
    {
        if (!TryFromDate(date, out var result))
            throw new ArgumentOutOfRangeException(nameof(date), date, "公历日期超出农历支持范围。");
        return result;
    }

    /// <summary>
    /// 按输入年月日字段尝试转换农历日期。
    /// </summary>
    /// <param name="date">公历日期；忽略时间和 Kind。</param>
    /// <param name="result">成功时为农历日期，失败时为默认无效值。</param>
    /// <returns>日期在支持范围内为 true，否则为 false。</returns>
    public static bool TryFromDate(DateTime date, out LunarDate result)
    {
        var ticks = date.Date.Ticks;
        if (ticks < Minimum.Ticks || ticks > Maximum.Ticks)
        {
            result = default;
            return false;
        }

        lock (CalendarLock)
            result = FromDateCore(new DateTime(ticks, DateTimeKind.Unspecified));
        return true;
    }

    /// <summary>
    /// 将时间点所在的固定 UTC+08:00 自然日转换为农历日期。
    /// </summary>
    /// <param name="instant">任意偏移的时间点。</param>
    /// <returns>北京时间自然日对应的农历日期。</returns>
    /// <exception cref="ArgumentOutOfRangeException">北京时间自然日超出历法支持范围。</exception>
    public static LunarDate FromInstant(DateTimeOffset instant)
    {
        if (!TryFromInstant(instant, out var result))
            throw new ArgumentOutOfRangeException(nameof(instant), instant, "北京时间日期超出农历支持范围。");
        return result;
    }

    /// <summary>
    /// 尝试转换时间点所在的固定 UTC+08:00 自然日。
    /// </summary>
    /// <param name="instant">任意偏移的时间点。</param>
    /// <param name="result">成功时为农历日期，失败时为默认无效值。</param>
    /// <returns>北京时间自然日在支持范围内为 true，否则为 false。</returns>
    public static bool TryFromInstant(DateTimeOffset instant, out LunarDate result)
    {
        var beijingTicks = instant.UtcDateTime.Ticks + BeijingOffsetTicks;
        if (beijingTicks < Minimum.Ticks || beijingTicks >= Maximum.Ticks + TimeSpan.TicksPerDay)
        {
            result = default;
            return false;
        }

        return TryFromDate(new DateTime(beijingTicks, DateTimeKind.Unspecified), out result);
    }

    /// <summary>
    /// 按农历显示年月日及闰月标志创建日期。
    /// </summary>
    /// <param name="year">历法支持范围内的农历年份。</param>
    /// <param name="month">显示月份，范围为 1～12。</param>
    /// <param name="day">当月日期，范围取决于实际月份天数。</param>
    /// <param name="isLeapMonth">是否选择该显示月份的闰月。</param>
    /// <returns>已验证的农历日期。</returns>
    /// <exception cref="ArgumentOutOfRangeException">年、月或日超出允许范围。</exception>
    /// <exception cref="ArgumentException">指定年度不存在所选闰月。</exception>
    public static LunarDate Create(int year, int month, int day, bool isLeapMonth = false)
    {
        var failure = TryCreateCore(year, month, day, isLeapMonth, out var result);
        switch (failure)
        {
            case CreateFailure.Year:
                throw new ArgumentOutOfRangeException(nameof(year), year, "农历年份超出支持范围。");
            case CreateFailure.Month:
                throw new ArgumentOutOfRangeException(nameof(month), month, "农历显示月份必须在 1～12 之间。");
            case CreateFailure.Day:
                throw new ArgumentOutOfRangeException(nameof(day), day, "农历日超出该月的有效范围。");
            case CreateFailure.LeapMonth:
                throw new ArgumentException("指定年度不存在该闰月。", nameof(isLeapMonth));
            default:
                return result;
        }
    }

    /// <summary>
    /// 尝试按农历显示年月日及闰月标志创建日期。
    /// </summary>
    /// <param name="year">农历年份。</param>
    /// <param name="month">显示月份。</param>
    /// <param name="day">农历日。</param>
    /// <param name="isLeapMonth">是否选择闰月。</param>
    /// <param name="result">成功时为农历日期，失败时为默认无效值。</param>
    /// <returns>输入有效为 true，否则为 false。</returns>
    public static bool TryCreate(int year, int month, int day, bool isLeapMonth, out LunarDate result) =>
        TryCreateCore(year, month, day, isLeapMonth, out result) == CreateFailure.None;

    /// <summary>
    /// 将已验证的农历分量转换为对应公历日期。
    /// </summary>
    /// <param name="year">农历年份。</param>
    /// <param name="month">显示月份。</param>
    /// <param name="day">农历日。</param>
    /// <param name="isLeapMonth">是否选择闰月。</param>
    /// <param name="result">成功时的农历日期。</param>
    /// <returns>失败原因；成功时为 None。</returns>
    private static CreateFailure TryCreateCore(int year, int month, int day, bool isLeapMonth, out LunarDate result)
    {
        result = default;
        if (year < MinimumYear || year > MaximumYear)
            return CreateFailure.Year;
        if (month < 1 || month > 12)
            return CreateFailure.Month;
        if (day < 1 || day > 30)
            return CreateFailure.Day;

        lock (CalendarLock)
        {
            var leapSlot = Calendar.GetLeapMonth(year);
            if (isLeapMonth && leapSlot != month + 1)
                return CreateFailure.LeapMonth;
            var slot = isLeapMonth ? leapSlot : leapSlot > 0 && month >= leapSlot ? month + 1 : month;
            if (day > Calendar.GetDaysInMonth(year, slot))
                return CreateFailure.Day;

            var date = Calendar.ToDateTime(year, slot, day, 0, 0, 0, 0);
            result = new LunarDate(date, year, month, day, slot, isLeapMonth);
            return CreateFailure.None;
        }
    }

    /// <summary>
    /// 在持有历法锁时转换已经检查范围的公历自然日。
    /// </summary>
    /// <param name="date">午夜 Unspecified 的有效公历日期。</param>
    /// <returns>对应的农历日期。</returns>
    private static LunarDate FromDateCore(DateTime date)
    {
        var year = Calendar.GetYear(date);
        var slot = Calendar.GetMonth(date);
        var day = Calendar.GetDayOfMonth(date);
        var leapSlot = Calendar.GetLeapMonth(year);
        var month = leapSlot > 0 && slot >= leapSlot ? slot - 1 : slot;
        return new LunarDate(date, year, month, day, slot, leapSlot > 0 && slot == leapSlot);
    }

    /// <summary>
    /// 获取已验证农历日期所在月份的天数。
    /// </summary>
    /// <param name="date">有效农历日期。</param>
    /// <returns>农历月天数。</returns>
    internal static int DaysInMonth(LunarDate date)
    {
        lock (CalendarLock)
            return Calendar.GetDaysInMonth(date.Year, date.CalendarMonth);
    }

    /// <summary>
    /// 获取已验证农历日期所在年份的天数。
    /// </summary>
    /// <param name="date">有效农历日期。</param>
    /// <returns>农历年天数。</returns>
    internal static int DaysInYear(LunarDate date)
    {
        lock (CalendarLock)
            return Calendar.GetDaysInYear(date.Year);
    }

    /// <summary>
    /// 使用共享历法判断日期所属农历年是否为闰年。
    /// </summary>
    /// <param name="date">待判断的公历日期。</param>
    /// <returns>农历年存在闰月时为 true。</returns>
    internal static bool IsLeapYear(DateTime date)
    {
        lock (CalendarLock)
            return ChineseDateHelper.IsLeapYear(Calendar, date);
    }

    /// <summary>
    /// 使用共享历法判断日期是否处于农历闰月。
    /// </summary>
    /// <param name="date">待判断的公历日期。</param>
    /// <returns>日期处于闰月时为 true。</returns>
    internal static bool IsLeapMonth(DateTime date)
    {
        lock (CalendarLock)
            return ChineseDateHelper.IsLeapMonth(Calendar, date);
    }

    /// <summary>
    /// 使用共享历法判断日期是否为农历闰日。
    /// </summary>
    /// <param name="date">待判断的公历日期。</param>
    /// <returns>日期为农历闰日时为 true。</returns>
    internal static bool IsLeapDay(DateTime date)
    {
        lock (CalendarLock)
            return ChineseDateHelper.IsLeapDay(Calendar, date);
    }

    /// <summary>
    /// 按农历实际月份顺序移动有效日期。
    /// </summary>
    /// <param name="date">有效农历日期。</param>
    /// <param name="months">有符号农历月偏移。</param>
    /// <param name="overflow">目标月缺少原日号时的处理方式。</param>
    /// <returns>目标农历日期。</returns>
    internal static LunarDate AddMonths(LunarDate date, int months, LunarDayOverflow overflow)
    {
        if (overflow != LunarDayOverflow.Reject && overflow != LunarDayOverflow.ClampToLastDay)
            throw new ArgumentOutOfRangeException(nameof(overflow), overflow, "不支持的农历日期截断策略。");
        if (months == 0)
            return date;

        lock (CalendarLock)
        {
            DateTime target;
            try
            {
                target = Calendar.AddMonths(date.ToDateTime(), months);
            }
            catch (ArgumentOutOfRangeException exception)
            {
                throw new ArgumentOutOfRangeException(nameof(months), months, exception.Message);
            }

            var result = FromDateCore(target);
            if (overflow == LunarDayOverflow.Reject && result.Day != date.Day)
                throw new ArgumentOutOfRangeException(nameof(months), months, "目标农历月不存在原日期的日号。");
            return result;
        }
    }

    /// <summary>
    /// 将农历日期映射到目标年份。
    /// </summary>
    /// <remarks>使用共享历法锁保护转换，并按指定策略处理缺失的闰月与日号。</remarks>
    /// <param name="date">原农历日期。</param>
    /// <param name="targetYear">目标农历年份。</param>
    /// <param name="missingLeapMonth">目标年份缺少原闰月时的处理策略。</param>
    /// <param name="missingDay">目标月份缺少原日号时的处理策略。</param>
    /// <param name="yearParameter">年份超界异常使用的参数名。</param>
    /// <param name="yearValue">年份超界异常携带的原始参数值。</param>
    /// <returns>应用缺月和缺日策略后的目标农历日期。</returns>
    internal static LunarDate MoveToYear(LunarDate date, long targetYear, LunarLeapMonthOverflow missingLeapMonth,
        LunarDayOverflow missingDay, string yearParameter, int yearValue)
    {
        if (missingLeapMonth != LunarLeapMonthOverflow.Reject && missingLeapMonth != LunarLeapMonthOverflow.UseRegularMonth)
            throw new ArgumentOutOfRangeException(nameof(missingLeapMonth), missingLeapMonth, "不支持的闰月缺失策略。");
        if (missingDay != LunarDayOverflow.Reject && missingDay != LunarDayOverflow.ClampToLastDay)
            throw new ArgumentOutOfRangeException(nameof(missingDay), missingDay, "不支持的农历日期截断策略。");
        if (targetYear < MinimumYear || targetYear > MaximumYear)
            throw new ArgumentOutOfRangeException(yearParameter, yearValue, "目标农历年超出支持范围。");

        lock (CalendarLock)
        {
            var year = (int)targetYear;
            var leapSlot = Calendar.GetLeapMonth(year);
            var useLeapMonth = date.IsLeapMonth && leapSlot == date.Month + 1;
            if (date.IsLeapMonth && !useLeapMonth && missingLeapMonth == LunarLeapMonthOverflow.Reject)
                throw new ArgumentOutOfRangeException(yearParameter, yearValue, "目标农历年不存在同名闰月。");

            var slot = useLeapMonth ? leapSlot : leapSlot > 0 && date.Month >= leapSlot ? date.Month + 1 : date.Month;
            var daysInMonth = Calendar.GetDaysInMonth(year, slot);
            if (date.Day > daysInMonth && missingDay == LunarDayOverflow.Reject)
                throw new ArgumentOutOfRangeException(yearParameter, yearValue, "目标农历月不存在原日号。");

            var day = Math.Min(date.Day, daysInMonth);
            var result = Calendar.ToDateTime(year, slot, day, 0, 0, 0, 0);
            return new LunarDate(result, year, date.Month, day, slot, useLeapMonth);
        }
    }

    /// <summary>
    /// 农历组件转换的预期失败原因。
    /// </summary>
    private enum CreateFailure
    {
        /// <summary>
        /// 输入有效。
        /// </summary>
        None,
        /// <summary>
        /// 年份无效。
        /// </summary>
        Year,
        /// <summary>
        /// 显示月份无效。
        /// </summary>
        Month,
        /// <summary>
        /// 日期无效。
        /// </summary>
        Day,
        /// <summary>
        /// 指定的闰月不存在。
        /// </summary>
        LeapMonth
    }
}
