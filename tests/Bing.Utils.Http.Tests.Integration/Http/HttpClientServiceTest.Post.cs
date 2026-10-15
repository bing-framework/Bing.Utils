using Bing.Helpers;
using Bing.IO;

namespace Bing.Utils.Http.Tests.Integration.Http;

/// <summary>
/// 验证 POST 请求的内容绑定、客户响应与文件上传。
/// </summary>
public partial class HttpClientServiceTest
{
    /// <summary>
    /// 验证 POST 请求提交客户编码并读取文本响应。
    /// </summary>
    [Fact]
    public async Task Test_Post_1()
    {
        var dto = new CustomerDto { Code = "a" };
        var result = await _client.Post("/api/test2/create", dto).GetResultAsync();
        result.ShouldBe("ok:a");
    }

    /// <summary>
    /// 验证 POST 请求提交客户数据并反序列化回显对象。
    /// </summary>
    [Fact]
    public async Task Test_Post_2()
    {
        var dto = new CustomerDto { Code = "a" };
        var result = await _client.Post<CustomerDto>("/api/test2", dto).GetResultAsync();
        result.Code.ShouldBe("a");
    }

    /// <summary>
    /// 验证按键值对设置的请求内容绑定为客户编码。
    /// </summary>
    [Fact]
    public async Task Test_Post_Content_1()
    {
        var result = await _client.Post("/api/test2/create").Content("code", "a").GetResultAsync();
        result.ShouldBe("ok:a");
    }

    /// <summary>
    /// 验证按字典设置的请求内容绑定为客户编码。
    /// </summary>
    [Fact]
    public async Task Test_Post_Content_2()
    {
        var dict = new Dictionary<string, string> { { "code", "a" } };
        var result = await _client.Post("/api/test2/create").Content(dict).GetResultAsync();
        result.ShouldBe("ok:a");
    }

    /// <summary>
    /// 验证按对象设置的请求内容绑定为客户编码。
    /// </summary>
    [Fact]
    public async Task Test_Post_Content_3()
    {
        var dto = new CustomerDto { Code = "a" };
        var result = await _client.Post("/api/test2/create").Content(dto).GetResultAsync();
        result.ShouldBe("ok:a");
    }

    /// <summary>
    /// 验证按文件路径上传时保留文件名与字段名。
    /// </summary>
    [Fact]
    public async Task Test_Post_FileContent_1()
    {
        var path = Common.GetPhysicalPath("Resources/a.png");
        var result = await _client.Post("/api/test6").FileContent(path, "file1").GetResultAsync();
        Assert.Equal("ok:file1:a.png", result);
    }

    /// <summary>
    /// 验证按流上传时使用指定文件名与字段名。
    /// </summary>
    [Fact]
    public async Task Test_Post_FileContent_2()
    {
        var path = Common.GetPhysicalPath("Resources/a.png");
        var stream = FileHelper.ReadToMemoryStream(path);
        var result = await _client.Post("/api/test6").FileContent(stream, "abc.png", "file2").GetResultAsync();
        Assert.Equal("ok:file2:abc.png", result);
    }

    /// <summary>
    /// 验证文件路径与文件流可在同一请求中上传。
    /// </summary>
    [Fact]
    public async Task Test_Post_FileContent_3()
    {
        var path = Common.GetPhysicalPath("Resources/a.png");
        var stream = FileHelper.ReadToMemoryStream(path);
        var result = await _client.Post("/api/test6/multi")
            .FileContent(path, "file1")
            .FileContent(stream, "b.png", "file2")
            .GetResultAsync();
        Assert.Equal("ok:file1:a.png:file2:b.png", result);
    }

    /// <summary>
    /// 验证文件上传可同时发送普通内容参数。
    /// </summary>
    [Fact]
    public async Task Test_Post_FileContent_4()
    {
        var path = Common.GetPhysicalPath("Resources/a.png");
        var result = await _client.Post("/api/test6")
            .FileContent(path, "file1")
            .Content("util", "core")
            .GetResultAsync();
        Assert.Equal("ok:file1:a.png:core", result);
    }
}
