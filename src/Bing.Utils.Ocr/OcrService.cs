using System.Diagnostics;

namespace Bing.Ocr;

/// <summary>共享、线程安全的识别编排器；借用注入组件，不负责释放组件。</summary>
public sealed class OcrService : IOcrService
{
    private readonly IOcrProvider _provider;
    private readonly IOcrImagePreprocessor _preprocessor;
    private readonly OcrServiceOptions _limits;
    // 仅使用异步等待，不访问 AvailableWaitHandle；无后台线程或定时任务。
    private readonly SemaphoreSlim _gate;
    private int _outstanding;

    /// <summary>创建共享服务并固定配置。</summary>
    public OcrService(IOcrProvider provider, IOcrImagePreprocessor preprocessor, OcrServiceOptions? options = null)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _preprocessor = preprocessor ?? throw new ArgumentNullException(nameof(preprocessor));
        _limits = (options ?? new OcrServiceOptions()).Snapshot();
        _gate = new SemaphoreSlim(_limits.MaxConcurrency, _limits.MaxConcurrency);
    }

    /// <inheritdoc />
    public async Task<OcrResult> RecognizeAsync(Stream image, OcrOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (image == null) throw new ArgumentNullException(nameof(image));
        if (!image.CanRead) throw new ArgumentException("输入流不可读。", nameof(image));
        cancellationToken.ThrowIfCancellationRequested();
        var request = (options ?? new OcrOptions()).Snapshot();
        _provider.ValidateOptions(request);
        var clock = Stopwatch.StartNew();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(_limits.Timeout);
        var token = deadline.Token;
        Enter();
        var acquired = false;
        try
        {
            await _gate.WaitAsync(token).ConfigureAwait(false);
            acquired = true;
            var bytes = await ReadBoundedAsync(image, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            // 每个适配器获得独立配置，防止可变配置影响后续请求。
            var prepared = _preprocessor.Prepare(bytes, request.Snapshot(), _limits.Snapshot(), token)
                ?? throw new OcrInputException("invalid_preprocessor_result", "预处理器没有返回图片。");
            token.ThrowIfCancellationRequested();
            _limits.CheckSize(prepared.OriginalWidth, prepared.OriginalHeight);
            _limits.CheckSize(prepared.Width, prepared.Height);
            if (prepared.ByteLength > _limits.MaxPreparedBytes)
                throw new OcrInputException("prepared_image_too_large", "预处理图片超过限制。");
            var result = await _provider.RecognizeAsync(prepared, request.Snapshot(), token).ConfigureAwait(false)
                ?? throw new OcrProviderException("识别引擎没有返回结果。");
            token.ThrowIfCancellationRequested();
            var lines = new List<OcrLine>(result.Lines.Count);
            foreach (var line in result.Lines)
            {
                token.ThrowIfCancellationRequested();
                var mapped = line.Polygon.Select(point =>
                {
                    CheckPoint(point, prepared.Width, prepared.Height);
                    var value = prepared.ToOriginal.Apply(point);
                    CheckPoint(value, prepared.OriginalWidth, prepared.OriginalHeight);
                    return new OcrPoint(Math.Max(0, Math.Min(value.X, prepared.OriginalWidth)),
                        Math.Max(0, Math.Min(value.Y, prepared.OriginalHeight)));
                }).ToArray();
                lines.Add(new OcrLine(line.Text, mapped, line.Confidence,
                    line.Confidence.HasValue && line.Confidence.Value < request.LowConfidenceThreshold));
            }
            return new OcrResult(prepared, lines, _provider.Name, result, clock.Elapsed);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            throw new OcrTimeoutException(ex);
        }
        finally
        {
            if (acquired) _gate.Release();
            Interlocked.Decrement(ref _outstanding);
        }
    }

    private void Enter()
    {
        while (true)
        {
            var current = Volatile.Read(ref _outstanding);
            if (current >= _limits.MaxConcurrency + _limits.MaxQueuedRequests) throw new OcrBusyException();
            if (Interlocked.CompareExchange(ref _outstanding, current + 1, current) == current) return;
        }
    }

    private async Task<byte[]> ReadBoundedAsync(Stream image, CancellationToken token)
    {
        using var output = new MemoryStream();
        var buffer = new byte[16384];
        while (true)
        {
            token.ThrowIfCancellationRequested();
            var count = (int)Math.Min(buffer.Length, (long)_limits.MaxInputBytes - output.Length + 1);
            int read;
            try { read = await image.ReadAsync(buffer, 0, count, token).ConfigureAwait(false); }
            catch (IOException ex) { throw new OcrInputException("input_read_failed", "无法读取图片。", ex); }
            if (read == 0) break;
            if (output.Length + read > _limits.MaxInputBytes) throw new OcrInputException("input_too_large", "输入图片超过限制。");
            output.Write(buffer, 0, read);
        }
        if (output.Length == 0) throw new OcrInputException("empty_input", "输入图片为空。");
        return output.ToArray();
    }

    private static void CheckPoint(OcrPoint point, int width, int height)
    {
        // 仅容忍浮点运算的边缘误差，不接收明显脱离图片的框。
        const double epsilon = 0.01;
        if (point.X < -epsilon || point.Y < -epsilon || point.X > width + epsilon || point.Y > height + epsilon)
            throw new OcrProviderException("文字框坐标超出图片范围。");
    }
}
