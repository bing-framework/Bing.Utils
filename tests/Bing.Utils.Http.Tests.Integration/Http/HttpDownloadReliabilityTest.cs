using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Bing.Http.Clients;

namespace Bing.Utils.Http.Tests.Integration.Http;

/// <summary>
/// 验证下载写入和文件上传的可靠性。
/// </summary>
[Trait("Bing.Http", "HttpDownload.Reliability")]
public class HttpDownloadReliabilityTest
{
    /// <summary>
    /// 验证成功下载替换目标文件并清理临时文件。
    /// </summary>
    [Fact]
    public async Task WriteAsync_Success_ReplacesExistingFileAndCleansTemporaryFile()
    {
        using var directory = new TemporaryDirectory();
        var target = directory.GetPath("download.bin");
        File.WriteAllText(target, "old", Encoding.UTF8);
        using var handler = new FakeHandler((_, _) =>
            Task.FromResult(CreateResponse(HttpStatusCode.OK, "new")));
        using var client = CreateClient(handler);

        await new HttpClientService().SetHttpClient(client).Get("/download").WriteAsync(target);

        File.ReadAllText(target, Encoding.UTF8).ShouldBe("new");
        handler.SendCount.ShouldBe(1);
        directory.GetTemporaryFiles().ShouldBeEmpty();
    }

    /// <summary>
    /// 验证非成功响应保留原文件并清理临时文件。
    /// </summary>
    /// <param name="statusCode">模拟的非成功 HTTP 状态码。</param>
    [Theory]
    [InlineData(404)]
    [InlineData(500)]
    public async Task WriteAsync_NonSuccessResponse_PreservesExistingFileAndCleansTemporaryFile(int statusCode)
    {
        using var directory = new TemporaryDirectory();
        var target = directory.GetPath("download.bin");
        File.WriteAllText(target, "original", Encoding.UTF8);
        using var handler = new FakeHandler((_, _) =>
            Task.FromResult(CreateResponse((HttpStatusCode)statusCode, "failure")));
        using var client = CreateClient(handler);

        await Should.ThrowAsync<HttpRequestException>(() =>
            new HttpClientService().SetHttpClient(client).Get("/download").WriteAsync(target));

        File.ReadAllText(target, Encoding.UTF8).ShouldBe("original");
        handler.SendCount.ShouldBe(1);
        directory.GetTemporaryFiles().ShouldBeEmpty();
    }

    /// <summary>
    /// 验证发送前拒绝时保留原文件且不创建临时文件。
    /// </summary>
    [Fact]
    public async Task WriteAsync_SendBeforeRejects_PreservesExistingFileAndDoesNotCreateTemporaryFile()
    {
        using var directory = new TemporaryDirectory();
        var target = directory.GetPath("download.bin");
        File.WriteAllText(target, "original", Encoding.UTF8);
        using var handler = new FakeHandler((_, _) =>
            Task.FromResult(CreateResponse(HttpStatusCode.OK, "unexpected")));
        using var client = CreateClient(handler);

        await Should.ThrowAsync<HttpRequestException>(() =>
            new HttpClientService().SetHttpClient(client).Get("/download")
                .OnSendBefore(_ => false)
                .WriteAsync(target));

        File.ReadAllText(target, Encoding.UTF8).ShouldBe("original");
        handler.SendCount.ShouldBe(0);
        directory.GetTemporaryFiles().ShouldBeEmpty();
    }

    /// <summary>
    /// 验证预先取消时保留原文件且不创建临时文件。
    /// </summary>
    [Fact]
    public async Task WriteAsync_PreCanceled_PreservesExistingFileAndDoesNotCreateTemporaryFile()
    {
        using var directory = new TemporaryDirectory();
        var target = directory.GetPath("download.bin");
        File.WriteAllText(target, "original", Encoding.UTF8);
        using var handler = new FakeHandler((_, _) =>
            Task.FromResult(CreateResponse(HttpStatusCode.OK, "unexpected")));
        using var client = CreateClient(handler);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() =>
            new HttpClientService().SetHttpClient(client).Get("/download")
                .WriteAsync(target, cancellation.Token));

        File.ReadAllText(target, Encoding.UTF8).ShouldBe("original");
        handler.SendCount.ShouldBe(0);
        directory.GetTemporaryFiles().ShouldBeEmpty();
    }

    /// <summary>
    /// 验证响应期间取消时保留原文件并清理临时文件。
    /// </summary>
    [Fact]
    public async Task WriteAsync_CancellationDuringResponse_PreservesExistingFileAndCleansTemporaryFile()
    {
        using var directory = new TemporaryDirectory();
        var target = directory.GetPath("download.bin");
        File.WriteAllText(target, "original", Encoding.UTF8);
        var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var handler = new FakeHandler(async (_, cancellationToken) =>
        {
            started.TrySetResult(true);
            await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException("响应取消后不应继续执行");
        });
        using var client = CreateClient(handler);
        using var cancellation = new CancellationTokenSource();
        var request = new HttpClientService().SetHttpClient(client).Get("/download");

        var writeTask = request.WriteAsync(target, cancellation.Token);
        await started.Task;
        cancellation.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() => writeTask);

        File.ReadAllText(target, Encoding.UTF8).ShouldBe("original");
        handler.SendCount.ShouldBe(1);
        directory.GetTemporaryFiles().ShouldBeEmpty();
    }

    /// <summary>
    /// 验证目标文件被占用时保留原文件并清理临时文件。
    /// </summary>
    [Fact]
    public async Task WriteAsync_WhenTargetIsOccupied_PreservesExistingFileAndCleansTemporaryFile()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        using var directory = new TemporaryDirectory();
        var target = directory.GetPath("download.bin");
        File.WriteAllText(target, "original", Encoding.UTF8);
        using var handler = new FakeHandler((_, _) =>
            Task.FromResult(CreateResponse(HttpStatusCode.OK, "replacement")));
        using var client = CreateClient(handler);
        var request = new HttpClientService().SetHttpClient(client).Get("/download");
        Exception exception;

        using (var lockStream = new FileStream(target, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            exception = await Should.ThrowAsync<Exception>(() => request.WriteAsync(target));
        }

        (exception is IOException || exception is UnauthorizedAccessException).ShouldBeTrue();
        File.ReadAllText(target, Encoding.UTF8).ShouldBe("original");
        handler.SendCount.ShouldBe(1);
        directory.GetTemporaryFiles().ShouldBeEmpty();
    }

    /// <summary>
    /// 验证上传缺失文件时失败且不发送请求。
    /// </summary>
    [Fact]
    public async Task FileContent_MissingFile_ThrowsAndDoesNotSend()
    {
        using var directory = new TemporaryDirectory();
        var missing = directory.GetPath("missing.bin");
        using var handler = new FakeHandler((_, _) =>
            Task.FromResult(CreateResponse(HttpStatusCode.OK, "unexpected")));
        using var client = CreateClient(handler);

        await Should.ThrowAsync<FileNotFoundException>(() =>
            new HttpClientService().SetHttpClient(client).Post("/upload")
                .FileContent(missing, "file")
                .GetResultAsync());

        handler.SendCount.ShouldBe(0);
    }

    /// <summary>
    /// 验证空文件作为 multipart 文件段发送。
    /// </summary>
    [Fact]
    public async Task FileContent_EmptyFile_IsSentAsMultipartPart()
    {
        using var directory = new TemporaryDirectory();
        var filePath = directory.GetPath("empty.bin");
        File.WriteAllBytes(filePath, Array.Empty<byte>());
        var hasFilePart = false;
        using var handler = new FakeHandler(async (request, _) =>
        {
            var body = await request.Content.ReadAsStringAsync();
            hasFilePart = body.Contains("name=\"file\"", StringComparison.Ordinal);
            return CreateResponse(HttpStatusCode.OK, "ok");
        });
        using var client = CreateClient(handler);

        var result = await new HttpClientService().SetHttpClient(client)
            .Post("/upload")
            .FileContent(filePath, "file")
            .GetResultAsync();

        result.ShouldBe("ok");
        hasFilePart.ShouldBeTrue();
        handler.SendCount.ShouldBe(1);
    }

    /// <summary>
    /// 创建指向本地测试地址的 HTTP 客户端。
    /// </summary>
    /// <param name="handler">处理请求的测试处理器。</param>
    /// <returns>配置完成的 HTTP 客户端。</returns>
    private static HttpClient CreateClient(FakeHandler handler) => new(handler)
    {
        BaseAddress = new Uri("http://localhost/")
    };

    /// <summary>
    /// 创建包含文本内容的 HTTP 响应。
    /// </summary>
    /// <param name="statusCode">响应状态码。</param>
    /// <param name="content">响应文本。</param>
    /// <returns>创建的 HTTP 响应。</returns>
    private static HttpResponseMessage CreateResponse(HttpStatusCode statusCode, string content) =>
        new(statusCode) { Content = new StringContent(content, Encoding.UTF8, "text/plain") };

    /// <summary>
    /// 提供可控响应的测试 HTTP 处理器。
    /// </summary>
    private sealed class FakeHandler : HttpMessageHandler
    {
        /// <summary>
        /// 保存测试请求处理委托。
        /// </summary>
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _send;

        /// <summary>
        /// 初始化 <see cref="FakeHandler" /> 类的新实例。
        /// </summary>
        /// <param name="send">处理 HTTP 请求的委托。</param>
        public FakeHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
        {
            _send = send ?? throw new ArgumentNullException(nameof(send));
        }

        /// <summary>
        /// 获取已发送的请求数。
        /// </summary>
        public int SendCount { get; private set; }

        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            SendCount++;
            return _send(request, cancellationToken);
        }
    }

    /// <summary>
    /// 表示用于测试文件写入的临时目录。
    /// </summary>
    private sealed class TemporaryDirectory : IDisposable
    {
        /// <summary>
        /// 初始化 <see cref="TemporaryDirectory" /> 类的新实例。
        /// </summary>
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                $"bing-http-test-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        /// <summary>
        /// 获取临时目录路径。
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// 获取目录中指定文件的路径。
        /// </summary>
        /// <param name="fileName">文件名。</param>
        /// <returns>组合后的文件路径。</returns>
        public string GetPath(string fileName) => System.IO.Path.Combine(Path, fileName);

        /// <summary>
        /// 获取目录中由下载器创建的临时文件。
        /// </summary>
        /// <returns>匹配的临时文件路径。</returns>
        public string[] GetTemporaryFiles() => Directory.GetFiles(Path, ".bing-http-*.tmp");

        /// <inheritdoc />
        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
