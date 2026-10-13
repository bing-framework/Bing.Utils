using System.Reflection;

#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Bing.Reflection;

/// <summary>
/// 类型访问器。
/// </summary>
public static partial class TypeVisit
{
    internal static int ConstructorScanCount => StrictConstructorBinder.ConstructorScanCount;
    internal static int ConstructorInvokerBuildCount => StrictConstructorBinder.ConstructorInvokerBuildCount;
    /// <summary>
    /// 批量创建指定类型的实例。
    /// </summary>
    /// <param name="type">要创建实例的类型。</param>
    /// <param name="argumentSets">每个元素对应一次构造函数调用的参数数组。</param>
    /// <returns>按参数集合顺序创建的实例列表。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="type"/> 或 <paramref name="argumentSets"/> 为 null 时抛出。</exception>
    /// <exception cref="ArgumentException">当目标类型不可实例化，或不存在唯一兼容构造函数时抛出。</exception>
    /// <exception cref="InvalidOperationException">当构造函数执行失败时抛出。</exception>
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("批量构造实例使用运行时生成的构造函数委托，要求运行时支持动态代码。")]
#endif
#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("批量构造实例依赖运行时构造函数元数据，裁剪时必须保留目标类型的公共构造函数。")]
#endif
    public static IReadOnlyList<object> CreateInstances(Type type, IEnumerable<object[]> argumentSets)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));
        if (argumentSets == null)
            throw new ArgumentNullException(nameof(argumentSets));
        EnsureCreatableType(type);

        var result = new List<object>();
        var index = 0;
        foreach (var arguments in argumentSets)
        {
            try
            {
                result.Add(CreateInstanceStrict(type, arguments));
            }
            catch (ArgumentException exception)
            {
                throw new ArgumentException(
                    $"无法创建类型“{type.FullName}”的第 {index} 个参数集合。构造参数类型：{GetArgumentTypeDescription(arguments)}。",
                    nameof(argumentSets), exception);
            }
            catch (InvalidOperationException exception)
            {
                throw new InvalidOperationException(
                    $"创建类型“{type.FullName}”的第 {index} 个参数集合时发生异常。",
                    exception);
            }

            index++;
        }
        return result;
    }

    /// <summary>
    /// 批量创建指定泛型类型的实例。
    /// </summary>
    /// <typeparam name="TInstance">要创建的实例类型。</typeparam>
    /// <param name="argumentSets">每个元素对应一次构造函数调用的参数数组。</param>
    /// <returns>按参数集合顺序创建的实例列表。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="argumentSets"/> 为 null 时抛出。</exception>
    /// <exception cref="ArgumentException">当目标类型不可实例化，或不存在唯一兼容构造函数时抛出。</exception>
    /// <exception cref="InvalidOperationException">当构造函数执行失败时抛出。</exception>
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("批量构造实例使用运行时生成的构造函数委托，要求运行时支持动态代码。")]
#endif
#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("批量构造实例依赖运行时构造函数元数据，裁剪时必须保留目标类型的公共构造函数。")]
#endif
    public static IReadOnlyList<TInstance> CreateInstances<TInstance>(IEnumerable<object[]> argumentSets)
    {
        var instances = CreateInstances(typeof(TInstance), argumentSets);
        return instances.Cast<TInstance>().ToArray();
    }

    /// <summary>
    /// 使用相同构造函数参数批量创建多个运行时类型的实例。
    /// </summary>
    /// <param name="types">要创建实例的运行时类型集合。</param>
    /// <param name="constructorArguments">传递给每个构造函数的参数。</param>
    /// <returns>按输入类型顺序创建的实例列表。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="types"/> 为 null 时抛出。</exception>
    /// <exception cref="ArgumentException">当类型集合包含 null、不可实例化类型或不存在唯一兼容构造函数时抛出。</exception>
    /// <exception cref="InvalidOperationException">当构造函数执行失败时抛出。</exception>
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("批量构造实例使用运行时生成的构造函数委托，要求运行时支持动态代码。")]
#endif
#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("批量构造实例依赖运行时类型和构造函数元数据，裁剪时必须保留目标类型的公共构造函数。")]
#endif
    public static IReadOnlyList<object> CreateInstances(IEnumerable<Type> types, params object[] constructorArguments)
    {
        if (types == null)
            throw new ArgumentNullException(nameof(types));

        var result = new List<object>();
        var index = 0;
        foreach (var type in types)
        {
            if (type == null)
                throw new ArgumentException($"类型集合的第 {index} 个元素为 null。", nameof(types));

            try
            {
                result.Add(CreateInstanceStrict(type, constructorArguments));
            }
            catch (ArgumentException exception)
            {
                throw new ArgumentException($"无法创建类型集合第 {index} 个类型“{type.FullName}”的实例。构造参数类型：{GetArgumentTypeDescription(constructorArguments)}。", nameof(types), exception);
            }
            catch (InvalidOperationException exception)
            {
                throw new InvalidOperationException($"创建类型集合第 {index} 个类型“{type.FullName}”的实例时发生异常。", exception);
            }

            index++;
        }

        return result;
    }

    /// <summary>
    /// 使用相同构造函数参数批量创建可赋值给指定类型的实例。
    /// </summary>
    /// <typeparam name="TInstance">期望的实例类型。</typeparam>
    /// <param name="types">要创建实例的运行时类型集合。</param>
    /// <param name="constructorArguments">传递给每个构造函数的参数。</param>
    /// <returns>按输入类型顺序创建的强类型实例列表。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="types"/> 为 null 时抛出。</exception>
    /// <exception cref="ArgumentException">当类型集合包含 null、不可实例化类型、不可赋值类型或不存在唯一兼容构造函数时抛出。</exception>
    /// <exception cref="InvalidOperationException">当构造函数执行失败时抛出。</exception>
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("批量构造实例使用运行时生成的构造函数委托，要求运行时支持动态代码。")]
#endif
#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("批量构造实例依赖运行时类型和构造函数元数据，裁剪时必须保留目标类型的公共构造函数。")]
#endif
    public static IReadOnlyList<TInstance> CreateInstances<TInstance>(IEnumerable<Type> types, params object[] constructorArguments)
    {
        if (types == null)
            throw new ArgumentNullException(nameof(types));

        var typedTypes = types.ToArray();
        for (var index = 0; index < typedTypes.Length; index++)
        {
            var type = typedTypes[index];
            if (type == null)
                throw new ArgumentException($"类型集合的第 {index} 个元素为 null。", nameof(types));
            if (!typeof(TInstance).IsAssignableFrom(type))
                throw new ArgumentException($"类型集合第 {index} 个类型“{type.FullName}”不可赋值给目标类型“{typeof(TInstance).FullName}”。", nameof(types));
        }

        return CreateInstances(typedTypes, constructorArguments).Cast<TInstance>().ToArray();
    }

    /// <summary>
    /// 创建指定开放泛型类型的封闭泛型实例。
    /// </summary>
    /// <param name="openGenericType">开放泛型类型定义。</param>
    /// <param name="genericArguments">用于封闭泛型类型的类型实参。</param>
    /// <param name="args">传递给构造函数的参数。</param>
    /// <returns>新建的封闭泛型实例。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="openGenericType"/> 或 <paramref name="genericArguments"/> 为 null 时抛出。</exception>
    /// <exception cref="ArgumentException">当类型不是开放泛型定义、类型实参无效，或不存在唯一兼容构造函数时抛出。</exception>
    /// <exception cref="InvalidOperationException">当构造函数执行失败时抛出。</exception>
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("封闭泛型类型并生成构造函数委托需要运行时动态代码。")]
#endif
#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("封闭泛型类型和构造函数实例化依赖运行时元数据，裁剪时必须保留相关类型及构造函数。")]
#endif
    public static object CreateGenericInstance(Type openGenericType, Type[] genericArguments, params object[] args)
    {
        if (openGenericType == null)
            throw new ArgumentNullException(nameof(openGenericType));
        if (genericArguments == null)
            throw new ArgumentNullException(nameof(genericArguments));
        if (!openGenericType.IsGenericTypeDefinition)
            throw new ArgumentException("类型必须是开放泛型类型定义", nameof(openGenericType));
        if (genericArguments.Any(argument => argument == null))
            throw new ArgumentException("泛型类型实参不能包含 null", nameof(genericArguments));
        if (openGenericType.GetGenericArguments().Length != genericArguments.Length)
            throw new ArgumentException("泛型类型实参数量与开放泛型类型定义不匹配", nameof(genericArguments));

        return CreateInstanceStrict(CloseGenericType(openGenericType, genericArguments), args);
    }

    /// <summary>
    /// 创建与指定封闭泛型类型一致的强类型实例。
    /// </summary>
    /// <typeparam name="TInstance">封闭泛型实例类型。</typeparam>
    /// <param name="openGenericType">开放泛型类型定义。</param>
    /// <param name="constructorArguments">传递给构造函数的参数。</param>
    /// <returns>创建的强类型实例。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="openGenericType"/> 为 null 时抛出。</exception>
    /// <exception cref="ArgumentException">当目标类型不是与 <typeparamref name="TInstance"/> 对应的开放泛型定义，或不存在唯一兼容构造函数时抛出。</exception>
    /// <exception cref="InvalidOperationException">当构造函数执行失败时抛出。</exception>
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("封闭泛型类型并生成构造函数委托需要运行时动态代码。")]
#endif
#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("封闭泛型类型和构造函数实例化依赖运行时元数据，裁剪时必须保留相关类型及构造函数。")]
#endif
    public static TInstance CreateGenericInstance<TInstance>(Type openGenericType, params object[] constructorArguments)
    {
        if (openGenericType == null)
            throw new ArgumentNullException(nameof(openGenericType));

        var instanceType = typeof(TInstance);
        if (!instanceType.IsConstructedGenericType || instanceType.GetGenericTypeDefinition() != openGenericType)
            throw new ArgumentException($"目标类型“{instanceType.FullName}”不是开放泛型类型“{openGenericType.FullName}”的封闭类型。", nameof(openGenericType));

        return (TInstance)CreateGenericInstance(openGenericType, instanceType.GetGenericArguments(), constructorArguments);
    }

    /// <summary>
    /// 使用显式泛型类型实参创建强类型实例。
    /// </summary>
    /// <typeparam name="TInstance">期望的实例类型。</typeparam>
    /// <param name="openGenericType">开放泛型类型定义。</param>
    /// <param name="genericArguments">用于封闭泛型类型的类型实参。</param>
    /// <param name="constructorArguments">传递给构造函数的参数。</param>
    /// <returns>创建的强类型实例。</returns>
    /// <exception cref="InvalidCastException">当封闭泛型类型不可赋值给 <typeparamref name="TInstance"/> 时抛出。</exception>
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("封闭泛型类型并生成构造函数委托需要运行时动态代码。")]
#endif
#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("封闭泛型类型和构造函数实例化依赖运行时元数据，裁剪时必须保留相关类型及构造函数。")]
#endif
    public static TInstance CreateGenericInstance<TInstance>(Type openGenericType, Type[] genericArguments, params object[] constructorArguments)
    {
        if (openGenericType == null)
            throw new ArgumentNullException(nameof(openGenericType));
        if (genericArguments == null)
            throw new ArgumentNullException(nameof(genericArguments));
        if (!openGenericType.IsGenericTypeDefinition)
            throw new ArgumentException("类型必须是开放泛型类型定义", nameof(openGenericType));
        if (genericArguments.Any(argument => argument == null))
            throw new ArgumentException("泛型类型实参不能包含 null", nameof(genericArguments));
        if (openGenericType.GetGenericArguments().Length != genericArguments.Length)
            throw new ArgumentException("泛型类型实参数量与开放泛型类型定义不匹配", nameof(genericArguments));

        var closedType = CloseGenericType(openGenericType, genericArguments);
        if (!typeof(TInstance).IsAssignableFrom(closedType))
            throw new InvalidCastException($"封闭泛型类型“{closedType.FullName}”不可赋值给目标类型“{typeof(TInstance).FullName}”。");
        return (TInstance)CreateInstanceStrict(closedType, constructorArguments);
    }

#if NET7_0_OR_GREATER
    [RequiresDynamicCode("封闭泛型类型需要运行时动态代码。")]
#endif
#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("封闭泛型类型需要运行时泛型元数据。")]
#endif
    private static Type CloseGenericType(Type openGenericType, Type[] genericArguments)
    {
        try
        {
            return openGenericType.MakeGenericType(genericArguments);
        }
        catch (ArgumentException exception)
        {
            throw new ArgumentException("泛型类型实参与开放泛型类型不兼容", nameof(genericArguments), exception);
        }
    }

    /// <summary>使用核心包共用的绑定器严格创建单个实例。</summary>
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("构造函数委托通过 Expression.Compile 生成，要求运行时支持动态代码。")]
#endif
#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("构造函数按运行时类型和参数匹配，裁剪时必须保留目标类型的公共构造函数。")]
#endif
    private static object CreateInstanceStrict(Type type, object[] args) =>
        StrictConstructorBinder.CreateInstance(type, args);

    private static void EnsureCreatableType(
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
        Type type) => StrictConstructorBinder.EnsureCreatableType(type);

    /// <summary>
    /// 获取构造参数类型说明。
    /// </summary>
    /// <param name="arguments">构造函数参数。</param>
    /// <returns>参数类型说明。</returns>
    private static string GetArgumentTypeDescription(object[] arguments)
    {
        if (arguments == null || arguments.Length == 0)
            return "无";
        return string.Join(", ", arguments.Select(argument => argument?.GetType().FullName ?? "null"));
    }
}
