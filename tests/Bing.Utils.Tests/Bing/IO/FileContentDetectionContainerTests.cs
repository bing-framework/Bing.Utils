using System.IO.Compression;
using System.Text;
using System.Threading;
using Bing.IO;

namespace Bing.Utils.Tests.Bing.IO;

/// <summary>
/// 验证显式容器检查、Office 主部件识别及 ZIP 资源预算。
/// </summary>
[Trait("Bing.IO", "FileContentDetectionContainers")]
public class FileContentDetectionContainerTests
{
    private const string ContentTypesNamespace = "http://schemas.openxmlformats.org/package/2006/content-types";
    private const string RelationshipsNamespace = "http://schemas.openxmlformats.org/package/2006/relationships";
    private const string TransitionalOfficeDocumentRelationship = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument";
    private const string StrictOfficeDocumentRelationship = "http://purl.oclc.org/ooxml/officeDocument/relationships/officeDocument";

    /// <summary>
    /// 提供六种 Office 主文档部件类型及默认路径。
    /// </summary>
    public static IEnumerable<object[]> OfficeCases()
    {
        yield return new object[] { "docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml", "word/document.xml", "word", "<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"/>" };
        yield return new object[] { "docm", "application/vnd.ms-word.document.macroEnabled.main+xml", "word/document.xml", "word", "<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"/>" };
        yield return new object[] { "xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml", "xl/workbook.xml", "xl", "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"/>" };
        yield return new object[] { "xlsm", "application/vnd.ms-excel.sheet.macroEnabled.main+xml", "xl/workbook.xml", "xl", "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"/>" };
        yield return new object[] { "pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation.main+xml", "ppt/presentation.xml", "ppt", "<p:presentation xmlns:p=\"http://schemas.openxmlformats.org/presentationml/2006/main\"/>" };
        yield return new object[] { "pptm", "application/vnd.ms-powerpoint.presentation.macroEnabled.main+xml", "ppt/presentation.xml", "ppt", "<p:presentation xmlns:p=\"http://schemas.openxmlformats.org/presentationml/2006/main\"/>" };
    }

    /// <summary>
    /// 显式检查真实 ZIP 包中的主部件类型、关系和部件均匹配对应 Office 格式。
    /// </summary>
    [Theory]
    [MemberData(nameof(OfficeCases))]
    public void Detect_InspectContainersRecognizesOfficeMainPart(string expectedId, string contentType,
        string partName, string rootDirectory, string mainPartXml)
    {
        var package = CreateOfficePackage(contentType, partName, mainPartXml);
        var options = new FileDetectionOptions { InspectContainers = true };

        var result = FileContentDetector.Detect(package, options);

        AssertOfficeResult(result, expectedId);
        Assert.Contains(result.Format.Extensions, extension => string.Equals(extension, expectedId, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(FileExtensionMatch.Match, FileContentDetector.CompareExtension(result, "." + expectedId));
        Assert.Equal(FileExtensionMatch.Mismatch, FileContentDetector.CompareExtension(result, rootDirectory == "word" ? ".xlsx" : ".docx"));
    }

    /// <summary>
    /// Strict 关系类型和非标准主部件路径仍可通过实际包关系识别。
    /// </summary>
    [Fact]
    public void Detect_OfficePackageSupportsStrictRelationshipAndCustomMainPartPath()
    {
        const string partName = "custom/parts/primary-document.xml";
        const string contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml";
        const string mainPartXml = "<w:document xmlns:w=\"http://purl.oclc.org/ooxml/wordprocessingml/main\"/>";
        var package = CreateOfficePackage(contentType, partName, mainPartXml,
            relationshipType: StrictOfficeDocumentRelationship);

        var result = FileContentDetector.Detect(package, new FileDetectionOptions { InspectContainers = true });

        AssertOfficeResult(result, "docx");
    }

    /// <summary>
    /// OPC 元数据以 Store 或 Deflate 压缩时均可识别。
    /// </summary>
    [Theory]
    [InlineData(CompressionLevel.NoCompression)]
    [InlineData(CompressionLevel.Optimal)]
    public void Detect_OfficePackageSupportsStoreAndDeflateMetadata(CompressionLevel compressionLevel)
    {
        var package = CreateOfficePackage(
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml",
            "word/document.xml", "<w:document/>", compressionLevel: compressionLevel);

        var result = FileContentDetector.Detect(package, new FileDetectionOptions { InspectContainers = true });

        AssertOfficeResult(result, "docx");
    }

    /// <summary>
    /// 按宏文档主部件 ContentType 识别格式，不要求包内实际含有 VBA 部件。
    /// </summary>
    [Theory]
    [InlineData("docm", "application/vnd.ms-word.document.macroEnabled.main+xml", "word/document.xml")]
    [InlineData("xlsm", "application/vnd.ms-excel.sheet.macroEnabled.main+xml", "xl/workbook.xml")]
    [InlineData("pptm", "application/vnd.ms-powerpoint.presentation.macroEnabled.main+xml", "ppt/presentation.xml")]
    public void Detect_MacroFormatUsesMainPartContentTypeWithoutVbaPart(string expectedId, string contentType, string partName)
    {
        var package = CreateOfficePackage(contentType, partName, "<main/>");

        var result = FileContentDetector.Detect(package, new FileDetectionOptions { InspectContainers = true });

        AssertOfficeResult(result, expectedId);
    }

    /// <summary>
    /// 普通 ZIP 和由 ZipArchive 生成的空 ZIP 在深层检查中保留为通用 ZIP 容器。
    /// </summary>
    [Fact]
    public void Detect_InspectContainersKeepsOrdinaryAndEmptyZipAsGenericContainers()
    {
        var ordinaryZip = CreateZip(("data/readme.txt", "plain archive"));
        var emptyZip = CreateZip();
        var options = new FileDetectionOptions { InspectContainers = true };

        var ordinary = FileContentDetector.Detect(ordinaryZip, options);
        var empty = FileContentDetector.Detect(emptyZip, options);

        AssertZipContainer(ordinary, FileDetectionEvidence.ContainerStructure);
        AssertZipContainer(empty, FileDetectionEvidence.ContainerStructure);
        Assert.Equal(FileExtensionMatch.Match, FileContentDetector.CompareExtension(ordinary, ".zip"));
    }

    /// <summary>
    /// 默认浅检测不读取容器目录，旧 ZIP 与 Office 扩展名比较仍保持不确定。
    /// </summary>
    [Fact]
    public void Detect_DefaultKeepsShallowZipBehaviorAndExtensionComparison()
    {
        var package = CreateOfficePackage(
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml",
            "word/document.xml", "<w:document/>");
        var defaults = new FileDetectionOptions();

        var shallow = FileContentDetector.Detect(package);

        Assert.Equal(4 * 1024 * 1024, defaults.MaxContainerBytesRead);
        Assert.Equal(1 * 1024 * 1024, defaults.MaxZipDirectoryBytes);
        Assert.Equal(4096, defaults.MaxZipEntries);
        Assert.Equal(1 * 1024 * 1024, defaults.MaxMetadataBytes);
        Assert.False(defaults.InspectContainers);
        Assert.Equal(FileDetectionStatus.Matched, shallow.Status);
        Assert.Equal("zip", shallow.Format.Id);
        Assert.Equal(FileDetectionEvidence.Signature, shallow.Evidence);
        Assert.Equal(0L, shallow.ContainerBytesRead);
        Assert.Equal(FileExtensionMatch.Indeterminate, FileContentDetector.CompareExtension(shallow, ".docx"));

        var deepOffice = FileContentDetector.Detect(package, new FileDetectionOptions { InspectContainers = true });
        var deepGenericZip = FileContentDetector.Detect(CreateZip(("readme.txt", "not an Office package")),
            new FileDetectionOptions { InspectContainers = true });

        AssertOfficeResult(deepOffice, "docx");
        Assert.Equal(FileExtensionMatch.Match, FileContentDetector.CompareExtension(deepOffice, "docx"));
        AssertZipContainer(deepGenericZip, FileDetectionEvidence.ContainerStructure);
        Assert.Equal(FileExtensionMatch.Mismatch, FileContentDetector.CompareExtension(deepGenericZip, ".docx"));
    }

    /// <summary>
    /// DTD、重复的包清单和缺失的主部件均作为无效 ZIP 容器报告。
    /// </summary>
    [Fact]
    public void Detect_InvalidOfficePackagesRetainZipSignatureEvidence()
    {
        const string contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml";
        const string partName = "word/document.xml";
        var dtdContentTypes = "<!DOCTYPE Types [<!ENTITY mainType \"" + contentType + "\">]>" +
            "<Types xmlns=\"" + ContentTypesNamespace + "\"><Override PartName=\"/" + partName +
            "\" ContentType=\"&mainType;\"/></Types>";
        var withDtd = CreateOfficePackage(contentType, partName, "<w:document/>", contentTypesXml: dtdContentTypes);
        var duplicateManifest = CreateOfficePackage(contentType, partName, "<w:document/>", duplicateManifest: true);
        var missingMainPart = CreateOfficePackage(contentType, partName, "<w:document/>", includeMainPart: false);

        AssertIncompleteZip(FileContentDetector.Detect(withDtd, new FileDetectionOptions { InspectContainers = true }), FileDetectionReason.InvalidContainer);
        AssertIncompleteZip(FileContentDetector.Detect(duplicateManifest, new FileDetectionOptions { InspectContainers = true }), FileDetectionReason.InvalidContainer);
        AssertIncompleteZip(FileContentDetector.Detect(missingMainPart, new FileDetectionOptions { InspectContainers = true }), FileDetectionReason.InvalidContainer);
    }

    /// <summary>
    /// ZIP 目录、条目、容器总读取量及元数据解压量分别受预算限制。
    /// </summary>
    [Fact]
    public void Detect_ContainerBudgetsStopDirectoryEntriesReadsAndMetadata()
    {
        const string contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml";
        const string partName = "word/document.xml";
        var package = CreateOfficePackage(contentType, partName, "<w:document/>", extraEntries: 2);

        var containerLimited = FileContentDetector.Detect(package,
            new FileDetectionOptions { InspectContainers = true, MaxContainerBytesRead = 32 });
        var directoryLimited = FileContentDetector.Detect(package,
            new FileDetectionOptions { InspectContainers = true, MaxZipDirectoryBytes = 8 });
        var entriesLimited = FileContentDetector.Detect(package,
            new FileDetectionOptions { InspectContainers = true, MaxZipEntries = 2 });
        var metadataLimited = FileContentDetector.Detect(package,
            new FileDetectionOptions { InspectContainers = true, MaxMetadataBytes = 32 });

        AssertIncompleteZip(containerLimited, FileDetectionReason.ContainerLimitReached);
        Assert.InRange(containerLimited.ContainerBytesRead, 0, 32);
        AssertIncompleteZip(directoryLimited, FileDetectionReason.ContainerLimitReached);
        AssertIncompleteZip(entriesLimited, FileDetectionReason.ContainerLimitReached);
        AssertIncompleteZip(metadataLimited, FileDetectionReason.ContainerLimitReached);
    }

    /// <summary>
    /// 所有 ZIP 深层预算都必须为正数。
    /// </summary>
    [Fact]
    public void Detect_RejectsNonPositiveContainerBudgets()
    {
        var package = CreateZip(("entry.txt", "data"));

        Assert.Throws<ArgumentOutOfRangeException>(() => FileContentDetector.Detect(package,
            new FileDetectionOptions { InspectContainers = true, MaxContainerBytesRead = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => FileContentDetector.Detect(package,
            new FileDetectionOptions { InspectContainers = true, MaxZipDirectoryBytes = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => FileContentDetector.Detect(package,
            new FileDetectionOptions { InspectContainers = true, MaxZipEntries = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => FileContentDetector.Detect(package,
            new FileDetectionOptions { InspectContainers = true, MaxMetadataBytes = 0 }));
    }

    /// <summary>
    /// 检测带非零当前位置的 ZIP 时以当前位置为归档起点，并恢复原位置。
    /// </summary>
    [Fact]
    public void Detect_DeepInspectionFromNonzeroPositionRestoresSourcePosition()
    {
        var package = CreateOfficePackage(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml",
            "xl/workbook.xml", "<workbook/>");
        var prefix = new byte[] { 0x10, 0x20, 0x30, 0x40, 0x50 };
        using var source = new MemoryStream(prefix.Concat(package).ToArray(), writable: false);
        source.Position = prefix.Length;

        var result = FileContentDetector.Detect(source, new FileDetectionOptions { InspectContainers = true });

        AssertOfficeResult(result, "xlsx");
        Assert.Equal(prefix.Length, source.Position);
        Assert.True(result.ContainerBytesRead > 0);
    }

    /// <summary>
    /// 深层检查期间源读取抛错时传播原异常并恢复调用前的位置。
    /// </summary>
    [Fact]
    public void Detect_DeepInspectionReadFailureRestoresSourcePosition()
    {
        var package = CreateOfficePackage(
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml",
            "word/document.xml", "<w:document/>");
        var prefix = new byte[] { 0x10, 0x20, 0x30 };
        using var source = new ThrowAfterReadStream(prefix.Concat(package).ToArray(), successfulReads: 1);
        source.Position = prefix.Length;

        Assert.Throws<IOException>(() => FileContentDetector.Detect(source,
            new FileDetectionOptions { InspectContainers = true }));

        Assert.Equal(prefix.Length, source.Position);
        Assert.True(source.CanRead);
    }

    /// <summary>
    /// 可定位异步 Probe 深层识别后恢复到前缀末尾，并可重放整个 ZIP。
    /// </summary>
    [Fact]
    public async Task ProbeAsync_SeekableOfficePackageRestoresPrefixPositionAndReplaysFullPackage()
    {
        var package = CreateOfficePackage(
            "application/vnd.openxmlformats-officedocument.presentationml.presentation.main+xml",
            "ppt/presentation.xml", "<p:presentation/>");
        using var source = new MemoryStream(package, writable: false);

        using var probe = await FileContentDetector.ProbeAsync(source,
            new FileDetectionOptions { InspectContainers = true });

        AssertOfficeResult(probe.Result, "pptx");
        Assert.Equal(probe.Result.BytesRead, source.Position);
        Assert.Equal(package, await ReadAllAsync(probe.Content));
    }

    /// <summary>
    /// 不可定位 ZIP 无法读取中央目录时返回原因，同时仍可从 Content 重放全部数据。
    /// </summary>
    [Fact]
    public void Probe_NonSeekableZipReportsRequirementAndPreservesFullContent()
    {
        var package = CreateOfficePackage(
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml",
            "word/document.xml", "<w:document/>");
        using var source = new NonSeekableStream(package);

        using var probe = FileContentDetector.Probe(source, new FileDetectionOptions { InspectContainers = true });

        AssertIncompleteZip(probe.Result, FileDetectionReason.SeekableSourceRequired);
        Assert.Equal(package, ReadAll(probe.Content));
        Assert.True(source.CanRead);
    }

    /// <summary>
    /// ZIP64、分卷标记、加密元数据和不支持的压缩方法均明确报告 UnsupportedContainer。
    /// </summary>
    [Fact]
    public void Detect_UnsupportedZipVariantsAreNotMisidentifiedAsOffice()
    {
        var zip64 = ConvertToZip64Directory(CreateZip(("data.txt", "zip64 marker")));
        var split = MarkZipAsSplit(CreateZip(("data.txt", "split marker")));
        var encryptedMetadata = SetZipEntryFlags(
            CreateOfficePackage("application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml",
                "word/document.xml", "<w:document/>"), "[Content_Types].xml", 0x0001);
        var unsupportedCompression = SetZipEntryCompressionMethod(
            CreateOfficePackage("application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml",
                "word/document.xml", "<w:document/>"), "[Content_Types].xml", 99);
        var options = new FileDetectionOptions { InspectContainers = true };

        AssertIncompleteZip(FileContentDetector.Detect(zip64, options), FileDetectionReason.UnsupportedContainer);
        AssertIncompleteZip(FileContentDetector.Detect(split, options), FileDetectionReason.UnsupportedContainer);
        AssertIncompleteZip(FileContentDetector.Detect(encryptedMetadata, options), FileDetectionReason.UnsupportedContainer);
        AssertIncompleteZip(FileContentDetector.Detect(unsupportedCompression, options), FileDetectionReason.UnsupportedContainer);
    }

    private static void AssertOfficeResult(FileDetectionResult result, string expectedId)
    {
        Assert.Equal(FileDetectionStatus.Matched, result.Status);
        Assert.Equal(FileDetectionReason.None, result.ReasonCode);
        Assert.Equal(FileDetectionEvidence.ContainerStructure, result.Evidence);
        Assert.NotNull(result.Format);
        Assert.Equal(expectedId, result.Format.Id);
        Assert.False(result.Format.IsContainer);
        Assert.Equal("zip", result.ContainerFormat?.Id);
        Assert.Contains(result.Candidates, candidate => candidate.Id == expectedId);
        Assert.True(result.BytesRead > 0);
        Assert.True(result.ContainerBytesRead > 0);
    }

    private static void AssertZipContainer(FileDetectionResult result, FileDetectionEvidence evidence)
    {
        Assert.Equal(FileDetectionStatus.Matched, result.Status);
        Assert.Equal(FileDetectionReason.None, result.ReasonCode);
        Assert.Equal(evidence, result.Evidence);
        Assert.Equal("zip", result.Format?.Id);
        Assert.True(result.Format.IsContainer);
        Assert.Equal("zip", result.ContainerFormat?.Id);
        Assert.Contains(result.Candidates, candidate => candidate.Id == "zip");
        Assert.True(result.BytesRead > 0);
    }

    private static void AssertIncompleteZip(FileDetectionResult result, FileDetectionReason expectedReason)
    {
        Assert.Equal(FileDetectionStatus.Incomplete, result.Status);
        Assert.Equal(expectedReason, result.ReasonCode);
        Assert.Null(result.Format);
        Assert.Equal("zip", result.ContainerFormat?.Id);
        Assert.Equal(FileDetectionEvidence.Signature, result.Evidence);
        Assert.Contains(result.Candidates, candidate => candidate.Id == "zip");
        Assert.True(result.BytesRead > 0);
    }

    private static byte[] CreateOfficePackage(string contentType, string partName, string mainPartXml,
        string relationshipType = TransitionalOfficeDocumentRelationship, bool includeMainPart = true,
        bool duplicateManifest = false, int extraEntries = 0,
        string contentTypesXml = null, CompressionLevel compressionLevel = CompressionLevel.Optimal)
    {
        var entries = new List<(string Name, string Content)>
        {
            ("[Content_Types].xml", contentTypesXml ?? CreateContentTypesXml(partName, contentType)),
            ("_rels/.rels", CreateRelationshipsXml(partName, relationshipType))
        };
        if (duplicateManifest)
            entries.Add(("[Content_Types].xml", CreateContentTypesXml(partName, contentType)));
        if (includeMainPart)
            entries.Add((partName.TrimStart('/'), mainPartXml));
        for (var index = 0; index < extraEntries; index++)
            entries.Add(($"extra/entry-{index}.txt", "extra"));
        return CreateZip(entries.ToArray(), compressionLevel);
    }

    private static string CreateContentTypesXml(string partName, string contentType)
        => "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
           "<Types xmlns=\"" + ContentTypesNamespace + "\">" +
           "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
           "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
           "<Override PartName=\"/" + partName.TrimStart('/') + "\" ContentType=\"" + contentType + "\"/>" +
           "</Types>";

    private static string CreateRelationshipsXml(string partName, string relationshipType)
        => "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
           "<Relationships xmlns=\"" + RelationshipsNamespace + "\">" +
           "<Relationship Id=\"rId1\" Type=\"" + relationshipType + "\" Target=\"" + partName.TrimStart('/') + "\"/>" +
           "</Relationships>";

    private static byte[] CreateZip(params (string Name, string Content)[] entries)
        => CreateZip(entries, CompressionLevel.Optimal);

    private static byte[] CreateZip((string Name, string Content)[] entries, CompressionLevel compressionLevel)
    {
        using var destination = new MemoryStream();
        using (var archive = new ZipArchive(destination, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var item in entries)
            {
                var entry = archive.CreateEntry(item.Name, compressionLevel);
                using var output = entry.Open();
                var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(item.Content);
                output.Write(bytes, 0, bytes.Length);
            }
        }
        return destination.ToArray();
    }

    private static byte[] ReadAll(Stream source)
    {
        using var destination = new MemoryStream();
        var buffer = new byte[4096];
        int read;
        while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
            destination.Write(buffer, 0, read);
        return destination.ToArray();
    }

    private static async Task<byte[]> ReadAllAsync(Stream source)
    {
        using var destination = new MemoryStream();
        var buffer = new byte[4096];
        int read;
        while ((read = await source.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None)) > 0)
            await destination.WriteAsync(buffer, 0, read, CancellationToken.None);
        return destination.ToArray();
    }

    private static byte[] ConvertToZip64Directory(byte[] zip)
    {
        var end = FindEndOfCentralDirectory(zip);
        var entriesOnDisk = ReadUInt16(zip, end + 8);
        var entryCount = ReadUInt16(zip, end + 10);
        var directorySize = ReadUInt32(zip, end + 12);
        var directoryOffset = ReadUInt32(zip, end + 16);
        var zip64Offset = end;
        var result = new byte[end + 56 + 20 + 22];
        Array.Copy(zip, 0, result, 0, end);

        var zip64 = end;
        WriteUInt32(result, zip64, 0x06064B50);
        WriteUInt64(result, zip64 + 4, 44);
        WriteUInt16(result, zip64 + 12, 45);
        WriteUInt16(result, zip64 + 14, 45);
        WriteUInt32(result, zip64 + 16, 0);
        WriteUInt32(result, zip64 + 20, 0);
        WriteUInt64(result, zip64 + 24, entriesOnDisk);
        WriteUInt64(result, zip64 + 32, entryCount);
        WriteUInt64(result, zip64 + 40, directorySize);
        WriteUInt64(result, zip64 + 48, directoryOffset);

        var locator = zip64 + 56;
        WriteUInt32(result, locator, 0x07064B50);
        WriteUInt32(result, locator + 4, 0);
        WriteUInt64(result, locator + 8, (ulong)zip64Offset);
        WriteUInt32(result, locator + 16, 1);

        var eocd = locator + 20;
        WriteUInt32(result, eocd, 0x06054B50);
        WriteUInt16(result, eocd + 4, 0);
        WriteUInt16(result, eocd + 6, 0);
        WriteUInt16(result, eocd + 8, ushort.MaxValue);
        WriteUInt16(result, eocd + 10, ushort.MaxValue);
        WriteUInt32(result, eocd + 12, uint.MaxValue);
        WriteUInt32(result, eocd + 16, uint.MaxValue);
        WriteUInt16(result, eocd + 20, 0);
        return result;
    }

    private static byte[] MarkZipAsSplit(byte[] zip)
    {
        var result = (byte[])zip.Clone();
        var end = FindEndOfCentralDirectory(result);
        WriteUInt16(result, end + 4, 1);
        return result;
    }

    private static byte[] SetZipEntryFlags(byte[] zip, string entryName, ushort flagsToSet)
    {
        var result = (byte[])zip.Clone();
        var local = FindLocalEntry(result, entryName);
        WriteUInt16(result, local + 6, (ushort)(ReadUInt16(result, local + 6) | flagsToSet));
        var central = FindCentralEntry(result, entryName);
        WriteUInt16(result, central + 8, (ushort)(ReadUInt16(result, central + 8) | flagsToSet));
        return result;
    }

    private static byte[] SetZipEntryCompressionMethod(byte[] zip, string entryName, ushort method)
    {
        var result = (byte[])zip.Clone();
        WriteUInt16(result, FindLocalEntry(result, entryName) + 8, method);
        WriteUInt16(result, FindCentralEntry(result, entryName) + 10, method);
        return result;
    }

    private static int FindLocalEntry(byte[] zip, string entryName)
    {
        for (var offset = 0; offset <= zip.Length - 30; offset++)
        {
            if (ReadUInt32(zip, offset) != 0x04034B50)
                continue;
            var nameLength = ReadUInt16(zip, offset + 26);
            if (offset + 30 + nameLength <= zip.Length &&
                Encoding.UTF8.GetString(zip, offset + 30, nameLength) == entryName)
                return offset;
        }
        throw new InvalidDataException("Local ZIP entry was not found.");
    }

    private static int FindCentralEntry(byte[] zip, string entryName)
    {
        for (var offset = 0; offset <= zip.Length - 46; offset++)
        {
            if (ReadUInt32(zip, offset) != 0x02014B50)
                continue;
            var nameLength = ReadUInt16(zip, offset + 28);
            if (offset + 46 + nameLength <= zip.Length &&
                Encoding.UTF8.GetString(zip, offset + 46, nameLength) == entryName)
                return offset;
        }
        throw new InvalidDataException("Central ZIP entry was not found.");
    }

    private static int FindEndOfCentralDirectory(byte[] zip)
    {
        for (var offset = zip.Length - 22; offset >= 0; offset--)
        {
            if (ReadUInt32(zip, offset) == 0x06054B50 &&
                offset + 22 + ReadUInt16(zip, offset + 20) == zip.Length)
                return offset;
        }
        throw new InvalidDataException("ZIP end record was not found.");
    }

    private static ushort ReadUInt16(byte[] data, int offset)
        => (ushort)(data[offset] | data[offset + 1] << 8);

    private static uint ReadUInt32(byte[] data, int offset)
        => (uint)(data[offset] | data[offset + 1] << 8 | data[offset + 2] << 16 | data[offset + 3] << 24);

    private static void WriteUInt16(byte[] data, int offset, ushort value)
    {
        data[offset] = (byte)value;
        data[offset + 1] = (byte)(value >> 8);
    }

    private static void WriteUInt32(byte[] data, int offset, uint value)
    {
        for (var index = 0; index < 4; index++)
            data[offset + index] = (byte)(value >> (8 * index));
    }

    private static void WriteUInt64(byte[] data, int offset, ulong value)
    {
        for (var index = 0; index < 8; index++)
            data[offset + index] = (byte)(value >> (8 * index));
    }

    /// <summary>
    /// 源流读取次数达到限制后抛错，用于确认深层检测的 finally 恢复行为。
    /// </summary>
    private sealed class ThrowAfterReadStream : MemoryStream
    {
        private readonly int _successfulReads;
        private int _readCount;

        internal ThrowAfterReadStream(byte[] content, int successfulReads) : base(content, writable: false)
            => _successfulReads = successfulReads;

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_readCount++ >= _successfulReads)
                throw new IOException("Injected container read failure.");
            return base.Read(buffer, offset, count);
        }
    }

    /// <summary>
    /// 提供读取能力但拒绝定位的流，供 ZIP 容器要求测试使用。
    /// </summary>
    private sealed class NonSeekableStream : Stream
    {
        private readonly MemoryStream _inner;

        internal NonSeekableStream(byte[] content) => _inner = new MemoryStream(content, writable: false);
        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => _inner.ReadAsync(buffer, offset, count, cancellationToken);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Flush() { }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _inner.Dispose();
            base.Dispose(disposing);
        }
    }
}
