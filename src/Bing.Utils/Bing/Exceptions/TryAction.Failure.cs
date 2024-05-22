namespace Bing.Exceptions;

/// <summary>
/// 失败操作
/// </summary>
public class FailureAction : TryAction
{
    /// <summary>
    /// 初始化一个<see cref="FailureAction"/>类型的实例
    /// </summary>
    /// <param name="exception">异常</param>
    /// <param name="hashOfAction">操作哈希值</param>
    /// <param name="cause">发生异常的原因</param>
    internal FailureAction(Exception exception, int hashOfAction, string cause)
    {
        Exception = new TryInvokingException(exception, cause);
        _ = hashOfAction;
    }

    /// <inheritdoc />
    public override bool IsFailure => true;

    /// <inheritdoc />
    public override bool IsSuccess => false;

    /// <inheritdoc />
    public override TryInvokingException Exception { get; }

    /// <inheritdoc />
    public override string Cause => Exception!.Cause;

    /// <inheritdoc />
    public override string ToString() => $"FailureAction<{Exception}>";

    /// <summary>
    /// 相等
    /// </summary>
    /// <param name="other">对象</param>
    public bool Equals(FailureAction other) => !(other is null) && ReferenceEquals(Exception, other.Exception);

    /// <inheritdoc />
    public override bool Equals(object obj) => obj is FailureAction failure && Equals(failure);

    /// <inheritdoc />
    public override int GetHashCode() => Exception!.GetHashCode();

    /// <inheritdoc />
    public override void Deconstruct(out bool tryResult, out TryInvokingException exception)
    {
        tryResult = IsSuccess;
        exception = Exception!;
    }

    /// <inheritdoc />
    public override TryAction Recover(Action<Exception> recoverFunction) => RecoverWith(ex => Try.Invoke(() => recoverFunction(ex)));

    /// <inheritdoc />
    public override TryAction RecoverWith(Func<Exception, TryAction> recoverFunction)
    {
        try
        {
            return recoverFunction(Exception);
        }
        catch (Exception e)
        {
            return new FailureAction(e, recoverFunction.GetHashCode(), "An exception occurred during recovery.");
        }
    }

    /// <inheritdoc />
    public override TryAction Tap(Action successAction = null, Action<Exception> failureAction = null)
    {
        failureAction?.Invoke(Exception);
        return this;
    }
}