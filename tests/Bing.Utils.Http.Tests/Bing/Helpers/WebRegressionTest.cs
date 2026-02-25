using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Bing.Helpers;

/// <summary>
/// 测试类：Web 参数解析回归测试
/// </summary>
[Trait("Category", "Regression")]
[Trait("Risk", "High")]
[Collection(WebHttpContextCollection.Name)]
public class WebRegressionTest : IDisposable
{
    /// <summary>
    /// 测试清理
    /// </summary>
    public void Dispose() => Web.HttpContextAccessor = null;

    /// <summary>
    /// 测试用例：GetParam 在表单读取异常时应继续回退到请求头
    /// </summary>
    [Fact]
    [Trait("DefectPattern", "Http.Web.GetParam.FormFallback")]
    public void GetParam_FormReadThrows_ShouldFallbackToHeader()
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = QueryString.Empty;
        context.Request.Headers["token"] = new StringValues("from-header");
        var accessor = new HttpContextAccessor { HttpContext = context };
        Web.HttpContextAccessor = accessor;

        var result = Web.GetParam("token");

        result.ShouldBe("from-header");
    }
}
