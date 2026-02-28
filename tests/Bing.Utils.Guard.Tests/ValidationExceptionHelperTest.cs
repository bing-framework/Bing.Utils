using Bing.Validation;

namespace Bing.Validation;

/// <summary>
/// 验证异常帮助类测试
/// </summary>
public class ValidationExceptionHelperTest
{
    /// <summary>
    /// 测试目的：验证当断言为 true 时，不会抛出异常
    /// </summary>
    [Fact]
    public void WrapAndRaise_AssertionTrue_DoesNotThrowException()
    {
        // Arrange
        var assertion = true;

        // Act & Assert
        var exception = Record.Exception(() =>
            ValidationExceptionHelper.WrapAndAndRaise<ArgumentException>(assertion, "test"));

        Assert.Null(exception);
    }

    /// <summary>
    /// 测试目的：验证当断言为 false 时，抛出指定类型的异常（无参数构造函数）
    /// </summary>
    [Fact]
    public void WrapAndRaise_AssertionFalse_ThrowsArgumentException()
    {
        // Arrange
        var assertion = false;

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            ValidationExceptionHelper.WrapAndAndRaise<ArgumentException>(assertion));
    }

    /// <summary>
    /// 测试目的：验证当断言为 false 时，抛出带参数的异常
    /// </summary>
    [Fact]
    public void WrapAndRaise_AssertionFalseWithMessage_ThrowsExceptionWithMessage()
    {
        // Arrange
        var assertion = false;
        var paramName = "testParam";

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            ValidationExceptionHelper.WrapAndAndRaise<ArgumentNullException>(assertion, paramName));

        Assert.Equal(paramName, exception.ParamName);
    }

    /// <summary>
    /// 测试目的：验证抛出带多个参数的异常
    /// </summary>
    [Fact]
    public void WrapAndRaise_AssertionFalseWithMultipleParams_ThrowsExceptionWithParams()
    {
        // Arrange
        var assertion = false;
        var paramName = "testParam";
        var message = "Test message";

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            ValidationExceptionHelper.WrapAndAndRaise<ArgumentNullException>(assertion, paramName, message));

        Assert.Equal(paramName, exception.ParamName);
        Assert.Contains(message, exception.Message);
    }

    /// <summary>
    /// 测试目的：验证抛出不同类型的异常
    /// </summary>
    [Theory]
    [InlineData(typeof(ArgumentException))]
    [InlineData(typeof(ArgumentNullException))]
    [InlineData(typeof(InvalidOperationException))]
    public void WrapAndRaise_AssertionFalse_ThrowsCorrectExceptionType(Type exceptionType)
    {
        // Arrange
        var assertion = false;
        var method = typeof(ValidationExceptionHelper)
            .GetMethod(nameof(ValidationExceptionHelper.WrapAndAndRaise))
            .MakeGenericMethod(exceptionType);

        // Act & Assert
        var exception = Assert.Throws(exceptionType, () =>
            method.Invoke(null, new object[] { assertion, new object[] { } }));

        Assert.IsType(exceptionType, exception);
    }

    /// <summary>
    /// 测试目的：验证多次调用时的正确性
    /// </summary>
    [Fact]
    public void WrapAndRaise_MultipleCalls_WorksCorrectly()
    {
        // Arrange & Act & Assert
        // 第一次：断言为 true，不抛出异常
        var exception1 = Record.Exception(() =>
            ValidationExceptionHelper.WrapAndAndRaise<ArgumentException>(true, "param1"));
        Assert.Null(exception1);

        // 第二次：断言为 false，抛出异常
        Assert.Throws<ArgumentException>(() =>
            ValidationExceptionHelper.WrapAndAndRaise<ArgumentException>(false, "param2"));

        // 第三次：断言为 true，不抛出异常
        var exception3 = Record.Exception(() =>
            ValidationExceptionHelper.WrapAndAndRaise<InvalidOperationException>(true));
        Assert.Null(exception3);
    }
}
