namespace Bing.Utils.Http.Tests.Integration.Controllers;

/// <summary>
/// 测试Api控制器2
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class Test2Controller : ControllerBase
{
    /// <summary>
    /// Post操作
    /// </summary>
    [HttpPost("create")]
    public string Create(CustomerDto dto) => $"ok:{dto.Code}";

    /// <summary>
    /// Post操作
    /// </summary>
    [HttpPost]
    public IActionResult Create2(CustomerDto dto) => new JsonResult(dto);
}