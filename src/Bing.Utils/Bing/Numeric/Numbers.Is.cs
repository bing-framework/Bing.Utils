using System.Runtime.CompilerServices;

namespace Bing.Numeric;

/// <summary>
/// 数值操作
/// </summary>
public static partial class Numbers
{
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