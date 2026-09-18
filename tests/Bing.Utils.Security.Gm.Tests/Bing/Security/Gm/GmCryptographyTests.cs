using System;
using System.IO;
using System.Security.Cryptography;
using Bing.Security.Encoding;
using Bing.Security.Gm;
using Org.BouncyCastle.Asn1.GM;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Utilities.IO.Pem;
using Org.BouncyCastle.X509;
using Shouldly;
using Xunit;

namespace Bing.Utils.Security.Gm.Tests.Bing.Security.Gm;

/// <summary>
/// 验证国密摘要、认证加密、SM2 密钥和签名行为。
/// </summary>
public class GmCryptographyTests
{
    /// <summary>
    /// 测试目的：SM3 应匹配 GM/T 0004 对 ASCII abc 的标准测试向量，HMAC-SM3 应保持确定性。
    /// </summary>
    [Fact]
    public void Sm3_WhenKnownInputIsHashed_ShouldMatchStandardVector()
    {
        // Arrange
        var data = System.Text.Encoding.ASCII.GetBytes("abc");
        var key = System.Text.Encoding.ASCII.GetBytes("hmac-sm3-test-key");

        // Act
        var digest = Sm3.Compute(data);
        var firstMac = HmacSm3.Compute(key, data);
        var secondMac = HmacSm3.Compute(key, data);

        // Assert
        HexEncoding.Encode(digest).ShouldBe("66c7f0f462eeedd9d1f2d46bdc10e4e24167c4875cf2f7a2297da02b8f4ba8e0");
        firstMac.ShouldBe(secondMac);
        firstMac.Length.ShouldBe(Sm3.DigestSize);
    }

    /// <summary>
    /// 测试目的：SM4-GCM 应认证附加数据，并在密文或标签遭篡改时拒绝解密。
    /// </summary>
    [Fact]
    public void Sm4Gcm_WhenPayloadIsTampered_ShouldRejectDecryption()
    {
        // Arrange
        var key = new byte[Sm4GcmEncryption.KeySize];
        var plaintext = System.Text.Encoding.UTF8.GetBytes("SM4-GCM authenticated payload");
        var associatedData = System.Text.Encoding.UTF8.GetBytes("request-42");
        var payload = Sm4GcmEncryption.Encrypt(plaintext, key, associatedData);
        var tag = payload.Tag.ToArray();
        tag[0] ^= 1;
        var tampered = new Sm4GcmPayload(payload.Nonce.ToArray(), payload.Ciphertext.ToArray(), tag);

        // Act
        var decrypted = Sm4GcmEncryption.Decrypt(payload, key, associatedData);
        var tamperedAction = new Action(() => Sm4GcmEncryption.Decrypt(tampered, key, associatedData));
        var wrongAadAction = new Action(() => Sm4GcmEncryption.Decrypt(payload, key, new byte[] { 1 }));

        // Assert
        decrypted.ShouldBe(plaintext);
        tamperedAction.ShouldThrow<CryptographicException>();
        wrongAadAction.ShouldThrow<CryptographicException>();
    }

    /// <summary>
    /// 测试目的：BSM1 负载必须可往返解析，且公开字段不暴露可修改数组。
    /// </summary>
    [Fact]
    public void Sm4GcmPayload_WhenEncodedAndExposedAsReadOnlyMemory_ShouldRemainVersionedAndImmutable()
    {
        // Arrange
        var key = new byte[Sm4GcmEncryption.KeySize];
        var plaintext = System.Text.Encoding.UTF8.GetBytes("SM4 BSM1 payload");
        var payload = Sm4GcmEncryption.Encrypt(plaintext, key);
        var encoded = payload.Encode();
        var exportedCiphertext = payload.Ciphertext.ToArray();
        exportedCiphertext[0] ^= 1;

        // Act
        var parsed = Sm4GcmPayload.Parse(encoded);
        var truncated = Sm4GcmPayload.TryParse(encoded[..^1], out var invalidPayload);
        var decrypted = Sm4GcmEncryption.Decrypt(payload, key);

        // Assert
        encoded.ShouldStartWith("QlNNMQ");
        Sm4GcmEncryption.Decrypt(parsed, key).ShouldBe(plaintext);
        decrypted.ShouldBe(plaintext);
        payload.Ciphertext.ToArray().ShouldNotBe(exportedCiphertext);
        truncated.ShouldBeFalse();
        invalidPayload.ShouldBeNull();
    }

    /// <summary>
    /// 测试目的：SM2 PEM 密钥应支持 C1C3C2 加密和使用默认或自定义用户标识的 SM2withSM3 签名。
    /// </summary>
    [Fact]
    public void Sm2_WhenPemKeysAreGenerated_ShouldEncryptAndSign()
    {
        // Arrange
        var pair = Sm2KeyGenerator.Generate();
        var data = System.Text.Encoding.UTF8.GetBytes("国密 SM2 PEM round-trip");
        var customUserId = System.Text.Encoding.ASCII.GetBytes("merchant-0000001");

        // Act
        var ciphertext = Sm2Encryption.Encrypt(data, pair.PublicKeyPem);
        var plaintext = Sm2Encryption.Decrypt(ciphertext, pair.PrivateKeyPem);
        var defaultSignature = Sm2Signature.Sign(data, pair.PrivateKeyPem);
        var customSignature = Sm2Signature.Sign(data, pair.PrivateKeyPem, customUserId);

        // Assert
        plaintext.ShouldBe(data);
        Sm2Signature.Verify(data, defaultSignature, pair.PublicKeyPem).ShouldBeTrue();
        Sm2Signature.Verify(data, customSignature, pair.PublicKeyPem, customUserId).ShouldBeTrue();
        Sm2Signature.Verify(data, customSignature, pair.PublicKeyPem).ShouldBeFalse();
        Sm2Signature.Verify(System.Text.Encoding.UTF8.GetBytes("changed"), defaultSignature, pair.PublicKeyPem).ShouldBeFalse();
    }

    /// <summary>
    /// 测试目的：GM/T 0003 附录 A.2 的固定签名和 C1C3C2 密文应能被验证和解密，保证实现符合公开标准向量。
    /// </summary>
    [Fact]
    public void Sm2_WhenGmt0003AppendixA2VectorIsProvided_ShouldVerifyAndDecrypt()
    {
        // Arrange
        const string privateKey = "3945208F7B2144B13F36E38AC6D39F95889393692860B51A42FB81EF4DF7C5B8";
        const string publicKeyX = "09F9DF311E5421A150DD7D161E4BC5C672179FAD1833FC076BB08FF356F35020";
        const string publicKeyY = "CCEA490CE26775A52DC6EA718CC1AA600AED05FBF35E084A6632F6072DA9AD13";
        const string signatureR = "F5A03B0648D2C4630EEAC513E1BB81A15944DA3827D5B74143AC7EACEEE720B3";
        const string signatureS = "B1B6AA29DF212FD8763182BC0D421CA1BB9038FD1F7F42D4840B69C485BBC1AA";
        const string ciphertext = "0404EBFC718E8D1798620432268E77FEB6415E2EDE0E073C0F4F640ECD2E149A73" +
                                  "E858F9D81E5430A57B36DAAB8F950A3C64E6EE6A63094D99283AFF767E124DF0" +
                                  "59983C18F809E262923C53AEC295D30383B54E39D609D160AFCB1908D0BD8766" +
                                  "21886CA989CA9C7D58087307CA93092D651EFA";
        var pair = CreatePemKeyPair(privateKey, publicKeyX, publicKeyY);
        var signingData = System.Text.Encoding.ASCII.GetBytes("message digest");
        var encryptionData = System.Text.Encoding.ASCII.GetBytes("encryption standard");
        var signature = CreateDerSignature(signatureR, signatureS);

        // Act
        var verified = Sm2Signature.Verify(signingData, signature, pair.PublicKeyPem);
        var plaintext = Sm2Encryption.Decrypt(HexEncoding.Decode(ciphertext), pair.PrivateKeyPem);

        // Assert
        verified.ShouldBeTrue();
        plaintext.ShouldBe(encryptionData);
    }

    /// <summary>
    /// 测试目的：SM2 PEM 必须使用标准 PKCS#8 与 SPKI 标签，且导入时只能包含一个对应对象。
    /// </summary>
    [Fact]
    public void Sm2_WhenPemContainsUnexpectedOrMultipleObjects_ShouldRejectIt()
    {
        // Arrange
        var pair = Sm2KeyGenerator.Generate();
        var data = new byte[] { 1, 2, 3 };
        var multiplePublicKeys = pair.PublicKeyPem + Environment.NewLine + pair.PublicKeyPem;

        // Act
        var encryptAction = new Action(() => Sm2Encryption.Encrypt(data, multiplePublicKeys));

        // Assert
        pair.PublicKeyPem.ShouldStartWith("-----BEGIN PUBLIC KEY-----");
        pair.PrivateKeyPem.ShouldStartWith("-----BEGIN PRIVATE KEY-----");
        encryptAction.ShouldThrow<ArgumentException>();
    }

    /// <summary>
    /// 测试目的：SM2 公钥和私钥 PEM 的 Base64 内容损坏时，所有入口都应以参数错误拒绝，而不是将密钥错误伪装成验签失败。
    /// </summary>
    [Fact]
    public void Sm2_WhenPemBase64IsMalformed_ShouldThrowArgumentException()
    {
        // Arrange
        const string malformedPublicKeyPem = "-----BEGIN PUBLIC KEY-----\n!!!!\n-----END PUBLIC KEY-----";
        const string malformedPrivateKeyPem = "-----BEGIN PRIVATE KEY-----\n!!!!\n-----END PRIVATE KEY-----";
        var data = new byte[] { 1, 2, 3 };
        var signature = new byte[] { 0x30, 0x00 };
        var ciphertext = new byte[] { 1, 2, 3 };

        // Act
        var encryptAction = new Action(() => Sm2Encryption.Encrypt(data, malformedPublicKeyPem));
        var verifyAction = new Action(() => Sm2Signature.Verify(data, signature, malformedPublicKeyPem));
        var signAction = new Action(() => Sm2Signature.Sign(data, malformedPrivateKeyPem));
        var decryptAction = new Action(() => Sm2Encryption.Decrypt(ciphertext, malformedPrivateKeyPem));

        // Assert
        encryptAction.ShouldThrow<ArgumentException>();
        verifyAction.ShouldThrow<ArgumentException>();
        signAction.ShouldThrow<ArgumentException>();
        decryptAction.ShouldThrow<ArgumentException>();
    }

    /// <summary>
    /// 测试目的：语法正确但使用 P-256 曲线的 PKCS#8 和 SPKI PEM 必须被 SM2 入口拒绝，避免跨曲线密钥被错误接受。
    /// </summary>
    [Fact]
    public void Sm2_WhenPemUsesNonSm2Curve_ShouldThrowArgumentException()
    {
        // Arrange
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var publicKeyPem = "-----BEGIN PUBLIC KEY-----\n" +
                   Convert.ToBase64String(ecdsa.ExportSubjectPublicKeyInfo()) +
                   "\n-----END PUBLIC KEY-----";
        var privateKeyPem = "-----BEGIN PRIVATE KEY-----\n" +
                    Convert.ToBase64String(ecdsa.ExportPkcs8PrivateKey()) +
                    "\n-----END PRIVATE KEY-----";
        var data = new byte[] { 1, 2, 3 };

        // Act
        var encryptAction = new Action(() => Sm2Encryption.Encrypt(data, publicKeyPem));
        var verifyAction = new Action(() => Sm2Signature.Verify(data, new byte[] { 0x30, 0x00 }, publicKeyPem));
        var signAction = new Action(() => Sm2Signature.Sign(data, privateKeyPem));
        var decryptAction = new Action(() => Sm2Encryption.Decrypt(new byte[] { 1, 2, 3 }, privateKeyPem));

        // Assert
        encryptAction.ShouldThrow<ArgumentException>();
        verifyAction.ShouldThrow<ArgumentException>();
        signAction.ShouldThrow<ArgumentException>();
        decryptAction.ShouldThrow<ArgumentException>();
    }

    /// <summary>
    /// 测试目的：SM2 密钥、加密和签名组件应使用同一标准 PEM 契约协同工作。
    /// </summary>
    [Fact]
    public void Sm2Components_WhenUsingSharedPemKeys_ShouldInteroperate()
    {
        // Arrange
        var pair = Sm2KeyGenerator.Generate();
        var data = System.Text.Encoding.UTF8.GetBytes("SM2 component facade interoperability");
        var userId = System.Text.Encoding.ASCII.GetBytes("merchant-0000001");

        // Act
        var componentCiphertext = Sm2Encryption.Encrypt(data, pair.PublicKeyPem);
        var componentSignature = Sm2Signature.Sign(data, pair.PrivateKeyPem, userId);
        var plaintext = Sm2Encryption.Decrypt(componentCiphertext, pair.PrivateKeyPem);

        // Assert
        plaintext.ShouldBe(data);
        Sm2Signature.Verify(data, componentSignature, pair.PublicKeyPem, userId).ShouldBeTrue();
    }

    /// <summary>
    /// 测试目的：SM2 解密必须拒绝被篡改、截断或使用错误私钥的 C1C3C2 密文。
    /// </summary>
    [Fact]
    public void Sm2Encryption_WhenCiphertextIsInvalid_ShouldRejectDecryption()
    {
        // Arrange
        var pair = Sm2KeyGenerator.Generate();
        var wrongPair = Sm2KeyGenerator.Generate();
        var plaintext = System.Text.Encoding.UTF8.GetBytes("SM2 authenticated ciphertext");
        var ciphertext = Sm2Encryption.Encrypt(plaintext, pair.PublicKeyPem);
        var tamperedCiphertext = (byte[])ciphertext.Clone();
        tamperedCiphertext[^1] ^= 1;
        var truncatedCiphertext = ciphertext[..^1];

        // Act
        var tamperedAction = new Action(() => Sm2Encryption.Decrypt(tamperedCiphertext, pair.PrivateKeyPem));
        var truncatedAction = new Action(() => Sm2Encryption.Decrypt(truncatedCiphertext, pair.PrivateKeyPem));
        var wrongKeyAction = new Action(() => Sm2Encryption.Decrypt(ciphertext, wrongPair.PrivateKeyPem));

        // Assert
        tamperedAction.ShouldThrow<CryptographicException>();
        truncatedAction.ShouldThrow<CryptographicException>();
        wrongKeyAction.ShouldThrow<CryptographicException>();
    }

    /// <summary>
    /// 测试目的：SM2 签名组件应拒绝不符合 ENTL 范围的用户标识，并将畸形 DER 签名报告为验证失败。
    /// </summary>
    [Fact]
    public void Sm2Signature_WhenUserIdIsOutsideEntlRangeOrSignatureIsMalformed_ShouldRejectInput()
    {
        // Arrange
        var pair = Sm2KeyGenerator.Generate();
        var data = new byte[] { 1, 2, 3 };
        var signature = Sm2Signature.Sign(data, pair.PrivateKeyPem);
        var emptyUserId = Array.Empty<byte>();
        var tooLongUserId = new byte[Sm2Signature.MaximumUserIdLength + 1];

        // Act
        var signAction = new Action(() => Sm2Signature.Sign(data, pair.PrivateKeyPem, emptyUserId));
        var verifyAction = new Action(() => Sm2Signature.Verify(data, signature, pair.PublicKeyPem, emptyUserId));
        var tooLongAction = new Action(() => Sm2Signature.Sign(data, pair.PrivateKeyPem, tooLongUserId));
        var malformedResult = Sm2Signature.Verify(data, new byte[] { 0x30, 0x01, 0x00 }, pair.PublicKeyPem);

        // Assert
        signAction.ShouldThrow<ArgumentException>();
        verifyAction.ShouldThrow<ArgumentException>();
        tooLongAction.ShouldThrow<ArgumentException>();
        malformedResult.ShouldBeFalse();
    }

    /// <summary>
    /// 将标准测试向量中的私钥标量和公钥坐标封装为库对外支持的 PKCS#8 与 SPKI PEM。
    /// </summary>
    private static (string PublicKeyPem, string PrivateKeyPem) CreatePemKeyPair(string privateKey, string publicKeyX, string publicKeyY)
    {
        var parameters = GMNamedCurves.GetByName("sm2p256v1");
        var domain = new ECNamedDomainParameters(GMObjectIdentifiers.sm2p256v1, parameters.Curve, parameters.G, parameters.N, parameters.H, parameters.GetSeed());
        var privateKeyParameters = new ECPrivateKeyParameters(new BigInteger(privateKey, 16), domain);
        var publicKeyParameters = new ECPublicKeyParameters(parameters.Curve.CreatePoint(new BigInteger(publicKeyX, 16), new BigInteger(publicKeyY, 16)), domain);
        return (
            WritePem("PUBLIC KEY", SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(publicKeyParameters).GetEncoded()),
            WritePem("PRIVATE KEY", PrivateKeyInfoFactory.CreatePrivateKeyInfo(privateKeyParameters).GetEncoded()));
    }

    /// <summary>
    /// 将 32 字节 r 和 s 组件封装为固定长度 DER SM2 签名。
    /// </summary>
    private static byte[] CreateDerSignature(string r, string s)
    {
        var rBytes = HexEncoding.Decode(r);
        var sBytes = HexEncoding.Decode(s);
        var signature = new byte[72];
        signature[0] = 0x30;
        signature[1] = 0x46;
        signature[2] = 0x02;
        signature[3] = 0x21;
        Buffer.BlockCopy(rBytes, 0, signature, 5, rBytes.Length);
        signature[37] = 0x02;
        signature[38] = 0x21;
        Buffer.BlockCopy(sBytes, 0, signature, 40, sBytes.Length);
        return signature;
    }

    /// <summary>
    /// 编码单个 PEM 对象。
    /// </summary>
    private static string WritePem(string type, byte[] encoded)
    {
        using var writer = new StringWriter();
        var pemWriter = new Org.BouncyCastle.OpenSsl.PemWriter(writer);
        pemWriter.WriteObject(new PemObject(type, encoded));
        pemWriter.Writer.Flush();
        return writer.ToString();
    }
}