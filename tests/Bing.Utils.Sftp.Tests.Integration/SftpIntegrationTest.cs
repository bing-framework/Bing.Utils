using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bing.Operations;
using Bing.Sftp;
using Renci.SshNet;
using Renci.SshNet.Common;
using Shouldly;
using Xunit;

namespace Bing.Utils.Sftp.Tests.Integration;

/// <summary>只有设置受控 OpenSSH 测试环境变量时才运行的 SFTP 集成测试。</summary>
public sealed class SftpIntegrationFactAttribute : FactAttribute
{
    public SftpIntegrationFactAttribute()
    {
        if (!SftpIntegrationEnvironment.IsConfigured)
            Skip = "BLOCKED_EXTERNAL: 设置 tests/Bing.Utils.Sftp.Tests.Integration/README.md 中的 BING_SFTP_* 环境变量后运行。";
    }
}

/// <summary>SFTP 外部服务集成测试。</summary>
public sealed class SftpIntegrationTest
{
    private readonly SftpIntegrationEnvironment _environment = SftpIntegrationEnvironment.Load();

    /// <summary>测试目的：错误主机指纹和认证凭据均必须在 SSH 握手阶段被拒绝。</summary>
    [SftpIntegrationFact]
    public async Task Connect_WithWrongHostKeyOrCredentials_ShouldReject()
    {
        var wrongHostKey = _environment.CreateClient("SHA256:AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
        await Should.ThrowAsync<SshConnectionException>(() => wrongHostKey.ListDirectoryAsync(_environment.RemoteDirectory));

        var wrongCredentials = _environment.CreateWrongCredentialsClient();
        try
        {
            await Should.ThrowAsync<SshAuthenticationException>(() => wrongCredentials.ListDirectoryAsync(_environment.RemoteDirectory));
        }
        finally
        {
            _environment.DeleteWrongCredentialsKey();
        }
    }

    /// <summary>测试目的：流与文件传输、目录枚举、文件信息和进度序号应形成完整往返。</summary>
    [SftpIntegrationFact]
    public async Task UploadDownload_WithControlledOpenSsh_ShouldPublishAndRoundTrip()
    {
        var payload = Encoding.UTF8.GetBytes("姓名,数量\r\n张三,3\r\n李四,5\r\n");
        var remotePath = _environment.CreateRemotePath("roundtrip");
        var localPath = Path.Combine(Path.GetTempPath(), $"bing-sftp-{Guid.NewGuid():N}.csv");
        var client = _environment.CreateClient();
        var progress = new CapturingProgress();

        try
        {
            using var source = new MemoryStream(payload, writable: false);
            var uploaded = await client.UploadAsync(source, remotePath, progress: progress);
            uploaded.Completed.ShouldBeTrue();
            uploaded.Committed.ShouldBeTrue();
            AssertProgressSequence(progress.Events, expectCommit: true);

            var info = await client.GetFileInfoAsync(remotePath);
            info.IsRegularFile.ShouldBeTrue();
            info.Length.ShouldBe(payload.LongLength);
            (await client.ListDirectoryEntriesAsync(_environment.RemoteDirectory))
                .ShouldContain(entry => entry.FullName == remotePath);

            using var destination = new MemoryStream();
            var downloaded = await client.DownloadAsync(remotePath, destination);
            downloaded.Completed.ShouldBeTrue();
            downloaded.Committed.ShouldBeFalse();
            destination.ToArray().ShouldBe(payload);

            var fileResult = await client.DownloadFileAsync(remotePath, localPath);
            fileResult.Committed.ShouldBeTrue();
            File.ReadAllBytes(localPath).ShouldBe(payload);
        }
        finally
        {
            File.Delete(localPath);
            await _environment.DeleteIfExistsAsync(remotePath);
        }
    }

    /// <summary>测试目的：已有目标在重复上传冲突后仍保留原始内容。</summary>
    [SftpIntegrationFact]
    public async Task Upload_WhenTargetExists_ShouldPreserveExistingContent()
    {
        var remotePath = _environment.CreateRemotePath("conflict");
        var original = Encoding.UTF8.GetBytes("original");
        var client = _environment.CreateClient();
        try
        {
            using (var first = new MemoryStream(original, writable: false))
                await client.UploadAsync(first, remotePath);

            using var replacement = new MemoryStream(Encoding.UTF8.GetBytes("replacement"), writable: false);
            await Should.ThrowAsync<IOException>(() => client.UploadAsync(replacement, remotePath));

            using var downloaded = new MemoryStream();
            await client.DownloadAsync(remotePath, downloaded);
            downloaded.ToArray().ShouldBe(original);
        }
        finally
        {
            await _environment.DeleteIfExistsAsync(remotePath);
        }
    }

    /// <summary>测试目的：本地下载目标冲突时拒绝发布并保留原内容。</summary>
    [SftpIntegrationFact]
    public async Task DownloadFile_WhenLocalTargetExists_ShouldPreserveExistingContent()
    {
        var remotePath = _environment.CreateRemotePath("local-conflict");
        var localPath = Path.Combine(Path.GetTempPath(), $"bing-sftp-{Guid.NewGuid():N}.txt");
        var original = Encoding.UTF8.GetBytes("keep-local");
        File.WriteAllBytes(localPath, original);

        try
        {
            await Should.ThrowAsync<IOException>(() => _environment.CreateClient().DownloadFileAsync(remotePath, localPath));
            File.ReadAllBytes(localPath).ShouldBe(original);
        }
        finally
        {
            File.Delete(localPath);
        }
    }

    /// <summary>测试目的：文件下载取消时保留目标未发布状态并清理同目录暂存文件。</summary>
    [SftpIntegrationFact]
    public async Task DownloadFile_WhenCanceledDuringCopy_ShouldCleanLocalTemporaryFile()
    {
        var remotePath = _environment.CreateRemotePath("download-cancel");
        var localPath = Path.Combine(Path.GetTempPath(), $"bing-sftp-{Guid.NewGuid():N}.txt");
        var client = _environment.CreateClient();
        using (var source = new MemoryStream(new byte[4 * 1024 * 1024], writable: false))
            await client.UploadAsync(source, remotePath);

        using var cancellation = new CancellationTokenSource();
        var progress = new InlineProgress<OperationProgress>(eventProgress =>
        {
            if (eventProgress.Stage == OperationStage.Write && !eventProgress.IsFinal)
                cancellation.Cancel();
        });
        try
        {
            await Should.ThrowAsync<OperationCanceledException>(() =>
                client.DownloadFileAsync(remotePath, localPath, progress, cancellation.Token));

            File.Exists(localPath).ShouldBeFalse();
            Directory.GetFiles(Path.GetDirectoryName(localPath)!, $".{Path.GetFileName(localPath)}.bing-sftp-*.tmp")
                .ShouldBeEmpty();
        }
        finally
        {
            File.Delete(localPath);
            await _environment.DeleteIfExistsAsync(remotePath);
        }
    }

    /// <summary>测试目的：复制期间取消必须停止读写并清理远端暂存文件。</summary>
    [SftpIntegrationFact]
    public async Task Upload_WhenCanceledDuringCopy_ShouldStopAndCleanTemporaryFile()
    {
        var remotePath = _environment.CreateRemotePath("cancel");
        using var cancellation = new CancellationTokenSource();
        using var source = new CancelAfterReadsStream(cancellation, cancelAfterReads: 4);
        var client = _environment.CreateClient();

        await Should.ThrowAsync<OperationCanceledException>(() =>
            client.UploadAsync(source, remotePath, leaveOpen: false, cancellationToken: cancellation.Token));

        var entries = await client.ListDirectoryEntriesAsync(_environment.RemoteDirectory);
        entries.ShouldNotContain(entry => entry.Name.Contains("bing-upload-", StringComparison.Ordinal));
        entries.ShouldNotContain(entry => entry.FullName == remotePath);
    }

    /// <summary>测试目的：rename 收到服务器拒绝时应保留竞争写入的目标并清理暂存文件。</summary>
    [SftpIntegrationFact]
    public async Task Upload_WhenTargetAppearsBeforeRename_ShouldPreserveWinnerAndCleanTemporaryFile()
    {
        var remotePath = _environment.CreateRemotePath("rename-race");
        var winner = Encoding.UTF8.GetBytes("winner");
        var client = _environment.CreateClient();
        var injected = 0;
        var progress = new InlineProgress<OperationProgress>(eventProgress =>
        {
            if (eventProgress.Stage == OperationStage.Write && eventProgress.IsFinal &&
                Interlocked.Exchange(ref injected, 1) == 0)
            {
                using var source = new MemoryStream(winner, writable: false);
                _environment.CreateClient().UploadAsync(source, remotePath).GetAwaiter().GetResult();
            }
        });

        try
        {
            using var source = new MemoryStream(Encoding.UTF8.GetBytes("loser"), writable: false);
            await Should.ThrowAsync<SftpException>(() => client.UploadAsync(source, remotePath, progress: progress));

            using var downloaded = new MemoryStream();
            await client.DownloadAsync(remotePath, downloaded);
            downloaded.ToArray().ShouldBe(winner);
            var entries = await client.ListDirectoryEntriesAsync(_environment.RemoteDirectory);
            entries.ShouldNotContain(entry => entry.Name.Contains("bing-upload-", StringComparison.Ordinal));
        }
        finally
        {
            await _environment.DeleteIfExistsAsync(remotePath);
        }
    }

    private static void AssertProgressSequence(IReadOnlyList<OperationProgress> events, bool expectCommit)
    {
        events.Select(item => item.Sequence).ShouldBe(Enumerable.Range(0, events.Count).Select(value => (long)value));
        if (expectCommit)
        {
            events.ShouldContain(item => item.Stage == OperationStage.Read && item.IsFinal);
            events.ShouldContain(item => item.Stage == OperationStage.Write && item.IsFinal);
            events.Last().Stage.ShouldBe(OperationStage.Commit);
            events.Last().IsFinal.ShouldBeTrue();
        }
    }

    private sealed class CapturingProgress : IProgress<OperationProgress>
    {
        public List<OperationProgress> Events { get; } = new();
        public void Report(OperationProgress value) => Events.Add(value);
    }

    private sealed class InlineProgress<T> : IProgress<T>
    {
        private readonly Action<T> _report;
        public InlineProgress(Action<T> report) => _report = report;
        public void Report(T value) => _report(value);
    }

    private sealed class CancelAfterReadsStream : Stream
    {
        private readonly CancellationTokenSource _cancellation;
        private readonly int _cancelAfterReads;
        private int _reads;
        private bool _disposed;

        public CancelAfterReadsStream(CancellationTokenSource cancellation, int cancelAfterReads)
        {
            _cancellation = cancellation;
            _cancelAfterReads = cancelAfterReads;
        }

        public override bool CanRead => !_disposed;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref _reads) >= _cancelAfterReads) _cancellation.Cancel();
            cancellationToken.ThrowIfCancellationRequested();
            Array.Fill(buffer, (byte)'x', offset, Math.Min(count, 8192));
            return Task.FromResult(Math.Min(count, 8192));
        }

        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing)
        {
            _disposed = true;
            base.Dispose(disposing);
        }
    }
}

internal sealed class SftpIntegrationEnvironment
{
    private string _wrongCredentialKeyPath;
    private readonly string _password;
    private readonly string _privateKeyPath;
    private readonly string _privateKeyPassPhrase;

    private SftpIntegrationEnvironment(string host, int port, string userName, string password,
        string privateKeyPath, string privateKeyPassPhrase, string hostKeySha256, string remoteDirectory)
    {
        Host = host;
        Port = port;
        UserName = userName;
        _password = password;
        _privateKeyPath = privateKeyPath;
        _privateKeyPassPhrase = privateKeyPassPhrase;
        HostKeySha256 = hostKeySha256;
        var trimmedDirectory = remoteDirectory.TrimEnd('/');
        RemoteDirectory = trimmedDirectory.Length == 0 ? "/" : trimmedDirectory;
    }

    internal string Host { get; }
    internal int Port { get; }
    internal string UserName { get; }
    internal string HostKeySha256 { get; }
    internal string RemoteDirectory { get; }

    internal static bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BING_SFTP_HOST")) &&
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BING_SFTP_USER")) &&
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BING_SFTP_HOST_KEY_SHA256")) &&
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BING_SFTP_REMOTE_DIRECTORY")) &&
        (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BING_SFTP_PASSWORD")) ||
         !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BING_SFTP_PRIVATE_KEY_PATH")));

    internal static SftpIntegrationEnvironment Load()
    {
        var host = Environment.GetEnvironmentVariable("BING_SFTP_HOST");
        var portText = Environment.GetEnvironmentVariable("BING_SFTP_PORT");
        var port = string.IsNullOrWhiteSpace(portText) ? 22 : int.Parse(portText);
        return new SftpIntegrationEnvironment(host, port,
            Environment.GetEnvironmentVariable("BING_SFTP_USER"),
            Environment.GetEnvironmentVariable("BING_SFTP_PASSWORD"),
            Environment.GetEnvironmentVariable("BING_SFTP_PRIVATE_KEY_PATH"),
            Environment.GetEnvironmentVariable("BING_SFTP_PRIVATE_KEY_PASSPHRASE"),
            Environment.GetEnvironmentVariable("BING_SFTP_HOST_KEY_SHA256"),
            Environment.GetEnvironmentVariable("BING_SFTP_REMOTE_DIRECTORY"));
    }

    internal string CreateRemotePath(string prefix) =>
        $"{RemoteDirectory}/{prefix}-{Guid.NewGuid():N}.dat";

    internal SftpTransferClient CreateClient(string hostKey = null, bool wrongCredentials = false) =>
        new(new SftpConnectionOptions
        {
            Host = Host,
            Port = Port,
            UserName = UserName,
            HostKeySha256 = hostKey ?? HostKeySha256,
            Password = _privateKeyPath == null ? (wrongCredentials ? "invalid-password" : _password) : null,
            PrivateKeyPath = _privateKeyPath,
            PrivateKeyPassPhrase = _privateKeyPassPhrase
        });

    internal SftpTransferClient CreateWrongCredentialsClient()
    {
        if (_privateKeyPath == null) return CreateClient(wrongCredentials: true);
        var keyPath = Path.Combine(Path.GetTempPath(), $"bing-sftp-invalid-{Guid.NewGuid():N}.pem");
        try
        {
            using (var rsa = RSA.Create(2048))
                File.WriteAllText(keyPath, rsa.ExportPkcs8PrivateKeyPem(), new UTF8Encoding(false));
            _wrongCredentialKeyPath = keyPath;
            return new SftpTransferClient(new SftpConnectionOptions
            {
                Host = Host,
                Port = Port,
                UserName = UserName,
                HostKeySha256 = HostKeySha256,
                PrivateKeyPath = keyPath
            });
        }
        catch
        {
            File.Delete(keyPath);
            throw;
        }
    }

    internal void DeleteWrongCredentialsKey()
    {
        if (_wrongCredentialKeyPath != null && File.Exists(_wrongCredentialKeyPath))
            File.Delete(_wrongCredentialKeyPath);
        _wrongCredentialKeyPath = null;
    }

    internal async Task DeleteIfExistsAsync(string remotePath)
    {
        AuthenticationMethod authentication = _privateKeyPath == null
            ? new PasswordAuthenticationMethod(UserName, _password)
            : new PrivateKeyAuthenticationMethod(UserName, new PrivateKeyFile(_privateKeyPath, _privateKeyPassPhrase));
        using var client = new Renci.SshNet.SftpClient(new ConnectionInfo(Host, Port, UserName, authentication));
        client.HostKeyReceived += (_, args) => args.CanTrust = SftpTransferClient.MatchesHostKey(args.HostKey, HostKeySha256);
        await client.ConnectAsync(CancellationToken.None);
        if (await client.ExistsAsync(remotePath, CancellationToken.None))
            await client.DeleteFileAsync(remotePath, CancellationToken.None);
    }
}
