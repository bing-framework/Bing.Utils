using Bing.Http;

namespace Bing.Utils.Http.Tests.Integration.Http;

/// <summary>
/// 验证 GET 请求的响应读取、参数绑定与请求元数据传递。
/// </summary>
public partial class HttpClientServiceTest
{
    #region 测试初始化

    /// <summary>
    /// 通过测试服务器发送集成测试请求的客户端。
    /// </summary>
    private readonly IHttpClient _client;

    /// <summary>
    /// 初始化一个 <see cref="HttpClientServiceTest"/> 类型的实例。
    /// </summary>
    /// <param name="client">集成测试使用的 HTTP 客户端。</param>
    public HttpClientServiceTest(IHttpClient client)
    {
        _client = client;
    }

    #endregion

    #region 基础测试

    /// <summary>
    /// 验证 GET 请求返回成功文本。
    /// </summary>
    [Fact]
    public async Task Test_Get_1()
    {
        var result = await _client.Get("/api/test1").GetResultAsync();
        result.ShouldBe("ok");
    }

    /// <summary>
    /// 验证 GET 请求传递路由标识并读取回显结果。
    /// </summary>
    [Fact]
    public async Task Test_Get_2()
    {
        var result = await _client.Get("/api/test1/2").GetResultAsync();
        result.ShouldBe("ok:2");
    }

    /// <summary>
    /// 验证带查询对象的 GET 请求反序列化客户列表。
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
    /// 验证按名称设置的授权请求头到达服务端。
    /// </summary>
    [Fact]
    public async Task Test_Get_Header_1()
    {
        var result = await _client.Get("/api/test1/header").Header("Authorization", "abc").GetResultAsync();
        result.ShouldBe("ok:abc");
    }

    /// <summary>
    /// 验证请求头字典中的授权值到达服务端。
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
    /// 验证地址中直接声明的查询参数到达服务端。
    /// </summary>
    [Fact]
    public async Task Test_Get_QueryString_1()
    {
        var result = await _client.Get("/api/test1/query?code=9&name=a").GetResultAsync();
        result.ShouldBe("code:9,name:a");
    }

    /// <summary>
    /// 验证逐项添加的查询参数到达服务端。
    /// </summary>
    [Fact]
    public async Task Test_Get_QueryString_2()
    {
        var result = await _client.Get("/api/test1/query").QueryString("code", "9").QueryString("name", "a").GetResultAsync();
        result.ShouldBe("code:9,name:a");
    }

    /// <summary>
    /// 验证显式查询参数与地址中的已有参数组合发送。
    /// </summary>
    [Fact]
    public async Task Test_Get_QueryString_3()
    {
        var result = await _client.Get("/api/test1/query?code=9").QueryString("name", "a").GetResultAsync();
        result.ShouldBe("code:9,name:a");
    }

    /// <summary>
    /// 验证查询参数字典被正确绑定。
    /// </summary>
    [Fact]
    public async Task Test_Get_QueryString_4()
    {
        var queryString = new Dictionary<string, string> { { "code", "9" }, { "name", "a" } };
        var result = await _client.Get("/api/test1/query").QueryString(queryString).GetResultAsync();
        result.ShouldBe("code:9,name:a");
    }

    /// <summary>
    /// 验证对象属性转换为查询参数并被正确绑定。
    /// </summary>
    [Fact]
    public async Task Test_Get_QueryString_5()
    {
        var query = new CustomerQuery { Code = "9", Name = "a" };
        var result = await _client.Get("/api/test1/query").QueryString(query).GetResultAsync();
        result.ShouldBe("code:9,name:a");
    }

    /// <summary>
    /// 验证 GET 重载中的查询对象被正确绑定。
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
    /// 验证单个 Cookie 随请求发送。
    /// </summary>
    [Fact]
    public async Task Test_Get_Cookie_1()
    {
        var result = await _client.Get("http://a.com/api/test1/cookie").Cookie("code", "a").GetResultAsync();
        result.ShouldBe("code:a,name:");
    }

    /// <summary>
    /// 验证连续设置的两个 Cookie 均随请求发送。
    /// </summary>
    [Fact]
    public async Task Test_Get_Cookie_2()
    {
        var result = await _client.Get("/api/test1/cookie").Cookie("code", "a").Cookie("name", "b").GetResultAsync();
        result.ShouldBe("code:a,name:b");
    }

    #endregion
}
