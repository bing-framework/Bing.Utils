using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bing.DependencyInjection;
#if NET6_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>面向启动阶段的显式接口批量注册；实例创建、解析和释放由容器负责。</summary>
/// <remarks>调用期间不得并发修改目标集合。反射所需接口与构造函数元数据须由调用者保留，不保证任意扫描兼容裁剪或 NativeAOT。</remarks>
public static class BingServiceCollectionExtensions
{
    /// <summary>注册指定接口的实现；默认跳过重复组合，保留已有生命周期。</summary>
    /// <typeparam name="TService">显式选择的服务接口。</typeparam>
    /// <param name="services">目标服务集合。</param>
    /// <param name="implementationTypes">实现类型集合，空集合不注册。</param>
    /// <param name="lifetime">本批新增描述符的生命周期。</param>
    /// <param name="strategy">重复组合的处理策略。</param>
    /// <returns>原服务集合。</returns>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("反射检查实现接口，且容器需要实现构造函数；请保留这些类型的元数据。")]
#endif
    public static IServiceCollection AddImplementations<TService>(this IServiceCollection services,
        IEnumerable<Type> implementationTypes, ServiceLifetime lifetime,
        ServiceRegistrationStrategy strategy = ServiceRegistrationStrategy.SkipDuplicates) =>
        services.AddImplementations(typeof(TService), implementationTypes, lifetime, strategy);

    /// <summary>注册运行时指定接口的实现；开放泛型仅支持参数原序一一对应的投影。</summary>
    /// <param name="services">目标服务集合。</param>
    /// <param name="serviceType">闭合接口或开放泛型接口定义。</param>
    /// <param name="implementationTypes">非抽象实现类集合。</param>
    /// <param name="lifetime">本批新增描述符的生命周期。</param>
    /// <param name="strategy">重复组合的处理策略。</param>
    /// <returns>原服务集合；参数和冲突失败时不写入本批描述符。</returns>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("反射检查实现接口，且容器需要实现构造函数；请保留这些类型的元数据。")]
#endif
    public static IServiceCollection AddImplementations(this IServiceCollection services, Type serviceType,
        IEnumerable<Type> implementationTypes, ServiceLifetime lifetime,
        ServiceRegistrationStrategy strategy = ServiceRegistrationStrategy.SkipDuplicates)
    {
        ServiceRegistrationBuilder.Validate(services, lifetime, strategy);
        ServiceRegistrationBuilder.ValidateService(serviceType);
        var types = ServiceRegistrationBuilder.Materialize(implementationTypes, nameof(implementationTypes));
        return ServiceRegistrationBuilder.Apply(services,
            types.Select(type => ServiceRegistrationBuilder.Describe(serviceType, type, lifetime)), strategy);
    }

    /// <summary>从明确提供的程序集扫描指定接口的公开非抽象实现。</summary>
    /// <typeparam name="TService">显式选择的服务接口。</typeparam>
    /// <param name="services">目标服务集合。</param>
    /// <param name="assemblies">扫描范围；重复程序集只扫描一次。</param>
    /// <param name="lifetime">本批新增描述符的生命周期。</param>
    /// <param name="strategy">重复组合的处理策略。</param>
    /// <param name="predicate">可选类型过滤器；异常直接向调用者传播。</param>
    /// <returns>原服务集合。</returns>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("程序集扫描需要保留被扫描类型、接口和构造函数，无法静态分析任意程序集。")]
#endif
    public static IServiceCollection ScanImplementations<TService>(this IServiceCollection services,
        IEnumerable<Assembly> assemblies, ServiceLifetime lifetime,
        ServiceRegistrationStrategy strategy = ServiceRegistrationStrategy.SkipDuplicates, Func<Type, bool> predicate = null) =>
        services.ScanImplementations(typeof(TService), assemblies, lifetime, strategy, predicate);

    /// <summary>扫描运行时指定接口；按程序集输入顺序及各程序集类型全名的序数顺序注册。</summary>
    /// <param name="services">目标服务集合。</param>
    /// <param name="serviceType">闭合接口或标准开放泛型接口定义。</param>
    /// <param name="assemblies">明确提供的扫描范围，不自动加载程序集。</param>
    /// <param name="lifetime">本批新增描述符的生命周期。</param>
    /// <param name="strategy">重复组合的处理策略。</param>
    /// <param name="predicate">可选类型过滤器。</param>
    /// <returns>原服务集合；扫描或类型校验失败时不写入本批描述符。</returns>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("程序集扫描需要保留被扫描类型、接口和构造函数，无法静态分析任意程序集。")]
#endif
    public static IServiceCollection ScanImplementations(this IServiceCollection services, Type serviceType,
        IEnumerable<Assembly> assemblies, ServiceLifetime lifetime,
        ServiceRegistrationStrategy strategy = ServiceRegistrationStrategy.SkipDuplicates, Func<Type, bool> predicate = null)
    {
        ServiceRegistrationBuilder.Validate(services, lifetime, strategy);
        ServiceRegistrationBuilder.ValidateService(serviceType);
        var candidates = ServiceRegistrationBuilder.Scan(assemblies, predicate)
            .Where(type => ServiceRegistrationBuilder.Implements(serviceType, type)).ToArray();
        return services.AddImplementations(serviceType, candidates, lifetime, strategy);
    }

    /// <summary>为有限的闭合泛型接口集合提前推导并注册实现，支持重排、嵌套、固定参数和数组。</summary>
    /// <param name="services">目标服务集合。</param>
    /// <param name="closedServiceTypes">明确列出的闭合泛型接口，不进行解析时动态推导。</param>
    /// <param name="implementationTypes">闭合实现类或开放泛型类定义。</param>
    /// <param name="lifetime">本批新增描述符的生命周期。</param>
    /// <param name="strategy">重复组合的处理策略。</param>
    /// <returns>原服务集合；无有效实现或推导歧义时整批失败。</returns>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("泛型推导需要实现接口、泛型约束及构造函数元数据。")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("MakeGenericType 推导的闭合实现可能没有预编译本机代码。")]
#endif
    public static IServiceCollection AddClosedGenericImplementations(this IServiceCollection services,
        IEnumerable<Type> closedServiceTypes, IEnumerable<Type> implementationTypes, ServiceLifetime lifetime,
        ServiceRegistrationStrategy strategy = ServiceRegistrationStrategy.SkipDuplicates)
    {
        ServiceRegistrationBuilder.Validate(services, lifetime, strategy);
        var requests = ServiceRegistrationBuilder.Materialize(closedServiceTypes, nameof(closedServiceTypes));
        var candidates = ServiceRegistrationBuilder.Materialize(implementationTypes, nameof(implementationTypes));
        return ServiceRegistrationBuilder.Apply(services, GenericImplementationMatcher.Describe(requests, candidates, lifetime), strategy);
    }

    /// <summary>从指定程序集扫描候选，并对列出的闭合泛型接口完成启动时推导。</summary>
    /// <param name="services">目标服务集合。</param>
    /// <param name="closedServiceTypes">明确列出的闭合泛型接口。</param>
    /// <param name="assemblies">明确提供的扫描范围。</param>
    /// <param name="lifetime">本批新增描述符的生命周期。</param>
    /// <param name="strategy">重复组合的处理策略。</param>
    /// <param name="predicate">可选类型过滤器。</param>
    /// <returns>原服务集合；扫描、推导和冲突错误不留下本批部分注册。</returns>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("程序集扫描和泛型推导需要完整的实现类型元数据。")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("MakeGenericType 推导的闭合实现可能没有预编译本机代码。")]
#endif
    public static IServiceCollection ScanClosedGenericImplementations(this IServiceCollection services,
        IEnumerable<Type> closedServiceTypes, IEnumerable<Assembly> assemblies, ServiceLifetime lifetime,
        ServiceRegistrationStrategy strategy = ServiceRegistrationStrategy.SkipDuplicates, Func<Type, bool> predicate = null)
    {
        ServiceRegistrationBuilder.Validate(services, lifetime, strategy);
        var requests = ServiceRegistrationBuilder.Materialize(closedServiceTypes, nameof(closedServiceTypes));
        foreach (var request in requests) ServiceRegistrationBuilder.ValidateService(request, true);
        var candidates = ServiceRegistrationBuilder.Scan(assemblies, predicate);
        return services.AddClosedGenericImplementations(requests, candidates, lifetime, strategy);
    }
}
