using System.Collections;

namespace Bing.Reflection;

/// <summary>
/// 类型判断选项
/// </summary>
public enum TypeIsOptions
{
    /// <summary>
    /// 默认
    /// </summary>
    Default = 0,

    /// <summary>
    /// 忽略可空
    /// </summary>
    IgnoreNullable = 1
}

/// <summary>
/// Bing 类型 扩展
/// </summary>
internal static partial class TypeExtensions
{
    /// <summary>
    /// 获取拆箱类型
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="value">值</param>
    // ReSharper disable once IdentifierTypo
    public static Type GetUnboxedType<T>(this T value) => typeof(T);
}

/// <summary>
/// 类型 操作
/// </summary>
public static partial class Types
{
    #region Tuple

    /// <summary>
    /// 判断给定的类型是否为元祖类型
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="ofOptions">类型选项</param>
    /// <param name="isOptions">类型判断选项</param>
    public static bool IsTupleType(Type type, TypeOfOptions ofOptions = TypeOfOptions.Owner, TypeIsOptions isOptions = TypeIsOptions.Default)
    {
        if (type is null)
            return false;

        if (isOptions == TypeIsOptions.IgnoreNullable)
            type = TypeConv.GetNonNullableType(type);

        if (type == typeof(Tuple) || type == typeof(ValueTuple))
            return true;

        while (type != null)
        {
            if (type.IsGenericType)
            {
                var genType = type.GetGenericTypeDefinition();
                if (genType == typeof(Tuple<>)
                    || genType == typeof(Tuple<,>)
                    || genType == typeof(Tuple<,,>)
                    || genType == typeof(Tuple<,,,>)
                    || genType == typeof(Tuple<,,,,>)
                    || genType == typeof(Tuple<,,,,,>)
                    || genType == typeof(Tuple<,,,,,,>)
                    || genType == typeof(Tuple<,,,,,,,>))
                    return true;

                if (genType == typeof(ValueTuple<>)
                    || genType == typeof(ValueTuple<,>)
                    || genType == typeof(ValueTuple<,,>)
                    || genType == typeof(ValueTuple<,,,>)
                    || genType == typeof(ValueTuple<,,,,>)
                    || genType == typeof(ValueTuple<,,,,,>)
                    || genType == typeof(ValueTuple<,,,,,,>)
                    || genType == typeof(ValueTuple<,,,,,,,>))
                    return true;
            }

            if (ofOptions == TypeOfOptions.Owner)
                break;

            type = type.BaseType;
        }

        return false;
    }
    /// <summary>
    /// 判断给定的类型是否为元组类型
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <param name="ofOptions">类型选项</param>
    /// <param name="isOptions">类型判断选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsTupleType<T>(TypeOfOptions ofOptions = TypeOfOptions.Owner, TypeIsOptions isOptions = TypeIsOptions.Default) =>
        IsTupleType(typeof(T), ofOptions, isOptions);

    /// <summary>
    /// 判断给定的对象是否为元祖类型
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <param name="value">值</param>
    /// <param name="ofOptions">类型选项</param>
    /// <param name="isOptions">类型判断选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsTupleType<T>(T value, TypeOfOptions ofOptions = TypeOfOptions.Owner, TypeIsOptions isOptions = TypeIsOptions.Default)
    {
        var type = value?.GetUnboxedType();
        return type is not null && IsTupleType(type, ofOptions, isOptions);
    }

    #endregion

    #region ValueTuple

    /// <summary>
    /// 判断给定的类型是否为值元祖类型
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="ofOptions">类型选项</param>
    /// <param name="isOptions">类型判断选项</param>
    public static bool IsValueTupleType(Type type, TypeOfOptions ofOptions = TypeOfOptions.Owner, TypeIsOptions isOptions = TypeIsOptions.Default)
    {
        if (type is null)
            return false;

        if (isOptions == TypeIsOptions.IgnoreNullable)
            type = TypeConv.GetNonNullableType(type);

        if (type == typeof(ValueTuple))
            return true;

        while (type != null)
        {
            if (type.IsGenericType)
            {
                var genType = type.GetGenericTypeDefinition();
                if (genType == typeof(ValueTuple<>)
                    || genType == typeof(ValueTuple<,>)
                    || genType == typeof(ValueTuple<,,>)
                    || genType == typeof(ValueTuple<,,,>)
                    || genType == typeof(ValueTuple<,,,,>)
                    || genType == typeof(ValueTuple<,,,,,>)
                    || genType == typeof(ValueTuple<,,,,,,>)
                    || genType == typeof(ValueTuple<,,,,,,,>))
                    return true;
            }

            if (ofOptions == TypeOfOptions.Owner)
                break;

            type = type.BaseType;
        }

        return false;
    }
    /// <summary>
    /// 判断给定的类型是否为值元祖类型
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <param name="ofOptions">类型选项</param>
    /// <param name="isOptions">类型判断选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValueTupleType<T>(TypeOfOptions ofOptions = TypeOfOptions.Owner, TypeIsOptions isOptions = TypeIsOptions.Default) =>
        IsValueTupleType(typeof(T), ofOptions, isOptions);

    /// <summary>
    /// 判断给定的类型是否为值元祖类型
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <param name="value">值</param>
    /// <param name="ofOptions">类型选项</param>
    /// <param name="isOptions">类型判断选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValueTupleType<T>(T value, TypeOfOptions ofOptions = TypeOfOptions.Owner, TypeIsOptions isOptions = TypeIsOptions.Default)
    {
        var type = value?.GetUnboxedType();
        return type is not null && IsValueTupleType(type, ofOptions, isOptions);
    }

    #endregion

    #region Numeric

    /// <summary>
    /// 判断给定的类型是否为数字类型
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="isOptions">类型判断选项</param>
    public static bool IsNumericType(Type type, TypeIsOptions isOptions = TypeIsOptions.Default)
    {
        if (type is null)
            return false;

        if (isOptions == TypeIsOptions.IgnoreNullable)
            type = TypeConv.GetNonNullableType(type);

        return type == TypeClass.ByteClazz
               || type == TypeClass.SByteClazz
               || type == TypeClass.Int16Clazz
               || type == TypeClass.Int32Clazz
               || type == TypeClass.Int64Clazz
               || type == TypeClass.UInt16Clazz
               || type == TypeClass.UInt32Clazz
               || type == TypeClass.UInt64Clazz
               || type == TypeClass.DecimalClazz
               || type == TypeClass.DoubleClazz
               || type == TypeClass.FloatClazz;
    }

    /// <summary>
    /// 判断给定的类型信息是否为数字类型
    /// </summary>
    /// <param name="typeInfo">类型信息</param>
    /// <param name="isOptions">类型判断选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNumericType(TypeInfo typeInfo, TypeIsOptions isOptions = TypeIsOptions.Default) => IsNumericType(typeInfo.AsType(), isOptions);

    /// <summary>
    /// 判断给定的类型是否为数字类型
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <param name="isOptions">类型判断选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNumericType<T>(TypeIsOptions isOptions = TypeIsOptions.Default) => IsNumericType(typeof(T), isOptions);

    /// <summary>
    /// 判断给定的对象是否为数字类型
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <param name="value">值</param>
    /// <param name="isOptions">类型判断选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNumericType<T>(T value, TypeIsOptions isOptions = TypeIsOptions.Default) => IsNumericType(value.GetUnboxedType(), isOptions);

    #endregion

    #region Nullable

    /// <summary>
    /// 判断给定的类型是否为可空类型
    /// </summary>
    /// <param name="type">类型</param>
    public static bool IsNullableType(Type type) =>
        type is not null
        && type.IsGenericType
        && type.GetGenericTypeDefinition() == typeof(Nullable<>);

    /// <summary>
    /// 判断给定的类型信息是否为可空类型
    /// </summary>
    /// <param name="typeInfo">类型信息</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNullableType(TypeInfo typeInfo) => IsNullableType(typeInfo.AsType());

    /// <summary>
    /// 判断给定的类型是否为可空类型
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNullableType<T>() => IsNullableType(typeof(T));

    /// <summary>
    /// 判断给定的对象是否为可空类型
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <param name="value">值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNullableType<T>(T value) => IsNullableType(value.GetUnboxedType());

    #endregion

    #region Enum

    /// <summary>
    /// 判断给定的类型是否为枚举类型
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="isOptions">类型判断选项</param>
    public static bool IsEnumType(Type type, TypeIsOptions isOptions = TypeIsOptions.Default) =>
        type is not null && isOptions switch
        {
            TypeIsOptions.Default => type.GetTypeInfo().IsEnum,
            TypeIsOptions.IgnoreNullable => TypeConv.GetNonNullableType(type)?.GetTypeInfo().IsEnum ?? false,
            _ => type.IsEnum
        };

    /// <summary>
    /// 判断给定的类型信息是否为枚举类型
    /// </summary>
    /// <param name="typeInfo">类型信息</param>
    /// <param name="isOptions">类型判断选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEnumType(TypeInfo typeInfo, TypeIsOptions isOptions = TypeIsOptions.Default)
    {
        return IsEnumType(typeInfo.AsType(), isOptions);
    }

    /// <summary>
    /// 判断给定的类型是否为枚举类型
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <param name="isOptions">类型判断选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEnumType<T>(TypeIsOptions isOptions = TypeIsOptions.Default)
    {
        return IsEnumType(typeof(T), isOptions);
    }

    /// <summary>
    /// 判断给定对象是否为枚举类型
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <param name="value">值</param>
    /// <param name="isOptions">类型判断选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEnumType<T>(T value, TypeIsOptions isOptions = TypeIsOptions.Default)
    {
        return value is not null && IsEnumType(typeof(T), isOptions) && typeof(T).IsEnumDefined(value);
    }

    #endregion

    #region ValueType

    /// <summary>
    /// 判断给定的类型是否为值类型
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="isOptions">类型判断选项</param>
    public static bool IsValueType(Type type, TypeIsOptions isOptions = TypeIsOptions.Default)
    {
        return type is not null && isOptions switch
        {
            TypeIsOptions.Default => type.GetTypeInfo().IsValueType,
            TypeIsOptions.IgnoreNullable => TypeConv.GetNonNullableType(type)?.GetTypeInfo().IsValueType ?? false,
            _ => type.IsValueType
        };
    }

    /// <summary>
    /// 判断给定的类型是否为值类型
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <param name="isOptions">类型判断选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValueType<T>(TypeIsOptions isOptions = TypeIsOptions.Default) => IsValueType(typeof(T), isOptions);

    /// <summary>
    /// 判断给定的类型是否为值类型
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <param name="value">值</param>
    /// <param name="isOptions">类型判断选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValueType<T>(T value, TypeIsOptions isOptions = TypeIsOptions.Default) =>
        value is not null && IsValueType(typeof(T), isOptions);

    #endregion

    #region PrimitiveType

    /// <summary>
    /// 判断给定的类型是否为原始类型
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="isOptions">类型判断选项</param>
    public static bool IsPrimitiveType(Type type, TypeIsOptions isOptions = TypeIsOptions.Default)
    {
        return type is not null && isOptions switch
        {
            TypeIsOptions.Default => type.GetTypeInfo().IsPrimitive,
            TypeIsOptions.IgnoreNullable => TypeConv.GetNonNullableType(type)?.GetTypeInfo().IsPrimitive ?? false,
            _ => type.IsPrimitive
        };
    }

    /// <summary>
    /// 判断给定的类型是否为原始类型
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <param name="isOptions">类型判断选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPrimitiveType<T>(TypeIsOptions isOptions = TypeIsOptions.Default) => IsPrimitiveType(typeof(T), isOptions);

    /// <summary>
    /// 判断给定的类型是否为原始类型
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <param name="value">值</param>
    /// <param name="isOptions">类型判断选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPrimitiveType<T>(T value, TypeIsOptions isOptions = TypeIsOptions.Default) =>
        value is not null && IsPrimitiveType(typeof(T), isOptions);

    #endregion

    #region Struct

    /// <summary>
    /// 判断给定的类型是否为结构类型
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="isOptions">类型判断选项</param>
    public static bool IsStructType(Type type, TypeIsOptions isOptions = TypeIsOptions.Default)
    {
        // ReSharper disable once InconsistentNaming
        bool __check(Type yourType) => IsValueType(yourType, isOptions)
                                       && !IsEnumType(yourType, isOptions)
                                       && !IsPrimitiveType(yourType, isOptions);
        return type is not null
               && isOptions switch
               {
                   TypeIsOptions.Default => __check(type),
                   TypeIsOptions.IgnoreNullable => __check(TypeConv.GetNonNullableType(type)),
                   _ => __check(type)
               };
    }

    /// <summary>
    /// 判断给定的类型信息是否为结构类型
    /// </summary>
    /// <param name="typeInfo">类型信息</param>
    /// <param name="isOptions">类型判断选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsStructType(TypeInfo typeInfo, TypeIsOptions isOptions = TypeIsOptions.Default) => IsStructType(typeInfo.AsType(), isOptions);

    /// <summary>
    /// 判断给定的类型是否为结构类型
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <param name="isOptions">类型判断选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsStructType<T>(TypeIsOptions isOptions = TypeIsOptions.Default) => IsStructType(typeof(T), isOptions);

    /// <summary>
    /// 判断给定的对象是否为结构类型
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <param name="value">值</param>
    /// <param name="isOptions">类型判断选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsStructType<T>(T value, TypeIsOptions isOptions = TypeIsOptions.Default) => value is not null && IsStructType(typeof(T), isOptions);

    #endregion

    #region Static

    /// <summary>
    /// 判断给定的类型是否为静态类型
    /// </summary>
    /// <param name="type">类型</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsStaticType(Type type) => type is not null && type.IsAbstract && type.IsSealed;

    /// <summary>
    /// 判断给定的类型是否为静态类型
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsStaticType<T>() => IsStaticType(typeof(T));

    #endregion

    #region Anonymous

    /// <summary>
    /// 判断给定的类型是否为匿名类型
    /// </summary>
    /// <param name="type">类型</param>
    public static bool IsAnonymousType(Type type) =>
        type is not null
        && Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false)
        && type.IsGenericType
        && type.Name.StartsWith("<>")
        && type.Name.Contains("AnonymousType")
        // ReSharper disable once NonConstantEqualityExpressionHasConstantResult
        && (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;

    #endregion

    #region Array/Collection

    /// <summary>
    /// 判断给定的类型是否为集合或数组类型
    /// </summary>
    /// <param name="type">类型</param>
    public static bool IsCollectionType(Type type)
    {
        return type is not null && (type.IsArray || type.GetInterfaces().Any(InterfacePredicate));

        bool InterfacePredicate(Type typeOfInterface)
        {
            if (typeOfInterface.IsGenericType)
                typeOfInterface = typeOfInterface.GetGenericTypeDefinition();
            return typeOfInterface == typeof(IEnumerable<>)
                   || typeOfInterface == typeof(ICollection<>)
                   || typeOfInterface == typeof(IEnumerable)
                   || typeOfInterface == typeof(ICollection);
        }
    }

    /// <summary>
    /// 判断给定的类型是否为集合或数组类型
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsCollectionType<T>() => IsCollectionType(typeof(T));

    /// <summary>
    /// 判断给定的对象是否为集合或数组类型
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <param name="value">值</param>
    /// <param name="isOptions">类型判断选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsCollectionType<T>(T value, TypeIsOptions isOptions = TypeIsOptions.Default) =>
        isOptions switch
        {
            TypeIsOptions.Default => value is not null && IsCollectionType(typeof(T)),
            TypeIsOptions.IgnoreNullable => IsCollectionType(typeof(T)),
            _ => value is not null && IsCollectionType(typeof(T))
        };

    #endregion

    #region Attribute Defined

    /// <summary>
    /// 判断给定的特性是否定义
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="attributeType">特性类型</param>
    /// <param name="options">反射选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAttributeDefined(Type type, Type attributeType, ReflectionOptions options = ReflectionOptions.Default) => TypeReflections.IsAttributeDefined(type, attributeType, options);

    /// <summary>
    /// 判断给定的特性是否定义
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="type">类型</param>
    /// <param name="options">反射选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAttributeDefined<TAttribute>(Type type, ReflectionOptions options = ReflectionOptions.Default) where TAttribute : Attribute =>
        TypeReflections.IsAttributeDefined<TAttribute>(type, options);

    /// <summary>
    /// 判断给定的特性是否定义
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="options">反射选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAttributeDefined<T, TAttribute>(ReflectionOptions options = ReflectionOptions.Default) where TAttribute : Attribute =>
        TypeReflections.IsAttributeDefined<TAttribute>(typeof(T), options);

    /// <summary>
    /// 判断给定的特性是否定义
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="value">值</param>
    /// <param name="options">反射选项</param>
    /// <param name="isOptions">类型判断选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAttributeDefined<T, TAttribute>(T value, ReflectionOptions options = ReflectionOptions.Default, TypeIsOptions isOptions = TypeIsOptions.Default)
        where TAttribute : Attribute
    {
        return isOptions switch
        {
            TypeIsOptions.Default => value is not null && LocalFunc(),
            TypeIsOptions.IgnoreNullable => LocalFunc(),
            _ => value is not null && LocalFunc()
        };

        bool LocalFunc() => TypeReflections.IsAttributeDefined<TAttribute>(typeof(T), options);
    }

    #endregion

    #region Interface Defined

    /// <summary>
    /// 判断给定的接口是否定义
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="interfaceType">接口类型</param>
    /// <param name="options">接口选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInterfaceDefined(Type type, Type interfaceType, InterfaceOptions options = InterfaceOptions.Default) =>
        TypeReflections.IsInterfaceDefined(type, interfaceType, options);

    /// <summary>
    /// 判断给定的接口是否定义
    /// </summary>
    /// <typeparam name="TInterface">接口类型</typeparam>
    /// <param name="type">类型</param>
    /// <param name="options">接口选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInterfaceDefined<TInterface>(Type type, InterfaceOptions options = InterfaceOptions.Default) =>
        TypeReflections.IsInterfaceDefined<TInterface>(type, options);

    /// <summary>
    /// 判断给定的接口是否定义
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <typeparam name="TInterface">接口类型</typeparam>
    /// <param name="options">接口选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInterfaceDefined<T, TInterface>(InterfaceOptions options = InterfaceOptions.Default) =>
        TypeReflections.IsInterfaceDefined<TInterface>(typeof(T), options);

    /// <summary>
    /// 判断给定的接口是否定义
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <typeparam name="TInterface">接口类型</typeparam>
    /// <param name="value">值</param>
    /// <param name="options">接口选项</param>
    /// <param name="isOptions">类型判断选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInterfaceDefined<T, TInterface>(T value, InterfaceOptions options = InterfaceOptions.Default, TypeIsOptions isOptions = TypeIsOptions.Default)
    {
        return isOptions switch
        {
            TypeIsOptions.Default => value is not null && LocalFunc(),
            TypeIsOptions.IgnoreNullable => LocalFunc(),
            _ => value is not null && LocalFunc()
        };

        bool LocalFunc() => TypeReflections.IsInterfaceDefined<TInterface>(typeof(T), options);
    }

    #endregion
}