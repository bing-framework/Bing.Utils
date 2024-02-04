using Bing.Conversions;

namespace BingUtilsUT.ConvUT;

/// <summary>
/// 任意[2,62]进制转换器 测试
/// </summary>
[Trait("ConvUT", "AnyRadixConvert")]
public class AnyRadixConvertTest
{
    #region X2X(任意进制)

    /// <summary>
    /// 测试 - 常用进制转换
    /// </summary>
    [Fact]
    public void Test_X2X_SystemToSystem()
    {
        // 2 -> [8,10,16]
        AnyRadixConvert.X2X("101110", 2, 8).ShouldBe("56");
        AnyRadixConvert.X2X("101110", 2, 10).ShouldBe("46");
        AnyRadixConvert.X2X("101110", 2, 16).ShouldBe("2E");
        // 8 -> [2,10,16]
        AnyRadixConvert.X2X("140", 8, 2).ShouldBe("1100000");
        AnyRadixConvert.X2X("140", 8, 10).ShouldBe("96");
        AnyRadixConvert.X2X("140", 8, 16).ShouldBe("60");
        // 10 -> [2,8,16]
        AnyRadixConvert.X2X("128", 10, 2).ShouldBe("10000000");
        AnyRadixConvert.X2X("128", 10, 8).ShouldBe("200");
        AnyRadixConvert.X2X("128", 10, 16).ShouldBe("80");
        // 16 -> [2,8,10]
        AnyRadixConvert.X2X("2E", 16, 2).ShouldBe("101110");
        AnyRadixConvert.X2X("2E", 16, 8).ShouldBe("56");
        AnyRadixConvert.X2X("2E", 16, 10).ShouldBe("46");
    }

    /// <summary>
    /// 测试 - 相同进制
    /// </summary>
    [Fact]
    public void Test_X2X_SystemSamToSystemSam()
    {
        AnyRadixConvert.X2X("101110", 2, 2).ShouldBe("101110");
        AnyRadixConvert.X2X("101110", 3, 3).ShouldBe("101110");
        AnyRadixConvert.X2X("101110", 4, 4).ShouldBe("101110");
        AnyRadixConvert.X2X("101110", 5, 5).ShouldBe("101110");
        AnyRadixConvert.X2X("101110", 6, 6).ShouldBe("101110");
        AnyRadixConvert.X2X("101110", 7, 7).ShouldBe("101110");
        AnyRadixConvert.X2X("101110", 8, 8).ShouldBe("101110");
        AnyRadixConvert.X2X("101110", 9, 9).ShouldBe("101110");
        AnyRadixConvert.X2X("101110", 10, 10).ShouldBe("101110");
        AnyRadixConvert.X2X("101110", 11, 11).ShouldBe("101110");
        AnyRadixConvert.X2X("101110", 12, 12).ShouldBe("101110");
        AnyRadixConvert.X2X("101110", 13, 13).ShouldBe("101110");
        AnyRadixConvert.X2X("101110", 14, 14).ShouldBe("101110");
        AnyRadixConvert.X2X("101110", 15, 15).ShouldBe("101110");
        AnyRadixConvert.X2X("101110", 16, 16).ShouldBe("101110");
        AnyRadixConvert.X2X("101110", 17, 17).ShouldBe("101110");
        AnyRadixConvert.X2X("101110", 18, 18).ShouldBe("101110");
        AnyRadixConvert.X2X("101110", 19, 19).ShouldBe("101110");
        AnyRadixConvert.X2X("101110", 20, 20).ShouldBe("101110");
        AnyRadixConvert.X2X("101110", 21, 21).ShouldBe("101110");
        AnyRadixConvert.X2X("101110", 22, 22).ShouldBe("101110");
        AnyRadixConvert.X2X("101110", 23, 23).ShouldBe("101110");
        AnyRadixConvert.X2X("101110", 24, 24).ShouldBe("101110");
        AnyRadixConvert.X2X("101110", 25, 25).ShouldBe("101110");
        AnyRadixConvert.X2X("101110", 26, 26).ShouldBe("101110");
        AnyRadixConvert.X2X("101110", 27, 27).ShouldBe("101110");
        AnyRadixConvert.X2X("101110", 28, 28).ShouldBe("101110");
        AnyRadixConvert.X2X("101110", 29, 29).ShouldBe("101110");
        AnyRadixConvert.X2X("101110", 30, 30).ShouldBe("101110");
        AnyRadixConvert.X2X("101110", 31, 31).ShouldBe("101110");
        AnyRadixConvert.X2X("101110", 32, 32).ShouldBe("101110");
        AnyRadixConvert.X2X("101110", 33, 33).ShouldBe("101110");
        AnyRadixConvert.X2X("101110", 34, 34).ShouldBe("101110");
        AnyRadixConvert.X2X("101110", 35, 35).ShouldBe("101110");
        AnyRadixConvert.X2X("101110", 36, 36).ShouldBe("101110");
    }

    /// <summary>
    /// 测试 - 任意进制
    /// </summary>
    [Theory]
    [InlineData(10, 32, "32", "10")]
    [InlineData(10, 33, "32", "w")]
    [InlineData(10, 34, "32", "w")]
    [InlineData(10, 35, "32", "w")]
    [InlineData(10, 36, "32", "w")]
    public void Test_X2X_Any_LowerFirst(int fromRadix, int toRadix, string input, string result)
    {
        AnyRadixConvert.X2X(input, fromRadix, toRadix, RadixCharsetStrategy.LowerFirst).ShouldBe(result);
    }

    #endregion


    #region Binary(二进制)

    /// <summary>
    /// 测试 - 二进制值转换为八进制值
    /// </summary>
    [Fact]
    public void Test_BinToOct()
    {
        AnyRadixConvert.BinToOct("101110").ShouldBe("56");
    }

    /// <summary>
    /// 测试 - 二进制值转换为十进制值
    /// </summary>
    [Fact]
    public void Test_BinToDec()
    {
        AnyRadixConvert.BinToDec("101110").ShouldBe(46);
    }

    /// <summary>
    /// 测试 - 二进制值转换为十六进制值
    /// </summary>
    [Fact]
    public void Test_BinToHex()
    {
        AnyRadixConvert.BinToHex("101110").ShouldBe("2E");
    }

    /// <summary>
    /// 测试 - 二进制值转换为八进制值 - 对比 - 系统转换
    /// </summary>
    [Fact]
    public void Test_BinToOct_VS_SystemConvert()
    {
        var bin = "101110";
        var decimalValue = Convert.ToInt32(bin, 2);

        AnyRadixConvert.BinToOct(bin).ShouldBe("56");
        Convert.ToString(decimalValue, 8).ShouldBe("56");
    }

    /// <summary>
    /// 测试 - 二进制值转换为十进制值 - 对比 - 系统转换
    /// </summary>
    [Fact]
    public void Test_BinToDec_VS_SystemConvert()
    {
        var bin = "101110";

        AnyRadixConvert.BinToDec(bin).ShouldBe(46);
        Convert.ToInt32(bin, 2).ShouldBe(46);
    }

    /// <summary>
    /// 测试 - 二进制值转换为八进制值 - 对比 - 系统转换
    /// </summary>
    [Fact]
    public void Test_BinToHex_VS_SystemConvert()
    {
        var bin = "101110";
        var decimalValue = Convert.ToInt32(bin, 2);

        AnyRadixConvert.BinToHex(bin).ShouldBe("2E");
        Convert.ToString(decimalValue, 16).ToUpper().ShouldBe("2E");
    }

    /// <summary>
    /// 测试 - 二进制 - 高低位交换
    /// </summary>
    [Fact]
    public void Test_Bin_Reverse()
    {
        Bin.Reverse("1101110100000011010110100001110").ShouldBe("00001110101011011000000101101110");
        Bin.Reverse("01101110100000011010110100001110").ShouldBe("00001110101011011000000101101110");
    }

    #endregion

    #region Octal(八进制)

    /// <summary>
    /// 测试 - 八进制值转换为二进制值
    /// </summary>
    [Fact]
    public void Test_OctToBin()
    {
        AnyRadixConvert.OctToBin("56").ShouldBe("101110");
    }

    /// <summary>
    /// 测试 - 二进制值转换为十进制值
    /// </summary>
    [Fact]
    public void Test_OctToDec()
    {
        AnyRadixConvert.OctToDec("56").ShouldBe(46);
    }

    /// <summary>
    /// 测试 - 二进制值转换为十六进制值
    /// </summary>
    [Fact]
    public void Test_OctToHex()
    {
        AnyRadixConvert.OctToHex("56").ShouldBe("2E");
    }

    /// <summary>
    /// 测试 - 八进制值转换为二进制值 - 对比 - 系统转换
    /// </summary>
    [Fact]
    public void Test_OctToBin_VS_SystemConvert()
    {
        var oct = "56";
        var decimalValue = Convert.ToInt32(oct, 8);

        AnyRadixConvert.OctToBin(oct).ShouldBe("101110");
        Convert.ToString(decimalValue, 2).ShouldBe("101110");
    }

    /// <summary>
    /// 测试 - 八进制值转换为十进制值 - 对比 - 系统转换
    /// </summary>
    [Fact]
    public void Test_OctToDec_VS_SystemConvert()
    {
        var oct = "56";

        AnyRadixConvert.OctToDec(oct).ShouldBe(46);
        Convert.ToInt32(oct, 8).ShouldBe(46);
    }

    /// <summary>
    /// 测试 - 八进制值转换为八进制值 - 对比 - 系统转换
    /// </summary>
    [Fact]
    public void Test_OctToHex_VS_SystemConvert()
    {
        var oct = "56";
        var decimalValue = Convert.ToInt32(oct, 8);

        AnyRadixConvert.OctToHex(oct).ShouldBe("2E");
        Convert.ToString(decimalValue, 16).ToUpper().ShouldBe("2E");
    }

    #endregion

    #region Decimal(十进制)

    /// <summary>
    /// 测试 - 十进制值转换为二进制值
    /// </summary>
    [Fact]
    public void Test_DecToBin()
    {
        AnyRadixConvert.DecToBin("46").ShouldBe("101110");
        AnyRadixConvert.DecToBin(46).ShouldBe("101110");
    }

    /// <summary>
    /// 测试 - 十进制值转换为十六进制值
    /// </summary>
    [Fact]
    public void Test_DecToHex()
    {
        AnyRadixConvert.DecToHex("46").ShouldBe("2E");
        AnyRadixConvert.DecToHex("46", 4).ShouldBe("002E");
        AnyRadixConvert.DecToHex(46).ShouldBe("2E");
        AnyRadixConvert.DecToHex(65, 66).ShouldBe("4142");
        AnyRadixConvert.DecToHex(66, 65).ShouldBe("4241");
    }

    /// <summary>
    /// 测试 - 十进制值转换为二进制值 - 对比 - 系统转换
    /// </summary>
    [Fact]
    public void Test_DecToBin_VS_SystemConvert()
    {
        AnyRadixConvert.DecToBin("46").ShouldBe("101110");
        Convert.ToString(46, 2).ShouldBe("101110");
    }

    /// <summary>
    /// 测试 - 十进制值转换为八进制值 - 对比 - 系统转换
    /// </summary>
    [Fact]
    public void Test_DecToOct_VS_SystemConvert()
    {
        AnyRadixConvert.DecToOct("46").ShouldBe("56");
        Convert.ToString(46, 8).ShouldBe("56");
    }

    /// <summary>
    /// 测试 - 十进制值转换为十六进制值 - 对比 - 系统转换
    /// </summary>
    [Fact]
    public void Test_DecToHex_VS_SystemConvert()
    {
        AnyRadixConvert.DecToHex("46").ShouldBe("2E");
        Convert.ToString(46, 16).ToUpper().ShouldBe("2E");
    }

    #endregion

    #region Hexadecimal(十六进制)

    /// <summary>
    /// 测试 - 十六进制值转换为二进制值
    /// </summary>
    [Fact]
    public void Test_HexToBin()
    {
        AnyRadixConvert.HexToBin("2E").ShouldBe("101110");
    }

    /// <summary>
    /// 测试 - 十六进制值转换为十进制值
    /// </summary>
    [Fact]
    public void Test_HexToDec()
    {
        AnyRadixConvert.HexToDec("2E").ShouldBe("46");
    }

    /// <summary>
    /// 测试 - 十六进制值转换为二进制值 - 对比 - 系统转换
    /// </summary>
    [Fact]
    public void Test_HexToBin_VS_SystemConvert()
    {
        var hex = "2E";
        var decimalValue = Convert.ToInt32(hex, 16);

        AnyRadixConvert.HexToBin(hex).ShouldBe("101110");
        Convert.ToString(decimalValue, 2).ShouldBe("101110");
    }

    /// <summary>
    /// 测试 - 十六进制值转换为八进制值 - 对比 - 系统转换
    /// </summary>
    [Fact]
    public void Test_HexToOct_VS_SystemConvert()
    {
        var hex = "2E";
        var decimalValue = Convert.ToInt32(hex, 16);

        AnyRadixConvert.HexToOct(hex).ShouldBe("56");
        Convert.ToString(decimalValue, 8).ShouldBe("56");
    }

    /// <summary>
    /// 测试 - 十六进制值转换为十进制值 - 对比 - 系统转换
    /// </summary>
    [Fact]
    public void Test_HexToDec_VS_SystemConvert()
    {
        var hex = "2E";
        AnyRadixConvert.HexToDec(hex).ShouldBe("46");
        Convert.ToInt32(hex, 16).ShouldBe(46);
    }

    /// <summary>
    /// 测试 - 十六进制 - 高低位交换
    /// </summary>
    [Fact]
    public void Test_Hex_Reverse()
    {
        Hex.Reverse("E81AD0E").ShouldBe("0EAD810E");
        Hex.Reverse("6E81AD0E").ShouldBe("0EAD816E");
    }

    /// <summary>
    /// 测试 - 将字符串转换为十六进制数的字符串
    /// </summary>
    [Fact]
    public void Test_LettersToHex()
    {
        AnyRadixConvert.LettersToHex("ABC").ShouldBe("41 42 43");
        AnyRadixConvert.HexToLetters("41 42 43").ShouldBe("ABC");
    }

    /// <summary>
    /// 测试 - 将长十六进制字符串转换为十进制字节数组
    /// </summary>
    [Fact]
    public void Test_LongHexToDecBytes()
    {
        var byteArray = AnyRadixConvert.LongHexToDecBytes("41 42 43");

        byteArray.Length.ShouldBe(3);
        byteArray[0].ShouldBe((byte)65);
        byteArray[1].ShouldBe((byte)66);
        byteArray[2].ShouldBe((byte)67);

        var longHex = AnyRadixConvert.DecBytesToLongHex(byteArray);

        longHex.ShouldNotBeEmpty();
        longHex.ShouldBe("41 42 43");
    }

    #endregion
}