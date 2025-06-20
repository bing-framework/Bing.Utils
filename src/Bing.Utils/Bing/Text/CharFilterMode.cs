namespace Bing.Text;

/// <summary>
/// 字符串过滤模式
/// </summary>
public enum CharFilterMode
{
    /// <summary>
    /// 标准模式，使用.NET内置的字符类型判断，支持Unicode
    /// </summary>
    Standard,

    /// <summary>
    /// ASCII模式，仅识别基本拉丁字母和数字
    /// </summary>
    Ascii
}