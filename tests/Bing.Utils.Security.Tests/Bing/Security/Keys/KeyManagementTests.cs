using System;
using Shouldly;
using Xunit;

namespace Bing.Security.Keys;

/// <summary>
/// 验证强类型密钥生成、PEM 导入和公钥指纹行为。
/// </summary>
public class KeyManagementTests
{
    /// <summary>
    /// 测试目的：AES 密钥生成器应生成指定的标准密钥长度。
    /// </summary>
    [Theory]
    [InlineData(AesKeySize.Size128, 16)]
    [InlineData(AesKeySize.Size192, 24)]
    [InlineData(AesKeySize.Size256, 32)]
    public void Generate_WhenAesKeySizeIsSupported_ShouldReturnExpectedLength(AesKeySize keySize, int expectedLength)
    {
        // Arrange

        // Act
        var key = AesKeyGenerator.Generate(keySize);

        // Assert
        key.Length.ShouldBe(expectedLength);
    }

    /// <summary>
    /// 测试目的：RSA 密钥应以 PKCS#8 和 SubjectPublicKeyInfo PEM 表示并可重新导入。
    /// </summary>
    [Fact]
    public void RsaKeyGenerator_WhenGenerating2048BitKey_ShouldExportStandardPem()
    {
        // Arrange

        // Act
        var pair = RsaKeyGenerator.Generate(2048);
        using var publicKey = PemKeySerializer.ImportRsaPublicKey(pair.PublicKeyPem);
        using var privateKey = PemKeySerializer.ImportRsaPrivateKey(pair.PrivateKeyPem);

        // Assert
        pair.PublicKeyPem.ShouldContain("BEGIN PUBLIC KEY");
        pair.PrivateKeyPem.ShouldContain("BEGIN PRIVATE KEY");
        publicKey.KeySize.ShouldBe(2048);
        privateKey.KeySize.ShouldBe(2048);
    }

    /// <summary>
    /// 测试目的：ECDSA 密钥应使用所选命名曲线并可按标准 PEM 重新导入。
    /// </summary>
    [Theory]
    [InlineData(EcdsaCurve.P256)]
    [InlineData(EcdsaCurve.P384)]
    [InlineData(EcdsaCurve.P521)]
    public void EcdsaKeyGenerator_WhenCurveIsSupported_ShouldExportImportablePem(EcdsaCurve curve)
    {
        // Arrange

        // Act
        var pair = EcdsaKeyGenerator.Generate(curve);
        using var publicKey = PemKeySerializer.ImportEcdsaPublicKey(pair.PublicKeyPem);
        using var privateKey = PemKeySerializer.ImportEcdsaPrivateKey(pair.PrivateKeyPem);

        // Assert
        publicKey.KeySize.ShouldBeGreaterThan(0);
        privateKey.KeySize.ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// 测试目的：密钥用途错误和非法 PEM 应产生明确失败。
    /// </summary>
    [Fact]
    public void PemKeySerializer_WhenKeyPurposeOrFormatIsInvalid_ShouldThrowArgumentException()
    {
        // Arrange
        var pair = RsaKeyGenerator.Generate(2048);

        // Act
        var publicAsPrivate = () => PemKeySerializer.ImportRsaPrivateKey(pair.PublicKeyPem);
        var privateAsPublic = () => PemKeySerializer.ImportRsaPublicKey(pair.PrivateKeyPem);
        var invalid = () => PemKeySerializer.ImportRsaPublicKey("not a pem");

        // Assert
        publicAsPrivate.ShouldThrow<ArgumentException>();
        privateAsPublic.ShouldThrow<ArgumentException>();
        invalid.ShouldThrow<ArgumentException>();
    }

    /// <summary>
    /// 测试目的：同一公钥的指纹应稳定，且不受 PEM 换行符影响。
    /// </summary>
    [Fact]
    public void KeyFingerprint_WhenPemLineEndingsDiffer_ShouldRemainStable()
    {
        // Arrange
        var pair = RsaKeyGenerator.Generate(2048);
        var unixPem = pair.PublicKeyPem.Replace("\r\n", "\n");

        // Act
        var first = KeyFingerprint.ComputeSha256(pair.PublicKeyPem);
        var second = KeyFingerprint.ComputeSha256(unixPem);

        // Assert
        first.ShouldBe(second);
        first.Length.ShouldBe(64);
    }
}
