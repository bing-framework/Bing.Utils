namespace Bing.Conversions.Internals;

/// <summary>
/// 进制反转帮助类
/// </summary>
internal static class ScaleRevHelper
{
    /// <summary>
    /// 反转定长字符分组的排列顺序。
    /// </summary>
    /// <param name="val">要分组的文本；为空白时原样返回。</param>
    /// <param name="bitLength">每组字符数，调用方应传入正整数。</param>
    /// <returns>分组顺序反转后的文本，组内字符顺序不变；输入为 null 时返回 null。</returns>
    /// <remarks>
    /// 不足整组时先在左侧补零。
    /// </remarks>
    public static string Reverse(string val, int bitLength)
    {
        if (string.IsNullOrWhiteSpace(val))
            return val;
        var left = val.Length % bitLength;
        if (left > 0)
            val = $"{'0'.Repeat(bitLength - left)}{val}";
        var builder = new StringBuilder();
        for (var i = val.Length - bitLength; i >= 0; i -= bitLength)
            builder.Append(val.Substring(i, bitLength));
        return builder.ToString();
    }
}
