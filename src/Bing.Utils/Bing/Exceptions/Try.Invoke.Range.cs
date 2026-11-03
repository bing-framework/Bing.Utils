namespace Bing.Exceptions;

// 尝试 - 批量调用
public static partial class Try
{
    /// <summary>
    /// 调用委托，返回一组 <see cref="TryAction"/> 实例。
    /// </summary>
    /// <typeparam name="T">集合元素类型。</typeparam>
    /// <param name="coll">需要延迟枚举的非空集合。</param>
    /// <param name="invokeAction">逐项执行的非空操作。</param>
    /// <param name="cause">可选的单项业务失败原因。</param>
    /// <returns>按输入顺序产生的尝试结果；每次枚举重新执行。</returns>
    /// <remarks>输入配置立即校验；枚举时逐项捕获参数工厂与操作异常，源枚举异常直接传播。重复枚举会重复执行。</remarks>
    public static IEnumerable<TryAction> InvokeRange<T>(IEnumerable<T> coll, Action<T> invokeAction, string cause = null)
    {
        if (coll is null)
            throw new ArgumentNullException(nameof(coll));
        if (invokeAction is null)
            throw new ArgumentNullException(nameof(invokeAction));
        return Enumerate();

        IEnumerable<TryAction> Enumerate()
        {
            var index = 0;
            foreach (var item in coll)
            {
                TryAction result;
                try
                {
                    result = Invoke(invokeAction, item, cause);
                }
                catch (Exception exception)
                {
                    result = new FailureAction(exception, 0, cause);
                }
                yield return result;
                index++;
            }
        }
    }

    /// <summary>
    /// 调用委托，返回一组 <see cref="TryAction"/> 实例。
    /// </summary>
    /// <typeparam name="T1">输入参数类型</typeparam>
    /// <typeparam name="T2">输入参数类型</typeparam>
    /// <param name="coll">需要遍历的集合</param>
    /// <param name="invokeAction">需要调用的操作</param>
    /// <param name="arg2Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一组 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例表示本次执行成功。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    /// <remarks>输入配置立即校验；枚举时逐项捕获参数工厂与操作异常，源枚举异常直接传播。重复枚举会重复执行。</remarks>
    public static IEnumerable<TryAction> InvokeRange<T1, T2>(IEnumerable<T1> coll, Action<T1, T2> invokeAction, Func<int, T2> arg2Func, string cause = null)
    {
        if (coll is null)
            throw new ArgumentNullException(nameof(coll));
        if (invokeAction is null)
            throw new ArgumentNullException(nameof(invokeAction));
        if (arg2Func is null)
            throw new ArgumentNullException(nameof(arg2Func));
        return Enumerate();

        IEnumerable<TryAction> Enumerate()
        {
            var index = 0;
            foreach (var item in coll)
            {
                TryAction result;
                try
                {
                    result = Invoke(invokeAction, item, arg2Func(index), cause);
                }
                catch (Exception exception)
                {
                    result = new FailureAction(exception, 0, cause);
                }
                yield return result;
                index++;
            }
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
    /// <param name="arg2Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg3Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一组 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例表示本次执行成功。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    /// <remarks>输入配置立即校验；枚举时逐项捕获参数工厂与操作异常，源枚举异常直接传播。重复枚举会重复执行。</remarks>
    public static IEnumerable<TryAction> InvokeRange<T1, T2, T3>(
        IEnumerable<T1> coll,
        Action<T1, T2, T3> invokeAction,
        Func<int, T2> arg2Func, Func<int, T3> arg3Func,
        string cause = null)
    {
        if (coll is null)
            throw new ArgumentNullException(nameof(coll));
        if (invokeAction is null)
            throw new ArgumentNullException(nameof(invokeAction));
        if (arg2Func is null)
            throw new ArgumentNullException(nameof(arg2Func));
        if (arg3Func is null)
            throw new ArgumentNullException(nameof(arg3Func));
        return Enumerate();

        IEnumerable<TryAction> Enumerate()
        {
            var index = 0;
            foreach (var item in coll)
            {
                TryAction result;
                try
                {
                    result = Invoke(invokeAction, item, arg2Func(index), arg3Func(index), cause);
                }
                catch (Exception exception)
                {
                    result = new FailureAction(exception, 0, cause);
                }
                yield return result;
                index++;
            }
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
    /// <param name="arg2Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg3Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg4Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一组 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例表示本次执行成功。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    /// <remarks>输入配置立即校验；枚举时逐项捕获参数工厂与操作异常，源枚举异常直接传播。重复枚举会重复执行。</remarks>
    public static IEnumerable<TryAction> InvokeRange<T1, T2, T3, T4>(
        IEnumerable<T1> coll,
        Action<T1, T2, T3, T4> invokeAction,
        Func<int, T2> arg2Func, Func<int, T3> arg3Func, Func<int, T4> arg4Func,
        string cause = null)
    {
        if (coll is null)
            throw new ArgumentNullException(nameof(coll));
        if (invokeAction is null)
            throw new ArgumentNullException(nameof(invokeAction));
        if (arg2Func is null)
            throw new ArgumentNullException(nameof(arg2Func));
        if (arg3Func is null)
            throw new ArgumentNullException(nameof(arg3Func));
        if (arg4Func is null)
            throw new ArgumentNullException(nameof(arg4Func));
        return Enumerate();

        IEnumerable<TryAction> Enumerate()
        {
            var index = 0;
            foreach (var item in coll)
            {
                TryAction result;
                try
                {
                    result = Invoke(invokeAction, item, arg2Func(index), arg3Func(index), arg4Func(index), cause);
                }
                catch (Exception exception)
                {
                    result = new FailureAction(exception, 0, cause);
                }
                yield return result;
                index++;
            }
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
    /// <param name="arg2Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg3Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg4Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg5Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一组 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例表示本次执行成功。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    /// <remarks>输入配置立即校验；枚举时逐项捕获参数工厂与操作异常，源枚举异常直接传播。重复枚举会重复执行。</remarks>
    public static IEnumerable<TryAction> InvokeRange<T1, T2, T3, T4, T5>(
        IEnumerable<T1> coll,
        Action<T1, T2, T3, T4, T5> invokeAction,
        Func<int, T2> arg2Func, Func<int, T3> arg3Func, Func<int, T4> arg4Func, Func<int, T5> arg5Func,
        string cause = null)
    {
        if (coll is null)
            throw new ArgumentNullException(nameof(coll));
        if (invokeAction is null)
            throw new ArgumentNullException(nameof(invokeAction));
        if (arg2Func is null)
            throw new ArgumentNullException(nameof(arg2Func));
        if (arg3Func is null)
            throw new ArgumentNullException(nameof(arg3Func));
        if (arg4Func is null)
            throw new ArgumentNullException(nameof(arg4Func));
        if (arg5Func is null)
            throw new ArgumentNullException(nameof(arg5Func));
        return Enumerate();

        IEnumerable<TryAction> Enumerate()
        {
            var index = 0;
            foreach (var item in coll)
            {
                TryAction result;
                try
                {
                    result = Invoke(invokeAction, item, arg2Func(index), arg3Func(index), arg4Func(index), arg5Func(index), cause);
                }
                catch (Exception exception)
                {
                    result = new FailureAction(exception, 0, cause);
                }
                yield return result;
                index++;
            }
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
    /// <param name="arg2Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg3Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg4Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg5Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg6Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一组 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例表示本次执行成功。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    /// <remarks>输入配置立即校验；枚举时逐项捕获参数工厂与操作异常，源枚举异常直接传播。重复枚举会重复执行。</remarks>
    public static IEnumerable<TryAction> InvokeRange<T1, T2, T3, T4, T5, T6>(
        IEnumerable<T1> coll,
        Action<T1, T2, T3, T4, T5, T6> invokeAction,
        Func<int, T2> arg2Func, Func<int, T3> arg3Func, Func<int, T4> arg4Func, Func<int, T5> arg5Func, Func<int, T6> arg6Func,
        string cause = null)
    {
        if (coll is null)
            throw new ArgumentNullException(nameof(coll));
        if (invokeAction is null)
            throw new ArgumentNullException(nameof(invokeAction));
        if (arg2Func is null)
            throw new ArgumentNullException(nameof(arg2Func));
        if (arg3Func is null)
            throw new ArgumentNullException(nameof(arg3Func));
        if (arg4Func is null)
            throw new ArgumentNullException(nameof(arg4Func));
        if (arg5Func is null)
            throw new ArgumentNullException(nameof(arg5Func));
        if (arg6Func is null)
            throw new ArgumentNullException(nameof(arg6Func));
        return Enumerate();

        IEnumerable<TryAction> Enumerate()
        {
            var index = 0;
            foreach (var item in coll)
            {
                TryAction result;
                try
                {
                    result = Invoke(invokeAction, item, arg2Func(index), arg3Func(index), arg4Func(index), arg5Func(index), arg6Func(index), cause);
                }
                catch (Exception exception)
                {
                    result = new FailureAction(exception, 0, cause);
                }
                yield return result;
                index++;
            }
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
    /// <param name="arg2Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg3Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg4Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg5Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg6Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg7Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一组 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例表示本次执行成功。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    /// <remarks>输入配置立即校验；枚举时逐项捕获参数工厂与操作异常，源枚举异常直接传播。重复枚举会重复执行。</remarks>
    public static IEnumerable<TryAction> InvokeRange<T1, T2, T3, T4, T5, T6, T7>(
        IEnumerable<T1> coll,
        Action<T1, T2, T3, T4, T5, T6, T7> invokeAction,
        Func<int, T2> arg2Func, Func<int, T3> arg3Func, Func<int, T4> arg4Func, Func<int, T5> arg5Func, Func<int, T6> arg6Func, Func<int, T7> arg7Func,
        string cause = null)
    {
        if (coll is null)
            throw new ArgumentNullException(nameof(coll));
        if (invokeAction is null)
            throw new ArgumentNullException(nameof(invokeAction));
        if (arg2Func is null)
            throw new ArgumentNullException(nameof(arg2Func));
        if (arg3Func is null)
            throw new ArgumentNullException(nameof(arg3Func));
        if (arg4Func is null)
            throw new ArgumentNullException(nameof(arg4Func));
        if (arg5Func is null)
            throw new ArgumentNullException(nameof(arg5Func));
        if (arg6Func is null)
            throw new ArgumentNullException(nameof(arg6Func));
        if (arg7Func is null)
            throw new ArgumentNullException(nameof(arg7Func));
        return Enumerate();

        IEnumerable<TryAction> Enumerate()
        {
            var index = 0;
            foreach (var item in coll)
            {
                TryAction result;
                try
                {
                    result = Invoke(invokeAction, item, arg2Func(index), arg3Func(index), arg4Func(index), arg5Func(index), arg6Func(index), arg7Func(index), cause);
                }
                catch (Exception exception)
                {
                    result = new FailureAction(exception, 0, cause);
                }
                yield return result;
                index++;
            }
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
    /// <param name="arg2Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg3Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg4Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg5Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg6Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg7Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg8Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一组 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例表示本次执行成功。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    /// <remarks>输入配置立即校验；枚举时逐项捕获参数工厂与操作异常，源枚举异常直接传播。重复枚举会重复执行。</remarks>
    public static IEnumerable<TryAction> InvokeRange<T1, T2, T3, T4, T5, T6, T7, T8>(
        IEnumerable<T1> coll,
        Action<T1, T2, T3, T4, T5, T6, T7, T8> invokeAction,
        Func<int, T2> arg2Func, Func<int, T3> arg3Func, Func<int, T4> arg4Func, Func<int, T5> arg5Func, Func<int, T6> arg6Func, Func<int, T7> arg7Func, Func<int, T8> arg8Func,
        string cause = null)
    {
        if (coll is null)
            throw new ArgumentNullException(nameof(coll));
        if (invokeAction is null)
            throw new ArgumentNullException(nameof(invokeAction));
        if (arg2Func is null)
            throw new ArgumentNullException(nameof(arg2Func));
        if (arg3Func is null)
            throw new ArgumentNullException(nameof(arg3Func));
        if (arg4Func is null)
            throw new ArgumentNullException(nameof(arg4Func));
        if (arg5Func is null)
            throw new ArgumentNullException(nameof(arg5Func));
        if (arg6Func is null)
            throw new ArgumentNullException(nameof(arg6Func));
        if (arg7Func is null)
            throw new ArgumentNullException(nameof(arg7Func));
        if (arg8Func is null)
            throw new ArgumentNullException(nameof(arg8Func));
        return Enumerate();

        IEnumerable<TryAction> Enumerate()
        {
            var index = 0;
            foreach (var item in coll)
            {
                TryAction result;
                try
                {
                    result = Invoke(invokeAction, item, arg2Func(index), arg3Func(index), arg4Func(index), arg5Func(index), arg6Func(index), arg7Func(index), arg8Func(index), cause);
                }
                catch (Exception exception)
                {
                    result = new FailureAction(exception, 0, cause);
                }
                yield return result;
                index++;
            }
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
    /// <param name="arg2Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg3Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg4Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg5Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg6Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg7Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg8Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg9Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一组 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例表示本次执行成功。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    /// <remarks>输入配置立即校验；枚举时逐项捕获参数工厂与操作异常，源枚举异常直接传播。重复枚举会重复执行。</remarks>
    public static IEnumerable<TryAction> InvokeRange<T1, T2, T3, T4, T5, T6, T7, T8, T9>(
        IEnumerable<T1> coll,
        Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> invokeAction,
        Func<int, T2> arg2Func, Func<int, T3> arg3Func, Func<int, T4> arg4Func, Func<int, T5> arg5Func, Func<int, T6> arg6Func, Func<int, T7> arg7Func, Func<int, T8> arg8Func, Func<int, T9> arg9Func,
        string cause = null)
    {
        if (coll is null)
            throw new ArgumentNullException(nameof(coll));
        if (invokeAction is null)
            throw new ArgumentNullException(nameof(invokeAction));
        if (arg2Func is null)
            throw new ArgumentNullException(nameof(arg2Func));
        if (arg3Func is null)
            throw new ArgumentNullException(nameof(arg3Func));
        if (arg4Func is null)
            throw new ArgumentNullException(nameof(arg4Func));
        if (arg5Func is null)
            throw new ArgumentNullException(nameof(arg5Func));
        if (arg6Func is null)
            throw new ArgumentNullException(nameof(arg6Func));
        if (arg7Func is null)
            throw new ArgumentNullException(nameof(arg7Func));
        if (arg8Func is null)
            throw new ArgumentNullException(nameof(arg8Func));
        if (arg9Func is null)
            throw new ArgumentNullException(nameof(arg9Func));
        return Enumerate();

        IEnumerable<TryAction> Enumerate()
        {
            var index = 0;
            foreach (var item in coll)
            {
                TryAction result;
                try
                {
                    result = Invoke(invokeAction, item, arg2Func(index), arg3Func(index), arg4Func(index), arg5Func(index), arg6Func(index), arg7Func(index), arg8Func(index), arg9Func(index), cause);
                }
                catch (Exception exception)
                {
                    result = new FailureAction(exception, 0, cause);
                }
                yield return result;
                index++;
            }
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
    /// <param name="arg2Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg3Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg4Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg5Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg6Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg7Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg8Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg9Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg10Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一组 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例表示本次执行成功。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    /// <remarks>输入配置立即校验；枚举时逐项捕获参数工厂与操作异常，源枚举异常直接传播。重复枚举会重复执行。</remarks>
    public static IEnumerable<TryAction> InvokeRange<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
        IEnumerable<T1> coll,
        Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> invokeAction,
        Func<int, T2> arg2Func, Func<int, T3> arg3Func, Func<int, T4> arg4Func, Func<int, T5> arg5Func, Func<int, T6> arg6Func, Func<int, T7> arg7Func, Func<int, T8> arg8Func, Func<int, T9> arg9Func, Func<int, T10> arg10Func,
        string cause = null)
    {
        if (coll is null)
            throw new ArgumentNullException(nameof(coll));
        if (invokeAction is null)
            throw new ArgumentNullException(nameof(invokeAction));
        if (arg2Func is null)
            throw new ArgumentNullException(nameof(arg2Func));
        if (arg3Func is null)
            throw new ArgumentNullException(nameof(arg3Func));
        if (arg4Func is null)
            throw new ArgumentNullException(nameof(arg4Func));
        if (arg5Func is null)
            throw new ArgumentNullException(nameof(arg5Func));
        if (arg6Func is null)
            throw new ArgumentNullException(nameof(arg6Func));
        if (arg7Func is null)
            throw new ArgumentNullException(nameof(arg7Func));
        if (arg8Func is null)
            throw new ArgumentNullException(nameof(arg8Func));
        if (arg9Func is null)
            throw new ArgumentNullException(nameof(arg9Func));
        if (arg10Func is null)
            throw new ArgumentNullException(nameof(arg10Func));
        return Enumerate();

        IEnumerable<TryAction> Enumerate()
        {
            var index = 0;
            foreach (var item in coll)
            {
                TryAction result;
                try
                {
                    result = Invoke(invokeAction, item, arg2Func(index), arg3Func(index), arg4Func(index), arg5Func(index), arg6Func(index), arg7Func(index), arg8Func(index), arg9Func(index), arg10Func(index), cause);
                }
                catch (Exception exception)
                {
                    result = new FailureAction(exception, 0, cause);
                }
                yield return result;
                index++;
            }
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
    /// <param name="arg2Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg3Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg4Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg5Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg6Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg7Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg8Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg9Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg10Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg11Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一组 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例表示本次执行成功。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    /// <remarks>输入配置立即校验；枚举时逐项捕获参数工厂与操作异常，源枚举异常直接传播。重复枚举会重复执行。</remarks>
    public static IEnumerable<TryAction> InvokeRange<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
        IEnumerable<T1> coll,
        Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> invokeAction,
        Func<int, T2> arg2Func, Func<int, T3> arg3Func, Func<int, T4> arg4Func, Func<int, T5> arg5Func, Func<int, T6> arg6Func, Func<int, T7> arg7Func, Func<int, T8> arg8Func, Func<int, T9> arg9Func, Func<int, T10> arg10Func, Func<int, T11> arg11Func,
        string cause = null)
    {
        if (coll is null)
            throw new ArgumentNullException(nameof(coll));
        if (invokeAction is null)
            throw new ArgumentNullException(nameof(invokeAction));
        if (arg2Func is null)
            throw new ArgumentNullException(nameof(arg2Func));
        if (arg3Func is null)
            throw new ArgumentNullException(nameof(arg3Func));
        if (arg4Func is null)
            throw new ArgumentNullException(nameof(arg4Func));
        if (arg5Func is null)
            throw new ArgumentNullException(nameof(arg5Func));
        if (arg6Func is null)
            throw new ArgumentNullException(nameof(arg6Func));
        if (arg7Func is null)
            throw new ArgumentNullException(nameof(arg7Func));
        if (arg8Func is null)
            throw new ArgumentNullException(nameof(arg8Func));
        if (arg9Func is null)
            throw new ArgumentNullException(nameof(arg9Func));
        if (arg10Func is null)
            throw new ArgumentNullException(nameof(arg10Func));
        if (arg11Func is null)
            throw new ArgumentNullException(nameof(arg11Func));
        return Enumerate();

        IEnumerable<TryAction> Enumerate()
        {
            var index = 0;
            foreach (var item in coll)
            {
                TryAction result;
                try
                {
                    result = Invoke(invokeAction, item, arg2Func(index), arg3Func(index), arg4Func(index), arg5Func(index), arg6Func(index), arg7Func(index), arg8Func(index), arg9Func(index), arg10Func(index), arg11Func(index), cause);
                }
                catch (Exception exception)
                {
                    result = new FailureAction(exception, 0, cause);
                }
                yield return result;
                index++;
            }
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
    /// <param name="arg2Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg3Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg4Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg5Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg6Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg7Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg8Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg9Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg10Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg11Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg12Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一组 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例表示本次执行成功。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    /// <remarks>输入配置立即校验；枚举时逐项捕获参数工厂与操作异常，源枚举异常直接传播。重复枚举会重复执行。</remarks>
    public static IEnumerable<TryAction> InvokeRange<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(
        IEnumerable<T1> coll,
        Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> invokeAction,
        Func<int, T2> arg2Func, Func<int, T3> arg3Func, Func<int, T4> arg4Func, Func<int, T5> arg5Func, Func<int, T6> arg6Func, Func<int, T7> arg7Func, Func<int, T8> arg8Func, Func<int, T9> arg9Func, Func<int, T10> arg10Func, Func<int, T11> arg11Func, Func<int, T12> arg12Func,
        string cause = null)
    {
        if (coll is null)
            throw new ArgumentNullException(nameof(coll));
        if (invokeAction is null)
            throw new ArgumentNullException(nameof(invokeAction));
        if (arg2Func is null)
            throw new ArgumentNullException(nameof(arg2Func));
        if (arg3Func is null)
            throw new ArgumentNullException(nameof(arg3Func));
        if (arg4Func is null)
            throw new ArgumentNullException(nameof(arg4Func));
        if (arg5Func is null)
            throw new ArgumentNullException(nameof(arg5Func));
        if (arg6Func is null)
            throw new ArgumentNullException(nameof(arg6Func));
        if (arg7Func is null)
            throw new ArgumentNullException(nameof(arg7Func));
        if (arg8Func is null)
            throw new ArgumentNullException(nameof(arg8Func));
        if (arg9Func is null)
            throw new ArgumentNullException(nameof(arg9Func));
        if (arg10Func is null)
            throw new ArgumentNullException(nameof(arg10Func));
        if (arg11Func is null)
            throw new ArgumentNullException(nameof(arg11Func));
        if (arg12Func is null)
            throw new ArgumentNullException(nameof(arg12Func));
        return Enumerate();

        IEnumerable<TryAction> Enumerate()
        {
            var index = 0;
            foreach (var item in coll)
            {
                TryAction result;
                try
                {
                    result = Invoke(invokeAction, item, arg2Func(index), arg3Func(index), arg4Func(index), arg5Func(index), arg6Func(index), arg7Func(index), arg8Func(index), arg9Func(index), arg10Func(index), arg11Func(index), arg12Func(index), cause);
                }
                catch (Exception exception)
                {
                    result = new FailureAction(exception, 0, cause);
                }
                yield return result;
                index++;
            }
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
    /// <param name="arg2Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg3Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg4Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg5Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg6Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg7Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg8Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg9Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg10Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg11Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg12Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg13Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一组 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例表示本次执行成功。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    /// <remarks>输入配置立即校验；枚举时逐项捕获参数工厂与操作异常，源枚举异常直接传播。重复枚举会重复执行。</remarks>
    public static IEnumerable<TryAction> InvokeRange<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(
        IEnumerable<T1> coll,
        Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> invokeAction,
        Func<int, T2> arg2Func, Func<int, T3> arg3Func, Func<int, T4> arg4Func, Func<int, T5> arg5Func, Func<int, T6> arg6Func, Func<int, T7> arg7Func, Func<int, T8> arg8Func, Func<int, T9> arg9Func, Func<int, T10> arg10Func, Func<int, T11> arg11Func, Func<int, T12> arg12Func, Func<int, T13> arg13Func,
        string cause = null)
    {
        if (coll is null)
            throw new ArgumentNullException(nameof(coll));
        if (invokeAction is null)
            throw new ArgumentNullException(nameof(invokeAction));
        if (arg2Func is null)
            throw new ArgumentNullException(nameof(arg2Func));
        if (arg3Func is null)
            throw new ArgumentNullException(nameof(arg3Func));
        if (arg4Func is null)
            throw new ArgumentNullException(nameof(arg4Func));
        if (arg5Func is null)
            throw new ArgumentNullException(nameof(arg5Func));
        if (arg6Func is null)
            throw new ArgumentNullException(nameof(arg6Func));
        if (arg7Func is null)
            throw new ArgumentNullException(nameof(arg7Func));
        if (arg8Func is null)
            throw new ArgumentNullException(nameof(arg8Func));
        if (arg9Func is null)
            throw new ArgumentNullException(nameof(arg9Func));
        if (arg10Func is null)
            throw new ArgumentNullException(nameof(arg10Func));
        if (arg11Func is null)
            throw new ArgumentNullException(nameof(arg11Func));
        if (arg12Func is null)
            throw new ArgumentNullException(nameof(arg12Func));
        if (arg13Func is null)
            throw new ArgumentNullException(nameof(arg13Func));
        return Enumerate();

        IEnumerable<TryAction> Enumerate()
        {
            var index = 0;
            foreach (var item in coll)
            {
                TryAction result;
                try
                {
                    result = Invoke(invokeAction, item, arg2Func(index), arg3Func(index), arg4Func(index), arg5Func(index), arg6Func(index), arg7Func(index), arg8Func(index), arg9Func(index), arg10Func(index), arg11Func(index), arg12Func(index), arg13Func(index), cause);
                }
                catch (Exception exception)
                {
                    result = new FailureAction(exception, 0, cause);
                }
                yield return result;
                index++;
            }
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
    /// <param name="arg2Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg3Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg4Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg5Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg6Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg7Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg8Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg9Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg10Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg11Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg12Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg13Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg14Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一组 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例表示本次执行成功。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    /// <remarks>输入配置立即校验；枚举时逐项捕获参数工厂与操作异常，源枚举异常直接传播。重复枚举会重复执行。</remarks>
    public static IEnumerable<TryAction> InvokeRange<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(
        IEnumerable<T1> coll,
        Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> invokeAction,
        Func<int, T2> arg2Func, Func<int, T3> arg3Func, Func<int, T4> arg4Func, Func<int, T5> arg5Func, Func<int, T6> arg6Func, Func<int, T7> arg7Func, Func<int, T8> arg8Func, Func<int, T9> arg9Func, Func<int, T10> arg10Func, Func<int, T11> arg11Func, Func<int, T12> arg12Func, Func<int, T13> arg13Func, Func<int, T14> arg14Func,
        string cause = null)
    {
        if (coll is null)
            throw new ArgumentNullException(nameof(coll));
        if (invokeAction is null)
            throw new ArgumentNullException(nameof(invokeAction));
        if (arg2Func is null)
            throw new ArgumentNullException(nameof(arg2Func));
        if (arg3Func is null)
            throw new ArgumentNullException(nameof(arg3Func));
        if (arg4Func is null)
            throw new ArgumentNullException(nameof(arg4Func));
        if (arg5Func is null)
            throw new ArgumentNullException(nameof(arg5Func));
        if (arg6Func is null)
            throw new ArgumentNullException(nameof(arg6Func));
        if (arg7Func is null)
            throw new ArgumentNullException(nameof(arg7Func));
        if (arg8Func is null)
            throw new ArgumentNullException(nameof(arg8Func));
        if (arg9Func is null)
            throw new ArgumentNullException(nameof(arg9Func));
        if (arg10Func is null)
            throw new ArgumentNullException(nameof(arg10Func));
        if (arg11Func is null)
            throw new ArgumentNullException(nameof(arg11Func));
        if (arg12Func is null)
            throw new ArgumentNullException(nameof(arg12Func));
        if (arg13Func is null)
            throw new ArgumentNullException(nameof(arg13Func));
        if (arg14Func is null)
            throw new ArgumentNullException(nameof(arg14Func));
        return Enumerate();

        IEnumerable<TryAction> Enumerate()
        {
            var index = 0;
            foreach (var item in coll)
            {
                TryAction result;
                try
                {
                    result = Invoke(invokeAction, item, arg2Func(index), arg3Func(index), arg4Func(index), arg5Func(index), arg6Func(index), arg7Func(index), arg8Func(index), arg9Func(index), arg10Func(index), arg11Func(index), arg12Func(index), arg13Func(index), arg14Func(index), cause);
                }
                catch (Exception exception)
                {
                    result = new FailureAction(exception, 0, cause);
                }
                yield return result;
                index++;
            }
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
    /// <param name="arg2Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg3Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg4Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg5Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg6Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg7Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg8Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg9Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg10Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg11Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg12Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg13Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg14Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg15Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一组 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例表示本次执行成功。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    /// <remarks>输入配置立即校验；枚举时逐项捕获参数工厂与操作异常，源枚举异常直接传播。重复枚举会重复执行。</remarks>
    public static IEnumerable<TryAction> InvokeRange<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(
        IEnumerable<T1> coll,
        Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> invokeAction,
        Func<int, T2> arg2Func, Func<int, T3> arg3Func, Func<int, T4> arg4Func, Func<int, T5> arg5Func, Func<int, T6> arg6Func, Func<int, T7> arg7Func, Func<int, T8> arg8Func, Func<int, T9> arg9Func, Func<int, T10> arg10Func, Func<int, T11> arg11Func, Func<int, T12> arg12Func, Func<int, T13> arg13Func, Func<int, T14> arg14Func, Func<int, T15> arg15Func,
        string cause = null)
    {
        if (coll is null)
            throw new ArgumentNullException(nameof(coll));
        if (invokeAction is null)
            throw new ArgumentNullException(nameof(invokeAction));
        if (arg2Func is null)
            throw new ArgumentNullException(nameof(arg2Func));
        if (arg3Func is null)
            throw new ArgumentNullException(nameof(arg3Func));
        if (arg4Func is null)
            throw new ArgumentNullException(nameof(arg4Func));
        if (arg5Func is null)
            throw new ArgumentNullException(nameof(arg5Func));
        if (arg6Func is null)
            throw new ArgumentNullException(nameof(arg6Func));
        if (arg7Func is null)
            throw new ArgumentNullException(nameof(arg7Func));
        if (arg8Func is null)
            throw new ArgumentNullException(nameof(arg8Func));
        if (arg9Func is null)
            throw new ArgumentNullException(nameof(arg9Func));
        if (arg10Func is null)
            throw new ArgumentNullException(nameof(arg10Func));
        if (arg11Func is null)
            throw new ArgumentNullException(nameof(arg11Func));
        if (arg12Func is null)
            throw new ArgumentNullException(nameof(arg12Func));
        if (arg13Func is null)
            throw new ArgumentNullException(nameof(arg13Func));
        if (arg14Func is null)
            throw new ArgumentNullException(nameof(arg14Func));
        if (arg15Func is null)
            throw new ArgumentNullException(nameof(arg15Func));
        return Enumerate();

        IEnumerable<TryAction> Enumerate()
        {
            var index = 0;
            foreach (var item in coll)
            {
                TryAction result;
                try
                {
                    result = Invoke(invokeAction, item, arg2Func(index), arg3Func(index), arg4Func(index), arg5Func(index), arg6Func(index), arg7Func(index), arg8Func(index), arg9Func(index), arg10Func(index), arg11Func(index), arg12Func(index), arg13Func(index), arg14Func(index), arg15Func(index), cause);
                }
                catch (Exception exception)
                {
                    result = new FailureAction(exception, 0, cause);
                }
                yield return result;
                index++;
            }
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
    /// <param name="arg2Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg3Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg4Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg5Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg6Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg7Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg8Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg9Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg10Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg11Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg12Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg13Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg14Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg15Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="arg16Func">根据从零开始的项索引生成操作参数的非空工厂</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一组 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例表示本次执行成功。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    /// <remarks>输入配置立即校验；枚举时逐项捕获参数工厂与操作异常，源枚举异常直接传播。重复枚举会重复执行。</remarks>
    public static IEnumerable<TryAction> InvokeRange<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(
        IEnumerable<T1> coll,
        Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> invokeAction,
        Func<int, T2> arg2Func, Func<int, T3> arg3Func, Func<int, T4> arg4Func, Func<int, T5> arg5Func, Func<int, T6> arg6Func, Func<int, T7> arg7Func, Func<int, T8> arg8Func, Func<int, T9> arg9Func, Func<int, T10> arg10Func, Func<int, T11> arg11Func, Func<int, T12> arg12Func, Func<int, T13> arg13Func, Func<int, T14> arg14Func, Func<int, T15> arg15Func, Func<int, T16> arg16Func,
        string cause = null)
    {
        if (coll is null)
            throw new ArgumentNullException(nameof(coll));
        if (invokeAction is null)
            throw new ArgumentNullException(nameof(invokeAction));
        if (arg2Func is null)
            throw new ArgumentNullException(nameof(arg2Func));
        if (arg3Func is null)
            throw new ArgumentNullException(nameof(arg3Func));
        if (arg4Func is null)
            throw new ArgumentNullException(nameof(arg4Func));
        if (arg5Func is null)
            throw new ArgumentNullException(nameof(arg5Func));
        if (arg6Func is null)
            throw new ArgumentNullException(nameof(arg6Func));
        if (arg7Func is null)
            throw new ArgumentNullException(nameof(arg7Func));
        if (arg8Func is null)
            throw new ArgumentNullException(nameof(arg8Func));
        if (arg9Func is null)
            throw new ArgumentNullException(nameof(arg9Func));
        if (arg10Func is null)
            throw new ArgumentNullException(nameof(arg10Func));
        if (arg11Func is null)
            throw new ArgumentNullException(nameof(arg11Func));
        if (arg12Func is null)
            throw new ArgumentNullException(nameof(arg12Func));
        if (arg13Func is null)
            throw new ArgumentNullException(nameof(arg13Func));
        if (arg14Func is null)
            throw new ArgumentNullException(nameof(arg14Func));
        if (arg15Func is null)
            throw new ArgumentNullException(nameof(arg15Func));
        if (arg16Func is null)
            throw new ArgumentNullException(nameof(arg16Func));
        return Enumerate();

        IEnumerable<TryAction> Enumerate()
        {
            var index = 0;
            foreach (var item in coll)
            {
                TryAction result;
                try
                {
                    result = Invoke(invokeAction, item, arg2Func(index), arg3Func(index), arg4Func(index), arg5Func(index), arg6Func(index), arg7Func(index), arg8Func(index), arg9Func(index), arg10Func(index), arg11Func(index), arg12Func(index), arg13Func(index), arg14Func(index), arg15Func(index), arg16Func(index), cause);
                }
                catch (Exception exception)
                {
                    result = new FailureAction(exception, 0, cause);
                }
                yield return result;
                index++;
            }
        }
    }
}
