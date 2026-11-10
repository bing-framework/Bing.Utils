using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

// ReSharper disable once CheckNamespace
namespace Bing.Reflection;
/// <summary>
/// 类型反射操作。
/// </summary>
public static partial class TypeReflections
{
    /// <summary>
    /// 公共实例属性缓存。
    /// </summary>
    /// <remarks>
    /// 缓存数组不直接暴露给调用方，避免调用方修改结果污染后续查询。
    /// </remarks>
    private static readonly ConditionalWeakTable<Type, Lazy<PropertyInfo[]>> PublicInstancePropertiesCache = new();
    /// <summary>
    /// 已确认不包含可回收程序集成员的属性快照标记表。
    /// </summary>
    private static readonly ConditionalWeakTable<object, object> SafePropertySnapshots = new();
    /// <summary>
    /// 标记属性快照已完成可回收程序集检查。
    /// </summary>
    private static readonly object SafePropertySnapshotMarker = new();
    /// <summary>
    /// 判断属性快照是否包含可回收程序集成员。
    /// </summary>
    /// <param name="properties">待检查的属性快照。</param>
    /// <returns>包含可回收程序集成员时为 true，否则为 false。</returns>
    private static bool ContainsCollectible(PropertyInfo[] properties)
    {
        if (properties == null || SafePropertySnapshots.TryGetValue(properties, out _))
            return false;
        if (properties.Any(property => ReflectionCacheScope.IsCollectible((MemberInfo)property)))
            return true;
        SafePropertySnapshots.GetValue(properties, _ => SafePropertySnapshotMarker);
        return false;
    }
    /// <summary>
    /// 获取指定类型的公共实例属性。
    /// </summary>
    /// <param name="type">要检查的类型。</param>
    /// <returns>经过筛选、去重和稳定排序的公共实例属性快照。</returns>
    /// <remarks>
    /// 非索引器属性会合并接口继承图；同名同类型且访问器形状、公开性和初始化语义相同的属性只保留一个，其余契约分别保留。
    /// </remarks>
    /// <exception cref="ArgumentNullException">当 <paramref name="type"/> 为 null 时抛出。</exception>
    public static IReadOnlyList<PropertyInfo> GetPublicInstanceProperties(
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.Interfaces)]
#endif
        Type type)
    {
        return GetPublicInstancePropertiesCore(type).ToArray();
    }
    /// <summary>
    /// 返回仅供内部读取的属性集合。
    /// </summary>
    /// <param name="type">要检查的类型。</param>
    /// <returns>缓存中的公共实例属性数组。</returns>
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "ConditionalWeakTable 的 Type 键回调不会保留参数级动态成员标注，实际扫描使用已标注的外层 type。")]
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2087", Justification = "ConditionalWeakTable 的 Type 键回调不会保留参数级动态成员标注，实际扫描使用已标注的外层 type。")]
#endif
    internal static PropertyInfo[] GetPublicInstancePropertiesCore(
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.Interfaces)]
#endif
        Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));
        if (ReflectionCacheScope.IsCollectible(type))
            return CreatePublicInstanceProperties(type);

        var lazy = PublicInstancePropertiesCache.GetValue(type, _ => new Lazy<PropertyInfo[]>(
            () => CreatePublicInstanceProperties(type), LazyThreadSafetyMode.ExecutionAndPublication));
        var properties = lazy.Value;
        if (ContainsCollectible(properties))
            PublicInstancePropertiesCache.Remove(type);
        return properties;
    }
    /// <summary>
    /// 获取指定类型的公共实例属性。
    /// </summary>
    /// <typeparam name="T">要检查的类型。</typeparam>
    /// <returns>公共实例、非索引器属性的独立快照。</returns>
    public static IReadOnlyList<PropertyInfo> GetPublicInstanceProperties<
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.Interfaces)]
#endif
        T>() => GetPublicInstanceProperties(typeof(T));
    /// <summary>
    /// 获取指定类型的公共可读实例属性。
    /// </summary>
    /// <param name="type">要检查的类型。</param>
    /// <returns>公共可读、非索引器实例属性的独立快照。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="type"/> 为 null 时抛出。</exception>
    public static IReadOnlyList<PropertyInfo> GetPublicReadableInstanceProperties(
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.Interfaces)]
#endif
        Type type)
    {
        return GetPublicInstancePropertiesCore(type)
            .Where(property => property.GetMethod != null && property.GetMethod.IsPublic)
            .ToArray();
    }
    /// <summary>
    /// 获取指定类型的公共可写实例属性。
    /// </summary>
    /// <param name="type">要检查的类型。</param>
    /// <returns>公共可写、非索引器实例属性的独立快照。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="type"/> 为 null 时抛出。</exception>
    public static IReadOnlyList<PropertyInfo> GetPublicWritableInstanceProperties(
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.Interfaces)]
#endif
        Type type)
    {
        return GetPublicInstancePropertiesCore(type)
            .Where(property => property.SetMethod != null
                               && property.SetMethod.IsPublic
                               && !PropertyInfoExtensions.IsInitOnlySetter(property))
            .ToArray();
    }
    /// <summary>
    /// 创建指定类型的公共实例属性快照。
    /// </summary>
    /// <param name="type">要检查的类型。</param>
    /// <returns>经过筛选、去重和稳定排序的属性数组。</returns>
    private static PropertyInfo[] CreatePublicInstanceProperties(
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.Interfaces)]
#endif
        Type type)
    {
        var properties = type.IsInterface
            ? GetInterfaceProperties(type)
            : type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        return properties
            .Where(IsPublicInstanceNonIndexerProperty)
            .GroupBy(CreatePropertySignature)
            .Select(group => group
                .OrderBy(property => property.DeclaringType?.FullName, StringComparer.Ordinal)
                .ThenBy(property => GetMetadataToken(property))
                .First())
            .OrderBy(property => property.DeclaringType?.FullName, StringComparer.Ordinal)
            .ThenBy(property => property.Name, StringComparer.Ordinal)
            .ThenBy(property => property.PropertyType.FullName, StringComparer.Ordinal)
            .ThenBy(property => GetIndexParameterSignature(property), StringComparer.Ordinal)
            .ThenBy(GetMetadataToken)
            .ToArray();
    }
    /// <summary>
    /// 获取接口及其所有继承接口声明的属性。
    /// </summary>
    /// <param name="type">接口类型。</param>
    /// <param name="flags">属性可见性和实例绑定标志。</param>
    /// <returns>接口继承图中的声明属性。</returns>
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "接口继承图来自已保留的运行时 Type；公共入口已声明接口和属性元数据需求。")]
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "接口继承图通过队列遍历运行时 Type，泛型集合无法表达元素级裁剪注解。")]
#endif
    internal static IEnumerable<PropertyInfo> GetInterfaceProperties(Type type,
        BindingFlags flags = BindingFlags.Public | BindingFlags.Instance)
    {
        var visited = new HashSet<Type>();
        var pending = new Queue<Type>();
        pending.Enqueue(type);

        while (pending.Count > 0)
        {
            var current = pending.Dequeue();
            if (!visited.Add(current))
                continue;

            foreach (var property in current.GetProperties(flags | BindingFlags.DeclaredOnly))
                yield return property;

            foreach (var inheritedInterface in current.GetInterfaces().OrderBy(item => item.FullName, StringComparer.Ordinal))
                pending.Enqueue(inheritedInterface);
        }
    }
    /// <summary>
    /// 判断属性是否为公共实例非索引器属性。
    /// </summary>
    /// <param name="property">属性元数据。</param>
    /// <returns>属性满足公共实例非索引器条件时返回 true；否则返回 false。</returns>
    private static bool IsPublicInstanceNonIndexerProperty(PropertyInfo property)
    {
        if (property.GetIndexParameters().Length != 0)
            return false;

        var getter = property.GetMethod;
        var setter = property.SetMethod;
        return (getter != null && getter.IsPublic && !getter.IsStatic)
               || (setter != null && setter.IsPublic && !setter.IsStatic);
    }
    /// <summary>
    /// 创建用于属性去重的签名。
    /// </summary>
    /// <param name="property">属性元数据。</param>
    /// <returns>包含名称、类型和访问器契约的属性签名。</returns>
    /// <remarks>
    /// 调用方已排除索引器；Type 相等性保留泛型参数的声明者及位置。
    /// </remarks>
    internal static (string Name, Type PropertyType, bool HasGetter, bool HasSetter,
        bool HasPublicGetter, bool HasPublicSetter, bool IsInitOnly) CreatePropertySignature(PropertyInfo property)
    {
        var getter = property.GetMethod;
        var setter = property.SetMethod;
        return (property.Name, property.PropertyType, getter != null, setter != null,
            getter?.IsPublic == true, setter?.IsPublic == true,
            setter != null && PropertyInfoExtensions.IsInitOnlySetter(property));
    }
    /// <summary>
    /// 获取索引参数类型签名。
    /// </summary>
    /// <param name="property">属性元数据。</param>
    /// <returns>索引参数类型组成的稳定签名。</returns>
    private static string GetIndexParameterSignature(PropertyInfo property) =>
        string.Join(";", property.GetIndexParameters().Select(parameter => parameter.ParameterType.AssemblyQualifiedName));
    /// <summary>
    /// 获取元数据标识，用于同程序集成员的补充排序。
    /// </summary>
    /// <param name="property">属性元数据。</param>
    /// <returns>可用的元数据标识；动态成员不支持时返回 0。</returns>
    private static int GetMetadataToken(PropertyInfo property)
    {
        try
        {
            return property.MetadataToken;
        }
        catch (InvalidOperationException)
        {
            return 0;
        }
    }
}
