using Bing.OneTimePassword;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bing.Utils.OneTimePassword.DependencyInjection.Tests;

/// <summary>
/// 验证 OTP 服务注册的组件唯一性、配置快照和资源所有权。
/// </summary>
public sealed class RegistrationTests
{
    /// <summary>
    /// 验证缺少任一必需组件时解析 OTP 服务失败。
    /// </summary>
    /// <param name="missingRole">未注册的组件角色。</param>
    [Theory]
    [InlineData("store")]
    [InlineData("protector")]
    [InlineData("limiter")]
    public void ServiceResolution_WhenRequiredRoleIsMissing_ShouldThrow(string missingRole)
    {
        var services = new ServiceCollection();
        services.AddBingOneTimePassword();
        RegisterComponents(services, missingRole);

        using var provider = services.BuildServiceProvider();
        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IOtpService>());
    }

    /// <summary>
    /// 验证绕过注册扩展重复添加的组件在解析服务时被拒绝。
    /// </summary>
    /// <param name="duplicateRole">重复注册的组件角色。</param>
    [Theory]
    [InlineData("store")]
    [InlineData("protector")]
    [InlineData("limiter")]
    public void ServiceResolution_WhenRequiredRoleIsDuplicated_ShouldThrow(string duplicateRole)
    {
        var services = new ServiceCollection();
        services.AddBingOneTimePassword();
        RegisterComponents(services, duplicateRole: duplicateRole);

        using var provider = services.BuildServiceProvider();
        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IOtpService>());
    }

    /// <summary>
    /// 验证注册扩展会立即拒绝同一组件角色的重复选择。
    /// </summary>
    [Fact]
    public void ComponentRegistration_WhenSameRoleIsAddedTwice_ShouldThrow()
    {
        var services = new ServiceCollection();
        services.AddOtpStore(_ => new RecordingStore());

        Assert.Throws<InvalidOperationException>(() => services.AddOtpStore(_ => new RecordingStore()));
    }

    /// <summary>
    /// 验证注册时复制应用标识、发行者、用途列表和生命周期配置。
    /// </summary>
    [Fact]
    public async Task ServiceResolution_WhenOptionsChangeAfterRegistration_ShouldUseSnapshot()
    {
        var clock = new FixedClock(DateTimeOffset.FromUnixTimeSeconds(1_700_000_000));
        var store = new RecordingStore();
        var protector = new RecordingProtector();
        var limiter = new RecordingRateLimiter();
        var options = new OtpServiceOptions
        {
            ApplicationId = "application-before",
            Issuer = "issuer-before",
            AllowedPurposes = new[] { "login" },
            ManagementPurpose = "login",
            EnrollmentLifetime = TimeSpan.FromMinutes(2)
        };
        var services = new ServiceCollection();
        services.AddSingleton<IOtpClock>(clock);
        services.AddOtpStore(_ => store);
        services.AddOtpSecretProtector(_ => protector);
        services.AddOtpRateLimiter(_ => limiter);
        services.AddBingOneTimePassword(options);

        options.ApplicationId = "application-after";
        options.Issuer = "issuer-after";
        options.AllowedPurposes[0] = "step-up";
        options.EnrollmentLifetime = TimeSpan.FromMinutes(3);

        using var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<IOtpService>();
        var context = new OtpContext("subject-1", "login", "session-1", authenticatedAt: clock.UtcNow, clientAddress: "127.0.0.1");
        var enrollment = await service.BeginEnrollmentAsync(context, new OtpParameters(), "alice@example.test");

        Assert.Contains("issuer=issuer-before", enrollment.ProvisioningUri);
        Assert.DoesNotContain("issuer-after", enrollment.ProvisioningUri);
        Assert.Equal("application-before", Assert.IsType<OtpSecretContext>(protector.LastContext).ApplicationId);
        Assert.Equal(clock.UtcNow.AddMinutes(2), enrollment.ExpiresAt);
        Assert.NotEmpty(limiter.Partitions);
        Assert.All(limiter.Partitions, partition => Assert.StartsWith("application-before:", partition, StringComparison.Ordinal));
    }

    /// <summary>
    /// 验证通过工厂创建的可释放组件由服务容器负责释放。
    /// </summary>
    [Fact]
    public void ServiceProvider_WhenDisposed_ShouldDisposeFactoryOwnedComponents()
    {
        var store = new RecordingStore();
        var protector = new RecordingProtector();
        var limiter = new RecordingRateLimiter();
        var services = new ServiceCollection();
        services.AddOtpStore(_ => store);
        services.AddOtpSecretProtector(_ => protector);
        services.AddOtpRateLimiter(_ => limiter);
        services.AddBingOneTimePassword();

        var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IOtpService>();
        provider.Dispose();

        Assert.True(store.Disposed);
        Assert.True(protector.Disposed);
        Assert.True(limiter.Disposed);
    }

    /// <summary>
    /// 验证直接注册的宿主实例在服务容器释放时仍由宿主持有。
    /// </summary>
    [Fact]
    public void ServiceProvider_WhenDisposed_ShouldLeaveHostInstancesOwnedByHost()
    {
        var store = new RecordingStore();
        var protector = new RecordingProtector();
        var limiter = new RecordingRateLimiter();
        var services = new ServiceCollection().AddBingOneTimePassword();
        services.AddSingleton<IOtpStore>(store);
        services.AddSingleton<IOtpSecretProtector>(protector);
        services.AddSingleton<IOtpRateLimiter>(limiter);

        using (var provider = services.BuildServiceProvider())
            provider.GetRequiredService<IOtpService>();

        Assert.False(store.Disposed);
        Assert.False(protector.Disposed);
        Assert.False(limiter.Disposed);
    }

    private static void RegisterComponents(IServiceCollection services, string? missingRole = null, string? duplicateRole = null)
    {
        if (missingRole != "store")
        {
            services.AddOtpStore(_ => new RecordingStore());
            if (duplicateRole == "store")
                services.AddSingleton<IOtpStore>(_ => new RecordingStore());
        }
        if (missingRole != "protector")
        {
            services.AddOtpSecretProtector(_ => new RecordingProtector());
            if (duplicateRole == "protector")
                services.AddSingleton<IOtpSecretProtector>(_ => new RecordingProtector());
        }
        if (missingRole != "limiter")
        {
            services.AddOtpRateLimiter(_ => new RecordingRateLimiter());
            if (duplicateRole == "limiter")
                services.AddSingleton<IOtpRateLimiter>(_ => new RecordingRateLimiter());
        }
    }

    private sealed class RecordingStore : IOtpStore, IDisposable
    {
        private readonly Dictionary<string, OtpAccountState> _accounts = new(StringComparer.Ordinal);

        public bool Disposed { get; private set; }

        public Task<T> ExecuteAsync<T>(string subjectId, Func<OtpAccountState, T> operation, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_accounts.TryGetValue(subjectId, out var state))
            {
                state = new OtpAccountState(subjectId);
                _accounts.Add(subjectId, state);
            }
            return Task.FromResult(operation(state));
        }

        public Task<int> CleanupAsync(DateTimeOffset now, int maxRecords = 256, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(0);
        }

        public void Dispose() => Disposed = true;
    }

    private sealed class RecordingProtector : IOtpSecretProtector, IDisposable
    {
        public OtpSecretContext? LastContext { get; private set; }
        public bool Disposed { get; private set; }

        public byte[] Protect(OtpSecretContext context, byte[] secret)
        {
            LastContext = context;
            return secret.ToArray();
        }

        public byte[] Unprotect(OtpSecretContext context, byte[] protectedSecret) => protectedSecret.ToArray();

        public void Dispose() => Disposed = true;
    }

    private sealed class RecordingRateLimiter : IOtpRateLimiter, IDisposable
    {
        public List<string> Partitions { get; } = new();
        public bool Disposed { get; private set; }

        public Task<bool> TryAcquireAsync(string partition, int limit, TimeSpan window, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Partitions.Add(partition);
            return Task.FromResult(true);
        }

        public void Dispose() => Disposed = true;
    }

    private sealed class FixedClock : IOtpClock
    {
        public FixedClock(DateTimeOffset utcNow) => UtcNow = utcNow;
        public DateTimeOffset UtcNow { get; }
    }
}
