using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bing.Reflection;

/// <summary>
/// 类型访问器
/// </summary>
public static partial class TypeVisit
{
    /// <summary>
    /// 获取给定 <see cref="TypeInfo"/> 的名称
    /// </summary>
    /// <param name="type">类型</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static string GetFullName(Type type)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));
        return type.FullName;
    }

    /// <summary>
    /// 获取给定 <see cref="Type"/> 的名称、程序集名称
    /// </summary>
    /// <param name="type">类型</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static string GetFullNameWithAssemblyName(Type type)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));
        return type.FullName + ", " + type.Assembly.GetName().Name;
    }

    /// <summary>
    /// 获取给定 <see cref="TypeInfo"/> 的完全限定名。
    /// </summary>
    /// <param name="type">类型</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static string GetFullyQualifiedName(Type type)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));
        var sb = new StringBuilder();
        if (type.IsGenericType)
        {
            sb.Append(type.GetGenericTypeDefinition().FullName)
                .Append("[");
            var genericArgs = type.GetGenericArguments().ToList();
            for (var i = 0; i < genericArgs.Count; i++)
            {
                sb.Append(genericArgs[i].GetFullyQualifiedName());
                if (i != genericArgs.Count - 1)
                    sb.Append(", ");
            }

            sb.Append("]");
        }
        else
        {
            if (!string.IsNullOrEmpty(type.FullName))
                sb.Append(type.FullName);
            else if (!string.IsNullOrEmpty(type.Name))
                sb.Append(type.Name);
            else
                sb.Append(type);
        }

        return sb.ToString();
    }
}

/// <summary>
/// 类型元数据访问器扩展
/// </summary>
public static partial class TypeMetaVisitExtensions
{
    /// <summary>
    /// 获取给定 <see cref="Type"/> 的名称、程序集名称
    /// </summary>
    /// <param name="type">类型</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetFullNameWithAssemblyName(this Type type) => TypeVisit.GetFullNameWithAssemblyName(type);

    /// <summary>
    /// 获取给定 <see cref="TypeInfo"/> 的完全限定名。
    /// </summary>
    /// <param name="type">类型</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static string GetFullyQualifiedName(this Type type)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));
        return TypeVisit.GetFullyQualifiedName(type);
    }
}