namespace Bing.Extensions.Base;
/// <summary>
/// 布尔值扩展测试
/// </summary>
[Trait("ExtensionsUT", "Boolean")]
public class BooleanExtensionsTest
{
    #region MustTrue
    /// <summary>
    /// 测试 - MustTrue - 值为真不抛异常
    /// </summary>
    [Fact]
    public void MustTrue_ValueIsTrue_NoExceptionThrown()
    {
        // Arrange
        bool value = true;
        // Act & Assert
        // 不应抛出异常
        value.MustTrue();
    }
    /// <summary>
    /// 测试 - MustTrue - 值为假抛出异常
    /// </summary>
    [Fact]
    public void MustTrue_ValueIsFalse_ThrowArgumentException()
    {
        // Arrange
        bool value = false;
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => value.MustTrue());
        Assert.Equal("值必须为真", exception.Message);
    }
    #endregion
    #region MustFalse
    /// <summary>
    /// 测试 - MustFalse - 值为假不抛异常
    /// </summary>
    [Fact]
    public void MustFalse_ValueIsFalse_NoExceptionThrown()
    {
        // Arrange
        bool value = false;
        // Act & Assert
        // 不应抛出异常
        value.MustFalse();
    }
    /// <summary>
    /// 测试 - MustFalse - 值为真抛出异常
    /// </summary>
    [Fact]
    public void MustFalse_ValueIsTrue_ThrowArgumentException()
    {
        // Arrange
        bool value = true;
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => value.MustFalse());
        Assert.Equal("值必须为假", exception.Message);
    }
    #endregion
    #region And
    /// <summary>
    /// 测试 - And - 两个值都为真时返回真
    /// </summary>
    [Fact]
    public void And_BothValuesAreTrue_ReturnTrue()
    {
        // Arrange
        bool value = true;
        bool condition = true;
        // Act
        var result = value.And(condition);
        // Assert
        Assert.True(result);
    }
    /// <summary>
    /// 测试 - And - 第一个值为真第二个值为假时返回假
    /// </summary>
    [Fact]
    public void And_FirstTrueSecondFalse_ReturnFalse()
    {
        // Arrange
        bool value = true;
        bool condition = false;
        // Act
        var result = value.And(condition);
        // Assert
        Assert.False(result);
    }
    /// <summary>
    /// 测试 - And - 第一个值为假第二个值为真时返回假
    /// </summary>
    [Fact]
    public void And_FirstFalseSecondTrue_ReturnFalse()
    {
        // Arrange
        bool value = false;
        bool condition = true;
        // Act
        var result = value.And(condition);
        // Assert
        Assert.False(result);
    }
    /// <summary>
    /// 测试 - And - 两个值都为假时返回假
    /// </summary>
    [Fact]
    public void And_BothValuesAreFalse_ReturnFalse()
    {
        // Arrange
        bool value = false;
        bool condition = false;
        // Act
        var result = value.And(condition);
        // Assert
        Assert.False(result);
    }
    /// <summary>
    /// 测试 - And - 链式调用正确计算结果
    /// </summary>
    [Fact]
    public void And_ChainedCalls_EvaluateCorrectly()
    {
        // Arrange
        bool first = true;
        bool second = true;
        bool third = false;
        // Act
        var result = first.And(second).And(third);
        // Assert
        Assert.False(result);
    }
    #endregion
    #region Or
    /// <summary>
    /// 测试 - Or - 两个值都为真时返回真
    /// </summary>
    [Fact]
    public void Or_BothValuesAreTrue_ReturnTrue()
    {
        // Arrange
        bool value = true;
        bool condition = true;
        // Act
        var result = value.Or(condition);
        // Assert
        Assert.True(result);
    }
    /// <summary>
    /// 测试 - Or - 第一个值为真第二个值为假时返回真
    /// </summary>
    [Fact]
    public void Or_FirstTrueSecondFalse_ReturnTrue()
    {
        // Arrange
        bool value = true;
        bool condition = false;
        // Act
        var result = value.Or(condition);
        // Assert
        Assert.True(result);
    }
    /// <summary>
    /// 测试 - Or - 第一个值为假第二个值为真时返回真
    /// </summary>
    [Fact]
    public void Or_FirstFalseSecondTrue_ReturnTrue()
    {
        // Arrange
        bool value = false;
        bool condition = true;
        // Act
        var result = value.Or(condition);
        // Assert
        Assert.True(result);
    }
    /// <summary>
    /// 测试 - Or - 两个值都为假时返回假
    /// </summary>
    [Fact]
    public void Or_BothValuesAreFalse_ReturnFalse()
    {
        // Arrange
        bool value = false;
        bool condition = false;
        // Act
        var result = value.Or(condition);
        // Assert
        Assert.False(result);
    }
    /// <summary>
    /// 测试 - Or - 链式调用正确计算结果
    /// </summary>
    [Fact]
    public void Or_ChainedCalls_EvaluateCorrectly()
    {
        // Arrange
        bool first = false;
        bool second = false;
        bool third = true;
        // Act
        var result = first.Or(second).Or(third);
        // Assert
        Assert.True(result);
    }
    #endregion
}
