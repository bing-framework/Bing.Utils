using System.Reflection;

#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Bing.Reflection;

/// <summary>
/// 类型访问器
/// </summary>
public static partial class TypeVisit
{
    /// <summary>
    /// 推测当前类型是否存在无参构造函数
    /// </summary>
    /// <param name="type">类型</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException">类型不可实例化时抛出。</exception>
    public static bool HasParameterlessConstructor(
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
        Type type)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));
        try
        {
            GetParameterlessConstructor(type);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// 获取当前类型的无参构造函数
    /// </summary>
    /// <param name="type">类型</param>
    /// <returns>公共无参构造函数。</returns>
    /// <exception cref="ArgumentNullException">类型为 null 时抛出。</exception>
    /// <exception cref="ArgumentException">类型不可实例化或不存在公共无参构造函数时抛出。</exception>
    public static ConstructorInfo GetParameterlessConstructor(
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
        Type type)
    {
        return GetMatchingConstructor(type, Type.EmptyTypes);
    }

    /// <summary>
    /// 获取命中参数的构造函数
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="constructorParameterTypes">构造函数参数类型</param>
    /// <returns>唯一兼容的公共构造函数。</returns>
    /// <exception cref="ArgumentNullException">类型或参数类型数组为 null 时抛出。</exception>
    /// <exception cref="ArgumentException">类型不可实例化、参数类型数组包含 null 或不存在兼容构造函数时抛出。</exception>
    /// <exception cref="AmbiguousMatchException">多个构造函数兼容时抛出。</exception>
    public static ConstructorInfo GetMatchingConstructor(
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
        Type type, Type[] constructorParameterTypes)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));
        if (constructorParameterTypes == null)
            throw new ArgumentNullException(nameof(constructorParameterTypes));
        if (constructorParameterTypes.Any(parameterType => parameterType == null))
            throw new ArgumentException("参数类型不能包含 null。", nameof(constructorParameterTypes));
        EnsureCreatableType(type);
        return StrictConstructorBinder.FindConstructor(type, constructorParameterTypes.ToArray());
    }
}

/// <summary>
/// 类型元数据访问器扩展
/// </summary>
public static partial class TypeMetaVisitExtensions
{
    /// <summary>
    /// 推测当前类型是否存在无参构造函数
    /// </summary>
    /// <param name="type">类型</param>
    public static bool HasParameterlessConstructor(
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
        this Type type) => TypeVisit.HasParameterlessConstructor(type);

    /// <summary>
    /// 获取当前类型的无参构造函数
    /// </summary>
    /// <param name="type">类型</param>
    public static ConstructorInfo GetParameterlessConstructor(
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
        this Type type) => TypeVisit.GetParameterlessConstructor(type);

    /// <summary>
    /// 获取命中参数的构造函数
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="constructorParameterTypes">构造函数参数类型</param>
    public static ConstructorInfo GetMatchingConstructor(
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
        this Type type, Type[] constructorParameterTypes) => TypeVisit.GetMatchingConstructor(type, constructorParameterTypes);
}
