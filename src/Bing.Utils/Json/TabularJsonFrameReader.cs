using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bing.Utils.Json;

/// <summary>
/// 在固定输入缓冲上切分 v2 结构和行值，先检查原始字节预算再解析 JSON。
/// </summary>
internal sealed class TabularJsonFrameReader
{
    /// <summary>
    /// 保存调用方输入流。
    /// </summary>
    private readonly Stream _source;
    /// <summary>
    /// 保存已固定的读取限制。
    /// </summary>
    private readonly TabularJsonOptions _options;
    /// <summary>
    /// 保存固定预读缓冲。
    /// </summary>
    private readonly byte[] _buffer = new byte[4096];
    /// <summary>
    /// 保存当前缓冲位置。
    /// </summary>
    private int _offset;
    /// <summary>
    /// 保存当前缓冲有效长度。
    /// </summary>
    private int _length;
    /// <summary>
    /// 保存实际从底层读取的字节总数。
    /// </summary>
    private long _total;

    /// <summary>
    /// 创建读取状态；资源归属由外层读取器控制。
    /// </summary>
    internal TabularJsonFrameReader(Stream source, TabularJsonOptions options) { _source = source; _options = options; }

    /// <summary>
    /// 查看下一个字节；短读只补充缓冲，不视为文档结束。
    /// </summary>
    internal async ValueTask<int> PeekAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (_offset < _length) return _buffer[_offset];
        var remaining = (_options.MaxInputBytes ?? long.MaxValue) - _total;
        var count = remaining >= _buffer.Length ? _buffer.Length : (int)remaining + 1;
        _length = await _source.ReadAsync(_buffer, 0, count, token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        _offset = 0; _total += _length;
        if (_options.MaxInputBytes.HasValue && _total > _options.MaxInputBytes.Value)
            throw new JsonSerializationException("表格输入超过字节限额。");
        return _length == 0 ? -1 : _buffer[0];
    }

    /// <summary>
    /// 消费下一个字节。
    /// </summary>
    private async ValueTask<int> TakeAsync(CancellationToken token)
    {
        var value = await PeekAsync(token).ConfigureAwait(false);
        if (value >= 0) _offset++;
        return value;
    }

    /// <summary>
    /// 跳过 JSON 空白并查看下一字节。
    /// </summary>
    internal async ValueTask<int> NextAsync(CancellationToken token)
    {
        var value = await PeekAsync(token).ConfigureAwait(false);
        while (value == 32 || value == 9 || value == 10 || value == 13)
        {
            _offset++; value = await PeekAsync(token).ConfigureAwait(false);
        }
        return value;
    }

    /// <summary>
    /// 验证并消费固定标点。
    /// </summary>
    internal async Task ExpectAsync(char expected, CancellationToken token)
    {
        if (await NextAsync(token).ConfigureAwait(false) != expected) throw new JsonSerializationException($"v2 文档应包含 '{expected}'。");
        _offset++;
    }

    /// <summary>
    /// 验证固定属性名，v2 要求结构先于行数据。
    /// </summary>
    internal async Task PropertyAsync(string name, CancellationToken token)
    {
        var value = await ValueAsync(256, token).ConfigureAwait(false);
        if (value.Type != JTokenType.String || (string)value != name) throw new JsonSerializationException($"v2 文档应包含属性 {name}。");
        await ExpectAsync(':', token).ConfigureAwait(false);
    }

    /// <summary>
    /// 读取文档起点，允许 UTF-8 BOM。
    /// </summary>
    internal async Task SkipBomAsync(CancellationToken token)
    {
        if (await PeekAsync(token).ConfigureAwait(false) == 239)
        {
            await TakeAsync(token).ConfigureAwait(false);
            if (await TakeAsync(token).ConfigureAwait(false) != 187 || await TakeAsync(token).ConfigureAwait(false) != 191)
                throw new JsonSerializationException("UTF-8 BOM 无效。");
        }
    }

    /// <summary>
    /// 切分单个值；只保留该记录，拒绝超预算或超过深度的输入。
    /// </summary>
    internal async Task<JToken> ValueAsync(long? maximum, CancellationToken token)
    {
        var first = await NextAsync(token).ConfigureAwait(false);
        if (first < 0) throw new JsonSerializationException("表格 JSON 意外结束。");
        using var bytes = new MemoryStream();
        var compound = first == '{' || first == '[';
        var quoted = first == '"';
        var inString = false; var escaped = false; var depth = 0;
        while (true)
        {
            var value = await PeekAsync(token).ConfigureAwait(false);
            if (!compound && !quoted && (value < 0 || value == ',' || value == ']' || value == '}' || value == 32 || value == 9 || value == 10 || value == 13)) break;
            if (value < 0) throw new JsonSerializationException("表格 JSON 记录未结束。");
            if (maximum.HasValue && bytes.Length >= maximum.Value) throw new JsonSerializationException("表格 JSON 记录超过字节限额。");
            _offset++; bytes.WriteByte((byte)value);
            if (inString)
            {
                if (escaped) escaped = false;
                else if (value == '\\') escaped = true;
                else if (value == '"') { inString = false; if (quoted) break; }
            }
            else if (value == '"') inString = true;
            else if (value == '{' || value == '[')
            {
                if (++depth > _options.MaxDepth) throw new JsonSerializationException("表格 JSON 超过深度限额。");
            }
            else if (value == '}' || value == ']')
            {
                if (--depth == 0) break;
            }
        }
        var text = new UTF8Encoding(false, true).GetString(bytes.GetBuffer(), 0, checked((int)bytes.Length));
        using var input = new StringReader(text);
        using var reader = new JsonTextReader(input) { DateParseHandling = DateParseHandling.None, MaxDepth = _options.MaxDepth };
        var result = JToken.ReadFrom(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error, CommentHandling = CommentHandling.Load });
        if (result.Type == JTokenType.Comment || result is JContainer container && container.Descendants().Any(t => t.Type == JTokenType.Comment)
            || reader.Read()) throw new JsonSerializationException("表格 JSON 记录包含额外内容。");
        return result;
    }
}
