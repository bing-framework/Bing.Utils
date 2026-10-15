using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Bing.Http;

/// <summary>
/// 验证 ASP.NET Core 请求参数读取、内容类型及客户端识别扩展。
/// </summary>
[Trait("Bing.Http", "HttpRequestExtensions")]
public class HttpRequestExtensionsTest
{
    /// <summary>
    /// 创建可由回调配置的测试 HTTP 请求。
    /// </summary>
    /// <param name="setup">配置请求的回调；为 null 时保留默认请求上下文。</param>
    /// <returns>由新的默认 HTTP 上下文创建的请求。</returns>
    private static HttpRequest CreateRequest(Action<HttpRequest> setup = null)
    {
        var context = new DefaultHttpContext();
        var request = context.Request;
        setup?.Invoke(request);
        return request;
    }

    /// <summary>
    /// <c>GetAbsoluteUri</c> 应拼接 Scheme/Host/PathBase/Path/Query 为完整地址
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
    /// 验证查询参数可转换时返回指定类型的值。
    /// </summary>
    [Fact]
    public void Query_ShouldReturnConvertedValue()
    {
        var request = CreateRequest(r => r.QueryString = new QueryString("?age=18"));

        request.Query<int>("age", -1).ShouldBe(18);
    }

    /// <summary>
    /// 验证目标查询参数缺失时返回默认值。
    /// </summary>
    [Fact]
    public void Query_ShouldReturnDefault_WhenKeyNotExists()
    {
        var request = CreateRequest(r => r.QueryString = new QueryString("?name=bing"));

        request.Query<int>("age", -1).ShouldBe(-1);
    }

    /// <summary>
    /// 验证查询参数无法转换时抛出格式异常。
    /// </summary>
    [Fact]
    public void Query_ShouldThrowFormatException_WhenValueInvalid()
    {
        var request = CreateRequest(r => r.QueryString = new QueryString("?age=abc"));

        Should.Throw<FormatException>(() => request.Query<int>("age", -1));
    }

    /// <summary>
    /// 验证表单参数可转换时返回指定类型的值。
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
    /// 验证目标表单参数缺失时返回默认值。
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
    /// 验证表单参数无法转换时抛出格式异常。
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
    /// <c>Params</c> 同时存在 Query/Form 时，应优先返回 Query 值
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
    /// <c>Params</c> 在 Query 缺失时，应回退读取 Form 值
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
    /// <c>Params</c> 在 Query/Form 都不存在目标键时，应返回 <c>null</c>
    /// </summary>
    [Fact]
    public void Params_ShouldReturnNull_WhenNotFound()
    {
        var request = CreateRequest();

        request.Params("id").ShouldBeNull();
    }

    /// <summary>
    /// <c>IsAjaxRequest</c> 在存在 <c>X-Requested-With=XMLHttpRequest</c> 头时应返回 <c>true</c>
    /// </summary>
    [Fact]
    public void IsAjaxRequest_ShouldReturnTrue_WhenHeaderExists()
    {
        var request = CreateRequest(r => r.Headers["X-Requested-With"] = "XMLHttpRequest");

        request.IsAjaxRequest().ShouldBeTrue();
    }

    /// <summary>
    /// <c>IsAjaxRequest</c> 在 <c>ContentType</c> 为 JSON 时应返回 <c>true</c>
    /// </summary>
    [Fact]
    public void IsAjaxRequest_ShouldReturnTrue_WhenContentTypeIsJson()
    {
        var request = CreateRequest(r => r.ContentType = "application/json");

        request.IsAjaxRequest().ShouldBeTrue();
    }

    /// <summary>
    /// <c>IsAjaxRequest</c> 在请求对象为 <c>null</c> 时应抛出 <c>ArgumentNullException(request)</c>
    /// </summary>
    [Fact]
    public void IsAjaxRequest_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        HttpRequest request = null;

        Should.Throw<ArgumentNullException>(() => request.IsAjaxRequest())
            .ParamName.ShouldBe("request");
    }

    /// <summary>
    /// <c>IsJsonContentType</c> 在 <c>Content-Type</c> 头包含 JSON 时应返回 <c>true</c>
    /// </summary>
    [Fact]
    public void IsJsonContentType_ShouldReturnTrue_WhenContentTypeHeaderIsJson()
    {
        var request = CreateRequest(r => r.Headers["Content-Type"] = "application/json; charset=utf-8");

        request.IsJsonContentType().ShouldBeTrue();
    }

    /// <summary>
    /// <c>IsJsonContentType</c> 在 <c>Accept</c> 头包含 JSON 时应返回 <c>true</c>
    /// </summary>
    [Fact]
    public void IsJsonContentType_ShouldReturnTrue_WhenAcceptHeaderIsJson()
    {
        var request = CreateRequest(r => r.Headers["Accept"] = "application/json");

        request.IsJsonContentType().ShouldBeTrue();
    }

    /// <summary>
    /// <c>IsJsonContentType</c> 在 JSON 相关请求头都缺失时应返回 <c>false</c>
    /// </summary>
    [Fact]
    public void IsJsonContentType_ShouldReturnFalse_WhenJsonHeadersMissing()
    {
        var request = CreateRequest(r => r.Headers["Accept"] = "text/html");

        request.IsJsonContentType().ShouldBeFalse();
    }

    /// <summary>
    /// <c>IsJsonContentType</c> 在请求对象为 <c>null</c> 时应抛出 <c>ArgumentNullException(request)</c>
    /// </summary>
    [Fact]
    public void IsJsonContentType_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        HttpRequest request = null;

        Should.Throw<ArgumentNullException>(() => request.IsJsonContentType())
            .ParamName.ShouldBe("request");
    }

    /// <summary>
    /// <c>UserAgent</c> 应返回 <c>User-Agent</c> 请求头值
    /// </summary>
    [Fact]
    public void UserAgent_ShouldReturnHeaderValue()
    {
        var request = CreateRequest(r => r.Headers["User-Agent"] = "Mozilla/5.0");

        request.UserAgent().ShouldBe("Mozilla/5.0");
    }

    /// <summary>
    /// <c>IsMobileBrowser</c> 在移动端 User-Agent 下应返回 <c>true</c>
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
    /// <c>IsMobileBrowser</c> 在桌面端 User-Agent 下应返回 <c>false</c>
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
    /// 验证 User-Agent 长度不足时不匹配移动浏览器特征。
    /// </summary>
    [Fact]
    public void IsMobileBrowser_ShouldReturnFalse_WhenUserAgentLengthLessThan4()
    {
        var request = CreateRequest(r => r.Headers["User-Agent"] = "abc");

        request.IsMobileBrowser().ShouldBeFalse();
    }

    /// <summary>
    /// 验证长度不足四个字符的 User-Agent 即使命中特征也不识别为移动浏览器。
    /// </summary>
    /// <param name="userAgent">与移动浏览器特征匹配但长度不足的客户端标识样例。</param>
    [Theory]
    [InlineData("psp")]
    [InlineData("xda")]
    public void IsMobileBrowser_ShouldReturnFalse_WhenShortUserAgentMatchesMobilePattern(string userAgent)
    {
        var request = CreateRequest(r => r.Headers["User-Agent"] = userAgent);

        request.IsMobileBrowser().ShouldBeFalse();
    }

    /// <summary>
    /// 验证未提供 User-Agent 时返回 false。
    /// </summary>
    [Fact]
    public void IsMobileBrowser_ShouldReturnFalse_WhenUserAgentIsMissing()
    {
        var request = CreateRequest(_ => { });

        request.IsMobileBrowser().ShouldBeFalse();
    }
}
