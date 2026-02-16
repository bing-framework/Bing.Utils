using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Bing.Helpers;
using Bing.Utils.Parameters;

namespace Bing.Parameters;

[Trait("Bing.Parameters", "UrlParameterBuilderExtensions")]
[Collection(Bing.Helpers.WebHttpContextCollection.Name)]
public class UrlParameterBuilderExtensionsTest
{
    [Fact]
    public void LoadForm_NoHttpContext_ShouldDoNothing()
    {
        Web.HttpContextAccessor = null;
        var builder = new UrlParameterBuilder();

        Should.NotThrow(() => builder.LoadForm());
        builder.Result().ShouldBe(string.Empty);
    }

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
