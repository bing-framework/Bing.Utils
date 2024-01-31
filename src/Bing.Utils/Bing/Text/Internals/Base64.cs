namespace Bing.Text.Internals;

/// <summary>
/// 表示 Base64 编码实现的类。
/// </summary>
internal class Base64 : BaseXCore
{
    /// <summary>
    /// 默认的 Base64 字符集。
    /// </summary>
    /// <remarks>
    /// 此字符集包括所有大写字母 (A-Z)、所有小写字母 (a-z)、数字 (0-9) 以及两个额外的符号 '+' 和 '/'。
    /// 它是标准 Base64 编码中使用的默认字符集。
    /// </remarks>
    public const string DefaultAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

    /// <summary>
    /// Base64 编码中用于填充的默认特殊字符。
    /// </summary>
    /// <remarks>
    /// 特殊字符 '=' 用于 Base64 编码的输出，以确保输出字符串的长度总是 4 的倍数。
    /// 它在编码过程中填补不足的字符位。
    /// </remarks>
    public const char DefaultSpecial = '=';

    /// <inheritdoc />
    public override bool HasSpecial => true;

    /// <summary>
    /// 初始化一个 <see cref="Base64"/> 类型的实例。
    /// </summary>
    /// <param name="alphabet">Base64 编码的字符集，默认为包含大写字母、小写字母、数字以及两个额外符号 '+' 和 '/' 的标准字符集。</param>
    /// <param name="special">Base64 编码中用于填充的特殊字符，默认为 '='。</param>
    /// <param name="encoding">用于解码字符串的字符编码，默认为 UTF-8 编码。</param>
    /// <param name="parallel">指示是否使用并行编码或解码的标志，默认为 false。</param>
    public Base64(string alphabet = DefaultAlphabet, char special = DefaultSpecial, Encoding encoding = null, bool parallel = false)
        : base(64, alphabet, special, encoding, parallel)
    {
    }

    /// <inheritdoc />
    public override string Encode(byte[] data)
    {
        if (data is null || data.Length == 0)
            return string.Empty;

        var resultLength = (data.Length + 2) / 3 * 4;
        var result = new char[resultLength];

        var length3 = data.Length / 3;
        if (!Parallel)
        {
            EncodeBlock(data, result, 0, length3);
        }
        else
        {
            var processorCount = Math.Min(length3, Environment.ProcessorCount);
            System.Threading.Tasks.Parallel.For(0, processorCount, i =>
            {
                var beginInd = i * length3 / processorCount;
                var endInd = (i + 1) * length3 / processorCount;
                EncodeBlock(data, result, beginInd, endInd);
            });
        }

        int ind;
        int x1, x2;
        int srcInd, dstInd;
        switch (data.Length - length3 * 3)
        {
            case 1:
                ind = length3;
                srcInd = ind * 3;
                dstInd = ind * 4;
                x1 = data[srcInd];
                result[dstInd] = Alphabet[x1 >> 2];
                result[dstInd + 1] = Alphabet[(x1 << 4) & 0x30];
                result[dstInd + 2] = Special;
                result[dstInd + 3] = Special;
                break;
            case 2:
                ind = length3;
                srcInd = ind * 3;
                dstInd = ind * 4;
                x1 = data[srcInd];
                x2 = data[srcInd + 1];
                result[dstInd] = Alphabet[x1 >> 2];
                result[dstInd + 1] = Alphabet[((x1 << 4) & 0x30) | (x2 >> 4)];
                result[dstInd + 2] = Alphabet[(x2 << 2) & 0x3C];
                result[dstInd + 3] = Special;
                break;
        }

        return new string(result);
    }

    /// <inheritdoc />
    public override byte[] Decode(string data)
    {
        unchecked
        {
            if (string.IsNullOrEmpty(data))
                return Array.Empty<byte>();

            var lastSpecialInd = data.Length;
            while (data[lastSpecialInd - 1] == Special)
                lastSpecialInd--;
            var tailLength = data.Length - lastSpecialInd;

            var resultLength = (data.Length + 3) / 4 * 3 - tailLength;
            var result = new byte[resultLength];

            var length4 = (data.Length - tailLength) / 4;
            if (!Parallel)
            {
                DecodeBlock(data, result, 0, length4);
            }
            else
            {
                var processorCount = Math.Min(length4, Environment.ProcessorCount);
                System.Threading.Tasks.Parallel.For(0, processorCount, i =>
                {
                    var beginInd = i * length4 / processorCount;
                    var endInd = (i + 1) * length4 / processorCount;
                    DecodeBlock(data, result, beginInd, endInd);
                });
            }

            int ind;
            int x1, x2, x3;
            int srcInd, dstInd;
            switch (tailLength)
            {
                case 2:
                    ind = length4;
                    srcInd = ind * 4;
                    dstInd = ind * 3;
                    x1 = InvAlphabet[data[srcInd]];
                    x2 = InvAlphabet[data[srcInd + 1]];
                    result[dstInd] = (byte)((x1 << 2) | ((x2 >> 4) & 0x3));
                    break;
                case 1:
                    ind = length4;
                    srcInd = ind * 4;
                    dstInd = ind * 3;
                    x1 = InvAlphabet[data[srcInd]];
                    x2 = InvAlphabet[data[srcInd + 1]];
                    x3 = InvAlphabet[data[srcInd + 2]];
                    result[dstInd] = (byte)((x1 << 2) | ((x2 >> 4) & 0x3));
                    result[dstInd + 1] = (byte)((x2 << 4) | ((x3 >> 2) & 0xF));
                    break;
            }

            return result;
        }
    }

    /// <summary>
    /// 将源数组中的字节块编码为目标字符数组。
    /// </summary>
    /// <param name="src">包含要编码数据的源字节数组。</param>
    /// <param name="dst">用于存储编码数据的目标字符数组。</param>
    /// <param name="beginInd">源数组中的起始索引。</param>
    /// <param name="endInd">源数组中的结束索引。</param>
    /// <remarks>
    /// 此方法使用Base64编码方案将三个字节的块编码为四个字符。
    /// 源数组中的每个字节被分割为六位段，然后用于索引到编码字母表中以产生输出字符。
    /// </remarks>
    private void EncodeBlock(byte[] src, char[] dst, int beginInd, int endInd)
    {
        for (var ind = beginInd; ind < endInd; ind++)
        {
            var srcInd = ind * 3; // 计算此块在源数组中的起始索引。
            var dstInd = ind * 4; // 计算此块在目标数组中的起始索引。

            // 从源数组中提取三个字节。
            var x1 = src[srcInd];
            var x2 = src[srcInd + 1];
            var x3 = src[srcInd + 2];

            // 将三个字节转换为四个Base64字符。
            dst[dstInd] = Alphabet[x1 >> 2]; // 取x1的前6位。
            dst[dstInd + 1] = Alphabet[((x1 << 4) & 0x30) | (x2 >> 4)]; // 取x1的最后2位和x2的前4位。
            dst[dstInd + 2] = Alphabet[((x2 << 2) & 0x3C) | (x3 >> 6)]; // 取x2的最后4位和x3的前2位。
            dst[dstInd + 3] = Alphabet[x3 & 0x3F]; // 取x3的最后6位。
        }
    }

    /// <summary>
    /// 将源字符串中的字符块解码为目标字节数组。
    /// </summary>
    /// <param name="src">包含要解码数据的源字符串。</param>
    /// <param name="dst">用于存储解码数据的目标字节数组。</param>
    /// <param name="beginInd">源字符串中的起始索引。</param>
    /// <param name="endInd">源字符串中的结束索引。</param>
    /// <remarks>
    /// 此方法使用Base64解码方案将四个字符的块解码为三个字节。
    /// 源字符串中的每个字符被映射到Base64解码表中以产生输出字节。
    /// </remarks>
    private void DecodeBlock(string src, byte[] dst, int beginInd, int endInd)
    {
        unchecked
        {
            for (var ind = beginInd; ind < endInd; ind++)
            {
                var srcInd = ind * 4; // 计算此块在源字符串中的起始索引。
                var dstInd = ind * 3; // 计算此块在目标数组中的起始索引。

                // 从源字符串中提取四个字符。
                var x1 = InvAlphabet[src[srcInd]];
                var x2 = InvAlphabet[src[srcInd + 1]];
                var x3 = InvAlphabet[src[srcInd + 2]];
                var x4 = InvAlphabet[src[srcInd + 3]];

                // 将四个Base64字符转换为三个字节。
                dst[dstInd] = (byte)((x1 << 2) | ((x2 >> 4) & 0x3)); // 取x1的前6位和x2的前2位。
                dst[dstInd + 1] = (byte)((x2 << 4) | ((x3 >> 2) & 0xF)); // 取x2的后4位和x3的前4位。
                dst[dstInd + 2] = (byte)((x3 << 6) | (x4 & 0x3F)); // 取x3的后2位和x4的所有位。
            }
        }
    }
}