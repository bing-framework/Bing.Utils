using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using AspectCore.Extensions.Reflection;

namespace Bing.Reflection;

/// <summary>
/// 反射选项
/// </summary>
public enum ReflectionOptions
{
    /// <summary>
    /// 默认
    /// </summary>
    Default = 0,
    /// <summary>
    /// 继承
    /// </summary>
    Inherit = 1
}

/// <summary>
/// 反射歧义选项
/// </summary>
public enum ReflectionAmbiguousOptions
{
    /// <summary>
    /// 默认
    /// </summary>
    Default = 0,
    /// <summary>
    /// 忽略歧义
    /// </summary>
    IgnoreAmbiguous = 1
}

/// <summary>
/// 类型反射器帮助类
/// </summary>
internal static class TypeReflectorHelper
{
    /// <summary>
    /// 获取反射器
    /// </summary>
    /// <param name="member">成员元数据</param>
    /// <exception cref="InvalidOperationException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ICustomAttributeReflectorProvider GetReflector(MemberInfo member)
    {
        if (member is null)
            throw new ArgumentNullException(nameof(member));
        return member switch
        {
            TypeInfo typeInfo => typeInfo.GetReflector(),
            Type type => type.GetReflector(),
            FieldInfo field => field.GetReflector(),
            PropertyInfo property => property.GetReflector(),
            MethodInfo method => method.GetReflector(),
            ConstructorInfo constructor => constructor.GetReflector(),
            _ => throw new InvalidOperationException("Current MemberInfo cannot be converted to Reflector.")
        };
    }

    /// <summary>
    /// 获取反射器
    /// </summary>
    /// <param name="parameter">参数元数据</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ICustomAttributeReflectorProvider GetReflector(ParameterInfo parameter) => parameter.GetReflector();

    /// <summary>
    /// 自定义特性反射器运行时类型处理
    /// </summary>
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once FieldCanBeMadeReadOnly.Local
    private static FieldInfo CustomAttributeReflectorRuntimeTypeHandle = typeof(CustomAttributeReflector).GetField("_tokens", BindingFlags.Instance | BindingFlags.NonPublic);

    /// <summary>
    /// 获取运行时类型处理器集合
    /// </summary>
    /// <param name="reflector">自定义特性反射器</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HashSet<RuntimeTypeHandle> GetRuntimeTypeHandles(CustomAttributeReflector reflector) => CustomAttributeReflectorRuntimeTypeHandle.GetValue(reflector) as HashSet<RuntimeTypeHandle>;
}

/// <summary>
/// 类型反射 操作
/// </summary>
public static partial class TypeReflections
{
    /// <summary>
    /// 校验
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TRequired AttrRequired<TRequired>(this TRequired value, string message) => value ?? throw new ArgumentException(message);

    /// <summary>
    /// 校验
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IEnumerable<TRequired> AttrRequired<TRequired>(this IEnumerable<TRequired> value, string message)
    {
        if (value is null || !value.Any())
            throw new ArgumentException(message);
        return value;
    }

    /// <summary>
    /// 消除歧义，取第一项的值
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TAttribute AttrDisambiguation<TAttribute>(this IEnumerable<TAttribute> value) =>
        value is null ? default : value.FirstOrDefault();

    #region IsAttributeDefined

    /// <summary>
    /// 判断给定的特性是否定义
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="member">成员元数据</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAttributeDefined<TAttribute>(MemberInfo member) where TAttribute : Attribute => 
        IsAttributeDefinedImpl(TypeReflectorHelper.GetReflector(member), typeof(TAttribute));

    /// <summary>
    /// 判断给定的特性是否定义
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="member">成员元数据</param>
    /// <param name="options">反射选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAttributeDefined<TAttribute>(MemberInfo member, ReflectionOptions options) where TAttribute : Attribute =>
        options switch
        {
            ReflectionOptions.Default => IsAttributeDefinedImpl(TypeReflectorHelper.GetReflector(member), typeof(TAttribute)),
            ReflectionOptions.Inherit => member.GetCustomAttributes<TAttribute>(true).Any(),
            _ => member.GetCustomAttributes<TAttribute>().Any()
        };

    /// <summary>
    /// 判断给定的特性是否定义
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="parameter">参数元数据</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAttributeDefined<TAttribute>(ParameterInfo parameter) where TAttribute : Attribute => 
        IsAttributeDefinedImpl(TypeReflectorHelper.GetReflector(parameter), typeof(TAttribute));

    /// <summary>
    /// 判断给定的特性是否定义
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="parameter">参数元数据</param>
    /// <param name="options">反射选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAttributeDefined<TAttribute>(ParameterInfo parameter, ReflectionOptions options) where TAttribute : Attribute =>
        options switch
        {
            ReflectionOptions.Default => IsAttributeDefinedImpl(TypeReflectorHelper.GetReflector(parameter), typeof(TAttribute)),
            ReflectionOptions.Inherit => parameter.GetCustomAttributes<TAttribute>(true).Any(),
            _ => parameter.GetCustomAttributes<TAttribute>().Any()
        };

    /// <summary>
    /// 判断给定的特性是否定义
    /// </summary>
    /// <param name="member">成员元数据</param>
    /// <param name="attributeType">特性类型</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAttributeDefined(MemberInfo member, Type attributeType) => IsAttributeDefinedImpl(TypeReflectorHelper.GetReflector(member), attributeType);

    /// <summary>
    /// 判断给定的特性是否定义
    /// </summary>
    /// <param name="member">成员元数据</param>
    /// <param name="attributeType">特性类型</param>
    /// <param name="options">反射选项</param>
    public static bool IsAttributeDefined(MemberInfo member, Type attributeType, ReflectionOptions options) =>
        options switch
        {
            ReflectionOptions.Default => IsAttributeDefinedImpl(TypeReflectorHelper.GetReflector(member), attributeType),
            ReflectionOptions.Inherit => member.GetCustomAttributes(attributeType, true).Any(),
            _ => member.GetCustomAttributes(attributeType).Any()
        };

    /// <summary>
    /// 判断给定的特性是否定义
    /// </summary>
    /// <param name="parameter">参数元数据</param>
    /// <param name="attributeType">特性类型</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAttributeDefined(ParameterInfo parameter, Type attributeType) => IsAttributeDefinedImpl(TypeReflectorHelper.GetReflector(parameter), attributeType);

    /// <summary>
    /// 判断给定的特性是否定义
    /// </summary>
    /// <param name="parameter">参数元数据</param>
    /// <param name="attributeType">特性类型</param>
    /// <param name="options">反射选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAttributeDefined(ParameterInfo parameter, Type attributeType, ReflectionOptions options) =>
        options switch
        {
            ReflectionOptions.Default => IsAttributeDefinedImpl(TypeReflectorHelper.GetReflector(parameter), attributeType),
            ReflectionOptions.Inherit => parameter.GetCustomAttributes(attributeType, true).Any(),
            _ => parameter.GetCustomAttributes(attributeType).Any()
        };

    /// <summary>
    /// 判断给定的特性是否定义 的实现方式
    /// </summary>
    /// <param name="customAttributeReflectorProvider">自定义特性反射器提供程序</param>
    /// <param name="attributeType">特性类型</param>
    /// <exception cref="ArgumentNullException"></exception>
    private static bool IsAttributeDefinedImpl(ICustomAttributeReflectorProvider customAttributeReflectorProvider, Type attributeType)
    {
        if (attributeType is null)
            throw new ArgumentNullException(nameof(attributeType));
        var attributeReflectors = customAttributeReflectorProvider != null
            ? customAttributeReflectorProvider.CustomAttributeReflectors
            : throw new ArgumentNullException(nameof(customAttributeReflectorProvider));
        var length = attributeReflectors.Length;
        if (length == 0)
            return false;
        var typeHandle = attributeType.TypeHandle;
        for (var i = 0; i < length; i++)
        {
            if (TypeReflectorHelper.GetRuntimeTypeHandles(attributeReflectors[i]).Contains(typeHandle))
                return true;
        }
        return false;
    }

    #endregion

    #region GetAttribute

    /// <summary>
    /// 从成员信息中获取指定的 <see cref="Attribute"/> 实例
    /// </summary>
    /// <param name="member">成员元数据</param>
    /// <param name="attributeType">特性类型</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Attribute GetAttribute(MemberInfo member, Type attributeType) => GetAttributeImpl(TypeReflectorHelper.GetReflector(member), attributeType);

    /// <summary>
    /// 从成员信息中获取指定的 <see cref="Attribute"/> 实例
    /// </summary>
    /// <param name="parameter">参数元数据</param>
    /// <param name="attributeType">特性类型</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Attribute GetAttribute(ParameterInfo parameter, Type attributeType) => GetAttributeImpl(TypeReflectorHelper.GetReflector(parameter), attributeType);

    /// <summary>
    /// 从成员信息中获取指定的 <see cref="Attribute"/> 实例
    /// </summary>
    /// <param name="member">成员元数据</param>
    /// <param name="attributeType">特性类型</param>
    /// <param name="refOptions">反射选项</param>
    /// <param name="ambOptions">反射歧义选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Attribute GetAttribute(MemberInfo member, Type attributeType, ReflectionOptions refOptions, ReflectionAmbiguousOptions ambOptions = ReflectionAmbiguousOptions.Default) =>
        refOptions switch
        {
            ReflectionOptions.Default => GetAttributeImpl(TypeReflectorHelper.GetReflector(member), attributeType),
            ReflectionOptions.Inherit => ambOptions switch
            {
                ReflectionAmbiguousOptions.Default => member.GetCustomAttribute(attributeType, true),
                ReflectionAmbiguousOptions.IgnoreAmbiguous => GetAttributes(member, attributeType, refOptions).AttrDisambiguation(),
                _ => member.GetCustomAttribute(attributeType, true)
            },
            _ => member.GetCustomAttribute(attributeType)
        };

    /// <summary>
    /// 从成员信息中获取指定的 <see cref="Attribute"/> 实例
    /// </summary>
    /// <param name="parameter">参数元数据</param>
    /// <param name="attributeType">特性类型</param>
    /// <param name="refOptions">反射选项</param>
    /// <param name="ambOptions">反射歧义选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Attribute GetAttribute(ParameterInfo parameter, Type attributeType, ReflectionOptions refOptions, ReflectionAmbiguousOptions ambOptions = ReflectionAmbiguousOptions.Default) =>
        refOptions switch
        {
            ReflectionOptions.Default => GetAttributeImpl(TypeReflectorHelper.GetReflector(parameter), attributeType),
            ReflectionOptions.Inherit => ambOptions switch
            {
                ReflectionAmbiguousOptions.Default => parameter.GetCustomAttribute(attributeType, true),
                ReflectionAmbiguousOptions.IgnoreAmbiguous => GetAttributes(parameter, attributeType, refOptions).AttrDisambiguation(),
                _ => parameter.GetCustomAttribute(attributeType, true)
            },
            _ => parameter.GetCustomAttribute(attributeType)
        };

    /// <summary>
    /// 从成员信息中获取指定的 <see cref="Attribute"/> 实例
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="member">成员元数据</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TAttribute GetAttribute<TAttribute>(MemberInfo member) where TAttribute : Attribute => (TAttribute)GetAttributeImpl(TypeReflectorHelper.GetReflector(member), typeof(TAttribute));

    /// <summary>
    /// 从成员信息中获取指定的 <see cref="Attribute"/> 实例
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="parameter">参数元数据</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TAttribute GetAttribute<TAttribute>(ParameterInfo parameter) where TAttribute : Attribute => (TAttribute)GetAttributeImpl(TypeReflectorHelper.GetReflector(parameter), typeof(TAttribute));

    /// <summary>
    /// 从成员信息中获取指定的 <see cref="Attribute"/> 实例
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="member">成员元数据</param>
    /// <param name="refOptions">反射选项</param>
    /// <param name="ambOptions">反射歧义选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TAttribute GetAttribute<TAttribute>(MemberInfo member, ReflectionOptions refOptions, ReflectionAmbiguousOptions ambOptions = ReflectionAmbiguousOptions.Default) where TAttribute : Attribute =>
        refOptions switch
        {
            ReflectionOptions.Default => (TAttribute)GetAttributeImpl(TypeReflectorHelper.GetReflector(member), typeof(TAttribute)),
            ReflectionOptions.Inherit => ambOptions switch
            {
                ReflectionAmbiguousOptions.Default => member.GetCustomAttribute<TAttribute>(true),
                ReflectionAmbiguousOptions.IgnoreAmbiguous => GetAttributes<TAttribute>(member, refOptions).AttrDisambiguation(),
                _ => member.GetCustomAttribute<TAttribute>(true)
            },
            _ => member.GetCustomAttribute<TAttribute>()
        };

    /// <summary>
    /// 从成员信息中获取指定的 <see cref="Attribute"/> 实例
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="parameter">参数元数据</param>
    /// <param name="refOptions">反射选项</param>
    /// <param name="ambOptions">反射歧义选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TAttribute GetAttribute<TAttribute>(ParameterInfo parameter, ReflectionOptions refOptions, ReflectionAmbiguousOptions ambOptions = ReflectionAmbiguousOptions.Default) where TAttribute : Attribute =>
        refOptions switch
        {
            ReflectionOptions.Default => (TAttribute)GetAttributeImpl(TypeReflectorHelper.GetReflector(parameter), typeof(TAttribute)),
            ReflectionOptions.Inherit => ambOptions switch
            {
                ReflectionAmbiguousOptions.Default => parameter.GetCustomAttribute<TAttribute>(true),
                ReflectionAmbiguousOptions.IgnoreAmbiguous => GetAttributes<TAttribute>(parameter, refOptions).AttrDisambiguation(),
                _ => parameter.GetCustomAttribute<TAttribute>(true)
            },
            _ => parameter.GetCustomAttribute<TAttribute>()
        };

    /// <summary>
    /// 获取特性 的实现方式
    /// </summary>
    /// <param name="customAttributeReflectorProvider">自定义特性反射器提供程序</param>
    /// <param name="attributeType">特性类型</param>
    /// <exception cref="ArgumentNullException"></exception>
    private static Attribute GetAttributeImpl(ICustomAttributeReflectorProvider customAttributeReflectorProvider, Type attributeType)
    {
        if (attributeType is null)
            throw new ArgumentNullException(nameof(attributeType));
        var attributeReflectors = customAttributeReflectorProvider != null
            ? customAttributeReflectorProvider.CustomAttributeReflectors
            : throw new ArgumentNullException(nameof(customAttributeReflectorProvider));
        var length = attributeReflectors.Length;
        if (length == 0)
            return default;
        var typeHandle = attributeType.TypeHandle;
        for (var i = 0; i < length; i++)
        {
            if (TypeReflectorHelper.GetRuntimeTypeHandles(attributeReflectors[i]).Contains(typeHandle))
                return attributeReflectors[i].Invoke();
        }
        return default;
    }

    #endregion

    #region GetAttributes

    /// <summary>
    /// 从成员信息中获取一组指定的 <see cref="Attribute"/> 实例
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="member">成员元数据</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<TAttribute> GetAttributes<TAttribute>(MemberInfo member) where TAttribute : Attribute => GetAttributesImpl(TypeReflectorHelper.GetReflector(member), typeof(TAttribute)).Cast<TAttribute>();

    /// <summary>
    /// 从成员信息中获取一组指定的 <see cref="Attribute"/> 实例
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="parameter">参数元数据</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<TAttribute> GetAttributes<TAttribute>(ParameterInfo parameter) where TAttribute : Attribute => GetAttributesImpl(TypeReflectorHelper.GetReflector(parameter), typeof(TAttribute)).Cast<TAttribute>();

    /// <summary>
    /// 从成员信息中获取一组指定的 <see cref="Attribute"/> 实例
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="member">成员元数据</param>
    /// <param name="refOptions">反射选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<TAttribute> GetAttributes<TAttribute>(MemberInfo member, ReflectionOptions refOptions) where TAttribute : Attribute =>
        refOptions switch
        {
            ReflectionOptions.Default => GetAttributesImpl(TypeReflectorHelper.GetReflector(member), typeof(TAttribute)).Cast<TAttribute>(),
            ReflectionOptions.Inherit => member.GetCustomAttributes<TAttribute>(true),
            _ => member.GetCustomAttributes<TAttribute>()
        };

    /// <summary>
    /// 从成员信息中获取一组指定的 <see cref="Attribute"/> 实例
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="parameter">参数元数据</param>
    /// <param name="refOptions">反射选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<TAttribute> GetAttributes<TAttribute>(ParameterInfo parameter, ReflectionOptions refOptions) where TAttribute : Attribute =>
        refOptions switch
        {
            ReflectionOptions.Default => GetAttributesImpl(TypeReflectorHelper.GetReflector(parameter), typeof(TAttribute)).Cast<TAttribute>(),
            ReflectionOptions.Inherit => parameter.GetCustomAttributes<TAttribute>(true),
            _ => parameter.GetCustomAttributes<TAttribute>()
        };

    /// <summary>
    /// 从成员信息中获取一组指定的 <see cref="Attribute"/> 实例
    /// </summary>
    /// <param name="member">成员元数据</param>
    /// <param name="attributeType">特性类型</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<Attribute> GetAttributes(MemberInfo member, Type attributeType) => GetAttributesImpl(TypeReflectorHelper.GetReflector(member), attributeType);

    /// <summary>
    /// 从成员信息中获取一组指定的 <see cref="Attribute"/> 实例
    /// </summary>
    /// <param name="parameter">参数元数据</param>
    /// <param name="attributeType">特性类型</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<Attribute> GetAttributes(ParameterInfo parameter, Type attributeType) => GetAttributesImpl(TypeReflectorHelper.GetReflector(parameter), attributeType);

    /// <summary>
    /// 从成员信息中获取一组指定的 <see cref="Attribute"/> 实例
    /// </summary>
    /// <param name="member">成员元数据</param>
    /// <param name="attributeType">特性类型</param>
    /// <param name="refOptions">反射选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<Attribute> GetAttributes(MemberInfo member, Type attributeType, ReflectionOptions refOptions) =>
        refOptions switch
        {
            ReflectionOptions.Default => GetAttributesImpl(TypeReflectorHelper.GetReflector(member), attributeType),
            ReflectionOptions.Inherit => member.GetCustomAttributes(attributeType, true) as Attribute[],
            _ => member.GetCustomAttributes(attributeType)
        };

    /// <summary>
    /// 从成员信息中获取一组指定的 <see cref="Attribute"/> 实例
    /// </summary>
    /// <param name="parameter">参数元数据</param>
    /// <param name="attributeType">特性类型</param>
    /// <param name="refOptions">反射选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<Attribute> GetAttributes(ParameterInfo parameter, Type attributeType, ReflectionOptions refOptions) =>
        refOptions switch
        {
            ReflectionOptions.Default => GetAttributesImpl(TypeReflectorHelper.GetReflector(parameter), attributeType),
            ReflectionOptions.Inherit => parameter.GetCustomAttributes(attributeType, true) as Attribute[],
            _ => parameter.GetCustomAttributes(attributeType)
        };

    /// <summary>
    /// 获取特性集合 的实现方式
    /// </summary>
    /// <param name="customAttributeReflectorProvider">自定义特性反射器提供程序</param>
    /// <param name="attributeType">特性类型</param>
    /// <exception cref="ArgumentNullException"></exception>
    private static IEnumerable<Attribute> GetAttributesImpl(ICustomAttributeReflectorProvider customAttributeReflectorProvider, Type attributeType)
    {
        if (attributeType is null)
            throw new ArgumentNullException(nameof(attributeType));
        var attributeReflectors = customAttributeReflectorProvider != null
            ? customAttributeReflectorProvider.CustomAttributeReflectors
            : throw new ArgumentNullException(nameof(customAttributeReflectorProvider));
        var length = attributeReflectors.Length;
        if (length == 0)
            return Enumerable.Empty<Attribute>();
        var typeHandle = attributeType.TypeHandle;
        var holder = new Attribute[length];
        var counter = 0;
        for (var i = 0; i < length; i++)
        {
            var attributeReflector = attributeReflectors[i];
            if (TypeReflectorHelper.GetRuntimeTypeHandles(attributeReflector).Contains(typeHandle))
                holder[counter++] = attributeReflector.Invoke();
        }

        if (length == counter)
            return holder;

        var result = new Attribute[counter];
        Array.Copy(holder, result, counter);
        return result;
    }

    #endregion

    #region GetRequiredAttribute

    /// <summary>
    /// 从成员信息中获取指定的 <see cref="Attribute"/> 实例，如果获取失败则抛出异常
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="member">成员元数据</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TAttribute GetRequiredAttribute<TAttribute>(MemberInfo member) where TAttribute : Attribute => GetAttribute<TAttribute>(member).AttrRequired($"There is no {typeof(TAttribute)} attribute can be found.");

    /// <summary>
    /// 从成员信息中获取指定的 <see cref="Attribute"/> 实例，如果获取失败则抛出异常
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="parameter">参数元数据</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TAttribute GetRequiredAttribute<TAttribute>(ParameterInfo parameter) where TAttribute : Attribute => GetAttribute<TAttribute>(parameter).AttrRequired($"There is no {typeof(TAttribute)} attribute can be found.");

    /// <summary>
    /// 从成员信息中获取指定的 <see cref="Attribute"/> 实例，如果获取失败则抛出异常
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="member">成员元数据</param>
    /// <param name="refOptions">反射选项</param>
    /// <param name="ambOptions">反射歧义选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TAttribute GetRequiredAttribute<TAttribute>(MemberInfo member, ReflectionOptions refOptions,
        ReflectionAmbiguousOptions ambOptions = ReflectionAmbiguousOptions.Default) where TAttribute : Attribute
    {
        var val = ambOptions switch
        {
            ReflectionAmbiguousOptions.Default => GetAttribute<TAttribute>(member, refOptions),
            ReflectionAmbiguousOptions.IgnoreAmbiguous => GetAttributes<TAttribute>(member, refOptions)
                .AttrDisambiguation(),
            _ => GetAttribute<TAttribute>(member, refOptions)
        };
        return val.AttrRequired($"There is no {typeof(TAttribute)} attribute can be found.");
    }

    /// <summary>
    /// 从成员信息中获取指定的 <see cref="Attribute"/> 实例，如果获取失败则抛出异常
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="parameter">参数元数据</param>
    /// <param name="refOptions">反射选项</param>
    /// <param name="ambOptions">反射歧义选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TAttribute GetRequiredAttribute<TAttribute>(ParameterInfo parameter, ReflectionOptions refOptions,
        ReflectionAmbiguousOptions ambOptions = ReflectionAmbiguousOptions.Default) where TAttribute : Attribute
    {
        var val = ambOptions switch
        {
            ReflectionAmbiguousOptions.Default => GetAttribute<TAttribute>(parameter, refOptions),
            ReflectionAmbiguousOptions.IgnoreAmbiguous => GetAttributes<TAttribute>(parameter, refOptions)
                .AttrDisambiguation(),
            _ => GetAttribute<TAttribute>(parameter, refOptions)
        };
        return val.AttrRequired($"There is no {typeof(TAttribute)} attribute can be found.");
    }

    /// <summary>
    /// 从成员信息中获取指定的 <see cref="Attribute"/> 实例，如果获取失败则抛出异常
    /// </summary>
    /// <param name="member">成员元数据</param>
    /// <param name="attributeType">特性类型</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Attribute GetRequiredAttribute(MemberInfo member, Type attributeType) => GetAttribute(member, attributeType).AttrRequired($"There is no {attributeType} attribute can be found.");

    /// <summary>
    /// 从成员信息中获取指定的 <see cref="Attribute"/> 实例，如果获取失败则抛出异常
    /// </summary>
    /// <param name="parameter">参数元数据</param>
    /// <param name="attributeType">特性类型</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Attribute GetRequiredAttribute(ParameterInfo parameter, Type attributeType) => GetAttribute(parameter, attributeType).AttrRequired($"There is no {attributeType} attribute can be found.");

    /// <summary>
    /// 从成员信息中获取指定的 <see cref="Attribute"/> 实例，如果获取失败则抛出异常
    /// </summary>
    /// <param name="member">成员元数据</param>
    /// <param name="attributeType">特性类型</param>
    /// <param name="refOptions">反射选项</param>
    /// <param name="ambOptions">反射歧义选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Attribute GetRequiredAttribute(MemberInfo member, Type attributeType, ReflectionOptions refOptions, ReflectionAmbiguousOptions ambOptions = ReflectionAmbiguousOptions.Default)
    {
        var val = ambOptions switch
        {
            ReflectionAmbiguousOptions.Default => GetAttribute(member, attributeType, refOptions),
            ReflectionAmbiguousOptions.IgnoreAmbiguous => GetAttributes(member, attributeType, refOptions).AttrDisambiguation(),
            _ => GetAttribute(member, attributeType, refOptions)
        };
        return val.AttrRequired($"There is no {attributeType} attribute can be found.");
    }

    /// <summary>
    /// 从成员信息中获取指定的 <see cref="Attribute"/> 实例，如果获取失败则抛出异常
    /// </summary>
    /// <param name="parameter">参数元数据</param>
    /// <param name="attributeType">特性类型</param>
    /// <param name="refOptions">反射选项</param>
    /// <param name="ambOptions">反射歧义选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Attribute GetRequiredAttribute(ParameterInfo parameter, Type attributeType, ReflectionOptions refOptions, ReflectionAmbiguousOptions ambOptions = ReflectionAmbiguousOptions.Default)
    {
        var val = ambOptions switch
        {
            ReflectionAmbiguousOptions.Default => GetAttribute(parameter, attributeType, refOptions),
            ReflectionAmbiguousOptions.IgnoreAmbiguous => GetAttributes(parameter, attributeType, refOptions).AttrDisambiguation(),
            _ => GetAttribute(parameter, attributeType, refOptions)
        };
        return val.AttrRequired($"There is no {attributeType} attribute can be found.");
    }

    #endregion

    #region GetRequiredAttributes

    /// <summary>
    /// 从成员信息中获取一组指定的 <see cref="Attribute"/> 实例，如果获取失败则抛出异常
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="member">成员元数据</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<TAttribute> GetRequiredAttributes<TAttribute>(MemberInfo member) where TAttribute : Attribute => GetAttributes<TAttribute>(member).AttrRequired($"There is no any {typeof(TAttribute)} attributes can be found.");

    /// <summary>
    /// 从成员信息中获取一组指定的 <see cref="Attribute"/> 实例，如果获取失败则抛出异常
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="parameter">参数元数据</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<TAttribute> GetRequiredAttributes<TAttribute>(ParameterInfo parameter) where TAttribute : Attribute => GetAttributes<TAttribute>(parameter).AttrRequired($"There is no any {typeof(TAttribute)} attributes can be found.");

    /// <summary>
    /// 从成员信息中获取一组指定的 <see cref="Attribute"/> 实例，如果获取失败则抛出异常
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="member">成员元数据</param>
    /// <param name="refOptions">反射选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<TAttribute> GetRequiredAttributes<TAttribute>(MemberInfo member, ReflectionOptions refOptions) where TAttribute : Attribute => GetAttributes<TAttribute>(member, refOptions).AttrRequired($"There is no any {typeof(TAttribute)} attributes can be found.");

    /// <summary>
    /// 从成员信息中获取一组指定的 <see cref="Attribute"/> 实例，如果获取失败则抛出异常
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="parameter">参数元数据</param>
    /// <param name="refOptions">反射选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<TAttribute> GetRequiredAttributes<TAttribute>(ParameterInfo parameter, ReflectionOptions refOptions) where TAttribute : Attribute => GetAttributes<TAttribute>(parameter, refOptions).AttrRequired($"There is no any {typeof(TAttribute)} attributes can be found.");

    /// <summary>
    /// 从成员信息中获取一组指定的 <see cref="Attribute"/> 实例，如果获取失败则抛出异常
    /// </summary>
    /// <param name="member">成员元数据</param>
    /// <param name="attributeType">特性类型</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<Attribute> GetRequiredAttributes(MemberInfo member, Type attributeType) => GetAttributes(member, attributeType).AttrRequired($"There is no any {attributeType} attributes can be found.");

    /// <summary>
    /// 从成员信息中获取一组指定的 <see cref="Attribute"/> 实例，如果获取失败则抛出异常
    /// </summary>
    /// <param name="parameter">参数元数据</param>
    /// <param name="attributeType">特性类型</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<Attribute> GetRequiredAttributes(ParameterInfo parameter, Type attributeType) => GetAttributes(parameter, attributeType).AttrRequired($"There is no any {attributeType} attributes can be found.");

    /// <summary>
    /// 从成员信息中获取一组指定的 <see cref="Attribute"/> 实例，如果获取失败则抛出异常
    /// </summary>
    /// <param name="member">成员元数据</param>
    /// <param name="attributeType">特性类型</param>
    /// <param name="refOptions">反射选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<Attribute> GetRequiredAttributes(MemberInfo member, Type attributeType, ReflectionOptions refOptions) => GetAttributes(member, attributeType, refOptions).AttrRequired($"There is no any {attributeType} attributes can be found.");

    /// <summary>
    /// 从成员信息中获取一组指定的 <see cref="Attribute"/> 实例，如果获取失败则抛出异常
    /// </summary>
    /// <param name="parameter">参数元数据</param>
    /// <param name="attributeType">特性类型</param>
    /// <param name="refOptions">反射选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<Attribute> GetRequiredAttributes(ParameterInfo parameter, Type attributeType, ReflectionOptions refOptions) => GetAttributes(parameter, attributeType, refOptions).AttrRequired($"There is no any {attributeType} attributes can be found.");

    #endregion

    #region GetAllAttributes

    /// <summary>
    /// 从成员信息中获取所有 <see cref="Attribute"/> 实例
    /// </summary>
    /// <param name="member">成员元数据</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<Attribute> GetAttributes(MemberInfo member) => GetAttributesImpl(TypeReflectorHelper.GetReflector(member));

    /// <summary>
    /// 从成员信息中获取所有 <see cref="Attribute"/> 实例
    /// </summary>
    /// <param name="parameter">参数元数据</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<Attribute> GetAttributes(ParameterInfo parameter) => GetAttributesImpl(TypeReflectorHelper.GetReflector(parameter));

    /// <summary>
    /// 从成员信息中获取所有 <see cref="Attribute"/> 实例
    /// </summary>
    /// <param name="member">成员元数据</param>
    /// <param name="refOptions">反射选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<Attribute> GetAttributes(MemberInfo member, ReflectionOptions refOptions) =>
        refOptions switch
        {
            ReflectionOptions.Default => GetAttributesImpl(TypeReflectorHelper.GetReflector(member)),
            ReflectionOptions.Inherit => member.GetCustomAttributes(true).Cast<Attribute>(),
            _ => member.GetCustomAttributes()
        };

    /// <summary>
    /// 从成员信息中获取所有 <see cref="Attribute"/> 实例
    /// </summary>
    /// <param name="parameter">参数元数据</param>
    /// <param name="refOptions">反射选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<Attribute> GetAttributes(ParameterInfo parameter, ReflectionOptions refOptions) =>
        refOptions switch
        {
            ReflectionOptions.Default => GetAttributesImpl(TypeReflectorHelper.GetReflector(parameter)),
            ReflectionOptions.Inherit => parameter.GetCustomAttributes(true).Cast<Attribute>(),
            _ => parameter.GetCustomAttributes()
        };

    /// <summary>
    /// 获取所有特性 的实现方式
    /// </summary>
    /// <param name="customAttributeReflectorProvider">自定义特性反射器提供程序</param>
    /// <exception cref="ArgumentNullException"></exception>
    private static IEnumerable<Attribute> GetAttributesImpl(ICustomAttributeReflectorProvider customAttributeReflectorProvider)
    {
        var attributeReflectors = customAttributeReflectorProvider != null
            ? customAttributeReflectorProvider.CustomAttributeReflectors
            : throw new ArgumentNullException(nameof(customAttributeReflectorProvider));
        var length = attributeReflectors.Length;
        if (length == 0)
            return Enumerable.Empty<Attribute>();
        var attributeArray = new Attribute[length];
        for (var i = 0; i < length; i++) 
            attributeArray[i] = attributeReflectors[i].Invoke();
        return attributeArray;
    }

    #endregion
}