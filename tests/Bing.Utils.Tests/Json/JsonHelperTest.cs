using System.Text;
using Bing.Tests.Samples;
using Bing.Tests.XUnitHelpers;
using Bing.Utils.Json;
using Newtonsoft.Json;
using Xunit.Abstractions;

namespace Bing.Utils.Tests.Json;

/// <summary>
/// Json测试
/// </summary>
public class JsonHelperTest : TestBase
{
    /// <summary>
    /// 初始化一个<see cref="TestBase"/>类型的实例
    /// </summary>
    public JsonHelperTest(ITestOutputHelper output) : base(output)
    {
    }

    /// <summary>
    /// 测试循环引用序列化
    /// </summary>
    [Fact]
    public void Test_Loop()
    {
        A a = new A { Name = "a" };
        B b = new B { Name = "b" };
        C c = new C { Name = "c" };
        a.B = b;
        b.C = c;
        c.A = a;
        AssertHelper.Throws<JsonSerializationException>(() => JsonHelper.ToJson(c));
    }

    /// <summary>
    /// 转成Json,验证空
    /// </summary>
    [Fact]
    public void Test_ToJson_Null()
    {
        Assert.Empty(JsonHelper.ToJson(null));
    }

    /// <summary>
    /// 测试转成Json
    /// </summary>
    [Fact]
    public void Test_ToJson()
    {
        var result = new StringBuilder();
        result.Append("{");
        result.Append("\"Name\":\"a\",");
        result.Append("\"nickname\":\"b\",");
        result.Append("\"Value\":null,");
        result.Append("\"Date\":\"2012/1/1 0:00:00\",");
        result.Append("\"Age\":1,");
        result.Append("\"isShow\":true");
        result.Append("}");
        var actualData = JsonTestSample.Create();
        actualData.Date = DateTime.Parse(actualData.Date).ToString("yyyy/M/d 0:00:00");
        Assert.Equal(result.ToString(), JsonHelper.ToJson(actualData));
    }

    /// <summary>
    /// 测试转成Json，将双引号转成单引号
    /// </summary>
    [Fact]
    public void Test_ToJson_ToSingleQuotes()
    {
        var result = new StringBuilder();
        result.Append("{");
        result.Append("'Name':'a',");
        result.Append("'nickname':'b',");
        result.Append("'Value':null,");
        result.Append("'Date':'2012/1/1 0:00:00',");
        result.Append("'Age':1,");
        result.Append("'isShow':true");
        result.Append("}");

        var actualData = JsonTestSample.Create();
        actualData.Date = DateTime.Parse(actualData.Date).ToString("yyyy/M/d 0:00:00");
        Assert.Equal(result.ToString(), JsonHelper.ToJson(actualData, true));
    }

    /// <summary>
    /// 测试转成对象
    /// </summary>
    [Fact]
    public void Test_ToObject()
    {
        var customer = JsonHelper.ToObject<JsonTestSample>("{\"Name\":\"a\"}");
        Assert.Equal("a", customer.Name);
    }
}