namespace Bing.Utils.Conversions;

/// <summary>
/// 尝试转换
/// </summary>
/// <typeparam name="TFrom">来源类型</typeparam>
/// <typeparam name="TTo">目标类型</typeparam>
public interface IConversionTry<in TFrom, TTo>
{
    /// <summary>
    /// 是否转换成功
    /// </summary>
    /// <param name="from">源对象</param>
    /// <param name="to">目标对象</param>
    /// <returns>转换成功时返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    bool Is(TFrom from, out TTo to);
}
