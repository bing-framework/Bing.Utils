namespace Bing.Utils.Net.Tests.Integration;

/// <summary>
/// FTP 集成测试配置
/// </summary>
internal static class FtpIntegrationTestSettings
{
    public static string SkipReason
    {
        get
        {
            if (!string.Equals(Environment.GetEnvironmentVariable("RUN_INTEGRATION_TESTS"), "true", StringComparison.OrdinalIgnoreCase))
                return "需要设置 RUN_INTEGRATION_TESTS=true 才会执行 FTP 集成测试。";

            return TryGetConfiguration(out _, out var reason) ? null : reason;
        }
    }

    public static FtpIntegrationConfiguration GetConfiguration()
    {
        if (TryGetConfiguration(out var configuration, out var reason))
            return configuration;

        throw new InvalidOperationException(reason);
    }

    private static bool TryGetConfiguration(out FtpIntegrationConfiguration configuration, out string reason)
    {
        var host = Environment.GetEnvironmentVariable("BING_FTP_HOST");
        var userName = Environment.GetEnvironmentVariable("BING_FTP_USERNAME");
        var password = Environment.GetEnvironmentVariable("BING_FTP_PASSWORD");
        var portValue = Environment.GetEnvironmentVariable("BING_FTP_PORT");
        var port = 21;

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            configuration = default;
            reason = "需要设置 BING_FTP_HOST、BING_FTP_USERNAME 和 BING_FTP_PASSWORD 才会执行 FTP 集成测试。";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(portValue) && (!int.TryParse(portValue, out port) || port is < 1 or > 65535))
        {
            configuration = default;
            reason = "BING_FTP_PORT 必须为 1 到 65535 的整数。";
            return false;
        }

        configuration = new FtpIntegrationConfiguration(host, port, userName, password);
        reason = null;
        return true;
    }
}

internal readonly struct FtpIntegrationConfiguration
{
    public FtpIntegrationConfiguration(string host, int port, string userName, string password)
    {
        Host = host;
        Port = port;
        UserName = userName;
        Password = password;
    }

    public string Host { get; }

    public int Port { get; }

    public string UserName { get; }

    public string Password { get; }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class FtpIntegrationFactAttribute : FactAttribute
{
    public FtpIntegrationFactAttribute()
    {
        Skip = FtpIntegrationTestSettings.SkipReason;
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class FtpIntegrationTheoryAttribute : TheoryAttribute
{
    public FtpIntegrationTheoryAttribute()
    {
        Skip = FtpIntegrationTestSettings.SkipReason;
    }
}