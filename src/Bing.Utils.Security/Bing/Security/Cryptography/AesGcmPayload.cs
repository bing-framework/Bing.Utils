#if !NETSTANDARD2_0
using Bing.Security.Encoding;

namespace Bing.Security.Cryptography;

/// <summary>
/// 表示版本化 AES-GCM 认证加密载荷。
/// </summary>
public sealed class AesGcmPayload
{
    /// <summary>
    /// 当前 AES-GCM 载荷格式版本。
    /// </summary>
    public const byte CurrentVersion = 1;

    /// <summary>
    /// AES-GCM Nonce 的标准字节长度。
    /// </summary>
    public const int NonceSize = 12;

    /// <summary>
    /// AES-GCM 认证标签的标准字节长度。
    /// </summary>
    public const int TagSize = 16;

    /// <summary>
    /// 载荷格式版本。
    /// </summary>
    public byte Version { get; }

    /// <summary>
    /// 每次加密唯一的随机 Nonce。
    /// </summary>
    public byte[] Nonce { get; }

    /// <summary>
    /// 已认证的密文字节。
    /// </summary>
    public byte[] Ciphertext { get; }

    /// <summary>
    /// GCM 认证标签。
    /// </summary>
    public byte[] Tag { get; }

    /// <summary>
    /// 使用当前格式版本初始化认证加密载荷。
    /// </summary>
    /// <param name="nonce">12 字节随机 Nonce。</param>
    /// <param name="ciphertext">密文字节。</param>
    /// <param name="tag">16 字节认证标签。</param>
    public AesGcmPayload(byte[] nonce, byte[] ciphertext, byte[] tag) : this(CurrentVersion, nonce, ciphertext, tag)
    {
    }

    /// <summary>
    /// 使用指定格式版本初始化认证加密载荷。
    /// </summary>
    /// <param name="version">载荷格式版本。</param>
    /// <param name="nonce">12 字节随机 Nonce。</param>
    /// <param name="ciphertext">密文字节。</param>
    /// <param name="tag">16 字节认证标签。</param>
    /// <exception cref="ArgumentException">字段长度不符合格式约束时抛出。</exception>
    public AesGcmPayload(byte version, byte[] nonce, byte[] ciphertext, byte[] tag)
    {
        if (version != CurrentVersion)
            throw new ArgumentException("不支持的 AES-GCM 载荷版本。", nameof(version));
        if (nonce == null || nonce.Length != NonceSize)
            throw new ArgumentException("AES-GCM Nonce 必须为 12 字节。", nameof(nonce));
        if (ciphertext == null)
            throw new ArgumentNullException(nameof(ciphertext));
        if (tag == null || tag.Length != TagSize)
            throw new ArgumentException("AES-GCM 认证标签必须为 16 字节。", nameof(tag));

        Version = version;
        Nonce = nonce.ToArray();
        Ciphertext = ciphertext.ToArray();
        Tag = tag.ToArray();
    }

    /// <summary>
    /// 将载荷编码为无填充 Base64Url 字符串。
    /// </summary>
    /// <returns>包含魔数、版本、字段长度和密文长度的 Base64Url 载荷。</returns>
    public string Encode()
    {
        checked
        {
            var result = new byte[11 + Nonce.Length + Tag.Length + Ciphertext.Length];
            result[0] = (byte)'B';
            result[1] = (byte)'S';
            result[2] = (byte)'P';
            result[3] = (byte)'1';
            result[4] = Version;
            result[5] = (byte)Nonce.Length;
            result[6] = (byte)Tag.Length;
            WriteUInt32(result.AsSpan(7, 4), (uint)Ciphertext.Length);
            Nonce.CopyTo(result, 11);
            Ciphertext.CopyTo(result, 11 + Nonce.Length);
            Tag.CopyTo(result, 11 + Nonce.Length + Ciphertext.Length);
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

    /// <summary>
    /// 解析无填充 Base64Url AES-GCM 载荷。
    /// </summary>
    /// <param name="value">载荷文本。</param>
    /// <returns>已验证格式的 AES-GCM 载荷。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> 为 <c>null</c> 时抛出。</exception>
    /// <exception cref="FormatException">载荷格式无效、截断或包含未知版本时抛出。</exception>
    public static AesGcmPayload Parse(string value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        if (!TryParse(value, out var payload))
            throw new FormatException("文本不是有效的 AES-GCM 载荷。");
        return payload;
    }

    /// <summary>
    /// 尝试解析无填充 Base64Url AES-GCM 载荷。
    /// </summary>
    /// <param name="value">载荷文本。</param>
    /// <param name="payload">解析成功时返回载荷；失败时返回 <c>null</c>。</param>
    /// <returns>载荷格式有效时返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    public static bool TryParse(string value, out AesGcmPayload payload)
    {
        payload = null;
        if (!Base64UrlEncoding.TryDecode(value, out var bytes))
            return false;

        try
        {
            if (bytes.Length < 11 || bytes[0] != 'B' || bytes[1] != 'S' || bytes[2] != 'P' || bytes[3] != '1' || bytes[4] != CurrentVersion)
                return false;

            var nonceLength = bytes[5];
            var tagLength = bytes[6];
            var ciphertextLength = ReadUInt32(bytes.AsSpan(7, 4));
            if (nonceLength != NonceSize || tagLength != TagSize || ciphertextLength > int.MaxValue)
                return false;

            var expectedLength = 11L + nonceLength + tagLength + ciphertextLength;
            if (bytes.Length != expectedLength)
                return false;

            var nonce = bytes.AsSpan(11, nonceLength).ToArray();
            var ciphertext = bytes.AsSpan(11 + nonceLength, (int)ciphertextLength).ToArray();
            var tag = bytes.AsSpan(11 + nonceLength + (int)ciphertextLength, tagLength).ToArray();
            payload = new AesGcmPayload(CurrentVersion, nonce, ciphertext, tag);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
        finally
        {
            CryptographicOperationsCompat.ZeroMemory(bytes);
        }
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