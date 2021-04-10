using System;

namespace Bing.Conversions
{
    /// <summary>
    /// <see cref="DayOfWeek"/> 转换
    /// </summary>
    public static class DayOfWeekConv
    {
        /// <summary>
        /// 将 <see cref="DayOfWeek"/> 转换为 <see cref="int"/>
        /// </summary>
        /// <param name="week">星期几</param>
        public static int ToInt(DayOfWeek week) => ToInt(week, 1);

        /// <summary>
        /// 将 <see cref="DayOfWeek"/> 转换为 <see cref="int"/>
        /// </summary>
        /// <param name="week">星期几</param>
        /// <param name="offset">偏移量</param>
        public static int ToInt(DayOfWeek week, int offset) =>
            offset + week switch
            {
                DayOfWeek.Sunday => 0,
                DayOfWeek.Monday => 1,
                DayOfWeek.Tuesday => 2,
                DayOfWeek.Wednesday => 3,
                DayOfWeek.Thursday => 4,
                DayOfWeek.Friday => 5,
                DayOfWeek.Saturday => 6,
                _ => 0
            };
    }

    /// <summary>
    /// Bing <see cref="DayOfWeekConv"/> 扩展
    /// </summary>
    public static class DayOfWeekConvExtensions
    {
        /// <summary>
        /// 将 <see cref="DayOfWeek"/> 转换为 <see cref="int"/>
        /// </summary>
        /// <param name="week">星期几</param>
        public static int CastToInt(this DayOfWeek week) => DayOfWeekConv.ToInt(week);

        /// <summary>
        /// 将 <see cref="DayOfWeek"/> 转换为 <see cref="int"/>
        /// </summary>
        /// <param name="week">星期几</param>
        /// <param name="offset">偏移量</param>
        public static int CastToInt(this DayOfWeek week, int offset) => DayOfWeekConv.ToInt(week, offset);
    }
}
