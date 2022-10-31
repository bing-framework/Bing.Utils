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
    /// 获取给定 <see cref="MethodInfo"/> 的名称，包括类型名和方法名
    /// </summary>
    /// <param name="method">方法元数据</param>
    public static string GetFullName(MethodInfo method)
    {
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
    public static MethodInfo GetMethodBySignature(Type type, MethodInfo method)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));
        if (method is null)
            throw new ArgumentNullException(nameof(method));
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(x => x.Name.Equals(method.Name))
            .ToArray();
        var parameterTypes = method.GetParameters().Select(x => x.ParameterType).ToArray();
        if (method.ContainsGenericParameters)
        {
            foreach (var info in methods)
            {
                var innerParams = info.GetParameters();
                if (innerParams.Length != parameterTypes.Length)
                    continue;
                var idx = 0;
                foreach (var param in innerParams)
                {
                    if (!param.ParameterType.IsGenericParameter
                        && !parameterTypes[idx].IsGenericParameter
                        && param.ParameterType != parameterTypes[idx])
                        break;
                    idx++;
                }

                if (idx < parameterTypes.Length)
                    continue;
                return info;
            }

            return null;
        }

        return type.GetMethod(method.Name, parameterTypes);
    }

    /// <summary>
    /// 获取基础方法
    /// </summary>
    /// <param name="method">方法元数据</param>
    public static MethodInfo GetBaseMethod(MethodInfo method)
    {
        if (null == method?.DeclaringType?.BaseType)
            return null;
        return GetMethodBySignature(method.DeclaringType.BaseType, method);
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
        method.ReturnType == TypeClass.TaskClazz
        || method.ReturnType == TypeClass.ValueTaskClazz
        || method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == TypeClass.GenericTaskClazz
        || method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == TypeClass.ValueTaskClazz;

    /// <summary>
    /// 判断指定的方法是否是重写方法
    /// </summary>
    /// <param name="method">方法元数据</param>
    public static bool IsOverridden(this MethodInfo method) => method.GetBaseDefinition().DeclaringType != method.DeclaringType;

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
    public static MethodInfo GetMethodBySignature(this Type type, MethodInfo method) => TypeVisit.GetMethodBySignature(type, method);

    /// <summary>
    /// 获取基础方法
    /// </summary>
    /// <param name="method">方法元数据</param>
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