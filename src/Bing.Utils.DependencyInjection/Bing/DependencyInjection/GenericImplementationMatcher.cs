using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Bing.DependencyInjection;

/// <summary>只针对启动时明确列出的闭合服务进行结构推导，不扩展变体规则。</summary>
internal static class GenericImplementationMatcher
{
    internal static ServiceDescriptor[] Describe(Type[] services, Type[] implementations, ServiceLifetime lifetime)
    {
        foreach (var service in services) ServiceRegistrationBuilder.ValidateService(service, true);
        foreach (var implementation in implementations) ServiceRegistrationBuilder.ValidateImplementation(implementation);
        var descriptors = new List<ServiceDescriptor>();
        foreach (var service in services)
        {
            var failures = new List<string>();
            var found = false;
            foreach (var implementation in implementations)
            {
                var matches = Close(service, implementation, out var reason);
                if (matches.Length > 1)
                    throw new InvalidOperationException($"服务 {service} 的候选 {implementation} 推导出多个闭合实现：{string.Join(", ", matches.Select(type => type.ToString()))}。");
                if (matches.Length == 0)
                    failures.Add($"{implementation}: {reason}");
                else
                {
                    found = true;
                    descriptors.Add(new ServiceDescriptor(service, matches[0], lifetime));
                }
            }
            if (!found)
                throw new InvalidOperationException($"服务 {service} 没有有效实现。候选及原因：{(failures.Count == 0 ? "候选集合为空" : string.Join("; ", failures))}。");
        }
        return descriptors.ToArray();
    }

    private static Type[] Close(Type service, Type implementation, out string reason)
    {
        var results = new HashSet<Type>();
        reason = "接口投影与服务结构不匹配";
        var definition = service.GetGenericTypeDefinition();
        foreach (var projection in implementation.GetInterfaces().Where(type => type.IsGenericType && type.GetGenericTypeDefinition() == definition))
        {
            var bindings = new Dictionary<Type, Type>();
            if (!Match(projection, service, bindings)) continue;
            var closed = implementation;
            if (implementation.IsGenericTypeDefinition)
            {
                var parameters = implementation.GetGenericArguments();
                if (parameters.Any(parameter => !bindings.ContainsKey(parameter)))
                {
                    reason = "存在无法推导的实现泛型参数";
                    continue;
                }
                try
                {
                    closed = implementation.MakeGenericType(parameters.Select(parameter => bindings[parameter]).ToArray());
                }
                catch (ArgumentException exception)
                {
                    reason = $"泛型约束不满足：{exception.Message}";
                    continue;
                }
            }
            if (!closed.ContainsGenericParameters && service.IsAssignableFrom(closed)) results.Add(closed);
            else reason = "闭合实现不能赋值给请求服务";
        }
        return results.ToArray();
    }

    private static bool Match(Type projection, Type requested, Dictionary<Type, Type> bindings)
    {
        if (projection.IsGenericParameter)
        {
            if (bindings.TryGetValue(projection, out var bound)) return bound == requested;
            bindings.Add(projection, requested);
            return true;
        }
        if (projection.IsArray)
        {
            if (!requested.IsArray || projection.GetArrayRank() != requested.GetArrayRank()) return false;
            // Rank 为 1 时还须区分向量 T[] 与非向量 T[*]，不能仅比较维度。
            if ((projection == projection.GetElementType().MakeArrayType()) !=
                (requested == requested.GetElementType().MakeArrayType())) return false;
            return Match(projection.GetElementType(), requested.GetElementType(), bindings);
        }
        if (!projection.IsGenericType) return projection == requested;
        if (!requested.IsGenericType || projection.GetGenericTypeDefinition() != requested.GetGenericTypeDefinition()) return false;
        var arguments = projection.GetGenericArguments();
        var requestedArguments = requested.GetGenericArguments();
        for (var i = 0; i < arguments.Length; i++)
            if (!Match(arguments[i], requestedArguments[i], bindings)) return false;
        return true;
    }
}
