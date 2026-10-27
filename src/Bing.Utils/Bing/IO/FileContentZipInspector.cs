using System.IO.Compression;
using System.Text;
using System.Xml;

namespace Bing.IO;

/// <summary>只读取 ZIP32 目录、OPC 元数据及主部件本地头的有界检测器。</summary>
internal static class FileContentZipInspector
{
    /// <summary>无需复制完整 Span；仅为目录与识别元数据分配受预算约束的缓冲。</summary>
    internal static FileDetectionResult Inspect(ReadOnlySpan<byte> content, FileDetectionResult header,
        FileDetectionOptions options)
    {
        var session = new Inspection(header, options, content.Length, default);
        try
        {
            var directory = FindDirectory(content, session);
            var entries = directory.Entries;
            var metadata = session.SelectMetadata(entries);
            if (metadata.Length == 0) return session.Success(header.Format);
            var documents = new byte[2][];
            for (var i = 0; i < metadata.Length; i++)
            {
                var entry = metadata[i];
                var local = ReadSpan(content, entry.Offset, 30, session);
                var nameLength = session.ValidateLocalHeader(local, entry, directory.Offset);
                var name = ReadSpan(content, entry.Offset + 30, nameLength, session);
                session.ValidateName(name, entry);
                var offset = entry.Offset + 30 + nameLength + U16(local, 28);
                documents[i] = session.Inflate(ReadSpan(content, offset, entry.CompressedSize, session), entry);
            }
            var format = FileContentOfficePackage.Identify(documents[0], documents[1], entries.Select(x => x.Name), options.MaxMetadataBytes, out var mainPart);
            if (mainPart != null)
            {
                var entry = FindMainEntry(entries, mainPart);
                var local = ReadSpan(content, entry.Offset, 30, session);
                var nameLength = session.ValidateLocalHeader(local, entry, directory.Offset, false);
                session.ValidateName(ReadSpan(content, entry.Offset + 30, nameLength, session), entry);
            }
            return session.Success(format);
        }
        catch (ContainerException ex) { return session.Incomplete(ex.Reason, ex.LimitKind); }
        catch (XmlException) { return session.Incomplete(FileDetectionReason.InvalidContainer); }
    }

    /// <summary>所有源读取遵守同步/异步入口；解压只作用于已读入内存的有限元数据。</summary>
    internal static async Task<FileDetectionResult> InspectAsync(Stream source, long origin, FileDetectionResult header,
        FileDetectionOptions options, CancellationToken token, bool asynchronous)
    {
        var session = new Inspection(header, options, source.Length - origin, token);
        try
        {
            var directory = await FindDirectoryAsync(source, origin, session, asynchronous).ConfigureAwait(false);
            var entries = directory.Entries;
            var metadata = session.SelectMetadata(entries);
            if (metadata.Length == 0) return session.Success(header.Format);
            var documents = new byte[2][];
            for (var i = 0; i < metadata.Length; i++)
            {
                var entry = metadata[i];
                var local = await ReadAt(source, origin, entry.Offset, 30, session, asynchronous).ConfigureAwait(false);
                var nameLength = session.ValidateLocalHeader(local, entry, directory.Offset);
                var name = await ReadAt(source, origin, entry.Offset + 30, nameLength, session, asynchronous).ConfigureAwait(false);
                session.ValidateName(name, entry);
                var offset = entry.Offset + 30 + nameLength + U16(local, 28);
                var compressed = await ReadAt(source, origin, offset, entry.CompressedSize, session, asynchronous).ConfigureAwait(false);
                documents[i] = session.Inflate(compressed, entry);
            }
            token.ThrowIfCancellationRequested();
            var format = FileContentOfficePackage.Identify(documents[0], documents[1], entries.Select(x => x.Name), options.MaxMetadataBytes, out var mainPart);
            if (mainPart != null)
            {
                var entry = FindMainEntry(entries, mainPart);
                var local = await ReadAt(source, origin, entry.Offset, 30, session, asynchronous).ConfigureAwait(false);
                var nameLength = session.ValidateLocalHeader(local, entry, directory.Offset, false);
                var name = await ReadAt(source, origin, entry.Offset + 30, nameLength, session, asynchronous).ConfigureAwait(false);
                session.ValidateName(name, entry);
            }
            token.ThrowIfCancellationRequested();
            return session.Success(format);
        }
        catch (ContainerException ex) { return session.Incomplete(ex.Reason, ex.LimitKind); }
        catch (XmlException) { return session.Incomplete(FileDetectionReason.InvalidContainer); }
    }

    /// <summary>主部件已经过唯一性检查，按相同规则取对应条目。</summary>
    private static Entry FindMainEntry(List<Entry> entries, string name) => entries.Single(x =>
        FileContentOfficePackage.PartNameComparer.Equals(FileContentOfficePackage.NormalizeEntryName(x.Name), name));

    /// <summary>先读最短尾记录，再按需补读注释窗口；选择距离尾部最近且目录完整有效的候选。</summary>
    private static DirectoryInfo FindDirectory(ReadOnlySpan<byte> content, Inspection session)
    {
        var tail = ReadSpan(content, session.Length - Math.Min(22, session.Length), Math.Min(22, session.Length), session);
        ContainerException failure = null;
        for (var pass = 0; pass < 2; pass++)
        {
            foreach (var position in CandidatePositions(tail))
            {
                try
                {
                    var directory = session.ParseDirectory(tail, position);
                    directory.Entries = session.ReadDirectory(ReadSpan(content, directory.Offset, directory.Size, session), directory);
                    return directory;
                }
                catch (ContainerException ex)
                {
                    // 实际读取预算不可通过切换候选重置；其他候选结构失败允许继续搜索。
                    if (ex.LimitKind == FileDetectionLimitKind.ContainerBytesRead) throw;
                    if (failure == null || failure.Reason == FileDetectionReason.InvalidContainer) failure = ex;
                }
            }
            if (pass != 0 || session.Length <= tail.Length) break;
            var length = (int)Math.Min(65557, session.Length);
            var prefix = ReadSpan(content, session.Length - length, length - tail.Length, session);
            tail = CombineTail(prefix, tail);
        }
        throw failure ?? Invalid();
    }

    private static async Task<DirectoryInfo> FindDirectoryAsync(Stream source, long origin, Inspection session, bool asynchronous)
    {
        var tailLength = (int)Math.Min(22, session.Length);
        var tail = await ReadAt(source, origin, session.Length - tailLength, tailLength, session, asynchronous).ConfigureAwait(false);
        ContainerException failure = null;
        for (var pass = 0; pass < 2; pass++)
        {
            foreach (var position in CandidatePositions(tail))
            {
                try
                {
                    var directory = session.ParseDirectory(tail, position);
                    var data = await ReadAt(source, origin, directory.Offset, directory.Size, session, asynchronous).ConfigureAwait(false);
                    directory.Entries = session.ReadDirectory(data, directory);
                    return directory;
                }
                catch (ContainerException ex)
                {
                    if (ex.LimitKind == FileDetectionLimitKind.ContainerBytesRead) throw;
                    if (failure == null || failure.Reason == FileDetectionReason.InvalidContainer) failure = ex;
                }
            }
            if (pass != 0 || session.Length <= tail.Length) break;
            var length = (int)Math.Min(65557, session.Length);
            var prefix = await ReadAt(source, origin, session.Length - length, length - tail.Length, session, asynchronous).ConfigureAwait(false);
            tail = CombineTail(prefix, tail);
        }
        throw failure ?? Invalid();
    }

    private static byte[] CombineTail(byte[] prefix, byte[] end)
    {
        var tail = new byte[prefix.Length + end.Length];
        Array.Copy(prefix, tail, prefix.Length);
        Array.Copy(end, 0, tail, prefix.Length, end.Length);
        return tail;
    }

    /// <summary>只枚举注释长度恰好覆盖到末尾的签名；扩展后重验尾候选以识别前置 ZIP64 locator。</summary>
    private static IEnumerable<int> CandidatePositions(byte[] tail)
    {
        for (var i = tail.Length - 22; i >= 0; i--)
            if (U32(tail, i) == 0x06054b50 && i + 22 + U16(tail, i + 20) == tail.Length) yield return i;
    }

    /// <summary>读取片段并计入预算；整个输入不会被复制。</summary>
    private static byte[] ReadSpan(ReadOnlySpan<byte> content, long offset, long count, Inspection session)
    {
        session.CheckRead(offset, count);
        session.BytesRead += count;
        return content.Slice((int)offset, (int)count).ToArray();
    }

    /// <summary>按相对起点读取，循环处理短读；不会超过预算请求额外字节。</summary>
    private static async Task<byte[]> ReadAt(Stream source, long origin, long offset, long count,
        Inspection session, bool asynchronous)
    {
        session.CheckRead(offset, count);
        var buffer = new byte[(int)count];
        source.Position = origin + offset;
        var read = 0;
        while (read < buffer.Length)
        {
            session.Token.ThrowIfCancellationRequested();
            var size = asynchronous
                ? await source.ReadAsync(buffer, read, buffer.Length - read, session.Token).ConfigureAwait(false)
                : source.Read(buffer, read, buffer.Length - read);
            session.BytesRead += size;
            session.Token.ThrowIfCancellationRequested();
            if (size == 0) throw Invalid();
            read += size;
        }
        return buffer;
    }

    private static ushort U16(byte[] buffer, int offset) => (ushort)(buffer[offset] | buffer[offset + 1] << 8);
    private static uint U32(byte[] buffer, int offset) => (uint)(buffer[offset] | buffer[offset + 1] << 8 |
        buffer[offset + 2] << 16 | buffer[offset + 3] << 24);
    internal static ContainerException Invalid() => new ContainerException(FileDetectionReason.InvalidContainer);
    private static ContainerException Unsupported() => new ContainerException(FileDetectionReason.UnsupportedContainer);
    private static ContainerException Limit(FileDetectionLimitKind kind) => new ContainerException(FileDetectionReason.ContainerLimitReached, kind);

    /// <summary>结构/预算停止与源 IO 错误分开，后者继续传播给调用方。</summary>
    internal sealed class ContainerException : Exception
    {
        internal FileDetectionReason Reason { get; }
        internal FileDetectionLimitKind LimitKind { get; }
        internal ContainerException(FileDetectionReason reason, FileDetectionLimitKind kind = FileDetectionLimitKind.None) { Reason = reason; LimitKind = kind; }
    }

    /// <summary>经尾记录验证的中央目录位置。</summary>
    private sealed class DirectoryInfo
    {
        internal long Offset;
        internal int Size;
        internal int Count;
        internal List<Entry> Entries;
    }

    /// <summary>仅保存定位元数据需要的中央目录字段。</summary>
    private sealed class Entry
    {
        internal string Name;
        internal byte[] RawName;
        internal ushort Flags;
        internal ushort Method;
        internal uint Crc;
        internal long CompressedSize;
        internal long Size;
        internal long Offset;
    }

    /// <summary>一次检查的不可共享状态、累计预算及结果构造。</summary>
    private sealed class Inspection
    {
        private readonly FileDetectionResult _header;
        private readonly FileDetectionOptions _options;
        private int _metadataBytes;
        internal long Length { get; }
        internal long BytesRead { get; set; }
        internal CancellationToken Token { get; }

        internal Inspection(FileDetectionResult header, FileDetectionOptions options, long length, CancellationToken token)
        {
            _header = header;
            _options = options;
            Length = length;
            Token = token;
        }

        internal FileDetectionResult Incomplete(FileDetectionReason reason, FileDetectionLimitKind kind = FileDetectionLimitKind.None) => new FileDetectionResult(
            FileDetectionStatus.Incomplete, reason, _header.Candidates, _header.BytesRead,
            _header.Format, FileDetectionEvidence.Signature, BytesRead, kind);

        internal FileDetectionResult Success(FileContentFormat format) => new FileDetectionResult(
            FileDetectionStatus.Matched, FileDetectionReason.None, new[] { format ?? _header.Format }, _header.BytesRead,
            _header.Format, FileDetectionEvidence.ContainerStructure, BytesRead);

        /// <summary>在分配缓冲和定位前检查范围与累计请求上限。</summary>
        internal void CheckRead(long offset, long count)
        {
            Token.ThrowIfCancellationRequested();
            if (offset < 0 || count < 0 || offset > Length || count > Length - offset) throw Invalid();
            if (count > _options.MaxContainerBytesRead - BytesRead) throw Limit(FileDetectionLimitKind.ContainerBytesRead);
        }

        /// <summary>验证单个候选；结构关系先于可配置预算检查，避免伪标记直接遮蔽真实目录。</summary>
        internal DirectoryInfo ParseDirectory(byte[] tail, int position)
        {
            if (U16(tail, position + 8) == ushort.MaxValue || U16(tail, position + 10) == ushort.MaxValue ||
                U32(tail, position + 12) == uint.MaxValue || U32(tail, position + 16) == uint.MaxValue ||
                position >= 20 && U32(tail, position - 20) == 0x07064b50) throw Unsupported();
            if (U16(tail, position + 4) != 0 || U16(tail, position + 6) != 0 ||
                U16(tail, position + 8) != U16(tail, position + 10)) throw Unsupported();
            var count = U16(tail, position + 10);
            var size = U32(tail, position + 12);
            var offset = U32(tail, position + 16);
            if ((long)offset + size != Length - tail.Length + position || (long)count * 46 > size) throw Invalid();
            if (count > _options.MaxZipEntries) throw Limit(FileDetectionLimitKind.ZipEntries);
            if (size > _options.MaxZipDirectoryBytes) throw Limit(FileDetectionLimitKind.ZipDirectoryBytes);
            return new DirectoryInfo { Offset = offset, Size = (int)size, Count = count };
        }

        /// <summary>解析有限目录，条目数既核对声明也核对实际数据，不能被伪造的 EOCD 数量绕过。</summary>
        internal List<Entry> ReadDirectory(byte[] data, DirectoryInfo directory)
        {
            var entries = new List<Entry>(directory.Count);
            var position = 0;
            while (position < data.Length)
            {
                Token.ThrowIfCancellationRequested();
                if (entries.Count >= _options.MaxZipEntries) throw Limit(FileDetectionLimitKind.ZipEntries);
                if (data.Length - position < 46 || U32(data, position) != 0x02014b50) throw Invalid();
                var nameLength = U16(data, position + 28);
                var length = 46 + nameLength + U16(data, position + 30) + U16(data, position + 32);
                if (length > data.Length - position || nameLength == 0) throw Invalid();
                var size = U32(data, position + 24);
                var compressed = U32(data, position + 20);
                var offset = U32(data, position + 42);
                if (size == uint.MaxValue || compressed == uint.MaxValue || offset == uint.MaxValue || U16(data, position + 34) != 0) throw Unsupported();
                if (offset >= directory.Offset) throw Invalid();
                var flags = U16(data, position + 8);
                var name = new byte[nameLength];
                Array.Copy(data, position + 46, name, 0, nameLength);
                // 未置 UTF-8 标记的非 ASCII 名称不参与 OPC 部件匹配，避免猜测本机代码页。
                var decoded = (flags & 0x800) != 0 ? DecodeUtf8(name) : name.All(x => x < 128) ? Encoding.ASCII.GetString(name) : null;
                entries.Add(new Entry { Name = decoded, RawName = name, Flags = flags,
                    Method = U16(data, position + 10), Crc = U32(data, position + 16),
                    CompressedSize = compressed, Size = size, Offset = offset });
                position += length;
            }
            if (entries.Count != directory.Count) throw Invalid();
            return entries;
        }

        private static string DecodeUtf8(byte[] data)
        {
            try { return new UTF8Encoding(false, true).GetString(data); }
            catch (DecoderFallbackException) { throw Invalid(); }
        }

        /// <summary>完整没有 OPC 元数据的 ZIP 保留通用容器结论；缺一项或重复清单不确定子类型。</summary>
        internal Entry[] SelectMetadata(List<Entry> entries)
        {
            var types = entries.Where(x => FileContentOfficePackage.PartNameComparer.Equals(x.Name, "[Content_Types].xml")).ToArray();
            var relationships = entries.Where(x => FileContentOfficePackage.PartNameComparer.Equals(x.Name, "_rels/.rels")).ToArray();
            if (types.Length == 0 && relationships.Length == 0) return Array.Empty<Entry>();
            if (types.Length != 1 || relationships.Length != 1) throw Invalid();
            return new[] { types[0], relationships[0] };
        }

        /// <summary>确认识别所需部件的本地头与中央目录一致，数据范围位于目录之前。</summary>
        internal int ValidateLocalHeader(byte[] data, Entry entry, long directoryOffset, bool metadata = true)
        {
            if ((entry.Flags & 0x41) != 0 || entry.Method != 0 && entry.Method != 8) throw Unsupported();
            if (metadata && entry.Size > _options.MaxMetadataBytes - _metadataBytes) throw Limit(FileDetectionLimitKind.MetadataBytes);
            if (U32(data, 0) != 0x04034b50 || U16(data, 6) != entry.Flags || U16(data, 8) != entry.Method) throw Invalid();
            if ((entry.Flags & 8) == 0 && (U32(data, 14) != entry.Crc || U32(data, 18) != entry.CompressedSize || U32(data, 22) != entry.Size)) throw Invalid();
            if ((entry.Flags & 8) != 0 &&
                (U32(data, 14) != 0 && U32(data, 14) != entry.Crc ||
                 U32(data, 18) != 0 && U32(data, 18) != entry.CompressedSize ||
                 U32(data, 22) != 0 && U32(data, 22) != entry.Size)) throw Invalid();
            if (entry.Method == 0 && entry.CompressedSize != entry.Size) throw Invalid();
            var nameLength = U16(data, 26);
            var end = entry.Offset + 30 + nameLength + U16(data, 28) + entry.CompressedSize;
            if (nameLength != entry.RawName.Length || end > directoryOffset) throw Invalid();
            return nameLength;
        }

        internal void ValidateName(byte[] name, Entry entry)
        {
            if (!name.SequenceEqual(entry.RawName)) throw Invalid();
        }

        /// <summary>只解压识别元数据；输出预算按实际字节累计，不能信任 ZIP 的未压缩长度声明。</summary>
        internal byte[] Inflate(byte[] data, Entry entry)
        {
            if (entry.Method == 0)
            {
                if (data.LongLength != entry.Size) throw Invalid();
                _metadataBytes += data.Length;
                return data;
            }
            using var input = new MemoryStream(data, false);
            using var deflate = new DeflateStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            var buffer = new byte[Math.Min(8192, _options.MaxMetadataBytes - _metadataBytes)];
            while (true)
            {
                Token.ThrowIfCancellationRequested();
                var remaining = _options.MaxMetadataBytes - _metadataBytes;
                if (remaining == 0) throw Limit(FileDetectionLimitKind.MetadataBytes);
                int count;
                try { count = deflate.Read(buffer, 0, Math.Min(buffer.Length, remaining)); }
                catch (InvalidDataException) { throw Invalid(); }
                Token.ThrowIfCancellationRequested();
                if (count == 0) break;
                _metadataBytes += count;
                output.Write(buffer, 0, count);
            }
            if (output.Length != entry.Size) throw Invalid();
            return output.ToArray();
        }
    }
}
