#if !NETSTANDARD2_0
using System.Security.Cryptography;

namespace Bing.Security.Keys;

/// <summary>
/// 提供 RSA 密钥对生成操作。
/// </summary>
public static class RsaKeyGenerator
{
    /// <summary>
    /// 生成 RSA 密钥对。
    /// </summary>
    /// <param name="keySize">RSA 密钥位长度，最小为 2048，默认使用 3072。</param>
    /// <returns>使用 PKCS#8 私钥和 SubjectPublicKeyInfo 公钥表示的密钥对。</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="keySize"/> 小于 2048 或不是 256 的倍数时抛出。</exception>
    public static RsaKeyPair Generate(int keySize = 3072)
    {
        if (keySize < 2048 || keySize % 256 != 0)
            throw new ArgumentOutOfRangeException(nameof(keySize), keySize, "RSA 密钥长度必须不小于 2048 位且为 256 的倍数。");

        using var rsa = RSA.Create(keySize);
        return new RsaKeyPair(
            new string(PemEncoding.Write("PUBLIC KEY", rsa.ExportSubjectPublicKeyInfo())),
            new string(PemEncoding.Write("PRIVATE KEY", rsa.ExportPkcs8PrivateKey())));
    }
}
#endif