namespace Bing.IO;

/// <summary>
/// 临时路径
/// </summary>
public abstract class TempPath : IDisposable
{
    /// <summary>
    /// 初始化一个<see cref="TempPath"/>类型的实例
    /// </summary>
    /// <param name="prefix">临时路径前缀</param>
    protected TempPath(string prefix = null)
    {
        Name = $"{prefix ?? string.Empty}{Guid.NewGuid()}";
        FullPath = Path.Combine(Path.GetTempPath(), Name);
        Initialize();
    }

    /// <summary>
    /// 完整的绝对路径
    /// </summary>
    public string FullPath { get; }

    /// <summary>
    /// 名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 释放资源。确保删除临时路径
    /// </summary>
    /// <param name="disposing">是否释放中</param>
    protected abstract void Dispose(bool disposing);

    /// <summary>
    /// 初始化路径
    /// </summary>
    protected virtual void InitializePath()
    {
    }

    /// <summary>
    /// 初始化
    /// </summary>
    private void Initialize() => InitializePath();
}