using System;
using Bing.Security.Encoding;

namespace Bing.Security.Gm;

/// <summary>
/// 表示 SM4-GCM 认证加密结果。
/// </summary>
public sealed class Sm4GcmPayload
{
    /// <summary>
    /// 当前 SM4-GCM 负载格式版本。
    /// </summary>
    public const byte CurrentVersion = 1;

    /// <summary>
    /// 文本负载允许的最大密文字节长度。
    /// </summary>
    public const int MaximumCiphertextSize = 16 * 1024 * 1024;

    /// <summary>
    /// 由负载独占所有权的 Nonce 字节。
    /// </summary>
    private readonly byte[] _nonce;

    /// <summary>
    /// 由负载独占所有权的密文字节。
    /// </summary>
    private readonly byte[] _ciphertext;

    /// <summary>
    /// 由负载独占所有权的认证标签字节。
    /// </summary>
    private readonly byte[] _tag;

    /// <summary>
    /// 负载格式版本。
    /// </summary>
    public byte Version { get; }

    /// <summary>
    /// SM4-GCM 随机 Nonce 的标准字节长度。
    /// </summary>
    public const int NonceSize = 12;

    /// <summary>
    /// SM4-GCM 认证标签长度，单位为字节。
    /// </summary>
    public const int TagSize = 16;

    /// <summary>
    /// 每次加密唯一的 12 字节 Nonce。
    /// </summary>
    public byte[] Nonce => (byte[])_nonce.Clone();

    /// <summary>
    /// 加密后的密文字节。
    /// </summary>
    public byte[] Ciphertext => (byte[])_ciphertext.Clone();

    /// <summary>
    /// 16 字节 GCM 认证标签。
    /// </summary>
    public byte[] Tag => (byte[])_tag.Clone();

    /// <summary>
    /// 使用已认证的 SM4-GCM 字段初始化载荷。
    /// </summary>
    /// <param name="nonce">12 字节随机 Nonce。</param>
    /// <param name="ciphertext">密文字节。</param>
    /// <param name="tag">16 字节认证标签。</param>
    /// <exception cref="ArgumentException">Nonce 或认证标签长度无效时抛出。</exception>
    public Sm4GcmPayload(byte[] nonce, byte[] ciphertext, byte[] tag) : this(CurrentVersion, nonce, ciphertext, tag)
    {
    }

    /// <summary>
    /// 使用指定格式版本和已认证的 SM4-GCM 字段初始化载荷。
    /// </summary>
    /// <param name="version">负载格式版本。</param>
    /// <param name="nonce">12 字节随机 Nonce。</param>
    /// <param name="ciphertext">密文字节。</param>
    /// <param name="tag">16 字节认证标签。</param>
    /// <exception cref="ArgumentException">版本、Nonce、密文或认证标签无效时抛出。</exception>
    public Sm4GcmPayload(byte version, byte[] nonce, byte[] ciphertext, byte[] tag)
    {
        if (version != CurrentVersion)
            throw new ArgumentException("不支持的 SM4-GCM 负载版本。", nameof(version));
        if (nonce == null || nonce.Length != NonceSize)
            throw new ArgumentException("SM4-GCM Nonce 必须为 12 字节。", nameof(nonce));
        if (ciphertext == null)
            throw new ArgumentNullException(nameof(ciphertext));
        if (ciphertext.Length > MaximumCiphertextSize)
            throw new ArgumentOutOfRangeException(nameof(ciphertext), "SM4-GCM 密文长度不能超过 16 MiB。");
        if (tag == null || tag.Length != TagSize)
            throw new ArgumentException("SM4-GCM 认证标签必须为 16 字节。", nameof(tag));

        Version = version;
        _nonce = (byte[])nonce.Clone();
        _ciphertext = (byte[])ciphertext.Clone();
        _tag = (byte[])tag.Clone();
    }

    /// <summary>
    /// 将负载编码为无填充 Base64Url 文本。
    /// </summary>
    /// <returns>包含 BSM1 魔数、版本、字段长度和密文长度的负载文本。</returns>
    public string Encode()
    {
        var bytes = new byte[11 + _nonce.Length + _ciphertext.Length + _tag.Length];
        try
        {
            bytes[0] = (byte)'B';
            bytes[1] = (byte)'S';
            bytes[2] = (byte)'M';
            bytes[3] = (byte)'1';
            bytes[4] = Version;
            bytes[5] = NonceSize;
            bytes[6] = TagSize;
            WriteUInt32(bytes, 7, (uint)_ciphertext.Length);
            Buffer.BlockCopy(_nonce, 0, bytes, 11, _nonce.Length);
            Buffer.BlockCopy(_ciphertext, 0, bytes, 11 + _nonce.Length, _ciphertext.Length);
            Buffer.BlockCopy(_tag, 0, bytes, 11 + _nonce.Length + _ciphertext.Length, _tag.Length);
            return Base64UrlEncoding.Encode(bytes);
        }
        finally
        {
            GmCryptographicOperationsCompat.ZeroMemory(bytes);
        }
    }

    /// <summary>
    /// 解析无填充 Base64Url SM4-GCM 负载。
    /// </summary>
    /// <param name="value">负载文本。</param>
    /// <returns>已验证格式的 SM4-GCM 负载。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> 为 <c>null</c> 时抛出。</exception>
    /// <exception cref="FormatException">负载格式无效、截断或版本未知时抛出。</exception>
    public static Sm4GcmPayload Parse(string value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        if (!TryParse(value, out var payload))
            throw new FormatException("文本不是有效的 SM4-GCM 负载。");
        return payload;
    }

    /// <summary>
    /// 尝试解析无填充 Base64Url SM4-GCM 负载。
    /// </summary>
    /// <param name="value">负载文本。</param>
    /// <param name="payload">解析成功时返回负载；失败时返回 <c>null</c>。</param>
    /// <returns>负载格式有效时返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    public static bool TryParse(string value, out Sm4GcmPayload payload)
    {
        payload = null;
        if (value == null || value.Length > GetMaximumEncodedLength() || !Base64UrlEncoding.TryDecode(value, out var bytes))
            return false;

        byte[] nonce = null;
        byte[] ciphertext = null;
        byte[] tag = null;
        try
        {
            if (bytes.Length < 11 || bytes[0] != 'B' || bytes[1] != 'S' || bytes[2] != 'M' || bytes[3] != '1' || bytes[4] != CurrentVersion)
                return false;
            var nonceLength = bytes[5];
            var tagLength = bytes[6];
            var ciphertextLength = ReadUInt32(bytes, 7);
            if (nonceLength != NonceSize || tagLength != TagSize || ciphertextLength > MaximumCiphertextSize || bytes.Length != 11L + nonceLength + ciphertextLength + tagLength)
                return false;
            nonce = new byte[nonceLength];
            ciphertext = new byte[(int)ciphertextLength];
            tag = new byte[tagLength];
            Buffer.BlockCopy(bytes, 11, nonce, 0, nonce.Length);
            Buffer.BlockCopy(bytes, 11 + nonce.Length, ciphertext, 0, ciphertext.Length);
            Buffer.BlockCopy(bytes, 11 + nonce.Length + ciphertext.Length, tag, 0, tag.Length);
            payload = new Sm4GcmPayload(CurrentVersion, nonce, ciphertext, tag);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
        finally
        {
            if (nonce != null)
                GmCryptographicOperationsCompat.ZeroMemory(nonce);
            if (ciphertext != null)
                GmCryptographicOperationsCompat.ZeroMemory(ciphertext);
            if (tag != null)
                GmCryptographicOperationsCompat.ZeroMemory(tag);
            GmCryptographicOperationsCompat.ZeroMemory(bytes);
        }
    }

    /// <summary>
    /// 获取最大二进制负载对应的无填充 Base64Url 字符数。
    /// </summary>
    /// <returns>最大允许的文本长度。</returns>
    private static int GetMaximumEncodedLength() => checked((int)(((11L + NonceSize + TagSize + MaximumCiphertextSize) * 4 + 2) / 3));

    /// <summary>
    /// 以大端序写入无符号 32 位整数。
    /// </summary>
    /// <param name="destination">目标字节数组。</param>
    /// <param name="offset">写入偏移量。</param>
    /// <param name="value">要写入的值。</param>
    private static void WriteUInt32(byte[] destination, int offset, uint value)
    {
        destination[offset] = (byte)(value >> 24);
        destination[offset + 1] = (byte)(value >> 16);
        destination[offset + 2] = (byte)(value >> 8);
        destination[offset + 3] = (byte)value;
    }

    /// <summary>
    /// 以大端序读取无符号 32 位整数。
    /// </summary>
    /// <param name="source">源字节数组。</param>
    /// <param name="offset">读取偏移量。</param>
    /// <returns>读取到的数值。</returns>
    private static uint ReadUInt32(byte[] source, int offset) => ((uint)source[offset] << 24) | ((uint)source[offset + 1] << 16) | ((uint)source[offset + 2] << 8) | source[offset + 3];
}