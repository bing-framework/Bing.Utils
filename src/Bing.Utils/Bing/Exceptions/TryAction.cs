namespace Bing.Exceptions;

/// <summary>
/// 尝试操作
/// </summary>
public abstract class TryAction
{
    /// <summary>
    /// 获取是否失败
    /// </summary>
    public abstract bool IsFailure { get; }

    /// <summary>
    /// 获取是否成功
    /// </summary>
    public abstract bool IsSuccess { get; }

    /// <summary>
    /// 获取异常
    /// </summary>
    public abstract TryInvokingException Exception { get; }

    /// <summary>
    /// 获取发生异常的原因
    /// </summary>
    public abstract string Cause { get; }

    /// <summary>
    /// ==
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static bool operator ==(TryAction left, TryAction right)
    {
        if (left is null && right is null)
            return true;
        if (left is null || right is null)
            return false;
        return left.Equals(right);
    }

    /// <summary>
    /// !=
    /// </summary>
    /// <param name="left">左对象</param>
    /// <param name="right">右对象</param>
    public static bool operator !=(TryAction left, TryAction right) => !(left == right);

    /// <inheritdoc />
    public abstract override string ToString();

    /// <inheritdoc />
    public abstract override bool Equals(object obj);

    /// <inheritdoc />
    public abstract override int GetHashCode();

    /// <summary>
    /// 解构
    /// </summary>
    /// <param name="tryResult">尝试结果</param>
    /// <param name="exception">异常</param>
    public abstract void Deconstruct(out bool tryResult, out TryInvokingException exception);

    /// <summary>
    /// 将异常转换为指定类型异常
    /// </summary>
    /// <typeparam name="TException">异常类型</typeparam>
    /// <returns>返回异常链中指定类型的异常；不存在时返回 <see langword="null"/>。</returns>
    public TException ExceptionAs<TException>() where TException : Exception => Exception as TException;

    /// <summary>
    /// 恢复
    /// </summary>
    /// <param name="recoverFunction">恢复函数</param>
    /// <returns>返回恢复后的尝试结果。</returns>
    public abstract TryAction Recover(Action<Exception> recoverFunction);

    /// <summary>
    /// 恢复
    /// </summary>
    /// <param name="recoverFunction">恢复函数</param>
    /// <returns>返回恢复后的尝试结果。</returns>
    public abstract TryAction RecoverWith(Func<Exception, TryAction> recoverFunction);

    /// <summary>
    /// 触发
    /// </summary>
    /// <param name="successAction">成功操作</param>
    /// <param name="failureAction">失败操作</param>
    /// <returns>返回当前尝试结果。</returns>
    public abstract TryAction Tap(Action successAction = null, Action<Exception> failureAction = null);
}
