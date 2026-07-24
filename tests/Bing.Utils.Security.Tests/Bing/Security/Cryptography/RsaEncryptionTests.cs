using System;
using System.Security.Cryptography;
using System.Text;
using Bing.Security.Cryptography;
using Bing.Security.Keys;
using Shouldly;
using Xunit;

namespace Bing.Utils.Security.Tests.Bing.Security.Cryptography;

/// <summary>
/// 验证 RSA-OAEP-SHA256 加密、解密和长度边界。
/// </summary>
public class RsaEncryptionTests
{
    /// <summary>
    /// 测试目的：RSA 公钥应以 OAEP-SHA256 加密短数据，私钥应能恢复原文。
    /// </summary>
    [Fact]
    public void EncryptDecrypt_WhenKeysAreValid_ShouldRoundTrip()
    {
        // Arrange
        var pair = RsaKeyGenerator.Generate(2048);
        var plaintext = System.Text.Encoding.UTF8.GetBytes("RSA-OAEP-SHA256");

        // Act
        var ciphertext = RsaEncryption.Encrypt(plaintext, pair.PublicKeyPem);
        var decrypted = RsaEncryption.Decrypt(ciphertext, pair.PrivateKeyPem);

        // Assert
        ciphertext.Length.ShouldBe(256);
        decrypted.ShouldBe(plaintext);
    }

    /// <summary>
    /// 测试目的：RSA 不应自动分段加密超长数据，并应拒绝错误用途或长度的密文。
    /// </summary>
    [Fact]
    public void EncryptDecrypt_WhenInputOrKeyUsageIsInvalid_ShouldThrowArgumentException()
    {
        // Arrange
        var pair = RsaKeyGenerator.Generate(2048);
        var overlongPlaintext = new byte[191];
        var validCiphertext = RsaEncryption.Encrypt(new byte[] { 1 }, pair.PublicKeyPem);

        // Act
        var encryptAction = () => RsaEncryption.Encrypt(overlongPlaintext, pair.PublicKeyPem);
        var publicDecryptAction = () => RsaEncryption.Decrypt(validCiphertext, pair.PublicKeyPem);
        var invalidCiphertextAction = () => RsaEncryption.Decrypt(new byte[1], pair.PrivateKeyPem);

        // Assert
        encryptAction.ShouldThrow<ArgumentException>();
        publicDecryptAction.ShouldThrow<ArgumentException>();
        invalidCiphertextAction.ShouldThrow<ArgumentException>();
    }

    /// <summary>
    /// 测试目的：错误 RSA 私钥不能解密由其他公钥加密的密文。
    /// </summary>
    [Fact]
    public void Decrypt_WhenPrivateKeyIsIncorrect_ShouldThrowCryptographicException()
    {
        // Arrange
        var sourcePair = RsaKeyGenerator.Generate(2048);
        var otherPair = RsaKeyGenerator.Generate(2048);
        var ciphertext = RsaEncryption.Encrypt(System.Text.Encoding.UTF8.GetBytes("secret"), sourcePair.PublicKeyPem);

        // Act
        var action = () => RsaEncryption.Decrypt(ciphertext, otherPair.PrivateKeyPem);

        // Assert
        action.ShouldThrow<CryptographicException>();
    }
}