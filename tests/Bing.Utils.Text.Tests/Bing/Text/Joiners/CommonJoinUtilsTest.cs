using System.Text;
using Bing.Text.Joiners;

namespace Bing.Text;

/// <summary>
/// 通用连接工具的条件、替代值和索引重载测试。
/// </summary>
[Trait("TextUT", "CommonJoinUtils")]
public class CommonJoinUtilsTest
{
    #region JoinToString (Func<T, bool> predicate)

    /// <summary>
    /// 验证空源列表不会追加文本。
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
    /// 验证所有元素通过筛选时按分隔符连接。
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
    /// 验证仅连接通过筛选的元素。
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
    /// 验证未通过筛选的元素使用替代值参与连接。
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
    /// 验证空列表不会追加文本。
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
    /// 验证空谓词连接全部元素。
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
    /// 验证带索引重载按分隔符连接全部元素。
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
    /// 验证带索引谓词只连接偶数索引元素。
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
    /// 验证带索引替代函数替换未通过筛选的元素。
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
    /// 验证带索引重载的空源列表不会追加文本。
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
