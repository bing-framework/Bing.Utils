namespace Bing.Conversions.Internals;

/// <summary>
/// 进制反转帮助类
/// </summary>
internal static class ScaleRevHelper
{
    /// <summary>
    /// 将字符串按指定位长度反转。
    /// </summary>
    /// <param name="val">要反转的字符串。</param>
    /// <param name="bitLength">每个部分的位长度。</param>
    /// <returns>反转后的字符串。</returns>
    public static string Reverse(string val, int bitLength)
    {
        if (string.IsNullOrWhiteSpace(val))
            return val;
        var left = val.Length % bitLength;
        if (left > 0)
            val = $"{'0'.Repeat(left)}{val}";
        var builder = new StringBuilder();
        for (var i = val.Length - bitLength; i >= 0; i -= bitLength)
            builder.Append(val.Substring(i, bitLength));
        return builder.ToString();
    }
}