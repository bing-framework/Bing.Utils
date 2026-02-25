namespace Bing.IO;

/// <summary>
/// 测试类：PathHelper 回归测试
/// </summary>
[Trait("Category", "Regression")]
[Trait("Risk", "High")]
public class PathHelperRegressionTest
{
    /// <summary>
    /// 测试用例：EnsureDirectoryExists 创建目录后应返回 Exists=true 的 DirectoryInfo
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
