namespace Bing.IO;

/// <summary>
/// 临时目录
/// </summary>
public class TempDirectory : TempPath
{
    /// <summary>
    /// 初始化一个<see cref="TempDirectory"/>类型的实例
    /// </summary>
    /// <param name="prefix">临时目录前缀</param>
    public TempDirectory(string prefix = null) : base(prefix)
    {
    }

    /// <summary>
    /// 初始化路径
    /// </summary>
    protected override void InitializePath() => Directory.CreateDirectory(FullPath);

    /// <summary>
    /// 释放资源。确保删除临时路径
    /// </summary>
    /// <param name="disposing">是否释放中</param>
    protected override void Dispose(bool disposing)
    {
        if (!Directory.Exists(FullPath))
            return;
        try
        {
            Directory.Delete(FullPath, true);
        }
        catch
        {
            // ignored
        }
    }
}