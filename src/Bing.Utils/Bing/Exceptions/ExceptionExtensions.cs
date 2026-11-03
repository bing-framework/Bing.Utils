using System.Runtime.ExceptionServices;

namespace Bing.Exceptions;

/// <summary>
/// 异常(<see cref="Exception"/>) 扩展
/// </summary>
public static class ExceptionExtensions
{
    #region FormatMessage(格式化异常消息)

    /// <summary>
    /// 使用默认资源预算格式化异常消息。
    /// </summary>
    /// <param name="e">异常对象</param>
    /// <param name="isHideStackTrace">是否隐藏异常堆栈信息</param>
    /// <returns>长度不超过 32768 个 UTF-16 字符的诊断文本；超限时包含截断标记。</returns>
    public static string FormatMessage(this Exception e, bool isHideStackTrace = false) =>
        ExceptionFormatter.Format(e, new ExceptionFormatOptions { HideStackTrace = isHideStackTrace }, false);

    /// <summary>
    /// 根据有限资源预算格式化异常消息。
    /// </summary>
    /// <param name="e">异常；为 null 时返回空字符串。</param>
    /// <param name="options">本次格式化配置；隐藏堆栈不等于消息脱敏。</param>
    /// <returns>不超过输出预算的诊断文本，必要时包含截断标记。</returns>
    /// <exception cref="ArgumentNullException">配置为 null。</exception>
    public static string FormatMessage(this Exception e, ExceptionFormatOptions options) =>
        ExceptionFormatter.Format(e, options, false);

    #endregion

    #region ReThrow(将异常重新抛出)

    /// <summary>
    /// 将异常重新抛出，同时保留堆栈跟踪。使用 <see cref="ExceptionDispatchInfo.Capture"/> 方法重新抛出异常
    /// </summary>
    /// <param name="exception">异常</param>
    public static void ReThrow(this Exception exception) => ExceptionDispatchInfo.Capture(exception).Throw();

    #endregion

    #region ThrowIf(条件抛出异常)

    /// <summary>
    /// 如果条件成立，则抛出异常
    /// </summary>
    /// <param name="exception">异常</param>
    /// <param name="isThrow">条件</param>
    public static void ThrowIf(this Exception exception, bool isThrow)
    {
        if (isThrow)
            throw exception;
    }

    /// <summary>
    /// 如果条件成立，则抛出异常
    /// </summary>
    /// <param name="exception">异常</param>
    /// <param name="isThrowFunc">条件函数</param>
    public static void ThrowIf(this Exception exception, Func<bool> isThrowFunc)
    {
        if (isThrowFunc())
            throw exception;
    }

    #endregion
}
