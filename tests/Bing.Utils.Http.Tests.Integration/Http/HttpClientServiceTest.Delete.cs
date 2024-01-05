namespace Bing.Utils.Http.Tests.Integration.Http;

/// <summary>
/// Http客户端测试 - Delete操作
/// </summary>
public partial class HttpClientServiceTest
{

    /// <summary>
    /// 测试 - 调用Delete操作方法 - 返回字符串结果
    /// </summary>
    [Fact]
    public async Task Test_Delete_1()
    {
        var result = await _client.Delete("/api/test4/delete/1").GetResultAsync();
        Assert.Equal("ok:1", result);
    }

    /// <summary>
    /// 测试 - 调用Put方法 - 返回泛型结果
    /// </summary>
    [Fact]
    public async Task Test_Delete_2()
    {
        var result = await _client.Delete<CustomerDto>("/api/test4/1").GetResultAsync();
        Assert.Equal("1", result.Code);
    }
}