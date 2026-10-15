namespace Bing.IO;

/// <summary>
/// 测试类：FileHelper 写入回归测试
/// </summary>
[Trait("Category", "Regression")]
[Trait("Risk", "High")]
public class FileHelperWriteRegressionTest
{
    /// <summary>
    /// 验证覆盖较短内容时截断文件旧尾部。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
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
}
