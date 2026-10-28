using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Bing.IO;
using Bing.Operations;
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Sftp;

[assembly: InternalsVisibleTo("Bing.Utils.Sftp.Tests")]
[assembly: InternalsVisibleTo("Bing.Utils.Sftp.Tests.Integration")]

namespace Bing.Sftp;

/// <summary>
/// SFTP 连接配置。
/// </summary>
public sealed class SftpConnectionOptions
{
    /// <summary>获取或设置服务器主机名。</summary>
    public string Host { get; set; }

    /// <summary>获取或设置服务器端口，默认值为 22。</summary>
    public int Port { get; set; } = 22;

    /// <summary>获取或设置认证用户名。</summary>
    public string UserName { get; set; }

    /// <summary>获取或设置密码；使用私钥认证时可为 null。</summary>
    public string Password { get; set; }

    /// <summary>获取或设置私钥文件路径；为 null 时使用密码认证。</summary>
    public string PrivateKeyPath { get; set; }

    /// <summary>获取或设置私钥口令。</summary>
    public string PrivateKeyPassPhrase { get; set; }

    /// <summary>获取或设置期望主机密钥的 SHA-256 Base64 指纹。</summary>
    /// <remarks>格式为 OpenSSH 指纹，可带或不带 <c>SHA256:</c> 前缀，末尾填充等号可省略。</remarks>
    public string HostKeySha256 { get; set; }
}

/// <summary>表示远端目录项或文件的只读信息快照。</summary>
public sealed class SftpFileInfo
{
    internal SftpFileInfo(string name, string fullName, long length, bool isDirectory, bool isSymbolicLink,
        bool isRegularFile, DateTime lastWriteTimeUtc)
    {
        Name = name;
        FullName = fullName;
        Length = length;
        IsDirectory = isDirectory;
        IsSymbolicLink = isSymbolicLink;
        IsRegularFile = isRegularFile;
        LastWriteTimeUtc = lastWriteTimeUtc;
    }

    /// <summary>获取目录项名称。</summary>
    public string Name { get; }

    /// <summary>获取服务器返回的远端 POSIX 路径。</summary>
    public string FullName { get; }

    /// <summary>获取文件长度；目录长度由服务器决定。</summary>
    public long Length { get; }

    /// <summary>获取是否为目录。</summary>
    public bool IsDirectory { get; }

    /// <summary>获取是否为符号链接。</summary>
    public bool IsSymbolicLink { get; }

    /// <summary>获取是否为普通文件。</summary>
    public bool IsRegularFile { get; }

    /// <summary>获取服务器报告的 UTC 修改时间。</summary>
    public DateTime LastWriteTimeUtc { get; }

    internal static SftpFileInfo From(ISftpFile file) => new(file.Name, file.FullName, file.Length,
        file.IsDirectory, file.IsSymbolicLink, file.IsRegularFile, file.LastWriteTimeUtc);

    internal static SftpFileInfo From(string path, SftpFileAttributes attributes) => new(
        GetRemoteName(path), path, attributes.Size, attributes.IsDirectory, attributes.IsSymbolicLink,
        attributes.IsRegularFile, attributes.LastWriteTimeUtc);

    private static string GetRemoteName(string path)
    {
        var trimmed = path.TrimEnd('/');
        var index = trimmed.LastIndexOf('/');
        return index < 0 ? trimmed : trimmed.Substring(index + 1);
    }
}

/// <summary>表示一次完成的 SFTP 传输及其发布状态。</summary>
public sealed class SftpTransferResult
{
    private readonly List<Exception> _cleanupErrors = new();
    private readonly List<Exception> _progressObserverErrors = new();

    internal SftpTransferResult(string path, long bytesTransferred, bool completed, bool committed)
    {
        Path = path;
        BytesTransferred = bytesTransferred;
        Completed = completed;
        Committed = committed;
    }

    /// <summary>获取本次操作的目标路径；SFTP 路径使用远端 POSIX 格式。</summary>
    public string Path { get; }

    /// <summary>获取成功复制的字节数。</summary>
    public long BytesTransferred { get; }

    /// <summary>获取数据复制是否全部完成。</summary>
    public bool Completed { get; }

    /// <summary>获取暂存内容是否已发布到目标路径。</summary>
    /// <remarks>流下载直接写入调用方流，因此成功完成时此值仍为 false。</remarks>
    public bool Committed { get; }

    /// <summary>获取操作成功后遇到的清理错误；已发布的结果不会因此被报告为未提交。</summary>
    public IReadOnlyList<Exception> CleanupErrors => _cleanupErrors.AsReadOnly();

    /// <summary>获取进度观察器自身抛出的异常；观察器异常不会中断或回滚传输。</summary>
    public IReadOnlyList<Exception> ProgressObserverErrors => _progressObserverErrors.AsReadOnly();

    internal void AddCleanupError(Exception error) => _cleanupErrors.Add(error);
    internal void AddProgressObserverError(Exception error) => _progressObserverErrors.Add(error);
}

/// <summary>表示 rename 请求后无法确认远端发布结果的异常。</summary>
public sealed class SftpCommitOutcomeUnknownException : IOException
{
    internal SftpCommitOutcomeUnknownException(string remotePath, Exception innerException)
        : base($"远端提交结果不确定：{remotePath}", innerException)
    {
        RemotePath = remotePath;
    }

    /// <summary>获取提交操作的远端目标路径。</summary>
    public string RemotePath { get; }

    /// <summary>获取提交是否可能已在远端完成。</summary>
    public bool OutcomeUnknown => true;
}

/// <summary>提供 SFTP 上传与下载；每个操作使用独立连接。</summary>
/// <remarks>
/// 上传先写入同目录唯一暂存文件，再以 rename 发布；已有目标默认拒绝。流下载直接写入目标流，失败时可能留下部分输出；
/// 文件下载则先写入本地同目录临时文件，再发布。每次调用都会等待其连接、复制、发布和清理，不会在后台遗留传输任务。
/// 进度观察器的 Report 仅发送通知；事件序号用于排序，操作 Task 完成才表示本次调用已结束。
/// </remarks>
public sealed class SftpTransferClient
{
    private readonly SftpConnectionOptions _options;

    /// <summary>初始化 SFTP 客户端。</summary>
    /// <param name="options">连接配置；必须提供 SHA-256 主机指纹及密码或私钥。</param>
    public SftpTransferClient(SftpConnectionOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>异步列出远端目录中的名称，不包含 <c>.</c> 和 <c>..</c>。</summary>
    /// <param name="path">远端 POSIX 目录路径。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>目录项名称。</returns>
    public async Task<IReadOnlyList<string>> ListDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        var entries = await ListDirectoryEntriesAsync(path, cancellationToken).ConfigureAwait(false);
        return entries.Select(entry => entry.Name).ToArray();
    }

    /// <summary>异步枚举远端目录项及其文件属性。</summary>
    /// <param name="path">远端 POSIX 目录路径。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>目录项快照列表。</returns>
    public Task<IReadOnlyList<SftpFileInfo>> ListDirectoryEntriesAsync(string path,
        CancellationToken cancellationToken = default)
    {
        ValidateRemotePath(path);
        return RunAsync<IReadOnlyList<SftpFileInfo>>(async client =>
        {
            var entries = new List<SftpFileInfo>();
            await foreach (var file in client.ListDirectoryAsync(path, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (file.Name != "." && file.Name != "..") entries.Add(SftpFileInfo.From(file));
            }

            return entries.AsReadOnly();
        }, cancellationToken);
    }

    /// <summary>异步获取一个远端文件或目录的属性。</summary>
    /// <param name="remotePath">远端 POSIX 路径。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>远端文件属性快照。</returns>
    public Task<SftpFileInfo> GetFileInfoAsync(string remotePath, CancellationToken cancellationToken = default)
    {
        ValidateRemotePath(remotePath);
        return RunAsync(async client =>
        {
            var attributes = await client.GetAttributesAsync(remotePath, cancellationToken).ConfigureAwait(false);
            return SftpFileInfo.From(remotePath, attributes);
        }, cancellationToken);
    }

    /// <summary>异步上传一个流，并在暂存文件 rename 成功后发布到远端目标。</summary>
    /// <param name="source">可读源流。</param>
    /// <param name="remotePath">最终远端 POSIX 路径；目标已存在时拒绝上传。</param>
    /// <param name="leaveOpen">是否保留源流，默认值为 true。</param>
    /// <param name="progress">操作进度观察器。</param>
    /// <param name="cancellationToken">取消连接、读取、写入或 rename。</param>
    /// <returns>复制完成且确认发布的结果。</returns>
    public Task<SftpTransferResult> UploadAsync(Stream source, string remotePath, bool leaveOpen = true,
        IProgress<OperationProgress> progress = null, CancellationToken cancellationToken = default)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (!source.CanRead) throw new ArgumentException("源流不可读。", nameof(source));
        ValidateRemotePath(remotePath);
        var sequencedProgress = new SequencedProgress(progress, deferFinalStages: true);
        return WithOwnedStreamAsync(source, leaveOpen, async () =>
        {
            var result = await RunAsync(client => UploadCoreAsync(client, source, remotePath,
                sequencedProgress, cancellationToken), cancellationToken).ConfigureAwait(false);
            AddProgressObserverErrors(result, sequencedProgress);
            return result;
        });
    }

    /// <summary>异步上传本地文件，已有远端目标时拒绝发布。</summary>
    /// <param name="localPath">本地源文件路径。</param>
    /// <param name="remotePath">最终远端 POSIX 路径。</param>
    /// <param name="progress">操作进度观察器。</param>
    /// <param name="cancellationToken">取消连接、读取、写入或 rename。</param>
    /// <returns>复制完成且确认发布的结果。</returns>
    public Task<SftpTransferResult> UploadFileAsync(string localPath, string remotePath,
        IProgress<OperationProgress> progress = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(localPath)) throw new ArgumentException("本地文件路径不能为空。", nameof(localPath));
        ValidateRemotePath(remotePath);
        var source = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        return UploadAsync(source, remotePath, leaveOpen: false, progress, cancellationToken);
    }

    /// <summary>异步下载远端文件到流；失败时目标流可能包含部分输出。</summary>
    /// <param name="remotePath">远端 POSIX 文件路径。</param>
    /// <param name="destination">可写目标流。</param>
    /// <param name="leaveOpen">是否保留目标流，默认值为 true。</param>
    /// <param name="progress">操作进度观察器。</param>
    /// <param name="cancellationToken">取消连接、远端读取或目标写入。</param>
    /// <returns>已完成的数据复制结果；此流式操作不发布文件路径。</returns>
    public Task<SftpTransferResult> DownloadAsync(string remotePath, Stream destination, bool leaveOpen = true,
        IProgress<OperationProgress> progress = null, CancellationToken cancellationToken = default)
    {
        if (destination == null) throw new ArgumentNullException(nameof(destination));
        if (!destination.CanWrite) throw new ArgumentException("目标流不可写。", nameof(destination));
        ValidateRemotePath(remotePath);
        var sequencedProgress = new SequencedProgress(progress, deferFinalStages: false);
        return WithOwnedStreamAsync(destination, leaveOpen, async () =>
        {
            var result = await RunAsync(client => DownloadCoreAsync(client, remotePath, destination,
                sequencedProgress, deferStageFinals: false, cancellationToken), cancellationToken).ConfigureAwait(false);
            AddProgressObserverErrors(result, sequencedProgress);
            return result;
        });
    }

    /// <summary>异步下载远端文件到本地文件，并在同目录临时文件完成后发布。</summary>
    /// <param name="remotePath">远端 POSIX 文件路径。</param>
    /// <param name="localPath">本地目标路径；已存在时拒绝覆盖。</param>
    /// <param name="progress">操作进度观察器。</param>
    /// <param name="cancellationToken">取消连接、读取、写入或发布前等待。</param>
    /// <returns>复制完成且确认发布的结果。</returns>
    public Task<SftpTransferResult> DownloadFileAsync(string remotePath, string localPath,
        IProgress<OperationProgress> progress = null, CancellationToken cancellationToken = default)
    {
        ValidateRemotePath(remotePath);
        if (string.IsNullOrWhiteSpace(localPath)) throw new ArgumentException("本地文件路径不能为空。", nameof(localPath));
        return DownloadFileCoreAsync(remotePath, localPath, progress, cancellationToken);
    }

    private async Task<SftpTransferResult> UploadCoreAsync(Renci.SshNet.SftpClient client, Stream source,
        string remotePath, SequencedProgress progress, CancellationToken cancellationToken)
    {
        var temporaryPath = CreateTemporaryPath(remotePath);
        Exception failure = null;
        SftpTransferResult result = null;
        long bytesTransferred = 0;
        try
        {
            if (await client.ExistsAsync(remotePath, cancellationToken).ConfigureAwait(false))
                throw new IOException("远端目标已存在，未修改现有文件。");

            Stream remoteStream = null;
            Exception streamFailure = null;
            try
            {
                remoteStream = await client.OpenAsync(temporaryPath, FileMode.CreateNew, FileAccess.Write,
                    cancellationToken).ConfigureAwait(false);
                var copied = await StreamTransfer.CopyAsync(source, remoteStream,
                    new StreamTransferOptions { OperationProgress = progress }, leaveOpen: true,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
                bytesTransferred = copied.BytesTransferred;
                await remoteStream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception error)
            {
                streamFailure = error;
            }
            finally
            {
                if (remoteStream != null)
                {
                    try { remoteStream.Dispose(); }
                    catch (Exception error)
                    {
                        if (streamFailure == null) streamFailure = error;
                        else AttachCleanupError(streamFailure, error);
                    }
                }
            }

            if (streamFailure != null) ExceptionDispatchInfo.Capture(streamFailure).Throw();
            progress.CompleteTransferStages();
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await client.RenameFileAsync(temporaryPath, remotePath, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException error)
            {
                MarkCommitOutcomeUnknown(error);
                throw;
            }
            catch (SftpException)
            {
                throw;
            }
            catch (Exception error)
            {
                throw new SftpCommitOutcomeUnknownException(remotePath, error);
            }

            progress.ReportCommit();
            result = new SftpTransferResult(remotePath, bytesTransferred, completed: true, committed: true);
        }
        catch (Exception error)
        {
            failure = error;
        }
        finally
        {
            failure = await CleanupRemoteTemporaryFileAsync(client, temporaryPath, failure, result)
                .ConfigureAwait(false);
            AttachProgressObserverErrors(failure, progress);
        }

        if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
        return result;
    }

    private async Task<SftpTransferResult> DownloadCoreAsync(Renci.SshNet.SftpClient client, string remotePath,
        Stream destination, SequencedProgress progress, bool deferStageFinals, CancellationToken cancellationToken)
    {
        var remoteStream = await client.OpenAsync(remotePath, FileMode.Open, FileAccess.Read, cancellationToken)
            .ConfigureAwait(false);
        Exception failure = null;
        long bytesTransferred = 0;
        try
        {
            var copied = await StreamTransfer.CopyAsync(remoteStream, destination,
                new StreamTransferOptions { OperationProgress = progress }, leaveOpen: true,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            bytesTransferred = copied.BytesTransferred;
        }
        catch (Exception error)
        {
            failure = error;
        }
        finally
        {
            try { remoteStream.Dispose(); }
            catch (Exception error)
            {
                if (failure == null) failure = error;
                else AttachCleanupError(failure, error);
            }
        }

        if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
        progress.MarkCopyCompleted();
        if (!deferStageFinals) progress.CompleteTransferStages();
        return new SftpTransferResult(remotePath, bytesTransferred, completed: true, committed: false);
    }

    private async Task<SftpTransferResult> DownloadFileCoreAsync(string remotePath, string localPath,
        IProgress<OperationProgress> observer, CancellationToken cancellationToken)
    {
        var fullPath = Path.GetFullPath(localPath);
        var directory = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrEmpty(directory)) directory = Directory.GetCurrentDirectory();
        var fileName = Path.GetFileName(fullPath);
        var temporaryPath = Path.Combine(directory, $".{fileName}.bing-sftp-{Guid.NewGuid():N}.tmp");
        var progress = new SequencedProgress(observer, deferFinalStages: true);
        FileStream temporaryFile = null;
        Exception failure = null;
        SftpTransferResult result = null;
        long bytesTransferred = 0;
        try
        {
            if (File.Exists(fullPath)) throw new IOException("本地目标已存在，未修改现有文件。");
            temporaryFile = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
            var transfer = await RunAsync(client => DownloadCoreAsync(client, remotePath, temporaryFile,
                progress, deferStageFinals: true, cancellationToken), cancellationToken).ConfigureAwait(false);
            bytesTransferred = transfer.BytesTransferred;
            await temporaryFile.FlushAsync(cancellationToken).ConfigureAwait(false);
            temporaryFile.Dispose();
            temporaryFile = null;
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, fullPath);
            progress.CompleteTransferStages();
            progress.ReportCommit();
            result = new SftpTransferResult(fullPath, bytesTransferred, completed: true, committed: true);
        }
        catch (Exception error)
        {
            failure = error;
        }
        finally
        {
            if (temporaryFile != null)
            {
                try { temporaryFile.Dispose(); }
                catch (Exception error)
                {
                    if (failure == null && result?.Committed == true) result.AddCleanupError(error);
                    else if (failure == null) failure = error;
                    else AttachCleanupError(failure, error);
                }
            }

            if (progress.CopyCompleted) progress.CompleteTransferStages();
            AddProgressObserverErrors(result, progress);
            AttachProgressObserverErrors(failure, progress);
            try
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
            catch (Exception error)
            {
                if (failure != null) AttachCleanupError(failure, error);
                else if (result?.Committed == true) result.AddCleanupError(error);
                else failure = error;
            }
        }

        if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
        return result;
    }

    private async Task<T> RunAsync<T>(Func<Renci.SshNet.SftpClient, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        Renci.SshNet.SftpClient client = null;
        Exception failure = null;
        T result = default;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            client = CreateClient();
            await client.ConnectAsync(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            result = await operation(client).ConfigureAwait(false);
        }
        catch (Exception error)
        {
            failure = error;
        }

        if (client != null)
        {
            try { client.Dispose(); }
            catch (Exception error)
            {
                if (failure != null) AttachCleanupError(failure, error);
                else if (result is SftpTransferResult transfer && transfer.Committed) transfer.AddCleanupError(error);
                else failure = error;
            }
        }

        if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
        return result;
    }

    private Renci.SshNet.SftpClient CreateClient()
    {
        ValidateOptions();
        AuthenticationMethod authentication = string.IsNullOrEmpty(_options.PrivateKeyPath)
            ? new PasswordAuthenticationMethod(_options.UserName, _options.Password)
            : new PrivateKeyAuthenticationMethod(_options.UserName,
                new PrivateKeyFile(_options.PrivateKeyPath, _options.PrivateKeyPassPhrase));
        var client = new Renci.SshNet.SftpClient(
            new ConnectionInfo(_options.Host, _options.Port, _options.UserName, authentication));
        client.HostKeyReceived += VerifyHostKey;
        return client;
    }

    private void VerifyHostKey(object sender, HostKeyEventArgs args) =>
        args.CanTrust = MatchesHostKey(args.HostKey, _options.HostKeySha256);

    internal static bool MatchesHostKey(byte[] hostKey, string expectedFingerprint)
    {
        if (hostKey == null || string.IsNullOrWhiteSpace(expectedFingerprint)) return false;
        var expected = expectedFingerprint.Trim();
        const string prefix = "SHA256:";
        if (expected.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) expected = expected.Substring(prefix.Length);
        expected = expected.TrimEnd('=');
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var actual = Convert.ToBase64String(sha256.ComputeHash(hostKey)).TrimEnd('=');
        return string.Equals(expected, actual, StringComparison.Ordinal);
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.Host)) throw new ArgumentException("主机名不能为空。", nameof(_options.Host));
        if (_options.Port is < 1 or > 65535) throw new ArgumentOutOfRangeException(nameof(_options.Port));
        if (string.IsNullOrWhiteSpace(_options.UserName)) throw new ArgumentException("用户名不能为空。", nameof(_options.UserName));
        if (string.IsNullOrWhiteSpace(_options.HostKeySha256))
            throw new ArgumentException("必须提供 SHA-256 主机密钥指纹。", nameof(_options.HostKeySha256));
        if (string.IsNullOrEmpty(_options.PrivateKeyPath) && _options.Password == null)
            throw new ArgumentException("必须提供密码或私钥。", nameof(_options));
    }

    private static async Task<T> WithOwnedStreamAsync<T>(Stream stream, bool leaveOpen, Func<Task<T>> operation)
    {
        T result = default;
        Exception failure = null;
        try { result = await operation().ConfigureAwait(false); }
        catch (Exception error) { failure = error; }

        if (!leaveOpen)
        {
            try { stream.Dispose(); }
            catch (Exception error)
            {
                if (failure != null) AttachCleanupError(failure, error);
                else if (result is SftpTransferResult transfer && transfer.Committed) transfer.AddCleanupError(error);
                else failure = error;
            }
        }

        if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
        return result;
    }

    private static async Task<Exception> CleanupRemoteTemporaryFileAsync(Renci.SshNet.SftpClient client,
        string temporaryPath, Exception failure, SftpTransferResult result)
    {
        try
        {
            if (await client.ExistsAsync(temporaryPath, CancellationToken.None).ConfigureAwait(false))
                await client.DeleteFileAsync(temporaryPath, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception error)
        {
            if (failure != null) AttachCleanupError(failure, error);
            else if (result?.Committed == true) result.AddCleanupError(error);
            else failure = error;
        }

        return failure;
    }

    internal static Exception AttachCleanupError(Exception failure, Exception cleanupError)
    {
        if (failure == null) return cleanupError;
        if (cleanupError == null) return failure;
        const string key = "Bing.Sftp.CleanupErrors";
        if (!(failure.Data[key] is List<Exception> errors))
        {
            errors = new List<Exception>();
            failure.Data[key] = errors;
        }

        errors.Add(cleanupError);
        return failure;
    }

    private static void MarkCommitOutcomeUnknown(OperationCanceledException error) =>
        error.Data["Bing.Sftp.CommitOutcome"] = "Unknown";

    private static void AttachProgressObserverErrors(Exception failure, SequencedProgress progress)
    {
        if (failure == null || progress.ObserverErrors.Count == 0) return;
        const string key = "Bing.Sftp.ProgressObserverErrors";
        failure.Data[key] = progress.ObserverErrors.ToArray();
    }

    private static void AddProgressObserverErrors(SftpTransferResult result, SequencedProgress progress)
    {
        if (result == null) return;
        foreach (var error in progress.ObserverErrors) result.AddProgressObserverError(error);
    }

    private static void ValidateRemotePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("远端 POSIX 路径不能为空。", nameof(path));
    }

    private static string CreateTemporaryPath(string remotePath) =>
        $"{remotePath}.bing-upload-{Guid.NewGuid():N}.tmp";

    private sealed class SequencedProgress : IProgress<OperationProgress>
    {
        private readonly IProgress<OperationProgress> _observer;
        private readonly bool _deferFinalStages;
        private readonly List<OperationProgress> _finalStages = new();
        private readonly List<Exception> _observerErrors = new();
        private long _sequence;

        internal SequencedProgress(IProgress<OperationProgress> observer, bool deferFinalStages)
        {
            _observer = observer;
            _deferFinalStages = deferFinalStages;
        }

        internal IReadOnlyList<Exception> ObserverErrors => _observerErrors;
        internal bool CopyCompleted { get; private set; }

        public void Report(OperationProgress value)
        {
            if (_deferFinalStages && value.IsFinal)
            {
                _finalStages.Add(value);
                return;
            }

            Emit(value);
        }

        internal void MarkCopyCompleted() => CopyCompleted = true;

        internal void CompleteTransferStages()
        {
            foreach (var stage in _finalStages) Emit(stage);
            _finalStages.Clear();
            CopyCompleted = true;
        }

        internal void ReportCommit() => Emit(new OperationProgress(OperationStage.Commit,
            ProgressUnit.File, 1, 1, isFinal: true));

        private void Emit(OperationProgress value)
        {
            var current = new OperationProgress(value.Stage, value.Unit, value.Completed, value.Total,
                value.Attempt, _sequence++, value.IsFinal);
            if (_observer == null) return;
            try { _observer.Report(current); }
            catch (Exception error) { _observerErrors.Add(error); }
        }
    }
}
