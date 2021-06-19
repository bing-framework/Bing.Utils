using System;
using NodaTime;

namespace Bing.Date
{
    /// <summary>
    /// Bing 日期时间(<see cref="DateTime"/>) 扩展
    /// </summary>
    public static partial class DateTimeExtensions
    {
        #region Add

        /// <summary>
        /// 添加一个星期
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="weeks">星期数</param>
        public static DateTime AddWeeks(this DateTime dt, int weeks) => DateTimeCalc.OffsetByWeeks(dt, weeks);

        /// <summary>
        /// 添加一个季度
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="quarters">季度数</param>
        public static DateTime AddQuarters(this DateTime dt, int quarters) => DateTimeCalc.OffsetByQuarters(dt, quarters);

        /// <summary>
        /// 添加一段持续时间
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="duration">持续时间</param>
        public static DateTime AddDuration(this DateTime dt, Duration duration) => DateTimeCalc.OffsetByDuration(dt, duration);

        /// <summary>
        /// 添加指定个数量的工作日
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="days">天数</param>
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
        /// 根据生日计算当前的年龄
        /// </summary>
        /// <param name="birthday">生日</param>
        public static int ToCalculateAge(this DateTime birthday) => birthday.ToCalculateAge(DateTime.Today);

        /// <summary>
        /// 根据生日和参照时间，计算当时年龄
        /// </summary>
        /// <param name="birthday">生日</param>
        /// <param name="referenceDate">参照时间</param>
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
        /// 克隆
        /// </summary>
        /// <param name="dt">时间</param>
        public static DateTime Clone(this DateTime dt) => new DateTime(dt.Ticks, dt.Kind);

        #endregion

        #region Diff

        /// <summary>
        /// 计算两个时间之间相差的月份数
        /// </summary>
        /// <param name="dt1">时间</param>
        /// <param name="dt2">时间</param>
        public static int GetMonthDiff(this DateTime dt1, DateTime dt2)
        {
            var l = dt1 < dt2 ? dt1 : dt2;
            var r = dt1 >= dt2 ? dt1 : dt2;
            return (l.Day == r.Day ? 0 : l.Day > r.Day ? 0 : 1)
                   + (l.Month == r.Month ? 0 : r.Month - l.Month)
                   + (l.Year == r.Year ? 0 : (r.Year - l.Year) * 12);
        }

        /// <summary>
        /// 计算两个日期之间相差的确切月份数
        /// </summary>
        /// <param name="dt1">时间</param>
        /// <param name="dt2">时间</param>
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
        /// 计算此刻与指定时间之间的时间差
        /// </summary>
        /// <param name="dt">时间</param>
        public static TimeSpan ElapsedTime(this DateTime dt) => DateTime.Now - dt;

        /// <summary>
        /// 计算此刻与指定时间之间的时间差（毫秒）
        /// </summary>
        /// <param name="dt">时间</param>
        public static int ElapsedMilliseconds(this DateTime dt) => (int)(DateTime.Now - dt).TotalMilliseconds;

        #endregion

        #region Is

        /// <summary>
        /// 判断当前日期是否在 from 和 to 之间
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="from">开始时间</param>
        /// <param name="to">结束时间</param>
        /// <param name="includeBoundary">是否包含边界</param>
        public static bool IsBetween(this DateTime dt, DateTime from, DateTime to, bool includeBoundary = true)
        {
            return includeBoundary
                ? dt >= from && dt <= to
                : dt > from && dt < to;
        }

        /// <summary>
        /// 判断当前日期是否在 min 和 max 之间，闭包区间。
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="min">开始时间</param>
        /// <param name="max">结束时间</param>
        public static bool IsDateBetweenWithBoundary(this DateTime dt, DateTime min, DateTime max) =>
            dt.IsBetween(min, max.AddDays(+1), false);

        /// <summary>
        /// 判断当前日期是否在 min 和 max 之间，闭包区间。
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="min">开始时间</param>
        /// <param name="max">结束时间</param>
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
        /// 判断当前日期是否在 min 和 max 之间，开区间。
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="min">开始时间</param>
        /// <param name="max">结束时间</param>
        public static bool IsDateBetweenWithoutBoundary(this DateTime dt, DateTime min, DateTime max) =>
            dt.IsBetween(min, max, false);

        /// <summary>
        /// 判断给定时间是否今天
        /// </summary>
        /// <param name="dt">时间</param>
        public static bool IsToday(this DateTime dt) => dt.Date == DateTime.Today;

        /// <summary>
        /// 判断给定时间是否今天
        /// </summary>
        /// <param name="dt">时间</param>
        public static bool IsToday(this DateTime? dt) => dt.GetValueOrDefault().Date == DateTime.Today;

        /// <summary>
        /// 判断给定时间是否是早晨
        /// </summary>
        /// <param name="dt">时间</param>
        public static bool IsMorning(this DateTime dt)
        {
            var hour = dt.Hour;
            return 6 <= hour && hour < 12;
        }

        /// <summary>
        /// 判断给定时间是否是清晨
        /// </summary>
        /// <param name="dt">时间</param>
        public static bool IsEarlyMorning(this DateTime dt)
        {
            var hour = dt.Hour;
            return 0 <= hour && hour < 6;
        }

        /// <summary>
        /// 判断给定时间是否是下午
        /// </summary>
        /// <param name="dt">时间</param>
        public static bool IsAfternoon(this DateTime dt)
        {
            var hour = dt.Hour;
            return 12 <= hour && hour < 18;
        }

        /// <summary>
        /// 判断给定时间是否是黄昏
        /// </summary>
        /// <param name="dt">时间</param>
        public static bool IsDusk(this DateTime dt)
        {
            var hour = dt.Hour;
            return 16 <= hour && hour < 19;
        }

        /// <summary>
        /// 判断给定时间是否是夜晚
        /// </summary>
        /// <param name="dt">时间</param>
        public static bool IsEvening(this DateTime dt)
        {
            var hour = dt.Hour;
            return 18 <= hour && hour < 24 || 0 <= hour && hour < 6;
        }

        /// <summary>
        /// 判断指定时间是否相对给定时间的过去
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="toCompareWith">比较时间</param>
        public static bool IsBefore(this DateTime dt, DateTime toCompareWith) => dt < toCompareWith;

        /// <summary>
        /// 判断指定时间是否相对给定时间的未来
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="toCompareWith">比较时间</param>
        public static bool IsAfter(this DateTime dt, DateTime toCompareWith) => dt > toCompareWith;

        /// <summary>
        /// 判断指定时间是否相对当前时间的未来
        /// </summary>
        /// <param name="dt">时间</param>
        public static bool IsInTheFuture(this DateTime dt) => dt > DateTime.Now;

        /// <summary>
        /// 判断指定时间是否相对当前时间的过去
        /// </summary>
        /// <param name="dt">时间</param>
        public static bool IsInThePast(this DateTime dt) => dt < DateTime.Now;

        /// <summary>
        /// 判断是否为工作日
        /// </summary>
        /// <param name="dt">时间</param>
        public static bool IsWeekday(this DateTime dt) => !dt.IsWeekend();

        /// <summary>
        /// 判断是否为工作日
        /// </summary>
        /// <param name="dt">时间</param>
        public static bool IsWeekday(this DateTime? dt) => dt.GetValueOrDefault().IsWeekday();

        /// <summary>
        /// 判断是否为周末
        /// </summary>
        /// <param name="dt">时间</param>
        public static bool IsWeekend(this DateTime dt) => dt.DayOfWeek == DayOfWeek.Sunday || dt.DayOfWeek == DayOfWeek.Saturday;

        /// <summary>
        /// 判断是否为周末
        /// </summary>
        /// <param name="dt">时间</param>
        public static bool IsWeekend(this DateTime? dt) => dt.GetValueOrDefault().IsWeekend();

        /// <summary>
        /// 判断是否同一天
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="date">日期</param>
        public static bool IsSameDay(this DateTime dt, DateTime date) => dt.Date == date.Date;

        /// <summary>
        /// 判断是否同一个月
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="date">日期</param>
        public static bool IsSameMonth(this DateTime dt, DateTime date) => dt.Month == date.Month;

        /// <summary>
        /// 判断是否同一年
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="date">日期</param>
        public static bool IsSameYear(this DateTime dt, DateTime date) => dt.Year == date.Year;

        /// <summary>
        /// 判断日期是否相等
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="date">日期</param>
        public static bool IsDateEqual(this DateTime dt, DateTime date) => dt.IsSameDay(date);

        /// <summary>
        /// 判断时间是否相等
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="date">日期</param>
        public static bool IsTimeEqual(this DateTime dt, DateTime date) => dt.TimeOfDay == date.TimeOfDay;

        #endregion

        #region Round

        /// <summary>
        /// 将时间四舍五入到最接近给定精度的值
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="rt">舍入精度</param>
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
        /// 转换为 UTC 时间
        /// </summary>
        /// <param name="dt">时间</param>
        public static DateTime ToUtc(this DateTime dt) => new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, DateTimeKind.Utc);

        /// <summary>
        /// 转换为纪元时间间隔
        /// </summary>
        /// <param name="dt">时间</param>
        public static TimeSpan ToEpochTimeSpan(this DateTime dt) => dt.Subtract(DateTimeFactory.Create(1970, 1, 1));

        /// <summary>
        /// 转换为 <see cref="LocalDateTime"/>
        /// </summary>
        /// <param name="dt">时间</param>
        public static LocalDateTime ToLocalDateTime(this DateTime dt) => LocalDateTime.FromDateTime(dt);

        /// <summary>
        /// 转换为 <see cref="LocalDate"/>
        /// </summary>
        /// <param name="dt">时间</param>
        public static LocalDate ToLocalDate(this DateTime dt) => LocalDate.FromDateTime(dt);

        /// <summary>
        /// 转换为 <see cref="LocalTime"/>
        /// </summary>
        /// <param name="dt">时间</param>
        public static LocalTime ToLocalTime(this DateTime dt)=>new LocalTime(dt.Hour,dt.Minute,dt.Second,dt.Millisecond);

        #endregion
    }
}
