using System.Text;
using System.Text.RegularExpressions;
using SkiaSharp;

namespace Bing.Drawing;

/// <summary>
/// 测试类：覆盖 SkiaSharp 验证码 API 扩展。
/// </summary>
[Trait("Drawing", "SkiaSharp.Captcha")]
public class SkiaSharpCaptchaExtendedTest
{
    #region GetCaptchaCode

    [Fact]
    public void GetCaptchaCode_NumberType_ReturnsDigitsOnly()
    {
        var code = SkiaSharpHelper.GetCaptchaCode(10, CaptchaType.Number);
        code.Length.ShouldBe(10);
        Regex.IsMatch(code, @"^\d+$").ShouldBeTrue();
    }

    [Fact]
    public void GetCaptchaCode_ChineseType_ReturnsNonEmpty()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var code = SkiaSharpHelper.GetCaptchaCode(5, CaptchaType.ChineseChar);
        code.Length.ShouldBe(5);
        code.ShouldNotBeNullOrWhiteSpace();
    }

    #endregion
}
