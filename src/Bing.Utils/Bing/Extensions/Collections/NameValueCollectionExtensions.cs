using System.Collections.Specialized;
using Bing.Text;

// ReSharper disable once CheckNamespace
namespace Bing.Extensions;

/// <summary>
/// 键值对集合(<see cref="NameValueCollection"/>) 扩展
/// </summary>
public static class NameValueCollectionExtensions
{
    #region ToQueryString(将键值对集合转换成查询字符串)

    /// <summary>
    /// 将键值对集合转换成查询字符串
    /// </summary>
    /// <param name="collection">键值对集合</param>
    /// <returns>RFC 3986 编码的查询字符串。</returns>
    /// <remarks>
    /// 键和值均进行 UTF-8 百分号编码，值为 <c>null</c> 的项会被忽略。
    /// </remarks>
    public static string ToQueryString(this NameValueCollection collection)
    {
        if (collection == null)
            return string.Empty;
        return QueryStringExtensions.ToQueryString(collection.AllKeys.Select(key =>
            new KeyValuePair<string, object>(key, collection[key])));
    }

    #endregion
}