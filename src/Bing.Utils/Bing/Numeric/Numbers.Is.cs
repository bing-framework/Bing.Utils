namespace Bing.Numeric;

/// <summary>
/// 数值操作
/// </summary>
public static partial class Numbers
{
    /// <summary>
    /// 最大的JSON安全整数
    /// </summary>
    /// <remarks>
    /// 参考：https://developer.mozilla.org/zh-CN/docs/Web/JavaScript/Reference/Global_Objects/Number/MAX_SAFE_INTEGER
    /// </remarks>
    public const long MaxJsonSafeInteger = 9007199254740991L;

    /// <summary>
    /// 是否NaN
    /// </summary>
    /// <param name="value">值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNaN(double value) => double.IsNaN(value);

    /// <summary>
    /// 是否NaN
    /// </summary>
    /// <param name="value">值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNaN(float value) => float.IsNaN(value);

    /// <summary>
    /// 是否默认值
    /// </summary>
    /// <param name="value">值</param>
    public static bool IsDefaultValue(double value) => value.Equals(default);

    /// <summary>
    /// 判断值是否为JSON安全整数
    /// </summary>
    /// <param name="value">值</param>
    /// <returns>如果值是JSON安全整数，则返回true；否则返回false</returns>
    public static bool IsJsonSafeInteger(ulong value) => value <= MaxJsonSafeInteger;
}

/// <summary>
/// 数值操作扩展
/// </summary>
public static partial class NumberExtensions
{
    /// <summary>
    /// 是否默认值
    /// </summary>
    /// <param name="value">值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsDefault(this double value) => Numbers.IsDefaultValue(value);
}