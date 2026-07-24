#if NET6_0_OR_GREATER
using System.Text;
using Bing.Security.Encoding;

namespace Bing.Security.Cryptography;

/// <summary>
/// 表示由 RSA-OAEP-SHA256 包装密钥和 AES-GCM 数据组成的版本化混合加密载荷。
/// </summary>
public sealed class HybridEncryptedPayload
{
    /// <summary>
    /// 当前混合加密载荷格式版本。
    /// </summary>
    public const byte CurrentVersion = 1;

    /// <summary>
    /// 载荷格式版本。
    /// </summary>
    public byte Version { get; }

    /// <summary>
    /// 使用 RSA-OAEP-SHA256 加密的临时 AES 密钥。
    /// </summary>
    public byte[] EncryptedKey { get; }

    /// <summary>
    /// 使用 AES-GCM 加密的数据载荷。
    /// </summary>
    public AesGcmPayload EncryptedData { get; }

    /// <summary>
    /// 使用当前版本初始化混合加密载荷。
    /// </summary>
    /// <param name="encryptedKey">RSA 加密的临时 AES 密钥。</param>
    /// <param name="encryptedData">AES-GCM 数据载荷。</param>
    public HybridEncryptedPayload(byte[] encryptedKey, AesGcmPayload encryptedData) : this(CurrentVersion, encryptedKey, encryptedData)
    {
    }

    /// <summary>
    /// 使用指定版本初始化混合加密载荷。
    /// </summary>
    /// <param name="version">载荷格式版本。</param>
    /// <param name="encryptedKey">RSA 加密的临时 AES 密钥。</param>
    /// <param name="encryptedData">AES-GCM 数据载荷。</param>
    /// <exception cref="ArgumentException">版本或加密密钥无效时抛出。</exception>
    /// <exception cref="ArgumentNullException"><paramref name="encryptedData"/> 为 <c>null</c> 时抛出。</exception>
    public HybridEncryptedPayload(byte version, byte[] encryptedKey, AesGcmPayload encryptedData)
    {
        if (version != CurrentVersion)
            throw new ArgumentException("不支持的混合加密载荷版本。", nameof(version));
        if (encryptedKey == null || encryptedKey.Length == 0 || encryptedKey.Length > ushort.MaxValue)
            throw new ArgumentException("RSA 加密密钥不能为空且长度不能超过 65535 字节。", nameof(encryptedKey));
        if (encryptedData == null)
            throw new ArgumentNullException(nameof(encryptedData));

        Version = version;
        EncryptedKey = encryptedKey.ToArray();
        EncryptedData = encryptedData;
    }

    /// <summary>
    /// 将混合加密载荷编码为无填充 Base64Url 文本。
    /// </summary>
    /// <returns>包含魔数、版本和字段长度的 Base64Url 载荷。</returns>
    public string Encode()
    {
        var encryptedDataText = EncryptedData.Encode();
        var encryptedDataBytes = System.Text.Encoding.ASCII.GetBytes(encryptedDataText);
        try
        {
            checked
            {
                var result = new byte[11 + EncryptedKey.Length + encryptedDataBytes.Length];
                result[0] = (byte)'B';
                result[1] = (byte)'S';
                result[2] = (byte)'H';
                result[3] = (byte)'1';
                result[4] = Version;
                WriteUInt16(result.AsSpan(5, 2), (ushort)EncryptedKey.Length);
                WriteUInt32(result.AsSpan(7, 4), (uint)encryptedDataBytes.Length);
                EncryptedKey.CopyTo(result, 11);
                encryptedDataBytes.CopyTo(result, 11 + EncryptedKey.Length);
                try
                {
                    return Base64UrlEncoding.Encode(result);
                }
                finally
                {
                    CryptographicOperationsCompat.ZeroMemory(result);
                }
            }
        }
        finally
        {
            CryptographicOperationsCompat.ZeroMemory(encryptedDataBytes);
        }
    }

    /// <summary>
    /// 解析无填充 Base64Url 混合加密载荷。
    /// </summary>
    /// <param name="value">载荷文本。</param>
    /// <returns>格式有效的混合加密载荷。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> 为 <c>null</c> 时抛出。</exception>
    /// <exception cref="FormatException">载荷格式无效、截断或包含未知版本时抛出。</exception>
    public static HybridEncryptedPayload Parse(string value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        if (!TryParse(value, out var payload))
            throw new FormatException("文本不是有效的混合加密载荷。");
        return payload;
    }

    /// <summary>
    /// 尝试解析无填充 Base64Url 混合加密载荷。
    /// </summary>
    /// <param name="value">载荷文本。</param>
    /// <param name="payload">解析成功时返回载荷；失败时返回 <c>null</c>。</param>
    /// <returns>载荷格式有效时返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    public static bool TryParse(string value, out HybridEncryptedPayload payload)
    {
        payload = null;
        if (!Base64UrlEncoding.TryDecode(value, out var bytes))
            return false;

        byte[] encryptedKey = null;
        try
        {
            if (bytes.Length < 12 || bytes[0] != 'B' || bytes[1] != 'S' || bytes[2] != 'H' || bytes[3] != '1' || bytes[4] != CurrentVersion)
                return false;

            var encryptedKeyLength = ReadUInt16(bytes.AsSpan(5, 2));
            var encryptedDataLength = ReadUInt32(bytes.AsSpan(7, 4));
            if (encryptedKeyLength == 0 || encryptedDataLength == 0 || encryptedDataLength > int.MaxValue)
                return false;

            var expectedLength = 11L + encryptedKeyLength + encryptedDataLength;
            if (bytes.Length != expectedLength)
                return false;

            encryptedKey = bytes.AsSpan(11, encryptedKeyLength).ToArray();
            var encryptedDataText = System.Text.Encoding.ASCII.GetString(bytes, 11 + encryptedKeyLength, (int)encryptedDataLength);
            if (!AesGcmPayload.TryParse(encryptedDataText, out var encryptedData))
                return false;

            payload = new HybridEncryptedPayload(CurrentVersion, encryptedKey, encryptedData);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
        finally
        {
            if (encryptedKey != null)
                CryptographicOperationsCompat.ZeroMemory(encryptedKey);
            CryptographicOperationsCompat.ZeroMemory(bytes);
        }
    }

    /// <summary>
    /// 以大端序写入无符号 16 位整数。
    /// </summary>
    /// <param name="destination">两字节目标缓冲区。</param>
    /// <param name="value">要写入的数值。</param>
    private static void WriteUInt16(Span<byte> destination, ushort value)
    {
        destination[0] = (byte)(value >> 8);
        destination[1] = (byte)value;
    }

    /// <summary>
    /// 以大端序写入无符号 32 位整数。
    /// </summary>
    /// <param name="destination">四字节目标缓冲区。</param>
    /// <param name="value">要写入的数值。</param>
    private static void WriteUInt32(Span<byte> destination, uint value)
    {
        destination[0] = (byte)(value >> 24);
        destination[1] = (byte)(value >> 16);
        destination[2] = (byte)(value >> 8);
        destination[3] = (byte)value;
    }

    /// <summary>
    /// 以大端序读取无符号 16 位整数。
    /// </summary>
    /// <param name="source">两字节源缓冲区。</param>
    /// <returns>读取到的数值。</returns>
    private static ushort ReadUInt16(ReadOnlySpan<byte> source)
    {
        return (ushort)((source[0] << 8) | source[1]);
    }

    /// <summary>
    /// 以大端序读取无符号 32 位整数。
    /// </summary>
    /// <param name="source">四字节源缓冲区。</param>
    /// <returns>读取到的数值。</returns>
    private static uint ReadUInt32(ReadOnlySpan<byte> source)
    {
        return ((uint)source[0] << 24) | ((uint)source[1] << 16) | ((uint)source[2] << 8) | source[3];
    }
}
#endif