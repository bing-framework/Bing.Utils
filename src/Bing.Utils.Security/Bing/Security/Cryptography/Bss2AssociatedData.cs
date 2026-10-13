#if NET6_0_OR_GREATER
using System.Buffers.Binary;

namespace Bing.Security.Cryptography;

/// <summary>
/// 写入 BSS2 记录附加认证数据。
/// </summary>
internal static class Bss2AssociatedData
{
    /// <summary>
    /// 将 BSS2 记录元数据和扩展数据写入附加认证数据缓冲区。
    /// </summary>
    /// <param name="destination">用于写入附加认证数据的目标缓冲区。</param>
    /// <param name="headerDigest">32 字节的格式头摘要。</param>
    /// <param name="recordType">记录类型标识。</param>
    /// <param name="index">记录序号。</param>
    /// <param name="length">记录数据长度。</param>
    /// <param name="isFinal">是否为最后一条记录。</param>
    /// <param name="extra">追加到固定认证数据后的扩展数据。</param>
    /// <returns>写入的字节数。</returns>
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
