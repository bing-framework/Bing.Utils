namespace Bing.Threading;
/// <summary>
/// 线程安全的读取操作的帮助类 测试
/// </summary>
public class InterlockedHelperTest
{
    /// <summary>
    /// 验证读取整数时返回原值。
    /// </summary>
    [Fact]
    public void Read_ShouldReturnCorrectIntValue()
    {
        // Arrange
        int value = 42;
        // Act
        int result = InterlockedHelper.Read(ref value);
        // Assert
        Assert.Equal(42, result);
    }
    /// <summary>
    /// 验证读取单精度数时返回原值。
    /// </summary>
    [Fact]
    public void Read_ShouldReturnCorrectFloatValue()
    {
        // Arrange
        float value = 42.42f;
        // Act
        float result = InterlockedHelper.Read(ref value);
        // Assert
        Assert.Equal(42.42f, result);
    }
    /// <summary>
    /// 验证读取双精度数时返回原值。
    /// </summary>
    [Fact]
    public void Read_ShouldReturnCorrectDoubleValue()
    {
        // Arrange
        double value = 42.42;
        // Act
        double result = InterlockedHelper.Read(ref value);
        // Assert
        Assert.Equal(42.42, result);
    }
    /// <summary>
    /// 验证读取引用类型时返回原值。
    /// </summary>
    [Fact]
    public void Read_ShouldReturnCorrectReferenceTypeValue()
    {
        // Arrange
        string value = "Hello, World!";
        // Act
        string result = InterlockedHelper.Read(ref value);
        // Assert
        Assert.Equal("Hello, World!", result);
    }
}
