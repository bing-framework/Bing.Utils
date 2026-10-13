namespace Bing.Text.Formatting;

/// <summary>
/// 格式化字符串占位符
/// </summary>
internal class FormatStringToken
{
    /// <summary>
    /// 初始化一个 <see cref="FormatStringToken"/> 类型的实例。
    /// </summary>
    /// <param name="text">文本</param>
    /// <param name="type">类型</param>
    public FormatStringToken(string text, FormatStringTokenType type)
    {
        Text = text;
        Type = type;
    }

    /// <summary>
    /// 获取或设置文本
    /// </summary>
    public string Text { get; private set; }

    /// <summary>
    /// 获取或设置类型
    /// </summary>
    public FormatStringTokenType Type { get; private set; }
}
