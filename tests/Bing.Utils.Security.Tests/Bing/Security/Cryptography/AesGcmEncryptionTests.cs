using System;
using System.Security.Cryptography;
using Bing.Security.Keys;
using Shouldly;
using Xunit;

namespace Bing.Security.Cryptography;

/// <summary>
/// 验证 AES-GCM 认证加密、关联数据和版本化载荷行为。
/// </summary>
public class AesGcmEncryptionTests
{
    /// <summary>
    /// 验证支持的 AES 密钥长度可往返处理空数据和二进制数据。
    /// </summary>
    /// <param name="keySize">要用于往返加解密的 AES 密钥长度。</param>
    [Theory]
    [InlineData(AesKeySize.Size128)]
    [InlineData(AesKeySize.Size192)]
    [InlineData(AesKeySize.Size256)]
    public void EncryptDecrypt_WhenKeySizeIsSupported_ShouldRoundTrip(AesKeySize keySize)
    {
        // Arrange
        var key = AesKeyGenerator.Generate(keySize);
        var plaintext = new byte[] { 0, 1, 2, 255 };

        // Act
        var payload = AesGcmEncryption.Encrypt(plaintext, key);
        var decrypted = AesGcmEncryption.Decrypt(payload, key);
        var emptyPayload = AesGcmEncryption.Encrypt(Array.Empty<byte>(), key);

        // Assert
        decrypted.ShouldBe(plaintext);
        AesGcmEncryption.Decrypt(emptyPayload, key).ShouldBeEmpty();
    }

    /// <summary>
    /// 验证同一明文和密钥的重复加密使用不同 Nonce。
    /// </summary>
    [Fact]
    public void Encrypt_WhenCalledMultipleTimes_ShouldUseDifferentNonces()
    {
        // Arrange
        var key = AesKeyGenerator.Generate();
        var plaintext = System.Text.Encoding.UTF8.GetBytes("相同明文");

        // Act
        var first = AesGcmEncryption.Encrypt(plaintext, key);
        var second = AesGcmEncryption.Encrypt(plaintext, key);

        // Assert
        first.Nonce.Span.SequenceEqual(second.Nonce.Span).ShouldBeFalse();
        first.Encode().ShouldNotBe(second.Encode());
    }

    /// <summary>
    /// 验证修改密文、Nonce、认证标签、关联数据或密钥后无法解密。
    /// </summary>
    [Fact]
    public void Decrypt_WhenAuthenticatedInputChanges_ShouldThrowCryptographicException()
    {
        // Arrange
        var key = AesKeyGenerator.Generate();
        var associatedData = System.Text.Encoding.UTF8.GetBytes("request-id");
        var payload = AesGcmEncryption.Encrypt(System.Text.Encoding.UTF8.GetBytes("机密数据"), key, associatedData);
        var changedCiphertextBytes = payload.Ciphertext.ToArray();
        changedCiphertextBytes[0] ^= 1;
        var changedNonceBytes = payload.Nonce.ToArray();
        changedNonceBytes[0] ^= 1;
        var changedTagBytes = payload.Tag.ToArray();
        changedTagBytes[0] ^= 1;
        var changedCiphertext = new AesGcmPayload(payload.Nonce.ToArray(), changedCiphertextBytes, payload.Tag.ToArray());
        var changedNonce = new AesGcmPayload(changedNonceBytes, payload.Ciphertext.ToArray(), payload.Tag.ToArray());
        var changedTag = new AesGcmPayload(payload.Nonce.ToArray(), payload.Ciphertext.ToArray(), changedTagBytes);

        // Act
        var ciphertextAction = () => AesGcmEncryption.Decrypt(changedCiphertext, key, associatedData);
        var nonceAction = () => AesGcmEncryption.Decrypt(changedNonce, key, associatedData);
        var tagAction = () => AesGcmEncryption.Decrypt(changedTag, key, associatedData);
        var associatedDataAction = () => AesGcmEncryption.Decrypt(payload, key, System.Text.Encoding.UTF8.GetBytes("other"));
        var keyAction = () => AesGcmEncryption.Decrypt(payload, AesKeyGenerator.Generate(), associatedData);

        // Assert
        ciphertextAction.ShouldThrow<CryptographicException>();
        nonceAction.ShouldThrow<CryptographicException>();
        tagAction.ShouldThrow<CryptographicException>();
        associatedDataAction.ShouldThrow<CryptographicException>();
        keyAction.ShouldThrow<CryptographicException>();
    }

    /// <summary>
    /// 验证版本化载荷可往返解析，并拒绝截断数据。
    /// </summary>
    [Fact]
    public void AesGcmPayload_WhenEncodedAndParsed_ShouldValidateFormat()
    {
        // Arrange
        var key = AesKeyGenerator.Generate();
        var payload = AesGcmEncryption.Encrypt(System.Text.Encoding.UTF8.GetBytes("版本化载荷"), key);

        // Act
        var encoded = payload.Encode();
        var parsed = AesGcmPayload.Parse(encoded);
        var truncated = encoded[..^1];
        var success = AesGcmPayload.TryParse(truncated, out var invalidPayload);

        // Assert
        AesGcmEncryption.Decrypt(parsed, key).ShouldBe(System.Text.Encoding.UTF8.GetBytes("版本化载荷"));
        success.ShouldBeFalse();
        invalidPayload.ShouldBeNull();
    }

    /// <summary>
    /// 验证修改读取出的密文字节副本不会改变载荷保存的认证数据。
    /// </summary>
    [Fact]
    public void AesGcmPayload_WhenExportedBytesAreModified_ShouldRetainOwnedData()
    {
        // Arrange
        var key = AesKeyGenerator.Generate();
        var plaintext = System.Text.Encoding.UTF8.GetBytes("不可变载荷");
        var payload = AesGcmEncryption.Encrypt(plaintext, key);
        var exportedCiphertext = payload.Ciphertext.ToArray();
        exportedCiphertext[0] ^= 1;

        // Act
        var decrypted = AesGcmEncryption.Decrypt(payload, key);

        // Assert
        decrypted.ShouldBe(plaintext);
        payload.Ciphertext.Span.SequenceEqual(exportedCiphertext).ShouldBeFalse();
    }
}
