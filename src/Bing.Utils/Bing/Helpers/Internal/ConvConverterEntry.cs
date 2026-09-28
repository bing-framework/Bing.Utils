using Bing.Helpers;

namespace Bing.Helpers.Internal;

/// <summary>
/// 包装指定源类型和目标类型的转换函数。
/// </summary>
/// <typeparam name="TSource">源类型。</typeparam>
/// <typeparam name="TTarget">目标类型。</typeparam>
internal sealed class ConvConverterEntry<TSource, TTarget> : IConvConverterEntry<TTarget>
{
    /// <summary>
    /// 保存条目生命周期内使用的转换函数。
    /// </summary>
    private readonly ConvTryConverter<TSource, TTarget> _converter;

    /// <summary>
    /// 初始化转换条目。
    /// </summary>
    /// <param name="converter">该类型对的转换函数。</param>
    internal ConvConverterEntry(ConvTryConverter<TSource, TTarget> converter) => _converter = converter;

    /// <inheritdoc />
    public bool TryConvert(object input, out TTarget result)
    {
        var success = _converter((TSource)input, out var value);
        result = success ? value : default;
        return success;
    }
}
