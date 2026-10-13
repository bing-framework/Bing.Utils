#if NET6_0_OR_GREATER
using System.Buffers.Binary;

namespace Bing.Security.Cryptography;

/// <summary>
/// 创建 BSS2 记录专属 Nonce。
/// </summary>
internal static class Bss2NonceSequence
{
    /// <summary>
    /// 根据固定前缀和记录序号写入 12 字节 Nonce。
    /// </summary>
    /// <param name="prefix">8 字节的 Nonce 前缀。</param>
    /// <param name="index">记录序号，以大端序写入 Nonce 尾部。</param>
    /// <param name="destination">长度为 12 字节的目标缓冲区。</param>
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
