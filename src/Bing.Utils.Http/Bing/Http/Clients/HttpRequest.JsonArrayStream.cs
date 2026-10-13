using System.Net.Http;
using System.Text.Json;
using Bing.Http.Clients.Internal;

namespace Bing.Http.Clients;

public partial class HttpRequest<TResult> where TResult : class
{
    /// <summary>
    /// 标记其内部拥有并管理 HTTP 响应的对象。
    /// </summary>
    private interface IResponseOwnershipTransfer { }

    /// <summary>

    /// 持有响应消息和正文流并统一释放其资源。

    /// </summary>
    private sealed class JsonArrayResponseLease : IResponseOwnershipTransfer, IDisposable
    {
        /// <summary>
        /// 保存枚举期间持有的 HTTP 响应。
        /// </summary>
        private HttpResponseMessage _response;
        /// <summary>
        /// 提供正文前导处理和读取限制的响应流。
        /// </summary>
        private ResponseBodyReadStream _body;

        /// <summary>
        /// 初始化一个 <see cref="JsonArrayResponseLease"/> 类型的实例。
        /// </summary>
        public JsonArrayResponseLease(HttpResponseMessage response, ResponseBodyReadStream body)
        {
            _response = response;
            _body = body;
        }

        /// <summary>

        /// 获取已准备的响应正文读取流。

        /// </summary>
        public ResponseBodyReadStream Body => _body;

        /// <inheritdoc />
        public void Dispose()
        {
            var body = Interlocked.Exchange(ref _body, null);
            var response = Interlocked.Exchange(ref _response, null);
            try { body?.Dispose(); }
            finally { response?.Dispose(); }
        }
    }

    /// <inheritdoc />
    public IAsyncEnumerable<TResult> GetJsonArrayStreamAsync(CancellationToken cancellationToken = default)
    {
        if (SendAfterAction != null || SendAfterWithCancellationAction != null ||
            ConvertAction != null || FailAction != null || CompleteAction != null ||
            SuccessAction != null || SuccessFunc != null || _completionObserver != null)
            throw new NotSupportedException(
                "GetJsonArrayStreamAsync cannot be combined with response callbacks or completion observation.");
        return new JsonArrayEnumerable(this, cancellationToken);
    }

    /// <summary>

    /// 创建 JSON 数组枚举所需的响应租约和解析器。

    /// </summary>
    private Task<JsonArrayResponseLease> StartJsonArrayAsync(CancellationToken cancellationToken)
    {
        return ExecuteAsync(async (response, token) =>
        {
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException(
                    $"The HTTP response status code {(int)response.StatusCode} ({response.StatusCode}) indicates failure.");

            if (HasNoResponseBody(response) || response.Content == null)
                return new JsonArrayResponseLease(response, null);

            if (_maxResponseBytes.HasValue && response.Content.Headers.ContentLength > _maxResponseBytes.Value)
                throw new HttpResponseSizeLimitException(_maxResponseBytes.Value);

            var mediaType = response.Content.Headers.ContentType?.MediaType;
            if (!string.IsNullOrWhiteSpace(mediaType) &&
                !string.Equals(mediaType, "application/json", StringComparison.OrdinalIgnoreCase) &&
                !(mediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase)))
                throw new NotSupportedException(
                    "GetJsonArrayStreamAsync requires application/json or a +json response.");

            var charset = response.Content.Headers.ContentType?.CharSet?.Trim('"');
            if (!string.IsNullOrWhiteSpace(charset) &&
                !string.Equals(charset, "utf-8", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(charset, "utf8", StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException("GetJsonArrayStreamAsync requires a UTF-8 response.");

            ResponseBodyReadStream body = null;
            try
            {
                var source = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                body = new ResponseBodyReadStream(source, _maxResponseBytes);
                if (!await body.InitializeAsync(token).ConfigureAwait(false))
                {
                    body.Dispose();
                    return new JsonArrayResponseLease(response, null);
                }
                return new JsonArrayResponseLease(response, body);
            }
            catch
            {
                body?.Dispose();
                throw;
            }
        }, cancellationToken, allowRetries: false);
    }

    /// <summary>

    /// 为一次 JSON 数组响应创建可枚举序列。

    /// </summary>
    private sealed class JsonArrayEnumerable : IAsyncEnumerable<TResult>
    {
        /// <summary>
        /// 负责创建 JSON 数组响应的请求。
        /// </summary>
        private readonly HttpRequest<TResult> _request;
        /// <summary>
        /// 创建枚举器时传入的请求取消令牌。
        /// </summary>
        private readonly CancellationToken _requestToken;
        /// <summary>
        /// 标记响应流是否已开始枚举。
        /// </summary>
        private int _enumerated;

        /// <summary>
        /// 初始化一个 <see cref="JsonArrayEnumerable"/> 类型的实例。
        /// </summary>
        public JsonArrayEnumerable(HttpRequest<TResult> request, CancellationToken requestToken)
        {
            _request = request;
            _requestToken = requestToken;
        }

        /// <inheritdoc />
        public IAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _enumerated, 1) != 0)
                throw new InvalidOperationException("A JSON array stream can only be enumerated once.");

            CancellationTokenSource linked = null;
            var token = _requestToken;
            if (_requestToken.CanBeCanceled && cancellationToken.CanBeCanceled)
            {
                linked = CancellationTokenSource.CreateLinkedTokenSource(_requestToken, cancellationToken);
                token = linked.Token;
            }
            else if (cancellationToken.CanBeCanceled)
            {
                token = cancellationToken;
            }

            return new JsonArrayEnumerator(_request, token, linked);
        }
    }

    /// <summary>

    /// 按需读取并反序列化 JSON 数组元素。

    /// </summary>
    private sealed class JsonArrayEnumerator : IAsyncEnumerator<TResult>
    {
        /// <summary>
        /// 负责创建 JSON 数组响应的请求。
        /// </summary>
        private readonly HttpRequest<TResult> _request;
        /// <summary>
        /// 当前枚举操作使用的取消令牌。
        /// </summary>
        private readonly CancellationToken _token;
        /// <summary>
        /// 链接请求和枚举器释放信号的取消源。
        /// </summary>
        private readonly CancellationTokenSource _linkedTokenSource;
        /// <summary>
        /// 当前枚举器持有的响应资源租约。
        /// </summary>
        private JsonArrayResponseLease _lease;
        /// <summary>
        /// 增量解析 JSON 数组元素的解析器。
        /// </summary>
        private JsonArrayParser _parser;
        /// <summary>
        /// 标记解析器是否已读取数组起始标记。
        /// </summary>
        private bool _started;
        /// <summary>
        /// 标记枚举器是否已释放。
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// 初始化一个 <see cref="JsonArrayEnumerator"/> 类型的实例。
        /// </summary>
        public JsonArrayEnumerator(HttpRequest<TResult> request, CancellationToken token,
            CancellationTokenSource linkedTokenSource)
        {
            _request = request;
            _token = token;
            _linkedTokenSource = linkedTokenSource;
        }

        /// <summary>

        /// 获取当前反序列化的数组元素。

        /// </summary>
        public TResult Current { get; private set; }

        /// <inheritdoc />
        public async ValueTask<bool> MoveNextAsync()
        {
            if (_disposed) return false;
            try
            {
                if (!_started)
                {
                    _started = true;
                    _lease = await _request.StartJsonArrayAsync(_token).ConfigureAwait(false);
                    if (_lease.Body == null)
                    {
                        await DisposeAsync().ConfigureAwait(false);
                        return false;
                    }
                    _parser = new JsonArrayParser(_lease.Body, _request.GetJsonSerializerOptions());
                }

                if (await _parser.MoveNextAsync(_token).ConfigureAwait(false))
                {
                    Current = _parser.Current;
                    return true;
                }

                await DisposeAsync().ConfigureAwait(false);
                return false;
            }
            catch
            {
                await DisposeAsync().ConfigureAwait(false);
                throw;
            }
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            var parser = Interlocked.Exchange(ref _parser, null);
            parser?.Dispose();
            var lease = Interlocked.Exchange(ref _lease, null);
            lease?.Dispose();
            _linkedTokenSource?.Dispose();
            await Task.CompletedTask.ConfigureAwait(false);
        }
    }

    /// <summary>

    /// 跨缓冲区解析 JSON 数组并提取单个元素。

    /// </summary>
    private sealed class JsonArrayParser : IDisposable
    {
        /// <summary>
        /// 供 JSON 读取器读取的响应正文流。
        /// </summary>
        private readonly Stream _stream;
        /// <summary>
        /// 反序列化数组元素时使用的选项。
        /// </summary>
        private readonly JsonSerializerOptions _options;
        /// <summary>
        /// 暂存当前 JSON 元素的原始字节。
        /// </summary>
        private readonly MemoryStream _element = new MemoryStream();
        /// <summary>
        /// 保存增量读取的 JSON 正文缓冲区。
        /// </summary>
        private byte[] _buffer = new byte[16 * 1024];
        /// <summary>
        /// 缓冲区中下一个待解析字节的位置。
        /// </summary>
        private int _offset;
        /// <summary>
        /// 缓冲区当前有效字节数。
        /// </summary>
        private int _count;
        /// <summary>
        /// 跨缓冲区保留的 JSON 读取器状态。
        /// </summary>
        private JsonReaderState _readerState;
        /// <summary>
        /// 标记底层流是否已读到末尾。
        /// </summary>
        private bool _finalRead;
        /// <summary>
        /// 标记解析器是否已读取数组起始标记。
        /// </summary>
        private bool _started;
        /// <summary>
        /// 标记 JSON 数组结束标记是否已读取。
        /// </summary>
        private bool _arrayEnded;
        /// <summary>
        /// 标记数组解析及尾部校验是否完成。
        /// </summary>
        private bool _completed;
        /// <summary>
        /// 标记当前元素是否已开始解析。
        /// </summary>
        private bool _valueStarted;
        /// <summary>
        /// 记录当前复合元素的嵌套深度。
        /// </summary>
        private int _valueDepth;

        /// <summary>
        /// 初始化一个 <see cref="JsonArrayParser"/> 类型的实例。
        /// </summary>
        public JsonArrayParser(ResponseBodyReadStream body, JsonSerializerOptions options)
        {
            _stream = body;
            _options = options;
            _readerState = new JsonReaderState(new JsonReaderOptions
            {
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = options?.MaxDepth ?? 0
            });
        }

        /// <summary>

        /// 获取当前反序列化的数组元素。

        /// </summary>
        public TResult Current { get; private set; }

        /// <summary>

        /// 异步读取下一个 JSON 数组元素。

        /// </summary>
        public async ValueTask<bool> MoveNextAsync(CancellationToken token)
        {
            if (_completed) return false;
            while (true)
            {
                if (_arrayEnded)
                {
                    await ValidateTailAsync(token).ConfigureAwait(false);
                    _completed = true;
                    return false;
                }

                if (_offset == _count && !_finalRead)
                    await ReadMoreAsync(token).ConfigureAwait(false);

                var reader = new Utf8JsonReader(
                    new ReadOnlySpan<byte>(_buffer, _offset, _count - _offset),
                    _finalRead, _readerState);
                var previousConsumed = 0L;
                var endedInThisBuffer = false;

                while (reader.Read())
                {
                    var consumed = reader.BytesConsumed;
                    var tokenType = reader.TokenType;
                    if (!_started)
                    {
                        if (tokenType != JsonTokenType.StartArray)
                            throw new JsonException("The JSON response must start with an array.");
                        _started = true;
                        previousConsumed = consumed;
                        continue;
                    }

                    if (!_valueStarted && tokenType == JsonTokenType.EndArray)
                    {
                        _arrayEnded = true;
                        endedInThisBuffer = true;
                        previousConsumed = consumed;
                        break;
                    }

                    if (!_valueStarted)
                    {
                        _valueStarted = true;
                        _valueDepth = 0;
                        _element.SetLength(0);
                        previousConsumed = reader.TokenStartIndex;
                    }

                    AppendElementBytes(previousConsumed, consumed);
                    previousConsumed = consumed;

                    if (IsContainerStart(tokenType)) _valueDepth++;
                    else if (IsContainerEnd(tokenType)) _valueDepth--;

                    if (_valueDepth == 0 && (IsScalarValueToken(tokenType) || IsContainerEnd(tokenType)))
                    {
                        Current = JsonSerializer.Deserialize<TResult>(_element.ToArray(), _options);
                        _valueStarted = false;
                        _valueDepth = 0;
                        _offset += checked((int)consumed);
                        _readerState = reader.CurrentState;
                        return true;
                    }
                }

                var bytesConsumed = checked((int)reader.BytesConsumed);
                _offset += bytesConsumed;
                _readerState = reader.CurrentState;

                if (_arrayEnded && endedInThisBuffer) continue;
                if (_finalRead)
                    throw new JsonException(_started
                        ? "The JSON response ended before the array was complete."
                        : "The JSON response is empty or incomplete.");
                await ReadMoreAsync(token).ConfigureAwait(false);
            }
        }

        /// <summary>

        /// 将已消费的正文字节追加到当前 JSON 元素。

        /// </summary>
        private void AppendElementBytes(long previousConsumed, long consumed)
        {
            var start = checked((int)previousConsumed);
            var length = checked((int)(consumed - previousConsumed));
            if (length > 0) _element.Write(_buffer, checked(_offset + start), length);
        }

        /// <summary>

        /// 判断 JSON 令牌是否为容器起始标记。

        /// </summary>
        private static bool IsContainerStart(JsonTokenType tokenType) =>
            tokenType == JsonTokenType.StartObject || tokenType == JsonTokenType.StartArray;

        /// <summary>

        /// 判断 JSON 令牌是否为容器结束标记。

        /// </summary>
        private static bool IsContainerEnd(JsonTokenType tokenType) =>
            tokenType == JsonTokenType.EndObject || tokenType == JsonTokenType.EndArray;

        /// <summary>

        /// 判断 JSON 令牌是否表示标量值。

        /// </summary>
        private static bool IsScalarValueToken(JsonTokenType tokenType) =>
            tokenType == JsonTokenType.String || tokenType == JsonTokenType.Number ||
            tokenType == JsonTokenType.True || tokenType == JsonTokenType.False ||
            tokenType == JsonTokenType.Null;

        /// <summary>

        /// 校验数组结束标记之后不存在额外 JSON 内容。

        /// </summary>
        private async Task ValidateTailAsync(CancellationToken token)
        {
            while (true)
            {
                for (var index = _offset; index < _count; index++)
                {
                    var value = _buffer[index];
                    if (value != 0x20 && value != 0x09 && value != 0x0A && value != 0x0D)
                        throw new JsonException("Unexpected data after the JSON array.");
                }

                _offset = _count;
                if (_finalRead) return;
                await ReadMoreAsync(token).ConfigureAwait(false);
            }
        }

        /// <summary>

        /// 从响应流读取更多 JSON 正文数据。

        /// </summary>
        private async Task ReadMoreAsync(CancellationToken token)
        {
            if (_finalRead) return;
            if (_offset != 0)
            {
                if (_offset < _count)
                    Buffer.BlockCopy(_buffer, _offset, _buffer, 0, _count - _offset);
                _count -= _offset;
                _offset = 0;
            }

            if (_count == _buffer.Length)
            {
                var larger = new byte[checked(_buffer.Length * 2)];
                Buffer.BlockCopy(_buffer, 0, larger, 0, _count);
                _buffer = larger;
            }

            var read = await _stream.ReadAsync(_buffer, _count, _buffer.Length - _count, token)
                .ConfigureAwait(false);
            if (read == 0) _finalRead = true;
            else _count += read;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _element.Dispose();
            _buffer = null;
        }
    }
}
