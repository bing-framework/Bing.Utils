using System.Globalization;

namespace Bing.Date;

/// <summary>
/// 时间格式化器
/// </summary>
public class TimeFormat
{
    /// <summary>
    /// 日期格式 <c>[yyyy-MM-dd]</c>
    /// </summary>
    public static readonly TimeFormat DATE = new("yyyy-MM-dd");

    /// <summary>
    /// 日期格式 <c>[yyyy年MM月dd日]</c>
    /// </summary>
    public static readonly TimeFormat DATE_CHINESE = new("yyyy年MM月dd日");

    /// <summary>
    /// 日期格式 <c>[yyyyMMdd]</c>
    /// </summary>
    public static readonly TimeFormat DATE_COMPACT = new("yyyyMMdd");

    /// <summary>
    /// 日期格式 <c>[yyyy_MM_dd]</c>
    /// </summary>
    public static readonly TimeFormat DATE_UNDERLINE = new("yyyy_MM_dd");

    /// <summary>
    /// 时间格式 <c>[HH:mm:ss]</c>
    /// </summary>
    public static readonly TimeFormat TIME = new("HH:mm:ss");

    /// <summary>
    /// 时间格式 <c>[HH时mm分ss秒]</c>
    /// </summary>
    public static readonly TimeFormat TIME_CHINESE = new("HH时mm分ss秒");

    /// <summary>
    /// 时间格式 <c>[HHmmss]</c>
    /// </summary>
    public static readonly TimeFormat TIME_COMPACT = new("HHmmss");

    /// <summary>
    /// 时间格式 <c>[HH_mm_ss]</c>
    /// </summary>
    public static readonly TimeFormat TIME_UNDERLINE = new("HH_mm_ss");

    /// <summary>
    /// 时间格式 <c>[HH:mm:ss.fff]</c>
    /// </summary>
    public static readonly TimeFormat TIME_MILLI = new("HH:mm:ss.fff");

    /// <summary>
    /// 时间格式 <c>[HHmmssfff]</c>
    /// </summary>
    public static readonly TimeFormat TIME_MILLI_COMPACT = new("HHmmssfff");

    /// <summary>
    /// 时间格式 <c>[HH_mm_ss_fff]</c>
    /// </summary>
    public static readonly TimeFormat TIME_MILLI_UNDERLINE = new("HH_mm_ss_fff");

    /// <summary>
    /// 日期时间格式 <c>[yyyy-MM-dd HH:mm:ss]</c>
    /// </summary>
    public static readonly TimeFormat DATE_TIME = new("yyyy-MM-dd HH:mm:ss");

    /// <summary>
    /// 日期时间格式 <c>[yyyy年MM月dd日 HH时mm分ss秒]</c>
    /// </summary>
    public static readonly TimeFormat DATE_TIME_CHINESE = new("yyyy年MM月dd日 HH时mm分ss秒");

    /// <summary>
    /// 日期时间格式 <c>[yyyyMMddHHmmss]</c>
    /// </summary>
    public static readonly TimeFormat DATE_TIME_COMPACT = new("yyyyMMddHHmmss");

    /// <summary>
    /// 日期时间格式 <c>[yyyy_MM_dd_HH_mm_ss]</c>
    /// </summary>
    public static readonly TimeFormat DATE_TIME_UNDERLINE = new("yyyy_MM_dd_HH_mm_ss");

    /// <summary>
    /// 日期时间格式 <c>[yyyy-MM-dd HH:mm:ss.fff]</c>
    /// </summary>
    public static readonly TimeFormat DATE_TIME_MILLI = new("yyyy-MM-dd HH:mm:ss.fff");

    /// <summary>
    /// 日期时间格式 <c>[yyyyMMddHHmmssfff]</c>
    /// </summary>
    public static readonly TimeFormat DATE_TIME_MILLI_COMPACT = new("yyyyMMddHHmmssfff");

    /// <summary>
    /// 日期时间格式 <c>[yyyy_MM_dd_HH_mm_ss_fff]</c>
    /// </summary>
    public static readonly TimeFormat DATE_TIME_MILLI_UNDERLINE = new("yyyy_MM_dd_HH_mm_ss_fff");

    /// <summary>
    /// 格式
    /// </summary>
    private readonly string _pattern;

    /// <summary>
    /// 初始化一个<see cref="TimeFormat"/>类型的实例
    /// </summary>
    private TimeFormat() { }

    /// <summary>
    /// 初始化一个<see cref="TimeFormat"/>类型的实例
    /// </summary>
    /// <param name="pattern">格式</param>
    private TimeFormat(string pattern) => _pattern = pattern;

    /// <summary>
    /// 获取当前时间
    /// </summary>
    /// <returns>返回格式化后的当前时间字符串</returns>
    public string Now() => Format(DateTime.Now);

    /// <summary>
    /// 将<see cref="DateTime"/>格式化为自身格式的时间字符串，且忽略区域性
    /// </summary>
    /// <param name="dateTime">日期时间</param>
    /// <returns>返回格式化后的时间字符串</returns>
    public string Format(DateTime dateTime) => dateTime.ToString(_pattern, DateTimeFormatInfo.InvariantInfo);

    /// <summary>
    ///  将<see cref="DateTime"/>格式化为自身格式的时间字符串，且忽略区域性
    /// </summary>
    /// <param name="dateTime">日期时间</param>
    /// <returns>返回格式化后的时间字符串，如果为null，则返回<see cref="string.Empty"/></returns>
    public string Format(DateTime? dateTime) => dateTime == null ? string.Empty : Format(dateTime.Value);
}