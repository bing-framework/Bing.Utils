using System.Collections.Concurrent;

// ReSharper disable once CheckNamespace
namespace Bing.Reflection;

// 类型反射 - 判断 - 异步
public static partial class TypeReflections
{
    /// <summary>
    /// 是否 <see cref="Task{T}"/> 泛型缓存
    /// </summary>
    private static readonly ConcurrentDictionary<TypeInfo, bool> IsTaskOfTCache = new();

    /// <summary>
    /// 是否 <see cref="ValueTask{T}"/> 泛型缓存
    /// </summary>
    private static readonly ConcurrentDictionary<TypeInfo, bool> IsValueTaskOfTCache = new();

    /// <summary>
    /// 无返回结果类型
    /// </summary>
    private static readonly Type VoidTaskResultType = Type.GetType("System.Threading.Tasks.VoidTaskResult", false);

    /// <summary>
    /// 是否 <see cref="Task"/>
    /// </summary>
    /// <param name="typeInfo">类型信息</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static bool IsTask(TypeInfo typeInfo)
    {
        if (typeInfo == null)
            throw new ArgumentNullException(nameof(typeInfo));
        return typeInfo.AsType() == typeof(Task);
    }

    /// <summary>
    /// 是否 <see cref="Task{TResult}"/>
    /// </summary>
    /// <param name="typeInfo">类型信息</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static bool IsTaskWithResult(TypeInfo typeInfo)
    {
        if (typeInfo == null)
            throw new ArgumentNullException(nameof(typeInfo));
        return IsTaskOfTCache.GetOrAdd(typeInfo, info => info.IsGenericType && typeof(Task).GetTypeInfo().IsAssignableFrom(info));
    }

    /// <summary>
    /// 是否 VoidTaskResult
    /// </summary>
    /// <param name="typeInfo">类型信息</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static bool IsTaskWithVoidTaskResult(TypeInfo typeInfo)
    {
        if (typeInfo == null)
            throw new ArgumentNullException(nameof(typeInfo));
        return typeInfo.GenericTypeParameters.Length > 0 && typeInfo.GenericTypeParameters[0] == VoidTaskResultType;
    }

    /// <summary>
    /// 是否 <see cref="ValueTask"/>
    /// </summary>
    /// <param name="typeInfo">类型信息</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static bool IsValueTask(TypeInfo typeInfo)
    {
        if (typeInfo == null)
            throw new ArgumentNullException(nameof(typeInfo));
        return typeInfo.AsType() == typeof(ValueTask);
    }

    /// <summary>
    /// 是否 <see cref="ValueTask{TResult}"/>
    /// </summary>
    /// <param name="typeInfo">类型信息</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static bool IsValueTaskWithResult(TypeInfo typeInfo)
    {
        if (typeInfo == null)
            throw new ArgumentNullException(nameof(typeInfo));
        return IsValueTaskOfTCache.GetOrAdd(typeInfo, info => info.IsGenericType && info.GetGenericTypeDefinition() == typeof(ValueTask<>));
    }
}

/// <summary>
/// 类型反射扩展
/// </summary>
public static partial class TypeReflectionsExtensions
{
    /// <summary>
    /// 是否 <see cref="Task"/>
    /// </summary>
    /// <param name="typeInfo">类型信息</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsTask(this TypeInfo typeInfo) => TypeReflections.IsTask(typeInfo);

    /// <summary>
    /// 是否 <see cref="Task{TResult}"/>
    /// </summary>
    /// <param name="typeInfo">类型信息</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsTaskWithResult(this TypeInfo typeInfo) => TypeReflections.IsTaskWithResult(typeInfo);

    /// <summary>
    /// 是否 VoidTaskResult
    /// </summary>
    /// <param name="typeInfo">类型信息</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsTaskWithVoidTaskResult(this TypeInfo typeInfo) => TypeReflections.IsTaskWithVoidTaskResult(typeInfo);

    /// <summary>
    /// 是否 <see cref="ValueTask"/>
    /// </summary>
    /// <param name="typeInfo">类型信息</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValueTask(this TypeInfo typeInfo) => TypeReflections.IsValueTask(typeInfo);

    /// <summary>
    /// 是否 <see cref="ValueTask{TResult}"/>
    /// </summary>
    /// <param name="typeInfo">类型信息</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValueTaskWithResult(this TypeInfo typeInfo) => TypeReflections.IsValueTaskWithResult(typeInfo);
}