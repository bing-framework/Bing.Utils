using System;
using System.Runtime.CompilerServices;

namespace Bing.Numeric;

/// <summary>
/// 数值操作
/// </summary>
public static partial class Numbers
{
    /// <summary>
    /// 将数值截断，保留两位小数
    /// </summary>
    /// <param name="value">值</param>
    /// <remarks>该方法截断保留2位小数并且不进行四舍五入操作</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal RoundTruncate(decimal value) => RoundTruncate(value, 2);

    /// <summary>
    /// 将数值截断，保留指定小数位数
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="digits">小数位数</param>
    /// <remarks>该方法截断保留N位小数并且不进行四舍五入操作</remarks>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static decimal RoundTruncate(decimal value, int digits)
    {
        if (digits < 0)
            throw new ArgumentOutOfRangeException(nameof(digits), "digits must be greater than or equal to 0.");
#if NETCOREAPP3_0_OR_GREATER
        return Math.Round(value, digits, MidpointRounding.ToZero);
#else
            var power10 = CalculatePow(digits);
            return decimal.Truncate(value * power10) / power10;
#endif
    }

    /// <summary>
    /// 计算Pow
    /// </summary>
    /// <param name="precision">精度</param>
    private static decimal CalculatePow(int precision) => (decimal)Math.Pow(10, precision);
}

/// <summary>
/// 数值操作扩展
/// </summary>
public static partial class NumberExtensions
{
    /// <summary>
    /// 将数值截断，保留两位小数
    /// </summary>
    /// <param name="value">值</param>
    /// <remarks>该方法截断保留2位小数并且不进行四舍五入操作</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] 
    public static decimal RoundTruncate(this decimal value) => Numbers.RoundTruncate(value);

    /// <summary>
    /// 将数值截断，保留指定小数位数
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="digits">小数位数</param>
    /// <remarks>该方法截断保留N位小数并且不进行四舍五入操作</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] 
    public static decimal RoundTruncate(this decimal value, int digits) => Numbers.RoundTruncate(value, digits);
}