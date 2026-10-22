
// ReSharper disable once CheckNamespace
namespace Bing.Reflection;

// 类型反射 - 判断
public static partial class TypeReflections
{
    /// <summary>
    /// 判断成员声明类型经选项处理后是否满足指定条件。
    /// </summary>
    /// <param name="member">待检查的类型、属性或字段；其他成员返回 false。</param>
    /// <param name="typeClear">从成员中提取声明类型的函数。</param>
    /// <param name="typeInfer">判断处理后的类型是否符合条件的函数。</param>
    /// <returns>成员类型满足判断条件时返回 true，否则返回 false。</returns>
    private static bool X(MemberInfo member, Func<Type, Type> typeClear, Func<Type, bool> typeInfer) =>
        member switch
        {
            null => false,
            Type type => typeInfer(typeClear(type)),
            PropertyInfo property => typeInfer(typeClear(property.PropertyType)),
            FieldInfo field => typeInfer(typeClear(field.FieldType)),
            _ => false
        };

    /// <summary>
    /// 根据类型判断选项处理待检查的类型。
    /// </summary>
    /// <param name="type">待处理的类型。</param>
    /// <param name="isOptions">类型判断选项。</param>
    /// <returns>应用选项后的类型。</returns>
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
    /// 判断给定的 <see cref="MemberInfo"/> 元信息是否为原始类型
    /// </summary>
    /// <param name="member">成员元数据</param>
    /// <param name="isOptions">类型判断选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPrimitive(MemberInfo member, TypeIsOptions isOptions = TypeIsOptions.Default) => X(member, type => type, type => Types.IsPrimitiveType(type, isOptions));

    /// <summary>
    /// 判断给定的 <see cref="MemberInfo"/> 元信息是否为值类型
    /// </summary>
    /// <param name="member">成员元数据</param>
    /// <param name="isOptions">类型判断选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValueType(MemberInfo member, TypeIsOptions isOptions = TypeIsOptions.Default) => X(member, type => type, type => Types.IsValueType(type, isOptions));

    /// <summary>
    /// 判断给定的 <see cref="MemberInfo"/> 元信息是否为集合或数组类型
    /// </summary>
    /// <param name="member">成员元数据</param>
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2111", Justification = "该入口通过 Func<Type, bool> 间接调用带类型保留契约的集合判断器，成员类型来自运行时元数据。")]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsCollection(MemberInfo member) => X(member, type => type, Types.IsCollectionType);
}
