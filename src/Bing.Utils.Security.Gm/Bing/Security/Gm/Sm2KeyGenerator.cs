using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace Bing.Security.Gm;

/// <summary>
/// 提供固定使用 sm2p256v1 曲线的 SM2 密钥对生成操作。
/// </summary>
public static class Sm2KeyGenerator
{
    /// <summary>
    /// 生成使用 PKCS#8 私钥和 SubjectPublicKeyInfo 公钥 PEM 编码的 SM2 密钥对。
    /// </summary>
    /// <returns>新生成的 sm2p256v1 密钥对。</returns>
    public static Sm2KeyPair Generate()
    {
        var generator = new ECKeyPairGenerator();
        generator.Init(new ECKeyGenerationParameters(Sm2KeySerializer.Domain, new SecureRandom()));
        var pair = generator.GenerateKeyPair();
        return new Sm2KeyPair(
            Sm2KeySerializer.WritePublicKey((ECPublicKeyParameters)pair.Public),
            Sm2KeySerializer.WritePrivateKey((ECPrivateKeyParameters)pair.Private));
    }
}