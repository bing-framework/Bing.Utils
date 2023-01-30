namespace Bing.Reflection;

/// <summary>
/// 接口选项
/// </summary>
public enum InterfaceOptions
{
    /// <summary>
    /// 默认
    /// </summary>
    Default = 0,
    /// <summary>
    /// 忽略泛型参数
    /// </summary>
    IgnoreGenericArgs = 1
}

// 类型反射操作 - 接口
public static partial class TypeReflections
{
    #region IsInterfaceDefined

    /// <summary>
    /// 判断给定的接口是否定义
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="interfaceType">接口类型</param>
    /// <param name="options">接口选项</param>
    public static bool IsInterfaceDefined(Type type, Type interfaceType, InterfaceOptions options = InterfaceOptions.Default)
    {
        if (type is null || interfaceType is null)
            return false;
        if (!interfaceType.IsInterface)
            return false;

        var sourceInterfaceTypes = type.GetInterfaces();

        return options switch
        {
            InterfaceOptions.Default => sourceInterfaceTypes.Contains(interfaceType),
            InterfaceOptions.IgnoreGenericArgs => IsInterfaceDefinedImpl(sourceInterfaceTypes, interfaceType),
            _ => sourceInterfaceTypes.Contains(interfaceType)
        };
    }

    /// <summary>
    /// 判断给定的接口是否定义
    /// </summary>
    /// <typeparam name="TInterface">接口类型</typeparam>
    /// <param name="type">类型</param>
    /// <param name="options">接口选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInterfaceDefined<TInterface>(Type type, InterfaceOptions options = InterfaceOptions.Default) => IsInterfaceDefined(type, typeof(TInterface), options);

    /// <summary>
    /// 判断给定的接口是否定义 的实现方式
    /// </summary>
    /// <param name="sourceInterfaceTypes">源接口类型集合</param>
    /// <param name="interfaceType">接口类型</param>
    private static bool IsInterfaceDefinedImpl(IEnumerable<Type> sourceInterfaceTypes, Type interfaceType)
    {
        if (!interfaceType.IsGenericType)
            return sourceInterfaceTypes.Contains(interfaceType);

        var interfaceGenericTypeDefinition = interfaceType.GetGenericTypeDefinition();

        foreach (var sourceInterfaceType in sourceInterfaceTypes)
        {
            if (!sourceInterfaceType.IsGenericType)
                continue;
            if (sourceInterfaceType == interfaceType)
                return true;
            var sourceGenericTypeDefinition = sourceInterfaceType.GetGenericTypeDefinition();
            if (sourceGenericTypeDefinition == interfaceGenericTypeDefinition)
                return true;
        }

        return false;
    }

    #endregion
}