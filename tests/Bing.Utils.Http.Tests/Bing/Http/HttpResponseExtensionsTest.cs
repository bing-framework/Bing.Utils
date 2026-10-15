using Microsoft.AspNetCore.Http;

namespace Bing.Http;

/// <summary>
/// 验证 HTTP 响应的缓存控制头设置及已有值保留行为。
/// </summary>
[Trait("Bing.Http", "HttpResponseExtensions")]
public class HttpResponseExtensionsTest
{
    /// <summary>
    /// 验证缺少缓存控制头时写入指定的最大缓存秒数。
    /// </summary>
    [Fact]
    public void SetCache_ShouldSetCacheControl_WhenHeaderIsMissing()
    {
        var response = new DefaultHttpContext().Response;

        response.SetCache(60);

        response.Headers["Cache-Control"].ToString().ShouldBe("max-age=60");
    }



    /// <summary>
    /// 验证禁用缓存时补齐缓存控制头及 Pragma 头。
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
    /// 验证设置或禁用缓存均不覆盖已有缓存响应头。
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
