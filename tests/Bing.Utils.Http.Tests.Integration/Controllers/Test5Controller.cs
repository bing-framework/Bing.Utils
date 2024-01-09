namespace Bing.Utils.Http.Tests.Integration.Controllers;

/// <summary>
/// 测试Api控制器5 - 用于测试授权
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class Test5Controller : ControllerBase
{
    /// <summary>
    /// 操作1
    /// </summary>
    [HttpGet("1")]
    public string Get()
    {
        return "ok";
    }
}