namespace Bing.Exceptions;

/// <summary>
/// 成功操作
/// </summary>
public class SuccessAction : TryAction
{
    /// <summary>
    /// 操作哈希值
    /// </summary>
    private readonly int _hashOfAction;

    /// <summary>
    /// 初始化一个<see cref="SuccessAction"/>类型的实例
    /// </summary>
    /// <param name="hashOfAction">操作哈希值</param>
    internal SuccessAction(int hashOfAction) => _hashOfAction = hashOfAction;

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
    /// 相等
    /// </summary>
    /// <param name="other">对象</param>
    public bool Equals(SuccessAction other) => other is not null && EqualityComparer<int>.Default.Equals(_hashOfAction, other._hashOfAction);

    /// <inheritdoc />
    public override bool Equals(object obj) => obj is SuccessAction success && Equals(success);

    /// <inheritdoc />
    public override int GetHashCode() => EqualityComparer<int>.Default.GetHashCode(_hashOfAction);

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