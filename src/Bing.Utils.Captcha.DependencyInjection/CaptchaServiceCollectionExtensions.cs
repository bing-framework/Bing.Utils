using Bing.Captcha;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>验证码服务注册扩展；实例由容器管理，外部实例仍由宿主管理。</summary>
public static class CaptchaServiceCollectionExtensions
{
    /// <summary>注册唯一验证码服务；存储、渲染器、密钥提供器必须在解析前显式注册。</summary>
    /// <param name="services">宿主服务集合。</param>
    /// <param name="options">构造时验证并复制的配置。</param>
    /// <returns>原服务集合。</returns>
    public static IServiceCollection AddBingCaptcha(this IServiceCollection services, CaptchaServiceOptions? options = null)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (services.Any(d => d.ServiceType == typeof(ICaptchaService))) throw new InvalidOperationException("验证码服务已注册。");
        var snapshot = (options ?? new CaptchaServiceOptions()).Snapshot();
        services.TryAddSingleton<ICaptchaClock, SystemCaptchaClock>();
        services.AddSingleton<ICaptchaService>(sp =>
        {
            var stores = sp.GetServices<ICaptchaStore>().ToArray();
            var renderers = sp.GetServices<ICaptchaRenderer>().ToArray();
            var keys = sp.GetServices<ICaptchaKeyProvider>().ToArray();
            if (stores.Length != 1 || renderers.Length != 1 || keys.Length != 1) throw new InvalidOperationException("必须各注册一个存储、渲染器和密钥提供器。");
            return new CaptchaService(stores[0], renderers[0], keys[0], snapshot, sp.GetRequiredService<ICaptchaClock>());
        });
        return services;
    }
    /// <summary>注册容器拥有的内存存储；只能选择一个存储。</summary>
    /// <param name="services">宿主服务集合。</param>
    /// <param name="options">复制后用于创建单例存储的配置。</param>
    /// <returns>原服务集合。</returns>
    public static IServiceCollection AddMemoryCaptchaStore(this IServiceCollection services, MemoryCaptchaStoreOptions? options = null)
    {
        options ??= new MemoryCaptchaStoreOptions();
        var snapshot = new MemoryCaptchaStoreOptions { Capacity = options.Capacity, CleanupInterval = options.CleanupInterval, CleanupBatchSize = options.CleanupBatchSize };
        return services.AddCaptchaStore(sp => new MemoryCaptchaStore(snapshot, sp.GetService<ICaptchaClock>()));
    }
    /// <summary>注册容器拥有的自定义存储，不引入具体适配器依赖。</summary>
    /// <param name="services">宿主服务集合。</param>
    /// <param name="factory">只创建单例存储的工厂；不得捕获 scoped 服务。</param>
    /// <returns>原服务集合。</returns>
    public static IServiceCollection AddCaptchaStore(this IServiceCollection services, Func<IServiceProvider, ICaptchaStore> factory)
    {
        EnsureUnique<ICaptchaStore>(services, factory); services.AddSingleton(factory); return services;
    }
    /// <summary>注册容器拥有的自定义渲染器。</summary>
    /// <param name="services">宿主服务集合。</param>
    /// <param name="factory">只创建单例渲染器的工厂；容器管理其释放。</param>
    /// <returns>原服务集合。</returns>
    public static IServiceCollection AddCaptchaRenderer(this IServiceCollection services, Func<IServiceProvider, ICaptchaRenderer> factory)
    {
        EnsureUnique<ICaptchaRenderer>(services, factory); services.AddSingleton(factory); return services;
    }
    private static void EnsureUnique<T>(IServiceCollection services, object factory)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (factory == null) throw new ArgumentNullException(nameof(factory));
        if (services.Any(d => d.ServiceType == typeof(T))) throw new InvalidOperationException("已选择此验证码组件。");
    }
}
