namespace Bing.Conversions.Internals;

/// <summary>
/// 比例转换帮助类
/// </summary>
internal static class ScaleConvHelper
{
    /// <summary>
    /// long 长度
    /// </summary>
    private const int BITS_IN_LONG = 64;

    /// <summary>
    /// 数字
    /// </summary>
    private const string DIGITS = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    /// <summary>
    /// 数字数组
    /// </summary>
    private static readonly char[] DigitArray;

    /// <summary>
    /// 静态构造函数
    /// </summary>
    static ScaleConvHelper() => DigitArray = DIGITS.ToCharArray();

    /// <summary>
    /// 将字符串表示形式从源数制转换为目标数制。
    /// </summary>
    /// <param name="things">要转换的字符串。</param>
    /// <param name="baseOfSource">源数制的基数。</param>
    /// <param name="baseOfTarget">目标数制的基数。</param>
    /// <returns>转换后的字符串。</returns>
    public static string ThingsToThings(string things, int baseOfSource, int baseOfTarget)
    {
        if (string.IsNullOrWhiteSpace(things))
            return string.Empty;
        if (baseOfSource < 2 || baseOfSource > 36)
            throw new ArgumentOutOfRangeException(nameof(baseOfSource), $"The baseOfSource radix\"{baseOfSource}\" is not in the range 2..36.");
        if (baseOfTarget < 2 || baseOfTarget > 36)
            throw new ArgumentOutOfRangeException(nameof(baseOfTarget), $"The baseOfTarget radix\"{baseOfTarget}\" is not in the range 2..36.");
        if (baseOfSource == baseOfTarget)
            return things;
        var val = ThingsToLong(things, baseOfSource);
        return LongToThings(val, baseOfTarget);
    }

    /// <summary>
    /// 将给定的字符串表示形式转换为长整型，基于指定的源数制。
    /// </summary>
    /// <param name="things">要转换的字符串。</param>
    /// <param name="baseOfSource">源数制的基数。</param>
    /// <returns>转换后的长整型数值。</returns>
    private static long ThingsToLong(string things, int baseOfSource)
    {
        if (string.IsNullOrWhiteSpace(things))
            return 0L;
        var currentDigits = DIGITS.AsSpan().Slice(0, baseOfSource).ToArray();
        var val = 0L;
        things = things.Trim().ToUpperInvariant();
        for (var i = 0; i < things.Length; i++)
        {
            if (currentDigits.Contains(things[i]))
            {
                try
                {
                    val += (long)Math.Pow(baseOfSource, i) * GetCharIndex(things[things.Length - i - 1]);
                }
                catch
                {
                    throw new OverflowException("An overflow occurred during operation.");
                }
            }
            else
            {
                throw new ArgumentException($"The argument \"{things[i]}\" is not in {baseOfSource} system.");
            }
        }
        return val;
    }

    /// <summary>
    /// 将长整型数值转换为指定目标数制的字符串表示形式。
    /// </summary>
    /// <param name="value">要转换的长整型数值。</param>
    /// <param name="baseOfTarget">目标数制的基数。</param>
    /// <returns>转换后的字符串。</returns>
    private static string LongToThings(long value, int baseOfTarget)
    {
        int digitsIndex;
        var longPositive = Math.Abs(value);
        var digitsOut = new char[BITS_IN_LONG - 1];
        for (digitsIndex = 0; digitsIndex <= BITS_IN_LONG; digitsIndex++)
        {
            if (longPositive == 0)
                break;
            digitsOut[digitsOut.Length - digitsIndex - 1] = DigitArray[longPositive % baseOfTarget];
            longPositive /= baseOfTarget;
        }
        return new string(digitsOut, digitsOut.Length - digitsIndex, digitsIndex);
    }

    /// <summary>
    /// 获取指定字符在数字字符数组中的索引位置。
    /// </summary>
    /// <param name="value">要查找的字符。</param>
    /// <returns>字符在数字字符数组中的索引位置，如果未找到则返回 0。</returns>
    private static int GetCharIndex(char value)
    {
        for (var i = 0; i < DIGITS.Length; i++)
        {
            if (DIGITS[i] == value)
                return i;
        }
        return 0;
    }
}