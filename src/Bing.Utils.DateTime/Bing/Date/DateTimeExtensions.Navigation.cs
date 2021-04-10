using System;

namespace Bing.Date
{
    public static partial class DateTimeExtensions
    {
        #region Offset

        /// <summary>
        /// 偏移指定值
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="offsetVal">偏移值</param>
        /// <param name="styles">时间偏移样式</param>
        public static DateTime OffsetBy(this DateTime dt, int offsetVal, DateTimeOffsetStyles styles) =>
            styles switch
            {
                DateTimeOffsetStyles.Day => DateTimeCalc.OffsetByDays(dt, offsetVal),
                DateTimeOffsetStyles.Week => DateTimeCalc.OffsetByWeeks(dt, offsetVal),
                DateTimeOffsetStyles.Month => DateTimeCalc.OffsetByMonths(dt, offsetVal, DateTimeOffsetOptions.Relatively),
                DateTimeOffsetStyles.Quarters => DateTimeCalc.OffsetByQuarters(dt, offsetVal, DateTimeOffsetOptions.Relatively),
                DateTimeOffsetStyles.Year => DateTimeCalc.OffsetByYears(dt, offsetVal, DateTimeOffsetOptions.Relatively),
                _ => DateTimeCalc.OffsetByDays(dt, offsetVal)
            };

        #endregion
    }
}
