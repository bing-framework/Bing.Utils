using System;
using System.Reflection;

namespace Bing.Reflection;

/// <summary>
/// 类型访问器
/// </summary>
public static partial class TypeVisit
{
}

/// <summary>
/// 类型元数据访问器
/// </summary>
public static partial class TypeMetaVisitExtensions
{
    /// <summary>
    /// 判断给定的特性是否定义
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="member">成员元数据</param>
    /// <param name="options">反射选项</param>
    public static bool IsAttributeDefined<TAttribute>(this MemberInfo member, ReflectionOptions options = ReflectionOptions.Default)
        where TAttribute : Attribute =>
        TypeReflections.IsAttributeDefined<TAttribute>(member, options);

    /// <summary>
    /// 判断给定的特性是否未定义
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="member">成员元数据</param>
    /// <param name="options">反射选项</param>
    public static bool IsAttributeNotDefined<TAttribute>(this MemberInfo member, ReflectionOptions options = ReflectionOptions.Default)
        where TAttribute : Attribute =>
        !TypeReflections.IsAttributeDefined<TAttribute>(member, options);

    /// <summary>
    /// 判断给定的特性是否定义
    /// </summary>
    /// <param name="member">成员元数据</param>
    /// <param name="attributeType">特性类型</param>
    /// <param name="options">反射选项</param>
    public static bool IsAttributeDefined(this MemberInfo member, Type attributeType, ReflectionOptions options = ReflectionOptions.Default) =>
        TypeReflections.IsAttributeDefined(member, attributeType, options);

    /// <summary>
    /// 判断给定的特性是否未定义
    /// </summary>
    /// <param name="member">成员元数据</param>
    /// <param name="attributeType">特性类型</param>
    /// <param name="options">反射选项</param>
    public static bool IsAttributeNotDefined(this MemberInfo member, Type attributeType, ReflectionOptions options = ReflectionOptions.Default) =>
        TypeReflections.IsAttributeDefined(member, attributeType, options);
}