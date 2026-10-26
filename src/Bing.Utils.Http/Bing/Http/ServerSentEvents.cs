using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bing.Http;

/// <summary>
/// 一条服务端推送事件。
/// </summary>
public sealed class ServerSentEvent
{
    /// <summary>
    /// 获取或设置事件数据，多行数据用 LF 连接。
    /// </summary>
    public string Data { get; set; } = string.Empty;
    /// <summary>
    /// 获取或设置事件类型，默认 message。
    /// </summary>
    public string EventType { get; set; } = "message";
    /// <summary>
    /// 获取或设置事件 ID；写入时 null 省略字段，空字符串重置客户端 ID。
    /// </summary>
    public string Id { get; set; }
    /// <summary>
    /// 获取或设置服务器建议的非负重连间隔，单位为毫秒。
    /// </summary>
    public long? RetryMilliseconds { get; set; }
}

/// <summary>
/// SSE 格式读取与写入的预算。
/// </summary>
public sealed class SseReadOptions
{
    /// <summary>
    /// 获取或设置单个空行分隔块的 UTF-8 字节上限；默认 1 MiB，含字段、注释及 BOM，不含 CR/LF。
    /// </summary>
    public long MaxEventBytes { get; set; } = 1048576;
    /// <summary>
    /// 获取或设置单条连接总输入字节预算，null 不限制。
    /// </summary>
    public long? MaxInputBytes { get; set; }
    internal SseReadOptions Snapshot()
    {
        if (MaxEventBytes < 0) throw new ArgumentOutOfRangeException(nameof(MaxEventBytes));
        if (MaxInputBytes < 0) throw new ArgumentOutOfRangeException(nameof(MaxInputBytes));
        return (SseReadOptions)MemberwiseClone();
    }
}

/// <summary>
/// 流式读取和写入 UTF-8 SSE 格式。
/// </summary>
public static class ServerSentEvents
{
    /// <summary>
    /// 从当前位置读取事件；支持 CR、LF、CRLF 和 BOM，EOF 丢弃未以空行结束的事件。
    /// </summary>
    /// <remarks>按 WHATWG 规则忽略注释和未知字段；UTF-8 解码使用替换字符处理无效字节。
    /// 提前结束及执行中失败遵守 leaveOpen；开始前取消不接管流。</remarks>
    /// <returns>逐条事件，不缓存整个连接。</returns>
    public static IAsyncEnumerable<ServerSentEvent> ReadAsync(Stream source, SseReadOptions options = null,
        bool leaveOpen = true, CancellationToken cancellationToken = default)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (!source.CanRead) throw new ArgumentException("Source must be readable.", nameof(source));
        return ReadCore(source, (options ?? new SseReadOptions()).Snapshot(), leaveOpen, cancellationToken);
    }
    private static async IAsyncEnumerable<ServerSentEvent> ReadCore(Stream source, SseReadOptions config, bool leaveOpen,
        [EnumeratorCancellation] CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        try
        {
            var parser = new SseParser(source, config, string.Empty);
            while (true)
            {
                var item = await parser.ReadNextAsync(token).ConfigureAwait(false);
                if (item == null) break;
                yield return item;
            }
        }
        finally { if (!leaveOpen) source.Dispose(); }
    }

    /// <summary>
    /// 写入一条完整 SSE 事件并可选刷新，始终保持目标打开。
    /// </summary>
    /// <remarks>验证及预算通过后才写入；I/O 失败可留下部分事件。数据换行统一为 LF，事件和 ID 字段拒绝 CR/LF 注入。</remarks>
    public static async Task WriteAsync(Stream destination, ServerSentEvent value, long maxEventBytes = 1048576,
        bool flush = true, CancellationToken cancellationToken = default)
    {
        if (destination == null) throw new ArgumentNullException(nameof(destination));
        if (!destination.CanWrite) throw new ArgumentException("Destination must be writable.", nameof(destination));
        if (value == null) throw new ArgumentNullException(nameof(value));
        if (maxEventBytes < 0) throw new ArgumentOutOfRangeException(nameof(maxEventBytes));
        ValidateField(value.EventType, nameof(value.EventType)); ValidateField(value.Id, nameof(value.Id));
        if (value.Id?.IndexOf('\0') >= 0 || value.RetryMilliseconds < 0) throw new ArgumentException("Invalid SSE ID or retry interval.", nameof(value));
        cancellationToken.ThrowIfCancellationRequested();
        var data = value.Data ?? string.Empty;
        if (Encoding.UTF8.GetByteCount(data) > maxEventBytes) throw new InvalidDataException("SSE event exceeds its byte budget.");
        using var output = new MemoryStream();
        void Line(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text + "\n");
            if (output.Length > maxEventBytes - bytes.Length) throw new InvalidDataException("SSE event exceeds its byte budget.");
            output.Write(bytes, 0, bytes.Length);
        }
        if (value.Id != null) Line("id: " + value.Id);
        if (!string.IsNullOrEmpty(value.EventType)) Line("event: " + value.EventType);
        if (value.RetryMilliseconds.HasValue) Line("retry: " + value.RetryMilliseconds.Value.ToString(CultureInfo.InvariantCulture));
        foreach (var line in data.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n')) { cancellationToken.ThrowIfCancellationRequested(); Line("data: " + line); }
        Line(string.Empty);
        await destination.WriteAsync(output.GetBuffer(), 0, checked((int)output.Length), cancellationToken).ConfigureAwait(false);
        if (flush) await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
    private static void ValidateField(string value, string name)
    { if (value?.IndexOfAny(new[] { '\r', '\n' }) >= 0) throw new ArgumentException("SSE fields cannot contain newlines.", name); }
}

/// <summary>
/// 保留连接 ID 与重连指令的增量 SSE 解析器。
/// </summary>
internal sealed class SseParser
{
    private readonly Stream _source;
    private readonly SseReadOptions _options;
    /// <summary>
    /// 底层读取的空闲等待预算，不包含消费者处理事件的时间。
    /// </summary>
    private readonly TimeSpan? _idleTimeout;
    private readonly byte[] _buffer = new byte[4096];
    private int _offset, _count;
    private long _read, _blockBytes;
    private bool _skipLf, _first = true;
    private string _pendingId;
    internal string LastEventId { get; private set; }
    internal long? RetryMilliseconds { get; private set; }
    internal SseParser(Stream source, SseReadOptions options, string lastEventId, TimeSpan? idleTimeout = null)
    { _source = source; _options = options; _idleTimeout = idleTimeout; LastEventId = _pendingId = lastEventId; }
    internal async Task<ServerSentEvent> ReadNextAsync(CancellationToken token)
    {
        var data = new StringBuilder(); var type = string.Empty; var hasData = false;
        while (true)
        {
            var line = await ReadLineAsync(token).ConfigureAwait(false);
            if (line == null) return null;
            if (line.Length == 0)
            {
                LastEventId = _pendingId; _blockBytes = 0;
                if (hasData) return new ServerSentEvent { Data = data.ToString(0, data.Length - 1),
                    EventType = type.Length == 0 ? "message" : type, Id = LastEventId, RetryMilliseconds = RetryMilliseconds };
                type = string.Empty; continue;
            }
            var colon = line.IndexOf(':');
            var field = colon < 0 ? line : line.Substring(0, colon);
            var value = colon < 0 ? string.Empty : line.Substring(colon + 1);
            if (value.StartsWith(" ", StringComparison.Ordinal)) value = value.Substring(1);
            switch (field)
            {
                case "data": data.Append(value).Append('\n'); hasData = true; break;
                case "event": type = value; break;
                case "id": if (value.IndexOf('\0') < 0) _pendingId = value; break;
                case "retry":
                    if (value.Length > 0 && value.All(c => c >= '0' && c <= '9') && long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var retry)) RetryMilliseconds = retry;
                    break;
            }
        }
    }
    private async Task<string> ReadLineAsync(CancellationToken token)
    {
        using var line = new MemoryStream();
        while (true)
        {
            var value = await ReadByteAsync(token).ConfigureAwait(false);
            if (value < 0) return line.Length == 0 ? null : DecodeLine(line);
            if (_skipLf) { _skipLf = false; if (value == 10) continue; }
            if (value == 13 || value == 10)
            {
                _skipLf = value == 13;
                return DecodeLine(line);
            }
            if (++_blockBytes > _options.MaxEventBytes) throw new InvalidDataException("SSE event exceeds its byte budget.");
            line.WriteByte((byte)value);
        }
    }
    private string DecodeLine(MemoryStream line)
    {
        var text = Encoding.UTF8.GetString(line.GetBuffer(), 0, checked((int)line.Length));
        if (_first) { _first = false; if (text.StartsWith("\ufeff", StringComparison.Ordinal)) text = text.Substring(1); }
        return text;
    }
    private async ValueTask<int> ReadByteAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (_offset < _count) return _buffer[_offset++];
        var remaining = (_options.MaxInputBytes ?? long.MaxValue) - _read;
        var count = remaining >= _buffer.Length ? _buffer.Length : (int)remaining + 1;
        _count = await ReadSourceAsync(count, token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested(); _offset = 0; _read += _count;
        if (_options.MaxInputBytes.HasValue && _read > _options.MaxInputBytes) throw new InvalidDataException("SSE input exceeds its byte budget.");
        return _count == 0 ? -1 : _buffer[_offset++];
    }

    /// <summary>
    /// 等待新字节时设置超时，等待读取收尾后传播失败，不遗留后台读取。
    /// </summary>
    /// <returns>本次读取的字节数；零表示正文结束。</returns>
    private async ValueTask<int> ReadSourceAsync(int count, CancellationToken token)
    {
        if (!_idleTimeout.HasValue)
            return await _source.ReadAsync(_buffer, 0, count, token).ConfigureAwait(false);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(_idleTimeout.Value);
        try
        {
            var read = await _source.ReadAsync(_buffer, 0, count, timeout.Token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            if (timeout.IsCancellationRequested) throw new TimeoutException("SSE body idle timeout elapsed.");
            return read;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        { throw new OperationCanceledException(token); }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        { throw new TimeoutException("SSE body idle timeout elapsed."); }
    }
}
