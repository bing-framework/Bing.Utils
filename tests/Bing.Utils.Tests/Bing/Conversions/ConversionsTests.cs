using Bing.Conversions;

namespace Bing.Utils.Tests.Bing.Conversions;

/// <summary>
/// 进制转换测试 — AnyRadixConvert / Hex / AsciiConv / BaseConv
/// </summary>
[Trait("Bing.Conversions", "RadixAndBaseConversions")]
public class ConversionsTests
{
    #region AnyRadixConvert — BinToXxx

    /// <summary>
    /// 验证二进制文本转换为八进制文本。
    /// </summary>
    [Fact]
    public void BinToOct_ValidBinary_ReturnsOctal()
    {
        AnyRadixConvert.BinToOct("101110").ShouldBe("56");
    }

    /// <summary>
    /// 验证二进制文本转换为十进制数值。
    /// </summary>
    [Fact]
    public void BinToDec_ValidBinary_ReturnsDecimal()
    {
        AnyRadixConvert.BinToDec("101110").ShouldBe(46);
    }

    /// <summary>
    /// 验证二进制文本转换为十六进制文本。
    /// </summary>
    [Fact]
    public void BinToHex_ValidBinary_ReturnsHex()
    {
        AnyRadixConvert.BinToHex("101110").ShouldBe("2E");
    }

    #endregion

    #region AnyRadixConvert — OctToXxx

    /// <summary>
    /// 验证八进制文本转换为二进制文本。
    /// </summary>
    [Fact]
    public void OctToBin_ValidOctal_ReturnsBinary()
    {
        AnyRadixConvert.OctToBin("140").ShouldBe("1100000");
    }

    /// <summary>
    /// 验证八进制文本转换为十进制数值。
    /// </summary>
    [Fact]
    public void OctToDec_ValidOctal_ReturnsDecimal()
    {
        AnyRadixConvert.OctToDec("140").ShouldBe(96);
    }

    /// <summary>
    /// 验证八进制文本转换为十六进制文本。
    /// </summary>
    [Fact]
    public void OctToHex_ValidOctal_ReturnsHex()
    {
        AnyRadixConvert.OctToHex("140").ShouldBe("60");
    }

    #endregion

    #region AnyRadixConvert — DecToXxx

    /// <summary>
    /// 验证字节数值转换为二进制文本。
    /// </summary>
    [Fact]
    public void DecToBin_ByteInput_ReturnsBinary()
    {
        AnyRadixConvert.DecToBin((byte)46).ShouldBe("101110");
    }

    /// <summary>
    /// 验证十进制文本转换为二进制文本。
    /// </summary>
    [Fact]
    public void DecToBin_StringInput_ReturnsBinary()
    {
        AnyRadixConvert.DecToBin("46").ShouldBe("101110");
    }

    /// <summary>
    /// 验证字节数值转换为八进制文本。
    /// </summary>
    [Fact]
    public void DecToOct_ByteInput_ReturnsOctal()
    {
        AnyRadixConvert.DecToOct((byte)128).ShouldBe("200");
    }

    /// <summary>
    /// 验证十进制文本转换为八进制文本。
    /// </summary>
    [Fact]
    public void DecToOct_StringInput_ReturnsOctal()
    {
        AnyRadixConvert.DecToOct("128").ShouldBe("200");
    }

    /// <summary>
    /// 验证字节数值转换为十六进制文本。
    /// </summary>
    [Fact]
    public void DecToHex_ByteInput_ReturnsHex()
    {
        AnyRadixConvert.DecToHex((byte)128).ShouldBe("80");
    }

    /// <summary>
    /// 验证十进制文本转换为十六进制文本。
    /// </summary>
    [Fact]
    public void DecToHex_StringInput_ReturnsHex()
    {
        AnyRadixConvert.DecToHex("46").ShouldBe("2E");
    }

    /// <summary>
    /// 验证十六进制文本按指定长度左侧补零。
    /// </summary>
    [Fact]
    public void DecToHex_WithFormatLength_PadsLeft()
    {
        AnyRadixConvert.DecToHex("46", 4).ShouldBe("002E");
    }

    /// <summary>
    /// 验证高低字节按顺序组成十六进制文本。
    /// </summary>
    [Fact]
    public void DecToHex_HighLowByte_ReturnsCombined()
    {
        AnyRadixConvert.DecToHex((byte)65, (byte)66).ShouldBe("4142");
    }

    /// <summary>
    /// 验证字节数组转换为以空格分隔的十六进制文本。
    /// </summary>
    [Fact]
    public void DecBytesToLongHex_ByteArray_ReturnsSeparatedHex()
    {
        var result = AnyRadixConvert.DecBytesToLongHex(new byte[] { 65, 66, 67 });
        result.ShouldBe("41 42 43");
    }

    #endregion

    #region AnyRadixConvert — HexToXxx

    /// <summary>
    /// 验证十六进制文本转换为二进制文本。
    /// </summary>
    [Fact]
    public void HexToBin_ValidHex_ReturnsBinary()
    {
        AnyRadixConvert.HexToBin("2E").ShouldBe("101110");
    }

    /// <summary>
    /// 验证十六进制文本转换为八进制文本。
    /// </summary>
    [Fact]
    public void HexToOct_ValidHex_ReturnsOctal()
    {
        AnyRadixConvert.HexToOct("2E").ShouldBe("56");
    }

    /// <summary>
    /// 验证十六进制文本转换为十进制文本。
    /// </summary>
    [Fact]
    public void HexToDec_ValidHex_ReturnsDecimal()
    {
        AnyRadixConvert.HexToDec("2E").ShouldBe("46");
    }

    /// <summary>
    /// 验证 ASCII 字符转换为对应的十六进制文本。
    /// </summary>
    [Fact]
    public void LettersToHex_AsciiString_ReturnsHexWithSpaces()
    {
        var result = AnyRadixConvert.LettersToHex("A");
        // 'A' = 0x41
        result.ShouldBe("41");
    }

    /// <summary>
    /// 验证十六进制文本转换为对应的 ASCII 字符。
    /// </summary>
    [Fact]
    public void HexToLetters_ValidHex_ReturnsString()
    {
        var result = AnyRadixConvert.HexToLetters("41");
        result.ShouldBe("A");
    }

    /// <summary>
    /// 验证连续十六进制文本解码为字节数组。
    /// </summary>
    [Fact]
    public void LongHexToDecBytes_ValidHex_ReturnsByteArray()
    {
        var result = AnyRadixConvert.LongHexToDecBytes("2E3D");
        result[0].ShouldBe((byte)46);
        result[1].ShouldBe((byte)61);
    }

    #endregion

    #region AnyRadixConvert — X2X

    /// <summary>
    /// 验证指定源进制和目标进制的二进制转十进制转换。
    /// </summary>
    [Fact]
    public void X2X_BinaryToDecimal_ReturnsCorrect()
    {
        AnyRadixConvert.X2X("101110", 2, 10).ShouldBe("46");
    }

    /// <summary>
    /// 验证指定源进制和目标进制的十进制转十六进制转换。
    /// </summary>
    [Fact]
    public void X2X_DecimalToHex_ReturnsCorrect()
    {
        AnyRadixConvert.X2X("46", 10, 16).ShouldBe("2E");
    }

    #endregion

    #region Bin.Reverse

    /// <summary>
    /// 验证相同的八字符分组反转后文本保持一致。
    /// </summary>
    [Fact]
    public void Bin_Reverse_ReversesGroupsOf8()
    {
        // Reverse swaps groups of 'size' chars
        var result = Bin.Reverse("1234567812345678");
        // Groups: "12345678" and "12345678" swapped
        result.ShouldBe("1234567812345678");
    }

    #endregion

    #region Hex

    /// <summary>
    /// 验证单个字节编码为大写十六进制文本。
    /// </summary>
    [Fact]
    public void Hex_ToString_Byte_ReturnsUpperHex()
    {
        Hex.ToString((byte)128).ShouldBe("80");
    }

    /// <summary>
    /// 验证字节数组编码为连续十六进制文本。
    /// </summary>
    [Fact]
    public void Hex_ToString_ByteArray_ReturnsConcatenatedHex()
    {
        Hex.ToString(new byte[] { 65, 66, 67 }).ShouldBe("414243");
    }

    /// <summary>
    /// 验证十六进制文本解码为对应字节数组。
    /// </summary>
    [Fact]
    public void Hex_ToBytes_ValidHex_ReturnsByteArray()
    {
        var bytes = Hex.ToBytes("414243");
        bytes.ShouldBe(new byte[] { 65, 66, 67 });
    }

    /// <summary>
    /// 验证奇数长度十六进制文本补齐后解码。
    /// </summary>
    [Fact]
    public void Hex_ToBytes_OddLength_PadsLeft()
    {
        var bytes = Hex.ToBytes("F");
        bytes[0].ShouldBe((byte)15);
    }

    /// <summary>
    /// 验证空十六进制引用解码为单个零字节。
    /// </summary>
    [Fact]
    public void Hex_ToBytes_Null_ReturnsZeroByte()
    {
        Hex.ToBytes(null).ShouldBe(new byte[] { 0 });
    }

    /// <summary>
    /// 验证十六进制反转交换字节顺序。
    /// </summary>
    [Fact]
    public void Hex_Reverse_TwoByteString_SwapsBytes()
    {
        // Reverse swaps 2-char groups: "4142" → "4241"
        Hex.Reverse("4142").ShouldBe("4241");
    }

    /// <summary>
    /// 验证字节数组扩展生成连续十六进制文本。
    /// </summary>
    [Fact]
    public void HexExtensions_CastToHexString_ReturnsConcatenatedHex()
    {
        new byte[] { 65, 66, 67 }.CastToHexString().ShouldBe("414243");
    }

    /// <summary>
    /// 验证十六进制文本扩展解码为字节数组。
    /// </summary>
    [Fact]
    public void HexExtensions_CastToHexBytes_ReturnsBytes()
    {
        "414243".CastToHexBytes().ShouldBe(new byte[] { 65, 66, 67 });
    }

    #endregion

    #region AsciiConv

    /// <summary>
    /// 验证 ASCII 字节数组解码为文本。
    /// </summary>
    [Fact]
    public void AsciiConv_BytesToAsciiString_ReturnsString()
    {
        AsciiConv.BytesToAsciiString(new byte[] { 65, 66, 67 }).ShouldBe("ABC");
    }

    /// <summary>
    /// 验证 ASCII 文本编码为字节数组。
    /// </summary>
    [Fact]
    public void AsciiConv_AsciiStringToBytes_ReturnsByteArray()
    {
        AsciiConv.AsciiStringToBytes("ABC").ShouldBe(new byte[] { 65, 66, 67 });
    }

    /// <summary>
    /// 验证 ASCII 编解码保留原文本。
    /// </summary>
    [Fact]
    public void AsciiConv_RoundTrip_PreservesOriginal()
    {
        var original = "Hello";
        var bytes = AsciiConv.AsciiStringToBytes(original);
        AsciiConv.BytesToAsciiString(bytes).ShouldBe(original);
    }

    #endregion

    #region BaseConv — Base32

    /// <summary>
    /// 验证 Base32 编解码保留原字节数组。
    /// </summary>
    [Fact]
    public void BaseConv_ToBase32_FromBase32_RoundTrip()
    {
        var data = new byte[] { 1, 2, 3, 4, 5 };
        var encoded = BaseConv.ToBase32(data);
        encoded.ShouldNotBeNullOrEmpty();
        var decoded = BaseConv.FromBase32(encoded);
        decoded.ShouldBe(data);
    }

    /// <summary>
    /// 验证 Base32 编解码保留原文本。
    /// </summary>
    [Fact]
    public void BaseConv_ToBase32String_FromBase32String_RoundTrip()
    {
        var original = "Hello World";
        var encoded = BaseConv.ToBase32String(original);
        encoded.ShouldNotBeNullOrEmpty();
        var decoded = BaseConv.FromBase32String(encoded);
        decoded.ShouldBe(original);
    }

    #endregion

    #region BaseConv — ZBase32

    /// <summary>
    /// 验证 ZBase32 编解码保留原字节数组。
    /// </summary>
    [Fact]
    public void BaseConv_ToZBase32_FromZBase32_RoundTrip()
    {
        var data = new byte[] { 10, 20, 30 };
        var encoded = BaseConv.ToZBase32(data);
        encoded.ShouldNotBeNullOrEmpty();
        var decoded = BaseConv.FromZBase32(encoded);
        decoded.ShouldBe(data);
    }

    /// <summary>
    /// 验证 ZBase32 编解码保留原文本。
    /// </summary>
    [Fact]
    public void BaseConv_ToZBase32String_FromZBase32String_RoundTrip()
    {
        var original = "Test";
        var encoded = BaseConv.ToZBase32String(original);
        var decoded = BaseConv.FromZBase32String(encoded);
        decoded.ShouldBe(original);
    }

    #endregion

    #region BaseConv — Base64

    /// <summary>
    /// 验证 Base64 编解码保留原字节数组。
    /// </summary>
    [Fact]
    public void BaseConv_ToBase64_FromBase64_RoundTrip()
    {
        var data = new byte[] { 72, 101, 108, 108, 111 };
        var encoded = BaseConv.ToBase64(data);
        var decoded = BaseConv.FromBase64(encoded);
        decoded.ShouldBe(data);
    }

    /// <summary>
    /// 验证 Base64 编解码保留原文本。
    /// </summary>
    [Fact]
    public void BaseConv_ToBase64String_FromBase64String_RoundTrip()
    {
        var original = "Hello World";
        var encoded = BaseConv.ToBase64String(original);
        var decoded = BaseConv.FromBase64String(encoded);
        decoded.ShouldBe(original);
    }

    /// <summary>
    /// 验证 URL 安全 Base64 不含保留符号且可还原原文本。
    /// </summary>
    [Fact]
    public void BaseConv_ToBase64UrlString_FromBase64UrlString_RoundTrip()
    {
        var original = "Hello World!";
        var encoded = BaseConv.ToBase64UrlString(original);
        // URL safe: no +, /, = characters
        encoded.ShouldNotContain("+");
        encoded.ShouldNotContain("/");
        encoded.ShouldNotContain("=");
        var decoded = BaseConv.FromBase64UrlString(encoded);
        decoded.ShouldBe(original);
    }

    #endregion

    #region BaseConv — Base91

    /// <summary>
    /// 验证 Base91 编解码保留原字节数组。
    /// </summary>
    [Fact]
    public void BaseConv_ToBase91_FromBase91_RoundTrip()
    {
        var data = new byte[] { 1, 2, 3, 100, 200 };
        var encoded = BaseConv.ToBase91(data);
        encoded.ShouldNotBeNullOrEmpty();
        var decoded = BaseConv.FromBase91(encoded);
        decoded.ShouldBe(data);
    }

    /// <summary>
    /// 验证 Base91 编解码保留原文本。
    /// </summary>
    [Fact]
    public void BaseConv_ToBase91String_FromBase91String_RoundTrip()
    {
        var original = "Test123";
        var encoded = BaseConv.ToBase91String(original);
        encoded.ShouldNotBeNullOrEmpty();
        var decoded = BaseConv.FromBase91String(encoded);
        decoded.ShouldBe(original);
    }

    #endregion
}
