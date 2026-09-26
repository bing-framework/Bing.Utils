using System;
using System.Linq;
using System.Text;
using Xunit;

namespace Bing.Conversions;

/// <summary>
/// 验证进制转换和 Base32 编码的边界及无损往返行为。
/// </summary>
public class ConversionRegressionTest
{
    /// <summary>
    /// 验证超出有符号整数范围的进制转换会抛出溢出异常。
    /// </summary>
    /// <param name="value">待转换的十进制字符串。</param>
    [Theory]
    [InlineData("9223372036854775808")]
    [InlineData("18446744073709551616")]
    public void Radix_Overflow_IsRejected(string value) =>
        Assert.Throws<OverflowException>(() => AnyRadixConvert.X2X(value, 10, 16));

    /// <summary>
    /// 验证所有支持的进制和字符集策略均可无损往返转换。
    /// </summary>
    [Fact]
    public void Radix_AllBasesAndStrategies_RoundTrip()
    {
        foreach (var strategy in new[] { RadixCharsetStrategy.AvoidConfusion, RadixCharsetStrategy.LowerFirst })
        for (var radix = 2; radix <= 62; radix++)
        foreach (var value in new[] { "0", "1", "31", "255", "9223372036854775807" })
            Assert.Equal(value, AnyRadixConvert.X2X(AnyRadixConvert.X2X(value, 10, radix, strategy), radix, 10, strategy));
    }

    /// <summary>
    /// 验证 Crockford 字符集规则和相同进制输入校验。
    /// </summary>
    [Fact]
    public void Radix_CrockfordAndIdentity_AreValidated()
    {
        Assert.Equal("Z", AnyRadixConvert.X2X("31", 10, 32));
        Assert.Equal("V", AnyRadixConvert.X2X("27", 10, 32));
        Assert.Throws<ArgumentException>(() => AnyRadixConvert.X2X("U", 32, 10));
        Assert.Throws<ArgumentException>(() => AnyRadixConvert.X2X("2", 2, 2));
        Assert.Equal(" 001 ", AnyRadixConvert.X2X(" 001 ", 2, 2));
        Assert.Equal("", AnyRadixConvert.X2X(null, 10, 16));
    }

    /// <summary>
    /// 验证字节边界值在十六进制转换中保持不变。
    /// </summary>
    [Fact]
    public void Hex_ByteBoundaries_ArePreserved()
    {
        Assert.Equal("0102", AnyRadixConvert.DecToHex((byte)1, (byte)2));
        var bytes = new byte[] { 0, 1, 15, 16, 255 };
        var encoded = AnyRadixConvert.DecBytesToLongHex(bytes);
        Assert.Equal("00 01 0F 10 FF", encoded);
        Assert.Equal(bytes, AnyRadixConvert.LongHexToDecBytes(encoded));
        Assert.Equal("1", AnyRadixConvert.DecToHex((byte)1));
    }

    /// <summary>
    /// 验证二进制反转会保留非完整字节的有效位顺序。
    /// </summary>
    /// <param name="input">待反转的二进制字符串。</param>
    /// <param name="expected">期望的反转结果。</param>
    [Theory]
    [InlineData("1", "00000001")]
    [InlineData("101", "00000101")]
    [InlineData("10101010", "10101010")]
    [InlineData("100000001", "0000000100000001")]
    [InlineData("1010101000000001", "0000000110101010")]
    public void BinaryReverse_PreservesPartialByte(string input, string expected) => Assert.Equal(expected, Bin.Reverse(input));

    /// <summary>
    /// 验证无效 Base32 输入统一抛出格式异常。
    /// </summary>
    /// <param name="input">待解码的 Base32 字符串。</param>
    [Theory]
    [InlineData("!!!!!!!!")]
    [InlineData("========")]
    [InlineData("A")]
    [InlineData("MY")]
    [InlineData("my======")]
    [InlineData("MY=====A")]
    [InlineData("MY======AAAAAAAA")]
    [InlineData("AAA=====")]
    [InlineData("MZ======")]
    [InlineData("MZXR====")]
    [InlineData("MZXW7===")]
    [InlineData("MZXW6YR=")]
    [InlineData("汉AAAAAAA")]
    public void Base32_InvalidInput_IsFormatException(string input) =>
        Assert.Throws<FormatException>(() => BaseConv.FromBase32(input));

    /// <summary>
    /// 验证 Base32 已知向量可正确编码和解码。
    /// </summary>
    /// <param name="plain">待编码的明文字符串。</param>
    /// <param name="encoded">期望的 Base32 编码结果。</param>
    [Theory]
    [InlineData("", "")]
    [InlineData("f", "MY======")]
    [InlineData("fo", "MZXQ====")]
    [InlineData("foo", "MZXW6===")]
    [InlineData("foob", "MZXW6YQ=")]
    [InlineData("fooba", "MZXW6YTB")]
    [InlineData("foobar", "MZXW6YTBOI======")]
    public void Base32_KnownVectors_RoundTrip(string plain, string encoded)
    {
        var bytes = Encoding.UTF8.GetBytes(plain);
        Assert.Equal(encoded, BaseConv.ToBase32(bytes));
        Assert.Equal(bytes, BaseConv.FromBase32(encoded));
    }

    /// <summary>
    /// 验证全部字节数据及不同尾长度均可进行 Base32 无损往返。
    /// </summary>
    [Fact]
    public void Base32_AllBytesAndTailLengths_RoundTrip()
    {
        var bytes = Enumerable.Range(0, 256).Select(x => (byte)x).ToArray();
        for (var length = 0; length <= bytes.Length; length++)
        {
            var input = bytes.Take(length).ToArray();
            Assert.Equal(input, BaseConv.FromBase32(BaseConv.ToBase32(input)));
        }
        Assert.Empty(BaseConv.FromBase32(null));
    }
}
