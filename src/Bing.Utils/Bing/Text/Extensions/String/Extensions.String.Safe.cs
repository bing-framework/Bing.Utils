using System.Web;

// ReSharper disable once CheckNamespace
namespace Bing.Text;

/// <summary>
/// 字符串(<see cref="string"/>) 扩展
/// </summary>
public static partial class StringExtensions
{
    #region UrlEncode(Url编码)

    /// <summary>
    /// Url编码，默认编码为 <see cref="Encoding.UTF8"/>
    /// </summary>
    /// <param name="source">url编码字符串</param>
    /// <param name="encoding">编码格式</param>
    public static string UrlEncode(this string source, Encoding encoding = null)
    {
        encoding ??= Encoding.UTF8;
        return HttpUtility.UrlEncode(source, encoding);
    }

    #endregion

    #region UrlDecode(Url解码)

    /// <summary>
    /// Url解码，默认编码为 <see cref="Encoding.UTF8"/>
    /// </summary>
    /// <param name="source">url编码字符串</param>
    /// <param name="encoding">编码格式</param>
    public static string UrlDecode(this string source, Encoding encoding = null)
    {
        encoding ??= Encoding.UTF8;
        return HttpUtility.UrlDecode(source, encoding);
    }

    #endregion
}