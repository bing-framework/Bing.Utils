using System.Security.Cryptography;
using System.Text;
using Bing.Encryption;
using Bing.Security;
using Bing.Security.Asymmetric;
using Bing.Security.Hashing;
using Bing.Security.Passwords;
using Bing.Security.Symmetric;
using Shouldly;
using Xunit;

namespace Bing.Utils.Security.Tests;

public class SecurityTests
{
    [Fact]
    public void Base64Url_Should_RoundTrip()
    {
        var value = Encoding.UTF8.GetBytes("Bing.Utils.Security/中文");
        SecurityEncoding.FromBase64Url(SecurityEncoding.ToBase64Url(value)).ShouldBe(value);
    }

    [Fact]
    public void Sha256_Should_Match_Known_Vector() =>
        HashingProvider.ComputeHex("abc").ShouldBe("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad");

    [Fact]
    public void AesCipher_Should_RoundTrip_And_Detect_Tampering()
    {
        var key = AesCipher.GenerateKey();
        var envelope = AesCipher.Encrypt("hello security", key);
        AesCipher.Decrypt(envelope, key).ShouldBe("hello security");

        var parsed = AesEncryptionEnvelope.Parse(envelope);
        parsed.CipherText[0] ^= 1;
        Should.Throw<CryptographicException>(() => AesCipher.Decrypt(parsed, key));
    }

    [Fact]
    public void PasswordHasher_Should_Verify()
    {
        var hash = Pbkdf2PasswordHasher.Hash("P@ssw0rd!", 10000);
        Pbkdf2PasswordHasher.Verify("P@ssw0rd!", hash).ShouldBeTrue();
        Pbkdf2PasswordHasher.Verify("wrong", hash).ShouldBeFalse();
    }

    [Fact]
    public void RsaCipher_Should_Encrypt_And_Sign()
    {
        var keys = RsaCipher.CreateKeyPair(2048);
        var source = Encoding.UTF8.GetBytes("rsa-value");
        RsaCipher.Decrypt(RsaCipher.Encrypt(source, keys.PublicKey), keys.PrivateKey).ShouldBe(source);
        RsaCipher.Verify(source, RsaCipher.Sign(source, keys.PrivateKey), keys.PublicKey).ShouldBeTrue();
    }

    [Fact]
    public void Legacy_Aes_Should_RoundTrip()
    {
#pragma warning disable CS0618
        const string key = "12345678901234567890123456789012";
        const string iv = "1234567890123456";
        var cipher = AESEncryptionProvider.Encrypt("legacy-aes", key, iv);
        AESEncryptionProvider.Decrypt(cipher, key, iv).ShouldBe("legacy-aes");
#pragma warning restore CS0618
    }
}
