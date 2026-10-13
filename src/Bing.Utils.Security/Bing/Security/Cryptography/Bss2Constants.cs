#if NET6_0_OR_GREATER
namespace Bing.Security.Cryptography;

/// <summary>
/// 定义 BSS2 认证流格式的固定常量。
/// </summary>
internal static class Bss2Constants
{
    /// <summary>
    /// BSS2 认证流格式的版本号。
    /// </summary>
    internal const byte Version = 2;

    /// <summary>
    /// AES-256-GCM 加密算法的格式标识。
    /// </summary>
    internal const byte AlgorithmAes256Gcm = 1;

    /// <summary>
    /// HKDF-SHA256 密钥派生算法的格式标识。
    /// </summary>
    internal const byte KdfHkdfSha256 = 1;

    /// <summary>
    /// BSS2 数据记录的类型标识。
    /// </summary>
    internal const byte DataRecordType = 1;

    /// <summary>
    /// BSS2 最终记录的类型标识。
    /// </summary>
    internal const byte FinalRecordType = 2;

    /// <summary>
    /// BSS2 固定头的长度，单位为字节。
    /// </summary>
    internal const int HeaderLength = 52;

    /// <summary>
    /// 每文件随机盐的长度，单位为字节。
    /// </summary>
    internal const int FileSaltLength = 32;

    /// <summary>
    /// 每文件 Nonce 前缀的长度，单位为字节。
    /// </summary>
    internal const int NoncePrefixLength = 8;

    /// <summary>
    /// 数据记录头的长度，单位为字节。
    /// </summary>
    internal const int DataRecordHeaderLength = 8;

    /// <summary>
    /// 最终记录附加数据的长度，单位为字节。
    /// </summary>
    internal const int FinalRecordDataLength = 12;

    /// <summary>
    /// 固定附加认证数据的长度，单位为字节。
    /// </summary>
    internal const int AuthenticationDataLength = 43;
}
#endif
