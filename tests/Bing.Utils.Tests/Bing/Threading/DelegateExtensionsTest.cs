namespace Bing.Threading;

/// <summary>
/// 委托(<see cref="Delegate"/>) 扩展 测试
/// </summary>
public class DelegateExtensionsTest
{
    /// <summary>
    /// 测试 - 安全调用委托 - 非空委托
    /// </summary>
    [Fact]
    public void Test_InvokeSafe_NonNullDelegate()
    {
        // Arrange
        int result = 0;
        Action<int> action = x => result = x;

        // Act
        action.InvokeSafe(5);

        // Assert
        Assert.Equal(5, result);
    }

    /// <summary>
    /// 测试 - 安全调用委托 - 空委托
    /// </summary>
    [Fact]
    public void Test_InvokeSafe_NullDelegate()
    {
        // Arrange
        Action action = null;

        // Act & Assert
        // 确保不会抛出异常
        action.InvokeSafe();
    }

    /// <summary>
    /// 测试 - 尝试执行委托 - 成功情况
    /// </summary>
    [Fact]
    public void Test_Try_Success()
    {
        // Arrange
        int result = 0;
        Action<int> action = x => result = x;

        // Act
        var success = action.Try(5);

        // Assert
        Assert.True(success);
        Assert.Equal(5, result);
    }

    /// <summary>
    /// 测试 - 尝试执行委托 - 失败情况
    /// </summary>
    [Fact]
    public void Test_Try_Failure()
    {
        // Arrange
        Action action = () => throw new Exception("Test exception");

        // Act
        var success = action.Try();

        // Assert
        Assert.False(success);
    }

    /// <summary>
    /// 测试 - 尝试执行委托 - 重试情况
    /// </summary>
    [Fact]
    public void Test_Try_WithRetries()
    {
        // Arrange
        int callCount = 0;
        Action action = () =>
        {
            callCount++;
            if (callCount < 3)
                throw new Exception("Test exception");
        };

        // Act
        var success = action.Try(3, TimeSpan.FromMilliseconds(10));

        // Assert
        Assert.True(success);
        Assert.Equal(3, callCount);
    }

    /// <summary>
    /// 测试 - 尝试执行委托 - 达到最大重试次数仍失败
    /// </summary>
    [Fact]
    public void Test_Try_ExceedMaxRetries()
    {
        // Arrange
        int callCount = 0;
        Action action = () =>
        {
            callCount++;
            throw new Exception("Test exception");
        };

        // Act
        var success = action.Try(3, TimeSpan.FromMilliseconds(10));

        // Assert
        Assert.False(success);
        Assert.Equal(3, callCount);
    }

    /// <summary>
    /// 测试 - 调用委托并返回结果 - 成功情况
    /// </summary>
    [Fact]
    public void Test_InvokeOrDefault_Success()
    {
        // Arrange
        Func<int, int> func = x => x * 2;

        // Act
        var result = func.InvokeOrDefault(0, 5);

        // Assert
        Assert.Equal(10, result);
    }

    /// <summary>
    /// 测试 - 调用委托并返回结果 - 失败情况
    /// </summary>
    [Fact]
    public void Test_InvokeOrDefault_Failure()
    {
        // Arrange
        Func<int> func = () => throw new Exception("Test exception");

        // Act
        var result = func.InvokeOrDefault(42);

        // Assert
        Assert.Equal(42, result);
    }

    /// <summary>
    /// 测试 - 调用委托并返回结果 - 类型不匹配情况
    /// </summary>
    [Fact]
    public void Test_InvokeOrDefault_TypeMismatch()
    {
        // Arrange
        Delegate del = new Func<string>(() => "test");
        int defaultValue = 42;

        // Act
        // 使用 InvokeOrDefault<int> 尝试将 string 转换为 int，应该返回默认值
        var result = del.InvokeOrDefault(defaultValue);

        // Assert
        Assert.Equal(defaultValue, result);
    }

    /// <summary>
    /// 测试 - 调用委托并返回结果 - 重试情况
    /// </summary>
    [Fact]
    public void Test_InvokeOrDefault_WithRetries()
    {
        // Arrange
        int callCount = 0;
        Func<int> func = () =>
        {
            callCount++;
            if (callCount < 3)
                throw new Exception("Test exception");
            return 99;
        };

        // Act
        var result = func.InvokeOrDefault(3, TimeSpan.FromMilliseconds(10), 42);

        // Assert
        Assert.Equal(99, result);
        Assert.Equal(3, callCount);
    }

    /// <summary>
    /// 测试 - 调用委托并返回结果 - 达到最大重试次数仍失败
    /// </summary>
    [Fact]
    public void Test_InvokeOrDefault_ExceedMaxRetries()
    {
        // Arrange
        int callCount = 0;
        Func<int> func = () =>
        {
            callCount++;
            throw new Exception("Test exception");
        };
        int defaultValue = 42;

        // Act
        var result = func.InvokeOrDefault(3, TimeSpan.FromMilliseconds(10), defaultValue);

        // Assert
        Assert.Equal(defaultValue, result);
        Assert.Equal(3, callCount);
    }
}