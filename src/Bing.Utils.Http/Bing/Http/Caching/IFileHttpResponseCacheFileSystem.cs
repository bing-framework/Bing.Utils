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
    IEnumerable<string> GetFiles(string directory, string pattern);

    /// <summary>

    /// 枚举指定目录的直接子目录。

    /// </summary>
    IEnumerable<string> GetDirectories(string directory);

    /// <summary>

    /// 判断文件是否存在。

    /// </summary>
    bool FileExists(string path);

    /// <summary>

    /// 读取文件的 UTC 最后写入时间。

    /// </summary>
    DateTime GetLastWriteTimeUtc(string path);

    /// <summary>

    /// 获取独占文件锁。

    /// </summary>
    FileStream OpenLock(string path, CancellationToken cancellationToken, Action onWait);

    /// <summary>

    /// 尝试删除文件；删除失败返回 <see langword="false"/>。

    /// </summary>
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

    /// 初始化一个<see cref="DefaultFileHttpResponseCacheFileSystem"/>类型的实例。

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
