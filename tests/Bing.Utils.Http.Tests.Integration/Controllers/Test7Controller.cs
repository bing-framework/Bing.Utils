using System.Collections.Concurrent;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Bing.Utils.Http.Tests.Integration.Controllers;

/// <summary>
/// 测试Api控制器7 - 用于测试组合请求、超时取消、错误映射与序列化链路
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class Test7Controller : ControllerBase
{
    private static readonly ConcurrentDictionary<string, int> RetryCounter = new();

    /// <summary>
    /// 组合请求：Header + Query + Body
    /// </summary>
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
    /// 延迟响应，用于测试超时与取消
    /// </summary>
    [HttpGet("delay/{milliseconds:int}")]
    public async Task<string> Delay(int milliseconds)
    {
        await Task.Delay(milliseconds, HttpContext.RequestAborted);
        return $"ok:{milliseconds}";
    }

    /// <summary>
    /// 指定状态码响应
    /// </summary>
    [HttpGet("status/{statusCode:int}")]
    public IActionResult Status(int statusCode) => StatusCode(statusCode, new ErrorResponse
    {
        Code = statusCode,
        Message = $"error:{statusCode}"
    });

    /// <summary>
    /// 第一次返回500，后续返回成功，用于重试链路测试
    /// </summary>
    [HttpGet("retry-once/{key}")]
    public IActionResult RetryOnce(string key)
    {
        var attempt = RetryCounter.AddOrUpdate(key, 1, (_, current) => current + 1);
        if (attempt == 1)
            return StatusCode(500, new ErrorResponse { Code = 500, Message = "transient" });
        return Ok($"ok:{attempt}");
    }

    /// <summary>
    /// 回显xml请求内容与内容类型
    /// </summary>
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
    /// 日期序列化回显
    /// </summary>
    [HttpPost("datetime-echo")]
    public ActionResult<CustomerDto> DateTimeEcho([FromBody] CustomerDto dto) => dto;

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

    public class ComposeResponse
    {
        public string Id { get; set; }
        public string Source { get; set; }
        public string CorrelationId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public DateTime? Birthday { get; set; }
    }

    public class ErrorResponse
    {
        public int Code { get; set; }
        public string Message { get; set; }
    }

    public class RawPayloadResponse
    {
        public string ContentType { get; set; }
        public string Charset { get; set; }
        public string Body { get; set; }
    }
}
