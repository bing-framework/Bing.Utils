namespace Bing.Exceptions;

/// <summary>
/// 尝试组件的异常封装
/// </summary>
public sealed class TryCreatingValueException : Exception
{
    /// <summary>
    /// 默认错误消息
    /// </summary>
    private const string DEFAULT_ERROR_MSG = "An unknown exception occurred while trying to create value.";

    /// <summary>
    /// 标识
    /// </summary>
    private const string FLAG = "__TRY_CREATE_FLG";

    /// <summary>
    /// 默认错误编码
    /// </summary>
    private const long ERROR_CODE = 1052;

    /// <summary>
    /// 初始化一个<see cref="TryCreatingValueException"/>类型的实例
    /// </summary>
    /// <param name="exception">异常</param>
    /// <param name="cause">导致错误的原因</param>
    public TryCreatingValueException(Exception exception, string cause)
        : this(ERROR_CODE, exception?.Message ?? DEFAULT_ERROR_MSG, FLAG, exception)
    {
        Cause = cause ?? exception?.Message ?? DEFAULT_ERROR_MSG;
    }

    /// <summary>
    /// 初始化一个<see cref="TryCreatingValueException"/>类型的实例
    /// </summary>
    /// <param name="errorCode">错误码</param>
    /// <param name="errorMessage">错误消息</param>
    /// <param name="flag">错误标识</param>
    /// <param name="innerException">内部异常</param>
    private TryCreatingValueException(long errorCode, string errorMessage, string flag, Exception innerException)
        : base(errorMessage, innerException)
    {
        Code = errorCode.ToString();
        Flag = flag;
    }

    /// <summary>
    /// 错误码
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// 错误标识
    /// </summary>
    public string Flag { get; }

    /// <summary>
    /// 导致错误的原因
    /// </summary>
    public string Cause { get; }
}