namespace Bing.Exceptions;

// 尝试 - 批量调用
public static partial class Try
{
    /// <summary>
    /// 调用委托，返回一组 <see cref="TryAction"/> 实例。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="coll"></param>
    /// <param name="invokeAction"></param>
    /// <param name="cause"></param>
    /// <returns></returns>
    public static IEnumerable<TryAction> InvokeRange<T>(IEnumerable<T> coll, Action<T> invokeAction, string cause = null)
    {
        return coll.Select(item => Invoke(invokeAction, item, cause));
    }

    /// <summary>
    /// 调用委托，返回一组 <see cref="TryAction"/> 实例。
    /// </summary>
    /// <typeparam name="T1">输入参数类型</typeparam>
    /// <typeparam name="T2">输入参数类型</typeparam>
    /// <param name="coll">需要遍历的集合</param>
    /// <param name="invokeAction">需要调用的操作</param>
    /// <param name="arg2Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一组 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例的值为操作的哈希码。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    public static IEnumerable<TryAction> InvokeRange<T1, T2>(IEnumerable<T1> coll, Action<T1, T2> invokeAction, Func<int, T2> arg2Func, string cause = null)
    {
        var index = 0;
        foreach (var item in coll)
        {
            yield return Invoke(invokeAction, item, arg2Func.Invoke(index), cause);
            index++;
        }
    }

    /// <summary>
    /// 调用委托，返回一组 <see cref="TryAction"/> 实例。
    /// </summary>
    /// <typeparam name="T1">输入参数类型</typeparam>
    /// <typeparam name="T2">输入参数类型</typeparam>
    /// <typeparam name="T3">输入参数类型</typeparam>
    /// <param name="coll">需要遍历的集合</param>
    /// <param name="invokeAction">需要调用的操作</param>
    /// <param name="arg2Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg3Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一组 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例的值为操作的哈希码。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    public static IEnumerable<TryAction> InvokeRange<T1, T2, T3>(
        IEnumerable<T1> coll,
        Action<T1, T2, T3> invokeAction,
        Func<int, T2> arg2Func, Func<int, T3> arg3Func,
        string cause = null)
    {
        var index = 0;
        foreach (var item in coll)
        {
            yield return Invoke(invokeAction, item,
                arg2Func.Invoke(index), arg3Func.Invoke(index),
                cause);
            index++;
        }
    }

    /// <summary>
    /// 调用委托，返回一组 <see cref="TryAction"/> 实例。
    /// </summary>
    /// <typeparam name="T1">输入参数类型</typeparam>
    /// <typeparam name="T2">输入参数类型</typeparam>
    /// <typeparam name="T3">输入参数类型</typeparam>
    /// <typeparam name="T4">输入参数类型</typeparam>
    /// <param name="coll">需要遍历的集合</param>
    /// <param name="invokeAction">需要调用的操作</param>
    /// <param name="arg2Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg3Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg4Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一组 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例的值为操作的哈希码。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    public static IEnumerable<TryAction> InvokeRange<T1, T2, T3, T4>(
        IEnumerable<T1> coll,
        Action<T1, T2, T3, T4> invokeAction,
        Func<int, T2> arg2Func, Func<int, T3> arg3Func, Func<int, T4> arg4Func,
        string cause = null)
    {
        var index = 0;
        foreach (var item in coll)
        {
            yield return Invoke(invokeAction, item,
                arg2Func.Invoke(index), arg3Func.Invoke(index), arg4Func.Invoke(index),
                cause);
            index++;
        }
    }

    /// <summary>
    /// 调用委托，返回一组 <see cref="TryAction"/> 实例。
    /// </summary>
    /// <typeparam name="T1">输入参数类型</typeparam>
    /// <typeparam name="T2">输入参数类型</typeparam>
    /// <typeparam name="T3">输入参数类型</typeparam>
    /// <typeparam name="T4">输入参数类型</typeparam>
    /// <typeparam name="T5">输入参数类型</typeparam>
    /// <param name="coll">需要遍历的集合</param>
    /// <param name="invokeAction">需要调用的操作</param>
    /// <param name="arg2Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg3Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg4Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg5Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一组 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例的值为操作的哈希码。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    public static IEnumerable<TryAction> InvokeRange<T1, T2, T3, T4, T5>(
        IEnumerable<T1> coll,
        Action<T1, T2, T3, T4, T5> invokeAction,
        Func<int, T2> arg2Func, Func<int, T3> arg3Func, Func<int, T4> arg4Func, Func<int, T5> arg5Func,
        string cause = null)
    {
        var index = 0;
        foreach (var item in coll)
        {
            yield return Invoke(invokeAction, item,
                arg2Func.Invoke(index), arg3Func.Invoke(index), arg4Func.Invoke(index), arg5Func.Invoke(index),
                cause);
            index++;
        }
    }

    /// <summary>
    /// 调用委托，返回一组 <see cref="TryAction"/> 实例。
    /// </summary>
    /// <typeparam name="T1">输入参数类型</typeparam>
    /// <typeparam name="T2">输入参数类型</typeparam>
    /// <typeparam name="T3">输入参数类型</typeparam>
    /// <typeparam name="T4">输入参数类型</typeparam>
    /// <typeparam name="T5">输入参数类型</typeparam>
    /// <typeparam name="T6">输入参数类型</typeparam>
    /// <param name="coll">需要遍历的集合</param>
    /// <param name="invokeAction">需要调用的操作</param>
    /// <param name="arg2Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg3Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg4Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg5Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg6Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一组 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例的值为操作的哈希码。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    public static IEnumerable<TryAction> InvokeRange<T1, T2, T3, T4, T5, T6>(
        IEnumerable<T1> coll,
        Action<T1, T2, T3, T4, T5, T6> invokeAction,
        Func<int, T2> arg2Func, Func<int, T3> arg3Func, Func<int, T4> arg4Func, Func<int, T5> arg5Func, Func<int, T6> arg6Func,
        string cause = null)
    {
        var index = 0;
        foreach (var item in coll)
        {
            yield return Invoke(invokeAction, item,
                arg2Func.Invoke(index), arg3Func.Invoke(index), arg4Func.Invoke(index), arg5Func.Invoke(index),
                arg6Func.Invoke(index),
                cause);
            index++;
        }
    }

    /// <summary>
    /// 调用委托，返回一组 <see cref="TryAction"/> 实例。
    /// </summary>
    /// <typeparam name="T1">输入参数类型</typeparam>
    /// <typeparam name="T2">输入参数类型</typeparam>
    /// <typeparam name="T3">输入参数类型</typeparam>
    /// <typeparam name="T4">输入参数类型</typeparam>
    /// <typeparam name="T5">输入参数类型</typeparam>
    /// <typeparam name="T6">输入参数类型</typeparam>
    /// <typeparam name="T7">输入参数类型</typeparam>
    /// <param name="coll">需要遍历的集合</param>
    /// <param name="invokeAction">需要调用的操作</param>
    /// <param name="arg2Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg3Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg4Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg5Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg6Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg7Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一组 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例的值为操作的哈希码。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    public static IEnumerable<TryAction> InvokeRange<T1, T2, T3, T4, T5, T6, T7>(
        IEnumerable<T1> coll,
        Action<T1, T2, T3, T4, T5, T6, T7> invokeAction,
        Func<int, T2> arg2Func, Func<int, T3> arg3Func, Func<int, T4> arg4Func, Func<int, T5> arg5Func, Func<int, T6> arg6Func, Func<int, T7> arg7Func,
        string cause = null)
    {
        var index = 0;
        foreach (var item in coll)
        {
            yield return Invoke(invokeAction, item,
                arg2Func.Invoke(index), arg3Func.Invoke(index), arg4Func.Invoke(index), arg5Func.Invoke(index),
                arg6Func.Invoke(index), arg7Func.Invoke(index),
                cause);
            index++;
        }
    }

    /// <summary>
    /// 调用委托，返回一组 <see cref="TryAction"/> 实例。
    /// </summary>
    /// <typeparam name="T1">输入参数类型</typeparam>
    /// <typeparam name="T2">输入参数类型</typeparam>
    /// <typeparam name="T3">输入参数类型</typeparam>
    /// <typeparam name="T4">输入参数类型</typeparam>
    /// <typeparam name="T5">输入参数类型</typeparam>
    /// <typeparam name="T6">输入参数类型</typeparam>
    /// <typeparam name="T7">输入参数类型</typeparam>
    /// <typeparam name="T8">输入参数类型</typeparam>
    /// <param name="coll">需要遍历的集合</param>
    /// <param name="invokeAction">需要调用的操作</param>
    /// <param name="arg2Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg3Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg4Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg5Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg6Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg7Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg8Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一组 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例的值为操作的哈希码。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    public static IEnumerable<TryAction> InvokeRange<T1, T2, T3, T4, T5, T6, T7, T8>(
        IEnumerable<T1> coll,
        Action<T1, T2, T3, T4, T5, T6, T7, T8> invokeAction,
        Func<int, T2> arg2Func, Func<int, T3> arg3Func, Func<int, T4> arg4Func, Func<int, T5> arg5Func, Func<int, T6> arg6Func, Func<int, T7> arg7Func, Func<int, T8> arg8Func,
        string cause = null)
    {
        var index = 0;
        foreach (var item in coll)
        {
            yield return Invoke(invokeAction, item,
                arg2Func.Invoke(index), arg3Func.Invoke(index), arg4Func.Invoke(index), arg5Func.Invoke(index),
                arg6Func.Invoke(index), arg7Func.Invoke(index), arg8Func.Invoke(index),
                cause);
            index++;
        }
    }

    /// <summary>
    /// 调用委托，返回一组 <see cref="TryAction"/> 实例。
    /// </summary>
    /// <typeparam name="T1">输入参数类型</typeparam>
    /// <typeparam name="T2">输入参数类型</typeparam>
    /// <typeparam name="T3">输入参数类型</typeparam>
    /// <typeparam name="T4">输入参数类型</typeparam>
    /// <typeparam name="T5">输入参数类型</typeparam>
    /// <typeparam name="T6">输入参数类型</typeparam>
    /// <typeparam name="T7">输入参数类型</typeparam>
    /// <typeparam name="T8">输入参数类型</typeparam>
    /// <typeparam name="T9">输入参数类型</typeparam>
    /// <param name="coll">需要遍历的集合</param>
    /// <param name="invokeAction">需要调用的操作</param>
    /// <param name="arg2Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg3Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg4Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg5Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg6Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg7Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg8Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg9Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一组 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例的值为操作的哈希码。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    public static IEnumerable<TryAction> InvokeRange<T1, T2, T3, T4, T5, T6, T7, T8, T9>(
        IEnumerable<T1> coll,
        Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> invokeAction,
        Func<int, T2> arg2Func, Func<int, T3> arg3Func, Func<int, T4> arg4Func, Func<int, T5> arg5Func, Func<int, T6> arg6Func, Func<int, T7> arg7Func, Func<int, T8> arg8Func, Func<int, T9> arg9Func,
        string cause = null)
    {
        var index = 0;
        foreach (var item in coll)
        {
            yield return Invoke(invokeAction, item,
                arg2Func.Invoke(index), arg3Func.Invoke(index), arg4Func.Invoke(index), arg5Func.Invoke(index),
                arg6Func.Invoke(index), arg7Func.Invoke(index), arg8Func.Invoke(index), arg9Func.Invoke(index),
                cause);
            index++;
        }
    }

    /// <summary>
    /// 调用委托，返回一组 <see cref="TryAction"/> 实例。
    /// </summary>
    /// <typeparam name="T1">输入参数类型</typeparam>
    /// <typeparam name="T2">输入参数类型</typeparam>
    /// <typeparam name="T3">输入参数类型</typeparam>
    /// <typeparam name="T4">输入参数类型</typeparam>
    /// <typeparam name="T5">输入参数类型</typeparam>
    /// <typeparam name="T6">输入参数类型</typeparam>
    /// <typeparam name="T7">输入参数类型</typeparam>
    /// <typeparam name="T8">输入参数类型</typeparam>
    /// <typeparam name="T9">输入参数类型</typeparam>
    /// <typeparam name="T10">输入参数类型</typeparam>
    /// <param name="coll">需要遍历的集合</param>
    /// <param name="invokeAction">需要调用的操作</param>
    /// <param name="arg2Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg3Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg4Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg5Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg6Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg7Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg8Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg9Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg10Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一组 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例的值为操作的哈希码。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    public static IEnumerable<TryAction> InvokeRange<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
        IEnumerable<T1> coll,
        Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> invokeAction,
        Func<int, T2> arg2Func, Func<int, T3> arg3Func, Func<int, T4> arg4Func, Func<int, T5> arg5Func, Func<int, T6> arg6Func, Func<int, T7> arg7Func, Func<int, T8> arg8Func, Func<int, T9> arg9Func, Func<int, T10> arg10Func,
        string cause = null)
    {
        var index = 0;
        foreach (var item in coll)
        {
            yield return Invoke(invokeAction, item,
                arg2Func.Invoke(index), arg3Func.Invoke(index), arg4Func.Invoke(index), arg5Func.Invoke(index),
                arg6Func.Invoke(index), arg7Func.Invoke(index), arg8Func.Invoke(index), arg9Func.Invoke(index),
                arg10Func.Invoke(index),
                cause);
            index++;
        }
    }

    /// <summary>
    /// 调用委托，返回一组 <see cref="TryAction"/> 实例。
    /// </summary>
    /// <typeparam name="T1">输入参数类型</typeparam>
    /// <typeparam name="T2">输入参数类型</typeparam>
    /// <typeparam name="T3">输入参数类型</typeparam>
    /// <typeparam name="T4">输入参数类型</typeparam>
    /// <typeparam name="T5">输入参数类型</typeparam>
    /// <typeparam name="T6">输入参数类型</typeparam>
    /// <typeparam name="T7">输入参数类型</typeparam>
    /// <typeparam name="T8">输入参数类型</typeparam>
    /// <typeparam name="T9">输入参数类型</typeparam>
    /// <typeparam name="T10">输入参数类型</typeparam>
    /// <typeparam name="T11">输入参数类型</typeparam>
    /// <param name="coll">需要遍历的集合</param>
    /// <param name="invokeAction">需要调用的操作</param>
    /// <param name="arg2Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg3Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg4Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg5Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg6Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg7Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg8Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg9Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg10Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg11Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一组 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例的值为操作的哈希码。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    public static IEnumerable<TryAction> InvokeRange<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
        IEnumerable<T1> coll,
        Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> invokeAction,
        Func<int, T2> arg2Func, Func<int, T3> arg3Func, Func<int, T4> arg4Func, Func<int, T5> arg5Func, Func<int, T6> arg6Func, Func<int, T7> arg7Func, Func<int, T8> arg8Func, Func<int, T9> arg9Func, Func<int, T10> arg10Func, Func<int, T11> arg11Func,
        string cause = null)
    {
        var index = 0;
        foreach (var item in coll)
        {
            yield return Invoke(invokeAction, item,
                arg2Func.Invoke(index), arg3Func.Invoke(index), arg4Func.Invoke(index), arg5Func.Invoke(index),
                arg6Func.Invoke(index), arg7Func.Invoke(index), arg8Func.Invoke(index), arg9Func.Invoke(index),
                arg10Func.Invoke(index), arg11Func.Invoke(index),
                cause);
            index++;
        }
    }

    /// <summary>
    /// 调用委托，返回一组 <see cref="TryAction"/> 实例。
    /// </summary>
    /// <typeparam name="T1">输入参数类型</typeparam>
    /// <typeparam name="T2">输入参数类型</typeparam>
    /// <typeparam name="T3">输入参数类型</typeparam>
    /// <typeparam name="T4">输入参数类型</typeparam>
    /// <typeparam name="T5">输入参数类型</typeparam>
    /// <typeparam name="T6">输入参数类型</typeparam>
    /// <typeparam name="T7">输入参数类型</typeparam>
    /// <typeparam name="T8">输入参数类型</typeparam>
    /// <typeparam name="T9">输入参数类型</typeparam>
    /// <typeparam name="T10">输入参数类型</typeparam>
    /// <typeparam name="T11">输入参数类型</typeparam>
    /// <typeparam name="T12">输入参数类型</typeparam>
    /// <param name="coll">需要遍历的集合</param>
    /// <param name="invokeAction">需要调用的操作</param>
    /// <param name="arg2Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg3Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg4Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg5Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg6Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg7Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg8Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg9Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg10Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg11Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg12Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一组 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例的值为操作的哈希码。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    public static IEnumerable<TryAction> InvokeRange<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(
        IEnumerable<T1> coll,
        Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> invokeAction,
        Func<int, T2> arg2Func, Func<int, T3> arg3Func, Func<int, T4> arg4Func, Func<int, T5> arg5Func, Func<int, T6> arg6Func, Func<int, T7> arg7Func, Func<int, T8> arg8Func, Func<int, T9> arg9Func, Func<int, T10> arg10Func, Func<int, T11> arg11Func, Func<int, T12> arg12Func,
        string cause = null)
    {
        var index = 0;
        foreach (var item in coll)
        {
            yield return Invoke(invokeAction, item,
                arg2Func.Invoke(index), arg3Func.Invoke(index), arg4Func.Invoke(index), arg5Func.Invoke(index),
                arg6Func.Invoke(index), arg7Func.Invoke(index), arg8Func.Invoke(index), arg9Func.Invoke(index),
                arg10Func.Invoke(index), arg11Func.Invoke(index), arg12Func.Invoke(index),
                 cause);
            index++;
        }
    }

    /// <summary>
    /// 调用委托，返回一组 <see cref="TryAction"/> 实例。
    /// </summary>
    /// <typeparam name="T1">输入参数类型</typeparam>
    /// <typeparam name="T2">输入参数类型</typeparam>
    /// <typeparam name="T3">输入参数类型</typeparam>
    /// <typeparam name="T4">输入参数类型</typeparam>
    /// <typeparam name="T5">输入参数类型</typeparam>
    /// <typeparam name="T6">输入参数类型</typeparam>
    /// <typeparam name="T7">输入参数类型</typeparam>
    /// <typeparam name="T8">输入参数类型</typeparam>
    /// <typeparam name="T9">输入参数类型</typeparam>
    /// <typeparam name="T10">输入参数类型</typeparam>
    /// <typeparam name="T11">输入参数类型</typeparam>
    /// <typeparam name="T12">输入参数类型</typeparam>
    /// <typeparam name="T13">输入参数类型</typeparam>
    /// <param name="coll">需要遍历的集合</param>
    /// <param name="invokeAction">需要调用的操作</param>
    /// <param name="arg2Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg3Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg4Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg5Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg6Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg7Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg8Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg9Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg10Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg11Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg12Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg13Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一组 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例的值为操作的哈希码。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    public static IEnumerable<TryAction> InvokeRange<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(
        IEnumerable<T1> coll,
        Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> invokeAction,
        Func<int, T2> arg2Func, Func<int, T3> arg3Func, Func<int, T4> arg4Func, Func<int, T5> arg5Func, Func<int, T6> arg6Func, Func<int, T7> arg7Func, Func<int, T8> arg8Func, Func<int, T9> arg9Func, Func<int, T10> arg10Func, Func<int, T11> arg11Func, Func<int, T12> arg12Func, Func<int, T13> arg13Func,
        string cause = null)
    {
        var index = 0;
        foreach (var item in coll)
        {
            yield return Invoke(invokeAction, item,
                arg2Func.Invoke(index), arg3Func.Invoke(index), arg4Func.Invoke(index), arg5Func.Invoke(index),
                arg6Func.Invoke(index), arg7Func.Invoke(index), arg8Func.Invoke(index), arg9Func.Invoke(index),
                arg10Func.Invoke(index), arg11Func.Invoke(index), arg12Func.Invoke(index), arg13Func.Invoke(index),
                 cause);
            index++;
        }
    }

    /// <summary>
    /// 调用委托，返回一组 <see cref="TryAction"/> 实例。
    /// </summary>
    /// <typeparam name="T1">输入参数类型</typeparam>
    /// <typeparam name="T2">输入参数类型</typeparam>
    /// <typeparam name="T3">输入参数类型</typeparam>
    /// <typeparam name="T4">输入参数类型</typeparam>
    /// <typeparam name="T5">输入参数类型</typeparam>
    /// <typeparam name="T6">输入参数类型</typeparam>
    /// <typeparam name="T7">输入参数类型</typeparam>
    /// <typeparam name="T8">输入参数类型</typeparam>
    /// <typeparam name="T9">输入参数类型</typeparam>
    /// <typeparam name="T10">输入参数类型</typeparam>
    /// <typeparam name="T11">输入参数类型</typeparam>
    /// <typeparam name="T12">输入参数类型</typeparam>
    /// <typeparam name="T13">输入参数类型</typeparam>
    /// <typeparam name="T14">输入参数类型</typeparam>
    /// <param name="coll">需要遍历的集合</param>
    /// <param name="invokeAction">需要调用的操作</param>
    /// <param name="arg2Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg3Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg4Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg5Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg6Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg7Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg8Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg9Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg10Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg11Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg12Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg13Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg14Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一组 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例的值为操作的哈希码。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    public static IEnumerable<TryAction> InvokeRange<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(
        IEnumerable<T1> coll,
        Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> invokeAction,
        Func<int, T2> arg2Func, Func<int, T3> arg3Func, Func<int, T4> arg4Func, Func<int, T5> arg5Func, Func<int, T6> arg6Func, Func<int, T7> arg7Func, Func<int, T8> arg8Func, Func<int, T9> arg9Func, Func<int, T10> arg10Func, Func<int, T11> arg11Func, Func<int, T12> arg12Func, Func<int, T13> arg13Func, Func<int, T14> arg14Func,
        string cause = null)
    {
        var index = 0;
        foreach (var item in coll)
        {
            yield return Invoke(invokeAction, item,
                arg2Func.Invoke(index), arg3Func.Invoke(index), arg4Func.Invoke(index), arg5Func.Invoke(index),
                arg6Func.Invoke(index), arg7Func.Invoke(index), arg8Func.Invoke(index), arg9Func.Invoke(index),
                arg10Func.Invoke(index), arg11Func.Invoke(index), arg12Func.Invoke(index), arg13Func.Invoke(index),
                arg14Func.Invoke(index), cause);
            index++;
        }
    }

    /// <summary>
    /// 调用委托，返回一组 <see cref="TryAction"/> 实例。
    /// </summary>
    /// <typeparam name="T1">输入参数类型</typeparam>
    /// <typeparam name="T2">输入参数类型</typeparam>
    /// <typeparam name="T3">输入参数类型</typeparam>
    /// <typeparam name="T4">输入参数类型</typeparam>
    /// <typeparam name="T5">输入参数类型</typeparam>
    /// <typeparam name="T6">输入参数类型</typeparam>
    /// <typeparam name="T7">输入参数类型</typeparam>
    /// <typeparam name="T8">输入参数类型</typeparam>
    /// <typeparam name="T9">输入参数类型</typeparam>
    /// <typeparam name="T10">输入参数类型</typeparam>
    /// <typeparam name="T11">输入参数类型</typeparam>
    /// <typeparam name="T12">输入参数类型</typeparam>
    /// <typeparam name="T13">输入参数类型</typeparam>
    /// <typeparam name="T14">输入参数类型</typeparam>
    /// <typeparam name="T15">输入参数类型</typeparam>
    /// <param name="coll">需要遍历的集合</param>
    /// <param name="invokeAction">需要调用的操作</param>
    /// <param name="arg2Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg3Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg4Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg5Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg6Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg7Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg8Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg9Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg10Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg11Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg12Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg13Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg14Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg15Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一组 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例的值为操作的哈希码。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    public static IEnumerable<TryAction> InvokeRange<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(
        IEnumerable<T1> coll,
        Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> invokeAction,
        Func<int, T2> arg2Func, Func<int, T3> arg3Func, Func<int, T4> arg4Func, Func<int, T5> arg5Func, Func<int, T6> arg6Func, Func<int, T7> arg7Func, Func<int, T8> arg8Func, Func<int, T9> arg9Func, Func<int, T10> arg10Func, Func<int, T11> arg11Func, Func<int, T12> arg12Func, Func<int, T13> arg13Func, Func<int, T14> arg14Func, Func<int, T15> arg15Func,
        string cause = null)
    {
        var index = 0;
        foreach (var item in coll)
        {
            yield return Invoke(invokeAction, item,
                arg2Func.Invoke(index), arg3Func.Invoke(index), arg4Func.Invoke(index), arg5Func.Invoke(index),
                arg6Func.Invoke(index), arg7Func.Invoke(index), arg8Func.Invoke(index), arg9Func.Invoke(index),
                arg10Func.Invoke(index), arg11Func.Invoke(index), arg12Func.Invoke(index), arg13Func.Invoke(index),
                arg14Func.Invoke(index), arg15Func.Invoke(index), cause);
            index++;
        }
    }

    /// <summary>
    /// 调用委托，返回一组 <see cref="TryAction"/> 实例。
    /// </summary>
    /// <typeparam name="T1">输入参数类型</typeparam>
    /// <typeparam name="T2">输入参数类型</typeparam>
    /// <typeparam name="T3">输入参数类型</typeparam>
    /// <typeparam name="T4">输入参数类型</typeparam>
    /// <typeparam name="T5">输入参数类型</typeparam>
    /// <typeparam name="T6">输入参数类型</typeparam>
    /// <typeparam name="T7">输入参数类型</typeparam>
    /// <typeparam name="T8">输入参数类型</typeparam>
    /// <typeparam name="T9">输入参数类型</typeparam>
    /// <typeparam name="T10">输入参数类型</typeparam>
    /// <typeparam name="T11">输入参数类型</typeparam>
    /// <typeparam name="T12">输入参数类型</typeparam>
    /// <typeparam name="T13">输入参数类型</typeparam>
    /// <typeparam name="T14">输入参数类型</typeparam>
    /// <typeparam name="T15">输入参数类型</typeparam>
    /// <typeparam name="T16">输入参数类型</typeparam>
    /// <param name="coll">需要遍历的集合</param>
    /// <param name="invokeAction">需要调用的操作</param>
    /// <param name="arg2Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg3Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg4Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg5Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg6Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg7Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg8Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg9Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg10Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg11Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg12Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg13Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg14Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg15Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="arg16Func">传递给 <paramref name="invokeAction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一组 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例的值为操作的哈希码。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    public static IEnumerable<TryAction> InvokeRange<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(
        IEnumerable<T1> coll,
        Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> invokeAction,
        Func<int, T2> arg2Func, Func<int, T3> arg3Func, Func<int, T4> arg4Func, Func<int, T5> arg5Func, Func<int, T6> arg6Func, Func<int, T7> arg7Func, Func<int, T8> arg8Func, Func<int, T9> arg9Func, Func<int, T10> arg10Func, Func<int, T11> arg11Func, Func<int, T12> arg12Func, Func<int, T13> arg13Func, Func<int, T14> arg14Func, Func<int, T15> arg15Func, Func<int, T16> arg16Func,
        string cause = null)
    {
        var index = 0;
        foreach (var item in coll)
        {
            yield return Invoke(invokeAction, item,
                arg2Func.Invoke(index), arg3Func.Invoke(index), arg4Func.Invoke(index), arg5Func.Invoke(index),
                arg6Func.Invoke(index), arg7Func.Invoke(index), arg8Func.Invoke(index), arg9Func.Invoke(index),
                arg10Func.Invoke(index), arg11Func.Invoke(index), arg12Func.Invoke(index), arg13Func.Invoke(index),
                arg14Func.Invoke(index), arg15Func.Invoke(index), arg16Func.Invoke(index), cause);
            index++;
        }
    }
}