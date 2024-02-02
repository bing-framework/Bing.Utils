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
        var hex01 = Hex.ToString(hexBytes01);

        hex01.ShouldBe(hex);
    }
}