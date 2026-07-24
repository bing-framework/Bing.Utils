using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Bing.Security.Certificates;
using Shouldly;
using Xunit;

namespace Bing.Utils.Security.Tests.Bing.Security.Certificates;

/// <summary>
/// 验证 X.509 证书加载、密钥读取和 SHA-256 指纹操作。
/// </summary>
public class CertificateTests
{
    /// <summary>
    /// 测试目的：PFX 证书应在正确密码下加载并提供 RSA 公私钥。
    /// </summary>
    [Fact]
    public void Load_WhenPfxPasswordIsCorrect_ShouldReadRsaKeys()
    {
        // Arrange
        using var source = CreateRsaCertificate();
        var pfx = source.Export(X509ContentType.Pfx, "test-password");

        // Act
        using var loaded = CertificateLoader.Load(pfx, "test-password");
        using var publicKey = CertificateKeyReader.GetRsaPublicKey(loaded);
        using var privateKey = CertificateKeyReader.GetRsaPrivateKey(loaded);

        // Assert
        loaded.HasPrivateKey.ShouldBeTrue();
        publicKey.KeySize.ShouldBe(2048);
        privateKey.KeySize.ShouldBe(2048);
    }

    /// <summary>
    /// 测试目的：错误 PFX 密码和不含私钥的证书应被正确处理。
    /// </summary>
    [Fact]
    public void LoadAndRead_WhenPasswordOrPrivateKeyIsUnavailable_ShouldThrowArgumentException()
    {
        // Arrange
        using var source = CreateRsaCertificate();
        var pfx = source.Export(X509ContentType.Pfx, "test-password");
        var certificateOnly = source.Export(X509ContentType.Cert);
        using var publicCertificate = CertificateLoader.Load(certificateOnly);

        // Act
        var wrongPassword = () => CertificateLoader.Load(pfx, "wrong-password");
        var missingPrivateKey = () => CertificateKeyReader.GetRsaPrivateKey(publicCertificate);

        // Assert
        wrongPassword.ShouldThrow<ArgumentException>();
        missingPrivateKey.ShouldThrow<ArgumentException>();
    }

    /// <summary>
    /// 测试目的：DER 与 PEM 证书的指纹应一致且基于稳定的原始证书数据。
    /// </summary>
    [Fact]
    public void CertificateFingerprint_WhenDerAndPemRepresentSameCertificate_ShouldMatch()
    {
        // Arrange
        using var source = CreateRsaCertificate();
        var der = source.Export(X509ContentType.Cert);
        var pem = new string(PemEncoding.Write("CERTIFICATE", der));

        // Act
        using var derCertificate = CertificateLoader.Load(der);
        using var pemCertificate = CertificateLoader.Load(System.Text.Encoding.ASCII.GetBytes(pem));
        var derFingerprint = CertificateFingerprint.ComputeSha256(derCertificate);
        var pemFingerprint = CertificateFingerprint.ComputeSha256(pemCertificate);

        // Assert
        derFingerprint.ShouldBe(pemFingerprint);
        derFingerprint.Length.ShouldBe(64);
    }

    /// <summary>
    /// 在测试运行时生成短期 RSA 自签名证书。
    /// </summary>
    /// <returns>包含 RSA 私钥的自签名证书。</returns>
    private static X509Certificate2 CreateRsaCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=Bing.Utils.Security.Tests", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
    }
}