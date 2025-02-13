// ReSharper disable once CheckNamespace
namespace Bing.Threading;

/// <summary>
/// <see cref="AsyncLocal{T}"/> 扩展
/// </summary>
public static class AsyncLocalExtensions
{
    /// <summary>
    /// 将指定的值设置到 AsyncLocal 对象中，并返回一个 IDisposable 对象，用于在作用域结束时恢复先前的值。
    /// </summary>
    /// <typeparam name="T">AsyncLocal 对象中存储的值的类型。</typeparam>
    /// <param name="asyncLocal">要设置值的 AsyncLocal 对象。</param>
    /// <param name="value">要设置的值。</param>
    /// <returns>一个 IDisposable 对象，用于在作用域结束时恢复先前的值。</returns>
    public static IDisposable SetScoped<T>(this AsyncLocal<T> asyncLocal, T value)
    {
        var previousValue = asyncLocal.Value;
        asyncLocal.Value = value;
        return new DisposeAction<ValueTuple<AsyncLocal<T>, T>>(static (state) =>
        {
            var (asyncLocal, previousValue) = state;
            asyncLocal.Value = previousValue;
        }, (asyncLocal, previousValue));
    }
}
