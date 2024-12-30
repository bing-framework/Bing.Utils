using Bing.Conversions;

namespace BingUtilsUT.ConvUT;

/// <summary>
/// BASE 转换工具测试
/// </summary>
[Trait("ConvUT", "BaseX")]
public class BaseConvTest
{
    private const string TestValue = "Bing.Utils";

    #region Base32

    /// <summary>
    /// 测试 - Base32 - 字符串
    /// </summary>
    [Fact]
    public void Test_Base32String()
    {
        var baseVal = BaseConv.ToBase32String(TestValue);
        var originalVal = BaseConv.FromBase32String(baseVal);

        originalVal.ShouldBe(TestValue);
    }

    /// <summary>
    /// 测试 - Base32 - 字节数组
    /// </summary>
    [Fact]
    public void Test_Base32Bytes()
    {
        var byteArray = Encoding.UTF8.GetBytes(TestValue);
        var baseVal = BaseConv.ToBase32(byteArray);

        var originalByteArray = BaseConv.FromBase32(baseVal);
        var originalVal = Encoding.UTF8.GetString(originalByteArray);

        originalVal.ShouldBe(TestValue);
    }

    #endregion

    #region Base64

    /// <summary>
    /// 测试 - Base64 - 字符串
    /// </summary>
    [Fact]
    public void Test_Base64String()
    {
        var baseVal = BaseConv.ToBase64String(TestValue);
        var originalVal = BaseConv.FromBase64String(baseVal);

        originalVal.ShouldBe(TestValue);
    }

    /// <summary>
    /// 测试 - Base64Url
    /// </summary>
    [Fact]
    public void Test_Base64UrlString()
    {
        const string input = "SGVsbG8gV29ybGQh";
        const string expected = "Hello World!";

        var actual = BaseConv.FromBase64UrlString(input);

        actual.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - Base64Url - 带 "-" 和 "_" 符号
    /// </summary>
    [Theory]
    [InlineData("WGZ-Xz0=", "Xf~_=")]
    [InlineData("bFo_ejc=", "lZ?z7")]
    public void Test_Base64UrlString_MinusAndUnderscore(string input, string expected)
    {
        BaseConv.FromBase64UrlString(input).ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - Base64Url - 缺失 "=" 符号
    /// </summary>
    [Theory]
    [InlineData("SA", "H")]
    [InlineData("SGk", "Hi")]
    public void Test_Base64UrlString_RemovedPadding(string input, string expected)
    {
        BaseConv.FromBase64UrlString(input).ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - Base64 - 字节数组
    /// </summary>
    [Fact]
    public void Test_Base64Bytes()
    {
        var byteArray = Encoding.UTF8.GetBytes(TestValue);
        var baseVal = BaseConv.ToBase64(byteArray);

        var originalByteArray = BaseConv.FromBase64(baseVal);
        var originalVal = Encoding.UTF8.GetString(originalByteArray);

        originalVal.ShouldBe(TestValue);
    }

    #endregion

    #region Base91

    /// <summary>
    /// 测试 - Base91 - 字符串
    /// </summary>
    [Fact]
    public void Test_Base91String()
    {
        var baseVal = BaseConv.ToBase91String(TestValue);
        var originalVal = BaseConv.FromBase91String(baseVal);

        originalVal.ShouldBe(TestValue);
    }

    /// <summary>
    /// 测试 - Base91 - 字节数组
    /// </summary>
    [Fact]
    public void Test_Base91Bytes()
    {
        var byteArray = Encoding.UTF8.GetBytes(TestValue);
        var baseVal = BaseConv.ToBase91(byteArray);

        var originalByteArray = BaseConv.FromBase91(baseVal);
        var originalVal = Encoding.UTF8.GetString(originalByteArray);

        originalVal.ShouldBe(TestValue);
    }

    #endregion

    #region Base256

    /// <summary>
    /// 测试 - Base256 - 字符串
    /// </summary>
    [Fact]
    public void Test_Base256String()
    {
        var baseVal = BaseConv.ToBase256String(TestValue);
        var originalVal = BaseConv.FromBase256String(baseVal);

        originalVal.ShouldBe(TestValue);
    }

    /// <summary>
    /// 测试 - Base256 - 字节数组
    /// </summary>
    [Fact]
    public void Test_Base256Bytes()
    {
        var byteArray = Encoding.UTF8.GetBytes(TestValue);
        var baseVal = BaseConv.ToBase256(byteArray);

        var originalByteArray = BaseConv.FromBase256(baseVal);
        var originalVal = Encoding.UTF8.GetString(originalByteArray);

        originalVal.ShouldBe(TestValue);
    }

    #endregion

    #region ZBase32

    /// <summary>
    /// 测试 - ZBase32 - 字符串
    /// </summary>
    [Fact]
    public void Test_ZBase32String()
    {
        var baseVal = BaseConv.ToZBase32String(TestValue);
        var originalVal = BaseConv.FromZBase32String(baseVal);

        originalVal.ShouldBe(TestValue);
    }

    /// <summary>
    /// 测试 - ZBase32 - 字节数组
    /// </summary>
    [Fact]
    public void Test_ZBase32Bytes()
    {
        var byteArray = Encoding.UTF8.GetBytes(TestValue);
        var baseVal = BaseConv.ToZBase32(byteArray);

        var originalByteArray = BaseConv.FromZBase32(baseVal);
        var originalVal = Encoding.UTF8.GetString(originalByteArray);

        originalVal.ShouldBe(TestValue);
    }

    #endregion
}

public static class Base85
{
    #region EncodeToString
    public static string EncodeToString(byte[] binary) => Encoding.ASCII.GetString(Encode(binary));
    #endregion

    #region Encode
    public static byte[] Encode(byte[] binary)
    {
        if (binary == null) { throw new ArgumentNullException(); }
        const int ASCII_OFFSET = 33;
        const byte Z = 0x7a;        // 'z'
        var loopCount = (binary.Length + 3) / 4;
        int paddingCount = (binary.Length % 4 == 0) ? 0 : 4 - binary.Length % 4;      // 0 ~ 3
        var result = new List<byte>(loopCount * 5 + 4 - paddingCount);
        result.Add(0x3c);           // '<'
        result.Add(0x7e);           // '~'
        for (int i = 0; i < loopCount; i++)
        {
            int j = i * 4;
            // ----------------------------
            uint block;
            byte ans0;
            byte ans1;
            byte? ans2 = null;
            byte? ans3 = null;
            byte? ans4 = null;
            if (i != loopCount - 1)
            {
                block = (uint)((binary[j] << 24) + (binary[j + 1] << 16) + (binary[j + 2] << 8) + binary[j + 3]);
            }
            else
            {
                block = (paddingCount == 0) ? (uint)((binary[j] << 24) + (binary[j + 1] << 16) + (binary[j + 2] << 8) + binary[j + 3]) :
                        (paddingCount == 1) ? (uint)((binary[j] << 24) + (binary[j + 1] << 16) + (binary[j + 2] << 8)) :
                        (paddingCount == 2) ? (uint)((binary[j] << 24) + (binary[j + 1] << 16)) :
                                              (uint)((binary[j] << 24));
            }
            if (block == 0)
            {
                result.Add(Z);
                continue;
            }
            uint tmp = (block / 85) * 85;
            if (i != loopCount - 1 || paddingCount < 1)
            {
                ans4 = (byte)(block - tmp + ASCII_OFFSET);
            }
            // ----------------------------
            block = tmp / 85;
            tmp = (block / 85) * 85;
            if (i != loopCount - 1 || paddingCount < 2)
            {
                ans3 = (byte)(block - tmp + ASCII_OFFSET);
            }
            // ----------------------------
            block = tmp / 85;
            tmp = (block / 85) * 85;
            if (i != loopCount - 1 || paddingCount < 3)
            {
                ans2 = (byte)(block - tmp + ASCII_OFFSET);
            }
            // ----------------------------
            block = tmp / 85;
            tmp = (block / 85) * 85;
            ans1 = (byte)(block - tmp + ASCII_OFFSET);
            // ----------------------------
            block = tmp / 85;
            tmp = (block / 85) * 85;
            ans0 = (byte)(block - tmp + ASCII_OFFSET);
            // ----------------------------
            result.Add(ans0);
            result.Add(ans1);
            if (ans2 != null) { result.Add(ans2.Value); }
            if (ans3 != null) { result.Add(ans3.Value); }
            if (ans4 != null) { result.Add(ans4.Value); }
        }
        result.Add(0x7e);   // '~'
        result.Add(0x3e);   // '>'
        return result.ToArray();
    }
    #endregion

    #region DecodeFromString
    public static byte[] DecodeFromString(string str) => Decode(Encoding.ASCII.GetBytes(str));
    #endregion

    #region Decode
    public static byte[] Decode(byte[] ascii)
    {
        if (ascii == null) { throw new ArgumentNullException(nameof(ascii)); }
        if (ascii[0] != 0x3c || ascii[1] != 0x7e || ascii[ascii.Length - 2] != 0x7e || ascii[ascii.Length - 1] != 0x3e)
        {
            throw new FormatException();
        }
        const int ASCII_OFFSET = 33;
        const int INDEX_OFFSET = 2;
        const byte PADDING_ASCII = 117;     // 'u'
        const byte Z = 0x7a;                // 'z'
        var len = ascii.Length - 4;
        var skipCount = 0;
        var result = new List<byte>(ascii.Length / 5 * 4);
        int i = 0;
        while (true)
        {
            int j = i * 5 + INDEX_OFFSET - skipCount * 4;
            if (j >= ascii.Length - INDEX_OFFSET) { break; }
            if (ascii[j] == Z)
            {
                result.Add(0x00);
                result.Add(0x00);
                result.Add(0x00);
                result.Add(0x00);
                skipCount++;
            }
            else
            {
                bool isLastLoop = (j >= ascii.Length - 5 - INDEX_OFFSET);
                uint block;
                int paddingCount = 0;
                if (!isLastLoop)
                {
                    block = (uint)(ascii[j] - ASCII_OFFSET) * 52200625 +
                            (uint)(ascii[j + 1] - ASCII_OFFSET) * 614125 +
                            (uint)(ascii[j + 2] - ASCII_OFFSET) * 7225 +
                            (uint)(ascii[j + 3] - ASCII_OFFSET) * 85 +
                            (uint)(ascii[j + 4] - ASCII_OFFSET);
                }
                else
                {
                    paddingCount = 5 - (ascii.Length - INDEX_OFFSET - j);       // 0 ~ 4
                    block = (paddingCount == 0) ? (uint)(ascii[j] - ASCII_OFFSET) * 52200625 + (uint)(ascii[j + 1] - ASCII_OFFSET) * 614125 + (uint)(ascii[j + 2] - ASCII_OFFSET) * 7225 + (uint)(ascii[j + 3] - ASCII_OFFSET) * 85 + (uint)(ascii[j + 4] - ASCII_OFFSET) :
                            (paddingCount == 1) ? (uint)(ascii[j] - ASCII_OFFSET) * 52200625 + (uint)(ascii[j + 1] - ASCII_OFFSET) * 614125 + (uint)(ascii[j + 2] - ASCII_OFFSET) * 7225 + (uint)(ascii[j + 3] - ASCII_OFFSET) * 85 + (uint)(PADDING_ASCII - ASCII_OFFSET) :
                            (paddingCount == 2) ? (uint)(ascii[j] - ASCII_OFFSET) * 52200625 + (uint)(ascii[j + 1] - ASCII_OFFSET) * 614125 + (uint)(ascii[j + 2] - ASCII_OFFSET) * 7225 + (uint)(PADDING_ASCII - ASCII_OFFSET) * 85 + (uint)(PADDING_ASCII - ASCII_OFFSET) :
                            (paddingCount == 3) ? (uint)(ascii[j] - ASCII_OFFSET) * 52200625 + (uint)(ascii[j + 1] - ASCII_OFFSET) * 614125 + (uint)(PADDING_ASCII - ASCII_OFFSET) * 7225 + (uint)(PADDING_ASCII - ASCII_OFFSET) * 85 + (uint)(PADDING_ASCII - ASCII_OFFSET) :
                                                  (uint)(ascii[j] - ASCII_OFFSET) * 52200625 + (uint)(PADDING_ASCII - ASCII_OFFSET) * 614125 + (uint)(PADDING_ASCII - ASCII_OFFSET) * 7225 + (uint)(PADDING_ASCII - ASCII_OFFSET) * 85 + (uint)(PADDING_ASCII - ASCII_OFFSET);
                }
                result.Add((byte)((block & 0xff000000) >> 24));
                if (!isLastLoop || paddingCount <= 2)
                {
                    result.Add((byte)((block & 0x00ff0000) >> 16));
                }
                if (!isLastLoop || paddingCount <= 1)
                {
                    result.Add((byte)((block & 0x0000ff00) >> 8));
                }
                if (!isLastLoop || paddingCount <= 0)
                {
                    result.Add((byte)(block & 0x000000ff));
                }
            }
            i++;
        }
        return result.ToArray();
    }
    #endregion
}