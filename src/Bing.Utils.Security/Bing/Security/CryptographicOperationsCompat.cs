using System.Security.Cryptography;

namespace Bing.Security;

/// <summary>
/// 提供在旧目标框架上可用的密码学内存操作。
/// </summary>
internal static class CryptographicOperationsCompat
{
    /// <summary>
    /// 清零包含敏感数据的缓冲区。
    /// </summary>
    /// <param name="buffer">要清零的缓冲区。</param>
    internal static void ZeroMemory(Span<byte> buffer)
    {
#if NETSTANDARD2_0
        for (var index = 0; index < buffer.Length; index++)
            buffer[index] = 0;
#else
        CryptographicOperations.ZeroMemory(buffer);
#endif
    }
}