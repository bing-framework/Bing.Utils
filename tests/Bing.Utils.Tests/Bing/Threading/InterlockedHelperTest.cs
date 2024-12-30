namespace Bing.Threading;

/// <summary>
/// 线程安全的读取操作的帮助类 测试
/// </summary>
public class InterlockedHelperTest
{
    /// <summary>
    /// 测试 - 线程安全地读取整数值
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
    /// 测试 - 线程安全地读取浮点数值
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
    /// 测试 - 线程安全地读取双精度浮点数值
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
    /// 测试 - 线程安全地读取引用类型的值
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