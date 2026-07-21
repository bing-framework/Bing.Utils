using System.Globalization;
using Bing.Helpers;

namespace Bing.Text.Templating;

/// <summary>
/// 简单文本模板渲染器
/// </summary>
public static class TemplateRenderer
{
    /// <summary>
    /// 获取模板中的占位符键。
    /// </summary>
    /// <param name="template">模板文本。</param>
    /// <param name="options">模板选项。为 <c>null</c> 时使用默认选项。</param>
    /// <returns>按首次出现顺序去重、且不包含分隔符的占位符键列表。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="template"/> 为 <c>null</c> 时抛出。</exception>
    /// <exception cref="ArgumentException">当选项中的分隔符无效时抛出。</exception>
    /// <remarks>
    /// 空占位符和未闭合占位符视为普通文本。该方法使用线性扫描，不使用正则表达式，
    /// 因此不受正则回溯影响。该方法不持有共享可变状态，可并发调用。
    /// </remarks>
    /// <example>
    /// <code>
    /// var keys = TemplateRenderer.GetKeys("{{name}}: {{name}} / {{age}}");
    /// // ["name", "age"]
    /// </code>
    /// </example>
    public static IReadOnlyList<string> GetKeys(string template, TemplateOptions options = null)
    {
        if (template == null)
            throw new ArgumentNullException(nameof(template));

        var effectiveOptions = ValidateOptions(options);
        var keys = new List<string>();
        var keySet = new HashSet<string>(effectiveOptions.KeyComparer);
        foreach (var placeholder in FindPlaceholders(template, effectiveOptions))
        {
            if (placeholder.Key.Length > 0 && keySet.Add(placeholder.Key))
                keys.Add(placeholder.Key);
        }
        return keys;
    }

    /// <summary>
    /// 使用键值字典渲染模板。
    /// </summary>
    /// <param name="template">模板文本。</param>
    /// <param name="values">模板键值字典。</param>
    /// <param name="options">模板选项。为 <c>null</c> 时使用默认选项。</param>
    /// <returns>渲染后的文本。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="template"/> 或 <paramref name="values"/> 为 <c>null</c> 时抛出。</exception>
    /// <exception cref="ArgumentException">当选项无效或键在比较器规则下重复时抛出。</exception>
    /// <remarks>
    /// 值为 <c>null</c> 的已解析键输出空字符串。未解析键由
    /// <see cref="TemplateOptions.PreserveUnresolvedPlaceholder"/> 决定保留原始占位符或输出空字符串。
    /// 值实现 <see cref="IFormattable"/> 时使用不变区域性格式化。该方法不支持嵌套路径、条件、循环或表达式。
    /// </remarks>
    /// <example>
    /// <code>
    /// var values = new Dictionary&lt;string, object&gt; { ["name"] = "张三" };
    /// var text = TemplateRenderer.Render("你好，{{name}}", values);
    /// // 你好，张三
    /// </code>
    /// </example>
    public static string Render(string template, IReadOnlyDictionary<string, object> values, TemplateOptions options = null)
    {
        if (template == null)
            throw new ArgumentNullException(nameof(template));
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        var effectiveOptions = ValidateOptions(options);
        var normalizedValues = NormalizeValues(values, effectiveOptions.KeyComparer);
        var builder = new StringBuilder(template.Length);
        var position = 0;
        foreach (var placeholder in FindPlaceholders(template, effectiveOptions))
        {
            builder.Append(template, position, placeholder.StartIndex - position);
            if (placeholder.Key.Length == 0)
            {
                builder.Append(template, placeholder.StartIndex, placeholder.Length);
            }
            else if (normalizedValues.TryGetValue(placeholder.Key, out var value))
            {
                builder.Append(FormatValue(value));
            }
            else if (effectiveOptions.PreserveUnresolvedPlaceholder)
            {
                builder.Append(template, placeholder.StartIndex, placeholder.Length);
            }
            position = placeholder.StartIndex + placeholder.Length;
        }
        builder.Append(template, position, template.Length - position);
        return builder.ToString();
    }

    /// <summary>
    /// 使用对象属性渲染模板。
    /// </summary>
    /// <param name="template">模板文本。</param>
    /// <param name="model">模板对象模型。为 <c>null</c> 时按空键值字典处理。</param>
    /// <param name="options">模板选项。为 <c>null</c> 时使用默认选项。</param>
    /// <returns>渲染后的文本。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="template"/> 为 <c>null</c> 时抛出。</exception>
    /// <exception cref="ArgumentException">当选项无效或对象属性在比较器规则下重复时抛出。</exception>
    /// <remarks>
    /// 使用 <see cref="Conv.ToDictionary(object)"/> 将对象属性转换为键值字典，不通过 JSON 序列化对象。
    /// 该方法不支持嵌套路径、条件、循环或表达式。
    /// </remarks>
    public static string Render(string template, object model, TemplateOptions options = null) =>
        Render(template, new Dictionary<string, object>(Conv.ToDictionary(model)), options);

    /// <summary>
    /// 验证并获取有效模板选项。
    /// </summary>
    /// <param name="options">模板选项。</param>
    /// <returns>有效的模板选项。</returns>
    /// <exception cref="ArgumentException">当分隔符无效时抛出。</exception>
    /// <exception cref="ArgumentNullException">当键比较器为空时抛出。</exception>
    private static TemplateOptions ValidateOptions(TemplateOptions options)
    {
        options ??= new TemplateOptions();
        if (string.IsNullOrEmpty(options.StartDelimiter))
            throw new ArgumentException("起始分隔符不能为空。", nameof(options));
        if (string.IsNullOrEmpty(options.EndDelimiter))
            throw new ArgumentException("结束分隔符不能为空。", nameof(options));
        if (string.Equals(options.StartDelimiter, options.EndDelimiter, StringComparison.Ordinal))
            throw new ArgumentException("起始分隔符和结束分隔符不能相同。", nameof(options));
        if (options.KeyComparer == null)
            throw new ArgumentNullException(nameof(options), "键比较器不能为空。");
        return options;
    }

    /// <summary>
    /// 按模板比较器创建键值字典快照。
    /// </summary>
    /// <param name="values">原始键值字典。</param>
    /// <param name="keyComparer">键比较器。</param>
    /// <returns>标准化后的键值字典。</returns>
    /// <exception cref="ArgumentException">当键为空或键在比较器规则下重复时抛出。</exception>
    private static Dictionary<string, object> NormalizeValues(IReadOnlyDictionary<string, object> values, StringComparer keyComparer)
    {
        var result = new Dictionary<string, object>(keyComparer);
        foreach (var value in values)
        {
            if (value.Key == null)
                throw new ArgumentException("模板键不能为 null。", nameof(values));
            if (result.ContainsKey(value.Key))
                throw new ArgumentException("模板键在指定比较器规则下重复。", nameof(values));
            result.Add(value.Key, value.Value);
        }
        return result;
    }

    /// <summary>
    /// 查找完整占位符。
    /// </summary>
    /// <param name="template">模板文本。</param>
    /// <param name="options">模板选项。</param>
    /// <returns>完整占位符序列。</returns>
    private static IEnumerable<Placeholder> FindPlaceholders(string template, TemplateOptions options)
    {
        var position = 0;
        while (position < template.Length)
        {
            var startIndex = template.IndexOf(options.StartDelimiter, position, StringComparison.Ordinal);
            if (startIndex < 0)
                yield break;

            var keyStartIndex = startIndex + options.StartDelimiter.Length;
            var endIndex = template.IndexOf(options.EndDelimiter, keyStartIndex, StringComparison.Ordinal);
            if (endIndex < 0)
                yield break;

            var length = endIndex + options.EndDelimiter.Length - startIndex;
            yield return new Placeholder(startIndex, length, template.Substring(keyStartIndex, endIndex - keyStartIndex));
            position = startIndex + length;
        }
    }

    /// <summary>
    /// 将模板值格式化为文本。
    /// </summary>
    /// <param name="value">模板值。</param>
    /// <returns>格式化后的文本。</returns>
    private static string FormatValue(object value) => value switch
    {
        null => string.Empty,
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString()
    };

    /// <summary>
    /// 模板占位符位置。
    /// </summary>
    /// <param name="StartIndex">占位符起始位置。</param>
    /// <param name="Length">占位符长度。</param>
    /// <param name="Key">不包含分隔符的键。</param>
    private readonly record struct Placeholder(int StartIndex, int Length, string Key);
}