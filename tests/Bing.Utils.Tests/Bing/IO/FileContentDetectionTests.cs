using Bing.IO;
using System.IO.Compression;
using System.Threading;

namespace Bing.Utils.Tests.Bing.IO;

/// <summary>
/// 验证文件内容签名识别结果、流契约及文件入口。
/// </summary>
[Trait("Bing.IO", "FileContentDetector")]
public class FileContentDetectionTests
{
    /// <summary>
    /// 提供路线 A 第一批支持的签名及变体。
    /// </summary>
    public static IEnumerable<object[]> HeaderCases()
    {
        yield return new object[] { "jpeg", new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 } };
        yield return new object[] { "png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } };
        yield return new object[] { "gif", Encoding.ASCII.GetBytes("GIF87a") };
        yield return new object[] { "gif", Encoding.ASCII.GetBytes("GIF89a") };
        yield return new object[] { "bmp", new byte[] { 0x42, 0x4D } };
        yield return new object[] { "tiff", new byte[] { 0x49, 0x49, 0x2A, 0x00 } };
        yield return new object[] { "tiff", new byte[] { 0x4D, 0x4D, 0x00, 0x2A } };
        yield return new object[] { "tiff", new byte[] { 0x49, 0x49, 0x2B, 0x00, 0x08, 0x00, 0x00, 0x00 } };
        yield return new object[] { "tiff", new byte[] { 0x4D, 0x4D, 0x00, 0x2B, 0x00, 0x08, 0x00, 0x00 } };
        yield return new object[] { "ico", new byte[] { 0x00, 0x00, 0x01, 0x00 } };
        yield return new object[] { "webp", new byte[] { 0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50 } };
        yield return new object[] { "pdf", Encoding.ASCII.GetBytes("%PDF-1.7") };
        yield return new object[] { "zip", new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x00, 0x00 } };
        yield return new object[] { "zip", new byte[] { 0x50, 0x4B, 0x05, 0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 } };
        yield return new object[] { "gzip", new byte[] { 0x1F, 0x8B, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00 } };
        yield return new object[] { "7z", new byte[] { 0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C } };
        yield return new object[] { "rar", new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x00 } };
        yield return new object[] { "rar", new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x01, 0x00 } };
        yield return new object[] { "wav", new byte[] { 0x52, 0x49, 0x46, 0x46, 0x04, 0x00, 0x00, 0x00, 0x57, 0x41, 0x56, 0x45 } };
        yield return new object[] { "avi", new byte[] { 0x52, 0x49, 0x46, 0x46, 0x24, 0x00, 0x00, 0x00, 0x41, 0x56, 0x49, 0x20 } };
        yield return new object[] { "flac", Encoding.ASCII.GetBytes("fLaC") };
    }

    /// <summary>
    /// 内置签名和格式变体在字节数组及只读跨度入口中得到相同结果。
    /// </summary>
    [Theory]
    [MemberData(nameof(HeaderCases))]
    public void Detect_HeaderRecognizesFormatAndVariant(string expectedId, byte[] header)
    {
        var arrayResult = FileContentDetector.Detect(header);
        var spanResult = FileContentDetector.Detect(new ReadOnlySpan<byte>(header));

        AssertMatchedFormat(arrayResult, expectedId);
        AssertMatchedFormat(spanResult, expectedId);
    }

    /// <summary>
    /// 空内容、未知内容及截断签名返回不同状态和原因。
    /// </summary>
    [Fact]
    public void Detect_EmptyUnknownAndTruncatedInputsHaveDistinctStatuses()
    {
        var empty = FileContentDetector.Detect(Array.Empty<byte>());
        var unknown = FileContentDetector.Detect(Encoding.ASCII.GetBytes("ordinary payload"));
        var truncated = FileContentDetector.Detect(new byte[] { 0x89, 0x50 });

        Assert.Equal(FileDetectionStatus.Empty, empty.Status);
        Assert.Equal(0, empty.BytesRead);
        Assert.Equal(FileDetectionStatus.Unknown, unknown.Status);
        Assert.Equal(FileDetectionReason.NoSignatureMatch, unknown.ReasonCode);
        Assert.Null(unknown.Format);
        Assert.Empty(unknown.Candidates);
        Assert.Equal(FileDetectionStatus.Incomplete, truncated.Status);
        Assert.Equal(FileDetectionReason.Truncated, truncated.ReasonCode);
    }

    /// <summary>
    /// 只在签名规定偏移识别容器标记，前置字节和错误偏移均不匹配。
    /// </summary>
    [Fact]
    public void Detect_RequiresSignatureAtItsDefinedOffset()
    {
        var prefixedPng = new byte[] { 0x00 }.Concat((byte[])HeaderCases().First(x => (string)x[0] == "png")[1]).ToArray();
        var misplacedWebp = Encoding.ASCII.GetBytes("RIFF\0\0\0\0XWEB");

        Assert.Equal(FileDetectionStatus.Unknown, FileContentDetector.Detect(prefixedPng).Status);
        Assert.Equal(FileDetectionStatus.Unknown, FileContentDetector.Detect(misplacedWebp).Status);
    }

    /// <summary>
    /// 头部预算必须为正，耗尽预算但尚有未排除规则时标记不完整。
    /// </summary>
    [Fact]
    public void Detect_ValidatesHeaderBudgetAndReportsLimit()
    {
        var png = (byte[])HeaderCases().First(x => (string)x[0] == "png")[1];

        Assert.Throws<ArgumentOutOfRangeException>(() => FileContentDetector.Detect(png, new FileDetectionOptions { MaxHeaderBytes = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => FileContentDetector.Detect(png, new FileDetectionOptions { MaxHeaderBytes = -1 }));
        Assert.Equal(65536, new FileDetectionOptions().MaxHeaderBytes);

        var limited = FileContentDetector.Detect(png, new FileDetectionOptions { MaxHeaderBytes = 1 });
        Assert.Equal(FileDetectionStatus.Incomplete, limited.Status);
        Assert.Equal(FileDetectionReason.HeaderLimitReached, limited.ReasonCode);
        Assert.Equal(1, limited.BytesRead);
    }

    /// <summary>
    /// 对嵌入的完整一像素 PNG/GIF 样本，只断言签名识别，不把结果当作结构校验。
    /// </summary>
    [Fact]
    public void Detect_CompletePngAndGifSamplesMatchTheirSignatures()
    {
        // 按 PNG 块规范生成 RGB 一像素样本（zlib + CRC32）；GIF 为手工编码的完整 4/0/5 LZW 码。
        var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAIAAACQd1PeAAAADElEQVR4nGP4z8AAAAMBAQDJ/pLvAAAAAElFTkSuQmCC");
        var gif = Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAP///ywAAAAAAQABAAACAkQBADs=");

        Assert.True(png.Length > 8);
        Assert.True(gif.Length > 6);
        AssertMatchedFormat(FileContentDetector.Detect(png), "png");
        AssertMatchedFormat(FileContentDetector.Detect(gif), "gif");
    }

    /// <summary>
    /// 运行时生成 ZIP 与 GZip 数据可按容器签名识别。
    /// </summary>
    [Fact]
    public void Detect_RuntimeZipAndGzipSamplesMatchTheirSignatures()
    {
        var zipBytes = CreateZipSample();
        var gzipBytes = CreateGzipSample();

        AssertMatchedFormat(FileContentDetector.Detect(zipBytes), "zip");
        AssertMatchedFormat(FileContentDetector.Detect(gzipBytes), "gzip");
    }

    /// <summary>
    /// 只定位流从当前位置检测，成功后恢复原位置并正确处理短读。
    /// </summary>
    [Fact]
    public void Detect_SeekableStreamUsesCurrentPositionAndRestoresIt()
    {
        var png = (byte[])HeaderCases().First(x => (string)x[0] == "png")[1];
        var bytes = new byte[] { 0xAA, 0xBB }.Concat(png).Concat(new byte[] { 0xCC }).ToArray();
        using var source = new TrackingReadStream(bytes, maxReadSize: 1);
        source.Position = 2;

        var result = FileContentDetector.Detect(source);

        AssertMatchedFormat(result, "png");
        Assert.Equal(2, source.Position);
        Assert.Equal(result.BytesRead, source.BytesRead);
        Assert.True(source.ReadCalls > 1);
        Assert.True(source.CanRead);
    }

    /// <summary>
    /// 同步定位流读失败时传播原异常，并在 finally 中恢复初始位置。
    /// </summary>
    [Fact]
    public void Detect_SeekableStreamRestoresPositionAfterReadFailure()
    {
        using var source = new ThrowAfterFirstReadStream(new byte[] { 0xAA, 0x89, 0x50, 0x4E, 0x47 });
        source.Position = 1;

        Assert.Throws<IOException>(() => FileContentDetector.Detect(source));

        Assert.Equal(1, source.Position);
        Assert.True(source.CanRead);
    }

    /// <summary>
    /// 异步定位流被取消后传播取消，并恢复初始位置且保留流所有权。
    /// </summary>
    [Fact]
    public async Task DetectAsync_SeekableStreamRestoresPositionAfterCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        using var source = new CancelAfterFirstReadStream(new byte[] { 0xAA, 0x89, 0x50, 0x4E, 0x47 }, cancellation);
        source.Position = 1;

        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            FileContentDetector.DetectAsync(source, cancellationToken: cancellation.Token));

        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(1, source.Position);
        Assert.True(source.CanRead);
        Assert.True(source.AsyncReadCalls > 0);
    }

    /// <summary>
    /// 异步定位流以短读完成识别，且检测后恢复读取起点。
    /// </summary>
    [Fact]
    public async Task DetectAsync_SeekableShortReadsRecognizeFormatAndRestorePosition()
    {
        var gif = Encoding.ASCII.GetBytes("GIF89a");
        var content = new byte[] { 0xAA }.Concat(gif).ToArray();
        using var source = new TrackingReadStream(content, maxReadSize: 1);
        source.Position = 1;

        var result = await FileContentDetector.DetectAsync(source);

        AssertMatchedFormat(result, "gif");
        Assert.Equal(1, source.Position);
        Assert.Equal(result.BytesRead, source.BytesRead);
        Assert.True(source.AsyncReadCalls > 1);
        Assert.True(source.CanRead);
    }

    /// <summary>
    /// 不可定位流拒绝 Detect，但 Probe 能用短读识别并完整重放前缀及剩余内容。
    /// </summary>
    [Fact]
    public void Detect_NonSeekableStreamRequiresProbeAndProbeReplaysFullContent()
    {
        var png = (byte[])HeaderCases().First(x => (string)x[0] == "png")[1];
        var payload = Enumerable.Range(0, 40).Select(x => (byte)x).ToArray();
        var content = png.Concat(payload).ToArray();
        using var source = new TrackingReadStream(content, maxReadSize: 1, canSeek: false);

        Assert.Throws<NotSupportedException>(() => FileContentDetector.Detect(source));
        Assert.Equal(0, source.BytesRead);

        using var probe = FileContentDetector.Probe(source);
        AssertMatchedFormat(probe.Result, "png");
        Assert.False(probe.Content.CanSeek);
        Assert.False(probe.Content.CanWrite);
        Assert.Throws<NotSupportedException>(() => probe.Content.Seek(0, SeekOrigin.Begin));
        Assert.Throws<NotSupportedException>(() => probe.Content.WriteByte(0xFF));

        var beforeZeroLengthRead = source.BytesRead;
        Assert.Equal(0, probe.Content.Read(new byte[4], 0, 0));
        Assert.Equal(beforeZeroLengthRead, source.BytesRead);

        Assert.Equal(content, ReadAll(probe.Content));
        Assert.Equal(content.Length, source.BytesRead);
        Assert.True(source.CanRead);
    }

    /// <summary>
    /// 异步 Probe 用异步短读识别，异步读取 Content 时返回完整原内容。
    /// </summary>
    [Fact]
    public async Task ProbeAsync_ShortReadsAndAsyncReplayPreserveFullContent()
    {
        var gif = Encoding.ASCII.GetBytes("GIF89a");
        var content = gif.Concat(Enumerable.Range(0, 35).Select(x => (byte)(255 - x))).ToArray();
        using var source = new TrackingReadStream(content, maxReadSize: 2, canSeek: false);

        using var probe = await FileContentDetector.ProbeAsync(source);

        AssertMatchedFormat(probe.Result, "gif");
        Assert.True(source.AsyncReadCalls > 0);
        Assert.Equal(content, await ReadAllAsync(probe.Content));
        Assert.Equal(content.Length, source.BytesRead);
    }

    /// <summary>
    /// Probe 对定位流也遵守消费契约，不把源位置复位，并默认保留源流。
    /// </summary>
    [Fact]
    public void Probe_SeekableStreamConsumesHeaderAndLeavesSourceOpen()
    {
        var pdf = Encoding.ASCII.GetBytes("%PDF-1.7\nbody");
        using var source = new TrackingReadStream(pdf);
        using var probe = FileContentDetector.Probe(source);

        AssertMatchedFormat(probe.Result, "pdf");
        Assert.Equal(probe.Result.BytesRead, source.Position);
        Assert.Equal(pdf, ReadAll(probe.Content));
        Assert.True(source.CanRead);
    }

    /// <summary>
    /// leaveOpen=false 在验证通过后接管源，成功返回时由释放 Probe 关闭源流。
    /// </summary>
    [Fact]
    public void Probe_LeaveOpenFalseTransfersOwnershipUntilProbeDisposal()
    {
        var pdf = Encoding.ASCII.GetBytes("%PDF-1.7");
        var source = new TrackingReadStream(pdf, canSeek: false);
        var probe = FileContentDetector.Probe(source, leaveOpen: false);

        Assert.True(source.CanRead);
        probe.Dispose();
        Assert.True(source.IsDisposed);
        Assert.False(source.CanRead);
    }

    /// <summary>
    /// Probe 参数错误和开始前取消不接管源流，也不消耗输入。
    /// </summary>
    [Fact]
    public async Task Probe_InvalidOptionsAndInitialCancellationDoNotTakeOwnership()
    {
        var source = new TrackingReadStream(Encoding.ASCII.GetBytes("%PDF-1.7"), canSeek: false);
        Assert.Throws<ArgumentOutOfRangeException>(() => FileContentDetector.Probe(source,
            new FileDetectionOptions { MaxHeaderBytes = 0 }, leaveOpen: false));
        Assert.Equal(0, source.BytesRead);
        Assert.True(source.CanRead);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => FileContentDetector.ProbeAsync(source,
            leaveOpen: false, cancellationToken: cancellation.Token));
        Assert.Equal(0, source.BytesRead);
        Assert.True(source.CanRead);
    }

    /// <summary>
    /// Probe 在开始读取后失败时按 leaveOpen 决定是否释放源流。
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Probe_ReadFailureHonorsOwnership(bool leaveOpen)
    {
        var source = new ThrowAfterFirstReadStream(new byte[] { 0x89, 0x50, 0x4E, 0x47 });

        Assert.Throws<IOException>(() => FileContentDetector.Probe(source, leaveOpen: leaveOpen));

        Assert.Equal(!leaveOpen, source.IsDisposed);
        if (leaveOpen)
            Assert.True(source.CanRead);
    }

    /// <summary>
    /// ProbeAsync 中途取消时按 leaveOpen 清理源流，开始前取消则由调用者保留。
    /// </summary>
    [Fact]
    public async Task ProbeAsync_MidReadCancellationHonorsOwnership()
    {
        using var cancellation = new CancellationTokenSource();
        var source = new CancelAfterFirstReadStream(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, cancellation, canSeek: false);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => FileContentDetector.ProbeAsync(source,
            leaveOpen: false, cancellationToken: cancellation.Token));

        Assert.True(source.IsDisposed);
    }

    /// <summary>
    /// 不可定位的无限源只读取识别所需字节，头部预算到达时不会继续读取。
    /// </summary>
    [Fact]
    public void Probe_InfiniteSourceReadsAreBoundedBySignatureAndHeaderBudget()
    {
        using (var unrelated = new InfinitePatternStream(Encoding.ASCII.GetBytes("ordinary")))
        using (var probe = FileContentDetector.Probe(unrelated))
        {
            Assert.Equal(FileDetectionStatus.Unknown, probe.Result.Status);
            Assert.Equal(unrelated.BytesRead, probe.Result.BytesRead);
            Assert.InRange(unrelated.BytesRead, 1, 12);
        }

        var riffPrefix = new byte[] { 0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50 };
        using var limited = new InfinitePatternStream(riffPrefix);
        using var limitedProbe = FileContentDetector.Probe(limited, new FileDetectionOptions { MaxHeaderBytes = 5 });
        Assert.Equal(FileDetectionStatus.Incomplete, limitedProbe.Result.Status);
        Assert.Equal(FileDetectionReason.HeaderLimitReached, limitedProbe.Result.ReasonCode);
        Assert.Equal(5, limitedProbe.Result.BytesRead);
        Assert.Equal(5, limited.BytesRead);
    }

    /// <summary>
    /// 扩展名比较区分确定的一致、不一致与不支持的声明。
    /// </summary>
    [Fact]
    public void CompareExtension_ReturnsMatchMismatchOrIndeterminate()
    {
        var jpeg = FileContentDetector.Detect((byte[])HeaderCases().First(x => (string)x[0] == "jpeg")[1]);
        var png = FileContentDetector.Detect((byte[])HeaderCases().First(x => (string)x[0] == "png")[1]);
        var zip = FileContentDetector.Detect((byte[])HeaderCases().First(x => (string)x[0] == "zip")[1]);
        var unknown = FileContentDetector.Detect(Encoding.ASCII.GetBytes("unknown"));

        Assert.Equal(FileExtensionMatch.Match, FileContentDetector.CompareExtension(jpeg, "jpg"));
        Assert.Equal(FileExtensionMatch.Match, FileContentDetector.CompareExtension(jpeg, ".JPG"));
        Assert.Equal(FileExtensionMatch.Match, FileContentDetector.CompareExtension(png, ".png"));
        Assert.Equal(FileExtensionMatch.Mismatch, FileContentDetector.CompareExtension(png, "jpg"));
        Assert.Equal(FileExtensionMatch.Indeterminate, FileContentDetector.CompareExtension(png, null));
        Assert.Equal(FileExtensionMatch.Indeterminate, FileContentDetector.CompareExtension(png, string.Empty));
        Assert.Equal(FileExtensionMatch.Indeterminate, FileContentDetector.CompareExtension(png, "unknown-extension"));
        Assert.Equal(FileExtensionMatch.Indeterminate, FileContentDetector.CompareExtension(zip, ".docx"));
        Assert.Equal(FileExtensionMatch.Indeterminate, FileContentDetector.CompareExtension(unknown, ".jpg"));
    }

    /// <summary>
    /// 文件路径入口识别同步和异步样本，并在返回后释放文件句柄。
    /// </summary>
    [Fact]
    public async Task DetectFile_AndDetectFileAsyncRecognizeContentAndCloseFiles()
    {
        var directory = Path.Combine(Path.GetTempPath(), "bing-file-content-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var syncPath = Path.Combine(directory, "not-a-pdf-name.bin");
        var asyncPath = Path.Combine(directory, "not-a-gif-name.bin");
        try
        {
            File.WriteAllBytes(syncPath, Encoding.ASCII.GetBytes("%PDF-1.7"));
            File.WriteAllBytes(asyncPath, Encoding.ASCII.GetBytes("GIF89a"));

            AssertMatchedFormat(FileContentDetector.DetectFile(syncPath), "pdf");
            AssertCanOpenExclusively(syncPath);
            AssertMatchedFormat(await FileContentDetector.DetectFileAsync(asyncPath), "gif");
            AssertCanOpenExclusively(asyncPath);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// 检测结果提供稳定格式标识、MIME、扩展名集合和证据类型。
    /// </summary>
    [Fact]
    public void Detect_ResultExposesFormatMetadataAndSignatureEvidence()
    {
        var result = FileContentDetector.Detect(Encoding.ASCII.GetBytes("%PDF-1.7"));

        Assert.Equal(FileDetectionEvidence.Signature, result.Evidence);
        Assert.Equal(FileDetectionReason.None, result.ReasonCode);
        Assert.Equal("pdf", result.Format.Id);
        Assert.False(string.IsNullOrWhiteSpace(result.Format.MimeType));
        Assert.NotEmpty(result.Format.Extensions);
    }

    private static void AssertMatchedFormat(FileDetectionResult result, string expectedId)
    {
        Assert.Equal(FileDetectionStatus.Matched, result.Status);
        Assert.Equal(FileDetectionReason.None, result.ReasonCode);
        Assert.NotNull(result.Format);
        Assert.Equal(expectedId, result.Format.Id);
        Assert.False(string.IsNullOrWhiteSpace(result.Format.MimeType));
        Assert.NotEmpty(result.Format.Extensions);
        Assert.Equal(FileDetectionEvidence.Signature, result.Evidence);
        Assert.Contains(result.Candidates, candidate => candidate.Id == expectedId);
    }

    private static byte[] CreateZipSample()
    {
        using var destination = new MemoryStream();
        using (var archive = new ZipArchive(destination, ZipArchiveMode.Create, leaveOpen: true))
        using (var entry = archive.CreateEntry("sample.txt").Open())
        {
            var contents = Encoding.UTF8.GetBytes("zip sample");
            entry.Write(contents, 0, contents.Length);
        }
        return destination.ToArray();
    }

    private static byte[] CreateGzipSample()
    {
        using var destination = new MemoryStream();
        using (var gzip = new GZipStream(destination, CompressionMode.Compress, leaveOpen: true))
        {
            var contents = Encoding.UTF8.GetBytes("gzip sample");
            gzip.Write(contents, 0, contents.Length);
        }
        return destination.ToArray();
    }

    private static byte[] ReadAll(Stream stream)
    {
        using var destination = new MemoryStream();
        var buffer = new byte[3];
        int read;
        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
            destination.Write(buffer, 0, read);
        return destination.ToArray();
    }

    private static async Task<byte[]> ReadAllAsync(Stream stream)
    {
        using var destination = new MemoryStream();
        var buffer = new byte[3];
        int read;
        while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None)) > 0)
            await destination.WriteAsync(buffer, 0, read, CancellationToken.None);
        return destination.ToArray();
    }

    private static void AssertCanOpenExclusively(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        Assert.True(stream.CanRead);
    }

    /// <summary>
    /// 可配置短读、定位能力和异步读取计数的只读测试流。
    /// </summary>
    private class TrackingReadStream : MemoryStream
    {
        private readonly int _maxReadSize;
        private readonly bool _canSeek;

        public TrackingReadStream(byte[] bytes, int maxReadSize = int.MaxValue, bool canSeek = true)
            : base(bytes, writable: false)
        {
            _maxReadSize = maxReadSize;
            _canSeek = canSeek;
        }

        public int BytesRead { get; private set; }
        public int ReadCalls { get; private set; }
        public int AsyncReadCalls { get; protected set; }
        public bool IsDisposed { get; private set; }

        public override bool CanSeek => _canSeek && base.CanSeek;
        public override long Length => CanSeek ? base.Length : throw new NotSupportedException();
        public override long Position
        {
            get => CanSeek ? base.Position : throw new NotSupportedException();
            set
            {
                if (!CanSeek) throw new NotSupportedException();
                base.Position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ReadCalls++;
            var read = base.Read(buffer, offset, Math.Min(count, _maxReadSize));
            BytesRead += read;
            return read;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AsyncReadCalls++;
            return Task.FromResult(Read(buffer, offset, count));
        }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// 在读取一个字节推进位置后抛出异常，用于验证失败路径恢复和资源清理。
    /// </summary>
    private sealed class ThrowAfterFirstReadStream : TrackingReadStream
    {
        private bool _hasThrown;

        public ThrowAfterFirstReadStream(byte[] bytes) : base(bytes, maxReadSize: 1)
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_hasThrown)
                throw new IOException("Injected read failure.");
            var read = base.Read(buffer, offset, count);
            _hasThrown = true;
            if (read > 0)
                throw new IOException("Injected read failure after consuming a byte.");
            return read;
        }
    }

    /// <summary>
    /// 在首次异步读取后取消令牌，用于验证中途取消。
    /// </summary>
    private sealed class CancelAfterFirstReadStream : TrackingReadStream
    {
        private readonly CancellationTokenSource _cancellation;
        private bool _hasCanceled;

        public CancelAfterFirstReadStream(byte[] bytes, CancellationTokenSource cancellation, bool canSeek = true)
            : base(bytes, maxReadSize: 1, canSeek: canSeek)
        {
            _cancellation = cancellation;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AsyncReadCalls++;
            var read = base.Read(buffer, offset, count);
            if (!_hasCanceled && read > 0)
            {
                _hasCanceled = true;
                _cancellation.Cancel();
            }
            return Task.FromResult(read);
        }
    }

    /// <summary>
    /// 按固定模式持续生成数据，供无限源和预算测试使用。
    /// </summary>
    private sealed class InfinitePatternStream : Stream
    {
        private readonly byte[] _pattern;
        private long _position;

        public InfinitePatternStream(byte[] pattern) => _pattern = pattern;

        public int BytesRead { get; private set; }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            for (var index = 0; index < count; index++)
                buffer[offset + index] = _pattern[(int)((_position + index) % _pattern.Length)];
            _position += count;
            BytesRead += count;
            return count;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Read(buffer, offset, count));
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Flush() { }
    }
}
