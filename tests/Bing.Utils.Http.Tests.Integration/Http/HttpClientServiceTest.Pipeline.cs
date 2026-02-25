using System.Net;
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
        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            await _client.Get("/api/test7/delay/800")
                .Timeout(TimeSpan.FromMilliseconds(120))
                .GetResultAsync();
        });
    }

    /// <summary>
    /// 测试 - 取消令牌生效
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
    /// 测试 - 4xx/5xx错误映射到OnFail并返回空结果
    /// </summary>
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
