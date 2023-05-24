using AspectCore.Extensions.Reflection;

// ReSharper disable once CheckNamespace
namespace Bing.Reflection;

/// <summary>
/// 类型 操作
/// </summary>
public static partial class Types
{
    #region DefaultValue(获取默认值)

    /// <summary>
    /// 获取默认值
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    public static T DefaultValue<T>() => TypeDefault.Of<T>();

    /// <summary>
    /// 获取默认值
    /// </summary>
    /// <param name="type">对象类型，仅支持值类型</param>
    /// <returns>如果是类型为默认值则返回默认值，否则返回 null</returns>
    public static object DefaultValue(Type type)
    {
        if (type.IsValueType)
            return Activator.CreateInstance(type);
        return null;
    }

    #endregion

    #region IsGenericImplementation(是否泛型实现类型)

    /// <summary>
    /// 是否泛型实现类型
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="genericType">泛型类型</param>
    public static bool IsGenericImplementation(Type type, Type genericType) => TypeJudgment.IsGenericImplementation(type, genericType);

    /// <summary>
    /// 是否泛型实现类型
    /// </summary>
    /// <typeparam name="TGot">类型</typeparam>
    /// <typeparam name="TGeneric">泛型类型</typeparam>
    public static bool IsGenericImplementation<TGot, TGeneric>() => TypeJudgment.IsGenericImplementation<TGot, TGeneric>();

    #endregion

    #region GetRawTypeFromGenericClass(获取原始类型)

    /// <summary>
    /// 获取原始类型。当类型从泛型类型中继承时，获取泛型类型中与该类型相对应的第一个类型参数
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="genericType">泛型类型</param>
    public static Type GetRawTypeFromGenericClass(Type type, Type genericType) => Reflections.GetRawTypeFromGenericClass(type, genericType);

    /// <summary>
    /// 获取原始类型。当类型从泛型类型中继承时，获取泛型类型中与该类型相对应的第一个类型参数
    /// </summary>
    /// <typeparam name="TGot">类型</typeparam>
    /// <typeparam name="TGeneric">泛型类型</typeparam>
    public static Type GetRawTypeFromGenericClass<TGot, TGeneric>() => Reflections.GetRawTypeFromGenericClass<TGot, TGeneric>();

    #endregion

    #region IsDefaultValue(是否默认值)

    /// <summary>
    /// 是否默认值
    /// </summary>
    /// <param name="obj">对象</param>
    public static bool IsDefaultValue(object obj)
    {
        if (obj == null)
            return true;
        return obj.Equals(DefaultValue(obj.GetType()));
    }

    /// <summary>
    /// 是否默认值
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="obj">对象</param>
    public static bool IsDefaultValue<T>(T obj)
    {
        if (obj == null)
            return true;
        return obj.Equals(DefaultValue<T>());
    }

    #endregion

    #region CreateInstance(创建实例)

    /// <summary>
    /// 创建实例
    /// </summary>
    /// <typeparam name="TInstance">实例类型</typeparam>
    /// <param name="args">参数</param>
    public static TInstance CreateInstance<TInstance>(params object[] args) =>
        args is null || args.Length == 0
            ? CreateInstanceCore<TInstance>()
            : CreateInstanceCore<TInstance>(args);

    /// <summary>
    /// 创建实例
    /// </summary>
    /// <typeparam name="TInstance">实例类型</typeparam>
    /// <param name="className">实例名称</param>
    /// <param name="args">参数</param>
    public static TInstance CreateInstance<TInstance>(string className, params object[] args)
    {
        var type = Type.GetType(className) ?? Assembly.GetCallingAssembly().GetType(className);
        return CreateInstance<TInstance>(type, args);
    }

    /// <summary>
    /// 创建实例
    /// </summary>
    /// <typeparam name="TInstance">实例类型</typeparam>
    /// <param name="type">实例类型</param>
    /// <param name="args">参数</param>
    public static TInstance CreateInstance<TInstance>(Type type, params object[] args) =>
        CreateInstance(type, args) is TInstance ret ? ret : default;

    /// <summary>
    /// 创建实例
    /// </summary>
    /// <param name="type">实例类型</param>
    /// <param name="args">参数</param>
    public static object CreateInstance(Type type, params object[] args) => args is null || args.Length == 0
        ? CreateInstanceCore(type)
        : CreateInstanceCore(type, args);

    /// <summary>
    /// 创建实例
    /// </summary>
    /// <typeparam name="TInstance">实例类型</typeparam>
    private static TInstance CreateInstanceCore<TInstance>() =>
        CreateInstanceCore(typeof(TInstance)) is TInstance ret ? ret : default;

    /// <summary>
    /// 创建实例
    /// </summary>
    /// <typeparam name="TInstance">实例类型</typeparam>
    /// <param name="args">参数</param>
    private static TInstance CreateInstanceCore<TInstance>(object[] args) =>
        CreateInstanceCore(typeof(TInstance), args) is TInstance ret ? ret : default;

    /// <summary>
    /// 创建实例
    /// </summary>
    /// <param name="type">实例类型</param>
    private static object CreateInstanceCore(Type type) => type.GetConstructors()
        .FirstOrDefault(x => !x.GetParameters().Any())?.GetReflector().Invoke();

    /// <summary>
    /// 创建实例
    /// </summary>
    /// <param name="type">实例类型</param>
    /// <param name="args">参数</param>
    private static object CreateInstanceCore(Type type, object[] args) => type.GetConstructor(Reflection.Types.Of(args))?.GetReflector().Invoke(args);

    #endregion
}