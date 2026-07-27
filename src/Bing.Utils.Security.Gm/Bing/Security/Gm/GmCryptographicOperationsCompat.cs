using System;

namespace Bing.Security.Gm;

/// <summary>
/// 提供 GM 程序集内可用于清除敏感字节的跨目标框架兼容操作。
/// </summary>
internal static class GmCryptographicOperationsCompat
{
    /// <summary>
    /// 清零指定的敏感内存区域。
    /// </summary>
    /// <param name="buffer">需要清零的内存区域。</param>
    internal static void ZeroMemory(Span<byte> buffer)
    {
#if NETSTANDARD2_0
        for (var index = 0; index < buffer.Length; index++)
            buffer[index] = 0;
#else
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(buffer);
#endif
    }
}