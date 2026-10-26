using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.ExceptionServices;
using System.Runtime.Loader;
using System.Runtime.Versioning;

namespace Bing.Helpers;

/// <summary>使用隔离临时目录验证 ZIP 路径边界；可加载真实 netstandard2.0 产物重跑同一套用例。</summary>
public class ZipReviewRegressionTest : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "bing-zip-review-" + Guid.NewGuid().ToString("N"));
    private readonly AssemblyLoadContext _context;
    private readonly MethodInfo _unzip;
    private readonly MethodInfo _legacyUnzip;
    private readonly bool _legacyOverwrites;

    public ZipReviewRegressionTest()
    {
        Directory.CreateDirectory(_root);
        var assembly = typeof(Bing.IO.ZipHelper).Assembly;
        var assemblyPath = Environment.GetEnvironmentVariable("BING_REVIEW_NETSTANDARD_DLL");
        if (!string.IsNullOrEmpty(assemblyPath))
        {
            _context = new AssemblyLoadContext("zip-review-" + Guid.NewGuid(), isCollectible: true);
            assembly = _context.LoadFromAssemblyPath(Path.GetFullPath(assemblyPath));
            assembly.GetCustomAttribute<TargetFrameworkAttribute>().FrameworkName.ShouldBe(".NETStandard,Version=v2.0");
            _unzip = assembly.GetType("Bing.Helpers.Compression").GetMethod("UnZip", new[] { typeof(string), typeof(string), typeof(bool) });
        }
        _legacyUnzip = assembly.GetType("Bing.IO.ZipHelper").GetMethod("UnZip", new[] { typeof(string), typeof(string) });
        _legacyOverwrites = assembly.GetCustomAttribute<TargetFrameworkAttribute>().FrameworkName != ".NETStandard,Version=v2.0";
    }

    [Theory]
    [InlineData("../sentinel.txt", false)]
    [InlineData("../sentinel.txt", true)]
    [InlineData("../target-other/sentinel.txt", false)]
    [InlineData("../target-other/sentinel.txt", true)]
    [InlineData("..\\sentinel.txt", false)]
    [InlineData("..\\sentinel.txt", true)]
    [InlineData("/rooted.txt", false)]
    [InlineData("/rooted.txt", true)]
    [InlineData("C:/rooted.txt", false)]
    [InlineData("C:/rooted.txt", true)]
    [InlineData("\\\\server/share/file.txt", false)]
    [InlineData("\\\\server/share/file.txt", true)]
    public void RejectsUnsafeEntriesBeforeAnyExtraction(string entryName, bool legacy)
    {
        var target = Path.Combine(_root, "target");
        var sibling = Path.Combine(_root, "target-other");
        Directory.CreateDirectory(sibling);
        var sentinel = Path.Combine(_root, "sentinel.txt");
        File.WriteAllText(sentinel, "unchanged", Encoding.UTF8);
        File.WriteAllText(Path.Combine(sibling, "sentinel.txt"), "unchanged", Encoding.UTF8);
        var zip = CreateZip(("normal.txt", "normal"), (entryName, "changed"));
        AssertRejected(zip, target, legacy);
        Directory.Exists(target).ShouldBeFalse();
        File.ReadAllText(sentinel, Encoding.UTF8).ShouldBe("unchanged");
        File.ReadAllText(Path.Combine(sibling, "sentinel.txt"), Encoding.UTF8).ShouldBe("unchanged");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RejectsAbsolutePathWithinControlledFixture(bool legacy)
    {
        var sentinel = Path.Combine(_root, "absolute.txt");
        File.WriteAllText(sentinel, "unchanged", Encoding.UTF8);
        var zip = CreateZip((sentinel, "changed"));
        AssertRejected(zip, Path.Combine(_root, "target"), legacy);
        File.ReadAllText(sentinel, Encoding.UTF8).ShouldBe("unchanged");
    }

    [Fact]
    public void NestedFilesHonorOverwriteOption()
    {
        var target = Path.Combine(_root, "target");
        var zip = CreateZip(("./", null), ("empty/", null), ("nested/", null), ("nested/file.txt", "first"));
        Extract(zip, target, true);
        var file = Path.Combine(target, "nested", "file.txt");
        File.ReadAllText(file, Encoding.UTF8).ShouldBe("first");
        Directory.Exists(Path.Combine(target, "empty")).ShouldBeTrue();
        var changed = CreateZip(("nested/file.txt", "second"));
        Assert.Throws<InvalidOperationException>(() => Extract(changed, target, false));
        File.ReadAllText(file, Encoding.UTF8).ShouldBe("first");
        Extract(changed, target, true);
        File.ReadAllText(file, Encoding.UTF8).ShouldBe("second");
    }

    /// <summary>
    /// 验证目标目录中的既有目录链接不能将归档文件写到目录外。
    /// </summary>
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public void RejectsExistingDirectoryLinkBeforeAnyExtraction(bool legacy, bool existingFile, bool rootIsLink)
    {
        var target = Path.Combine(_root, "target");
        var outside = Path.Combine(_root, "outside");
        var link = rootIsLink ? target : Path.Combine(target, "link");
        var sentinel = Path.Combine(outside, "sentinel.txt");
        if (!rootIsLink) Directory.CreateDirectory(target);
        Directory.CreateDirectory(outside);
        if (existingFile) File.WriteAllText(sentinel, "unchanged", Encoding.UTF8);

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                CreateJunction(link, outside);
#if NET6_0_OR_GREATER
            else
                Directory.CreateSymbolicLink(link, outside);
#else
            else
                throw Xunit.Sdk.SkipException.ForSkip("Directory symbolic links require .NET 6 on non-Windows test targets.");
#endif

            var zip = CreateZip(("normal.txt", "normal"), (rootIsLink ? "sentinel.txt" : "link/sentinel.txt", "changed"));

            AssertRejected(zip, target, legacy);
            if (existingFile) File.ReadAllText(sentinel, Encoding.UTF8).ShouldBe("unchanged");
            else File.Exists(sentinel).ShouldBeFalse();
            File.Exists(Path.Combine(target, "normal.txt")).ShouldBeFalse();
        }
        finally
        {
            DeleteDirectoryLink(link);
        }
    }

    /// <summary>旧入口保持正常嵌套解压，以及各目标框架原有的覆盖规则。</summary>
    [Fact]
    public void ZipHelperPreservesFrameworkOverwriteBehavior()
    {
        var target = Path.Combine(_root, "target");
        ExtractLegacy(CreateZip(("empty/", null), ("nested/file.txt", "first")), target);
        var file = Path.Combine(target, "nested", "file.txt");
        File.ReadAllText(file, Encoding.UTF8).ShouldBe("first");
        Directory.Exists(Path.Combine(target, "empty")).ShouldBeTrue();
        var changed = CreateZip(("nested/file.txt", "second"));
        if (_legacyOverwrites)
        {
            ExtractLegacy(changed, target);
            File.ReadAllText(file, Encoding.UTF8).ShouldBe("second");
        }
        else
        {
            Assert.Throws<IOException>(() => ExtractLegacy(changed, target));
            File.ReadAllText(file, Encoding.UTF8).ShouldBe("first");
        }
    }

    /// <summary>共享预检不为旧入口的格式错误增加外层异常包装。</summary>
    [Fact]
    public void ZipHelperPreservesInvalidArchiveException()
    {
        var zip = Path.Combine(_root, "invalid.zip");
        File.WriteAllText(zip, "not a zip", Encoding.UTF8);
        Assert.Throws<InvalidDataException>(() => ExtractLegacy(zip, Path.Combine(_root, "target")));
    }

    /// <summary>两个公开入口共享安全断言，但保留各自的异常包装约定。</summary>
    private void AssertRejected(string zip, string target, bool legacy)
    {
        if (legacy) Assert.Throws<InvalidDataException>(() => ExtractLegacy(zip, target));
        else Assert.Throws<InvalidOperationException>(() => Extract(zip, target, true));
    }

    private void ExtractLegacy(string zip, string target)
    {
        try { _legacyUnzip.Invoke(null, new object[] { zip, target }); }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    private static void CreateJunction(string junctionPath, string targetPath)
    {
        var commandProcessor = Environment.GetEnvironmentVariable("ComSpec");
        var startInfo = new ProcessStartInfo
        {
            FileName = string.IsNullOrEmpty(commandProcessor) ? "cmd.exe" : commandProcessor,
            Arguments = $"/d /c mklink /J \"{junctionPath}\" \"{targetPath}\"",
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("无法启动 cmd.exe 创建测试 junction。");
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(10000))
        {
            process.Kill();
            process.WaitForExit();
            throw new TimeoutException("创建测试 junction 超时。");
        }
        Task.WaitAll(outputTask, errorTask);
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"创建测试 junction 失败: {outputTask.Result}{errorTask.Result}");
    }

    private static void DeleteDirectoryLink(string linkPath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (Directory.Exists(linkPath))
                Directory.Delete(linkPath);
            return;
        }

        if (Directory.Exists(linkPath) || File.Exists(linkPath))
            File.Delete(linkPath);
    }

    private string CreateZip(params (string Name, string Content)[] entries)
    {
        var path = Path.Combine(_root, Guid.NewGuid() + ".zip");
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        foreach (var item in entries)
        {
            var entry = archive.CreateEntry(item.Name);
            if (item.Content == null) continue;
            using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
            writer.Write(item.Content);
        }
        return path;
    }

    private void Extract(string zip, string target, bool overwrite)
    {
        if (_unzip == null) { Compression.UnZip(zip, target, overwrite); return; }
        try { _unzip.Invoke(null, new object[] { zip, target, overwrite }); }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    public void Dispose()
    {
        _context?.Unload();
        Directory.Delete(_root, true);
    }
}
