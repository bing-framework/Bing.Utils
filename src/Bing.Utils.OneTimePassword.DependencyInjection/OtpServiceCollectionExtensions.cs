using Bing.OneTimePassword;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// OTP 服务注册扩展，具体存储和保护适配器必须显式选择。
/// </summary>
public static class OtpServiceCollectionExtensions
{
    /// <summary>
    /// 注册唯一服务并复制配置；解析服务时验证组件注册。
    /// </summary>
    /// <param name="services">目标依赖注入服务集合。</param>
    /// <param name="options">可选生命周期配置；注册时复制并验证，后续修改原对象不会改变服务快照。</param>
    /// <returns>原服务集合。</returns>
    public static IServiceCollection AddBingOneTimePassword(this IServiceCollection services, OtpServiceOptions? options = null)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (services.Any(x => x.ServiceType == typeof(IOtpService))) throw new InvalidOperationException("OTP 服务已注册。");
        var snapshot = (options ?? new OtpServiceOptions()).Snapshot();
        services.TryAddSingleton<IOtpClock, SystemOtpClock>();
        services.AddSingleton<IOtpService>(sp => new OtpService(Unique<IOtpStore>(sp), Unique<IOtpSecretProtector>(sp), Unique<IOtpRateLimiter>(sp), snapshot, Unique<IOtpClock>(sp)));
        return services;
    }

    /// <summary>
    /// 注册容器拥有的唯一存储。
    /// </summary>
    /// <param name="services">目标依赖注入服务集合。</param>
    /// <param name="factory">使用服务提供器创建单例存储的工厂；容器管理返回实例，并在其实现 <see cref="IDisposable"/> 或 <see cref="IAsyncDisposable"/> 时负责释放。</param>
    /// <returns>原服务集合。</returns>
    public static IServiceCollection AddOtpStore(this IServiceCollection services, Func<IServiceProvider, IOtpStore> factory) => AddUnique(services, factory);

    /// <summary>
    /// 注册容器拥有的唯一密钥保护器。
    /// </summary>
    /// <param name="services">目标依赖注入服务集合。</param>
    /// <param name="factory">使用服务提供器创建单例密钥保护器的工厂；容器管理返回实例，并在其实现 <see cref="IDisposable"/> 或 <see cref="IAsyncDisposable"/> 时负责释放。</param>
    /// <returns>原服务集合。</returns>
    public static IServiceCollection AddOtpSecretProtector(this IServiceCollection services, Func<IServiceProvider, IOtpSecretProtector> factory) => AddUnique(services, factory);

    /// <summary>
    /// 注册容器拥有的唯一限流器。
    /// </summary>
    /// <param name="services">目标依赖注入服务集合。</param>
    /// <param name="factory">使用服务提供器创建单例限流器的工厂；容器管理返回实例，并在其实现 <see cref="IDisposable"/> 或 <see cref="IAsyncDisposable"/> 时负责释放。</param>
    /// <returns>原服务集合。</returns>
    public static IServiceCollection AddOtpRateLimiter(this IServiceCollection services, Func<IServiceProvider, IOtpRateLimiter> factory) => AddUnique(services, factory);

    /// <summary>
    /// 显式选择有界单实例限流，不是 Redis 故障回退。
    /// </summary>
    /// <param name="services">目标依赖注入服务集合。</param>
    /// <param name="capacity">内存中最多保留的限流身份数，范围为 1 至 1000000；容量耗尽时拒绝新身份。</param>
    /// <returns>原服务集合。</returns>
    public static IServiceCollection AddMemoryOtpRateLimiter(this IServiceCollection services, int capacity = 10000)
        => services.AddOtpRateLimiter(sp => new MemoryOtpRateLimiter(capacity, sp.GetService<IOtpClock>()));

    /// <summary>
    /// 拒绝同角色重复注册。
    /// </summary>
    /// <typeparam name="T">本次注册的组件接口或服务类型。</typeparam>
    /// <param name="services">目标依赖注入服务集合。</param>
    /// <param name="factory">创建单例组件的工厂。</param>
    /// <returns>添加该组件注册后的原服务集合。</returns>
    private static IServiceCollection AddUnique<T>(IServiceCollection services, Func<IServiceProvider, T> factory) where T : class
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (factory == null) throw new ArgumentNullException(nameof(factory));
        if (services.Any(x => x.ServiceType == typeof(T))) throw new InvalidOperationException("OTP 组件已经选择。");
        services.AddSingleton(factory);
        return services;
    }

    /// <summary>
    /// 解析唯一实例，防止外部注册绕过组件约束。
    /// </summary>
    /// <typeparam name="T">要从容器中唯一解析的组件类型。</typeparam>
    /// <param name="services">用于解析组件的依赖注入服务提供器。</param>
    /// <returns>已注册的唯一组件实例。</returns>
    private static T Unique<T>(IServiceProvider services)
    {
        var instances = services.GetServices<T>().ToArray();
        if (instances.Length != 1 || instances[0] == null) throw new InvalidOperationException("OTP 每种组件必须恰好注册一个。");
        return instances[0];
    }
}
