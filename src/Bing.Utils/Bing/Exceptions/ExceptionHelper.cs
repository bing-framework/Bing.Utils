using System.Runtime.ExceptionServices;

namespace Bing.Exceptions;

/// <summary>
/// 异常操作辅助类
/// </summary>
public static class ExceptionHelper
{
    /// <summary>
    /// 捕抓异常并重新抛出
    /// </summary>
    /// <param name="exception">异常</param>
    public static Exception PrepareForRethrow(Exception exception)
    {
        ExceptionDispatchInfo.Capture(exception).Throw();
        // The code cannot ever get here. We just return a value to work around a badly-designed API (ExceptionDispatchInfo.Throw):
        //  https://connect.microsoft.com/VisualStudio/feedback/details/689516/exceptiondispatchinfo-api-modifications (http://www.webcitation.org/6XQ7RoJmO)
        return exception;
    }

    /// <summary>
    /// 获取异常详情
    /// </summary>
    /// <param name="exception">异常</param>
    /// <returns>格式化之后的异常信息</returns>
    public static string GetExceptionDetail(Exception exception)
    {
        var detail = new StringBuilder();
        detail.Append(@"***************************************");
        detail.AppendFormat(@" 异常发生时间： {0} ", DateTime.Now);
        detail.AppendFormat(@" 异常类型： {0} ", exception.HResult);
        detail.AppendFormat(@" 导致当前异常的 Exception 实例： {0} ", exception.InnerException);
        detail.AppendFormat(@" 导致异常的应用程序或对象的名称： {0} ", exception.Source);
        detail.AppendFormat(@" 引发异常的方法： {0} ", exception.TargetSite);
        detail.AppendFormat(@" 异常堆栈信息： {0} ", exception.StackTrace);
        detail.AppendFormat(@" 异常消息： {0} ", exception.Message);
        detail.Append(@"***************************************");
        return detail.ToString();
    }

    /// <summary>
    /// 解包异常，获取最内层的异常
    /// </summary>
    /// <param name="exception">需要解包的异常</param>
    /// <returns>返回最内层的异常。如果输入的异常本身就没有内部异常，那么返回输入的异常。</returns>
    /// <exception cref="ArgumentNullException">当输入的异常为 null 时抛出此异常</exception>
    public static Exception Unwrap(Exception exception)
    {
        if (exception is null)
            throw new ArgumentNullException(nameof(exception));   
        while (exception.InnerException != null)
            exception = exception.InnerException;
        return exception;
    }

    /// <summary>
    /// 解包异常，获取指定类型的异常
    /// </summary>
    /// <param name="exception">需要解包的异常</param>
    /// <param name="targetType">需要获取的异常类型</param>
    /// <param name="mayDerivedClass">是否可以是指定异常类型的派生类，默认为 true</param>
    /// <returns>返回指定类型的异常。如果没有找到指定类型的异常，返回 null。</returns>
    /// <exception cref="ArgumentNullException">当输入的异常或指定的类型为 null 时抛出此异常</exception>
    /// <exception cref="ArgumentException">当指定的类型不是 Exception 类型或其派生类时抛出此异常</exception>
    public static Exception Unwrap(Exception exception, Type targetType, bool mayDerivedClass = true)
    {
        if (exception is null)
            throw new ArgumentNullException(nameof(exception));
        if (targetType is null)
            throw new ArgumentNullException(nameof(targetType));
        if (!targetType.IsSubclassOf(typeof(Exception)))
            throw new ArgumentException($"Type '{targetType}' does not divided from {typeof(Exception)}", nameof(targetType));

        return exception.GetType() == targetType || mayDerivedClass && exception.GetType().IsSubclassOf(targetType)
            ? exception
            : exception.InnerException is null
                ? null
                : Unwrap(exception.InnerException, targetType, mayDerivedClass);
    }

    /// <summary>
    /// 解包异常，获取指定类型的异常
    /// </summary>
    /// <typeparam name="TException">需要获取的异常类型</typeparam>
    /// <param name="exception">需要解包的异常</param>
    /// <returns>返回指定类型的异常。如果没有找到指定类型的异常，返回 null。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TException Unwrap<TException>(this Exception exception) where TException : Exception => exception.Unwrap(Reflection.Types.Of<TException>()) as TException;
}

/// <summary>
/// Bing <see cref="Exception"/> 扩展
/// </summary>
public static class ExceptionHelperExtensions
{
    /// <summary>
    /// 解包异常，获取最内层的异常
    /// </summary>
    /// <param name="exception">需要解包的异常</param>
    /// <returns>返回最内层的异常。如果输入的异常本身就没有内部异常，那么返回输入的异常。</returns>
    /// <exception cref="ArgumentNullException">当输入的异常为 null 时抛出此异常</exception>
    public static Exception Unwrap(this Exception exception) => ExceptionHelper.Unwrap(exception);

    /// <summary>
    /// 解包异常，获取指定类型的异常
    /// </summary>
    /// <param name="exception">需要解包的异常</param>
    /// <param name="targetType">需要获取的异常类型</param>
    /// <param name="mayDerivedClass">是否可以是指定异常类型的派生类，默认为 true</param>
    /// <returns>返回指定类型的异常。如果没有找到指定类型的异常，返回 null。</returns>
    /// <exception cref="ArgumentNullException">当输入的异常或指定的类型为 null 时抛出此异常</exception>
    /// <exception cref="ArgumentException">当指定的类型不是 Exception 类型或其派生类时抛出此异常</exception>
    public static Exception Unwrap(this Exception exception, Type targetType, bool mayDerivedClass = true) => ExceptionHelper.Unwrap(exception, targetType, mayDerivedClass);

    /// <summary>
    /// 解包异常，获取指定类型的异常
    /// </summary>
    /// <typeparam name="TException">需要获取的异常类型</typeparam>
    /// <param name="exception">需要解包的异常</param>
    /// <returns>返回指定类型的异常。如果没有找到指定类型的异常，返回 null。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TException Unwrap<TException>(this Exception exception) where TException : Exception => ExceptionHelper.Unwrap<TException>(exception);

    /// <summary>
    /// 解包并返回消息
    /// </summary>
    /// <param name="ex">异常</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToUnwrappedString(this Exception ex) => ex.Unwrap().Message;
}