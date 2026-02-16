using Bing.Drawing;
using System.Text.RegularExpressions;

namespace Bing.Utils.Tests.Drawing;

public class CaptchaBuilderTest : TestBase
{
    private readonly CaptchaBuilder _coder;

    public CaptchaBuilderTest(ITestOutputHelper output) : base(output)
    {
        _coder = new CaptchaBuilder();
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    [Fact]
    public void Test_GetCode_NumberAndLetter()
    {
        var result = _coder.GetCode(10);
        result.Length.ShouldBe(10);
        Regex.IsMatch(result, @"^[A-Za-z0-9]+$").ShouldBeTrue();
    }

    [Fact]
    public void Test_GetCode_Number()
    {
        var result = _coder.GetCode(10, CaptchaType.Number);
        result.Length.ShouldBe(10);
        Regex.IsMatch(result, @"^\d+$").ShouldBeTrue();
    }

    [Fact]
    public void Test_GetCode_ChineseChar()
    {
        var result = _coder.GetCode(10, CaptchaType.ChineseChar);
        result.Length.ShouldBe(10);
        result.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Test_CreateImage()
    {
        _coder.RandomPointPercent = 5;
        _coder.RandomColor = true;
        using var image = _coder.CreateImage(4, out var code, CaptchaType.ChineseChar);
        image.Width.ShouldBe(_coder.FontWidth * 4 + _coder.FontWidth);
        image.Height.ShouldBe(_coder.FontSize + _coder.FontSize / 2);
        code.Length.ShouldBe(4);
    }

    [Fact]
    public void Test_GetCode_InvalidLength()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => _coder.GetCode(0));
    }

    [Fact]
    public void Test_CreateImage_InvalidLength()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => _coder.CreateImage(0, out _));
    }

    [Fact]
    public void Test_CreateImage_EmptyCode()
    {
        Should.Throw<ArgumentNullException>(() => _coder.CreateImage(string.Empty));
    }
}
