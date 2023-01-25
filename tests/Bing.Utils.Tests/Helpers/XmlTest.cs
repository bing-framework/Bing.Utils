using System.Linq;
using Bing.Helpers;
using Xunit.Abstractions;

namespace Bing.Utils.Tests.Helpers;

/// <summary>
/// Xml操作测试
/// </summary>
public class XmlTest : TestBase
{
    /// <summary>
    /// Xml操作
    /// </summary>
    private readonly Xml _xml;

    /// <summary>
    /// 测试初始化
    /// </summary>
    public XmlTest(ITestOutputHelper output) : base(output)
    {
        _xml = new Xml();
    }

    /// <summary>
    /// 测试添加节点
    /// </summary>
    [Fact]
    public void Test_AddNode_1()
    {
        //结果
        var result = new Str();
        result.Append("<xml>");
        result.Append("<a>1</a>");
        result.Append("</xml>");

        //操作
        _xml.AddNode("a", "1");

        //验证
        Assert.Equal(result.ToString(), _xml.ToString());

        //输出结果
        Output.WriteLine(_xml.ToString());
    }

    /// <summary>
    /// 测试添加节点 - 父子节点
    /// </summary>
    [Fact]
    public void Test_AddNode_2()
    {
        //结果
        var result = new Str();
        result.Append("<xml>");
        result.Append("<a>");
        result.Append("<b>2</b>");
        result.Append("</a>");
        result.Append("</xml>");

        //操作
        var parent = _xml.AddNode("a");
        _xml.AddNode("b", "2", parent);

        //验证
        Assert.Equal(result.ToString(), _xml.ToString());

        //输出结果
        Output.WriteLine(_xml.ToString());
    }

    /// <summary>
    /// 测试添加注释类型节点
    /// </summary>
    [Fact]
    public void Test_AddCDataNode_1()
    {
        //结果
        var result = new Str();
        result.Append("<xml>");
        result.Append("<![CDATA[1]]>");
        result.Append("</xml>");

        //操作
        _xml.AddCDataNode("1");

        //验证
        Assert.Equal(result.ToString(), _xml.ToString());

        //输出结果
        Output.WriteLine(_xml.ToString());
    }

    /// <summary>
    /// 测试添加注释类型节点 - 父节点
    /// </summary>
    [Fact]
    public void Test_AddCDataNode_2()
    {
        //结果
        var result = new Str();
        result.Append("<xml>");
        result.Append("<a>");
        result.Append("<![CDATA[1]]>");
        result.Append("</a>");
        result.Append("</xml>");

        //操作
        var parent = _xml.AddNode("a");
        _xml.AddCDataNode("1", parent);

        //验证
        Assert.Equal(result.ToString(), _xml.ToString());

        //输出结果
        Output.WriteLine(_xml.ToString());
    }

    /// <summary>
    /// 测试添加注释类型节点 - 父节点 - 通过设置字符串父节点名称
    /// </summary>
    [Fact]
    public void Test_AddCDataNode_3()
    {
        //结果
        var result = new Str();
        result.Append("<xml>");
        result.Append("<a>");
        result.Append("<![CDATA[1]]>");
        result.Append("</a>");
        result.Append("</xml>");

        //操作
        _xml.AddCDataNode("1", "a");

        //验证
        Assert.Equal(result.ToString(), _xml.ToString());

        //输出结果
        Output.WriteLine(_xml.ToString());
    }

    /// <summary>
    /// xml字符串转换为XElement列表
    /// </summary>
    [Fact]
    public void Test_ToElements_1()
    {
        //输入
        var input = new Str();
        input.Append("<xml>");
        input.Append("<a>1</a>");
        input.Append("<b>2</b>");
        input.Append("</xml>");

        //操作
        var elements = Xml.ToElements(input.ToString());
        var element = elements.FirstOrDefault(t => t.Name == "b");

        //验证
        Assert.Equal("2", element?.Value);
    }

    /// <summary>
    /// xml字符串转换为XElement列表
    /// </summary>
    [Fact]
    public void Test_ToElements_2()
    {
        //输入
        var input = new Str();
        input.Append("<xml>");
        input.Append("<a>1</a>");
        input.Append("<b>2</b>");
        input.Append("<c>");
        input.Append("<![CDATA[3]]>");
        input.Append("</c>");
        input.Append("</xml>");

        //操作
        var elements = Xml.ToElements(input.ToString());
        var element = elements.FirstOrDefault(t => t?.Name == "c");

        //验证
        Assert.Equal("3", element?.Value);
    }
}