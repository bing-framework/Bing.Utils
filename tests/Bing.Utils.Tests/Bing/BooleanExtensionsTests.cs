namespace Bing.Utils.Tests.Bing;

/// <summary>
/// <see cref="BooleanExtensions"/> 单元测试
/// </summary>
public class BooleanExtensionsTests
{
    // ─────────────────────────────────────────────────────────────────
    // IfTrue / IfFalse (Action)
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证真值执行条件回调。
    /// </summary>
    [Fact]
    public void IfTrue_True_InvokesAction()
    {
        var called = false;
        true.IfTrue(() => called = true);
        called.ShouldBeTrue();
    }

    /// <summary>
    /// 验证假值不执行真值回调。
    /// </summary>
    [Fact]
    public void IfTrue_False_DoesNotInvokeAction()
    {
        var called = false;
        false.IfTrue(() => called = true);
        called.ShouldBeFalse();
    }

    /// <summary>
    /// 验证可空假值不执行真值回调。
    /// </summary>
    [Fact]
    public void IfTrue_NullableFalse_DoesNotInvokeAction()
    {
        var called = false;
        ((bool?)false).IfTrue(() => called = true);
        called.ShouldBeFalse();
    }

    /// <summary>
    /// 验证空布尔值不执行真值回调。
    /// </summary>
    [Fact]
    public void IfTrue_NullableNull_DoesNotInvokeAction()
    {
        var called = false;
        ((bool?)null).IfTrue(() => called = true);
        called.ShouldBeFalse();
    }

    /// <summary>
    /// 验证假值执行条件回调。
    /// </summary>
    [Fact]
    public void IfFalse_False_InvokesAction()
    {
        var called = false;
        false.IfFalse(() => called = true);
        called.ShouldBeTrue();
    }

    /// <summary>
    /// 验证真值不执行假值回调。
    /// </summary>
    [Fact]
    public void IfFalse_True_DoesNotInvokeAction()
    {
        var called = false;
        true.IfFalse(() => called = true);
        called.ShouldBeFalse();
    }

    /// <summary>
    /// 验证空布尔值执行假值回调。
    /// </summary>
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

    /// <summary>
    /// 验证真值返回给定条件值。
    /// </summary>
    [Fact]
    public void IfTrue_Generic_True_ReturnsValue() =>
        true.IfTrue("yes").ShouldBe("yes");

    /// <summary>
    /// 验证假值的真值分支返回默认值。
    /// </summary>
    [Fact]
    public void IfTrue_Generic_False_ReturnsDefault() =>
        false.IfTrue("yes").ShouldBeNull();

    /// <summary>
    /// 验证假值返回给定条件值。
    /// </summary>
    [Fact]
    public void IfFalse_Generic_False_ReturnsValue() =>
        false.IfFalse("no").ShouldBe("no");

    /// <summary>
    /// 验证真值的假值分支返回默认值。
    /// </summary>
    [Fact]
    public void IfFalse_Generic_True_ReturnsDefault() =>
        true.IfFalse("no").ShouldBeNull();

    // ─────────────────────────────────────────────────────────────────
    // IfTtt — Action overload
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证真值选择真分支回调。
    /// </summary>
    [Fact]
    public void IfTtt_True_InvokesTrueAction()
    {
        var result = "";
        true.IfTtt(() => result = "A", () => result = "B");
        result.ShouldBe("A");
    }

    /// <summary>
    /// 验证假值选择假分支回调。
    /// </summary>
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

    /// <summary>
    /// 验证真值返回真分支工厂结果。
    /// </summary>
    [Fact]
    public void IfTtt_Generic_True_ReturnsTrueValue() =>
        true.IfTtt(() => "A", () => "B").ShouldBe("A");

    /// <summary>
    /// 验证假值返回假分支工厂结果。
    /// </summary>
    [Fact]
    public void IfTtt_Generic_False_ReturnsFalseValue() =>
        false.IfTtt(() => "A", () => "B").ShouldBe("B");

    // ─────────────────────────────────────────────────────────────────
    // IfTrueThenThrow / IfFalseThenThrow
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证真值触发给定异常。
    /// </summary>
    [Fact]
    public void IfTrueThenThrow_True_Throws()
    {
        var ex = new InvalidOperationException("boom");
        Should.Throw<InvalidOperationException>(() => true.IfTrueThenThrow(ex));
    }

    /// <summary>
    /// 验证假值不触发真值条件异常。
    /// </summary>
    [Fact]
    public void IfTrueThenThrow_False_DoesNotThrow()
    {
        Should.NotThrow(() => false.IfTrueThenThrow(new Exception("silent")));
    }

    /// <summary>
    /// 验证假值触发给定异常。
    /// </summary>
    [Fact]
    public void IfFalseThenThrow_False_Throws()
    {
        var ex = new InvalidOperationException("boom");
        Should.Throw<InvalidOperationException>(() => false.IfFalseThenThrow(ex));
    }

    /// <summary>
    /// 验证真值不触发假值条件异常。
    /// </summary>
    [Fact]
    public void IfFalseThenThrow_True_DoesNotThrow()
    {
        Should.NotThrow(() => true.IfFalseThenThrow(new Exception("silent")));
    }

    // ─────────────────────────────────────────────────────────────────
    // ToBinary
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证真值转换为字节一。
    /// </summary>
    [Fact]
    public void ToBinary_True_Returns1() => true.ToBinary().ShouldBe((byte)1);

    /// <summary>
    /// 验证假值转换为字节零。
    /// </summary>
    [Fact]
    public void ToBinary_False_Returns0() => false.ToBinary().ShouldBe((byte)0);

    // ─────────────────────────────────────────────────────────────────
    // ToString(trueString, falseString)
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证真值转换为指定真值文本。
    /// </summary>
    [Fact]
    public void ToString_True_ReturnsTrueString() =>
        true.ToString("yes", "no").ShouldBe("yes");

    /// <summary>
    /// 验证假值转换为指定假值文本。
    /// </summary>
    [Fact]
    public void ToString_False_ReturnsFalseString() =>
        false.ToString("yes", "no").ShouldBe("no");

    /// <summary>
    /// 验证可空真值转换为指定真值文本。
    /// </summary>
    [Fact]
    public void ToString_NullableTrue_ReturnsTrueString() =>
        ((bool?)true).ToString("yes", "no").ShouldBe("yes");

    /// <summary>
    /// 验证空布尔值转换为指定假值文本。
    /// </summary>
    [Fact]
    public void ToString_NullableNull_ReturnsFalseString() =>
        ((bool?)null).ToString("yes", "no").ShouldBe("no");

    // ─────────────────────────────────────────────────────────────────
    // ToChineseString
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证真值转换为中文“是”。
    /// </summary>
    [Fact]
    public void ToChineseString_True_ReturnsYes() =>
        true.ToChineseString().ShouldBe("是");

    /// <summary>
    /// 验证假值转换为中文“否”。
    /// </summary>
    [Fact]
    public void ToChineseString_False_ReturnsNo() =>
        false.ToChineseString().ShouldBe("否");

    /// <summary>
    /// 验证可空假值转换为中文“否”。
    /// </summary>
    [Fact]
    public void ToChineseString_NullableFalse_ReturnsNo() =>
        ((bool?)false).ToChineseString().ShouldBe("否");
}
