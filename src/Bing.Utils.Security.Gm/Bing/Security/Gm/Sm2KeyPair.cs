using System;

namespace Bing.Security.Gm;

/// <summary>
/// 表示使用 PKCS#8 和 SubjectPublicKeyInfo PEM 编码的 SM2 密钥对。
/// </summary>
public sealed class Sm2KeyPair
{
    /// <summary>
    /// 获取SubjectPublicKeyInfo PEM 格式的 SM2 公钥。
    /// </summary>
    public string PublicKeyPem { get; }

    /// <summary>
    /// 获取PKCS#8 PEM 格式的 SM2 私钥。
    /// </summary>
    public string PrivateKeyPem { get; }

    /// <summary>
    /// 初始化一个 <see cref="Sm2KeyPair"/> 类型的实例。
    /// </summary>
    /// <param name="publicKeyPem">SubjectPublicKeyInfo PEM 公钥。</param>
    /// <param name="privateKeyPem">PKCS#8 PEM 私钥。</param>
    /// <exception cref="ArgumentException">任一 PEM 文本为空时抛出。</exception>
    internal Sm2KeyPair(string publicKeyPem, string privateKeyPem)
    {
        if (string.IsNullOrWhiteSpace(publicKeyPem))
            throw new ArgumentException("SM2 公钥 PEM 不能为空。", nameof(publicKeyPem));
        if (string.IsNullOrWhiteSpace(privateKeyPem))
            throw new ArgumentException("SM2 私钥 PEM 不能为空。", nameof(privateKeyPem));

        PublicKeyPem = publicKeyPem;
        PrivateKeyPem = privateKeyPem;
    }
}
