using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.Json;
using Bing.Http;
using Bing.Http.Clients;

if (args.Length > 0 && args[0] == "--compare")
{
    await BaselineComparer.RunAsync(args.Skip(1).ToArray());
    return;
}

var positionalArgs = args.ToList();
string? baselinePath = null;
var baselineOptionIndex = positionalArgs.IndexOf("--baseline");
if (baselineOptionIndex >= 0)
{
    if (baselineOptionIndex + 1 >= positionalArgs.Count ||
        string.IsNullOrWhiteSpace(positionalArgs[baselineOptionIndex + 1]))
        throw new ArgumentException("--baseline requires an output path.");
    baselinePath = positionalArgs[baselineOptionIndex + 1];
    positionalArgs.RemoveRange(baselineOptionIndex, 2);
}

var mode = positionalArgs.Count > 0 ? positionalArgs[0] : "upload";
var sizeMiB = positionalArgs.Count > 1 ? int.Parse(positionalArgs[1]) : 1;
var concurrency = positionalArgs.Count > 2 ? int.Parse(positionalArgs[2]) : 1;
var repetitions = positionalArgs.Count > 3 ? int.Parse(positionalArgs[3]) : 3;
if (sizeMiB < 0) throw new ArgumentOutOfRangeException(nameof(sizeMiB));
if (concurrency <= 0) throw new ArgumentOutOfRangeException(nameof(concurrency));
if (repetitions <= 0) throw new ArgumentOutOfRangeException(nameof(repetitions));
var path = Path.Combine(Path.GetTempPath(), $"bing-http-benchmark-{Guid.NewGuid():N}.bin");
var largePayloadNameLength = 0;
var baselineSamples = new List<BaselineSample>();
var operationsPerSample = mode is "json-large" or "json-large-stream" or "metadata-large" or "raw-large" ? 100 :
    mode == "upload" ? concurrency : 1000;

if (mode is "json-large" or "json-large-stream" or "metadata-large" or "raw-large")
{
    if (sizeMiB <= 0) throw new ArgumentOutOfRangeException(nameof(sizeMiB), "Large JSON modes require sizeMiB > 0.");
    largePayloadNameLength = checked(sizeMiB * 1024 * 1024);
    var largeJson = $"{{\"name\":\"{new string('x', largePayloadNameLength)}\"}}";
    BenchmarkHandler.ConfigureLargePayload(System.Text.Encoding.UTF8.GetBytes(largeJson));
}

if (mode == "upload")
{
    using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
        file.SetLength((long)sizeMiB * 1024 * 1024);
}

try
{
    using var client = new HttpClient(new BenchmarkHandler(mode is "retry-unobserved" or "retry-observed"));
    if (mode != "upload")
    {
        var warmup = new HttpRequest<Payload>(null, client, HttpMethod.Get,
            largePayloadNameLength > 0 ? "https://bench.invalid/large" : "https://bench.invalid/json");
        if (mode is "retry-unobserved" or "retry-observed")
            warmup.Retry(new HttpRetryOptions { MaxRetries = 1, BaseDelay = TimeSpan.Zero, MaxDelay = TimeSpan.Zero, UseJitter = false });
        if (mode == "retry-observed") warmup.ObserveRetry(_ => { });
        for (var i = 0; i < 10; i++)
        {
            if (mode is "metadata" or "raw" or "metadata-large" or "raw-large")
                await warmup.GetResponseAsync(includeRawContent: mode is "raw" or "raw-large");
            else if (mode is "json-stream" or "json-large-stream") await warmup.GetJsonStreamAsync();
            else if (mode is "download-result" or "download-progress")
            {
                using var destination = new MemoryStream();
                await warmup.DownloadToWithResultAsync(destination);
            }
            else await warmup.GetResultAsync();
        }
    }
    for (var repetition = 0; repetition < repetitions; repetition++)
    {
        BenchmarkHandler.Reset();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var allocatedBefore = GC.GetTotalAllocatedBytes(true);
        long peakHeap = GC.GetTotalMemory(false);
        using var stopSampling = new CancellationTokenSource();
        var sampler = Task.Run(async () =>
        {
            while (!stopSampling.IsCancellationRequested)
            {
                BenchmarkMeasurement.RecordMaximum(ref peakHeap, GC.GetTotalMemory(false));
                try { await Task.Delay(5, stopSampling.Token); }
                catch (OperationCanceledException) { break; }
            }
        });
        var watch = Stopwatch.StartNew();
        if (mode == "upload")
        {
            var requests = Enumerable.Range(0, concurrency).Select(_ =>
            {
                BenchmarkHandler.Schedule();
                return Task.Run(async () => await new HttpRequest<string>(null, client, HttpMethod.Post, "https://bench.invalid/upload")
                    .FileContent(path).GetResultAsync());
            });
            await Task.WhenAll(requests);
        }
        else if (mode == "json" || mode == "json-stream" || mode == "json-large" ||
                 mode == "json-large-stream" || mode == "metadata-large" || mode == "raw-large" || mode == "metadata" || mode == "raw" ||
                 mode == "interceptor" || mode == "observed" || mode == "download-result" ||
                 mode == "download-progress" || mode == "retry-unobserved" || mode == "retry-observed")
        {
            var url = mode is "json-large" or "json-large-stream" or "metadata-large" or "raw-large"
                ? "https://bench.invalid/large"
                : "https://bench.invalid/json";
            var request = new HttpRequest<Payload>(null, client, HttpMethod.Get, url);
            if (mode == "interceptor") request.UseInterceptor(new BenchmarkInterceptor());
            if (mode == "observed") request.ObserveCompletion(_ => { });
            if (mode == "download-progress") request.ObserveDownloadProgress(_ => { });
            if (mode is "retry-unobserved" or "retry-observed")
                request.Retry(new HttpRetryOptions { MaxRetries = 1, BaseDelay = TimeSpan.Zero, MaxDelay = TimeSpan.Zero, UseJitter = false });
            if (mode == "retry-observed") request.ObserveRetry(_ => { });
            var iterationCount = mode is "json-large" or "json-large-stream" or "metadata-large" or "raw-large" ? 100 : 1000;
            for (var i = 0; i < iterationCount; i++)
            {
                if (mode is "metadata" or "raw" or "metadata-large" or "raw-large")
                {
                    var result = await request.GetResponseAsync(includeRawContent: mode is "raw" or "raw-large");
                    var expectedLength = largePayloadNameLength > 0 ? largePayloadNameLength : "benchmark".Length;
                    if (result?.Data?.Name?.Length != expectedLength ||
                        (mode is "raw" or "raw-large") && string.IsNullOrEmpty(result.RawContent))
                        throw new InvalidOperationException("Response payload was not fully consumed.");
                }
                else if (mode is "download-result" or "download-progress")
                {
                    using var destination = new MemoryStream();
                    await request.DownloadToWithResultAsync(destination);
                }
                else if (mode == "json-stream") await request.GetJsonStreamAsync();
                else if (mode is "json-large" or "json-large-stream" or "metadata-large" or "raw-large")
                {
                    var result = mode == "json-large-stream"
                        ? await request.GetJsonStreamAsync()
                        : await request.GetResultAsync();
                    if (result?.Name?.Length != largePayloadNameLength)
                        throw new InvalidOperationException("Large JSON payload was not fully consumed.");
                }
                else await request.GetResultAsync();
            }
        }
        else throw new ArgumentException($"Unknown benchmark mode: {mode}");
        watch.Stop();
        stopSampling.Cancel();
        await sampler;
        var elapsedMilliseconds = watch.Elapsed.TotalMilliseconds;
        var allocatedMiB = (GC.GetTotalAllocatedBytes(true) - allocatedBefore) / 1048576.0;
        var requestAllocatedMiB = allocatedMiB / operationsPerSample;
        var peakHeapMiB = peakHeap / 1048576.0;
        if (baselinePath == null)
        {
            Console.WriteLine($"{mode},{sizeMiB},{concurrency},{repetition + 1},{elapsedMilliseconds:F1},{allocatedMiB:F2},{peakHeapMiB:F2},{BenchmarkHandler.MaxActive},{BenchmarkHandler.MaxQueued}");
        }
        else
        {
            baselineSamples.Add(new BaselineSample(
                repetition + 1, elapsedMilliseconds, allocatedMiB, requestAllocatedMiB, peakHeapMiB,
                BenchmarkHandler.MaxActive, BenchmarkHandler.MaxQueued));
        }
    }

    if (baselinePath != null)
    {
        var fullBaselinePath = Path.GetFullPath(baselinePath);
        var baselineDirectory = Path.GetDirectoryName(fullBaselinePath);
        if (!string.IsNullOrEmpty(baselineDirectory)) Directory.CreateDirectory(baselineDirectory);
        var report = new BaselineReport(
            mode,
            sizeMiB,
            concurrency,
            repetitions,
            operationsPerSample,
            AppContext.TargetFrameworkName ?? "unknown",
            RuntimeInformation.FrameworkDescription,
            Environment.Version.ToString(),
            RuntimeInformation.OSDescription,
            RuntimeInformation.OSArchitecture.ToString(),
            Environment.ProcessorCount,
            System.Runtime.GCSettings.IsServerGC,
            baselineSamples);
        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(fullBaselinePath, json + Environment.NewLine);
        Console.WriteLine($"baseline,{fullBaselinePath}");
    }
}
finally
{
    if (File.Exists(path)) File.Delete(path);
}

/// <summary>

/// 模拟 HTTP 服务并记录基准请求的并发状态。

/// </summary>
internal sealed class BenchmarkHandler : HttpMessageHandler
{
    /// <summary>
    /// 供大正文基准模式返回的共享 JSON 正文。
    /// </summary>
    private static byte[]? _largePayload;
    /// <summary>
    /// 是否交替返回可重试失败响应。
    /// </summary>
    private readonly bool _retry;
    /// <summary>
    /// 当前处理器已经发送的请求数。
    /// </summary>
    private int _sends;
    /// <summary>
    /// 初始化一个<see cref="BenchmarkHandler"/>类型的实例。
    /// </summary>
    public BenchmarkHandler(bool retry = false) => _retry = retry;
    /// <summary>
    /// 当前正在处理的请求数。
    /// </summary>
    private static int _active;
    /// <summary>
    /// 等待处理的请求数。
    /// </summary>
    private static int _queued;
    /// <summary>
    /// 本轮测量观察到的最大并发请求数。
    /// </summary>
    private static int _maxActive;
    /// <summary>
    /// 本轮测量观察到的最大排队请求数。
    /// </summary>
    private static int _maxQueued;
    /// <summary>
    /// 获取本轮测量观察到的最大并发请求数。
    /// </summary>
    public static int MaxActive => Volatile.Read(ref _maxActive);
    /// <summary>
    /// 获取本轮测量观察到的最大排队请求数。
    /// </summary>
    public static int MaxQueued => Volatile.Read(ref _maxQueued);

    /// <summary>

    /// 设置大正文基准模式使用的响应正文。

    /// </summary>
    /// <param name="payload">要返回的 UTF-8 JSON 正文。</param>
    public static void ConfigureLargePayload(byte[] payload) => _largePayload = payload;

    /// <summary>

    /// 重置本轮基准测量的并发计数。

    /// </summary>
    public static void Reset()
    {
        Volatile.Write(ref _active, 0);
        Volatile.Write(ref _queued, 0);
        Volatile.Write(ref _maxActive, 0);
        Volatile.Write(ref _maxQueued, 0);
    }

    /// <summary>

    /// 记录一个已排队、尚未发送的请求。

    /// </summary>
    public static void Schedule()
        => RecordMaximum(ref _maxQueued, Interlocked.Increment(ref _queued));

    /// <summary>

    /// 以原子方式更新整数最大值。

    /// </summary>
    /// <param name="maximum">保存当前最大值的位置。</param>
    /// <param name="value">待比较的新值。</param>
    private static void RecordMaximum(ref int maximum, int value)
    {
        int old;
        while ((old = Volatile.Read(ref maximum)) < value &&
               Interlocked.CompareExchange(ref maximum, value, old) != old) { }
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Method == HttpMethod.Post) Interlocked.Decrement(ref _queued);
        RecordMaximum(ref _maxActive, Interlocked.Increment(ref _active));
        try
        {
            if (request.Content != null)
                await request.Content.CopyToAsync(Stream.Null, cancellationToken);
            var largePayload = request.RequestUri?.AbsolutePath == "/large" ? _largePayload : null;
            var content = largePayload != null
                ? new ByteArrayContent(largePayload)
                : new StringContent("{\"name\":\"benchmark\"}", System.Text.Encoding.UTF8, "application/json");
            if (largePayload != null)
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json")
                {
                    CharSet = "utf-8"
                };
            var statusCode = _retry && ++_sends % 2 == 1 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK;
            return new HttpResponseMessage(statusCode)
            {
                Content = content
            };
        }
        finally
        {
            Interlocked.Decrement(ref _active);
        }
    }
}

/// <summary>

/// 提供基准测量中的并发安全统计辅助方法。

/// </summary>
internal static class BenchmarkMeasurement
{
    /// <summary>
    /// 以原子方式记录观测到的最大堆大小。
    /// </summary>
    public static void RecordMaximum(ref long maximum, long value)
    {
        long old;
        while ((old = Volatile.Read(ref maximum)) < value &&
               Interlocked.CompareExchange(ref maximum, value, old) != old) { }
    }
}

/// <summary>

/// 基准 JSON 响应中的示例载荷。

/// </summary>
internal sealed class Payload
{
    /// <summary>
    /// 载荷中的名称值。
    /// </summary>
    public string? Name { get; set; }
}

/// <summary>

/// 用于测量请求拦截器调用开销的空拦截器。

/// </summary>
internal sealed class BenchmarkInterceptor : HttpRequestInterceptor { }

/// <summary>

/// 单轮 HTTP 基线测量结果。

/// </summary>
internal sealed record BaselineSample(
    int Repetition,
    double ElapsedMilliseconds,
    double AllocatedMiB,
    double RequestAllocatedMiB,
    double PeakHeapMiB,
    int MaxActualParallelism,
    int MaxQueuedRequests);

/// <summary>

/// 可离线复用的 HTTP 基线报告。

/// </summary>
internal sealed record BaselineReport(
    string Mode,
    int SizeMiB,
    int Concurrency,
    int Repetitions,
    int IterationCount,
    string TargetFramework,
    string Runtime,
    string RuntimeVersion,
    string OperatingSystem,
    string OperatingSystemArchitecture,
    int ProcessorCount,
    bool ServerGarbageCollection,
    IReadOnlyList<BaselineSample> Samples);

/// <summary>

/// 离线比较两份 HTTP 基线报告的结果。

/// </summary>
internal sealed record BaselineComparisonReport(
    string Before,
    string After,
    BaselineComparisonDimensions Dimensions,
    BaselineEnvironment Environment,
    IReadOnlyList<BaselineSampleComparison> Samples,
    IReadOnlyList<BaselinePercentileComparison> Percentiles,
    double? RegressionThresholdPercent,
    string RegressionMetric,
    string? RegressionPercentile,
    IReadOnlyList<BaselineRegressionFailure> RegressionFailures);

/// <summary>

/// 参与比较的固定输入维度。

/// </summary>
internal sealed record BaselineComparisonDimensions(
    string Mode,
    string TargetFramework,
    int SizeMiB,
    int Concurrency,
    int Repetitions,
    int IterationCount);

/// <summary>

/// 基线运行环境的兼容性维度。

/// </summary>
internal sealed record BaselineEnvironment(
    string TargetFramework,
    string Runtime,
    string RuntimeVersion,
    string OperatingSystem,
    string OperatingSystemArchitecture,
    int ProcessorCount,
    bool ServerGarbageCollection);

/// <summary>

/// 一轮基线的前后测量值与变化量。

/// </summary>
internal sealed record BaselineSampleComparison(
    int Repetition,
    BaselineMetricComparison ElapsedMilliseconds,
    BaselineMetricComparison AllocatedMiB,
    BaselineMetricComparison RequestAllocatedMiB,
    BaselineMetricComparison PeakHeapMiB,
    int BeforeMaxActualParallelism,
    int AfterMaxActualParallelism,
    int BeforeMaxQueuedRequests,
    int AfterMaxQueuedRequests);

/// <summary>

/// 一个数值指标的前后绝对值、绝对变化和百分比变化。

/// </summary>
internal sealed record BaselineMetricComparison(
    double Before,
    double After,
    double AbsoluteChange,
    double? PercentChange);

/// <summary>

/// 多轮样本的分位数前后比较结果。

/// </summary>
internal sealed record BaselinePercentileComparison(
    string Percentile,
    BaselineMetricComparison ElapsedMilliseconds,
    BaselineMetricComparison AllocatedMiB,
    BaselineMetricComparison RequestAllocatedMiB,
    BaselineMetricComparison PeakHeapMiB);

/// <summary>

/// 超过回归阈值的指标。

/// </summary>
internal sealed record BaselineRegressionFailure(int Repetition, string Metric, double PercentChange, string Statistic = "sample");

/// <summary>

/// 读取并比较 HTTP 基线报告。

/// </summary>
internal static class BaselineComparer
{
    /// <summary>
    /// 执行离线比较，不发送网络请求。
    /// </summary>
    public static async Task RunAsync(string[] args)
    {
        if (args.Length < 2)
            throw new ArgumentException("Usage: --compare <before.json> <after.json> [--output <path>] [--fail-on-regression <percent>] [--metric time|allocated|request-allocated|peak] [--percentile p50|p95].");

        string? outputPath = null;
        double? threshold = null;
        var metric = "all";
        string? percentile = null;
        for (var index = 2; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--output" when index + 1 < args.Length && !string.IsNullOrWhiteSpace(args[++index]):
                    outputPath = args[index];
                    break;
                case "--fail-on-regression" when index + 1 < args.Length &&
                    double.TryParse(args[++index], System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out var parsedThreshold) &&
                    parsedThreshold >= 0 && !double.IsNaN(parsedThreshold) && !double.IsInfinity(parsedThreshold):
                    threshold = parsedThreshold;
                    break;
                case "--metric" when index + 1 < args.Length &&
                    (args[index + 1] is "time" or "allocated" or "request-allocated" or "peak" or "all"):
                    metric = args[++index];
                    break;
                case "--percentile" when index + 1 < args.Length &&
                    (args[index + 1] is "p50" or "p95"):
                    percentile = args[++index];
                    break;
                default:
            throw new ArgumentException("Compare options must use --output, --fail-on-regression, --metric, and --percentile.");
            }
        }

        var beforePath = Path.GetFullPath(args[0]);
        var afterPath = Path.GetFullPath(args[1]);
        var before = await ReadAsync(beforePath);
        var after = await ReadAsync(afterPath);
        ValidateDimensions(before, after);
        ValidateEnvironment(before, after);

        var beforeSamples = before.Samples.OrderBy(item => item.Repetition).ToArray();
        var afterSamples = after.Samples.OrderBy(item => item.Repetition).ToArray();
        if (beforeSamples.Length != afterSamples.Length)
            throw new InvalidDataException("Baseline reports must contain the same number of samples.");

        var samples = new List<BaselineSampleComparison>(beforeSamples.Length);
        for (var index = 0; index < beforeSamples.Length; index++)
        {
            var left = beforeSamples[index];
            var right = afterSamples[index];
            if (left.Repetition != right.Repetition)
                throw new InvalidDataException("Baseline reports must contain matching repetition numbers.");
            samples.Add(new BaselineSampleComparison(
                left.Repetition,
                CompareMetric(left.ElapsedMilliseconds, right.ElapsedMilliseconds),
                CompareMetric(left.AllocatedMiB, right.AllocatedMiB),
                CompareMetric(RequestAllocated(left, before), RequestAllocated(right, after)),
                CompareMetric(left.PeakHeapMiB, right.PeakHeapMiB),
                left.MaxActualParallelism,
                right.MaxActualParallelism,
                left.MaxQueuedRequests,
                right.MaxQueuedRequests));
        }

        var percentileComparisons = new[] { "p50", "p95" }
            .Select(name => ComparePercentile(name, samples)).ToArray();
        var failures = threshold.HasValue
            ? percentile == null
                ? FindRegressionFailures(samples, threshold.Value, metric)
                : FindPercentileRegressionFailures(percentileComparisons, percentile, threshold.Value, metric)
            : Array.Empty<BaselineRegressionFailure>();
        var report = new BaselineComparisonReport(
            beforePath,
            afterPath,
            new BaselineComparisonDimensions(before.Mode, before.TargetFramework, before.SizeMiB,
                before.Concurrency, before.Repetitions, before.IterationCount),
            new BaselineEnvironment(before.TargetFramework, before.Runtime, before.RuntimeVersion,
                before.OperatingSystem, before.OperatingSystemArchitecture, before.ProcessorCount,
                before.ServerGarbageCollection),
            samples, percentileComparisons, threshold, metric, percentile, failures);
        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine;
        if (outputPath == null)
        {
            Console.Write(json);
        }
        else
        {
            var fullOutputPath = Path.GetFullPath(outputPath);
            var outputDirectory = Path.GetDirectoryName(fullOutputPath);
            if (!string.IsNullOrEmpty(outputDirectory)) Directory.CreateDirectory(outputDirectory);
            await File.WriteAllTextAsync(fullOutputPath, json);
            Console.WriteLine($"comparison,{fullOutputPath}");
        }

        if (failures.Count > 0)
        {
            foreach (var failure in failures)
                Console.Error.WriteLine($"regression,statistic={failure.Statistic},repetition={failure.Repetition},metric={failure.Metric},percent={failure.PercentChange:F2}");
            Environment.ExitCode = 1;
        }
    }

    /// <summary>

    /// 从文件异步读取基线报告。

    /// </summary>
    /// <param name="path">基线报告文件路径。</param>
    /// <returns>反序列化后的基线报告。</returns>
    private static async Task<BaselineReport> ReadAsync(string path)
    {
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<BaselineReport>(stream)
            ?? throw new InvalidDataException($"Baseline report is empty: {path}");
    }

    /// <summary>

    /// 验证两份报告使用相同的测量输入维度。

    /// </summary>
    /// <param name="before">原始基线报告。</param>
    /// <param name="after">新基线报告。</param>
    private static void ValidateDimensions(BaselineReport before, BaselineReport after)
    {
        if (!string.Equals(before.Mode, after.Mode, StringComparison.Ordinal) ||
            !string.Equals(before.TargetFramework, after.TargetFramework, StringComparison.Ordinal) ||
            before.SizeMiB != after.SizeMiB ||
            before.Concurrency != after.Concurrency ||
            before.Repetitions != after.Repetitions ||
            before.IterationCount != after.IterationCount)
            throw new InvalidDataException("Baseline reports have incompatible mode, target framework, input, concurrency, repetitions, or iteration dimensions.");
    }

    /// <summary>

    /// 验证两份报告来自兼容的运行环境。

    /// </summary>
    /// <param name="before">原始基线报告。</param>
    /// <param name="after">新基线报告。</param>
    private static void ValidateEnvironment(BaselineReport before, BaselineReport after)
    {
        if (!string.Equals(before.Runtime, after.Runtime, StringComparison.Ordinal) ||
            !string.Equals(before.RuntimeVersion, after.RuntimeVersion, StringComparison.Ordinal) ||
            !string.Equals(before.OperatingSystem, after.OperatingSystem, StringComparison.Ordinal) ||
            !string.Equals(before.OperatingSystemArchitecture, after.OperatingSystemArchitecture, StringComparison.Ordinal) ||
            before.ProcessorCount != after.ProcessorCount ||
            before.ServerGarbageCollection != after.ServerGarbageCollection)
            throw new InvalidDataException("Baseline reports have incompatible runtime, OS, architecture, processor count, or GC mode.");
    }

    /// <summary>

    /// 获取单个请求的分摊分配量，并兼容旧版报告。

    /// </summary>
    /// <param name="sample">本轮样本。</param>
    /// <param name="report">样本所属报告。</param>
    /// <returns>以 MiB 表示的单请求分配量。</returns>
    private static double RequestAllocated(BaselineSample sample, BaselineReport report)
        => sample.RequestAllocatedMiB > 0
            ? sample.RequestAllocatedMiB
            : sample.AllocatedMiB / Math.Max(1, report.IterationCount);

    /// <summary>

    /// 计算数值指标的前后变化量和百分比。

    /// </summary>
    /// <param name="before">原始测量值。</param>
    /// <param name="after">新测量值。</param>
    /// <returns>指标比较结果。</returns>
    private static BaselineMetricComparison CompareMetric(double before, double after)
    {
        var absoluteChange = after - before;
        double? percentChange = before == 0 ? (after == 0 ? 0 : null) : absoluteChange / before * 100;
        return new BaselineMetricComparison(before, after, absoluteChange, percentChange);
    }

    /// <summary>

    /// 查找单轮样本中超过阈值的指标回归。

    /// </summary>
    private static IReadOnlyList<BaselineRegressionFailure> FindRegressionFailures(
        IReadOnlyList<BaselineSampleComparison> samples, double threshold, string metric)
    {
        var failures = new List<BaselineRegressionFailure>();
        foreach (var sample in samples)
        {
            AddFailure(failures, "sample", "time", sample.ElapsedMilliseconds.PercentChange, threshold, metric, sample.Repetition);
            AddFailure(failures, "sample", "allocated", sample.AllocatedMiB.PercentChange, threshold, metric, sample.Repetition);
            AddFailure(failures, "sample", "request-allocated", sample.RequestAllocatedMiB.PercentChange, threshold, metric, sample.Repetition);
            AddFailure(failures, "sample", "peak", sample.PeakHeapMiB.PercentChange, threshold, metric, sample.Repetition);
        }
        return failures;
    }

    /// <summary>

    /// 计算指定分位数的各项基线指标比较结果。

    /// </summary>
    private static BaselinePercentileComparison ComparePercentile(
        string name, IReadOnlyList<BaselineSampleComparison> samples)
    {
        var elapsed = samples.Select(sample => sample.ElapsedMilliseconds).ToArray();
        var allocated = samples.Select(sample => sample.AllocatedMiB).ToArray();
        var peak = samples.Select(sample => sample.PeakHeapMiB).ToArray();
        return new BaselinePercentileComparison(
            name,
            CompareMetric(Percentile(elapsed.Select(item => item.Before), name),
                Percentile(elapsed.Select(item => item.After), name)),
            CompareMetric(Percentile(allocated.Select(item => item.Before), name),
                Percentile(allocated.Select(item => item.After), name)),
            CompareMetric(Percentile(samples.Select(item => item.RequestAllocatedMiB.Before), name),
                Percentile(samples.Select(item => item.RequestAllocatedMiB.After), name)),
            CompareMetric(Percentile(peak.Select(item => item.Before), name),
                Percentile(peak.Select(item => item.After), name)));
    }

    /// <summary>

    /// 计算样本序列的中位数或第 95 百分位数。

    /// </summary>
    /// <param name="values">待统计的测量值。</param>
    /// <param name="name">分位数名称。</param>
    /// <returns>插值计算后的分位数。</returns>
    private static double Percentile(IEnumerable<double> values, string name)
    {
        var ordered = values.OrderBy(value => value).ToArray();
        if (ordered.Length == 0) throw new InvalidDataException("Baseline reports must contain samples.");
        var rank = (name == "p95" ? 0.95 : 0.50) * (ordered.Length - 1);
        var lower = (int)Math.Floor(rank);
        var upper = (int)Math.Ceiling(rank);
        if (lower == upper) return ordered[lower];
        return ordered[lower] + (ordered[upper] - ordered[lower]) * (rank - lower);
    }

    /// <summary>

    /// 查找选定分位数中超过阈值的指标回归。

    /// </summary>
    private static IReadOnlyList<BaselineRegressionFailure> FindPercentileRegressionFailures(
        IReadOnlyList<BaselinePercentileComparison> percentiles, string selectedPercentile,
        double threshold, string metric)
    {
        var selected = percentiles.Single(item => item.Percentile == selectedPercentile);
        var failures = new List<BaselineRegressionFailure>();
        AddFailure(failures, selected.Percentile, "time", selected.ElapsedMilliseconds.PercentChange, threshold, metric);
        AddFailure(failures, selected.Percentile, "allocated", selected.AllocatedMiB.PercentChange, threshold, metric);
        AddFailure(failures, selected.Percentile, "request-allocated", selected.RequestAllocatedMiB.PercentChange, threshold, metric);
        AddFailure(failures, selected.Percentile, "peak", selected.PeakHeapMiB.PercentChange, threshold, metric);
        return failures;
    }

    /// <summary>

    /// 将符合阈值和指标筛选条件的回归加入结果集合。

    /// </summary>
    private static void AddFailure(List<BaselineRegressionFailure> failures, string statistic, string name,
        double? percent, double threshold, string selectedMetric, int repetition = 0)
    {
        if (percent.HasValue && percent.Value > threshold && (selectedMetric == "all" || selectedMetric == name))
            failures.Add(new BaselineRegressionFailure(repetition, name, percent.Value, statistic));
    }
}
