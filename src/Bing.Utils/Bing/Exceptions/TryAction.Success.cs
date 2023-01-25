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
    /// 是否失败
    /// </summary>
    public override bool IsFailure => false;

    /// <summary>
    /// 是否成功
    /// </summary>
    public override bool IsSuccess => true;

    /// <summary>
    /// 异常
    /// </summary>
    public override Exception Exception => null;

    /// <summary>
    /// 初始化一个<see cref="SuccessAction"/>类型的实例
    /// </summary>
    /// <param name="hashOfAction">操作哈希值</param>
    internal SuccessAction(int hashOfAction) => _hashOfAction = hashOfAction;

    /// <summary>
    /// 重载输出字符串
    /// </summary>
    public override string ToString() => "SuccessAction<Void>";

    /// <summary>
    /// 相等
    /// </summary>
    /// <param name="other">对象</param>
    public bool Equals(SuccessAction other) => !(other is null) && EqualityComparer<int>.Default.Equals(_hashOfAction, other._hashOfAction);

    /// <summary>
    /// 重载相等
    /// </summary>
    /// <param name="obj">对象</param>
    public override bool Equals(object obj) => obj is SuccessAction success && Equals(success);

    /// <summary>
    /// 重载获取哈希码
    /// </summary>
    public override int GetHashCode() => EqualityComparer<int>.Default.GetHashCode(_hashOfAction);

    /// <summary>
    /// 解构
    /// </summary>
    /// <param name="tryResult">尝试结果</param>
    /// <param name="exception">异常</param>
    public override void Deconstruct(out bool tryResult, out Exception exception)
    {
        tryResult = IsSuccess;
        exception = default;
    }

    /// <summary>
    /// 恢复
    /// </summary>
    /// <param name="recoverFunc">恢复函数</param>
    public override TryAction Recover(Action<Exception> recoverFunc) => this;

    /// <summary>
    /// 恢复
    /// </summary>
    /// <param name="recoverFunc">恢复函数</param>
    public override TryAction RecoverWith(Func<Exception, TryAction> recoverFunc) => this;

    /// <summary>
    /// 触发
    /// </summary>
    /// <param name="successAction">成功操作</param>
    /// <param name="failureAction">失败操作</param>
    public override TryAction Tap(Action successAction = null, Action<Exception> failureAction = null)
    {
        successAction?.Invoke();
        return this;
    }
}