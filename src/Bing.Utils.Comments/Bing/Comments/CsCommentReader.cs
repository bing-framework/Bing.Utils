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
        var xpath = $"T:{type.FullName.Replace("+", ".")}";
        return CreateComment(type, xpath);
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
        var xpath = GetXPath("M", ctor.DeclaringType, ctor.IsStatic ? "#cctor" : "#ctor");
        xpath += GetXPathByParameters(ctor.GetParameters());
        return CreateComment(ctor, xpath);
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
        var xpath = GetXPath("E", @event.DeclaringType, @event.Name);
        return CreateComment(@event, xpath);
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
        var xpath = GetXPath("M", method.DeclaringType, method.Name);
        if (method.IsGenericMethod)
        {
            xpath += "``" + method.GetGenericArguments().Length;
            if (method.IsGenericMethodDefinition == false)
            {
                method = method.GetGenericMethodDefinition();
            }
        }
        xpath += GetXPathByParameters(method.GetParameters());
        return CreateComment(method, xpath);
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
        var xpath = GetXPath("P", prop.DeclaringType, prop.Name);
        xpath += GetXPathByParameters(prop.GetIndexParameters());
        return CreateComment(prop, xpath);
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
        var xpath = GetXPath("F", field.DeclaringType, field.Name);
        return CreateComment(field, xpath);
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
    /// <param name="path">XML 文档中对应成员的标识路径。</param>
    /// <returns>找到成员节点时返回解析结果；无法读取或正在递归解析时返回 <see langword="null"/>。</returns>
    private static CsComments CreateComment(MemberInfo member, string path)
    {
        if (_member == null)
            _member = new List<MemberInfo>();
        else if (_member.Contains(member))
            return null;
        _member.Add(member);

        var xml = CsCommentsXmlCache.Get(member);
        if (xml == null)
            return null;

        var node = xml.SelectNodes("/doc/members/member[@name='" + path.Replace('+', '.') + "']");
        if (node.Count == 0)
            return null;

        _member.RemoveAt(_member.Count - 1);
        return new CsComments(node[0]);
    }

    /// <summary>
    /// 根据反射参数生成 XML 文档成员标识中的参数部分。
    /// </summary>
    /// <param name="args">方法或索引器的反射参数。</param>
    /// <returns>不含参数时为 <see langword="null"/>；否则返回括号包围的参数类型列表。</returns>
    private static string GetXPathByParameters(ParameterInfo[] args)
    {
        if (args == null || args.Length == 0)
            return null;
        var list = new List<string>(args.Length);
        foreach (var item in args)
        {
            var type = item.ParameterType;
            list.Add(GetParameterName(type));
        }

        return "(" + string.Join(",", list.ToArray()) + ")";
    }

    /// <summary>
    /// 将类型信息转换为 XML 文档成员标识使用的名称。
    /// </summary>
    /// <param name="types">要转换的类型序列。</param>
    /// <returns>以花括号包围、逗号分隔的类型标识。</returns>
    private static string GetParameterName(Type[] types)
    {
        var list = new List<string>(types.Length);
        foreach (var type in types)
            list.Add(GetParameterName(type));
        return "{" + string.Join(",", list.ToArray()) + "}";
    }

    /// <summary>
    /// 将类型信息转换为 XML 文档成员标识使用的名称。
    /// </summary>
    /// <param name="type">要转换或用于成员标识的类型。</param>
    /// <returns>符合 XML 文档成员标识格式的类型名称。</returns>
    private static string GetParameterName(Type type)
    {
        if (type.IsGenericType)
        {
            if (type.IsGenericParameter)
                return "``" + type.GenericParameterPosition;
            else if (type.IsGenericTypeDefinition)
            {

            }
            else
            {
                return type.FullName.Substring(0, type.FullName.IndexOf('`')) +
                       GetParameterName(type.GetGenericArguments());
            }
        }

        return type.FullName;
    }

    /// <summary>
    /// 根据类型和成员名生成 XML 文档成员标识前缀。
    /// </summary>
    /// <param name="prefix">成员标识的种类前缀，例如类型、方法、属性、事件或字段。</param>
    /// <param name="type">要转换或用于成员标识的类型。</param>
    /// <param name="name">成员名称。</param>
    /// <returns>包含前缀、完整类型名和成员名的 XML 文档标识。</returns>
    private static string GetXPath(string prefix, Type type, string name)
    {
        if (type.IsGenericType && type.IsGenericTypeDefinition == false)
            type = type.GetGenericTypeDefinition();
        return $"{prefix}:{type.FullName.Replace("+", ".")}.{name}";
    }
}
