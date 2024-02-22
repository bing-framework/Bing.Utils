using Bing.Text;

namespace BingUtilsUT.StringUT;

[Trait("StringUT", "Strings.StringBuilder")]
public class StringBuilderTest
{
    /// <summary>
    /// 测试 - 反转字符串 - 自身
    /// </summary>
    [Fact]
    public void Test_Reverse_Self()
    {
        var builder = new StringBuilder();
        builder.Append("A");
        builder.Append("B");
        builder.Append("C");

        Strings.Reverse(builder);

        builder.ToString().ShouldBe("CBA");
    }

    /// <summary>
    /// 测试 - 反转字符串
    /// </summary>
    [Fact]
    public void Test_ReverseAndReturnNewInstance()
    {
        var builder = new StringBuilder();
        builder.Append("A");
        builder.Append("B");
        builder.Append("C");

        var sb = Strings.ReverseAndReturnNewInstance(builder);
        sb.GetHashCode().ShouldNotBe(builder.GetHashCode());
        sb.ToString().ShouldBe("CBA");
    }

    /// <summary>
    /// 测试 - 反转字符串
    /// </summary>
    [Fact]
    public void Test_ReverseAndToString()
    {
        var builder = new StringBuilder();
        builder.Append("A");
        builder.Append("B");
        builder.Append("C");

        var val = Strings.ReverseAndToString(builder);

        builder.ToString().ShouldBe("ABC");
        val.ShouldBe("CBA");
    }

    #region RemoveStart

    /// <summary>
    /// 测试 - 移除起始字符串
    /// </summary>
    [Theory]
    [InlineData(null, null, null)]
    [InlineData(null, "a", null)]
    public void Test_RemoveStart_StringBuilder(StringBuilder value, string removeValue, string result)
    {
        Assert.Equal(result, Strings.RemoveStart(value, removeValue)?.ToString());
    }

    /// <summary>
    /// 测试 - 移除起始字符串
    /// </summary>
    [Theory]
    [InlineData(null, null, "")]
    [InlineData(null, "a", "")]
    [InlineData("", "", "")]
    [InlineData("a", "b", "a")]
    [InlineData("ab", "b", "ab")]
    [InlineData("ab", "a", "b")]
    [InlineData("abc", "ab", "c")]
    [InlineData("abc", "Ab", "abc")]
    [InlineData("abc", "abc", "")]
    [InlineData("ab", "abc", "ab")]
    [InlineData("a.cs.cshtml", "a.cs", ".cshtml")]
    [InlineData("\r\na", "\r\n", "a")]
    public void Test_RemoveStart_StringBuilder_2(string value, string removeValue, string result)
    {
        var builder = new StringBuilder(value);
        Assert.Equal(result, Strings.RemoveStart(builder, removeValue)?.ToString());
    }

    #endregion

    #region RemoveEnd

    /// <summary>
    /// 测试 - 移除末尾字符串
    /// </summary>
    [Theory]
    [InlineData(null, null, null)]
    [InlineData(null, "a", null)]
    public void Test_RemoveEnd_StringBuilder(StringBuilder value, string removeValue, string result)
    {
        Assert.Equal(result, Strings.RemoveEnd(value, removeValue)?.ToString());
    }

    /// <summary>
    /// 测试 - 移除末尾字符串
    /// </summary>
    [Theory]
    [InlineData(null, null, "")]
    [InlineData(null, "a", "")]
    [InlineData("", "", "")]
    [InlineData("a", "b", "a")]
    [InlineData("ab", "a", "ab")]
    [InlineData("ab", "b", "a")]
    [InlineData("abc", "abc", "")]
    [InlineData("bc", "abc", "bc")]
    [InlineData("ab", "abc", "ab")]
    [InlineData("a.cs.cshtml", ".cshtml", "a.cs")]
    [InlineData("a\r\n", "\r\n", "a")]
    public void Test_RemoveEnd_StringBuilder_2(string value, string removeValue, string result)
    {
        var builder = new StringBuilder(value);
        Assert.Equal(result, Strings.RemoveEnd(builder, removeValue)?.ToString());
    }

    #endregion
}