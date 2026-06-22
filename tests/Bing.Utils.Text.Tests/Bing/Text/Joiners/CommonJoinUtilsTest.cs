using System.Text;
using Bing.Text.Joiners;

namespace Bing.Text;

/// <summary>
/// 测试类：覆盖 <see cref="CommonJoinUtils"/> 的 JoinToString 两个重载。
/// </summary>
[Trait("TextUT", "CommonJoinUtils")]
public class CommonJoinUtilsTest
{
    #region JoinToString (Func<T, bool> predicate)

    /// <summary>
    /// 测试用例：验证 <see cref="CommonJoinUtils.JoinToString{T,TContainer}"/> 在
    /// `NullList` 场景下，结果为 `ContainerUnchanged`。
    /// </summary>
    [Fact]
    public void JoinToString_NullList_ContainerUnchanged()
    {
        var sb = new StringBuilder();
        CommonJoinUtils.JoinToString<int, StringBuilder>(sb, (c, s) => c.Append(s),
            null, ",", _ => true, x => x.ToString());
        sb.ToString().ShouldBe(string.Empty);
    }

    /// <summary>
    /// 测试用例：验证 <see cref="CommonJoinUtils.JoinToString{T,TContainer}"/> 在
    /// `AllItemsPass` 场景下，结果为 `AllItemsJoined`。
    /// </summary>
    [Fact]
    public void JoinToString_AllItemsPass_AllItemsJoined()
    {
        var sb = new StringBuilder();
        var list = new[] { 1, 2, 3 };
        CommonJoinUtils.JoinToString(sb, (c, s) => c.Append(s),
            list, ",", _ => true, x => x.ToString());
        sb.ToString().ShouldBe("1,2,3");
    }

    /// <summary>
    /// 测试用例：验证 <see cref="CommonJoinUtils.JoinToString{T,TContainer}"/> 在
    /// `SomeItemsFiltered` 场景下，结果为 `OnlyPassingItemsJoined`。
    /// </summary>
    [Fact]
    public void JoinToString_SomeItemsFiltered_OnlyPassingItemsJoined()
    {
        var sb = new StringBuilder();
        var list = new[] { 1, 2, 3, 4, 5 };
        CommonJoinUtils.JoinToString(sb, (c, s) => c.Append(s),
            list, ",", x => x % 2 == 1, x => x.ToString()); // 只保留奇数
        sb.ToString().ShouldBe("1,3,5");
    }

    /// <summary>
    /// 测试用例：验证 <see cref="CommonJoinUtils.JoinToString{T,TContainer}"/> 在
    /// `WithReplaceFunc` 场景下，结果为 `ReplacedItemIncluded`。
    /// </summary>
    [Fact]
    public void JoinToString_WithReplaceFunc_ReplacedItemIncluded()
    {
        var sb = new StringBuilder();
        var list = new[] { 1, 2, 3 };
        CommonJoinUtils.JoinToString(sb, (c, s) => c.Append(s),
            list, ",",
            x => x != 2,         // 偶数不通过
            x => x.ToString(),
            _ => 0);              // 偶数用 0 替换
        sb.ToString().ShouldBe("1,0,3");
    }

    /// <summary>
    /// 测试用例：验证 <see cref="CommonJoinUtils.JoinToString{T,TContainer}"/> 在
    /// `EmptyList` 场景下，结果为 `ContainerUnchanged`。
    /// </summary>
    [Fact]
    public void JoinToString_EmptyList_ContainerUnchanged()
    {
        var sb = new StringBuilder();
        CommonJoinUtils.JoinToString(sb, (c, s) => c.Append(s),
            Array.Empty<int>(), ",", _ => true, x => x.ToString());
        sb.ToString().ShouldBe(string.Empty);
    }

    /// <summary>
    /// 测试用例：验证 <see cref="CommonJoinUtils.JoinToString{T,TContainer}"/> 在
    /// `NullPredicate` 场景下（predicate=null），结果为 `AllItemsIncluded`。
    /// </summary>
    [Fact]
    public void JoinToString_NullPredicate_AllItemsIncluded()
    {
        var sb = new StringBuilder();
        var list = new[] { "a", "b", "c" };
        CommonJoinUtils.JoinToString<string, StringBuilder>(sb, (c, s) => c.Append(s),
            list, "-", null, x => x);
        sb.ToString().ShouldBe("a-b-c");
    }

    #endregion

    #region JoinToString (Func<T, int, bool> predicate with index)

    /// <summary>
    /// 测试用例：验证带索引版本的 <see cref="CommonJoinUtils.JoinToString{T,TContainer}"/>
    /// 在 `AllItemsPass` 场景下，结果为 `AllItemsJoined`。
    /// </summary>
    [Fact]
    public void JoinToStringWithIndex_AllItemsPass_AllItemsJoined()
    {
        var sb = new StringBuilder();
        var list = new[] { "x", "y", "z" };
        CommonJoinUtils.JoinToString(sb, (c, s) => c.Append(s),
            list, "|", (_, _) => true, (x, _) => x);
        sb.ToString().ShouldBe("x|y|z");
    }

    /// <summary>
    /// 测试用例：验证带索引版本的 <see cref="CommonJoinUtils.JoinToString{T,TContainer}"/>
    /// 在 `FilterByIndex` 场景下，结果为 `OnlyEvenIndexJoined`。
    /// </summary>
    [Fact]
    public void JoinToStringWithIndex_FilterByIndex_OnlyEvenIndexJoined()
    {
        var sb = new StringBuilder();
        var list = new[] { "a", "b", "c", "d", "e" };
        CommonJoinUtils.JoinToString(sb, (c, s) => c.Append(s),
            list, ",", (_, i) => i % 2 == 0, (x, _) => x); // 只保留偶数索引项
        sb.ToString().ShouldBe("a,c,e");
    }

    /// <summary>
    /// 测试用例：验证带索引版本的 <see cref="CommonJoinUtils.JoinToString{T,TContainer}"/>
    /// 在 `WithIndexedReplace` 场景下，结果为 `ReplacedByIndex`。
    /// </summary>
    [Fact]
    public void JoinToStringWithIndex_WithIndexedReplace_ReplacedByIndex()
    {
        var sb = new StringBuilder();
        var list = new[] { "a", "b", "c" };
        CommonJoinUtils.JoinToString(sb, (c, s) => c.Append(s),
            list, ",",
            (x, _) => x != "b",
            (x, _) => x,
            (_, _) => "X"); // b 替换为 X
        sb.ToString().ShouldBe("a,X,c");
    }

    /// <summary>
    /// 测试用例：验证带索引版本在 `NullList` 场景下，结果为 `ContainerUnchanged`。
    /// </summary>
    [Fact]
    public void JoinToStringWithIndex_NullList_ContainerUnchanged()
    {
        var sb = new StringBuilder();
        CommonJoinUtils.JoinToString<string, StringBuilder>(sb, (c, s) => c.Append(s),
            null, ",", (_, _) => true, (x, _) => x);
        sb.ToString().ShouldBe(string.Empty);
    }

    #endregion
}
