namespace Bing.Conversions;

/// <summary>
/// 任意[2,62]进制转换器 测试
/// </summary>
public class AnyRadixConvertTest
{
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
        AnyRadixConvert.X2X("140", 8,2).ShouldBe("1100000");
        AnyRadixConvert.X2X("140", 8,10).ShouldBe("96");
        AnyRadixConvert.X2X("140", 8, 16).ShouldBe("60");
        // 10 -> [2,8,16]
        AnyRadixConvert.X2X("128", 10,2).ShouldBe("10000000");
        AnyRadixConvert.X2X("128", 10,8).ShouldBe("200");
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
        AnyRadixConvert.DecToHex(46,4).ShouldBe("002E");
    }
}