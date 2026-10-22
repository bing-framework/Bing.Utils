using System.Collections.Generic;

namespace Bing.Drawing;

/// <summary>
/// 统一图像输出格式。
/// </summary>
public enum ImageOutputFormat
{
    /// <summary>
    /// PNG 格式。
    /// </summary>
    Png,

    /// <summary>
    /// JPEG 格式。
    /// </summary>
    Jpeg,

    /// <summary>
    /// WebP 格式。
    /// </summary>
    WebP
}

/// <summary>
/// 图像缩放模式。
/// </summary>
public enum ImageResizeMode
{
    /// <summary>
    /// 保持比例缩放并完整显示图像。
    /// </summary>
    Contain,

    /// <summary>
    /// 保持比例缩放并填满区域。
    /// </summary>
    Cover,

    /// <summary>
    /// 将图像拉伸到目标尺寸。
    /// </summary>
    Stretch
}

/// <summary>
/// 图像在目标区域中的九宫格定位。
/// </summary>
public enum ImageAnchor
{
    /// <summary>
    /// 左上角。
    /// </summary>
    TopLeft,

    /// <summary>
    /// 顶部居中。
    /// </summary>
    Top,

    /// <summary>
    /// 右上角。
    /// </summary>
    TopRight,

    /// <summary>
    /// 左侧居中。
    /// </summary>
    Left,

    /// <summary>
    /// 中心。
    /// </summary>
    Center,

    /// <summary>
    /// 右侧居中。
    /// </summary>
    Right,

    /// <summary>
    /// 左下角。
    /// </summary>
    BottomLeft,

    /// <summary>
    /// 底部居中。
    /// </summary>
    Bottom,

    /// <summary>
    /// 右下角。
    /// </summary>
    BottomRight
}

/// <summary>
/// 拼图的排列方式。
/// </summary>
public enum ImageComposeLayout
{
    /// <summary>
    /// 水平排列。
    /// </summary>
    Horizontal,

    /// <summary>
    /// 垂直排列。
    /// </summary>
    Vertical,

    /// <summary>
    /// 按列数排列为网格。
    /// </summary>
    Grid
}

/// <summary>
/// 图像标注的形状。
/// </summary>
public enum ImageAnnotationKind
{
    /// <summary>
    /// 矩形。
    /// </summary>
    Rectangle,

    /// <summary>
    /// 椭圆。
    /// </summary>
    Ellipse,

    /// <summary>
    /// 直线。
    /// </summary>
    Line,

    /// <summary>
    /// 箭头。
    /// </summary>
    Arrow,

    /// <summary>
    /// 文字。
    /// </summary>
    Text
}

/// <summary>
/// 图像解码、合成及输出资源限制。
/// </summary>
public sealed class ImageProcessingLimits
{
    /// <summary>
    /// 获取或设置单次处理允许读取的最大输入字节数。
    /// </summary>
    public long MaxInputBytes { get; set; } = 20L * 1024 * 1024;

    /// <summary>
    /// 获取或设置允许处理的最大像素数。
    /// </summary>
    public long MaxPixels { get; set; } = 40_000_000;

    /// <summary>
    /// 获取或设置图像单边允许的最大尺寸。
    /// </summary>
    public int MaxDimension { get; set; } = 16384;

    /// <summary>
    /// 获取或设置允许处理的最大帧数。
    /// </summary>
    public int MaxFrames { get; set; } = 1;

    /// <summary>
    /// 获取或设置拼图允许包含的最大图像数。
    /// </summary>
    public int MaxComposeImages { get; set; } = 16;
}

/// <summary>
/// 定义像素裁剪区域。
/// </summary>
/// <remarks>
/// 坐标原点位于方向纠正后的图像左上角。
/// </remarks>
public sealed class ImageCropOptions
{
    /// <summary>
    /// 获取或设置裁剪区域左上角的横坐标。
    /// </summary>
    public int X { get; set; }

    /// <summary>
    /// 获取或设置裁剪区域左上角的纵坐标。
    /// </summary>
    public int Y { get; set; }

    /// <summary>
    /// 获取或设置裁剪区域的宽度。
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// 获取或设置裁剪区域的高度。
    /// </summary>
    public int Height { get; set; }
}

/// <summary>
/// 定义指定尺寸框中的缩放规则。
/// </summary>
public sealed class ImageResizeOptions
{
    /// <summary>
    /// 获取或设置目标宽度。
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// 获取或设置目标高度。
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// 获取或设置缩放模式。
    /// </summary>
    public ImageResizeMode Mode { get; set; } = ImageResizeMode.Contain;

    /// <summary>
    /// 获取或设置缩放后图像的定位方式。
    /// </summary>
    public ImageAnchor Anchor { get; set; } = ImageAnchor.Center;

    /// <summary>
    /// 获取或设置是否允许放大原图。
    /// </summary>
    public bool AllowEnlarge { get; set; }
}

/// <summary>
/// 定义图像中的文字样式。
/// </summary>
/// <remarks>
/// 字体文件必须包含文字所需的全部字形。
/// </remarks>
public sealed class ImageTextOptions
{
    /// <summary>
    /// 获取或设置要绘制的文字。
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置字体文件路径。
    /// </summary>
    public string FontPath { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置字号。
    /// </summary>
    public float FontSize { get; set; } = 24;

    /// <summary>
    /// 获取或设置文字颜色。
    /// </summary>
    public RgbColor Color { get; set; } = new(0, 0, 0);
}

/// <summary>
/// 定义图片或文字水印。
/// </summary>
/// <remarks>
/// 图片和文字必须且只能设置其中一种。
/// </remarks>
public sealed class ImageWatermarkOptions
{
    /// <summary>
    /// 获取或设置水印图片的编码数据。
    /// </summary>
    public byte[]? ImageBytes { get; set; }

    /// <summary>
    /// 获取或设置水印文字样式。
    /// </summary>
    public ImageTextOptions? Text { get; set; }

    /// <summary>
    /// 获取或设置水印宽度；未设置时使用原始宽度。
    /// </summary>
    public int? Width { get; set; }

    /// <summary>
    /// 获取或设置水印高度；未设置时使用原始高度。
    /// </summary>
    public int? Height { get; set; }

    /// <summary>
    /// 获取或设置水印定位方式。
    /// </summary>
    public ImageAnchor Anchor { get; set; } = ImageAnchor.BottomRight;

    /// <summary>
    /// 获取或设置水印与画布边缘的间距。
    /// </summary>
    public int Margin { get; set; } = 10;

    /// <summary>
    /// 获取或设置水印不透明度，范围为 0-1。
    /// </summary>
    public float Opacity { get; set; } = 1;
}

/// <summary>
/// 定义最终画布上的图像标注。
/// </summary>
/// <remarks>
/// 直线和箭头使用起点 <c>X/Y</c> 及终点 <c>X2/Y2</c>。
/// </remarks>
public sealed class ImageAnnotation
{
    /// <summary>
    /// 获取或设置标注形状。
    /// </summary>
    public ImageAnnotationKind Kind { get; set; }

    /// <summary>
    /// 获取或设置标注起点的横坐标。
    /// </summary>
    public float X { get; set; }

    /// <summary>
    /// 获取或设置标注起点的纵坐标。
    /// </summary>
    public float Y { get; set; }

    /// <summary>
    /// 获取或设置标注终点的横坐标。
    /// </summary>
    public float X2 { get; set; }

    /// <summary>
    /// 获取或设置标注终点的纵坐标。
    /// </summary>
    public float Y2 { get; set; }

    /// <summary>
    /// 获取或设置矩形或椭圆标注的宽度。
    /// </summary>
    public float Width { get; set; }

    /// <summary>
    /// 获取或设置矩形或椭圆标注的高度。
    /// </summary>
    public float Height { get; set; }

    /// <summary>
    /// 获取或设置线条宽度。
    /// </summary>
    public float StrokeWidth { get; set; } = 2;

    /// <summary>
    /// 获取或设置线条颜色。
    /// </summary>
    public RgbColor Color { get; set; } = new(255, 0, 0);

    /// <summary>
    /// 获取或设置填充颜色；未设置时不填充。
    /// </summary>
    public RgbColor? FillColor { get; set; }

    /// <summary>
    /// 获取或设置文字标注的样式。
    /// </summary>
    public ImageTextOptions? Text { get; set; }
}

/// <summary>
/// 定义统一静态图像处理选项。
/// </summary>
/// <remarks>
/// 处理阶段依次执行方向纠正、裁剪、缩放、旋转、翻转、边框、水印、标注和编码。
/// </remarks>
public sealed class ImageProcessOptions
{
    /// <summary>
    /// 获取或设置处理资源限制。
    /// </summary>
    public ImageProcessingLimits Limits { get; set; } = new();

    /// <summary>
    /// 获取或设置是否根据元数据自动纠正方向。
    /// </summary>
    public bool AutoOrient { get; set; } = true;

    /// <summary>
    /// 获取或设置裁剪选项。
    /// </summary>
    public ImageCropOptions? Crop { get; set; }

    /// <summary>
    /// 获取或设置缩放选项。
    /// </summary>
    public ImageResizeOptions? Resize { get; set; }

    /// <summary>
    /// 获取或设置顺时针旋转角度。
    /// </summary>
    public int Rotation { get; set; }

    /// <summary>
    /// 获取或设置是否水平翻转图像。
    /// </summary>
    public bool FlipHorizontal { get; set; }

    /// <summary>
    /// 获取或设置是否垂直翻转图像。
    /// </summary>
    public bool FlipVertical { get; set; }

    /// <summary>
    /// 获取或设置圆角半径。
    /// </summary>
    public float CornerRadius { get; set; }

    /// <summary>
    /// 获取或设置边框宽度。
    /// </summary>
    /// <remarks>
    /// 边框绘制在画布内部，不改变输出尺寸。
    /// </remarks>
    public int BorderWidth { get; set; }

    /// <summary>
    /// 获取或设置边框颜色。
    /// </summary>
    public RgbColor BorderColor { get; set; } = new(0, 0, 0);

    /// <summary>
    /// 获取或设置水印列表。
    /// </summary>
    public IList<ImageWatermarkOptions> Watermarks { get; set; } = new List<ImageWatermarkOptions>();

    /// <summary>
    /// 获取或设置标注列表。
    /// </summary>
    public IList<ImageAnnotation> Annotations { get; set; } = new List<ImageAnnotation>();

    /// <summary>
    /// 获取或设置是否移除元数据。
    /// </summary>
    public bool RemoveMetadata { get; set; } = true;

    /// <summary>
    /// 获取或设置是否保留 ICC 配置文件。
    /// </summary>
    public bool PreserveIccProfile { get; set; } = true;

    /// <summary>
    /// 获取或设置输出格式。
    /// </summary>
    public ImageOutputFormat Format { get; set; } = ImageOutputFormat.Png;

    /// <summary>
    /// 获取或设置 JPEG 输出的背景颜色。
    /// </summary>
    public RgbColor JpegBackground { get; set; } = new(255, 255, 255);

    /// <summary>
    /// 获取或设置初始编码质量，范围为 1-100。
    /// </summary>
    public int Quality { get; set; } = 85;

    /// <summary>
    /// 获取或设置允许使用的最低 JPEG 编码质量。
    /// </summary>
    public int MinimumQuality { get; set; } = 40;

    /// <summary>
    /// 获取或设置目标文件大小；未设置时不限制目标大小。
    /// </summary>
    public long? TargetSizeBytes { get; set; }

    /// <summary>
    /// 获取或设置未达到目标大小时是否抛出异常。
    /// </summary>
    public bool StrictTargetSize { get; set; }

    /// <summary>
    /// 获取或设置压缩时允许的最小图像边长。
    /// </summary>
    public int MinimumDimension { get; set; } = 64;
}

/// <summary>
/// 定义固定单元格拼图选项。
/// </summary>
/// <remarks>
/// 输出编辑选项在图像合成之后应用。
/// </remarks>
public sealed class ImageComposeOptions
{
    /// <summary>
    /// 获取或设置拼图排列方式。
    /// </summary>
    public ImageComposeLayout Layout { get; set; } = ImageComposeLayout.Grid;

    /// <summary>
    /// 获取或设置网格列数。
    /// </summary>
    public int Columns { get; set; } = 2;

    /// <summary>
    /// 获取或设置拼图单元格宽度。
    /// </summary>
    public int CellWidth { get; set; } = 256;

    /// <summary>
    /// 获取或设置拼图单元格高度。
    /// </summary>
    public int CellHeight { get; set; } = 256;

    /// <summary>
    /// 获取或设置单元格之间的间距。
    /// </summary>
    public int Spacing { get; set; }

    /// <summary>
    /// 获取或设置画布内边距。
    /// </summary>
    public int Padding { get; set; }

    /// <summary>
    /// 获取或设置单元格中的缩放模式。
    /// </summary>
    public ImageResizeMode Mode { get; set; } = ImageResizeMode.Contain;

    /// <summary>
    /// 获取或设置画布背景颜色。
    /// </summary>
    public RgbColor Background { get; set; } = new(255, 255, 255);

    /// <summary>
    /// 获取或设置合成后的输出处理选项。
    /// </summary>
    public ImageProcessOptions Output { get; set; } = new();
}

/// <summary>
/// 表示编码图像的头部信息。
/// </summary>
/// <remarks>
/// 宽高尚未应用方向纠正。
/// </remarks>
public sealed class ImageInfo
{
    /// <summary>
    /// 获取图像宽度。
    /// </summary>
    public int Width { get; internal set; }

    /// <summary>
    /// 获取图像高度。
    /// </summary>
    public int Height { get; internal set; }

    /// <summary>
    /// 获取图像帧数。
    /// </summary>
    public int FrameCount { get; internal set; } = 1;

    /// <summary>
    /// 获取图像方向值。
    /// </summary>
    public int Orientation { get; internal set; } = 1;

    /// <summary>
    /// 获取图像格式。
    /// </summary>
    public ImageOutputFormat Format { get; internal set; }

    /// <summary>
    /// 获取图像格式对应的 MIME 类型。
    /// </summary>
    public string MimeType => Format == ImageOutputFormat.Jpeg ? "image/jpeg" : Format == ImageOutputFormat.Png ? "image/png" : "image/webp";
}

/// <summary>
/// 表示图像处理后的编码结果。
/// </summary>
/// <remarks>
/// 未达到体积目标不等同于处理异常，是否严格失败由选项决定。
/// </remarks>
public sealed class ImageProcessResult
{
    /// <summary>
    /// 获取编码后的图像数据。
    /// </summary>
    public byte[] Bytes { get; internal set; } = Array.Empty<byte>();

    /// <summary>
    /// 获取输出图像格式。
    /// </summary>
    public ImageOutputFormat Format { get; internal set; }

    /// <summary>
    /// 获取输出图像对应的 MIME 类型。
    /// </summary>
    public string MimeType => Format == ImageOutputFormat.Jpeg ? "image/jpeg" : Format == ImageOutputFormat.Png ? "image/png" : "image/webp";

    /// <summary>
    /// 获取输出图像宽度。
    /// </summary>
    public int Width { get; internal set; }

    /// <summary>
    /// 获取输出图像高度。
    /// </summary>
    public int Height { get; internal set; }

    /// <summary>
    /// 获取编码数据的字节数。
    /// </summary>
    public long SizeBytes => Bytes.LongLength;

    /// <summary>
    /// 获取实际使用的编码质量；非 JPEG 输出为 null。
    /// </summary>
    public int? Quality { get; internal set; }

    /// <summary>
    /// 获取是否达到目标文件大小。
    /// </summary>
    public bool TargetSizeReached { get; internal set; }
}
