﻿namespace Bing.Utils.Http.Tests.Integration.Controllers;

/// <summary>
/// 测试Api控制器3
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class Test3Controller : ControllerBase
{
    /// <summary>
    /// Put操作
    /// </summary>
    [HttpPut("update")]
    public string Update(CustomerDto dto) => $"ok:{dto.Code}";

    /// <summary>
    /// Put操作
    /// </summary>
    [HttpPut("{id}")]
    public IActionResult Update2(string id, CustomerDto dto)
    {
        dto.Id = id;
        return new JsonResult(dto);
    }
}