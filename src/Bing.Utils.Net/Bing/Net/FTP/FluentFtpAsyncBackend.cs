using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;

namespace Bing.Net.FTP;

internal interface IAsyncFtpBackend : IDisposable
{
    bool IsConnected { get; }

    Task ConnectAsync(CancellationToken cancellationToken);

    Task DisconnectAsync(CancellationToken cancellationToken);

    Task<bool> FileExistsAsync(string remotePath, CancellationToken cancellationToken);

    Task<bool> DirectoryExistsAsync(string remotePath, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetFilesAsync(string remoteDirectory, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetDirectoriesAsync(string remoteDirectory, CancellationToken cancellationToken);

    Task<bool> UploadFileAsync(string localPath, string remotePath, IProgress<FtpProgress> progress, CancellationToken cancellationToken);

    Task<bool> DownloadFileAsync(string localPath, string remotePath, IProgress<FtpProgress> progress, CancellationToken cancellationToken);

    Task<bool> UploadStreamAsync(Stream source, string remotePath, IProgress<FtpProgress> progress, CancellationToken cancellationToken);

    Task<bool> DownloadStreamAsync(Stream destination, string remotePath, IProgress<FtpProgress> progress, CancellationToken cancellationToken);

    Task DeleteFileAsync(string remotePath, CancellationToken cancellationToken);

    Task<bool> CreateDirectoryAsync(string remotePath, CancellationToken cancellationToken);

    Task<bool> MoveFileAsync(string remotePath, string destinationPath, CancellationToken cancellationToken);
}

internal sealed class FluentFtpAsyncBackend : IAsyncFtpBackend, IAsyncFtpDirectoryBackend
{
    private readonly FluentFTP.AsyncFtpClient _client;

    internal FluentFtpAsyncBackend(AsyncFtpClientOptions options)
    {
        _client = CreateClient(options);
    }

    internal FluentFTP.AsyncFtpClient NativeClient => _client;

    public bool IsConnected => _client.IsConnected && _client.IsAuthenticated;

    public Task ConnectAsync(CancellationToken cancellationToken) => _client.Connect(cancellationToken);

    public Task DisconnectAsync(CancellationToken cancellationToken) => _client.Disconnect(cancellationToken);

    public Task<bool> FileExistsAsync(string remotePath, CancellationToken cancellationToken) =>
        _client.FileExists(remotePath, cancellationToken);

    public Task<bool> DirectoryExistsAsync(string remotePath, CancellationToken cancellationToken) =>
        _client.DirectoryExists(remotePath, cancellationToken);

    public async Task<IReadOnlyList<string>> GetFilesAsync(string remoteDirectory, CancellationToken cancellationToken)
    {
        var items = await _client.GetListing(remoteDirectory, cancellationToken).ConfigureAwait(false);
        return items.Where(item => item.Type == FtpObjectType.File).Select(item => item.Name).ToArray();
    }

    public async Task<IReadOnlyList<string>> GetDirectoriesAsync(string remoteDirectory, CancellationToken cancellationToken)
    {
        var items = await _client.GetListing(remoteDirectory, cancellationToken).ConfigureAwait(false);
        return items.Where(item => item.Type == FtpObjectType.Directory).Select(item => item.Name).ToArray();
    }

    public async Task<bool> UploadFileAsync(
        string localPath,
        string remotePath,
        IProgress<FtpProgress> progress,
        CancellationToken cancellationToken)
    {
        var status = await _client.UploadFile(
            localPath,
            remotePath,
            FtpRemoteExists.Overwrite,
            true,
            FtpVerify.Retry | FtpVerify.Delete | FtpVerify.Throw,
            progress,
            cancellationToken).ConfigureAwait(false);
        return status == FtpStatus.Success;
    }

    public async Task<bool> DownloadFileAsync(
        string localPath,
        string remotePath,
        IProgress<FtpProgress> progress,
        CancellationToken cancellationToken)
    {
        var status = await _client.DownloadFile(
            localPath,
            remotePath,
            FtpLocalExists.Overwrite,
            FtpVerify.Retry | FtpVerify.Delete | FtpVerify.Throw,
            progress,
            cancellationToken).ConfigureAwait(false);
        return status == FtpStatus.Success;
    }

    public async Task<bool> UploadStreamAsync(
        Stream source,
        string remotePath,
        IProgress<FtpProgress> progress,
        CancellationToken cancellationToken)
    {
        var status = await _client.UploadStream(
            source,
            remotePath,
            FtpRemoteExists.Overwrite,
            true,
            progress,
            cancellationToken).ConfigureAwait(false);
        return status == FtpStatus.Success;
    }

    public Task<bool> DownloadStreamAsync(
        Stream destination,
        string remotePath,
        IProgress<FtpProgress> progress,
        CancellationToken cancellationToken) =>
        _client.DownloadStream(destination, remotePath, 0, progress, cancellationToken);

    public Task DeleteFileAsync(string remotePath, CancellationToken cancellationToken) =>
        _client.DeleteFile(remotePath, cancellationToken);

    public Task<bool> CreateDirectoryAsync(string remotePath, CancellationToken cancellationToken) =>
        _client.CreateDirectory(remotePath, cancellationToken);

    public Task<bool> MoveFileAsync(string remotePath, string destinationPath, CancellationToken cancellationToken) =>
        _client.MoveFile(remotePath, destinationPath, FtpRemoteExists.Overwrite, cancellationToken);

    public async Task<IReadOnlyList<FtpDirectoryEntry>> GetDirectoryEntriesAsync(
        string remoteDirectory,
        CancellationToken cancellationToken)
    {
        var items = await _client.GetListing(remoteDirectory, cancellationToken).ConfigureAwait(false);
        var entries = new FtpDirectoryEntry[items.Length];
        for (var index = 0; index < items.Length; index++)
        {
            var item = items[index];
            var type = item.Type switch
            {
                FtpObjectType.File => FtpDirectoryEntryType.File,
                FtpObjectType.Directory => FtpDirectoryEntryType.Directory,
                FtpObjectType.Link => FtpDirectoryEntryType.Link,
                _ => FtpDirectoryEntryType.Unknown
            };
            entries[index] = new FtpDirectoryEntry(
                item.Name,
                item.FullName,
                type,
                type == FtpDirectoryEntryType.File && item.Size >= 0 ? item.Size : null,
                item.Modified == DateTime.MinValue ? null : item.Modified);
        }

        return entries;
    }

    public Task<FtpStatus> UploadFileAsync(
        string localPath,
        string remotePath,
        FtpRemoteExists existsMode,
        IProgress<FtpProgress> progress,
        CancellationToken cancellationToken) =>
        _client.UploadFile(localPath, remotePath, existsMode, true, FtpVerify.None, progress, cancellationToken);

    public Task<FtpStatus> DownloadFileAsync(
        string localPath,
        string remotePath,
        FtpLocalExists existsMode,
        IProgress<FtpProgress> progress,
        CancellationToken cancellationToken) =>
        _client.DownloadFile(localPath, remotePath, existsMode, FtpVerify.None, progress, cancellationToken);

    public Task<bool> MoveDirectoryAsync(
        string remotePath,
        string destinationPath,
        FtpRemoteExists existsMode,
        CancellationToken cancellationToken) =>
        _client.MoveDirectory(remotePath, destinationPath, existsMode, cancellationToken);

    public Task DeleteDirectoryAsync(string remotePath, CancellationToken cancellationToken) =>
        _client.DeleteDirectory(remotePath, cancellationToken);

    public async Task<FtpRemoteChecksum> GetFileChecksumAsync(
        string remotePath,
        FtpHashAlgorithm algorithm,
        CancellationToken cancellationToken)
    {
        FtpHash hash;
        try
        {
            hash = await _client.GetChecksum(remotePath, algorithm, cancellationToken).ConfigureAwait(false);
        }
        catch (FtpHashUnsupportedException exception)
        {
            throw new NotSupportedException($"FTP 服务器不支持文件“{remotePath}”的校验算法 {algorithm}。", exception);
        }

        if (hash == null || !hash.IsValid)
        {
            throw new NotSupportedException($"FTP 服务器未提供文件“{remotePath}”的有效校验值。");
        }

        return new FtpRemoteChecksum(hash.Algorithm, hash.Value);
    }

    public void Dispose() => _client.Dispose();

    private static FluentFTP.AsyncFtpClient CreateClient(AsyncFtpClientOptions options)
    {
        var client = options.Proxy == null
            ? new FluentFTP.AsyncFtpClient()
            : CreateProxyClient(options);

        client.Host = options.Host;
        client.Port = options.Port;
        client.Credentials = new NetworkCredential(options.UserName, options.Password);
        client.Encoding = options.Encoding;

        var config = client.Config;
        config.ConnectTimeout = options.ConnectTimeoutMilliseconds;
        config.DataConnectionConnectTimeout = options.ConnectTimeoutMilliseconds;
        config.ReadTimeout = options.ReadTimeoutMilliseconds;
        config.DataConnectionReadTimeout = options.ReadTimeoutMilliseconds;
        config.DataConnectionType = options.UsePassive
            ? FtpDataConnectionType.AutoPassive
            : FtpDataConnectionType.AutoActive;
        config.UploadDataType = ToNativeDataType(options.TransferType);
        config.DownloadDataType = ToNativeDataType(options.TransferType);
        config.SocketKeepAlive = options.SocketKeepAlive;
        config.EncryptionMode = ToNativeEncryptionMode(options.EncryptionType);

        if (config.EncryptionMode != FtpEncryptionMode.None)
            client.ValidateCertificate += ValidateCertificate(options.CertificateValidationPolicy);

        return client;
    }

    private static FluentFTP.AsyncFtpClient CreateProxyClient(AsyncFtpClientOptions options)
    {
        var proxyOptions = options.Proxy;
        var profile = new FtpProxyProfile
        {
            ProxyHost = proxyOptions.Host,
            ProxyPort = proxyOptions.Port,
            ProxyCredentials = new NetworkCredential(proxyOptions.UserName, proxyOptions.Password),
            FtpHost = options.Host,
            FtpPort = options.Port,
            FtpCredentials = new NetworkCredential(options.UserName, options.Password)
        };

        switch (proxyOptions.Type)
        {
            case FtpAsyncProxyType.Http11:
                return new FluentFTP.Proxy.AsyncProxy.AsyncFtpClientHttp11Proxy(profile);
            case FtpAsyncProxyType.Socks4:
                return new FluentFTP.Proxy.AsyncProxy.AsyncFtpClientSocks4Proxy(profile);
            case FtpAsyncProxyType.Socks4a:
                return new FluentFTP.Proxy.AsyncProxy.AsyncFtpClientSocks4aProxy(profile);
            case FtpAsyncProxyType.Socks5:
                return new FluentFTP.Proxy.AsyncProxy.AsyncFtpClientSocks5Proxy(profile);
            case FtpAsyncProxyType.BlueCoat:
                return new FluentFTP.Proxy.AsyncProxy.AsyncFtpClientBlueCoatProxy(profile);
            case FtpAsyncProxyType.UserAtHost:
                return new FluentFTP.Proxy.AsyncProxy.AsyncFtpClientUserAtHostProxy(profile);
            default:
                throw new ArgumentOutOfRangeException(nameof(proxyOptions.Type), proxyOptions.Type, "不支持的 FTP 代理类型。");
        }
    }

    private static FtpDataType ToNativeDataType(FtpTransferType type) =>
        type == FtpTransferType.Binary ? FtpDataType.Binary : FtpDataType.ASCII;

    private static FtpEncryptionMode ToNativeEncryptionMode(EncryptionType type)
    {
        switch (type)
        {
            case EncryptionType.None:
                return FtpEncryptionMode.None;
            case EncryptionType.Implicit:
                return FtpEncryptionMode.Implicit;
            case EncryptionType.Explicit:
                return FtpEncryptionMode.Explicit;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, "不支持的 FTP 加密模式。");
        }
    }

    private static FtpSslValidation ValidateCertificate(
        Func<FtpSslValidationEventArgs, bool> policy)
    {
        return (_, args) =>
        {
            try
            {
                args.Accept = policy == null
                    ? args.PolicyErrors == SslPolicyErrors.None
                    : policy(args);
            }
            catch
            {
                args.Accept = false;
                throw;
            }
        };
    }
}

internal interface IAsyncFtpDirectoryBackend
{
    Task<IReadOnlyList<FtpDirectoryEntry>> GetDirectoryEntriesAsync(string remoteDirectory,
        CancellationToken cancellationToken);

    Task<FtpStatus> UploadFileAsync(string localPath, string remotePath, FtpRemoteExists existsMode,
        IProgress<FtpProgress> progress, CancellationToken cancellationToken);

    Task<FtpStatus> DownloadFileAsync(string localPath, string remotePath, FtpLocalExists existsMode,
        IProgress<FtpProgress> progress, CancellationToken cancellationToken);

    Task<bool> MoveDirectoryAsync(string remotePath, string destinationPath, FtpRemoteExists existsMode,
        CancellationToken cancellationToken);

    Task DeleteDirectoryAsync(string remotePath, CancellationToken cancellationToken);

    Task<FtpRemoteChecksum> GetFileChecksumAsync(string remotePath, FtpHashAlgorithm algorithm,
        CancellationToken cancellationToken);
}
