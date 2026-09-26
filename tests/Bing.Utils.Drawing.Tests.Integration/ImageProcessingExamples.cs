using Bing.Drawing;

namespace Bing.Utils.Drawing.Tests.Integration;

/// <summary>
/// 提供随集成项目编译的图像处理调用示例。
/// </summary>
public static class ImageProcessingExamples
{
    /// <summary>
    /// 压缩上传流并返回处理结果。
    /// </summary>
    /// <param name="upload">待处理的上传流。</param>
    /// <returns>图像处理结果。</returns>
    public static ImageProcessResult CompressUpload(Stream upload) => ImageSharpHelper.Process(upload,
        new ImageProcessOptions { Format = ImageOutputFormat.Jpeg, TargetSizeBytes = 200 * 1024,
            Resize = new ImageResizeOptions { Width = 1600, Height = 1600 } });

    /// <summary>
    /// 居中裁剪头像并允许放大到固定尺寸。
    /// </summary>
    /// <param name="upload">待处理的图像字节数组。</param>
    /// <returns>图像处理结果。</returns>
    public static ImageProcessResult CreateAvatar(byte[] upload) => ImageSharpHelper.Process(upload,
        new ImageProcessOptions { Resize = new ImageResizeOptions { Width = 256, Height = 256,
            Mode = ImageResizeMode.Cover, AllowEnlarge = true }, CornerRadius = 128 });

    /// <summary>
    /// 使用指定字体为图像添加中文水印。
    /// </summary>
    /// <param name="image">待处理的图像字节数组。</param>
    /// <param name="fontPath">中文字体文件路径。</param>
    /// <returns>图像处理结果。</returns>
    public static ImageProcessResult AddChineseWatermark(byte[] image, string fontPath) => ImageSharpHelper.Process(image,
        new ImageProcessOptions { Watermarks = new List<ImageWatermarkOptions> { new()
        { Text = new ImageTextOptions { Text = "图片示例", FontPath = fontPath, FontSize = 24,
            Color = new RgbColor(255, 255, 255) }, Opacity = .7f, Margin = 16 } } });

    /// <summary>
    /// 拼接多张图像、添加标注并保存结果。
    /// </summary>
    /// <param name="images">待拼接的图像字节数组。</param>
    /// <param name="path">结果文件路径。</param>
    public static void ComposeAndAnnotate(IReadOnlyList<byte[]> images, string path)
    {
        var result = ImageSharpHelper.Compose(images, new ImageComposeOptions { Columns = 2,
            CellWidth = 320, CellHeight = 240, Spacing = 12, Padding = 12,
            Output = new ImageProcessOptions { Annotations = new List<ImageAnnotation> { new()
            { Kind = ImageAnnotationKind.Rectangle, X = 12, Y = 12, Width = 100, Height = 80,
                Color = new RgbColor(255, 0, 0), StrokeWidth = 3 } } } });
        ImageSharpHelper.Save(result, path);
    }
}
