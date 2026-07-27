using System;
using System.Security.Cryptography;
using Bing.Security.Keys;
using Shouldly;
using Xunit;

namespace Bing.Security.Cryptography;

/// <summary>
/// 验证 RSA 与 AES-GCM 混合加密及版本化载荷行为。
/// </summary>
public class HybridEncryptionTests
{
    /// <summary>
    /// 测试目的：混合加密应能处理大于 RSA 明文边界的数据和关联数据。
    /// </summary>
    [Fact]
    public void EncryptDecrypt_WhenDataAndAssociatedDataAreValid_ShouldRoundTrip()
    {
        // Arrange
        var pair = RsaKeyGenerator.Generate(2048);
        var plaintext = new byte[4096];
        var associatedData = new byte[] { 7, 8, 9 };
        RandomNumberGenerator.Fill(plaintext);

        // Act
        var payload = HybridEncryption.Encrypt(plaintext, pair.PublicKeyPem, associatedData);
        var encoded = payload.Encode();
        var parsed = HybridEncryptedPayload.Parse(encoded);
        var decrypted = HybridEncryption.Decrypt(parsed, pair.PrivateKeyPem, associatedData);

        // Assert
        decrypted.ShouldBe(plaintext);
        payload.EncryptedKey.Length.ShouldBe(256);
    }

    /// <summary>
    /// 测试目的：关联数据、RSA 包装密钥、AES-GCM 密文和私钥被修改后必须解密失败。
    /// </summary>
    [Fact]
    public void Decrypt_WhenAuthenticatedFieldsOrPrivateKeyChange_ShouldThrowCryptographicException()
    {
        // Arrange
        var pair = RsaKeyGenerator.Generate(2048);
        var otherPair = RsaKeyGenerator.Generate(2048);
        var associatedData = new byte[] { 1, 2, 3 };
        var payload = HybridEncryption.Encrypt(new byte[] { 4, 5, 6 }, pair.PublicKeyPem, associatedData);
        var changedKey = payload.EncryptedKey.ToArray();
        changedKey[0] ^= 1;
        var changedKeyPayload = new HybridEncryptedPayload(changedKey, payload.EncryptedData);
        var changedCiphertext = payload.EncryptedData.Ciphertext.ToArray();
        changedCiphertext[0] ^= 1;
        var changedDataPayload = new HybridEncryptedPayload(payload.EncryptedKey.ToArray(), new AesGcmPayload(payload.EncryptedData.Nonce.ToArray(), changedCiphertext, payload.EncryptedData.Tag.ToArray()));

        // Act
        var associatedDataAction = () => HybridEncryption.Decrypt(payload, pair.PrivateKeyPem, new byte[] { 3, 2, 1 });
        var keyAction = () => HybridEncryption.Decrypt(changedKeyPayload, pair.PrivateKeyPem, associatedData);
        var ciphertextAction = () => HybridEncryption.Decrypt(changedDataPayload, pair.PrivateKeyPem, associatedData);
        var privateKeyAction = () => HybridEncryption.Decrypt(payload, otherPair.PrivateKeyPem, associatedData);

        // Assert
        associatedDataAction.ShouldThrow<CryptographicException>();
        keyAction.ShouldThrow<CryptographicException>();
        ciphertextAction.ShouldThrow<CryptographicException>();
        privateKeyAction.ShouldThrow<CryptographicException>();
    }

    /// <summary>
    /// 测试目的：混合载荷解析应拒绝截断和格式非法的数据。
    /// </summary>
    [Fact]
    public void HybridEncryptedPayload_WhenValueIsTruncated_ShouldReturnFalse()
    {
        // Arrange
        var pair = RsaKeyGenerator.Generate(2048);
        var encoded = HybridEncryption.Encrypt(Array.Empty<byte>(), pair.PublicKeyPem).Encode();

        // Act
        var success = HybridEncryptedPayload.TryParse(encoded[..^1], out var payload);

        // Assert
        success.ShouldBeFalse();
        payload.ShouldBeNull();
    }
}
