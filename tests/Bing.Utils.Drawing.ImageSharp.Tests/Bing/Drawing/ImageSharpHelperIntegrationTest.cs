using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace Bing.Drawing;

/// <summary>
/// 验证图像文件读写、处理及编码转换的完整链路。
/// </summary>
public class ImageSharpHelperIntegrationTest
{
    /// <summary>
    /// 验证图像读取、透明度处理、Data URL 转换及文件输出的完整链路。
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
    /// 验证 PNG 和 JPEG 编码输出各自的文件签名。
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
    /// 验证非图像内容的文件和流均返回空加载结果。
    /// </summary>
    [Fact]
    public void InvalidInput_FileAndStream_ReturnNull()
    {
        using var temp = new TempDirectory("bing-utils-imagesharp-invalid");
        var invalidPath = Path.Combine(temp.Path, "invalid.png");
        File.WriteAllText(invalidPath, "not-an-image", Encoding.UTF8);

        var fromFile = ImageSharpHelper.FromFile(invalidPath);
        fromFile.ShouldBeNull();

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("plain-text"));
        var fromStream = ImageSharpHelper.FromStream(stream);
        fromStream.ShouldBeNull();
    }

    /// <summary>
    /// 创建颜色随坐标变化的图像以验证处理链路。
    /// </summary>
    /// <param name="width">图像宽度，单位为像素。</param>
    /// <param name="height">图像高度，单位为像素。</param>
    /// <returns>指定尺寸的不透明 RGBA 图像，调用方负责释放。</returns>
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
    /// 提供测试期间创建及清理的独立临时目录。
    /// </summary>
    private sealed class TempDirectory : IDisposable
    {
        /// <summary>
        /// 初始化一个 <see cref="TempDirectory"/> 类型的实例。
        /// </summary>
        /// <param name="prefix">临时目录名称的前缀。</param>
        public TempDirectory(string prefix)
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{prefix}-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        /// <summary>
        /// 获取当前测试临时目录的完整路径。
        /// </summary>
        public string Path { get; }

        /// <inheritdoc />
        /// <remarks>尝试递归删除临时目录；忽略清理异常以免覆盖测试结果。</remarks>
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
