using System.Reflection;
using System.Text;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Bing.Reflection;

/// <summary>
/// 类型访问器
/// </summary>
public static partial class TypeVisit
{
    internal const int MethodSignatureCacheCapacity = 1024;
    private static int _methodSignatureScanCount;
    internal static int MethodSignatureScanCount => System.Threading.Volatile.Read(ref _methodSignatureScanCount);
    private sealed class MethodSignatureLookup
    {
        internal readonly ConcurrentDictionary<string, Lazy<MethodInfo>> Methods = new();
        internal readonly object AdmissionGate = new();
    }

    private static readonly ConditionalWeakTable<Type, MethodSignatureLookup> MethodSignatureCache = new();
    private static readonly ConditionalWeakTable<Type, ConditionalWeakTable<MethodInfo, Lazy<MethodInfo>>> MethodInstanceCache = new();
    /// <summary>
    /// 获取给定 <see cref="MethodInfo"/> 的名称，包括类型名和方法名
    /// </summary>
    /// <param name="method">方法元数据</param>
    public static string GetFullName(MethodInfo method)
    {
        if (method == null)
            throw new ArgumentNullException(nameof(method));
        var sb = new StringBuilder();
        var type = method.DeclaringType;
        if (type != null)
            sb.Append(type.FullName).Append('.');
        sb.Append(method.Name);
        return sb.ToString();
    }

    /// <summary>
    /// 获取给定 <see cref="MethodInfo"/> 的完全限定名。
    /// </summary>
    /// <param name="method">方法元数据</param>
    public static string GetFullyQualifiedName(MethodInfo method)
    {
        if (method == null)
            throw new ArgumentNullException(nameof(method));
        var sb = new StringBuilder();
        sb.Append(method.ReturnType.GetFullyQualifiedName());
        sb.Append(" ");
        sb.Append(method.Name);
        if (method.IsGenericMethod)
        {
            sb.Append("[");
            var genericArgs = method.GetGenericArguments().ToList();
            for (var i = 0; i < genericArgs.Count; i++)
            {
                sb.Append(genericArgs[i].GetFullyQualifiedName());
                if (i != genericArgs.Count - 1)
                    sb.Append(", ");
            }

            sb.Append("]");
        }

        sb.Append("(");
        var parameters = method.GetParameters();
        for (var i = 0; i < parameters.Length; i++)
        {
            sb.Append(parameters[i].ParameterType.GetFullyQualifiedName());
            if (i != parameters.Length - 1)
                sb.Append(", ");
        }

        sb.Append(")");
        return sb.ToString();
    }

    /// <summary>
    /// 获取方法签名
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="method">方法</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="MissingMethodException">未找到签名兼容的方法时抛出。</exception>
#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("方法签名按运行时元数据扫描，裁剪时必须保留目标类型的方法和接口元数据。")]
#endif
    public static MethodInfo GetMethodBySignature(Type type, MethodInfo method)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));
        if (method is null)
            throw new ArgumentNullException(nameof(method));
        if (ReflectionPackageCacheScope.IsCollectible(type)
            || ReflectionPackageCacheScope.IsCollectible(method))
        {
            ReflectionCacheMetrics.RecordBypass();
            return ResolveMethodBySignature(type, method);
        }

        var instances = MethodInstanceCache.GetValue(type, _ => new ConditionalWeakTable<MethodInfo, Lazy<MethodInfo>>());
        if (instances.TryGetValue(method, out var existing))
        {
            ReflectionCacheMetrics.Record(existing);
            return GetCachedInstanceMethod(instances, method, existing);
        }

        var created = false;
        var lazy = instances.GetValue(method, source =>
        {
            created = true;
            return ReflectionCacheMetrics.CreateLazy(() => FindMethodByStructuralSignature(type, source));
        });
        if (created)
            ReflectionCacheMetrics.RecordMiss();
        else
            ReflectionCacheMetrics.Record(lazy);
        return GetCachedInstanceMethod(instances, method, lazy);
    }

    private static MethodInfo GetCachedInstanceMethod(
        ConditionalWeakTable<MethodInfo, Lazy<MethodInfo>> instances,
        MethodInfo source,
        Lazy<MethodInfo> lazy)
    {
        var resolved = lazy.Value;
        if (!ReflectionPackageCacheScope.IsCollectible(resolved))
            return resolved;

        // 并发命中同一个 Lazy 时，所有调用方都必须清理 collectible 解析结果，避免竞态留下强缓存。
        instances.Remove(source);
        ReflectionCacheMetrics.RecordBypass();
        return resolved;
    }

#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("方法签名按运行时元数据扫描，裁剪时必须保留目标类型的方法和接口元数据。")]
#endif
    private static MethodInfo FindMethodByStructuralSignature(Type type, MethodInfo method)
    {
        var lookup = MethodSignatureCache.GetValue(type, _ => new MethodSignatureLookup());
        var key = CreateMethodIdentity(method);
        if (key.Length > 8192)
        {
            ReflectionCacheMetrics.RecordStructuralBypass();
            return ResolveMethodBySignature(type, method);
        }
        if (lookup.Methods.TryGetValue(key, out var existing))
            return GetCachedStructuralMethod(lookup, key, existing);

        Lazy<MethodInfo> lazy = null;
        var bypass = false;
        lock (lookup.AdmissionGate)
        {
            if (lookup.Methods.TryGetValue(key, out existing))
                lazy = existing;
            else if (lookup.Methods.Count >= MethodSignatureCacheCapacity)
            {
                ReflectionCacheMetrics.RecordStructuralBypass();
                bypass = true;
            }
            else
            {
                lazy = ReflectionCacheMetrics.CreateLazy(() => ResolveMethodBySignature(type, method));
                lookup.Methods.TryAdd(key, lazy);
            }
        }

        if (bypass)
            return ResolveMethodBySignature(type, method);
        return GetCachedStructuralMethod(lookup, key, lazy);
    }

    private static MethodInfo GetCachedStructuralMethod(MethodSignatureLookup lookup, string key, Lazy<MethodInfo> lazy)
    {
        var method = lazy.Value;
        if (!ReflectionPackageCacheScope.IsCollectible(method))
            return method;

        // 结构签名命中 collectible 返回类型时，立即移除强缓存项，避免宿主类型延长加载上下文生命周期。
        ((ICollection<KeyValuePair<string, Lazy<MethodInfo>>>)lookup.Methods)
            .Remove(new KeyValuePair<string, Lazy<MethodInfo>>(key, lazy));
        ReflectionCacheMetrics.RecordBypass();
        return method;
    }

    /// <summary>
    /// 获取基础方法
    /// </summary>
    /// <param name="method">方法元数据</param>
    /// <exception cref="ArgumentNullException">方法元数据为 null 时抛出。</exception>
#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("基础方法按运行时方法签名扫描，裁剪时必须保留目标类型的方法和接口元数据。")]
#endif
    public static MethodInfo GetBaseMethod(MethodInfo method)
    {
        if (method is null)
            throw new ArgumentNullException(nameof(method));
        if (method.DeclaringType?.BaseType is null)
            return null;
        try
        {
            return GetMethodBySignature(method.DeclaringType.BaseType, method);
        }
        catch (MissingMethodException)
        {
            return null;
        }
    }

    /// <summary>
    /// 判断方法是可见且为虚方法
    /// </summary>
    /// <param name="method">方法元数据</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static bool IsVisibleAndVirtual(MethodInfo method)
    {
        if (method is null)
            throw new ArgumentNullException(nameof(method));
        if (method.IsStatic || method.IsFinal)
            return false;
        return method.IsVirtual && (method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly);
    }

    /// <summary>
    /// 判断方法是否为可见的
    /// </summary>
    /// <param name="method">方法元数据</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static bool IsVisible(MethodInfo method)
    {
        if (method is null)
            throw new ArgumentNullException(nameof(method));
        return method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly;
    }
}

/// <summary>
/// 类型元数据访问器
/// </summary>
public static partial class TypeMetaVisitExtensions
{
    /// <summary>
    /// 判断指定的方法是否为异步方法
    /// </summary>
    /// <param name="method">方法元数据</param>
    public static bool IsAsyncMethod(this MethodInfo method) =>
        (method ?? throw new ArgumentNullException(nameof(method))).ReturnType == TypeClass.TaskClazz
        || method.ReturnType == TypeClass.ValueTaskClazz
        || method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == TypeClass.GenericTaskClazz
        || method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == TypeClass.GenericValueTaskClazz;

    /// <summary>
    /// 判断指定的方法是否是重写方法
    /// </summary>
    /// <param name="method">方法元数据</param>
    public static bool IsOverridden(this MethodInfo method) =>
        (method ?? throw new ArgumentNullException(nameof(method))).GetBaseDefinition().DeclaringType != method.DeclaringType;

    /// <summary>
    /// 获取给定 <see cref="MethodInfo"/> 的名称，包括类型名和方法名
    /// </summary>
    /// <param name="method">方法元数据</param>
    public static string GetFullName(this MethodInfo method) => TypeVisit.GetFullName(method);

    /// <summary>
    /// 获取给定 <see cref="MethodInfo"/> 的完全限定名。
    /// </summary>
    /// <param name="method">方法元数据</param>
    public static string GetFullyQualifiedName(this MethodInfo method) => TypeVisit.GetFullyQualifiedName(method);

    /// <summary>
    /// 获取方法签名
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="method">方法</param>
#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("方法签名按运行时元数据扫描，裁剪时必须保留目标类型的方法和接口元数据。")]
#endif
    public static MethodInfo GetMethodBySignature(this Type type, MethodInfo method) => TypeVisit.GetMethodBySignature(type, method);

    /// <summary>
    /// 获取基础方法
    /// </summary>
    /// <param name="method">方法元数据</param>
#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("基础方法按运行时方法签名扫描，裁剪时必须保留目标类型的方法和接口元数据。")]
#endif
    public static MethodInfo GetBaseMethod(this MethodInfo method) => TypeVisit.GetBaseMethod(method);

    /// <summary>
    /// 判断方法是可见且为虚方法
    /// </summary>
    /// <param name="method">方法元数据</param>
    public static bool IsVisibleAndVirtual(this MethodInfo method) => TypeVisit.IsVisibleAndVirtual(method);

    /// <summary>
    /// 判断方法是否为可见的
    /// </summary>
    /// <param name="method">方法元数据</param>
    public static bool IsVisible(this MethodInfo method) => TypeVisit.IsVisible(method);
}
