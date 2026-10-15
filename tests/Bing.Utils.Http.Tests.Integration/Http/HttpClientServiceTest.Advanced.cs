using System.Net;
using System.Text;

namespace Bing.Utils.Http.Tests.Integration.Http;

/// <summary>
/// 验证授权令牌、请求回调及字节响应读取。
/// </summary>
public partial class HttpClientServiceTest
{
    /// <summary>
    /// 验证承载令牌按 Bearer 格式写入授权请求头。
    /// </summary>
    [Fact]
    public async Task Test_Get_BearerToken_1()
    {
        var result = await _client.Get("/api/test1/header").BearerToken("token-123").GetResultAsync();
        result.ShouldBe("ok:Bearer token-123");
    }

    /// <summary>
    /// 验证成功回调接收与请求结果相同的内容。
    /// </summary>
    [Fact]
    public async Task Test_Get_OnSuccess_1()
    {
        string callbackResult = null;
        var result = await _client.Get("/api/test1")
            .OnSuccess(value => callbackResult = value)
            .GetResultAsync();

        result.ShouldBe("ok");
        callbackResult.ShouldBe("ok");
    }

    /// <summary>
    /// 验证不存在的端点触发失败与完成回调并返回空结果。
    /// </summary>
    [Fact]
    public async Task Test_Get_OnFail_OnComplete_1()
    {
        HttpStatusCode? statusCode = null;
        object failContent = null;
        var completeCalled = false;

        var result = await _client.Get("/api/not-found")
            .OnFail((response, content) =>
            {
                statusCode = response.StatusCode;
                failContent = content;
            })
            .OnComplete((response, content) => { completeCalled = true; })
            .GetResultAsync();

        result.ShouldBeNull();
        statusCode.ShouldBe(HttpStatusCode.NotFound);
        failContent.ShouldNotBeNull();
        completeCalled.ShouldBeTrue();
    }

    /// <summary>
    /// 验证发送前回调返回否定值时终止请求并返回空结果。
    /// </summary>
    [Fact]
    public async Task Test_Get_OnSendBefore_Cancel_1()
    {
        var called = false;
        var result = await _client.Get("/api/test1")
            .OnSendBefore(_ =>
            {
                called = true;
                return false;
            })
            .GetResultAsync();

        called.ShouldBeTrue();
        result.ShouldBeNull();
    }

    /// <summary>
    /// 验证仅配置发送后回调即可返回自定义结果。
    /// </summary>
    [Fact]
    public async Task Test_Get_OnSendAfter_WithoutSendBefore_1()
    {
        var result = await _client.Get("/api/test1")
            .OnSendAfter(_ => Task.FromResult("custom"))
            .GetResultAsync();

        result.ShouldBe("custom");
    }

    /// <summary>
    /// 验证响应字节内容按 UTF-8 解码为成功文本。
    /// </summary>
    [Fact]
    public async Task Test_GetStream_1()
    {
        var bytes = await _client.Get("/api/test1").GetStreamAsync();
        bytes.ShouldNotBeNull();
        bytes.Length.ShouldBeGreaterThan(0);
        Encoding.UTF8.GetString(bytes).ShouldBe("ok");
    }
}
