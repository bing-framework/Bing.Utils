using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using Bing.Helpers;
using Bing.Http;
using Bing.Http.Clients;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Bing.Utils.Http.Tests.Integration.Http;

/// <summary>

/// 验证 HTTP 客户端工厂配置复用和文件下载失败保护。

/// </summary>
public class HttpRequestFactoryAndDownloadIntegrationTest
{
    /// <summary>
    /// 承载集成测试服务的宿主实例。
    /// </summary>
    private readonly IHost _host;

    /// <summary>

    /// 初始化一个<see cref="HttpRequestFactoryAndDownloadIntegrationTest"/>类型的实例。

    /// </summary>
    /// <param name="host">用于测试的依赖注入应用实例。</param>
    public HttpRequestFactoryAndDownloadIntegrationTest(IHost host) => _host = host;

    /// <summary>

    /// 验证输入文件和流保持不变。

    /// </summary>
    [Fact]
    public async Task WriteAsync_ShouldPreserveInputFileAndStreams()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".bin");
        var bytes = Enumerable.Range(0, 4097).Select(x => (byte)(x % 239)).ToArray();
        await File.WriteAllBytesAsync(path, bytes);
        var previousAccessor = Web.HttpContextAccessor;
        try
        {
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            Web.HttpContextAccessor = new HttpContextAccessor { HttpContext = context };
            using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

            await Web.WriteAsync(file);

            File.Exists(path).ShouldBeTrue();
            file.CanRead.ShouldBeTrue();
            context.Response.Body.CanWrite.ShouldBeTrue();
            context.Response.Headers["Content-Length"].ToString().ShouldBe(bytes.Length.ToString());
            context.Response.Body.Position = 0;
            using var copy = new MemoryStream();
            await context.Response.Body.CopyToAsync(copy);
            copy.ToArray().ShouldBe(bytes);
        }
        finally
        {
            Web.HttpContextAccessor = previousAccessor;
            File.Delete(path);
        }
    }

    /// <summary>

    /// 验证使用On完成报告成功保存一次。

    /// </summary>
    [Fact]
    public async Task WriteAsync_WithOnComplete_ReportsSuccessfulSaveOnce()
    {
        var directory = Path.Combine(Path.GetTempPath(), "Bing.Utils.Http.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "download.bin");
        var completionCalls = new List<HttpRequestCompletion>();
        var callbackCount = 0;
        try
        {
            using var client = new HttpClient(new FixedBodyHandler(HttpStatusCode.OK, new byte[] { 1, 2, 3 }));
            await new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/file")
                .OnComplete((_, _) => callbackCount++)
                .ObserveCompletion(completionCalls.Add)
                .WriteAsync(path);

            (await File.ReadAllBytesAsync(path)).ShouldBe(new byte[] { 1, 2, 3 });
            callbackCount.ShouldBe(1);
            completionCalls.Count.ShouldBe(1);
            completionCalls[0].Outcome.ShouldBe(HttpRequestOutcome.Success);
            completionCalls[0].Exception.ShouldBeNull();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>

    /// 验证使用On完成报告保存失败一次。

    /// </summary>
    [Fact]
    public async Task WriteAsync_WithOnComplete_ReportsSaveFailureOnce()
    {
        var directory = Path.Combine(Path.GetTempPath(), "Bing.Utils.Http.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var blocker = Path.Combine(directory, "not-a-directory");
        await File.WriteAllTextAsync(blocker, "blocker");
        var path = Path.Combine(blocker, "download.bin");
        var completionCalls = new List<HttpRequestCompletion>();
        var callbackCount = 0;
        try
        {
            using var client = new HttpClient(new FixedBodyHandler(HttpStatusCode.OK, new byte[] { 1, 2, 3 }));
            var error = await Should.ThrowAsync<InvalidOperationException>(() =>
                new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/file")
                    .OnComplete((_, _) => callbackCount++)
                    .ObserveCompletion(completionCalls.Add)
                    .WriteAsync(path));

            error.ShouldNotBeNull();
            callbackCount.ShouldBe(1);
            completionCalls.Count.ShouldBe(1);
            completionCalls[0].Outcome.ShouldBe(HttpRequestOutcome.Faulted);
            completionCalls[0].Exception.ShouldBeSameAs(error);
            (await File.ReadAllTextAsync(blocker)).ShouldBe("blocker");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>

    /// 验证应流文件到响应。

    /// </summary>
    [Fact]
    public async Task DownloadFileAsync_ShouldStreamFileToResponse()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".bin");
        var bytes = Enumerable.Range(0, 3073).Select(x => (byte)(x % 241)).ToArray();
        await File.WriteAllBytesAsync(path, bytes);
        var previousAccessor = Web.HttpContextAccessor;
        try
        {
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            Web.HttpContextAccessor = new HttpContextAccessor { HttpContext = context };

            await Web.DownloadFileAsync(path, "stream.bin", Encoding.UTF8, CancellationToken.None);

            File.Exists(path).ShouldBeTrue();
            context.Response.Headers["Content-Length"].ToString().ShouldBe(bytes.Length.ToString());
            context.Response.Body.Position = 0;
            using var copy = new MemoryStream();
            await context.Response.Body.CopyToAsync(copy);
            copy.ToArray().ShouldBe(bytes);
        }
        finally
        {
            Web.HttpContextAccessor = previousAccessor;
            File.Delete(path);
        }
    }

    /// <summary>

    /// 验证复用已配置的处理管线。

    /// </summary>
    [Fact]
    public async Task NamedHttpClientFactory_ReusesConfiguredHandlerPipeline()
    {
        var handlerCreations = 0;
        var services = new ServiceCollection();
        services.AddHttpClient("test-server", client => client.BaseAddress = new Uri("http://localhost/"))
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                Interlocked.Increment(ref handlerCreations);
                return _host.GetTestServer().CreateHandler();
            });

        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var service = new HttpClientService(factory);

        var first = await service.Get("api/test7/delay/0").HttpClientName("test-server").GetResultAsync();
        var second = await service.Get("api/test7/delay/0").HttpClientName("test-server").GetResultAsync();

        first.ShouldBe("ok:0");
        second.ShouldBe("ok:0");
        handlerCreations.ShouldBe(1);
    }

    /// <summary>

    /// 验证根据请求名称选择已配置的处理程序。

    /// </summary>
    [Fact]
    public async Task NamedHttpClient_SelectsTheConfiguredHandlerByRequestName()
    {
        var services = new ServiceCollection();
        services.AddHttpClient("first")
            .ConfigurePrimaryHttpMessageHandler(() =>
                new FixedBodyHandler(HttpStatusCode.OK, Encoding.UTF8.GetBytes("first-handler")));
        services.AddHttpClient("second")
            .ConfigurePrimaryHttpMessageHandler(() =>
                new FixedBodyHandler(HttpStatusCode.OK, Encoding.UTF8.GetBytes("second-handler")));

        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var service = new HttpClientService(factory);

        var first = await service.Get("https://example.test/selected")
            .HttpClientName("first").GetResultAsync();
        var second = await service.Get("https://example.test/selected")
            .HttpClientName("second").GetResultAsync();

        first.ShouldBe("first-handler");
        second.ShouldBe("second-handler");
    }
    /// <summary>
    /// 验证失败响应保留现有文件。
    /// </summary>
    [Fact]
    public async Task DownloadFileAsync_FailedResponsePreservesExistingFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), "Bing.Utils.Http.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "download.bin");
        await File.WriteAllTextAsync(path, "existing");
        try
        {
            using var client = new HttpClient(new FailedResponseHandler());
            await new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/file")
                .DownloadFileAsync(path);
            (await File.ReadAllTextAsync(path)).ShouldBe("existing");
            Directory.GetFiles(directory).ShouldBe(new[] { path });
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>

    /// 验证传输失败时保留现有文件并删除临时文件。

    /// </summary>
    [Fact]
    public async Task DownloadFileAsync_TransferFailurePreservesExistingFileAndRemovesTemporaryFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), "Bing.Utils.Http.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "download.bin");
        await File.WriteAllTextAsync(path, "existing");

        try
        {
            using var client = new HttpClient(new FailingBodyHandler());
            await Should.ThrowAsync<IOException>(async () =>
                await new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/file")
                    .DownloadFileAsync(path));

            (await File.ReadAllTextAsync(path)).ShouldBe("existing");
            Directory.GetFiles(directory).ShouldBe(new[] { path });
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>

    /// 验证取消时保留现有文件并删除临时文件。

    /// </summary>
    [Fact]
    public async Task DownloadFileAsync_CancellationPreservesExistingFileAndRemovesTemporaryFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), "Bing.Utils.Http.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "download.bin");
        await File.WriteAllTextAsync(path, "existing");
        using var cancellation = new CancellationTokenSource();
        try
        {
            using var client = new HttpClient(new CancelingBodyHandler(cancellation));
            await Should.ThrowAsync<OperationCanceledException>(async () =>
                await new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/file")
                    .DownloadFileAsync(path, cancellation.Token));

            (await File.ReadAllTextAsync(path)).ShouldBe("existing");
            Directory.GetFiles(directory).ShouldBe(new[] { path });
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>

    /// 验证取消期间清理响应之前提交结果，并保留目标文件。

    /// </summary>
    /// <param name="withResult">测试输入值。</param>
    /// <param name="targetExists">测试输入值。</param>
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task DownloadFile_CancellationDuringResponseCleanupBeforeCommitPreservesTarget(
        bool withResult, bool targetExists)
    {
        var directory = Path.Combine(Path.GetTempPath(), "Bing.Utils.Http.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "download.bin");
        if (targetExists) await File.WriteAllTextAsync(path, "existing", Encoding.UTF8);
        using var cancellation = new CancellationTokenSource();
        var completions = new List<HttpRequestCompletion>();
        var handler = new CancellingDisposeBodyHandler(cancellation);
        using var client = new HttpClient(handler);
        var completeCalls = 0;

        try
        {
            var request = new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/file")
                .OnComplete((_, _) => completeCalls++)
                .ObserveCompletion(completions.Add);

            OperationCanceledException error = null;
            try
            {
                if (withResult)
                    await request.DownloadFileWithResultAsync(path, cancellation.Token);
                else
                    await request.DownloadFileAsync(path, cancellation.Token);
            }
            catch (OperationCanceledException exception)
            {
                error = exception;
            }

            error.ShouldNotBeNull();
            error.CancellationToken.ShouldBe(cancellation.Token);
            completeCalls.ShouldBe(1);
            File.Exists(path).ShouldBe(targetExists);
            if (targetExists)
                (await File.ReadAllTextAsync(path, Encoding.UTF8)).ShouldBe("existing");
            Directory.GetFiles(directory).ShouldBe(targetExists ? new[] { path } : Array.Empty<string>());
            handler.SendCount.ShouldBe(1);
            completions.Count.ShouldBe(1);
            completions[0].StatusCode.ShouldBe(HttpStatusCode.OK);
            completions[0].Attempts.ShouldBe(1);
            completions[0].Outcome.ShouldBe(HttpRequestOutcome.Cancelled);
            completions[0].Exception.ShouldBeSameAs(error);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>

    /// 验证成功响应会保存文件并报告文件状态。

    /// </summary>
    [Fact]
    public async Task DownloadFileWithResultAsync_SavesSuccessfulResponseAndReportsFileState()
    {
        var directory = Path.Combine(Path.GetTempPath(), "Bing.Utils.Http.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "download.bin");
        await File.WriteAllTextAsync(path, "replace me");
        var bytes = Enumerable.Range(1, 513).Select(x => (byte)(x % 251)).ToArray();
        var completions = new List<HttpRequestCompletion>();
        try
        {
            using var client = new HttpClient(new FixedBodyHandler(HttpStatusCode.OK, bytes));
            var result = await new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/file")
                .ObserveCompletion(completions.Add)
                .DownloadFileWithResultAsync(path);

            result.StatusCode.ShouldBe(HttpStatusCode.OK);
            result.IsSuccessStatusCode.ShouldBeTrue();
            result.BytesWritten.ShouldBe(bytes.Length);
            result.IsFileSaved.ShouldBeTrue();
            (await File.ReadAllBytesAsync(path)).ShouldBe(bytes);
            Directory.GetFiles(directory).ShouldBe(new[] { path });
            completions.Count.ShouldBe(1);
            completions[0].StatusCode.ShouldBe(HttpStatusCode.OK);
            completions[0].Attempts.ShouldBe(1);
            completions[0].Outcome.ShouldBe(HttpRequestOutcome.Success);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>

    /// 验证 HTTP 失败时保留现有文件并报告文件未保存。

    /// </summary>
    [Fact]
    public async Task DownloadFileWithResultAsync_HttpFailurePreservesExistingFileAndReportsNoSave()
    {
        var directory = Path.Combine(Path.GetTempPath(), "Bing.Utils.Http.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "download.bin");
        await File.WriteAllTextAsync(path, "existing");
        try
        {
            using var client = new HttpClient(new FailedResponseHandler());
            var result = await new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/file")
                .DownloadFileWithResultAsync(path);

            result.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            result.IsSuccessStatusCode.ShouldBeFalse();
            result.BytesWritten.ShouldBe(0);
            result.IsFileSaved.ShouldBeFalse();
            (await File.ReadAllTextAsync(path)).ShouldBe("existing");
            Directory.GetFiles(directory).ShouldBe(new[] { path });
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>

    /// 验证保存失败时删除临时文件并报告失败状态。

    /// </summary>
    [Fact]
    public async Task DownloadFileWithResultAsync_SaveFailureRemovesTemporaryFileAndReportsFaulted()
    {
        var parent = Path.Combine(Path.GetTempPath(), "Bing.Utils.Http.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(parent);
        var targetDirectory = Path.Combine(parent, "target");
        Directory.CreateDirectory(targetDirectory);
        var completions = new List<HttpRequestCompletion>();
        try
        {
            using var client = new HttpClient(new FixedBodyHandler(HttpStatusCode.OK, new byte[] { 1, 2, 3 }));
            var error = await Should.ThrowAsync<IOException>(() =>
                new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/file")
                    .ObserveCompletion(completions.Add)
                    .DownloadFileWithResultAsync(targetDirectory));

            error.ShouldNotBeNull();
            Directory.Exists(targetDirectory).ShouldBeTrue();
            Directory.GetFileSystemEntries(parent).ShouldBe(new[] { targetDirectory });
            completions.Count.ShouldBe(1);
            completions[0].StatusCode.ShouldBe(HttpStatusCode.OK);
            completions[0].Attempts.ShouldBe(1);
            completions[0].Outcome.ShouldBe(HttpRequestOutcome.Faulted);
            completions[0].Exception.ShouldBeSameAs(error);
        }
        finally
        {
            Directory.Delete(parent, recursive: true);
        }
    }

    /// <summary>

    /// 验证解压后的正文超过限制时会被拒绝。

    /// </summary>
    [Fact]
    public async Task MaxResponseBytes_RejectsExpandedBodyAfterDecompression()
    {
        using var compressed = new MemoryStream();
        using (var gzip = new GZipStream(compressed, CompressionLevel.Fastest, leaveOpen: true))
        {
            var original = new byte[4096];
            await gzip.WriteAsync(original, 0, original.Length);
        }
        compressed.Position = 0;
        using var body = new GZipStream(compressed, CompressionMode.Decompress, leaveOpen: true);
        using var client = new HttpClient(new DecompressedBodyHandler(body));

        await Should.ThrowAsync<HttpResponseSizeLimitException>(async () =>
            await new HttpRequest<string>(null, client, HttpMethod.Get, "https://example.test/file")
                .MaxResponseBytes(1024)
                .DownloadToAsync(Stream.Null));
    }

    /// <summary>

    /// 固定返回未找到状态码的消息处理器。

    /// </summary>
    private sealed class FailedResponseHandler : HttpMessageHandler
    {
        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("missing")
            });
    }

    /// <summary>

    /// 返回指定状态码和正文数据的消息处理器。

    /// </summary>
    private sealed class FixedBodyHandler : HttpMessageHandler
    {
        /// <summary>
        /// 存储测试响应使用的 HTTP 状态码。
        /// </summary>
        private readonly HttpStatusCode _statusCode;
        /// <summary>
        /// 测试请求或响应使用的字节数据。
        /// </summary>
        private readonly byte[] _bytes;
        /// <summary>
        /// 初始化一个<see cref="FixedBodyHandler"/>类型的实例。
        /// </summary>
        /// <param name="statusCode">测试响应的 HTTP 状态码。</param>
        /// <param name="bytes">响应正文的字节数据。</param>
        public FixedBodyHandler(HttpStatusCode statusCode, byte[] bytes) { _statusCode = statusCode; _bytes = bytes; }
        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(_statusCode) { Content = new ByteArrayContent(_bytes) });
    }

    /// <summary>

    /// 返回在响应内容释放时触发取消的消息处理器。

    /// </summary>
    private sealed class CancellingDisposeBodyHandler : HttpMessageHandler
    {
        /// <summary>
        /// 控制测试取消时机的令牌源。
        /// </summary>
        private readonly CancellationTokenSource _cancellation;
        /// <summary>
        /// 记录处理程序发送请求的次数。
        /// </summary>
        public int SendCount { get; private set; }

        /// <summary>

        /// 初始化一个<see cref="CancellingDisposeBodyHandler"/>类型的实例。

        /// </summary>
        /// <param name="cancellation">用于触发测试取消的令牌源。</param>
        public CancellingDisposeBodyHandler(CancellationTokenSource cancellation) => _cancellation = cancellation;

        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            SendCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new CancellingDisposeContent(_cancellation)
            });
        }
    }

    /// <summary>

    /// 释放内容时触发取消的字节内容包装器。

    /// </summary>
    private sealed class CancellingDisposeContent : ByteArrayContent
    {
        /// <summary>
        /// 控制测试取消时机的令牌源。
        /// </summary>
        private readonly CancellationTokenSource _cancellation;

        /// <summary>

        /// 初始化一个<see cref="CancellingDisposeContent"/>类型的实例。

        /// </summary>
        /// <param name="cancellation">用于触发测试取消的令牌源。</param>
        public CancellingDisposeContent(CancellationTokenSource cancellation)
            : base(new byte[] { 1, 2, 3 }) => _cancellation = cancellation;

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing) _cancellation.Cancel();
            base.Dispose(disposing);
        }
    }

    /// <summary>

    /// 返回待自动解压流的消息处理器。

    /// </summary>
    private sealed class DecompressedBodyHandler : HttpMessageHandler
    {
        /// <summary>
        /// 测试请求或响应使用的字节数据。
        /// </summary>
        private readonly Stream _body;
        /// <summary>
        /// 初始化一个<see cref="DecompressedBodyHandler"/>类型的实例。
        /// </summary>
        /// <param name="body">响应正文数据。</param>
        public DecompressedBodyHandler(Stream body) => _body = body;
        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(_body)
            });
    }

    /// <summary>

    /// 返回读取正文时失败的响应内容。

    /// </summary>
    private sealed class FailingBodyHandler : HttpMessageHandler
    {
        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new FailingBodyContent()
            };
            return Task.FromResult(response);
        }
    }

    /// <summary>

    /// 返回读取正文后触发取消的消息处理器。

    /// </summary>
    private sealed class CancelingBodyHandler : HttpMessageHandler
    {
        /// <summary>
        /// 控制测试取消时机的令牌源。
        /// </summary>
        private readonly CancellationTokenSource _cancellation;
        /// <summary>
        /// 初始化一个<see cref="CancelingBodyHandler"/>类型的实例。
        /// </summary>
        /// <param name="cancellation">用于触发测试取消的令牌源。</param>
        public CancelingBodyHandler(CancellationTokenSource cancellation) => _cancellation = cancellation;
        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(new CancelAfterFirstReadStream(_cancellation))
            });
    }

    /// <summary>

    /// 首次读取返回数据后触发取消的内存流。

    /// </summary>
    private sealed class CancelAfterFirstReadStream : MemoryStream
    {
        /// <summary>
        /// 控制测试取消时机的令牌源。
        /// </summary>
        private readonly CancellationTokenSource _cancellation;
        /// <summary>
        /// 记录流是否已返回过正文数据。
        /// </summary>
        private bool _hasReturnedData;

        /// <summary>

        /// 初始化一个<see cref="CancelAfterFirstReadStream"/>类型的实例。

        /// </summary>
        /// <param name="cancellation">用于触发测试取消的令牌源。</param>
        public CancelAfterFirstReadStream(CancellationTokenSource cancellation) => _cancellation = cancellation;

        /// <inheritdoc />
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (_hasReturnedData)
            {
                _cancellation.Cancel();
                cancellationToken.ThrowIfCancellationRequested();
            }
            _hasReturnedData = true;
            buffer[offset] = 1;
            return Task.FromResult(1);
        }
    }

    /// <summary>

    /// 创建读取操作抛出异常的 HTTP 内容。

    /// </summary>
    private sealed class FailingBodyContent : HttpContent
    {
        /// <inheritdoc />
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
            => throw new NotSupportedException();

        /// <inheritdoc />
        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }

        /// <inheritdoc />
        protected override Task<Stream> CreateContentReadStreamAsync()
            => Task.FromResult<Stream>(new FailAfterFirstReadStream());
    }

    /// <summary>

    /// 首次读取后抛出异常的内存流。

    /// </summary>
    private sealed class FailAfterFirstReadStream : MemoryStream
    {
        /// <summary>
        /// 记录流是否已返回过正文数据。
        /// </summary>
        private bool _hasReturnedData;

        /// <inheritdoc />
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (_hasReturnedData)
                throw new IOException("Simulated response transfer failure.");

            _hasReturnedData = true;
            buffer[offset] = 1;
            return Task.FromResult(1);
        }
    }
}
