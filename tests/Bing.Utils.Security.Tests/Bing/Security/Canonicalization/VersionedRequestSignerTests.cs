using System;
using System.Security.Cryptography;
using Bing.Security.Keys;
using Bing.Security.Signatures;
using Shouldly;
using Xunit;
namespace Bing.Security.Canonicalization;

/// <summary>
/// 验证带版本封套的 RSA 请求签名和 PEM 密钥导入行为。
/// </summary>
public class VersionedRequestSignerTests
{
    /// <summary>
    /// 验证签名绑定规范化参数、协议标识和算法标识，且调用方仍拥有密钥。
    /// </summary>
    [Fact]
    public void SignAndVerify_ShouldBindProtocolAndParameters_WithoutOwningKey()
    {
        using var key = RSA.Create(2048);
        var parameters = new[] { new CanonicalParameter("message", "中文") };
        var signature = VersionedRequestSigner.Sign(parameters, key);
        VersionedRequestSigner.Verify(parameters, signature, key).ShouldBeTrue();
        VersionedRequestSigner.Verify(new[] { new CanonicalParameter("message", "changed") }, signature, key).ShouldBeFalse();
        VersionedRequestSigner.Verify(parameters, signature.Replace("BRS1", "BRS0"), key).ShouldBeFalse();
        VersionedRequestSigner.Verify(parameters, signature.Replace("RSA-PSS", "RSA-PKCS1"), key).ShouldBeFalse();
        using var other = RSA.Create(2048);
        VersionedRequestSigner.Verify(parameters, signature, other).ShouldBeFalse();
        var raw = key.SignData(System.Text.Encoding.UTF8.GetBytes(CanonicalParameterSerializer.Serialize(parameters)),
            HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        VersionedRequestSigner.Verify(parameters, VersionedRequestSigner.Protocol + "." + Convert.ToBase64String(raw), key).ShouldBeFalse();
        key.SignData(new byte[] { 1 }, HashAlgorithmName.SHA256, RSASignaturePadding.Pss).Length.ShouldBe(256);
    }

    /// <summary>
    /// 验证格式无效或版本不支持的签名封套返回 <see langword="false"/>。
    /// </summary>
    /// <param name="signature">要验证的版本化签名封套。</param>
    [Theory]
    [InlineData("")]
    [InlineData("BRS2.RSA-PSS-SHA256.AA==")]
    [InlineData("BRS1.RSA-PSS-SHA256.invalid")]
    public void Verify_InvalidEnvelope_ShouldReturnFalse(string signature)
    {
        using var key = RSA.Create(2048);
        VersionedRequestSigner.Verify(Array.Empty<CanonicalParameter>(), signature, key).ShouldBeFalse();
    }

    /// <summary>
    /// 验证弱 RSA 密钥及空参数会被签名和验证入口拒绝。
    /// </summary>
    [Fact]
    public void SignAndVerify_InvalidConfiguration_ShouldThrow()
    {
        using var weak = RSA.Create(1024);
        Should.Throw<ArgumentException>(() => VersionedRequestSigner.Sign(Array.Empty<CanonicalParameter>(), weak));
        Should.Throw<ArgumentNullException>(() => VersionedRequestSigner.Sign(null, null));
        using var key = RSA.Create(2048);
        Should.Throw<ArgumentNullException>(() => VersionedRequestSigner.Sign(null, key));
        Should.Throw<ArgumentNullException>(() => VersionedRequestSigner.Verify(Array.Empty<CanonicalParameter>(), null, key));
    }

    /// <summary>
    /// 验证 RSA 验签对无效签名返回 <see langword="false"/>，对无效密钥抛出参数异常。
    /// </summary>
    [Fact]
    public void RsaVerify_InvalidSignature_ShouldReturnFalse_ButInvalidKeyShouldThrow()
    {
        using var key = RSA.Create(2048);
        RsaSignature.Verify(new byte[] { 1 }, new byte[] { 0 }, new string(PemEncoding.Write("PUBLIC KEY", key.ExportSubjectPublicKeyInfo()))).ShouldBeFalse();
        Should.Throw<ArgumentException>(() => RsaSignature.Verify(new byte[] { 1 }, new byte[] { 0 }, "invalid"));
    }

    /// <summary>
    /// 验证 PEM 导入拒绝混合用途或多个对象，并保留导入私钥的签名能力。
    /// </summary>
    [Fact]
    public void PemImport_ShouldRejectMixedPurpose_AndKeepPrivateKeyUsable()
    {
        using var key = RSA.Create(2048);
        var pem = new string(PemEncoding.Write("PRIVATE KEY", key.ExportPkcs8PrivateKey()));
        using var imported = PemKeySerializer.ImportRsaPrivateKey(pem);
        imported.SignData(new byte[] { 1 }, HashAlgorithmName.SHA256, RSASignaturePadding.Pss).Length.ShouldBe(256);
        var mixed = "-----BEGIN PRIVATE KEY-----\ninvalid\n-----END PRIVATE KEY-----\n" + new string(PemEncoding.Write("PUBLIC KEY", key.ExportSubjectPublicKeyInfo()));
        Should.Throw<ArgumentException>(() => PemKeySerializer.ImportRsaPrivateKey(mixed));
        Should.Throw<ArgumentException>(() => PemKeySerializer.ImportRsaPublicKey(pem + new string(PemEncoding.Write("PUBLIC KEY", key.ExportSubjectPublicKeyInfo()))));
    }
}
