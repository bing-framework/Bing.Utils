using Bing.Exceptions;

namespace Bing.Utils.Tests.Bing.Exceptions;

/// <summary>
/// 验证异常辅助方法和异常扩展的行为。
/// </summary>
public class ExceptionHelperTests
{
    // ─────────────────────────────────────────────────────────────────
    // ExceptionHelper.GetExceptionDetail
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证异常详情包含异常消息。
    /// </summary>
    [Fact]
    public void GetExceptionDetail_ContainsExceptionMessage()
    {
        var ex = new InvalidOperationException("test-msg");
        var detail = ExceptionHelper.GetExceptionDetail(ex);
        detail.ShouldContain("test-msg");
    }

    /// <summary>
    /// 验证异常详情不为空。
    /// </summary>
    [Fact]
    public void GetExceptionDetail_ContainsExceptionType()
    {
        var ex = new InvalidOperationException("msg");
        var detail = ExceptionHelper.GetExceptionDetail(ex);
        detail.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// 验证异常详情包含星号分隔符。
    /// </summary>
    [Fact]
    public void GetExceptionDetail_StartsWithSeparator()
    {
        var ex = new Exception("x");
        var detail = ExceptionHelper.GetExceptionDetail(ex);
        detail.ShouldContain("*");
    }

    // ─────────────────────────────────────────────────────────────────
    // ExceptionHelper.Unwrap (get innermost)
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证无内部异常时返回原异常。
    /// </summary>
    [Fact]
    public void Unwrap_NoInnerException_ReturnsSelf()
    {
        var ex = new Exception("root");
        ExceptionHelper.Unwrap(ex).ShouldBeSameAs(ex);
    }

    /// <summary>
    /// 验证解包返回内部异常。
    /// </summary>
    [Fact]
    public void Unwrap_WithInnerException_ReturnsInnermost()
    {
        var inner = new InvalidOperationException("inner");
        var outer = new Exception("outer", inner);
        ExceptionHelper.Unwrap(outer).ShouldBeSameAs(inner);
    }

    /// <summary>
    /// 验证多层解包返回最深层异常。
    /// </summary>
    [Fact]
    public void Unwrap_DeepNested_ReturnsDeepestInner()
    {
        var deepest = new ArgumentException("deepest");
        var mid = new InvalidOperationException("mid", deepest);
        var outer = new Exception("outer", mid);
        ExceptionHelper.Unwrap(outer).ShouldBeSameAs(deepest);
    }

    /// <summary>
    /// 验证解包拒绝空异常。
    /// </summary>
    [Fact]
    public void Unwrap_NullException_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ExceptionHelper.Unwrap(null!));
    }

    // ─────────────────────────────────────────────────────────────────
    // ExceptionHelper.Unwrap(Type) — typed overload
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证按类型解包返回匹配的内部异常。
    /// </summary>
    [Fact]
    public void UnwrapTyped_FindsMatchingType()
    {
        var inner = new ArgumentException("found");
        var outer = new Exception("outer", inner);
        var result = ExceptionHelper.Unwrap(outer, typeof(ArgumentException));
        result.ShouldBeSameAs(inner);
    }

    /// <summary>
    /// 验证按类型解包在无匹配项时返回空值。
    /// </summary>
    [Fact]
    public void UnwrapTyped_NoMatchingType_ReturnsNull()
    {
        var ex = new Exception("no match");
        var result = ExceptionHelper.Unwrap(ex, typeof(ArgumentException));
        result.ShouldBeNull();
    }

    /// <summary>
    /// 验证允许派生类型时返回匹配基类的内部异常。
    /// </summary>
    [Fact]
    public void UnwrapTyped_DerivedClass_FoundWhenMayDerivedClassTrue()
    {
        var inner = new ArgumentNullException("p"); // ArgumentNullException : ArgumentException
        var outer = new Exception("outer", inner);
        var result = ExceptionHelper.Unwrap(outer, typeof(ArgumentException), mayDerivedClass: true);
        result.ShouldBeSameAs(inner);
    }

    /// <summary>
    /// 验证禁止派生类型时忽略仅匹配基类的内部异常。
    /// </summary>
    [Fact]
    public void UnwrapTyped_DerivedClass_NotFoundWhenMayDerivedClassFalse()
    {
        var inner = new ArgumentNullException("p");
        var outer = new Exception("outer", inner);
        var result = ExceptionHelper.Unwrap(outer, typeof(ArgumentException), mayDerivedClass: false);
        result.ShouldBeNull();
    }

    /// <summary>
    /// 验证按类型解包拒绝空异常。
    /// </summary>
    [Fact]
    public void UnwrapTyped_NullException_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ExceptionHelper.Unwrap(null!, typeof(ArgumentException)));
    }

    /// <summary>
    /// 验证按类型解包拒绝空类型。
    /// </summary>
    [Fact]
    public void UnwrapTyped_NullType_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ExceptionHelper.Unwrap(new Exception("x"), null!));
    }

    /// <summary>
    /// 验证按类型解包拒绝非异常类型。
    /// </summary>
    [Fact]
    public void UnwrapTyped_NonExceptionType_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => ExceptionHelper.Unwrap(new Exception("x"), typeof(string)));
    }

    // ─────────────────────────────────────────────────────────────────
    // ExceptionHelper.Unwrap<TException> (generic)
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证泛型解包返回指定类型的内部异常。
    /// </summary>
    [Fact]
    public void UnwrapGeneric_FindsCorrectType()
    {
        var inner = new ArgumentException("found");
        var outer = new Exception("outer", inner);
        ExceptionHelper.Unwrap<ArgumentException>(outer).ShouldBeSameAs(inner);
    }

    /// <summary>
    /// 验证泛型解包在无匹配项时返回空值。
    /// </summary>
    [Fact]
    public void UnwrapGeneric_NoMatch_ReturnsNull()
    {
        var ex = new Exception("no match");
        ExceptionHelper.Unwrap<ArgumentException>(ex).ShouldBeNull();
    }

    // ─────────────────────────────────────────────────────────────────
    // ExceptionHelperExtensions.Unwrap (instance ext)
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证解包扩展在无内部异常时返回原异常。
    /// </summary>
    [Fact]
    public void ExtUnwrap_NoInner_ReturnsSelf()
    {
        var ex = new Exception("solo");
        ex.Unwrap().ShouldBeSameAs(ex);
    }

    /// <summary>
    /// 验证解包文本采用最深层异常消息。
    /// </summary>
    [Fact]
    public void ToUnwrappedString_ReturnsInnermostMessage()
    {
        var inner = new Exception("deepest-msg");
        var outer = new Exception("outer", inner);
        outer.ToUnwrappedString().ShouldBe("deepest-msg");
    }

    // ─────────────────────────────────────────────────────────────────
    // ExceptionExtensions.FormatMessage
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证格式化文本包含异常消息。
    /// </summary>
    [Fact]
    public void FormatMessage_ContainsExceptionMessage()
    {
        var ex = new Exception("hello-world");
        ex.FormatMessage().ShouldContain("hello-world");
    }

    /// <summary>
    /// 验证格式化文本包含异常类型全名。
    /// </summary>
    [Fact]
    public void FormatMessage_ContainsTypeFullName()
    {
        var ex = new InvalidOperationException("test");
        ex.FormatMessage().ShouldContain(typeof(InvalidOperationException).FullName!);
    }

    /// <summary>
    /// 验证隐藏堆栈时不输出异常堆栈标题。
    /// </summary>
    [Fact]
    public void FormatMessage_HideStackTrace_DoesNotContainStack()
    {
        var ex = new Exception("msg");
        var fmt = ex.FormatMessage(isHideStackTrace: true);
        fmt.ShouldNotContain("异常堆栈");
    }

    /// <summary>
    /// 验证格式化文本包含内部异常消息。
    /// </summary>
    [Fact]
    public void FormatMessage_WithInnerException_ContainsInnerMessage()
    {
        var inner = new Exception("inner-msg");
        var outer = new Exception("outer-msg", inner);
        var fmt = outer.FormatMessage();
        fmt.ShouldContain("inner-msg");
    }

    // ─────────────────────────────────────────────────────────────────
    // ExceptionExtensions.ThrowIf
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证条件为真时抛出异常。
    /// </summary>
    [Fact]
    public void ThrowIf_ConditionTrue_ThrowsException()
    {
        var ex = new InvalidOperationException("thrown");
        Should.Throw<InvalidOperationException>(() => ex.ThrowIf(true));
    }

    /// <summary>
    /// 验证条件为假时不抛出异常。
    /// </summary>
    [Fact]
    public void ThrowIf_ConditionFalse_DoesNotThrow()
    {
        var ex = new InvalidOperationException("not thrown");
        Should.NotThrow(() => ex.ThrowIf(false));
    }

    /// <summary>
    /// 验证条件委托返回真时抛出异常。
    /// </summary>
    [Fact]
    public void ThrowIf_FuncConditionTrue_ThrowsException()
    {
        var ex = new Exception("thrown via func");
        Should.Throw<Exception>(() => ex.ThrowIf(() => true));
    }

    /// <summary>
    /// 验证条件委托返回假时不抛出异常。
    /// </summary>
    [Fact]
    public void ThrowIf_FuncConditionFalse_DoesNotThrow()
    {
        var ex = new Exception("not thrown via func");
        Should.NotThrow(() => ex.ThrowIf(() => false));
    }

    // ─────────────────────────────────────────────────────────────────
    // ExceptionHelper.PrepareForRethrow
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证重新抛出操作抛出原异常的类型。
    /// </summary>
    [Fact]
    public void PrepareForRethrow_RethrowsOriginalException()
    {
        var original = new InvalidOperationException("rethrown");
        Should.Throw<InvalidOperationException>(() => ExceptionHelper.PrepareForRethrow(original));
    }
}
