using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Bing.Http.Clients.Internal;

namespace Bing.Http.Clients;

/// <summary>
/// HTTP 请求的可选正文、下载进度与流式 JSON 能力。
/// </summary>
public partial class HttpRequest<TResult> : IHttpRequestRawContentRegistration<TResult>,
    IHttpRequestDownloadProgressRegistration<TResult>, IHttpRequestStreamingJson<TResult>, IHttpRequestStreamingResponse<TResult>, IHttpRequestJsonArrayStreaming<TResult>, IHttpRequestResumableDownload<TResult> where TResult : class
{
    /// <summary>
    /// 标记请求是否已配置原始二进制正文。
    /// </summary>
    private bool _hasRawBody;
    /// <summary>
    /// 保存待发送的二进制正文副本。
    /// </summary>
    private byte[] _binaryBytes;
    /// <summary>
    /// 保存待发送的二进制源流。
    /// </summary>
    private Stream _binaryStream;
    /// <summary>
    /// 记录可重放二进制流的注册起始位置。
    /// </summary>
    private long _binaryStartPosition;
    /// <summary>
    /// 标记请求发送后是否保留调用方的二进制流。
    /// </summary>
    private bool _binaryLeaveOpen;
    /// <summary>
    /// 为每次发送尝试创建独立正文内容的工厂。
    /// </summary>
    private Func<HttpContent> _bodyFactory;
    /// <summary>
    /// 接收正文下载进度快照的回调。
    /// </summary>
    private Action<HttpDownloadProgress> _downloadProgressObserver;
    /// <summary>
    /// 两次下载进度通知之间的最小字节间隔。
    /// </summary>
    private long _progressMinimumBytes;
    /// <summary>
    /// 两次下载进度通知之间的最小时间间隔。
    /// </summary>
    private TimeSpan _progressMinimumInterval;

    /// <inheritdoc />
    public void SetBinaryContent(byte[] content, string contentType)
    {
        if (content == null) throw new ArgumentNullException(nameof(content));
        ValidateBinaryContentType(contentType);
        EnsureNoStructuredBody();
        var copy = (byte[])content.Clone();
        ReplaceRawBody(null);
        _binaryBytes = copy;
        HttpContentType = contentType;
    }

    /// <inheritdoc />
    public void SetBinaryContent(Stream content, string contentType, bool leaveOpen)
    {
        if (content == null) throw new ArgumentNullException(nameof(content));
        if (!content.CanRead) throw new ArgumentException("Binary content must be readable.", nameof(content));
        ValidateBinaryContentType(contentType);
        EnsureNoStructuredBody();
        var start = content.CanSeek ? content.Position : 0;
        ReplaceRawBody(content);
        _binaryStream = content;
        _binaryStartPosition = start;
        _binaryLeaveOpen = leaveOpen;
        HttpContentType = contentType;
    }

    /// <inheritdoc />
    public void SetContentFactory(Func<HttpContent> factory)
    {
        if (factory == null) throw new ArgumentNullException(nameof(factory));
        EnsureNoStructuredBody();
        ReplaceRawBody(null);
        _bodyFactory = factory;
    }

    /// <summary>
    /// 验证原始二进制正文的内容类型。
    /// </summary>
    private static void ValidateBinaryContentType(string contentType)
    {
        if (contentType == null) throw new ArgumentNullException(nameof(contentType));
        MediaTypeHeaderValue.Parse(contentType);
    }

    /// <summary>
    /// 拒绝与已配置的结构化正文混用。
    /// </summary>
    private void EnsureNoStructuredBody()
    {
        if (Param != null || Params.Count != 0 || Files.Count != 0)
            throw new NotSupportedException("BinaryContent and ContentFactory cannot be combined with structured or multipart content.");
    }

    /// <summary>
    /// 替换当前原始正文并按所有权释放旧流。
    /// </summary>
    private void ReplaceRawBody(Stream replacement)
    {
        if (_binaryStream != null && !_binaryLeaveOpen && !ReferenceEquals(_binaryStream, replacement))
            _binaryStream.Dispose();
        _binaryBytes = null;
        _binaryStream = null;
        _bodyFactory = null;
        _hasRawBody = true;
    }

    /// <summary>
    /// 创建可供请求发送并支持重放的原始正文内容。
    /// </summary>
    private HttpContent CreateRawHttpContent()
    {
        EnsureNoStructuredBody();
        if (_bodyFactory != null)
            return _bodyFactory() ?? throw new InvalidOperationException("ContentFactory returned null.");
        if (_binaryStream != null && _binaryStream.CanSeek)
            _binaryStream.Position = _binaryStartPosition;
        var content = _binaryStream != null
            ? (HttpContent)new StreamContent(new NonClosingStream(_binaryStream))
            : new ByteArrayContent(_binaryBytes);
        try
        {
            content.Headers.ContentType = MediaTypeHeaderValue.Parse(HttpContentType);
            return content;
        }
        catch (Exception ex)
        {
            DisposePreservingException(content, ex);
            throw;
        }
    }

    /// <inheritdoc />
    public void SetDownloadProgressObserver(Action<HttpDownloadProgress> observer, long minimumBytes, TimeSpan minimumInterval)
    {
        if (observer == null) throw new ArgumentNullException(nameof(observer));
        if (minimumBytes <= 0) throw new ArgumentOutOfRangeException(nameof(minimumBytes));
        if (minimumInterval < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(minimumInterval));
        _downloadProgressObserver = observer;
        _progressMinimumBytes = minimumBytes;
        _progressMinimumInterval = minimumInterval;
    }

    /// <summary>
    /// 按字节数和时间阈值跟踪响应下载进度。
    /// </summary>
    private sealed class DownloadProgressState
    {
        /// <summary>
        /// 接收当前响应下载进度的回调。
        /// </summary>
        private readonly Action<HttpDownloadProgress> _observer;
        /// <summary>
        /// 触发下一次进度通知所需的最小字节增量。
        /// </summary>
        private readonly long _minimumBytes;
        /// <summary>
        /// 触发下一次进度通知所需的最小时间间隔。
        /// </summary>
        private readonly TimeSpan _minimumInterval;
        /// <summary>
        /// 记录当前正文复制的经过时间。
        /// </summary>
        private readonly Stopwatch _watch = Stopwatch.StartNew();
        /// <summary>
        /// 上一次已通知的累计字节数。
        /// </summary>
        private long _lastBytes;
        /// <summary>
        /// 上一次已通知时的经过时间。
        /// </summary>
        private TimeSpan _lastTime;

        /// <summary>
        /// 初始化一个 <see cref="DownloadProgressState"/> 类型的实例。
        /// </summary>
        public DownloadProgressState(Action<HttpDownloadProgress> observer, long minimumBytes, TimeSpan minimumInterval)
        {
            _observer = observer;
            _minimumBytes = minimumBytes;
            _minimumInterval = minimumInterval;
        }

        /// <summary>
        /// 按进度阈值向观察者报告下载状态。
        /// </summary>
        /// <param name="bytes">已复制的字节数。</param>
        /// <param name="total">已知时表示正文总字节数。</param>
        /// <param name="completed">是否已完成正文复制。</param>
        /// <param name="force">是否忽略阈值立即报告。</param>
        public void Report(long bytes, long? total, bool completed, bool force)
        {
            var elapsed = _watch.Elapsed;
            if (!force && (bytes - _lastBytes < _minimumBytes || elapsed - _lastTime < _minimumInterval)) return;
            _observer(new HttpDownloadProgress(bytes, total, completed));
            _lastBytes = bytes;
            _lastTime = elapsed;
        }
    }

    /// <inheritdoc />
    public Task<TResult> GetJsonStreamAsync(CancellationToken cancellationToken = default)
    {
        if (SendAfterAction != null || SendAfterWithCancellationAction != null)
            return FailPreflightAsync<TResult>(new NotSupportedException("GetJsonStreamAsync cannot be combined with OnSendAfter."));
        return ExecuteAsync(async (response, token) =>
        {
            // 需要原文语义时走既有扩展点，避免流式解析改变转换和正文回调。
            if (!CanStreamJson(response))
            {
                _operationToken.Value = token;
                try { return await SendAfterAsync(response).ConfigureAwait(false); }
                finally { _operationToken.Value = default; }
            }
            return await ReadJsonStreamAsync(response, token).ConfigureAwait(false);
        }, cancellationToken);
    }

    /// <summary>
    /// 判断响应是否可按 JSON 正文流式解析。
    /// </summary>
    private bool CanStreamJson(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode || ConvertAction != null || FailAction != null || CompleteAction != null ||
            typeof(TResult) == typeof(string)) return false;
        if (HasNoResponseBody(response) || response.Content == null) return true;
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (!string.Equals(mediaType, "application/json", StringComparison.OrdinalIgnoreCase) &&
            !(mediaType?.EndsWith("+json", StringComparison.OrdinalIgnoreCase) == true)) return false;
        var charset = response.Content.Headers.ContentType?.CharSet?.Trim('"');
        return string.IsNullOrWhiteSpace(charset) || System.Text.Encoding.GetEncoding(charset).CodePage == 65001;
    }

    /// <summary>
    /// 流式解析响应 JSON 并生成请求结果。
    /// </summary>
    private async Task<TResult> ReadJsonStreamAsync(HttpResponseMessage response, CancellationToken token)
    {
        var result = await ReadJsonStreamResponseAsync(response, token).ConfigureAwait(false);
        return result?.Data;
    }

    /// <summary>
    /// 流式解析 JSON 并保留响应元数据。
    /// </summary>
    private async Task<HttpResponseResult<TResult>> ReadJsonStreamResponseAsync(
        HttpResponseMessage response, CancellationToken token)
    {
        Stream source = null;
        ResponseBodyReadStream body = null;
        Exception processingError = null;
        try
        {
            TResult result = null;
            if (!HasNoResponseBody(response) && response.Content != null)
            {
                if (EffectiveMaxResponseBytes.HasValue && response.Content.Headers.ContentLength > EffectiveMaxResponseBytes.Value)
                    throw new HttpResponseSizeLimitException(EffectiveMaxResponseBytes.Value);
                source = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                body = new ResponseBodyReadStream(source, EffectiveMaxResponseBytes);
                if (await body.InitializeAsync(token).ConfigureAwait(false))
                    result = await JsonSerializer.DeserializeAsync<TResult>(body, GetJsonSerializerOptions(), token).ConfigureAwait(false);
            }
            SuccessAction?.Invoke(result);
            if (SuccessFunc != null) await SuccessFunc(result).ConfigureAwait(false);
            return new HttpResponseResult<TResult>(response, result, null);
        }
        catch (Exception ex)
        {
            processingError = ex;
            throw;
        }
        finally
        {
            try { DisposePreservingException((IDisposable)body ?? source, processingError); }
            catch (Exception ex) { processingError = ex; throw; }
            finally { CompleteHandlerPreservingException(response, null, processingError); }
        }
    }

    /// <inheritdoc />
    public Task<HttpResponseResult<TResult>> GetJsonStreamResponseAsync(CancellationToken cancellationToken = default)
    {
        if (SendAfterAction != null || SendAfterWithCancellationAction != null)
            return FailPreflightAsync<HttpResponseResult<TResult>>(
                new NotSupportedException("GetJsonStreamResponseAsync cannot be combined with OnSendAfter."));
        return ExecuteAsync(async (response, token) =>
        {
            if (!CanStreamJson(response))
                return await ReadResponseResultAsync(response, token, false).ConfigureAwait(false);
            return await ReadJsonStreamResponseAsync(response, token).ConfigureAwait(false);
        }, cancellationToken);
    }
}
