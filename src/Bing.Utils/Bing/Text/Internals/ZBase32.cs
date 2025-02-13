namespace Bing.Text.Internals;

/// <summary>
/// 表示 ZBase32 编码实现的类。
/// </summary>
internal class ZBase32 : BaseXCore
{
    /// <summary>
    /// 默认的 ZBase32 字符集。
    /// </summary>
    /// <remarks>
    /// 包含不同于Base32的字符顺序，提供更好的压缩性能。
    /// </remarks>
    public const string DefaultAlphabet = "ybndrfg8ejkmcpqxot1uwisza345h769";

    /// <summary>
    /// 默认的特殊字符，ZBase32不使用特殊字符。
    /// </summary>
    /// <remarks>
    /// 由于ZBase32编码不需要填充字符，因此这里使用(char)0作为默认值。
    /// </remarks>
    public const char DefaultSpecial = (char)0;

    /// <inheritdoc />
    public override bool HasSpecial => false;

    /// <summary>
    /// 初始化一个 <see cref="ZBase32"/> 类型的实例。
    /// </summary>
    /// <param name="alphabet">用于 ZBase32 编码的字符集。如果未提供，则使用默认的字符集。</param>
    /// <param name="special">特殊字符。ZBase32 编码中通常不使用特殊字符，故默认为(char)0。</param>
    /// <param name="encoding">用于编码字符串的编码方式。如果未指定，则默认使用UTF-8编码。</param>
    public ZBase32(string alphabet = DefaultAlphabet, char special = DefaultSpecial, Encoding encoding = null)
        : base(32, alphabet, special, encoding)
    {
    }

    /// <inheritdoc />
    public override string Encode(byte[] data)
    {
        if (data is null || data.Length == 0)
            return string.Empty;

        unchecked
        {
            var encodedResult = new StringBuilder((int)Math.Ceiling(data.Length * 8.0 / 5.0));

            for (var i = 0; i < data.Length; i += 5)
            {
                var byteCount = Math.Min(5, data.Length - i);

                ulong buffer = 0;
                for (var j = 0; j < byteCount; ++j)
                    buffer = (buffer << 8) | data[i + j];

                var bitCount = byteCount * 8;
                while (bitCount > 0)
                {
                    var index = bitCount >= 5
                        ? (int)(buffer >> (bitCount - 5)) & 0x1f
                        : (int)(buffer & (ulong)(0x1f >> (5 - bitCount))) << (5 - bitCount);

                    encodedResult.Append(DefaultAlphabet[index]);
                    bitCount -= 5;
                }
            }

            return encodedResult.ToString();
        }
    }

    /// <inheritdoc />
    public override byte[] Decode(string data)
    {
        if (string.IsNullOrEmpty(data))
            return Array.Empty<byte>();

        var result = new List<byte>((int)Math.Ceiling(data.Length * 5.0 / 8.0));
        var index = new int[8]; // 使用常规数组替换 stackalloc

        for (var i = 0; i < data.Length;)
        {
            int currentPosition = i;

            var k = 0;
            while (k < 8)
            {
                if (currentPosition >= data.Length)
                {
                    index[k++] = -1;
                    continue;
                }

                if (InvAlphabet[data[currentPosition]] == -1)
                {
                    currentPosition++;
                    continue;
                }

                index[k] = data[currentPosition];
                k++;
                currentPosition++;
            }

            i = currentPosition;

            var shortByteCount = 0;
            ulong buffer = 0;
            for (var j = 0; j < 8 && index[j] != -1; ++j)
            {
#pragma warning disable CS0675
                buffer = (buffer << 5) | (ulong)(InvAlphabet[index[j]] & 0x1f);
#pragma warning restore CS0675
                shortByteCount++;
            }

            var bitCount = shortByteCount * 5;
            while (bitCount >= 8)
            {
                result.Add((byte)((buffer >> (bitCount - 8)) & 0xff));
                bitCount -= 8;
            }
        }

        return result.ToArray();
    }
}