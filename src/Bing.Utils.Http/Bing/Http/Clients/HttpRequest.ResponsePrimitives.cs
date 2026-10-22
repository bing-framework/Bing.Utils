using System.IO;
using System.Net;
using System.Text;
using Bing.Extensions;
using Bing.Helpers;
using Bing.Serialization.SystemTextJson;

namespace Bing.Http.Clients;

public partial class HttpRequest<TResult> where TResult : class
{
    /// <summary>
    /// 复制响应头及其值，避免依赖可变响应对象。
    /// </summary>
    private static IReadOnlyDictionary<string, IReadOnlyList<string>> SnapshotHeaders(System.Net.Http.Headers.HttpHeaders headers)
    {
        var values = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        if (headers != null)
            foreach (var header in headers)
                values[header.Key] = Array.AsReadOnly(header.Value.ToArray());
        return values;
    }

    /// <summary>
    /// 清理一个资源；已有主异常时保留主异常。
    /// </summary>
    private static void DisposePreservingException(IDisposable resource, Exception primaryError)
    {
        try { resource?.Dispose(); }
        catch when (primaryError != null) { }
    }

    /// <summary>
    /// 将响应正文读取为字节数组；不执行失败或完成回调。
    /// </summary>
    /// <param name="response">Http响应消息。</param>
    /// <param name="token">取消令牌。</param>
    /// <returns>响应正文的字节数组。</returns>
    private async Task<byte[]> ReadResponseBytesAsync(HttpResponseMessage response, CancellationToken token)
    {
        using var buffer = new MemoryStream();
        await CopyResponseAsync(response, buffer, token);
        return buffer.ToArray();
    }

    #region ConvertTo(将内容转换为结果)

    /// <summary>
    /// 将内容转换为结果
    /// </summary>
    /// <param name="content">内容</param>
    /// <param name="contentType">内容类型</param>
    protected virtual TResult ConvertTo(string content, string contentType)
    {
        if (ConvertAction != null) return ConvertAction(content);
        if (typeof(TResult) == typeof(string)) return (TResult)(object)content;
        return (contentType.SafeString().Equals("application/json", StringComparison.OrdinalIgnoreCase) ||
                contentType.SafeString().EndsWith("+json", StringComparison.OrdinalIgnoreCase))
            ? Json.ToObject<TResult>(content, GetJsonSerializerOptions()) : null;
    }

    #endregion

    /// <summary>
    /// 将响应正文复制到可解码缓冲区；复制过程沿用统一的大小限制、取消和资源释放语义。
    /// </summary>
    /// <param name="response">Http响应消息。</param>
    /// <param name="token">取消令牌。</param>
    /// <returns>位置已重置到开头的正文缓冲区。</returns>
    private async Task<MemoryStream> ReadResponseBufferAsync(HttpResponseMessage response, CancellationToken token)
    {
        var buffer = new MemoryStream();
        try
        {
            await CopyResponseAsync(response, buffer, token).ConfigureAwait(false);
            buffer.Position = 0;
            return buffer;
        }
        catch (Exception error)
        {
            DisposePreservingException(buffer, error);
            throw;
        }
    }

    /// <summary>
    /// 根据响应声明和前导字节解析正文编码；调用方仍负责创建读取器和读取正文。
    /// </summary>
    /// <param name="mediaType">响应媒体类型。</param>
    /// <param name="charset">响应声明的字符集。</param>
    /// <param name="preamble">正文开头的字节缓冲区。</param>
    /// <param name="preambleLength">前导字节的有效长度。</param>
    /// <returns>用于读取正文的编码。</returns>
    private static System.Text.Encoding ResolveResponseEncoding(string mediaType, string charset, byte[] preamble, int preambleLength)
    {
        _ = mediaType;
        var declaredEncoding = string.IsNullOrWhiteSpace(charset)
            ? System.Text.Encoding.UTF8
            : System.Text.Encoding.GetEncoding(charset);
        if (preamble == null || preambleLength <= 0)
            return declaredEncoding;

        var length = Math.Min(Math.Min(preambleLength, preamble.Length), 4);
        if (length >= 4 && preamble[0] == 0xFF && preamble[1] == 0xFE && preamble[2] == 0x00 && preamble[3] == 0x00)
            return new UTF32Encoding(false, true);
        if (length >= 4 && preamble[0] == 0x00 && preamble[1] == 0x00 && preamble[2] == 0xFE && preamble[3] == 0xFF)
            return new UTF32Encoding(true, true);
        if (length >= 3 && preamble[0] == 0xEF && preamble[1] == 0xBB && preamble[2] == 0xBF)
            return new UTF8Encoding(true);
        if (length >= 2 && preamble[0] == 0xFF && preamble[1] == 0xFE)
            return new UnicodeEncoding(false, true);
        if (length >= 2 && preamble[0] == 0xFE && preamble[1] == 0xFF)
            return new UnicodeEncoding(true, true);
        return declaredEncoding;
    }

    /// <summary>
    /// 判断 HTTP 响应是否按语义不包含正文。
    /// </summary>
    private static bool HasNoResponseBody(HttpResponseMessage response) =>
        response.StatusCode == HttpStatusCode.NoContent || response.RequestMessage?.Method == HttpMethod.Head;
}
