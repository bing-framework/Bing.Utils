namespace Bing.Reflection;

/// <summary>
/// 类型(<see cref="Type"/>) 判断器
/// </summary>
public static class TypeJudge
{
    #region IsEnumType(是否枚举类型)

    /// <summary>
    /// 判断是否为枚举类型
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="mayNullable">是否可空</param>
    public static bool IsEnumType<T>(bool mayNullable = false) => mayNullable
        ? (Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T)).IsEnum
        : typeof(T).IsEnum;

    /// <summary>
    /// 判断是否为枚举类型
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="mayNullable">是否可空</param>
    public static bool IsEnumType(Type type, bool mayNullable = false) => mayNullable
        ? (Nullable.GetUnderlyingType(type) ?? type).IsEnum
        : type.IsEnum;

    /// <summary>
    /// 判断是否为枚举类型
    /// </summary>
    /// <param name="typeInfo">类型信息</param>
    /// <param name="mayNullable">是否可空</param>
    /// <exception cref="ArgumentNullException">类型信息为 null 时抛出。</exception>
    public static bool IsEnumType(TypeInfo typeInfo, bool mayNullable = false)
    {
        if (typeInfo is null)
            throw new ArgumentNullException(nameof(typeInfo));
        return mayNullable
            ? (Nullable.GetUnderlyingType(typeInfo) ?? typeInfo).IsEnum
            : typeInfo.IsEnum;
    }

    #endregion

    #region IsNumericType(是否数值类型)

    /// <summary>
    /// 是否数值类型
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    public static bool IsNumericType<T>() => IsNumericType(typeof(T));

    /// <summary>
    /// 是否数值类型
    /// </summary>
    /// <param name="type">类型</param>
    public static bool IsNumericType(Type type) =>
        type == typeof(byte)
        || type == typeof(short)
        || type == typeof(int)
        || type == typeof(long)
        || type == typeof(sbyte)
        || type == typeof(ushort)
        || type == typeof(uint)
        || type == typeof(ulong)
        || type == typeof(decimal)
        || type == typeof(double)
        || type == typeof(float);

    /// <summary>
    /// 是否数值类型
    /// </summary>
    /// <param name="typeInfo">类型信息</param>
    /// <exception cref="ArgumentNullException">类型信息为 null 时抛出。</exception>
    public static bool IsNumericType(TypeInfo typeInfo)
    {
        if (typeInfo is null)
            throw new ArgumentNullException(nameof(typeInfo));
        return IsNumericType(typeInfo.AsType());
    }

    #endregion

    #region IsNullableType(是否可空类型)

    /// <summary>
    /// 是否可空类型
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    public static bool IsNullableType<T>() => IsNullableType(typeof(T));

    /// <summary>
    /// 是否可空类型
    /// </summary>
    /// <param name="type">类型</param>
    public static bool IsNullableType(Type type) =>
        type != null
        && type.GetTypeInfo().IsGenericType
        && type.GetGenericTypeDefinition() == typeof(Nullable<>);

    /// <summary>
    /// 是否可空类型
    /// </summary>
    /// <param name="typeInfo">类型信息</param>
    /// <exception cref="ArgumentNullException">类型信息为 null 时抛出。</exception>
    public static bool IsNullableType(TypeInfo typeInfo)
    {
        if (typeInfo is null)
            throw new ArgumentNullException(nameof(typeInfo));
        return IsNullableType(typeInfo.AsType());
    }

    #endregion

    #region IsGenericImplementation(是否泛型实现类型)

    /// <summary>
    /// 是否泛型实现类型
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="genericType">泛型类型</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static bool IsGenericImplementation(
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.Interfaces)]
#endif
        Type type, Type genericType)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));
        if (genericType is null)
            throw new ArgumentNullException(nameof(genericType));
        if (!genericType.IsGenericType)
            return false;
        // 开放泛型按定义匹配，闭合泛型保留参数进行精确匹配。
        var genericTypeDefinition = genericType.IsGenericTypeDefinition
            ? genericType
            : genericType.GetGenericTypeDefinition();
        var matchGenericDefinition = genericType.IsGenericTypeDefinition;
        bool testFlag;
        testFlag = type.GetInterfaces().Any(_checkRawGenericType);
        if (testFlag)
            return true;
        while (type != null && type != TypeClass.ObjectClazz)
        {
            testFlag = _checkRawGenericType(type);
            if (testFlag)
                return true;
            type = type.BaseType;
        }

        return false;

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
    /// 是否泛型实现类型
    /// </summary>
    /// <typeparam name="TGot">类型</typeparam>
    /// <typeparam name="TGeneric">泛型类型</typeparam>
    public static bool IsGenericImplementation<
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.Interfaces)]
#endif
        TGot, TGeneric>() => IsGenericImplementation(typeof(TGot), typeof(TGeneric));

    #endregion

}
