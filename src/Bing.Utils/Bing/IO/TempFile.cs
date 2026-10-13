namespace Bing.IO;

/// <summary>
/// 临时文件
/// </summary>
public class TempFile : TempPath
{
    /// <summary>
    /// 初始化一个 <see cref="TempFile"/> 类型的实例。
    /// </summary>
    /// <param name="prefix">临时文件前缀</param>
    public TempFile(string prefix = null) : base(prefix)
    {
    }

    /// <inheritdoc />
    protected override void InitializePath() => File.Create(FullPath).Dispose();

    /// <inheritdoc />
    /// <remarks>
    /// 释放时尝试删除临时文件；删除失败时忽略异常。
    /// </remarks>
    protected override void Dispose(bool disposing)
    {
        if (!File.Exists(FullPath))
            return;
        try
        {
            File.Delete(FullPath);
        }
        catch
        {
            // ignored
        }
    }
}
