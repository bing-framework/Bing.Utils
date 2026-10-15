using Bing.Date;
using Bing.Extensions;

namespace Bing.Utils.Http.Tests.Integration.Http;

/// <summary>
/// 验证 PUT 请求的客户数据绑定与日期响应转换。
/// </summary>
public partial class HttpClientServiceTest
{
    /// <summary>
    /// 验证 PUT 请求提交客户编码并读取文本响应。
    /// </summary>
    [Fact]
    public async Task Test_Put_1()
    {
        var dto = new CustomerDto { Code = "a" };
        var result = await _client.Put("/api/test3/update", dto).GetResultAsync();
        Assert.Equal("ok:a", result);
    }

    /// <summary>
    /// 验证 PUT 请求回显路由标识、客户编码与出生日期。
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
