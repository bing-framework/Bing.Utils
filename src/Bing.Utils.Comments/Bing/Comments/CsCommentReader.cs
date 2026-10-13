using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace Bing.Comments;

/// <summary>
/// 读取 C# 程序集生成的 XML 文档注释。
/// </summary>
public static class CsCommentReader
{
    /// <summary>
    /// 读取任意成员的注释信息。
    /// </summary>
    /// <param name="member">需要读取注释的成员</param>
    /// <returns>成员注释；XML 文件或目标成员不存在时返回 null。</returns>
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
    /// 读取类型的注释信息。
    /// </summary>
    /// <param name="type">需要读取注释的类</param>
    /// <returns>类型注释；XML 文件或目标类型不存在时返回 null。</returns>
    public static CsComments Create(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));
        return CreateComment(type, CsCommentId.Create(type));
    }

    /// <summary>
    /// 读取构造函数的注释信息。
    /// </summary>
    /// <param name="ctor">需要读取注释的构造函数</param>
    /// <returns>构造函数注释；XML 文件或目标成员不存在时返回 null。</returns>
    public static CsComments Create(ConstructorInfo ctor)
    {
        if (ctor == null)
            throw new ArgumentNullException(nameof(ctor));
        return CreateComment(ctor, CsCommentId.Create(ctor));
    }

    /// <summary>
    /// 读取事件的注释信息。
    /// </summary>
    /// <param name="event">需要读取注释的事件</param>
    /// <returns>事件注释；XML 文件或目标成员不存在时返回 null。</returns>
    public static CsComments Create(EventInfo @event)
    {
        if (@event == null)
            throw new ArgumentNullException(nameof(@event));
        return CreateComment(@event, CsCommentId.Create(@event));
    }

    /// <summary>
    /// 读取方法的注释信息。
    /// </summary>
    /// <param name="method">需要读取注释的方法</param>
    /// <returns>方法注释；XML 文件或目标成员不存在时返回 null。</returns>
    public static CsComments Create(MethodInfo method)
    {
        if (method == null)
            throw new ArgumentNullException(nameof(method));
        return CreateComment(method, CsCommentId.Create(method));
    }

    /// <summary>
    /// 读取属性的注释信息。
    /// </summary>
    /// <param name="prop">需要读取注释的属性</param>
    /// <returns>属性注释；XML 文件或目标成员不存在时返回 null。</returns>
    public static CsComments Create(PropertyInfo prop)
    {
        if (prop == null)
            throw new ArgumentNullException(nameof(prop));
        return CreateComment(prop, CsCommentId.Create(prop));
    }

    /// <summary>
    /// 读取字段的注释信息。
    /// </summary>
    /// <param name="field">需要读取注释的字段</param>
    /// <returns>字段注释；XML 文件或目标成员不存在时返回 null。</returns>
    public static CsComments Create(FieldInfo field)
    {
        if (field == null)
            throw new ArgumentNullException(nameof(field));
        return CreateComment(field, CsCommentId.Create(field));
    }

    /// <summary> 
    /// 读取枚举的值和注释
    /// </summary>
    /// <param name="enumType">枚举的类型</param>
    public static Dictionary<Enum, string> CreateEnumComment(Type enumType) => CreateEnumComment<Enum>(enumType);

    /// <summary>
    /// 读取枚举的值和注释。
    /// </summary>
    /// <typeparam name="T">枚举的类型</typeparam>
    /// <returns>枚举值与注释文本的映射。</returns>
    public static Dictionary<T, string> CreateEnumComment<T>() where T : struct, IComparable, IFormattable, IConvertible => CreateEnumComment<T>(typeof(T));

    /// <summary> 
    /// 读取枚举的值和注释
    /// </summary>
    /// <typeparam name="T">枚举的类型</typeparam>
    /// <param name="enumType">枚举的类型</param>
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
    /// 获取成员的DescriptionAttribute中的描述，如果没有这个特性返回Name
    /// </summary>
    /// <param name="member">需要获取描述的成员</param>
    private static string GetDescription(MemberInfo member)
    {
        var attr = (DescriptionAttribute)Attribute.GetCustomAttribute(member, typeof(DescriptionAttribute));
        if (attr == null)
            return member.Name;
        return attr.Description ?? member.Name;
    }

    /// <summary>
    /// 成员信息列表
    /// </summary>
    [ThreadStatic]
    private static List<MemberInfo> _member;

    /// <summary>
    /// 创建注释对象，读取任意成员的注释信息，如果xml不存在或者注释不存在返回null
    /// </summary>
    /// <param name="member">成员对象</param>
    /// <param name="memberId">成员的 XML 文档标识</param>
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