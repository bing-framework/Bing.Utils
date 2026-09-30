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
    /// 32进制：使用 Crockford 字符集，排除 I、L、O、U；历史编码需按旧映射迁移。<br />
    /// 36进制：包含数字和小写字母。<br />
    /// 52进制：包含小写和大写字母，不包含数字。<br />
    /// 58进制：排除数字 0、大写字母 I、O 和小写字母 l。<br />
    /// 62进制：完整混合了数字、小写和大写字母。<br />
    /// </remarks>
    private static readonly Dictionary<int, string> LessConfusingCharsets = new()
    {
        { 26, "abcdefghijklmnopqrstuvwxyz" },
        { 32, "0123456789ABCDEFGHJKMNPQRSTVWXYZ" },
        { 36, "0123456789abcdefghijklmnopqrstuvwxyz" },
        { 52, "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ" },
        { 58, "123456789abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ" },
        { 62, "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ" },
    };

    /// <summary>
    /// 将数值从源进制转换为目标进制。
    /// </summary>
    /// <param name="things">要转换的非负整数文本；为空白时返回空字符串。</param>
    /// <param name="baseOfSource">源进制，范围为 2 到 62。</param>
    /// <param name="baseOfTarget">目标进制，范围为 2 到 62。</param>
    /// <param name="strategy">确定数字字符顺序的策略。</param>
    /// <returns>转换后的文本；源和目标进制相同时校验数值后保留原始文本。</returns>
    /// <remarks>
    /// 非空白输入支持 0 到 Int64.MaxValue；解析时忽略两端空白。
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">输入非空白且源进制或目标进制不在 2 到 62 之间。</exception>
    /// <exception cref="ArgumentException">输入含有所选进制字符集之外的字符。</exception>
    /// <exception cref="OverflowException">输入数值超过 Int64.MaxValue。</exception>
    public static string ThingsToThings(string things, int baseOfSource, int baseOfTarget, RadixCharsetStrategy strategy = RadixCharsetStrategy.AvoidConfusion)
    {
        if (string.IsNullOrWhiteSpace(things))
            return string.Empty;
        if (baseOfSource is < 2 or > 62)
            throw new ArgumentOutOfRangeException(nameof(baseOfSource), $"The baseOfSource radix\"{baseOfSource}\" is not in the range 2..62.");
        if (baseOfTarget is < 2 or > 62)
            throw new ArgumentOutOfRangeException(nameof(baseOfTarget), $"The baseOfTarget radix\"{baseOfTarget}\" is not in the range 2..62.");
        var val = ThingsToLong(things, baseOfSource, strategy);
        if (baseOfSource == baseOfTarget)
            return things;
        return LongToThings(val, baseOfTarget, strategy);
    }

    /// <summary>
    /// 解析指定进制的非负整数文本。
    /// </summary>
    /// <param name="things">要解析的文本；为空白时返回零。</param>
    /// <param name="baseOfSource">源进制。</param>
    /// <param name="strategy">确定数字字符顺序的策略。</param>
    /// <returns>解析得到的非负长整数。</returns>
    /// <exception cref="ArgumentException">输入含有所选进制字符集之外的字符。</exception>
    /// <exception cref="OverflowException">输入数值超过 Int64.MaxValue。</exception>
    private static long ThingsToLong(string things, int baseOfSource, RadixCharsetStrategy strategy)
    {
        if (string.IsNullOrWhiteSpace(things))
            return 0L;
        var val = 0L;
        var baseCharset = GetBaseCharset(baseOfSource, strategy);
        things = things.Trim();
        for (var i = 0; i < things.Length; i++)
        {
            var index = baseCharset.IndexOf(things[i]);
            if (index < 0 || index >= baseOfSource)
                throw new ArgumentException($"The argument \"{things[i]}\" is not in {baseOfSource} system.");
            // 逐位受检累加，避免溢出后不同输入静默得到同一结果。
            val = checked(val * baseOfSource + index);
        }
        return val;
    }

    /// <summary>
    /// 将长整数转换为指定进制的文本。
    /// </summary>
    /// <param name="value">要转换的长整型数值。</param>
    /// <param name="baseOfTarget">目标数制的基数。</param>
    /// <param name="strategy">进制字符集策略</param>
    /// <returns>转换后的字符串。</returns>
    private static string LongToThings(long value, int baseOfTarget, RadixCharsetStrategy strategy)
    {
        var baseCharset = GetBaseCharset(baseOfTarget, strategy);
        if (value == 0)
            return baseCharset[0].ToString();
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
    /// 获取指定进制和策略的数字字符集。
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
