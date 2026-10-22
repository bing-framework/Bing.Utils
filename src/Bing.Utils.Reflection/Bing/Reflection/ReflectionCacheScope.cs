using System.Collections.Generic;
using System.Reflection;

namespace Bing.Reflection;

/// <summary>判断反射子包缓存是否可以安全持有元数据。</summary>
internal static class ReflectionPackageCacheScope
{
#if NET6_0_OR_GREATER
    internal static bool IsCollectible(Type type) => IsCollectible(type, null);

    private static bool IsCollectible(Type type, HashSet<Type> visitedGenericParameters)
    {
        if (type == null)
            return false;
        if (type.Assembly.IsCollectible)
            return true;
        if (type.HasElementType && IsCollectible(type.GetElementType(), visitedGenericParameters))
            return true;
        if (type.IsGenericParameter)
        {
            visitedGenericParameters ??= new HashSet<Type>();
            if (!visitedGenericParameters.Add(type))
                return false;
            return type.GetGenericParameterConstraints()
                .Any(constraint => IsCollectible(constraint, visitedGenericParameters));
        }
        return type.IsGenericType && type.GetGenericArguments()
            .Any(argument => IsCollectible(argument, visitedGenericParameters));
    }
#else
    private static readonly PropertyInfo AssemblyIsCollectible = typeof(Assembly).GetProperty("IsCollectible");

    internal static bool IsCollectible(Type type) => IsCollectible(type, null);

    private static bool IsCollectible(Type type, HashSet<Type> visitedGenericParameters)
    {
        if (type == null)
            return false;
        if (AssemblyIsCollectible?.GetValue(type.Assembly) is true)
            return true;
        if (type.HasElementType && IsCollectible(type.GetElementType(), visitedGenericParameters))
            return true;
        if (type.IsGenericParameter)
        {
            visitedGenericParameters ??= new HashSet<Type>();
            if (!visitedGenericParameters.Add(type))
                return false;
            return type.GetGenericParameterConstraints()
                .Any(constraint => IsCollectible(constraint, visitedGenericParameters));
        }
        return type.IsGenericType && type.GetGenericArguments()
            .Any(argument => IsCollectible(argument, visitedGenericParameters));
    }
#endif

    internal static bool IsCollectible(PropertyInfo property)
    {
        if (property == null)
            return false;
        return IsCollectible(property.DeclaringType)
               || IsCollectible(property.ReflectedType)
               || IsCollectible(property.PropertyType)
               || HasCollectibleCustomModifiers(property.GetRequiredCustomModifiers())
               || HasCollectibleCustomModifiers(property.GetOptionalCustomModifiers());
    }

    /// <summary>判断成员及其签名引用的元数据是否来自可回收程序集。</summary>
    internal static bool IsCollectible(MemberInfo member)
    {
        if (member == null)
            return false;
        if (member is Type type)
            return IsCollectible(type);
        if (IsCollectible(member.DeclaringType) || IsCollectible(member.ReflectedType))
            return true;
        return member switch
        {
            PropertyInfo property => IsCollectible(property),
            FieldInfo field => IsCollectible(field.FieldType)
                               || HasCollectibleCustomModifiers(field.GetRequiredCustomModifiers())
                               || HasCollectibleCustomModifiers(field.GetOptionalCustomModifiers()),
            MethodBase method => IsCollectible(method),
            _ => false
        };
    }

    /// <summary>判断方法或构造函数的声明类型、泛型参数和签名是否引用可回收元数据。</summary>
    internal static bool IsCollectible(MethodBase method)
    {
        if (method == null)
            return false;
        if (IsCollectible(method.DeclaringType))
            return true;
        if (method is MethodInfo methodInfo)
        {
            foreach (var argument in methodInfo.GetGenericArguments())
            {
                if (IsCollectible(argument))
                    return true;
            }
            if (IsCollectible(methodInfo.ReturnType) || IsCollectible(methodInfo.ReturnParameter))
                return true;
        }
        foreach (var parameter in method.GetParameters())
        {
            if (IsCollectible(parameter))
                return true;
        }
        return false;
    }

    /// <summary>判断参数类型及其所属成员是否引用可回收元数据。</summary>
    internal static bool IsCollectible(ParameterInfo parameter)
    {
        if (parameter == null)
            return false;
        if (IsCollectible(parameter.ParameterType)
            || HasCollectibleCustomModifiers(parameter.GetRequiredCustomModifiers())
            || HasCollectibleCustomModifiers(parameter.GetOptionalCustomModifiers()))
            return true;
        var member = parameter.Member;
        if (IsCollectible(member?.DeclaringType) || IsCollectible(member?.ReflectedType))
            return true;
        if (member is MethodInfo method)
        {
            foreach (var argument in method.GetGenericArguments())
            {
                if (IsCollectible(argument))
                    return true;
            }
        }
        return member is PropertyInfo property && IsCollectible(property);
    }

    private static bool HasCollectibleCustomModifiers(Type[] modifiers)
    {
        return modifiers != null && modifiers.Any(IsCollectible);
    }
}
