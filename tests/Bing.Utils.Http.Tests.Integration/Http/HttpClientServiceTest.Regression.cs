using Bing.Helpers;
using System.IO;

namespace Bing.Utils.Http.Tests.Integration.Http;

/// <summary>
/// Http客户端测试 - 缺陷回归场景
/// </summary>
public partial class HttpClientServiceTest
{
    /// <summary>
    /// 测试用例：仅配置 OnSendAfter 时，请求应正常发送并返回自定义结果
    /// </summary>
    [Fact]
    [Trait("Category", "Regression")]
    [Trait("Risk", "High")]
    [Trait("DefectPattern", "Http.HttpRequest.SendBeforeGuard")]
    public async Task Get_OnSendAfterWithoutSendBefore_ShouldReturnCustomResult()
    {
        var sendAfterCalled = false;
        var result = await _client.Get("/api/test1")
            .OnSendAfter(async response =>
            {
                sendAfterCalled = true;
                var content = await response.Content.ReadAsStringAsync();
                return $"wrapped:{content}";
            })
            .GetResultAsync();

        sendAfterCalled.ShouldBeTrue();
        result.ShouldBe("wrapped:ok");
    }

    /// <summary>
    /// 测试用例：FileContent 传入文件路径时，应使用真实路径文件名参与上传
    /// </summary>
    [Fact]
    [Trait("Category", "Regression")]
    [Trait("Risk", "High")]
    [Trait("DefectPattern", "Http.HttpRequest.FileContent.PathFileName")]
    public async Task Post_FileContentWithPath_ShouldUsePathFileName()
    {
        var fileName = $"reg-upload-{Guid.NewGuid():N}.txt";
        var filePath = Path.Combine(Path.GetTempPath(), fileName);
        try
        {
            await File.WriteAllTextAsync(filePath, "regression-upload");

            var result = await _client.Post("/api/test6")
                .FileContent(filePath, "file1")
                .GetResultAsync();

            result.ShouldBe($"ok:file1:{fileName}");
        }
        finally
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }
}
