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
}
