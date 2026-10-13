namespace Bing.Date;

/// <summary>
/// 时间输出风格
/// </summary>
public enum DateTimeOutputStyles
{
    /// <summary>
    /// 按预设格式模式输出日期和时间。
    /// </summary>
    /// <remarks>
    /// 使用格式字符串 <c>yyyy-MM-dd HH:mm:ss</c>。
    /// </remarks>
    DateTime,

    /// <summary>
    /// 按预设格式模式输出日期。
    /// </summary>
    /// <remarks>
    /// 使用格式字符串 <c>yyyy-MM-dd</c>。
    /// </remarks>
    Date,

    /// <summary>
    /// 按预设格式模式输出时间。
    /// </summary>
    /// <remarks>
    /// 使用格式字符串 <c>HH:mm:ss</c>。
    /// </remarks>
    Time,

    /// <summary>
    /// 使用当前区域设置定义的长日期格式。
    /// </summary>
    LongDate,

    /// <summary>
    /// 使用当前区域设置定义的长时间格式。
    /// </summary>
    LongTime,

    /// <summary>
    /// 使用当前区域设置定义的短日期格式。
    /// </summary>
    ShortDate,

    /// <summary>
    /// 使用当前区域设置定义的短时间格式。
    /// </summary>
    ShortTime,

    /// <summary>
    /// 按预设格式模式输出精确到毫秒的日期时间。
    /// </summary>
    /// <remarks>
    /// 使用格式字符串 <c>yyyy-MM-dd HH:mm:ss.fff</c>。
    /// </remarks>
    Millisecond,
}
