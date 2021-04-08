using System;

namespace Bing.Exceptions
{
    /// <summary>
    /// 尝试操作
    /// </summary>
    public abstract class TryAction
    {
        /// <summary>
        /// 是否失败
        /// </summary>
        public abstract bool IsFailure { get; }

        /// <summary>
        /// 是否成功
        /// </summary>
        public abstract bool IsSuccess { get; }

        /// <summary>
        /// 异常
        /// </summary>
        public abstract Exception Exception { get; }

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

        /// <summary>
        /// 重载输出字符串
        /// </summary>
        public abstract override string ToString();

        /// <summary>
        /// 重载相等
        /// </summary>
        /// <param name="obj">对象</param>
        public abstract override bool Equals(object obj);

        /// <summary>
        /// 重载获取哈希码
        /// </summary>
        public abstract override int GetHashCode();

        /// <summary>
        /// 解构
        /// </summary>
        /// <param name="tryResult">尝试结果</param>
        /// <param name="exception">异常</param>
        public abstract void Deconstruct(out bool tryResult, out Exception exception);

        /// <summary>
        /// 将异常转换为指定类型异常
        /// </summary>
        /// <typeparam name="TException">异常类型</typeparam>
        public TException ExceptionAs<TException>() where TException : Exception => Exception as TException;

        /// <summary>
        /// 恢复
        /// </summary>
        /// <param name="recoverFunc">恢复函数</param>
        public abstract TryAction Recover(Action<Exception> recoverFunc);

        /// <summary>
        /// 恢复
        /// </summary>
        /// <param name="recoverFunc">恢复函数</param>
        public abstract TryAction RecoverWith(Func<Exception, TryAction> recoverFunc);

        /// <summary>
        /// 触发
        /// </summary>
        /// <param name="successAction">成功操作</param>
        /// <param name="failureAction">失败操作</param>
        public abstract TryAction Tap(Action successAction = null, Action<Exception> failureAction = null);
    }
}
