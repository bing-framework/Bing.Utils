using System;
using System.IO;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Security;

namespace Bing.Security.Gm;

/// <summary>
/// 提供固定使用 SM2withSM3 和 DER 编码的 SM2 签名操作。
/// </summary>
public static class Sm2Signature
{
    /// <summary>
    /// SM2 ENTL 可表示的最大用户标识字节长度。
    /// </summary>
    public const int MaximumUserIdLength = 8191;

    /// <summary>
    /// GM/T 0003 定义的默认 SM2 用户标识。
    /// </summary>
    private static readonly byte[] DefaultUserIdBytes = System.Text.Encoding.ASCII.GetBytes("1234567812345678");

    /// <summary>
    /// 获取 GM/T 0003 定义的默认 SM2 用户标识副本。
    /// </summary>
    public static byte[] DefaultUserId => (byte[])DefaultUserIdBytes.Clone();

    /// <summary>
    /// 使用 SM2withSM3 对数据生成 DER 编码签名。
    /// </summary>
    /// <param name="data">要签名的原始数据。</param>
    /// <param name="privateKeyPem">PKCS#8 PEM 格式的 sm2p256v1 私钥。</param>
    /// <param name="userId">可选 SM2 用户标识，未指定时使用 GM/T 默认值。</param>
    /// <returns>DER 编码的 SM2 签名。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> 为 <c>null</c> 时抛出。</exception>
    /// <exception cref="ArgumentException">私钥 PEM 无效或用户标识为空时抛出。</exception>
    public static byte[] Sign(byte[] data, string privateKeyPem, byte[] userId = null)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        var normalizedUserId = NormalizeUserId(userId);
        try
        {
            var signer = new SM2Signer();
            signer.Init(true, new ParametersWithID(new ParametersWithRandom(Sm2KeySerializer.ReadPrivateKey(privateKeyPem), new SecureRandom()), normalizedUserId));
            signer.BlockUpdate(data, 0, data.Length);
            return signer.GenerateSignature();
        }
        finally
        {
            GmCryptographicOperationsCompat.ZeroMemory(normalizedUserId);
        }
    }

    /// <summary>
    /// 验证 DER 编码的 SM2withSM3 签名。
    /// </summary>
    /// <param name="data">签名时的原始数据。</param>
    /// <param name="signature">DER 编码的 SM2 签名。</param>
    /// <param name="publicKeyPem">SubjectPublicKeyInfo PEM 格式的 sm2p256v1 公钥。</param>
    /// <param name="userId">签名时使用的 SM2 用户标识，未指定时使用 GM/T 默认值。</param>
    /// <returns>签名有效时返回 <c>true</c>；签名格式或认证不匹配时返回 <c>false</c>。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> 或 <paramref name="signature"/> 为 <c>null</c> 时抛出。</exception>
    /// <exception cref="ArgumentException">公钥 PEM 无效或用户标识为空时抛出。</exception>
    public static bool Verify(byte[] data, byte[] signature, string publicKeyPem, byte[] userId = null)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));
        if (signature == null)
            throw new ArgumentNullException(nameof(signature));

        var normalizedUserId = NormalizeUserId(userId);
        try
        {
            var signer = new SM2Signer();
            signer.Init(false, new ParametersWithID(Sm2KeySerializer.ReadPublicKey(publicKeyPem), normalizedUserId));
            signer.BlockUpdate(data, 0, data.Length);
            return signer.VerifySignature(signature);
        }
        catch (InvalidCipherTextException)
        {
            return false;
        }
        finally
        {
            GmCryptographicOperationsCompat.ZeroMemory(normalizedUserId);
        }
    }

    /// <summary>
    /// 复制用户标识并拒绝空标识。
    /// </summary>
    /// <param name="userId">调用方提供的用户标识。</param>
    /// <returns>用于本次签名或验证的独立用户标识字节。</returns>
    /// <exception cref="ArgumentException"><paramref name="userId"/> 为空时抛出。</exception>
    private static byte[] NormalizeUserId(byte[] userId)
    {
        if (userId == null)
            return (byte[])DefaultUserIdBytes.Clone();
        if (userId.Length == 0 || userId.Length > MaximumUserIdLength)
            throw new ArgumentException("SM2 用户标识长度必须介于 1 和 8191 字节之间。", nameof(userId));
        return (byte[])userId.Clone();
    }
}