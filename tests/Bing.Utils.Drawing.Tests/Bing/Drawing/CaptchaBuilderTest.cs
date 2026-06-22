using System.Text;
using System.Text.RegularExpressions;

namespace Bing.Drawing;

/// <summary>
/// 测试类：覆盖 <see cref="CaptchaBuilder"/> 相关行为。
/// </summary>
[Trait("Drawing", "CaptchaBuilder")]
public class CaptchaBuilderTest
{
    private readonly CaptchaBuilder _coder = new();

    public CaptchaBuilderTest()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>
    /// 测试用例：验证 <see cref="CaptchaBuilder.GetCode(int, CaptchaType)"/> 在默认场景下返回数字字母混合字符串。
    /// </summary>
    [Fact]
    public void GetCode_NumberAndLetter_ReturnsExpectedShape()
    {
        var result = _coder.GetCode(10);
        result.Length.ShouldBe(10);
        Regex.IsMatch(result, @"^[A-Za-z0-9]+$").ShouldBeTrue();
    }

    /// <summary>
    /// 测试用例：验证 <see cref="CaptchaBuilder.GetCode(int, CaptchaType)"/> 在数字模式下仅返回数字。
    /// </summary>
    [Fact]
    public void GetCode_Number_ReturnsDigitsOnly()
    {
        var result = _coder.GetCode(10, CaptchaType.Number);
        result.Length.ShouldBe(10);
        Regex.IsMatch(result, @"^\d+$").ShouldBeTrue();
    }

    /// <summary>
    /// 测试用例：验证 <see cref="CaptchaBuilder.GetCode(int, CaptchaType)"/> 在汉字模式下返回非空字符串。
    /// </summary>
    [Fact]
    public void GetCode_ChineseChar_ReturnsNonEmptyText()
    {
        var result = _coder.GetCode(10, CaptchaType.ChineseChar);
        result.Length.ShouldBe(10);
        result.ShouldNotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// 测试用例：验证 <see cref="CaptchaBuilder.CreateImage(int, out string, CaptchaType)"/> 能按配置生成图片。
    /// </summary>
    [Fact]
    public void CreateImage_ReturnsExpectedSizeAndCodeLength()
    {
        _coder.RandomPointPercent = 5;
        _coder.RandomColor = true;

        using var image = _coder.CreateImage(4, out var code, CaptchaType.ChineseChar);

        image.Width.ShouldBe(_coder.FontWidth * 4 + _coder.FontWidth);
        image.Height.ShouldBe(_coder.FontSize + _coder.FontSize / 2);
        code.Length.ShouldBe(4);
    }

    /// <summary>
    /// 测试用例：验证 <see cref="CaptchaBuilder.GetCode(int, CaptchaType)"/> 在非法长度下抛出异常。
    /// </summary>
    [Fact]
    public void GetCode_InvalidLength_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => _coder.GetCode(0));
    }

    /// <summary>
    /// 测试用例：验证 <see cref="CaptchaBuilder.CreateImage(int, out string, CaptchaType)"/> 在非法长度下抛出异常。
    /// </summary>
    [Fact]
    public void CreateImage_InvalidLength_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => _coder.CreateImage(0, out _));
    }

    /// <summary>
    /// 测试用例：验证 <see cref="CaptchaBuilder.CreateImage(string)"/> 在空字符串下抛出异常。
    /// </summary>
    [Fact]
    public void CreateImage_EmptyCode_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => _coder.CreateImage(string.Empty));
    }
}