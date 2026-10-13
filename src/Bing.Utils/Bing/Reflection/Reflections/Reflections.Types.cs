
// ReSharper disable once CheckNamespace
namespace Bing.Reflection;

/// <summary>
/// 反射 操作
/// </summary>
public static partial class Reflections
{
    #region GetUnderlyingType(获取基础类型)

    /// <summary>
    /// 获取基础类型
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    public static Type GetUnderlyingType<T>()
    {
        var type = typeof(T);
        return Nullable.GetUnderlyingType(type) ?? type;
    }

    /// <summary>
    /// 获取基础类型
    /// </summary>
    /// <param name="type">类型</param>
    /// <exception cref="ArgumentNullException">类型为 null 时抛出。</exception>
    public static Type GetUnderlyingType(Type type)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));
        return Nullable.GetUnderlyingType(type) ?? type;
    }

    /// <summary>
    /// 获取基础类型
    /// </summary>
    /// <param name="typeInfo">类型信息</param>
    /// <exception cref="ArgumentNullException">类型信息为 null 时抛出。</exception>
    public static Type GetUnderlyingType(TypeInfo typeInfo)
    {
        if (typeInfo is null)
            throw new ArgumentNullException(nameof(typeInfo));
        var type = typeInfo.AsType();
        return Nullable.GetUnderlyingType(type) ?? type;
    }

    #endregion

    #region GetRawTypeFromGenericClass(获取原始类型)

    /// <summary>
    /// 获取原始类型。当类型从泛型类型中继承时，获取泛型类型中与该类型相对应的第一个类型参数
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="genericType">泛型类型</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static Type GetRawTypeFromGenericClass(Type type, Type genericType)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));
        if (genericType is null)
            throw new ArgumentNullException(nameof(genericType));
        if (!genericType.IsGenericType)
            return null;
        // 开放泛型按定义匹配，闭合泛型保留参数进行精确匹配。
        var genericTypeDefinition = genericType.IsGenericTypeDefinition
            ? genericType
            : genericType.GetGenericTypeDefinition();
        var matchGenericDefinition = genericType.IsGenericTypeDefinition;
        while (type != null && type != TypeClass.ObjectClazz)
        {
            var testFlag = _checkRawGenericType(type);
            if (testFlag)
                return type.GenericTypeArguments.Length > 0 ? type.GenericTypeArguments[0] : null;
            type = type.BaseType;
        }

        return null;

        // 检查原始泛型类型
        // ReSharper disable once InconsistentNaming
        bool _checkRawGenericType(Type test)
        {
            if (!test.IsGenericType)
                return false;
            return matchGenericDefinition
                ? genericTypeDefinition == test.GetGenericTypeDefinition()
                : genericType == test;
        }
    }

    /// <summary>
    /// 获取原始类型。当类型从泛型类型中继承时，获取泛型类型中与该类型相对应的第一个类型参数
    /// </summary>
    /// <typeparam name="TGot">类型</typeparam>
    /// <typeparam name="TGeneric">泛型类型</typeparam>
    public static Type GetRawTypeFromGenericClass<TGot, TGeneric>() => GetRawTypeFromGenericClass(typeof(TGot), typeof(TGeneric));

    #endregion

    #region GetFullNameWithAssemblyName(获取包含程序集名称的类型全名)

    /// <summary>
    /// 获取包含程序集名称的类型全名。
    /// </summary>
    /// <param name="type">类型</param>
    /// <returns>包含程序集名称的类型全名。</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static string GetFullNameWithAssemblyName(Type type)
    {
        _ = type ?? throw new ArgumentNullException(nameof(type));
        return $"{type.FullName}, {type.Assembly.GetName().Name}";
    }

    #endregion
}
