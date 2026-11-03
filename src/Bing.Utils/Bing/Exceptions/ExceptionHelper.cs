using System.Runtime.ExceptionServices;

namespace Bing.Exceptions;

/// <summary>
/// 提供异常重抛、诊断格式化与异常链查找。
/// </summary>
public static class ExceptionHelper
{
    /// <summary>
    /// 保留异常的原始堆栈并立即重新抛出。
    /// </summary>
    /// <param name="exception">需要重新抛出的非空异常。</param>
    /// <returns>此方法始终抛出异常，不会正常返回。</returns>
    public static Exception PrepareForRethrow(Exception exception)
    {
        ExceptionDispatchInfo.Capture(exception).Throw();
        return exception;
    }

    /// <summary>
    /// 使用默认资源预算获取异常诊断详情。
    /// </summary>
    /// <param name="exception">需要格式化的非空异常。</param>
    /// <returns>包含记录时间与诊断字段的有限长度文本。</returns>
    public static string GetExceptionDetail(Exception exception) => GetExceptionDetail(exception, new ExceptionFormatOptions());

    /// <summary>
    /// 根据资源预算获取异常诊断详情。
    /// </summary>
    /// <param name="exception">需要格式化的非空异常。</param>
    /// <param name="options">本次调用的有限格式化配置，调用开始时读取快照。</param>
    /// <returns>长度不超过配置上限的诊断文本，必要时包含截断标记。</returns>
    /// <exception cref="ArgumentNullException">异常或配置为 null。</exception>
    public static string GetExceptionDetail(Exception exception, ExceptionFormatOptions options)
    {
        if (exception is null)
            throw new ArgumentNullException(nameof(exception));
        return ExceptionFormatter.Format(exception, options, true);
    }

    /// <summary>
    /// 沿单条内部异常链获取最内层异常。
    /// </summary>
    /// <param name="exception">需要解包的非空异常。</param>
    /// <returns>单链最内层的原异常实例。</returns>
    /// <exception cref="ArgumentNullException">异常为 null。</exception>
    public static Exception Unwrap(Exception exception)
    {
        if (exception is null)
            throw new ArgumentNullException(nameof(exception));
        while (exception.InnerException != null)
            exception = exception.InnerException;
        return exception;
    }

    /// <summary>
    /// 从外到内沿单链查找指定类型的异常。
    /// </summary>
    /// <param name="exception">需要查找的非空异常。</param>
    /// <param name="targetType">Exception 本身或其派生类型。</param>
    /// <param name="mayDerivedClass">是否允许目标类型的派生类型。</param>
    /// <returns>第一个匹配的原异常实例，未命中时为 null。</returns>
    public static Exception Unwrap(Exception exception, Type targetType, bool mayDerivedClass = true) =>
        Unwrap(exception, targetType, mayDerivedClass, false);

    /// <summary>
    /// 在异常链或聚合异常分支中查找指定类型。
    /// </summary>
    /// <param name="exception">需要查找的非空异常。</param>
    /// <param name="targetType">Exception 本身或其派生类型。</param>
    /// <param name="mayDerivedClass">是否允许目标类型的派生类型。</param>
    /// <param name="includeAggregateExceptions">是否根优先、按分支顺序深度优先遍历聚合异常。</param>
    /// <returns>第一个匹配的原异常实例，未命中时为 null。</returns>
    /// <exception cref="ArgumentNullException">异常或目标类型为 null。</exception>
    /// <exception cref="ArgumentException">目标类型不属于异常类型。</exception>
    public static Exception Unwrap(Exception exception, Type targetType, bool mayDerivedClass, bool includeAggregateExceptions)
    {
        if (exception is null)
            throw new ArgumentNullException(nameof(exception));
        if (targetType is null)
            throw new ArgumentNullException(nameof(targetType));
        if (!typeof(Exception).IsAssignableFrom(targetType))
            throw new ArgumentException($"类型 '{targetType}' 必须为 Exception 或其派生类型。", nameof(targetType));
        if (!includeAggregateExceptions)
        {
            for (var current = exception; current != null; current = current.InnerException)
            {
                var actualType = current.GetType();
                if (actualType == targetType || mayDerivedClass && targetType.IsAssignableFrom(actualType))
                    return current;
            }
            return null;
        }
        foreach (var node in ExceptionTraversal.Enumerate(exception, true))
        {
            if (node.IsRepeated)
                continue;
            var actualType = node.Exception.GetType();
            if (actualType == targetType || mayDerivedClass && targetType.IsAssignableFrom(actualType))
                return node.Exception;
        }
        return null;
    }

    /// <summary>
    /// 沿单链查找指定类型或其派生类型的异常。
    /// </summary>
    /// <typeparam name="TException">目标异常类型。</typeparam>
    /// <param name="exception">需要查找的非空异常。</param>
    /// <returns>第一个匹配的原异常实例，未命中时为 null。</returns>
    public static TException Unwrap<TException>(Exception exception) where TException : Exception =>
        (TException)Unwrap(exception, typeof(TException));

    /// <summary>
    /// 在异常链或聚合分支中查找指定类型的异常。
    /// </summary>
    /// <typeparam name="TException">目标异常类型，允许其派生类型。</typeparam>
    /// <param name="exception">需要查找的非空异常。</param>
    /// <param name="includeAggregateExceptions">是否展开聚合异常的全部分支。</param>
    /// <returns>第一个匹配的原异常实例，未命中时为 null。</returns>
    public static TException Unwrap<TException>(Exception exception, bool includeAggregateExceptions) where TException : Exception =>
        (TException)Unwrap(exception, typeof(TException), true, includeAggregateExceptions);
}

/// <summary>
/// 提供异常查找与最内层消息的扩展入口。
/// </summary>
public static class ExceptionHelperExtensions
{
    /// <inheritdoc cref="ExceptionHelper.Unwrap(Exception)"/>
    public static Exception Unwrap(this Exception exception) => ExceptionHelper.Unwrap(exception);

    /// <inheritdoc cref="ExceptionHelper.Unwrap(Exception, Type, bool)"/>
    public static Exception Unwrap(this Exception exception, Type targetType, bool mayDerivedClass = true) =>
        ExceptionHelper.Unwrap(exception, targetType, mayDerivedClass);

    /// <inheritdoc cref="ExceptionHelper.Unwrap(Exception, Type, bool, bool)"/>
    public static Exception Unwrap(this Exception exception, Type targetType, bool mayDerivedClass, bool includeAggregateExceptions) =>
        ExceptionHelper.Unwrap(exception, targetType, mayDerivedClass, includeAggregateExceptions);

    /// <inheritdoc cref="ExceptionHelper.Unwrap{TException}(Exception)"/>
    public static TException Unwrap<TException>(this Exception exception) where TException : Exception =>
        ExceptionHelper.Unwrap<TException>(exception);

    /// <inheritdoc cref="ExceptionHelper.Unwrap{TException}(Exception, bool)"/>
    public static TException Unwrap<TException>(this Exception exception, bool includeAggregateExceptions) where TException : Exception =>
        ExceptionHelper.Unwrap<TException>(exception, includeAggregateExceptions);

    /// <summary>
    /// 获取单链最内层异常的消息。
    /// </summary>
    /// <param name="ex">需要解包的非空异常。</param>
    /// <returns>最内层异常的原始消息。</returns>
    public static string ToUnwrappedString(this Exception ex) => ExceptionHelper.Unwrap(ex).Message;
}
