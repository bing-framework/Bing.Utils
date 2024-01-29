using System.Text.RegularExpressions;

namespace Bing.Text.Internals;

/// <summary>
/// 提供基本的Base-X编码/解码的核心功能。
/// </summary>
internal abstract class BaseXCore
{
    /// <summary>
    /// 获取字符集的数量。
    /// </summary>
    public uint CharsCount { get; }

    /// <summary>
    /// 获取每个字符所占用的位数。
    /// </summary>
    public double BitsPerChars => (double)BlockBitsCount / BlockCharsCount;

    /// <summary>
    /// 获取或设置编码块的位数。
    /// </summary>
    public int BlockBitsCount { get; protected set; }

    /// <summary>
    /// 获取或设置编码块的字符数。
    /// </summary>
    public int BlockCharsCount { get; protected set; }

    /// <summary>
    /// 获取字符集。
    /// </summary>
    public string Alphabet { get; }

    /// <summary>
    /// 获取用于特殊字符的标记。
    /// </summary>
    public char Special { get; }

    /// <summary>
    /// 获取是否包含特殊字符。
    /// </summary>
    public abstract bool HasSpecial { get; }

    /// <summary>
    /// 获取或设置用于字符编码的编码方式。
    /// </summary>
    public Encoding Encoding { get; set; }

    /// <summary>
    /// 获取或设置是否并行进行编码/解码操作。
    /// </summary>
    public bool Parallel { get; set; }

    /// <summary>
    /// 获取字符集的反向索引数组。
    /// </summary>
    protected readonly int[] InvAlphabet;

    /// <summary>
    /// 初始化 <see cref="BaseXCore"/> 类的新实例。
    /// </summary>
    /// <param name="charsCount">字符集的数量。</param>
    /// <param name="alphabet">字符集。</param>
    /// <param name="special">特殊字符标记。</param>
    /// <param name="encoding">用于字符编码的编码方式。</param>
    /// <param name="parallel">指示是否并行进行编码/解码操作。</param>
    public BaseXCore(uint charsCount, string alphabet, char special, Encoding encoding = null, bool parallel = false)
    {
        if (alphabet.Length != charsCount)
            throw new ArgumentException($"Base string should contain {charsCount} chars.");
        for (var i = 0; i < charsCount; i++)
            for (var j = i + 1; j < charsCount; j++)
                if (alphabet[i] == alphabet[j])
                    throw new ArgumentException("Base string should contain distinct chars.");
        if (alphabet.Contains(special))
            throw new ArgumentException("Base string should not contain special char.");
        CharsCount = charsCount;
        Alphabet = alphabet;
        Special = special;

        var bitsPerChar = LogBase2(charsCount);
        BlockBitsCount = Lcm(bitsPerChar, 8);
        BlockCharsCount = BlockBitsCount / bitsPerChar;

        InvAlphabet = new int[Alphabet.Max() + 1];

        for (var i = 0; i < InvAlphabet.Length; i++)
            InvAlphabet[i] = -1;

        for (var i = 0; i < charsCount; i++)
            InvAlphabet[Alphabet[i]] = i;
        Encoding = encoding ?? Encoding.UTF8;
        Parallel = parallel;
    }

    /// <summary>
    /// 将字符串进行编码。
    /// </summary>
    /// <param name="data">要编码的字符串。</param>
    /// <returns>编码后的字符串。</returns>
    public virtual string EncodeString(string data) => Encode(Encoding.GetBytes(data));

    /// <summary>
    /// 将字节数组进行编码。
    /// </summary>
    /// <param name="data">要编码的字节数组。</param>
    /// <returns>编码后的字符串。</returns>
    public abstract string Encode(byte[] data);

    /// <summary>
    /// 将编码后的字符串解码为字符串形式。
    /// </summary>
    /// <param name="data">要解码的字符串。</param>
    /// <returns>解码后的字符串。</returns>
    public string DecodeToString(string data) => Encoding.GetString(Decode(Regex.Replace(data, @"\r\n?|\n", "")));

    /// <summary>
    /// 将编码后的字符串解码为字节数组。
    /// </summary>
    /// <param name="data">要解码的字符串。</param>
    /// <returns>解码后的字节数组。</returns>
    public abstract byte[] Decode(string data);

    /// <summary>
    /// 判断一个无符号整数是否是2的幂。
    /// </summary>
    /// <param name="x">要判断的无符号整数。</param>
    /// <returns>如果是2的幂则为true，否则为false。</returns>
    public static bool IsPowerOf2(uint x)
    {
        var xInt = x;
        if (x - xInt != 0)
            return false;
        return (xInt & (xInt - 1)) == 0;
    }

    /// <summary>
    /// 计算两个整数的最小公倍数。
    /// </summary>
    /// <param name="a">第一个整数。</param>
    /// <param name="b">第二个整数。</param>
    /// <returns>最小公倍数。</returns>
    public static int Lcm(int a, int b)
    {
        int num1, num2;
        if (a > b)
        {
            num1 = a;
            num2 = b;
        }
        else
        {
            num1 = b;
            num2 = a;
        }

        for (var i = 1; i <= num2; i++)
        {
            var mult = num1 * i;
            if (mult % num2 == 0)
                return mult;
        }

        return num2;
    }

    /// <summary>
    /// 计算一个无符号整数以2为底的对数。
    /// </summary>
    /// <param name="x">要计算对数的无符号整数。</param>
    /// <returns>以2为底的对数。</returns>
    private static int LogBase2(uint x)
    {
        var r = 0;
        while ((x >>= 1) != 0)
            r++;
        return r;
    }

    /// <summary>
    /// 计算一个无符号整数以指定基数为底的对数。
    /// </summary>
    /// <param name="x">要计算对数的无符号整数。</param>
    /// <param name="n">指定的基数。</param>
    /// <returns>以指定基数为底的对数。</returns>
    private static int LogBaseN(uint x, uint n)
    {
        var r = 0;
        while ((x /= n) != 0)
            r++;
        return r;
    }

    /// <summary>
    /// 获取使字符集以指定的位数进行编码时，位数的最佳选择。
    /// </summary>
    /// <param name="charsCount">字符集的数量。</param>
    /// <param name="charsCountInBits">输出参数，字符集编码后的位数。</param>
    /// <param name="maxBitsCount">最大位数，默认为64。</param>
    /// <param name="base2BitsCount">指示是否要求位数是2的倍数，默认为false。</param>
    /// <returns>最佳的位数。</returns>
    public static int GetOptimalBitsCount2(uint charsCount, out uint charsCountInBits, uint maxBitsCount = 64, bool base2BitsCount = false)
    {
        var result = 0;
        charsCountInBits = 0;
        var n1 = LogBase2(charsCount);
        var charsCountLog = Math.Log(2, charsCount);
        double maxRatio = 0;

        for (var n = n1; n <= maxBitsCount; n++)
        {
            if (base2BitsCount && n % 8 != 0)
                continue;
            var l1 = (uint)Math.Ceiling(n * charsCountLog);
            var ratio = (double)n / l1;
            if (ratio > maxRatio)
            {
                maxRatio = ratio;
                result = n;
                charsCountInBits = l1;
            }
        }
        return result;
    }

    /// <summary>
    /// 获取使字符集以指定的基数进行编码时，位数的最佳选择。
    /// </summary>
    /// <param name="charsCount">字符集的数量。</param>
    /// <param name="charsCountInBits">输出参数，字符集编码后的位数。</param>
    /// <param name="maxBitsCount">最大位数，默认为64。</param>
    /// <param name="radix">指定的基数，默认为2。</param>
    /// <returns>最佳的位数。</returns>
    protected static int GetOptimalBitsCount(uint charsCount, out uint charsCountInBits, uint maxBitsCount = 64, uint radix = 2)
    {
        var result = 0;
        charsCountInBits = 0;
        var n0 = LogBaseN(charsCount, radix);
        var charsCountLog = Math.Log(radix, charsCount);
        double maxRatio = 0;

        for (var n = n0; n <= maxBitsCount; n++)
        {
            var k = (uint)Math.Ceiling(n * charsCountLog);
            var ratio = (double)n / k;
            if (ratio > maxRatio)
            {
                maxRatio = ratio;
                result = n;
                charsCountInBits = k;
            }
        }
        return result;
    }
}