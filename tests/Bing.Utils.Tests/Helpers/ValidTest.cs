using Bing.Helpers;

namespace Bing.Utils.Tests.Helpers;

/// <summary>
/// 验证操作 单元测试
/// </summary>
public class ValidTest : TestBase
{
    /// <inheritdoc />
    public ValidTest(ITestOutputHelper output) : base(output)
    {
    }

    /// <summary>
    /// 测试 - IsNull - 当值为 null 时应返回 true
    /// </summary>
    [Fact]
    public void IsNull_ShouldReturnTrue_WhenValueIsNull()
    {
        // Arrange
        object value = null;

        // Act
        var result = Valid.IsNull(value);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsNull - 当值不为 null 时应返回 false
    /// </summary>
    [Fact]
    public void IsNull_ShouldReturnFalse_WhenValueIsNotNull()
    {
        // Arrange
        object value = new object();

        // Act
        var result = Valid.IsNull(value);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 测试 - IsNotNull - 当值不为 null 时应返回 true
    /// </summary>
    [Fact]
    public void IsNotNull_ShouldReturnTrue_WhenValueIsNotNull()
    {
        // Arrange
        object value = new object();

        // Act
        var result = Valid.IsNotNull(value);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试 - IsNotNull - 当值为 null 时应返回 false
    /// </summary>
    [Fact]
    public void IsNotNull_ShouldReturnFalse_WhenValueIsNull()
    {
        // Arrange
        object value = null;

        // Act
        var result = Valid.IsNotNull(value);

        // Assert
        Assert.False(result);
    }
}