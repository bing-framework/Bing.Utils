namespace Bing.Reflection;

// 类型反射 - 判断
public static partial class TypeReflections
{
    private static bool X(MemberInfo member, Func<Type, Type> typeClear, Func<Type, bool> typeInfer) =>
        member switch
        {
            null => false,
            Type type => typeInfer(typeClear(type)),
            PropertyInfo property => typeInfer(typeClear(property.PropertyType)),
            FieldInfo field => typeInfer(typeClear(field.FieldType)),
            _ => false
        };

    private static Type N(Type type, TypeIsOptions isOptions = TypeIsOptions.Default) =>
        isOptions switch
        {
            TypeIsOptions.Default => type,
            TypeIsOptions.IgnoreNullable => TypeConv.GetNonNullableType(type),
            _ => type
        };

    /// <summary>
    /// 判断给定的 <see cref="MemberInfo"/> 元信息是否为布尔类型
    /// </summary>
    /// <param name="member">成员元数据</param>
    /// <param name="isOptions">类型判断选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsBoolean(MemberInfo member, TypeIsOptions isOptions = TypeIsOptions.Default) => X(member, type => N(type, isOptions), type => type == TypeClass.BooleanClazz);

    /// <summary>
    /// 判断给定的 <see cref="MemberInfo"/> 元信息是否为 <see cref="DateTime"/> 类型
    /// </summary>
    /// <param name="member">成员元数据</param>
    /// <param name="isOptions">类型判断选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsDateTime(MemberInfo member, TypeIsOptions isOptions = TypeIsOptions.Default) => X(member, type => N(type, isOptions), type => type == TypeClass.DateTimeClazz);

    /// <summary>
    /// 判断给定的 <see cref="MemberInfo"/> 元信息是否为数值类型
    /// </summary>
    /// <param name="member">成员元数据</param>
    /// <param name="isOptions">类型判断选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNumeric(MemberInfo member, TypeIsOptions isOptions = TypeIsOptions.Default) => X(member, type => type, type => Types.IsNumericType(type, isOptions));

    /// <summary>
    /// 判断给定的 <see cref="MemberInfo"/> 元信息是否为枚举类型
    /// </summary>
    /// <param name="member">成员元数据</param>
    /// <param name="isOptions">类型判断选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEnum(MemberInfo member, TypeIsOptions isOptions = TypeIsOptions.Default) => X(member, type => type, type => Types.IsEnumType(type, isOptions));

    /// <summary>
    /// 判断给定的 <see cref="MemberInfo"/> 元信息是否为结构类型
    /// </summary>
    /// <param name="member">成员元数据</param>
    /// <param name="isOptions">类型判断选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsStruct(MemberInfo member, TypeIsOptions isOptions = TypeIsOptions.Default) => X(member, type => type, type => Types.IsStructType(type, isOptions));

    /// <summary>
    /// 判断给定的 <see cref="MemberInfo"/> 元信息是否为集合或数组类型
    /// </summary>
    /// <param name="member">成员元数据</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsCollection(MemberInfo member) => X(member, type => type, Types.IsCollectionType);
}