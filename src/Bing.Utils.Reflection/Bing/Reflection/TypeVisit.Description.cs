using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
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
    /// 判断给定的 <see cref="DescriptionAttribute"/> 或 <see cref="DisplayAttribute"/> 是否定义
    /// </summary>
    /// <param name="member">成员元数据</param>
    /// <param name="options">反射选项</param>
    public static bool IsDescriptionDefined(this MemberInfo member, ReflectionOptions options = ReflectionOptions.Default) =>
        TypeReflections.IsDescriptionDefined(member, options);

    /// <summary>
    /// 获取描述
    /// </summary>
    /// <param name="member">成员元数据</param>
    /// <param name="options">反射选项</param>
    public static string GetDescription(this MemberInfo member, ReflectionOptions options = ReflectionOptions.Default) =>
        TypeReflections.GetDescription(member, options);

    /// <summary>
    /// 获取描述
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <param name="this">当前对象</param>
    /// <param name="expression">表达式</param>
    /// <param name="options">反射选项</param>
    public static string GetDescription<T>(this T @this, Expression<Func<T, object>> expression, ReflectionOptions options = ReflectionOptions.Default) =>
        @this is null ? string.Empty : TypeReflections.GetDescription(expression, options);

    /// <summary>
    /// 获取描述，或返回默认值
    /// </summary>
    /// <param name="member">成员元数据</param>
    /// <param name="defaultVal">默认值</param>
    /// <param name="options">反射选项</param>
    public static string GetDescriptionOr(this MemberInfo member, string defaultVal, ReflectionOptions options = ReflectionOptions.Default) =>
        TypeReflections.GetDescriptionOr(member, defaultVal, options);

    /// <summary>
    /// 获取描述，或返回默认值
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <param name="this">当前对象</param>
    /// <param name="expression">表达式</param>
    /// <param name="defaultVal">默认值</param>
    /// <param name="options">反射选项</param>
    public static string GetDescriptionOr<T>(this T @this, Expression<Func<T, object>> expression, string defaultVal, ReflectionOptions options = ReflectionOptions.Default) =>
        @this is null ? string.Empty : TypeReflections.GetDescriptionOr(expression, defaultVal, options);
}