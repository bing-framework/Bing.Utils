using System.Text;
using SkiaSharp;

namespace Bing.Drawing;

/// <summary>
/// 验证 SkiaSharp 真实处理链路的集成行为。
/// </summary>
public class SkiaSharpHelperIntegrationTest
{
    /// <summary>
    /// 验证读取、处理和输出完整链路可稳定执行并清理临时目录。
    /// </summary>
    [Fact]
    public void ProcessingPipeline_ReadProcessWrite_RoundTripAndCleanup()
    {
        using var temp = new TempDirectory("bing-utils-skiasharp-int");
        var inputPath = Path.Combine(temp.Path, "input.png");
        var outputPath = Path.Combine(temp.Path, "output.jpg");

        using var source = CreatePatternImage(64, 32);
        File.WriteAllBytes(inputPath, SkiaSharpHelper.ToBytes(source, (SKEncodedImageFormat.Png, 100)));

        using var loaded = SkiaSharpHelper.FromFile(inputPath);
        loaded.ShouldNotBeNull();

        using var withOpacity = SkiaSharpHelper.SetOpacity(loaded!, 0.6f);
        var dataUrlFromHelper = SkiaSharpHelper.ToDataUrl(withOpacity, (SKEncodedImageFormat.Jpeg, 90));
        dataUrlFromHelper.ShouldStartWith("data:image/");
        dataUrlFromHelper.ShouldContain("image/jpeg;base64,");

        var base64 = SkiaSharpHelper.ToBase64String(withOpacity, (SKEncodedImageFormat.Jpeg, 90));
        var dataUrl = $"data:image/jpeg;base64,{base64}";

        using var restored = SkiaSharpHelper.FromDataUrl(dataUrl);
        restored.ShouldNotBeNull();
        restored!.Width.ShouldBe(64);
        restored.Height.ShouldBe(32);

        var outputBytes = SkiaSharpHelper.ToBytes(restored, (SKEncodedImageFormat.Jpeg, 90));
        File.WriteAllBytes(outputPath, outputBytes);

        File.Exists(outputPath).ShouldBeTrue();
        new FileInfo(outputPath).Length.ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// 验证 JPEG 质量参数影响输出体积，低质量输出通常更小。
    /// </summary>
    [Fact]
    public void OutputQuality_JpegLowQuality_SmallerThanHighQuality()
    {
        using var source = CreateNoiseImage(256, 256);

        var highQuality = SkiaSharpHelper.ToBytes(source, (SKEncodedImageFormat.Jpeg, 100));
        var lowQuality = SkiaSharpHelper.ToBytes(source, (SKEncodedImageFormat.Jpeg, 25));

        lowQuality.Length.ShouldBeLessThan(highQuality.Length);
    }

    /// <summary>
    /// 验证非法文件内容和非法流会抛出 <see cref="InvalidDataException" />。
    /// </summary>
    [Fact]
    public void InvalidInput_FileAndStream_ThrowsInvalidDataException()
    {
        using var temp = new TempDirectory("bing-utils-skiasharp-invalid");
        var invalidPath = Path.Combine(temp.Path, "invalid.png");
        File.WriteAllText(invalidPath, "not-an-image", Encoding.UTF8);

        Should.Throw<InvalidDataException>(() => SkiaSharpHelper.FromFile(invalidPath));

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("plain-text"));
        Should.Throw<InvalidDataException>(() => SkiaSharpHelper.FromStream(stream));
    }

    /// <summary>
    /// 创建用于集成测试的图案图像。
    /// </summary>
    /// <param name="width">图像宽度。</param>
    /// <param name="height">图像高度。</param>
    /// <returns>填充确定性图案的图像。</returns>
    private static SKImage CreatePatternImage(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                bitmap.SetPixel(x, y, new SKColor((byte)(x * 3 % 255), (byte)(y * 7 % 255), (byte)((x + y) * 5 % 255), 255));
            }
        }
        return SKImage.FromBitmap(bitmap);
    }

    /// <summary>
    /// 创建用于质量比较的确定性噪声图像。
    /// </summary>
    /// <param name="width">图像宽度。</param>
    /// <param name="height">图像高度。</param>
    /// <returns>填充确定性噪声的图像。</returns>
    private static SKImage CreateNoiseImage(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        var random = new Random(42);
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                bitmap.SetPixel(x, y, new SKColor((byte)random.Next(0, 256), (byte)random.Next(0, 256), (byte)random.Next(0, 256), 255));
            }
        }
        return SKImage.FromBitmap(bitmap);
    }

    /// <summary>
    /// 管理测试期间创建的临时目录。
    /// </summary>
    private sealed class TempDirectory : IDisposable
    {
        /// <summary>
        /// 初始化 <see cref="TempDirectory" /> 类的新实例。
        /// </summary>
        /// <param name="prefix">临时目录名称前缀。</param>
        public TempDirectory(string prefix)
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{prefix}-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        /// <summary>
        /// 获取临时目录路径。
        /// </summary>
        public string Path { get; }

        /// <inheritdoc />
        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Path))
                    Directory.Delete(Path, recursive: true);
            }
            catch
            {
                // 保持清理幂等，避免影响测试结果
            }
        }
    }
}
