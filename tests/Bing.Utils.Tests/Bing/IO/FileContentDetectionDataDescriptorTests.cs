using System.IO.Compression;
using System.Text;
using System.Threading;
using Bing.IO;

namespace Bing.Utils.Tests.Bing.IO;

/// <summary>验证由不可定位 ZIP 输出流生成的 OPC 数据描述符兼容性。</summary>
[Trait("Bing.IO", "FileContentDetectionDataDescriptor")]
public class FileContentDetectionDataDescriptorTests
{
    private const string ContentTypesNamespace = "http://schemas.openxmlformats.org/package/2006/content-types";
    private const string RelationshipsNamespace = "http://schemas.openxmlformats.org/package/2006/relationships";
    private const string OfficeDocumentRelationship = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument";
    private const string MainPartContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml";

    /// <summary>由不可定位输出流生成的 Deflate ZIP 在数组、同步和异步检测入口均能识别。</summary>
    [Fact]
    public async Task Detect_DataDescriptorDeflateOfficePackageWorksAcrossInputsAndRestoresPosition()
    {
        var package = CreateOfficePackage();
        AssertOfficeDataDescriptors(package);

        var arrayResult = FileContentDetector.Detect(package, DeepOptions());
        AssertDocx(arrayResult);

        var prefix = new byte[] { 0x19, 0x29, 0x39, 0x49, 0x59 };
        var positionedBytes = prefix.Concat(package).ToArray();
        using var source = new MemoryStream(positionedBytes, writable: false);
        source.Position = prefix.Length;
        var streamResult = FileContentDetector.Detect(source, DeepOptions());
        AssertDocx(streamResult);
        Assert.Equal(prefix.Length, source.Position);

        using var asyncSource = new MemoryStream(positionedBytes, writable: false);
        asyncSource.Position = prefix.Length;
        var asyncResult = await FileContentDetector.DetectAsync(asyncSource, DeepOptions());
        AssertDocx(asyncResult);
        Assert.Equal(prefix.Length, asyncSource.Position);
    }

    /// <summary>异步缓存入口可深检不可定位数据描述符 ZIP，并在落盘后完整回读和清理。</summary>
    [Fact]
    public async Task ProbeBufferedAsync_DataDescriptorOfficePackageSpillsAndReplaysCompleteContent()
    {
        var package = CreateOfficePackage();
        AssertOfficeDataDescriptors(package);
        using var fixture = new TemporaryStoreFixture(package.Length);
        using var source = new NonSeekableReadStream(package, chunkSize: 67);

        await using (var buffered = await FileContentDetector.ProbeBufferedAsync(source, fixture.Store,
                           package.Length, memoryThresholdBytes: 16))
        {
            AssertDocx(buffered.Result);
            Assert.True(buffered.Content.CanSeek);
            Assert.False(buffered.Content.CanWrite);
            Assert.Equal(0, buffered.Content.Position);
            Assert.Equal(package.Length, buffered.Content.Length);
            Assert.Equal(package, await ReadAllAsync(buffered.Content));
            Assert.Equal(1, fixture.Store.GetStatistics().FileCount);
        }

        Assert.Equal(0, fixture.Store.GetStatistics().FileCount);
    }

    /// <summary>bit 3 下本地头非零 CRC 或长度只允许与中央目录一致。</summary>
    [Theory]
    [InlineData(14, 16)]
    [InlineData(18, 20)]
    [InlineData(22, 24)]
    public void Detect_DataDescriptorLocalHeaderNonzeroFieldsMustMatchCentralDirectory(int localFieldOffset,
        int centralFieldOffset)
    {
        var package = CreateOfficePackage();
        AssertOfficeDataDescriptors(package);
        var localOffset = FindLocalEntry(package, "[Content_Types].xml");
        var centralOffset = FindCentralEntry(package, "[Content_Types].xml");
        var centralValue = ReadUInt32(package, centralOffset + centralFieldOffset);
        var incorrectNonzeroValue = centralValue == 1 ? 2u : 1u;
        Assert.NotEqual(0u, incorrectNonzeroValue);
        Assert.NotEqual(centralValue, incorrectNonzeroValue);
        WriteUInt32(package, localOffset + localFieldOffset, incorrectNonzeroValue);

        var result = FileContentDetector.Detect(package, DeepOptions());

        Assert.Equal(FileDetectionStatus.Incomplete, result.Status);
        Assert.Equal(FileDetectionReason.InvalidContainer, result.ReasonCode);
        Assert.Equal("zip", result.ContainerFormat?.Id);
        Assert.Equal(FileDetectionEvidence.Signature, result.Evidence);
        Assert.Contains(result.Candidates, candidate => candidate.Id == "zip");
    }

    private static FileDetectionOptions DeepOptions() => new FileDetectionOptions { InspectContainers = true };

    private static void AssertDocx(FileDetectionResult result)
    {
        Assert.Equal(FileDetectionStatus.Matched, result.Status);
        Assert.Equal("docx", result.Format?.Id);
        Assert.Equal("zip", result.ContainerFormat?.Id);
        Assert.Equal(FileDetectionEvidence.ContainerStructure, result.Evidence);
    }

    private static byte[] CreateOfficePackage()
    {
        using var destination = new MemoryStream();
        using (var nonSeekableOutput = new NonSeekableWriteStream(destination))
        using (var archive = new ZipArchive(nonSeekableOutput, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteDeflatedEntry(archive, "[Content_Types].xml",
                "<Types xmlns=\"" + ContentTypesNamespace + "\"><Override PartName=\"/word/document.xml\" " +
                "ContentType=\"" + MainPartContentType + "\"/></Types>");
            WriteDeflatedEntry(archive, "_rels/.rels",
                "<Relationships xmlns=\"" + RelationshipsNamespace + "\"><Relationship Id=\"rId1\" Type=\"" +
                OfficeDocumentRelationship + "\" Target=\"word/document.xml\"/></Relationships>");
            WriteDeflatedEntry(archive, "word/document.xml",
                "<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"/>");
        }
        return destination.ToArray();
    }

    private static void WriteDeflatedEntry(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        using var output = entry.Open();
        var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(content);
        output.Write(bytes, 0, bytes.Length);
    }

    private static void AssertOfficeDataDescriptors(byte[] package)
    {
        AssertDataDescriptor(package, "[Content_Types].xml");
        AssertDataDescriptor(package, "_rels/.rels");
        AssertDataDescriptor(package, "word/document.xml");
    }

    /// <summary>检查 central directory、local header 和 descriptor 三处真实 ZIP 元数据。</summary>
    private static void AssertDataDescriptor(byte[] package, string entryName)
    {
        var centralOffset = FindCentralEntry(package, entryName);
        var localOffset = checked((int)ReadUInt32(package, centralOffset + 42));
        Assert.Equal(0x04034B50u, ReadUInt32(package, localOffset));
        Assert.Equal(0x02014B50u, ReadUInt32(package, centralOffset));
        var flags = ReadUInt16(package, centralOffset + 8);
        Assert.Equal(flags, ReadUInt16(package, localOffset + 6));
        Assert.True((flags & 0x0008) != 0, entryName + " must use the ZIP data descriptor flag.");
        Assert.Equal((ushort)8, ReadUInt16(package, centralOffset + 10));
        Assert.Equal((ushort)8, ReadUInt16(package, localOffset + 8));
        Assert.Equal(0u, ReadUInt32(package, localOffset + 14));
        Assert.Equal(0u, ReadUInt32(package, localOffset + 18));
        Assert.Equal(0u, ReadUInt32(package, localOffset + 22));

        var localNameLength = ReadUInt16(package, localOffset + 26);
        var localExtraLength = ReadUInt16(package, localOffset + 28);
        var centralNameLength = ReadUInt16(package, centralOffset + 28);
        var centralExtraLength = ReadUInt16(package, centralOffset + 30);
        var centralCommentLength = ReadUInt16(package, centralOffset + 32);
        var localName = Encoding.UTF8.GetString(package, localOffset + 30, localNameLength);
        var centralName = Encoding.UTF8.GetString(package, centralOffset + 46, centralNameLength);
        Assert.Equal(entryName, localName);
        Assert.Equal(localName, centralName);

        var compressedSize = ReadUInt32(package, centralOffset + 20);
        var descriptorOffset = checked(localOffset + 30 + localNameLength + localExtraLength + (int)compressedSize);
        var directoryOffset = checked((int)ReadUInt32(package, FindEndOfCentralDirectory(package) + 16));
        Assert.True(descriptorOffset < directoryOffset, "The data descriptor must precede the central directory.");
        var descriptorDataOffset = descriptorOffset;
        var centralCrc = ReadUInt32(package, centralOffset + 16);
        var uncompressedSize = ReadUInt32(package, centralOffset + 24);
        if (descriptorOffset + 16 <= directoryOffset &&
            ReadUInt32(package, descriptorDataOffset) == 0x08074B50u &&
            ReadUInt32(package, descriptorDataOffset + 4) == centralCrc &&
            ReadUInt32(package, descriptorDataOffset + 8) == compressedSize &&
            ReadUInt32(package, descriptorDataOffset + 12) == uncompressedSize)
            descriptorDataOffset += 4;
        Assert.True(descriptorDataOffset + 12 <= directoryOffset, "The data descriptor must be complete before the central directory.");
        Assert.Equal(centralCrc, ReadUInt32(package, descriptorDataOffset));
        Assert.Equal(compressedSize, ReadUInt32(package, descriptorDataOffset + 4));
        Assert.Equal(uncompressedSize, ReadUInt32(package, descriptorDataOffset + 8));

        var nextCentralOffset = centralOffset + 46 + centralNameLength + centralExtraLength + centralCommentLength;
        Assert.True(nextCentralOffset <= package.Length, "The central-directory entry must be complete.");
    }

    private static int FindCentralEntry(byte[] package, string entryName)
    {
        var end = FindEndOfCentralDirectory(package);
        var offset = checked((int)ReadUInt32(package, end + 16));
        var directoryEnd = checked(offset + (int)ReadUInt32(package, end + 12));
        while (offset < directoryEnd)
        {
            if (ReadUInt32(package, offset) != 0x02014B50u)
                throw new InvalidDataException("Central-directory signature was not found.");
            var nameLength = ReadUInt16(package, offset + 28);
            var extraLength = ReadUInt16(package, offset + 30);
            var commentLength = ReadUInt16(package, offset + 32);
            if (Encoding.UTF8.GetString(package, offset + 46, nameLength) == entryName) return offset;
            offset = checked(offset + 46 + nameLength + extraLength + commentLength);
        }
        throw new InvalidDataException("Central-directory entry was not found: " + entryName);
    }

    private static int FindLocalEntry(byte[] package, string entryName)
    {
        var offset = checked((int)ReadUInt32(package, FindCentralEntry(package, entryName) + 42));
        if (ReadUInt32(package, offset) != 0x04034B50u)
            throw new InvalidDataException("Local-entry signature was not found: " + entryName);
        return offset;
    }

    private static int FindEndOfCentralDirectory(byte[] package)
    {
        for (var offset = package.Length - 22; offset >= 0; offset--)
        {
            if (ReadUInt32(package, offset) == 0x06054B50u &&
                offset + 22 + ReadUInt16(package, offset + 20) == package.Length)
                return offset;
        }
        throw new InvalidDataException("ZIP end-of-central-directory record was not found.");
    }

    private static ushort ReadUInt16(byte[] data, int offset)
        => (ushort)(data[offset] | data[offset + 1] << 8);

    private static uint ReadUInt32(byte[] data, int offset)
        => (uint)(data[offset] | data[offset + 1] << 8 | data[offset + 2] << 16 | data[offset + 3] << 24);

    private static void WriteUInt32(byte[] data, int offset, uint value)
    {
        for (var index = 0; index < 4; index++)
            data[offset + index] = (byte)(value >> (8 * index));
    }

    private static async Task<byte[]> ReadAllAsync(Stream source)
    {
        using var destination = new MemoryStream();
        var buffer = new byte[4096];
        int count;
        while ((count = await source.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None).ConfigureAwait(false)) > 0)
            await destination.WriteAsync(buffer, 0, count, CancellationToken.None).ConfigureAwait(false);
        return destination.ToArray();
    }

    private sealed class TemporaryStoreFixture : IDisposable
    {
        private readonly string _root;
        internal TemporaryFileStore Store { get; }

        internal TemporaryStoreFixture(long quota)
        {
            _root = Path.Combine(Path.GetTempPath(), "bing-file-content-dd-" + Guid.NewGuid().ToString("N"));
            Store = new TemporaryFileStore(_root, quota);
        }

        public void Dispose()
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
        }
    }

    private sealed class NonSeekableWriteStream : Stream
    {
        private readonly Stream _destination;

        internal NonSeekableWriteStream(Stream destination) => _destination = destination;

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() => _destination.Flush();
        public override void Write(byte[] buffer, int offset, int count) => _destination.Write(buffer, offset, count);
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => _destination.WriteAsync(buffer, offset, count, cancellationToken);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }

    private sealed class NonSeekableReadStream : Stream
    {
        private readonly MemoryStream _inner;
        private readonly int _chunkSize;

        internal NonSeekableReadStream(byte[] content, int chunkSize)
        {
            _inner = new MemoryStream(content, writable: false);
            _chunkSize = chunkSize;
        }

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
            => _inner.Read(buffer, offset, Math.Min(count, _chunkSize));

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => _inner.ReadAsync(buffer, offset, Math.Min(count, _chunkSize), cancellationToken);

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Flush() { }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _inner.Dispose();
            base.Dispose(disposing);
        }
    }
}
