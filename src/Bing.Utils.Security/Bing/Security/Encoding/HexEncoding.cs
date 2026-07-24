namespace Bing.Security.Encoding;

/// <summary>
/// 提供严格的十六进制编码和解码操作。
/// </summary>
public static class HexEncoding
{
    /// <summary>
    /// 将字节序列编码为十六进制文本。
    /// </summary>
    /// <param name="value">要编码的字节序列。</param>
    /// <param name="lowerCase">为 <c>true</c> 时输出小写字母；否则输出大写字母。</param>
    /// <returns>十六进制文本；空输入返回空字符串。</returns>
    public static string Encode(ReadOnlySpan<byte> value, bool lowerCase = true)
    {
        if (value.IsEmpty)
            return string.Empty;

        const string lowerAlphabet = "0123456789abcdef";
        const string upperAlphabet = "0123456789ABCDEF";
        var alphabet = lowerCase ? lowerAlphabet : upperAlphabet;
        var result = new char[value.Length * 2];
        for (var index = 0; index < value.Length; index++)
        {
            result[index * 2] = alphabet[value[index] >> 4];
            result[index * 2 + 1] = alphabet[value[index] & 0x0F];
        }
        return new string(result);
    }

    /// <summary>
    /// 将十六进制文本解码为字节。
    /// </summary>
    /// <param name="value">要解码的十六进制文本。</param>
    /// <returns>解码后的字节数组。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> 为 <c>null</c> 时抛出。</exception>
    /// <exception cref="FormatException">文本长度为奇数或包含非十六进制字符时抛出。</exception>
    public static byte[] Decode(string value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        if (value.Length % 2 != 0)
            throw new FormatException("十六进制文本长度必须为偶数。");

        var result = new byte[value.Length / 2];
        for (var index = 0; index < result.Length; index++)
        {
            var high = GetNibble(value[index * 2]);
            var low = GetNibble(value[index * 2 + 1]);
            if (high < 0 || low < 0)
                throw new FormatException("十六进制文本包含非法字符。");
            result[index] = (byte)((high << 4) | low);
        }
        return result;
    }

    /// <summary>
    /// 尝试将十六进制文本解码为字节。
    /// </summary>
    /// <param name="value">要解码的十六进制文本。</param>
    /// <param name="result">解码成功时返回字节数组；失败时返回空数组。</param>
    /// <returns>解码成功时返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    public static bool TryDecode(string value, out byte[] result)
    {
        result = Array.Empty<byte>();
        if (value == null || value.Length % 2 != 0)
            return false;

        var decoded = new byte[value.Length / 2];
        for (var index = 0; index < decoded.Length; index++)
        {
            var high = GetNibble(value[index * 2]);
            var low = GetNibble(value[index * 2 + 1]);
            if (high < 0 || low < 0)
                return false;
            decoded[index] = (byte)((high << 4) | low);
        }

        result = decoded;
        return true;
    }

    /// <summary>
    /// 获取单个十六进制字符对应的数值。
    /// </summary>
    /// <param name="value">十六进制字符。</param>
    /// <returns>字符对应的数值；非法字符返回负数。</returns>
    private static int GetNibble(char value)
    {
        if (value >= '0' && value <= '9')
            return value - '0';
        if (value >= 'a' && value <= 'f')
            return value - 'a' + 10;
        if (value >= 'A' && value <= 'F')
            return value - 'A' + 10;
        return -1;
    }
}