using System;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Bing.Reflection;

/// <summary>
/// 类型访问器
/// </summary>
public static partial class TypeVisit
{
    /// <summary>
    /// 获取给定 <see cref="TypeInfo"/> 的名称
    /// </summary>
    /// <param name="type">类型</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static string GetFullName(Type type)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));
        return type.FullName;
    }

    /// <summary>
    /// 获取给定 <see cref="TypeInfo"/> 的完全限定名。
    /// </summary>
    /// <param name="type">类型</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static string GetFullyQualifiedName(Type type)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));
        var sb = new StringBuilder();
        if (type.IsGenericType)
        {
            sb.Append(type.GetGenericTypeDefinition().FullName)
                .Append("[");
            var genericArgs = type.GetGenericArguments().ToList();
            for (var i = 0; i < genericArgs.Count; i++)
            {
                sb.Append(genericArgs[i].GetFullyQualifiedName());
                if (i != genericArgs.Count - 1)
                    sb.Append(", ");
            }

            sb.Append("]");
        }
        else
        {
            if (!string.IsNullOrEmpty(type.FullName))
                sb.Append(type.FullName);
            else if (!string.IsNullOrEmpty(type.Name))
                sb.Append(type.Name);
            else
                sb.Append(type);
        }

        return sb.ToString();
    }

    /// <summary>
    /// 推测当前类型是否存在无参构造函数
    /// </summary>
    /// <param name="type">类型</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static bool HasParameterlessConstructor(Type type)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));
        var ctor = type.GetConstructors()
            .OrderBy(c => c.IsPublic ? 0 : (c.IsPrivate ? 2 : 1))
            .ThenBy(c => c.GetParameters().Length)
            .FirstOrDefault();
        return ctor?.GetParameters().Length == 0;
    }

    /// <summary>
    /// 获取当前类型的无参构造函数
    /// </summary>
    /// <param name="type">类型</param>
    public static ConstructorInfo GetParameterlessConstructor(Type type)
    {
        var ctor= type.GetConstructors()
            .OrderBy(c => c.IsPublic ? 0 : (c.IsPrivate ? 2 : 1))
            .ThenBy(c => c.GetParameters().Length)
            .FirstOrDefault();
        return ctor?.GetParameters().Length == 0 ? ctor : null;
    }

    /// <summary>
    /// 获取命中参数的构造函数
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="constructorParameterTypes">构造函数参数类型</param>
    public static ConstructorInfo GetMatchingConstructor(Type type, Type[] constructorParameterTypes)
    {
        if (constructorParameterTypes == null || constructorParameterTypes.Length == 0)
            return GetParameterlessConstructor(type);
        return type.GetConstructors()
            .FirstOrDefault(c => c
                .GetParameters()
                .Select(p => p.ParameterType)
                .SequenceEqual(constructorParameterTypes)
            );
    }
}

/// <summary>
/// 类型元数据访问器扩展
/// </summary>
public static partial class TypeMetaVisitExtensions
{
    /// <summary>
    /// 获取给定 <see cref="TypeInfo"/> 的完全限定名。
    /// </summary>
    /// <param name="type">类型</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static string GetFullyQualifiedName(this Type type)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));
        return TypeVisit.GetFullyQualifiedName(type);
    }

    /// <summary>
    /// 推测当前类型是否存在无参构造函数
    /// </summary>
    /// <param name="type">类型</param>
    public static bool HasParameterlessConstructor(this Type type) => TypeVisit.HasParameterlessConstructor(type);

    /// <summary>
    /// 获取当前类型的无参构造函数
    /// </summary>
    /// <param name="type">类型</param>
    public static ConstructorInfo GetParameterlessConstructor(this Type type) => TypeVisit.GetParameterlessConstructor(type);

    /// <summary>
    /// 获取命中参数的构造函数
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="constructorParameterTypes">构造函数参数类型</param>
    public static ConstructorInfo GetMatchingConstructor(this Type type, Type[] constructorParameterTypes) => TypeVisit.GetMatchingConstructor(type, constructorParameterTypes);
}