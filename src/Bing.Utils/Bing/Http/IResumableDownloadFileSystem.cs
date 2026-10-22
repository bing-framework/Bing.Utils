namespace Bing.Http;

/// <summary>
/// 为断点续传侧车维护提供可替换的文件系统操作。
/// </summary>
/// <remarks>该接口仅供库内部和测试使用，默认实现直接调用 System.IO。</remarks>
internal interface IResumableDownloadFileSystem
{
    /// <summary>
    /// 判断指定目录是否存在。
    /// </summary>
    /// <param name="path">目录路径。</param>
    /// <returns>目录存在时返回 <see langword="true"/>。</returns>
    bool DirectoryExists(string path);
    /// <summary>
    /// 枚举指定目录中符合模式的顶层文件。
    /// </summary>
    /// <param name="directory">待扫描目录。</param>
    /// <param name="pattern">文件名搜索模式。</param>
    /// <returns>匹配的文件路径。</returns>
    IEnumerable<string> GetFiles(string directory, string pattern);
    /// <summary>
    /// 枚举指定目录的直接子目录。
    /// </summary>
    /// <param name="directory">待扫描目录。</param>
    /// <returns>子目录路径。</returns>
    IEnumerable<string> GetDirectories(string directory);
    /// <summary>
    /// 判断指定文件是否存在。
    /// </summary>
    /// <param name="path">文件路径。</param>
    /// <returns>文件存在时返回 <see langword="true"/>。</returns>
    bool FileExists(string path);
    /// <summary>
    /// 获取文件最后修改时间的 UTC 值。
    /// </summary>
    /// <param name="path">文件路径。</param>
    /// <returns>文件最后修改时间。</returns>
    DateTime GetLastWriteTimeUtc(string path);
    /// <summary>
    /// 以独占方式打开或创建锁文件。
    /// </summary>
    /// <param name="path">锁文件路径。</param>
    /// <returns>用于持有锁的文件流。</returns>
    FileStream OpenLock(string path);
    /// <summary>
    /// 删除指定文件。
    /// </summary>
    /// <param name="path">文件路径。</param>
    /// <returns>文件存在并被删除时返回 <see langword="true"/>。</returns>
    bool Delete(string path);
}

/// <summary>
/// 断点续传侧车维护的默认文件系统适配器。
/// </summary>
internal sealed class DefaultResumableDownloadFileSystem : IResumableDownloadFileSystem
{
    /// <summary>
    /// 供续传侧车维护使用的默认文件系统适配器实例。
    /// </summary>
    internal static readonly DefaultResumableDownloadFileSystem Instance = new DefaultResumableDownloadFileSystem();

    /// <inheritdoc />
    public bool DirectoryExists(string path) => Directory.Exists(path);
    /// <inheritdoc />
    public IEnumerable<string> GetFiles(string directory, string pattern)
        => Directory.GetFiles(directory, pattern, SearchOption.TopDirectoryOnly);
    /// <inheritdoc />
    public IEnumerable<string> GetDirectories(string directory)
        => Directory.GetDirectories(directory, "*", SearchOption.TopDirectoryOnly);
    /// <inheritdoc />
    public bool FileExists(string path) => File.Exists(path);
    /// <inheritdoc />
    public DateTime GetLastWriteTimeUtc(string path) => File.GetLastWriteTimeUtc(path);
    /// <inheritdoc />
    public FileStream OpenLock(string path)
        => new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
    /// <inheritdoc />
    public bool Delete(string path)
    {
        if (!File.Exists(path)) return false;
        File.Delete(path);
        return true;
    }
}
