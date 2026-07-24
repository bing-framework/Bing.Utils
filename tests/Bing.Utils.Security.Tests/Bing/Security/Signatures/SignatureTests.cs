using System;
using System.Linq;
using Bing.Security.Keys;
using Bing.Security.Signatures;
using Shouldly;
using Xunit;

namespace Bing.Utils.Security.Tests.Bing.Security.Signatures;

/// <summary>
/// 验证 RSA-PSS 与 DER ECDSA 签名操作。
/// </summary>
public class SignatureTests
{
    /// <summary>
    /// 测试目的：RSA-PSS-SHA256 应验证原始数据并拒绝被篡改的数据、签名和公钥。
    /// </summary>
    [Fact]
    public void RsaSignature_WhenDataSignatureOrKeyChanges_ShouldReturnExpectedResult()
    {
        // Arrange
        var pair = RsaKeyGenerator.Generate(2048);
        var otherPair = RsaKeyGenerator.Generate(2048);
        var data = new byte[] { 1, 2, 3 };
        var signature = RsaSignature.Sign(data, pair.PrivateKeyPem);
        var changedSignature = signature.ToArray();
        changedSignature[0] ^= 1;

        // Act
        var valid = RsaSignature.Verify(data, signature, pair.PublicKeyPem);
        var changedData = RsaSignature.Verify(new byte[] { 1, 2, 4 }, signature, pair.PublicKeyPem);
        var changedSignatureResult = RsaSignature.Verify(data, changedSignature, pair.PublicKeyPem);
        var wrongKey = RsaSignature.Verify(data, signature, otherPair.PublicKeyPem);

        // Assert
        valid.ShouldBeTrue();
        changedData.ShouldBeFalse();
        changedSignatureResult.ShouldBeFalse();
        wrongKey.ShouldBeFalse();
    }

    /// <summary>
    /// 测试目的：每条支持的 NIST 曲线都应生成并验证 DER 格式 ECDSA 签名。
    /// </summary>
    [Theory]
    [InlineData(EcdsaCurve.P256)]
    [InlineData(EcdsaCurve.P384)]
    [InlineData(EcdsaCurve.P521)]
    public void EcdsaSignature_WhenCurveIsSupported_ShouldUseDerSignature(EcdsaCurve curve)
    {
        // Arrange
        var pair = EcdsaKeyGenerator.Generate(curve);
        var otherPair = EcdsaKeyGenerator.Generate(curve);
        var data = new byte[] { 3, 2, 1 };
        var signature = EcdsaSignature.Sign(data, pair.PrivateKeyPem);
        var changedSignature = signature.ToArray();
        changedSignature[^1] ^= 1;

        // Act
        var valid = EcdsaSignature.Verify(data, signature, pair.PublicKeyPem);
        var changedData = EcdsaSignature.Verify(new byte[] { 3, 2, 0 }, signature, pair.PublicKeyPem);
        var changedSignatureResult = EcdsaSignature.Verify(data, changedSignature, pair.PublicKeyPem);
        var wrongKey = EcdsaSignature.Verify(data, signature, otherPair.PublicKeyPem);
        var malformed = EcdsaSignature.Verify(data, new byte[] { 1, 2, 3 }, pair.PublicKeyPem);

        // Assert
        signature[0].ShouldBe((byte)0x30);
        valid.ShouldBeTrue();
        changedData.ShouldBeFalse();
        changedSignatureResult.ShouldBeFalse();
        wrongKey.ShouldBeFalse();
        malformed.ShouldBeFalse();
    }
}