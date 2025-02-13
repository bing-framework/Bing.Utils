namespace Bing.Net.FTP;

/// <summary>
/// 加密方式
/// </summary>
public enum EncryptionType
{
    /// <summary>
    /// 无，无需加密
    /// </summary>
    None = 0,

    /// <summary>
    /// 隐式加密（SSL）
    /// </summary>
    Implicit = 1,

    /// <summary>
    /// 显式加密（TLS）
    /// </summary>
    Explicit = 2
}