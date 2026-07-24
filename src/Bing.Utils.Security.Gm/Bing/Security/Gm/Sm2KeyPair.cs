using System;

namespace Bing.Security.Gm;

/// <summary>
/// 表示使用 PKCS#8 和 SubjectPublicKeyInfo PEM 编码的 SM2 密钥对。
/// </summary>
public sealed class Sm2KeyPair
{
    /// <summary>
    /// SubjectPublicKeyInfo PEM 格式的 SM2 公钥。
    /// </summary>
    public string PublicKeyPem { get; }

    /// <summary>
    /// PKCS#8 PEM 格式的 SM2 私钥。
    /// </summary>
    public string PrivateKeyPem { get; }

    /// <summary>
    /// 使用 PEM 编码的 SM2 密钥初始化密钥对。
    /// </summary>
    /// <param name="publicKeyPem">SubjectPublicKeyInfo PEM 公钥。</param>
    /// <param name="privateKeyPem">PKCS#8 PEM 私钥。</param>
    /// <exception cref="ArgumentException">任一 PEM 文本为空时抛出。</exception>
    public Sm2KeyPair(string publicKeyPem, string privateKeyPem)
    {
        if (string.IsNullOrWhiteSpace(publicKeyPem))
            throw new ArgumentException("SM2 公钥 PEM 不能为空。", nameof(publicKeyPem));
        if (string.IsNullOrWhiteSpace(privateKeyPem))
            throw new ArgumentException("SM2 私钥 PEM 不能为空。", nameof(privateKeyPem));

        PublicKeyPem = publicKeyPem;
        PrivateKeyPem = privateKeyPem;
    }
}