using System.Text;
using System.Text.RegularExpressions;
using SkiaSharp;

namespace Bing.Drawing;

/// <summary>
/// 验证不同字符类型验证码文本的生成结果。
/// </summary>
[Trait("Drawing", "SkiaSharp.Captcha")]
public class SkiaSharpCaptchaExtendedTest
{
    #region GetCaptchaCode

    /// <summary>
    /// 验证数字类型验证码仅包含数字且长度符合请求值。
    /// </summary>
    [Fact]
    public void GetCaptchaCode_NumberType_ReturnsDigitsOnly()
    {
        var code = SkiaSharpHelper.GetCaptchaCode(10, CaptchaType.Number);
        code.Length.ShouldBe(10);
        Regex.IsMatch(code, @"^\d+$").ShouldBeTrue();
    }

    /// <summary>
    /// 验证中文类型验证码按请求长度生成非空文本。
    /// </summary>
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
