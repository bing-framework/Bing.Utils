namespace Bing.Date;

/// <summary>
/// 时间输出风格
/// </summary>
public enum DateTimeOutputStyles
{
    /// <summary>
    /// 格式：yyyy-MM-dd HH:mm:ss
    /// </summary>
    DateTime,

    /// <summary>
    /// 格式：yyyy-MM-dd
    /// </summary>
    Date,

    /// <summary>
    /// 格式：HH:mm:ss
    /// </summary>
    Time,

    /// <summary>
    /// 长日期。格式：yyyy年MM月dd日
    /// </summary>
    LongDate,

    /// <summary>
    /// 长时间。格式：HH:mm:ss
    /// </summary>
    LongTime,

    /// <summary>
    /// 短日期。格式：yyyy/MM/dd
    /// </summary>
    ShortDate,

    /// <summary>
    /// 短时间。格式：HH:mm
    /// </summary>
    ShortTime,

    /// <summary>
    /// 格式：yyyy-MM-dd HH:mm:ss.fff
    /// </summary>
    Millisecond,
}