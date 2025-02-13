using System.Diagnostics.Contracts;
using System.Globalization;

namespace Bing.Numeric;

/// <summary>
/// 数值操作
/// </summary>
public static partial class Numbers
{
    /// <summary>
    /// 转换为decimal。通过先转换为字符串将浮点数转换为小数的准确方法，避免公差问题
    /// </summary>
    /// <param name="value">值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal ToDecimal(float value) => decimal.Parse(value.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// 转换为decimal。
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="precision">小数位数</param>
    /// <param name="mode">四舍五入策略。默认值：<see cref=" MidpointRounding.AwayFromZero"/></param>
    /// <returns>decimal</returns>
    public static decimal ToDecimal(float value, int precision, MidpointRounding mode = MidpointRounding.AwayFromZero) => Math.Round(value.ToDecimal(), precision, mode);

    /// <summary>
    /// 转换为decimal。通过先转换为字符串将浮点数转换为小数的准确方法，避免公差问题
    /// </summary>
    /// <param name="value">值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal ToDecimal(double value) => decimal.Parse(value.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// 转换为decimal。
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="precision">小数位数</param>
    /// <param name="mode">四舍五入策略。默认值：<see cref=" MidpointRounding.AwayFromZero"/></param>
    /// <returns>decimal</returns>
    public static decimal ToDecimal(double value, int precision, MidpointRounding mode = MidpointRounding.AwayFromZero) => Math.Round(value.ToDecimal(), precision, mode);

    /// <summary>
    /// 转换为double。通过将有限值四舍五入到小数点公差级别来将 float 转换为 double 的准确方法。
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="precision">精度</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static double ToDouble(float value, int precision)
    {
        if (precision < 0 || precision > 15)
            throw new ArgumentOutOfRangeException(nameof(precision), precision, "Must be between 0 and 15.");

        Contract.EndContractBlock();
        var result = ReturnZeroIfFinite(value);
        return IsZeroValue(result) ? Math.Round(value, precision) : result;
    }

    /// <summary>
    /// 转换为double。 通过首先转换为字符串将 float 转换为 double 的准确方法。 避免公差问题。
    /// </summary>
    /// <param name="value">值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ToDouble(float value)
    {
        var result = ReturnZeroIfFinite(value);
        return IsZeroValue(result) ? double.Parse(value.ToString(CultureInfo.InvariantCulture)) : result;
    }

    /// <summary>
    /// 转换为double。通过首先转换为字符串来将可能的 float 转换为 double 的准确方法。 避免公差问题。
    /// </summary>
    /// <param name="value">值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ToDouble(float? value) => value.HasValue ? ToDouble(value.Value) : double.NaN;

    /// <summary>
    /// 转换为double。通过将有限值四舍五入到小数点公差级别，将可能的浮点数转换为双精度值的准确方法。
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="precision">精度</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static double ToDouble(float? value, int precision)
    {
        if (precision < 0 || precision > 15)
            throw new ArgumentOutOfRangeException(nameof(precision), precision, "Must be between 0 and 15.");
        Contract.EndContractBlock();
        return value.HasValue ? ToDouble(value.Value, precision) : double.NaN;
    }
}

/// <summary>
/// 数值操作扩展
/// </summary>
public static partial class NumberExtensions
{
    /// <summary>
    /// 转换为decimal。通过先转换为字符串将浮点数转换为小数的准确方法，避免公差问题
    /// </summary>
    /// <param name="value">值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal ToDecimal(this float value) => Numbers.ToDecimal(value);

    /// <summary>
    /// 转换为decimal。
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="precision">小数位数</param>
    /// <param name="mode">四舍五入策略。默认值：<see cref=" MidpointRounding.AwayFromZero"/></param>
    /// <returns>decimal</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal ToDecimal(this float value, int precision, MidpointRounding mode = MidpointRounding.AwayFromZero) => Numbers.ToDecimal(value, precision, mode);

    /// <summary>
    /// 转换为decimal。通过先转换为字符串将浮点数转换为小数的准确方法，避免公差问题
    /// </summary>
    /// <param name="value">值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal ToDecimal(this double value) => Numbers.ToDecimal(value);

    /// <summary>
    /// 转换为decimal。
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="precision">小数位数</param>
    /// <param name="mode">四舍五入策略。默认值：<see cref=" MidpointRounding.AwayFromZero"/></param>
    /// <returns>decimal</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal ToDecimal(this double value, int precision, MidpointRounding mode = MidpointRounding.AwayFromZero) => Numbers.ToDecimal(value, precision, mode);

    /// <summary>
    /// 转换为double。通过将有限值四舍五入到小数点公差级别来将 float 转换为 double 的准确方法。
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="precision">精度</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ToDouble(this float value, int precision) => Numbers.ToDouble(value, precision);

    /// <summary>
    /// 转换为double。 通过首先转换为字符串将 float 转换为 double 的准确方法。 避免公差问题。
    /// </summary>
    /// <param name="value">值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ToDouble(this float value) => Numbers.ToDouble(value);

    /// <summary>
    /// 转换为double。通过首先转换为字符串来将可能的 float 转换为 double 的准确方法。 避免公差问题。
    /// </summary>
    /// <param name="value">值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ToDouble(this float? value) => Numbers.ToDouble(value);

    /// <summary>
    /// 转换为double。通过将有限值四舍五入到小数点公差级别，将可能的浮点数转换为双精度值的准确方法。
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="precision">精度</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ToDouble(this float? value, int precision) => Numbers.ToDouble(value, precision);
}