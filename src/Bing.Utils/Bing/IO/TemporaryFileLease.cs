namespace Bing.IO;

/// <summary>临时文件租约。Stream 可定位且限制最大长度；租约释放前清理器不会删除文件。</summary>
/// <remarks>调用方负责释放租约。只关闭 Stream 不会释放活动租约；不得绕过 Stream 按 FilePath 写入。
/// 同一流不支持并发操作；释放时也不能有未完成的读写。释放不保证持久化到物理磁盘。</remarks>
public sealed class TemporaryFileLease : IDisposable, IAsyncDisposable
{
    private readonly TemporaryFileStore _store;
    private readonly string _entry;
    private readonly FileStream _leaseLock;
    private readonly bool _deleteOnDispose;
    private bool _disposed;

    internal TemporaryFileLease(TemporaryFileStore store, string entry, string filePath, long maxBytes, FileStream body, FileStream leaseLock, bool deleteOnDispose)
    {
        _store = store; _entry = entry; FilePath = filePath; MaxBytes = maxBytes;
        Stream = new QuotaStream(body, maxBytes); _leaseLock = leaseLock; _deleteOnDispose = deleteOnDispose;
    }
    /// <summary>正文完整路径，供已完成文件的外部只读使用。</summary>
    public string FilePath { get; }
    /// <summary>预留的正文最大长度。</summary>
    public long MaxBytes { get; private set; }
    /// <summary>获取唯一的有界读写流；初始位置为零，由租约负责最终释放。</summary>
    public Stream Stream { get; }
    /// <summary>
    /// 获取可用于重开保留文件的条目标识。
    /// </summary>
    public string Id => Path.GetFileName(_entry);

    /// <summary>
    /// 获取过期清理保护原因；null 表示允许过期清理。
    /// </summary>
    public string RetentionReason { get { ThrowIfDisposed(); return TemporaryFileStore.ReadRetention(_entry); } }

    /// <summary>
    /// 明确保留跨操作资源的额度；非 null 标记阻止过期清理，null 解除保护。
    /// </summary>
    /// <remarks>保护不改变 deleteOnDispose 或显式 Delete 的含义；调用方负责最终释放，不得用于无限泄漏额度。</remarks>
    public void SetRetention(string reason) { ThrowIfDisposed(); _store.SetRetention(_entry, reason); }

    /// <summary>
    /// 关闭租约并明确删除正文、保留标记和额度；删除失败可重新 OpenFile 后重试。
    /// </summary>
    public void Delete()
    {
        ThrowIfDisposed(); _disposed = true;
        try { Stream.Dispose(); } finally { _leaseLock.Dispose(); }
        _store.DeleteLease(_entry);
    }

    /// <summary>
    /// 调整预留上限，不截断正文；新上限不能小于当前长度或位置。
    /// </summary>
    /// <remarks>不得与流读写并发调用；配额不足或磁盘更新失败时保留旧上限。</remarks>
    public void Resize(long maxBytes)
    {
        ThrowIfDisposed();
        if (maxBytes < 0) throw new ArgumentOutOfRangeException(nameof(maxBytes));
        if (maxBytes < Stream.Length || maxBytes < Stream.Position)
            throw new ArgumentException("The reservation cannot be smaller than length or position.", nameof(maxBytes));
        _store.ResizeReservation(_entry, maxBytes, () => { ((QuotaStream)Stream).Maximum = maxBytes; MaxBytes = maxBytes; });
    }

    /// <summary>
    /// 将正文原子移动或替换到最终路径；成功后租约关闭并释放预留。
    /// </summary>
    /// <remarks>目标父目录须存在且与临时文件位于同一文件系统；不降级复制。
    /// 提交前取消保留正文；提交成功不再响应迟到取消。失败可继续使用 Stream 或重试。
    /// 不得与流操作并发调用；提交后的元数据清理失败会抛出，但目标已经提交。</remarks>
    public void Promote(string destinationPath, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(destinationPath)) throw new ArgumentException("A destination is required.", nameof(destinationPath));
        var target = Path.GetFullPath(destinationPath);
        var root = _store.DirectoryPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (target.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The destination must be outside the temporary store.", nameof(destinationPath));
        cancellationToken.ThrowIfCancellationRequested();
        var stream = (QuotaStream)Stream;
        stream.Inner.Flush(true);
        cancellationToken.ThrowIfCancellationRequested();
        var position = stream.Position;
        stream.Inner.Dispose();
        try
        {
            if (overwrite && File.Exists(target)) File.Replace(FilePath, target, null);
            else CommitNewFile(FilePath, target);
        }
        catch
        {
            stream.Inner = new FileStream(FilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read, 4096, FileOptions.Asynchronous);
            stream.Inner.Position = position;
            throw;
        }
        _disposed = true;
        _leaseLock.Dispose();
        _store.DeleteLease(_entry);
    }

    /// <summary>
    /// 新目标只允许同文件系统提交。Windows 禁止移动降级复制；POSIX 原子建立目标硬链接，源由提交后的租约清理移除。
    /// </summary>
    private static void CommitNewFile(string source, string target)
    {
        bool success;
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            success = MoveFileEx(source, target, 0);
        else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux) ||
                 System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
            success = Link(System.Text.Encoding.UTF8.GetBytes(source + "\0"), System.Text.Encoding.UTF8.GetBytes(target + "\0")) == 0;
        else
            throw new PlatformNotSupportedException("Atomic promotion is supported on Windows, Linux and macOS.");
        if (!success)
            throw new IOException("Cannot atomically promote the temporary file on this filesystem.",
                new System.ComponentModel.Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error()));
    }

    /// <summary>
    /// 不允许跨卷复制或覆盖现有目标的 Windows 移动入口。
    /// </summary>
    [System.Runtime.InteropServices.DllImport("kernel32.dll", EntryPoint = "MoveFileExW", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool MoveFileEx(string source, string target, uint flags);

    /// <summary>
    /// POSIX 的同文件系统链接提交；已存在目标或不支持链接的文件系统均返回失败。
    /// </summary>
    [System.Runtime.InteropServices.DllImport("libc", EntryPoint = "link", SetLastError = true)]
    private static extern int Link(byte[] source, byte[] target);

    /// <summary>
    /// 释放正文和活动锁；与同步释放具有相同删除策略。
    /// </summary>
    public ValueTask DisposeAsync() { Dispose(); return default; }

    /// <summary>
    /// 拒绝已释放租约的管理操作。
    /// </summary>
    private void ThrowIfDisposed() { if (_disposed) throw new ObjectDisposedException(nameof(TemporaryFileLease)); }
    /// <summary>关闭流和活动锁；按创建选项保留或删除文件。幂等，删除失败可由存储清理器重试。</summary>
    public void Dispose()
    {
        if (_disposed) return;
        ReleaseHandles();
        if (_deleteOnDispose) _store.DeleteLease(_entry);
    }

    /// <summary>
    /// 批量创建回滚时只关闭句柄，目录清理由已持有根锁的存储执行。
    /// </summary>
    internal void ReleaseHandles()
    {
        if (_disposed) return;
        _disposed = true;
        try { Stream.Dispose(); } finally { _leaseLock.Dispose(); }
    }

    private sealed class QuotaStream : Stream
    {
        internal FileStream Inner;
        internal long Maximum;
        private FileStream _inner => Inner;
        private long _maximum => Maximum;
        internal QuotaStream(FileStream inner, long maximum) { Inner = inner; Maximum = maximum; }
        public override bool CanRead => _inner.CanRead;
        public override bool CanWrite => _inner.CanWrite;
        public override bool CanSeek => _inner.CanSeek;
        public override long Length => _inner.Length;
        public override long Position { get => _inner.Position; set { CheckLength(value); _inner.Position = value; } }
        public override void Flush() => _inner.Flush();
        public override Task FlushAsync(CancellationToken token) => _inner.FlushAsync(token);
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token) => _inner.ReadAsync(buffer, offset, count, token);
        public override long Seek(long offset, SeekOrigin origin)
        {
            var start = origin == SeekOrigin.Begin ? 0 : origin == SeekOrigin.Current ? Position : origin == SeekOrigin.End ? Length : throw new ArgumentOutOfRangeException(nameof(origin));
            Position = checked(start + offset); return Position;
        }
        public override void SetLength(long value) { CheckLength(value); _inner.SetLength(value); }
        public override void Write(byte[] buffer, int offset, int count) { CheckWrite(buffer, offset, count); _inner.Write(buffer, offset, count); }
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken token)
        {
            CheckWrite(buffer, offset, count); token.ThrowIfCancellationRequested(); return _inner.WriteAsync(buffer, offset, count, token);
        }
        private void CheckWrite(byte[] buffer, int offset, int count)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || count < 0 || offset > buffer.Length - count) throw new ArgumentOutOfRangeException(nameof(count));
            if (Position > _maximum - count) throw new IOException("The temporary file length quota would be exceeded.");
        }
        private void CheckLength(long value)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            if (value > _maximum) throw new IOException("The temporary file length quota would be exceeded.");
        }
        protected override void Dispose(bool disposing) { if (disposing) _inner.Dispose(); base.Dispose(disposing); }
    }
}
