using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace Bing.Comments;

/// <summary>
/// 根据反射成员读取程序集生成的 XML 文档注释。
/// </summary>
public static class CsCommentReader
{
    /// <summary>
    /// 读取指定反射成员的文档注释。
    /// </summary>
    /// <param name="member">要读取注释的类型、构造函数、事件、字段、方法或属性。</param>
    /// <returns>找到文档注释时返回解析结果；没有注释或 XML 文档时返回 <see langword="null"/>。</returns>
    /// <exception cref="NotSupportedException">成员类型不受支持时抛出。</exception>
    public static CsComments Create(MemberInfo member)
    {
        if (member == null)
            throw new ArgumentNullException(nameof(member));
        switch (member.MemberType)
        {
            case MemberTypes.Constructor:
                return Create((ConstructorInfo)member);
            case MemberTypes.Event:
                return Create((EventInfo)member);
            case MemberTypes.Field:
                return Create((FieldInfo)member);
            case MemberTypes.Method:
                return Create((MethodInfo)member);
            case MemberTypes.Property:
                return Create((PropertyInfo)member);
            case MemberTypes.TypeInfo:
                return Create((Type)member);
            case MemberTypes.NestedType:
                if (member is Type type)
                    return Create(type);
                break;
            case MemberTypes.Custom:
            case MemberTypes.All:
            default:
                break;
        }
        throw new NotSupportedException("不支持当前类型的对象");
    }

    /// <summary>
    /// 读取指定类型的文档注释。
    /// </summary>
    /// <param name="type">要读取注释的类型。</param>
    /// <returns>找到文档注释时返回解析结果；没有注释或 XML 文档时返回 <see langword="null"/>。</returns>
    public static CsComments Create(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));
        return CreateComment(type, CsCommentId.Create(type));
    }

    /// <summary>
    /// 读取指定构造函数的文档注释。
    /// </summary>
    /// <param name="ctor">要读取注释的构造函数。</param>
    /// <returns>找到文档注释时返回解析结果；没有注释或 XML 文档时返回 <see langword="null"/>。</returns>
    public static CsComments Create(ConstructorInfo ctor)
    {
        if (ctor == null)
            throw new ArgumentNullException(nameof(ctor));
        return CreateComment(ctor, CsCommentId.Create(ctor));
    }

    /// <summary>
    /// 读取指定事件的文档注释。
    /// </summary>
    /// <param name="event">要读取注释的事件。</param>
    /// <returns>找到文档注释时返回解析结果；没有注释或 XML 文档时返回 <see langword="null"/>。</returns>
    public static CsComments Create(EventInfo @event)
    {
        if (@event == null)
            throw new ArgumentNullException(nameof(@event));
        return CreateComment(@event, CsCommentId.Create(@event));
    }

    /// <summary>
    /// 读取指定方法的文档注释。
    /// </summary>
    /// <param name="method">要读取注释的方法。</param>
    /// <returns>找到文档注释时返回解析结果；没有注释或 XML 文档时返回 <see langword="null"/>。</returns>
    public static CsComments Create(MethodInfo method)
    {
        if (method == null)
            throw new ArgumentNullException(nameof(method));
        return CreateComment(method, CsCommentId.Create(method));
    }

    /// <summary>
    /// 读取指定属性或索引器的文档注释。
    /// </summary>
    /// <param name="prop">要读取注释的属性或索引器。</param>
    /// <returns>找到文档注释时返回解析结果；没有注释或 XML 文档时返回 <see langword="null"/>。</returns>
    public static CsComments Create(PropertyInfo prop)
    {
        if (prop == null)
            throw new ArgumentNullException(nameof(prop));
        return CreateComment(prop, CsCommentId.Create(prop));
    }

    /// <summary>
    /// 读取指定字段的文档注释。
    /// </summary>
    /// <param name="field">要读取注释的字段。</param>
    /// <returns>找到文档注释时返回解析结果；没有注释或 XML 文档时返回 <see langword="null"/>。</returns>
    public static CsComments Create(FieldInfo field)
    {
        if (field == null)
            throw new ArgumentNullException(nameof(field));
        return CreateComment(field, CsCommentId.Create(field));
    }

    /// <summary>
    /// 创建枚举值到说明文本的映射。
    /// </summary>
    /// <param name="enumType">要读取成员说明的枚举类型。</param>
    /// <returns>每个枚举值及其文档注释或备用描述文本组成的映射。</returns>
    public static Dictionary<Enum, string> CreateEnumComment(Type enumType) => CreateEnumComment<Enum>(enumType);

    /// <summary>
    /// 创建枚举值到说明文本的映射。
    /// </summary>
    /// <typeparam name="T">要读取说明的枚举类型。</typeparam>
    /// <returns>每个枚举值及其文档注释或备用描述文本组成的映射。</returns>
    public static Dictionary<T, string> CreateEnumComment<T>() where T : struct, IComparable, IFormattable, IConvertible => CreateEnumComment<T>(typeof(T));

    /// <summary>
    /// 创建枚举值到说明文本的映射。
    /// </summary>
    /// <typeparam name="T">要读取说明的枚举类型。</typeparam>
    /// <param name="enumType">要读取成员说明的枚举类型。</param>
    /// <returns>每个枚举值及其文档注释或备用描述文本组成的映射。</returns>
    private static Dictionary<T, string> CreateEnumComment<T>(Type enumType)
    {
        if (enumType == null)
            throw new ArgumentNullException(nameof(enumType));
        if (enumType.IsEnum == false)
            throw new ArgumentOutOfRangeException(nameof(enumType), "enumType 不是 System.Enum");
        var maps = new Dictionary<T, string>();
        var array = Enum.GetValues(enumType);
        foreach (T item in array)
        {
            var field = enumType.GetField(item.ToString());
            var comment = Create(field);
            if (comment != null && comment.Summary != null)
            {
                maps.Add(item, comment.Summary);
            }
            else
            {
                maps.Add(item, GetDescription(field));
            }
        }
        return maps;
    }

    /// <summary>
    /// 获取成员的 <see cref="DescriptionAttribute"/> 描述；特性缺失时返回成员名称。
    /// </summary>
    /// <param name="member">要读取描述文本的成员。</param>
    /// <returns>特性中的描述；特性缺失时为成员名称。</returns>
    private static string GetDescription(MemberInfo member)
    {
        var attr = (DescriptionAttribute)Attribute.GetCustomAttribute(member, typeof(DescriptionAttribute));
        if (attr == null)
            return member.Name;
        return attr.Description ?? member.Name;
    }

    /// <summary>
    /// 当前线程正在解析的成员，用于阻止注释解析递归。
    /// </summary>
    [ThreadStatic]
    private static List<MemberInfo> _member;

    /// <summary>
    /// 按 XML 文档成员路径解析注释；XML 文档或对应成员节点不存在时返回 <see langword="null"/>。
    /// </summary>
    /// <param name="member">要解析注释的反射成员。</param>
    /// <param name="memberId">XML 文档中对应成员的标识。</param>
    /// <returns>找到成员节点时返回解析结果；无法读取或正在递归解析时返回 <see langword="null"/>。</returns>
    private static CsComments CreateComment(MemberInfo member, string memberId)
    {
        if (_member == null)
            _member = new List<MemberInfo>();
        else if (_member.Contains(member))
            return null;
        _member.Add(member);

        try
        {
            var node = CsCommentsXmlCache.GetMember(member, memberId);
            if (node == null)
                return null;
            return new CsComments(CsCommentsInheritanceResolver.Resolve(member, memberId, node));
        }
        finally
        {
            _member.RemoveAt(_member.Count - 1);
        }
    }
}
