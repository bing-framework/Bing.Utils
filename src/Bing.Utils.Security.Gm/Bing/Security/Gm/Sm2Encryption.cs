using System;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace Bing.Security.Gm;

/// <summary>
/// 提供固定使用 C1C3C2 编码的 SM2 加密和解密操作。
/// </summary>
public static class Sm2Encryption
{
    /// <summary>
    /// 使用 SM2 C1C3C2 编码加密数据。
    /// </summary>
    /// <param name="plaintext">要加密的明文字节。</param>
    /// <param name="publicKeyPem">SubjectPublicKeyInfo PEM 格式的 sm2p256v1 公钥。</param>
    /// <returns>SM2 C1C3C2 格式密文字节。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="plaintext"/> 为 <c>null</c> 时抛出。</exception>
    /// <exception cref="ArgumentException">公钥 PEM 格式、类型或曲线无效时抛出。</exception>
    public static byte[] Encrypt(byte[] plaintext, string publicKeyPem)
    {
        if (plaintext == null)
            throw new ArgumentNullException(nameof(plaintext));

        var engine = new SM2Engine(SM2Engine.Mode.C1C3C2);
        engine.Init(true, new ParametersWithRandom(Sm2KeySerializer.ReadPublicKey(publicKeyPem), new SecureRandom()));
        return engine.ProcessBlock(plaintext, 0, plaintext.Length);
    }

    /// <summary>
    /// 验证 SM2 C1C3C2 密文后解密数据。
    /// </summary>
    /// <param name="ciphertext">SM2 C1C3C2 格式密文字节。</param>
    /// <param name="privateKeyPem">PKCS#8 PEM 格式的 sm2p256v1 私钥。</param>
    /// <returns>认证成功后的明文字节。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="ciphertext"/> 为 <c>null</c> 时抛出。</exception>
    /// <exception cref="ArgumentException">私钥 PEM 格式、类型或曲线无效时抛出。</exception>
    /// <exception cref="CryptographicException">密文验证失败时抛出。</exception>
    public static byte[] Decrypt(byte[] ciphertext, string privateKeyPem)
    {
        if (ciphertext == null)
            throw new ArgumentNullException(nameof(ciphertext));

        try
        {
            var engine = new SM2Engine(SM2Engine.Mode.C1C3C2);
            engine.Init(false, Sm2KeySerializer.ReadPrivateKey(privateKeyPem));
            return engine.ProcessBlock(ciphertext, 0, ciphertext.Length);
        }
        catch (InvalidCipherTextException exception)
        {
            throw new CryptographicException("SM2 密文验证失败。", exception);
        }
    }
}