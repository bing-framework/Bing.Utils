using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using Bing.IO;

namespace Bing.Utils.Tests.Bing.IO;

/// <summary>以独立 OPC 生产器生成的固定夹具验证合法 URI 和 Default 扩展名兼容性。</summary>
public class FileContentDetectionOpcCompatibilityTests
{
    private static FileDetectionOptions Deep() => new FileDetectionOptions { InspectContainers = true };

    /// <summary>测试夹具不使用检测器的规则拼装清单，覆盖三个直接入口和前缀重放。</summary>
    [Theory]
    [InlineData("ordinary.zip", "word/document.xml")]
    [InlineData("space-default.zip", "custom/document%20name.xml")]
    [InlineData("space-override.zip", "custom/document%20name.xml")]
    [InlineData("main-colon-extension.zip", "custom/document.a:b")]
    [InlineData("main-escaped-extension.zip", "custom/document.a%2Bb")]
    [InlineData("attachment-colon-extension.zip", "word/document.xml")]
    [InlineData("attachment-escaped-extension.zip", "word/document.xml")]
    public async Task Detect_AcceptsIndependentPackagingFixture(string fixture, string mainPart)
    {
        var data = ReadFixture(fixture);
        // 验证夹具确实包含指定部件，防止误用普通 ZIP 样本导致回归失效。
        using (var archive = new ZipArchive(new MemoryStream(data, false), ZipArchiveMode.Read))
            Assert.NotNull(archive.GetEntry(mainPart));

        AssertDocx(FileContentDetector.Detect(data, Deep()));
        using var source = new MemoryStream(data, false);
        AssertDocx(FileContentDetector.Detect(source, Deep()));
        Assert.Equal(0, source.Position);
        AssertDocx(await FileContentDetector.DetectAsync(source, Deep()));
        Assert.Equal(0, source.Position);
        using var probe = await FileContentDetector.ProbeAsync(source, Deep());
        AssertDocx(probe.Result);
        using var replayed = new MemoryStream();
        await probe.Content.CopyToAsync(replayed);
        Assert.Equal(data, replayed.ToArray());
    }

    /// <summary>夹具确实保留需要修复的 Default / Override 声明。</summary>
    [Theory]
    [InlineData("space-override.zip", "Override", "PartName", "/custom/document%20name.xml")]
    [InlineData("main-colon-extension.zip", "Default", "Extension", "a:b")]
    [InlineData("main-escaped-extension.zip", "Default", "Extension", "a%2Bb")]
    [InlineData("attachment-colon-extension.zip", "Default", "Extension", "a:b")]
    [InlineData("attachment-escaped-extension.zip", "Default", "Extension", "a%2Bb")]
    public void Fixture_ContainsExpectedDeclaration(string fixture, string element, string attribute, string value)
    {
        using var archive = new ZipArchive(new MemoryStream(ReadFixture(fixture), false), ZipArchiveMode.Read);
        using var input = archive.GetEntry("[Content_Types].xml").Open();
        using var reader = new StreamReader(input, Encoding.UTF8);
        var types = XElement.Parse(reader.ReadToEnd());
        XNamespace ns = "http://schemas.openxmlformats.org/package/2006/content-types";
        Assert.Contains(types.Elements(ns + element), item => (string)item.Attribute(attribute) == value);
    }

    /// <summary>转义大小写不应影响 Default 查找；不把转义后的保留字符解码成其他名称。</summary>
    [Fact]
    public void Detect_DefaultEscapeCaseDoesNotAffectLookup()
    {
        var data = RewriteManifest(ReadFixture("main-escaped-extension.zip"),
            text => text.Replace("Extension=\"a%2Bb\"", "Extension=\"a%2bb\""));
        AssertDocx(FileContentDetector.Detect(data, Deep()));
    }

    /// <summary>放行编码后的空格不意味着允许原始空格、控制字符或编码后的分隔符。</summary>
    [Theory]
    [InlineData("document name")]
    [InlineData("document%00name")]
    [InlineData("document%1Fname")]
    [InlineData("document%7Fname")]
    [InlineData("document%2Fname")]
    [InlineData("document%5Cname")]
    [InlineData("document%2")]
    [InlineData("document%GGname")]
    public void Detect_StillRejectsInvalidOverrideEncoding(string part)
    {
        var data = RewriteManifest(ReadFixture("space-override.zip"),
            text => text.Replace("document%20name", part));
        var result = FileContentDetector.Detect(data, Deep());
        Assert.Equal(FileDetectionReason.InvalidContainer, result.ReasonCode);
    }

    /// <summary>扩展名仍必须是合法单段，不能借由百分号逃逸为路径或未保留字符别名。</summary>
    [Theory]
    [InlineData("a/b")]
    [InlineData("a%2Fb")]
    [InlineData("a%5Cb")]
    [InlineData("a%00b")]
    [InlineData("a%2Eb")]
    [InlineData("a%6Db")]
    [InlineData("a%2")]
    [InlineData("a b")]
    public void Detect_StillRejectsInvalidDefaultEncoding(string extension)
    {
        var data = RewriteManifest(ReadFixture("attachment-escaped-extension.zip"),
            text => text.Replace("Extension=\"a%2Bb\"", "Extension=\"" + extension + "\""));
        Assert.Equal(FileDetectionReason.InvalidContainer, FileContentDetector.Detect(data, Deep()).ReasonCode);
    }

    private static byte[] ReadFixture(string name) => File.ReadAllBytes(
        Path.Combine(AppContext.BaseDirectory, "Resources", "FileContentDetection", name));

    private static void AssertDocx(FileDetectionResult result)
    {
        Assert.Equal(FileDetectionStatus.Matched, result.Status);
        Assert.Same(FileContentFormats.Docx, result.Format);
        Assert.Same(FileContentFormats.Zip, result.ContainerFormat);
        Assert.Equal(FileDetectionEvidence.ContainerStructure, result.Evidence);
    }

    /// <summary>仅在已有独立夹具上制造反例，正常用例始终使用原始生产器输出。</summary>
    private static byte[] RewriteManifest(byte[] data, Func<string, string> rewrite)
    {
        using var output = new MemoryStream();
        output.Write(data, 0, data.Length);
        using (var archive = new ZipArchive(output, ZipArchiveMode.Update, true))
        {
            var entry = archive.GetEntry("[Content_Types].xml");
            string text;
            using (var reader = new StreamReader(entry.Open(), Encoding.UTF8)) text = reader.ReadToEnd();
            entry.Delete();
            using var writer = new StreamWriter(archive.CreateEntry("[Content_Types].xml").Open(), new UTF8Encoding(false));
            writer.Write(rewrite(text));
        }
        return output.ToArray();
    }
}
