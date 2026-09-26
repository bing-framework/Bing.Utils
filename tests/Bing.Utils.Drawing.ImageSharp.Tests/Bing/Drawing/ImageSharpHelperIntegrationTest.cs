using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace Bing.Drawing;

/// <summary>
/// 验证 ImageSharp 真实处理链路的集成行为。
/// </summary>
public class ImageSharpHelperIntegrationTest
{
    /// <summary>
    /// 验证读取、处理和输出完整链路可稳定执行并清理临时目录。
    /// </summary>
    [Fact]
    public void ProcessingPipeline_ReadProcessWrite_RoundTripAndCleanup()
    {
        using var temp = new TempDirectory("bing-utils-imagesharp-int");
        var inputPath = Path.Combine(temp.Path, "input.png");
        var outputPath = Path.Combine(temp.Path, "output.jpg");

        using var source = CreatePatternImage(64, 32);
        File.WriteAllBytes(inputPath, ImageSharpHelper.ToBytes(source, PngFormat.Instance));

        using var loaded = ImageSharpHelper.FromFile(inputPath);
        loaded.ShouldNotBeNull();

        using var withOpacity = ImageSharpHelper.SetOpacity(loaded!, 0.6f);
        var dataUrl = ImageSharpHelper.ToDataUrl(withOpacity, JpegFormat.Instance);
        dataUrl.ShouldStartWith("data:image/jpeg;base64,");

        using var restored = ImageSharpHelper.FromDataUrl(dataUrl);
        restored.ShouldNotBeNull();
        restored!.Width.ShouldBe(64);
        restored.Height.ShouldBe(32);

        var outputBytes = ImageSharpHelper.ToBytes(restored, JpegFormat.Instance);
        File.WriteAllBytes(outputPath, outputBytes);

        File.Exists(outputPath).ShouldBeTrue();
        new FileInfo(outputPath).Length.ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// 验证不同输出格式生成对应的文件签名。
    /// </summary>
    [Fact]
    public void OutputFormat_PngAndJpeg_HaveExpectedMagicBytes()
    {
        using var source = CreatePatternImage(20, 20);

        var pngBytes = ImageSharpHelper.ToBytes(source, PngFormat.Instance);
        var jpegBytes = ImageSharpHelper.ToBytes(source, JpegFormat.Instance);

        pngBytes.Length.ShouldBeGreaterThan(8);
        pngBytes[0].ShouldBe((byte)0x89);
        pngBytes[1].ShouldBe((byte)0x50);
        pngBytes[2].ShouldBe((byte)0x4E);
        pngBytes[3].ShouldBe((byte)0x47);

        jpegBytes.Length.ShouldBeGreaterThan(4);
        jpegBytes[0].ShouldBe((byte)0xFF);
        jpegBytes[1].ShouldBe((byte)0xD8);
    }

    /// <summary>
    /// 验证非法文件内容和非法流会抛出 <see cref="InvalidDataException" />。
    /// </summary>
    [Fact]
    public void InvalidInput_FileAndStream_ThrowsInvalidDataException()
    {
        using var temp = new TempDirectory("bing-utils-imagesharp-invalid");
        var invalidPath = Path.Combine(temp.Path, "invalid.png");
        File.WriteAllText(invalidPath, "not-an-image", Encoding.UTF8);

        Should.Throw<InvalidDataException>(() => ImageSharpHelper.FromFile(invalidPath));

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("plain-text"));
        Should.Throw<InvalidDataException>(() => ImageSharpHelper.FromStream(stream));
    }

    /// <summary>
    /// 创建用于集成测试的图案图像。
    /// </summary>
    /// <param name="width">图像宽度。</param>
    /// <param name="height">图像高度。</param>
    /// <returns>填充确定性图案的图像。</returns>
    private static Image<Rgba32> CreatePatternImage(int width, int height)
    {
        var image = new Image<Rgba32>(width, height);
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                image[x, y] = new Rgba32((byte)(x * 3 % 255), (byte)(y * 7 % 255), (byte)((x + y) * 5 % 255), 255);
            }
        }
        return image;
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
