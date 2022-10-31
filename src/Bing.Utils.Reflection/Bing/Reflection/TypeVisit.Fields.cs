using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Bing.Reflection;

/// <summary>
/// 类型访问器
/// </summary>
public static partial class TypeVisit
{
    /// <summary>
    /// 获取字段列表
    /// </summary>
    /// <param name="type">类型</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static IEnumerable<FieldInfo> GetFields(Type type)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));
        return type.GetFields();
    }

    /// <summary>
    /// 获取字段列表
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="fieldSelectors">字段选择器集合</param>
    public static IEnumerable<FieldInfo> GetFields<T>(params Expression<Func<T, object>>[] fieldSelectors) => GetFields((IEnumerable<Expression<Func<T, object>>>)fieldSelectors);

    /// <summary>
    /// 获取字段列表
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="fieldSelectors">字段选择器集合</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static IEnumerable<FieldInfo> GetFields<T>(IEnumerable<Expression<Func<T, object>>> fieldSelectors)
    {
        if (fieldSelectors is null)
            throw new ArgumentNullException(nameof(fieldSelectors));
        return fieldSelectors.Select(GetField);
    }

    /// <summary>
    /// 获取字段
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <typeparam name="TField">字段类型</typeparam>
    /// <param name="fieldSelector">字段选择器</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public static FieldInfo GetField<T, TField>(Expression<Func<T, TField>> fieldSelector)
    {
        if (fieldSelector is null)
            throw new ArgumentNullException(nameof(fieldSelector));
        var member = fieldSelector.Body as MemberExpression;

        if (member is null
            && fieldSelector.Body.NodeType == ExpressionType.Convert
            && fieldSelector.Body is UnaryExpression unary)
            member = unary.Operand as MemberExpression;

        if (member?.Member is not FieldInfo fieldInfo)
            throw new ArgumentException($"The expression parameter ({nameof(fieldSelector)}) is not a field expression.");
        return fieldInfo;
    }
}