using System.Text;
using System.Text.RegularExpressions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Bing.Drawing;

/// <summary>
/// 验证验证码文本生成、图像尺寸及参数边界。
/// </summary>
[Trait("Drawing", "ImageSharp.Captcha")]
public class ImageSharpCaptchaTest
{
    #region GetCaptchaCode

    /// <summary>
    /// 验证默认验证码为指定长度的数字字母串。
    /// </summary>
    [Fact]
    public void GetCaptchaCode_ValidLength_ReturnsExpectedShape()
    {
        var code = ImageSharpHelper.GetCaptchaCode(6);
        code.Length.ShouldBe(6);
        Regex.IsMatch(code, @"^[A-Za-z0-9]+$").ShouldBeTrue();
    }

    /// <summary>
    /// 验证数字验证码仅包含数字并满足指定长度。
    /// </summary>
    [Fact]
    public void GetCaptchaCode_NumberType_ReturnsDigitsOnly()
    {
        var code = ImageSharpHelper.GetCaptchaCode(10, CaptchaType.Number);
        code.Length.ShouldBe(10);
        Regex.IsMatch(code, @"^\d+$").ShouldBeTrue();
    }

    /// <summary>
    /// 验证中文验证码满足指定长度且不为空白。
    /// </summary>
    [Fact]
    public void GetCaptchaCode_ChineseType_ReturnsNonEmpty()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var code = ImageSharpHelper.GetCaptchaCode(5, CaptchaType.ChineseChar);
        code.Length.ShouldBe(5);
        code.ShouldNotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// 验证验证码文本生成拒绝非正长度。
    /// </summary>
    [Fact]
    public void GetCaptchaCode_InvalidLength_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ImageSharpHelper.GetCaptchaCode(0));
        Should.Throw<ArgumentOutOfRangeException>(() => ImageSharpHelper.GetCaptchaCode(-1));
    }

    #endregion

    #region CreateCaptchaImage

    /// <summary>
    /// 验证给定验证码文本可以生成非空且尺寸为正的图像。
    /// </summary>
    [Fact]
    public void CreateCaptchaImage_ValidCode_ReturnsLoadableImage()
    {
        using var image = ImageSharpHelper.CreateCaptchaImage("ABC123");
        image.ShouldNotBeNull();
        image.Width.ShouldBeGreaterThan(0);
        image.Height.ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// 验证验证码图像生成同时输出指定长度的验证码文本。
    /// </summary>
    [Fact]
    public void CreateCaptchaImage_WithLengthOutCode_ReturnsExpectedCodeLength()
    {
        using var image = ImageSharpHelper.CreateCaptchaImage(6, out var code);
        code.Length.ShouldBe(6);
        image.ShouldNotBeNull();
    }

    /// <summary>
    /// 验证自定义字体尺寸决定验证码图像的宽高。
    /// </summary>
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

    /// <summary>
    /// 验证关闭边框选项仍可生成验证码图像。
    /// </summary>
    [Fact]
    public void CreateCaptchaImage_NoBorder_NoBorderPixels()
    {
        var options = new CaptchaOptions { HasBorder = false };
        using var image = ImageSharpHelper.CreateCaptchaImage("A", options);
        image.ShouldNotBeNull();
    }

    /// <summary>
    /// 验证验证码图像生成拒绝空字符串及空白文本。
    /// </summary>
    [Fact]
    public void CreateCaptchaImage_EmptyCode_Throws()
    {
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.CreateCaptchaImage(""));
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.CreateCaptchaImage("  "));
    }

    /// <summary>
    /// 验证按长度生成验证码图像时拒绝零长度。
    /// </summary>
    [Fact]
    public void CreateCaptchaImage_InvalidLength_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ImageSharpHelper.CreateCaptchaImage(0, out _));
    }

    /// <summary>
    /// 验证验证码图像可编码为非空 PNG 字节。
    /// </summary>
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
