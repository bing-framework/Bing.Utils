using Bing.Reflection;

namespace Bing.Helpers;

public static partial class MapperHelper
{
    /// <summary>
    /// 将源对象的成员映射到已有目标对象。
    /// </summary>
    /// <typeparam name="TSource">源对象类型。</typeparam>
    /// <typeparam name="TDestination">目标对象类型。</typeparam>
    /// <param name="source">源对象。</param>
    /// <param name="destination">已有目标对象。</param>
    /// <param name="options">对象映射选项。</param>
    /// <returns>成功写入的目标成员数量。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="source"/> 或 <paramref name="destination"/> 为 null 时抛出。</exception>
    /// <exception cref="ArgumentException">当目标对象为值类型时抛出。</exception>
    /// <exception cref="InvalidOperationException">当成员读取或写入过程中发生异常时抛出。</exception>
    public static int MapTo<TSource, TDestination>(TSource source, TDestination destination, MapOptions options = null)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));
        if (destination is null)
            throw new ArgumentNullException(nameof(destination));
        if (typeof(TDestination).IsValueType)
            throw new ArgumentException("已有目标实例映射不支持值类型目标，请使用现有 Map 方法", nameof(destination));

        options ??= new MapOptions();
        var comparer = options.IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var includedMembers = CopyMemberNames(options.IncludedMembers, comparer);
        var excludedMembers = CopyMemberNames(options.ExcludedMembers, comparer);
        var sourceMembers = GetReadableMembers(source.GetType(), options.IncludeFields).ToArray();
        var destinationMembers = GetWritableMembers(destination.GetType(), options.IncludeFields);
        var count = 0;

        foreach (var destinationMember in destinationMembers)
        {
            if (!ShouldMapMember(destinationMember.Name, includedMembers, excludedMembers))
                continue;
            var sourceMember = FindMatchingMember(sourceMembers, destinationMember.Name, options.IgnoreCase, source.GetType());
            if (sourceMember == null)
                continue;

            object value;
            try
            {
                value = GetMemberValue(sourceMember, source);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException($"读取源类型“{source.GetType().FullName}”的成员“{sourceMember.Name}”时发生异常", exception);
            }

            if (value == null && options.IgnoreNullValues)
                continue;

            var destinationType = GetMemberType(destinationMember);
            if (!TryGetMappedValue(value, destinationType, options.ConvertValues, out var convertedValue))
            {
                if (options.IgnoreConversionErrors)
                    continue;
                throw CreateMapConversionException(source.GetType(), sourceMember.Name, value, destination.GetType(), destinationMember.Name, destinationType, null);
            }

            try
            {
                SetMemberValue(destinationMember, destination, convertedValue);
                count++;
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    $"将源成员“{sourceMember.Name}”的值写入目标类型“{destination.GetType().FullName}”的成员“{destinationMember.Name}”时发生异常", exception);
            }
        }

        return count;
    }

    /// <summary>
    /// 将源集合映射为新的目标对象列表。
    /// </summary>
    /// <typeparam name="TSource">源元素类型。</typeparam>
    /// <typeparam name="TDestination">目标元素类型。</typeparam>
    /// <param name="source">源集合。</param>
    /// <param name="options">对象映射选项。</param>
    /// <returns>按源集合顺序创建的目标对象列表；源集合中的 null 元素会被跳过。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="source"/> 为 null 时抛出。</exception>
    /// <exception cref="ArgumentException">当目标元素类型为值类型时抛出。</exception>
    public static List<TDestination> MapList<TSource, TDestination>(IEnumerable<TSource> source, MapOptions options = null)
        where TDestination : new()
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (typeof(TDestination).IsValueType)
            throw new ArgumentException("列表映射不支持值类型目标，请使用现有 Map 方法", nameof(TDestination));

        var result = new List<TDestination>();
        foreach (var item in source)
        {
            if (item is null)
                continue;

            var destination = new TDestination();
            MapTo(item, destination, options);
            result.Add(destination);
        }

        return result;
    }

    /// <summary>
    /// 获取可读取的公共实例成员。
    /// </summary>
    /// <param name="type">成员所属类型。</param>
    /// <param name="includeFields">是否包含字段。</param>
    /// <returns>按属性优先、字段次之排序的可读取成员。</returns>
    private static IEnumerable<MemberInfo> GetReadableMembers(Type type, bool includeFields)
    {
        var properties = TypeReflections.GetPublicInstanceProperties(type)
            .Where(property => property.GetMethod != null && property.GetMethod.IsPublic)
            .Cast<MemberInfo>();
        if (!includeFields)
            return properties;

        return properties.Concat(type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(field => !field.IsStatic)
                .Cast<MemberInfo>())
            .OrderByDescending(IsPropertyMember)
            .ThenBy(member => GetInheritanceDepth(type, member.DeclaringType))
            .ThenBy(member => member.Name, StringComparer.Ordinal);
    }

    /// <summary>
    /// 获取可写入的公共实例成员。
    /// </summary>
    /// <param name="type">成员所属类型。</param>
    /// <param name="includeFields">是否包含字段。</param>
    /// <returns>按属性优先、字段次之排序的可写成员。</returns>
    private static IEnumerable<MemberInfo> GetWritableMembers(Type type, bool includeFields)
    {
        var properties = TypeReflections.GetPublicInstanceProperties(type)
            .Where(property => property.SetMethod != null && property.SetMethod.IsPublic)
            .Cast<MemberInfo>();
        if (!includeFields)
            return properties;

        return properties.Concat(type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(field => !field.IsStatic && !field.IsLiteral && !field.IsInitOnly)
                .Cast<MemberInfo>())
            .OrderByDescending(IsPropertyMember)
            .ThenBy(member => GetInheritanceDepth(type, member.DeclaringType))
            .ThenBy(member => member.Name, StringComparer.Ordinal);
    }

    /// <summary>
    /// 获取成员的声明类型。
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
    /// 读取成员值。
    /// </summary>
    /// <param name="member">成员元数据。</param>
    /// <param name="instance">成员所属对象。</param>
    /// <returns>成员当前值。</returns>
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
    /// 写入成员值。
    /// </summary>
    /// <param name="member">成员元数据。</param>
    /// <param name="instance">成员所属对象。</param>
    /// <param name="value">要写入的值。</param>
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

    /// <summary>
    /// 根据选项获取可写入的目标值。
    /// </summary>
    /// <param name="value">源值。</param>
    /// <param name="destinationType">目标成员类型。</param>
    /// <param name="convertValues">是否允许转换。</param>
    /// <param name="convertedValue">可写入的值。</param>
    /// <returns>值可写入目标成员时返回 true；否则返回 false。</returns>
    private static bool TryGetMappedValue(object value, Type destinationType, bool convertValues, out object convertedValue)
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

        if (!convertValues)
        {
            convertedValue = null;
            return false;
        }

        return TryConvertValue(value, destinationType, out convertedValue);
    }

    /// <summary>
    /// 复制成员名称集合，避免映射期间受到调用方修改影响。
    /// </summary>
    /// <param name="members">原始成员名称集合。</param>
    /// <param name="comparer">成员名称比较器。</param>
    /// <returns>成员名称快照。</returns>
    private static ISet<string> CopyMemberNames(ISet<string> members, IEqualityComparer<string> comparer)
    {
        return members == null ? new HashSet<string>(comparer) : new HashSet<string>(members, comparer);
    }

    /// <summary>
    /// 判断成员是否符合包含和排除规则。
    /// </summary>
    /// <param name="memberName">成员名称。</param>
    /// <param name="includedMembers">包含成员集合。</param>
    /// <param name="excludedMembers">排除成员集合。</param>
    /// <returns>成员允许映射时返回 true；否则返回 false。</returns>
    private static bool ShouldMapMember(string memberName, ISet<string> includedMembers, ISet<string> excludedMembers)
    {
        if (excludedMembers.Contains(memberName))
            return false;
        return includedMembers.Count == 0 || includedMembers.Contains(memberName);
    }

    /// <summary>
    /// 从候选源成员中选择与目标名称匹配的唯一成员。
    /// </summary>
    /// <param name="members">候选源成员。</param>
    /// <param name="name">目标成员名称。</param>
    /// <param name="ignoreCase">是否忽略大小写。</param>
    /// <param name="runtimeType">源对象运行时类型。</param>
    /// <returns>唯一匹配的源成员；未匹配时返回 null。</returns>
    /// <exception cref="AmbiguousMatchException">当多个候选成员无法按确定规则消歧时抛出。</exception>
    private static MemberInfo FindMatchingMember(IEnumerable<MemberInfo> members, string name, bool ignoreCase, Type runtimeType = null)
    {
        var exactMatches = members.Where(member => string.Equals(member.Name, name, StringComparison.Ordinal)).ToArray();
        if (exactMatches.Length > 0)
            return SelectPreferredMember(exactMatches, name, runtimeType);
        if (!ignoreCase)
            return null;

        var insensitiveMatches = members.Where(member => string.Equals(member.Name, name, StringComparison.OrdinalIgnoreCase)).ToArray();
        return insensitiveMatches.Length == 0 ? null : SelectPreferredMember(insensitiveMatches, name, runtimeType);
    }

    /// <summary>
    /// 按属性优先和派生类型优先规则选择成员。
    /// </summary>
    /// <param name="members">同名候选成员。</param>
    /// <param name="name">用于异常信息的成员名称。</param>
    /// <param name="runtimeType">源对象运行时类型。</param>
    /// <returns>唯一优先成员。</returns>
    /// <exception cref="AmbiguousMatchException">当最高优先级存在多个候选成员时抛出。</exception>
    private static MemberInfo SelectPreferredMember(IEnumerable<MemberInfo> members, string name, Type runtimeType)
    {
        var candidates = members.ToArray();
        var properties = candidates.Where(IsPropertyMember).ToArray();
        var preferred = properties.Length > 0 ? properties : candidates;
        runtimeType ??= preferred[0].ReflectedType ?? preferred[0].DeclaringType;
        var minimumDepth = preferred.Min(member => GetInheritanceDepth(runtimeType, member.DeclaringType));
        var best = preferred.Where(member => GetInheritanceDepth(runtimeType, member.DeclaringType) == minimumDepth).ToArray();
        if (best.Length != 1)
            throw new AmbiguousMatchException($"成员名称“{name}”存在多个无法消歧的映射候选项。");
        return best[0];
    }

    /// <summary>
    /// 判断成员是否为属性。
    /// </summary>
    /// <param name="member">成员元数据。</param>
    /// <returns>成员为属性时返回 true；否则返回 false。</returns>
    private static bool IsPropertyMember(MemberInfo member) => member is PropertyInfo;

    /// <summary>
    /// 获取声明类型相对于运行时类型的继承深度。
    /// </summary>
    /// <param name="runtimeType">运行时类型。</param>
    /// <param name="declaringType">成员声明类型。</param>
    /// <returns>运行时类型声明成员时返回 0；继承层级越深，返回值越大。</returns>
    private static int GetInheritanceDepth(Type runtimeType, Type declaringType)
    {
        var depth = 0;
        for (var current = runtimeType; current != null; current = current.BaseType, depth++)
        {
            if (current == declaringType)
                return depth;
        }

        return int.MaxValue;
    }
}
