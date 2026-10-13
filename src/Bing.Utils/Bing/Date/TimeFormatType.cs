namespace Bing.Date;

/// <summary>
/// 表示日期和时间格式化方式。
/// </summary>
/// <remarks>
/// 内置格式字符串可通过 <see cref="TimeFormatter.Register(TimeFormatType, string)"/> 覆盖。
/// </remarks>
public enum TimeFormatType
{
    /// <summary>
    /// 使用默认日期时间格式输出日期和时间。
    /// </summary>
    /// <remarks>
    /// 默认格式字符串为 <c>yyyy-MM-dd HH:mm:ss</c>。
    /// </remarks>
    Default,

    #region Norm

    /// <summary>
    /// 使用标准分隔格式输出年份。
    /// </summary>
    /// <remarks>
    /// 默认格式字符串为 <c>yyyy</c>。
    /// </remarks>
    NormYear,

    /// <summary>
    /// 使用标准分隔格式输出年份和月份。
    /// </summary>
    /// <remarks>
    /// 默认格式字符串为 <c>yyyy-MM</c>。
    /// </remarks>
    NormMonth,

    /// <summary>
    /// 使用标准分隔格式输出日期。
    /// </summary>
    /// <remarks>
    /// 默认格式字符串为 <c>yyyy-MM-dd</c>。
    /// </remarks>
    NormDate,

    /// <summary>
    /// 使用标准分隔格式输出时间。
    /// </summary>
    /// <remarks>
    /// 默认格式字符串为 <c>HH:mm:ss</c>。
    /// </remarks>
    NormTime,

    /// <summary>
    /// 使用标准分隔格式输出精确到毫秒的时间。
    /// </summary>
    /// <remarks>
    /// 默认格式字符串为 <c>HH:mm:ss.fff</c>。
    /// </remarks>
    NormTimeMs,

    /// <summary>
    /// 使用标准分隔格式输出精确到分钟的日期时间。
    /// </summary>
    /// <remarks>
    /// 默认格式字符串为 <c>yyyy-MM-dd HH:mm</c>。
    /// </remarks>
    NormDateTimeMinute,

    /// <summary>
    /// 使用标准分隔格式输出日期时间。
    /// </summary>
    /// <remarks>
    /// 默认格式字符串为 <c>yyyy-MM-dd HH:mm:ss</c>。
    /// </remarks>
    NormDateTime,

    /// <summary>
    /// 使用标准分隔格式输出精确到毫秒的日期时间。
    /// </summary>
    /// <remarks>
    /// 默认格式字符串为 <c>yyyy-MM-dd HH:mm:ss.fff</c>。
    /// </remarks>
    NormDateTimeMs,

    #endregion

    #region Chinese

    /// <summary>
    /// 使用中文单位格式输出年份。
    /// </summary>
    /// <remarks>
    /// 默认格式字符串为 <c>yyyy年</c>。
    /// </remarks>
    ChineseYear,

    /// <summary>
    /// 使用中文单位格式输出年份和月份。
    /// </summary>
    /// <remarks>
    /// 默认格式字符串为 <c>yyyy年MM月</c>。
    /// </remarks>
    ChineseMonth,

    /// <summary>
    /// 使用中文单位格式输出日期。
    /// </summary>
    /// <remarks>
    /// 默认格式字符串为 <c>yyyy年MM月dd日</c>。
    /// </remarks>
    ChineseDate,

    /// <summary>
    /// 使用中文单位格式输出时间。
    /// </summary>
    /// <remarks>
    /// 默认格式字符串为 <c>HH时mm分ss秒</c>。
    /// </remarks>
    ChineseTime,

    /// <summary>
    /// 使用中文单位格式输出精确到毫秒的时间。
    /// </summary>
    /// <remarks>
    /// 默认格式字符串为 <c>HH时mm分ss秒.fff</c>。
    /// </remarks>
    ChineseTimeMs,

    /// <summary>
    /// 使用中文单位格式输出精确到分钟的日期时间。
    /// </summary>
    /// <remarks>
    /// 默认格式字符串为 <c>yyyy年MM月dd日 HH时mm分</c>。
    /// </remarks>
    ChineseDateTimeMinute,

    /// <summary>
    /// 使用中文单位格式输出日期时间。
    /// </summary>
    /// <remarks>
    /// 默认格式字符串为 <c>yyyy年MM月dd日 HH时mm分ss秒</c>。
    /// </remarks>
    ChineseDateTime,

    /// <summary>
    /// 使用中文单位格式输出精确到毫秒的日期时间。
    /// </summary>
    /// <remarks>
    /// 默认格式字符串为 <c>yyyy年MM月dd日 HH时mm分ss秒.fff</c>。
    /// </remarks>
    ChineseDateTimeMs,

    #endregion

    #region Pure

    /// <summary>
    /// 使用无分隔符格式输出年份。
    /// </summary>
    /// <remarks>
    /// 默认格式字符串为 <c>yyyy</c>。
    /// </remarks>
    PureYear,

    /// <summary>
    /// 使用无分隔符格式输出年份和月份。
    /// </summary>
    /// <remarks>
    /// 默认格式字符串为 <c>yyyyMM</c>。
    /// </remarks>
    PureMonth,

    /// <summary>
    /// 使用无分隔符格式输出日期。
    /// </summary>
    /// <remarks>
    /// 默认格式字符串为 <c>yyyyMMdd</c>。
    /// </remarks>
    PureDate,

    /// <summary>
    /// 使用无分隔符格式输出时间。
    /// </summary>
    /// <remarks>
    /// 默认格式字符串为 <c>HHmmss</c>。
    /// </remarks>
    PureTime,

    /// <summary>
    /// 使用无分隔符格式输出精确到毫秒的时间。
    /// </summary>
    /// <remarks>
    /// 默认格式字符串为 <c>HHmmssfff</c>。
    /// </remarks>
    PureTimeMs,

    /// <summary>
    /// 使用无分隔符格式输出精确到分钟的日期时间。
    /// </summary>
    /// <remarks>
    /// 默认格式字符串为 <c>yyyyMMddHHmm</c>。
    /// </remarks>
    PureDateTimeMinute,

    /// <summary>
    /// 使用无分隔符格式输出日期时间。
    /// </summary>
    /// <remarks>
    /// 默认格式字符串为 <c>yyyyMMddHHmmss</c>。
    /// </remarks>
    PureDateTime,

    /// <summary>
    /// 使用无分隔符格式输出精确到毫秒的日期时间。
    /// </summary>
    /// <remarks>
    /// 默认格式字符串为 <c>yyyyMMddHHmmssfff</c>。
    /// </remarks>
    PureDateTimeMs,

    #endregion

    #region Underline

    /// <summary>
    /// 使用下划线分隔格式输出年份。
    /// </summary>
    /// <remarks>
    /// 默认格式字符串为 <c>yyyy</c>。
    /// </remarks>
    UnderlineYear,

    /// <summary>
    /// 使用下划线分隔格式输出年份和月份。
    /// </summary>
    /// <remarks>
    /// 默认格式字符串为 <c>yyyy_MM</c>。
    /// </remarks>
    UnderlineMonth,

    /// <summary>
    /// 使用下划线分隔格式输出日期。
    /// </summary>
    /// <remarks>
    /// 默认格式字符串为 <c>yyyy_MM_dd</c>。
    /// </remarks>
    UnderlineDate,

    /// <summary>
    /// 使用下划线分隔格式输出时间。
    /// </summary>
    /// <remarks>
    /// 默认格式字符串为 <c>HH_mm_ss</c>。
    /// </remarks>
    UnderlineTime,

    /// <summary>
    /// 使用下划线分隔格式输出精确到毫秒的时间。
    /// </summary>
    /// <remarks>
    /// 默认格式字符串为 <c>HH_mm_ss_fff</c>。
    /// </remarks>
    UnderlineTimeMs,

    /// <summary>
    /// 使用下划线分隔格式输出精确到分钟的日期时间。
    /// </summary>
    /// <remarks>
    /// 默认格式字符串为 <c>yyyy_MM_dd_HH_mm</c>。
    /// </remarks>
    UnderlineDateTimeMinute,

    /// <summary>
    /// 使用下划线分隔格式输出日期时间。
    /// </summary>
    /// <remarks>
    /// 默认格式字符串为 <c>yyyy_MM_dd_HH_mm_ss</c>。
    /// </remarks>
    UnderlineDateTime,

    /// <summary>
    /// 使用下划线分隔格式输出精确到毫秒的日期时间。
    /// </summary>
    /// <remarks>
    /// 默认格式字符串为 <c>yyyy_MM_dd_HH_mm_ss_fff</c>。
    /// </remarks>
    UnderlineDateTimeMs,

    #endregion

}
