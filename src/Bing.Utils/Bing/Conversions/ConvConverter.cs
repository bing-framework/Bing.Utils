using Bing.Conversions.Internals;
using Bing.Helpers;

namespace Bing.Conversions;

/// <summary>
/// 持有调用方注册的类型转换函数。
/// </summary>
/// <remarks>
/// 转换器可并发读取；注册函数及其捕获对象随实例引用一同存活。
/// </remarks>
public sealed class ConvConverter
{
    /// <summary>
    /// 保存构建时复制的注册条目。
    /// </summary>
    private readonly Dictionary<(Type Source, Type Target), IConverterEntry> _entries;

    /// <summary>
    /// 初始化转换器。
    /// </summary>
    /// <param name="entries">已复制的转换条目。</param>
    internal ConvConverter(Dictionary<(Type Source, Type Target), IConverterEntry> entries) => _entries = entries;

    /// <summary>
    /// 将输入转换为指定类型。
    /// </summary>
    /// <typeparam name="T">目标类型。</typeparam>
    /// <param name="input">待转换的输入值。</param>
    /// <returns>转换结果；失败时返回目标类型的默认值。</returns>
    public T To<T>(object input) => Conv.To<T>(input, this);

    /// <summary>
    /// 尝试将输入转换为指定类型。
    /// </summary>
    /// <typeparam name="T">目标类型。</typeparam>
    /// <param name="input">待转换的输入值。</param>
    /// <param name="result">转换成功时的结果；失败时为默认值。</param>
    /// <returns>转换成功返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    public bool TryTo<T>(object input, out T result) => Conv.TryTo<T>(input, this, out result);

    /// <summary>
    /// 将输入转换为指定类型。
    /// </summary>
    /// <typeparam name="TSource">源类型。</typeparam>
    /// <typeparam name="TTarget">目标类型。</typeparam>
    /// <param name="input">待转换的输入值。</param>
    /// <returns>转换结果；失败时返回目标类型的默认值。</returns>
    public TTarget To<TSource, TTarget>(TSource input) => Conv.To<TSource, TTarget>(input, this);

    /// <summary>
    /// 尝试将输入转换为指定类型。
    /// </summary>
    /// <typeparam name="TSource">源类型。</typeparam>
    /// <typeparam name="TTarget">目标类型。</typeparam>
    /// <param name="input">待转换的输入值。</param>
    /// <param name="result">转换成功时的结果；失败时为默认值。</param>
    /// <returns>转换成功返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    /// <remarks>
    /// 空输入和 <see cref="DBNull"/> 视为失败；成功得到目标类型默认值时仍返回 <see langword="true"/>。
    /// </remarks>
    public bool TryTo<TSource, TTarget>(TSource input, out TTarget result) =>
        Conv.TryTo<TSource, TTarget>(input, this, out result);

    /// <summary>
    /// 尝试执行精确匹配的注册转换。
    /// </summary>
    /// <typeparam name="T">声明的目标类型。</typeparam>
    /// <param name="input">非空的输入值。</param>
    /// <param name="result">转换成功时的结果；失败时为默认值。</param>
    /// <param name="matched">是否找到与运行时源类型和目标类型完全匹配的注册项。</param>
    /// <returns>注册转换成功返回 <see langword="true"/>；未匹配或转换失败返回 <see langword="false"/>。</returns>
    /// <remarks>
    /// 普通委托异常视为失败；取消和内存不足异常继续传播。
    /// </remarks>
    internal bool TryConvert<T>(object input, out T result, out bool matched)
    {
        matched = _entries.TryGetValue((input.GetType(), typeof(T)), out var entry);
        result = default;
        if (!matched)
            return false;
        try
        {
            if (!((IConverterEntry<T>)entry).TryConvert(input, out var value))
                return false;
            result = value;
            return true;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException and not OperationCanceledException)
        {
            return false;
        }
    }

    /// <summary>
    /// 尝试执行保留源类型的精确注册转换。
    /// </summary>
    /// <typeparam name="TSource">源类型。</typeparam>
    /// <typeparam name="TTarget">目标类型。</typeparam>
    /// <param name="input">非空的输入值。</param>
    /// <param name="result">转换成功时的结果；失败时为默认值。</param>
    /// <param name="matched">是否找到精确注册项。</param>
    /// <returns>注册转换成功返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    /// <remarks>
    /// 普通委托异常视为失败；取消和内存不足异常继续传播。
    /// </remarks>
    internal bool TryConvert<TSource, TTarget>(TSource input, out TTarget result, out bool matched)
    {
        matched = _entries.TryGetValue((typeof(TSource), typeof(TTarget)), out var entry);
        result = default;
        if (!matched)
            return false;
        try
        {
            if (!((IConverterEntry<TSource, TTarget>)entry).TryConvert(input, out var value))
                return false;
            result = value;
            return true;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException and not OperationCanceledException)
        {
            return false;
        }
    }
}
