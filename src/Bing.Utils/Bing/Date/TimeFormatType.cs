namespace Bing.Date;

/// <summary>
/// 时间格式类型（支持标准、紧凑、中文、带毫秒等格式）
/// </summary>
public enum TimeFormatType
{
    /// <summary>
    /// 默认标准格式：<c>[yyyy-MM-dd HH:mm:ss]</c>
    /// </summary>
    Default,

    #region Norm

    /// <summary>
    /// 标准年格式：<c>[yyyy]</c>。
    /// 例如：2025
    /// </summary>
    NormYear,

    /// <summary>
    /// 标准年月格式：<c>[yyyy-MM]</c>。
    /// 例如：2025-06
    /// </summary>
    NormMonth,

    /// <summary>
    /// 标准日期格式：<c>[yyyy-MM-dd]</c>。
    /// 例如：2025-06-18
    /// </summary>
    NormDate,

    /// <summary>
    /// 标准时间格式：<c>[HH:mm:ss]</c>。
    /// 例如：17:20:01
    /// </summary>
    NormTime,

    /// <summary>
    /// 标准时间格式，精确到毫秒：<c>[HH:mm:ss.fff]</c>。
    /// 例如：17:20:01.123
    /// </summary>
    NormTimeMs,

    /// <summary>
    /// 标准日期时间格式，精确到分：<c>[yyyy-MM-dd HH:mm]</c>。
    /// 例如：2025-06-18 17:20
    /// </summary>
    NormDateTimeMinute,

    /// <summary>
    /// 标准日期时间格式：<c>[yyyy-MM-dd HH:mm:ss]</c>。
    /// 例如：2025-06-18 17:20:01
    /// </summary>
    NormDateTime,

    /// <summary>
    /// 标准日期时间格式，精确到毫秒：<c>[yyyy-MM-dd HH:mm:ss.fff]</c>。
    /// 例如：2025-06-18 17:20:01.123
    /// </summary>
    NormDateTimeMs,

    #endregion

    #region Chinese

    /// <summary>
    /// 标准年格式：<c>[yyyy年]</c>。
    /// 例如：2025年
    /// </summary>
    ChineseYear,

    /// <summary>
    /// 标准年月格式：<c>[yyyy年MM月]</c>。
    /// 例如：2025年06月
    /// </summary>
    ChineseMonth,

    /// <summary>
    /// 标准日期格式：<c>[yyyy年MM月dd日]</c>。
    /// 例如：2025年06月18日
    /// </summary>
    ChineseDate,

    /// <summary>
    /// 标准日期格式：<c>[HH时mm分ss秒]</c>。
    /// 例如：17时20分01秒
    /// </summary>
    ChineseTime,

    /// <summary>
    /// 标准日期格式，精确到毫秒：<c>[HH时mm分ss秒.fff]</c>。
    /// 例如：17时20分01秒.123
    /// </summary>
    ChineseTimeMs,

    /// <summary>
    /// 标准日期时间格式，精确到分：<c>[yyyy年MM月dd日 HH时mm分]</c>。
    /// 例如：2025年06月18日 17时20分
    /// </summary>
    ChineseDateTimeMinute,

    /// <summary>
    /// 标准日期时间格式：<c>[yyyy年MM月dd日 HH时mm分ss秒]</c>。
    /// 例如：2025年06月18日 17时20分01秒
    /// </summary>
    ChineseDateTime,

    /// <summary>
    /// 标准日期时间格式，精确到毫秒：<c>[yyyy年MM月dd日 HH时mm分ss秒.fff]</c>。
    /// 例如：2025年06月18日 17时20分01秒.123
    /// </summary>
    ChineseDateTimeMs,

    #endregion

    #region Pure

    /// <summary>
    /// 标准年格式：<c>[yyyy]</c>。
    /// 例如：2025
    /// </summary>
    PureYear,

    /// <summary>
    /// 标准年月格式：<c>[yyyyMM]</c>。
    /// 例如：202506
    /// </summary>
    PureMonth,

    /// <summary>
    /// 标准日期格式：<c>[yyyyMMdd]</c>。
    /// 例如：20250618
    /// </summary>
    PureDate,

    /// <summary>
    /// 标准时间格式：<c>[HHmmss]</c>。
    /// 例如：172001
    /// </summary>
    PureTime,

    /// <summary>
    /// 标准时间格式，精确到毫秒：<c>[HHmmssfff]</c>。
    /// 例如：172001123
    /// </summary>
    PureTimeMs,

    /// <summary>
    /// 标准日期时间格式，精确到分：<c>[yyyyMMddHHmm]</c>。
    /// 例如：202506181720
    /// </summary>
    PureDateTimeMinute,

    /// <summary>
    /// 标准日期时间格式：<c>[yyyyMMddHHmmss]</c>。
    /// 例如：20250618172001
    /// </summary>
    PureDateTime,

    /// <summary>
    /// 标准日期时间格式，精确到毫秒：<c>[yyyyMMddHHmmssfff]</c>。
    /// 例如：20250618172001123
    /// </summary>
    PureDateTimeMs,

    #endregion

    #region Underline

    /// <summary>
    /// 标准年格式：<c>[yyyy]</c>。
    /// 例如：2025
    /// </summary>
    UnderlineYear,

    /// <summary>
    /// 标准年月格式：<c>[yyyy_MM]</c>。
    /// 例如：2025_06
    /// </summary>
    UnderlineMonth,

    /// <summary>
    /// 标准日期格式：<c>yyyy_MM_dd</c>。
    /// 例如：2025_06_18
    /// </summary>
    UnderlineDate,

    /// <summary>
    /// 标准时间格式：<c>[HH_mm_ss]</c>。
    /// 例如：17_20_01
    /// </summary>
    UnderlineTime,

    /// <summary>
    /// 标准时间格式，精确到毫秒：<c>[HH_mm_ss_fff]</c>。
    /// 例如：17_20_01_123
    /// </summary>
    UnderlineTimeMs,

    /// <summary>
    /// 标准日期时间格式，精确到分：<c>[yyyy_MM_dd_HH_mm]</c>。
    /// 例如：2025_06_18_17_20
    /// </summary>
    UnderlineDateTimeMinute,

    /// <summary>
    /// 标准日期时间格式：<c>[yyyy_MM_dd_HH_mm_ss]</c>。
    /// 例如：2025_06_18_17_20_01
    /// </summary>
    UnderlineDateTime,

    /// <summary>
    /// 标准日期时间格式，精确到毫秒：<c>[yyyy_MM_dd_HH_mm_ss_fff]</c>。
    /// 例如：2025_06_18_17_20_01_123
    /// </summary>
    UnderlineDateTimeMs,

    #endregion

}