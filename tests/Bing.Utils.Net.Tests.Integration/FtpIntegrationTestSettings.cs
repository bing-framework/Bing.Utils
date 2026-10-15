namespace Bing.Utils.Net.Tests.Integration;

/// <summary>
/// 管理 FTP 集成测试的环境配置和跳过条件。
/// </summary>
internal static class FtpIntegrationTestSettings
{
    /// <summary>
    /// 获取 FTP 集成测试因开关或连接配置不满足条件时的跳过原因。
    /// </summary>
    /// <remarks>设置 RUN_INTEGRATION_TESTS=true 并提供所需 FTP 环境变量后返回 null。</remarks>
    public static string SkipReason
    {
        get
        {
            if (!string.Equals(Environment.GetEnvironmentVariable("RUN_INTEGRATION_TESTS"), "true", StringComparison.OrdinalIgnoreCase))
                return "需要设置 RUN_INTEGRATION_TESTS=true 才会执行 FTP 集成测试。";

            return TryGetConfiguration(out _, out var reason) ? null : reason;
        }
    }

    /// <summary>
    /// 获取由 FTP 集成测试环境变量构造的连接配置。
    /// </summary>
    /// <returns>包含主机、端口、用户名和密码的 FTP 配置。</returns>
    /// <exception cref="InvalidOperationException">所需环境变量缺失或端口格式无效时抛出。</exception>
    public static FtpIntegrationConfiguration GetConfiguration()
    {
        if (TryGetConfiguration(out var configuration, out var reason))
            return configuration;

        throw new InvalidOperationException(reason);
    }

    /// <summary>
    /// 尝试读取并验证 FTP 集成测试连接配置。
    /// </summary>
    /// <param name="configuration">验证成功时接收连接配置；失败时为默认值。</param>
    /// <param name="reason">验证失败时接收可读原因；成功时为 null。</param>
    /// <returns>配置有效时返回 true；缺少必需值或端口无效时返回 false。</returns>
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

/// <summary>
/// 保存 FTP 集成测试建立连接所需的配置。
/// </summary>
internal readonly struct FtpIntegrationConfiguration
{
    /// <summary>
    /// 初始化一个 <see cref="FtpIntegrationConfiguration"/> 类型的实例。
    /// </summary>
    /// <param name="host">FTP 服务器主机名或地址。</param>
    /// <param name="port">FTP 服务器端口。</param>
    /// <param name="userName">连接使用的用户名。</param>
    /// <param name="password">连接使用的密码。</param>
    public FtpIntegrationConfiguration(string host, int port, string userName, string password)
    {
        Host = host;
        Port = port;
        UserName = userName;
        Password = password;
    }

    /// <summary>
    /// 获取 FTP 服务器主机名或地址。
    /// </summary>
    public string Host { get; }

    /// <summary>
    /// 获取 FTP 服务器连接端口。
    /// </summary>
    public int Port { get; }

    /// <summary>
    /// 获取建立 FTP 连接使用的用户名。
    /// </summary>
    public string UserName { get; }

    /// <summary>
    /// 获取建立 FTP 连接使用的密码。
    /// </summary>
    public string Password { get; }
}

/// <summary>
/// 根据 FTP 集成环境配置决定是否运行单个测试的方法特性。
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class FtpIntegrationFactAttribute : FactAttribute
{
    /// <summary>
    /// 初始化一个 <see cref="FtpIntegrationFactAttribute"/> 类型的实例。
    /// </summary>
    public FtpIntegrationFactAttribute()
    {
        Skip = FtpIntegrationTestSettings.SkipReason;
    }
}

/// <summary>
/// 根据 FTP 集成环境配置决定是否运行参数化测试的特性。
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class FtpIntegrationTheoryAttribute : TheoryAttribute
{
    /// <summary>
    /// 初始化一个 <see cref="FtpIntegrationTheoryAttribute"/> 类型的实例。
    /// </summary>
    public FtpIntegrationTheoryAttribute()
    {
        Skip = FtpIntegrationTestSettings.SkipReason;
    }
}