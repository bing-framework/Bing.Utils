namespace Bing.Text.Templating;

/// <summary>
/// 模板渲染选项
/// </summary>
public sealed class TemplateOptions
{
    /// <summary>
    /// 获取或设置占位符起始分隔符，默认值为 <c>{{</c>。
    /// </summary>
    /// <exception cref="ArgumentException">当渲染时该值为 <c>null</c> 或空字符串时抛出。</exception>
    public string StartDelimiter { get; set; } = "{{";

    /// <summary>
    /// 获取或设置占位符结束分隔符，默认值为 <c>}}</c>。
    /// </summary>
    /// <exception cref="ArgumentException">当渲染时该值为 <c>null</c>、空字符串或与起始分隔符相同时抛出。</exception>
    public string EndDelimiter { get; set; } = "}}";

    /// <summary>
    /// 获取或设置未解析占位符是否保留原始文本，默认值为 <c>true</c>。
    /// </summary>
    public bool PreserveUnresolvedPlaceholder { get; set; } = true;

    /// <summary>
    /// 获取或设置键比较器，默认值为 <see cref="StringComparer.Ordinal"/>。
    /// </summary>
    /// <exception cref="ArgumentNullException">当渲染时该值为 <c>null</c> 时抛出。</exception>
    public StringComparer KeyComparer { get; set; } = StringComparer.Ordinal;
}