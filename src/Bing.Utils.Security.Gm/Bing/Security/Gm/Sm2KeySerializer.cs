using System;
using System.IO;
using Org.BouncyCastle.Asn1.GM;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Utilities.IO.Pem;
using Org.BouncyCastle.X509;

namespace Bing.Security.Gm;

/// <summary>
/// 提供仅限 SM2 内部组件使用的 sm2p256v1 PEM 编码和验证操作。
/// </summary>
internal static class Sm2KeySerializer
{
    /// <summary>
    /// 固定 sm2p256v1 命名曲线域参数。
    /// </summary>
    internal static readonly ECDomainParameters Domain = CreateDomain();

    /// <summary>
    /// 读取并验证单个 SubjectPublicKeyInfo SM2 公钥 PEM。
    /// </summary>
    /// <param name="pem">公钥 PEM 文本。</param>
    /// <returns>已验证的 sm2p256v1 公钥参数。</returns>
    /// <exception cref="ArgumentException">PEM 格式、对象类型、曲线或公钥点无效时抛出。</exception>
    internal static ECPublicKeyParameters ReadPublicKey(string pem)
    {
        var key = ReadPemKey(pem, "PUBLIC KEY");
        if (!(key is ECPublicKeyParameters publicKey))
            throw new ArgumentException("PEM 文本不包含 SM2 公钥。", nameof(pem));
        ValidatePublicKey(publicKey, nameof(pem));
        return publicKey;
    }

    /// <summary>
    /// 读取并验证单个 PKCS#8 SM2 私钥 PEM。
    /// </summary>
    /// <param name="pem">私钥 PEM 文本。</param>
    /// <returns>已验证的 sm2p256v1 私钥参数。</returns>
    /// <exception cref="ArgumentException">PEM 格式、对象类型、曲线或私钥标量无效时抛出。</exception>
    internal static ECPrivateKeyParameters ReadPrivateKey(string pem)
    {
        var key = ReadPemKey(pem, "PRIVATE KEY");
        if (!(key is ECPrivateKeyParameters privateKey))
            throw new ArgumentException("PEM 文本不包含 SM2 私钥。", nameof(pem));
        ValidatePrivateKey(privateKey, nameof(pem));
        return privateKey;
    }

    /// <summary>
    /// 将 sm2p256v1 公钥写为 SubjectPublicKeyInfo PEM。
    /// </summary>
    /// <param name="key">需要编码的 SM2 公钥。</param>
    /// <returns>单个 <c>PUBLIC KEY</c> PEM 对象。</returns>
    internal static string WritePublicKey(ECPublicKeyParameters key)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));
        ValidatePublicKey(key, nameof(key));
        return WritePem("PUBLIC KEY", SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(key).GetEncoded());
    }

    /// <summary>
    /// 将 sm2p256v1 私钥写为 PKCS#8 PEM。
    /// </summary>
    /// <param name="key">需要编码的 SM2 私钥。</param>
    /// <returns>单个 <c>PRIVATE KEY</c> PEM 对象。</returns>
    internal static string WritePrivateKey(ECPrivateKeyParameters key)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));
        ValidatePrivateKey(key, nameof(key));
        return WritePem("PRIVATE KEY", PrivateKeyInfoFactory.CreatePrivateKeyInfo(key).GetEncoded());
    }

    /// <summary>
    /// 创建具有 sm2p256v1 OID 的命名曲线域参数。
    /// </summary>
    /// <returns>固定使用的 sm2p256v1 域参数。</returns>
    private static ECDomainParameters CreateDomain()
    {
        var parameters = GMNamedCurves.GetByName("sm2p256v1");
        if (parameters == null)
            throw new InvalidOperationException("当前密码学提供程序不支持 sm2p256v1 曲线。 ");
        return new ECNamedDomainParameters(GMObjectIdentifiers.sm2p256v1, parameters.Curve, parameters.G, parameters.N, parameters.H, parameters.GetSeed());
    }

    /// <summary>
    /// 读取只包含一个指定类型对象的 PEM 密钥。
    /// </summary>
    /// <param name="pem">PEM 文本。</param>
    /// <param name="expectedType">要求的 PEM 对象标签。</param>
    /// <returns>已读取的非对称密钥参数。</returns>
    /// <exception cref="ArgumentException">PEM 文本不符合单对象约束或不包含密钥时抛出。</exception>
    private static AsymmetricKeyParameter ReadPemKey(string pem, string expectedType)
    {
        if (string.IsNullOrWhiteSpace(pem))
            throw new ArgumentException("PEM 文本不能为空。", nameof(pem));

        var normalized = pem.Trim();
        if (!normalized.StartsWith("-----BEGIN " + expectedType + "-----", StringComparison.Ordinal) ||
            !normalized.EndsWith("-----END " + expectedType + "-----", StringComparison.Ordinal) ||
            CountOccurrences(normalized, "-----BEGIN ") != 1 ||
            CountOccurrences(normalized, "-----END ") != 1)
            throw new ArgumentException("PEM 文本必须且只能包含一个 " + expectedType + " 对象。", nameof(pem));

        try
        {
            var reader = new Org.BouncyCastle.OpenSsl.PemReader(new StringReader(pem));
            var value = reader.ReadObject();
            if (reader.ReadObject() != null)
                throw new ArgumentException("PEM 文本只能包含一个密钥对象。", nameof(pem));
            if (value is AsymmetricKeyParameter key)
                return key;
            throw new ArgumentException("PEM 文本不包含可用密钥。", nameof(pem));
        }
        catch (Exception exception) when (exception is IOException || exception is FormatException)
        {
            throw new ArgumentException("PEM 文本格式无效。", nameof(pem), exception);
        }
    }

    /// <summary>
    /// 将 DER 字节写为单个 PEM 对象。
    /// </summary>
    /// <param name="type">PEM 对象标签。</param>
    /// <param name="encoded">DER 编码字节。</param>
    /// <returns>PEM 文本。</returns>
    private static string WritePem(string type, byte[] encoded)
    {
        using var writer = new StringWriter();
        var pemWriter = new Org.BouncyCastle.OpenSsl.PemWriter(writer);
        pemWriter.WriteObject(new PemObject(type, encoded));
        pemWriter.Writer.Flush();
        return writer.ToString();
    }

    /// <summary>
    /// 验证公钥属于 sm2p256v1 且椭圆曲线点有效。
    /// </summary>
    /// <param name="key">待验证公钥。</param>
    /// <param name="parameterName">异常中使用的参数名。</param>
    /// <exception cref="ArgumentException">曲线或点无效时抛出。</exception>
    private static void ValidatePublicKey(ECPublicKeyParameters key, string parameterName)
    {
        if (!IsSm2Domain(key.Parameters) || key.Q == null || key.Q.IsInfinity || !key.Q.IsValid())
            throw new ArgumentException("PEM 公钥必须是 sm2p256v1 曲线上的有效点。", parameterName);
    }

    /// <summary>
    /// 验证私钥属于 sm2p256v1 且标量位于合法范围。
    /// </summary>
    /// <param name="key">待验证私钥。</param>
    /// <param name="parameterName">异常中使用的参数名。</param>
    /// <exception cref="ArgumentException">曲线或私钥标量无效时抛出。</exception>
    private static void ValidatePrivateKey(ECPrivateKeyParameters key, string parameterName)
    {
        if (!IsSm2Domain(key.Parameters) || key.D.SignValue <= 0 || key.D.CompareTo(Domain.N) >= 0)
            throw new ArgumentException("PEM 私钥必须是 sm2p256v1 曲线的合法私钥标量。", parameterName);
    }

    /// <summary>
    /// 检查域参数是否与固定 sm2p256v1 域匹配。
    /// </summary>
    /// <param name="parameters">待检查域参数。</param>
    /// <returns>参数匹配时返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    private static bool IsSm2Domain(ECDomainParameters parameters)
    {
        return parameters != null &&
               parameters.Curve.Equals(Domain.Curve) &&
               parameters.G.Equals(Domain.G) &&
               parameters.N.Equals(Domain.N) &&
               parameters.H.Equals(Domain.H);
    }

    /// <summary>
    /// 统计文本中指定片段出现的次数。
    /// </summary>
    /// <param name="value">待搜索文本。</param>
    /// <param name="token">统计片段。</param>
    /// <returns>片段出现次数。</returns>
    private static int CountOccurrences(string value, string token)
    {
        var count = 0;
        var offset = 0;
        while ((offset = value.IndexOf(token, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += token.Length;
        }
        return count;
    }
}