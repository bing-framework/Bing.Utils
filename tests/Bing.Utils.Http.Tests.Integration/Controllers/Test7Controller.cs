using System.Collections.Concurrent;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Bing.Utils.Http.Tests.Integration.Controllers;

/// <summary>
/// 提供请求组合、延迟响应、重试及序列化回显测试端点。
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class Test7Controller : ControllerBase
{
    /// <summary>
    /// 按请求键累计重试测试端点的访问次数。
    /// </summary>
    private static readonly ConcurrentDictionary<string, int> RetryCounter = new();

    /// <summary>
    /// 组合路由、查询字符串、请求头与客户数据以验证请求绑定。
    /// </summary>
    /// <param name="id">路由中的客户标识。</param>
    /// <param name="source">查询字符串中的来源。</param>
    /// <param name="dto">请求正文中的客户数据。</param>
    /// <returns>包含各请求来源数据的组合响应。</returns>
    [HttpPost("compose/{id}")]
    public ActionResult<ComposeResponse> Compose(string id, [FromQuery] string source, [FromBody] CustomerDto dto)
    {
        var correlationId = Request.Headers["X-Correlation-Id"].FirstOrDefault();
        return new ComposeResponse
        {
            Id = id,
            Source = source,
            CorrelationId = correlationId,
            Code = dto?.Code,
            Name = dto?.Name,
            Birthday = dto?.Birthday
        };
    }

    /// <summary>
    /// 延迟指定时间后返回测试文本。
    /// </summary>
    /// <param name="milliseconds">响应前等待的毫秒数。</param>
    /// <returns>包含延迟毫秒数的成功文本。</returns>
    /// <remarks>延迟受请求中止令牌控制。</remarks>
    [HttpGet("delay/{milliseconds:int}")]
    public async Task<string> Delay(int milliseconds)
    {
        await Task.Delay(milliseconds, HttpContext.RequestAborted);
        return $"ok:{milliseconds}";
    }

    /// <summary>
    /// 返回指定状态码及对应错误数据。
    /// </summary>
    /// <param name="statusCode">需要返回的 HTTP 状态码。</param>
    /// <returns>包含状态码与错误文本的响应。</returns>
    [HttpGet("status/{statusCode:int}")]
    public IActionResult Status(int statusCode) => StatusCode(statusCode, new ErrorResponse
    {
        Code = statusCode,
        Message = $"error:{statusCode}"
    });

    /// <summary>
    /// 抛出受控异常，由测试宿主中间件生成错误响应。
    /// </summary>
    [HttpGet("throw")]
    public IActionResult Throw() => throw new InvalidOperationException("integration-test-exception");

    /// <summary>
    /// 按请求键模拟首次失败后成功的重试响应。
    /// </summary>
    /// <param name="key">用于隔离访问次数的请求键。</param>
    /// <returns>首次访问返回 HTTP 500，后续访问返回包含累计次数的成功响应。</returns>
    [HttpGet("retry-once/{key}")]
    public IActionResult RetryOnce(string key)
    {
        var attempt = RetryCounter.AddOrUpdate(key, 1, (_, current) => current + 1);
        if (attempt == 1)
            return StatusCode(500, new ErrorResponse { Code = 500, Message = "transient" });
        return Ok($"ok:{attempt}");
    }

    /// <summary>
    /// 读取并回显原始请求正文及其内容类型和字符集。
    /// </summary>
    /// <returns>包含原始正文、内容类型和解析字符集的响应。</returns>
    [HttpPost("echo-xml")]
    public async Task<ActionResult<RawPayloadResponse>> EchoXml()
    {
        var contentType = Request.ContentType ?? string.Empty;
        var encoding = ResolveRequestEncoding(contentType);
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, encoding, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        Request.Body.Position = 0;
        return new RawPayloadResponse
        {
            ContentType = contentType,
            Charset = encoding.WebName,
            Body = body
        };
    }

    /// <summary>
    /// 回显客户数据以验证日期序列化。
    /// </summary>
    /// <param name="dto">请求正文中的客户数据。</param>
    /// <returns>原客户数据。</returns>
    [HttpPost("datetime-echo")]
    public ActionResult<CustomerDto> DateTimeEcho([FromBody] CustomerDto dto) => dto;

    /// <summary>
    /// 解析内容类型声明的字符编码。
    /// </summary>
    /// <param name="contentType">待解析的请求内容类型。</param>
    /// <returns>声明的字符编码；内容类型无效、字符集缺失或无法识别时返回 UTF-8。</returns>
    private static Encoding ResolveRequestEncoding(string contentType)
    {
        if (!MediaTypeHeaderValue.TryParse(contentType, out var mediaType))
            return Encoding.UTF8;
        var charset = mediaType.Charset.ToString();
        if (string.IsNullOrWhiteSpace(charset))
            return Encoding.UTF8;
        try
        {
            return Encoding.GetEncoding(charset);
        }
        catch
        {
            return Encoding.UTF8;
        }
    }

    /// <summary>
    /// 保存请求组合测试中来自不同请求位置的数据。
    /// </summary>
    public class ComposeResponse
    {
        /// <summary>
        /// 获取或设置路由中的客户标识。
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// 获取或设置查询字符串中的来源。
        /// </summary>
        public string Source { get; set; }
        /// <summary>
        /// 获取或设置关联标识请求头的值。
        /// </summary>
        public string CorrelationId { get; set; }
        /// <summary>
        /// 获取或设置请求正文中的客户编码。
        /// </summary>
        public string Code { get; set; }
        /// <summary>
        /// 获取或设置请求正文中的客户姓名。
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 获取或设置请求正文中的客户出生日期。
        /// </summary>
        public DateTime? Birthday { get; set; }
    }

    /// <summary>
    /// 保存指定状态码与重试失败测试的错误响应。
    /// </summary>
    public class ErrorResponse
    {
        /// <summary>
        /// 获取或设置错误状态码。
        /// </summary>
        public int Code { get; set; }
        /// <summary>
        /// 获取或设置错误测试文本。
        /// </summary>
        public string Message { get; set; }
    }

    /// <summary>
    /// 保存原始请求正文及其编码信息。
    /// </summary>
    public class RawPayloadResponse
    {
        /// <summary>
        /// 获取或设置原请求内容类型。
        /// </summary>
        public string ContentType { get; set; }
        /// <summary>
        /// 获取或设置用于读取正文的字符编码名称。
        /// </summary>
        public string Charset { get; set; }
        /// <summary>
        /// 获取或设置原始请求正文文本。
        /// </summary>
        public string Body { get; set; }
    }
}
