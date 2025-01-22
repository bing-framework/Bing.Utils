namespace Bing.Reflection;

/// <summary>
/// 调用帮助类
/// </summary>
public static class InvokeHelper
{
    /// <summary>
    /// 静态构造函数
    /// </summary>
    static InvokeHelper()
    {
        OnInvokeException = ex => Console.WriteLine($@"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}]{typeof(InvokeHelper)}:{ex}");
    }

    /// <summary>
    /// 调用函数异常处理
    /// </summary>
    public static Action<Exception> OnInvokeException { get; set; }
}