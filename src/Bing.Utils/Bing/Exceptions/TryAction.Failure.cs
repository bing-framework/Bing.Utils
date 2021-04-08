using System;

namespace Bing.Exceptions
{
    /// <summary>
    /// 失败操作
    /// </summary>
    public class FailureAction : TryAction
    {
        /// <summary>
        /// 操作哈希值
        /// </summary>
        private readonly int _hashOfAction;

        /// <summary>
        /// 是否失败
        /// </summary>
        public override bool IsFailure => true;

        /// <summary>
        /// 是否成功
        /// </summary>
        public override bool IsSuccess => false;

        /// <summary>
        /// 异常
        /// </summary>
        public override Exception Exception { get; }

        /// <summary>
        /// 初始化一个<see cref="FailureAction"/>类型的实例
        /// </summary>
        /// <param name="exception">异常</param>
        /// <param name="hashOfAction">操作哈希值</param>
        internal FailureAction(Exception exception, int hashOfAction)
        {
            Exception = exception ?? new ArgumentNullException(nameof(exception));
            _hashOfAction = hashOfAction;
        }

        /// <summary>
        /// 重载输出字符串
        /// </summary>
        public override string ToString() => $"FailureAction<{Exception}>";

        /// <summary>
        /// 相等
        /// </summary>
        /// <param name="other">对象</param>
        public bool Equals(FailureAction other) => !(other is null) && ReferenceEquals(Exception, other.Exception);

        /// <summary>
        /// 重载相等
        /// </summary>
        /// <param name="obj">对象</param>
        public override bool Equals(object obj) => obj is FailureAction failure && Equals(failure);

        /// <summary>
        /// 重载获取哈希码
        /// </summary>
        public override int GetHashCode() => Exception.GetHashCode();

        /// <summary>
        /// 解构
        /// </summary>
        /// <param name="tryResult">尝试结果</param>
        /// <param name="exception">异常</param>
        public override void Deconstruct(out bool tryResult, out Exception exception)
        {
            tryResult = IsSuccess;
            exception = Exception;
        }

        /// <summary>
        /// 恢复
        /// </summary>
        /// <param name="recoverFunc">恢复函数</param>
        public override TryAction Recover(Action<Exception> recoverFunc) => RecoverWith(ex => Try.Invoke(() => recoverFunc(ex)));

        /// <summary>
        /// 恢复
        /// </summary>
        /// <param name="recoverFunc">恢复函数</param>
        public override TryAction RecoverWith(Func<Exception, TryAction> recoverFunc)
        {
            try
            {
                return recoverFunc(Exception);
            }
            catch (Exception e)
            {
                return new FailureAction(e, recoverFunc?.GetHashCode() ?? 0);
            }
        }

        /// <summary>
        /// 触发
        /// </summary>
        /// <param name="successAction">成功操作</param>
        /// <param name="failureAction">失败操作</param>
        public override TryAction Tap(Action successAction = null, Action<Exception> failureAction = null)
        {
            failureAction?.Invoke(Exception);
            return this;
        }
    }
}
