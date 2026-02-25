using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Bing.Http;

/// <summary>
/// 测试类：`HttpRequestExtensions` 扩展方法测试
/// </summary>
[Trait("Bing.Http", "HttpRequestExtensions")]
public class HttpRequestExtensionsTest
{
    /// <summary>
    /// 测试辅助方法：创建 `HttpRequest`，并允许通过回调配置请求上下文
    /// </summary>
    private static HttpRequest CreateRequest(Action<HttpRequest> setup = null)
    {
        var context = new DefaultHttpContext();
        var request = context.Request;
        setup?.Invoke(request);
        return request;
    }

    /// <summary>
    /// 测试用例：`GetAbsoluteUri` 应拼接 Scheme/Host/PathBase/Path/Query 为完整地址
    /// </summary>
    [Fact]
    public void GetAbsoluteUri_ShouldReturnExpectedUri()
    {
        var request = CreateRequest(r =>
        {
            r.Scheme = "https";
            r.Host = new HostString("example.com", 8080);
            r.PathBase = "/api";
            r.Path = "/users";
            r.QueryString = new QueryString("?id=1");
        });

        request.GetAbsoluteUri().ShouldBe("https://example.com:8080/api/users?id=1");
    }

    /// <summary>
    /// 测试用例：`Query<T>` 在查询字符串包含目标键且可转换时，应返回转换后的值
    /// </summary>
    [Fact]
    public void Query_ShouldReturnConvertedValue()
    {
        var request = CreateRequest(r => r.QueryString = new QueryString("?age=18"));

        request.Query<int>("age", -1).ShouldBe(18);
    }

    /// <summary>
    /// 测试用例：`Query<T>` 在查询字符串缺少目标键时，应返回默认值
    /// </summary>
    [Fact]
    public void Query_ShouldReturnDefault_WhenKeyNotExists()
    {
        var request = CreateRequest(r => r.QueryString = new QueryString("?name=bing"));

        request.Query<int>("age", -1).ShouldBe(-1);
    }

    /// <summary>
    /// 测试用例：`Query<T>` 在值无法转换时，应抛出 `FormatException`
    /// </summary>
    [Fact]
    public void Query_ShouldThrowFormatException_WhenValueInvalid()
    {
        var request = CreateRequest(r => r.QueryString = new QueryString("?age=abc"));

        Should.Throw<FormatException>(() => request.Query<int>("age", -1));
    }

    /// <summary>
    /// 测试用例：`Form<T>` 在表单包含目标键且可转换时，应返回转换后的值
    /// </summary>
    [Fact]
    public void Form_ShouldReturnConvertedValue()
    {
        var request = CreateRequest(r =>
        {
            r.ContentType = "application/x-www-form-urlencoded";
            r.Form = new FormCollection(new Dictionary<string, StringValues>
            {
                ["age"] = "20"
            });
        });

        request.Form<int>("age", -1).ShouldBe(20);
    }

    /// <summary>
    /// 测试用例：`Form<T>` 在表单缺少目标键时，应返回默认值
    /// </summary>
    [Fact]
    public void Form_ShouldReturnDefault_WhenKeyNotExists()
    {
        var request = CreateRequest(r =>
        {
            r.ContentType = "application/x-www-form-urlencoded";
            r.Form = new FormCollection(new Dictionary<string, StringValues>
            {
                ["name"] = "bing"
            });
        });

        request.Form<int>("age", -1).ShouldBe(-1);
    }

    /// <summary>
    /// 测试用例：`Form<T>` 在表单值无法转换时，应抛出 `FormatException`
    /// </summary>
    [Fact]
    public void Form_ShouldThrowFormatException_WhenValueInvalid()
    {
        var request = CreateRequest(r =>
        {
            r.ContentType = "application/x-www-form-urlencoded";
            r.Form = new FormCollection(new Dictionary<string, StringValues>
            {
                ["age"] = "abc"
            });
        });

        Should.Throw<FormatException>(() => request.Form<int>("age", -1));
    }

    /// <summary>
    /// 测试用例：`Params` 同时存在 Query/Form 时，应优先返回 Query 值
    /// </summary>
    [Fact]
    public void Params_ShouldPreferQueryValue()
    {
        var request = CreateRequest(r =>
        {
            r.QueryString = new QueryString("?id=100");
            r.ContentType = "application/x-www-form-urlencoded";
            r.Form = new FormCollection(new Dictionary<string, StringValues>
            {
                ["id"] = "200"
            });
        });

        request.Params("id").ShouldBe("100");
    }

    /// <summary>
    /// 测试用例：`Params` 在 Query 缺失时，应回退读取 Form 值
    /// </summary>
    [Fact]
    public void Params_ShouldReturnFormValue_WhenQueryMissing()
    {
        var request = CreateRequest(r =>
        {
            r.ContentType = "application/x-www-form-urlencoded";
            r.Form = new FormCollection(new Dictionary<string, StringValues>
            {
                ["id"] = "200"
            });
        });

        request.Params("id").ShouldBe("200");
    }

    /// <summary>
    /// 测试用例：`Params` 在 Query/Form 都不存在目标键时，应返回 `null`
    /// </summary>
    [Fact]
    public void Params_ShouldReturnNull_WhenNotFound()
    {
        var request = CreateRequest();

        request.Params("id").ShouldBeNull();
    }

    /// <summary>
    /// 测试用例：`IsAjaxRequest` 在存在 `X-Requested-With=XMLHttpRequest` 头时应返回 `true`
    /// </summary>
    [Fact]
    public void IsAjaxRequest_ShouldReturnTrue_WhenHeaderExists()
    {
        var request = CreateRequest(r => r.Headers["X-Requested-With"] = "XMLHttpRequest");

        request.IsAjaxRequest().ShouldBeTrue();
    }

    /// <summary>
    /// 测试用例：`IsAjaxRequest` 在 `ContentType` 为 JSON 时应返回 `true`
    /// </summary>
    [Fact]
    public void IsAjaxRequest_ShouldReturnTrue_WhenContentTypeIsJson()
    {
        var request = CreateRequest(r => r.ContentType = "application/json");

        request.IsAjaxRequest().ShouldBeTrue();
    }

    /// <summary>
    /// 测试用例：`IsAjaxRequest` 在请求对象为 `null` 时应抛出 `ArgumentNullException(request)`
    /// </summary>
    [Fact]
    public void IsAjaxRequest_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        HttpRequest request = null;

        Should.Throw<ArgumentNullException>(() => request.IsAjaxRequest())
            .ParamName.ShouldBe("request");
    }

    /// <summary>
    /// 测试用例：`IsJsonContentType` 在 `Content-Type` 头包含 JSON 时应返回 `true`
    /// </summary>
    [Fact]
    public void IsJsonContentType_ShouldReturnTrue_WhenContentTypeHeaderIsJson()
    {
        var request = CreateRequest(r => r.Headers["Content-Type"] = "application/json; charset=utf-8");

        request.IsJsonContentType().ShouldBeTrue();
    }

    /// <summary>
    /// 测试用例：`IsJsonContentType` 在 `Accept` 头包含 JSON 时应返回 `true`
    /// </summary>
    [Fact]
    public void IsJsonContentType_ShouldReturnTrue_WhenAcceptHeaderIsJson()
    {
        var request = CreateRequest(r => r.Headers["Accept"] = "application/json");

        request.IsJsonContentType().ShouldBeTrue();
    }

    /// <summary>
    /// 测试用例：`IsJsonContentType` 在 JSON 相关请求头都缺失时应返回 `false`
    /// </summary>
    [Fact]
    public void IsJsonContentType_ShouldReturnFalse_WhenJsonHeadersMissing()
    {
        var request = CreateRequest(r => r.Headers["Accept"] = "text/html");

        request.IsJsonContentType().ShouldBeFalse();
    }

    /// <summary>
    /// 测试用例：`IsJsonContentType` 在请求对象为 `null` 时应抛出 `ArgumentNullException(request)`
    /// </summary>
    [Fact]
    public void IsJsonContentType_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        HttpRequest request = null;

        Should.Throw<ArgumentNullException>(() => request.IsJsonContentType())
            .ParamName.ShouldBe("request");
    }

    /// <summary>
    /// 测试用例：`UserAgent` 应返回 `User-Agent` 请求头值
    /// </summary>
    [Fact]
    public void UserAgent_ShouldReturnHeaderValue()
    {
        var request = CreateRequest(r => r.Headers["User-Agent"] = "Mozilla/5.0");

        request.UserAgent().ShouldBe("Mozilla/5.0");
    }

    /// <summary>
    /// 测试用例：`IsMobileBrowser` 在移动端 User-Agent 下应返回 `true`
    /// </summary>
    [Fact]
    public void IsMobileBrowser_ShouldReturnTrue_WhenUserAgentIsMobile()
    {
        var request = CreateRequest(r =>
            r.Headers["User-Agent"] =
                "Mozilla/5.0 (iPhone; CPU iPhone OS 13_2_3 like Mac OS X) AppleWebKit/605.1.15");

        request.IsMobileBrowser().ShouldBeTrue();
    }

    /// <summary>
    /// 测试用例：`IsMobileBrowser` 在桌面端 User-Agent 下应返回 `false`
    /// </summary>
    [Fact]
    public void IsMobileBrowser_ShouldReturnFalse_WhenUserAgentIsDesktop()
    {
        var request = CreateRequest(r =>
            r.Headers["User-Agent"] =
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/131.0.0.0");

        request.IsMobileBrowser().ShouldBeFalse();
    }

    /// <summary>
    /// 测试用例：`IsMobileBrowser` 在 User-Agent 长度小于 4 时应抛出 `ArgumentOutOfRangeException`
    /// </summary>
    [Fact]
    public void IsMobileBrowser_ShouldThrowArgumentOutOfRangeException_WhenUserAgentLengthLessThan4()
    {
        var request = CreateRequest(r => r.Headers["User-Agent"] = "abc");

        Should.Throw<ArgumentOutOfRangeException>(() => request.IsMobileBrowser());
    }
}
