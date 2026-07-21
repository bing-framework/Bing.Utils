using Bing.Extensions;
using Bing.Helpers;

namespace Bing.Net;

/// <summary>
/// 查询字符串构建器
/// </summary>
public static class QueryStringBuilder
{
    /// <summary>
    /// 将对象的属性转换为 RFC 3986 编码的查询字符串。
    /// </summary>
    /// <param name="source">要转换的对象。为 <c>null</c> 时返回空字符串。</param>
    /// <param name="ignoreNullValues">是否忽略值为 <c>null</c> 的属性。为 <c>false</c> 时将其输出为空值。</param>
    /// <returns>不包含前导问号和尾部分隔符的查询字符串。</returns>
    /// <remarks>
    /// 使用 <see cref="Conv.ToDictionary(object)"/> 读取对象属性，因此遵循其
    /// <see cref="TypeDescriptor"/> 属性描述符行为。该方法不通过 JSON 序列化对象。
    /// </remarks>
    /// <example>
    /// <code>
    /// var query = QueryStringBuilder.FromObject(new { Page = 1, Keyword = "test value" });
    /// // Page=1&amp;Keyword=test%20value
    /// </code>
    /// </example>
    public static string FromObject(object source, bool ignoreNullValues = true) =>
        QueryStringExtensions.ToQueryString(Conv.ToDictionary(source), ignoreNullValues);
}