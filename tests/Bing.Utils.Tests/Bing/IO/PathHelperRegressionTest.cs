namespace Bing.IO;

/// <summary>
/// 验证路径辅助方法创建目录后的状态刷新。
/// </summary>
[Trait("Category", "Regression")]
[Trait("Risk", "High")]
public class PathHelperRegressionTest
{
    /// <summary>
    /// 验证EnsureDirectoryExists 创建目录后应返回 Exists=true 的 DirectoryInfo
    /// </summary>
    [Fact]
    [Trait("DefectPattern", "IO.PathHelper.DirectoryInfoCache")]
    public void EnsureDirectoryExists_NewDirectory_ShouldReturnRefreshedDirectoryInfo()
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), $"bing-reg-dir-{Guid.NewGuid():N}");
        try
        {
            var dirInfo = PathHelper.EnsureDirectoryExists(directoryPath);

            dirInfo.ShouldNotBeNull();
            dirInfo.Exists.ShouldBeTrue();
            Directory.Exists(directoryPath).ShouldBeTrue();
        }
        finally
        {
            if (Directory.Exists(directoryPath))
                Directory.Delete(directoryPath);
        }
    }
}
