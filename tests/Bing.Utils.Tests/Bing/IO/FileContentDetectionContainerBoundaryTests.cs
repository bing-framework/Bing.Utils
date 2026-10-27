using Bing.IO;
using System.IO.Compression;
using System.Threading;

namespace Bing.Utils.Tests.Bing.IO;

/// <summary>验证容器读取的真实资源上限和异常/取消的流契约。</summary>
public class FileContentDetectionContainerBoundaryTests
{
    [Fact]
    public void Detect_ExactReadBudgetIsAccepted_OneByteLessStopsWithoutOverread()
    {
        var bytes = Package();
        using var baseline = new TrackingStream(bytes);
        var first = FileContentDetector.Detect(baseline, Deep());
        Assert.Equal("docx", first.Format?.Id);
        Assert.Equal(first.BytesRead + first.ContainerBytesRead, baseline.TotalRead);

        var options = Deep();
        options.MaxContainerBytesRead = checked((int)first.ContainerBytesRead);
        using var exact = new TrackingStream(bytes);
        Assert.Equal("docx", FileContentDetector.Detect(exact, options).Format?.Id);
        options.MaxContainerBytesRead--;
        using var limited = new TrackingStream(bytes);
        var stopped = FileContentDetector.Detect(limited, options);
        Assert.Equal(FileDetectionReason.ContainerLimitReached, stopped.ReasonCode);
        Assert.True(limited.TotalRead <= stopped.BytesRead + options.MaxContainerBytesRead);
        Assert.Equal(stopped.BytesRead + stopped.ContainerBytesRead, limited.TotalRead);
        Assert.Equal(0, limited.Position);
    }

    [Fact]
    public void Detect_LargeStoredPayloadReadsOnlyBoundedTailDirectoryAndMetadata()
    {
        var bytes = Package(extraPayload: 512 * 1024);
        using var stream = new TrackingStream(bytes);
        var result = FileContentDetector.Detect(stream, Deep());
        Assert.Equal("docx", result.Format?.Id);
        Assert.True(stream.TotalRead < 80 * 1024);
        Assert.Equal(result.BytesRead + result.ContainerBytesRead, stream.TotalRead);
        Assert.Equal(0, stream.Position);
        // Span 入口复用相同范围，不复制整个 512 KiB 正文。
        var spanResult = FileContentDetector.Detect(new ReadOnlySpan<byte>(bytes), Deep());
        Assert.Equal("docx", spanResult.Format?.Id);
        Assert.Equal(result.ContainerBytesRead, spanResult.ContainerBytesRead);
    }

    [Fact]
    public void Detect_ForgedUncompressedSizeCannotBypassActualInflationBudget()
    {
        var bytes = Package(padding: 10000);
        var central = Find(bytes, 0x02014b50);
        Write32(bytes, 22, 1);
        Write32(bytes, central + 24, 1);
        var options = Deep();
        options.MaxMetadataBytes = 1024;
        var result = FileContentDetector.Detect(bytes, options);
        Assert.Equal(FileDetectionStatus.Incomplete, result.Status);
        Assert.Equal(FileDetectionReason.ContainerLimitReached, result.ReasonCode);
        Assert.Equal("zip", result.ContainerFormat.Id);
    }

    [Fact]
    public void Detect_ForgedEntryCountDoesNotHideActualDirectoryEntries()
    {
        var bytes = Package();
        var end = bytes.Length - 22;
        bytes[end + 8] = bytes[end + 9] = bytes[end + 10] = bytes[end + 11] = 0;
        Assert.Equal(FileDetectionReason.InvalidContainer, FileContentDetector.Detect(bytes, Deep()).ReasonCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Detect_SourceErrorsPropagateAndRestorePosition(bool invalidData)
    {
        Exception failure = invalidData ? new InvalidDataException("source failure") : new IOException("source failure");
        using var source = new TrackingStream(Package()) { Failure = failure };
        var actual = await Record.ExceptionAsync(() => FileContentDetector.DetectAsync(source, Deep()));
        Assert.Same(failure, actual);
        Assert.Equal(0, source.Position);
        Assert.True(source.CanRead);
    }

    [Fact]
    public async Task DetectAsync_CancellationDuringContainerReadRestoresPosition()
    {
        using var cancellation = new CancellationTokenSource();
        using var source = new TrackingStream(Package()) { Cancel = cancellation };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => FileContentDetector.DetectAsync(source, Deep(), cancellation.Token));
        Assert.Equal(0, source.Position);
        Assert.True(source.CanRead);
    }

    [Fact]
    public async Task DetectAsync_UsesOnlyAsyncSourceReadsAndHandlesShortReads()
    {
        using var source = new TrackingStream(Package()) { AsyncOnly = true, ChunkSize = 3 };
        var result = await FileContentDetector.DetectAsync(source, Deep());
        Assert.Equal("docx", result.Format?.Id);
        Assert.Equal(result.BytesRead + result.ContainerBytesRead, source.TotalRead);
        Assert.Equal(0, source.Position);
    }

    [Fact]
    public async Task DetectFile_DeepSyncAndAsyncCloseTheirHandles()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(path, Package());
            Assert.Equal("docx", FileContentDetector.DetectFile(path, Deep()).Format?.Id);
            Assert.Equal("docx", (await FileContentDetector.DetectFileAsync(path, Deep())).Format?.Id);
            using var exclusive = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            Assert.True(exclusive.CanWrite);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Detect_ContainerOptionsAreSnapshottedBeforeSourceReads()
    {
        var options = Deep();
        using var source = new TrackingStream(Package()) { OnFirstRead = () => options.MaxZipEntries = 1 };
        Assert.Equal("docx", FileContentDetector.Detect(source, options).Format?.Id);
    }

    [Fact]
    public void Detect_MainContentTypeUsesCaseInsensitiveMimeComparison()
    {
        using var input = new MemoryStream(Package());
        using var output = new MemoryStream();
        input.CopyTo(output);
        using (var archive = new ZipArchive(output, ZipArchiveMode.Update, true))
        {
            var entry = archive.GetEntry("[Content_Types].xml");
            string xml;
            using (var reader = new StreamReader(entry.Open(), Encoding.UTF8)) xml = reader.ReadToEnd();
            entry.Delete();
            Add(archive, "[Content_Types].xml", xml.Replace(
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml",
                "Application/Vnd.Openxmlformats-Officedocument.Wordprocessingml.Document.Main+Xml"));
        }
        var result = FileContentDetector.Detect(output.ToArray(), Deep());
        Assert.Equal("docx", result.Format?.Id);
        Assert.Equal(FileExtensionMatch.Match, FileContentDetector.CompareExtension(result, ".docx"));
        Assert.Equal(FileExtensionMatch.Mismatch, FileContentDetector.CompareExtension(result, ".xlsx"));
    }

    private static FileDetectionOptions Deep() => new FileDetectionOptions { InspectContainers = true };

    private static byte[] Package(int padding = 0, int extraPayload = 0)
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            Add(archive, "[Content_Types].xml", "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Override PartName=\"/word/document.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml\"/></Types>" + new string(' ', padding));
            Add(archive, "_rels/.rels", "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"word/document.xml\"/></Relationships>");
            Add(archive, "word/document.xml", "<document/>");
            if (extraPayload > 0)
            {
                using var body = archive.CreateEntry("body.bin", CompressionLevel.NoCompression).Open();
                var buffer = new byte[extraPayload];
                body.Write(buffer, 0, buffer.Length);
            }
        }
        return output.ToArray();
    }

    private static void Add(ZipArchive archive, string name, string text)
    {
        using var entry = archive.CreateEntry(name, CompressionLevel.Optimal).Open();
        var bytes = Encoding.UTF8.GetBytes(text);
        entry.Write(bytes, 0, bytes.Length);
    }

    private static int Find(byte[] bytes, uint value)
    {
        for (var i = 0; i <= bytes.Length - 4; i++)
            if (BitConverter.ToUInt32(bytes, i) == value) return i;
        throw new InvalidOperationException("Missing fixture record");
    }

    private static void Write32(byte[] bytes, int offset, uint value)
        => Array.Copy(BitConverter.GetBytes(value), 0, bytes, offset, 4);

    /// <summary>度量真实源读取；第二次读取发生在容器阶段。</summary>
    private sealed class TrackingStream : MemoryStream
    {
        private int _calls;
        internal long TotalRead { get; private set; }
        internal int ChunkSize { get; set; } = int.MaxValue;
        internal bool AsyncOnly { get; set; }
        internal Exception Failure { get; set; }
        internal CancellationTokenSource Cancel { get; set; }
        internal Action OnFirstRead { get; set; }
        internal TrackingStream(byte[] data) : base(data, false) { }

        private int ReadCore(byte[] buffer, int offset, int count)
        {
            _calls++;
            if (_calls == 1) OnFirstRead?.Invoke();
            if (_calls == 2 && Failure != null) throw Failure;
            var read = base.Read(buffer, offset, Math.Min(count, ChunkSize));
            TotalRead += read;
            if (_calls == 2) Cancel?.Cancel();
            return read;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (AsyncOnly) throw new InvalidOperationException("Synchronous source IO is forbidden");
            return ReadCore(buffer, offset, count);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(ReadCore(buffer, offset, count));
        }
    }
}
