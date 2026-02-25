using System.Collections.Generic;
using System.Drawing;

namespace Bing.Helpers;

/// <summary>
/// 测试类：覆盖 <see cref="ColorConverter"/> 的 CSS 颜色解析与 HSLA 转换行为。
/// </summary>
[Trait("Bing.Helpers", "ColorConverter")]
public class ColorConverterTest
{
    /// <summary>
    /// 测试用例：`GetColorFromCssString` 在空白输入时应抛出 <see cref="ArgumentNullException"/>。
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void GetColorFromCssString_NullOrWhiteSpace_ThrowsArgumentNullException(string input)
    {
        var ex = Should.Throw<ArgumentNullException>(() => ColorConverter.GetColorFromCssString(input));
        ex.ParamName.ShouldBe("cssColour");
    }

    /// <summary>
    /// 测试用例：`GetColorFromCssString` 解析十六进制颜色（3/6/8位）时返回预期 RGBA。
    /// </summary>
    [Theory]
    [MemberData(nameof(GetHexColorCases))]
    public void GetColorFromCssString_HexInputs_ReturnExpectedColor(
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
    /// 测试用例：`GetColorFromCssString` 解析超范围 RGB 分量时应进行截断到有效区间。
    /// </summary>
    [Fact]
    public void GetColorFromCssString_RgbWithOutOfRangeValues_ClampsToValidRange()
    {
        var color = ColorConverter.GetColorFromCssString("rgb(300,-10,50)");
        color.A.ShouldBe((byte)0xFF);
        color.R.ShouldBe((byte)255);
        color.G.ShouldBe((byte)0);
        color.B.ShouldBe((byte)50);
    }

    /// <summary>
    /// 测试用例：`GetColorFromCssString` 解析 RGBA 字符串时，当前实现抛出 <see cref="ArgumentException"/>。
    /// </summary>
    [Fact]
    public void GetColorFromCssString_RgbaInput_CurrentlyThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => ColorConverter.GetColorFromCssString("rgba(10,20,30,0.5)"));
    }

    /// <summary>
    /// 测试用例：`GetColorFromCssString` 解析命名颜色时应返回系统定义颜色（大小写不敏感）。
    /// </summary>
    [Theory]
    [MemberData(nameof(GetNamedColorCases))]
    public void GetColorFromCssString_NamedColor_ReturnsExpectedColor(string input, Color expected)
    {
        var color = ColorConverter.GetColorFromCssString(input);

        color.ToArgb().ShouldBe(expected.ToArgb());
    }

    /// <summary>
    /// 测试用例：`GetColorFromCssString` 在非法字符串输入时应抛出 <see cref="ArgumentException"/>。
    /// </summary>
    [Fact]
    public void GetColorFromCssString_InvalidInput_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => ColorConverter.GetColorFromCssString("not-a-color"));
    }

    /// <summary>
    /// 测试用例：`GetColorFromCssString` 解析 HSL 输入时，当前实现抛出 <see cref="OverflowException"/>。
    /// </summary>
    [Fact]
    public void GetColorFromCssString_HslRedInput_CurrentlyThrowsOverflowException()
    {
        Should.Throw<OverflowException>(() => ColorConverter.GetColorFromCssString("hsl(0,100%,50%)"));
    }

    /// <summary>
    /// 测试用例：`HslaToRgba` 在饱和度为 0 时应返回灰度色并保留 Alpha。
    /// </summary>
    [Fact]
    public void HslaToRgba_ZeroSaturation_ReturnsGrayWithGivenAlpha()
    {
        var color = ColorConverter.HslaToRgba(120, 0, 128, 77);
        color.A.ShouldBe((byte)77);
        color.R.ShouldBe((byte)128);
        color.G.ShouldBe((byte)128);
        color.B.ShouldBe((byte)128);
    }

    public static IEnumerable<object[]> GetHexColorCases()
    {
        yield return new object[] { "#112233", (byte)0xFF, (byte)0x11, (byte)0x22, (byte)0x33 };
        yield return new object[] { "#11223344", (byte)0x11, (byte)0x22, (byte)0x33, (byte)0x44 };
        yield return new object[] { "#abc", (byte)0xFF, (byte)0xAA, (byte)0xBB, (byte)0xCC };
    }

    public static IEnumerable<object[]> GetNamedColorCases()
    {
        yield return new object[] { "blue", Color.Blue };
        yield return new object[] { "BLUE", Color.Blue };
        yield return new object[] { "Red", Color.Red };
    }
}
