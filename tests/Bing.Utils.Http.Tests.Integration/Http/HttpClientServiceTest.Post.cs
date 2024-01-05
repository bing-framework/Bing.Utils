namespace Bing.Utils.Http.Tests.Integration.Http;

/// <summary>
/// Http客户端测试 - Post操作
/// </summary>
public partial class HttpClientServiceTest
{
    /// <summary>
    /// 测试 - Post调用 - 返回字符串结果
    /// </summary>
    [Fact]
    public async Task Test_Post_1()
    {
        var dto = new CustomerDto { Code = "a" };
        var result = await _client.Post("/api/test2/create", dto).GetResultAsync();
        result.ShouldBe("ok:a");
    }

    /// <summary>
    /// 测试 - Post调用 - 返回泛型结果
    /// </summary>
    [Fact]
    public async Task Test_Post_2()
    {
        var dto = new CustomerDto { Code = "a" };
        var result = await _client.Post<CustomerDto>("/api/test2", dto).GetResultAsync();
        result.Code.ShouldBe("a");
    }

    /// <summary>
    /// 测试 - Post调用 -使用Content方法传递参数 - 键值对
    /// </summary>
    [Fact]
    public async Task Test_Post_Content_1()
    {
        var result = await _client.Post("/api/test2/create").Content("code", "a").GetResultAsync();
        result.ShouldBe("ok:a");
    }

    /// <summary>
    /// 测试 - Post调用 -使用Content方法传递参数 - 字典
    /// </summary>
    [Fact]
    public async Task Test_Post_Content_2()
    {
        var dict = new Dictionary<string, string> { { "code", "a" } };
        var result = await _client.Post("/api/test2/create").Content(dict).GetResultAsync();
        result.ShouldBe("ok:a");
    }

    /// <summary>
    /// 测试 - Post调用 -使用Content方法传递参数 - 对象
    /// </summary>
    [Fact]
    public async Task Test_Post_Content_3()
    {
        var dto = new CustomerDto { Code = "a" };
        var result = await _client.Post("/api/test2/create").Content(dto).GetResultAsync();
        result.ShouldBe("ok:a");
    }
}