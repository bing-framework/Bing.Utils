namespace Bing.Utils.Http.Tests.Integration.Controllers;

/// <summary>
/// 测试Api控制器4
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class Test4Controller : ControllerBase
{
    /// <summary>
    /// Delete操作
    /// </summary>
    [HttpDelete("delete/{id}")]
    public string Delete(string id) => $"ok:{id}";

    /// <summary>
    /// Delete操作
    /// </summary>
    [HttpDelete("{id}")]
    public CustomerDto Delete2(string id) => new() { Code = id };
}