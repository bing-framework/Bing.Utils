using System.Text.Json;

namespace Bing.Serialization.SystemTextJson;

/// <summary>
/// 配置 NDJSON 每条记录的预算和刷新策略。
/// </summary>
public sealed class NdjsonOptions
{
    /// <summary>
    /// 获取或设置每条记录 UTF-8 字节上限；不包含 CRLF/LF 和文件开头 BOM，null 不限制。
    /// </summary>
    public long? MaxRecordBytes { get; set; }
    /// <summary>
    /// 获取或设置每多少条记录刷新输出；零表示仅结束时刷新。
    /// </summary>
    public int FlushEveryRecords { get; set; }
    /// <summary>
    /// 获取或设置是否忽略空行，默认拒绝空行。
    /// </summary>
    public bool IgnoreEmptyLines { get; set; }

    /// <summary>
    /// 固定每次操作的记录配置。
    /// </summary>
    internal NdjsonOptions Snapshot()
    {
        if (MaxRecordBytes < 0) throw new ArgumentOutOfRangeException(nameof(MaxRecordBytes));
        if (FlushEveryRecords < 0) throw new ArgumentOutOfRangeException(nameof(FlushEveryRecords));
        return (NdjsonOptions)MemberwiseClone();
    }
}

/// <summary>
/// 增量读写 UTF-8 JSON 数组和 NDJSON 记录。
/// </summary>
/// <remarks>从当前位置读写，默认保留流；失败可有部分输出。单条记录预算不是序列化器内部堆内存预算。</remarks>
public static partial class JsonStream
{
    /// <summary>
    /// 将异步序列逐项写为 JSON 数组，不收集整个序列。
    /// </summary>
    /// <typeparam name="T">元素类型。</typeparam>
    /// <param name="destination">可写目标。</param>
    /// <param name="source">异步源。</param>
    /// <param name="options">System.Text.Json 选项，null 使用默认语义。</param>
    /// <param name="flushEveryItems">按条目刷新间隔，零使用约 80 KiB 缓冲边界及结束刷新。</param>
    /// <param name="leaveOpen">是否保留目标，参数失败或开始前取消不接管。</param>
    /// <param name="cancellationToken">取消枚举和写入。</param>
    public static async Task WriteArrayAsync<T>(Stream destination, IAsyncEnumerable<T> source, JsonSerializerOptions options = null,
        int flushEveryItems = 0, bool leaveOpen = true, CancellationToken cancellationToken = default)
    {
        ValidateWritable(destination);
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (flushEveryItems < 0) throw new ArgumentOutOfRangeException(nameof(flushEveryItems));
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            using var writer = new Utf8JsonWriter(destination, new JsonWriterOptions { Indented = options?.WriteIndented ?? false, Encoder = options?.Encoder });
            writer.WriteStartArray();
            var count = 0;
            await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                JsonSerializer.Serialize(writer, item, options);
                count++;
                if (writer.BytesPending >= 81920 || flushEveryItems > 0 && count >= flushEveryItems)
                {
                    await writer.FlushAsync(cancellationToken).ConfigureAwait(false); count = 0;
                }
            }
            cancellationToken.ThrowIfCancellationRequested();
            writer.WriteEndArray();
            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        finally { if (!leaveOpen) destination.Dispose(); }
    }

    /// <summary>
    /// 逐条写出 NDJSON；超限记录不会写入目标，取消仍可能留下部分当前记录。
    /// </summary>
    /// <typeparam name="T">记录类型。</typeparam>
    /// <param name="destination">可写目标。</param>
    /// <param name="source">异步记录源。</param>
    /// <param name="recordOptions">记录预算和刷新间隔。</param>
    /// <param name="jsonOptions">JSON 选项；不能开启缩进。</param>
    /// <param name="leaveOpen">是否保留目标。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public static async Task WriteNdjsonAsync<T>(Stream destination, IAsyncEnumerable<T> source, NdjsonOptions recordOptions = null,
        JsonSerializerOptions jsonOptions = null, bool leaveOpen = true, CancellationToken cancellationToken = default)
    {
        ValidateWritable(destination);
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (jsonOptions?.WriteIndented == true) throw new ArgumentException("NDJSON 不支持多行缩进。", nameof(jsonOptions));
        var config = (recordOptions ?? new NdjsonOptions()).Snapshot();
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var count = 0; var newline = new byte[] { 10 };
            await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var record = new RecordBuffer(config.MaxRecordBytes);
                await JsonSerializer.SerializeAsync(record, item, jsonOptions, cancellationToken).ConfigureAwait(false);
                await destination.WriteAsync(record.GetBuffer(), 0, checked((int)record.Length), cancellationToken).ConfigureAwait(false);
                await destination.WriteAsync(newline, 0, 1, cancellationToken).ConfigureAwait(false);
                if (config.FlushEveryRecords > 0 && ++count >= config.FlushEveryRecords)
                { await destination.FlushAsync(cancellationToken).ConfigureAwait(false); count = 0; }
            }
            await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        finally { if (!leaveOpen) destination.Dispose(); }
    }

    /// <summary>
    /// 从 UTF-8 NDJSON 逐行读取记录，支持短读、不可定位源和末尾无换行。
    /// </summary>
    /// <typeparam name="T">记录类型。</typeparam>
    /// <param name="source">可读源。</param>
    /// <param name="recordOptions">单记录字节预算和空行规则。</param>
    /// <param name="jsonOptions">JSON 选项。</param>
    /// <param name="leaveOpen">是否保留源，提前结束枚举也遵守此配置。</param>
    /// <param name="cancellationToken">取消读取和枚举。</param>
    /// <returns>逐条反序列化的记录。</returns>
    public static IAsyncEnumerable<T> ReadNdjsonAsync<T>(Stream source, NdjsonOptions recordOptions = null,
        JsonSerializerOptions jsonOptions = null, bool leaveOpen = true, CancellationToken cancellationToken = default)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (!source.CanRead) throw new ArgumentException("源不可读。", nameof(source));
        var config = (recordOptions ?? new NdjsonOptions()).Snapshot();
        return ReadRecords<T>(source, config, jsonOptions, leaveOpen, cancellationToken);
    }

    /// <summary>
    /// 按字节识别行边界，单条记录内存随记录预算而非输入总长增长。
    /// </summary>
    private static async IAsyncEnumerable<T> ReadRecords<T>(Stream source, NdjsonOptions config, JsonSerializerOptions options,
        bool leaveOpen, [EnumeratorCancellation] CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        try
        {
            var bytes = new byte[8192];
            using var line = new MemoryStream();
            long lineNumber = 1;
            while (true)
            {
                var count = await source.ReadAsync(bytes, 0, bytes.Length, token).ConfigureAwait(false);
                if (count == 0) break;
                for (var i = 0; i < count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    if (bytes[i] == 10)
                    {
                        var record = ReadRecord<T>(line, lineNumber, config, options);
                        line.SetLength(0); line.Position = 0; lineNumber++;
                        if (record.HasValue) yield return record.Value;
                    }
                    else
                    {
                        line.WriteByte(bytes[i]);
                        // 开头三个字节可能组成 BOM，末尾 CR 可能是 CRLF 的一部分。
                        var length = line.Length;
                        if (lineNumber == 1 && length < 3) continue;
                        var offset = HasBom(line, lineNumber) ? 3 : 0;
                        CheckRecordLimit(length - offset - (bytes[i] == 13 ? 1 : 0), config.MaxRecordBytes, lineNumber);
                    }
                }
            }
            token.ThrowIfCancellationRequested();
            if (line.Length > 0)
            {
                var record = ReadRecord<T>(line, lineNumber, config, options);
                if (record.HasValue) yield return record.Value;
            }
        }
        finally { if (!leaveOpen) source.Dispose(); }
    }

    /// <summary>
    /// 校验行长度并在异常中保留记录位置。
    /// </summary>
    private static (bool HasValue, T Value) ReadRecord<T>(MemoryStream line, long number, NdjsonOptions config, JsonSerializerOptions options)
    {
        var data = line.GetBuffer();
        var offset = HasBom(line, number) ? 3 : 0;
        var length = checked((int)line.Length);
        if (length > offset && data[length - 1] == 13) length--;
        length -= offset;
        CheckRecordLimit(length, config.MaxRecordBytes, number);
        if (length == 0)
        {
            if (config.IgnoreEmptyLines) return (false, default);
            throw new JsonException($"NDJSON 第 {number} 行为空。");
        }
        try { return (true, JsonSerializer.Deserialize<T>(new ReadOnlySpan<byte>(data, offset, length), options)); }
        catch (JsonException ex) { throw new JsonException($"NDJSON 第 {number} 行无效：{ex.Message}", ex); }
    }

    /// <summary>
    /// 只识别文件开头的 UTF-8 BOM。
    /// </summary>
    private static bool HasBom(MemoryStream line, long number)
    {
        if (number != 1 || line.Length < 3) return false;
        var data = line.GetBuffer(); return data[0] == 239 && data[1] == 187 && data[2] == 191;
    }

    /// <summary>
    /// 在添加超限字节到目标之前拒绝记录。
    /// </summary>
    private static void CheckRecordLimit(long length, long? maximum, long line)
    {
        if (maximum.HasValue && length > maximum.Value) throw new InvalidDataException($"NDJSON 第 {line} 条记录超过 {maximum} 字节限额。");
    }

    /// <summary>
    /// 校验输出能力。
    /// </summary>
    private static void ValidateWritable(Stream stream)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        if (!stream.CanWrite) throw new ArgumentException("目标不可写。", nameof(stream));
    }

    /// <summary>
    /// 在序列化写入时限制单条记录的缓存大小。
    /// </summary>
    private sealed class RecordBuffer : MemoryStream
    {
        private readonly long? _maximum;
        internal RecordBuffer(long? maximum) => _maximum = maximum;
        public override void Write(byte[] buffer, int offset, int count)
        {
            CheckRecordLimit(Position + count, _maximum, 0); base.Write(buffer, offset, count);
        }
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested(); Write(buffer, offset, count); return Task.CompletedTask;
        }
#if NET6_0_OR_GREATER
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            CheckRecordLimit(Position + buffer.Length, _maximum, 0); base.Write(buffer);
        }
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested(); Write(buffer.Span); return default;
        }
#endif
    }
}
