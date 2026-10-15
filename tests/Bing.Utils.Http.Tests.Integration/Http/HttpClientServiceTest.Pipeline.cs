using System.Net;
using System.Text.Json;
using System.Threading;
using Bing.Http;
using Bing.Utils.Http.Tests.Integration.Controllers;

namespace Bing.Utils.Http.Tests.Integration.Http;

/// <summary>
/// Http客户端测试 - 真实链路协作场景
/// </summary>
public partial class HttpClientServiceTest
{
    /// <summary>
    /// 测试 - 组合请求链路（Header + Query + Body + 反序列化）
    /// </summary>
    [Fact]
    public async Task Test_Post_ComposeRequestPipeline_1()
    {
        var birthday = new DateTime(2024, 7, 15, 13, 14, 15);
        var dto = new CustomerDto
        {
            Code = "C-001",
            Name = "张三",
            Birthday = birthday
        };

        var result = await _client.Post<Test7Controller.ComposeResponse>("/api/test7/compose/42")
            .Header("X-Correlation-Id", "trace-42")
            .QueryString("source", "integration")
            .JsonContent(dto)
            .GetResultAsync();

        result.ShouldNotBeNull();
        result.Id.ShouldBe("42");
        result.Source.ShouldBe("integration");
        result.CorrelationId.ShouldBe("trace-42");
        result.Code.ShouldBe("C-001");
        result.Name.ShouldBe("张三");
        result.Birthday.ShouldBe(birthday);
    }

    /// <summary>
    /// 测试 - 超时配置生效
    /// </summary>
    [Fact]
    public async Task Test_Get_Timeout_ThrowsOperationCanceledException_1()
    {
        var failCalled = false;
        var completeCalled = false;
        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            await _client.Get("/api/test7/delay/800")
                .Timeout(TimeSpan.FromMilliseconds(120))
                .OnFail((_, _) => failCalled = true)
                .OnComplete((_, _) => completeCalled = true)
                .GetResultAsync();
        });

        // 发送阶段取消没有 HTTP 响应，不进入响应失败或完成回调。
        failCalled.ShouldBeFalse();
        completeCalled.ShouldBeFalse();
    }

    /// <summary>
    /// 测试 - 取消令牌生效
    /// </summary>
    [Fact]
    public async Task Test_Get_CancellationToken_ThrowsOperationCanceledException_1()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(120));
        var failCalled = false;
        var completeCalled = false;

        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            await _client.Get("/api/test7/delay/1000")
                .OnFail((_, _) => failCalled = true)
                .OnComplete((_, _) => completeCalled = true)
                .GetResultAsync(cts.Token);
        });

        failCalled.ShouldBeFalse();
        completeCalled.ShouldBeFalse();
    }

    /// <summary>
    /// 测试 - 4xx/5xx错误映射到OnFail并返回空结果
    /// </summary>
    [Theory]
    [InlineData(400)]
    [InlineData(500)]
    public async Task Test_Get_OnFail_For4xxAnd5xx_1(int statusCode)
    {
        await AssertFailureResponseAsync($"/api/test7/status/{statusCode}", statusCode);
    }

    /// <summary>
    /// 测试 - 中间件捕获异常后的完整错误体与显式 500 响应一致，并原样传给回调。
    /// </summary>
    [Fact]
    public async Task Test_Get_Failure_ExceptionMiddleware_PreservesErrorBody_1()
    {
        var expectedBody = await AssertFailureResponseAsync("/api/test7/status/500", 500);
        var actualBody = await AssertFailureResponseAsync("/api/test7/throw", 500);

        actualBody.ShouldBe(expectedBody);
    }

    /// <summary>
    /// 验证失败状态及完整错误结构，且失败、完成回调收到相同响应体。
    /// </summary>
    private async Task<string> AssertFailureResponseAsync(string url, int statusCode)
    {
        HttpStatusCode? capturedStatusCode = null;
        HttpStatusCode? completeStatusCode = null;
        string failPayload = null;
        string completePayload = null;
        var callbacks = new List<string>();
        var successCalled = false;

        var result = await _client.Get(url)
            .OnFail((response, content) =>
            {
                callbacks.Add("fail");
                capturedStatusCode = response.StatusCode;
                response.Content.Headers.ContentType.MediaType.ShouldBe("application/json");
                failPayload = content.ShouldBeOfType<string>();
            })
            .OnComplete((response, content) =>
            {
                callbacks.Add("complete");
                completeStatusCode = response.StatusCode;
                completePayload = content.ShouldBeOfType<string>();
            })
            .OnSuccess(_ => successCalled = true)
            .GetResultAsync();

        result.ShouldBeNull();
        capturedStatusCode.ShouldBe((HttpStatusCode)statusCode);
        completeStatusCode.ShouldBe(capturedStatusCode);
        failPayload.ShouldNotBeNullOrWhiteSpace();
        completePayload.ShouldBe(failPayload);
        callbacks.ShouldBe(new[] { "fail", "complete" });
        successCalled.ShouldBeFalse();
        using var payload = JsonDocument.Parse(failPayload);
        payload.RootElement.GetProperty("code").GetInt32().ShouldBe(statusCode);
        payload.RootElement.GetProperty("message").GetString().ShouldBe($"error:{statusCode}");
        return failPayload;
    }

    /// <summary>
    /// 测试 - Text + Http 编码与内容类型协同
    /// </summary>
    [Fact]
    public async Task Test_Post_XmlContent_UnicodeEncoding_RoundTrip_1()
    {
        const string xml = "<root><name>中文内容</name></root>";

        var result = await _client.Post<Test7Controller.RawPayloadResponse>("/api/test7/echo-xml")
            .Encoding(System.Text.Encoding.Unicode)
            .XmlContent(xml)
            .GetResultAsync();

        result.ShouldNotBeNull();
        result.ContentType.ShouldContain("text/xml");
        result.ContentType.ToLowerInvariant().ShouldContain("charset=utf-16");
        result.Charset.ShouldBe("utf-16");
        result.Body.ShouldBe(xml);
    }

    /// <summary>
    /// 测试 - DateTime + 序列化回环一致性
    /// </summary>
    [Fact]
    public async Task Test_Post_DateTimeSerialization_RoundTrip_1()
    {
        var dto = new CustomerDto
        {
            Code = "dt",
            Birthday = new DateTime(2024, 1, 2, 3, 4, 5)
        };

        var rawJson = await _client.Post("/api/test7/datetime-echo", dto).GetResultAsync();
        rawJson.ShouldContain("\"birthday\":\"2024-01-02 03:04:05\"");

        var typedResult = await _client.Post<CustomerDto>("/api/test7/datetime-echo", dto).GetResultAsync();
        typedResult.ShouldNotBeNull();
        typedResult.Code.ShouldBe("dt");
        typedResult.Birthday.ShouldBe(dto.Birthday);
    }
}
