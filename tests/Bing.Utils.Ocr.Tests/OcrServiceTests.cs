using Bing.Ocr;
using Xunit;

namespace Bing.Utils.Ocr.Tests;

public class OcrServiceTests
{
    private static readonly OcrPoint[] Quad = { new(1,2), new(11,2), new(11,5), new(1,5) };

    [Fact]
    public async Task Recognize_MapsCoordinatesPreservesTextAndMarksLowScore()
    {
        var provider = new FakeProvider { Action = (_,_,_) => Task.FromResult(new OcrProviderResult(new[] {
            new OcrLine(" 中文 A  ", Quad, .4), new OcrLine("B!", Quad) })) };
        var service = new OcrService(provider, new FakePreprocessor());
        using var stream = new MemoryStream(new byte[] { 9, 1, 2 }); stream.Position = 1;
        var result = await service.RecognizeAsync(stream);
        Assert.Equal(" 中文 A  \nB!", result.Text);
        Assert.Equal(OcrStatus.TextDetected, result.Status);
        Assert.Equal(100, result.ImageWidth); Assert.Equal(80, result.ImageHeight);
        Assert.Equal(12, result.Lines[0].Polygon[0].X); Assert.Equal(24, result.Lines[0].Polygon[0].Y);
        Assert.True(result.Lines[0].IsLowConfidence); Assert.Null(result.Lines[1].Confidence);
        Assert.True(stream.CanRead); Assert.Equal(3, stream.Position);
    }

    [Fact]
    public async Task Recognize_NonSeekableStreamAndEmptyResult_AreSupported()
    {
        var service = new OcrService(new FakeProvider(), new FakePreprocessor());
        using var input = new NonSeekStream(new byte[] { 1 });
        var result = await service.RecognizeAsync(input);
        Assert.Equal(OcrStatus.NoTextDetected, result.Status); Assert.Empty(result.Lines); Assert.Equal("", result.Text);
        Assert.True(input.CanRead);
    }

    [Fact]
    public async Task Limits_AreSnapshottedAndInputIsBounded()
    {
        var limits = new OcrServiceOptions { MaxInputBytes = 2 };
        var service = new OcrService(new FakeProvider(), new FakePreprocessor(), limits);
        limits.MaxInputBytes = 1;
        await service.RecognizeAsync(new MemoryStream(new byte[] { 1, 2 }));
        var error = await Assert.ThrowsAsync<OcrInputException>(() => service.RecognizeAsync(new MemoryStream(new byte[] { 1,2,3,4 })));
        Assert.Equal("input_too_large", error.Code);
        await Assert.ThrowsAsync<OcrInputException>(() => service.RecognizeAsync(new MemoryStream()));
    }

    [Fact]
    public async Task QueueFull_RejectsBeforeReading_AndQueuedCancellationReleasesCapacity()
    {
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = new FakeProvider { Action = async (_,_,ct) => { entered.TrySetResult(true); await release.Task; ct.ThrowIfCancellationRequested(); return new OcrProviderResult(Array.Empty<OcrLine>()); } };
        var service = new OcrService(provider, new FakePreprocessor(), new OcrServiceOptions { MaxQueuedRequests = 1 });
        var active = service.RecognizeAsync(new MemoryStream(new byte[] { 1 }));
        await entered.Task;
        using var queuedCancel = new CancellationTokenSource();
        var queued = service.RecognizeAsync(new ThrowOnReadStream(), cancellationToken: queuedCancel.Token);
        await Assert.ThrowsAsync<OcrBusyException>(() => service.RecognizeAsync(new ThrowOnReadStream()));
        queuedCancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => queued);
        var replacement = service.RecognizeAsync(new MemoryStream(new byte[] { 1 }));
        release.TrySetResult(true);
        await Task.WhenAll(active, replacement);
    }

    [Fact]
    public async Task TimeoutAndCallerCancellation_AreDistinct_AndSlotIsReusable()
    {
        var provider = new FakeProvider { Action = async (_,_,ct) => { await Task.Delay(Timeout.Infinite, ct); return new OcrProviderResult(Array.Empty<OcrLine>()); } };
        var service = new OcrService(provider, new FakePreprocessor(), new OcrServiceOptions { Timeout = TimeSpan.FromMilliseconds(40), MaxQueuedRequests = 0 });
        await Assert.ThrowsAsync<OcrTimeoutException>(() => service.RecognizeAsync(new MemoryStream(new byte[] { 1 })));
        using var cts = new CancellationTokenSource(); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.RecognizeAsync(new ThrowOnReadStream(), cancellationToken: cts.Token));
        provider.Action = null;
        await service.RecognizeAsync(new MemoryStream(new byte[] { 1 }));
    }

    [Fact]
    public async Task ProviderFailure_IsNotAnEmptySuccess()
    {
        var provider = new FakeProvider { Action = (_,_,_) => throw new OcrProviderException("failed") };
        var service = new OcrService(provider, new FakePreprocessor(), new OcrServiceOptions { MaxQueuedRequests = 0 });
        await Assert.ThrowsAsync<OcrProviderException>(() => service.RecognizeAsync(new MemoryStream(new byte[] { 1 })));
        provider.Action = null;
        Assert.Equal(OcrStatus.NoTextDetected, (await service.RecognizeAsync(new MemoryStream(new byte[] { 1 }))).Status);
    }

    [Fact]
    public async Task RequestOptions_AreIsolatedFromCaller()
    {
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var options = new OcrOptions { LanguageHint = "en" };
        var provider = new FakeProvider { Action = async (_, request, _) => {
            entered.SetResult(true); await release.Task; Assert.Equal("en", request.LanguageHint);
            return new OcrProviderResult(Array.Empty<OcrLine>()); } };
        var task = new OcrService(provider, new FakePreprocessor()).RecognizeAsync(new MemoryStream(new byte[] { 1 }), options);
        await entered.Task; options.LanguageHint = "changed"; release.SetResult(true); await task;
    }

    [Theory]
    [InlineData(45)] [InlineData(-90)] [InlineData(360)]
    public async Task InvalidRotation_IsRejectedBeforeInput(int rotation)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => new OcrService(new FakeProvider(), new FakePreprocessor())
            .RecognizeAsync(new ThrowOnReadStream(), new OcrOptions { RotationDegrees = rotation }));
    }

    [Fact]
    public void ResultCollectionsAndImageBytes_AreCopied()
    {
        var points = (OcrPoint[])Quad.Clone(); var line = new OcrLine("hello", points);
        points[0] = new OcrPoint(99, 99); Assert.Equal(1, line.Polygon[0].X);
        var lines = new[] { line }; var result = new OcrProviderResult(lines); lines[0] = null!;
        Assert.Same(line, result.Lines[0]);
        var data = new byte[] { 1, 2 }; var image = new PreparedOcrImage(data, 1,1,1,1,new OcrTransform());
        data[0] = 9; var copy = image.CopyBytes(); copy[0] = 8;
        Assert.Equal(1, image.CopyBytes()[0]);
        using var read = image.OpenRead(); Assert.False(read.CanWrite);
    }

    [Fact]
    public void Geometry_RejectsInvalidOrSelfIntersectingPolygons()
    {
        Assert.Throws<ArgumentException>(() => new OcrLine("x", new[] { Quad[0], Quad[2], Quad[1], Quad[3] }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new OcrPoint(double.NaN, 0));
        Assert.Throws<ArgumentException>(() => new OcrTransform(0,0,0,0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new OcrLine("x", Quad, double.PositiveInfinity));
    }

    [Theory]
    [InlineData(0.5, 20, 30)] [InlineData(2, -10, 6)]
    public void Geometry_InverseReturnsOriginalPoint(double scale, double x, double y)
    {
        var transform = new OcrTransform(offsetX:x, offsetY:y).Then(new OcrTransform(0,-1,1,0,100,0)).Then(new OcrTransform(scale,0,0,scale));
        var original = new OcrPoint(13.25, 29.5);
        var roundtrip = transform.Inverse().Apply(transform.Apply(original));
        Assert.Equal(original.X, roundtrip.X, 8); Assert.Equal(original.Y, roundtrip.Y, 8);
    }

    private sealed class FakeProvider : IOcrProvider
    {
        public string Name => "test";
        public Func<PreparedOcrImage,OcrOptions,CancellationToken,Task<OcrProviderResult>>? Action;
        public void ValidateOptions(OcrOptions options) { }
        public Task<OcrProviderResult> RecognizeAsync(PreparedOcrImage image, OcrOptions options, CancellationToken ct = default)
            => Action?.Invoke(image, options, ct) ?? Task.FromResult(new OcrProviderResult(Array.Empty<OcrLine>()));
    }
    private sealed class FakePreprocessor : IOcrImagePreprocessor
    {
        public PreparedOcrImage Prepare(byte[] bytes, OcrOptions options, OcrServiceOptions limits, CancellationToken token = default)
            => new(bytes, 40,20,100,80,new OcrTransform(2,0,0,2,10,20));
    }
    private sealed class NonSeekStream : MemoryStream
    {
        public NonSeekStream(byte[] bytes) : base(bytes) { }
        public override bool CanSeek => false;
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    }
    private sealed class ThrowOnReadStream : MemoryStream
    {
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token)
            => throw new InvalidOperationException("请求必须在读取前被阻止。");
    }
}
