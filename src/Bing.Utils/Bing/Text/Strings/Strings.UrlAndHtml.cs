using System.Web;

namespace Bing.Text;

/// <summary>
/// 字符串工具
/// </summary>
public static partial class Strings
{
    #region As or From Url/Html Value

    /// <summary>
    /// 作为URL值，将字符串进行URL编码。
    /// </summary>
    /// <param name="value">字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string AsUrlValue(string value)
    {
        return HttpUtility.UrlEncode(value);
    }

    /// <summary>
    /// 作为URL值，将字符串进行URL编码。
    /// </summary>
    /// <param name="value">字符串</param>
    /// <param name="encoding">编码格式。默认编码为：<see cref="Encoding.UTF8"/></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string AsUrlValue(string value, Encoding encoding)
    {
        return HttpUtility.UrlEncode(value, encoding ?? Encoding.UTF8);
    }

    /// <summary>
    /// 作为HTML值，将字符串进行HTML编码。
    /// </summary>
    /// <param name="value">字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string AsHtmlValue(string value)
    {
        return HttpUtility.HtmlEncode(value);
    }

    /// <summary>
    /// 来自URL值，将字符串进行URL解码。
    /// </summary>
    /// <param name="value">字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string FromUrlValue(string value)
    {
        return HttpUtility.UrlDecode(value);
    }

    /// <summary>
    /// 来自URL值，将字符串进行URL解码。
    /// </summary>
    /// <param name="value">字符串</param>
    /// <param name="encoding">编码格式。默认编码为：<see cref="Encoding.UTF8"/></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string FromUrlValue(string value, Encoding encoding)
    {
        return HttpUtility.UrlDecode(value, encoding ?? Encoding.UTF8);
    }

    /// <summary>
    /// 来自HTML值，将字符串进行HTML解码。
    /// </summary>
    /// <param name="value">字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string FromHtmlValue(string value)
    {
        return HttpUtility.HtmlDecode(value);
    }

    #endregion
}

/// <summary>
/// 字符串扩展
/// </summary>
public static partial class StringsExtensions
{
    #region As or From Url/Html Value

    /// <summary>
    /// 作为URL值，将字符串进行URL编码。
    /// </summary>
    /// <param name="value">字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string AsUrlValue(this string value)
    {
        return Strings.AsUrlValue(value);
    }

    /// <summary>
    /// 作为URL值，将字符串进行URL编码。
    /// </summary>
    /// <param name="value">字符串</param>
    /// <param name="encoding">编码格式。默认编码为：<see cref="Encoding.UTF8"/></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string AsUrlValue(this string value, Encoding encoding)
    {
        return Strings.AsUrlValue(value, encoding);
    }

    /// <summary>
    /// 作为HTML值，将字符串进行HTML编码。
    /// </summary>
    /// <param name="value">字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string AsHtmlValue(this string value)
    {
        return Strings.AsHtmlValue(value);
    }

    /// <summary>
    /// 来自URL值，将字符串进行URL解码。
    /// </summary>
    /// <param name="value">字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string FromUrlValue(this string value)
    {
        return Strings.FromUrlValue(value);
    }

    /// <summary>
    /// 来自URL值，将字符串进行URL解码。
    /// </summary>
    /// <param name="value">字符串</param>
    /// <param name="encoding">编码格式。默认编码为：<see cref="Encoding.UTF8"/></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string FromUrlValue(this string value, Encoding encoding)
    {
        return Strings.FromUrlValue(value, encoding);
    }

    /// <summary>
    /// 来自HTML值，将字符串进行HTML解码。
    /// </summary>
    /// <param name="value">字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string FromHtmlValue(this string value)
    {
        return Strings.FromHtmlValue(value);
    }

    #endregion
}