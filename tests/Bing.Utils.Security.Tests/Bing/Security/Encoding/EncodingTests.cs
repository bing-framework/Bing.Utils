using System;
using System.Text;
using Bing.Security.Encoding;
using Shouldly;
using Xunit;

namespace Bing.Utils.Security.Tests.Bing.Security.Encoding;

/// <summary>
/// 验证安全编码类型的格式约束和往返行为。
/// </summary>
public class EncodingTests
{
    /// <summary>
    /// 测试目的：空字节序列应编码为空字符串。
    /// </summary>
    [Fact]
    public void HexEncode_WhenValueIsEmpty_ShouldReturnEmptyString()
    {
        // Arrange
        var value = Array.Empty<byte>();

        // Act
        var result = HexEncoding.Encode(value);

        // Assert
        result.ShouldBe(string.Empty);
    }

    /// <summary>
    /// 测试目的：十六进制编码应支持大小写输出和往返解码。
    /// </summary>
    [Fact]
    public void HexEncoding_WhenValueIsValid_ShouldRespectCaseAndRoundTrip()
    {
        // Arrange
        var value = new byte[] { 0x0A, 0xBC, 0xFF };

        // Act
        var lower = HexEncoding.Encode(value);
        var upper = HexEncoding.Encode(value, false);
        var decoded = HexEncoding.Decode(lower);

        // Assert
        lower.ShouldBe("0abcff");
        upper.ShouldBe("0ABCFF");
        decoded.ShouldBe(value);
    }

    /// <summary>
    /// 测试目的：奇数长度和非法字符的十六进制文本应失败。
    /// </summary>
    [Theory]
    [InlineData("a")]
    [InlineData("0g")]
    public void HexDecode_WhenValueIsInvalid_ShouldThrowFormatException(string value)
    {
        // Arrange

        // Act
        var action = () => HexEncoding.Decode(value);

        // Assert
        action.ShouldThrow<FormatException>();
    }

    /// <summary>
    /// 测试目的：TryDecode 对非法十六进制文本不应抛出异常。
    /// </summary>
    [Fact]
    public void HexTryDecode_WhenValueIsInvalid_ShouldReturnFalse()
    {
        // Arrange

        // Act
        var success = HexEncoding.TryDecode("zz", out var result);

        // Assert
        success.ShouldBeFalse();
        result.ShouldBeEmpty();
    }

    /// <summary>
    /// 测试目的：Base64Url 应采用 URL 安全字符且不包含填充。
    /// </summary>
    [Fact]
    public void Base64UrlEncoding_WhenValueContainsUrlUnsafeBase64Characters_ShouldUseUrlSafeAlphabet()
    {
        // Arrange
        var value = new byte[] { 0xFB, 0xFF, 0xFF };

        // Act
        var result = Base64UrlEncoding.Encode(value);

        // Assert
        result.ShouldBe("-___");
        result.ShouldNotContain("=");
        Base64UrlEncoding.Decode(result).ShouldBe(value);
    }

    /// <summary>
    /// 测试目的：Base64Url 应支持空输入和 UTF-8 文本往返。
    /// </summary>
    [Fact]
    public void Base64UrlEncoding_WhenValueIsEmptyOrUnicode_ShouldRoundTrip()
    {
        // Arrange
        var value = System.Text.Encoding.UTF8.GetBytes("安全测试");

        // Act
        var empty = Base64UrlEncoding.Encode(Array.Empty<byte>());
        var decoded = Base64UrlEncoding.Decode(Base64UrlEncoding.Encode(value));

        // Assert
        empty.ShouldBe(string.Empty);
        decoded.ShouldBe(value);
    }

    /// <summary>
    /// 测试目的：普通 Base64、填充和非法长度不应作为 Base64Url 被接受。
    /// </summary>
    [Theory]
    [InlineData("+/8=")]
    [InlineData("abcde")]
    [InlineData("abc=")]
    public void Base64UrlTryDecode_WhenValueIsInvalid_ShouldReturnFalse(string value)
    {
        // Arrange

        // Act
        var success = Base64UrlEncoding.TryDecode(value, out var result);

        // Assert
        success.ShouldBeFalse();
        result.ShouldBeEmpty();
    }
}