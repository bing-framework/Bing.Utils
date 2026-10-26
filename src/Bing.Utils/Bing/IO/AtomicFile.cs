namespace Bing.IO;

/// <summary>
/// 通过同目录临时文件和原子提交写入文件。
/// </summary>
/// <remarks>
/// 提交前的失败和取消保留原文件。平台不支持原子替换时直接失败，不降级复制，不承诺断电持久性或并发写入串行化。
/// 提交是不可取消的最后一步；提交成功后返回成功，不再因迟到取消报告失败。
/// </remarks>
public static class AtomicFile
{
    /// <summary>
    /// 从源当前位置写入临时文件并原子提交。
    /// </summary>
    /// <param name="path">目标文件路径，父目录须存在。</param>
    /// <param name="source">可读源。</param>
    /// <param name="options">传输限额、进度与校验。</param>
    /// <param name="leaveOpen">是否保留源。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>已提交内容的计数和校验值。</returns>
    public static StreamTransferResult Write(string path, Stream source, StreamTransferOptions options = null, bool leaveOpen = true, CancellationToken cancellationToken = default)
        => WriteCore(path, source, options, leaveOpen, cancellationToken, false).GetAwaiter().GetResult();

    /// <summary>
    /// 异步写入临时文件并原子提交，失败清理临时文件。
    /// </summary>
    /// <param name="path">目标文件路径，父目录须存在。</param>
    /// <param name="source">可读源。</param>
    /// <param name="options">传输限额、进度与校验。</param>
    /// <param name="leaveOpen">是否保留源。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>已提交内容的计数和校验值。</returns>
    public static Task<StreamTransferResult> WriteAsync(string path, Stream source, StreamTransferOptions options = null, bool leaveOpen = true, CancellationToken cancellationToken = default)
        => WriteCore(path, source, options, leaveOpen, cancellationToken, true);

    /// <summary>
    /// 将字符串以无 BOM UTF-8 原子写入文件。
    /// </summary>
    /// <param name="path">目标路径。</param>
    /// <param name="content">文本，不能为 null。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public static async Task WriteTextAsync(string path, string content, CancellationToken cancellationToken = default)
    {
        if (content == null) throw new ArgumentNullException(nameof(content));
        cancellationToken.ThrowIfCancellationRequested();
        using var input = new MemoryStream(new UTF8Encoding(false, true).GetBytes(content));
        await WriteAsync(path, input, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 共享临时文件清理和提交边界。
    /// </summary>
    private static async Task<StreamTransferResult> WriteCore(string path, Stream source, StreamTransferOptions options, bool leaveOpen, CancellationToken token, bool asynchronous)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("路径不能为空。", nameof(path));
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (!source.CanRead) throw new ArgumentException("源不可读。", nameof(source));
        var config = (options ?? new StreamTransferOptions()).Snapshot();
        var fullPath = Path.GetFullPath(path);
        var temporary = Path.Combine(Path.GetDirectoryName(fullPath), ".bing-atomic-" + Guid.NewGuid().ToString("N") + ".tmp");
        token.ThrowIfCancellationRequested();
        var created = false;
        try
        {
            StreamTransferResult result;
            using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920,
                asynchronous ? FileOptions.Asynchronous | FileOptions.SequentialScan : FileOptions.SequentialScan))
            {
                created = true;
                result = asynchronous
                    ? await StreamTransfer.CopyAsync(source, output, config, cancellationToken: token).ConfigureAwait(false)
                    : StreamTransfer.Copy(source, output, config, cancellationToken: token);
                if (asynchronous) await output.FlushAsync(token).ConfigureAwait(false);
                else output.Flush();
            }
            token.ThrowIfCancellationRequested();
            if (File.Exists(fullPath)) File.Replace(temporary, fullPath, null);
            else File.Move(temporary, fullPath);
            created = false;
            return result;
        }
        finally
        {
            try { if (created) File.Delete(temporary); }
            finally { if (!leaveOpen) source.Dispose(); }
        }
    }
}
