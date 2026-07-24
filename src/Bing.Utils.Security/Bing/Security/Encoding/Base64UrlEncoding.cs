namespace Bing.Security.Encoding;

/// <summary>
/// 提供 RFC 4648 Base64Url 编码和严格解码操作。
/// </summary>
public static class Base64UrlEncoding
{
    /// <summary>
    /// 将字节序列编码为无填充的 Base64Url 文本。
    /// </summary>
    /// <param name="value">要编码的字节序列。</param>
    /// <returns>无填充、使用 <c>-</c> 和 <c>_</c> 的 Base64Url 文本。</returns>
    public static string Encode(ReadOnlySpan<byte> value)
    {
        if (value.IsEmpty)
            return string.Empty;

        return Convert.ToBase64String(value.ToArray())
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    /// <summary>
    /// 将无填充 Base64Url 文本解码为字节。
    /// </summary>
    /// <param name="value">要解码的 Base64Url 文本。</param>
    /// <returns>解码后的字节数组。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> 为 <c>null</c> 时抛出。</exception>
    /// <exception cref="FormatException">文本不符合无填充 Base64Url 格式时抛出。</exception>
    public static byte[] Decode(string value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        if (!TryDecode(value, out var result))
            throw new FormatException("文本不是有效的无填充 Base64Url 值。");
        return result;
    }

    /// <summary>
    /// 尝试将无填充 Base64Url 文本解码为字节。
    /// </summary>
    /// <param name="value">要解码的 Base64Url 文本。</param>
    /// <param name="result">解码成功时返回字节数组；失败时返回空数组。</param>
    /// <returns>解码成功时返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    public static bool TryDecode(string value, out byte[] result)
    {
        result = Array.Empty<byte>();
        if (value == null || value.IndexOf('=') >= 0 || value.Length % 4 == 1)
            return false;

        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            var isUpper = character >= 'A' && character <= 'Z';
            var isLower = character >= 'a' && character <= 'z';
            var isDigit = character >= '0' && character <= '9';
            if (!isUpper && !isLower && !isDigit && character != '-' && character != '_')
                return false;
        }

        var paddingLength = (4 - value.Length % 4) % 4;
        var base64 = value.Replace('-', '+').Replace('_', '/') + new string('=', paddingLength);
        try
        {
            result = Convert.FromBase64String(base64);
            return true;
        }
        catch (FormatException)
        {
            result = Array.Empty<byte>();
            return false;
        }
    }
}