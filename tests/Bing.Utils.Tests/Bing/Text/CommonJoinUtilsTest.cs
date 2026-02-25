using System.Text;
using Bing.Text.Joiners;

namespace Bing.Text;

/// <summary>
/// 测试类：覆盖 `CommonJoinUtils` 相关行为。
/// </summary>
[Trait("TextUT", "CommonJoinUtils")]
public class CommonJoinUtilsTest
{
    /// <summary>
    /// 测试用例：验证 `JoinToString` 在 `BasicEnumerable` 场景下，结果为 `PreservesOrderAndDelimiter`。
    /// </summary>
    [Fact]
    public void JoinToString_BasicEnumerable_PreservesOrderAndDelimiter()
    {
        var builder = new StringBuilder();
        var values = new[] { "a", "b", "c" };

        CommonJoinUtils.JoinToString(
            builder,
            static (b, text) => b.Append(text),
            values,
            ",",
            static _ => true,
            static item => item);

        builder.ToString().ShouldBe("a,b,c");
        values.ShouldBe(new[] { "a", "b", "c" });
    }

    /// <summary>
    /// 测试用例：验证 `JoinToString` 在 `PredicateRejectAndNoReplace` 场景下，结果为 `SkipsItemsWithoutExtraDelimiter`。
    /// </summary>
    [Fact]
    public void JoinToString_PredicateRejectAndNoReplace_SkipsItemsWithoutExtraDelimiter()
    {
        var builder = new StringBuilder();

        CommonJoinUtils.JoinToString(
            builder,
            static (b, text) => b.Append(text),
            new[] { 1, 2, 3, 4 },
            "|",
            static item => item % 2 == 0,
            static item => item.ToString());

        builder.ToString().ShouldBe("2|4");
    }

    /// <summary>
    /// 测试用例：验证 `JoinToString` 在 `PredicateRejectWithReplace` 场景下，结果为 `UsesReplacementValue`。
    /// </summary>
    [Fact]
    public void JoinToString_PredicateRejectWithReplace_UsesReplacementValue()
    {
        var builder = new StringBuilder();

        CommonJoinUtils.JoinToString(
            builder,
            static (b, text) => b.Append(text),
            new[] { 1, 2, 3 },
            ",",
            static item => item % 2 == 0,
            static item => item.ToString(),
            static _ => -1);

        builder.ToString().ShouldBe("-1,2,-1");
    }

    /// <summary>
    /// 测试用例：验证 `JoinToString` 在 `NullList` 场景下，结果为 `KeepsContainerUnchanged`。
    /// </summary>
    [Fact]
    public void JoinToString_NullList_KeepsContainerUnchanged()
    {
        var builder = new StringBuilder("seed");

        CommonJoinUtils.JoinToString<string, StringBuilder>(
            builder,
            static (b, text) => b.Append(text),
            null,
            ",",
            static _ => true,
            static item => item);

        builder.ToString().ShouldBe("seed");
    }

    /// <summary>
    /// 测试用例：验证 `JoinToString` 在 `IndexOverload` 场景下，结果为 `ProvidesStableIndex`。
    /// </summary>
    [Fact]
    public void JoinToString_IndexOverload_ProvidesStableIndex()
    {
        var builder = new StringBuilder();

        CommonJoinUtils.JoinToString(
            builder,
            static (b, text) => b.Append(text),
            new[] { "x", "y", "z" },
            ";",
            static (_, index) => index != 1,
            static (item, index) => $"{index}:{item}",
            static (_, index) => $"R{index}");

        builder.ToString().ShouldBe("0:x;1:R1;2:z");
    }

    /// <summary>
    /// 测试用例：验证 `JoinToString` 在 `NullContainerUpdateFunc` 场景下，结果为 `ThrowsNullReferenceException`。
    /// </summary>
    [Fact]
    public void JoinToString_NullContainerUpdateFunc_ThrowsNullReferenceException()
    {
        var builder = new StringBuilder();

        Should.Throw<NullReferenceException>(() =>
            CommonJoinUtils.JoinToString(
                builder,
                null,
                new[] { 1 },
                ",",
                static _ => true,
                static item => item.ToString()));
    }
}
