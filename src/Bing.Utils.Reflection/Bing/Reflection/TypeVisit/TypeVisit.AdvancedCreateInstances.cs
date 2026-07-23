using System.Reflection;

namespace Bing.Reflection;

/// <summary>
/// 类型访问器。
/// </summary>
public static partial class TypeVisit
{
    /// <summary>
    /// 批量创建指定类型的实例。
    /// </summary>
    /// <param name="type">要创建实例的类型。</param>
    /// <param name="argumentSets">每个元素对应一次构造函数调用的参数数组。</param>
    /// <returns>按参数集合顺序创建的实例列表。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="type"/> 或 <paramref name="argumentSets"/> 为 null 时抛出。</exception>
    /// <exception cref="ArgumentException">当目标类型不可实例化，或不存在唯一兼容构造函数时抛出。</exception>
    /// <exception cref="InvalidOperationException">当构造函数执行失败时抛出。</exception>
    public static IReadOnlyList<object> CreateInstances(Type type, IEnumerable<object[]> argumentSets)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));
        if (argumentSets == null)
            throw new ArgumentNullException(nameof(argumentSets));
        EnsureCreatableType(type);

        var result = new List<object>();
        foreach (var arguments in argumentSets)
            result.Add(CreateInstanceStrict(type, arguments));
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

        Type closedType;
        try
        {
            closedType = openGenericType.MakeGenericType(genericArguments);
        }
        catch (ArgumentException exception)
        {
            throw new ArgumentException("泛型类型实参与开放泛型类型不兼容", nameof(genericArguments), exception);
        }

        return CreateInstanceStrict(closedType, args);
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
    public static TInstance CreateGenericInstance<TInstance>(Type openGenericType, Type[] genericArguments, params object[] constructorArguments)
    {
        var instance = CreateGenericInstance(openGenericType, genericArguments, constructorArguments);
        if (instance is TInstance typedInstance)
            return typedInstance;
        throw new InvalidCastException($"封闭泛型类型“{instance.GetType().FullName}”不可赋值给目标类型“{typeof(TInstance).FullName}”。");
    }

    /// <summary>
    /// 严格创建单个实例。
    /// </summary>
    /// <param name="type">要创建实例的类型。</param>
    /// <param name="args">构造函数参数。</param>
    /// <returns>创建的实例。</returns>
    /// <exception cref="ArgumentException">当目标类型不可实例化，或不存在唯一兼容构造函数时抛出。</exception>
    /// <exception cref="InvalidOperationException">当构造函数执行失败时抛出。</exception>
    private static object CreateInstanceStrict(Type type, object[] args)
    {
        EnsureCreatableType(type);
        args ??= Array.Empty<object>();
        var constructor = FindCompatibleConstructor(type, args);

        try
        {
            return constructor.Invoke(args);
        }
        catch (TargetInvocationException exception)
        {
            throw new InvalidOperationException($"调用类型“{type.FullName}”的构造函数时发生异常", exception.InnerException ?? exception);
        }
    }

    /// <summary>
    /// 查找唯一的兼容构造函数。
    /// </summary>
    /// <param name="type">目标类型。</param>
    /// <param name="args">构造函数参数。</param>
    /// <returns>唯一兼容的构造函数。</returns>
    /// <exception cref="ArgumentException">当不存在或存在多个兼容构造函数时抛出。</exception>
    private static ConstructorInfo FindCompatibleConstructor(Type type, object[] args)
    {
        var candidates = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .Where(constructor => IsConstructorCompatible(constructor, args))
            .ToArray();
        if (candidates.Length == 0)
            throw new ArgumentException($"类型“{type.FullName}”不存在与给定参数兼容的公共构造函数", nameof(args));
        if (candidates.Length > 1)
            throw new ArgumentException($"类型“{type.FullName}”存在多个与给定参数兼容的公共构造函数", nameof(args));
        return candidates[0];
    }

    /// <summary>
    /// 判断构造函数是否与给定实参兼容。
    /// </summary>
    /// <param name="constructor">构造函数元数据。</param>
    /// <param name="args">构造函数实参。</param>
    /// <returns>实参数量和类型均兼容时返回 true；否则返回 false。</returns>
    private static bool IsConstructorCompatible(ConstructorInfo constructor, object[] args)
    {
        var parameters = constructor.GetParameters();
        if (parameters.Length != args.Length)
            return false;

        for (var index = 0; index < parameters.Length; index++)
        {
            if (!IsArgumentCompatible(parameters[index].ParameterType, args[index]))
                return false;
        }

        return true;
    }

    /// <summary>
    /// 判断单个构造函数实参是否可赋值给参数类型。
    /// </summary>
    /// <param name="parameterType">构造函数参数类型。</param>
    /// <param name="argument">构造函数实参。</param>
    /// <returns>实参可赋值时返回 true；否则返回 false。</returns>
    private static bool IsArgumentCompatible(Type parameterType, object argument)
    {
        if (argument == null)
            return !parameterType.IsValueType || Nullable.GetUnderlyingType(parameterType) != null;
        return parameterType.IsInstanceOfType(argument);
    }

    /// <summary>
    /// 验证类型可通过公共构造函数创建实例。
    /// </summary>
    /// <param name="type">待验证的类型。</param>
    /// <exception cref="ArgumentException">当类型为接口、抽象类、开放泛型或指针类型时抛出。</exception>
    private static void EnsureCreatableType(Type type)
    {
        if (type.IsInterface || type.IsAbstract || type.ContainsGenericParameters || type.IsPointer)
            throw new ArgumentException($"类型“{type.FullName}”不能通过公共构造函数创建实例", nameof(type));
    }

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
