using Bing.Helpers;

namespace Bing.Utils.Tests.Helpers;

/// <summary>
/// 正则操作测试
/// </summary>
public class RegexsTest : TestBase
{
    /// <summary>
    /// 初始化一个<see cref="TestBase"/>类型的实例
    /// </summary>
    public RegexsTest(ITestOutputHelper output) : base(output)
    {
    }

    /// <summary>
    /// 测试获取值
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="pattern">模式字符串</param>
    /// <param name="resultPattern">结果模式字符串</param>
    /// <param name="result">结果</param>
    [Theory]
    [InlineData("", "", "", "")]
    [InlineData("123", "a", "", "")]
    [InlineData("123", @"\d", "", "1")]
    [InlineData("123abc456", @"\d+([a-z]+\d+)", "$1", "abc456")]
    [InlineData("123abc456", @"\d+([a-z]\d+)", "$1", "")]
    public void Test_GetValue(string input, string pattern, string resultPattern, string result)
    {
        Assert.Equal(result, Regexs.GetValue(input, pattern, resultPattern));
    }

    /// <summary>
    /// 测试获取值集合
    /// </summary>
    [Fact]
    public void Test_GetValues()
    {
        Assert.Empty(Regexs.GetValues("", "", null));
        Assert.Empty(Regexs.GetValues("123abc456", @"\d{5}", new[] { "$1" }));
        Assert.Equal("123", Regexs.GetValues("123abc456", @"(\d*)", new[] { "$1" })["$1"]);
        Assert.Equal("abc", Regexs.GetValues("123abc456", @"\d*([a-z]*)\d*", new[] { "$1" })["$1"]);
        Assert.Equal("123", Regexs.GetValues("123abc456", @"(\d*)([a-z]*)(\d*)", new[] { "$1", "$2", "$3" })["$1"]);
        Assert.Equal("abc", Regexs.GetValues("123abc456", @"(\d*)([a-z]*)(\d*)", new[] { "$1", "$2", "$3" })["$2"]);
        Assert.Equal("456", Regexs.GetValues("123abc456", @"(\d*)([a-z]*)(\d*)", new[] { "$1", "$2", "$3" })["$3"]);
    }
}