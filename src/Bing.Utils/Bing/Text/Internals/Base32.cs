namespace Bing.Text.Internals;

/// <summary>
/// 表示 Base32 编码实现的类。
/// </summary>
internal class Base32 : BaseXCore
{
    /// <summary>
    /// 默认的 Base32 字符集。
    /// </summary>
    /// <remarks>
    /// 这个字母表包含了 26 个大写英文字母和数字 2 到 7，总共 32 个字符。
    /// Base32 编码通常使用这个字母表来表示二进制数据。
    /// </remarks>
    public const string DefaultAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    /// <summary>
    /// 定义 Base32 编码中使用的默认特殊字符。
    /// </summary>
    /// <remarks>
    /// 特殊字符 '=' 用于 Base32 编码的填充。当编码的数据不足以填满最后一个 5 位的编码单元时，
    /// 使用 '=' 字符来填充，以确保编码的字符串长度是 8 的倍数。
    /// </remarks>
    public const char DefaultSpecial = '=';

    /// <inheritdoc />
    public override bool HasSpecial => true;

    /// <summary>
    /// 初始化一个 <see cref="Base32"/> 类型的实例。
    /// </summary>
    /// <param name="alphabet">用于 Base32 编码的字符集。如果未提供，则使用默认的字符集。</param>
    /// <param name="special">用于 Base32 编码的特殊填充字符。如果未提供，则使用默认的 '=' 字符。</param>
    /// <param name="encoding">用于字符串和字节之间转换的编码。如果未提供，则默认使用 UTF-8 编码。</param>
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
    public override byte[] Decode(string data)
    {
        unchecked
        {
            if (string.IsNullOrEmpty(data))
                return Array.Empty<byte>();

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
};