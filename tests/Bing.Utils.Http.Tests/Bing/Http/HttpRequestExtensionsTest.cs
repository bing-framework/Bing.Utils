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
    /// 创建测试请求。
    /// </summary>
    /// <param name="setup">可选的请求配置委托。</param>
    /// <returns>基于新 HTTP 上下文创建的请求。</returns>
    private static HttpRequest CreateRequest(Action<HttpRequest> setup = null)
    {
        var context = new DefaultHttpContext();
        var request = context.Request;
        setup?.Invoke(request);
        return request;
    }

    /// <summary>
    /// 验证绝对地址包含完整请求路径和查询参数。
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
    /// 验证查询参数可转换为目标类型。
    /// </summary>
    [Fact]
    public void Query_ShouldReturnConvertedValue()
    {
        var request = CreateRequest(r => r.QueryString = new QueryString("?age=18"));

        request.Query<int>("age", -1).ShouldBe(18);
    }

    /// <summary>
    /// 验证查询参数缺失时返回默认值。
    /// </summary>
    [Fact]
    public void Query_ShouldReturnDefault_WhenKeyNotExists()
    {
        var request = CreateRequest(r => r.QueryString = new QueryString("?name=bing"));

        request.Query<int>("age", -1).ShouldBe(-1);
    }

    /// <summary>
    /// 验证查询参数格式错误时抛出格式异常。
    /// </summary>
    [Fact]
    public void Query_ShouldThrowFormatException_WhenValueInvalid()
    {
        var request = CreateRequest(r => r.QueryString = new QueryString("?age=abc"));

        Should.Throw<FormatException>(() => request.Query<int>("age", -1));
    }

    /// <summary>
    /// 验证表单参数可转换为目标类型。
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
    /// 验证表单参数缺失时返回默认值。
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
    /// 验证表单参数格式错误时抛出格式异常。
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
    /// 验证请求参数优先采用查询值。
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
    /// 验证查询参数缺失时采用表单值。
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
    /// 验证请求参数不存在时返回 null。
    /// </summary>
    [Fact]
    public void Params_ShouldReturnNull_WhenNotFound()
    {
        var request = CreateRequest();

        request.Params("id").ShouldBeNull();
    }

    /// <summary>
    /// 验证 AJAX 请求头可被识别。
    /// </summary>
    [Fact]
    public void IsAjaxRequest_ShouldReturnTrue_WhenHeaderExists()
    {
        var request = CreateRequest(r => r.Headers["X-Requested-With"] = "XMLHttpRequest");

        request.IsAjaxRequest().ShouldBeTrue();
    }

    /// <summary>
    /// 验证 JSON 内容类型符合 AJAX 判定条件。
    /// </summary>
    [Fact]
    public void IsAjaxRequest_ShouldReturnTrue_WhenContentTypeIsJson()
    {
        var request = CreateRequest(r => r.ContentType = "application/json");

        request.IsAjaxRequest().ShouldBeTrue();
    }

    /// <summary>
    /// 验证 AJAX 判定拒绝 null 请求。
    /// </summary>
    [Fact]
    public void IsAjaxRequest_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        HttpRequest request = null;

        Should.Throw<ArgumentNullException>(() => request.IsAjaxRequest())
            .ParamName.ShouldBe("request");
    }

    /// <summary>
    /// 验证 Content-Type 中的 JSON 类型可被识别。
    /// </summary>
    [Fact]
    public void IsJsonContentType_ShouldReturnTrue_WhenContentTypeHeaderIsJson()
    {
        var request = CreateRequest(r => r.Headers["Content-Type"] = "application/json; charset=utf-8");

        request.IsJsonContentType().ShouldBeTrue();
    }

    /// <summary>
    /// 验证 Accept 中的 JSON 类型可被识别。
    /// </summary>
    [Fact]
    public void IsJsonContentType_ShouldReturnTrue_WhenAcceptHeaderIsJson()
    {
        var request = CreateRequest(r => r.Headers["Accept"] = "application/json");

        request.IsJsonContentType().ShouldBeTrue();
    }

    /// <summary>
    /// 验证 JSON 媒体类型按完整名称匹配。
    /// </summary>
    /// <param name="contentType">Content-Type 头值。</param>
    /// <param name="accept">Accept 头值。</param>
    /// <param name="expected">预期是否识别为 JSON。</param>
    [Theory]
    [InlineData("APPLICATION/JSON; charset=utf-8", null, true)]
    [InlineData("text/json", null, true)]
    [InlineData("application/jsonp", null, false)]
    [InlineData("application/problem+json", null, false)]
    [InlineData("text/plain", "text/html, application/json; q=0.9", true)]
    [InlineData(null, "text/html, TEXT/JSON", true)]
    [InlineData(null, "application/jsonp", false)]
    [InlineData(null, "application/json, invalid", false)]
    public void IsJsonContentType_ShouldParseMediaTypesExactly(string contentType, string accept,
        bool expected)
    {
        var request = CreateRequest(r =>
        {
            if (contentType != null)
                r.Headers["Content-Type"] = contentType;
            if (accept != null)
                r.Headers["Accept"] = accept;
        });

        request.IsJsonContentType().ShouldBe(expected);
    }

    /// <summary>
    /// 验证多个 Accept 头值中的 JSON 类型可被识别。
    /// </summary>
    [Fact]
    public void IsJsonContentType_ShouldReturnTrue_WhenAcceptHasMultipleHeaderValues()
    {
        var request = CreateRequest(r =>
            r.Headers["Accept"] = new StringValues(new[] { "text/html", "text/json; q=0.8" }));

        request.IsJsonContentType().ShouldBeTrue();
    }

    /// <summary>
    /// 验证未声明 JSON 类型时返回 false。
    /// </summary>
    [Fact]
    public void IsJsonContentType_ShouldReturnFalse_WhenJsonHeadersMissing()
    {
        var request = CreateRequest(r => r.Headers["Accept"] = "text/html");

        request.IsJsonContentType().ShouldBeFalse();
    }

    /// <summary>
    /// 验证 JSON 类型判定拒绝 null 请求。
    /// </summary>
    [Fact]
    public void IsJsonContentType_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        HttpRequest request = null;

        Should.Throw<ArgumentNullException>(() => request.IsJsonContentType())
            .ParamName.ShouldBe("request");
    }

    /// <summary>
    /// 验证用户代理标识取自请求头。
    /// </summary>
    [Fact]
    public void UserAgent_ShouldReturnHeaderValue()
    {
        var request = CreateRequest(r => r.Headers["User-Agent"] = "Mozilla/5.0");

        request.UserAgent().ShouldBe("Mozilla/5.0");
    }

    /// <summary>
    /// 验证移动浏览器的用户代理标识可被识别。
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
    /// 验证桌面浏览器不被识别为移动浏览器。
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
    /// 验证过短用户代理标识触发范围异常。
    /// </summary>
    [Fact]
    public void IsMobileBrowser_ShouldThrowArgumentOutOfRangeException_WhenUserAgentLengthLessThan4()
    {
        var request = CreateRequest(r => r.Headers["User-Agent"] = "abc");

        Should.Throw<ArgumentOutOfRangeException>(() => request.IsMobileBrowser());
    }
}
