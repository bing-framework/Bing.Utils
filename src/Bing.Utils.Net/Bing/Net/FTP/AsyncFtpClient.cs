using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;

namespace Bing.Net.FTP;

/// <summary>
/// 使用 FluentFTP 原生异步 API 执行 FTP 操作。
/// </summary>
/// <remarks>
/// 同一实例内的命令由单个异步信号量串行执行。取消正在等待信号量的操作不会影响当前命令。
/// 文件流和网络操作不会通过 <see cref="Task.Run(Action)"/> 包装同步 I/O。调用 <see cref="Dispose"/> 会同步等待当前命令结束。
/// </remarks>
public sealed partial class AsyncFtpClient : IFtpAsyncClient, IFtpAsyncDirectoryClient
{
    private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
    private readonly IAsyncFtpBackend _backend;
    private volatile bool _disposed;

    /// <summary>
    /// 使用严格校验的选项初始化异步 FTP 客户端。
    /// </summary>
    /// <param name="options">连接和传输选项；构造时会验证并复制选项快照。</param>
    public AsyncFtpClient(AsyncFtpClientOptions options)
        : this(options, snapshot => new FluentFtpAsyncBackend(snapshot))
    {
    }

    internal AsyncFtpClient(
        AsyncFtpClientOptions options,
        Func<AsyncFtpClientOptions, IAsyncFtpBackend> backendFactory)
    {
        if (backendFactory == null)
            throw new ArgumentNullException(nameof(backendFactory));

        var snapshot = AsyncFtpClientOptions.CreateValidatedSnapshot(options);
        _backend = backendFactory(snapshot) ?? throw new InvalidOperationException("FTP 异步后端工厂返回了空值。");
    }

    /// <inheritdoc />
    public bool IsConnected => !_disposed && _backend.IsConnected;

    /// <inheritdoc />
    public Task ConnectAsync(CancellationToken cancellationToken = default) =>
        ExecuteAsync(async token =>
        {
            if (!_backend.IsConnected)
                await _backend.ConnectAsync(token).ConfigureAwait(false);
        }, cancellationToken);

    /// <inheritdoc />
    public Task DisconnectAsync(CancellationToken cancellationToken = default) =>
        ExecuteAsync(async token =>
        {
            if (_backend.IsConnected)
                await _backend.DisconnectAsync(token).ConfigureAwait(false);
        }, cancellationToken);

    /// <inheritdoc />
    public Task<bool> FileExistsAsync(string remotePath, CancellationToken cancellationToken = default)
    {
        ValidatePath(remotePath, nameof(remotePath));
        return ExecuteAsync(token => _backend.FileExistsAsync(remotePath, token), cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> DirectoryExistsAsync(string remotePath, CancellationToken cancellationToken = default)
    {
        ValidatePath(remotePath, nameof(remotePath));
        return ExecuteAsync(token => _backend.DirectoryExistsAsync(remotePath, token), cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> GetFilesAsync(
        string remoteDirectory,
        CancellationToken cancellationToken = default)
    {
        ValidatePath(remoteDirectory, nameof(remoteDirectory));
        return ExecuteAsync(token => _backend.GetFilesAsync(remoteDirectory, token), cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> GetDirectoriesAsync(
        string remoteDirectory,
        CancellationToken cancellationToken = default)
    {
        ValidatePath(remoteDirectory, nameof(remoteDirectory));
        return ExecuteAsync(token => _backend.GetDirectoriesAsync(remoteDirectory, token), cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> UploadFileAsync(
        string localPath,
        string remotePath,
        IProgress<FtpProgress> progress = null,
        CancellationToken cancellationToken = default)
    {
        ValidatePath(localPath, nameof(localPath));
        ValidatePath(remotePath, nameof(remotePath));
        return ExecuteAsync(token => _backend.UploadFileAsync(localPath, remotePath, progress, token), cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> DownloadFileAsync(
        string localPath,
        string remotePath,
        IProgress<FtpProgress> progress = null,
        CancellationToken cancellationToken = default)
    {
        ValidatePath(localPath, nameof(localPath));
        ValidatePath(remotePath, nameof(remotePath));
        return ExecuteAsync(token => _backend.DownloadFileAsync(localPath, remotePath, progress, token), cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> UploadStreamAsync(
        Stream source,
        string remotePath,
        IProgress<FtpProgress> progress = null,
        CancellationToken cancellationToken = default)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (!source.CanRead)
            throw new ArgumentException("上传源流必须可读。", nameof(source));
        ValidatePath(remotePath, nameof(remotePath));

        return ExecuteAsync(
            token => _backend.UploadStreamAsync(new NonClosingStream(source), remotePath, progress, token),
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> DownloadStreamAsync(
        Stream destination,
        string remotePath,
        IProgress<FtpProgress> progress = null,
        CancellationToken cancellationToken = default)
    {
        if (destination == null)
            throw new ArgumentNullException(nameof(destination));
        if (!destination.CanWrite)
            throw new ArgumentException("下载目标流必须可写。", nameof(destination));
        ValidatePath(remotePath, nameof(remotePath));

        return ExecuteAsync(
            token => _backend.DownloadStreamAsync(new NonClosingStream(destination), remotePath, progress, token),
            cancellationToken);
    }

    /// <inheritdoc />
    public Task DeleteFileAsync(string remotePath, CancellationToken cancellationToken = default)
    {
        ValidatePath(remotePath, nameof(remotePath));
        return ExecuteAsync(token => _backend.DeleteFileAsync(remotePath, token), cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> CreateDirectoryAsync(string remotePath, CancellationToken cancellationToken = default)
    {
        ValidatePath(remotePath, nameof(remotePath));
        return ExecuteAsync(token => _backend.CreateDirectoryAsync(remotePath, token), cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> MoveFileAsync(
        string remotePath,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        ValidatePath(remotePath, nameof(remotePath));
        ValidatePath(destinationPath, nameof(destinationPath));
        return ExecuteAsync(
            token => _backend.MoveFileAsync(remotePath, destinationPath, token),
            cancellationToken);
    }

    /// <summary>
    /// 等待当前实例的操作门闩并释放底层 FTP 客户端。
    /// </summary>
    /// <remarks>若调用时有 FTP 命令正在执行，此方法会同步等待该命令结束。</remarks>
    public void Dispose()
    {
        _gate.Wait();
        try
        {
            if (_disposed)
                return;

            _disposed = true;
            _backend.Dispose();
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            await operation(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            return await operation(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AsyncFtpClient));
    }

    private static void ValidatePath(string path, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("路径不能为空。", parameterName);
    }

    private sealed class NonClosingStream : Stream
    {
        private readonly Stream _inner;

        public NonClosingStream(Stream inner)
        {
            _inner = inner;
        }

        public override bool CanRead => _inner.CanRead;

        public override bool CanSeek => _inner.CanSeek;

        public override bool CanWrite => _inner.CanWrite;

        public override long Length => _inner.Length;

        public override long Position
        {
            get => _inner.Position;
            set => _inner.Position = value;
        }

        public override void Flush() => _inner.Flush();

        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);

        public override void SetLength(long value) => _inner.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);

        public override Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken) =>
            _inner.ReadAsync(buffer, offset, count, cancellationToken);

        public override Task WriteAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken) =>
            _inner.WriteAsync(buffer, offset, count, cancellationToken);

        protected override void Dispose(bool disposing)
        {
        }
    }
}
