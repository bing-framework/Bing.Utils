namespace BingUtilsUT.StringUT;

[Trait("StringUT", "Strings.Remove")]
public class StringRemoveTest
{
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
    public void Test_RemoveStart(string value, string removeValue, string result)
    {
        Assert.Equal(result, Bing.Text.Strings.RemoveStart(value, removeValue));
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
    public void Test_RemoveEnd(string value, string removeValue, string result)
    {
        Assert.Equal(result, Bing.Text.Strings.RemoveEnd(value, removeValue));
    }
}