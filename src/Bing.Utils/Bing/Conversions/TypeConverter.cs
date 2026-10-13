using Bing.Reflection;

namespace Bing.Conversions;

/// <summary>
/// 类型 转换器
/// </summary>
public static class TypeConverter
{
    /// <summary>
    /// 将 可空类型 转换为 非可空基础类型
    /// </summary>
    /// <param name="type">类型</param>
    /// <exception cref="ArgumentNullException">类型为 null 时抛出。</exception>
    public static Type ToNonNullableType(Type type)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));
        return Nullable.GetUnderlyingType(type);
    }

    /// <summary>
    /// 将 可空类型信息 转换为 非可空基础类型信息
    /// </summary>
    /// <param name="typeInfo">类型信息</param>
    /// <exception cref="ArgumentNullException">类型信息为 null 时抛出。</exception>
    public static TypeInfo ToNonNullableTypeInfo(TypeInfo typeInfo)
    {
        if (typeInfo is null)
            throw new ArgumentNullException(nameof(typeInfo));
        return Nullable.GetUnderlyingType(typeInfo.AsType())?.GetTypeInfo();
    }

    /// <summary>
    /// 将 可空类型 安全转换为 非可空基础类型
    /// </summary>
    /// <param name="type">类型</param>
    /// <exception cref="ArgumentNullException">类型为 null 时抛出。</exception>
    public static Type ToSafeNonNullableType(Type type)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));
        return TypeJudge.IsNullableType(type) ? ToNonNullableType(type) : type;
    }

    /// <summary>
    /// 将 可空类型信息 安全转换为 非可空基础类型信息
    /// </summary>
    /// <param name="typeInfo">类型信息</param>
    /// <exception cref="ArgumentNullException">类型信息为 null 时抛出。</exception>
    public static TypeInfo ToSafeNonNullableTypeInfo(TypeInfo typeInfo)
    {
        if (typeInfo is null)
            throw new ArgumentNullException(nameof(typeInfo));
        return TypeJudge.IsNullableType(typeInfo) ? ToNonNullableTypeInfo(typeInfo) : typeInfo;
    }
}
