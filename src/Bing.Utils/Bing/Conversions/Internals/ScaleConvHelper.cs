namespace Bing.Conversions.Internals;

/// <summary>
/// 进制转换帮助类
/// </summary>
internal static class ScaleConvHelper
{
    /// <summary>
    /// 通用字符集：数字 + 小写字母 + 大写字母
    /// </summary>
    private const string CommonCharsetLowerFirst = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

    /// <summary>
    /// 通用字符集：数字 + 大写字母 + 小写字母
    /// </summary>
    private const string CommonCharsetUpperFirst = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    /// <summary>
    /// 避免视觉上容易混淆的字符集
    /// </summary>
    /// <remarks>
    /// 26进制：使用全小写字母作为26进制的字符集。<br />
    /// 32进制：修正32进制字符集，仅包含数字和大写字母，排除易混淆的I、O、L等。<br />
    /// 36进制：包含数字和小写字母。<br />
    /// 52进制：包含小写和大写字母，不包含数字。<br />
    /// 58进制：避开视觉上相似的字符，常用于加密货币。<br />
    /// 62进制：完整混合了数字、小写和大写字母。<br />
    /// </remarks>
    private static readonly Dictionary<int, string> LessConfusingCharsets = new()
    {
        { 26, "abcdefghijklmnopqrstuvwxyz" },
        { 32, "0123456789ABCDEFGHJKMNPQRSTUVWXYZ" },
        { 36, "0123456789abcdefghijklmnopqrstuvwxyz" },
        { 52, "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ" },
        { 58, "123456789abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ" },
        { 62, "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ" },
    };

    /// <summary>
    /// 将字符串表示形式从源数制转换为目标数制。
    /// </summary>
    /// <param name="things">要转换的字符串。</param>
    /// <param name="baseOfSource">源数制的基数。</param>
    /// <param name="baseOfTarget">目标数制的基数。</param>
    /// <param name="strategy">进制字符集策略</param>
    /// <returns>转换后的字符串。</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static string ThingsToThings(string things, int baseOfSource, int baseOfTarget, RadixCharsetStrategy strategy = RadixCharsetStrategy.AvoidConfusion)
    {
        if (string.IsNullOrWhiteSpace(things))
            return string.Empty;
        if (baseOfSource is < 2 or > 62)
            throw new ArgumentOutOfRangeException(nameof(baseOfSource), $"The baseOfSource radix\"{baseOfSource}\" is not in the range 2..62.");
        if (baseOfTarget is < 2 or > 62)
            throw new ArgumentOutOfRangeException(nameof(baseOfTarget), $"The baseOfTarget radix\"{baseOfTarget}\" is not in the range 2..62.");
        if (baseOfSource == baseOfTarget)
            return things;
        var val = ThingsToLong(things, baseOfSource, strategy);
        return LongToThings(val, baseOfTarget, strategy);
    }

    /// <summary>
    /// 将给定的字符串表示形式转换为长整型，基于指定的源数制。
    /// </summary>
    /// <param name="things">要转换的字符串。</param>
    /// <param name="baseOfSource">源数制的基数。</param>
    /// <param name="strategy">进制字符集策略</param>
    /// <returns>转换后的长整型数值。</returns>
    private static long ThingsToLong(string things, int baseOfSource, RadixCharsetStrategy strategy)
    {
        if (string.IsNullOrWhiteSpace(things))
            return 0L;
        var val = 0L;
        var multiplier = 1L;
        var baseCharset = GetBaseCharset(baseOfSource, strategy);
        things = things.Trim();
        for (var i = things.Length - 1; i >= 0; i--)
        {
            var index = baseCharset.IndexOf(things[i]);
            if (index < 0 || index >= baseOfSource)
                throw new ArgumentException($"The argument \"{things[i]}\" is not in {baseOfSource} system.");
            val += index * multiplier;
            multiplier *= baseOfSource;
        }
        return val;
    }

    /// <summary>
    /// 将长整型数值转换为指定目标数制的字符串表示形式。
    /// </summary>
    /// <param name="value">要转换的长整型数值。</param>
    /// <param name="baseOfTarget">目标数制的基数。</param>
    /// <param name="strategy">进制字符集策略</param>
    /// <returns>转换后的字符串。</returns>
    private static string LongToThings(long value, int baseOfTarget, RadixCharsetStrategy strategy)
    {
        if (value == 0)
            return "0";
        var baseCharset = GetBaseCharset(baseOfTarget, strategy);
        var result = new StringBuilder();
        var isNegative = value < 0;
        value = Math.Abs(value);
        while (value > 0)
        {
            result.Insert(0, baseCharset[(int)(value % baseOfTarget)]);
            value /= baseOfTarget;
        }
        if (isNegative)
            result.Insert(0, '-');
        return result.ToString();
    }

    /// <summary>
    /// 根据指定的基数获取对应的字符集字符串。
    /// </summary>
    /// <param name="radix">要获取字符集的基数。</param>
    /// <param name="strategy">进制字符串策略</param>
    /// <returns>指定基数对应的字符集字符串。</returns>
    private static string GetBaseCharset(int radix, RadixCharsetStrategy strategy)
    {
        if (strategy == RadixCharsetStrategy.LowerFirst)
            return CommonCharsetLowerFirst.Substring(0, radix);
        return LessConfusingCharsets.TryGetValue(radix, out var charset)
            ? charset
            : CommonCharsetUpperFirst.Substring(0, radix);
    }
}