using System.Text;
using SkiaSharp;

namespace Bing.Drawing;

/// <summary>
/// 验证 SkiaSharp 图像处理流程在文件、流和编码格式之间的集成行为。
/// </summary>
public class SkiaSharpHelperIntegrationTest
{
    /// <summary>
    /// 验证图像经过读取、处理、编码和写入后尺寸正确且临时文件可清理。
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
    /// 验证 JPEG 低质量设置生成的文件小于高质量设置。
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
    /// 验证无效文件内容和流数据均返回空图像结果。
    /// </summary>
    [Fact]
    public void InvalidInput_FileAndStream_ReturnNull()
    {
        using var temp = new TempDirectory("bing-utils-skiasharp-invalid");
        var invalidPath = Path.Combine(temp.Path, "invalid.png");
        File.WriteAllText(invalidPath, "not-an-image", Encoding.UTF8);

        var fromFile = SkiaSharpHelper.FromFile(invalidPath);
        fromFile.ShouldBeNull();

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("plain-text"));
        var fromStream = SkiaSharpHelper.FromStream(stream);
        fromStream.ShouldBeNull();
    }

    /// <summary>
    /// 创建颜色随坐标变化的测试图像。
    /// </summary>
    /// <param name="width">图像宽度。</param>
    /// <param name="height">图像高度。</param>
    /// <returns>按像素坐标生成颜色的图像。</returns>
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
    /// 创建由固定随机种子生成像素的噪声图像。
    /// </summary>
    /// <param name="width">图像宽度。</param>
    /// <param name="height">图像高度。</param>
    /// <returns>包含确定性随机颜色的图像。</returns>
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
    /// 在系统临时目录中创建并清理测试目录。
    /// </summary>
    private sealed class TempDirectory : IDisposable
    {
        /// <summary>
        /// 初始化一个 <see cref="TempDirectory"/> 类型的实例。
        /// </summary>
        /// <param name="prefix">临时目录名称使用的前缀。</param>
        public TempDirectory(string prefix)
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{prefix}-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        /// <summary>
        /// 获取临时目录的完整路径。
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
