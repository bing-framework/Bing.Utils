using Shouldly;
using Xunit;

namespace Bing.Security;

/// <summary>
/// 验证固定时间比较的结果语义。
/// </summary>
public class SecureComparisonTests
{
    /// <summary>
    /// 测试目的：相同序列应比较成功，不同内容或长度应比较失败。
    /// </summary>
    [Fact]
    public void FixedTimeEquals_WhenSequencesDiffer_ShouldReturnExpectedResult()
    {
        // Arrange
        var value = new byte[] { 1, 2, 3 };

        // Act
        var equal = SecureComparison.FixedTimeEquals(value, new byte[] { 1, 2, 3 });
        var differentContent = SecureComparison.FixedTimeEquals(value, new byte[] { 1, 2, 4 });
        var differentLength = SecureComparison.FixedTimeEquals(value, new byte[] { 1, 2 });

        // Assert
        equal.ShouldBeTrue();
        differentContent.ShouldBeFalse();
        differentLength.ShouldBeFalse();
    }
}
