namespace Bing.Utils.Http.Tests.Integration.Controllers;

/// <summary>
/// 提供授权请求测试使用的固定响应端点。
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class Test5Controller : ControllerBase
{
    /// <summary>
    /// 返回授权请求测试使用的成功文本。
    /// </summary>
    /// <returns>固定文本 <c>ok</c>。</returns>
    [HttpGet("1")]
    public string Get()
    {
        return "ok";
    }
}
