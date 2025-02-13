using System.Reflection;
using AspectCore.Extensions.Reflection;
using Bing.Dynamic;

namespace Bing.Reflection;

/// <summary>
/// 类型访问器
/// </summary>
public static partial class TypeVisit
{
	#region Create Instance

    /// <summary>
    /// 根据指定类型和构造函数参数创建类型的实例。
    /// </summary>
    /// <param name="type">要创建实例的实例。</param>
    /// <param name="args">传递给构造函数的参数数组。</param>
    /// <returns>创建的类型实例。</returns>
    /// <exception cref="ArgumentNullException">如果<paramref name="type"/>为null，则抛出此异常。</exception>
    /// <remarks>
    /// 此方法提供了一种通用的机制来根据运行时类型信息动态创建对象实例。
    /// 它首先尝试找到与提供的参数匹配的构造函数，然后使用该构造函数创建类型的新实例。
    /// 如果没有提供参数（即<paramref name="args"/>为空或长度为0），则尝试使用无参数的构造函数创建实例。
    /// </remarks>
    public static object CreateInstance(Type type, params object[] args)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));
        return CreateInstanceImpl(type, args);
    }

    /// <summary>
    /// 根据指定的泛型类型和构造函数参数动态创建类型的实例。
    /// </summary>
    /// <typeparam name="TInstance">要创建实例的泛型类型。</typeparam>
    /// <param name="args">传递给构造函数的参数数组。</param>
    /// <returns>指定类型的新实例。</returns>
    /// <remarks>
    /// 此方法利用反射来动态创建指定类型的对象实例，适用于当类型在编写代码时未知或者需要在运行时决定的情况。
    /// 它首先尝试根据传递的参数找到匹配的构造函数，然后使用找到的构造函数创建类型的新实例。
    /// 如果没有提供参数（即<paramref name="args"/>为空或长度为0），则尝试使用无参数的构造函数创建实例。
    /// 此方法在内部调用<see cref="CreateInstanceImpl(Type, object[])"/>来执行实例化逻辑，并尝试将结果转换为<typeparamref name="TInstance"/>类型。
    /// 如果转换失败，可能返回默认值（对于值类型是0或false，对于引用类型是null）。
    /// </remarks>
    public static TInstance CreateInstance<TInstance>(params object[] args)
    {
        return CreateInstanceImpl(Types.Of<TInstance>(), args).AsOrDefault<TInstance>();
    }

    /// <summary>
    /// 根据提供的类型信息和构造函数参数动态创建指定类型的实例，并将其转换为泛型参数指定的类型。
    /// </summary>
    /// <typeparam name="TInstance">期望返回的实例的类型。</typeparam>
    /// <param name="type">要创建实例的类型。</param>
    /// <param name="args">传递给构造函数的参数数组。</param>
    /// <returns>转换为<typeparamref name="TInstance"/>类型的新实例。</returns>
    /// <exception cref="ArgumentNullException">如果<paramref name="type"/>为null，则抛出此异常。</exception>
    /// <remarks>
    /// 此方法使用反射来根据运行时提供的类型信息创建对象实例。它支持带参数的构造函数，允许在运行时动态地传递构造函数参数。
    /// 如果没有提供参数（即<paramref name="args"/>为空或长度为0），则尝试使用无参数的构造函数创建实例。
    /// 创建实例后，尝试将其转换为<typeparamref name="TInstance"/>类型。如果转换失败，则可能返回类型的默认值。
    /// 此方法适用于类型在编译时未知或需要根据条件动态选择类型的场景。
    /// </remarks>
    public static TInstance CreateInstance<TInstance>(Type type, params object[] args)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));
        return CreateInstanceImpl(type, args).AsOrDefault<TInstance>();
    }

    ///// <summary>
    ///// 根据类名和构造函数参数动态创建指定类型的实例，并将其转换为泛型参数指定的类型。
    ///// </summary>
    ///// <typeparam name="TInstance">期望返回的实例的类型。</typeparam>
    ///// <param name="className">要创建实例的类的完全限定名称。</param>
    ///// <param name="args">传递给构造函数的参数数组。</param>
    ///// <returns>转换为<typeparamref name="TInstance"/>类型的新实例。</returns>
    ///// <remarks>
    ///// 此方法首先尝试使用<paramref name="className"/>在当前加载的程序集中查找类型。如果未找到，
    ///// 则尝试在调用此方法的程序集中查找类型。一旦确定了类型，就会根据提供的参数使用反射创建该类型的实例。
    ///// 此方法支持带参数的构造函数，允许在运行时动态地传递构造函数参数。
    ///// 如果没有提供参数（即<paramref name="args"/>为空或长度为0），则尝试使用无参数的构造函数创建实例。
    ///// 创建实例后，尝试将其转换为<typeparamref name="TInstance"/>类型。如果转换失败，则可能返回类型的默认值。
    ///// 这个方法适用于当类型在编写代码时未知，或者需要根据条件在运行时选择类型的场景。
    ///// </remarks>
    //public static TInstance CreateInstance<TInstance>(string className, params object[] args)
    //{
    //    var type = Type.GetType(className) ?? Assembly.GetCallingAssembly().GetType(className);
    //    return CreateInstance<TInstance>(type, args);
    //}

    /// <summary>
    /// 实现创建类型实例的逻辑
    /// </summary>
    /// <param name="type">要创建实例的实例。</param>
    /// <param name="args">传递给构造函数的参数数组。</param>
    /// <returns>创建的类型实例。</returns>
    private static object CreateInstanceImpl(Type type, params object[] args)
    {
        return args is null || args.Length == 0
            ? type.GetConstructor(Type.EmptyTypes)?.GetReflector().Invoke()
            : type.GetConstructor(Types.Of(args))?.GetReflector().Invoke(args);
    }

    #endregion

    #region Create Instance for Dynamic Type

#if !NETSTANDARD
    /// <summary>
    /// 根据提供的属性名称和值动态创建一个对象实例，并设置其属性。
    /// </summary>
    /// <param name="values">一个字典，包含属性名称作为键和相应的属性值作为值。</param>
    /// <returns>一个动态创建的对象实例，其属性被设置为<paramref name="values"/>中提供的值。</returns>
    /// <remarks>
    /// 此方法首先基于<paramref name="values"/>中的键值对动态创建一个类型定义，其中键表示属性名称，值表示属性的类型。
    /// 使用此类型定义，然后实例化该类型的一个实例。对于字典中的每个键值对，设置新实例对应属性的值。
    /// </remarks>
    public static object CreateInstance(IDictionary<string, object> values)
    {
        var properties = values.ToDictionary(_ => _.Key, _ => _.Value.GetType());
        var dynamicType = TypeFactory.CreateType(properties);
        var @object = (DynamicBase)Activator.CreateInstance(dynamicType);
        foreach (var item in values) 
            @object.SetPropertyValue(item.Key,item.Value);
        return @object;
    }
#endif

    #endregion
}