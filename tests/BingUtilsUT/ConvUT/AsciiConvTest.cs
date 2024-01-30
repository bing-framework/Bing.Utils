using Bing.Conversions;

namespace BingUtilsUT.ConvUT;

/// <summary>
/// ASCII转换器测试
/// </summary>
[Trait("ConvUT", "Ascii")]
public class AsciiConvTest
{
    /// <summary>
    /// 测试 - 将 byte[] 转换为 ASCII <see cref="string"/>
    /// </summary>
    [Fact]
    public void Test_BytesToAsciiString()
    {
        var bytes = new byte[] { 65, 66, 67 };
        var asciiString = AsciiConv.BytesToAsciiString(bytes);

        asciiString.ShouldBe("ABC");
    }

    /// <summary>
    /// 测试 - 将 ASCII <see cref="string"/> 转换为 byte[]
    /// </summary>
    [Fact]
    public void Test_AsciiStringToBytes()
    {
        var byteArray = AsciiConv.AsciiStringToBytes("ABC");

        byteArray.Length.ShouldBe(3);
        byteArray[0].ShouldBe((byte)65);
        byteArray[1].ShouldBe((byte)66);
        byteArray[2].ShouldBe((byte)67);
    }
}