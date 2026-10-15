using System.IO;
using Bing.Helpers;

namespace Bing.Utils.Http.Tests.Integration.Http;

/// <summary>
/// 验证发送后回调及路径文件名上传的回归行为。
/// </summary>
public partial class HttpClientServiceTest
{
    /// <summary>
    /// 验证仅配置发送后回调时仍可发送请求并包装响应。
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
    /// 验证按路径上传时使用路径中的实际文件名。
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
