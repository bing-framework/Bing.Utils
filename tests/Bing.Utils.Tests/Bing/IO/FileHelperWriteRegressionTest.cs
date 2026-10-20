namespace Bing.IO;

/// <summary>
/// 测试类：FileHelper 写入回归测试
/// </summary>
[Trait("Category", "Regression")]
[Trait("Risk", "High")]
public class FileHelperWriteRegressionTest
{
    /// <summary>
    /// 测试用例：WriteAsync 在覆盖模式下应截断旧内容，避免残留历史字节
    /// </summary>
    [Fact]
    [Trait("DefectPattern", "IO.FileWrite.OverwriteTruncate")]
    public async Task WriteAsync_OverwriteShorterBytes_ShouldTruncateOldTail()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bing-reg-write-{Guid.NewGuid():N}.bin");
        try
        {
            await File.WriteAllBytesAsync(path, new byte[] { 1, 2, 3, 4, 5 });

            var writeOk = await FileHelper.WriteAsync(new byte[] { 9, 8 }, path, appendMode: false);

            writeOk.ShouldBeTrue();
            File.ReadAllBytes(path).ShouldBe(new byte[] { 9, 8 });
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    /// <summary>
    /// 测试用例：WriteAsync 同一路径重复覆盖时应截断旧内容并释放文件句柄
    /// </summary>
    [Fact]
    [Trait("DefectPattern", "IO.FileWrite.RepeatedOverwriteHandleRelease")]
    public async Task WriteAsync_RepeatedOverwrite_ShouldTruncateAndReleaseHandle()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bing-reg-repeat-{Guid.NewGuid():N}.bin");
        var contents = new[]
        {
            new byte[] { 1, 2, 3, 4 },
            new byte[] { 9, 8 },
            Array.Empty<byte>(),
            new byte[] { 7 }
        };

        try
        {
            foreach (var expected in contents)
            {
                var writeOk = await FileHelper.WriteAsync(expected, path, appendMode: false);

                writeOk.ShouldBeTrue();
                ReadExclusively(path).ShouldBe(expected);
            }
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    /// <summary>
    /// 测试用例：WriteAsync 协调并发写入不同文件时，每个文件均应保留完整内容
    /// </summary>
    [Fact]
    [Trait("DefectPattern", "IO.FileWrite.ConcurrentIndependentFiles")]
    public async Task WriteAsync_ConcurrentDifferentFiles_ShouldWriteCompleteContents()
    {
        var cases = Enumerable.Range(0, 8)
            .Select(index =>
            {
                var path = Path.Combine(Path.GetTempPath(), $"bing-reg-concurrent-{Guid.NewGuid():N}.bin");
                var content = Enumerable.Range(0, 1024 + index)
                    .Select(offset => (byte)((offset * 31 + index) % 251))
                    .ToArray();
                return (path, content);
            })
            .ToArray();
        var start = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        try
        {
            var writes = cases.Select(async item =>
            {
                await start.Task;
                var writeOk = await FileHelper.WriteAsync(item.content, item.path, appendMode: false);
                return (item.path, item.content, writeOk);
            }).ToArray();

            start.SetResult(true);
            var results = await Task.WhenAll(writes);

            foreach (var result in results)
            {
                result.writeOk.ShouldBeTrue();
                File.ReadAllBytes(result.path).ShouldBe(result.content);
            }
        }
        finally
        {
            foreach (var (path, _) in cases)
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }
    }

    private static byte[] ReadExclusively(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
        var content = new byte[checked((int)stream.Length)];
        var offset = 0;
        while (offset < content.Length)
        {
            var read = stream.Read(content, offset, content.Length - offset);
            if (read == 0)
                break;
            offset += read;
        }

        offset.ShouldBe(content.Length);
        return content;
    }
}
