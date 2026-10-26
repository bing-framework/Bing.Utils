using System.Buffers;
using System.Security.Cryptography;

namespace Bing.Http;

/// <summary>
/// 管理 HTTP 分片的临时文件写入和最终顺序合并。
/// </summary>
/// <remarks>该组件不发送 HTTP 请求，也不会自动删除或修改调用方的状态文件。</remarks>
public sealed partial class HttpDownloadSegmentFileStore
{
    /// <summary>
    /// 大分片每次最多复制 1 MiB，减少异步 I/O 次数，同时限制每路并发的临时内存。
    /// </summary>
    private const int MaxTransferBufferSize = 1024 * 1024;

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
        cancellationToken.ThrowIfCancellationRequested();
        if (range.Length <= 0) throw new ArgumentOutOfRangeException(nameof(range));
        Directory.CreateDirectory(_stagingDirectory);
        var path = SegmentPath(range);
        if (EnforceDiskReservation)
            foreach (var residue in Directory.EnumerateFiles(_stagingDirectory, Path.GetFileName(path) + ".*.tmp"))
                DeleteReservedFile(residue);
        var tempPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            string hash;
            using (var output = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write,
                FileShare.None, 4096, true))
            using (var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
            {
                var buffer = ArrayPool<byte>.Shared.Rent(TransferBufferSize(range.Length));
                try
                {
                    hash = await CopySegmentAsync(content, output, range.Length, buffer, sha,
                        cancellationToken).ConfigureAwait(false);
                }
                finally { ArrayPool<byte>.Shared.Return(buffer, clearArray: true); }
                output.Flush(true);
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (File.Exists(path)) File.Replace(tempPath, path, null);
            else File.Move(tempPath, path);
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
        cancellationToken.ThrowIfCancellationRequested();
        if (state.Segments.Any(segment => !segment.Completed || segment.BytesWritten != segment.End - segment.Start + 1))
            throw new InvalidDataException("All segments must be complete before merging.");

        var directory = Path.GetDirectoryName(_targetPath);
        if (string.IsNullOrEmpty(directory)) throw new InvalidDataException("The target directory is missing.");
        Directory.CreateDirectory(directory);
        var tempTarget = _targetPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var output = new FileStream(tempTarget, FileMode.CreateNew, FileAccess.Write,
                FileShare.None, 65536, true))
            using (var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
            {
                // 整次合并共享缓冲区和摘要实例，避免小分片反复租用、清零和构造。
                var largest = state.Segments.Count == 0 ? 1 : state.Segments.Max(segment => segment.Range.Length);
                var buffer = ArrayPool<byte>.Shared.Rent(TransferBufferSize(largest));
                try
                {
                    foreach (var segment in state.Segments)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        // 校验与复制使用同一次读取；任何失败只影响临时目标，不提交旧文件。
                        var segmentPath = SegmentPath(segment.Range);
                        // 使用真正的异步句柄；复制器已有缓冲区，输入不再重复缓冲。
                        using (var input = OpenSegment(segmentPath))
                        {
                            if (input.Length != segment.Range.Length)
                                throw new InvalidDataException("A completed segment file has an invalid length.");
                            var hash = await CopySegmentAsync(input, output, segment.Range.Length, buffer,
                                string.IsNullOrEmpty(segment.Sha256) ? null : sha, cancellationToken,
                                verifyFileLength: true).ConfigureAwait(false);
                            if (!string.IsNullOrEmpty(segment.Sha256) &&
                                !string.Equals(hash, segment.Sha256, StringComparison.OrdinalIgnoreCase))
                                throw new InvalidDataException("A completed segment file has an invalid SHA-256 digest.");
                        }
                    }
                }
                finally { ArrayPool<byte>.Shared.Return(buffer, clearArray: true); }
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
    /// 按分片长度限制租用和归还时清零的范围；极小分片不再使用大块复制缓冲区。
    /// </summary>
    private static int TransferBufferSize(long length)
        => (int)Math.Min(MaxTransferBufferSize, Math.Max(1, length));

    /// <summary>
    /// 打开分片并保持缺失文件的既有异常契约，省去每片打开前额外的存在性查询。
    /// </summary>
    private static FileStream OpenSegment(string path)
    {
        try
        {
            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
        }
        catch (FileNotFoundException exception)
        {
            throw new InvalidDataException("A completed segment file is missing.", exception);
        }
        catch (DirectoryNotFoundException exception)
        {
            throw new InvalidDataException("A completed segment file is missing.", exception);
        }
        catch (UnauthorizedAccessException exception) when (Directory.Exists(path))
        {
            throw new InvalidDataException("A completed segment file is missing.", exception);
        }
        catch (IOException exception) when (Directory.Exists(path))
        {
            throw new InvalidDataException("A completed segment file is missing.", exception);
        }
    }

    /// <summary>
    /// 复制严格等长的分片；写入进行时计算摘要，等待写入结束后才复用缓冲区。
    /// </summary>
    /// <remarks>外部流多读一个字节确认结束；已打开的本地分片复核句柄长度，省去额外 EOF 读取。超额字节不写入。</remarks>
    private static async Task<string> CopySegmentAsync(Stream input, FileStream output, long length,
        byte[] buffer, IncrementalHash sha, CancellationToken cancellationToken, bool verifyFileLength = false)
    {
        long total = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var remaining = length - total;
            if (remaining == 0 && verifyFileLength)
            {
                if (input.Length != length) throw new InvalidDataException("A completed segment file has an invalid length.");
                break;
            }
            var requested = remaining >= buffer.Length ? buffer.Length : (int)remaining + 1;
#if NETSTANDARD2_0
            var read = await input.ReadAsync(buffer, 0, requested, cancellationToken).ConfigureAwait(false);
#else
            var read = await input.ReadAsync(buffer.AsMemory(0, requested), cancellationToken).ConfigureAwait(false);
#endif
            cancellationToken.ThrowIfCancellationRequested();
            if (read == 0) break;
            if (read > remaining) throw new InvalidDataException("The segment stream exceeds its range length.");
#if NETSTANDARD2_0
            var write = output.WriteAsync(buffer, 0, read, cancellationToken);
#else
            var write = output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
#endif
            // 无论摘要是否失败，都必须收尾已启动的写入，才允许归还缓冲区或关闭文件。
            try { sha?.AppendData(buffer, 0, read); }
            finally { await write.ConfigureAwait(false); }
            total += read;
        }
        if (total != length) throw new InvalidDataException("The segment stream ended before its range length.");
        cancellationToken.ThrowIfCancellationRequested();
        return sha == null ? null : BitConverter.ToString(sha.GetHashAndReset()).Replace("-", string.Empty).ToLowerInvariant();
    }

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
        // File.Delete 对不存在的文件直接返回；不再为每个分片多查一次元数据。
        try { File.Delete(path); }
        catch { }
    }

    /// <summary>
    /// 仅当目录存在且为空时尝试删除该目录。</summary>
    /// <param name="path">待检查的目录路径。</param>
    private static void TryDeleteEmptyDirectory(string path)
    {
        try
        {
            // 非递归删除只会删除空目录；非空或已删除时按既有清理约定忽略错误。
            Directory.Delete(path);
        }
        catch { }
    }
}
