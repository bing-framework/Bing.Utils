using Bing.Http;

namespace Bing.Utils.Http.Tests.Integration.Http;

/// <summary>
/// Http客户端测试 - Get操作
/// </summary>
public partial class HttpClientServiceTest
{
    #region 测试初始化

    /// <summary>
    /// Http客户端
    /// </summary>
    private readonly IHttpClient _client;

    /// <summary>
    /// 测试初始化
    /// </summary>
    public HttpClientServiceTest(IHttpClient client)
    {
        _client = client;
    }

    #endregion

    #region 基础测试

    /// <summary>
    /// 测试 - 调用Get方法
    /// </summary>
    [Fact]
    public async Task Test_Get_1()
    {
        var result = await _client.Get("/api/test1").GetResultAsync();
        result.ShouldBe("ok");
    }

    /// <summary>
    /// 测试 - 调用Get方法 - 传递id
    /// </summary>
    [Fact]
    public async Task Test_Get_2()
    {
        var result = await _client.Get("/api/test1/2").GetResultAsync();
        result.ShouldBe("ok:2");
    }

    /// <summary>
    /// 测试 - 调用Get方法 - 返回对象
    /// </summary>
    [Fact]
    public async Task Test_Get_3()
    {
        var query = new CustomerQuery { Code = "a", Name = "b" };
        var result = await _client.Get<List<CustomerDto>>("/api/test1/list", query).GetResultAsync();
        result.Count.ShouldBe(2);
        result[0].Code.ShouldBe("a");
        result[1].Name.ShouldBe("b");
    }

    #endregion

    #region Header

    /// <summary>
    /// 测试 - 调用Get方法 - 设置请求头
    /// </summary>
    [Fact]
    public async Task Test_Get_Header_1()
    {
        var result = await _client.Get("/api/test1/header").Header("Authorization", "abc").GetResultAsync();
        result.ShouldBe("ok:abc");
    }

    /// <summary>
    /// 测试 - 调用Get方法 - 设置请求头 - 传入字典
    /// </summary>
    [Fact]
    public async Task Test_Get_Header_2()
    {
        var headers = new Dictionary<string, string> { { "Authorization", "abc" } };
        var result = await _client.Get("/api/test1/header").Header(headers).GetResultAsync();
        result.ShouldBe("ok:abc");
    }

    #endregion

    #region QueryString

    /// <summary>
    /// 测试 - 调用Get方法 - 发送query参数 - 硬编码
    /// </summary>
    [Fact]
    public async Task Test_Get_QueryString_1()
    {
        var result = await _client.Get("/api/test1/query?code=9&name=a").GetResultAsync();
        result.ShouldBe("code:9,name:a");
    }

    /// <summary>
    /// 测试 - 调用Get方法 - 发送query参数
    /// </summary>
    [Fact]
    public async Task Test_Get_QueryString_2()
    {
        var result = await _client.Get("/api/test1/query").QueryString("code", "9").QueryString("name", "a").GetResultAsync();
        result.ShouldBe("code:9,name:a");
    }

    /// <summary>
    /// 测试 - 调用Get方法 - 发送query参数 - 部分硬编码
    /// </summary>
    [Fact]
    public async Task Test_Get_QueryString_3()
    {
        var result = await _client.Get("/api/test1/query?code=9").QueryString("name", "a").GetResultAsync();
        result.ShouldBe("code:9,name:a");
    }

    /// <summary>
    /// 测试 - 调用Get方法 - 发送query参数 - 传入字典
    /// </summary>
    [Fact]
    public async Task Test_Get_QueryString_4()
    {
        var queryString = new Dictionary<string, string> { { "code", "9" }, { "name", "a" } };
        var result = await _client.Get("/api/test1/query").QueryString(queryString).GetResultAsync();
        result.ShouldBe("code:9,name:a");
    }

    /// <summary>
    /// 测试 - 调用Get方法 - 发送query参数 - 传入对象
    /// </summary>
    [Fact]
    public async Task Test_Get_QueryString_5()
    {
        var query = new CustomerQuery { Code = "9", Name = "a" };
        var result = await _client.Get("/api/test1/query").QueryString(query).GetResultAsync();
        result.ShouldBe("code:9,name:a");
    }

    /// <summary>
    /// 测试 - 调用Get方法 - 发送query参数 - 使用Get重载方法传入参数
    /// </summary>
    [Fact]
    public async Task Test_Get_QueryString_6()
    {
        var query = new CustomerQuery { Code = "9", Name = "a" };
        var result = await _client.Get("/api/test1/query", query).GetResultAsync();
        result.ShouldBe("code:9,name:a");
    }

    #endregion

    #region Cookie

    /// <summary>
    /// 测试 - 调用Get方法 - 设置cookie - 1个参数
    /// </summary>
    [Fact]
    public async Task Test_Get_Cookie_1()
    {
        var result = await _client.Get("http://a.com/api/test1/cookie").Cookie("code", "a").GetResultAsync();
        result.ShouldBe("code:a,name:");
    }

    /// <summary>
    /// 测试 - 调用Get方法 - 设置cookie - 1个参数
    /// </summary>
    [Fact]
    public async Task Test_Get_Cookie_2()
    {
        var result = await _client.Get("/api/test1/cookie").Cookie("code", "a").Cookie("name", "b").GetResultAsync();
        result.ShouldBe("code:a,name:b");
    }

    #endregion
}