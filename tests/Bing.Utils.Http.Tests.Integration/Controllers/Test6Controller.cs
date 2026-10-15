using System.IO;
using System.Text;
using Bing.Extensions;
using Bing.Helpers;
using Bing.IO;

namespace Bing.Utils.Http.Tests.Integration.Controllers;

/// <summary>
/// 提供将上传文件保存到临时目录的测试端点。
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class Test6Controller : ControllerBase
{
    /// <summary>
    /// 保存上传文件并回显文件信息及可选表单参数。
    /// </summary>
    /// <returns>包含字段名称、原始文件名及可选 <c>util</c> 参数的测试文本。</returns>
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
    /// 保存所有上传文件并回显各文件信息。
    /// </summary>
    /// <returns>带 <c>ok</c> 前缀并依次包含字段名称与原始文件名的测试文本。</returns>
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
