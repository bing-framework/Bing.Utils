using System.Text;

namespace Bing.OneTimePassword;

/// <summary>
/// 按 RFC 4648 编解码无填充 Base32 文本。
/// </summary>
public static class OtpSecretEncoding
{
    /// <summary>
    /// Base32 使用的 RFC 4648 字母表。
    /// </summary>
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    /// <summary>
    /// 将任意字节编码为大写且无填充的 Base32 文本。
    /// </summary>
    /// <param name="data">待编码的数据；空数据编码为空文本。</param>
    /// <returns>无填充的大写 RFC 4648 Base32 文本。</returns>
    public static string Encode(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
            return string.Empty;

        var outputLength = (data.Length * 8 + 4) / 5;
        var builder = new StringBuilder(outputLength);
        uint buffer = 0;
        var bitCount = 0;
        foreach (var value in data)
        {
            buffer = (buffer << 8) | value;
            bitCount += 8;
            while (bitCount >= 5)
            {
                bitCount -= 5;
                builder.Append(Alphabet[(int)((buffer >> bitCount) & 0x1f)]);
            }
            buffer &= bitCount == 0 ? 0u : (1u << bitCount) - 1;
        }

        if (bitCount > 0)
            builder.Append(Alphabet[(int)((buffer << (5 - bitCount)) & 0x1f)]);
        return builder.ToString();
    }

    /// <summary>
    /// 严格解码不含填充、空格或非规范尾部位的 Base32 文本。
    /// </summary>
    /// <param name="value">待解码的 Base32 文本；允许 ASCII 小写字母。</param>
    /// <returns>解码后的字节；空文本返回空数组。</returns>
    /// <exception cref="ArgumentNullException">输入为 <see langword="null" /> 时抛出。</exception>
    /// <exception cref="FormatException">长度、字符、填充或尾部未使用位不合法时抛出。</exception>
    public static byte[] Decode(string value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        if (value.Length == 0)
            return Array.Empty<byte>();
        if (value.Length > 8192)
            throw new FormatException("Base32 文本超过长度上限。");

        var remainder = value.Length & 7;
        if (remainder == 1 || remainder == 3 || remainder == 6)
            throw new FormatException("Base32 文本长度不符合 RFC 4648。");

        var output = new byte[value.Length * 5 / 8];
        uint buffer = 0;
        var bitCount = 0;
        var outputIndex = 0;
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (character >= 'a' && character <= 'z')
                character = (char)(character - ('a' - 'A'));
            var digit = Alphabet.IndexOf(character);
            if (digit < 0)
                throw Invalid(output);

            buffer = (buffer << 5) | (uint)digit;
            bitCount += 5;
            if (bitCount >= 8)
            {
                bitCount -= 8;
                output[outputIndex++] = (byte)(buffer >> bitCount);
            }
            buffer &= bitCount == 0 ? 0u : (1u << bitCount) - 1;
        }

        if (buffer != 0 || outputIndex != output.Length)
            throw Invalid(output);
        return output;
    }

    /// <summary>
    /// 清理失败解码过程中已分配的字节并创建格式异常。
    /// </summary>
    /// <param name="bytes">需要清理的局部输出缓冲区。</param>
    /// <returns>包含稳定格式说明的异常实例。</returns>
    private static FormatException Invalid(byte[] bytes)
    {
        Array.Clear(bytes, 0, bytes.Length);
        return new FormatException("Base32 文本包含非法字符或非零尾部未使用位。");
    }
}
