extern alias guard;
using ValidationExceptionHelper = guard::Bing.Validation.ValidationExceptionHelper;

namespace Bing.Validation;

/// <summary>
/// 验证异常帮助方法根据断言结果创建并抛出指定异常。
/// </summary>
public class ValidationExceptionHelperTest
{
    /// <summary>
    /// 验证断言为 true 时调用不会抛出异常。
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
    /// 验证断言为 false 且未提供参数时抛出指定异常。
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
    /// 验证断言为 false 时将参数名传入异常构造函数。
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
    /// 验证断言为 false 时将多个构造参数传入异常。
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
    /// 验证泛型异常类型可通过反射构造并抛出。
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
        // 通过反射调用时，异常会被 TargetInvocationException 包裹
        var outer = Assert.Throws<System.Reflection.TargetInvocationException>(() =>
            method.Invoke(null, new object[] { assertion, new object[] { } }));

        Assert.IsType(exceptionType, outer.InnerException);
    }

    /// <summary>
    /// 验证连续调用时每次均根据断言结果决定是否抛出异常。
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
