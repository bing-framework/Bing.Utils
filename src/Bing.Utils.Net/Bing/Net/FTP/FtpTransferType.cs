namespace Bing.Net.FTP;

/// <summary>
/// 指定 FTP 文件传输的数据类型。
/// </summary>
public enum FtpTransferType
{
    /// <summary>
    /// 以二进制方式传输数据。
    /// </summary>
    Binary = 0,

    /// <summary>
    /// 以 ASCII 文本方式传输数据。
    /// </summary>
    Ascii = 1
}
