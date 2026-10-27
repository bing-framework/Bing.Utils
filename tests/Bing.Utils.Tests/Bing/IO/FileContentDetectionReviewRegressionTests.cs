using System.IO.Compression;
using System.Security;
using System.Text;
using System.Threading;
using Bing.IO;

namespace Bing.Utils.Tests.Bing.IO;

/// <summary>验证审查发现的主部件、URI、目录候选及资源诊断问题。</summary>
public class FileContentDetectionReviewRegressionTests
{
    private const string MainType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml";
    private static FileDetectionOptions Deep() => new FileDetectionOptions { InspectContainers = true };

    /// <summary>中央目录里存在名称，不足以证明正文主部件拥有可读取的本地记录。</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(8)]
    [InlineData(14)]
    [InlineData(18)]
    [InlineData(22)]
    [InlineData(30)]
    public async Task Detect_RejectsInconsistentMainLocalRecord(int field)
    {
        var data = Package();
        var local = LocalOffset(data, "word/document.xml");
        data[local + field] ^= 1;
        await AssertInvalidAcrossInputs(data);
    }

    /// <summary>即使中央目录和本地头共同伪造长度，也不能把主部件范围延伸到目录之后。</summary>
    [Fact]
    public async Task Detect_RejectsMainPartOutsidePhysicalRange()
    {
        var data = Package();
        var central = CentralOffset(data, "word/document.xml");
        var local = LocalOffset(data, "word/document.xml");
        Write32(data, central + 20, 100000);
        Write32(data, central + 24, 100000);
        Write32(data, local + 18, 100000);
        Write32(data, local + 22, 100000);
        await AssertInvalidAcrossInputs(data);
    }

    /// <summary>ZIP64 可以在传统尾记录保留真实小计数，扩展窗口仍须检查 locator。</summary>
    [Fact]
    public async Task Detect_Zip64LocatorWithoutSentinelsIsUnsupported()
    {
        var original = Package();
        var end = original.Length - 22;
        var data = new byte[original.Length + 76];
        Array.Copy(original, data, end);
        Array.Copy(original, end, data, end + 76, 22);
        Write32(data, end, 0x06064b50);
        Write32(data, end + 4, 44);
        Write16(data, end + 12, 45);
        Write16(data, end + 14, 45);
        Write32(data, end + 24, 3);
        Write32(data, end + 32, 3);
        Write32(data, end + 40, Read32(original, end + 12));
        Write32(data, end + 48, Read32(original, end + 16));
        Write32(data, end + 56, 0x07064b50);
        Write32(data, end + 64, (uint)end);
        Write32(data, end + 72, 1);
        var result = FileContentDetector.Detect(data, Deep());
        Assert.Equal(FileDetectionReason.UnsupportedContainer, result.ReasonCode);
        using var source = new MemoryStream(data, false);
        Assert.Equal(result.ReasonCode, (await FileContentDetector.DetectAsync(source, Deep())).ReasonCode);
    }

    /// <summary>清单和关系部件不能通过伪造 ContentType 冒充主文档。</summary>
    [Theory]
    [InlineData("[Content_Types].xml")]
    [InlineData("_rels/.rels")]
    [InlineData("word/_rels/document.xml.rels")]
    public async Task Detect_RejectsReservedMainParts(string part)
        => await AssertInvalidAcrossInputs(Package(part, includeMain: part.StartsWith("word/", StringComparison.Ordinal)));

    /// <summary>PartName 必须是规范部件名，不能通过点段或转义别名与 ZIP 条目匹配。</summary>
    [Theory]
    [InlineData("/word/../word/document.xml")]
    [InlineData("/word/./document.xml")]
    [InlineData("/word/%64ocument.xml")]
    [InlineData("/word%2Fdocument.xml")]
    [InlineData("/word/document%ZZ.xml")]
    [InlineData("/word/document.xml.")]
    public async Task Detect_RejectsInvalidOverridePartNames(string partName)
        => await AssertInvalidAcrossInputs(Package(manifest: Override(partName)));

    /// <summary>关系 URI 中的 scheme、authority、片段及越根路径不是包内部件。</summary>
    [Theory]
    [InlineData("https://host/word/document.xml")]
    [InlineData("//host/word/document.xml")]
    [InlineData("../word/document.xml")]
    [InlineData("word/document.xml#fragment")]
    [InlineData("word/document.xml?query")]
    [InlineData("word%5Cdocument.xml")]
    [InlineData("word/document.xml/.")]
    [InlineData("word/document.xml/child/..")]
    public async Task Detect_RejectsInvalidTargets(string target)
        => await AssertInvalidAcrossInputs(Package(target: target));

    /// <summary>冒号是后续路径段中合法的 URI 字符，根关系仍允许解析正常点段。</summary>
    [Theory]
    [InlineData("custom/folder:part/document.xml", "custom/folder:part/document.xml")]
    [InlineData("custom/folder:part/document.xml", "/custom/folder:part/document.xml")]
    [InlineData("word/document.xml", "./word/parts/../document.xml")]
    [InlineData("word/Document.xml", "WORD/document.XML")]
    public async Task Detect_AcceptsValidTargets(string part, string target)
    {
        var data = Package(part, target);
        var result = FileContentDetector.Detect(data, Deep());
        Assert.Same(FileContentFormats.Docx, result.Format);
        using var source = new MemoryStream(data, false);
        Assert.Same(result.Format, (await FileContentDetector.DetectAsync(source, Deep())).Format);
        Assert.Equal(0, source.Position);
    }

    /// <summary>Unicode 大小写折叠不能把真实不同的部件名称视为同一目标。</summary>
    [Fact]
    public async Task Detect_DoesNotFoldNonAsciiPartNames()
    {
        const string actual = "word/doc\u00b5.xml";
        const string declared = "word/doc\u039c.xml";
        await AssertInvalidAcrossInputs(Package(actual, declared, Override("/" + declared)));
    }

    /// <summary>Default 的扩展名不能包含路径；父目录中的点不属于文件名。</summary>
    [Theory]
    [InlineData("dir/document")]
    [InlineData("xml/")]
    [InlineData(".xml")]
    [InlineData("x%6Dl")]
    public async Task Detect_RejectsInvalidDefaultExtension(string extension)
        => await AssertInvalidAcrossInputs(Package("word.dir/document", manifest: $"<Default Extension=\"{extension}\" ContentType=\"{MainType}\"/>"));

    [Fact]
    public async Task Detect_DirectoryDotDoesNotSupplyExtension()
        => await AssertInvalidAcrossInputs(Package("word.xml/document", manifest: $"<Default Extension=\"xml\" ContentType=\"{MainType}\"/>"));

    /// <summary>默认扩展名匹配只看最后一个路径段，并忽略扩展名大小写。</summary>
    [Fact]
    public void Detect_AcceptsValidDefaultExtension()
    {
        var data = Package("word.dir/document.XML", manifest: $"<Default Extension=\"xml\" ContentType=\"{MainType}\"/>");
        Assert.Same(FileContentFormats.Docx, FileContentDetector.Detect(data, Deep()).Format);
    }

    /// <summary>注释内候选无效时继续找真实目录，包含只有目录内容验证才能排除的伪候选。</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Detect_IgnoresInvalidEocdInsideComment(int variant)
    {
        var original = Package();
        var data = AddFakeComment(original, variant);
        var result = FileContentDetector.Detect(data, Deep());
        Assert.Same(FileContentFormats.Docx, result.Format);
        using var source = new CountingStream(data);
        var streamed = await FileContentDetector.DetectAsync(source, Deep());
        Assert.Same(result.Format, streamed.Format);
        Assert.Equal(result.ContainerBytesRead, streamed.ContainerBytesRead);
        Assert.Equal(streamed.BytesRead + streamed.ContainerBytesRead, source.TotalRead);
    }

    /// <summary>如果注释故意携带另一个完整有效空目录，使用距离文件尾最近的有效候选。</summary>
    [Fact]
    public void Detect_NearestValidDirectoryHasDocumentedPrecedence()
    {
        var data = AddFakeComment(Package(), 3);
        var result = FileContentDetector.Detect(data, Deep());
        Assert.Same(FileContentFormats.Zip, result.Format);
        Assert.Equal(FileDetectionEvidence.ContainerStructure, result.Evidence);
    }

    /// <summary>没有注释的大 ZIP 不再固定读取 64 KiB 尾窗口，也不解压主部件。</summary>
    [Fact]
    public async Task Detect_QuickTailReadsOnlyDirectoryAndRequiredHeaders()
    {
        var data = Package(payload: new byte[512 * 1024]);
        using var source = new CountingStream(data);
        var result = await FileContentDetector.DetectAsync(source, Deep());
        Assert.Same(FileContentFormats.Docx, result.Format);
        Assert.InRange(source.TotalRead, 1, 4096);
        Assert.Equal(result.BytesRead + result.ContainerBytesRead, source.TotalRead);
        Assert.Equal(FileDetectionLimitKind.None, result.LimitKind);
    }

    /// <summary>调用方能直接知道命中哪一种预算，无须猜测 ContainerLimitReached。</summary>
    [Fact]
    public void Detect_ReportsSpecificBudgetKind()
    {
        var data = Package();
        var cases = new[]
        {
            (new FileDetectionOptions { InspectContainers = true, MaxContainerBytesRead = 21 }, FileDetectionLimitKind.ContainerBytesRead),
            (new FileDetectionOptions { InspectContainers = true, MaxZipDirectoryBytes = 1 }, FileDetectionLimitKind.ZipDirectoryBytes),
            (new FileDetectionOptions { InspectContainers = true, MaxZipEntries = 1 }, FileDetectionLimitKind.ZipEntries),
            (new FileDetectionOptions { InspectContainers = true, MaxMetadataBytes = 1 }, FileDetectionLimitKind.MetadataBytes)
        };
        foreach (var item in cases)
        {
            var result = FileContentDetector.Detect(data, item.Item1);
            Assert.Equal(FileDetectionReason.ContainerLimitReached, result.ReasonCode);
            Assert.Equal(item.Item2, result.LimitKind);
        }
        var header = FileContentDetector.Detect(data, new FileDetectionOptions { MaxHeaderBytes = 1 });
        Assert.Equal(FileDetectionLimitKind.HeaderBytes, header.LimitKind);
    }

    [Theory]
    [InlineData("report.DOCX", FileExtensionMatch.Match)]
    [InlineData(@"C:\a.pdf\report.docx", FileExtensionMatch.Match)]
    [InlineData("/a.pdf/report", FileExtensionMatch.Indeterminate)]
    [InlineData("report.pdf", FileExtensionMatch.Mismatch)]
    [InlineData(".docx", FileExtensionMatch.Indeterminate)]
    [InlineData("report.", FileExtensionMatch.Indeterminate)]
    [InlineData(null, FileExtensionMatch.Indeterminate)]
    public void CompareFileName_UsesFinalPathSegment(string name, FileExtensionMatch expected)
        => Assert.Equal(expected, FileContentDetector.CompareFileName(FileContentDetector.Detect(Package(), Deep()), name));

    /// <summary>目录和检测结果共享不可变格式实例，调用方可以使用强类型常量。</summary>
    [Fact]
    public void SupportedFormats_IsReadOnlyAndUsesSharedInstances()
    {
        Assert.Equal(21, FileContentDetector.SupportedFormats.Count);
        Assert.Equal(21, FileContentDetector.SupportedFormats.Select(x => x.Id).Distinct().Count());
        Assert.Contains(FileContentFormats.Docx, FileContentDetector.SupportedFormats);
        Assert.Same(FileContentFormats.Pdf, FileContentDetector.Detect(Encoding.ASCII.GetBytes("%PDF-1.7")).Format);
        Assert.Throws<NotSupportedException>(() => ((IList<FileContentFormat>)FileContentDetector.SupportedFormats).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<string>)FileContentFormats.Docx.Extensions).Clear());
    }

    private static async Task AssertInvalidAcrossInputs(byte[] data)
    {
        var result = FileContentDetector.Detect(data, Deep());
        Assert.Equal(FileDetectionReason.InvalidContainer, result.ReasonCode);
        Assert.Equal(FileDetectionStatus.Incomplete, result.Status);
        Assert.Equal(FileDetectionLimitKind.None, result.LimitKind);
        using var source = new MemoryStream(data, false);
        var sync = FileContentDetector.Detect(source, Deep());
        var asyncResult = await FileContentDetector.DetectAsync(source, Deep());
        Assert.Equal(result.ReasonCode, sync.ReasonCode);
        Assert.Equal(result.ReasonCode, asyncResult.ReasonCode);
        Assert.Equal(result.ContainerBytesRead, sync.ContainerBytesRead);
        Assert.Equal(result.ContainerBytesRead, asyncResult.ContainerBytesRead);
        Assert.Equal(0, source.Position);
    }

    private static string Override(string partName) => $"<Override PartName=\"{SecurityElement.Escape(partName)}\" ContentType=\"{MainType}\"/>";

    private static byte[] Package(string part = "word/document.xml", string target = null, string manifest = null,
        bool includeMain = true, byte[] payload = null)
    {
        using var output = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            void Add(string name, byte[] bytes)
            {
                using var entry = zip.CreateEntry(name, CompressionLevel.NoCompression).Open();
                entry.Write(bytes, 0, bytes.Length);
            }
            Add("[Content_Types].xml", Encoding.UTF8.GetBytes($"<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">{manifest ?? Override("/" + part)}</Types>"));
            Add("_rels/.rels", Encoding.UTF8.GetBytes($"<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"r1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"{SecurityElement.Escape(target ?? part)}\"/></Relationships>"));
            if (includeMain) Add(part, payload ?? Encoding.UTF8.GetBytes("<document/>"));
        }
        return output.ToArray();
    }

    private static int LocalOffset(byte[] data, string part) => (int)Read32(data, CentralOffset(data, part) + 42);

    private static int CentralOffset(byte[] data, string part)
    {
        var position = (int)Read32(data, data.Length - 22 + 16);
        while (Read32(data, position) == 0x02014b50)
        {
            var nameLength = Read16(data, position + 28);
            if (Encoding.UTF8.GetString(data, position + 46, nameLength) == part) return position;
            position += 46 + nameLength + Read16(data, position + 30) + Read16(data, position + 32);
        }
        throw new InvalidOperationException("未找到测试主部件。");
    }

    private static byte[] AddFakeComment(byte[] original, int variant)
    {
        var data = new byte[original.Length + 22];
        Array.Copy(original, data, original.Length);
        Write16(data, original.Length - 2, 22);
        var fake = original.Length;
        Write32(data, fake, 0x06054b50);
        if (variant == 1) Write16(data, fake + 4, 1); // 不支持的分卷伪标记。
        if (variant == 2)
        {
            Write32(data, fake + 16, (uint)(fake - 46));
            Write32(data, fake + 12, 46); // 范围正确，但目录内容无效。
            Write16(data, fake + 8, 1);
            Write16(data, fake + 10, 1);
        }
        if (variant == 3) Write32(data, fake + 16, (uint)fake); // 完整有效的空目录。
        return data;
    }

    private static int Read16(byte[] data, int offset) => data[offset] | data[offset + 1] << 8;
    private static uint Read32(byte[] data, int offset) => (uint)(Read16(data, offset) | Read16(data, offset + 2) << 16);
    private static void Write16(byte[] data, int offset, int value) { data[offset] = (byte)value; data[offset + 1] = (byte)(value >> 8); }
    private static void Write32(byte[] data, int offset, uint value) { Write16(data, offset, (int)value); Write16(data, offset + 2, (int)(value >> 16)); }

    private sealed class CountingStream : MemoryStream
    {
        internal long TotalRead { get; private set; }
        internal CountingStream(byte[] data) : base(data, false) { }
        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = base.Read(buffer, offset, count);
            TotalRead += read;
            return read;
        }
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult(Read(buffer, offset, count));
        }
    }
}
