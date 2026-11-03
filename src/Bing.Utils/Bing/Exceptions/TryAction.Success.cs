namespace Bing.Exceptions;

/// <summary>
/// 表示一次成功执行的独立操作结果。
/// </summary>
public class SuccessAction : TryAction
{
    /// <summary>
    /// 初始化一个 <see cref="SuccessAction"/> 类型的实例。
    /// </summary>
    internal SuccessAction() { }

    /// <inheritdoc />
    public override bool IsFailure => false;

    /// <inheritdoc />
    public override bool IsSuccess => true;

    /// <inheritdoc />
    public override TryInvokingException Exception => default;

    /// <inheritdoc />
    public override string Cause => string.Empty;

    /// <inheritdoc />
    public override string ToString() => "SuccessAction<Void>";

    /// <summary>
    /// 按结果实例身份判断是否相等。
    /// </summary>
    /// <param name="other">需要比较的结果，允许为 null。</param>
    /// <returns>同一结果实例为 true，其余情况为 false。</returns>
    public bool Equals(SuccessAction other) => ReferenceEquals(this, other);

    /// <inheritdoc />
    public override bool Equals(object obj) => obj is SuccessAction success && Equals(success);

    /// <inheritdoc />
    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);

    /// <inheritdoc />
    public override void Deconstruct(out bool tryResult, out TryInvokingException exception)
    {
        tryResult = IsSuccess;
        exception = default!;
    }

    /// <inheritdoc />
    public override TryAction Recover(Action<Exception> recoverFunction) => this;

    /// <inheritdoc />
    public override TryAction RecoverWith(Func<Exception, TryAction> recoverFunction) => this;

    /// <inheritdoc />
    public override TryAction Tap(Action successAction = null, Action<Exception> failureAction = null)
    {
        successAction?.Invoke();
        return this;
    }
}
