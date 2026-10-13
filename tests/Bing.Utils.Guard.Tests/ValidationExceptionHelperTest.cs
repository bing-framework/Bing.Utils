extern alias guard;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using ValidationExceptionHelper = guard::Bing.Validation.ValidationExceptionHelper;

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
    /// 测试目的：验证 null 参数被类型推断过滤后，仍保持旧实现忽略多余参数的行为。
    /// </summary>
    [Fact]
    public void WrapAndRaise_NullArgumentWithParameterlessConstructor_PreservesBehavior()
    {
        var exception = Assert.Throws<NullTolerantException>(() =>
            ValidationExceptionHelper.WrapAndAndRaise<NullTolerantException>(false, new object[] { null! }));

        Assert.Equal("parameterless", exception.Message);
    }

    /// <summary>
    /// 测试目的：验证异常构造函数抛错时仍传播原始异常，而不是暴露 TargetInvocationException。
    /// </summary>
    [Fact]
    public void WrapAndRaise_ThrowingConstructor_PreservesOriginalException()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ValidationExceptionHelper.WrapAndAndRaise<ThrowingException>(false));

        Assert.Equal("constructor failure", exception.Message);
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
        // 通过反射调用时，异常会被 TargetInvocationException 包裹
        var outer = Assert.Throws<System.Reflection.TargetInvocationException>(() =>
            method.Invoke(null, new object[] { assertion, new object[] { } }));

        Assert.IsType(exceptionType, outer.InnerException);
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

    private sealed class NullTolerantException : Exception
    {
        public NullTolerantException() : base("parameterless") { }

        public NullTolerantException(string message) : base(message) { }
    }

    private sealed class ThrowingException : Exception
    {
        public ThrowingException() => throw new InvalidOperationException("constructor failure");
    }

#if NET6_0_OR_GREATER
    /// <summary>
    /// 测试目的：验证动态异常类型经过 Guard 构造后不会被构造函数缓存固定。
    /// </summary>
    [Fact]
    public void WrapAndRaise_CollectibleException_DoesNotPinAssembly()
    {
        var assembly = UseCollectibleException();
        for (var attempt = 0; attempt < 20 && assembly.IsAlive; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        Assert.False(assembly.IsAlive);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference UseCollectibleException()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName($"GuardCollectibleException_{Guid.NewGuid():N}"),
            AssemblyBuilderAccess.RunAndCollect);
        var module = assembly.DefineDynamicModule("Main");
        var builder = module.DefineType(
            "CollectibleValidationException",
            TypeAttributes.Public | TypeAttributes.Class,
            typeof(Exception));
        var constructor = builder.DefineConstructor(
            MethodAttributes.Public,
            CallingConventions.Standard,
            Type.EmptyTypes);
        var il = constructor.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, typeof(Exception).GetConstructor(Type.EmptyTypes)!);
        il.Emit(OpCodes.Ret);
        var exceptionType = builder.CreateTypeInfo()!.AsType();

        var method = typeof(ValidationExceptionHelper)
            .GetMethod(nameof(ValidationExceptionHelper.WrapAndAndRaise))!
            .MakeGenericMethod(exceptionType);
        var outer = Assert.Throws<TargetInvocationException>(() =>
            method.Invoke(null, new object[] { false, Array.Empty<object>() }));

        Assert.NotNull(outer.InnerException);
        Assert.Equal(exceptionType, outer.InnerException!.GetType());
        return new WeakReference(assembly);
    }
#endif
}
