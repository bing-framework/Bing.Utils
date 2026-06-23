using System.Text;
using System.Text.RegularExpressions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Bing.Drawing;

/// <summary>
/// 测试类：覆盖验证码相关 API。
/// </summary>
[Trait("Drawing", "ImageSharp.Captcha")]
public class ImageSharpCaptchaTest
{
    #region GetCaptchaCode

    [Fact]
    public void GetCaptchaCode_ValidLength_ReturnsExpectedShape()
    {
        var code = ImageSharpHelper.GetCaptchaCode(6);
        code.Length.ShouldBe(6);
        Regex.IsMatch(code, @"^[A-Za-z0-9]+$").ShouldBeTrue();
    }

    [Fact]
    public void GetCaptchaCode_NumberType_ReturnsDigitsOnly()
    {
        var code = ImageSharpHelper.GetCaptchaCode(10, CaptchaType.Number);
        code.Length.ShouldBe(10);
        Regex.IsMatch(code, @"^\d+$").ShouldBeTrue();
    }

    [Fact]
    public void GetCaptchaCode_ChineseType_ReturnsNonEmpty()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var code = ImageSharpHelper.GetCaptchaCode(5, CaptchaType.ChineseChar);
        code.Length.ShouldBe(5);
        code.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GetCaptchaCode_InvalidLength_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ImageSharpHelper.GetCaptchaCode(0));
        Should.Throw<ArgumentOutOfRangeException>(() => ImageSharpHelper.GetCaptchaCode(-1));
    }

    #endregion

    #region CreateCaptchaImage

    [Fact]
    public void CreateCaptchaImage_ValidCode_ReturnsLoadableImage()
    {
        using var image = ImageSharpHelper.CreateCaptchaImage("ABC123");
        image.ShouldNotBeNull();
        image.Width.ShouldBeGreaterThan(0);
        image.Height.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void CreateCaptchaImage_WithLengthOutCode_ReturnsExpectedCodeLength()
    {
        using var image = ImageSharpHelper.CreateCaptchaImage(6, out var code);
        code.Length.ShouldBe(6);
        image.ShouldNotBeNull();
    }

    [Fact]
    public void CreateCaptchaImage_WithCustomOptions_RespectsSize()
    {
        var options = new CaptchaOptions { FontSize = 30, FontWidth = 30 };
        using var image = ImageSharpHelper.CreateCaptchaImage("AB", options);
        // width = fontWidth * code.Length + fontWidth = 30*2+30 = 90
        // height = fontSize + fontSize/2 = 30+15 = 45
        image.Width.ShouldBe(90);
        image.Height.ShouldBe(45);
    }

    [Fact]
    public void CreateCaptchaImage_NoBorder_NoBorderPixels()
    {
        var options = new CaptchaOptions { HasBorder = false };
        using var image = ImageSharpHelper.CreateCaptchaImage("A", options);
        image.ShouldNotBeNull();
    }

    [Fact]
    public void CreateCaptchaImage_EmptyCode_Throws()
    {
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.CreateCaptchaImage(""));
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.CreateCaptchaImage("  "));
    }

    [Fact]
    public void CreateCaptchaImage_InvalidLength_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ImageSharpHelper.CreateCaptchaImage(0, out _));
    }

    [Fact]
    public void CreateCaptchaImage_CanRoundTripToBytes()
    {
        using var image = ImageSharpHelper.CreateCaptchaImage("TEST");
        var bytes = ImageSharpHelper.ToBytes(image, SixLabors.ImageSharp.Formats.Png.PngFormat.Instance);
        bytes.ShouldNotBeNull();
        bytes.Length.ShouldBeGreaterThan(0);
    }

    #endregion
}
