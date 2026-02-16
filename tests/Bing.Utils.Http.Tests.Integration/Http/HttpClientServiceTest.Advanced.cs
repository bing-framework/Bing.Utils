using System.Net;
using System.Text;

namespace Bing.Utils.Http.Tests.Integration.Http;

/// <summary>
/// Http client integration tests - advanced scenarios
/// </summary>
public partial class HttpClientServiceTest
{
    /// <summary>
    /// Test - BearerToken header injection
    /// </summary>
    [Fact]
    public async Task Test_Get_BearerToken_1()
    {
        var result = await _client.Get("/api/test1/header").BearerToken("token-123").GetResultAsync();
        result.ShouldBe("ok:Bearer token-123");
    }

    /// <summary>
    /// Test - success callback
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
    /// Test - fail and complete callbacks
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
    /// Test - send before can cancel request
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
    /// Test - send after works without send before callback
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
    /// Test - get byte stream
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
