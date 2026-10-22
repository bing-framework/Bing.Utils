using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bing.Drawing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace Bing.Utils.Benchmark.Benchmarks;

/// <summary>
/// 为 ImageSharp 和 SkiaSharp 提供同一组输入、缩放和编码参数的性能探针。
/// </summary>
internal static class ImagePerformanceProbe
{
    private const int OutputWidth = 256;
    private const int MeasureRounds = 3;
    private const int WarmupBatches = 2;
    private const int MeasuredBatches = 5;
    private const int BatchSize = 8;
    private const int InputSeed = 42;

    /// <summary>
    /// 执行图像性能探针。
    /// </summary>
    /// <param name="args">包含性能模式、输入目录和报告路径的命令行参数。</param>
    internal static void Run(string[] args)
    {
        if (args is null)
            throw new ArgumentNullException(nameof(args));

        var mode = GetRequiredOption(args, "--image-performance");
        var inputDirectory = GetRequiredOption(args, "--image-inputs");
        var outputPath = GetRequiredOption(args, "--image-output");
        if (!string.Equals(mode, "smoke", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(mode, "measure", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("--image-performance 必须是 smoke 或 measure。", nameof(args));
        if (!Path.IsPathFullyQualified(inputDirectory))
            throw new ArgumentException("--image-inputs 必须是绝对目录。", nameof(args));
        if (!Path.IsPathFullyQualified(outputPath))
            throw new ArgumentException("--image-output 必须是绝对文件路径。", nameof(args));

        var inputs = EnsureInputs(inputDirectory);
        var scenarios = BuildScenarios(inputs);
        var scenarioReports = string.Equals(mode, "smoke", StringComparison.OrdinalIgnoreCase)
            ? RunSmoke(scenarios)
            : RunMeasure(scenarios);
        var report = new PerformanceReport
        {
            SchemaVersion = 1,
            Mode = mode.ToLowerInvariant(),
            Runtime = RuntimeInformation.FrameworkDescription,
            Os = RuntimeInformation.OSDescription,
            Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
            Rounds = string.Equals(mode, "measure", StringComparison.OrdinalIgnoreCase) ? MeasureRounds : 0,
            Inputs = inputs.Select(input => new InputReport { Name = input.Name, Sha256 = input.Sha256 }).ToList(),
            Scenarios = scenarioReports
        };

        WriteReport(outputPath, report);
    }

    private static string GetRequiredOption(IReadOnlyList<string> args, string name)
    {
        var prefix = name + "=";
        var values = args
            .Where(arg => arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(arg => arg[prefix.Length..])
            .ToArray();
        if (values.Length != 1 || string.IsNullOrWhiteSpace(values[0]))
            throw new ArgumentException($"必须且只能指定一次 {name}=...。", nameof(args));
        return values[0];
    }

    private static IReadOnlyList<InputImage> EnsureInputs(string directory)
    {
        Directory.CreateDirectory(directory);
        var specs = new[]
        {
            new InputSpec("512x512.jpeg", 512, 512, ImageOutputFormat.Jpeg),
            new InputSpec("512x512.png", 512, 512, ImageOutputFormat.Png),
            new InputSpec("1920x1080.jpeg", 1920, 1080, ImageOutputFormat.Jpeg),
            new InputSpec("1920x1080.png", 1920, 1080, ImageOutputFormat.Png)
        };
        var inputs = new List<InputImage>(specs.Length);
        foreach (var spec in specs)
        {
            var path = Path.Combine(directory, spec.Name);
            var bytes = File.Exists(path) ? File.ReadAllBytes(path) : CreateInput(spec);
            if (!File.Exists(path))
                File.WriteAllBytes(path, bytes);

            var info = ImageSharpHelper.Identify(bytes);
            if (info.Width != spec.Width || info.Height != spec.Height || info.Format != spec.Format)
                throw new InvalidDataException($"输入 {spec.Name} 的格式或尺寸不符合固定探针输入。" +
                                                $" 实际为 {info.Width}x{info.Height} {FormatName(info.Format)}。");
            inputs.Add(new InputImage(spec, bytes, Convert.ToHexString(SHA256.HashData(bytes))));
        }

        return inputs;
    }

    private static byte[] CreateInput(InputSpec spec)
    {
        using var image = new Image<Rgba32>(spec.Width, spec.Height);
        for (var y = 0; y < spec.Height; y++)
        {
            for (var x = 0; x < spec.Width; x++)
            {
                var value = CreateSeededValue(x, y);
                image[x, y] = new Rgba32(
                    (byte)value,
                    (byte)(value >> 8),
                    (byte)(value >> 16),
                    byte.MaxValue);
            }
        }

        return spec.Format == ImageOutputFormat.Jpeg
            ? ImageSharpHelper.ToBytes(image, JpegFormat.Instance, 80)
            : ImageSharpHelper.ToBytes(image, PngFormat.Instance);
    }

    private static uint CreateSeededValue(int x, int y)
    {
        var value = (uint)InputSeed;
        value ^= (uint)x * 0x9E3779B9u;
        value ^= (uint)y * 0x85EBCA6Bu;
        value ^= value >> 13;
        value *= 0xC2B2AE35u;
        value ^= value >> 16;
        return value;
    }

    private static IReadOnlyList<Scenario> BuildScenarios(IReadOnlyList<InputImage> inputs)
    {
        var scenarios = new List<Scenario>(8);
        foreach (var backend in new[] { "ImageSharp", "SkiaSharp" })
        {
            foreach (var input in inputs)
            {
                var options = new ImageProcessOptions
                {
                    Resize = new ImageResizeOptions
                    {
                        Width = OutputWidth,
                        Height = input.OutputHeight,
                        Mode = ImageResizeMode.Contain,
                        AllowEnlarge = false
                    },
                    Format = input.Spec.Format,
                    Quality = 80,
                    RemoveMetadata = true
                };
                scenarios.Add(new Scenario(
                    $"{backend}-{input.Spec.Width}x{input.Spec.Height}-{FormatName(input.Spec.Format)}",
                    backend,
                    input,
                    options));
            }
        }

        return scenarios;
    }

    private static IReadOnlyList<ScenarioReport> RunSmoke(IReadOnlyList<Scenario> scenarios)
    {
        var reports = new List<ScenarioReport>(scenarios.Count);
        foreach (var scenario in scenarios)
        {
            var result = Process(scenario);
            GC.KeepAlive(result.Bytes);
            GC.KeepAlive(result);
            ValidateSmokeResult(scenario, result);
            reports.Add(CreateScenarioReport(scenario, 0, 0, new List<RoundReport>()));
        }

        return reports;
    }

    private static void ValidateSmokeResult(Scenario scenario, ImageProcessResult result)
    {
        if (result.Bytes is null || result.Bytes.Length == 0)
            throw new InvalidDataException($"场景 {scenario.Id} 未生成编码图像。");
        if (result.Format != scenario.Input.Spec.Format || result.Width != OutputWidth ||
            result.Height != scenario.Input.OutputHeight)
            throw new InvalidDataException($"场景 {scenario.Id} 的处理结果尺寸或格式不正确。");

        var info = ImageSharpHelper.Identify(result.Bytes);
        if (info.Format != scenario.Input.Spec.Format || info.Width != OutputWidth ||
            info.Height != scenario.Input.OutputHeight)
            throw new InvalidDataException($"场景 {scenario.Id} 的编码头部校验失败。");

        if (string.Equals(scenario.Backend, "ImageSharp", StringComparison.Ordinal))
        {
            var image = ImageSharpHelper.FromBytes(result.Bytes);
            if (image is null)
                throw new InvalidDataException($"场景 {scenario.Id} 的 ImageSharp 输出无法解码。");
            using (image)
            {
                if (image.Width != OutputWidth || image.Height != scenario.Input.OutputHeight)
                    throw new InvalidDataException($"场景 {scenario.Id} 的 ImageSharp 解码尺寸不正确。");
            }
        }
        else
        {
            var image = SkiaSharpHelper.FromBytes(result.Bytes);
            if (image is null)
                throw new InvalidDataException($"场景 {scenario.Id} 的 SkiaSharp 输出无法解码。");
            using (image)
            {
                if (image.Width != OutputWidth || image.Height != scenario.Input.OutputHeight)
                    throw new InvalidDataException($"场景 {scenario.Id} 的 SkiaSharp 解码尺寸不正确。");
            }
        }
    }

    private static IReadOnlyList<ScenarioReport> RunMeasure(IReadOnlyList<Scenario> scenarios)
    {
        var reports = new List<ScenarioReport>(scenarios.Count);
        foreach (var scenario in scenarios)
        {
            var rounds = new List<RoundReport>(MeasureRounds);
            for (var round = 1; round <= MeasureRounds; round++)
            {
                for (var warmup = 0; warmup < WarmupBatches; warmup++)
                    RunBatch(scenario);

                var samples = new List<SampleReport>(MeasuredBatches);
                for (var batch = 0; batch < MeasuredBatches; batch++)
                {
                    var allocatedBefore = GC.GetTotalAllocatedBytes(true);
                    var started = Stopwatch.GetTimestamp();
                    for (var item = 0; item < BatchSize; item++)
                    {
                        var result = Process(scenario);
                        GC.KeepAlive(result.Bytes);
                        GC.KeepAlive(result);
                    }

                    var elapsedMs = ToMilliseconds(Stopwatch.GetTimestamp() - started);
                    var allocatedBytes = GC.GetTotalAllocatedBytes(true) - allocatedBefore;
                    samples.Add(new SampleReport { TimeMs = elapsedMs, AllocatedBytes = allocatedBytes });
                }

                rounds.Add(new RoundReport
                {
                    Round = round,
                    TimeMs = Median(samples.Select(sample => sample.TimeMs)),
                    AllocatedBytes = Median(samples.Select(sample => sample.AllocatedBytes)),
                    Samples = samples
                });
            }

            reports.Add(CreateScenarioReport(
                scenario,
                Median(rounds.Select(round => round.TimeMs)),
                Median(rounds.Select(round => round.AllocatedBytes)),
                rounds));
        }

        return reports;
    }

    private static void RunBatch(Scenario scenario)
    {
        for (var item = 0; item < BatchSize; item++)
        {
            var result = Process(scenario);
            GC.KeepAlive(result.Bytes);
            GC.KeepAlive(result);
        }
    }

    private static ImageProcessResult Process(Scenario scenario)
    {
        return string.Equals(scenario.Backend, "ImageSharp", StringComparison.Ordinal)
            ? ImageSharpHelper.Process(scenario.Input.Bytes, scenario.Options)
            : SkiaSharpHelper.Process(scenario.Input.Bytes, scenario.Options);
    }

    private static ScenarioReport CreateScenarioReport(
        Scenario scenario,
        double timeMs,
        long allocatedBytes,
        IReadOnlyList<RoundReport> rounds)
    {
        return new ScenarioReport
        {
            Id = scenario.Id,
            Backend = scenario.Backend,
            Width = scenario.Input.Spec.Width,
            Height = scenario.Input.Spec.Height,
            Format = FormatName(scenario.Input.Spec.Format),
            TimeMs = timeMs,
            AllocatedBytes = allocatedBytes,
            RoundResults = rounds
        };
    }

    private static double ToMilliseconds(long ticks) => ticks * 1000d / Stopwatch.Frequency;

    private static double Median(IEnumerable<double> values)
    {
        var ordered = values.ToArray();
        Array.Sort(ordered);
        return ordered[ordered.Length / 2];
    }

    private static long Median(IEnumerable<long> values)
    {
        var ordered = values.ToArray();
        Array.Sort(ordered);
        return ordered[ordered.Length / 2];
    }

    private static string FormatName(ImageOutputFormat format) => format switch
    {
        ImageOutputFormat.Jpeg => "Jpeg",
        ImageOutputFormat.Png => "Png",
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "探针仅支持 JPEG 和 PNG。")
    };

    private static void WriteReport(string outputPath, PerformanceReport report)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(outputPath, json, new UTF8Encoding(false));
        Console.WriteLine($"Image {report.Mode}: {report.Scenarios.Count} scenarios; {outputPath}");
    }

    private sealed class InputSpec
    {
        public InputSpec(string name, int width, int height, ImageOutputFormat format)
        {
            Name = name;
            Width = width;
            Height = height;
            Format = format;
        }

        public string Name { get; }
        public int Width { get; }
        public int Height { get; }
        public ImageOutputFormat Format { get; }
    }

    private sealed class InputImage
    {
        public InputImage(InputSpec spec, byte[] bytes, string sha256)
        {
            Spec = spec;
            Bytes = bytes;
            Sha256 = sha256;
        }

        public InputSpec Spec { get; }
        public byte[] Bytes { get; }
        public string Sha256 { get; }
        public int OutputHeight => checked(Spec.Height * OutputWidth / Spec.Width);
        public string Name => Spec.Name;
    }

    private sealed class Scenario
    {
        public Scenario(string id, string backend, InputImage input, ImageProcessOptions options)
        {
            Id = id;
            Backend = backend;
            Input = input;
            Options = options;
        }

        public string Id { get; }
        public string Backend { get; }
        public InputImage Input { get; }
        public ImageProcessOptions Options { get; }
    }

    private sealed class PerformanceReport
    {
        [JsonPropertyName("schemaVersion")]
        public int SchemaVersion { get; set; }

        [JsonPropertyName("mode")]
        public string Mode { get; set; } = string.Empty;

        [JsonPropertyName("runtime")]
        public string Runtime { get; set; } = string.Empty;

        [JsonPropertyName("os")]
        public string Os { get; set; } = string.Empty;

        [JsonPropertyName("architecture")]
        public string Architecture { get; set; } = string.Empty;

        [JsonPropertyName("rounds")]
        public int Rounds { get; set; }

        [JsonPropertyName("inputs")]
        public List<InputReport> Inputs { get; set; } = new();

        [JsonPropertyName("scenarios")]
        public IReadOnlyList<ScenarioReport> Scenarios { get; set; } = Array.Empty<ScenarioReport>();
    }

    private sealed class InputReport
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("sha256")]
        public string Sha256 { get; set; } = string.Empty;
    }

    private sealed class ScenarioReport
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("backend")]
        public string Backend { get; set; } = string.Empty;

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("format")]
        public string Format { get; set; } = string.Empty;

        [JsonPropertyName("timeMs")]
        public double TimeMs { get; set; }

        [JsonPropertyName("allocatedBytes")]
        public long AllocatedBytes { get; set; }

        [JsonPropertyName("roundResults")]
        public IReadOnlyList<RoundReport> RoundResults { get; set; } = Array.Empty<RoundReport>();
    }

    private sealed class RoundReport
    {
        [JsonPropertyName("round")]
        public int Round { get; set; }

        [JsonPropertyName("timeMs")]
        public double TimeMs { get; set; }

        [JsonPropertyName("allocatedBytes")]
        public long AllocatedBytes { get; set; }

        [JsonPropertyName("samples")]
        public IReadOnlyList<SampleReport> Samples { get; set; } = Array.Empty<SampleReport>();
    }

    private sealed class SampleReport
    {
        [JsonPropertyName("timeMs")]
        public double TimeMs { get; set; }

        [JsonPropertyName("allocatedBytes")]
        public long AllocatedBytes { get; set; }
    }
}
