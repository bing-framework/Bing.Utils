namespace Bing.Conversions.Internals;

/// <summary>
/// 定义保留源类型和目标类型的转换条目。
/// </summary>
/// <typeparam name="TSource">源类型。</typeparam>
/// <typeparam name="TTarget">目标类型。</typeparam>
internal interface IConverterEntry<TSource, TTarget> : IConverterEntry
{
    /// <summary>
    /// 尝试转换输入值。
    /// </summary>
    /// <param name="input">待转换的输入值。</param>
    /// <param name="result">转换成功时的结果；失败时为默认值。</param>
    /// <returns>转换成功返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    bool TryConvert(TSource input, out TTarget result);
}
