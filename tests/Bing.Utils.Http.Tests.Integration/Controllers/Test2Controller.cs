namespace Bing.Utils.Http.Tests.Integration.Controllers;

/// <summary>
/// 提供客户数据 POST 回显测试端点。
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class Test2Controller : ControllerBase
{
    /// <summary>
    /// 回显客户编码以验证 POST 请求绑定。
    /// </summary>
    /// <param name="dto">请求中绑定的客户测试数据。</param>
    /// <returns>带 <c>ok:</c> 前缀的客户编码。</returns>
    [HttpPost("create")]
    public string Create(CustomerDto dto) => $"ok:{dto.Code}";

    /// <summary>
    /// 以 JSON 回显提交的客户数据。
    /// </summary>
    /// <param name="dto">请求中绑定的客户测试数据。</param>
    /// <returns>包含原客户数据的 JSON 响应。</returns>
    [HttpPost]
    public IActionResult Create2(CustomerDto dto) => new JsonResult(dto);
}
