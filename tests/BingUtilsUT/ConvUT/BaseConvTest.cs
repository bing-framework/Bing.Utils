using System.Text;
using Bing.Conversions;

namespace BingUtilsUT.ConvUT;

/// <summary>
/// BASE 转换工具测试
/// </summary>
[Trait("ConvUT", "BaseX")]
public class BaseConvTest
{
    private const string TestValue = "Bing.Utils";

    /// <summary>
    /// 测试 - Base32 - 字符串
    /// </summary>
    [Fact]
    public void Test_Base32String()
    {
        var baseVal = BaseConv.ToBase32String(TestValue);
        var originalVal = BaseConv.FromBase32String(baseVal);

        originalVal.ShouldBe(TestValue);
    }

    /// <summary>
    /// 测试 - Base32 - 字节数组
    /// </summary>
    [Fact]
    public void Test_Base32Bytes()
    {
        var byteArray = Encoding.UTF8.GetBytes(TestValue);
        var baseVal = BaseConv.ToBase32(byteArray);

        var originalByteArray = BaseConv.FromBase32(baseVal);
        var originalVal = Encoding.UTF8.GetString(originalByteArray);

        originalVal.ShouldBe(TestValue);
    }

    /// <summary>
    /// 测试 - Base64 - 字符串
    /// </summary>
    [Fact]
    public void Test_Base64String()
    {
        var baseVal = BaseConv.ToBase64String(TestValue);
        var originalVal = BaseConv.FromBase64String(baseVal);

        originalVal.ShouldBe(TestValue);
    }

    /// <summary>
    /// 测试 - Base64Url
    /// </summary>
    [Fact]
    public void Test_Base64UrlString()
    {
        const string input = "SGVsbG8gV29ybGQh";
        const string expected = "Hello World!";

        var actual = BaseConv.FromBase64UrlString(input);

        actual.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - Base64Url - 带 "-" 和 "_" 符号
    /// </summary>
    [Theory]
    [InlineData("WGZ-Xz0=", "Xf~_=")]
    [InlineData("bFo_ejc=", "lZ?z7")]
    public void Test_Base64UrlString_MinusAndUnderscore(string input, string expected)
    {
        BaseConv.FromBase64UrlString(input).ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - Base64Url - 缺失 "=" 符号
    /// </summary>
    [Theory]
    [InlineData("SA", "H")]
    [InlineData("SGk", "Hi")]
    public void Test_Base64UrlString_RemovedPadding(string input, string expected)
    {
        BaseConv.FromBase64UrlString(input).ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - Base64 - 字节数组
    /// </summary>
    [Fact]
    public void Test_Base64Bytes()
    {
        var byteArray = Encoding.UTF8.GetBytes(TestValue);
        var baseVal = BaseConv.ToBase64(byteArray);

        var originalByteArray = BaseConv.FromBase64(baseVal);
        var originalVal = Encoding.UTF8.GetString(originalByteArray);

        originalVal.ShouldBe(TestValue);
    }

    /// <summary>
    /// 测试 - Base91 - 字符串
    /// </summary>
    [Fact]
    public void Test_Base91String()
    {
        var baseVal = BaseConv.ToBase91String(TestValue);
        var originalVal = BaseConv.FromBase91String(baseVal);

        originalVal.ShouldBe(TestValue);
    }

    /// <summary>
    /// 测试 - Base91 - 字节数组
    /// </summary>
    [Fact]
    public void Test_Base91Bytes()
    {
        var byteArray = Encoding.UTF8.GetBytes(TestValue);
        var baseVal = BaseConv.ToBase91(byteArray);

        var originalByteArray = BaseConv.FromBase91(baseVal);
        var originalVal = Encoding.UTF8.GetString(originalByteArray);

        originalVal.ShouldBe(TestValue);
    }

    /// <summary>
    /// 测试 - Base256 - 字符串
    /// </summary>
    [Fact]
    public void Test_Base256String()
    {
        var baseVal = BaseConv.ToBase256String(TestValue);
        var originalVal = BaseConv.FromBase256String(baseVal);

        originalVal.ShouldBe(TestValue);
    }

    /// <summary>
    /// 测试 - Base256 - 字节数组
    /// </summary>
    [Fact]
    public void Test_Base256Bytes()
    {
        var byteArray = Encoding.UTF8.GetBytes(TestValue);
        var baseVal = BaseConv.ToBase256(byteArray);

        var originalByteArray = BaseConv.FromBase256(baseVal);
        var originalVal = Encoding.UTF8.GetString(originalByteArray);

        originalVal.ShouldBe(TestValue);
    }

    /// <summary>
    /// 测试 - ZBase32 - 字符串
    /// </summary>
    [Fact]
    public void Test_ZBase32String()
    {
        var baseVal = BaseConv.ToZBase32String(TestValue);
        var originalVal = BaseConv.FromZBase32String(baseVal);

        originalVal.ShouldBe(TestValue);
    }

    /// <summary>
    /// 测试 - ZBase32 - 字节数组
    /// </summary>
    [Fact]
    public void Test_ZBase32Bytes()
    {
        var byteArray = Encoding.UTF8.GetBytes(TestValue);
        var baseVal = BaseConv.ToZBase32(byteArray);

        var originalByteArray = BaseConv.FromZBase32(baseVal);
        var originalVal = Encoding.UTF8.GetString(originalByteArray);

        originalVal.ShouldBe(TestValue);
    }
}