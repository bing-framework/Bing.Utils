using System.Text;
using System.IO;
using Bing.Extensions;
using Bing.Helpers;
using Bing.IO;

namespace Bing.Utils.Http.Tests.Integration.Controllers;

/// <summary>
/// 测试Api控制器6 - 用于测试上传
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class Test6Controller : ControllerBase
{
    /// <summary>
    /// 上传
    /// </summary>
    [HttpPost]
    public async Task<string> Upload()
    {
        var file = Web.GetFile();
        await using var stream = file.OpenReadStream();
        var fileName = Path.GetFileName(file.FileName);
        var savePath = Path.Combine(Path.GetTempPath(), fileName);
        await FileHelper.WriteAsync(savePath, stream);
        var param = Web.GetParam("util");
        if (param.IsEmpty())
            return $"ok:{file.Name}:{file.FileName}";
        return $"ok:{file.Name}:{file.FileName}:{param}";
    }

    /// <summary>
    /// 多上传
    /// </summary>
    [HttpPost("multi")]
    public async Task<string> MultiUpload()
    {
        var files = Web.GetFiles();
        var result = new StringBuilder();
        result.Append("ok");
        foreach (var file in files)
        {
            await using var stream = file.OpenReadStream();
            var fileName = Path.GetFileName(file.FileName);
            var savePath = Path.Combine(Path.GetTempPath(), fileName);
            await FileHelper.WriteAsync(savePath, stream);
            result.Append($":{file.Name}:{file.FileName}");
        }
        return result.ToString();
    }
}
