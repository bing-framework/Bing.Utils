namespace Bing.Utils.Http.Tests.Integration.Controllers;

/// <summary>
/// 提供客户数据 PUT 更新测试端点。
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class Test3Controller : ControllerBase
{
    /// <summary>
    /// 回显客户编码以验证 PUT 请求绑定。
    /// </summary>
    /// <param name="dto">请求中绑定的客户测试数据。</param>
    /// <returns>带 <c>ok:</c> 前缀的客户编码。</returns>
    [HttpPut("update")]
    public string Update(CustomerDto dto) => $"ok:{dto.Code}";

    /// <summary>
    /// 将路由标识写入客户数据后以 JSON 回显。
    /// </summary>
    /// <param name="id">需要写入客户数据的路由标识。</param>
    /// <param name="dto">请求中绑定的客户测试数据。</param>
    /// <returns>包含更新后客户标识的 JSON 响应。</returns>
    [HttpPut("{id}")]
    public IActionResult Update2(string id, CustomerDto dto)
    {
        dto.Id = id;
        return new JsonResult(dto);
    }
}
