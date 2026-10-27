using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Bing.Http;
using Microsoft.AspNetCore.Http;

namespace Bing.Utils.Http.Tests.Bing.Http;

[Collection("TimingSensitive")]
public class ServerSentEventsTest
{
    [Fact]
    public async Task ParsesMultilineBomCrLfIdResetAndDropsIncompleteEvent()
    {
        using var stream = new ShortStream("\ufeff: comment\r\nid: 42\revent: update\ndata: one\r\ndata: two\r\n\r\nid:\ndata:\n\ndata: incomplete");
        var values = await Collect(ServerSentEvents.ReadAsync(stream));
        Assert.Equal(2, values.Count); Assert.Equal("one\ntwo", values[0].Data); Assert.Equal("update", values[0].EventType); Assert.Equal("42", values[0].Id);
        Assert.Equal("", values[1].Data); Assert.Equal("", values[1].Id); Assert.Equal("message", values[1].EventType);
    }
    [Fact]
    public async Task ByteBudgetsAndWriterRejectHeaderInjection()
    {
        using var stream = new ShortStream("data: 123\n\n");
        await Assert.ThrowsAsync<InvalidDataException>(() => Collect(ServerSentEvents.ReadAsync(stream, new SseReadOptions { MaxEventBytes = 5 })));
        using var output = new MemoryStream();
        await Assert.ThrowsAsync<ArgumentException>(() => ServerSentEvents.WriteAsync(output, new ServerSentEvent { Id = "id\ndata: injected" })); Assert.Equal(0, output.Length);
        await Assert.ThrowsAsync<InvalidDataException>(() => ServerSentEvents.WriteAsync(output, new ServerSentEvent { Data = "long" }, 2)); Assert.Equal(0, output.Length);
        await ServerSentEvents.WriteAsync(output, new ServerSentEvent { Data = "one\r\ntwo\n", Id = "x", RetryMilliseconds = 25 });
        output.Position = 0; var result = Assert.Single(await Collect(ServerSentEvents.ReadAsync(output)));
        Assert.Equal("one\ntwo\n", result.Data); Assert.Equal(25, result.RetryMilliseconds);
    }
    [Fact]
    public async Task ReconnectSendsLastIdAnd204Stops()
    {
        var calls = 0;
        using var client = new HttpClient(new Handler(request =>
        {
            if (++calls == 1) return EventResponse("id: 7\nretry: 0\ndata: first\n\n");
            Assert.Equal("7", Assert.Single(request.Headers.GetValues("Last-Event-ID")));
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }));
        var records = await Collect(client.GetServerSentEventsAsync(new Uri("https://test/events"), new HttpSseOptions { MaxReconnects = 3, ReconnectDelay = TimeSpan.Zero }));
        Assert.Single(records); Assert.Equal(2, calls);
    }
    [Fact]
    public async Task CancellationAndEarlyExitCloseOwnedResponses()
    {
        var body = new ShortStream("data: first\n\ndata: second\n\n");
        using var client = new HttpClient(new Handler(_ => EventResponse(body)));
        await using (var enumerator = client.GetServerSentEventsAsync(new Uri("https://test/events")).GetAsyncEnumerator()) Assert.True(await enumerator.MoveNextAsync());
        Assert.False(body.CanRead);
        using var cts = new CancellationTokenSource(); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Collect(client.GetServerSentEventsAsync(new Uri("https://test/events"), cancellationToken: cts.Token)));
    }
    [Fact]
    public async Task ResponseWriterKeepsBodyAndHonorsRequestAborted()
    {
        var context = new DefaultHttpContext(); using var output = new MemoryStream(); context.Response.Body = output;
        async IAsyncEnumerable<ServerSentEvent> Source([EnumeratorCancellation] CancellationToken token = default)
        { token.ThrowIfCancellationRequested(); yield return new ServerSentEvent { Data = "中文" }; await Task.Yield(); }
        await context.Response.WriteServerSentEventsAsync(Source()); Assert.Equal("text/event-stream; charset=utf-8", context.Response.ContentType); Assert.True(output.CanWrite);
        output.Position = 0; Assert.Equal("中文", Assert.Single(await Collect(ServerSentEvents.ReadAsync(output))).Data);
        using var cts = new CancellationTokenSource(); cts.Cancel(); context.RequestAborted = cts.Token;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.Response.WriteServerSentEventsAsync(Source()));
    }
    [Fact]
    public async Task ControlLineAtEofIsProcessedWithoutDispatchingPartialData()
    {
        using var source = new ShortStream("data: unfinished\nretry: 12");
        var parser = new SseParser(source, new SseReadOptions(), "old");
        Assert.Null(await parser.ReadNextAsync(CancellationToken.None));
        Assert.Equal(12, parser.RetryMilliseconds); Assert.Equal("old", parser.LastEventId);
    }

    [Fact]
    public async Task ExactInputBudgetSucceedsAndProtocolErrorsDoNotReconnect()
    {
        using var source = new ShortStream("data: x\n\n");
        Assert.Single(await Collect(ServerSentEvents.ReadAsync(source, new SseReadOptions { MaxInputBytes = 9, MaxEventBytes = 7 })));
        var calls = 0;
        using var client = new HttpClient(new Handler(_ =>
        { calls++; return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("not-sse") }; }));
        await Assert.ThrowsAsync<InvalidDataException>(() => Collect(client.GetServerSentEventsAsync(new Uri("https://test/events"),
            new HttpSseOptions { MaxReconnects = 3, ReconnectDelay = TimeSpan.Zero })));
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task IdleTimeoutClosesResponseAndReportsTimeout()
    {
        var body = new StallingStream();
        using var client = new HttpClient(new Handler(_ => EventResponse(body)));
        await Assert.ThrowsAsync<TimeoutException>(() => Collect(client.GetServerSentEventsAsync(new Uri("https://test/events"),
            new HttpSseOptions { IdleTimeout = TimeSpan.FromMilliseconds(50) })));
        Assert.False(body.CanRead);
    }

    [Fact]
    public async Task IdleReconnectPreservesLastCommittedId()
    {
        var calls = 0;
        var body = new StallingStream("id: 8\ndata: first\n\n");
        using var client = new HttpClient(new Handler(request =>
        {
            if (++calls == 1) return EventResponse(body);
            Assert.Equal("8", Assert.Single(request.Headers.GetValues("Last-Event-ID")));
            Assert.False(body.CanRead);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }));
        Assert.Single(await Collect(client.GetServerSentEventsAsync(new Uri("https://test/events"), new HttpSseOptions
        { IdleTimeout = TimeSpan.FromMilliseconds(50), MaxReconnects = 1, ReconnectDelay = TimeSpan.Zero })));
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task ExplicitCancellationKeepsTokenAndDoesNotReconnect()
    {
        var body = new StallingStream(); var calls = 0;
        using var cancellation = new CancellationTokenSource();
        using var client = new HttpClient(new Handler(_ => { calls++; return EventResponse(body); }));
        var read = Collect(client.GetServerSentEventsAsync(new Uri("https://test/events"), new HttpSseOptions
        { IdleTimeout = TimeSpan.FromSeconds(10), MaxReconnects = 1 }, cancellation.Token));
        await body.Waiting.Task; cancellation.Cancel();
        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => read);
        Assert.Equal(cancellation.Token, error.CancellationToken); Assert.Equal(1, calls); Assert.False(body.CanRead);
    }

    [Fact]
    public async Task ConsumerDelayDoesNotConsumeIdleBudget()
    {
        using var client = new HttpClient(new Handler(_ => EventResponse("data: one\n\ndata: two\n\n")));
        await using var reader = client.GetServerSentEventsAsync(new Uri("https://test/events"),
            new HttpSseOptions { IdleTimeout = TimeSpan.FromMilliseconds(30) }).GetAsyncEnumerator();
        Assert.True(await reader.MoveNextAsync());
        await Task.Delay(80);
        Assert.True(await reader.MoveNextAsync()); Assert.Equal("two", reader.Current.Data);
    }

    [Fact]
    public async Task HeartbeatBytesResetIdleWaitWithoutDispatchingEvents()
    {
        using var body = new HeartbeatStream();
        using var client = new HttpClient(new Handler(_ => EventResponse(body)));
        var values = await Collect(client.GetServerSentEventsAsync(new Uri("https://test/events"),
            new HttpSseOptions { IdleTimeout = TimeSpan.FromMilliseconds(200) }));
        Assert.Equal("done", Assert.Single(values).Data); Assert.Equal(13, body.Reads);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(2147483647)]
    public void InvalidIdleTimeoutRejected(double milliseconds)
    {
        using var client = new HttpClient();
        Assert.Throws<ArgumentOutOfRangeException>(() => client.GetServerSentEventsAsync(new Uri("https://test/events"),
            new HttpSseOptions { IdleTimeout = TimeSpan.FromMilliseconds(milliseconds) }));
    }

    private sealed class StallingStream : MemoryStream
    {
        internal readonly TaskCompletionSource<bool> Waiting = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal StallingStream(string prefix = "") : base(Encoding.UTF8.GetBytes(prefix)) { }
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token)
        {
            if (Position < Length) return await base.ReadAsync(buffer, offset, count, token);
            Waiting.TrySetResult(true);
            await Task.Delay(Timeout.Infinite, token); return 0;
        }
    }
    private sealed class HeartbeatStream : MemoryStream
    {
        private const int HeartbeatCount = 11;
        internal int Reads;
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token)
        {
            var read = ++Reads;
            if (read == HeartbeatCount + 2) return 0;
            if (read <= HeartbeatCount) await Task.Delay(20, token);
            var bytes = Encoding.UTF8.GetBytes(read == HeartbeatCount + 1 ? "data: done\n\n" : ": ping\n\n");
            Array.Copy(bytes, 0, buffer, offset, bytes.Length); return bytes.Length;
        }
    }

    private static HttpResponseMessage EventResponse(string text) => EventResponse(new ShortStream(text));
    private static HttpResponseMessage EventResponse(Stream stream)
    { var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(stream) }; response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/event-stream"); return response; }
    private static async Task<List<T>> Collect<T>(IAsyncEnumerable<T> source) { var values = new List<T>(); await foreach (var value in source) values.Add(value); return values; }
    private sealed class Handler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handle;
        internal Handler(Func<HttpRequestMessage, HttpResponseMessage> handle) => _handle = handle;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => Task.FromResult(_handle(request));
    }
    private sealed class ShortStream : MemoryStream
    {
        internal ShortStream(string text) : base(Encoding.UTF8.GetBytes(text)) { }
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token) => base.ReadAsync(buffer, offset, Math.Min(count, 2), token);
    }
}
