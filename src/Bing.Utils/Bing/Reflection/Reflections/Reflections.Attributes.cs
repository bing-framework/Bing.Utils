using AspectCore.Extensions.Reflection;

// ReSharper disable once CheckNamespace
namespace Bing.Reflection;

/// <summary>
/// 反射 操作
/// </summary>
public static partial class Reflections
{
    /// <summary>
    /// 判断当前运行时是否应绕过依赖动态代码的第三方特性反射器。
    /// </summary>
    private static bool UsePublicAttributeApi
    {
        get => !ReflectionRuntimeFeatures.IsDynamicCodeSupported;
    }

    /// <summary>判断成员是否属于第三方特性反射器支持的元数据类型。</summary>
    private static bool SupportsCustomAttributeReflector(MemberInfo member) =>
        member is Type or FieldInfo or PropertyInfo or MethodInfo or ConstructorInfo;

    /// <summary>统一校验非泛型特性查询的类型参数。</summary>
    private static void ValidateAttributeType(Type attributeType)
    {
        if (attributeType is null)
            throw new ArgumentNullException(nameof(attributeType));
        if (!typeof(Attribute).IsAssignableFrom(attributeType) || attributeType.ContainsGenericParameters)
            throw new ArgumentException("特性类型必须是已封闭的 Attribute 派生类型。", nameof(attributeType));
    }

    /// <summary>使用当前运行时可用的特性读取后端获取单个泛型特性。</summary>
    private static TAttribute GetCustomAttribute<TAttribute>(MemberInfo member) where TAttribute : Attribute
    {
        if (member is null)
            throw new ArgumentNullException(nameof(member));
        if (!SupportsCustomAttributeReflector(member))
            return member.GetCustomAttribute<TAttribute>();
        if (UsePublicAttributeApi)
            return member.GetCustomAttribute<TAttribute>();
        return TypeReflections.GetAttribute<TAttribute>(member);
    }

    /// <summary>使用当前运行时可用的特性读取后端获取单个指定类型特性。</summary>
    private static Attribute GetCustomAttribute(MemberInfo member, Type attributeType)
    {
        if (member is null)
            throw new ArgumentNullException(nameof(member));
        ValidateAttributeType(attributeType);
        if (!SupportsCustomAttributeReflector(member))
            return member.GetCustomAttribute(attributeType);
        if (UsePublicAttributeApi)
            return member.GetCustomAttribute(attributeType);
        return TypeReflections.GetAttribute(member, attributeType);
    }

    /// <summary>使用当前运行时可用的特性读取后端获取泛型特性集合。</summary>
    private static IEnumerable<TAttribute> GetCustomAttributes<TAttribute>(MemberInfo member) where TAttribute : Attribute
    {
        if (member is null)
            throw new ArgumentNullException(nameof(member));
        if (!SupportsCustomAttributeReflector(member))
            return member.GetCustomAttributes<TAttribute>();
        if (UsePublicAttributeApi)
            return member.GetCustomAttributes<TAttribute>();
        return TypeReflections.GetAttributes<TAttribute>(member);
    }

    /// <summary>使用当前运行时可用的特性读取后端获取指定类型特性集合。</summary>
    private static IEnumerable<Attribute> GetCustomAttributes(MemberInfo member, Type attributeType)
    {
        if (member is null)
            throw new ArgumentNullException(nameof(member));
        ValidateAttributeType(attributeType);
        if (!SupportsCustomAttributeReflector(member))
            return member.GetCustomAttributes(attributeType);
        if (UsePublicAttributeApi)
            return member.GetCustomAttributes(attributeType);
        return TypeReflections.GetAttributes(member, attributeType);
    }

    /// <summary>获取必需的泛型特性集合，并拒绝空集合。</summary>
    private static IEnumerable<TAttribute> GetRequiredCustomAttributes<TAttribute>(MemberInfo member, string message)
        where TAttribute : Attribute
    {
        var attributes = GetCustomAttributes<TAttribute>(member)?.ToArray();
        if (attributes is null || attributes.Length == 0)
            throw new ArgumentException(message);
        return attributes;
    }

    /// <summary>获取必需的指定类型特性集合，并拒绝空集合。</summary>
    private static IEnumerable<Attribute> GetRequiredCustomAttributes(MemberInfo member, Type attributeType, string message)
    {
        var attributes = GetCustomAttributes(member, attributeType)?.ToArray();
        if (attributes is null || attributes.Length == 0)
            throw new ArgumentException(message);
        return attributes;
    }

    #region GetAttribute(获取特性)

    /// <summary>
    /// 获取特性
    /// </summary>
    /// <typeparam name="TAttribute">泛型特性</typeparam>
    /// <param name="fieldInfo">字段元数据</param>
    public static TAttribute GetAttribute<TAttribute>(FieldInfo fieldInfo) where TAttribute : Attribute =>
        GetCustomAttribute<TAttribute>(fieldInfo);

    /// <summary>
    /// 获取特性
    /// </summary>
    /// <typeparam name="TAttribute">泛型特性</typeparam>
    /// <param name="memberInfo">元数据</param>
    public static TAttribute GetAttribute<TAttribute>(MemberInfo memberInfo) where TAttribute : Attribute =>
        GetCustomAttribute<TAttribute>(memberInfo);

    /// <summary>
    /// 获取特性
    /// </summary>
    /// <typeparam name="TAttribute">泛型特性</typeparam>
    /// <param name="type">类型</param>
    public static TAttribute GetAttribute<TAttribute>(Type type) where TAttribute : Attribute =>
        GetCustomAttribute<TAttribute>(type);

    /// <summary>
    /// 获取特性
    /// </summary>
    /// <typeparam name="TAttribute">泛型特性</typeparam>
    /// <param name="typeInfo">类型信息</param>
    public static TAttribute GetAttribute<TAttribute>(TypeInfo typeInfo) where TAttribute : Attribute =>
        GetCustomAttribute<TAttribute>(typeInfo);

    /// <summary>
    /// 获取特性
    /// </summary>
    /// <param name="fieldInfo">字段信息</param>
    /// <param name="attributeType">特性类型</param>
    public static Attribute GetAttribute(FieldInfo fieldInfo, Type attributeType) =>
        GetCustomAttribute(fieldInfo, attributeType);

    /// <summary>
    /// 获取特性
    /// </summary>
    /// <param name="memberInfo">元数据</param>
    /// <param name="attributeType">特性类型</param>
    public static Attribute GetAttribute(MemberInfo memberInfo, Type attributeType) =>
        GetCustomAttribute(memberInfo, attributeType);

    /// <summary>
    /// 获取特性
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="attributeType">特性类型</param>
    public static Attribute GetAttribute(Type type, Type attributeType) =>
        GetCustomAttribute(type, attributeType);

    /// <summary>
    /// 获取特性
    /// </summary>
    /// <param name="typeInfo">类型信息</param>
    /// <param name="attributeType">特性类型</param>
    public static Attribute GetAttribute(TypeInfo typeInfo, Type attributeType) =>
        GetCustomAttribute(typeInfo, attributeType);

    #endregion

    #region GetAttributes(获取特性集合)

    /// <summary>
    /// 获取特性集合
    /// </summary>
    /// <typeparam name="TAttribute">泛型特性</typeparam>
    /// <param name="fieldInfo">字段元数据</param>
    public static IEnumerable<TAttribute> GetAttributes<TAttribute>(FieldInfo fieldInfo) where TAttribute : Attribute =>
        GetCustomAttributes<TAttribute>(fieldInfo);

    /// <summary>
    /// 获取特性集合
    /// </summary>
    /// <typeparam name="TAttribute">泛型特性</typeparam>
    /// <param name="memberInfo">元数据</param>
    public static IEnumerable<TAttribute> GetAttributes<TAttribute>(MemberInfo memberInfo) where TAttribute : Attribute =>
        GetCustomAttributes<TAttribute>(memberInfo);

    /// <summary>
    /// 获取特性集合
    /// </summary>
    /// <typeparam name="TAttribute">泛型特性</typeparam>
    /// <param name="type">类型</param>
    public static IEnumerable<TAttribute> GetAttributes<TAttribute>(Type type) where TAttribute : Attribute =>
        GetCustomAttributes<TAttribute>(type);

    /// <summary>
    /// 获取特性集合
    /// </summary>
    /// <typeparam name="TAttribute">泛型特性</typeparam>
    /// <param name="typeInfo">类型信息</param>
    public static IEnumerable<TAttribute> GetAttributes<TAttribute>(TypeInfo typeInfo) where TAttribute : Attribute =>
        GetCustomAttributes<TAttribute>(typeInfo);

    /// <summary>
    /// 获取特性集合
    /// </summary>
    /// <param name="fieldInfo">字段信息</param>
    /// <param name="attributeType">特性类型</param>
    public static IEnumerable<Attribute> GetAttributes(FieldInfo fieldInfo, Type attributeType) =>
        GetCustomAttributes(fieldInfo, attributeType);

    /// <summary>
    /// 获取特性集合
    /// </summary>
    /// <param name="memberInfo">元数据</param>
    /// <param name="attributeType">特性类型</param>
    public static IEnumerable<Attribute> GetAttributes(MemberInfo memberInfo, Type attributeType) =>
        GetCustomAttributes(memberInfo, attributeType);

    /// <summary>
    /// 获取特性集合
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="attributeType">特性类型</param>
    public static IEnumerable<Attribute> GetAttributes(Type type, Type attributeType) =>
        GetCustomAttributes(type, attributeType);

    /// <summary>
    /// 获取特性集合
    /// </summary>
    /// <param name="typeInfo">类型信息</param>
    /// <param name="attributeType">特性类型</param>
    public static IEnumerable<Attribute> GetAttributes(TypeInfo typeInfo, Type attributeType) =>
        GetCustomAttributes(typeInfo, attributeType);

    #endregion

    #region GetRequiredAttribute(获取必填特性)

    /// <summary>
    /// 获取特性
    /// </summary>
    /// <typeparam name="TAttribute">泛型特性</typeparam>
    /// <param name="fieldInfo">字段元数据</param>
    /// <exception cref="ArgumentException"></exception>
    public static TAttribute GetRequiredAttribute<TAttribute>(FieldInfo fieldInfo) where TAttribute : Attribute =>
        GetCustomAttribute<TAttribute>(fieldInfo) ??
        throw new ArgumentException($"There is no {typeof(TAttribute)} attribute can be found.");

    /// <summary>
    /// 获取特性
    /// </summary>
    /// <typeparam name="TAttribute">泛型特性</typeparam>
    /// <param name="memberInfo">元数据</param>
    /// <exception cref="ArgumentException"></exception>
    public static TAttribute GetRequiredAttribute<TAttribute>(MemberInfo memberInfo) where TAttribute : Attribute =>
        GetCustomAttribute<TAttribute>(memberInfo) ??
        throw new ArgumentException($"There is no {typeof(TAttribute)} attribute can be found.");

    /// <summary>
    /// 获取特性
    /// </summary>
    /// <typeparam name="TAttribute">泛型特性</typeparam>
    /// <param name="type">类型</param>
    /// <exception cref="ArgumentException"></exception>
    public static TAttribute GetRequiredAttribute<TAttribute>(Type type) where TAttribute : Attribute =>
        GetCustomAttribute<TAttribute>(type) ??
        throw new ArgumentException($"There is no {typeof(TAttribute)} attribute can be found.");

    /// <summary>
    /// 获取特性
    /// </summary>
    /// <typeparam name="TAttribute">泛型特性</typeparam>
    /// <param name="typeInfo">类型信息</param>
    /// <exception cref="ArgumentException"></exception>
    public static TAttribute GetRequiredAttribute<TAttribute>(TypeInfo typeInfo) where TAttribute : Attribute =>
        GetCustomAttribute<TAttribute>(typeInfo) ??
        throw new ArgumentException($"There is no {typeof(TAttribute)} attribute can be found.");

    /// <summary>
    /// 获取特性
    /// </summary>
    /// <param name="fieldInfo">字段信息</param>
    /// <param name="attributeType">特性类型</param>
    /// <exception cref="ArgumentException"></exception>
    public static Attribute GetRequiredAttribute(FieldInfo fieldInfo, Type attributeType) =>
        GetCustomAttribute(fieldInfo, attributeType) ??
        throw new ArgumentException($"There is no {attributeType} attribute can be found.");

    /// <summary>
    /// 获取特性
    /// </summary>
    /// <param name="memberInfo">元数据</param>
    /// <param name="attributeType">特性类型</param>
    /// <exception cref="ArgumentException"></exception>
    public static Attribute GetRequiredAttribute(MemberInfo memberInfo, Type attributeType) =>
        GetCustomAttribute(memberInfo, attributeType) ??
        throw new ArgumentException($"There is no {attributeType} attribute can be found.");

    /// <summary>
    /// 获取特性
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="attributeType">特性类型</param>
    /// <exception cref="ArgumentException"></exception>
    public static Attribute GetRequiredAttribute(Type type, Type attributeType) =>
        GetCustomAttribute(type, attributeType) ??
        throw new ArgumentException($"There is no {attributeType} attribute can be found.");

    /// <summary>
    /// 获取特性
    /// </summary>
    /// <param name="typeInfo">类型信息</param>
    /// <param name="attributeType">特性类型</param>
    /// <exception cref="ArgumentException"></exception>
    public static Attribute GetRequiredAttribute(TypeInfo typeInfo, Type attributeType) =>
        GetCustomAttribute(typeInfo, attributeType) ??
        throw new ArgumentException($"There is no {attributeType} attribute can be found.");

    #endregion

    #region GetRequiredAttributes(获取必填特性集合)

    /// <summary>
    /// 获取特性集合
    /// </summary>
    /// <typeparam name="TAttribute">泛型特性</typeparam>
    /// <param name="fieldInfo">字段元数据</param>
    /// <exception cref="ArgumentException"></exception>
    public static IEnumerable<TAttribute> GetRequiredAttributes<TAttribute>(FieldInfo fieldInfo) where TAttribute : Attribute =>
        GetRequiredCustomAttributes<TAttribute>(fieldInfo, $"There is no any {typeof(TAttribute)} attributes can be found.");

    /// <summary>
    /// 获取特性集合
    /// </summary>
    /// <typeparam name="TAttribute">泛型特性</typeparam>
    /// <param name="memberInfo">元数据</param>
    /// <exception cref="ArgumentException"></exception>
    public static IEnumerable<TAttribute> GetRequiredAttributes<TAttribute>(MemberInfo memberInfo) where TAttribute : Attribute =>
        GetRequiredCustomAttributes<TAttribute>(memberInfo, $"There is no any {typeof(TAttribute)} attributes can be found.");

    /// <summary>
    /// 获取特性集合
    /// </summary>
    /// <typeparam name="TAttribute">泛型特性</typeparam>
    /// <param name="type">类型</param>
    /// <exception cref="ArgumentException"></exception>
    public static IEnumerable<TAttribute> GetRequiredAttributes<TAttribute>(Type type) where TAttribute : Attribute =>
        GetRequiredCustomAttributes<TAttribute>(type, $"There is no any {typeof(TAttribute)} attributes can be found.");

    /// <summary>
    /// 获取特性集合
    /// </summary>
    /// <typeparam name="TAttribute">泛型特性</typeparam>
    /// <param name="typeInfo">类型信息</param>
    /// <exception cref="ArgumentException"></exception>
    public static IEnumerable<TAttribute> GetRequiredAttributes<TAttribute>(TypeInfo typeInfo) where TAttribute : Attribute =>
        GetRequiredCustomAttributes<TAttribute>(typeInfo, $"There is no any {typeof(TAttribute)} attributes can be found.");

    /// <summary>
    /// 获取特性集合
    /// </summary>
    /// <param name="fieldInfo">字段信息</param>
    /// <param name="attributeType">特性类型</param>
    /// <exception cref="ArgumentException"></exception>
    public static IEnumerable<Attribute> GetRequiredAttributes(FieldInfo fieldInfo, Type attributeType) =>
        GetRequiredCustomAttributes(fieldInfo, attributeType, $"There is no any {attributeType} attributes can be found.");

    /// <summary>
    /// 获取特性集合
    /// </summary>
    /// <param name="memberInfo">元数据</param>
    /// <param name="attributeType">特性类型</param>
    /// <exception cref="ArgumentException"></exception>
    public static IEnumerable<Attribute> GetRequiredAttributes(MemberInfo memberInfo, Type attributeType) =>
        GetRequiredCustomAttributes(memberInfo, attributeType, $"There is no any {attributeType} attributes can be found.");

    /// <summary>
    /// 获取特性集合
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="attributeType">特性类型</param>
    /// <exception cref="ArgumentException"></exception>
    public static IEnumerable<Attribute> GetRequiredAttributes(Type type, Type attributeType) =>
        GetRequiredCustomAttributes(type, attributeType, $"There is no any {attributeType} attributes can be found.");

    /// <summary>
    /// 获取特性集合
    /// </summary>
    /// <param name="typeInfo">类型信息</param>
    /// <param name="attributeType">特性类型</param>
    /// <exception cref="ArgumentException"></exception>
    public static IEnumerable<Attribute> GetRequiredAttributes(TypeInfo typeInfo, Type attributeType) =>
        GetRequiredCustomAttributes(typeInfo, attributeType, $"There is no any {attributeType} attributes can be found.");

    #endregion
}
