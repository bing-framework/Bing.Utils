namespace Bing.Text.Internals;

/// <summary>
/// 表示 Base91 编码实现的类。
/// </summary>
internal class Base91 : BaseXCore
{
    /// <summary>
    /// 默认的 Base91 字符集。
    /// </summary>
    public const string DefaultAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!#$%&()*+,./:;<=>?@[]^_`{|}~\"";

    /// <summary>
    /// 默认的特殊字符，Base91不使用特殊字符。
    /// </summary>
    /// <remarks>
    /// 由于Base91编码不需要填充字符，因此这里使用(char)0作为默认值。
    /// </remarks>
    public const char DefaultSpecial = (char)0;

    /// <inheritdoc />
    public override bool HasSpecial => false;

    /// <summary>
    /// 初始化一个 <see cref="Base91"/> 类型的实例。
    /// </summary>
    /// <param name="alphabet">使用的字符集。如果不指定，默认为91个字符的集合。</param>
    /// <param name="special">特殊字符。Base91编码中通常不使用特殊字符，故默认为(char)0。</param>
    /// <param name="encoding">用于编码字符串的编码方式。如果未指定，则默认使用UTF-8编码。</param>
    public Base91(string alphabet = DefaultAlphabet, char special = DefaultSpecial, Encoding encoding = null) : base(91, alphabet, special, encoding)
    {
        BlockBitsCount = 13;
        BlockCharsCount = 2;
    }

    /// <inheritdoc />
    public override string Encode(byte[] data)
    {
        if (data is null || data.Length == 0)
            return string.Empty;

        var result = new StringBuilder(data.Length);

        int ebq = 0, en = 0;
        for (var i = 0; i < data.Length; ++i)
        {
            ebq |= (data[i] & 255) << en;
            en += 8;
            if (en > 13)
            {
                var ev = ebq & 8191;

                if (ev > 88)
                {
                    ebq >>= 13;
                    en -= 13;
                }
                else
                {
                    ev = ebq & 16383;
                    ebq >>= 14;
                    en -= 14;
                }

                var quotient = Math.DivRem(ev, 91, out var remainder);
                result.Append(Alphabet[remainder]);
                result.Append(Alphabet[quotient]);
            }
        }

        if (en > 0)
        {
            var quotient = Math.DivRem(ebq, 91, out var remainder);
            result.Append(Alphabet[remainder]);
            if (en > 7 || ebq > 90)
                result.Append(Alphabet[quotient]);
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

            int dbq = 0, dn = 0, dv = -1;

            var result = new List<byte>(data.Length);
            for (var i = 0; i < data.Length; ++i)
            {
                if (InvAlphabet[data[i]] == -1)
                    continue;
                if (dv == -1)
                    dv = InvAlphabet[data[i]];
                else
                {
                    dv += InvAlphabet[data[i]] * 91;
                    dbq |= dv << dn;
                    dn += (dv & 8191) > 88 ? 13 : 14;
                    do
                    {
                        result.Add((byte)dbq);
                        dbq >>= 8;
                        dn -= 8;
                    } while (dn > 7);

                    dv = -1;
                }
            }

            if (dv != -1)
                result.Add((byte)(dbq | dv << dn));

            return result.ToArray();
        }
    }
}