namespace Bing.Utils.Http.Tests.Integration.Controllers;

/// <summary>
/// 提供路由标识绑定的 DELETE 测试端点。
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class Test4Controller : ControllerBase
{
    /// <summary>
    /// 回显 DELETE 请求的路由标识。
    /// </summary>
    /// <param name="id">需回显的路由标识。</param>
    /// <returns>带 <c>ok:</c> 前缀的路由标识。</returns>
    [HttpDelete("delete/{id}")]
    public string Delete(string id) => $"ok:{id}";

    /// <summary>
    /// 将 DELETE 请求的路由标识映射为客户编码。
    /// </summary>
    /// <param name="id">用作客户编码的路由标识。</param>
    /// <returns>编码等于路由标识的客户测试对象。</returns>
    [HttpDelete("{id}")]
    public CustomerDto Delete2(string id) => new() { Code = id };
}
