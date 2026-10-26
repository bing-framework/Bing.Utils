using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.ExceptionServices;
using System.Runtime.CompilerServices;

namespace Bing.Http;

/// <summary>
/// 显式 SSE 连接与重连策略。
/// </summary>
public sealed class HttpSseOptions
{
    /// <summary>
    /// 获取或设置最大重连次数；默认零只打开一次连接，null 表示持续重连直到取消。
    /// </summary>
    public int? MaxReconnects { get; set; } = 0;
    /// <summary>
    /// 获取或设置初始重连间隔，默认三秒。
    /// </summary>
    public TimeSpan ReconnectDelay { get; set; } = TimeSpan.FromSeconds(3);
    /// <summary>
    /// 获取或设置服务端 retry 指令的等待上限，默认一分钟。
    /// </summary>
    public TimeSpan MaxReconnectDelay { get; set; } = TimeSpan.FromMinutes(1);
    /// <summary>
    /// 获取或设置每次等待正文新字节的最长时间；null 表示不限制。
    /// </summary>
    /// <remarks>必须大于零。注释和心跳字节同样结束本次等待；消费者处理事件的时间不计入。
    /// 超时按重连策略处理，次数耗尽抛出 TimeoutException。底层正文流必须响应读取取消。</remarks>
    public TimeSpan? IdleTimeout { get; set; }
    /// <summary>
    /// 获取或设置初始 Last-Event-ID，null 或空字符串不发送该头。
    /// </summary>
    public string LastEventId { get; set; }
    /// <summary>
    /// 获取或设置每条连接的格式及字节预算。
    /// </summary>
    public SseReadOptions ReadOptions { get; set; } = new();
    internal HttpSseOptions Snapshot()
    {
        if (MaxReconnects < 0) throw new ArgumentOutOfRangeException(nameof(MaxReconnects));
        if (ReconnectDelay < TimeSpan.Zero || MaxReconnectDelay < TimeSpan.Zero ||
            MaxReconnectDelay.TotalMilliseconds > int.MaxValue - 1 || ReconnectDelay > MaxReconnectDelay)
            throw new ArgumentOutOfRangeException(nameof(ReconnectDelay));
        if (LastEventId?.IndexOfAny(new[] { '\r', '\n', '\0' }) >= 0) throw new ArgumentException("Invalid Last-Event-ID.", nameof(LastEventId));
        if (IdleTimeout.HasValue && (IdleTimeout <= TimeSpan.Zero || IdleTimeout.Value.TotalMilliseconds > int.MaxValue - 1))
            throw new ArgumentOutOfRangeException(nameof(IdleTimeout));
        var result = (HttpSseOptions)MemberwiseClone(); result.ReadOptions = (ReadOptions ?? new SseReadOptions()).Snapshot(); return result;
    }
}

/// <summary>
/// 使用调用方 HttpClient 的逐条 SSE 消费入口。
/// </summary>
public static class HttpSseClientExtensions
{
    /// <summary>
    /// 发起 GET 并以 ResponseHeadersRead 读取事件，重连时携带最近已提交的事件 ID。
    /// </summary>
    /// <remarks>不拥有 HttpClient；每条连接拥有请求/响应，并在提前结束时释放。
    /// EOF、正文空闲超时和网络 I/O 错误可按配置重连；204 终止，其他非 200、内容类型错误、格式预算及取消不重试。
    /// HttpClient 的头等待超时仍生效；正文长连接由调用方令牌及可选 IdleTimeout 控制。</remarks>
    /// <returns>逐条事件，重连边界不产生虚构消息。</returns>
    public static IAsyncEnumerable<ServerSentEvent> GetServerSentEventsAsync(this HttpClient client, Uri uri,
        HttpSseOptions options = null, CancellationToken cancellationToken = default)
    {
        if (client == null) throw new ArgumentNullException(nameof(client));
        if (uri == null) throw new ArgumentNullException(nameof(uri));
        return ReadConnections(client, uri, (options ?? new HttpSseOptions()).Snapshot(), cancellationToken);
    }
    private static async IAsyncEnumerable<ServerSentEvent> ReadConnections(HttpClient client, Uri uri, HttpSseOptions options,
        [EnumeratorCancellation] CancellationToken token)
    {
        token.ThrowIfCancellationRequested(); var id = options.LastEventId ?? string.Empty; var delay = options.ReconnectDelay; var reconnects = 0;
        while (true)
        {
            token.ThrowIfCancellationRequested(); Exception failure = null; HttpResponseMessage response = null;
            using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
            {
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
                request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };
                if (id.Length != 0) request.Headers.TryAddWithoutValidation("Last-Event-ID", id);
                try { response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false); }
                catch (HttpRequestException error) { failure = error; }
                using (response)
                {
                    if (response != null)
                    {
                        if (response.StatusCode == HttpStatusCode.NoContent) yield break;
                        if (response.StatusCode != HttpStatusCode.OK) throw new HttpRequestException("SSE requires a 200 response.");
                        if (!string.Equals(response.Content?.Headers.ContentType?.MediaType, "text/event-stream", StringComparison.OrdinalIgnoreCase))
                            throw new InvalidDataException("SSE requires text/event-stream.");
                        var body = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                        var parser = new SseParser(body, options.ReadOptions, id, options.IdleTimeout);
                        while (true)
                        {
                            ServerSentEvent item = null;
                            try { item = await parser.ReadNextAsync(token).ConfigureAwait(false); }
                            catch (Exception error) when (error is TimeoutException || error is IOException && error is not InvalidDataException) { failure = error; }
                            id = parser.LastEventId;
                            if (parser.RetryMilliseconds.HasValue)
                                delay = TimeSpan.FromMilliseconds(Math.Min(parser.RetryMilliseconds.Value, options.MaxReconnectDelay.TotalMilliseconds));
                            if (failure != null || item == null) break;
                            token.ThrowIfCancellationRequested(); yield return item;
                        }
                    }
                }
            }
            token.ThrowIfCancellationRequested();
            if (options.MaxReconnects.HasValue && reconnects >= options.MaxReconnects.Value)
            { if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw(); yield break; }
            reconnects++; await Task.Delay(delay, token).ConfigureAwait(false);
        }
    }
}
