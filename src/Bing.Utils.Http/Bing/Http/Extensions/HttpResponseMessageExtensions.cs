using System.Net.Http;

// ReSharper disable once CheckNamespace
namespace Bing.Http;

/// <summary>
/// Http响应消息(<see cref="HttpResponseMessage"/>) 扩展
/// </summary>
public static class HttpResponseMessageExtensions
{
    /// <summary>
    /// 获取内容类型
    /// </summary>
    /// <param name="message">Http响应消息</param>
    public static string GetContentType(this HttpResponseMessage message) => message?.Content.Headers.ContentType?.MediaType;
}