namespace Bing.Utils.Http.Tests.Integration.Http;

/// <summary>
/// 验证 DELETE 请求的文本与客户对象响应。
/// </summary>
public partial class HttpClientServiceTest
{

    /// <summary>
    /// 验证 DELETE 请求返回路由标识的文本回显。
    /// </summary>
    [Fact]
    public async Task Test_Delete_1()
    {
        var result = await _client.Delete("/api/test4/delete/1").GetResultAsync();
        Assert.Equal("ok:1", result);
    }

    /// <summary>
    /// 验证 DELETE 请求反序列化返回的客户编码。
    /// </summary>
    [Fact]
    public async Task Test_Delete_2()
    {
        var result = await _client.Delete<CustomerDto>("/api/test4/1").GetResultAsync();
        Assert.Equal("1", result.Code);
    }
}
