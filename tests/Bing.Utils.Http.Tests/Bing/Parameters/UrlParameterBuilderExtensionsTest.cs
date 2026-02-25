using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Bing.Helpers;
using Bing.Utils.Parameters;
namespace Bing.Parameters;
/// <summary>
/// 测试类：覆盖 `UrlParameterBuilderExtensions` 相关行为。
/// </summary>
[Trait("Bing.Parameters", "UrlParameterBuilderExtensions")]
[Collection(Bing.Helpers.WebHttpContextCollection.Name)]
public class UrlParameterBuilderExtensionsTest
{
    /// <summary>
    /// 测试用例：验证 `LoadForm` 在 `NoHttpContext` 场景下，结果为 `ShouldDoNothing`。
    /// </summary>
    [Fact]
    public void LoadForm_NoHttpContext_ShouldDoNothing()
    {
        Web.HttpContextAccessor = null;
        var builder = new UrlParameterBuilder();
        Should.NotThrow(() => builder.LoadForm());
        builder.Result().ShouldBe(string.Empty);
    }
    /// <summary>
    /// 测试用例：验证 `LoadForm` 在 `WithRequestForm` 场景下，结果为 `ShouldLoadValues`。
    /// </summary>
    [Fact]
    public void LoadForm_WithRequestForm_ShouldLoadValues()
    {
        var context = new DefaultHttpContext();
        context.Request.Form = new FormCollection(new Dictionary<string, StringValues>
        {
            ["name"] = "bing",
            ["age"] = "18"
        });
        Web.HttpContextAccessor = new HttpContextAccessor { HttpContext = context };
        try
        {
            var builder = new UrlParameterBuilder();
            builder.LoadForm();
            builder.GetValue("name").ToString().ShouldBe("bing");
            builder.GetValue("age").ToString().ShouldBe("18");
        }
        finally
        {
            Web.HttpContextAccessor = null;
        }
    }
    /// <summary>
    /// 测试用例：验证 `LoadQuery` 在 `WithRequestQuery` 场景下，结果为 `ShouldLoadValues`。
    /// </summary>
    [Fact]
    public void LoadQuery_WithRequestQuery_ShouldLoadValues()
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString("?name=bing&city=beijing");
        Web.HttpContextAccessor = new HttpContextAccessor { HttpContext = context };
        try
        {
            var builder = new UrlParameterBuilder();
            builder.LoadQuery();
            builder.GetValue("name").ToString().ShouldBe("bing");
            builder.GetValue("city").ToString().ShouldBe("beijing");
        }
        finally
        {
            Web.HttpContextAccessor = null;
        }
    }
}

