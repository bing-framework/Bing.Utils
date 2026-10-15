using System.Net;
using System.Threading;
using Bing.Http;
using Bing.Utils.Http.Tests.Integration.Controllers;

namespace Bing.Utils.Http.Tests.Integration.Http;

/// <summary>
/// 验证真实 HTTP 管道中的请求组合、取消及序列化行为。
/// </summary>
public partial class HttpClientServiceTest
{
    /// <summary>
    /// 验证路由、请求头、查询参数及 JSON 正文组合后正确反序列化。
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
    /// 验证请求超时触发取消异常。
    /// </summary>
    [Fact]
    public async Task Test_Get_Timeout_ThrowsOperationCanceledException_1()
    {
        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            await _client.Get("/api/test7/delay/800")
                .Timeout(TimeSpan.FromMilliseconds(120))
                .GetResultAsync();
        });
    }

    /// <summary>
    /// 验证调用方取消令牌触发取消异常。
    /// </summary>
    [Fact]
    public async Task Test_Get_CancellationToken_ThrowsOperationCanceledException_1()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(120));

        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            await _client.Get("/api/test7/delay/1000")
                .GetResultAsync(cts.Token);
        });
    }

    /// <summary>
    /// 验证客户端与服务器错误触发失败回调并返回空结果。
    /// </summary>
    /// <param name="statusCode">测试服务端返回的 HTTP 错误状态码。</param>
    [Theory]
    [InlineData(400)]
    [InlineData(500)]
    public async Task Test_Get_OnFail_For4xxAnd5xx_1(int statusCode)
    {
        HttpStatusCode? capturedStatusCode = null;
        string failPayload = null;

        var result = await _client.Get($"/api/test7/status/{statusCode}")
            .OnFail((response, content) =>
            {
                capturedStatusCode = response.StatusCode;
                failPayload = content?.ToString();
            })
            .GetResultAsync();

        result.ShouldBeNull();
        capturedStatusCode.ShouldBe((HttpStatusCode)statusCode);
        failPayload.ShouldNotBeNullOrWhiteSpace();
        failPayload.ShouldContain($"\"code\":{statusCode}");
    }

    /// <summary>
    /// 验证 UTF-16 XML 正文及内容类型字符集能够往返传递。
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
    /// 验证日期使用指定 JSON 格式并可还原为原始客户数据。
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
