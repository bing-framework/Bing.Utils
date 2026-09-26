namespace Bing.Text.Internals;

/// <summary>
/// Base32 编码与解码器。
/// </summary>
internal class Base32 : BaseXCore
{
    /// <summary>
    /// Base32 默认编码字符集。
    /// </summary>
    /// <remarks>
    /// 由大写英文字母 A 到 Z 和数字 2 到 7 组成。
    /// </remarks>
    public const string DefaultAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    /// <summary>
    /// 用于补齐 Base32 编码块的默认填充字符。
    /// </summary>
    /// <remarks>
    /// 使用等号将编码文本补齐为 8 个字符的整数倍。
    /// </remarks>
    public const char DefaultSpecial = '=';

    /// <inheritdoc />
    public override bool HasSpecial => true;

    /// <summary>
    /// 初始化 <see cref="Base32" /> 类的新实例。
    /// </summary>
    /// <param name="alphabet">用于 Base32 编码的字符集。如果未提供，则使用默认的字符集。</param>
    /// <param name="special">用于 Base32 编码的特殊填充字符。如果未提供，则使用默认的 '=' 字符。</param>
    /// <param name="encoding">用于编码字符串的编码方式。如果未指定，则默认使用UTF-8编码。</param>
    public Base32(string alphabet = DefaultAlphabet, char special = DefaultSpecial, Encoding encoding = null)
        : base(32, alphabet, special, encoding)
    {
    }

    /// <inheritdoc />
    public override string Encode(byte[] data)
    {
        if (data is null || data.Length == 0)
            return string.Empty;
        var dataLength = data.Length;
        var result = new StringBuilder((dataLength + 4) / 5 * 8);

        byte x1, x2;
        int i;

        var length5 = (dataLength / 5) * 5;
        for (i = 0; i < length5; i += 5)
        {
            x1 = data[i];
            result.Append(Alphabet[x1 >> 3]);

            x2 = data[i + 1];
            result.Append(Alphabet[((x1 << 2) & 0x1C) | (x2 >> 6)]);
            result.Append(Alphabet[(x2 >> 1) & 0x1F]);

            x1 = data[i + 2];
            result.Append(Alphabet[((x2 << 4) & 0x10) | (x1 >> 4)]);

            x2 = data[i + 3];
            result.Append(Alphabet[((x1 << 1) & 0x1E) | (x2 >> 7)]);
            result.Append(Alphabet[(x2 >> 2) & 0x1F]);

            x1 = data[i + 4];
            result.Append(Alphabet[((x2 << 3) & 0x18) | (x1 >> 5)]);
            result.Append(Alphabet[x1 & 0x1F]);
        }

        switch (dataLength - length5)
        {
            case 1:
                x1 = data[i];
                result.Append(Alphabet[x1 >> 3]);
                result.Append(Alphabet[(x1 << 2) & 0x1C]);

                result.Append(Special, 6);
                break;
            case 2:
                x1 = data[i];
                result.Append(Alphabet[x1 >> 3]);
                x2 = data[i + 1];
                result.Append(Alphabet[((x1 << 2) & 0x1C) | (x2 >> 6)]);
                result.Append(Alphabet[(x2 >> 1) & 0x1F]);
                result.Append(Alphabet[(x2 << 4) & 0x10]);

                result.Append(Special, 4);
                break;
            case 3:
                x1 = data[i];
                result.Append(Alphabet[x1 >> 3]);
                x2 = data[i + 1];
                result.Append(Alphabet[((x1 << 2) & 0x1C) | (x2 >> 6)]);
                result.Append(Alphabet[(x2 >> 1) & 0x1F]);
                x1 = data[i + 2];
                result.Append(Alphabet[((x2 << 4) & 0x10) | (x1 >> 4)]);
                result.Append(Alphabet[(x1 << 1) & 0x1E]);

                result.Append(Special, 3);
                break;
            case 4:
                x1 = data[i];
                result.Append(Alphabet[x1 >> 3]);
                x2 = data[i + 1];
                result.Append(Alphabet[((x1 << 2) & 0x1C) | (x2 >> 6)]);
                result.Append(Alphabet[(x2 >> 1) & 0x1F]);
                x1 = data[i + 2];
                result.Append(Alphabet[((x2 << 4) & 0x10) | (x1 >> 4)]);
                x2 = data[i + 3];
                result.Append(Alphabet[((x1 << 1) & 0x1E) | (x2 >> 7)]);
                result.Append(Alphabet[(x2 >> 2) & 0x1F]);
                result.Append(Alphabet[(x2 << 3) & 0x18]);

                result.Append(Special);
                break;
        }

        return result.ToString();
    }

    /// <inheritdoc />
    /// <remarks>
    /// null 或空字符串返回空数组；非空输入须由完整的 8 字符编码块组成，填充数量合法且末尾未使用位为零。
    /// </remarks>
    /// <exception cref="FormatException">非空输入的长度、字符、填充或末尾未使用位不合法。</exception>
    public override byte[] Decode(string data)
    {
        unchecked
        {
            if (string.IsNullOrEmpty(data))
                return Array.Empty<byte>();

            ValidateEncodedData(data);

            int additionalBytes = 0, diff, tempLen;


            var lastSpecialInd = data.Length;
            while (data[lastSpecialInd - 1] == Special)
                lastSpecialInd--;
            var tailLength = data.Length - lastSpecialInd;

            additionalBytes = tailLength switch
            {
                6 => 4,
                4 => 3,
                3 => 2,
                1 => 1,
                _ => additionalBytes
            };

            diff = tailLength - additionalBytes;
            tailLength = additionalBytes;
            tempLen = data.Length - diff;

            var result = new byte[(tempLen + 7) / 8 * 5 - tailLength];
            var length5 = result.Length / 5 * 5;
            int x1, x2, x3, x4, x5, x6, x7, x8;

            int i, srcInd = 0;
            for (i = 0; i < length5; i += 5)
            {
                x1 = InvAlphabet[data[srcInd++]];
                x2 = InvAlphabet[data[srcInd++]];
                x3 = InvAlphabet[data[srcInd++]];
                x4 = InvAlphabet[data[srcInd++]];
                x5 = InvAlphabet[data[srcInd++]];
                x6 = InvAlphabet[data[srcInd++]];
                x7 = InvAlphabet[data[srcInd++]];
                x8 = InvAlphabet[data[srcInd++]];

                result[i] = (byte)((x1 << 3) | ((x2 >> 2) & 0x07));
                result[i + 1] = (byte)((x2 << 6) | ((x3 << 1) & 0x3E) | ((x4 >> 4) & 0x01));
                result[i + 2] = (byte)((x4 << 4) | ((x5 >> 1) & 0xF));
                result[i + 3] = (byte)((x5 << 7) | ((x6 << 2) & 0x7C) | ((x7 >> 3) & 0x03));
                result[i + 4] = (byte)((x7 << 5) | (x8 & 0x1F));
            }

            switch (tailLength)
            {
                case 4:
                    x1 = InvAlphabet[data[srcInd++]];
                    x2 = InvAlphabet[data[srcInd++]];
                    result[i] = (byte)((x1 << 3) | ((x2 >> 2) & 0x07));
                    break;
                case 3:
                    x1 = InvAlphabet[data[srcInd++]];
                    x2 = InvAlphabet[data[srcInd++]];
                    x3 = InvAlphabet[data[srcInd++]];
                    x4 = InvAlphabet[data[srcInd++]];

                    result[i] = (byte)((x1 << 3) | ((x2 >> 2) & 0x07));
                    result[i + 1] = (byte)((x2 << 6) | ((x3 << 1) & 0x3E) | ((x4 >> 4) & 0x01));
                    break;
                case 2:
                    x1 = InvAlphabet[data[srcInd++]];
                    x2 = InvAlphabet[data[srcInd++]];
                    x3 = InvAlphabet[data[srcInd++]];
                    x4 = InvAlphabet[data[srcInd++]];
                    x5 = InvAlphabet[data[srcInd++]];

                    result[i] = (byte)((x1 << 3) | ((x2 >> 2) & 0x07));
                    result[i + 1] = (byte)((x2 << 6) | ((x3 << 1) & 0x3E) | ((x4 >> 4) & 0x01));
                    result[i + 2] = (byte)((x4 << 4) | ((x5 >> 1) & 0xF));
                    break;
                case 1:
                    x1 = InvAlphabet[data[srcInd++]];
                    x2 = InvAlphabet[data[srcInd++]];
                    x3 = InvAlphabet[data[srcInd++]];
                    x4 = InvAlphabet[data[srcInd++]];
                    x5 = InvAlphabet[data[srcInd++]];
                    x6 = InvAlphabet[data[srcInd++]];
                    x7 = InvAlphabet[data[srcInd++]];

                    result[i] = (byte)((x1 << 3) | ((x2 >> 2) & 0x07));
                    result[i + 1] = (byte)((x2 << 6) | ((x3 << 1) & 0x3E) | ((x4 >> 4) & 0x01));
                    result[i + 2] = (byte)((x4 << 4) | ((x5 >> 1) & 0xF));
                    result[i + 3] = (byte)((x5 << 7) | ((x6 << 2) & 0x7C) | ((x7 >> 3) & 0x03));
                    break;
            }

            return result;
        }
    }

    /// <summary>
    /// 验证 Base32 编码格式。
    /// </summary>
    /// <param name="data">待验证的非空编码文本。</param>
    /// <remarks>
    /// 在分配结果和索引字符集前验证编码块长度、字符、填充及末尾未使用位。
    /// </remarks>
    /// <exception cref="FormatException">编码长度、字符、填充数量或末尾未使用位不合法。</exception>
    private void ValidateEncodedData(string data)
    {
        if (data.Length % 8 != 0)
            throw new FormatException("Base32 编码长度必须是 8 的倍数。");
        var contentLength = data.Length;
        while (contentLength > 0 && data[contentLength - 1] == Special)
            contentLength--;
        var padding = data.Length - contentLength;
        var unusedBits = padding switch { 0 => 0, 1 => 3, 3 => 1, 4 => 4, 6 => 2, _ => -1 };
        if (contentLength == 0 || unusedBits < 0)
            throw new FormatException("Base32 填充数量无效。");
        for (var i = 0; i < contentLength; i++)
        {
            var character = data[i];
            if (character >= InvAlphabet.Length || InvAlphabet[character] < 0)
                throw new FormatException("Base32 包含非法字符或中间填充。");
        }
        if ((InvAlphabet[data[contentLength - 1]] & ((1 << unusedBits) - 1)) != 0)
            throw new FormatException("Base32 末尾未使用位必须为零。");
    }
};
