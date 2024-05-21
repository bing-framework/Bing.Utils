﻿namespace Bing.Exceptions;

/// <summary>
/// 尝试
/// </summary>
public static partial class Try
{
    /// <summary>
    /// 创建
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="createFuncAsync">创建函数</param>
    public static Try<T> CreateFromTask<T>(Func<Task<T>> createFuncAsync)
    {
        try
        {
            return LiftValue(CallAsyncInSync(createFuncAsync));
        }
        catch (Exception e)
        {
            return LiftException<T>(e);
        }
    }

    /// <summary>
    /// 提取值
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="value">值</param>
    public static Try<T> LiftValue<T>(T value) => new Success<T>(value);

    /// <summary>
    /// 提取异常
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="exception">异常</param>
    /// <param name="cause">导致错误的原因</param>
    public static Try<T> LiftException<T>(Exception exception, string cause = null) => new Failure<T>(exception, cause);

    /// <summary>
    /// 同步调异步
    /// </summary>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="taskFunc">异步函数</param>
    /// <param name="cancellationToken">取消令牌</param>
    private static TResult CallAsyncInSync<TResult>(Func<Task<TResult>> taskFunc, CancellationToken cancellationToken = default)
    {
        if (taskFunc is null)
            throw new ArgumentNullException(nameof(taskFunc));
        var task = Create(taskFunc).GetValue();
        if (task is null)
            throw new InvalidOperationException($"The task factory {nameof(taskFunc)} failed to run.");
        return ThenWaitAndUnwrapException(task, cancellationToken);
    }

    /// <summary>
    /// 等待执行并展开异常
    /// </summary>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="task">异步</param>
    /// <param name="cancellationToken">取消令牌</param>
    private static TResult ThenWaitAndUnwrapException<TResult>(Task<TResult> task, CancellationToken cancellationToken)
    {
        if (task is null)
            throw new ArgumentNullException(nameof(task));
        try
        {
            task.Wait(cancellationToken);
            return task.Result;
        }
        catch (AggregateException e)
        {
            throw ExceptionHelper.PrepareForRethrow(e.InnerException);
        }
    }

    /// <summary>
    /// 调用
    /// </summary>
    /// <param name="invokeAction">操作</param>
    public static TryAction Invoke(Action invokeAction)
    {
        try
        {
            invokeAction();
            return new SuccessAction(invokeAction.GetHashCode());
        }
        catch (Exception e)
        {
            return new FailureAction(e, invokeAction?.GetHashCode() ?? 0);
        }
    }
}