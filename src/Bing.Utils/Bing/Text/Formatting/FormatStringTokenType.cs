namespace Bing.Text.Formatting;

/// <summary>
/// 格式化字符串占位符
/// </summary>
internal enum FormatStringTokenType
{
    /// <summary>
    /// 文本常量
    /// </summary>
    ConstantText,

    /// <summary>
    /// 变量
    /// </summary>
    DynamicValue
}