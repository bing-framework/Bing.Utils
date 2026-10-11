using System.IO;
using System.Text;

namespace Bing.Http.Clients;

public partial class HttpRequest<TResult> where TResult : class
{
    /// <summary>
    /// 读取响应正文并生成包含元数据的结果。
    /// </summary>
    private async Task<HttpResponseResult<TResult>> ReadResponseResultAsync(
        HttpResponseMessage response, CancellationToken token, bool includeRawContent)
    {
        string content = null;
        Exception processingError = null;
        MemoryStream buffer = null;
        try
        {
            var hasNoBody = HasNoResponseBody(response);
            if (!hasNoBody)
                buffer = await ReadResponseBufferAsync(response, token).ConfigureAwait(false);

            var bodyLength = buffer?.Length ?? 0;
            content = string.Empty;
            if (bodyLength != 0)
                content = await DecodeResponseTextAsync(response, buffer).ConfigureAwait(false);

            TResult data = null;
            if (response.IsSuccessStatusCode)
            {
                if (bodyLength != 0 && !hasNoBody)
                    data = await SuccessHandlerAsync(response, content).ConfigureAwait(false);
                else
                {
                    SuccessAction?.Invoke(null);
                    if (SuccessFunc != null)
                        await SuccessFunc(null).ConfigureAwait(false);
                }
            }
            else
                FailHandler(response, content);

            return new HttpResponseResult<TResult>(response, data, includeRawContent ? content : null);
        }
        catch (Exception ex)
        {
            processingError = ex;
            throw;
        }
        finally
        {
            buffer?.Dispose();
            CompleteHandlerPreservingException(response, content, processingError);
        }
    }
    /// <summary>
    /// 根据响应编码声明和字节前导解析正文文本。
    /// </summary>
    private static async Task<string> DecodeResponseTextAsync(HttpResponseMessage response, MemoryStream buffer)
    {
        var charset = response.Content?.Headers.ContentType?.CharSet?.Trim('"');
        var encoding = ResolveResponseEncoding(
            response.Content?.Headers.ContentType?.MediaType,
            charset,
            buffer.GetBuffer(),
            (int)Math.Min(buffer.Length, 4));
        using var reader = new StreamReader(buffer, encoding, true, 1024, leaveOpen: true);
        return await reader.ReadToEndAsync().ConfigureAwait(false);
    }
}
