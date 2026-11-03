namespace Bing.Exceptions;

/// <summary>
/// 提供等待实际任务完成的异常结果工厂。
/// </summary>
public static partial class Try
{
    /// <summary>
    /// 异步执行值工厂，将成功值或原异常保存为尝试结果。
    /// </summary>
    /// <typeparam name="T">结果值类型。</typeparam>
    /// <param name="createFunctionAsync">异步工厂；空委托、空任务及同步抛错均转换为失败。</param>
    /// <param name="cause">可选的业务失败原因。</param>
    /// <returns>任务实际完成后的尝试结果；取消也表示为失败，外层任务正常完成。</returns>
    public static async Task<Try<T>> CreateAsync<T>(Func<Task<T>> createFunctionAsync, string cause = null)
    {
        try
        {
            if (createFunctionAsync is null)
                throw new ArgumentNullException(nameof(createFunctionAsync));
            var task = createFunctionAsync();
            if (task is null)
                throw new ArgumentNullException(nameof(task));
            return LiftValue(await task.ConfigureAwait(false));
        }
        catch (Exception exception)
        {
            return LiftException<T>(exception, cause);
        }
    }

    /// <summary>
    /// 透传取消令牌并等待值工厂真正结束。
    /// </summary>
    /// <typeparam name="T">结果值类型。</typeparam>
    /// <param name="createFunctionAsync">接受取消令牌的异步工厂。</param>
    /// <param name="cancellationToken">预取消时不调用工厂；执行中由工厂协作取消。</param>
    /// <param name="cause">可选的业务失败原因。</param>
    /// <returns>值或原异常的尝试结果；保留取消异常及其令牌。</returns>
    /// <remarks>工厂忽略取消时仍等待它完成；不取消单独的包装等待，也不遗留后台操作。</remarks>
    public static async Task<Try<T>> CreateAsync<T>(Func<CancellationToken, Task<T>> createFunctionAsync, CancellationToken cancellationToken, string cause = null)
    {
        try
        {
            if (createFunctionAsync is null)
                throw new ArgumentNullException(nameof(createFunctionAsync));
            cancellationToken.ThrowIfCancellationRequested();
            var task = createFunctionAsync(cancellationToken);
            if (task is null)
                throw new ArgumentNullException(nameof(task));
            return LiftValue(await task.ConfigureAwait(false));
        }
        catch (Exception exception)
        {
            return LiftException<T>(exception, cause);
        }
    }

    /// <summary>
    /// 等待异步操作完成，并将执行异常保存为失败结果。
    /// </summary>
    /// <param name="invokeAsyncAction">异步操作；空委托、空任务及同步抛错均转换为失败。</param>
    /// <param name="cause">可选的业务失败原因。</param>
    /// <returns>真实操作结束后的结果；取消表示为失败，外层任务正常完成。</returns>
    public static async Task<TryAction> InvokeTaskAsync(Func<Task> invokeAsyncAction, string cause = null)
    {
        try
        {
            if (invokeAsyncAction is null)
                throw new ArgumentNullException(nameof(invokeAsyncAction));
            var task = invokeAsyncAction();
            if (task is null)
                throw new ArgumentNullException(nameof(task));
            await task.ConfigureAwait(false);
            return new SuccessAction();
        }
        catch (Exception exception)
        {
            return new FailureAction(exception, 0, cause);
        }
    }

    /// <summary>
    /// 透传取消令牌并等待异步操作真正结束。
    /// </summary>
    /// <param name="invokeAsyncAction">接受取消令牌的异步操作。</param>
    /// <param name="cancellationToken">预取消时不执行操作；执行中由操作协作取消。</param>
    /// <param name="cause">可选的业务失败原因。</param>
    /// <returns>操作结果；原取消异常保存在失败结果中。</returns>
    /// <remarks>操作忽略令牌时仍等待其完成；已成功完成的任务不会因令牌随后取消而被改为失败。</remarks>
    public static async Task<TryAction> InvokeTaskAsync(Func<CancellationToken, Task> invokeAsyncAction, CancellationToken cancellationToken, string cause = null)
    {
        try
        {
            if (invokeAsyncAction is null)
                throw new ArgumentNullException(nameof(invokeAsyncAction));
            cancellationToken.ThrowIfCancellationRequested();
            var task = invokeAsyncAction(cancellationToken);
            if (task is null)
                throw new ArgumentNullException(nameof(task));
            await task.ConfigureAwait(false);
            return new SuccessAction();
        }
        catch (Exception exception)
        {
            return new FailureAction(exception, 0, cause);
        }
    }
}
