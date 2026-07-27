using Bing.Security.Encoding;
using Bing.Security.Keys;
using Shouldly;
using Xunit;

namespace Bing.Utils.Security.Tests.Bing.Security.Keys;

/// <summary>
/// 验证 HKDF-SHA256 密钥派生行为。
/// </summary>
public class HkdfTests
{
    /// <summary>
    /// 测试目的：HKDF-SHA256 应匹配 RFC 5869 测试用例 1 的输出密钥材料。
    /// </summary>
    [Fact]
    public void DeriveKeySha256_WhenUsingRfc5869Case1_ShouldMatchExpectedOutput()
    {
        // Arrange
            var inputKeyMaterial = new byte[22];
            for (var index = 0; index < inputKeyMaterial.Length; index++)
                inputKeyMaterial[index] = 0x0b;
        var salt = HexEncoding.Decode("000102030405060708090a0b0c");
        var info = HexEncoding.Decode("f0f1f2f3f4f5f6f7f8f9");

        // Act
        var result = Hkdf.DeriveKeySha256(inputKeyMaterial, salt, info, 42);

        // Assert
        HexEncoding.Encode(result).ShouldBe("3cb25f25faacd57a90434f64d0362f2a2d2d0a90cf1a5a4c5db02d56ecc4c5bf34007208d5b887185865");
    }
}