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
            ValidateRawComponent(pair.Key, options, true);
            builder.Append(options.UrlEncodeKeys ? Uri.EscapeDataString(pair.Key) : pair.Key);
            if (!pair.HasValue)
                continue;
            ValidateRawComponent(pair.Value, options, false);
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
            DateTime dateTime => FormatDateTime(dateTime, options),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToUniversalTime().ToString(options.DateTimeFormat, CultureInfo.InvariantCulture),
            byte[] bytes => Convert.ToBase64String(bytes),
            float single => FormatFloatingPoint(single),
            double doubleValue => FormatFloatingPoint(doubleValue),
            decimal decimalValue => decimalValue.ToString("G29", CultureInfo.InvariantCulture),
            Guid guid => guid.ToString("D"),
            TimeSpan timeSpan => timeSpan.ToString("c", CultureInfo.InvariantCulture),
            Enum enumeration => FormatEnum(enumeration),
            sbyte or byte or short or ushort or int or uint or long or ulong or char => FormatInvariant(value),
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
    /// 将 DateTime 转换为确定性 UTC 往返格式，拒绝未指定时区。
    /// </summary>
    /// <param name="value">要规范化的日期时间。</param>
    /// <param name="options">规范化选项。</param>
    /// <returns>UTC 往返格式时间文本。</returns>
    /// <exception cref="ArgumentException">日期时间 Kind 未指定时抛出。</exception>
    private static string FormatDateTime(DateTime value, CanonicalParameterOptions options)
    {
        if (value.Kind == DateTimeKind.Unspecified)
            throw new ArgumentException("DateTime 必须指定 Utc 或 Local Kind；请优先使用 DateTimeOffset。", nameof(value));
        return value.ToUniversalTime().ToString(options.DateTimeFormat, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 将单精度浮点数转换为可往返的确定性文本。
    /// </summary>
    /// <param name="value">单精度浮点数。</param>
    /// <returns>可往返格式文本。</returns>
    /// <exception cref="ArgumentException">值为 NaN 或无穷大时抛出。</exception>
    private static string FormatFloatingPoint(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
            throw new ArgumentException("参数浮点值不能为 NaN 或无穷大。", nameof(value));
        return value.ToString("R", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 将双精度浮点数转换为可往返的确定性文本。
    /// </summary>
    /// <param name="value">双精度浮点数。</param>
    /// <returns>可往返格式文本。</returns>
    /// <exception cref="ArgumentException">值为 NaN 或无穷大时抛出。</exception>
    private static string FormatFloatingPoint(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            throw new ArgumentException("参数浮点值不能为 NaN 或无穷大。", nameof(value));
        return value.ToString("R", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 将枚举值规范化为名称；未定义的数值使用无符号十进制数。
    /// </summary>
    /// <param name="value">枚举值。</param>
    /// <returns>确定性枚举文本。</returns>
    private static string FormatEnum(Enum value)
    {
        return Enum.GetName(value.GetType(), value) ?? value.ToString("D");
    }

    /// <summary>
    /// 验证规范化选项。
    /// </summary>
    /// <param name="options">规范化选项。</param>
    /// <exception cref="ArgumentException">分隔符或日期格式无效时抛出。</exception>
    private static void ValidateOptions(CanonicalParameterOptions options)
    {
        if (string.IsNullOrEmpty(options.PairSeparator))
            throw new ArgumentException("参数对分隔符不能为空。", nameof(options));
        if (string.IsNullOrEmpty(options.KeyValueSeparator))
            throw new ArgumentException("键值分隔符不能为空。", nameof(options));
        if (options.PairSeparator == options.KeyValueSeparator || options.PairSeparator.IndexOf(options.KeyValueSeparator, StringComparison.Ordinal) >= 0 || options.KeyValueSeparator.IndexOf(options.PairSeparator, StringComparison.Ordinal) >= 0)
            throw new ArgumentException("参数对分隔符和键值分隔符不能相同或互为包含关系。", nameof(options));
        if (options.DateTimeFormat != "O")
            throw new ArgumentException("确定性参数规范化仅支持 O 日期时间格式。", nameof(options));
    }

    /// <summary>
    /// 在关闭 URL 编码时拒绝会与协议分隔符冲突的原始文本。
    /// </summary>
    /// <param name="value">原始键或值文本。</param>
    /// <param name="options">规范化选项。</param>
    /// <param name="isKey">为 <c>true</c> 时验证参数键。</param>
    /// <exception cref="ArgumentException">未编码文本包含协议分隔符时抛出。</exception>
    private static void ValidateRawComponent(string value, CanonicalParameterOptions options, bool isKey)
    {
        if ((isKey && options.UrlEncodeKeys) || (!isKey && options.UrlEncodeValues))
            return;
        if (value.IndexOf(options.PairSeparator, StringComparison.Ordinal) >= 0 || value.IndexOf(options.KeyValueSeparator, StringComparison.Ordinal) >= 0)
            throw new ArgumentException("关闭 URL 编码时，参数键和值不能包含协议分隔符。", isKey ? "key" : "value");
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