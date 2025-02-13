// ReSharper disable once CheckNamespace
namespace Bing.Threading;

/// <summary>
/// 值任务(<see cref="ValueTask"/>) 扩展
/// </summary>
public static class ValueTaskExtensions
{
    /// <summary>
    /// 无同步上下文等待 <see cref="ValueTask"/>。
    /// </summary>
    /// <param name="valueTask">要等待的 <see cref="ValueTask"/> 实例。</param>
    /// <returns>一个配置为不捕获当前同步上下文的 <see cref="ConfiguredValueTaskAwaitable"/>。</returns>
    public static ConfiguredValueTaskAwaitable NoSync(this ValueTask valueTask) => valueTask.ConfigureAwait(false);

    /// <summary>
    /// 无同步上下文等待带返回值的 <see cref="ValueTask{T}"/>。
    /// </summary>
    /// <typeparam name="T">返回值的类型。</typeparam>
    /// <param name="valueTask">要等待的 <see cref="ValueTask{T}"/> 实例。</param>
    /// <returns>一个配置为不捕获当前同步上下文的 <see cref="ConfiguredValueTaskAwaitable{T}"/>。</returns>
    public static ConfiguredValueTaskAwaitable<T> NoSync<T>(this ValueTask<T> valueTask) => valueTask.ConfigureAwait(false);
}