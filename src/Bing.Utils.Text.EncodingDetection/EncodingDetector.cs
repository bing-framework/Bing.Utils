using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UtfUnknown;

namespace Bing.Text.EncodingDetection;

/// <summary>处理显式编码与检测到的 BOM 不一致时的策略。</summary>
public enum BomConflictBehavior
{
    /// <summary>抛出冲突异常。</summary>
    Throw,
    /// <summary>以 BOM 指示的编码为准。</summary>
    PreferBom
}

/// <summary>描述检测候选的来源。</summary>
public enum EncodingCandidateSource
{
    /// <summary>字节序标记。</summary>
    Bom,
    /// <summary>UTF.Unknown 启发式检测。</summary>
    Detector,
    /// <summary>调用方显式指定。</summary>
    Explicit,
    /// <summary>调用方低置信度回退。</summary>
    Fallback
}

/// <summary>配置采样预算、置信度和编码选择策略。</summary>
public sealed class EncodingDetectionOptions
{
    /// <summary>获取或设置最大检测采样字节数；必须至少为 4 才能完整识别最长 BOM。</summary>
    public int MaxSampleBytes { get; set; } = 64 * 1024;
    /// <summary>获取或设置接受启发式候选的最低置信度，范围为 0 到 1。</summary>
    public double MinimumConfidence { get; set; } = 0.8;
    /// <summary>获取或设置无 BOM 时优先使用的显式编码。</summary>
    public Encoding ExplicitEncoding { get; set; }
    /// <summary>获取或设置候选置信度过低或内容仅为 ASCII 时使用的回退编码。</summary>
    public Encoding FallbackEncoding { get; set; }
    /// <summary>获取或设置显式编码与 BOM 冲突时的行为。</summary>
    public BomConflictBehavior BomConflictBehavior { get; set; } = BomConflictBehavior.Throw;
    /// <summary>获取或设置严格解码失败时是否以替换字符继续。</summary>
    public bool ReplaceInvalidSequences { get; set; }

    internal EncodingDetectionOptions Snapshot()
    {
        if (MaxSampleBytes < 4) throw new ArgumentOutOfRangeException(nameof(MaxSampleBytes), "采样上限必须至少为 4 字节，以完整识别 BOM。");
        if (double.IsNaN(MinimumConfidence) || MinimumConfidence < 0 || MinimumConfidence > 1)
            throw new ArgumentOutOfRangeException(nameof(MinimumConfidence), "最低置信度必须在 0 到 1 之间。");
        if (!Enum.IsDefined(typeof(BomConflictBehavior), BomConflictBehavior))
            throw new ArgumentOutOfRangeException(nameof(BomConflictBehavior));
        return new EncodingDetectionOptions
        {
            MaxSampleBytes = MaxSampleBytes,
            MinimumConfidence = MinimumConfidence,
            ExplicitEncoding = ExplicitEncoding == null ? null : (Encoding)ExplicitEncoding.Clone(),
            FallbackEncoding = FallbackEncoding == null ? null : (Encoding)FallbackEncoding.Clone(),
            BomConflictBehavior = BomConflictBehavior,
            ReplaceInvalidSequences = ReplaceInvalidSequences
        };
    }
}

/// <summary>表示一个编码检测候选和其证据。</summary>
public sealed class EncodingDetectionCandidate
{
    internal EncodingDetectionCandidate(Encoding encoding, double confidence, EncodingCandidateSource source, string evidence)
    {
        Encoding = encoding;
        Confidence = confidence;
        Source = source;
        Evidence = evidence;
    }

    /// <summary>获取候选编码。</summary>
    public Encoding Encoding { get; }
    /// <summary>获取 0 到 1 之间的候选置信度。</summary>
    public double Confidence { get; }
    /// <summary>获取候选来源。</summary>
    public EncodingCandidateSource Source { get; }
    /// <summary>获取候选证据说明。</summary>
    public string Evidence { get; }
}

/// <summary>返回编码选择、候选、采样范围和检测证据。</summary>
public sealed class EncodingDetectionResult
{
    internal EncodingDetectionResult(EncodingDetectionCandidate selected, IEnumerable<EncodingDetectionCandidate> candidates,
        IEnumerable<string> evidence, int sampledBytes, bool reachedEndOfStream, int bomLength, bool ambiguous)
    {
        Selected = selected;
        Candidates = new ReadOnlyCollection<EncodingDetectionCandidate>(candidates.ToArray());
        Evidence = new ReadOnlyCollection<string>(evidence.ToArray());
        SampledBytes = sampledBytes;
        SampleReachedEndOfStream = reachedEndOfStream;
        BomLength = bomLength;
        IsAmbiguous = ambiguous;
    }

    /// <summary>获取选中的编码。</summary>
    public Encoding Encoding => Selected.Encoding;
    /// <summary>获取选中编码的名称。</summary>
    public string EncodingName => Encoding.WebName;
    /// <summary>获取选中候选的置信度。</summary>
    public double Confidence => Selected.Confidence;
    /// <summary>获取选中候选来源。</summary>
    public EncodingCandidateSource SelectedSource => Selected.Source;
    /// <summary>获取是否检测到 BOM。</summary>
    public bool HasBom => BomLength > 0;
    /// <summary>获取 BOM 的字节数；未检测到时为零。</summary>
    public int BomLength { get; }
    /// <summary>获取送入编码检测的采样字节数，不包含用于 EOF 探测的额外字节。</summary>
    public int SampledBytes { get; }
    /// <summary>获取采样是否到达流末尾。</summary>
    public bool SampleReachedEndOfStream { get; }
    /// <summary>获取采样是否因达到上限而截断。</summary>
    public bool IsSampleTruncated => !SampleReachedEndOfStream;
    /// <summary>获取 ASCII 兼容或其他证据不足的歧义标志。</summary>
    public bool IsAmbiguous { get; }
    /// <summary>获取所有检测候选。</summary>
    public IReadOnlyList<EncodingDetectionCandidate> Candidates { get; }
    /// <summary>获取采样和选择证据。</summary>
    public IReadOnlyList<string> Evidence { get; }
    /// <summary>获取选中的候选。</summary>
    public EncodingDetectionCandidate Selected { get; }
}

/// <summary>编码检测失败，包含稳定错误码和可读诊断。</summary>
public sealed class EncodingDetectionException : Exception
{
    internal EncodingDetectionException(string errorCode, string diagnostic, Exception innerException = null)
        : base(diagnostic, innerException)
    {
        ErrorCode = errorCode;
        Diagnostic = diagnostic;
    }

    /// <summary>获取稳定错误码。</summary>
    public string ErrorCode { get; }
    /// <summary>获取中文诊断。</summary>
    public string Diagnostic { get; }
    /// <summary>获取检测失败时仍可用于恢复输入的完整回放流；普通检测和转码失败时为 null。</summary>
    public Stream ReplayStream { get; internal set; }
}

/// <summary>编码检测与增量 UTF-8 转码工具。</summary>
public static class TextEncodingDetector
{
    private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);

    /// <summary>检测字节数组中的文本编码。</summary>
    /// <param name="bytes">输入字节。</param>
    /// <returns>编码、候选及检测证据。</returns>
    public static EncodingDetectionResult Detect(byte[] bytes) => Detect(bytes, null);

    /// <summary>按指定策略检测字节数组中的文本编码。</summary>
    /// <param name="bytes">输入字节。</param>
    /// <param name="options">采样和编码选择配置。</param>
    /// <returns>编码、候选及检测证据。</returns>
    public static EncodingDetectionResult Detect(byte[] bytes, EncodingDetectionOptions options)
    {
        if (bytes == null) throw new ArgumentNullException(nameof(bytes));
        var config = (options ?? new EncodingDetectionOptions()).Snapshot();
        var count = Math.Min(bytes.Length, config.MaxSampleBytes);
        var sample = new byte[count];
        Array.Copy(bytes, sample, count);
        return DetectSample(sample, count, bytes.Length <= count, config);
    }

    /// <summary>检测文件开头的有限字节样本。</summary>
    /// <param name="path">文件路径。</param>
    /// <param name="sampleBytes">最多采样的字节数，必须至少为 4。</param>
    /// <returns>编码、候选及检测证据。</returns>
    public static EncodingDetectionResult DetectFile(string path, int sampleBytes = 64 * 1024)
        => DetectFile(path, new EncodingDetectionOptions { MaxSampleBytes = sampleBytes });

    /// <summary>按策略检测文件开头的有限字节样本。</summary>
    /// <param name="path">文件路径。</param>
    /// <param name="options">采样和编码选择配置。</param>
    /// <returns>编码、候选及检测证据。</returns>
    public static EncodingDetectionResult DetectFile(string path, EncodingDetectionOptions options)
    {
        if (path == null) throw new ArgumentNullException(nameof(path));
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Detect(stream, options);
    }

    /// <summary>检测流开头的有限字节样本；可定位流会恢复原位置。</summary>
    /// <param name="stream">输入流。</param>
    /// <param name="sampleBytes">最多采样的字节数，必须至少为 4。</param>
    /// <returns>编码、候选及检测证据。</returns>
    public static EncodingDetectionResult Detect(Stream stream, int sampleBytes = 64 * 1024)
        => Detect(stream, new EncodingDetectionOptions { MaxSampleBytes = sampleBytes });

    /// <summary>按策略检测流开头的有限字节样本；可定位流会恢复原位置。</summary>
    /// <param name="stream">输入流。</param>
    /// <param name="options">采样和编码选择配置。</param>
    /// <returns>编码、候选及检测证据。</returns>
    public static EncodingDetectionResult Detect(Stream stream, EncodingDetectionOptions options)
    {
        ValidateReadable(stream);
        var config = (options ?? new EncodingDetectionOptions()).Snapshot();
        var position = stream.CanSeek ? stream.Position : (long?)null;
        try
        {
            var sample = ReadSample(stream, config.MaxSampleBytes, out var eof, out _);
            return DetectSample(sample, sample.Length, eof, config);
        }
        finally
        {
            if (position.HasValue) stream.Position = position.Value;
        }
    }

    /// <summary>异步检测流开头的有限字节样本并向底层读取传递取消令牌。</summary>
    /// <param name="stream">输入流。</param>
    /// <param name="options">采样和编码选择配置。</param>
    /// <param name="cancellationToken">底层采样读取的取消令牌。</param>
    /// <returns>编码、候选及检测证据。</returns>
    public static async Task<EncodingDetectionResult> DetectAsync(Stream stream,
        EncodingDetectionOptions options = null, CancellationToken cancellationToken = default)
    {
        ValidateReadable(stream);
        var config = (options ?? new EncodingDetectionOptions()).Snapshot();
        cancellationToken.ThrowIfCancellationRequested();
        var position = stream.CanSeek ? stream.Position : (long?)null;
        try
        {
            var sampled = await ReadSampleAsync(stream, config.MaxSampleBytes, cancellationToken).ConfigureAwait(false);
            return DetectSample(sampled.Sample, sampled.Sample.Length, sampled.EndOfStream, config);
        }
        finally
        {
            if (position.HasValue) stream.Position = position.Value;
        }
    }

    /// <summary>异步检测文件开头的有限字节样本。</summary>
    /// <param name="path">文件路径。</param>
    /// <param name="options">采样和编码选择配置。</param>
    /// <param name="cancellationToken">底层文件读取的取消令牌。</param>
    /// <returns>编码、候选及检测证据。</returns>
    public static async Task<EncodingDetectionResult> DetectFileAsync(string path,
        EncodingDetectionOptions options = null, CancellationToken cancellationToken = default)
    {
        if (path == null) throw new ArgumentNullException(nameof(path));
        cancellationToken.ThrowIfCancellationRequested();
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
        return await DetectAsync(stream, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>检测流并返回可从当前逻辑位置完整回放的流。</summary>
    /// <param name="stream">输入流；回放流销毁时不会销毁此流。</param>
    /// <param name="sampleBytes">最多采样的字节数，必须至少为 4。</param>
    /// <returns>检测结果及包含完整原始内容的回放流。</returns>
    public static EncodingReplayResult DetectWithReplay(Stream stream, int sampleBytes = 64 * 1024)
        => DetectWithReplay(stream, new EncodingDetectionOptions { MaxSampleBytes = sampleBytes });

    /// <summary>按策略检测流并返回可从当前逻辑位置完整回放的流。</summary>
    /// <param name="stream">输入流；回放流销毁时不会销毁此流。</param>
    /// <param name="options">采样和编码选择配置。</param>
    /// <returns>检测结果及包含完整原始内容的回放流。</returns>
    public static EncodingReplayResult DetectWithReplay(Stream stream, EncodingDetectionOptions options)
    {
        ValidateReadable(stream);
        var config = (options ?? new EncodingDetectionOptions()).Snapshot();
        var sample = ReadSample(stream, config.MaxSampleBytes, out var eof, out var probe);
        var prefix = probe == null ? sample : AppendByte(sample, probe.Value);
        try
        {
            var detection = DetectSample(sample, sample.Length, eof, config);
            return new EncodingReplayResult(detection, new ReplayableStream(prefix, stream));
        }
        catch (EncodingDetectionException error)
        {
            error.ReplayStream = new ReplayableStream(prefix, stream);
            throw;
        }
    }

    /// <summary>异步检测流并返回可从当前逻辑位置完整回放的流。</summary>
    /// <param name="stream">输入流；回放流销毁时不会销毁此流。</param>
    /// <param name="options">采样和编码选择配置。</param>
    /// <param name="cancellationToken">底层采样读取的取消令牌。</param>
    /// <returns>检测结果及包含完整原始内容的回放流。</returns>
    public static async Task<EncodingReplayResult> DetectWithReplayAsync(Stream stream,
        EncodingDetectionOptions options = null, CancellationToken cancellationToken = default)
    {
        ValidateReadable(stream);
        var config = (options ?? new EncodingDetectionOptions()).Snapshot();
        cancellationToken.ThrowIfCancellationRequested();
        var sampled = await ReadSampleAsync(stream, config.MaxSampleBytes, cancellationToken).ConfigureAwait(false);
        var prefix = sampled.Probe.HasValue ? AppendByte(sampled.Sample, sampled.Probe.Value) : sampled.Sample;
        try
        {
            var detection = DetectSample(sampled.Sample, sampled.Sample.Length, sampled.EndOfStream, config);
            return new EncodingReplayResult(detection, new ReplayableStream(prefix, stream));
        }
        catch (EncodingDetectionException error)
        {
            error.ReplayStream = new ReplayableStream(prefix, stream);
            throw;
        }
    }

    /// <summary>将输入流严格增量转码为无 BOM UTF-8，并保留两个流。</summary>
    /// <param name="source">待检测和转码的输入流。</param>
    /// <param name="destination">UTF-8 输出流。</param>
    /// <param name="leaveOpen">是否保留输入和输出流。</param>
    /// <param name="cancellationToken">采样、底层读取和写入的取消令牌。</param>
    public static Task TranscodeToUtf8Async(Stream source, Stream destination, bool leaveOpen = true,
        CancellationToken cancellationToken = default)
        => TranscodeToUtf8Async(source, destination, null, leaveOpen, cancellationToken);

    /// <summary>按指定编码策略将输入流增量转码为无 BOM UTF-8。</summary>
    /// <param name="source">待检测和转码的输入流。</param>
    /// <param name="destination">UTF-8 输出流。</param>
    /// <param name="options">检测、回退和严格解码配置。</param>
    /// <param name="leaveOpen">是否保留输入和输出流。</param>
    /// <param name="cancellationToken">采样、底层读取和写入的取消令牌。</param>
    public static async Task TranscodeToUtf8Async(Stream source, Stream destination,
        EncodingDetectionOptions options, bool leaveOpen = true, CancellationToken cancellationToken = default)
    {
        ValidateReadable(source);
        if (destination == null) throw new ArgumentNullException(nameof(destination));
        if (!destination.CanWrite) throw new ArgumentException("目标流不可写。", nameof(destination));
        if (ReferenceEquals(source, destination)) throw new ArgumentException("源流和目标流不能相同。", nameof(destination));
        var config = (options ?? new EncodingDetectionOptions()).Snapshot();
        cancellationToken.ThrowIfCancellationRequested();

        Exception operationError = null;
        ReplayableStream replay = null;
        try
        {
            var replayResult = await DetectWithReplayAsync(source, config, cancellationToken).ConfigureAwait(false);
            replay = (ReplayableStream)replayResult.ReplayStream;
            var inputEncoding = (Encoding)replayResult.Detection.Encoding.Clone();
            inputEncoding.DecoderFallback = config.ReplaceInvalidSequences
                ? new DecoderReplacementFallback("\uFFFD")
                : DecoderFallback.ExceptionFallback;
            var decoder = inputEncoding.GetDecoder();
            var encoder = StrictUtf8.GetEncoder();
            await DiscardAsync(replay, replayResult.Detection.BomLength, cancellationToken).ConfigureAwait(false);

            var byteBuffer = new byte[8192];
            var charBuffer = new char[Math.Max(8, inputEncoding.GetMaxCharCount(byteBuffer.Length))];
            var utf8Buffer = new byte[StrictUtf8.GetMaxByteCount(charBuffer.Length)];
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var read = await replay.ReadAsync(byteBuffer, 0, byteBuffer.Length, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                if (read == 0)
                {
                    await ConvertDecodedAsync(decoder, Array.Empty<byte>(), 0, 0, true, charBuffer,
                        encoder, utf8Buffer, destination, cancellationToken).ConfigureAwait(false);
                    await ConvertEncodedAsync(encoder, Array.Empty<char>(), 0, 0, true, utf8Buffer, destination, cancellationToken).ConfigureAwait(false);
                    break;
                }

                await ConvertDecodedAsync(decoder, byteBuffer, 0, read, false, charBuffer,
                    encoder, utf8Buffer, destination, cancellationToken).ConfigureAwait(false);
            }
            await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception error)
        {
            operationError = error;
            throw;
        }
        finally
        {
            var cleanupErrors = new List<Exception>();
            try { replay?.Dispose(); }
            catch (Exception cleanup) { cleanupErrors.Add(cleanup); }
            if (!leaveOpen)
            {
                try { destination.Dispose(); }
                catch (Exception cleanup) { cleanupErrors.Add(cleanup); }
                try { source.Dispose(); }
                catch (Exception cleanup) { cleanupErrors.Add(cleanup); }
            }
            if (operationError != null)
            {
                foreach (var cleanup in cleanupErrors) AddCleanupError(operationError, cleanup);
            }
            else if (cleanupErrors.Count > 0)
            {
                var primaryCleanup = cleanupErrors[0];
                foreach (var cleanup in cleanupErrors.Skip(1)) AddCleanupError(primaryCleanup, cleanup);
                ExceptionDispatchInfo.Capture(primaryCleanup).Throw();
            }
        }
    }

    private static EncodingDetectionResult DetectSample(byte[] sample, int sampledBytes, bool eof, EncodingDetectionOptions options)
    {
        var evidence = new List<string>();
        var candidates = new List<EncodingDetectionCandidate>();
        var bom = DetectBom(sample);
        if (bom.Encoding != null)
        {
            evidence.Add($"检测到 {bom.Encoding.WebName} BOM（{bom.Length} 字节）。");
            if (options.ExplicitEncoding != null && !SameEncoding(options.ExplicitEncoding, bom.Encoding))
            {
                if (options.BomConflictBehavior == BomConflictBehavior.Throw)
                    throw new EncodingDetectionException("encoding.bom_conflict", $"显式编码 {options.ExplicitEncoding.WebName} 与 BOM 编码 {bom.Encoding.WebName} 冲突。");
                evidence.Add("显式编码与 BOM 冲突，按配置采用 BOM 编码。");
            }
            var bomCandidate = new EncodingDetectionCandidate(bom.Encoding, 1, EncodingCandidateSource.Bom, evidence[0]);
            candidates.Add(bomCandidate);
            return new EncodingDetectionResult(bomCandidate, candidates, evidence, sampledBytes, eof, bom.Length, false);
        }

        if (options.ExplicitEncoding != null)
        {
            evidence.Add($"未检测到 BOM，使用显式编码 {options.ExplicitEncoding.WebName}。");
            var selected = new EncodingDetectionCandidate(options.ExplicitEncoding, 1, EncodingCandidateSource.Explicit, evidence[0]);
            candidates.Add(selected);
            return new EncodingDetectionResult(selected, candidates, evidence, sampledBytes, eof, 0, false);
        }

        var asciiOnly = sample.All(value => value < 0x80);
        var incompleteUtf8Tail = IsTruncatedUtf8Tail(sample, eof, out var validUtf8Prefix);
        DetectionResult detected;
        try { detected = CharsetDetector.DetectFromBytes(sample); }
        catch (Exception error)
        {
            if (options.FallbackEncoding == null)
                throw new EncodingDetectionException("encoding.detector_failed", "编码检测器无法分析输入样本。", error);
            evidence.Add("检测器分析失败，按配置使用显式回退编码。");
            var fallback = new EncodingDetectionCandidate(options.FallbackEncoding, 0, EncodingCandidateSource.Fallback, evidence[0]);
            candidates.Add(fallback);
            return new EncodingDetectionResult(fallback, candidates, evidence, sampledBytes, eof, 0, true);
        }

        foreach (var detail in detected.Details ?? Array.Empty<DetectionDetail>())
        {
            if (detail.Encoding == null) continue;
            double confidence = Math.Max(0, Math.Min(1, detail.Confidence));
            if (asciiOnly) confidence = Math.Min(confidence, 0.5);
            candidates.Add(new EncodingDetectionCandidate(detail.Encoding, confidence, EncodingCandidateSource.Detector,
                $"UTF.Unknown: {detail.EncodingName}，置信度 {confidence:0.###}。"));
        }

        if (incompleteUtf8Tail && validUtf8Prefix && !asciiOnly)
        {
            var utf8 = candidates.FirstOrDefault(candidate => SameEncoding(candidate.Encoding, StrictUtf8));
            if (utf8 == null)
                candidates.Insert(0, new EncodingDetectionCandidate(StrictUtf8, 0.9, EncodingCandidateSource.Detector,
                    "采样尾部是不完整 UTF-8 多字节字符；已仅验证其之前的完整 UTF-8 前缀。"));
            else
            {
                candidates.Remove(utf8);
                candidates.Insert(0, new EncodingDetectionCandidate(utf8.Encoding, Math.Max(0.9, utf8.Confidence), utf8.Source,
                    "采样尾部是不完整 UTF-8 多字节字符；已仅验证其之前的完整 UTF-8 前缀。"));
            }
            evidence.Add("采样以不完整 UTF-8 多字节字符结束；该尾部未用于候选判定。");
        }

        if (asciiOnly)
        {
            evidence.Add("样本仅包含 ASCII 字节；多种 ASCII 兼容编码不可由此样本区分。");
        }
        if (incompleteUtf8Tail) evidence.Add("采样上限截断了末尾的 UTF-8 多字节字符；回放仍包含该字节。");
        evidence.Add(eof ? "采样已到达输入末尾。" : "采样达到上限，另行探测到后续字节；检测样本未超过配置上限。");

        var best = candidates.OrderByDescending(candidate => candidate.Confidence).FirstOrDefault();
        var ambiguous = asciiOnly || best == null || best.Confidence < options.MinimumConfidence;
        if (ambiguous)
        {
            if (options.FallbackEncoding == null)
                throw new EncodingDetectionException("encoding.low_confidence", asciiOnly
                    ? "样本仅包含 ASCII 字节，无法可靠区分 ASCII 兼容编码；请指定显式编码或回退编码。"
                    : $"编码候选置信度不足（最高 {best?.Confidence ?? 0:0.###}，要求 {options.MinimumConfidence:0.###}）；请指定显式编码或回退编码。");
            var fallback = new EncodingDetectionCandidate(options.FallbackEncoding,
                best?.Confidence ?? 0, EncodingCandidateSource.Fallback, "候选置信度不足，采用调用方指定的回退编码。");
            candidates.Add(fallback);
            evidence.Add(fallback.Evidence);
            best = fallback;
        }
        else
        {
            evidence.Add($"选择置信度最高的候选 {best.Encoding.WebName}。");
        }

        return new EncodingDetectionResult(best, candidates, evidence, sampledBytes, eof, 0, asciiOnly);
    }

    private static (Encoding Encoding, int Length) DetectBom(byte[] bytes)
    {
        if (StartsWith(bytes, 0xff, 0xfe, 0x00, 0x00)) return (new UTF32Encoding(false, true, true), 4);
        if (StartsWith(bytes, 0x00, 0x00, 0xfe, 0xff)) return (new UTF32Encoding(true, true, true), 4);
        if (StartsWith(bytes, 0xef, 0xbb, 0xbf)) return (new UTF8Encoding(true, true), 3);
        if (StartsWith(bytes, 0xff, 0xfe)) return (new UnicodeEncoding(false, true, true), 2);
        if (StartsWith(bytes, 0xfe, 0xff)) return (new UnicodeEncoding(true, true, true), 2);
        return (null, 0);
    }

    private static bool StartsWith(byte[] bytes, params byte[] prefix)
    {
        if (bytes.Length < prefix.Length) return false;
        for (var index = 0; index < prefix.Length; index++) if (bytes[index] != prefix[index]) return false;
        return true;
    }

    private static bool SameEncoding(Encoding left, Encoding right)
        => string.Equals(left.WebName, right.WebName, StringComparison.OrdinalIgnoreCase);

    private static bool IsTruncatedUtf8Tail(byte[] bytes, bool eof, out bool validPrefix)
    {
        validPrefix = false;
        if (eof || bytes.Length == 0) return false;
        var start = bytes.Length - 1;
        while (start >= 0 && (bytes[start] & 0xc0) == 0x80) start--;
        if (start < 0) return false;
        var lead = bytes[start];
        var expected = lead >= 0xc2 && lead <= 0xdf ? 2 : lead >= 0xe0 && lead <= 0xef ? 3 : lead >= 0xf0 && lead <= 0xf4 ? 4 : 1;
        var actual = bytes.Length - start;
        if (expected <= 1 || actual >= expected) return false;
        if (!IsValidUtf8PartialSequence(lead, bytes, start, actual)) return false;
        try
        {
            StrictUtf8.GetString(bytes, 0, start);
            validPrefix = start > 0;
            return true;
        }
        catch (DecoderFallbackException) { return false; }
    }

    private static bool IsValidUtf8PartialSequence(byte lead, byte[] bytes, int start, int length)
    {
        for (var index = 1; index < length; index++)
        {
            var value = bytes[start + index];
            if ((value & 0xc0) != 0x80) return false;
            if (index == 1 && (lead == 0xe0 && value < 0xa0 || lead == 0xed && value > 0x9f ||
                               lead == 0xf0 && value < 0x90 || lead == 0xf4 && value > 0x8f)) return false;
        }
        return true;
    }

    private static byte[] ReadSample(Stream stream, int maximum, out bool eof, out byte? probe)
    {
        var buffer = new byte[maximum];
        var count = 0;
        while (count < maximum)
        {
            var read = stream.Read(buffer, count, maximum - count);
            if (read == 0) { eof = true; probe = null; return Trim(buffer, count); }
            count += read;
        }
        var extra = stream.ReadByte();
        eof = extra < 0;
        probe = extra < 0 ? null : (byte)extra;
        return buffer;
    }

    private static async Task<(byte[] Sample, bool EndOfStream, byte? Probe)> ReadSampleAsync(
        Stream stream, int maximum, CancellationToken cancellationToken)
    {
        var buffer = new byte[maximum];
        var count = 0;
        while (count < maximum)
        {
            var read = await stream.ReadAsync(buffer, count, maximum - count, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (read == 0) return (Trim(buffer, count), true, null);
            count += read;
        }
        var extra = new byte[1];
        var probeRead = await stream.ReadAsync(extra, 0, 1, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return (buffer, probeRead == 0, probeRead == 0 ? null : extra[0]);
    }

    private static byte[] Trim(byte[] buffer, int length)
    {
        if (length == buffer.Length) return buffer;
        var trimmed = new byte[length];
        Array.Copy(buffer, trimmed, length);
        return trimmed;
    }

    private static byte[] AppendByte(byte[] bytes, byte value)
    {
        var result = new byte[bytes.Length + 1];
        Array.Copy(bytes, result, bytes.Length);
        result[result.Length - 1] = value;
        return result;
    }

    private static void ValidateReadable(Stream stream)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        if (!stream.CanRead) throw new ArgumentException("输入流不可读。", nameof(stream));
    }

    private static async Task DiscardAsync(Stream stream, int bytes, CancellationToken cancellationToken)
    {
        var buffer = new byte[bytes];
        var discarded = 0;
        while (discarded < bytes)
        {
            var read = await stream.ReadAsync(buffer, discarded, bytes - discarded, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (read == 0) throw new EndOfStreamException("回放流未包含检测到的 BOM。");
            discarded += read;
        }
    }

    private static async Task ConvertDecodedAsync(Decoder decoder, byte[] input, int offset, int count, bool flush,
        char[] chars, Encoder encoder, byte[] encoded, Stream destination, CancellationToken cancellationToken)
    {
        var consumed = 0;
        var completed = false;
        while (!completed)
        {
            cancellationToken.ThrowIfCancellationRequested();
            decoder.Convert(input, offset + consumed, count - consumed, chars, 0, chars.Length,
                flush, out var bytesUsed, out var charsUsed, out completed);
            consumed += bytesUsed;
            await ConvertEncodedAsync(encoder, chars, 0, charsUsed, false, encoded, destination, cancellationToken).ConfigureAwait(false);
            if (bytesUsed == 0 && charsUsed == 0 && !completed)
                throw new InvalidDataException("增量解码器无法继续处理当前输入块。");
        }
    }

    private static async Task ConvertEncodedAsync(Encoder encoder, char[] chars, int offset, int count, bool flush,
        byte[] bytes, Stream destination, CancellationToken cancellationToken)
    {
        var consumed = 0;
        var completed = false;
        while (!completed)
        {
            cancellationToken.ThrowIfCancellationRequested();
            encoder.Convert(chars, offset + consumed, count - consumed, bytes, 0, bytes.Length,
                flush, out var charsUsed, out var bytesUsed, out completed);
            consumed += charsUsed;
            if (bytesUsed > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await destination.WriteAsync(bytes, 0, bytesUsed, cancellationToken).ConfigureAwait(false);
            }
            if (charsUsed == 0 && bytesUsed == 0 && !completed)
                throw new InvalidDataException("增量编码器无法继续处理当前字符块。");
        }
    }

    private static void AddCleanupError(Exception operationError, Exception cleanupError)
    {
        if (operationError == null) return;
        const string key = "EncodingDetection.CleanupErrors";
        if (operationError.Data[key] is List<Exception> errors) errors.Add(cleanupError);
        else operationError.Data[key] = new List<Exception> { cleanupError };
    }

}

/// <summary>包含编码检测结果和回放流。</summary>
public sealed class EncodingReplayResult
{
    internal EncodingReplayResult(EncodingDetectionResult detection, Stream replayStream)
    {
        Detection = detection;
        ReplayStream = replayStream;
    }
    /// <summary>获取检测结果。</summary>
    public EncodingDetectionResult Detection { get; }
    /// <summary>获取从原始当前位置开始的完整回放流；销毁它不会销毁底层源流。</summary>
    public Stream ReplayStream { get; }
}

internal sealed class ReplayableStream : Stream
{
    private readonly MemoryStream _prefix;
    private readonly Stream _source;
    private bool _prefixDone;

    internal ReplayableStream(byte[] prefix, Stream source)
    {
        _prefix = new MemoryStream(prefix, writable: false);
        _source = source;
    }

    public override bool CanRead => _prefix != null && _source.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override void Flush() => throw new NotSupportedException();
    public override int Read(byte[] buffer, int offset, int count)
    {
        if (!_prefixDone)
        {
            var read = _prefix.Read(buffer, offset, count);
            if (read > 0) return read;
            _prefixDone = true;
        }
        return _source.Read(buffer, offset, count);
    }
    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        if (!_prefixDone)
        {
            var read = await _prefix.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
            if (read > 0) return read;
            _prefixDone = true;
        }
        return await _source.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) _prefix.Dispose();
        base.Dispose(disposing);
    }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
