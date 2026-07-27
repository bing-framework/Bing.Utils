using System;
using System.Security.Cryptography;
using Bing.Security.Encoding;
using Bing.Security.Gm;
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
        var firstMac = Sm3.ComputeHmac(key, data);
        var secondMac = Sm3.ComputeHmac(key, data);

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
        var tag = (byte[])payload.Tag.Clone();
        tag[0] ^= 1;
        var tampered = new Sm4GcmPayload(payload.Nonce, payload.Ciphertext, tag);

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
    /// 测试目的：BSM1 负载必须可往返解析，且导出数组被修改不得影响负载自身。
    /// </summary>
    [Fact]
    public void Sm4GcmPayload_WhenEncodedAndExported_ShouldRemainVersionedAndImmutable()
    {
        // Arrange
        var key = new byte[Sm4GcmEncryption.KeySize];
        var plaintext = System.Text.Encoding.UTF8.GetBytes("SM4 BSM1 payload");
        var payload = Sm4GcmEncryption.Encrypt(plaintext, key);
        var encoded = payload.Encode();
        var exportedCiphertext = payload.Ciphertext;
        exportedCiphertext[0] ^= 1;

        // Act
        var parsed = Sm4GcmPayload.Parse(encoded);
        var truncated = Sm4GcmPayload.TryParse(encoded[..^1], out var invalidPayload);
        var decrypted = Sm4GcmEncryption.Decrypt(payload, key);

        // Assert
        encoded.ShouldStartWith("QlNNMQ");
        Sm4GcmEncryption.Decrypt(parsed, key).ShouldBe(plaintext);
        decrypted.ShouldBe(plaintext);
        payload.Ciphertext.ShouldNotBe(exportedCiphertext);
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
        var pair = Sm2.GenerateKeyPair();
        var data = System.Text.Encoding.UTF8.GetBytes("国密 SM2 PEM round-trip");
        var customUserId = System.Text.Encoding.ASCII.GetBytes("merchant-0000001");

        // Act
        var ciphertext = Sm2.Encrypt(data, pair.PublicKeyPem);
        var plaintext = Sm2.Decrypt(ciphertext, pair.PrivateKeyPem);
        var defaultSignature = Sm2.Sign(data, pair.PrivateKeyPem);
        var customSignature = Sm2.Sign(data, pair.PrivateKeyPem, customUserId);

        // Assert
        plaintext.ShouldBe(data);
        Sm2.Verify(data, defaultSignature, pair.PublicKeyPem).ShouldBeTrue();
        Sm2.Verify(data, customSignature, pair.PublicKeyPem, customUserId).ShouldBeTrue();
        Sm2.Verify(data, customSignature, pair.PublicKeyPem).ShouldBeFalse();
        Sm2.Verify(System.Text.Encoding.UTF8.GetBytes("changed"), defaultSignature, pair.PublicKeyPem).ShouldBeFalse();
    }
}