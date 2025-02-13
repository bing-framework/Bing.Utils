using System.Globalization;

namespace Bing.Date;

/// <summary>
/// 日期时间间隔
/// </summary>
public partial struct DateTimeSpan
{
    /// <summary>
    /// 尝试解析
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="result">解析结果</param>
    public static bool TryParse(string input, out DateTimeSpan result) =>
        DateTimeSpanParse.TryParse(input, null, out result);

    /// <summary>
    /// 尝试解析
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="provider">格式化提供程序</param>
    /// <param name="result">解析结果</param>
    public static bool TryParse(string input, IFormatProvider provider, out DateTimeSpan result) =>
        DateTimeSpanParse.TryParse(input, provider, out result);

    /// <summary>
    /// 尝试精确解析
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="format">格式化字符串</param>
    /// <param name="provider">格式化提供程序</param>
    /// <param name="result">解析结果</param>
    public static bool TryParseExact(string input, string format, IFormatProvider provider, out DateTimeSpan result) =>
        DateTimeSpanParse.TryParseExact(input, format, provider, TimeSpanStyles.None, out result);

    /// <summary>
    /// 尝试精确解析
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="formats">格式化字符串</param>
    /// <param name="provider">格式化提供程序</param>
    /// <param name="result">解析结果</param>
    public static bool TryParseExact(string input, string[] formats, IFormatProvider provider, out DateTimeSpan result) =>
        DateTimeSpanParse.TryParseExactMultiple(input, formats, provider, TimeSpanStyles.None, out result);

    /// <summary>
    /// 尝试精确解析
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="format">格式化字符串</param>
    /// <param name="provider">格式化提供程序</param>
    /// <param name="styles">时间间隔样式</param>
    /// <param name="result">解析结果</param>
    public static bool TryParseExact(string input, string format, IFormatProvider provider, TimeSpanStyles styles, out DateTimeSpan result) =>
        DateTimeSpanParse.TryParseExact(input, format, provider, styles, out result);

    /// <summary>
    /// 尝试精确解析
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="formats">格式化字符串</param>
    /// <param name="provider">格式化提供程序</param>
    /// <param name="styles">时间间隔样式</param>
    /// <param name="result">解析结果</param>
    public static bool TryParseExact(string input, string[] formats, IFormatProvider provider, TimeSpanStyles styles, out DateTimeSpan result) =>
        DateTimeSpanParse.TryParseExactMultiple(input, formats, provider, styles, out result);
}

/// <summary>
/// 日期时间间隔解析器
/// </summary>
internal static class DateTimeSpanParse
{
    /// <summary>
    /// 尝试解析
    /// </summary>
    /// <param name="s">字符串</param>
    /// <param name="provider">格式化提供程序</param>
    /// <param name="result">解析结果</param>
    public static bool TryParse(string s, IFormatProvider provider, out DateTimeSpan result) => TryCreateDateTimeSpan(TimeSpan.TryParse(s, provider, out var timeSpan), timeSpan, out result);

    /// <summary>
    /// 尝试精确解析
    /// </summary>
    /// <param name="s">字符串</param>
    /// <param name="format">格式化字符串</param>
    /// <param name="provider">格式化提供程序</param>
    /// <param name="styles">时间间隔样式</param>
    /// <param name="result">解析结果</param>
    public static bool TryParseExact(string s, string format, IFormatProvider provider, TimeSpanStyles styles, out DateTimeSpan result) =>
        TryCreateDateTimeSpan(TimeSpan.TryParseExact(s, format, provider, styles, out var timeSpan), timeSpan, out result);

    /// <summary>
    /// 尝试精确解析多个
    /// </summary>
    /// <param name="s">字符串</param>
    /// <param name="formats">格式化字符串</param>
    /// <param name="provider">格式化提供程序</param>
    /// <param name="styles">时间间隔样式</param>
    /// <param name="result">解析结果</param>
    public static bool TryParseExactMultiple(string s, string[] formats, IFormatProvider provider, TimeSpanStyles styles, out DateTimeSpan result) =>
        TryCreateDateTimeSpan(TimeSpan.TryParseExact(s, formats, provider, styles, out var timeSpan), timeSpan, out result);

    /// <summary>
    /// 尝试创建日期时间间隔
    /// </summary>
    /// <param name="condition">条件</param>
    /// <param name="timeSpan">时间间隔</param>
    /// <param name="result">结果</param>
    private static bool TryCreateDateTimeSpan(bool condition, TimeSpan timeSpan, out DateTimeSpan result)
    {
        result = default;
        if (!condition)
            return false;
        result = new DateTimeSpan { TimeSpan = timeSpan };
        return true;
    }
}