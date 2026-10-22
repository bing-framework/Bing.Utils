using System.IO;
using System.Text;
using Bing.Extensions;
using Bing.IO;
using Microsoft.AspNetCore.Http;

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
        var form = await Request.ReadFormAsync();
        var file = form.Files.First();
        await SaveAndReadAsync(file);
        var param = form["util"].ToString();
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
        var files = (await Request.ReadFormAsync()).Files;
        var result = new StringBuilder();
        result.Append("ok");
        foreach (var file in files)
        {
            await SaveAndReadAsync(file);
            result.Append($":{file.Name}:{file.FileName}");
        }
        return result.ToString();
    }

    /// <summary>
    /// 返回上传文件的实际落盘内容，供同名及并发上传契约验证。
    /// </summary>
    [HttpPost("inspect")]
    public async Task<List<UploadReceipt>> InspectUpload()
    {
        var files = (await Request.ReadFormAsync()).Files;
        var result = new List<UploadReceipt>();
        foreach (var file in files)
        {
            result.Add(new UploadReceipt
            {
                Name = file.Name,
                FileName = file.FileName,
                Content = Convert.ToBase64String(await SaveAndReadAsync(file))
            });
        }
        return result;
    }

    /// <summary>
    /// 每个文件使用独立临时目录，避免请求间及同一请求内的同名文件互相覆盖。
    /// </summary>
    private static async Task<byte[]> SaveAndReadAsync(IFormFile file)
    {
        var directory = Path.Combine(Path.GetTempPath(), $"bing-http-upload-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var savePath = Path.Combine(directory, Path.GetFileName(file.FileName));
            await using var stream = file.OpenReadStream();
            await FileHelper.WriteAsync(savePath, stream);
            return await System.IO.File.ReadAllBytesAsync(savePath);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// 上传字段、原始文件名及落盘字节的测试回执。
    /// </summary>
    public sealed class UploadReceipt
    {
        public string Name { get; set; }
        public string FileName { get; set; }
        public string Content { get; set; }
    }
}
