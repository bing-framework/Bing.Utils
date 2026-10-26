using System.Runtime.CompilerServices;
using System.Text;

namespace Bing.Serialization.Csv;

/// <summary>
/// 以 UTF-8 逐条读取和写入 CSV/TSV 数据。
/// </summary>
/// <remarks>读取支持短读、不可定位流、RFC 风格引用和多行字段；所有操作从当前流位置开始。</remarks>
public static class CsvStream
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    /// <summary>
    /// 从当前位置逐条读取 CSV/TSV 记录。
    /// </summary>
    /// <param name="source">可读源流。</param>
    /// <param name="options">格式与资源预算；省略时使用逗号分隔和首行表头，MaxInputBytes 限制读取字节，MaxRows 限制数据行数。</param>
    /// <param name="leaveOpen">是否保留源流；枚举开始后提前结束、失败或取消时同样生效。</param>
    /// <param name="cancellationToken">取消读取和枚举。</param>
    /// <returns>逐条解析的记录。</returns>
    public static IAsyncEnumerable<CsvRow> ReadAsync(Stream source, CsvOptions options = null, bool leaveOpen = true,
        CancellationToken cancellationToken = default)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (!source.CanRead) throw new ArgumentException("源流不可读。", nameof(source));
        var config = (options ?? new CsvOptions()).Snapshot();
        cancellationToken.ThrowIfCancellationRequested();
        return ReadCoreAsync(source, config, leaveOpen, cancellationToken);
    }

    /// <summary>
    /// 将异步记录逐条写为 CSV/TSV，不缓存整个输入序列。
    /// </summary>
    /// <param name="destination">可写目标流。</param>
    /// <param name="source">待写入的记录序列。</param>
    /// <param name="options">格式与资源预算；省略时使用逗号分隔，MaxRows 限制写出的数据行数。</param>
    /// <param name="leaveOpen">是否保留目标流；参数检查和开始前取消不接管流。</param>
    /// <param name="cancellationToken">取消枚举和写入。</param>
    public static async Task WriteAsync(Stream destination, IAsyncEnumerable<CsvRow> source, CsvOptions options = null,
        bool leaveOpen = true, CancellationToken cancellationToken = default)
    {
        if (destination == null) throw new ArgumentNullException(nameof(destination));
        if (!destination.CanWrite) throw new ArgumentException("目标流不可写。", nameof(destination));
        if (source == null) throw new ArgumentNullException(nameof(source));
        var config = (options ?? new CsvOptions()).Snapshot();
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            string[] headers = config.Headers?.ToArray();
            var headerWritten = false;
            if (config.WriteHeader && headers != null)
            {
                await WriteRecordAsync(destination, headers, config, 1, cancellationToken).ConfigureAwait(false);
                headerWritten = true;
            }

            long rowNumber = 0;
            await foreach (var row in source.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (row == null) throw new ArgumentException("记录序列不能包含 null。", nameof(source));
                rowNumber++;

                if (headers == null && row.Headers != null)
                {
                    headers = row.Headers.ToArray();
                }
                else if (headers != null && row.Headers != null)
                {
                    EnsureSameHeaders(headers, row.Headers, rowNumber);
                }

                if (headers != null && headers.Length != row.Count)
                {
                    throw new CsvFormatException($"记录包含 {row.Count} 个字段，表头包含 {headers.Length} 个字段。",
                        rowNumber, Math.Min(row.Count, headers.Length) + 1);
                }

                if (config.WriteHeader && !headerWritten)
                {
                    if (headers == null)
                    {
                        throw new CsvFormatException("启用表头写入时，必须在选项或第一条记录中提供列名。", 1);
                    }

                    await WriteRecordAsync(destination, headers, config, 1, cancellationToken).ConfigureAwait(false);
                    headerWritten = true;
                }

                if (config.MaxRows.HasValue && rowNumber > config.MaxRows.Value)
                {
                    var outputRowNumber = rowNumber + (headerWritten ? 1 : 0);
                    throw new CsvFormatException($"数据行数超过上限 {config.MaxRows.Value}。",
                        outputRowNumber);
                }

                await WriteRecordAsync(destination, row.Values, config, rowNumber + (config.WriteHeader ? 1 : 0),
                    cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            if (!leaveOpen) destination.Dispose();
        }
    }

    private static async IAsyncEnumerable<CsvRow> ReadCoreAsync(Stream source, CsvOptions config, bool leaveOpen,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var reader = new CsvByteReader(source, config.MaxInputBytes);
            await reader.SkipUtf8BomAsync(cancellationToken).ConfigureAwait(false);

            string[] headers = config.Headers?.ToArray();
            long recordNumber = 1;
            long dataRowCount = 0;
            if (config.HasHeader)
            {
                var header = await ReadRawRecordAsync(reader, config, recordNumber, cancellationToken).ConfigureAwait(false);
                if (header == null) yield break;
                var actualHeaders = header.Values.ToArray();
                ValidateParsedHeaders(actualHeaders, recordNumber);
                if (headers != null) EnsureSameHeaders(headers, actualHeaders, recordNumber);
                else headers = actualHeaders;
                recordNumber++;
            }

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var row = await ReadRawRecordAsync(reader, config, recordNumber, cancellationToken).ConfigureAwait(false);
                if (row == null) yield break;
                if (headers != null && row.Count != headers.Length)
                {
                    throw new CsvFormatException($"记录包含 {row.Count} 个字段，表头包含 {headers.Length} 个字段。",
                        recordNumber, Math.Min(row.Count, headers.Length) + 1);
                }

                if (config.MaxRows.HasValue && dataRowCount >= config.MaxRows.Value)
                {
                    throw new CsvFormatException($"数据行数超过上限 {config.MaxRows.Value}。", recordNumber);
                }

                yield return new CsvRow(row.Values, headers, recordNumber);
                dataRowCount++;
                recordNumber++;
            }
        }
        finally
        {
            if (!leaveOpen) source.Dispose();
        }
    }

    private static async Task<CsvRow> ReadRawRecordAsync(CsvByteReader reader, CsvOptions config, long rowNumber,
        CancellationToken cancellationToken)
    {
        reader.SetRowNumber(rowNumber);
        var fields = new List<string>();
        var fieldBytes = new List<byte>();
        long rowByteCount = 0;
        long fieldByteCount = 0;
        var inQuotes = false;
        var afterQuote = false;
        var recordStarted = false;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var value = await reader.ReadByteAsync(cancellationToken).ConfigureAwait(false);
            if (value < 0)
            {
                if (!recordStarted && fields.Count == 0 && fieldByteCount == 0 && fieldBytes.Count == 0)
                {
                    return null;
                }

                if (inQuotes) throw Error("带引号字段未闭合。", rowNumber, fields.Count + 1);
                AddField(fields, fieldBytes, rowNumber, config);
                return new CsvRow(fields, rowNumber: rowNumber);
            }

            if (!inQuotes && (value == '\r' || value == '\n'))
            {
                if (value == '\r' && await reader.PeekByteAsync(0, cancellationToken).ConfigureAwait(false) == '\n')
                {
                    await reader.ReadByteAsync(cancellationToken).ConfigureAwait(false);
                }

                AddField(fields, fieldBytes, rowNumber, config);
                return new CsvRow(fields, rowNumber: rowNumber);
            }

            recordStarted = true;
            if (inQuotes)
            {
                if (value == '"')
                {
                    if (await reader.PeekByteAsync(0, cancellationToken).ConfigureAwait(false) == '"')
                    {
                        CountRawByte(ref rowByteCount, ref fieldByteCount, config, rowNumber, fields.Count + 1);
                        CountRawByte(ref rowByteCount, ref fieldByteCount, config, rowNumber, fields.Count + 1);
                        await reader.ReadByteAsync(cancellationToken).ConfigureAwait(false);
                        fieldBytes.Add((byte)'"');
                    }
                    else
                    {
                        CountRawByte(ref rowByteCount, ref fieldByteCount, config, rowNumber, fields.Count + 1);
                        inQuotes = false;
                        afterQuote = true;
                    }
                }
                else
                {
                    CountRawByte(ref rowByteCount, ref fieldByteCount, config, rowNumber, fields.Count + 1);
                    fieldBytes.Add((byte)value);
                }

                continue;
            }

            if (afterQuote)
            {
                if (value == config.Delimiter)
                {
                    CountRowByte(ref rowByteCount, config, rowNumber, fields.Count + 1);
                    AddField(fields, fieldBytes, rowNumber, config);
                    fieldByteCount = 0;
                    afterQuote = false;
                    EnsureNextFieldAllowed(fields, config, rowNumber);
                    continue;
                }

                throw Error("闭合引号后只能出现分隔符、记录结束符或输入结束。", rowNumber, fields.Count + 1);
            }

            if (value == config.Delimiter)
            {
                CountRowByte(ref rowByteCount, config, rowNumber, fields.Count + 1);
                AddField(fields, fieldBytes, rowNumber, config);
                fieldByteCount = 0;
                EnsureNextFieldAllowed(fields, config, rowNumber);
                continue;
            }

            if (fieldBytes.Count == 0 && fieldByteCount == 0 && value == '"')
            {
                CountRawByte(ref rowByteCount, ref fieldByteCount, config, rowNumber, fields.Count + 1);
                inQuotes = true;
                continue;
            }

            if (value == '"') throw Error("未引用字段中出现引号。", rowNumber, fields.Count + 1);
            CountRawByte(ref rowByteCount, ref fieldByteCount, config, rowNumber, fields.Count + 1);
            fieldBytes.Add((byte)value);
        }
    }

    private static void AddField(List<string> fields, List<byte> bytes, long rowNumber, CsvOptions config)
    {
        if (fields.Count >= config.MaxFieldsPerRow)
        {
            throw new CsvFormatException($"字段数量超过上限 {config.MaxFieldsPerRow}。", rowNumber,
                fields.Count + 1);
        }

        try
        {
            fields.Add(StrictUtf8.GetString(bytes.ToArray()));
        }
        catch (DecoderFallbackException exception)
        {
            throw new CsvFormatException("字段不是有效 UTF-8。", rowNumber, fields.Count + 1, innerException: exception);
        }

        bytes.Clear();
    }

    private static void EnsureNextFieldAllowed(List<string> fields, CsvOptions config, long rowNumber)
    {
        if (fields.Count >= config.MaxFieldsPerRow)
        {
            throw new CsvFormatException($"字段数量超过上限 {config.MaxFieldsPerRow}。", rowNumber,
                fields.Count + 1);
        }
    }

    private static void CountRawByte(ref long rowCount, ref long fieldCount, CsvOptions config, long rowNumber,
        int columnNumber)
    {
        CountRowByte(ref rowCount, config, rowNumber, columnNumber);
        if (fieldCount == long.MaxValue)
        {
            throw new CsvFormatException("字段字节计数溢出。", rowNumber, columnNumber);
        }

        fieldCount++;
        if (config.MaxFieldBytes.HasValue && fieldCount > config.MaxFieldBytes.Value)
        {
            throw new CsvFormatException($"字段超过 {config.MaxFieldBytes.Value} 字节上限。", rowNumber, columnNumber);
        }
    }

    private static void CountRowByte(ref long rowCount, CsvOptions config, long rowNumber, int columnNumber)
    {
        if (rowCount == long.MaxValue)
        {
            throw new CsvFormatException("记录字节计数溢出。", rowNumber, columnNumber);
        }

        rowCount++;
        if (config.MaxRowBytes.HasValue && rowCount > config.MaxRowBytes.Value)
        {
            throw new CsvFormatException($"记录超过 {config.MaxRowBytes.Value} 字节上限。", rowNumber, columnNumber);
        }
    }

    private static CsvFormatException Error(string message, long rowNumber, int columnNumber)
        => new(message, rowNumber, columnNumber);

    private static void ValidateParsedHeaders(string[] headers, long rowNumber)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < headers.Length; index++)
        {
            if (string.IsNullOrWhiteSpace(headers[index]))
            {
                throw new CsvFormatException("表头不能为空。", rowNumber, index + 1);
            }

            if (!seen.Add(headers[index]))
            {
                throw new CsvFormatException($"表头“{headers[index]}”重复。", rowNumber, index + 1, headers[index]);
            }
        }
    }

    private static void EnsureSameHeaders(IReadOnlyList<string> expected, IReadOnlyList<string> actual, long rowNumber)
    {
        if (expected.Count != actual.Count)
        {
            throw new CsvFormatException($"输入表头包含 {actual.Count} 列，配置要求 {expected.Count} 列。", rowNumber);
        }

        for (var index = 0; index < expected.Count; index++)
        {
            if (!string.Equals(expected[index], actual[index], StringComparison.OrdinalIgnoreCase))
            {
                throw new CsvFormatException($"输入表头“{actual[index]}”与配置列名“{expected[index]}”不匹配。",
                    rowNumber, index + 1, actual[index]);
            }
        }
    }

    private static async Task WriteRecordAsync(Stream destination, IReadOnlyList<string> fields, CsvOptions config,
        long rowNumber, CancellationToken cancellationToken)
    {
        if (fields.Count == 0)
        {
            throw new CsvFormatException("记录必须至少包含一个字段。", rowNumber, 1);
        }

        if (fields.Count > config.MaxFieldsPerRow)
        {
            throw new CsvFormatException($"字段数量超过上限 {config.MaxFieldsPerRow}。", rowNumber,
                config.MaxFieldsPerRow + 1);
        }

        using var record = new MemoryStream();
        long rowBytes = 0;
        for (var index = 0; index < fields.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (index > 0)
            {
                WriteByte(record, (byte)config.Delimiter, ref rowBytes, config.MaxRowBytes, rowNumber, index + 1);
            }

            var value = fields[index] ?? string.Empty;
            int encodedByteCount;
            try
            {
                encodedByteCount = StrictUtf8.GetByteCount(value);
            }
            catch (EncoderFallbackException exception)
            {
                throw new CsvFormatException("字段包含无效 UTF-16 字符。", rowNumber, index + 1, innerException: exception);
            }

            var quoteCount = value.Count(character => character == '"');
            var quoted = value.IndexOf(config.Delimiter) >= 0 || value.IndexOf('"') >= 0 || value.IndexOf('\r') >= 0 ||
                         value.IndexOf('\n') >= 0;
            var fieldByteCount = (long)encodedByteCount + (quoted ? 2L + quoteCount : 0L);
            if (config.MaxFieldBytes.HasValue && fieldByteCount > config.MaxFieldBytes.Value)
            {
                throw new CsvFormatException($"字段超过 {config.MaxFieldBytes.Value} 字节上限。", rowNumber, index + 1);
            }

            if (config.MaxRowBytes.HasValue && fieldByteCount > config.MaxRowBytes.Value - rowBytes)
            {
                throw new CsvFormatException($"记录超过 {config.MaxRowBytes.Value} 字节上限。", rowNumber, index + 1);
            }

            byte[] encoded;
            try
            {
                encoded = StrictUtf8.GetBytes(value);
            }
            catch (EncoderFallbackException exception)
            {
                throw new CsvFormatException("字段包含无效 UTF-16 字符。", rowNumber, index + 1, innerException: exception);
            }

            if (quoted) WriteByte(record, (byte)'"', ref rowBytes, config.MaxRowBytes, rowNumber, index + 1);
            for (var byteIndex = 0; byteIndex < encoded.Length; byteIndex++)
            {
                if (quoted && encoded[byteIndex] == '"')
                {
                    WriteByte(record, (byte)'"', ref rowBytes, config.MaxRowBytes, rowNumber, index + 1);
                    WriteByte(record, (byte)'"', ref rowBytes, config.MaxRowBytes, rowNumber, index + 1);
                }
                else
                {
                    WriteByte(record, encoded[byteIndex], ref rowBytes, config.MaxRowBytes, rowNumber, index + 1);
                }
            }

            if (quoted) WriteByte(record, (byte)'"', ref rowBytes, config.MaxRowBytes, rowNumber, index + 1);
        }

        var data = record.GetBuffer();
        var length = checked((int)record.Length);
        await destination.WriteAsync(data, 0, length, cancellationToken).ConfigureAwait(false);
        var newline = config.NewLine == "\n" ? new byte[] { 10 } : new byte[] { 13, 10 };
        await destination.WriteAsync(newline, 0, newline.Length, cancellationToken).ConfigureAwait(false);
    }

    private static void WriteByte(MemoryStream target, byte value, ref long count, long? maximum, long rowNumber,
        int columnNumber)
    {
        if (count == long.MaxValue || maximum.HasValue && count >= maximum.Value)
        {
            throw new CsvFormatException(maximum.HasValue ? $"记录超过 {maximum.Value} 字节上限。" : "记录字节计数溢出。",
                rowNumber, columnNumber);
        }

        count++;
        target.WriteByte(value);
    }

    private sealed class CsvByteReader
    {
        private readonly Stream _source;
        /// <summary>
        /// 读取时允许消耗的总字节上限；空值表示不限制。
        /// </summary>
        private readonly long? _maxInputBytes;
        private readonly byte[] _buffer = new byte[8192];
        private int _offset;
        private int _count;
        private bool _eof;
        /// <summary>
        /// 从底层流读取的字节数，包含已预读但尚未解析的字节。
        /// </summary>
        private long _bytesFetched;
        /// <summary>
        /// 当前解析记录编号，用于生成带行号的输入预算错误。
        /// </summary>
        private long _rowNumber = 1;

        /// <summary>
        /// 创建带总输入字节预算的缓冲读取器。
        /// </summary>
        /// <param name="source">从其当前位置读取的输入流。</param>
        /// <param name="maxInputBytes">总输入字节上限；空值表示不限制。</param>
        internal CsvByteReader(Stream source, long? maxInputBytes)
        {
            _source = source;
            _maxInputBytes = maxInputBytes;
        }

        /// <summary>
        /// 设置后续解析错误使用的记录编号。
        /// </summary>
        /// <param name="rowNumber">当前逻辑记录编号。</param>
        internal void SetRowNumber(long rowNumber) => _rowNumber = rowNumber;

        internal async Task SkipUtf8BomAsync(CancellationToken cancellationToken)
        {
            if (await PeekByteAsync(0, cancellationToken).ConfigureAwait(false) != 0xef) return;
            if (await PeekByteAsync(1, cancellationToken).ConfigureAwait(false) == 0xbb &&
                await PeekByteAsync(2, cancellationToken).ConfigureAwait(false) == 0xbf)
            {
                await ReadByteAsync(cancellationToken).ConfigureAwait(false);
                await ReadByteAsync(cancellationToken).ConfigureAwait(false);
                await ReadByteAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 查看缓冲区中指定偏移处的字节，必要时异步填充缓冲区。
        /// </summary>
        /// <param name="ahead">相对于当前读取位置的零基偏移。</param>
        /// <param name="cancellationToken">用于取消底层流读取的令牌。</param>
        /// <returns>指定位置的字节；到达输入末尾时返回 -1。</returns>
        internal ValueTask<int> PeekByteAsync(int ahead, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (ahead < 0) throw new ArgumentOutOfRangeException(nameof(ahead));
            if (_count - _offset > ahead) return new ValueTask<int>(_buffer[_offset + ahead]);
            if (_eof) return new ValueTask<int>(-1);
            return FillAndPeekAsync(ahead, cancellationToken);
        }

        /// <summary>
        /// 异步填充固定缓冲区，直到可查看指定偏移或到达输入末尾。
        /// </summary>
        /// <param name="ahead">相对于当前读取位置的零基偏移。</param>
        /// <param name="cancellationToken">用于取消底层流读取的令牌。</param>
        /// <returns>指定位置的字节；到达输入末尾时返回 -1。</returns>
        private async ValueTask<int> FillAndPeekAsync(int ahead, CancellationToken cancellationToken)
        {
            while (_count - _offset <= ahead && !_eof)
            {
                var available = _count - _offset;
                if (_offset > 0 && available > 0) Array.Copy(_buffer, _offset, _buffer, 0, available);
                _offset = 0;
                _count = available;
                if (_count == _buffer.Length)
                {
                    throw new InvalidOperationException("CSV 预读距离超过固定缓冲区。" );
                }

                var readCapacity = _buffer.Length - _count;
                if (_maxInputBytes.HasValue)
                {
                    var remaining = _maxInputBytes.Value - _bytesFetched;
                    if (remaining < readCapacity)
                    {
                        readCapacity = (int)remaining + 1;
                    }
                }

                var read = await _source.ReadAsync(_buffer, _count, readCapacity, cancellationToken)
                    .ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                if (read == 0) _eof = true;
                else
                {
                    if (_maxInputBytes.HasValue && read > _maxInputBytes.Value - _bytesFetched)
                    {
                        throw new CsvFormatException($"输入字节数超过上限 {_maxInputBytes.Value}。", _rowNumber);
                    }

                    if (read > long.MaxValue - _bytesFetched)
                    {
                        throw new CsvFormatException("输入字节计数溢出。", _rowNumber);
                    }

                    _bytesFetched += read;
                    _count += read;
                }
            }

            return _count - _offset <= ahead ? -1 : _buffer[_offset + ahead];
        }

        /// <summary>
        /// 读取下一个字节；缓冲区已有数据时同步完成，不额外分配任务。
        /// </summary>
        /// <param name="cancellationToken">用于取消底层流读取的令牌。</param>
        /// <returns>读取的字节；到达输入末尾时返回 -1。</returns>
        internal ValueTask<int> ReadByteAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_offset < _count) return new ValueTask<int>(_buffer[_offset++]);
            if (_eof) return new ValueTask<int>(-1);
            return FillAndReadAsync(cancellationToken);
        }

        /// <summary>
        /// 在缓冲区耗尽时填充并读取下一个字节。
        /// </summary>
        /// <param name="cancellationToken">用于取消底层流读取的令牌。</param>
        /// <returns>读取的字节；到达输入末尾时返回 -1。</returns>
        private async ValueTask<int> FillAndReadAsync(CancellationToken cancellationToken)
        {
            var value = await PeekByteAsync(0, cancellationToken).ConfigureAwait(false);
            if (value >= 0) _offset++;
            return value;
        }
    }
}
