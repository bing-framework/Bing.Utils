using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Bing.Reflection;

/// <summary>
/// 属性访问选项
/// </summary>
public enum PropertyAccessOptions
{
    /// <summary>
    /// 具有公共实例 getter 的非索引器属性。
    /// </summary>
    Getters,

    /// <summary>
    /// 具有公共实例 setter、可在初始化后写入的非索引器属性。
    /// </summary>
    Setters,

    /// <summary>
    /// 同时满足 getter 和 setter 条件的属性。
    /// </summary>
    Both,
}

/// <summary>
/// 属性反射帮助类
/// </summary>
internal static class PropertyReflectionHelper
{
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Type, Lazy<PropertyInfo[]>> RuntimePropertiesCache = new();
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Type, Lazy<PropertyInfo[]>[]> FilteredPropertiesCache = new();
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<object, object> SafePropertySnapshots = new();
    private static readonly object SafePropertySnapshotMarker = new();

    private static bool ContainsCollectible(PropertyInfo[] properties)
    {
        if (properties == null || SafePropertySnapshots.TryGetValue(properties, out _))
            return false;
        if (properties.Any(property => ReflectionPackageCacheScope.IsCollectible((MemberInfo)property)))
            return true;
        SafePropertySnapshots.GetValue(properties, _ => SafePropertySnapshotMarker);
        return false;
    }

#if NET5_0_OR_GREATER
    private static PropertyInfo[] GetRuntimeProperties([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] Type type)
#else
    private static PropertyInfo[] GetRuntimeProperties(Type type)
#endif
    {
        if (ReflectionPackageCacheScope.IsCollectible(type))
            return type.GetRuntimeProperties().ToArray();
        var lazy = RuntimePropertiesCache.GetValue(type, _ => new Lazy<PropertyInfo[]>(
            () => type.GetRuntimeProperties().ToArray(),
            System.Threading.LazyThreadSafetyMode.ExecutionAndPublication));
        var properties = lazy.Value;
        if (ContainsCollectible(properties))
            RuntimePropertiesCache.Remove(type);
        return properties;
    }
    /// <summary>
    /// 获取可设置的属性列表
    /// </summary>
    /// <param name="type">类型</param>
    public static IEnumerable<PropertyInfo> GetPropertiesWithPublicInstanceSetters(
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
#endif
        Type type) =>
        GetFilteredProperties(type, PropertyAccessOptions.Setters);

    /// <summary>
    /// 获取可访问的属性列表
    /// </summary>
    /// <param name="type">类型</param>
    public static IEnumerable<PropertyInfo> GetPropertiesWithPublicInstanceGetters(
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
#endif
        Type type) =>
        GetFilteredProperties(type, PropertyAccessOptions.Getters);

    /// <summary>
    /// 获取可访问及设置的属性列表
    /// </summary>
    /// <param name="type">类型</param>
    public static IEnumerable<PropertyInfo> GetPropertiesWithPublicInstance(
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
#endif
        Type type) =>
        GetFilteredProperties(type, PropertyAccessOptions.Both);

    private static PropertyInfo[] GetFilteredProperties(
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
#endif
        Type type, PropertyAccessOptions accessOptions)
    {
        if (ReflectionPackageCacheScope.IsCollectible(type))
        {
            var properties = GetRuntimeProperties(type);
            return accessOptions switch
            {
                PropertyAccessOptions.Getters => properties.Where(IsPublicGetter).ToArray(),
                PropertyAccessOptions.Setters => properties.Where(IsPublicSetter).ToArray(),
                PropertyAccessOptions.Both => properties.Where(property =>
                    IsPublicGetter(property) && IsPublicSetter(property)).ToArray(),
                _ => throw new InvalidOperationException("Invalid operation for unknown access type")
            };
        }

        var filters = FilteredPropertiesCache.GetValue(type, _ => new[]
        {
            new Lazy<PropertyInfo[]>(() => GetRuntimeProperties(type).Where(p => IsPublicGetter(p)).ToArray()),
            new Lazy<PropertyInfo[]>(() => GetRuntimeProperties(type).Where(p => IsPublicSetter(p)).ToArray()),
            new Lazy<PropertyInfo[]>(() => GetRuntimeProperties(type).Where(p => IsPublicGetter(p) && IsPublicSetter(p)).ToArray())
        });
        var filteredProperties = filters[(int)accessOptions].Value;
        if (ContainsCollectible(filteredProperties))
            FilteredPropertiesCache.Remove(type);
        return filteredProperties.ToArray();
    }

    private static bool IsPublicGetter(PropertyInfo property) => property.GetIndexParameters().Length == 0
        && property.GetMethod?.IsPublic == true && !property.GetMethod.IsStatic;

    private static bool IsPublicSetter(PropertyInfo property) => property.GetIndexParameters().Length == 0
        && property.SetMethod?.IsPublic == true && !property.SetMethod.IsStatic
        && !property.SetMethod.ReturnParameter.GetRequiredCustomModifiers()
            .Any(type => type.FullName == "System.Runtime.CompilerServices.IsExternalInit");

    /// <summary>
    /// 检查属性访问
    /// </summary>
    /// <param name="propertyInfo">属性元数据</param>
    /// <param name="accessOptions">属性访问选项</param>
    public static void CheckPropertyAccess(PropertyInfo propertyInfo, PropertyAccessOptions accessOptions)
    {
        if (propertyInfo == null)
            throw new ArgumentNullException(nameof(propertyInfo));
        ArgumentException CreatePropertyNotMatchAccessException() => new($"The property ({propertyInfo.Name}) does not match accessibility restrictions.");
        switch (accessOptions)
        {
            case PropertyAccessOptions.Getters:
                if (!IsPublicGetter(propertyInfo))
                    throw CreatePropertyNotMatchAccessException();
                break;
            case PropertyAccessOptions.Setters:
                if (!IsPublicSetter(propertyInfo))
                    throw CreatePropertyNotMatchAccessException();
                break;
            case PropertyAccessOptions.Both:
                if (!IsPublicGetter(propertyInfo) || !IsPublicSetter(propertyInfo))
                    throw CreatePropertyNotMatchAccessException();
                break;
            default:
                throw new InvalidOperationException("Invalid operation for unknown access type");
        }
    }
}

/// <summary>
/// 类型访问器
/// </summary>
public static partial class TypeVisit
{
    /// <summary>
    /// 从给定的 <see cref="Type"/> 中获得所有属性
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="accessOptions">属性访问选项</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public static IEnumerable<PropertyInfo> GetProperties(
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
#endif
        Type type, PropertyAccessOptions accessOptions)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));
        return accessOptions switch
        {
            PropertyAccessOptions.Getters => PropertyReflectionHelper.GetPropertiesWithPublicInstanceGetters(type),
            PropertyAccessOptions.Setters => PropertyReflectionHelper.GetPropertiesWithPublicInstanceSetters(type),
            PropertyAccessOptions.Both => PropertyReflectionHelper.GetPropertiesWithPublicInstance(type),
            _ => throw new InvalidOperationException("Invalid operation for unknown access type")
        };
    }

    /// <summary>
    /// 从给定的 <see cref="Type"/> 中获得所有属性
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="accessOptions">属性访问选项</param>
    /// <param name="propertySelectors">属性选择器集合</param>
    public static IEnumerable<PropertyInfo> GetProperties<
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
#endif
        T>(PropertyAccessOptions accessOptions, params Expression<Func<T, object>>[] propertySelectors)
    {
        // params 未传入时会生成空数组；此时与显式 null 一样表示“返回该访问模式的全部属性”。
        if (propertySelectors is null || propertySelectors.Length == 0)
            return GetProperties(typeof(T), accessOptions);
        return GetProperties(propertySelectors, accessOptions);
    }

    /// <summary>
    /// 从给定的 <see cref="Type"/> 中获得所有属性
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="propertySelectors">属性选择器集合</param>
    /// <param name="accessOptions">属性访问选项</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static IEnumerable<PropertyInfo> GetProperties<T>(IEnumerable<Expression<Func<T, object>>> propertySelectors, PropertyAccessOptions accessOptions = PropertyAccessOptions.Both)
    {
        if (propertySelectors is null)
            throw new ArgumentNullException(nameof(propertySelectors));

        var properties = new List<PropertyInfo>();
        foreach (var selector in propertySelectors)
            properties.Add(GetProperty(selector, accessOptions));
        return properties.ToArray();
    }

    /// <summary>
    /// 获取属性
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <typeparam name="TProperty">属性类型</typeparam>
    /// <param name="propertySelector">属性选择器</param>
    /// <param name="accessOptions">属性访问选项</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public static PropertyInfo GetProperty<T, TProperty>(Expression<Func<T, TProperty>> propertySelector, PropertyAccessOptions accessOptions = PropertyAccessOptions.Both)
    {
        if (propertySelector is null)
            throw new ArgumentNullException(nameof(propertySelector));
        var member = propertySelector.Body as MemberExpression;

        if (member is null
            && propertySelector.Body.NodeType == ExpressionType.Convert
            && propertySelector.Body is UnaryExpression unary)
            member = unary.Operand as MemberExpression;

        if (member?.Member is not PropertyInfo propertyInfo)
            throw new ArgumentException($"The expression parameter ({nameof(propertySelector)}) is not a property expression.");

        PropertyReflectionHelper.CheckPropertyAccess(propertyInfo, accessOptions);

        return propertyInfo;
    }

    /// <summary>
    /// 从 <see cref="PropertyInfo"/> 列表中排除所有满足给定条件的 <see cref="PropertyInfo"/>，并返回其余的 <see cref="PropertyInfo"/>
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="properties">属性元数据集合</param>
    /// <param name="expressions">排除属性选择器集合</param>
    public static IEnumerable<PropertyInfo> Exclude<T>(IEnumerable<PropertyInfo> properties, params Expression<Func<T, object>>[] expressions) =>
        Exclude(properties, (IEnumerable<Expression<Func<T, object>>>)expressions);

    /// <summary>
    /// 从 <see cref="PropertyInfo"/> 列表中排除所有满足给定条件的 <see cref="PropertyInfo"/>，并返回其余的 <see cref="PropertyInfo"/>
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="properties">属性元数据集合</param>
    /// <param name="expressions">排除属性选择器集合</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static IEnumerable<PropertyInfo> Exclude<T>(IEnumerable<PropertyInfo> properties, IEnumerable<Expression<Func<T, object>>> expressions)
    {
        if (properties is null)
            throw new ArgumentNullException(nameof(properties));
        if (expressions is null)
            throw new ArgumentNullException(nameof(expressions));
        ISet<PropertyInfo> excluded = new HashSet<PropertyInfo>(GetProperties(expressions));

        return properties.Where(p => !excluded.Contains(p));
    }

    /// <summary>
    /// 判断属性是可见且为虚方法
    /// </summary>
    /// <param name="property">属性元数据</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static bool IsVisibleAndVirtual(PropertyInfo property)
    {
        if (property is null)
            throw new ArgumentNullException(nameof(property));
        return (property.GetMethod?.IsVisibleAndVirtual() ?? false) ||
               (property.SetMethod?.IsVisibleAndVirtual() ?? false);
    }
}

/// <summary>
/// 类型访问器扩展
/// </summary>
public static partial class TypeVisitExtensions
{
    /// <summary>
    /// 获取属性
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <typeparam name="TProperty">属性类型</typeparam>
    /// <param name="this">当前对象</param>
    /// <param name="propertySelector">属性选择器</param>
    /// <param name="accessOptions">属性访问选项</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static PropertyInfo GetProperty<T, TProperty>(this T @this, Expression<Func<T, TProperty>> propertySelector, PropertyAccessOptions accessOptions = PropertyAccessOptions.Both)
    {
        if (@this is null)
            throw new ArgumentNullException(nameof(@this));
        if (propertySelector is null)
            throw new ArgumentNullException(nameof(propertySelector));
        return TypeVisit.GetProperty(propertySelector, accessOptions);
    }


    /// <summary>
    /// 从给定的 <see cref="Type"/> 中获得所有属性
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="this">当前对象</param>
    /// <param name="propertySelectors">属性选择器集合</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static IEnumerable<PropertyInfo> GetProperties<
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
#endif
        T>(this T @this, params Expression<Func<T, object>>[] propertySelectors)
    {
        if (@this is null)
            throw new ArgumentNullException(nameof(@this));
        return TypeVisit.GetProperties(PropertyAccessOptions.Both, propertySelectors);
    }

    /// <summary>
    /// 从给定的 <see cref="Type"/> 中获得所有属性
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="this">当前对象</param>
    /// <param name="accessOptions"></param>
    /// <param name="propertySelectors">属性选择器集合</param>
    /// <exception cref="ArgumentNullException">属性访问选项</exception>
    public static IEnumerable<PropertyInfo> GetProperties<
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
#endif
        T>(this T @this, PropertyAccessOptions accessOptions, params Expression<Func<T, object>>[] propertySelectors)
    {
        if (@this is null)
            throw new ArgumentNullException(nameof(@this));
        return TypeVisit.GetProperties(accessOptions, propertySelectors);
    }
}

/// <summary>
/// 类型元数据访问器扩展
/// </summary>
public static partial class TypeMetaVisitExtensions
{
    /// <summary>
    /// 判断指定属性是否是虚属性
    /// </summary>
    /// <param name="property">属性元数据</param>
    public static bool IsVirtual(this PropertyInfo property)
    {
        if (property is null)
            throw new ArgumentNullException(nameof(property));
        var accessor = property.GetAccessors().FirstOrDefault();
        return accessor is not null && accessor.IsVirtual && !accessor.IsFinal;
    }

    /// <summary>
    /// 判断指定属性是否是抽象属性
    /// </summary>
    /// <param name="property">属性元数据</param>
    public static bool IsAbstract(this PropertyInfo property)
    {
        if (property is null)
            throw new ArgumentNullException(nameof(property));
        var accessor = property.GetAccessors().FirstOrDefault();
        return accessor is not null && accessor.IsAbstract && !accessor.IsFinal;
    }

    /// <summary>
    /// 判断属性是可见且为虚方法
    /// </summary>
    /// <param name="property">属性元数据</param>
    public static bool IsVisibleAndVirtual(this PropertyInfo property) => TypeVisit.IsVisibleAndVirtual(property);
}
