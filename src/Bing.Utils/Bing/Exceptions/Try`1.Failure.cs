using System.Runtime.ExceptionServices;

namespace Bing.Exceptions;

/// <summary>
/// 尝试失败
/// </summary>
/// <typeparam name="T">类型</typeparam>
public class Failure<T> : Try<T>
{
    /// <summary>
    /// 初始化一个<see cref="Failure{T}"/>类型的实例
    /// </summary>
    /// <param name="exception">异常</param>
    /// <param name="cause">导致错误的原因</param>
    internal Failure(Exception exception, string cause) => Exception = new TryCreatingValueException(exception, cause);

    /// <inheritdoc />
    public override bool IsFailure => true;

    /// <inheritdoc />
    public override bool IsSuccess => false;

    /// <inheritdoc />
    public override TryCreatingValueException Exception { get; }

    /// <inheritdoc />
    public override T Value => throw Rethrow();

    /// <inheritdoc />
    public override string ToString() => $"Failure<{Exception}>";

    /// <summary>
    /// 相等
    /// </summary>
    /// <param name="other">对象</param>
    public bool Equals(Failure<T> other) => !(other is null) && ReferenceEquals(Exception, other.Exception);

    /// <inheritdoc />
    public override bool Equals(object obj) => obj is Failure<T> failure && Equals(failure);

    /// <inheritdoc />
    public override int GetHashCode() => Exception.GetHashCode();

    /// <inheritdoc />
    public override void Deconstruct(out T value, out Exception exception)
    {
        value = default!;
        exception = Exception;
    }

    /// <inheritdoc />
    public override void Deconstruct(out T value, out bool tryResult, out Exception exception)
    {
        value = Value;
        tryResult = IsSuccess;
        exception = Exception;
    }

    /// <inheritdoc />
    public override Try<T> Recover(Func<TryCreatingValueException, T> recoverFunction)
    {
        return RecoverWith(ex => Try.LiftValue(recoverFunction(ex)));
    }

    /// <inheritdoc />
    public override Try<T> Recover(Func<Exception, string, T> recoverFunction)
    {
        return RecoverWith(ex => Try.LiftValue(recoverFunction(ex?.InnerException, ex?.Cause)));
    }

    /// <inheritdoc />
    public override Try<T> RecoverWith(Func<TryCreatingValueException, Try<T>> recoverFunction)
    {
        try
        {
            return recoverFunction(Exception);
        }
        catch (Exception ex)
        {
            return new Failure<T>(ex, "An exception occurred during recovery.");
        }
    }

    /// <inheritdoc />
    public override Try<T> RecoverWith(Func<Exception, string, Try<T>> recoverFunction)
    {
        try
        {
            return recoverFunction(Exception?.InnerException, Exception?.Cause);
        }
        catch (Exception ex)
        {
            return new Failure<T>(ex, "An exception occurred during recovery.");
        }
    }

    /// <inheritdoc />
    public override TResult Match<TResult>(Func<T, TResult> whenValue, Func<TryCreatingValueException, TResult> whenException)
    {
        if (whenException is null)
            throw new ArgumentNullException(nameof(whenException));
        return whenException(Exception);
    }

    /// <inheritdoc />
    public override TResult Match<TResult>(Func<T, TResult> whenValue, Func<Exception, string, TResult> whenException)
    {
        if (whenException is null)
            throw new ArgumentNullException(nameof(whenException));
        return whenException(Exception?.InnerException, Exception?.Cause);
    }

    /// <inheritdoc />
    public override Try<T> Tap(Action<T> successAction = null, Action<TryCreatingValueException> failureAction = null)
    {
        failureAction?.Invoke(Exception);
        return this;
    }

    /// <inheritdoc />
    public override Try<T> Tap(Action<T> successAction = null, Action<Exception, string> failureAction = null)
    {
        failureAction?.Invoke(Exception.InnerException, Exception.Cause);
        return this;
    }

    /// <summary>
    /// 绑定
    /// </summary>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="bind">绑定函数</param>
    internal override Try<TResult> Bind<TResult>(Func<T, Try<TResult>> bind) => Try.LiftException<TResult>(Exception);

    /// <summary>
    /// 重抛异常
    /// </summary>
    private Exception Rethrow() => ExceptionDispatchInfo.Capture(Exception!).SourceException;
}