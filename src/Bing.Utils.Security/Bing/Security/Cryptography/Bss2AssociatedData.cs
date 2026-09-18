#if NET6_0_OR_GREATER
using System.Buffers.Binary;

namespace Bing.Security.Cryptography;

/// <summary>
/// 写入 BSS2 记录附加认证数据。
/// </summary>
internal static class Bss2AssociatedData
{
    internal static int Write(Span<byte> destination, ReadOnlySpan<byte> headerDigest, byte recordType, uint index, uint length, bool isFinal, ReadOnlySpan<byte> extra)
    {
        if (headerDigest.Length != 32)
            throw new ArgumentException("BSS2 格式头摘要必须为 32 字节。", nameof(headerDigest));
        if (destination.Length < Bss2Constants.AuthenticationDataLength + extra.Length)
            throw new ArgumentException("BSS2 附加认证数据目标缓冲区长度不足。", nameof(destination));

        headerDigest.CopyTo(destination);
        destination[32] = Bss2Constants.Version;
        destination[33] = recordType;
        BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(34, 4), index);
        BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(38, 4), length);
        destination[42] = isFinal ? (byte)1 : (byte)0;
        extra.CopyTo(destination.Slice(Bss2Constants.AuthenticationDataLength));
        return Bss2Constants.AuthenticationDataLength + extra.Length;
    }
}
#endif