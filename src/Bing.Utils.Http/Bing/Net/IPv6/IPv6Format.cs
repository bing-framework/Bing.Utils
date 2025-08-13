namespace Bing.Net.IPv6;

/// <summary>
/// IPv6地址格式枚举
/// </summary>
public enum IPv6Format
{
    /// <summary>
    /// 压缩格式（默认）
    /// </summary>
    Compressed,

    /// <summary>
    /// 完整展开格式
    /// </summary>
    Expanded,

    /// <summary>
    /// 十六进制格式（无分隔符）
    /// </summary>
    Hexadecimal,

    /// <summary>
    /// 二进制格式
    /// </summary>
    Binary
}