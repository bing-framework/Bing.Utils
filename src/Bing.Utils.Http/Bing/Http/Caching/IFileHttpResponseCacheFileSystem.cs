using System.IO;
using System.Threading;

namespace Bing.Http.Caching;

/// <summary>

/// 为文件响应缓存的清理操作提供可替换的文件系统边界。

/// </summary>
/// <remarks>该边界只服务于清理和清空，不参与缓存正文的读写。</remarks>
internal interface IFileHttpResponseCacheFileSystem
{
    /// <summary>
    /// 枚举指定目录中的匹配文件。
    /// </summary>
    /// <returns>匹配文件的完整路径集合。</returns>
    /// <param name="directory">要枚举的目录。</param>
    /// <param name="pattern">文件名匹配模式。</param>
    IEnumerable<string> GetFiles(string directory, string pattern);

    /// <summary>

    /// 枚举指定目录的直接子目录。

    /// </summary>
    /// <returns>直接子目录的完整路径集合。</returns>
    /// <param name="directory">要枚举的目录。</param>
    IEnumerable<string> GetDirectories(string directory);

    /// <summary>

    /// 判断文件是否存在。

    /// </summary>
    /// <returns>文件存在时返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    /// <param name="path">文件路径。</param>
    bool FileExists(string path);

    /// <summary>

    /// 读取文件的 UTC 最后写入时间。

    /// </summary>
    /// <returns>文件的 UTC 最后写入时间。</returns>
    /// <param name="path">文件路径。</param>
    DateTime GetLastWriteTimeUtc(string path);

    /// <summary>

    /// 获取独占文件锁。

    /// </summary>
    /// <returns>已打开并独占的文件流。</returns>
    /// <param name="path">要锁定的文件路径。</param>
    /// <param name="cancellationToken">等待锁时使用的取消令牌。</param>
    /// <param name="onWait">锁被占用时调用的等待回调。</param>
    FileStream OpenLock(string path, CancellationToken cancellationToken, Action onWait);

    /// <summary>

    /// 尝试删除文件；删除失败返回 <see langword="false"/>。

    /// </summary>
    /// <returns>删除成功时返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    /// <param name="path">要删除的文件路径。</param>
    bool TryDelete(string path);
}

/// <summary>

/// 使用当前进程默认文件系统的缓存清理实现。

/// </summary>
internal sealed class DefaultFileHttpResponseCacheFileSystem : IFileHttpResponseCacheFileSystem
{
    /// <summary>
    /// 供缓存清理操作复用的默认文件系统适配器。
    /// </summary>
    internal static readonly DefaultFileHttpResponseCacheFileSystem Instance =
        new DefaultFileHttpResponseCacheFileSystem();

    /// <summary>
    /// 初始化一个 <see cref="DefaultFileHttpResponseCacheFileSystem"/> 类型的实例。
    /// </summary>
    private DefaultFileHttpResponseCacheFileSystem()
    {
    }

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
    public FileStream OpenLock(string path, CancellationToken cancellationToken, Action onWait)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                onWait?.Invoke();
                cancellationToken.ThrowIfCancellationRequested();
                if (DateTime.UtcNow >= deadline) throw;
                cancellationToken.WaitHandle.WaitOne(10);
            }
        }
    }

    /// <inheritdoc />
    public bool TryDelete(string path)
    {
        try
        {
            if (!File.Exists(path)) return false;
            File.Delete(path);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
