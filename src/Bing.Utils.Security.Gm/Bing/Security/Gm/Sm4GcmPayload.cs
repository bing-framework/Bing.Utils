using System;

namespace Bing.Security.Gm;

/// <summary>
/// 表示 SM4-GCM 认证加密结果。
/// </summary>
public sealed class Sm4GcmPayload
{
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
    public byte[] Nonce { get; }

    /// <summary>
    /// 加密后的密文字节。
    /// </summary>
    public byte[] Ciphertext { get; }

    /// <summary>
    /// 16 字节 GCM 认证标签。
    /// </summary>
    public byte[] Tag { get; }

    /// <summary>
    /// 使用已认证的 SM4-GCM 字段初始化载荷。
    /// </summary>
    /// <param name="nonce">12 字节随机 Nonce。</param>
    /// <param name="ciphertext">密文字节。</param>
    /// <param name="tag">16 字节认证标签。</param>
    /// <exception cref="ArgumentException">Nonce 或认证标签长度无效时抛出。</exception>
    public Sm4GcmPayload(byte[] nonce, byte[] ciphertext, byte[] tag)
    {
        if (nonce == null || nonce.Length != NonceSize)
            throw new ArgumentException("SM4-GCM Nonce 必须为 12 字节。", nameof(nonce));
        if (ciphertext == null)
            throw new ArgumentNullException(nameof(ciphertext));
        if (tag == null || tag.Length != TagSize)
            throw new ArgumentException("SM4-GCM 认证标签必须为 16 字节。", nameof(tag));

        Nonce = (byte[])nonce.Clone();
        Ciphertext = (byte[])ciphertext.Clone();
        Tag = (byte[])tag.Clone();
    }
}