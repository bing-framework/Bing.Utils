using Microsoft.AspNetCore.Http;

namespace Bing.Http;

/// <summary>
/// HttpResponse 扩展测试。
/// </summary>
[Trait("Bing.Http", "HttpResponseExtensions")]
/// <summary>
/// 验证 HttpResponseExtensionsTest 相关行为的测试。
/// </summary>
public class HttpResponseExtensionsTest
{
    /// <summary>
    /// 验证应设置缓存控制时响应头是否缺少。
    /// </summary>
    [Fact]
    public void SetCache_ShouldSetCacheControl_WhenHeaderIsMissing()
    {
        var response = new DefaultHttpContext().Response;

        response.SetCache(60);

        response.Headers["Cache-Control"].ToString().ShouldBe("max-age=60");
    }

    /// <summary>

    /// 验证需要设置缓存控制和 Pragma 时补齐响应头。

    /// </summary>
    [Fact]
    public void SetNoCache_ShouldSetCacheControlAndPragma_WhenHeadersAreMissing()
    {
        var response = new DefaultHttpContext().Response;

        response.SetNoCache();

        response.Headers["Cache-Control"].ToString().ShouldBe("no-store, no-cache, max-age=0");
        response.Headers["Pragma"].ToString().ShouldBe("no-cache");
    }

    /// <summary>

    /// 验证应保留现有响应头。

    /// </summary>
    [Fact]
    public void SetCacheAndSetNoCache_ShouldPreserveExistingHeaders()
    {
        var response = new DefaultHttpContext().Response;
        response.Headers["Cache-Control"] = "custom";
        response.Headers["Pragma"] = "custom-pragma";

        response.SetCache(60);
        response.SetNoCache();

        response.Headers["Cache-Control"].ToString().ShouldBe("custom");
        response.Headers["Pragma"].ToString().ShouldBe("custom-pragma");
    }
}
