using System;

namespace Bing.Date.Chinese;
/// <summary>
/// 提供公历日期和时间点的农历转换扩展。
/// </summary>
public static class DateTimeLunarExtensions
{
    /// <summary>
    /// 按日期字段转换为农历日期。
    /// </summary>
    /// <param name="date">公历日期；忽略时刻和 Kind，不转换时区。</param>
    /// <returns>日期字段对应的农历日期。</returns>
    /// <exception cref="ArgumentOutOfRangeException">公历日期超出支持范围。</exception>
    public static LunarDate ToLunarDate(this DateTime date) => ChineseLunarCalendar.FromDate(date);
    /// <summary>
    /// 将时间点所在的 UTC+08:00 自然日转换为农历日期。
    /// </summary>
    /// <param name="instant">任意偏移的时间点。</param>
    /// <returns>北京时间自然日对应的农历日期。</returns>
    /// <exception cref="ArgumentOutOfRangeException">北京时间自然日超出支持范围。</exception>
    public static LunarDate ToLunarDate(this DateTimeOffset instant) => ChineseLunarCalendar.FromInstant(instant);
    /// <summary>
    /// 尝试按日期字段转换为农历日期。
    /// </summary>
    /// <param name="date">公历日期；忽略时刻和 Kind。</param>
    /// <param name="result">成功时为农历日期，失败时为默认无效值。</param>
    /// <returns>支持该日期时为 true，否则为 false。</returns>
    public static bool TryToLunarDate(this DateTime date, out LunarDate result) =>
        ChineseLunarCalendar.TryFromDate(date, out result);
    /// <summary>
    /// 尝试将时间点所在的 UTC+08:00 自然日转换为农历日期。
    /// </summary>
    /// <param name="instant">任意偏移的时间点。</param>
    /// <param name="result">成功时为农历日期，失败时为默认无效值。</param>
    /// <returns>支持该北京时间自然日时为 true，否则为 false。</returns>
    public static bool TryToLunarDate(this DateTimeOffset instant, out LunarDate result) =>
        ChineseLunarCalendar.TryFromInstant(instant, out result);
    /// <summary>
    /// 按日期字段生成农历日期文本。
    /// </summary>
    /// <param name="date">公历日期；忽略时刻和 Kind。</param>
    /// <param name="traditionalChineseCharacters">是否使用繁体字形。</param>
    /// <param name="includeYear">是否包含农历年份。</param>
    /// <returns>简体或繁体农历日期文本。</returns>
    /// <exception cref="ArgumentOutOfRangeException">公历日期超出支持范围。</exception>
    public static string ToLunarDateString(this DateTime date, bool traditionalChineseCharacters = false, bool includeYear = true) =>
        date.ToLunarDate().ToChineseString(traditionalChineseCharacters, includeYear);
    /// <summary>
    /// 按时间点所在的 UTC+08:00 自然日生成农历日期文本。
    /// </summary>
    /// <param name="instant">任意偏移的时间点。</param>
    /// <param name="traditionalChineseCharacters">是否使用繁体字形。</param>
    /// <param name="includeYear">是否包含农历年份。</param>
    /// <returns>简体或繁体农历日期文本。</returns>
    /// <exception cref="ArgumentOutOfRangeException">北京时间自然日超出支持范围。</exception>
    public static string ToLunarDateString(this DateTimeOffset instant, bool traditionalChineseCharacters = false, bool includeYear = true) =>
        instant.ToLunarDate().ToChineseString(traditionalChineseCharacters, includeYear);
}
