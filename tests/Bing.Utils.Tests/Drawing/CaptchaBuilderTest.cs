using Bing.Drawing;
using System.Text.RegularExpressions;
namespace Bing.Utils.Tests.Drawing;
/// <summary>
/// 测试类：覆盖 `CaptchaBuilder` 相关行为。
/// </summary>
public class CaptchaBuilderTest : TestBase
{
    private readonly CaptchaBuilder _coder;
    public CaptchaBuilderTest(ITestOutputHelper output) : base(output)
    {
        _coder = new CaptchaBuilder();
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }
    /// <summary>
    /// 测试用例：验证 `Test` 在 `GetCode` 场景下，结果为 `NumberAndLetter`。
    /// </summary>
    [Fact]
    public void Test_GetCode_NumberAndLetter()
    {
        var result = _coder.GetCode(10);
        result.Length.ShouldBe(10);
        Regex.IsMatch(result, @"^[A-Za-z0-9]+$").ShouldBeTrue();
    }
    /// <summary>
    /// 测试用例：验证 `Test` 在 `GetCode` 场景下，结果为 `Number`。
    /// </summary>
    [Fact]
    public void Test_GetCode_Number()
    {
        var result = _coder.GetCode(10, CaptchaType.Number);
        result.Length.ShouldBe(10);
        Regex.IsMatch(result, @"^\d+$").ShouldBeTrue();
    }
    /// <summary>
    /// 测试用例：验证 `Test` 在 `GetCode` 场景下，结果为 `ChineseChar`。
    /// </summary>
    [Fact]
    public void Test_GetCode_ChineseChar()
    {
        var result = _coder.GetCode(10, CaptchaType.ChineseChar);
        result.Length.ShouldBe(10);
        result.ShouldNotBeNullOrWhiteSpace();
    }
    /// <summary>
    /// 测试用例：验证 `Test` 在 `CreateImage` 场景下的行为。
    /// </summary>
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
    /// <summary>
    /// 测试用例：验证 `Test` 在 `GetCode` 场景下，结果为 `InvalidLength`。
    /// </summary>
    [Fact]
    public void Test_GetCode_InvalidLength()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => _coder.GetCode(0));
    }
    /// <summary>
    /// 测试用例：验证 `Test` 在 `CreateImage` 场景下，结果为 `InvalidLength`。
    /// </summary>
    [Fact]
    public void Test_CreateImage_InvalidLength()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => _coder.CreateImage(0, out _));
    }
    /// <summary>
    /// 测试用例：验证 `Test` 在 `CreateImage` 场景下，结果为 `EmptyCode`。
    /// </summary>
    [Fact]
    public void Test_CreateImage_EmptyCode()
    {
        Should.Throw<ArgumentNullException>(() => _coder.CreateImage(string.Empty));
    }
}

