using System;
using System.IO;
using System.Text;
using Org.BouncyCastle.Asn1.GM;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

namespace Bing.Security.Gm;

/// <summary>
/// 提供 SM2 密钥、加密和签名操作，PEM 接口不暴露第三方密码学类型。
/// </summary>
public static class Sm2
{
    /// <summary>
    /// GM/T 0003 定义的内部默认 SM2 用户标识，避免调用方修改共享数组。
    /// </summary>
    private static readonly byte[] DefaultUserIdBytes = System.Text.Encoding.ASCII.GetBytes("1234567812345678");

    /// <summary>
    /// 获取 GM/T 0003 定义的默认 SM2 用户标识副本。
    /// </summary>
    public static byte[] DefaultUserId => (byte[])DefaultUserIdBytes.Clone();

    /// <summary>
    /// 生成使用 sm2p256v1 曲线的 SM2 PEM 密钥对。
    /// </summary>
    /// <returns>含 PKCS#8 私钥和 SubjectPublicKeyInfo 公钥 PEM 的密钥对。</returns>
    public static Sm2KeyPair GenerateKeyPair()
    {
        var parameters = GMNamedCurves.GetByName("sm2p256v1");
        var domain = new ECDomainParameters(parameters.Curve, parameters.G, parameters.N, parameters.H, parameters.GetSeed());
        var generator = new ECKeyPairGenerator();
        generator.Init(new ECKeyGenerationParameters(domain, new SecureRandom()));
        var pair = generator.GenerateKeyPair();
        return new Sm2KeyPair(WritePem(pair.Public), WritePem(pair.Private));
    }

    /// <summary>
    /// 使用 SM2 C1C3C2 编码加密数据。
    /// </summary>
    /// <param name="plaintext">要加密的明文。</param>
    /// <param name="publicKeyPem">SubjectPublicKeyInfo PEM 格式 SM2 公钥。</param>
    /// <returns>SM2 C1C3C2 格式密文字节。</returns>
    public static byte[] Encrypt(byte[] plaintext, string publicKeyPem)
    {
        if (plaintext == null)
            throw new ArgumentNullException(nameof(plaintext));
        var publicKey = ReadPublicKey(publicKeyPem);
        var engine = new SM2Engine(SM2Engine.Mode.C1C3C2);
        engine.Init(true, new ParametersWithRandom(publicKey, new SecureRandom()));
        return engine.ProcessBlock(plaintext, 0, plaintext.Length);
    }

    /// <summary>
    /// 解密 SM2 C1C3C2 格式密文。
    /// </summary>
    /// <param name="ciphertext">SM2 C1C3C2 格式密文。</param>
    /// <param name="privateKeyPem">PKCS#8 PEM 格式 SM2 私钥。</param>
    /// <returns>认证成功后的明文。</returns>
    /// <exception cref="System.Security.Cryptography.CryptographicException">私钥或密文无效时抛出。</exception>
    public static byte[] Decrypt(byte[] ciphertext, string privateKeyPem)
    {
        if (ciphertext == null)
            throw new ArgumentNullException(nameof(ciphertext));
        try
        {
            var engine = new SM2Engine(SM2Engine.Mode.C1C3C2);
            engine.Init(false, ReadPrivateKey(privateKeyPem));
            return engine.ProcessBlock(ciphertext, 0, ciphertext.Length);
        }
        catch (InvalidCipherTextException exception)
        {
            throw new System.Security.Cryptography.CryptographicException("SM2 密文验证失败。", exception);
        }
    }

    /// <summary>
    /// 使用 SM2withSM3 对数据生成 DER 编码签名。
    /// </summary>
    /// <param name="data">要签名的数据。</param>
    /// <param name="privateKeyPem">PKCS#8 PEM 格式 SM2 私钥。</param>
    /// <param name="userId">可选 SM2 用户标识，未指定时使用 GM/T 默认值。</param>
    /// <returns>DER 编码的 SM2 签名。</returns>
    public static byte[] Sign(byte[] data, string privateKeyPem, byte[] userId = null)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));
        var signer = new SM2Signer();
        signer.Init(true, new ParametersWithID(new ParametersWithRandom(ReadPrivateKey(privateKeyPem), new SecureRandom()), NormalizeUserId(userId)));
        signer.BlockUpdate(data, 0, data.Length);
        return signer.GenerateSignature();
    }

    /// <summary>
    /// 验证 DER 编码的 SM2withSM3 签名。
    /// </summary>
    /// <param name="data">原始数据。</param>
    /// <param name="signature">DER 编码 SM2 签名。</param>
    /// <param name="publicKeyPem">SubjectPublicKeyInfo PEM 格式 SM2 公钥。</param>
    /// <param name="userId">签名时使用的 SM2 用户标识，未指定时使用 GM/T 默认值。</param>
    /// <returns>签名有效时返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    public static bool Verify(byte[] data, byte[] signature, string publicKeyPem, byte[] userId = null)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));
        if (signature == null)
            throw new ArgumentNullException(nameof(signature));
        try
        {
            var signer = new SM2Signer();
            signer.Init(false, new ParametersWithID(ReadPublicKey(publicKeyPem), NormalizeUserId(userId)));
            signer.BlockUpdate(data, 0, data.Length);
            return signer.VerifySignature(signature);
        }
        catch (InvalidCipherTextException)
        {
            return false;
        }
    }

    /// <summary>
    /// 解析 SM2 公钥 PEM。
    /// </summary>
    /// <param name="pem">SubjectPublicKeyInfo PEM 文本。</param>
    /// <returns>仅限内部使用的 SM2 公钥参数。</returns>
    private static ECPublicKeyParameters ReadPublicKey(string pem)
    {
        var key = ReadPemKey(pem);
        if (key is ECPublicKeyParameters publicKey)
            return publicKey;
        throw new ArgumentException("PEM 文本不包含 SM2 公钥。", nameof(pem));
    }

    /// <summary>
    /// 解析 SM2 私钥 PEM。
    /// </summary>
    /// <param name="pem">PKCS#8 PEM 文本。</param>
    /// <returns>仅限内部使用的 SM2 私钥参数。</returns>
    private static ECPrivateKeyParameters ReadPrivateKey(string pem)
    {
        var key = ReadPemKey(pem);
        if (key is ECPrivateKeyParameters privateKey)
            return privateKey;
        throw new ArgumentException("PEM 文本不包含 SM2 私钥。", nameof(pem));
    }

    /// <summary>
    /// 使用 BouncyCastle PEM 阅读器解析密钥，隔离第三方类型。
    /// </summary>
    /// <param name="pem">PEM 文本。</param>
    /// <returns>密钥参数。</returns>
    private static AsymmetricKeyParameter ReadPemKey(string pem)
    {
        if (string.IsNullOrWhiteSpace(pem))
            throw new ArgumentException("PEM 文本不能为空。", nameof(pem));
        var reader = new PemReader(new StringReader(pem));
        var value = reader.ReadObject();
        if (value is AsymmetricKeyParameter key)
            return key;
        if (value is AsymmetricCipherKeyPair pair)
            return pair.Private;
        throw new ArgumentException("PEM 文本不包含可用密钥。", nameof(pem));
    }

    /// <summary>
    /// 将 BouncyCastle 密钥写为标准 PEM。
    /// </summary>
    /// <param name="key">要编码的密钥参数。</param>
    /// <returns>PEM 文本。</returns>
    private static string WritePem(AsymmetricKeyParameter key)
    {
        using var writer = new StringWriter();
        var pemWriter = new PemWriter(writer);
        pemWriter.WriteObject(key);
        pemWriter.Writer.Flush();
        return writer.ToString();
    }

    /// <summary>
    /// 规范化用户标识并拒绝空标识。
    /// </summary>
    /// <param name="userId">调用方提供的用户标识。</param>
    /// <returns>用于 SM2 签名的用户标识。</returns>
    private static byte[] NormalizeUserId(byte[] userId)
    {
        if (userId == null)
            return (byte[])DefaultUserIdBytes.Clone();
        if (userId.Length == 0)
            throw new ArgumentException("SM2 用户标识不能为空。", nameof(userId));
        return (byte[])userId.Clone();
    }
}