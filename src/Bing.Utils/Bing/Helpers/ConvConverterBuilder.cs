using Bing.Helpers.Internal;

namespace Bing.Helpers;

/// <summary>
/// 构建按源类型和目标类型精确匹配的转换器。
/// </summary>
/// <remarks>
/// 不支持并发修改；每次构建都会复制注册表。
/// </remarks>
public sealed class ConvConverterBuilder
{
    /// <summary>
    /// 保存构建期间注册的转换条目。
    /// </summary>
    private readonly Dictionary<(Type Source, Type Target), IConvConverterEntry> _entries = new();

    /// <summary>
    /// 注册一个转换函数。
    /// </summary>
    /// <typeparam name="TSource">源类型。</typeparam>
    /// <typeparam name="TTarget">目标类型。</typeparam>
    /// <param name="converter">用于该类型对的转换函数。</param>
    /// <returns>当前构建器，可继续注册其他类型对。</returns>
    /// <exception cref="ArgumentNullException">转换函数为 <see langword="null"/>。</exception>
    /// <exception cref="InvalidOperationException">同一类型对已注册。</exception>
    public ConvConverterBuilder Register<TSource, TTarget>(ConvTryConverter<TSource, TTarget> converter)
    {
        if (converter == null)
            throw new ArgumentNullException(nameof(converter));
        if (_entries.ContainsKey((typeof(TSource), typeof(TTarget))))
            throw new InvalidOperationException("该类型对已注册转换函数。");
        _entries.Add((typeof(TSource), typeof(TTarget)), new ConvConverterEntry<TSource, TTarget>(converter));
        return this;
    }

    /// <summary>
    /// 创建独立的转换器快照。
    /// </summary>
    /// <returns>可供多个线程并发读取的转换器。</returns>
    public ConvConverter Build() => new(new Dictionary<(Type Source, Type Target), IConvConverterEntry>(_entries));
}
