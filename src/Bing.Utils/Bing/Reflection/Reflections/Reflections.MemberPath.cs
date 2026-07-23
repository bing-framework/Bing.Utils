using Bing.Helpers;

// ReSharper disable once CheckNamespace
namespace Bing.Reflection;

/// <summary>
/// 反射操作。
/// </summary>
public static partial class Reflections
{
    /// <summary>
    /// 判断类型是否包含可访问的成员路径。
    /// </summary>
    /// <param name="type">路径起始类型。</param>
    /// <param name="memberPath">以英文句点分隔的成员路径。</param>
    /// <param name="options">成员路径访问选项。</param>
    /// <returns>路径中的每个成员均存在且可访问时返回 true；否则返回 false。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="type"/> 为 null 时抛出。</exception>
    public static bool HasMemberPath(Type type, string memberPath, MemberPathOptions options = null)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        return TryResolveMemberPath(type, memberPath, options, out _);
    }

    /// <summary>
    /// 尝试获取成员路径的值。
    /// </summary>
    /// <param name="instance">路径起始对象。</param>
    /// <param name="memberPath">以英文句点分隔的成员路径。</param>
    /// <param name="value">成功时为路径的值；失败时为 null。</param>
    /// <param name="options">成员路径访问选项。</param>
    /// <returns>成功读取路径值时返回 true；路径不存在、中间值为 null 或成员不可读时返回 false。</returns>
    public static bool TryGetMemberValueByPath(object instance, string memberPath, out object value, MemberPathOptions options = null)
    {
        if (instance == null)
        {
            value = null;
            return false;
        }
        options ??= new MemberPathOptions();
        if (!TryResolveMemberPath(instance.GetType(), memberPath, options, out var members))
        {
            value = null;
            return false;
        }

        object current = instance;
        foreach (var member in members)
        {
            if (current == null || !CanRead(member, options))
            {
                value = null;
                return false;
            }

            current = GetMemberValue(member, current);
        }

        value = current;
        return true;
    }

    /// <summary>
    /// 获取成员路径的值。
    /// </summary>
    /// <param name="instance">路径起始对象。</param>
    /// <param name="memberPath">以英文句点分隔的成员路径。</param>
    /// <param name="options">成员路径访问选项。</param>
    /// <returns>成员路径的值。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="instance"/> 为 null 时抛出。</exception>
    /// <exception cref="InvalidOperationException">当路径不存在、成员不可读或中间值为 null 时抛出。</exception>
    public static object GetMemberValueByPath(object instance, string memberPath, MemberPathOptions options = null)
    {
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));
        if (string.IsNullOrWhiteSpace(memberPath))
            throw new ArgumentException("成员路径不能为空", nameof(memberPath));
        if (TryGetMemberValueByPath(instance, memberPath, out var value, options))
            return value;
        throw new InvalidOperationException($"无法读取对象类型“{instance.GetType().FullName}”的成员路径“{memberPath}”。");
    }

    /// <summary>
    /// 尝试设置成员路径的值。
    /// </summary>
    /// <param name="instance">路径起始对象。</param>
    /// <param name="memberPath">以英文句点分隔的成员路径。</param>
    /// <param name="value">要设置的值。</param>
    /// <param name="options">成员路径访问选项。</param>
    /// <returns>成功设置路径值时返回 true；路径不存在、成员不可写、中间值为 null 或值不能转换时返回 false。</returns>
    public static bool TrySetMemberValueByPath(object instance, string memberPath, object value, MemberPathOptions options = null)
    {
        if (instance == null)
            return false;
        options ??= new MemberPathOptions();
        if (!TryResolveMemberPath(instance.GetType(), memberPath, options, out var members) || members.Count == 0)
            return false;

        object current = instance;
        for (var index = 0; index < members.Count - 1; index++)
        {
            var member = members[index];
            if (!CanRead(member, options))
                return false;

            current = GetMemberValue(member, current);

            if (current == null)
                return false;
        }

        var finalMember = members[members.Count - 1];
        if (!CanWrite(finalMember, options)
            || !TryGetAssignedValue(value, GetMemberType(finalMember), options, out var convertedValue))
            return false;

        SetMemberValue(finalMember, current, convertedValue);
        return true;
    }

    /// <summary>
    /// 设置成员路径的值。
    /// </summary>
    /// <param name="instance">路径起始对象。</param>
    /// <param name="memberPath">以英文句点分隔的成员路径。</param>
    /// <param name="value">要设置的值。</param>
    /// <param name="options">成员路径访问选项。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="instance"/> 为 null 时抛出。</exception>
    /// <exception cref="InvalidOperationException">当路径不可写、中间值为 null 或值不能转换时抛出。</exception>
    public static void SetMemberValueByPath(object instance, string memberPath, object value, MemberPathOptions options = null)
    {
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));
        if (string.IsNullOrWhiteSpace(memberPath))
            throw new ArgumentException("成员路径不能为空", nameof(memberPath));
        if (TrySetMemberValueByPath(instance, memberPath, value, options))
            return;
        throw new InvalidOperationException($"无法设置对象类型“{instance.GetType().FullName}”的成员路径“{memberPath}”。");
    }

    /// <summary>
    /// 判断类型是否包含可访问的成员路径。
    /// </summary>
    /// <param name="type">路径起始类型。</param>
    /// <param name="memberPath">以英文句点分隔的成员路径。</param>
    /// <param name="options">成员路径访问选项。</param>
    /// <returns>路径可访问时返回 true；否则返回 false。</returns>
    public static bool HasMember(Type type, string memberPath, MemberPathOptions options = null) =>
        type != null && HasMemberPath(type, memberPath, options);

    /// <summary>
    /// 判断对象是否包含可访问的成员路径。
    /// </summary>
    /// <param name="instance">路径起始对象。</param>
    /// <param name="memberPath">以英文句点分隔的成员路径。</param>
    /// <param name="options">成员路径访问选项。</param>
    /// <returns>对象非 null 且路径可访问时返回 true；否则返回 false。</returns>
    public static bool HasMember(object instance, string memberPath, MemberPathOptions options = null) =>
        instance != null && HasMemberPath(instance.GetType(), memberPath, options);

    /// <summary>
    /// 尝试获取成员路径的值。
    /// </summary>
    /// <param name="instance">路径起始对象。</param>
    /// <param name="memberPath">以英文句点分隔的成员路径。</param>
    /// <param name="value">成功时为路径的值；失败时为 null。</param>
    /// <param name="options">成员路径访问选项。</param>
    /// <returns>成功读取路径值时返回 true；否则返回 false。</returns>
    public static bool TryGetMemberValue(object instance, string memberPath, out object value, MemberPathOptions options = null) =>
        TryGetMemberValueByPath(instance, memberPath, out value, options);

    /// <summary>
    /// 获取成员路径的值。
    /// </summary>
    /// <param name="instance">路径起始对象。</param>
    /// <param name="memberPath">以英文句点分隔的成员路径。</param>
    /// <param name="options">成员路径访问选项。</param>
    /// <returns>成员路径的值。</returns>
    public static object GetMemberValue(object instance, string memberPath, MemberPathOptions options = null) =>
        GetMemberValueByPath(instance, memberPath, options);

    /// <summary>
    /// 尝试设置成员路径的值。
    /// </summary>
    /// <param name="instance">路径起始对象。</param>
    /// <param name="memberPath">以英文句点分隔的成员路径。</param>
    /// <param name="value">要设置的值。</param>
    /// <param name="options">成员路径访问选项。</param>
    /// <returns>成功设置路径值时返回 true；否则返回 false。</returns>
    public static bool TrySetMemberValue(object instance, string memberPath, object value, MemberPathOptions options = null) =>
        TrySetMemberValueByPath(instance, memberPath, value, options);

    /// <summary>
    /// 设置成员路径的值。
    /// </summary>
    /// <param name="instance">路径起始对象。</param>
    /// <param name="memberPath">以英文句点分隔的成员路径。</param>
    /// <param name="value">要设置的值。</param>
    /// <param name="options">成员路径访问选项。</param>
    public static void SetMemberValue(object instance, string memberPath, object value, MemberPathOptions options = null) =>
        SetMemberValueByPath(instance, memberPath, value, options);

    /// <summary>
    /// 尝试解析成员路径。
    /// </summary>
    /// <param name="type">路径起始类型。</param>
    /// <param name="memberPath">成员路径。</param>
    /// <param name="options">成员路径访问选项。</param>
    /// <param name="members">解析成功后的成员列表。</param>
    /// <returns>解析成功时返回 true；否则返回 false。</returns>
    private static bool TryResolveMemberPath(Type type, string memberPath, MemberPathOptions options, out IReadOnlyList<MemberInfo> members)
    {
        members = Array.Empty<MemberInfo>();
        if (string.IsNullOrWhiteSpace(memberPath))
            return false;

        options ??= new MemberPathOptions();
        var normalizedPath = NormalizeMemberPath(type, memberPath);
        var segments = normalizedPath.Split('.');
        if (segments.Any(string.IsNullOrWhiteSpace))
            return false;

        var result = new List<MemberInfo>(segments.Length);
        var currentType = type;
        foreach (var segment in segments)
        {
            var member = FindInstanceMember(currentType, segment, options);
            if (member == null)
                return false;
            result.Add(member);
            currentType = GetMemberType(member);
        }

        members = result;
        return true;
    }

    /// <summary>
    /// 移除成员路径中可选的完全限定类型前缀。
    /// </summary>
    /// <param name="type">路径起始类型。</param>
    /// <param name="memberPath">原始成员路径。</param>
    /// <returns>规范化后的成员路径。</returns>
    private static string NormalizeMemberPath(Type type, string memberPath)
    {
        var typeName = type.FullName;
        if (!string.IsNullOrEmpty(typeName) && memberPath.StartsWith(typeName + ".", StringComparison.Ordinal))
            return memberPath.Substring(typeName.Length + 1);
        return memberPath;
    }

    /// <summary>
    /// 在类型上查找指定名称的实例属性或字段。
    /// </summary>
    /// <param name="type">成员所属类型。</param>
    /// <param name="name">成员名称。</param>
    /// <param name="options">成员路径访问选项。</param>
    /// <returns>匹配的属性或字段；未找到时返回 null。</returns>
    private static MemberInfo FindInstanceMember(Type type, string name, MemberPathOptions options)
    {
        var flags = BindingFlags.Instance | BindingFlags.Public;
        if (options.IncludeNonPublic)
            flags |= BindingFlags.NonPublic;
        var comparison = options.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var property = type.GetProperties(flags).FirstOrDefault(candidate =>
            candidate.GetIndexParameters().Length == 0 && string.Equals(candidate.Name, name, comparison));
        if (property != null)
            return property;
        if (!options.IncludeFields)
            return null;
        return type.GetFields(flags).FirstOrDefault(candidate => string.Equals(candidate.Name, name, comparison));
    }

    /// <summary>
    /// 判断成员是否可读。
    /// </summary>
    /// <param name="member">成员元数据。</param>
    /// <param name="options">成员路径访问选项。</param>
    /// <returns>成员可读时返回 true；否则返回 false。</returns>
    private static bool CanRead(MemberInfo member, MemberPathOptions options)
    {
        return member switch
        {
            PropertyInfo property => CanReadProperty(property, options),
            FieldInfo field => options.IncludeNonPublic || field.IsPublic,
            _ => false
        };
    }

    /// <summary>
    /// 判断成员是否可写。
    /// </summary>
    /// <param name="member">成员元数据。</param>
    /// <param name="options">成员路径访问选项。</param>
    /// <returns>成员可写时返回 true；否则返回 false。</returns>
    private static bool CanWrite(MemberInfo member, MemberPathOptions options)
    {
        return member switch
        {
            PropertyInfo property => CanWriteProperty(property, options),
            FieldInfo field => (options.IncludeNonPublic || field.IsPublic) && !field.IsInitOnly && !field.IsLiteral,
            _ => false
        };
    }

    /// <summary>
    /// 判断属性是否可读取。
    /// </summary>
    /// <param name="property">属性元数据。</param>
    /// <param name="options">成员路径访问选项。</param>
    /// <returns>允许调用 getter 时返回 true；否则返回 false。</returns>
    private static bool CanReadProperty(PropertyInfo property, MemberPathOptions options)
    {
        var getter = property.GetMethod;
        return getter != null && (options.IncludeNonPublic || getter.IsPublic);
    }

    /// <summary>
    /// 判断属性是否可写入。
    /// </summary>
    /// <param name="property">属性元数据。</param>
    /// <param name="options">成员路径访问选项。</param>
    /// <returns>允许调用 setter 时返回 true；否则返回 false。</returns>
    private static bool CanWriteProperty(PropertyInfo property, MemberPathOptions options)
    {
        var setter = property.SetMethod;
        return setter != null && (options.IncludeNonPublic || setter.IsPublic);
    }

    /// <summary>
    /// 根据选项获取可写入成员的值。
    /// </summary>
    /// <param name="value">源值。</param>
    /// <param name="destinationType">目标成员类型。</param>
    /// <param name="options">成员路径访问选项。</param>
    /// <param name="convertedValue">可写入的值。</param>
    /// <returns>值可写入目标成员时返回 true；否则返回 false。</returns>
    private static bool TryGetAssignedValue(object value, Type destinationType, MemberPathOptions options, out object convertedValue)
    {
        if (value == null || value == DBNull.Value)
        {
            convertedValue = null;
            return !destinationType.IsValueType || Nullable.GetUnderlyingType(destinationType) != null;
        }

        if (destinationType.IsInstanceOfType(value))
        {
            convertedValue = value;
            return true;
        }

        if (!options.ConvertAssignedValue)
        {
            convertedValue = null;
            return false;
        }

        return MapperHelper.TryConvertValue(value, destinationType, out convertedValue);
    }

    /// <summary>
    /// 获取成员类型。
    /// </summary>
    /// <param name="member">成员元数据。</param>
    /// <returns>成员类型。</returns>
    /// <exception cref="ArgumentException">当成员不是属性或字段时抛出。</exception>
    private static Type GetMemberType(MemberInfo member)
    {
        return member switch
        {
            PropertyInfo property => property.PropertyType,
            FieldInfo field => field.FieldType,
            _ => throw new ArgumentException($"成员“{member.Name}”不是属性或字段", nameof(member))
        };
    }

    /// <summary>
    /// 获取成员值。
    /// </summary>
    /// <param name="member">成员元数据。</param>
    /// <param name="instance">成员所属对象。</param>
    /// <returns>成员值。</returns>
    /// <exception cref="ArgumentException">当成员不是属性或字段时抛出。</exception>
    private static object GetMemberValue(MemberInfo member, object instance)
    {
        return member switch
        {
            PropertyInfo property => property.GetValue(instance),
            FieldInfo field => field.GetValue(instance),
            _ => throw new ArgumentException($"成员“{member.Name}”不是属性或字段", nameof(member))
        };
    }

    /// <summary>
    /// 设置成员值。
    /// </summary>
    /// <param name="member">成员元数据。</param>
    /// <param name="instance">成员所属对象。</param>
    /// <param name="value">要设置的值。</param>
    /// <exception cref="ArgumentException">当成员不是属性或字段时抛出。</exception>
    private static void SetMemberValue(MemberInfo member, object instance, object value)
    {
        switch (member)
        {
            case PropertyInfo property:
                property.SetValue(instance, value);
                return;
            case FieldInfo field:
                field.SetValue(instance, value);
                return;
            default:
                throw new ArgumentException($"成员“{member.Name}”不是属性或字段", nameof(member));
        }
    }
}
