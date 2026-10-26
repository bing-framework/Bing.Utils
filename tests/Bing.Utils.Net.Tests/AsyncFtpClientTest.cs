using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Security;
using FluentFTP;

namespace Bing.Net.FTP;

/// <summary>
/// 验证异步 FTP 客户端的选项映射、串行行为、取消和流生命周期。
/// </summary>
public sealed class AsyncFtpClientTest
{
    /// <summary>
    /// 验证客户端创建时会复制选项，后续修改原对象不会改变快照。
    /// </summary>
    [Fact]
    public void Constructor_CopiesAndValidatesOptionsBeforeCreatingBackend()
    {
        var options = CreateOptions();
        AsyncFtpClientOptions captured = null;
        var backend = new FakeBackend();

        using var client = new AsyncFtpClient(options, snapshot =>
        {
            captured = snapshot;
            return backend;
        });

        options.Host = "changed.example";

        Assert.Equal("ftp.example", captured.Host);
        Assert.NotSame(options, captured);
    }

    /// <summary>
    /// 验证无效端口和代理参数在创建底层客户端前被拒绝。
    /// </summary>
    [Fact]
    public void Constructor_RejectsInvalidConnectionAndProxyOptions()
    {
        var backendCreated = false;
        var invalidPort = CreateOptions();
        invalidPort.Port = 0;

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new AsyncFtpClient(invalidPort, _ =>
            {
                backendCreated = true;
                return new FakeBackend();
            }));
        Assert.False(backendCreated);

        var invalidProxy = CreateOptions();
        invalidProxy.Proxy = new FtpAsyncProxyOptions { Host = "proxy.example", Port = 70000 };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new AsyncFtpClient(invalidProxy, _ =>
            {
                backendCreated = true;
                return new FakeBackend();
            }));
        Assert.False(backendCreated);
    }

    /// <summary>
    /// 验证连接选项真实映射到 FluentFTP 异步客户端配置。
    /// </summary>
    [Fact]
    public void Constructor_MapsEncodingTimeoutModesAndTransferType()
    {
        var encoding = System.Text.Encoding.UTF8;
        var options = CreateOptions();
        options.Port = 2121;
        options.ConnectTimeoutMilliseconds = 1200;
        options.ReadTimeoutMilliseconds = 3400;
        options.Encoding = encoding;
        options.UsePassive = false;
        options.TransferType = FtpTransferType.Ascii;
        options.SocketKeepAlive = false;
        options.EncryptionType = EncryptionType.Explicit;

        using var client = new AsyncFtpClient(options);
        var backend = GetNativeBackend(client);
        var native = backend.NativeClient;

        Assert.Equal("ftp.example", native.Host);
        Assert.Equal(2121, native.Port);
        Assert.Same(encoding, native.Encoding);
        Assert.Equal(1200, native.Config.ConnectTimeout);
        Assert.Equal(1200, native.Config.DataConnectionConnectTimeout);
        Assert.Equal(3400, native.Config.ReadTimeout);
        Assert.Equal(3400, native.Config.DataConnectionReadTimeout);
        Assert.Equal(FtpDataConnectionType.AutoActive, native.Config.DataConnectionType);
        Assert.Equal(FtpDataType.ASCII, native.Config.UploadDataType);
        Assert.Equal(FtpDataType.ASCII, native.Config.DownloadDataType);
        Assert.False(native.Config.SocketKeepAlive);
        Assert.Equal(FtpEncryptionMode.Explicit, native.Config.EncryptionMode);
    }

    /// <summary>
    /// 验证每种声明支持的代理类型均创建对应的 FluentFTP 原生异步代理客户端。
    /// </summary>
    /// <param name="proxyType">代理类型。</param>
    /// <param name="expectedClientType">原生异步客户端类型名。</param>
    [Theory]
    [InlineData(FtpAsyncProxyType.Http11, "AsyncFtpClientHttp11Proxy")]
    [InlineData(FtpAsyncProxyType.Socks4, "AsyncFtpClientSocks4Proxy")]
    [InlineData(FtpAsyncProxyType.Socks4a, "AsyncFtpClientSocks4aProxy")]
    [InlineData(FtpAsyncProxyType.Socks5, "AsyncFtpClientSocks5Proxy")]
    [InlineData(FtpAsyncProxyType.BlueCoat, "AsyncFtpClientBlueCoatProxy")]
    [InlineData(FtpAsyncProxyType.UserAtHost, "AsyncFtpClientUserAtHostProxy")]
    public void Constructor_CreatesSelectedNativeAsyncProxy(FtpAsyncProxyType proxyType, string expectedClientType)
    {
        var options = CreateOptions();
        options.Proxy = new FtpAsyncProxyOptions
        {
            Type = proxyType,
            Host = "proxy.example",
            Port = 8080
        };

        using var client = new AsyncFtpClient(options);

        Assert.Equal(expectedClientType, GetNativeBackend(client).NativeClient.GetType().Name);
    }

    /// <summary>
    /// 验证默认 FTPS 证书策略只接受没有策略错误的证书。
    /// </summary>
    /// <param name="policyErrors">模拟的证书策略错误。</param>
    /// <param name="expectedAccept">预期的接受状态。</param>
    [Theory]
    [InlineData(SslPolicyErrors.None, true)]
    [InlineData(SslPolicyErrors.RemoteCertificateNameMismatch, false)]
    [InlineData(SslPolicyErrors.RemoteCertificateChainErrors, false)]
    [InlineData(SslPolicyErrors.RemoteCertificateNotAvailable, false)]
    public void EncryptedMode_DefaultPolicyAcceptsOnlyErrorFreeCertificates(
        SslPolicyErrors policyErrors,
        bool expectedAccept)
    {
        var options = CreateOptions();
        options.EncryptionType = EncryptionType.Explicit;
        using var client = new AsyncFtpClient(options);
        var args = new FtpSslValidationEventArgs
        {
            PolicyErrors = policyErrors,
            Accept = !expectedAccept
        };

        RaiseCertificateValidation(GetNativeBackend(client).NativeClient, args);

        Assert.Equal(expectedAccept, args.Accept);
    }

    /// <summary>
    /// 验证显式 FTPS 模式调用自定义证书策略，并在策略抛错时拒绝证书。
    /// </summary>
    [Fact]
    public void EncryptedMode_UsesPolicyAndFailsClosedWhenPolicyThrows()
    {
        var invoked = false;
        var options = CreateOptions();
        options.EncryptionType = EncryptionType.Implicit;
        options.CertificateValidationPolicy = args =>
        {
            invoked = true;
            return args.PolicyErrors == SslPolicyErrors.RemoteCertificateNameMismatch;
        };
        using var client = new AsyncFtpClient(options);
        var accepted = new FtpSslValidationEventArgs
        {
            PolicyErrors = SslPolicyErrors.RemoteCertificateNameMismatch,
            Accept = false
        };

        RaiseCertificateValidation(GetNativeBackend(client).NativeClient, accepted);

        Assert.True(invoked);
        Assert.True(accepted.Accept);

        options = CreateOptions();
        options.EncryptionType = EncryptionType.Explicit;
        options.CertificateValidationPolicy = _ => throw new InvalidOperationException("policy failure");
        using var throwingClient = new AsyncFtpClient(options);
        var rejected = new FtpSslValidationEventArgs { Accept = true };

        var exception = Assert.Throws<TargetInvocationException>(() =>
            RaiseCertificateValidation(GetNativeBackend(throwingClient).NativeClient, rejected));

        Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.False(rejected.Accept);
    }

    /// <summary>
    /// 验证异步操作将同一个取消令牌传给原生后端。
    /// </summary>
    [Fact]
    public async Task Operation_PassesCancellationTokenToBackend()
    {
        var backend = new FakeBackend();
        CancellationToken observed = default;
        backend.FileExistsHandler = token =>
        {
            observed = token;
            return Task.FromResult(true);
        };
        using var client = CreateClient(backend);
        using var cancellation = new CancellationTokenSource();

        var result = await client.FileExistsAsync("/remote/file.txt", cancellation.Token);

        Assert.True(result);
        Assert.Equal(cancellation.Token, observed);
    }

    /// <summary>
    /// 验证开始前取消不会调用 FTP 后端。
    /// </summary>
    [Fact]
    public async Task Operation_CanceledBeforeStartDoesNotCallBackend()
    {
        var backend = new FakeBackend();
        using var client = CreateClient(backend);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.FileExistsAsync("/remote/file.txt", cancellation.Token));

        Assert.Equal(0, backend.FileExistsCalls);
    }

    /// <summary>
    /// 验证同一实例串行调度命令，并且等待中的取消不会干扰正在执行的命令。
    /// </summary>
    [Fact]
    public async Task Operations_AreSerializedAndCanceledWaiterDoesNotInterruptCurrentOperation()
    {
        var backend = new FakeBackend();
        var connectEntered = NewSignal();
        var releaseConnect = NewSignal();
        backend.ConnectHandler = async _ =>
        {
            connectEntered.TrySetResult(true);
            await releaseConnect.Task.ConfigureAwait(false);
            backend.IsConnected = true;
        };
        using var client = CreateClient(backend);

        var connecting = client.ConnectAsync();
        await WaitWithin(connectEntered.Task);
        using var cancellation = new CancellationTokenSource();
        var waitingLookup = client.FileExistsAsync("/remote/file.txt", cancellation.Token);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waitingLookup);
        releaseConnect.TrySetResult(true);
        await connecting;

        Assert.Equal(0, backend.FileExistsCalls);
        Assert.Equal(1, backend.MaximumConcurrentOperations);
    }

    /// <summary>
    /// 验证未取消的后续命令会等待当前命令完成后才进入后端。
    /// </summary>
    [Fact]
    public async Task Operations_QueueNextCommandUntilCurrentCommandCompletes()
    {
        var backend = new FakeBackend();
        var connectEntered = NewSignal();
        var releaseConnect = NewSignal();
        backend.ConnectHandler = async _ =>
        {
            connectEntered.TrySetResult(true);
            await releaseConnect.Task.ConfigureAwait(false);
            backend.IsConnected = true;
        };
        using var client = CreateClient(backend);

        var connecting = client.ConnectAsync();
        await WaitWithin(connectEntered.Task);
        var lookup = client.FileExistsAsync("/remote/file.txt");

        Assert.Equal(0, backend.FileExistsCalls);
        releaseConnect.TrySetResult(true);
        await Task.WhenAll(connecting, lookup);

        Assert.Equal(1, backend.FileExistsCalls);
        Assert.Equal(1, backend.MaximumConcurrentOperations);
    }

    /// <summary>
    /// 验证流上传和下载不会关闭调用方提供的流。
    /// </summary>
    [Fact]
    public async Task StreamOperations_RetainCallerOwnedStreams()
    {
        var backend = new FakeBackend
        {
            UploadStreamHandler = (stream, _, _, _) =>
            {
                stream.Dispose();
                return Task.FromResult(true);
            },
            DownloadStreamHandler = (stream, _, _, _) =>
            {
                stream.Dispose();
                return Task.FromResult(true);
            }
        };
        using var client = CreateClient(backend);
        using var source = new MemoryStream(new byte[] { 1, 2, 3 });
        using var destination = new MemoryStream();

        Assert.True(await client.UploadStreamAsync(source, "/remote/upload.bin"));
        Assert.True(await client.DownloadStreamAsync(destination, "/remote/download.bin"));
        Assert.True(source.CanRead);
        Assert.True(destination.CanWrite);
    }

    /// <summary>
    /// 验证释放客户端后会释放后端，且不再接受新操作。
    /// </summary>
    [Fact]
    public async Task Dispose_ReleasesBackendAndRejectsLaterOperations()
    {
        var backend = new FakeBackend();
        var client = CreateClient(backend);

        client.Dispose();
        client.Dispose();

        Assert.Equal(1, backend.DisposeCalls);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => client.FileExistsAsync("/remote/file.txt"));
    }

    private static AsyncFtpClientOptions CreateOptions() =>
        new AsyncFtpClientOptions { Host = "ftp.example", UserName = "user", Password = "secret" };

    private static AsyncFtpClient CreateClient(FakeBackend backend) =>
        new AsyncFtpClient(CreateOptions(), _ => backend);

    private static FluentFtpAsyncBackend GetNativeBackend(AsyncFtpClient client)
    {
        var field = typeof(AsyncFtpClient).GetField("_backend", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return Assert.IsType<FluentFtpAsyncBackend>(field.GetValue(client));
    }

    private static void RaiseCertificateValidation(
        FluentFTP.AsyncFtpClient nativeClient,
        FtpSslValidationEventArgs args)
    {
        var raiseMethod = nativeClient.GetType().GetMethod(
            "OnValidateCertficate",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(raiseMethod);
        raiseMethod.Invoke(nativeClient, new object[] { args });
    }

    private static TaskCompletionSource<bool> NewSignal() =>
        new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

    private static async Task WaitWithin(Task task)
    {
        var completed = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.Same(task, completed);
        await task;
    }

    private sealed class FakeBackend : IAsyncFtpBackend
    {
        private int _activeOperations;
        private int _maximumConcurrentOperations;

        public bool IsConnected { get; set; }

        public int FileExistsCalls { get; private set; }

        public int DisposeCalls { get; private set; }

        public int MaximumConcurrentOperations => Volatile.Read(ref _maximumConcurrentOperations);

        public Func<CancellationToken, Task> ConnectHandler { get; set; } =
            _ => Task.CompletedTask;

        public Func<CancellationToken, Task<bool>> FileExistsHandler { get; set; } =
            _ => Task.FromResult(false);

        public Func<Stream, string, IProgress<FtpProgress>, CancellationToken, Task<bool>> UploadStreamHandler { get; set; } =
            (_, _, _, _) => Task.FromResult(true);

        public Func<Stream, string, IProgress<FtpProgress>, CancellationToken, Task<bool>> DownloadStreamHandler { get; set; } =
            (_, _, _, _) => Task.FromResult(true);

        public Task ConnectAsync(CancellationToken cancellationToken) =>
            TrackAsync(() => ConnectHandler(cancellationToken));

        public Task DisconnectAsync(CancellationToken cancellationToken) =>
            TrackAsync(() =>
            {
                IsConnected = false;
                return Task.CompletedTask;
            });

        public Task<bool> FileExistsAsync(string remotePath, CancellationToken cancellationToken)
        {
            FileExistsCalls++;
            return TrackAsync(() => FileExistsHandler(cancellationToken));
        }

        public Task<bool> DirectoryExistsAsync(string remotePath, CancellationToken cancellationToken) =>
            TrackAsync(() => Task.FromResult(false));

        public Task<IReadOnlyList<string>> GetFilesAsync(string remoteDirectory, CancellationToken cancellationToken) =>
            TrackAsync<IReadOnlyList<string>>(() =>
                Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>()));

        public Task<IReadOnlyList<string>> GetDirectoriesAsync(string remoteDirectory, CancellationToken cancellationToken) =>
            TrackAsync<IReadOnlyList<string>>(() =>
                Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>()));

        public Task<bool> UploadFileAsync(
            string localPath,
            string remotePath,
            IProgress<FtpProgress> progress,
            CancellationToken cancellationToken) =>
            TrackAsync(() => Task.FromResult(true));

        public Task<bool> DownloadFileAsync(
            string localPath,
            string remotePath,
            IProgress<FtpProgress> progress,
            CancellationToken cancellationToken) =>
            TrackAsync(() => Task.FromResult(true));

        public Task<bool> UploadStreamAsync(
            Stream source,
            string remotePath,
            IProgress<FtpProgress> progress,
            CancellationToken cancellationToken) =>
            TrackAsync(() => UploadStreamHandler(source, remotePath, progress, cancellationToken));

        public Task<bool> DownloadStreamAsync(
            Stream destination,
            string remotePath,
            IProgress<FtpProgress> progress,
            CancellationToken cancellationToken) =>
            TrackAsync(() => DownloadStreamHandler(destination, remotePath, progress, cancellationToken));

        public Task DeleteFileAsync(string remotePath, CancellationToken cancellationToken) =>
            TrackAsync(() => Task.CompletedTask);

        public Task<bool> CreateDirectoryAsync(string remotePath, CancellationToken cancellationToken) =>
            TrackAsync(() => Task.FromResult(true));

        public Task<bool> MoveFileAsync(
            string remotePath,
            string destinationPath,
            CancellationToken cancellationToken) =>
            TrackAsync(() => Task.FromResult(true));

        public void Dispose() => DisposeCalls++;

        private async Task TrackAsync(Func<Task> action)
        {
            EnterOperation();
            try
            {
                await action().ConfigureAwait(false);
            }
            finally
            {
                Interlocked.Decrement(ref _activeOperations);
            }
        }

        private async Task<T> TrackAsync<T>(Func<Task<T>> action)
        {
            EnterOperation();
            try
            {
                return await action().ConfigureAwait(false);
            }
            finally
            {
                Interlocked.Decrement(ref _activeOperations);
            }
        }

        private void EnterOperation()
        {
            var active = Interlocked.Increment(ref _activeOperations);
            var observed = Volatile.Read(ref _maximumConcurrentOperations);
            while (active > observed)
            {
                var previous = Interlocked.CompareExchange(ref _maximumConcurrentOperations, active, observed);
                if (previous == observed)
                    break;
                observed = previous;
            }
        }
    }
}
