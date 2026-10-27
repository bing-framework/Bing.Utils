namespace Bing.IO;

/// <summary>根据有界文件头及可选的 ZIP 元数据检查识别常用二进制格式。</summary>
/// <remarks>
/// 所有偏移相对于输入起点；不依赖文件扩展名；显式启用容器检查时只解压识别元数据，不校验完整正文或安全性。
/// 可定位流使用 Detect 并恢复位置，不可定位流使用 Probe 并继续消费返回的 Content。
/// 不得在检测期间并发操作同一个源流。
/// </remarks>
public static partial class FileContentDetector
{
    /// <summary>获取所有内置格式的只读目录；Office 子类型需要启用容器检查。</summary>
    public static IReadOnlyList<FileContentFormat> SupportedFormats => FileContentFormats.SupportedFormats;

    /// <summary>从文件名或路径的最后一个路径段提取扩展名，并与内容检测结果比较。</summary>
    /// <param name="result">内容检测结果。</param>
    /// <param name="fileName">文件名或路径；同时识别斜杠和反斜杠，不访问文件系统。</param>
    /// <returns>空白、无扩展名或未知扩展名返回 Indeterminate；其他语义与 CompareExtension 相同。</returns>
    public static FileExtensionMatch CompareFileName(FileDetectionResult result, string fileName)
    {
        if (result == null) throw new ArgumentNullException(nameof(result));
        if (string.IsNullOrWhiteSpace(fileName)) return FileExtensionMatch.Indeterminate;
        var name = fileName.Trim();
        var separator = Math.Max(name.LastIndexOf('/'), name.LastIndexOf('\\'));
        var dot = name.LastIndexOf('.');
        return dot <= separator + 1 || dot == name.Length - 1 ? FileExtensionMatch.Indeterminate
            : CompareExtension(result, name.Substring(dot));
    }

    /// <summary>检测字节数组；启用容器检查时须提供完整内容，不复制整个输入。</summary>
    /// <param name="content">完整内容或仅用于文件头检测的前缀；容器检查需要完整输入，空数组返回 Empty。</param>
    /// <param name="options">前缀及可选容器检查预算。</param>
    /// <returns>检测结论；签名前缀不足或请求的容器检查无法完成时返回 Incomplete。</returns>
    /// <exception cref="ArgumentNullException">content 为 null。</exception>
    /// <exception cref="ArgumentOutOfRangeException">读取预算不是正数。</exception>
    public static FileDetectionResult Detect(byte[] content, FileDetectionOptions options = null)
    {
        if (content == null) throw new ArgumentNullException(nameof(content));
        return Detect(new ReadOnlySpan<byte>(content), options);
    }

    /// <summary>检测字节片段中的文件头；不保存输入内存的引用。</summary>
    /// <param name="content">完整内容或仅用于文件头检测的前缀。</param>
    /// <param name="options">前缀及可选容器检查预算。</param>
    /// <returns>检测结论及前缀、容器阶段的字节计数。</returns>
    /// <exception cref="ArgumentOutOfRangeException">读取预算不是正数。</exception>
    public static FileDetectionResult Detect(ReadOnlySpan<byte> content, FileDetectionOptions options = null)
    {
        var snapshot = FileDetectionOptions.Snapshot(options);
        var limit = snapshot.MaxHeaderBytes;
        var count = Math.Min(content.Length, Math.Min(limit, FileContentSignatures.RequiredBytes));
        var result = FileContentSignatures.Match(content.Slice(0, count), FileContentSignatures.Rules,
            count == content.Length ? FileDetectionReason.Truncated : FileDetectionReason.HeaderLimitReached);
        return ShouldInspect(result, snapshot) ? FileContentZipInspector.Inspect(content, result, snapshot) : result;
    }

    /// <summary>同步检测可定位流，从当前位置读取并在成功、异常或取消后恢复位置，始终保留源流。</summary>
    /// <param name="source">可读且可定位的流；当前位置视为文件内容起点。</param>
    /// <param name="options">前缀及可选容器检查预算。</param>
    /// <param name="cancellationToken">每次读取前后检查的取消令牌。</param>
    /// <returns>文件头检测结果。</returns>
    /// <exception cref="NotSupportedException">源不支持定位；请使用 Probe。</exception>
    public static FileDetectionResult Detect(Stream source, FileDetectionOptions options = null,
        CancellationToken cancellationToken = default)
        => DetectStreamCore(source, options, cancellationToken, false).GetAwaiter().GetResult();

    /// <summary>异步检测可定位流；无论成功、异常或取消均恢复调用前位置，并保留源流。</summary>
    /// <param name="source">可读且可定位的流。</param>
    /// <param name="options">前缀及可选容器检查预算。</param>
    /// <param name="cancellationToken">传递给底层读取的取消令牌。</param>
    /// <returns>文件头检测结果。</returns>
    /// <exception cref="NotSupportedException">源不支持定位；请使用 ProbeAsync。</exception>
    public static Task<FileDetectionResult> DetectAsync(Stream source, FileDetectionOptions options = null,
        CancellationToken cancellationToken = default)
        => DetectStreamCore(source, options, cancellationToken, true);

    /// <summary>同步打开并检测文件，结束时关闭本方法打开的文件句柄。</summary>
    /// <param name="path">文件路径；扩展名不参与识别。</param>
    /// <param name="options">前缀及可选容器检查预算。</param>
    /// <param name="cancellationToken">读取前后观察的取消令牌。</param>
    /// <returns>文件头检测结果。</returns>
    public static FileDetectionResult DetectFile(string path, FileDetectionOptions options = null,
        CancellationToken cancellationToken = default)
        => DetectFileCore(path, options, cancellationToken, false).GetAwaiter().GetResult();

    /// <summary>异步打开并检测文件，结束时关闭本方法打开的文件句柄。</summary>
    /// <param name="path">文件路径；扩展名不参与识别。</param>
    /// <param name="options">前缀及可选容器检查预算。</param>
    /// <param name="cancellationToken">传递给文件读取的取消令牌。</param>
    /// <returns>文件头检测结果。</returns>
    public static Task<FileDetectionResult> DetectFileAsync(string path, FileDetectionOptions options = null,
        CancellationToken cancellationToken = default)
        => DetectFileCore(path, options, cancellationToken, true);

    /// <summary>同步探测可读流，返回检测结果和能够重放已读前缀的只读流。</summary>
    /// <param name="source">可读流；成功后源已前进，后续必须使用返回的 Content。</param>
    /// <param name="options">前缀及可选容器检查预算。</param>
    /// <param name="leaveOpen">默认 true；false 在参数验证和初始取消检查通过后接管源。</param>
    /// <param name="cancellationToken">读取前后观察的取消令牌。</param>
    /// <returns>应释放的探测租约；释放时清理前缀，并按 leaveOpen 处理源。</returns>
    /// <remarks>失败或取消不保证源位置恢复；接管源后发生失败会释放源。ZIP 深检测需要可定位源，否则返回 SeekableSourceRequired 并保留可重放正文。</remarks>
    public static FileContentProbe Probe(Stream source, FileDetectionOptions options = null, bool leaveOpen = true,
        CancellationToken cancellationToken = default)
        => ProbeCore(source, options, leaveOpen, cancellationToken, false).GetAwaiter().GetResult();

    /// <summary>异步探测可读流，返回检测结果和能够重放已读前缀的只读流。</summary>
    /// <param name="source">可读流；成功后源已前进，后续必须使用返回的 Content。</param>
    /// <param name="options">前缀及可选容器检查预算。</param>
    /// <param name="leaveOpen">默认 true；false 在参数验证和初始取消检查通过后接管源。</param>
    /// <param name="cancellationToken">传递给底层读取的取消令牌。</param>
    /// <returns>应释放的探测租约。</returns>
    /// <remarks>失败或取消不保证源位置恢复；接管源后发生失败会释放源。ZIP 深检测需要可定位源，否则返回 SeekableSourceRequired 并保留可重放正文。</remarks>
    public static Task<FileContentProbe> ProbeAsync(Stream source, FileDetectionOptions options = null, bool leaveOpen = true,
        CancellationToken cancellationToken = default)
        => ProbeCore(source, options, leaveOpen, cancellationToken, true);

    /// <summary>比较内容检测结果与声明扩展名，不使用扩展名反推或覆盖识别结果。</summary>
    /// <param name="result">内容检测结果。</param>
    /// <param name="extension">扩展名，例如 jpg 或 .jpg，忽略大小写及两端空白；不接受文件路径。</param>
    /// <returns>唯一匹配且声明为已知内置扩展名时返回 Match 或 Mismatch；其余返回 Indeterminate。</returns>
    /// <remarks>仅有 ZIP 签名证据时与 Office 扩展名比较返回 Indeterminate；完成结构检查后可比较六种 Office 格式。</remarks>
    public static FileExtensionMatch CompareExtension(FileDetectionResult result, string extension)
    {
        if (result == null) throw new ArgumentNullException(nameof(result));
        if (result.Status != FileDetectionStatus.Matched || string.IsNullOrWhiteSpace(extension))
            return FileExtensionMatch.Indeterminate;
        var normalized = extension.Trim();
        if (normalized.StartsWith(".", StringComparison.Ordinal)) normalized = normalized.Substring(1);
        if (result.Format.Extensions.Any(x => string.Equals(x, normalized, StringComparison.OrdinalIgnoreCase)))
            return FileExtensionMatch.Match;
        if (FileContentOfficePackage.IsOfficeExtension(normalized))
            return result.Format == FileContentFormats.Zip && result.Evidence == FileDetectionEvidence.Signature
                ? FileExtensionMatch.Indeterminate : FileExtensionMatch.Mismatch;
        return FileContentSignatures.Rules.Any(r => r.Format.Extensions.Any(x => string.Equals(x, normalized, StringComparison.OrdinalIgnoreCase)))
            ? FileExtensionMatch.Mismatch : FileExtensionMatch.Indeterminate;
    }

    /// <summary>在读取前固定预算并检查输入；此阶段不接管资源。</summary>
    private static FileDetectionOptions Validate(Stream source, FileDetectionOptions options, CancellationToken token)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (!source.CanRead) throw new ArgumentException("源流必须可读。", nameof(source));
        var snapshot = FileDetectionOptions.Snapshot(options);
        token.ThrowIfCancellationRequested();
        return snapshot;
    }

    /// <summary>使用 finally 恢复位置；同步路径只调用同步读取。</summary>
    private static async Task<FileDetectionResult> DetectStreamCore(Stream source, FileDetectionOptions options,
        CancellationToken token, bool asynchronous)
    {
        var snapshot = Validate(source, options, token);
        if (!source.CanSeek) throw new NotSupportedException("检测并恢复位置需要可定位流；不可定位流请使用 Probe 或 ProbeAsync。");
        var origin = source.Position;
        try
        {
            var header = await ReadHeader(source, snapshot.MaxHeaderBytes, token, asynchronous).ConfigureAwait(false);
            return await InspectContainer(source, origin, header.Result, snapshot, token, asynchronous).ConfigureAwait(false);
        }
        finally { source.Position = origin; }
    }

    /// <summary>管理自行打开的文件句柄。</summary>
    private static async Task<FileDetectionResult> DetectFileCore(string path, FileDetectionOptions options,
        CancellationToken token, bool asynchronous)
    {
        if (path == null) throw new ArgumentNullException(nameof(path));
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("文件路径不能为空。", nameof(path));
        var snapshot = FileDetectionOptions.Snapshot(options);
        token.ThrowIfCancellationRequested();
        using var source = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096,
            asynchronous ? FileOptions.Asynchronous : FileOptions.None);
        var header = await ReadHeader(source, snapshot.MaxHeaderBytes, token, asynchronous).ConfigureAwait(false);
        return await InspectContainer(source, 0, header.Result, snapshot, token, asynchronous).ConfigureAwait(false);
    }

    /// <summary>探测期间接管可选所有权，成功时交给重放流，失败时按契约清理。</summary>
    private static async Task<FileContentProbe> ProbeCore(Stream source, FileDetectionOptions options,
        bool leaveOpen, CancellationToken token, bool asynchronous)
    {
        var snapshot = Validate(source, options, token);
        try
        {
            var origin = source.CanSeek ? source.Position : 0;
            var header = await ReadHeader(source, snapshot.MaxHeaderBytes, token, asynchronous).ConfigureAwait(false);
            var result = header.Result;
            if (ShouldInspect(result, snapshot))
            {
                if (!source.CanSeek)
                    result = new FileDetectionResult(FileDetectionStatus.Incomplete, FileDetectionReason.SeekableSourceRequired,
                        result.Candidates, result.BytesRead, result.Format, FileDetectionEvidence.Signature);
                else
                {
                    var afterHeader = source.Position;
                    try { result = await InspectContainer(source, origin, result, snapshot, token, asynchronous).ConfigureAwait(false); }
                    finally { source.Position = afterHeader; }
                }
            }
            return new FileContentProbe(result, new FilePrefixReplayStream(source, header.Buffer, header.Result.BytesRead, leaveOpen));
        }
        catch
        {
            if (!leaveOpen) source.Dispose();
            throw;
        }
    }

    /// <summary>只在 ZIP 签名唯一确定且调用方明确启用时检查容器。</summary>
    private static bool ShouldInspect(FileDetectionResult result, FileDetectionOptions options)
        => options.InspectContainers && result.Status == FileDetectionStatus.Matched && result.Format == FileContentFormats.Zip;

    /// <summary>保持非 ZIP 及默认浅检测路径，容器位置由外层入口恢复。</summary>
    private static Task<FileDetectionResult> InspectContainer(Stream source, long origin, FileDetectionResult result,
        FileDetectionOptions options, CancellationToken token, bool asynchronous)
        => ShouldInspect(result, options)
            ? FileContentZipInspector.InspectAsync(source, origin, result, options, token, asynchronous)
            : Task.FromResult(result);

    /// <summary>按规则所需的最大前缀分配缓冲，支持短读并在所有候选已确定后停止。</summary>
    private static async Task<Header> ReadHeader(Stream source, int limit, CancellationToken token, bool asynchronous)
    {
        var buffer = new byte[Math.Min(limit, FileContentSignatures.RequiredBytes)];
        var count = 0;
        while (true)
        {
            token.ThrowIfCancellationRequested();
            var read = asynchronous
                ? await source.ReadAsync(buffer, count, buffer.Length - count, token).ConfigureAwait(false)
                : source.Read(buffer, count, buffer.Length - count);
            token.ThrowIfCancellationRequested();
            count += read;
            var result = MatchHeader(buffer, count, read == 0 ? FileDetectionReason.Truncated : FileDetectionReason.HeaderLimitReached);
            if (read == 0 || result.Status != FileDetectionStatus.Incomplete || count == buffer.Length)
                return new Header(buffer, result);
        }
    }

    /// <summary>将 Span 操作隔离在同步方法中，兼容所有目标框架。</summary>
    private static FileDetectionResult MatchHeader(byte[] buffer, int count, FileDetectionReason reason)
        => FileContentSignatures.Match(new ReadOnlySpan<byte>(buffer, 0, count), FileContentSignatures.Rules, reason);

    /// <summary>在探测完成与创建重放流之间传递独占的前缀缓冲。</summary>
    private sealed class Header
    {
        internal byte[] Buffer { get; }
        internal FileDetectionResult Result { get; }
        internal Header(byte[] buffer, FileDetectionResult result) { Buffer = buffer; Result = result; }
    }
}
