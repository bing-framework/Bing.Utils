namespace Bing.Numeric;

/// <summary>
/// 数值判断器
/// </summary>
public static class NumericJudge
{
    /// <summary>
    /// 判断给定的数值是否包含在左值和右值之间。
    /// </summary>
    /// <param name="value">数值</param>
    /// <param name="left">左值</param>
    /// <param name="right">右值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsBetween(short value, short left, short right) => value >= left && value <= right;

    /// <summary>
    /// 判断给定的数值是否包含在左值和右值之间。
    /// </summary>
    /// <param name="value">数值</param>
    /// <param name="left">左值</param>
    /// <param name="right">右值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsBetween(int value, int left, int right) => value >= left && value <= right;

    /// <summary>
    /// 判断给定的数值是否包含在左值和右值之间。
    /// </summary>
    /// <param name="value">数值</param>
    /// <param name="left">左值</param>
    /// <param name="right">右值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsBetween(long value, long left, long right) => value >= left && value <= right;

    /// <summary>
    /// 判断给定的数值是否包含在左值和右值之间。
    /// </summary>
    /// <param name="value">数值</param>
    /// <param name="left">左值</param>
    /// <param name="right">右值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsBetween(float value, float left, float right) => value >= left && value <= right;

    /// <summary>
    /// 判断给定的数值是否包含在左值和右值之间。
    /// </summary>
    /// <param name="value">数值</param>
    /// <param name="left">左值</param>
    /// <param name="right">右值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsBetween(double value, double left, double right) => value >= left && value <= right;

    /// <summary>
    /// 判断给定的数值是否包含在左值和右值之间。
    /// </summary>
    /// <param name="value">数值</param>
    /// <param name="left">左值</param>
    /// <param name="right">右值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsBetween(decimal value, decimal left, decimal right) => value >= left && value <= right;

    /// <summary>
    /// 判断是否为非数值
    /// </summary>
    /// <param name="value">数值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNaN(double value) => Numbers.IsNaN(value);

    /// <summary>
    /// 判断是否为非数值
    /// </summary>
    /// <param name="value">数值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNaN(float value) => Numbers.IsNaN(value);
}