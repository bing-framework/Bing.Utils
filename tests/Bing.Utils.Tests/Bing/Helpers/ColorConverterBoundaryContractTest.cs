using System.Drawing;

namespace Bing.Helpers;

/// <summary>
/// 测试类：覆盖 <see cref="ColorConverter"/> 的边界输入与当前契约行为。
/// </summary>
[Trait("Bing.Helpers", "ColorConverter.BoundaryContract")]
public class ColorConverterBoundaryContractTest
{
    /// <summary>
    /// 测试用例：HEX 输入的边界格式（无 `#`、带尾随字符）应满足当前解析契约。
    /// </summary>
    [Theory]
    [MemberData(nameof(GetHexBoundaryCases))]
    public void GetColorFromCssString_HexBoundaryInputs_ReturnExpectedColor(
        string input,
        byte expectedA,
        byte expectedR,
        byte expectedG,
        byte expectedB)
    {
        var color = ColorConverter.GetColorFromCssString(input);

        color.A.ShouldBe(expectedA);
        color.R.ShouldBe(expectedR);
        color.G.ShouldBe(expectedG);
        color.B.ShouldBe(expectedB);
    }

    /// <summary>
    /// 测试用例：RGB 边界格式（百分比、空白字符）应按当前规则解析。
    /// </summary>
    [Theory]
    [MemberData(nameof(GetRgbBoundaryCases))]
    public void GetColorFromCssString_RgbBoundaryInputs_ReturnExpectedColor(
        string input,
        byte expectedR,
        byte expectedG,
        byte expectedB)
    {
        var color = ColorConverter.GetColorFromCssString(input);

        color.A.ShouldBe((byte)0xFF);
        color.R.ShouldBe(expectedR);
        color.G.ShouldBe(expectedG);
        color.B.ShouldBe(expectedB);
    }

    /// <summary>
    /// 测试用例：若干当前已知异常契约（RGBA 缺 alpha、HSLA 解析）应抛出既定异常类型。
    /// </summary>
    [Theory]
    [MemberData(nameof(GetCurrentExceptionContractCases))]
    public void GetColorFromCssString_CurrentExceptionContracts_ThrowsExpectedExceptionType(string input, Type expectedExceptionType)
    {
        var ex = Should.Throw<Exception>(() => ColorConverter.GetColorFromCssString(input));

        ex.ShouldBeOfType(expectedExceptionType);
    }

    /// <summary>
    /// 测试用例：RGB 存在非法数字时应抛出 <see cref="ArgumentException"/> 且消息包含原始片段。
    /// </summary>
    [Fact]
    public void GetColorFromCssString_RgbWithInvalidNumeric_ThrowsArgumentExceptionWithRawValue()
    {
        var ex = Should.Throw<ArgumentException>(() => ColorConverter.GetColorFromCssString("rgb(a,20,30)"));

        ex.Message.ShouldContain("\"a\"");
    }

    /// <summary>
    /// 测试用例：命名颜色匹配应对大小写不敏感。
    /// </summary>
    [Fact]
    public void GetColorFromCssString_NamedColorWithUpperCase_ReturnsExpectedColor()
    {
        var color = ColorConverter.GetColorFromCssString("BLUE");

        color.ToArgb().ShouldBe(Color.Blue.ToArgb());
    }

    public static IEnumerable<object[]> GetHexBoundaryCases()
    {
        yield return new object[] { "112233", (byte)0xFF, (byte)0x11, (byte)0x22, (byte)0x33 };
        yield return new object[] { "#112233-ignored", (byte)0xFF, (byte)0x11, (byte)0x22, (byte)0x33 };
    }

    public static IEnumerable<object[]> GetRgbBoundaryCases()
    {
        yield return new object[] { "rgb(10%,20%,30%)", (byte)25, (byte)51, (byte)76 };
        yield return new object[] { "rgb( 1 , 2 , 3 )", (byte)1, (byte)2, (byte)3 };
    }

    public static IEnumerable<object[]> GetCurrentExceptionContractCases()
    {
        yield return new object[] { "rgba(10,20,30)", typeof(IndexOutOfRangeException) };
        yield return new object[] { "hsla(120,0%,50%,50%)", typeof(OverflowException) };
    }
}
