namespace Bing.Exceptions;

/// <summary>
/// 尝试成功
/// </summary>
/// <typeparam name="T">类型</typeparam>
public class Success<T> : Try<T>
{
    /// <summary>
    /// 初始化一个<see cref="Success{T}"/>类型的实例
    /// </summary>
    /// <param name="value">值</param>
    internal Success(T value) => Value = value;

    /// <inheritdoc />
    public override bool IsFailure => false;

    /// <inheritdoc />
    public override bool IsSuccess => true;

    /// <inheritdoc />
    public override TryCreatingValueException Exception => default!;

    /// <inheritdoc />
    public override T Value { get; }

    /// <inheritdoc />
    public override string ToString() => $"Success<{Value}>";

    /// <summary>
    /// 相等
    /// </summary>
    /// <param name="other">对象</param>
    public bool Equals(Success<T> other) => !(other is null) && EqualityComparer<T>.Default.Equals(Value, other.Value);

    /// <inheritdoc />
    public override bool Equals(object obj) => obj is Success<T> success && Equals(success);

    /// <inheritdoc />
    public override int GetHashCode() => Value is null ? 0 : EqualityComparer<T>.Default.GetHashCode(Value);

    /// <inheritdoc />
    public override void Deconstruct(out T value, out Exception exception)
    {
        value = Value;
        exception = default!;
    }

    /// <inheritdoc />
    public override void Deconstruct(out T value, out bool tryResult, out Exception exception)
    {
        value = Value;
        tryResult = IsSuccess;
        exception = default!;
    }

    /// <inheritdoc />
    public override Try<T> Recover(Func<TryCreatingValueException, T> recoverFunction) => this;

    /// <inheritdoc />
    public override Try<T> Recover(Func<Exception, string, T> recoverFunction) => this;

    /// <inheritdoc />
    public override Try<T> RecoverWith(Func<TryCreatingValueException, Try<T>> recoverFunction) => this;

    /// <inheritdoc />
    public override Try<T> RecoverWith(Func<Exception, string, Try<T>> recoverFunction) => this;

    /// <inheritdoc />
    public override TResult Match<TResult>(Func<T, TResult> whenValue, Func<TryCreatingValueException, TResult> whenException)
    {
        if (whenValue is null)
            throw new ArgumentNullException(nameof(whenValue));
        return whenValue(Value);
    }

    /// <inheritdoc />
    public override TResult Match<TResult>(Func<T, TResult> whenValue, Func<Exception, string, TResult> whenException)
    {
        if (whenValue is null)
            throw new ArgumentNullException(nameof(whenValue));
        return whenValue(Value);
    }

    /// <inheritdoc />
    public override Try<T> Tap(Action<T> successAction = null, Action<TryCreatingValueException> failureAction = null)
    {
        successAction?.Invoke(Value);
        return this;
    }

    /// <inheritdoc />
    public override Try<T> Tap(Action<T> successAction = null, Action<Exception, string> failureAction = null)
    {
        successAction?.Invoke(Value);
        return this;
    }

    /// <summary>
    /// 绑定
    /// </summary>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="bind">绑定函数</param>
    internal override Try<TResult> Bind<TResult>(Func<T, Try<TResult>> bind)
    {
        if (bind is null)
            throw new ArgumentNullException(nameof(bind));
        return bind(Value);
    }
}