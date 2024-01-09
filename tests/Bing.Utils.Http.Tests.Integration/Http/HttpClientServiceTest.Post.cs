using Bing.Helpers;
using Bing.IO;

namespace Bing.Utils.Http.Tests.Integration.Http;

/// <summary>
/// Http客户端测试 - Post操作
/// </summary>
public partial class HttpClientServiceTest
{
    /// <summary>
    /// 测试 - Post调用 - 返回字符串结果
    /// </summary>
    [Fact]
    public async Task Test_Post_1()
    {
        var dto = new CustomerDto { Code = "a" };
        var result = await _client.Post("/api/test2/create", dto).GetResultAsync();
        result.ShouldBe("ok:a");
    }

    /// <summary>
    /// 测试 - Post调用 - 返回泛型结果
    /// </summary>
    [Fact]
    public async Task Test_Post_2()
    {
        var dto = new CustomerDto { Code = "a" };
        var result = await _client.Post<CustomerDto>("/api/test2", dto).GetResultAsync();
        result.Code.ShouldBe("a");
    }

    /// <summary>
    /// 测试 - Post调用 -使用Content方法传递参数 - 键值对
    /// </summary>
    [Fact]
    public async Task Test_Post_Content_1()
    {
        var result = await _client.Post("/api/test2/create").Content("code", "a").GetResultAsync();
        result.ShouldBe("ok:a");
    }

    /// <summary>
    /// 测试 - Post调用 -使用Content方法传递参数 - 字典
    /// </summary>
    [Fact]
    public async Task Test_Post_Content_2()
    {
        var dict = new Dictionary<string, string> { { "code", "a" } };
        var result = await _client.Post("/api/test2/create").Content(dict).GetResultAsync();
        result.ShouldBe("ok:a");
    }

    /// <summary>
    /// 测试 - Post调用 -使用Content方法传递参数 - 对象
    /// </summary>
    [Fact]
    public async Task Test_Post_Content_3()
    {
        var dto = new CustomerDto { Code = "a" };
        var result = await _client.Post("/api/test2/create").Content(dto).GetResultAsync();
        result.ShouldBe("ok:a");
    }

    /// <summary>
    /// 测试 - Post调用 - 上传文件 - 文件路径
    /// </summary>
    [Fact]
    public async Task Test_Post_FileContent_1()
    {
        var path = Common.GetPhysicalPath("Resources/a.png");
        var result = await _client.Post("/api/test6").FileContent(path, "file1").GetResultAsync();
        Assert.Equal("ok:file1:a.png", result);
    }

    /// <summary>
    /// 测试 - Post调用 - 上传文件 - 文件流
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
    /// 测试 - Post调用 - 上传文件 - 多文件上传
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
    /// 测试 - Post调用 - 上传文件 - 发送参数
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