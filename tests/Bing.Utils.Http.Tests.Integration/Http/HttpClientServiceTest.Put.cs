using Bing.Date;
using Bing.Extensions;

namespace Bing.Utils.Http.Tests.Integration.Http;

/// <summary>
/// Http客户端测试 - Put操作
/// </summary>
public partial class HttpClientServiceTest
{
    /// <summary>
    /// 测试 - 调用Put操作方法 - 返回字符串结果
    /// </summary>
    [Fact]
    public async Task Test_Put_1()
    {
        var dto = new CustomerDto { Code = "a" };
        var result = await _client.Put("/api/test3/update", dto).GetResultAsync();
        Assert.Equal("ok:a", result);
    }

    /// <summary>
    /// 测试 - 调用Put方法 - 返回泛型结果
    /// </summary>
    [Fact]
    public async Task Test_Put_2()
    {
        var date = "2022-09-02 19:26:32";
        var dto = new CustomerDto { Code = "a", Birthday = date.ToDate() };
        var result = await _client.Put<CustomerDto>("/api/test3/2", dto).GetResultAsync();
        Assert.Equal("2", result?.Id);
        Assert.Equal("a", result?.Code);
        Assert.Equal(date, result?.Birthday.ToDateTimeString());
    }
}