namespace Bing.Exceptions;

/// <summary>
/// 尝试
/// </summary>
public static partial class Try
{
    /// <summary>
    /// 提取值
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="value">值</param>
    public static Try<T> LiftValue<T>(T value) => new Success<T>(value);

    /// <summary>
    /// 提取异常
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="exception">异常</param>
    /// <param name="cause">导致错误的原因</param>
    public static Try<T> LiftException<T>(Exception exception, string cause = null) => new Failure<T>(exception, cause);
}