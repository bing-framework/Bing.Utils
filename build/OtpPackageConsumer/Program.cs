using Bing.OneTimePassword;
using Bing.OneTimePassword.DataProtection;
using Bing.OneTimePassword.Redis;
using Bing.OneTimePassword.Sqlite;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

/// <summary>
/// 独立 NuGet 消费者，编译全部框架并在三个运行时验证持久化生命周期。
/// </summary>
public static class Program
{
    /// <summary>
    /// 只消费包内公开 API，不引用仓库项目。
    /// </summary>
    public static async Task Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        var root = Path.Combine(Path.GetTempPath(), "bing-otp-consumer-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            if (Hotp.Generate(Encoding.ASCII.GetBytes("12345678901234567890"), 0) != "755224") throw new Exception("RFC4226");
            var services = new ServiceCollection();
            services.AddBingOneTimePassword(new OtpServiceOptions { ApplicationId = "consumer" });
            services.AddOtpStore(_ => new SqliteOtpStore("Data Source=" + Path.Combine(root, "otp.db")));
            services.AddOtpSecretProtector(_ => new DataProtectionOtpSecretProtector(DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(root, "keys")))));
            services.AddMemoryOtpRateLimiter();
            using var provider = services.BuildServiceProvider();
            await ((SqliteOtpStore)provider.GetRequiredService<IOtpStore>()).InitializeAsync();
            var otp = provider.GetRequiredService<IOtpService>();
            var context = new OtpContext("consumer-subject", "manage", "trusted-session", DateTimeOffset.UtcNow);
            var enrollment = await otp.BeginEnrollmentAsync(context, new OtpParameters(OtpType.Hotp), "consumer@example.test");
            var parsed = OtpProvisioningUri.Parse(enrollment.ProvisioningUri);
            var secret = OtpSecretEncoding.Decode(parsed.SecretBase32);
            try
            {
                if (!(await otp.ConfirmEnrollmentAsync(context, enrollment.CredentialId, enrollment.TransactionId, Hotp.Generate(secret, 0))).Succeeded)
                    throw new Exception("confirmation");
                var login = new OtpContext("consumer-subject", "login", "trusted-session");
                var tx = await otp.BeginVerificationAsync(login);
                if (!(await otp.VerifyAsync(login, enrollment.CredentialId, tx.TransactionId, Hotp.Generate(secret, 1))).Succeeded)
                    throw new Exception("verification");
                if ((await otp.VerifyAsync(login, enrollment.CredentialId, tx.TransactionId, Hotp.Generate(secret, 1))).Succeeded)
                    throw new Exception("replay");
            }
            finally { Array.Clear(secret, 0, secret.Length); }
            // Redis 是可选连接，类型必须可以从最终包中加载；真实网络行为由专门集成测试验证。
            if (!typeof(IOtpRateLimiter).IsAssignableFrom(typeof(OtpRedisRateLimiter))) throw new Exception("Redis package contract");
            Console.WriteLine("OTP NuGet consumer: PASS");
        }
        finally { Directory.Delete(root, true); }
    }
}
