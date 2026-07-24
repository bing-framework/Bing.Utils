namespace Bing.Security.Canonicalization;

/// <summary>
/// 配置确定性参数规范化规则。
/// </summary>
public sealed class CanonicalParameterOptions
{
    /// <summary>
    /// 参数对之间使用的分隔符。
    /// </summary>
    public string PairSeparator { get; init; } = "&";

    /// <summary>
    /// 参数键和值之间使用的分隔符。
    /// </summary>
    public string KeyValueSeparator { get; init; } = "=";

    /// <summary>
    /// 指示是否忽略 <c>null</c> 值；为 <c>false</c> 时将 <c>null</c> 序列化为仅含键的参数，以区别于空字符串。
    /// </summary>
    public bool IgnoreNullValues { get; init; } = true;

    /// <summary>
    /// 指示是否使用 Ordinal 规则按键排序；为 <c>false</c> 时保留输入顺序。
    /// </summary>
    public bool SortOrdinal { get; init; } = true;

    /// <summary>
    /// 指示是否对参数键执行 RFC 3986 兼容 URL 编码。
    /// </summary>
    public bool UrlEncodeKeys { get; init; }

    /// <summary>
    /// 指示是否对参数值执行 RFC 3986 兼容 URL 编码。
    /// </summary>
    public bool UrlEncodeValues { get; init; }

    /// <summary>
    /// DateTime 和 DateTimeOffset 使用的固定格式，默认使用 ISO 8601 往返格式。
    /// </summary>
    public string DateTimeFormat { get; init; } = "O";
}