using Bing.Utils.Parameters;
namespace Bing.Utils.Tests.Parameters;
/// <summary>
/// 验证 URL 参数生成器的解析、排序和拼接行为。
/// </summary>
public class UrlParameterBuilderTest
{
    /// <summary>
    /// 保存当前测试使用的 URL 参数生成器实例。
    /// </summary>
    private UrlParameterBuilder _builder;
    /// <summary>
    /// 初始化一个 <see cref="UrlParameterBuilderTest"/> 类型的实例。
    /// </summary>
    public UrlParameterBuilderTest() => _builder = new UrlParameterBuilder();
    /// <summary>
    /// 验证新建生成器不包含参数。
    /// </summary>
    [Fact]
    public void Test_Default()
    {
        Assert.Empty(_builder.Result());
    }
    /// <summary>
    /// 验证不含等号的片段不会生成参数。
    /// </summary>
    [Fact]
    public void Test_LoadUrl_1()
    {
        _builder = new UrlParameterBuilder("a");
        Assert.Equal(0, _builder.GetDictionary().Count);
    }
    /// <summary>
    /// 验证解析单个键值对。
    /// </summary>
    [Fact]
    public void Test_LoadUrl_2()
    {
        _builder = new UrlParameterBuilder("a=b");
        Assert.Equal(1, _builder.GetDictionary().Count);
    }
    /// <summary>
    /// 验证解析多个键值对。
    /// </summary>
    [Fact]
    public void Test_LoadUrl_3()
    {
        _builder = new UrlParameterBuilder("a=1&b=2");
        Assert.Equal(2, _builder.GetDictionary().Count);
    }
    /// <summary>
    /// 验证忽略没有值的参数。
    /// </summary>
    [Fact]
    public void Test_LoadUrl_4()
    {
        _builder = new UrlParameterBuilder("a=1&b");
        Assert.Equal(1, _builder.GetDictionary().Count);
    }
    /// <summary>
    /// 验证忽略空值参数。
    /// </summary>
    [Fact]
    public void Test_LoadUrl_5()
    {
        _builder = new UrlParameterBuilder("a=1&b=");
        Assert.Equal(1, _builder.GetDictionary().Count);
    }
    /// <summary>
    /// 验证从完整 URL 解析参数并支持按键排序。
    /// </summary>
    [Fact]
    public void Test_LoadUrl_6()
    {
        _builder = new UrlParameterBuilder("http://test.com?b=2&c=3&a=1");
        Assert.Equal(3, _builder.GetDictionary().Count);
        Assert.Equal("a=1&b=2&c=3", _builder.Result(true));
    }
    /// <summary>
    /// 验证解析带空格的 URL 查询参数。
    /// </summary>
    [Fact]
    public void Test_LoadUrl_7()
    {
        _builder = new UrlParameterBuilder("http://test.com ? b = 2 & c & a = 1 ");
        Assert.Equal("a=1&b=2", _builder.Result(true));
    }
    /// <summary>
    /// 验证连续添加参数并生成查询字符串。
    /// </summary>
    [Fact]
    public void Test_Add_1()
    {
        Assert.Equal("a=b", _builder.Add("a", "b").Result());
        Assert.Equal("a=b&c=d", _builder.Add("c", "d").Result());
    }
    /// <summary>
    /// 验证添加参数时会清理键和值两端的空格。
    /// </summary>
    [Fact]
    public void Test_Add_2()
    {
        Assert.Equal("b=2&a=1", _builder.Add(" b ", " 2 ").Add(" a ", " 1 ").Result());
    }
    /// <summary>
    /// 验证查询字符串可按键排序。
    /// </summary>
    [Fact]
    public void Test_Result_Sort()
    {
        Assert.Equal("a=1&b=2", _builder.Add("b", "2").Add("a", "1").Result(true));
    }
    /// <summary>
    /// 将参数拼接到已有查询字符串中。
    /// </summary>
    /// <param name="url">待拼接查询参数的 URL。</param>
    /// <param name="result">预期拼接结果。</param>
    [Theory]
    [InlineData("http://test.com", "http://test.com?a=1&b=2")]
    [InlineData("http://test.com?", "http://test.com?a=1&b=2")]
    [InlineData("http://test.com?c=3", "http://test.com?c=3&a=1&b=2")]
    [InlineData("http://test.com?c=3&", "http://test.com?c=3&a=1&b=2")]
    public void Test_JoinUrl(string url, string result)
    {
        Assert.Equal(result, _builder.Add("a", "1").Add("b", "2").JoinUrl(url));
    }
}
