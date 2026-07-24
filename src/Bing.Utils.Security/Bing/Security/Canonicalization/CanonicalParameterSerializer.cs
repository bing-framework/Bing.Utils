using System.Collections;
using System.Globalization;
using System.Text;

namespace Bing.Security.Canonicalization;

/// <summary>
/// 使用固定文化、编码和排序规则将显式参数序列化为可签名文本。
/// </summary>
public static class CanonicalParameterSerializer
{
    /// <summary>
    /// 序列化参数集合。
    /// </summary>
    /// <param name="parameters">显式参数集合，可包含重复键和数组值。</param>
    /// <param name="options">规范化选项，未指定时使用默认规则。</param>
    /// <returns>由确定性规则生成的规范化参数文本。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="parameters"/> 为 <c>null</c> 时抛出。</exception>
    /// <exception cref="ArgumentException">参数、选项或参数值不符合规则时抛出。</exception>
    public static string Serialize(IEnumerable<CanonicalParameter> parameters, CanonicalParameterOptions options = null)
    {
        if (parameters == null)
            throw new ArgumentNullException(nameof(parameters));

        options ??= new CanonicalParameterOptions();
        ValidateOptions(options);
        var pairs = new List<CanonicalPair>();
        var index = 0;
        foreach (var parameter in parameters)
        {
            if (parameter == null)
                throw new ArgumentException("参数集合不能包含 null 项。", nameof(parameters));
            foreach (var value in ExpandValues(parameter.Values))
            {
                if (value == null && options.IgnoreNullValues)
                    continue;
                pairs.Add(new CanonicalPair(parameter.Key, FormatValue(value, options), value != null, index++));
            }
        }

        if (options.SortOrdinal)
            pairs.Sort(static (left, right) =>
            {
                var result = string.Compare(left.Key, right.Key, StringComparison.Ordinal);
                return result != 0 ? result : left.Index.CompareTo(right.Index);
            });

        var builder = new StringBuilder();
        for (var pairIndex = 0; pairIndex < pairs.Count; pairIndex++)
        {
            if (pairIndex > 0)
                builder.Append(options.PairSeparator);
            var pair = pairs[pairIndex];
            builder.Append(options.UrlEncodeKeys ? Uri.EscapeDataString(pair.Key) : pair.Key);
            if (!pair.HasValue)
                continue;
            builder.Append(options.KeyValueSeparator);
            builder.Append(options.UrlEncodeValues ? Uri.EscapeDataString(pair.Value) : pair.Value);
        }
        return builder.ToString();
    }

    /// <summary>
    /// 展开单个参数的值和数组值。
    /// </summary>
    /// <param name="values">参数值。</param>
    /// <returns>展开后的值序列。</returns>
    private static IEnumerable<object> ExpandValues(IReadOnlyList<object> values)
    {
        foreach (var value in values)
        {
            if (value is string || value is byte[] || value is not IEnumerable enumerable)
            {
                yield return value;
                continue;
            }

            foreach (var item in enumerable)
                yield return item;
        }
    }

    /// <summary>
    /// 将允许的显式参数类型转换为文化无关文本。
    /// </summary>
    /// <param name="value">参数值。</param>
    /// <param name="options">规范化选项。</param>
    /// <returns>规范化文本；<c>null</c> 值返回空字符串。</returns>
    /// <exception cref="ArgumentException">值类型不在受支持的确定性类型集合中时抛出。</exception>
    private static string FormatValue(object value, CanonicalParameterOptions options)
    {
        if (value == null)
            return string.Empty;
        return value switch
        {
            string text => text,
            bool boolean => boolean ? "true" : "false",
            DateTime dateTime => dateTime.ToUniversalTime().ToString(options.DateTimeFormat, CultureInfo.InvariantCulture),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToUniversalTime().ToString(options.DateTimeFormat, CultureInfo.InvariantCulture),
            byte[] bytes => Convert.ToBase64String(bytes),
            sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal or char or Guid or TimeSpan or Enum => FormatInvariant(value),
            _ => throw new ArgumentException("参数值类型必须是显式支持的标量、字节数组或数组类型。", nameof(value))
        };
    }

    /// <summary>
    /// 将实现格式化接口的标量转换为固定文化文本。
    /// </summary>
    /// <param name="value">标量值。</param>
    /// <returns>固定文化格式文本。</returns>
    private static string FormatInvariant(object value)
    {
        return value is IFormattable formattable
            ? formattable.ToString(null, CultureInfo.InvariantCulture)
            : Convert.ToString(value, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 验证规范化选项。
    /// </summary>
    /// <param name="options">规范化选项。</param>
    /// <exception cref="ArgumentException">分隔符或日期格式无效时抛出。</exception>
    private static void ValidateOptions(CanonicalParameterOptions options)
    {
        if (options.PairSeparator == null)
            throw new ArgumentException("参数对分隔符不能为 null。", nameof(options));
        if (options.KeyValueSeparator == null)
            throw new ArgumentException("键值分隔符不能为 null。", nameof(options));
        if (string.IsNullOrEmpty(options.DateTimeFormat))
            throw new ArgumentException("日期时间格式不能为空。", nameof(options));
    }

    /// <summary>
    /// 表示已展开的规范化参数对。
    /// </summary>
    /// <param name="Key">参数键。</param>
    /// <param name="Value">参数值文本。</param>
    /// <param name="HasValue">是否为非 null 值。</param>
    /// <param name="Index">输入稳定顺序索引。</param>
    private sealed record CanonicalPair(string Key, string Value, bool HasValue, int Index);
}