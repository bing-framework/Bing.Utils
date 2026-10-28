using System.Text;
using Bing.Text.EncodingDetection;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace Bing.Utils.Text.EncodingDetection.Tests;

/// <summary>
/// 编码检测单元测试。
/// </summary>
public class TextEncodingDetectorTest
{
    /// <summary>
    /// 测试目的：UTF-8 BOM 应优先于启发式检测。
    /// </summary>
    [Fact]
    public void Detect_WithUtf8Bom_ShouldReportBomEvidence()
    {
        // Arrange
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes("中文")).ToArray();

        // Act
        var result = TextEncodingDetector.Detect(bytes);

        // Assert
        result.HasBom.ShouldBeTrue();
        result.Encoding.WebName.ShouldBe("utf-8");
    }

    /// <summary>
    /// 测试目的：不可定位流采样后，回放流仍应包含完整原始内容。
    /// </summary>
    [Fact]
    public void DetectWithReplay_WithNonSeekableStream_ShouldPreserveBytes()
    {
        // Arrange
        var bytes = Encoding.UTF8.GetBytes("abc中文");
        using var source = new NonSeekableStream(bytes);

        // Act
        var replay = TextEncodingDetector.DetectWithReplay(source,
            new EncodingDetectionOptions { MaxSampleBytes = 4, FallbackEncoding = Encoding.UTF8 });
        using var copy = new MemoryStream();
        replay.ReplayStream.CopyTo(copy);

        // Assert
        copy.ToArray().ShouldBe(bytes);
    }

    /// <summary>
    /// 测试目的：无 BOM UTF-8 转码默认不写入 BOM，且保留调用方流所有权。
    /// </summary>
    [Fact]
    public async Task TranscodeToUtf8Async_WithUtf8Source_ShouldWriteBomlessUtf8AndLeaveStreamsOpen()
    {
        // Arrange
        using var source = new MemoryStream(Encoding.UTF8.GetBytes("中文"));
        using var destination = new MemoryStream();

        // Act
        await TextEncodingDetector.TranscodeToUtf8Async(source, destination,
            new EncodingDetectionOptions { FallbackEncoding = Encoding.UTF8 });

        // Assert
        destination.ToArray().ShouldBe(Encoding.UTF8.GetBytes("中文"));
        source.CanRead.ShouldBeTrue();
        destination.CanWrite.ShouldBeTrue();
    }

    /// <summary>
    /// 测试目的：UTF-32 长 BOM 应先于 UTF-16 前缀匹配并给出确定证据。
    /// </summary>
    [Theory]
    [InlineData(false, 12000)]
    [InlineData(true, 12001)]
    public void Detect_WithUtf32Bom_ShouldPreferLongBom(bool bigEndian, int expectedCodePage)
    {
        var encoding = new UTF32Encoding(bigEndian, true, true);
        var bytes = encoding.GetPreamble().Concat(encoding.GetBytes("A中")).ToArray();

        var result = TextEncodingDetector.Detect(bytes);

        result.Encoding.CodePage.ShouldBe(expectedCodePage);
        result.HasBom.ShouldBeTrue();
        result.BomLength.ShouldBe(4);
        result.Confidence.ShouldBe(1);
        result.SelectedSource.ShouldBe(EncodingCandidateSource.Bom);
    }

    /// <summary>
    /// 测试目的：文件异步入口应报告有界采样、EOF 状态和完整候选来源。
    /// </summary>
    [Fact]
    public async Task DetectFileAsync_ShouldReportBoundedSampleAndCandidateEvidence()
    {
        var text = "这是用于编码检测的中文内容，候选结果需要保留编码、来源和置信度。";
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(text)).ToArray();
        var path = Path.Combine(Path.GetTempPath(), $"bing-encoding-{Guid.NewGuid():N}.txt");
        await File.WriteAllBytesAsync(path, bytes);
        try
        {
            var fileResult = await TextEncodingDetector.DetectFileAsync(path,
                new EncodingDetectionOptions { MaxSampleBytes = 4 });
            var candidateResult = TextEncodingDetector.Detect(Encoding.UTF8.GetBytes(text),
                new EncodingDetectionOptions { MinimumConfidence = 0, FallbackEncoding = Encoding.UTF8 });

            fileResult.HasBom.ShouldBeTrue();
            fileResult.BomLength.ShouldBe(3);
            fileResult.SampledBytes.ShouldBe(4);
            fileResult.SampleReachedEndOfStream.ShouldBeFalse();
            candidateResult.Candidates.ShouldNotBeEmpty();
            candidateResult.Candidates.ShouldContain(item => item.Source == EncodingCandidateSource.Detector);
            candidateResult.Candidates.ShouldAllBe(item => item.Confidence >= 0 && item.Confidence <= 1 && item.Evidence.Length > 0);
            candidateResult.Evidence.ShouldNotBeEmpty();
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// 测试目的：ASCII 兼容输入默认报告歧义，调用方可显式指定回退编码。
    /// </summary>
    [Fact]
    public void Detect_WithAsciiOnly_ShouldRequireExplicitFallback()
    {
        var bytes = Encoding.ASCII.GetBytes("header\tvalue\r\n");

        var error = Should.Throw<EncodingDetectionException>(() => TextEncodingDetector.Detect(bytes));
        var fallback = TextEncodingDetector.Detect(bytes, new EncodingDetectionOptions { FallbackEncoding = Encoding.UTF8 });

        error.ErrorCode.ShouldBe("encoding.low_confidence");
        fallback.IsAmbiguous.ShouldBeTrue();
        fallback.SelectedSource.ShouldBe(EncodingCandidateSource.Fallback);
        fallback.Encoding.WebName.ShouldBe("utf-8");
    }

    /// <summary>
    /// 测试目的：采样截断 UTF-8 多字节尾部时忽略不完整尾部判定，并完整回放全部字节。
    /// </summary>
    [Fact]
    public void DetectWithReplay_WithTruncatedUtf8Sample_ShouldKeepCandidateAndReplayProbe()
    {
        var bytes = Encoding.UTF8.GetBytes("A中文");
        using var source = new NonSeekableStream(bytes);

        var replay = TextEncodingDetector.DetectWithReplay(source,
            new EncodingDetectionOptions { MaxSampleBytes = 5 });
        using var copy = new MemoryStream();
        replay.ReplayStream.CopyTo(copy);

        replay.Detection.Encoding.WebName.ShouldBe("utf-8");
        replay.Detection.SampledBytes.ShouldBe(5);
        replay.Detection.SampleReachedEndOfStream.ShouldBeFalse();
        replay.Detection.Evidence.Any(item => item.Contains("不完整 UTF-8")).ShouldBeTrue();
        copy.ToArray().ShouldBe(bytes);
    }

    /// <summary>
    /// 测试目的：显式编码与 BOM 不一致时默认拒绝，选择 BOM 策略则报告最终编码。
    /// </summary>
    [Fact]
    public void Detect_WithConflictingExplicitEncoding_ShouldFollowConflictPolicy()
    {
        var bytes = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes("文本")).ToArray();

        var error = Should.Throw<EncodingDetectionException>(() => TextEncodingDetector.Detect(bytes,
            new EncodingDetectionOptions { ExplicitEncoding = Encoding.UTF8 }));
        var result = TextEncodingDetector.Detect(bytes, new EncodingDetectionOptions
        {
            ExplicitEncoding = Encoding.UTF8,
            BomConflictBehavior = BomConflictBehavior.PreferBom
        });

        error.ErrorCode.ShouldBe("encoding.bom_conflict");
        result.Encoding.WebName.ShouldBe("utf-16");
        result.HasBom.ShouldBeTrue();
    }

    /// <summary>
    /// 测试目的：增量解码跨短读块处理字符，并按配置严格失败或替换无效序列。
    /// </summary>
    [Fact]
    public async Task TranscodeToUtf8Async_ShouldHandleChunkBoundariesAndInvalidSequences()
    {
        var text = new string('a', 8191) + "😀尾";
        var sourceBytes = Encoding.UTF8.GetBytes(text);
        using var chunked = new ChunkedStream(sourceBytes, 2048);
        using var output = new MemoryStream();
        await TextEncodingDetector.TranscodeToUtf8Async(chunked, output,
            new EncodingDetectionOptions { ExplicitEncoding = Encoding.UTF8 });
        Encoding.UTF8.GetString(output.ToArray()).ShouldBe(text);

        using var invalid = new MemoryStream(new byte[] { 0xc3, 0x28 });
        using var strictOutput = new MemoryStream();
        var error = await Should.ThrowAsync<DecoderFallbackException>(() => TextEncodingDetector.TranscodeToUtf8Async(
            invalid, strictOutput, new EncodingDetectionOptions { ExplicitEncoding = Encoding.UTF8 }));
        error.ShouldNotBeNull();

        using var replaceInput = new MemoryStream(new byte[] { 0xc3, 0x28 });
        using var replaceOutput = new MemoryStream();
        await TextEncodingDetector.TranscodeToUtf8Async(replaceInput, replaceOutput,
            new EncodingDetectionOptions { ExplicitEncoding = Encoding.UTF8, ReplaceInvalidSequences = true });
        Encoding.UTF8.GetString(replaceOutput.ToArray()).ShouldBe("\uFFFD(");
    }

    /// <summary>
    /// 测试目的：取消必须传入实际阻塞的底层读取和写入，并在 leaveOpen=false 时释放两端。
    /// </summary>
    [Fact]
    public async Task TranscodeToUtf8Async_WhenCanceledDuringIo_ShouldPassTokenAndDisposeOwnedStreams()
    {
        using var readCancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(80));
        var blockedSource = new BlockingStream(blockRead: true, blockWrite: false);
        var output = new BlockingStream(blockRead: false, blockWrite: false);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => TextEncodingDetector.TranscodeToUtf8Async(
            blockedSource, output, leaveOpen: false, cancellationToken: readCancellation.Token));

        blockedSource.ReadTokenObserved.ShouldBeTrue();
        blockedSource.WasDisposed.ShouldBeTrue();
        output.WasDisposed.ShouldBeTrue();

        using var writeCancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(80));
        var input = new ChunkedStream(Encoding.UTF8.GetBytes("中文"), 8192);
        var blockedOutput = new BlockingStream(blockRead: false, blockWrite: true);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => TextEncodingDetector.TranscodeToUtf8Async(
            input, blockedOutput, new EncodingDetectionOptions { ExplicitEncoding = Encoding.UTF8 },
            leaveOpen: false, cancellationToken: writeCancellation.Token));

        blockedOutput.WriteTokenObserved.ShouldBeTrue();
        input.WasDisposed.ShouldBeTrue();
        blockedOutput.WasDisposed.ShouldBeTrue();
    }

    /// <summary>
    /// 测试目的：采样上限严格有界，并区分恰好到 EOF 与仍有后续输入。
    /// </summary>
    [Fact]
    public void Detect_WithExactSampleLimit_ShouldReportEndOfStreamAccurately()
    {
        var exact = TextEncodingDetector.Detect(Encoding.UTF8.GetBytes("A中"),
            new EncodingDetectionOptions { MaxSampleBytes = 4, FallbackEncoding = Encoding.UTF8 });
        var truncated = TextEncodingDetector.Detect(Encoding.UTF8.GetBytes("A中文"),
            new EncodingDetectionOptions { MaxSampleBytes = 4, FallbackEncoding = Encoding.UTF8 });

        exact.SampledBytes.ShouldBe(4);
        exact.SampleReachedEndOfStream.ShouldBeTrue();
        truncated.SampledBytes.ShouldBe(4);
        truncated.SampleReachedEndOfStream.ShouldBeFalse();
    }

    /// <summary>
    /// 测试目的：不可定位流在采样边界应通过额外探测准确报告 EOF，探测不计入检测样本。
    /// </summary>
    [Fact]
    public async Task DetectAsync_WithNonSeekableStream_ShouldReportExactEofAtSampleLimit()
    {
        using var exactSource = new NonSeekableStream(Encoding.ASCII.GetBytes("ABCD"));
        using var truncatedSource = new NonSeekableStream(Encoding.ASCII.GetBytes("ABCDE"));

        var exact = await TextEncodingDetector.DetectAsync(exactSource,
            new EncodingDetectionOptions { MaxSampleBytes = 4, FallbackEncoding = Encoding.UTF8 });
        var truncated = await TextEncodingDetector.DetectAsync(truncatedSource,
            new EncodingDetectionOptions { MaxSampleBytes = 4, FallbackEncoding = Encoding.UTF8 });

        exact.SampledBytes.ShouldBe(4);
        exact.SampleReachedEndOfStream.ShouldBeTrue();
        truncated.SampledBytes.ShouldBe(4);
        truncated.SampleReachedEndOfStream.ShouldBeFalse();
    }

    /// <summary>
    /// 测试目的：4 字节最小预算应在所有入口识别 UTF-32 BOM，并完整回放及正确转码。
    /// </summary>
    [Theory]
    [InlineData(false, 12000)]
    [InlineData(true, 12001)]
    public async Task Detect_WithMinimumSampleBudget_ShouldRecognizeUtf32AcrossInputs(bool bigEndian, int expectedCodePage)
    {
        var encoding = new UTF32Encoding(bigEndian, true, true);
        var bytes = encoding.GetPreamble().Concat(encoding.GetBytes("A")).ToArray();
        var options = new EncodingDetectionOptions { MaxSampleBytes = 4 };
        var path = Path.Combine(Path.GetTempPath(), $"bing-utf32-{Guid.NewGuid():N}.txt");
        await File.WriteAllBytesAsync(path, bytes);
        try
        {
            AssertUtf32Detection(TextEncodingDetector.Detect(bytes, options), expectedCodePage);
            AssertUtf32Detection(TextEncodingDetector.DetectFile(path, 4), expectedCodePage);
            AssertUtf32Detection(await TextEncodingDetector.DetectFileAsync(path, options), expectedCodePage);

            using (var seekable = new MemoryStream(bytes))
            {
                AssertUtf32Detection(TextEncodingDetector.Detect(seekable, 4), expectedCodePage);
                seekable.Position.ShouldBe(0);
            }
            using (var seekable = new MemoryStream(bytes))
            {
                AssertUtf32Detection(await TextEncodingDetector.DetectAsync(seekable, options), expectedCodePage);
                seekable.Position.ShouldBe(0);
            }
            using (var nonSeekable = new NonSeekableStream(bytes))
                AssertUtf32Detection(TextEncodingDetector.Detect(nonSeekable, options), expectedCodePage);
            using (var nonSeekable = new NonSeekableStream(bytes))
                AssertUtf32Detection(await TextEncodingDetector.DetectAsync(nonSeekable, options), expectedCodePage);

            using (var syncSource = new NonSeekableStream(bytes))
            {
                var syncReplay = TextEncodingDetector.DetectWithReplay(syncSource, 4);
                AssertUtf32Detection(syncReplay.Detection, expectedCodePage);
                using (syncReplay.ReplayStream)
                using (var replayed = new MemoryStream())
                {
                    syncReplay.ReplayStream.CopyTo(replayed);
                    replayed.ToArray().ShouldBe(bytes);
                }
            }

            using (var asyncSource = new NonSeekableStream(bytes))
            {
                var asyncReplay = await TextEncodingDetector.DetectWithReplayAsync(asyncSource, options);
                AssertUtf32Detection(asyncReplay.Detection, expectedCodePage);
                using (asyncReplay.ReplayStream)
                using (var replayed = new MemoryStream())
                {
                    await asyncReplay.ReplayStream.CopyToAsync(replayed);
                    replayed.ToArray().ShouldBe(bytes);
                }
            }

            using var transcodeSource = new NonSeekableStream(bytes);
            using var output = new MemoryStream();
            await TextEncodingDetector.TranscodeToUtf8Async(transcodeSource, output, options);
            output.ToArray().ShouldBe(Encoding.UTF8.GetBytes("A"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// 测试目的：小于 BOM 最长长度的预算必须在字节、文件、流、回放和转码入口读入前拒绝。
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Detect_WithInsufficientSampleBudget_ShouldRejectBeforeReading(int sampleBytes)
    {
        var bytes = new byte[] { 0xff, 0xfe, 0x00, 0x00, 0x41, 0x00, 0x00, 0x00 };
        var options = new EncodingDetectionOptions { MaxSampleBytes = sampleBytes };
        var path = Path.Combine(Path.GetTempPath(), $"bing-short-budget-{Guid.NewGuid():N}.txt");
        await File.WriteAllBytesAsync(path, bytes);
        try
        {
            Should.Throw<ArgumentOutOfRangeException>(() => TextEncodingDetector.Detect(bytes, options));
            Should.Throw<ArgumentOutOfRangeException>(() => TextEncodingDetector.DetectFile(path, sampleBytes));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => TextEncodingDetector.DetectFileAsync(path, options));

            using (var seekable = new MemoryStream(bytes))
            {
                Should.Throw<ArgumentOutOfRangeException>(() => TextEncodingDetector.Detect(seekable, sampleBytes));
                seekable.Position.ShouldBe(0);
            }
            using (var seekable = new MemoryStream(bytes))
            {
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => TextEncodingDetector.DetectAsync(seekable, options));
                seekable.Position.ShouldBe(0);
            }

            using (var nonSeekable = new NonSeekableStream(bytes))
            {
                Should.Throw<ArgumentOutOfRangeException>(() => TextEncodingDetector.Detect(nonSeekable, options));
                nonSeekable.ReadCalls.ShouldBe(0);
            }
            using (var nonSeekable = new NonSeekableStream(bytes))
            {
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => TextEncodingDetector.DetectAsync(nonSeekable, options));
                nonSeekable.ReadCalls.ShouldBe(0);
            }
            using (var nonSeekable = new NonSeekableStream(bytes))
            {
                Should.Throw<ArgumentOutOfRangeException>(() => TextEncodingDetector.DetectWithReplay(nonSeekable, sampleBytes));
                nonSeekable.ReadCalls.ShouldBe(0);
            }
            using (var nonSeekable = new NonSeekableStream(bytes))
            {
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => TextEncodingDetector.DetectWithReplayAsync(nonSeekable, options));
                nonSeekable.ReadCalls.ShouldBe(0);
            }
            using (var nonSeekable = new NonSeekableStream(bytes))
            using (var output = new MemoryStream())
            {
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => TextEncodingDetector.TranscodeToUtf8Async(nonSeekable, output, options));
                nonSeekable.ReadCalls.ShouldBe(0);
                output.Length.ShouldBe(0);
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static void AssertUtf32Detection(EncodingDetectionResult result, int expectedCodePage)
    {
        result.Encoding.CodePage.ShouldBe(expectedCodePage);
        result.HasBom.ShouldBeTrue();
        result.BomLength.ShouldBe(4);
        result.Confidence.ShouldBe(1);
        result.SelectedSource.ShouldBe(EncodingCandidateSource.Bom);
    }

    private sealed class NonSeekableStream : MemoryStream
    {
        public NonSeekableStream(byte[] bytes) : base(bytes) { }
        public int ReadCalls { get; private set; }
        public override bool CanSeek => false;
        public override int Read(byte[] buffer, int offset, int count)
        {
            ReadCalls++;
            return base.Read(buffer, offset, count);
        }
        public override int ReadByte()
        {
            ReadCalls++;
            return base.ReadByte();
        }
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ReadCalls++;
            return base.ReadAsync(buffer, offset, count, cancellationToken);
        }
        public override long Seek(long offset, SeekOrigin loc) => throw new NotSupportedException();
    }

    private sealed class ChunkedStream : MemoryStream
    {
        private readonly int _chunkSize;
        public ChunkedStream(byte[] bytes, int chunkSize) : base(bytes) => _chunkSize = chunkSize;
        public bool WasDisposed { get; private set; }
        public override bool CanSeek => false;
        public override long Seek(long offset, SeekOrigin loc) => throw new NotSupportedException();
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => base.ReadAsync(buffer, offset, Math.Min(count, _chunkSize), cancellationToken);
        protected override void Dispose(bool disposing)
        {
            if (disposing) WasDisposed = true;
            base.Dispose(disposing);
        }
    }

    private sealed class BlockingStream : Stream
    {
        private readonly bool _blockRead;
        private readonly bool _blockWrite;
        public BlockingStream(bool blockRead, bool blockWrite)
        {
            _blockRead = blockRead;
            _blockWrite = blockWrite;
        }
        public bool ReadTokenObserved { get; private set; }
        public bool WriteTokenObserved { get; private set; }
        public bool WasDisposed { get; private set; }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ReadTokenObserved = cancellationToken.CanBeCanceled;
            return _blockRead ? WaitForCancellationAsync(cancellationToken) : Task.FromResult(0);
        }
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            WriteTokenObserved = cancellationToken.CanBeCanceled;
            return _blockWrite ? WaitForCancellationAsync(cancellationToken) : Task.CompletedTask;
        }
        private static async Task<int> WaitForCancellationAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return 0;
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) WasDisposed = true;
            base.Dispose(disposing);
        }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
