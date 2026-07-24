namespace Bing.Security.Canonicalization;

/// <summary>
/// 表示一个可包含重复值的显式请求参数。
/// </summary>
public sealed class CanonicalParameter
{
    /// <summary>
    /// 参数键。
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// 参数值序列；数组会被展开为使用相同键的多个参数对。
    /// </summary>
    public IReadOnlyList<object> Values { get; }

    /// <summary>
    /// 使用参数键和一个或多个显式值初始化参数。
    /// </summary>
    /// <param name="key">非空参数键。</param>
    /// <param name="values">参数值；可使用 <c>null</c> 表示显式空值。</param>
    /// <exception cref="ArgumentException"><paramref name="key"/> 为空时抛出。</exception>
    public CanonicalParameter(string key, params object[] values)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("参数键不能为空。", nameof(key));
        Key = key;
        Values = (values ?? Array.Empty<object>()).ToArray();
    }
}