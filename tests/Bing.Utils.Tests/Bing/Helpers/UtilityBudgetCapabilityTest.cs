using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using Bing.IO;

namespace Bing.Helpers;

/// <summary>
/// 验证便捷入口的预算和默认 JSON 配置兼容性。
/// </summary>
public class UtilityBudgetCapabilityTest
{
    /// <summary>
    /// ZIP 在创建目标目录前拒绝条目、层数、单文件和累计预算超限。
    /// </summary>
    [Theory]
    [InlineData("entries")]
    [InlineData("depth")]
    [InlineData("entryBytes")]
    [InlineData("totalBytes")]
    public async Task ZipBudgetsAreCheckedBeforeWriting(string budget)
    {
        var root = Path.Combine(Path.GetTempPath(), "bing-zip-budget-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        var zip = Path.Combine(root, "input.zip"); var target = Path.Combine(root, "output");
        try
        {
            using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
            {
                using var output = archive.CreateEntry("a/b/file.txt").Open(); output.Write(new byte[] { 1, 2, 3 }, 0, 3);
            }
            var options = new ZipExtractionOptions();
            if (budget == "entries") options.MaxEntries = 0;
            if (budget == "depth") options.MaxDirectoryDepth = 1;
            if (budget == "entryBytes") options.MaxEntryBytes = 2;
            if (budget == "totalBytes") options.MaxTotalBytes = 2;
            await Assert.ThrowsAsync<InvalidDataException>(() => ZipHelper.UnZipWithOptionsAsync(zip, target, options));
            Assert.False(Directory.Exists(target));
            options = new ZipExtractionOptions { MaxEntries = 1, MaxDirectoryDepth = 2, MaxEntryBytes = 3, MaxTotalBytes = 3 };
            await ZipHelper.UnZipWithOptionsAsync(zip, target, options);
            Assert.Equal(new byte[] { 1, 2, 3 }, File.ReadAllBytes(Path.Combine(target, "a/b/file.txt")));
            Assert.Empty(Directory.GetFiles(target, "*.tmp", SearchOption.AllDirectories));
            options.OverwriteFiles = false;
            Assert.Throws<IOException>(() => ZipHelper.UnZipWithOptions(zip, target, options));
            using var canceled = new CancellationTokenSource(); canceled.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ZipHelper.UnZipWithOptionsAsync("missing.zip", target, options, canceled.Token));
        }
        finally { Directory.Delete(root, true); }
    }

    /// <summary>
    /// 正则便捷入口传播超时，普通提取和替换仍可使用缓存。
    /// </summary>
    [Fact]
    public void RegexConvenienceUsesTimeout()
    {
        Assert.Throws<RegexMatchTimeoutException>(() => Regexs.IsMatchWithTimeout(new string('a', 20000) + "!", "^(a+)+$", TimeSpan.FromMilliseconds(1), useCache: false));
        Assert.True(Regexs.IsMatchWithTimeout("abc", "^abc$", TimeSpan.FromSeconds(1)));
        Assert.Equal("x", Regexs.ReplaceWithTimeout("abc", "abc", "x", TimeSpan.FromSeconds(1)));
        Assert.Equal(new[] { "a", "b" }, Regexs.SplitWithTimeout("a,b", ",", TimeSpan.FromSeconds(1)));
        Assert.Equal("12", Regexs.GetValueWithTimeout("a12", "(\\d+)", TimeSpan.FromSeconds(1), "$1"));
    }

    /// <summary>
    /// 内部选项复用不会污染显式选项，输出与原有长整数和空值语义一致。
    /// </summary>
    [Fact]
    public async Task JsonDefaultsRemainIndependentOfCallerOptions()
    {
        var value = new { Id = long.MaxValue, Name = (string)null };
        var expected = Json.ToJson(value);
        Assert.Contains("\"9223372036854775807\"", expected); Assert.DoesNotContain("Name", expected);
        var options = new JsonSerializerOptions();
        var custom = Json.ToJson(value, options);
        Assert.Contains("\"Name\":null", custom);
        var values = await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => Task.Run(() => Json.ToJson(value))));
        Assert.All(values, item => Assert.Equal(expected, item)); Assert.Empty(options.Converters);
    }
}
