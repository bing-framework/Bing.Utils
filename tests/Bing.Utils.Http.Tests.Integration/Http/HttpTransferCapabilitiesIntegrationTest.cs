using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using Bing.Http;
using Bing.Http.Clients;
using Bing.Utils.Http.Tests.Integration.Controllers;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace Bing.Utils.Http.Tests.Integration.Http;



/// <summary>
/// 验证 HTTP 传输新能力与本地 TestServer、文件系统的协作。
/// </summary>
public class HttpTransferCapabilitiesIntegrationTest
{
    /// <summary>
    /// 承载集成测试服务的宿主实例。
    /// </summary>
    private readonly IHost _host;
    /// <summary>
    /// 初始化一个 <see cref="HttpTransferCapabilitiesIntegrationTest"/> 类型的实例。
    /// </summary>
    /// <param name="host">用于测试的依赖注入应用实例。</param>
    public HttpTransferCapabilitiesIntegrationTest(IHost host) => _host = host;



    /// <summary>
    /// 验证解析本地测试服务端响应。
    /// </summary>
    [Fact]
    public async Task GetJsonStreamAsync_ParsesLocalTestServerResponse()
    {
        using var client = _host.GetTestServer().CreateClient();
        var result = await new HttpRequest<Test7Controller.ErrorResponse>(null, client, HttpMethod.Get,
                "http://localhost/api/test7/status/200")
            .GetJsonStreamAsync();

        result.Code.ShouldBe(200);
        result.Message.ShouldBe("error:200");
    }



    /// <summary>
    /// 验证正文读取到末尾后，即使文件提交失败也会报告读取完成。
    /// </summary>
    [Fact]
    public async Task DownloadProgress_EofRemainsCompletedWhenFileCommitFails()
    {
        var root = Path.Combine(Path.GetTempPath(), "Bing.Utils.Http.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var targetDirectory = Path.Combine(root, "download.bin");
        Directory.CreateDirectory(targetDirectory);
        var progress = new List<HttpDownloadProgress>();
        try
        {
            using var client = _host.GetTestServer().CreateClient();
            await Should.ThrowAsync<IOException>(() =>
                new HttpRequest<string>(null, client, HttpMethod.Get, "http://localhost/api/test7/status/200")
                    .ObserveDownloadProgress(progress.Add, minimumBytes: 1, minimumInterval: TimeSpan.Zero)
                    .DownloadFileAsync(targetDirectory));

            progress.Count.ShouldBeGreaterThan(1);
            progress[0].IsCompleted.ShouldBeFalse();
            progress[0].BytesWritten.ShouldBe(0);
            progress.Last().IsCompleted.ShouldBeTrue();
            progress.Last().BytesWritten.ShouldBeGreaterThan(0);
            Directory.GetFileSystemEntries(root).ShouldBe(new[] { targetDirectory });
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }



    /// <summary>
    /// 验证正文读取结束后清理响应触发取消时会保留目标文件并观察取消。
    /// </summary>
    [Fact]
    public async Task DownloadProgress_EofThenResponseCleanupCancellationPreservesTargetAndObservesCancellation()
    {
        var root = Path.Combine(Path.GetTempPath(), "Bing.Utils.Http.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var target = Path.Combine(root, "download.bin");
        await File.WriteAllTextAsync(target, "existing", Encoding.UTF8);
        using var cancellation = new CancellationTokenSource();
        var progress = new List<HttpDownloadProgress>();
        var completions = new List<HttpRequestCompletion>();
        var handler = new CancelOnContentDisposeHandler(_host.GetTestServer().CreateHandler(), cancellation);
        try
        {
            using var client = new HttpClient(handler);
            var request = new HttpRequest<string>(null, client, HttpMethod.Get,
                    "http://localhost/api/test7/status/200")
                .ObserveDownloadProgress(progress.Add, minimumBytes: 1, minimumInterval: TimeSpan.Zero)
                .ObserveCompletion(completions.Add);

            OperationCanceledException error = null;
            try
            {
                await request.DownloadFileWithResultAsync(target, cancellation.Token);
            }
            catch (OperationCanceledException exception)
            {
                error = exception;
            }

            error.ShouldNotBeNull();
            error.CancellationToken.ShouldBe(cancellation.Token);
            (await File.ReadAllTextAsync(target, Encoding.UTF8)).ShouldBe("existing");
            Directory.GetFiles(root).ShouldBe(new[] { target });
            progress.Last().IsCompleted.ShouldBeTrue();
            completions.Count.ShouldBe(1);
            completions[0].StatusCode.ShouldBe(HttpStatusCode.OK);
            completions[0].Attempts.ShouldBe(1);
            completions[0].Outcome.ShouldBe(HttpRequestOutcome.Cancelled);
            completions[0].Exception.ShouldBeSameAs(error);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }



    /// <summary>
    /// 包装内部处理器并在响应内容释放时触发取消。
    /// </summary>
    private sealed class CancelOnContentDisposeHandler : DelegatingHandler
    {
        /// <summary>
        /// 控制测试取消时机的令牌源。
        /// </summary>
        private readonly CancellationTokenSource _cancellation;
        /// <summary>
        /// 初始化一个 <see cref="CancelOnContentDisposeHandler"/> 类型的实例。
        /// </summary>
        /// <param name="inner">实际发送测试请求的内部处理器。</param>
        /// <param name="cancellation">响应内容释放时触发取消的令牌源。</param>
        public CancelOnContentDisposeHandler(HttpMessageHandler inner, CancellationTokenSource cancellation)
        {
            InnerHandler = inner;
            _cancellation = cancellation;
        }

        /// <inheritdoc />
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);
            if (response.Content != null)
                response.Content = new CancelOnDisposeContent(response.Content, _cancellation);
            return response;
        }
    }



    /// <summary>
    /// 释放时取消令牌并释放内部内容的包装器。
    /// </summary>
    private sealed class CancelOnDisposeContent : HttpContent
    {
        /// <summary>
        /// 被包装的 HTTP 内容。
        /// </summary>
        private readonly HttpContent _inner;
        /// <summary>
        /// 控制测试取消时机的令牌源。
        /// </summary>
        private readonly CancellationTokenSource _cancellation;



        /// <summary>
        /// 初始化一个 <see cref="CancelOnDisposeContent"/> 类型的实例。
        /// </summary>
        /// <param name="inner">需要包装并在释放时一同释放的响应内容。</param>
        /// <param name="cancellation">内容释放时触发取消的令牌源。</param>
        public CancelOnDisposeContent(HttpContent inner, CancellationTokenSource cancellation)
        {
            _inner = inner;
            _cancellation = cancellation;
            foreach (var header in inner.Headers)
                Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        /// <inheritdoc />
        protected override Task SerializeToStreamAsync(Stream stream, System.Net.TransportContext context)
            => _inner.CopyToAsync(stream);

        /// <inheritdoc />
        protected override bool TryComputeLength(out long length)
        {
            if (_inner.Headers.ContentLength.HasValue)
            {
                length = _inner.Headers.ContentLength.Value;
                return true;
            }
            length = 0;
            return false;
        }

        /// <inheritdoc />
        protected override Task<Stream> CreateContentReadStreamAsync() => _inner.ReadAsStreamAsync();

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cancellation.Cancel();
                _inner.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
