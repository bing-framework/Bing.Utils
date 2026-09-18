#if NET6_0_OR_GREATER
using System.Buffers.Binary;
using Bing.Security.Encoding;

namespace Bing.Security.Cryptography;

/// <summary>
/// 表示由 RSA-OAEP-SHA256 包装密钥和 AES-GCM 数据组成的版本化混合加密载荷。
/// </summary>
public sealed class HybridEncryptedPayload
{
    /// <summary>
    /// RSA 包装密钥允许的最大字节长度。
    /// </summary>
    public const int MaximumEncryptedKeySize = 16384;

    /// <summary>
    /// 混合二进制载荷固定头长度。
    /// </summary>
    private const int HeaderLength = 11;

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
    public ReadOnlyMemory<byte> EncryptedKey => _encryptedKey;

    /// <summary>
    /// 由负载独占所有权的 RSA 加密临时 AES 密钥。
    /// </summary>
    private readonly byte[] _encryptedKey;

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
        if (encryptedKey == null || encryptedKey.Length == 0 || encryptedKey.Length > MaximumEncryptedKeySize)
            throw new ArgumentException("RSA 加密密钥不能为空且长度不能超过 16384 字节。", nameof(encryptedKey));
        if (encryptedData == null)
            throw new ArgumentNullException(nameof(encryptedData));

        Version = version;
        _encryptedKey = encryptedKey.ToArray();
        EncryptedData = encryptedData;
    }

    /// <summary>
    /// 将混合加密载荷编码为无填充 Base64Url 文本。
    /// </summary>
    /// <returns>包含魔数、版本和字段长度的 Base64Url 载荷。</returns>
    public string Encode()
    {
        var encryptedDataBytes = EncryptedData.EncodeBinary();
        try
        {
            checked
            {
            var result = new byte[HeaderLength + _encryptedKey.Length + encryptedDataBytes.Length];
                result[0] = (byte)'B';
                result[1] = (byte)'S';
                result[2] = (byte)'H';
                result[3] = (byte)'1';
                result[4] = Version;
                BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(5, 2), (ushort)_encryptedKey.Length);
                BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(7, 4), (uint)encryptedDataBytes.Length);
                _encryptedKey.CopyTo(result, HeaderLength);
                encryptedDataBytes.CopyTo(result, HeaderLength + _encryptedKey.Length);
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
        if (value == null || value.Length > GetMaximumEncodedLength())
            return false;
        if (!Base64UrlEncoding.TryDecode(value, out var bytes))
            return false;

        byte[] encryptedKey = null;
        try
        {
            if (bytes.Length < HeaderLength || bytes[0] != 'B' || bytes[1] != 'S' || bytes[2] != 'H' || bytes[3] != '1' || bytes[4] != CurrentVersion)
                return false;

            var encryptedKeyLength = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(5, 2));
            var encryptedDataLength = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(7, 4));
            if (encryptedKeyLength == 0 || encryptedKeyLength > MaximumEncryptedKeySize || encryptedDataLength < AesGcmPayload.GetBinaryLength(0) || encryptedDataLength > AesGcmPayload.GetBinaryLength(AesGcmPayload.MaximumCiphertextSize))
                return false;

            var expectedLength = (long)HeaderLength + encryptedKeyLength + encryptedDataLength;
            if (bytes.Length != expectedLength)
                return false;

            encryptedKey = bytes.AsSpan(HeaderLength, encryptedKeyLength).ToArray();
            if (!AesGcmPayload.TryParseBinary(bytes.AsSpan(HeaderLength + encryptedKeyLength, (int)encryptedDataLength), out var encryptedData))
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
    /// 获取允许的最大无填充 Base64Url 文本长度。
    /// </summary>
    /// <returns>最大文本长度。</returns>
    private static int GetMaximumEncodedLength()
    {
        var binaryLength = (long)HeaderLength + MaximumEncryptedKeySize + AesGcmPayload.GetBinaryLength(AesGcmPayload.MaximumCiphertextSize);
        return checked((int)((binaryLength * 4 + 2) / 3));
    }
}
#endif