using System;
using System.Runtime.ExceptionServices;
using System.Text;

namespace Bing.Exceptions
{
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
    }

    /// <summary>
    /// Bing <see cref="Exception"/> 扩展
    /// </summary>
    public static class ExceptionHelperExtensions
    {
        /// <summary>
        /// 解包
        /// </summary>
        /// <param name="ex">异常</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static Exception Unwrap(this Exception ex)
        {
            if (ex is null)
                throw new ArgumentNullException(nameof(ex));
            while (ex.InnerException != null)
                ex = ex.InnerException;
            return ex;
        }

        /// <summary>
        /// 解包
        /// </summary>
        /// <param name="ex">异常</param>
        /// <param name="untilType">截止类型</param>
        /// <param name="mayDerivedClass">是否允许派生类</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static Exception Unwrap(this Exception ex, Type untilType, bool mayDerivedClass = true)
        {
            if (ex is null)
                throw new ArgumentNullException(nameof(ex));
            if (untilType is null)
                throw new ArgumentNullException(nameof(untilType));
            if (!untilType.IsSubclassOf(typeof(Exception)))
                throw new ArgumentException($"Type '{untilType}' does not devide from {typeof(Exception)}", nameof(untilType));

            var exception = ex;
            return exception.GetType() == untilType || mayDerivedClass && exception.GetType().IsSubclassOf(untilType)
                ? exception
                : exception.InnerException is null
                    ? null
                    : Unwrap(exception.InnerException, untilType, mayDerivedClass);
        }

        /// <summary>
        /// 解包
        /// </summary>
        /// <typeparam name="TException">异常类型</typeparam>
        /// <param name="ex">异常</param>
        public static Exception Unwrap<TException>(this Exception ex) where TException : Exception => ex.Unwrap(Reflection.Types.Of<TException>());

        /// <summary>
        /// 解包并返回消息
        /// </summary>
        /// <param name="ex">异常</param>
        public static string ToUnwrappedString(this Exception ex) => ex.Unwrap().Message;
    }
}
