using System.Globalization;

// ReSharper disable once CheckNamespace
namespace Bing.Extensions;

/// <summary>
/// 查询字符串扩展
/// </summary>
public static class QueryStringExtensions
{
    /// <summary>
    /// 将键值对集合转换为 RFC 3986 编码的查询字符串。
    /// </summary>
    /// <param name="source">键值对集合。为 <c>null</c> 时返回空字符串。</param>
    /// <param name="ignoreNullValues">是否忽略值为 <c>null</c> 的项。为 <c>false</c> 时将其输出为空值。</param>
    /// <returns>不包含前导问号和尾部分隔符的查询字符串。</returns>
    /// <exception cref="ArgumentException">当集合中包含 <c>null</c> 键时抛出。</exception>
    /// <remarks>
    /// 键和值均使用 UTF-8 百分号编码，空格编码为 <c>%20</c>。数值和日期等实现
    /// <see cref="IFormattable"/> 的值使用不变区域性格式化；<see cref="DateTime"/> 和
    /// <see cref="DateTimeOffset"/> 使用 round-trip 格式。该方法不排序，保留源集合枚举顺序。
    /// 该方法不持有共享可变状态，线程安全性取决于源集合的并发枚举安全性。
    /// </remarks>
    /// <example>
    /// <code>
    /// var query = new[] { new KeyValuePair&lt;string, object&gt;("name", "张三") }.ToQueryString();
    /// // name=%E5%BC%A0%E4%B8%89
    /// </code>
    /// </example>
    public static string ToQueryString(this IEnumerable<KeyValuePair<string, object>> source, bool ignoreNullValues = true)
    {
        if (source == null)
            return string.Empty;

        var builder = new StringBuilder();
        foreach (var item in source)
        {
            if (item.Key == null)
                throw new ArgumentException("查询字符串键不能为 null。", nameof(source));
            if (item.Value == null && ignoreNullValues)
                continue;

            if (builder.Length > 0)
                builder.Append('&');
            builder.Append(Uri.EscapeDataString(item.Key));
            builder.Append('=');
            if (item.Value != null)
                builder.Append(Uri.EscapeDataString(FormatValue(item.Value)));
        }
        return builder.ToString();
    }

    /// <summary>
    /// 将查询字符串值格式化为与区域性无关的字符串。
    /// </summary>
    /// <param name="value">要格式化的值。</param>
    /// <returns>格式化后的字符串。</returns>
    private static string FormatValue(object value) => value switch
    {
        DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
        DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
        bool boolean => boolean ? "true" : "false",
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString()
    };
}