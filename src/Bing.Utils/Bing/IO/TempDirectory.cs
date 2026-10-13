namespace Bing.IO;

/// <summary>
/// 临时目录
/// </summary>
public class TempDirectory : TempPath
{
    /// <summary>
    /// 初始化一个 <see cref="TempDirectory"/> 类型的实例。
    /// </summary>
    /// <param name="prefix">临时目录前缀</param>
    public TempDirectory(string prefix = null) : base(prefix)
    {
    }

    /// <inheritdoc />
    protected override void InitializePath() => Directory.CreateDirectory(FullPath);

    /// <inheritdoc />
    /// <remarks>
    /// 释放时尝试递归删除临时目录；删除失败时忽略异常。
    /// </remarks>
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
