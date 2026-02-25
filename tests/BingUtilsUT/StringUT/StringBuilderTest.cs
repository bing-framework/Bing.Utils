using System.Collections.Generic;
using System.Text;
using Bing.Text;

namespace BingUtilsUT.StringUT;

[Trait("StringUT", "Strings.StringBuilder")]
public class StringBuilderTest
{
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

    [Fact]
    public void RemoveStart_NullBuilder_ShouldReturnNull()
    {
        StringBuilder value = null;

        var result = Strings.RemoveStart(value, "a");

        result.ShouldBeNull();
    }

    [Theory]
    [MemberData(nameof(GetRemoveStartCases))]
    public void RemoveStart_BoundaryCases_ShouldReturnExpected(string value, string removeValue, string expected)
    {
        var builder = new StringBuilder(value);

        var result = Strings.RemoveStart(builder, removeValue);

        result.ShouldBeSameAs(builder);
        result.ToString().ShouldBe(expected);
    }

    [Fact]
    public void RemoveEnd_NullBuilder_ShouldReturnNull()
    {
        StringBuilder value = null;

        var result = Strings.RemoveEnd(value, "a");

        result.ShouldBeNull();
    }

    [Theory]
    [MemberData(nameof(GetRemoveEndCases))]
    public void RemoveEnd_BoundaryCases_ShouldReturnExpected(string value, string removeValue, string expected)
    {
        var builder = new StringBuilder(value);

        var result = Strings.RemoveEnd(builder, removeValue);

        result.ShouldBeSameAs(builder);
        result.ToString().ShouldBe(expected);
    }

    public static IEnumerable<object[]> GetReverseCases()
    {
        yield return new object[] { null, string.Empty };
        yield return new object[] { string.Empty, string.Empty };
        yield return new object[] { "ABC", "CBA" };
        yield return new object[] { "ab cd", "dc ba" };
        yield return new object[] { "12345", "54321" };
    }

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