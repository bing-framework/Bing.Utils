using System.Security.Cryptography;

namespace Bing.Http;

/// <summary>

/// 管理 HTTP 分片的临时文件写入和最终顺序合并。

/// </summary>
/// <remarks>该组件不发送 HTTP 请求，也不会自动删除或修改调用方的状态文件。</remarks>
public sealed class HttpDownloadSegmentFileStore
{
    /// <summary>
    /// 保存分片文件及清单的暂存目录。
    /// </summary>
    private readonly string _stagingDirectory;
    /// <summary>
    /// 分片全部完成后写入的目标文件路径。
    /// </summary>
    private readonly string _targetPath;

    /// <summary>

    /// 初始化一个 <see cref="HttpDownloadSegmentFileStore"/> 类型的实例。

    /// </summary>
    /// <param name="stagingDirectory">分片临时目录。</param>
    /// <param name="targetPath">最终目标文件路径。</param>
    public HttpDownloadSegmentFileStore(string stagingDirectory, string targetPath)
    {
        if (string.IsNullOrWhiteSpace(stagingDirectory))
            throw new ArgumentException("A staging directory is required.", nameof(stagingDirectory));
        if (string.IsNullOrWhiteSpace(targetPath))
            throw new ArgumentException("A target path is required.", nameof(targetPath));
        _stagingDirectory = Path.GetFullPath(stagingDirectory);
        _targetPath = Path.GetFullPath(targetPath);
    }

    /// <summary>

    /// 写入一个完整分片并返回可持久化的已完成状态。

    /// </summary>
    /// <param name="range">分片范围。</param>
    /// <param name="content">只包含该范围正文的可读流。</param>
    /// <param name="cancellationToken">取消写入。</param>
    /// <returns>写入完成且带 SHA-256 摘要的分片状态。</returns>
    public async Task<HttpDownloadSegmentState> WriteSegmentAsync(HttpDownloadRange range,
        Stream content, CancellationToken cancellationToken = default)
    {
        if (content == null) throw new ArgumentNullException(nameof(content));
        if (!content.CanRead) throw new ArgumentException("The segment stream must be readable.", nameof(content));
        Directory.CreateDirectory(_stagingDirectory);
        var path = SegmentPath(range);
        var tempPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var output = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write,
                FileShare.None, 81920, true))
            {
                var remaining = range.Length;
                var buffer = new byte[81920];
                while (remaining > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var read = await content.ReadAsync(buffer, 0,
                        (int)Math.Min(buffer.Length, remaining), cancellationToken).ConfigureAwait(false);
                    if (read == 0) throw new InvalidDataException("The segment stream ended before its range length.");
                    await output.WriteAsync(buffer, 0, read, cancellationToken).ConfigureAwait(false);
                    remaining -= read;
                }
                var extra = await content.ReadAsync(buffer, 0, 1, cancellationToken).ConfigureAwait(false);
                if (extra != 0) throw new InvalidDataException("The segment stream exceeded its range length.");
                output.Flush(true);
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (File.Exists(path)) File.Replace(tempPath, path, null);
            else File.Move(tempPath, path);
            var hash = ComputeSha256(path);
            return new HttpDownloadSegmentState
            {
                Start = range.Start,
                End = range.End,
                BytesWritten = range.Length,
                Completed = true,
                Sha256 = hash
            };
        }
        finally
        {
            TryDelete(tempPath);
        }
    }

    /// <summary>

    /// 清洗恢复状态，并移除不属于当前分片规划的残留文件。

    /// </summary>
    /// <param name="state">已验证的分片状态。</param>
    /// <returns>被降级的分片数量。</returns>
    public int Reconcile(HttpDownloadSegmentsState state)
    {
        HttpDownloadSegmentStateStore.Validate(state);
        if (!Directory.Exists(_stagingDirectory)) return 0;

        var expectedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var invalidated = 0;
        foreach (var segment in state.Segments)
        {
            var path = SegmentPath(segment.Range);
            expectedFiles.Add(Path.GetFullPath(path));
            if (!segment.Completed) continue;

            var valid = File.Exists(path) && new FileInfo(path).Length == segment.Range.Length;
            if (valid && !string.IsNullOrEmpty(segment.Sha256))
                valid = string.Equals(segment.Sha256, ComputeSha256(path), StringComparison.OrdinalIgnoreCase);
            else
                valid = false;
            if (valid) continue;

            segment.BytesWritten = 0;
            segment.Completed = false;
            segment.Sha256 = null;
            invalidated++;
        }

        foreach (var path in Directory.EnumerateFiles(_stagingDirectory))
        {
            if (!expectedFiles.Contains(Path.GetFullPath(path)) || path.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase))
                TryDelete(path);
        }
        return invalidated;
    }

    /// <summary>

    /// 验证全部分片后按范围顺序合并并原子替换目标文件。

    /// </summary>
    /// <param name="state">完整且已验证的分片状态。</param>
    /// <param name="cancellationToken">取消合并；取消不会替换目标文件。</param>
    public async Task MergeAsync(HttpDownloadSegmentsState state,
        CancellationToken cancellationToken = default)
        => await MergeAsync(state, null, cancellationToken).ConfigureAwait(false);

    /// <summary>

    /// 验证全部分片后执行最终正文校验，再原子替换目标文件。

    /// </summary>
    /// <param name="state">完整且已验证的分片状态。</param>
    /// <param name="validator">对目标临时文件执行的最终校验器。</param>
    /// <param name="cancellationToken">取消合并或校验；取消不会替换目标文件。</param>
    public async Task MergeAsync(HttpDownloadSegmentsState state,
        Func<Stream, CancellationToken, Task> validator,
        CancellationToken cancellationToken = default)
    {
        HttpDownloadSegmentStateStore.Validate(state);
        if (state.Segments.Any(segment => !segment.Completed || segment.BytesWritten != segment.End - segment.Start + 1))
            throw new InvalidDataException("All segments must be complete before merging.");

        foreach (var segment in state.Segments)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = SegmentPath(segment.Range);
            if (!File.Exists(path) || new FileInfo(path).Length != segment.Range.Length)
                throw new InvalidDataException("A completed segment file is missing or has an invalid length.");
            if (!string.IsNullOrEmpty(segment.Sha256) &&
                !string.Equals(segment.Sha256, ComputeSha256(path), StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("A segment checksum does not match its state.");
        }

        var directory = Path.GetDirectoryName(_targetPath);
        if (string.IsNullOrEmpty(directory)) throw new InvalidDataException("The target directory is missing.");
        Directory.CreateDirectory(directory);
        var tempTarget = _targetPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var output = new FileStream(tempTarget, FileMode.CreateNew, FileAccess.Write,
                FileShare.None, 81920, true))
            {
                foreach (var segment in state.Segments)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    using (var input = File.OpenRead(SegmentPath(segment.Range)))
                        await input.CopyToAsync(output, 81920, cancellationToken).ConfigureAwait(false);
                }
                output.Flush(true);
            }

            if (validator != null)
            {
                using (var validationStream = File.OpenRead(tempTarget))
                    await validator(validationStream, cancellationToken).ConfigureAwait(false);
            }
            cancellationToken.ThrowIfCancellationRequested();
            if (File.Exists(_targetPath)) File.Replace(tempTarget, _targetPath, null);
            else File.Move(tempTarget, _targetPath);
            foreach (var segment in state.Segments) TryDelete(SegmentPath(segment.Range));
            TryDeleteEmptyDirectory(_stagingDirectory);
        }
        finally
        {
            TryDelete(tempTarget);
        }
    }

    /// <summary>

    /// 根据范围起止位置生成分片文件路径。

    /// </summary>
    private string SegmentPath(HttpDownloadRange range)
        => Path.Combine(_stagingDirectory, "segment-" + range.Start + "-" + range.End + ".part");

    /// <summary>

    /// 计算指定分片文件的 SHA-256 摘要。

    /// </summary>
    private static string ComputeSha256(string path)
    {
        using (var sha = SHA256.Create())
        using (var stream = File.OpenRead(path))
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
    }

    /// <summary>

    /// 尝试删除缓存或临时文件。

    /// </summary>
    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { }
    }

    /// <summary>
    /// 仅当目录存在且为空时尝试删除该目录。</summary>
    /// <param name="path">待检查的目录路径。</param>
    private static void TryDeleteEmptyDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path) && !Directory.EnumerateFileSystemEntries(path).Any())
                Directory.Delete(path);
        }
        catch { }
    }
}
