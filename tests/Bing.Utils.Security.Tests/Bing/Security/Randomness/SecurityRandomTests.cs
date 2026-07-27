using System;
using Bing.Security.Encoding;
using Shouldly;
using Xunit;

namespace Bing.Security.Randomness;

/// <summary>
/// 验证密码学安全随机数接口的长度和格式行为。
/// </summary>
public class SecurityRandomTests
{
    /// <summary>
    /// 测试目的：随机字节生成器应返回请求长度并允许零长度。
    /// </summary>
    [Fact]
    public void GetBytes_WhenLengthIsValid_ShouldReturnRequestedLength()
    {
        // Arrange

        // Act
        var value = SecurityRandom.GetBytes(32);
        var empty = SecurityRandom.GetBytes(0);

        // Assert
        value.Length.ShouldBe(32);
        empty.ShouldBeEmpty();
    }

    /// <summary>
    /// 测试目的：负数随机字节长度应被拒绝。
    /// </summary>
    [Fact]
    public void GetBytes_WhenLengthIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange

        // Act
        var action = () => SecurityRandom.GetBytes(-1);

        // Assert
        action.ShouldThrow<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// 测试目的：连续随机输出不应恒等，并且编码结果应具有正确格式。
    /// </summary>
    [Fact]
    public void RandomGeneration_WhenCalledMultipleTimes_ShouldProduceIndependentValues()
    {
        // Arrange

        // Act
        var first = SecurityRandom.GetBytes(32);
        var second = SecurityRandom.GetBytes(32);
        var hex = SecurityRandom.GetHex(16);
        var base64Url = SecurityRandom.GetBase64Url(16);

        // Assert
        first.ShouldNotBe(second);
        hex.Length.ShouldBe(32);
        HexEncoding.Decode(hex).Length.ShouldBe(16);
        Base64UrlEncoding.Decode(base64Url).Length.ShouldBe(16);
    }

    /// <summary>
    /// 测试目的：空目标缓冲区不应导致随机填充失败。
    /// </summary>
    [Fact]
    public void Fill_WhenDestinationIsEmpty_ShouldNotThrow()
    {
        // Arrange
        Span<byte> destination = Span<byte>.Empty;

        // Act
        SecurityRandom.Fill(destination);

        // Assert
        destination.IsEmpty.ShouldBeTrue();
    }
}
