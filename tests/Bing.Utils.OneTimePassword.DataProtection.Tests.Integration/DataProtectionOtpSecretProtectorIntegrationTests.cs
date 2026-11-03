using System.Security.Cryptography;
using Bing.OneTimePassword;
using Bing.OneTimePassword.DataProtection;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bing.Utils.OneTimePassword.DataProtection.Tests.Integration;

/// <summary>
/// 使用临时文件密钥环验证 OTP Data Protection 绑定和持久化行为。
/// </summary>
public sealed class DataProtectionOtpSecretProtectorIntegrationTests
{
    /// <summary>
    /// 验证密钥往返解密，并绑定应用、账号、凭据及算法参数上下文。
    /// </summary>
    [Fact]
    public void ProtectAndUnprotect_WhenContextChanges_ShouldRejectCiphertext()
    {
        var directory = CreateTemporaryKeyDirectory();
        var applicationName = "otp-protector-" + Guid.NewGuid().ToString("N");
        var context = CreateContext();
        var secret = RandomNumberGenerator.GetBytes(32);
        try
        {
            using var provider = CreateProvider(directory, applicationName);
            var protector = new DataProtectionOtpSecretProtector(provider.GetRequiredService<IDataProtectionProvider>());
            var ciphertext = protector.Protect(context, secret);
            try
            {
                var plaintext = protector.Unprotect(context, ciphertext);
                try
                {
                    Assert.Equal(secret, plaintext);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(plaintext);
                }

                var changedContexts = new[]
                {
                    CreateContext(applicationId: "other-app"),
                    CreateContext(subjectId: "other-subject"),
                    CreateContext(credentialId: "other-credential"),
                    CreateContext(type: OtpType.Hotp),
                    CreateContext(algorithm: OtpHashAlgorithm.Sha256),
                    CreateContext(digits: 8),
                    CreateContext(periodSeconds: 60)
                };
                foreach (var changedContext in changedContexts)
                    Assert.Throws<OtpProtectionException>(() => protector.Unprotect(changedContext, ciphertext));
            }
            finally
            {
                CryptographicOperations.ZeroMemory(ciphertext);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(secret);
            DeleteTemporaryKeyDirectory(directory);
        }
    }

    /// <summary>
    /// 验证密文篡改、跨凭据交换和未知密钥环均不能解密密钥。
    /// </summary>
    [Fact]
    public void Unprotect_WhenCiphertextIsChangedOrKeyIsUnknown_ShouldThrow()
    {
        var directory = CreateTemporaryKeyDirectory();
        var unknownDirectory = CreateTemporaryKeyDirectory();
        var applicationName = "otp-protector-" + Guid.NewGuid().ToString("N");
        var contextA = CreateContext(credentialId: "credential-a");
        var contextB = CreateContext(credentialId: "credential-b");
        var secretA = RandomNumberGenerator.GetBytes(32);
        var secretB = RandomNumberGenerator.GetBytes(32);
        try
        {
            using var provider = CreateProvider(directory, applicationName);
            var protector = new DataProtectionOtpSecretProtector(provider.GetRequiredService<IDataProtectionProvider>());
            var ciphertextA = protector.Protect(contextA, secretA);
            var ciphertextB = protector.Protect(contextB, secretB);
            var tampered = ciphertextA.ToArray();
            tampered[tampered.Length / 2] ^= 0x40;
            try
            {
                Assert.Throws<OtpProtectionException>(() => protector.Unprotect(contextA, tampered));
                Assert.Throws<OtpProtectionException>(() => protector.Unprotect(contextA, ciphertextB));

                using var unknownProvider = CreateProvider(unknownDirectory, applicationName);
                var unknownProtector = new DataProtectionOtpSecretProtector(unknownProvider.GetRequiredService<IDataProtectionProvider>());
                Assert.Throws<OtpProtectionException>(() => unknownProtector.Unprotect(contextA, ciphertextA));
            }
            finally
            {
                CryptographicOperations.ZeroMemory(ciphertextA);
                CryptographicOperations.ZeroMemory(ciphertextB);
                CryptographicOperations.ZeroMemory(tampered);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(secretA);
            CryptographicOperations.ZeroMemory(secretB);
            DeleteTemporaryKeyDirectory(directory);
            DeleteTemporaryKeyDirectory(unknownDirectory);
        }
    }

    /// <summary>
    /// 验证创建新密钥后仍可解密旧凭据密文。
    /// </summary>
    [Fact]
    public void Unprotect_AfterCreatingNewKey_ShouldStillReadOldCiphertext()
    {
        var directory = CreateTemporaryKeyDirectory();
        var applicationName = "otp-protector-" + Guid.NewGuid().ToString("N");
        var context = CreateContext();
        var secret = RandomNumberGenerator.GetBytes(32);
        try
        {
            using var provider = CreateProvider(directory, applicationName);
            var protector = new DataProtectionOtpSecretProtector(provider.GetRequiredService<IDataProtectionProvider>());
            var ciphertext = protector.Protect(context, secret);
            try
            {
                var now = DateTimeOffset.UtcNow;
                provider.GetRequiredService<IKeyManager>().CreateNewKey(now.AddMinutes(-1), now.AddDays(90));

                var plaintext = protector.Unprotect(context, ciphertext);
                try
                {
                    Assert.Equal(secret, plaintext);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(plaintext);
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(ciphertext);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(secret);
            DeleteTemporaryKeyDirectory(directory);
        }
    }

    /// <summary>
    /// 验证重建 Data Protection 提供器后可从持久密钥环解密旧密文。
    /// </summary>
    [Fact]
    public void Unprotect_AfterProviderRebuild_ShouldReadPersistedKeyRing()
    {
        var directory = CreateTemporaryKeyDirectory();
        var applicationName = "otp-protector-" + Guid.NewGuid().ToString("N");
        var context = CreateContext();
        var secret = RandomNumberGenerator.GetBytes(32);
        byte[] ciphertext;
        try
        {
            using (var firstProvider = CreateProvider(directory, applicationName))
            {
                var protector = new DataProtectionOtpSecretProtector(firstProvider.GetRequiredService<IDataProtectionProvider>());
                ciphertext = protector.Protect(context, secret);
            }

            using (var rebuiltProvider = CreateProvider(directory, applicationName))
            {
                var protector = new DataProtectionOtpSecretProtector(rebuiltProvider.GetRequiredService<IDataProtectionProvider>());
                var plaintext = protector.Unprotect(context, ciphertext);
                try
                {
                    Assert.Equal(secret, plaintext);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(plaintext);
                }
            }
            CryptographicOperations.ZeroMemory(ciphertext);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(secret);
            DeleteTemporaryKeyDirectory(directory);
        }
    }

    private static ServiceProvider CreateProvider(string directory, string applicationName)
    {
        var services = new ServiceCollection();
        // 测试密钥环只写入临时目录，不配置生产环境的密钥加密。
        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(directory))
            .SetApplicationName(applicationName);
        return services.BuildServiceProvider();
    }

    private static string CreateTemporaryKeyDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "bing-otp-dp-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void DeleteTemporaryKeyDirectory(string directory)
    {
        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }

    private static OtpSecretContext CreateContext(
        string applicationId = "app-1",
        string subjectId = "subject-1",
        string credentialId = "credential-1",
        OtpType type = OtpType.Totp,
        OtpHashAlgorithm algorithm = OtpHashAlgorithm.Sha1,
        int digits = 6,
        int periodSeconds = 30) =>
        new(applicationId, subjectId, credentialId, new OtpParameters(type, algorithm, digits, periodSeconds));
}
