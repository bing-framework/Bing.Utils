namespace Bing.Conversions;

/// <summary>
/// 尝试将指定源类型转换为目标类型。
/// </summary>
/// <typeparam name="TSource">源类型。</typeparam>
/// <typeparam name="TTarget">目标类型。</typeparam>
/// <param name="input">待转换的输入值。</param>
/// <param name="result">转换成功时的结果；失败时由转换器统一置为默认值。</param>
/// <returns>转换成功返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
public delegate bool ConvTryConverter<TSource, TTarget>(TSource input, out TTarget result);
