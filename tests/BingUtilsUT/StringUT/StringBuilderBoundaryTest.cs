using Bing.Text;
using System.Text;

namespace BingUtilsUT.StringUT;

/// <summary>
/// 测试类：覆盖 Strings.StringBuilder 相关方法的边界行为与对象契约。
/// </summary>
[Trait("StringUT", "Strings.StringBuilder.Boundary")]
public class StringBuilderBoundaryTest
{
    /// <summary>
    /// 测试用例：Reverse 传入 null 时不应抛异常。
    /// </summary>
    [Fact]
    public void Reverse_NullBuilder_DoesNotThrow()
    {
        Should.NotThrow(() => Strings.Reverse((StringBuilder)null));
    }

    /// <summary>
    /// 测试用例：Reverse 传入空实例时应保持为空。
    /// </summary>
    [Fact]
    public void Reverse_EmptyBuilder_RemainsEmpty()
    {
        var builder = new StringBuilder();

        Strings.Reverse(builder);

        builder.ToString().ShouldBe(string.Empty);
    }

    /// <summary>
    /// 测试用例：ReverseAndReturnNewInstance 传入 null 时应返回新的空实例。
    /// </summary>
    [Fact]
    public void ReverseAndReturnNewInstance_NullBuilder_ReturnsNewEmptyBuilder()
    {
        var result = Strings.ReverseAndReturnNewInstance(null);

        result.ShouldNotBeNull();
        result.Length.ShouldBe(0);
    }

    /// <summary>
    /// 测试用例：ReverseAndToString 传入 null/空实例时应返回空字符串。
    /// </summary>
    [Fact]
    public void ReverseAndToString_NullOrEmptyBuilder_ReturnsEmptyString()
    {
        Strings.ReverseAndToString(null).ShouldBe(string.Empty);
        Strings.ReverseAndToString(new StringBuilder()).ShouldBe(string.Empty);
    }

    /// <summary>
    /// 测试用例：RemoveStart 命中前缀时，应原地修改并返回同一实例。
    /// </summary>
    [Fact]
    public void RemoveStart_PrefixMatched_MutatesAndReturnsSameInstance()
    {
        var builder = new StringBuilder("prefix-content");

        var result = Strings.RemoveStart(builder, "prefix-");

        ReferenceEquals(builder, result).ShouldBeTrue();
        result!.ToString().ShouldBe("content");
    }

    /// <summary>
    /// 测试用例：RemoveEnd 命中后缀时，应原地修改并返回同一实例。
    /// </summary>
    [Fact]
    public void RemoveEnd_SuffixMatched_MutatesAndReturnsSameInstance()
    {
        var builder = new StringBuilder("content-suffix");

        var result = Strings.RemoveEnd(builder, "-suffix");

        ReferenceEquals(builder, result).ShouldBeTrue();
        result!.ToString().ShouldBe("content");
    }

    /// <summary>
    /// 测试用例：RemoveStart/RemoveEnd 未命中时，应返回同一实例且内容不变。
    /// </summary>
    [Fact]
    public void RemoveStartOrEnd_NotMatched_ReturnsSameInstanceWithoutMutation()
    {
        var builder = new StringBuilder("content");

        var removeStartResult = Strings.RemoveStart(builder, "prefix-");
        var removeEndResult = Strings.RemoveEnd(builder, "-suffix");

        ReferenceEquals(builder, removeStartResult).ShouldBeTrue();
        ReferenceEquals(builder, removeEndResult).ShouldBeTrue();
        builder.ToString().ShouldBe("content");
    }
}
