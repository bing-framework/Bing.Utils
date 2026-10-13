namespace Bing.Http;

/// <summary>

/// 断点续传侧车文件一次清理操作的结果。

/// </summary>
public sealed class HttpResumableDownloadCleanupResult
{
    /// <summary>
    /// 初始化一个 <see cref="HttpResumableDownloadCleanupResult"/> 类型的实例。
    /// </summary>
    /// <param name="partialFilesRemoved">移除的续传临时数据文件数。</param>
    /// <param name="stateFilesRemoved">移除的续传状态文件数。</param>
    /// <param name="temporaryFilesRemoved">移除的续传状态临时文件数。</param>
    /// <param name="lockFilesRetained">保留的锁文件数。</param>
    internal HttpResumableDownloadCleanupResult(int partialFilesRemoved, int stateFilesRemoved,
        int temporaryFilesRemoved, int lockFilesRetained)
    {
        PartialFilesRemoved = partialFilesRemoved;
        StateFilesRemoved = stateFilesRemoved;
        TemporaryFilesRemoved = temporaryFilesRemoved;
        LockFilesRetained = lockFilesRetained;
    }

    /// <summary>

    /// 获取移除的 .binghttp.part 文件数。

    /// </summary>
    public int PartialFilesRemoved { get; }

    /// <summary>

    /// 获取移除的 .binghttp.resume.json 文件数。

    /// </summary>
    public int StateFilesRemoved { get; }

    /// <summary>

    /// 获取移除的续传状态临时文件数。

    /// </summary>
    public int TemporaryFilesRemoved { get; }

    /// <summary>

    /// 获取保留的锁文件数；包括活跃锁和为并发安全而保留的空闲锁。

    /// </summary>
    public int LockFilesRetained { get; }

    /// <summary>

    /// 获取本次实际移除的侧车文件总数。

    /// </summary>
    public int TotalRemoved => PartialFilesRemoved + StateFilesRemoved + TemporaryFilesRemoved;
}

/// <summary>

/// 提供显式的断点续传侧车文件维护操作。

/// </summary>
public static class HttpResumableDownloadMaintenance
{
    /// <summary>
    /// 续传临时数据文件的名称后缀。
    /// </summary>
    private const string PartialSuffix = ".binghttp.part";
    /// <summary>
    /// 续传状态文件的名称后缀。
    /// </summary>
    private const string StateSuffix = ".binghttp.resume.json";
    /// <summary>
    /// 用于协调续传与清理操作的锁文件后缀。
    /// </summary>
    private const string LockSuffix = ".binghttp.lock";
    /// <summary>
    /// 续传状态临时文件名称中的状态文件标记。
    /// </summary>
    private const string StateTempMarker = ".binghttp.resume.json.";

    /// <summary>

    /// 清理指定目录中过期且未被活动下载占用的断点续传侧车文件。

    /// </summary>
    /// <param name="directory">包含下载目标及其侧车文件的目录；目录不存在时返回空结果。</param>
    /// <param name="staleAfter">侧车文件至少应保留的时间；必须为非负有限值。</param>
    /// <param name="cancellationToken">取消扫描、锁等待和文件删除的令牌。</param>
    /// <returns>本次清理的文件统计。</returns>
    /// <remarks>
    /// 清理按目标文件分组，并先独占对应的 .binghttp.lock；无法取得锁的活动下载会被跳过。
    /// 锁文件本身始终保留，避免释放锁后删除造成跨进程竞态。
    /// 此方法只扫描指定目录的顶层文件，不启动后台定时器。
    /// </remarks>
    public static Task<HttpResumableDownloadCleanupResult> CleanupAsync(
        string directory, TimeSpan staleAfter, CancellationToken cancellationToken = default)
    {
        Validate(directory, staleAfter);
        return CleanupAsync(directory, staleAfter, cancellationToken,
            DefaultResumableDownloadFileSystem.Instance, false);
    }

    /// <summary>

    /// 递归清理指定目录及其子目录中过期且未被活动下载占用的侧车文件。

    /// </summary>
    /// <param name="directory">递归扫描的根目录。</param>
    /// <param name="staleAfter">侧车文件至少应保留的时间；必须为非负有限值。</param>
    /// <param name="cancellationToken">取消扫描、锁等待和文件删除的令牌。</param>
    /// <returns>本次清理的文件统计。</returns>
    /// <remarks>
    /// 这是显式的一次性操作，不启动后台定时器；既有 CleanupAsync 仍只扫描传入目录的顶层文件。
    /// 每个目标文件仍在对应目录中先独占 .binghttp.lock，再重新扫描并删除过期侧车文件。
    /// </remarks>
    public static Task<HttpResumableDownloadCleanupResult> CleanupRecursiveAsync(
        string directory, TimeSpan staleAfter, CancellationToken cancellationToken = default)
    {
        Validate(directory, staleAfter);
        return CleanupAsync(directory, staleAfter, cancellationToken,
            DefaultResumableDownloadFileSystem.Instance, true);
    }

    /// <summary>

    /// 使用指定文件系统清理目录顶层的过期续传侧车文件。

    /// </summary>
    /// <param name="directory">待扫描目录。</param>
    /// <param name="staleAfter">侧车文件至少应保留的时间。</param>
    /// <param name="cancellationToken">取消操作的令牌。</param>
    /// <param name="fileSystem">文件系统适配器。</param>
    /// <returns>本次清理的文件统计。</returns>
    internal static Task<HttpResumableDownloadCleanupResult> CleanupAsync(
        string directory, TimeSpan staleAfter, CancellationToken cancellationToken,
        IResumableDownloadFileSystem fileSystem)
        => CleanupAsync(directory, staleAfter, cancellationToken, fileSystem, false);

    /// <summary>

    /// 使用指定文件系统清理过期续传侧车文件。

    /// </summary>
    /// <param name="directory">待扫描目录。</param>
    /// <param name="staleAfter">侧车文件至少应保留的时间。</param>
    /// <param name="cancellationToken">取消操作的令牌。</param>
    /// <param name="fileSystem">文件系统适配器。</param>
    /// <param name="recursive">是否递归扫描子目录。</param>
    /// <returns>本次清理的文件统计。</returns>
    internal static Task<HttpResumableDownloadCleanupResult> CleanupAsync(
        string directory, TimeSpan staleAfter, CancellationToken cancellationToken,
        IResumableDownloadFileSystem fileSystem, bool recursive)
    {
        if (fileSystem == null) throw new ArgumentNullException(nameof(fileSystem));
        cancellationToken.ThrowIfCancellationRequested();

        var fullDirectory = Path.GetFullPath(directory);
        if (!fileSystem.DirectoryExists(fullDirectory))
            return Task.FromResult(new HttpResumableDownloadCleanupResult(0, 0, 0, 0));

        var cutoff = GetCutoff(DateTime.UtcNow, staleAfter);
        var partialFilesRemoved = 0;
        var stateFilesRemoved = 0;
        var temporaryFilesRemoved = 0;
        var lockFilesRetained = 0;

        foreach (var currentDirectory in FindDirectories(fullDirectory, recursive, cancellationToken, fileSystem))
        {
            var roots = FindRoots(currentDirectory, cancellationToken, fileSystem);
            foreach (var root in roots)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var partialPath = root + PartialSuffix;
                var statePath = root + StateSuffix;
                var lockPath = root + LockSuffix;
                var temporaryPaths = fileSystem.GetFiles(currentDirectory,
                    Path.GetFileName(root) + StateSuffix + ".*.tmp");
                var staleData = IsStale(partialPath, cutoff, fileSystem) || IsStale(statePath, cutoff, fileSystem) ||
                    temporaryPaths.Any(path => IsStale(path, cutoff, fileSystem));
                var staleLock = IsStale(lockPath, cutoff, fileSystem);
                if (!staleData && !staleLock) continue;

                using (var lockStream = TryAcquireLock(lockPath, fileSystem))
                {
                    if (lockStream == null)
                    {
                        lockFilesRetained++;
                        continue;
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    var currentTemporaryPaths = fileSystem.GetFiles(currentDirectory,
                        Path.GetFileName(root) + StateSuffix + ".*.tmp");
                    var currentStaleData = IsStale(partialPath, cutoff, fileSystem) || IsStale(statePath, cutoff, fileSystem) ||
                        currentTemporaryPaths.Any(path => IsStale(path, cutoff, fileSystem));
                    if (currentStaleData)
                    {
                        if (TryDelete(partialPath, fileSystem)) partialFilesRemoved++;
                        if (TryDelete(statePath, fileSystem)) stateFilesRemoved++;
                        foreach (var path in currentTemporaryPaths)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            if (IsStale(path, cutoff, fileSystem) && TryDelete(path, fileSystem)) temporaryFilesRemoved++;
                        }
                    }
                    if (fileSystem.FileExists(lockPath)) lockFilesRetained++;
                }
            }
        }

        return Task.FromResult(new HttpResumableDownloadCleanupResult(
            partialFilesRemoved, stateFilesRemoved, temporaryFilesRemoved, lockFilesRetained));
    }

    /// <summary>

    /// 验证清理目录和侧车保留时间。

    /// </summary>
    /// <param name="directory">待扫描目录。</param>
    /// <param name="staleAfter">侧车文件至少应保留的时间。</param>
    private static void Validate(string directory, TimeSpan staleAfter)
    {
        if (string.IsNullOrWhiteSpace(directory))
            throw new ArgumentException("A download directory is required.", nameof(directory));
        if (staleAfter < TimeSpan.Zero || staleAfter == Timeout.InfiniteTimeSpan)
            throw new ArgumentOutOfRangeException(nameof(staleAfter));
    }

    /// <summary>

    /// 枚举根目录及按需递归的子目录。

    /// </summary>
    /// <param name="directory">枚举起始目录。</param>
    /// <param name="recursive">是否递归枚举子目录。</param>
    /// <param name="cancellationToken">取消枚举的令牌。</param>
    /// <param name="fileSystem">文件系统适配器。</param>
    /// <returns>去重后的目录路径。</returns>
    private static IEnumerable<string> FindDirectories(string directory, bool recursive,
        CancellationToken cancellationToken, IResumableDownloadFileSystem fileSystem)
    {
        var pending = new Queue<string>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        pending.Enqueue(directory);

        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var current = pending.Dequeue();
            if (!visited.Add(current)) continue;
            yield return current;
            if (!recursive) continue;

            foreach (var child in fileSystem.GetDirectories(current))
            {
                cancellationToken.ThrowIfCancellationRequested();
                pending.Enqueue(Path.GetFullPath(child));
            }
        }
    }

    /// <summary>

    /// 从侧车文件路径中提取对应的下载目标路径。

    /// </summary>
    /// <param name="directory">待扫描目录。</param>
    /// <param name="cancellationToken">取消扫描的令牌。</param>
    /// <param name="fileSystem">文件系统适配器。</param>
    /// <returns>目录中发现的目标路径集合。</returns>
    private static HashSet<string> FindRoots(string directory, CancellationToken cancellationToken,
        IResumableDownloadFileSystem fileSystem)
    {
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in fileSystem.GetFiles(directory, "*" + PartialSuffix))
        {
            cancellationToken.ThrowIfCancellationRequested();
            roots.Add(path.Substring(0, path.Length - PartialSuffix.Length));
        }
        foreach (var path in fileSystem.GetFiles(directory, "*" + StateSuffix))
        {
            cancellationToken.ThrowIfCancellationRequested();
            roots.Add(path.Substring(0, path.Length - StateSuffix.Length));
        }
        foreach (var path in fileSystem.GetFiles(directory, "*" + LockSuffix))
        {
            cancellationToken.ThrowIfCancellationRequested();
            roots.Add(path.Substring(0, path.Length - LockSuffix.Length));
        }
        foreach (var path in fileSystem.GetFiles(directory, "*" + StateSuffix + ".*.tmp"))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var marker = path.IndexOf(StateTempMarker, StringComparison.OrdinalIgnoreCase);
            if (marker >= 0) roots.Add(path.Substring(0, marker));
        }
        return roots;
    }

    /// <summary>

    /// 根据当前时间和保留时长计算过期截止时间。

    /// </summary>
    /// <param name="now">当前 UTC 时间。</param>
    /// <param name="staleAfter">侧车文件至少应保留的时间。</param>
    /// <returns>早于或等于该时间的文件可视为过期。</returns>
    private static DateTime GetCutoff(DateTime now, TimeSpan staleAfter)
        => staleAfter.Ticks >= now.Ticks ? DateTime.MinValue : now.Subtract(staleAfter);

    /// <summary>

    /// 判断文件是否早于截止时间且可读取其修改时间。

    /// </summary>
    /// <param name="path">待检查文件路径。</param>
    /// <param name="cutoff">过期截止时间。</param>
    /// <param name="fileSystem">文件系统适配器。</param>
    /// <returns>文件存在且不晚于截止时间时返回 <see langword="true"/>。</returns>
    private static bool IsStale(string path, DateTime cutoff, IResumableDownloadFileSystem fileSystem)
    {
        if (!fileSystem.FileExists(path)) return false;
        try { return fileSystem.GetLastWriteTimeUtc(path) <= cutoff; }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }

    /// <summary>

    /// 尝试独占打开目标对应的清理锁。

    /// </summary>
    /// <param name="path">锁文件路径。</param>
    /// <param name="fileSystem">文件系统适配器。</param>
    /// <returns>取得锁时返回锁流；锁被占用或无权访问时返回 <see langword="null"/>。</returns>
    private static FileStream TryAcquireLock(string path, IResumableDownloadFileSystem fileSystem)
    {
        try
        {
            return fileSystem.OpenLock(path);
        }
        catch (IOException) { return null; }
        catch (UnauthorizedAccessException) { return null; }
    }

    /// <summary>

    /// 尝试删除文件，并将常见文件系统拒绝视为未删除。

    /// </summary>
    /// <param name="path">待删除文件路径。</param>
    /// <param name="fileSystem">文件系统适配器。</param>
    /// <returns>文件被删除时返回 <see langword="true"/>。</returns>
    private static bool TryDelete(string path, IResumableDownloadFileSystem fileSystem)
    {
        try
        {
            return fileSystem.Delete(path);
        }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }
}
