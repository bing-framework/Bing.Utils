#if NET6_0_OR_GREATER
namespace Bing.Security.Cryptography;

/// <summary>
/// 定义 BSS2 认证流格式的固定常量。
/// </summary>
internal static class Bss2Constants
{
    internal const byte Version = 2;
    internal const byte AlgorithmAes256Gcm = 1;
    internal const byte KdfHkdfSha256 = 1;
    internal const byte DataRecordType = 1;
    internal const byte FinalRecordType = 2;
    internal const int HeaderLength = 52;
    internal const int FileSaltLength = 32;
    internal const int NoncePrefixLength = 8;
    internal const int DataRecordHeaderLength = 8;
    internal const int FinalRecordDataLength = 12;
    internal const int AuthenticationDataLength = 43;
}
#endif