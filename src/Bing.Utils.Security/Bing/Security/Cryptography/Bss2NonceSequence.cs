#if NET6_0_OR_GREATER
using System.Buffers.Binary;

namespace Bing.Security.Cryptography;

/// <summary>
/// 创建 BSS2 记录专属 Nonce。
/// </summary>
internal static class Bss2NonceSequence
{
    internal static void Write(ReadOnlySpan<byte> prefix, uint index, Span<byte> destination)
    {
        if (prefix.Length != Bss2Constants.NoncePrefixLength)
            throw new ArgumentException("BSS2 Nonce 前缀必须为 8 字节。", nameof(prefix));
        if (destination.Length != AesGcmPayload.NonceSize)
            throw new ArgumentException("BSS2 Nonce 目标必须为 12 字节。", nameof(destination));

        prefix.CopyTo(destination);
        BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(Bss2Constants.NoncePrefixLength, 4), index);
    }
}
#endif