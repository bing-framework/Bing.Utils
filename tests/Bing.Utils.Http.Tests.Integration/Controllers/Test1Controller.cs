namespace Bing.Utils.Http.Tests.Integration.Controllers;

/// <summary>
/// 提供查询参数、请求头及 Cookie 回显的 GET 测试端点。
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class Test1Controller : ControllerBase
{
    /// <summary>
    /// 回显授权请求头。
    /// </summary>
    /// <returns>带 <c>ok:</c> 前缀的授权请求头值。</returns>
    [HttpGet("header")]
    public string GetHeader()
    {
        var header = Request.Headers["Authorization"].FirstOrDefault();
        return $"ok:{header}";
    }

    /// <summary>
    /// 返回用于 GET 请求验证的成功文本。
    /// </summary>
    /// <returns>固定文本 <c>ok</c>。</returns>
    [HttpGet]
    public string Get() => "ok";

    /// <summary>
    /// 返回用于 GET 请求验证的成功文本。
    /// </summary>
    /// <param name="id">需回显的路由标识。</param>
    /// <returns>带 <c>ok:</c> 前缀的路由标识。</returns>
    [HttpGet("{id}")]
    public string Get(string id) => $"ok:{id}";

    /// <summary>
    /// 回显绑定后的客户查询编码与姓名。
    /// </summary>
    /// <param name="query">从查询字符串绑定的客户查询条件。</param>
    /// <returns>包含客户编码与姓名的测试文本。</returns>
    [HttpGet("Query")]
    public string Query([FromQuery] CustomerQuery query) => $"code:{query.Code},name:{query.Name}";

    /// <summary>
    /// 将查询编码与姓名分别放入两个客户对象。
    /// </summary>
    /// <param name="query">从查询字符串绑定的客户查询条件。</param>
    /// <returns>分别携带编码与姓名的两个客户测试对象。</returns>
    [HttpGet("List")]
    public ActionResult<List<CustomerDto>> List([FromQuery] CustomerQuery query) =>
        new List<CustomerDto>
        {
            new() { Code = query.Code },
            new() { Name = query.Name }
        };

    /// <summary>
    /// 回显客户编码与姓名 Cookie。
    /// </summary>
    /// <returns>包含 <c>code</c> 与 <c>name</c> Cookie 值的测试文本。</returns>
    [HttpGet("cookie")]
    public string GetCookie() => $"code:{Request.Cookies["code"]},name:{Request.Cookies["name"]}";
}
