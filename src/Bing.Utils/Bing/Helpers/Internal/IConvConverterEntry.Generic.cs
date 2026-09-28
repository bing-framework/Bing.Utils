namespace Bing.Helpers.Internal;

/// <summary>
/// 定义目标类型明确的转换条目。
/// </summary>
/// <typeparam name="TTarget">目标类型。</typeparam>
internal interface IConvConverterEntry<TTarget> : IConvConverterEntry
{
    /// <summary>
    /// 尝试转换输入值。
    /// </summary>
    /// <param name="input">待转换的输入值。</param>
    /// <param name="result">转换成功时的结果；失败时为默认值。</param>
    /// <returns>转换成功返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    bool TryConvert(object input, out TTarget result);
}
