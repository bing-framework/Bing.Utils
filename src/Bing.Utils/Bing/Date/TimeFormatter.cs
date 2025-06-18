using System.Globalization;

namespace Bing.Date;

/// <summary>
/// 日期/时间格式化工具类
/// </summary>
public static class TimeFormatter
{
    /// <summary>
    /// 默认区域设置信息
    /// </summary>
    public static readonly CultureInfo DefaultCulture = CultureInfo.InvariantCulture;

    /// <summary>
    /// 格式类型字典
    /// </summary>
    private static readonly Dictionary<TimeFormatType, string> Patterns = new()
    {
        [TimeFormatType.Default] = "yyyy-MM-dd HH:mm:ss",

        [TimeFormatType.NormYear] = "yyyy",
        [TimeFormatType.NormMonth] = "yyyy-MM",
        [TimeFormatType.NormDate] = "yyyy-MM-dd",
        [TimeFormatType.NormTime] = "HH:mm:ss",
        [TimeFormatType.NormTimeMs] = "HH:mm:ss.fff",
        [TimeFormatType.NormDateTimeMinute] = "yyyy-MM-dd HH:mm",
        [TimeFormatType.NormDateTime] = "yyyy-MM-dd HH:mm:ss",
        [TimeFormatType.NormDateTimeMs] = "yyyy-MM-dd HH:mm:ss.fff",

        [TimeFormatType.ChineseYear] = "yyyy年",
        [TimeFormatType.ChineseMonth] = "yyyy年MM月",
        [TimeFormatType.ChineseDate] = "yyyy年MM月dd日",
        [TimeFormatType.ChineseTime] = "HH时mm分ss秒",
        [TimeFormatType.ChineseTimeMs] = "HH时mm分ss秒.fff",
        [TimeFormatType.ChineseDateTimeMinute] = "yyyy年MM月dd日 HH时mm分",
        [TimeFormatType.ChineseDateTime] = "yyyy年MM月dd日 HH时mm分ss秒",
        [TimeFormatType.ChineseDateTimeMs] = "yyyy年MM月dd日 HH时mm分ss秒.fff",

        [TimeFormatType.PureYear] = "yyyy",
        [TimeFormatType.PureMonth] = "yyyyMM",
        [TimeFormatType.PureDate] = "yyyyMMdd",
        [TimeFormatType.PureTime] = "HHmmss",
        [TimeFormatType.PureTimeMs] = "HHmmssfff",
        [TimeFormatType.PureDateTimeMinute] = "yyyyMMddHHmm",
        [TimeFormatType.PureDateTime] = "yyyyMMddHHmmss",
        [TimeFormatType.PureDateTimeMs] = "yyyyMMddHHmmssfff",

        [TimeFormatType.UnderlineYear] = "yyyy",
        [TimeFormatType.UnderlineMonth] = "yyyy_MM",
        [TimeFormatType.UnderlineDate] = "yyyy_MM_dd",
        [TimeFormatType.UnderlineTime] = "HH_mm_ss",
        [TimeFormatType.UnderlineTimeMs] = "HH_mm_ss_fff",
        [TimeFormatType.UnderlineDateTimeMinute] = "yyyy_MM_dd_HH_mm",
        [TimeFormatType.UnderlineDateTime] = "yyyy_MM_dd_HH_mm_ss",
        [TimeFormatType.UnderlineDateTimeMs] = "yyyy_MM_dd_HH_mm_ss_fff"
    };

    /// <summary>
    /// 使用默认格式格式化时间（等价于 Format(dt, Default)）
    /// </summary>
    /// <param name="dt">要格式化的时间</param>
    /// <returns>格式化后的字符串</returns>
    public static string Format(DateTime dt) => Format(dt, TimeFormatType.Default);

    /// <summary>
    /// 格式化指定时间为指定类型的字符串（使用默认文化）
    /// </summary>
    /// <param name="dt">要格式化的时间</param>
    /// <param name="type">格式类型</param>
    /// <returns>格式化后的字符串</returns>
    public static string Format(DateTime dt, TimeFormatType type) => Format(dt, type, DefaultCulture);

    /// <summary>
    /// 格式化指定时间为指定类型的字符串（支持指定文化信息）
    /// </summary>
    /// <param name="dt">要格式化的时间</param>
    /// <param name="type">格式类型</param>
    /// <param name="culture">区域文化</param>
    /// <returns>格式化后的字符串</returns>
    public static string Format(DateTime dt, TimeFormatType type, CultureInfo culture) => dt.ToString(Patterns[type], culture.DateTimeFormat);

    /// <summary>
    /// 格式化可空时间为字符串（使用默认文化、默认时间格式类型）
    /// </summary>
    /// <param name="dt">可空时间</param>
    /// <returns>格式化字符串，null 时返回空串</returns>
    public static string Format(DateTime? dt) => Format(dt, TimeFormatType.Default);

    /// <summary>
    /// 格式化可空时间为字符串（使用默认文化）
    /// </summary>
    /// <param name="dt">可空时间</param>
    /// <param name="type">格式类型</param>
    /// <returns>格式化字符串，null 时返回空串</returns>
    public static string Format(DateTime? dt, TimeFormatType type) => Format(dt, type, DefaultCulture);

    /// <summary>
    /// 格式化可空时间为字符串（支持指定文化）
    /// </summary>
    /// <param name="dt">可空时间</param>
    /// <param name="type">格式类型</param>
    /// <param name="culture">区域文化</param>
    /// <returns>格式化字符串，null 时返回空串</returns>
    public static string Format(DateTime? dt, TimeFormatType type, CultureInfo culture) =>
        dt.HasValue ? Format(dt.Value, type, culture) : string.Empty;

    /// <summary>
    /// 获取当前时间的默认格式字符串
    /// </summary>
    /// <returns>格式化后的当前时间字符串</returns>
    public static string Now() => Format(DateTime.Now, TimeFormatType.Default);

    /// <summary>
    /// 获取当前时间的格式化字符串（使用指定格式）
    /// </summary>
    /// <param name="type">格式类型</param>
    /// <returns>当前时间的格式化结果</returns>
    public static string Now(TimeFormatType type) => Format(DateTime.Now, type);

    /// <summary>
    /// 获取当前时间的格式化字符串（使用指定格式和文化）
    /// </summary>
    /// <param name="type">格式类型</param>
    /// <param name="culture">区域文化</param>
    /// <returns>格式化后的当前时间字符串</returns>
    public static string Now(TimeFormatType type, CultureInfo culture) => Format(DateTime.Now, type, culture);

    /// <summary>
    /// 注册或覆盖指定时间格式类型的格式字符串
    /// </summary>
    /// <param name="type">格式类型</param>
    /// <param name="pattern">格式字符串</param>
    /// <exception cref="ArgumentException"></exception>
    public static void Register(TimeFormatType type, string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            throw new ArgumentException("格式字符串不能为空", nameof(pattern));
        Patterns[type] = pattern;
    }

    /// <summary>
    /// 获取指定格式类型的字符串模板
    /// </summary>
    /// <param name="type">格式类型</param>
    /// <returns>格式字符串</returns>
    public static string GetPattern(TimeFormatType type) => Patterns[type];

    /// <summary>
    /// 尝试通过格式字符串反解析匹配的格式类型
    /// </summary>
    /// <param name="pattern">格式字符串</param>
    /// <param name="type">输出匹配的格式类型</param>
    /// <returns>是否匹配成功</returns>
    public static bool TryParsePattern(string pattern, out TimeFormatType type)
    {
        foreach (var kv in Patterns)
        {
            if (kv.Value.Equals(pattern, StringComparison.OrdinalIgnoreCase))
            {
                type = kv.Key;
                return true;
            }
        }
        type = default;
        return false;
    }
}