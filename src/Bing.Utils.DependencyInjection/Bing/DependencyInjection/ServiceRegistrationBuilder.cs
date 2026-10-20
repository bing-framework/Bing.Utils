using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bing.DependencyInjection;

/// <summary>在独立集合中校验本批描述符，避免输入及冲突错误留下部分注册。</summary>
internal static class ServiceRegistrationBuilder
{
    internal static void Validate(IServiceCollection services, ServiceLifetime lifetime, ServiceRegistrationStrategy strategy)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (!Enum.IsDefined(typeof(ServiceLifetime), lifetime)) throw new ArgumentOutOfRangeException(nameof(lifetime));
        if (!Enum.IsDefined(typeof(ServiceRegistrationStrategy), strategy)) throw new ArgumentOutOfRangeException(nameof(strategy));
    }

    internal static Type[] Materialize(IEnumerable<Type> types, string name)
    {
        if (types == null) throw new ArgumentNullException(name);
        var result = types.ToArray();
        if (result.Any(type => type == null)) throw new ArgumentException("类型集合不能包含 null。", name);
        return result;
    }

    internal static void ValidateService(Type serviceType, bool closedGeneric = false)
    {
        if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
        if (!serviceType.IsInterface ||
            (closedGeneric ? !serviceType.IsGenericType || serviceType.ContainsGenericParameters :
                serviceType.ContainsGenericParameters && !serviceType.IsGenericTypeDefinition))
            throw new ArgumentException(closedGeneric ? "服务必须是闭合泛型接口。" : "服务必须是闭合接口或开放泛型接口定义。", nameof(serviceType));
    }

    internal static void ValidateImplementation(Type type)
    {
        if (!type.IsClass || type.IsAbstract || (type.ContainsGenericParameters && !type.IsGenericTypeDefinition))
            throw new ArgumentException($"实现必须是非抽象闭合类或泛型类定义：{type}。", "implementationTypes");
    }

    internal static bool Implements(Type service, Type implementation)
    {
        if (!service.IsGenericTypeDefinition) return !implementation.ContainsGenericParameters && service.IsAssignableFrom(implementation);
        return implementation.GetInterfaces().Any(type => type.IsGenericType && type.GetGenericTypeDefinition() == service);
    }

    internal static ServiceDescriptor Describe(Type service, Type implementation, ServiceLifetime lifetime)
    {
        ValidateImplementation(implementation);
        if (!Implements(service, implementation))
            throw new ArgumentException($"{implementation} 不能实现服务 {service}。", "implementationTypes");
        if (service.IsGenericTypeDefinition)
        {
            var parameters = implementation.GetGenericArguments();
            if (!implementation.IsGenericTypeDefinition || parameters.Length != service.GetGenericArguments().Length ||
                !implementation.GetInterfaces().Any(type => type.IsGenericType && type.GetGenericTypeDefinition() == service &&
                    type.GetGenericArguments().SequenceEqual(parameters)))
                throw new ArgumentException($"{implementation} 对 {service} 不是标准开放泛型映射，请使用 AddClosedGenericImplementations 提前闭合。", "implementationTypes");
        }
        return new ServiceDescriptor(service, implementation, lifetime);
    }

    internal static Type[] Scan(IEnumerable<Assembly> assemblies, Func<Type, bool> predicate)
    {
        if (assemblies == null) throw new ArgumentNullException(nameof(assemblies));
        var inputs = assemblies.ToArray();
        if (inputs.Any(assembly => assembly == null)) throw new ArgumentException("程序集集合不能包含 null。", nameof(assemblies));
        // 不吞掉类型加载或用户谓词异常；整个扫描完成后才应用描述符。
        return inputs.Distinct().SelectMany(assembly => assembly.GetTypes().OrderBy(type => type.FullName, StringComparer.Ordinal))
            .Where(type => type.IsClass && !type.IsAbstract && type.IsVisible)
            .Where(type => predicate == null || predicate(type)).ToArray();
    }

    internal static IServiceCollection Apply(IServiceCollection services, IEnumerable<ServiceDescriptor> descriptors, ServiceRegistrationStrategy strategy)
    {
        var pending = descriptors.ToArray();
        var staged = new ServiceCollection();
        foreach (var existing in services) ((IServiceCollection)staged).Add(existing);
        var additions = new List<ServiceDescriptor>();
        foreach (var descriptor in pending)
        {
            if (strategy == ServiceRegistrationStrategy.Append)
                ((IServiceCollection)staged).Add(descriptor);
            else
            {
                var count = staged.Count;
                staged.TryAddEnumerable(descriptor);
                if (count == staged.Count)
                {
                    if (strategy == ServiceRegistrationStrategy.ThrowOnDuplicate)
                        throw new InvalidOperationException($"重复注册：服务 {descriptor.ServiceType}，实现 {descriptor.ImplementationType}。");
                    continue;
                }
            }
            additions.Add(descriptor);
        }
        foreach (var descriptor in additions) services.Add(descriptor);
        return services;
    }
}
