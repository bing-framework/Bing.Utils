namespace Bing.Exceptions;

// 尝试 - 异步调用
public static partial class Try
{
    /// <summary>
    /// 调用委托，返回一个 <see cref="TryAction"/> 实例
    /// </summary>
    /// <param name="invokeAsyncAction">需要调用的操作</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一个 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例的值为操作的哈希码。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    /// <exception cref="ArgumentNullException">当输入的操作为 null 时抛出此异常</exception>
    public static async Task<TryAction> InvokeAsync(Task<Action> invokeAsyncAction, string cause = null)
    {
        try
        {
            if (invokeAsyncAction is null)
                throw new ArgumentNullException(nameof(invokeAsyncAction));
            var invokeAction = await invokeAsyncAction;
            if (invokeAction is null)
                throw new ArgumentNullException(nameof(invokeAction));
            invokeAction();
            return new SuccessAction(invokeAsyncAction.GetHashCode());
        }
        catch (Exception e)
        {
            return new FailureAction(e, invokeAsyncAction?.GetHashCode() ?? 0, cause);
        }
    }

    /// <summary>
    /// 调用委托，返回一个 <see cref="TryAction"/> 实例
    /// </summary>
    /// <typeparam name="T">输入参数类型</typeparam>
    /// <param name="invokeAsyncAction">需要调用的操作</param>
    /// <param name="obj">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一个 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例的值为操作的哈希码。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    /// <exception cref="ArgumentNullException">当输入的操作为 null 时抛出此异常</exception>
    public static async Task<TryAction> InvokeAsync<T>(Task<Action<T>> invokeAsyncAction, T obj, string cause = null)
    {
        try
        {
            if (invokeAsyncAction is null)
                throw new ArgumentNullException(nameof(invokeAsyncAction));
            var invokeAction = await invokeAsyncAction;
            if (invokeAction is null)
                throw new ArgumentNullException(nameof(invokeAction));
            invokeAction(obj);
            return new SuccessAction(invokeAsyncAction.GetHashCode());
        }
        catch (Exception e)
        {
            return new FailureAction(e, invokeAsyncAction?.GetHashCode() ?? 0, cause);
        }
    }

    /// <summary>
    /// 调用委托，返回一个 <see cref="TryAction"/> 实例
    /// </summary>
    /// <typeparam name="T1">输入参数类型</typeparam>
    /// <typeparam name="T2">输入参数类型</typeparam>
    /// <param name="invokeAsyncAction">需要调用的操作</param>
    /// <param name="arg1">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg2">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一个 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例的值为操作的哈希码。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    /// <exception cref="ArgumentNullException">当输入的操作为 null 时抛出此异常</exception>
    public static async Task<TryAction> InvokeAsync<T1, T2>(
        Task<Action<T1, T2>> invokeAsyncAction,
        T1 arg1, T2 arg2, string cause = null)
    {
        try
        {
            if (invokeAsyncAction is null)
                throw new ArgumentNullException(nameof(invokeAsyncAction));
            var invokeAction = await invokeAsyncAction;
            if (invokeAction is null)
                throw new ArgumentNullException(nameof(invokeAction));
            invokeAction(arg1, arg2);
            return new SuccessAction(invokeAsyncAction.GetHashCode());
        }
        catch (Exception e)
        {
            return new FailureAction(e, invokeAsyncAction?.GetHashCode() ?? 0, cause);
        }
    }

    /// <summary>
    /// 调用委托，返回一个 <see cref="TryAction"/> 实例
    /// </summary>
    /// <typeparam name="T1">输入参数类型</typeparam>
    /// <typeparam name="T2">输入参数类型</typeparam>
    /// <typeparam name="T3">输入参数类型</typeparam>
    /// <param name="invokeAsyncAction">需要调用的操作</param>
    /// <param name="arg1">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg2">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg3">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一个 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例的值为操作的哈希码。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    /// <exception cref="ArgumentNullException">当输入的操作为 null 时抛出此异常</exception>
    public static async Task<TryAction> InvokeAsync<T1, T2, T3>(
        Task<Action<T1, T2, T3>> invokeAsyncAction,
        T1 arg1, T2 arg2, T3 arg3, string cause = null)
    {
        try
        {
            if (invokeAsyncAction is null)
                throw new ArgumentNullException(nameof(invokeAsyncAction));
            var invokeAction = await invokeAsyncAction;
            if (invokeAction is null)
                throw new ArgumentNullException(nameof(invokeAction));
            invokeAction(arg1, arg2, arg3);
            return new SuccessAction(invokeAsyncAction.GetHashCode());
        }
        catch (Exception e)
        {
            return new FailureAction(e, invokeAsyncAction?.GetHashCode() ?? 0, cause);
        }
    }

    /// <summary>
    /// 调用委托，返回一个 <see cref="TryAction"/> 实例
    /// </summary>
    /// <typeparam name="T1">输入参数类型</typeparam>
    /// <typeparam name="T2">输入参数类型</typeparam>
    /// <typeparam name="T3">输入参数类型</typeparam>
    /// <typeparam name="T4">输入参数类型</typeparam>
    /// <param name="invokeAsyncAction">需要调用的操作</param>
    /// <param name="arg1">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg2">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg3">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg4">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一个 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例的值为操作的哈希码。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    /// <exception cref="ArgumentNullException">当输入的操作为 null 时抛出此异常</exception>
    public static async Task<TryAction> InvokeAsync<T1, T2, T3, T4>(
        Task<Action<T1, T2, T3, T4>> invokeAsyncAction,
        T1 arg1, T2 arg2, T3 arg3, T4 arg4, string cause = null)
    {
        try
        {
            if (invokeAsyncAction is null)
                throw new ArgumentNullException(nameof(invokeAsyncAction));
            var invokeAction = await invokeAsyncAction;
            if (invokeAction is null)
                throw new ArgumentNullException(nameof(invokeAction));
            invokeAction(arg1, arg2, arg3, arg4);
            return new SuccessAction(invokeAsyncAction.GetHashCode());
        }
        catch (Exception e)
        {
            return new FailureAction(e, invokeAsyncAction?.GetHashCode() ?? 0, cause);
        }
    }

    /// <summary>
    /// 调用委托，返回一个 <see cref="TryAction"/> 实例
    /// </summary>
    /// <typeparam name="T1">输入参数类型</typeparam>
    /// <typeparam name="T2">输入参数类型</typeparam>
    /// <typeparam name="T3">输入参数类型</typeparam>
    /// <typeparam name="T4">输入参数类型</typeparam>
    /// <typeparam name="T5">输入参数类型</typeparam>
    /// <param name="invokeAsyncAction">需要调用的操作</param>
    /// <param name="arg1">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg2">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg3">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg4">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg5">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一个 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例的值为操作的哈希码。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    /// <exception cref="ArgumentNullException">当输入的操作为 null 时抛出此异常</exception>
    public static async Task<TryAction> InvokeAsync<T1, T2, T3, T4, T5>(
        Task<Action<T1, T2, T3, T4, T5>> invokeAsyncAction,
        T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, string cause = null)
    {
        try
        {
            if (invokeAsyncAction is null)
                throw new ArgumentNullException(nameof(invokeAsyncAction));
            var invokeAction = await invokeAsyncAction;
            if (invokeAction is null)
                throw new ArgumentNullException(nameof(invokeAction));
            invokeAction(arg1, arg2, arg3, arg4, arg5);
            return new SuccessAction(invokeAsyncAction.GetHashCode());
        }
        catch (Exception e)
        {
            return new FailureAction(e, invokeAsyncAction?.GetHashCode() ?? 0, cause);
        }
    }

    /// <summary>
    /// 调用委托，返回一个 <see cref="TryAction"/> 实例
    /// </summary>
    /// <typeparam name="T1">输入参数类型</typeparam>
    /// <typeparam name="T2">输入参数类型</typeparam>
    /// <typeparam name="T3">输入参数类型</typeparam>
    /// <typeparam name="T4">输入参数类型</typeparam>
    /// <typeparam name="T5">输入参数类型</typeparam>
    /// <typeparam name="T6">输入参数类型</typeparam>
    /// <param name="invokeAsyncAction">需要调用的操作</param>
    /// <param name="arg1">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg2">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg3">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg4">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg5">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg6">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一个 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例的值为操作的哈希码。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    /// <exception cref="ArgumentNullException">当输入的操作为 null 时抛出此异常</exception>
    public static async Task<TryAction> InvokeAsync<T1, T2, T3, T4, T5, T6>(
        Task<Action<T1, T2, T3, T4, T5, T6>> invokeAsyncAction,
        T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, string cause = null)
    {
        try
        {
            if (invokeAsyncAction is null)
                throw new ArgumentNullException(nameof(invokeAsyncAction));
            var invokeAction = await invokeAsyncAction;
            if (invokeAction is null)
                throw new ArgumentNullException(nameof(invokeAction));
            invokeAction(arg1, arg2, arg3, arg4, arg5, arg6);
            return new SuccessAction(invokeAsyncAction.GetHashCode());
        }
        catch (Exception e)
        {
            return new FailureAction(e, invokeAsyncAction?.GetHashCode() ?? 0, cause);
        }
    }

    /// <summary>
    /// 调用委托，返回一个 <see cref="TryAction"/> 实例
    /// </summary>
    /// <typeparam name="T1">输入参数类型</typeparam>
    /// <typeparam name="T2">输入参数类型</typeparam>
    /// <typeparam name="T3">输入参数类型</typeparam>
    /// <typeparam name="T4">输入参数类型</typeparam>
    /// <typeparam name="T5">输入参数类型</typeparam>
    /// <typeparam name="T6">输入参数类型</typeparam>
    /// <typeparam name="T7">输入参数类型</typeparam>
    /// <param name="invokeAsyncAction">需要调用的操作</param>
    /// <param name="arg1">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg2">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg3">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg4">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg5">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg6">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg7">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一个 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例的值为操作的哈希码。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    /// <exception cref="ArgumentNullException">当输入的操作为 null 时抛出此异常</exception>
    public static async Task<TryAction> InvokeAsync<T1, T2, T3, T4, T5, T6, T7>(
        Task<Action<T1, T2, T3, T4, T5, T6, T7>> invokeAsyncAction,
        T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, string cause = null)
    {
        try
        {
            if (invokeAsyncAction is null)
                throw new ArgumentNullException(nameof(invokeAsyncAction));
            var invokeAction = await invokeAsyncAction;
            if (invokeAction is null)
                throw new ArgumentNullException(nameof(invokeAction));
            invokeAction(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            return new SuccessAction(invokeAsyncAction.GetHashCode());
        }
        catch (Exception e)
        {
            return new FailureAction(e, invokeAsyncAction?.GetHashCode() ?? 0, cause);
        }
    }

    /// <summary>
    /// 调用委托，返回一个 <see cref="TryAction"/> 实例
    /// </summary>
    /// <typeparam name="T1">输入参数类型</typeparam>
    /// <typeparam name="T2">输入参数类型</typeparam>
    /// <typeparam name="T3">输入参数类型</typeparam>
    /// <typeparam name="T4">输入参数类型</typeparam>
    /// <typeparam name="T5">输入参数类型</typeparam>
    /// <typeparam name="T6">输入参数类型</typeparam>
    /// <typeparam name="T7">输入参数类型</typeparam>
    /// <typeparam name="T8">输入参数类型</typeparam>
    /// <param name="invokeAsyncAction">需要调用的操作</param>
    /// <param name="arg1">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg2">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg3">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg4">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg5">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg6">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg7">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg8">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一个 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例的值为操作的哈希码。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    /// <exception cref="ArgumentNullException">当输入的操作为 null 时抛出此异常</exception>
    public static async Task<TryAction> InvokeAsync<T1, T2, T3, T4, T5, T6, T7, T8>(
        Task<Action<T1, T2, T3, T4, T5, T6, T7, T8>> invokeAsyncAction,
        T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, string cause = null)
    {
        try
        {
            if (invokeAsyncAction is null)
                throw new ArgumentNullException(nameof(invokeAsyncAction));
            var invokeAction = await invokeAsyncAction;
            if (invokeAction is null)
                throw new ArgumentNullException(nameof(invokeAction));
            invokeAction(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
            return new SuccessAction(invokeAsyncAction.GetHashCode());
        }
        catch (Exception e)
        {
            return new FailureAction(e, invokeAsyncAction?.GetHashCode() ?? 0, cause);
        }
    }

    /// <summary>
    /// 调用委托，返回一个 <see cref="TryAction"/> 实例
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
    /// <param name="invokeAsyncAction">需要调用的操作</param>
    /// <param name="arg1">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg2">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg3">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg4">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg5">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg6">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg7">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg8">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg9">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一个 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例的值为操作的哈希码。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    /// <exception cref="ArgumentNullException">当输入的操作为 null 时抛出此异常</exception>
    public static async Task<TryAction> InvokeAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9>(
        Task<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9>> invokeAsyncAction,
        T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, string cause = null)
    {
        try
        {
            if (invokeAsyncAction is null)
                throw new ArgumentNullException(nameof(invokeAsyncAction));
            var invokeAction = await invokeAsyncAction;
            if (invokeAction is null)
                throw new ArgumentNullException(nameof(invokeAction));
            invokeAction(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
            return new SuccessAction(invokeAsyncAction.GetHashCode());
        }
        catch (Exception e)
        {
            return new FailureAction(e, invokeAsyncAction?.GetHashCode() ?? 0, cause);
        }
    }

    /// <summary>
    /// 调用委托，返回一个 <see cref="TryAction"/> 实例
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
    /// <param name="invokeAsyncAction">需要调用的操作</param>
    /// <param name="arg1">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg2">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg3">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg4">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg5">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg6">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg7">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg8">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg9">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg10">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一个 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例的值为操作的哈希码。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    /// <exception cref="ArgumentNullException">当输入的操作为 null 时抛出此异常</exception>
    public static async Task<TryAction> InvokeAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
        Task<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>> invokeAsyncAction,
        T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, string cause = null)
    {
        try
        {
            if (invokeAsyncAction is null)
                throw new ArgumentNullException(nameof(invokeAsyncAction));
            var invokeAction = await invokeAsyncAction;
            if (invokeAction is null)
                throw new ArgumentNullException(nameof(invokeAction));
            invokeAction(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
            return new SuccessAction(invokeAsyncAction.GetHashCode());
        }
        catch (Exception e)
        {
            return new FailureAction(e, invokeAsyncAction?.GetHashCode() ?? 0, cause);
        }
    }

    /// <summary>
    /// 调用委托，返回一个 <see cref="TryAction"/> 实例
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
    /// <param name="invokeAsyncAction">需要调用的操作</param>
    /// <param name="arg1">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg2">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg3">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg4">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg5">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg6">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg7">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg8">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg9">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg10">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg11">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一个 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例的值为操作的哈希码。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    /// <exception cref="ArgumentNullException">当输入的操作为 null 时抛出此异常</exception>
    public static async Task<TryAction> InvokeAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
        Task<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>> invokeAsyncAction,
        T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, string cause = null)
    {
        try
        {
            if (invokeAsyncAction is null)
                throw new ArgumentNullException(nameof(invokeAsyncAction));
            var invokeAction = await invokeAsyncAction;
            if (invokeAction is null)
                throw new ArgumentNullException(nameof(invokeAction));
            invokeAction(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11);
            return new SuccessAction(invokeAsyncAction.GetHashCode());
        }
        catch (Exception e)
        {
            return new FailureAction(e, invokeAsyncAction?.GetHashCode() ?? 0, cause);
        }
    }

    /// <summary>
    /// 调用委托，返回一个 <see cref="TryAction"/> 实例
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
    /// <param name="invokeAsyncAction">需要调用的操作</param>
    /// <param name="arg1">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg2">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg3">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg4">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg5">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg6">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg7">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg8">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg9">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg10">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg11">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg12">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一个 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例的值为操作的哈希码。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    /// <exception cref="ArgumentNullException">当输入的操作为 null 时抛出此异常</exception>
    public static async Task<TryAction> InvokeAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(
        Task<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>> invokeAsyncAction,
        T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, string cause = null)
    {
        try
        {
            if (invokeAsyncAction is null)
                throw new ArgumentNullException(nameof(invokeAsyncAction));
            var invokeAction = await invokeAsyncAction;
            if (invokeAction is null)
                throw new ArgumentNullException(nameof(invokeAction));
            invokeAction(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);
            return new SuccessAction(invokeAsyncAction.GetHashCode());
        }
        catch (Exception e)
        {
            return new FailureAction(e, invokeAsyncAction?.GetHashCode() ?? 0, cause);
        }
    }

    /// <summary>
    /// 调用委托，返回一个 <see cref="TryAction"/> 实例
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
    /// <param name="invokeAsyncAction">需要调用的操作</param>
    /// <param name="arg1">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg2">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg3">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg4">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg5">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg6">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg7">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg8">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg9">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg10">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg11">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg12">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg13">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一个 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例的值为操作的哈希码。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    /// <exception cref="ArgumentNullException">当输入的操作为 null 时抛出此异常</exception>
    public static async Task<TryAction> InvokeAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(
        Task<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>> invokeAsyncAction,
        T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, string cause = null)
    {
        try
        {
            if (invokeAsyncAction is null)
                throw new ArgumentNullException(nameof(invokeAsyncAction));
            var invokeAction = await invokeAsyncAction;
            if (invokeAction is null)
                throw new ArgumentNullException(nameof(invokeAction));
            invokeAction(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13);
            return new SuccessAction(invokeAsyncAction.GetHashCode());
        }
        catch (Exception e)
        {
            return new FailureAction(e, invokeAsyncAction?.GetHashCode() ?? 0, cause);
        }
    }

    /// <summary>
    /// 调用委托，返回一个 <see cref="TryAction"/> 实例
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
    /// <param name="invokeAsyncAction">需要调用的操作</param>
    /// <param name="arg1">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg2">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg3">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg4">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg5">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg6">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg7">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg8">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg9">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg10">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg11">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg12">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg13">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg14">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一个 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例的值为操作的哈希码。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    /// <exception cref="ArgumentNullException">当输入的操作为 null 时抛出此异常</exception>
    public static async Task<TryAction> InvokeAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(
        Task<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>> invokeAsyncAction,
        T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, string cause = null)
    {
        try
        {
            if (invokeAsyncAction is null)
                throw new ArgumentNullException(nameof(invokeAsyncAction));
            var invokeAction = await invokeAsyncAction;
            if (invokeAction is null)
                throw new ArgumentNullException(nameof(invokeAction));
            invokeAction(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14);
            return new SuccessAction(invokeAsyncAction.GetHashCode());
        }
        catch (Exception e)
        {
            return new FailureAction(e, invokeAsyncAction?.GetHashCode() ?? 0, cause);
        }
    }

    /// <summary>
    /// 调用委托，返回一个 <see cref="TryAction"/> 实例
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
    /// <param name="invokeAsyncAction">需要调用的操作</param>
    /// <param name="arg1">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg2">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg3">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg4">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg5">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg6">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg7">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg8">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg9">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg10">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg11">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg12">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg13">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg14">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg15">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一个 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例的值为操作的哈希码。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    /// <exception cref="ArgumentNullException">当输入的操作为 null 时抛出此异常</exception>
    public static async Task<TryAction> InvokeAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(
        Task<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>> invokeAsyncAction,
        T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, string cause = null)
    {
        try
        {
            if (invokeAsyncAction is null)
                throw new ArgumentNullException(nameof(invokeAsyncAction));
            var invokeAction = await invokeAsyncAction;
            if (invokeAction is null)
                throw new ArgumentNullException(nameof(invokeAction));
            invokeAction(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15);
            return new SuccessAction(invokeAsyncAction.GetHashCode());
        }
        catch (Exception e)
        {
            return new FailureAction(e, invokeAsyncAction?.GetHashCode() ?? 0, cause);
        }
    }

    /// <summary>
    /// 调用委托，返回一个 <see cref="TryAction"/> 实例
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
    /// <param name="invokeAsyncAction">需要调用的操作</param>
    /// <param name="arg1">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg2">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg3">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg4">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg5">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg6">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg7">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg8">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg9">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg10">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg11">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg12">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg13">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg14">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg15">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="arg16">传递给 <paramref name="invokeAsyncAction"/> 的参数</param>
    /// <param name="cause">异常的原因，可选参数</param>
    /// <returns>
    /// 返回一个 <see cref="TryAction"/> 实例。
    /// 如果操作成功执行，返回的 <see cref="TryAction"/> 实例的值为操作的哈希码。
    /// 如果操作抛出异常，返回的 <see cref="TryAction"/> 实例包含该异常。
    /// </returns>
    /// <exception cref="ArgumentNullException">当输入的操作为 null 时抛出此异常</exception>
    public static async Task<TryAction> InvokeAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(
        Task<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>> invokeAsyncAction,
        T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16, string cause = null)
    {
        try
        {
            if (invokeAsyncAction is null)
                throw new ArgumentNullException(nameof(invokeAsyncAction));
            var invokeAction = await invokeAsyncAction;
            if (invokeAction is null)
                throw new ArgumentNullException(nameof(invokeAction));
            invokeAction(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16);
            return new SuccessAction(invokeAsyncAction.GetHashCode());
        }
        catch (Exception e)
        {
            return new FailureAction(e, invokeAsyncAction?.GetHashCode() ?? 0, cause);
        }
    }
}