using System.Text.Json;

namespace Bing.Serialization.SystemTextJson;

/// <summary>
/// JSON 数组流读取的原始 UTF-8 字节预算。
/// </summary>
public sealed class JsonArrayReadOptions
{
    /// <summary>
    /// 获取或设置单个值的字节上限，不含前后空白及数组分隔符；null 不限制。
    /// </summary>
    public long? MaxItemBytes { get; set; }
    /// <summary>
    /// 获取或设置总输入字节上限，包含 BOM、空白及标点；null 不限制。
    /// </summary>
    public long? MaxInputBytes { get; set; }
    /// <summary>
    /// 固定操作配置并验证预算。
    /// </summary>
    internal JsonArrayReadOptions Snapshot()
    {
        if (MaxItemBytes < 0) throw new ArgumentOutOfRangeException(nameof(MaxItemBytes));
        if (MaxInputBytes < 0) throw new ArgumentOutOfRangeException(nameof(MaxInputBytes));
        return (JsonArrayReadOptions)MemberwiseClone();
    }
}

public static partial class JsonStream
{
    /// <summary>
    /// 从当前位置逐项读取 UTF-8 JSON 数组，支持短读、不可定位流及开头 BOM。
    /// </summary>
    /// <remarks>只缓存一个值及固定预读缓冲；预算不限制反序列化后对象大小。
    /// 提前结束不会校验剩余数组；预读可推进源超过最后交付元素。不支持注释；可通过 JSON 选项允许尾逗号。
    /// 从首次枚举开始接管资源，开始前取消不关闭流；提前结束、错误及执行中取消均遵守 leaveOpen。</remarks>
    /// <returns>按输入顺序交付的元素序列；错误包含从一开始的元素编号及从零开始的字节位置。</returns>
    public static IAsyncEnumerable<T> ReadArrayAsync<T>(Stream source, JsonArrayReadOptions readOptions = null,
        JsonSerializerOptions jsonOptions = null, bool leaveOpen = true, CancellationToken cancellationToken = default)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (!source.CanRead) throw new ArgumentException("源不可读。", nameof(source));
        if (jsonOptions != null && jsonOptions.ReadCommentHandling != JsonCommentHandling.Disallow)
            throw new ArgumentException("数组流读取不支持 JSON 注释。", nameof(jsonOptions));
        var config = (readOptions ?? new JsonArrayReadOptions()).Snapshot();
        return ReadArrayCore<T>(source, config, jsonOptions, leaveOpen, cancellationToken);
    }

    /// <summary>
    /// 按单值边界切分数组，结束时校验尾部。
    /// </summary>
    private static async IAsyncEnumerable<T> ReadArrayCore<T>(Stream source, JsonArrayReadOptions config,
        JsonSerializerOptions options, bool leaveOpen, [EnumeratorCancellation] CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        try
        {
            var reader = new ArrayByteReader(source, config);
            if (await reader.PeekAsync(token).ConfigureAwait(false) == 239)
            {
                reader.Consume();
                if (await reader.TakeAsync(token).ConfigureAwait(false) != 187 || await reader.TakeAsync(token).ConfigureAwait(false) != 191)
                    throw reader.Error("UTF-8 BOM 无效");
            }
            if (await reader.NonWhitespaceAsync(token).ConfigureAwait(false) != '[') throw reader.Error("输入必须是 JSON 数组");
            reader.Consume();
            var afterComma = false;
            while (true)
            {
                var first = await reader.NonWhitespaceAsync(token).ConfigureAwait(false);
                if (first == ']')
                {
                    if (afterComma && !(options?.AllowTrailingCommas ?? false)) throw reader.Error("数组尾逗号无效");
                    reader.Consume(); break;
                }
                if (first < 0 || first == ',') throw reader.Error("缺少数组元素");
                var value = await reader.ReadValueAsync<T>(options, token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested(); yield return value;
                reader.ItemIndex++;
                var separator = await reader.NonWhitespaceAsync(token).ConfigureAwait(false);
                if (separator == ']') { reader.Consume(); break; }
                if (separator != ',') throw reader.Error("缺少数组分隔符");
                reader.Consume(); afterComma = true;
            }
            if (await reader.NonWhitespaceAsync(token).ConfigureAwait(false) != -1) throw reader.Error("数组之后存在额外内容");
        }
        finally { if (!leaveOpen) source.Dispose(); }
    }

    /// <summary>
    /// 固定预读缓冲及逐值字节预算，不依赖源的 Length 或 Position。
    /// </summary>
    private sealed class ArrayByteReader
    {
        private readonly Stream _source;
        private readonly JsonArrayReadOptions _limits;
        private readonly byte[] _buffer = new byte[8192];
        private int _offset, _count;
        private long _read, _position;
        internal long ItemIndex = 1;
        internal ArrayByteReader(Stream source, JsonArrayReadOptions limits) { _source = source; _limits = limits; }
        internal void Consume() { _offset++; _position++; }
        internal JsonException Error(string text) => new($"JSON 数组第 {ItemIndex} 项，字节 {_position}：{text}。");
        internal async ValueTask<int> PeekAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (_offset < _count) return _buffer[_offset];
            var remaining = (_limits.MaxInputBytes ?? long.MaxValue) - _read;
            var count = remaining >= _buffer.Length ? _buffer.Length : checked((int)remaining + 1);
            _count = await _source.ReadAsync(_buffer, 0, count, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested(); _offset = 0; _read = checked(_read + _count);
            if (_limits.MaxInputBytes.HasValue && _read > _limits.MaxInputBytes.Value)
                throw new InvalidDataException($"JSON 数组第 {ItemIndex} 项，字节 {_position}：输入超过字节限额。");
            return _count == 0 ? -1 : _buffer[0];
        }
        internal async ValueTask<int> TakeAsync(CancellationToken token)
        { var value = await PeekAsync(token).ConfigureAwait(false); if (value >= 0) Consume(); return value; }
        internal async ValueTask<int> NonWhitespaceAsync(CancellationToken token)
        {
            var value = await PeekAsync(token).ConfigureAwait(false);
            while (IsWhitespace(value)) { Consume(); value = await PeekAsync(token).ConfigureAwait(false); }
            return value;
        }
        private static bool IsWhitespace(int value) => value == 32 || value == 9 || value == 10 || value == 13;
        internal async Task<T> ReadValueAsync<T>(JsonSerializerOptions options, CancellationToken token)
        {
            var first = await PeekAsync(token).ConfigureAwait(false);
            var compound = first == '{' || first == '[';
            var quoted = first == '"';
            var inString = false; var escaped = false; var depth = 0;
            var maximumDepth = options?.MaxDepth > 0 ? options.MaxDepth : 64;
            using var valueBytes = new MemoryStream();
            while (true)
            {
                var next = await PeekAsync(token).ConfigureAwait(false);
                if (!compound && !quoted && (next < 0 || next == ',' || next == ']' || IsWhitespace(next))) break;
                if (next < 0) throw Error("元素未结束");
                if (_limits.MaxItemBytes.HasValue && valueBytes.Length >= _limits.MaxItemBytes.Value)
                    throw new InvalidDataException($"JSON 数组第 {ItemIndex} 项，字节 {_position}：元素超过字节限额。");
                Consume(); valueBytes.WriteByte((byte)next);
                if (inString)
                {
                    if (escaped) escaped = false;
                    else if (next == '\\') escaped = true;
                    else if (next == '"') { inString = false; if (quoted) break; }
                }
                else if (next == '"') inString = true;
                else if (next == '[' || next == '{') { if (++depth > maximumDepth) throw Error("元素超过深度限制"); }
                else if (next == ']' || next == '}') { if (--depth == 0) break; }
            }
            try { return DeserializeValue<T>(valueBytes, options); }
            catch (JsonException ex) { throw new JsonException(Error(ex.Message).Message, ex); }
        }
        private static T DeserializeValue<T>(MemoryStream value, JsonSerializerOptions options)
            => JsonSerializer.Deserialize<T>(new ReadOnlySpan<byte>(value.GetBuffer(), 0, checked((int)value.Length)), options);
    }
}
