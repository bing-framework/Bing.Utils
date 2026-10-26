using Microsoft.AspNetCore.Http;

namespace Bing.Http;

public static partial class HttpResponseExtensions
{
    /// <summary>
    /// 将异步事件序列写为 UTF-8 SSE，每条刷新且保持响应体打开。
    /// </summary>
    /// <remarks>同时响应 RequestAborted 和显式取消；失败允许响应已有部分事件。
    /// 默认禁止响应缓存；代理是否缓冲须由部署配置决定。</remarks>
    public static Task WriteServerSentEventsAsync(this HttpResponse response, IAsyncEnumerable<ServerSentEvent> source,
        long maxEventBytes = 1048576, CancellationToken cancellationToken = default)
    {
        if (response == null) throw new ArgumentNullException(nameof(response));
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (maxEventBytes < 0) throw new ArgumentOutOfRangeException(nameof(maxEventBytes));
        return WriteRecordsResponseAsync(response, "text/event-stream; charset=utf-8", async token =>
        {
            response.Headers["Cache-Control"] = "no-cache";
            await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
                await ServerSentEvents.WriteAsync(response.Body, item, maxEventBytes, true, token).ConfigureAwait(false);
        }, cancellationToken);
    }
}
