namespace Bing.Utils.Tests.Bing;

/// <summary>
/// <see cref="BooleanExtensions"/> 单元测试
/// </summary>
public class BooleanExtensionsTests
{
    // ─────────────────────────────────────────────────────────────────
    // IfTrue / IfFalse (Action)
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void IfTrue_True_InvokesAction()
    {
        var called = false;
        true.IfTrue(() => called = true);
        called.ShouldBeTrue();
    }

    [Fact]
    public void IfTrue_False_DoesNotInvokeAction()
    {
        var called = false;
        false.IfTrue(() => called = true);
        called.ShouldBeFalse();
    }

    [Fact]
    public void IfTrue_NullableFalse_DoesNotInvokeAction()
    {
        var called = false;
        ((bool?)false).IfTrue(() => called = true);
        called.ShouldBeFalse();
    }

    [Fact]
    public void IfTrue_NullableNull_DoesNotInvokeAction()
    {
        var called = false;
        ((bool?)null).IfTrue(() => called = true);
        called.ShouldBeFalse();
    }

    [Fact]
    public void IfFalse_False_InvokesAction()
    {
        var called = false;
        false.IfFalse(() => called = true);
        called.ShouldBeTrue();
    }

    [Fact]
    public void IfFalse_True_DoesNotInvokeAction()
    {
        var called = false;
        true.IfFalse(() => called = true);
        called.ShouldBeFalse();
    }

    [Fact]
    public void IfFalse_NullableNull_InvokesAction()
    {
        var called = false;
        ((bool?)null).IfFalse(() => called = true);
        called.ShouldBeTrue();
    }

    // ─────────────────────────────────────────────────────────────────
    // IfTrue<T> / IfFalse<T> (return value)
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void IfTrue_Generic_True_ReturnsValue() =>
        true.IfTrue("yes").ShouldBe("yes");

    [Fact]
    public void IfTrue_Generic_False_ReturnsDefault() =>
        false.IfTrue("yes").ShouldBeNull();

    [Fact]
    public void IfFalse_Generic_False_ReturnsValue() =>
        false.IfFalse("no").ShouldBe("no");

    [Fact]
    public void IfFalse_Generic_True_ReturnsDefault() =>
        true.IfFalse("no").ShouldBeNull();

    // ─────────────────────────────────────────────────────────────────
    // IfTtt — Action overload
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void IfTtt_True_InvokesTrueAction()
    {
        var result = "";
        true.IfTtt(() => result = "A", () => result = "B");
        result.ShouldBe("A");
    }

    [Fact]
    public void IfTtt_False_InvokesFalseAction()
    {
        var result = "";
        false.IfTtt(() => result = "A", () => result = "B");
        result.ShouldBe("B");
    }

    // ─────────────────────────────────────────────────────────────────
    // IfTtt<T> — Func overload
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void IfTtt_Generic_True_ReturnsTrueValue() =>
        true.IfTtt(() => "A", () => "B").ShouldBe("A");

    [Fact]
    public void IfTtt_Generic_False_ReturnsFalseValue() =>
        false.IfTtt(() => "A", () => "B").ShouldBe("B");

    // ─────────────────────────────────────────────────────────────────
    // IfTrueThenThrow / IfFalseThenThrow
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void IfTrueThenThrow_True_Throws()
    {
        var ex = new InvalidOperationException("boom");
        Should.Throw<InvalidOperationException>(() => true.IfTrueThenThrow(ex));
    }

    [Fact]
    public void IfTrueThenThrow_False_DoesNotThrow()
    {
        Should.NotThrow(() => false.IfTrueThenThrow(new Exception("silent")));
    }

    [Fact]
    public void IfFalseThenThrow_False_Throws()
    {
        var ex = new InvalidOperationException("boom");
        Should.Throw<InvalidOperationException>(() => false.IfFalseThenThrow(ex));
    }

    [Fact]
    public void IfFalseThenThrow_True_DoesNotThrow()
    {
        Should.NotThrow(() => true.IfFalseThenThrow(new Exception("silent")));
    }

    // ─────────────────────────────────────────────────────────────────
    // ToBinary
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ToBinary_True_Returns1() => true.ToBinary().ShouldBe((byte)1);

    [Fact]
    public void ToBinary_False_Returns0() => false.ToBinary().ShouldBe((byte)0);

    // ─────────────────────────────────────────────────────────────────
    // ToString(trueString, falseString)
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ToString_True_ReturnsTrueString() =>
        true.ToString("yes", "no").ShouldBe("yes");

    [Fact]
    public void ToString_False_ReturnsFalseString() =>
        false.ToString("yes", "no").ShouldBe("no");

    [Fact]
    public void ToString_NullableTrue_ReturnsTrueString() =>
        ((bool?)true).ToString("yes", "no").ShouldBe("yes");

    [Fact]
    public void ToString_NullableNull_ReturnsFalseString() =>
        ((bool?)null).ToString("yes", "no").ShouldBe("no");

    // ─────────────────────────────────────────────────────────────────
    // ToChineseString
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ToChineseString_True_ReturnsYes() =>
        true.ToChineseString().ShouldBe("是");

    [Fact]
    public void ToChineseString_False_ReturnsNo() =>
        false.ToChineseString().ShouldBe("否");

    [Fact]
    public void ToChineseString_NullableFalse_ReturnsNo() =>
        ((bool?)false).ToChineseString().ShouldBe("否");
}
