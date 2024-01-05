namespace Bing.Utils.Http.Tests.Integration.Controllers;

/// <summary>
/// 测试Api控制器1
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class Test1Controller : ControllerBase
{
    /// <summary>
    /// 获取请求头操作
    /// </summary>
    [HttpGet("header")]
    public string GetHeader()
    {
        var header = Request.Headers["Authorization"].FirstOrDefault();
        return $"ok:{header}";
    }

    /// <summary>
    /// 默认Get操作
    /// </summary>
    [HttpGet]
    public string Get() => "ok";

    /// <summary>
    /// 通过id获取
    /// </summary>
    [HttpGet("{id}")]
    public string Get(string id) => $"ok:{id}";

    /// <summary>
    /// 查询操作,通过query参数绑定
    /// </summary>
    [HttpGet("Query")]
    public string Query([FromQuery] CustomerQuery query) => $"code:{query.Code},name:{query.Name}";

    /// <summary>
    /// 查询操作,返回对象
    /// </summary>
    [HttpGet("List")]
    public ActionResult<List<CustomerDto>> List([FromQuery] CustomerQuery query) =>
        new List<CustomerDto>
        {
            new() { Code = query.Code },
            new() { Name = query.Name }
        };

    /// <summary>
    /// 获取cookie值
    /// </summary>
    [HttpGet("cookie")]
    public string GetCookie() => $"code:{Request.Cookies["code"]},name:{Request.Cookies["name"]}";
}