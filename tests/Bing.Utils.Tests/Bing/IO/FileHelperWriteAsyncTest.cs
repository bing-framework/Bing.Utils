namespace Bing.IO;
/// <summary>
/// FileHelper 写入异步方法测试
/// </summary>
public class FileHelperWriteAsyncTest
{
    /// <summary>
    /// 验证覆盖写入替换已有文件内容。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [Fact]
    public async Task WriteAsync_OverwriteExistingFile_ShouldReplaceContent()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bing-filehelper-{Guid.NewGuid():N}.txt");
        try
        {
            await File.WriteAllTextAsync(path, "old");
            var result = await FileHelper.WriteAsync(new byte[] { 1, 2, 3 }, path, false);
            result.ShouldBeTrue();
            File.ReadAllBytes(path).ShouldBe(new byte[] { 1, 2, 3 });
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
    /// <summary>
    /// 验证追加写入将字节添加到已有文件末尾。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [Fact]
    public async Task WriteAsync_AppendMode_ShouldAppendBytes()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bing-filehelper-{Guid.NewGuid():N}.txt");
        try
        {
            await File.WriteAllBytesAsync(path, new byte[] { 1 });
            var result = await FileHelper.WriteAsync(new byte[] { 2, 3 }, path, true);
            result.ShouldBeTrue();
            File.ReadAllBytes(path).ShouldBe(new byte[] { 1, 2, 3 });
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
