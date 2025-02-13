using Bing.Conversions;

namespace BingUtilsUT.ConvUT;

/// <summary>
/// 十六进制工具测试
/// </summary>
[Trait("ConvUT", "Hex")]
public class HexTest
{
    /// <summary>
    /// 测试 - 十六进制
    /// </summary>
    [Fact]
    public void Test_Hex()
    {
        var dec = "1234567890";
        var hex = AnyRadixConvert.X2X(dec, 10, 16); // Should be 499602D2

        var hexBytes01 = Hex.ToBytes(hex); // Should be 73 150 2 210
        hexBytes01[0].ShouldBe((byte)73);
        hexBytes01[1].ShouldBe((byte)150);
        hexBytes01[2].ShouldBe((byte)2);
        hexBytes01[3].ShouldBe((byte)210);
        var hex01 = Hex.ToString(hexBytes01);

        hex01.ShouldBe(hex);
    }

    /// <summary>
    /// 测试 - 将字节数组转换成十六进制字符串
    /// </summary>
    [Fact]
    public void Test_BytesToString()
    {
        var bytes = new byte[] { 65, 66, 67 };
        Hex.ToString(bytes).ShouldBe("414243");
    }
}