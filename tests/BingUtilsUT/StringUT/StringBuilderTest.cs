using System.Collections.Generic;
using System.Text;
using Bing.Text;

namespace BingUtilsUT.StringUT;

/// <summary>
/// 验证 StringBuilder 的反转和前后缀删除行为。
/// </summary>
[Trait("StringUT", "Strings.StringBuilder")]
public class StringBuilderTest
{
    /// <summary>
    /// 验证反转操作修改原 StringBuilder 实例。
    /// </summary>
    [Theory]
    [MemberData(nameof(GetReverseCases))]
    public void Reverse_Self_ShouldMutateBuilder(string source, string expected)
    {
        var builder = source == null ? null : new StringBuilder(source);

        Strings.Reverse(builder);

        if (builder == null)
            builder.ShouldBeNull();
        else
            builder.ToString().ShouldBe(expected);
    }

    /// <summary>
    /// 验证反转副本操作返回新实例且保留原内容。
    /// </summary>
    [Theory]
    [MemberData(nameof(GetReverseCases))]
    public void ReverseAndReturnNewInstance_ShouldReturnReversedWithoutMutatingOriginal(string source, string expected)
    {
        var builder = source == null ? null : new StringBuilder(source);
        var original = builder?.ToString();

        var result = Strings.ReverseAndReturnNewInstance(builder);

        result.ShouldNotBeNull();
        result.ToString().ShouldBe(expected);

        if (builder != null)
        {
            builder.ToString().ShouldBe(original);
            ReferenceEquals(result, builder).ShouldBeFalse();
        }
    }

    /// <summary>
    /// 验证反转字符串操作返回反转结果且保留原内容。
    /// </summary>
    [Theory]
    [MemberData(nameof(GetReverseCases))]
    public void ReverseAndToString_ShouldReturnReversedWithoutMutatingOriginal(string source, string expected)
    {
        var builder = source == null ? null : new StringBuilder(source);
        var original = builder?.ToString();

        var result = Strings.ReverseAndToString(builder);

        result.ShouldBe(expected);
        if (builder != null)
            builder.ToString().ShouldBe(original);
    }

    /// <summary>
    /// 验证传入 null 时删除前缀操作返回 null。
    /// </summary>
    [Fact]
    public void RemoveStart_NullBuilder_ShouldReturnNull()
    {
        StringBuilder value = null;

        var result = Strings.RemoveStart(value, "a");

        result.ShouldBeNull();
    }

    /// <summary>
    /// 验证删除匹配前缀时修改并返回原 StringBuilder 实例。
    /// </summary>
    /// <param name="value">待处理的原始字符串。</param>
    /// <param name="removeValue">要删除的前缀；为 null 时不删除内容。</param>
    /// <param name="expected">预期的处理结果。</param>
    [Theory]
    [MemberData(nameof(GetRemoveStartCases))]
    public void RemoveStart_BoundaryCases_ShouldReturnExpected(string value, string removeValue, string expected)
    {
        var builder = new StringBuilder(value);

        var result = Strings.RemoveStart(builder, removeValue);

        result.ShouldBeSameAs(builder);
        result.ToString().ShouldBe(expected);
    }

    /// <summary>
    /// 验证传入 null 时删除后缀操作返回 null。
    /// </summary>
    [Fact]
    public void RemoveEnd_NullBuilder_ShouldReturnNull()
    {
        StringBuilder value = null;

        var result = Strings.RemoveEnd(value, "a");

        result.ShouldBeNull();
    }

    /// <summary>
    /// 验证删除匹配后缀时修改并返回原 StringBuilder 实例。
    /// </summary>
    /// <param name="value">待处理的原始字符串。</param>
    /// <param name="removeValue">要删除的后缀；为 null 时不删除内容。</param>
    /// <param name="expected">预期的处理结果。</param>
    [Theory]
    [MemberData(nameof(GetRemoveEndCases))]
    public void RemoveEnd_BoundaryCases_ShouldReturnExpected(string value, string removeValue, string expected)
    {
        var builder = new StringBuilder(value);

        var result = Strings.RemoveEnd(builder, removeValue);

        result.ShouldBeSameAs(builder);
        result.ToString().ShouldBe(expected);
    }

    /// <summary>
    /// 获取反转操作的输入及预期结果用例。
    /// </summary>
    /// <returns>包含原始字符串和反转结果的测试用例。</returns>
    public static IEnumerable<object[]> GetReverseCases()
    {
        yield return new object[] { null, string.Empty };
        yield return new object[] { string.Empty, string.Empty };
        yield return new object[] { "ABC", "CBA" };
        yield return new object[] { "ab cd", "dc ba" };
        yield return new object[] { "12345", "54321" };
    }

    /// <summary>
    /// 获取删除前缀操作的边界输入及预期结果用例。
    /// </summary>
    /// <returns>包含原始字符串、待删除前缀和预期结果的测试用例。</returns>
    public static IEnumerable<object[]> GetRemoveStartCases()
    {
        yield return new object[] { string.Empty, string.Empty, string.Empty };
        yield return new object[] { "abc", null, "abc" };
        yield return new object[] { "a", "b", "a" };
        yield return new object[] { "ab", "b", "ab" };
        yield return new object[] { "ab", "a", "b" };
        yield return new object[] { "abc", "ab", "c" };
        yield return new object[] { "abc", "Ab", "abc" };
        yield return new object[] { "abc", "abc", string.Empty };
        yield return new object[] { "ab", "abc", "ab" };
        yield return new object[] { "a.cs.cshtml", "a.cs", ".cshtml" };
        yield return new object[] { "\r\na", "\r\n", "a" };
    }

    /// <summary>
    /// 获取删除后缀操作的边界输入及预期结果用例。
    /// </summary>
    /// <returns>包含原始字符串、待删除后缀和预期结果的测试用例。</returns>
    public static IEnumerable<object[]> GetRemoveEndCases()
    {
        yield return new object[] { string.Empty, string.Empty, string.Empty };
        yield return new object[] { "abc", null, "abc" };
        yield return new object[] { "a", "b", "a" };
        yield return new object[] { "ab", "a", "ab" };
        yield return new object[] { "ab", "b", "a" };
        yield return new object[] { "abc", "abc", string.Empty };
        yield return new object[] { "bc", "abc", "bc" };
        yield return new object[] { "ab", "abc", "ab" };
        yield return new object[] { "a.cs.cshtml", ".cshtml", "a.cs" };
        yield return new object[] { "a\r\n", "\r\n", "a" };
    }
}
