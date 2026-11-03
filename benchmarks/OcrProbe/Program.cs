using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Bing.Ocr;
using Bing.Ocr.PaddleServing;
using Bing.Ocr.SkiaSharp;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;
if (args.Length != 2)
{
    Console.WriteLine("dotnet run --project benchmarks/OcrProbe -c Release -- dataset.json results.json");
    Console.WriteLine("设置 OCR_ENDPOINT 与 OCR_MODEL_PROFILE；dataset 为 [{image, expected, group}]，图片路径相对于 dataset。");
    return;
}
var endpoint = Environment.GetEnvironmentVariable("OCR_ENDPOINT") ?? throw new InvalidOperationException("缺少 OCR_ENDPOINT。");
var profile = Environment.GetEnvironmentVariable("OCR_MODEL_PROFILE") ?? throw new InvalidOperationException("缺少 OCR_MODEL_PROFILE。");
var datasetPath = Path.GetFullPath(args[0]);
var samples = JsonSerializer.Deserialize<List<Sample>>(await File.ReadAllTextAsync(datasetPath, Encoding.UTF8),
    new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? throw new InvalidDataException("数据集为空。");
if (samples.Count == 0 || samples.Any(x => string.IsNullOrWhiteSpace(x.Image) || x.Expected == null || string.IsNullOrWhiteSpace(x.Group)))
    throw new InvalidDataException("数据集必须包含 image、expected 和 group。");
using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = Timeout.InfiniteTimeSpan };
var service = new OcrService(new PaddleServingOcrProvider(client, new PaddleServingOptions
{
    Endpoint = new Uri(endpoint), ExpectedModelProfileId = profile
}), new SkiaOcrImagePreprocessor());
using var cancelled = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancelled.Cancel(); };
string ImagePath(Sample sample) => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(datasetPath)!, sample.Image));
// 首次调用单独记录，后续测量不把预热混入 P95。
var coldWatch = Stopwatch.StartNew();
await service.RecognizeFileAsync(ImagePath(samples[0]), cancellationToken: cancelled.Token);
var firstCallMs = coldWatch.Elapsed.TotalMilliseconds;
var rows = new List<Measurement>();
foreach (var sample in samples)
{
    var watch = Stopwatch.StartNew();
    var result = await service.RecognizeFileAsync(ImagePath(sample), cancellationToken: cancelled.Token);
    rows.Add(new Measurement(sample.Image, sample.Group, sample.Expected, result.Text,
        CharacterDistance(sample.Expected, result.Text), sample.Expected.EnumerateRunes().Count(),
        watch.Elapsed.TotalMilliseconds, result.ModelProfileId!, result.ModelIdentityVerified));
}
var report = new
{
    schemaVersion = 1, measuredUtc = DateTimeOffset.UtcNow, modelProfileId = profile,
    count = rows.Count, firstCallMs, concurrency = 1,
    normalization = "None: Unicode scalar values, spaces/punctuation/case/newlines preserved",
    groups = rows.GroupBy(x => x.Group).Select(group => new
    {
        group = group.Key, count = group.Count(),
        edits = group.Sum(x => x.Edits), referenceCharacters = group.Sum(x => x.ReferenceCharacters),
        cer = group.Sum(x => x.ReferenceCharacters) == 0 ? (double?)null : (double)group.Sum(x => x.Edits) / group.Sum(x => x.ReferenceCharacters),
        p95Ms = group.Select(x => x.Milliseconds).OrderBy(x => x).ElementAt((int)Math.Ceiling(group.Count() * .95) - 1)
    }), rows
};
await File.WriteAllTextAsync(args[1], JsonSerializer.Serialize(report, new JsonSerializerOptions
{ WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }), Encoding.UTF8);
Console.WriteLine($"已测量 {rows.Count} 张；报告：{Path.GetFullPath(args[1])}");

// 用 Unicode 标量而不是 UTF-16 单元计算 CER；逐行动态规划只保留两行。
static int CharacterDistance(string expected, string actual)
{
    var left = expected.EnumerateRunes().ToArray(); var right = actual.EnumerateRunes().ToArray();
    var previous = Enumerable.Range(0, right.Length + 1).ToArray();
    var current = new int[right.Length + 1];
    for (var i = 1; i <= left.Length; i++)
    {
        current[0] = i;
        for (var j = 1; j <= right.Length; j++)
            current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1),
                previous[j - 1] + (left[i - 1] == right[j - 1] ? 0 : 1));
        (previous, current) = (current, previous);
    }
    return previous[right.Length];
}
internal sealed record Sample(string Image, string Expected, string Group);
internal sealed record Measurement(string Image, string Group, string Expected, string Actual, int Edits,
    int ReferenceCharacters, double Milliseconds, string ModelProfileId, bool ModelIdentityVerified);
