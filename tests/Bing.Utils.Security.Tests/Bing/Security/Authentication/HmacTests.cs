using System;
using Shouldly;
using Xunit;

namespace Bing.Security.Authentication;

/// <summary>
/// 验证 HMAC SHA-2 的 RFC 4231 测试向量和验证行为。
/// </summary>
public class HmacTests
{
    /// <summary>
    /// 测试目的：HMAC SHA-2 实现应匹配 RFC 4231 第一组测试向量。
    /// </summary>
    [Theory]
    [InlineData(HmacAlgorithmType.Sha256, "b0344c61d8db38535ca8afceaf0bf12b881dc200c9833da726e9376c2e32cff7")]
    [InlineData(HmacAlgorithmType.Sha384, "afd03944d84895626b0825f4ab46907f15f9dadbe4101ec682aa034c7cebc59cfaea9ea9076ede7f4af152e8b2fa9cb6")]
    [InlineData(HmacAlgorithmType.Sha512, "87aa7cdea5ef619d4ff0b4241a1d6cb02379f4e2ce4ec2787ad0b30545e17cde daa833b7d6b8a702038b274eaea3f4e4be9d914eeb61f1702e696c203a126854" )]
    public void ComputeHex_WhenUsingRfc4231Case1_ShouldMatchStandardVector(HmacAlgorithmType algorithm, string expected)
    {
        // Arrange
        var key = new byte[20];
        Array.Fill(key, (byte)0x0B);

        // Act
        var result = Hmac.ComputeHex("Hi There", key, algorithm);

        // Assert
        result.ShouldBe(expected.Replace(" ", string.Empty));
    }

    /// <summary>
    /// 测试目的：HMAC 验证应拒绝错误消息、错误密钥和空密钥。
    /// </summary>
    [Fact]
    public void Verify_WhenDataOrKeyIsIncorrect_ShouldReturnFalseOrThrow()
    {
        // Arrange
        var key = System.Text.Encoding.UTF8.GetBytes("high-entropy-test-key");
        var mac = Hmac.ComputeHex("消息", key);

        // Act
        var changedData = Hmac.Verify(System.Text.Encoding.UTF8.GetBytes("消息!"), mac, key);
        var changedKey = Hmac.Verify(System.Text.Encoding.UTF8.GetBytes("消息"), mac, System.Text.Encoding.UTF8.GetBytes("different-key"));
        var action = () => Hmac.Compute(System.Text.Encoding.UTF8.GetBytes("消息"), Array.Empty<byte>());

        // Assert
        changedData.ShouldBeFalse();
        changedKey.ShouldBeFalse();
        action.ShouldThrow<ArgumentException>();
    }
}
