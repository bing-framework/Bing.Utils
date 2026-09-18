using System;
using System.Security.Cryptography;
using Bing.Security.Keys;
using Bing.Security.Signatures;
using Shouldly;
using Xunit;
namespace Bing.Security.Canonicalization;

public class VersionedRequestSignerTests
{
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

    [Theory]
    [InlineData("")]
    [InlineData("BRS2.RSA-PSS-SHA256.AA==")]
    [InlineData("BRS1.RSA-PSS-SHA256.invalid")]
    public void Verify_InvalidEnvelope_ShouldReturnFalse(string signature)
    {
        using var key = RSA.Create(2048);
        VersionedRequestSigner.Verify(Array.Empty<CanonicalParameter>(), signature, key).ShouldBeFalse();
    }

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

    [Fact]
    public void RsaVerify_InvalidSignature_ShouldReturnFalse_ButInvalidKeyShouldThrow()
    {
        using var key = RSA.Create(2048);
        RsaSignature.Verify(new byte[] { 1 }, new byte[] { 0 }, new string(PemEncoding.Write("PUBLIC KEY", key.ExportSubjectPublicKeyInfo()))).ShouldBeFalse();
        Should.Throw<ArgumentException>(() => RsaSignature.Verify(new byte[] { 1 }, new byte[] { 0 }, "invalid"));
    }

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
