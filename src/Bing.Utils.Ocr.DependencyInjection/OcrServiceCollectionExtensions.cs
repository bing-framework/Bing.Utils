using Bing.Ocr;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>OCR 注册扩展；工厂创建实例由容器管理，外部实例由调用方管理。</summary>
public static class OcrServiceCollectionExtensions
{
    /// <summary>注册共享 OCR 服务及不可变配置快照；组件须在解析前各注册一个。</summary>
    public static IServiceCollection AddBingOcr(this IServiceCollection services, OcrServiceOptions? options = null)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        EnsureUnique<IOcrService>(services);
        var snapshot = (options ?? new OcrServiceOptions()).Snapshot();
        services.AddSingleton<IOcrService>(sp =>
        {
            var providers = sp.GetServices<IOcrProvider>().ToArray();
            var preprocessors = sp.GetServices<IOcrImagePreprocessor>().ToArray();
            if (providers.Length != 1 || preprocessors.Length != 1)
                throw new InvalidOperationException("必须各注册一个 OCR Provider 和图像预处理器。");
            return new OcrService(providers[0], preprocessors[0], snapshot);
        });
        return services;
    }
    /// <summary>注册容器拥有的共享引擎；工厂不得捕获 scoped 服务。</summary>
    public static IServiceCollection AddOcrProvider(this IServiceCollection services, Func<IServiceProvider, IOcrProvider> factory)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (factory == null) throw new ArgumentNullException(nameof(factory));
        EnsureUnique<IOcrProvider>(services); services.AddSingleton(factory); return services;
    }
    /// <summary>注册容器拥有的共享预处理器。</summary>
    public static IServiceCollection AddOcrImagePreprocessor(this IServiceCollection services, Func<IServiceProvider, IOcrImagePreprocessor> factory)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (factory == null) throw new ArgumentNullException(nameof(factory));
        EnsureUnique<IOcrImagePreprocessor>(services); services.AddSingleton(factory); return services;
    }
    private static void EnsureUnique<T>(IServiceCollection services)
    {
        if (services.Any(x => x.ServiceType == typeof(T))) throw new InvalidOperationException("OCR 组件已注册。");
    }
}
