using Bing.Exceptions;

namespace Bing.Utils.Tests.Bing.Exceptions;

/// <summary>
/// <see cref="ExceptionHelper"/>、<see cref="ExceptionHelperExtensions"/>、
/// <see cref="ExceptionExtensions"/> 单元测试
/// </summary>
public class ExceptionHelperTests
{
    // ─────────────────────────────────────────────────────────────────
    // ExceptionHelper.GetExceptionDetail
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void GetExceptionDetail_ContainsExceptionMessage()
    {
        var ex = new InvalidOperationException("test-msg");
        var detail = ExceptionHelper.GetExceptionDetail(ex);
        detail.ShouldContain("test-msg");
    }

    [Fact]
    public void GetExceptionDetail_ContainsExceptionType()
    {
        var ex = new InvalidOperationException("msg");
        var detail = ExceptionHelper.GetExceptionDetail(ex);
        detail.ShouldNotBeNullOrEmpty();
    }

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

    [Fact]
    public void Unwrap_NoInnerException_ReturnsSelf()
    {
        var ex = new Exception("root");
        ExceptionHelper.Unwrap(ex).ShouldBeSameAs(ex);
    }

    [Fact]
    public void Unwrap_WithInnerException_ReturnsInnermost()
    {
        var inner = new InvalidOperationException("inner");
        var outer = new Exception("outer", inner);
        ExceptionHelper.Unwrap(outer).ShouldBeSameAs(inner);
    }

    [Fact]
    public void Unwrap_DeepNested_ReturnsDeepestInner()
    {
        var deepest = new ArgumentException("deepest");
        var mid = new InvalidOperationException("mid", deepest);
        var outer = new Exception("outer", mid);
        ExceptionHelper.Unwrap(outer).ShouldBeSameAs(deepest);
    }

    [Fact]
    public void Unwrap_NullException_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ExceptionHelper.Unwrap(null!));
    }

    // ─────────────────────────────────────────────────────────────────
    // ExceptionHelper.Unwrap(Type) — typed overload
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void UnwrapTyped_FindsMatchingType()
    {
        var inner = new ArgumentException("found");
        var outer = new Exception("outer", inner);
        var result = ExceptionHelper.Unwrap(outer, typeof(ArgumentException));
        result.ShouldBeSameAs(inner);
    }

    [Fact]
    public void UnwrapTyped_NoMatchingType_ReturnsNull()
    {
        var ex = new Exception("no match");
        var result = ExceptionHelper.Unwrap(ex, typeof(ArgumentException));
        result.ShouldBeNull();
    }

    [Fact]
    public void UnwrapTyped_DerivedClass_FoundWhenMayDerivedClassTrue()
    {
        var inner = new ArgumentNullException("p"); // ArgumentNullException : ArgumentException
        var outer = new Exception("outer", inner);
        var result = ExceptionHelper.Unwrap(outer, typeof(ArgumentException), mayDerivedClass: true);
        result.ShouldBeSameAs(inner);
    }

    [Fact]
    public void UnwrapTyped_DerivedClass_NotFoundWhenMayDerivedClassFalse()
    {
        var inner = new ArgumentNullException("p");
        var outer = new Exception("outer", inner);
        var result = ExceptionHelper.Unwrap(outer, typeof(ArgumentException), mayDerivedClass: false);
        result.ShouldBeNull();
    }

    [Fact]
    public void UnwrapTyped_NullException_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ExceptionHelper.Unwrap(null!, typeof(ArgumentException)));
    }

    [Fact]
    public void UnwrapTyped_NullType_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ExceptionHelper.Unwrap(new Exception("x"), null!));
    }

    [Fact]
    public void UnwrapTyped_NonExceptionType_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => ExceptionHelper.Unwrap(new Exception("x"), typeof(string)));
    }

    // ─────────────────────────────────────────────────────────────────
    // ExceptionHelper.Unwrap<TException> (generic)
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void UnwrapGeneric_FindsCorrectType()
    {
        var inner = new ArgumentException("found");
        var outer = new Exception("outer", inner);
        ExceptionHelper.Unwrap<ArgumentException>(outer).ShouldBeSameAs(inner);
    }

    [Fact]
    public void UnwrapGeneric_NoMatch_ReturnsNull()
    {
        var ex = new Exception("no match");
        ExceptionHelper.Unwrap<ArgumentException>(ex).ShouldBeNull();
    }

    // ─────────────────────────────────────────────────────────────────
    // ExceptionHelperExtensions.Unwrap (instance ext)
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ExtUnwrap_NoInner_ReturnsSelf()
    {
        var ex = new Exception("solo");
        ex.Unwrap().ShouldBeSameAs(ex);
    }

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

    [Fact]
    public void FormatMessage_ContainsExceptionMessage()
    {
        var ex = new Exception("hello-world");
        ex.FormatMessage().ShouldContain("hello-world");
    }

    [Fact]
    public void FormatMessage_ContainsTypeFullName()
    {
        var ex = new InvalidOperationException("test");
        ex.FormatMessage().ShouldContain(typeof(InvalidOperationException).FullName!);
    }

    [Fact]
    public void FormatMessage_HideStackTrace_DoesNotContainStack()
    {
        var ex = new Exception("msg");
        var fmt = ex.FormatMessage(isHideStackTrace: true);
        fmt.ShouldNotContain("异常堆栈");
    }

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

    [Fact]
    public void ThrowIf_ConditionTrue_ThrowsException()
    {
        var ex = new InvalidOperationException("thrown");
        Should.Throw<InvalidOperationException>(() => ex.ThrowIf(true));
    }

    [Fact]
    public void ThrowIf_ConditionFalse_DoesNotThrow()
    {
        var ex = new InvalidOperationException("not thrown");
        Should.NotThrow(() => ex.ThrowIf(false));
    }

    [Fact]
    public void ThrowIf_FuncConditionTrue_ThrowsException()
    {
        var ex = new Exception("thrown via func");
        Should.Throw<Exception>(() => ex.ThrowIf(() => true));
    }

    [Fact]
    public void ThrowIf_FuncConditionFalse_DoesNotThrow()
    {
        var ex = new Exception("not thrown via func");
        Should.NotThrow(() => ex.ThrowIf(() => false));
    }

    // ─────────────────────────────────────────────────────────────────
    // ExceptionHelper.PrepareForRethrow
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void PrepareForRethrow_RethrowsOriginalException()
    {
        var original = new InvalidOperationException("rethrown");
        Should.Throw<InvalidOperationException>(() => ExceptionHelper.PrepareForRethrow(original));
    }
}
