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
}
