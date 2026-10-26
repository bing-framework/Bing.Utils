using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Bing.Http;
using Microsoft.AspNetCore.Http;

namespace Bing.Utils.Http.Tests.Bing.Http;

/// <summary>
/// 验证 HTTP 响应流式 JSON 序列化及取消行为。
/// </summary>
[Trait("Bing.Http", "HttpResponseJsonStreamExtensions")]
public class HttpResponseJsonStreamExtensionsTest
{
    /// <summary>
    /// 验证对象被直接序列化为 UTF-8 JSON，且响应正文流保持打开。
    /// </summary>
    [Fact]
    public async Task WriteJsonStreamAsync_WritesJsonAndLeavesBodyOpen()
    {
        using var body = new TrackingStream();
        var response = CreateResponse(body);

        await response.WriteJsonStreamAsync(new { Name = "Ada" });

        Encoding.UTF8.GetString(body.ToArray()).ShouldBe("{\"Name\":\"Ada\"}");
        response.ContentType.ShouldBe("application/json; charset=utf-8");
        body.IsDisposed.ShouldBeFalse();
    }

    /// <summary>
    /// 验证字符串作为 JSON 字符串值序列化，而不是写作未转义正文。
    /// </summary>
    [Fact]
    public async Task WriteJsonStreamAsync_StringValueWritesJsonString()
    {
        using var body = new MemoryStream();
        var response = CreateResponse(body);

        await response.WriteJsonStreamAsync("hello");

        Encoding.UTF8.GetString(body.ToArray()).ShouldBe("\"hello\"");
    }

    /// <summary>
    /// 验证自定义序列化选项应用于直接流式写入。
    /// </summary>
    [Fact]
    public async Task WriteJsonStreamAsync_AppliesSerializerOptions()
    {
        using var body = new MemoryStream();
        var response = CreateResponse(body);
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        await response.WriteJsonStreamAsync(new { FirstName = "Ada" }, options);

        Encoding.UTF8.GetString(body.ToArray()).ShouldBe("{\"firstName\":\"Ada\"}");
    }

    /// <summary>
    /// 验证显式取消和 RequestAborted 任一取消都会中止正在进行的写入。
    /// </summary>
    /// <param name="cancelRequestAborted">是否取消 RequestAborted；否则取消调用方令牌。</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task WriteJsonStreamAsync_CancelsInProgressWriteWhenEitherTokenIsCanceled(bool cancelRequestAborted)
    {
        using var requestAborted = new CancellationTokenSource();
        using var caller = new CancellationTokenSource();
        using var body = new CancelDuringWriteStream(() =>
        {
            if (cancelRequestAborted)
                requestAborted.Cancel();
            else
                caller.Cancel();
        });
        var response = CreateResponse(body);
        response.HttpContext.RequestAborted = requestAborted.Token;

        var write = response.WriteJsonStreamAsync(new { Value = 1 }, cancellationToken: caller.Token);
        await Should.ThrowAsync<OperationCanceledException>(() => write);

        body.IsDisposed.ShouldBeFalse();
        requestAborted.IsCancellationRequested.ShouldBe(cancelRequestAborted);
        caller.IsCancellationRequested.ShouldBe(!cancelRequestAborted);
    }

    /// <summary>
    /// 验证预取消的任一令牌都会在设置响应类型和写入正文前终止操作。
    /// </summary>
    /// <param name="cancelRequestAborted">是否预取消 RequestAborted；否则预取消调用方令牌。</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task WriteJsonStreamAsync_PreCanceledTokenDoesNotWrite(bool cancelRequestAborted)
    {
        using var requestAborted = new CancellationTokenSource();
        using var caller = new CancellationTokenSource();
        var body = new TrackingStream();
        var response = CreateResponse(body);
        response.HttpContext.RequestAborted = requestAborted.Token;
        if (cancelRequestAborted)
            requestAborted.Cancel();
        else
            caller.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() =>
            response.WriteJsonStreamAsync(new { Value = 1 }, cancellationToken: caller.Token));

        body.Length.ShouldBe(0);
        response.ContentType.ShouldBeNull();
        body.IsDisposed.ShouldBeFalse();
    }

    private static HttpResponse CreateResponse(Stream body)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = body;
        return context.Response;
    }

    private sealed class TrackingStream : MemoryStream
    {
        public bool IsDisposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }

    private sealed class CancelDuringWriteStream : Stream
    {
        private readonly Action _cancel;

        public CancelDuringWriteStream(Action cancel) => _cancel = cancel;

        public bool IsDisposed { get; private set; }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            _cancel();
            return Task.Delay(1, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }
}
