﻿using Bing.Helpers;

namespace Bing.Threading;

/// <summary>
/// 异步帮助类
/// </summary>
public static class AsyncHelper
{
    /// <summary>
    /// 是否异步方法
    /// </summary>
    /// <param name="method">方法信息</param>
    public static bool IsAsync(this MethodInfo method)
    {
        Check.NotNull(method, nameof(method));
        return method.ReturnType.IsTaskOrTaskOfT();
    }

    /// <summary>
    /// 是否异步类型
    /// </summary>
    /// <param name="type">类型</param>
    public static bool IsTaskOrTaskOfT(this Type type) => type == typeof(Task) || type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>);

    /// <summary>
    /// 是否异步泛型类型
    /// </summary>
    /// <param name="type">类型</param>
    public static bool IsTaskOfT(this Type type) => type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>);

    /// <summary>
    /// 解封装（Unwrap）可能包含 Task 的类型，获取实际的结果类型。
    /// </summary>
    /// <param name="type">要解封装的类型。</param>
    /// <returns>解封装后的实际类型。</returns>
    public static Type UnwrapTask(Type type)
    {
        Check.NotNull(type, nameof(type));
        if (type == typeof(Task))
            return typeof(void);
        if (type.IsTaskOfT())
            return type.GenericTypeArguments[0];
        return type;
    }
}
