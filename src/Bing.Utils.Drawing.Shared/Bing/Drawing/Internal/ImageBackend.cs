namespace Bing.Drawing.Internal;

/// <summary>
/// 表示由图像后端拥有的图像表面。
/// </summary>
/// <remarks>
/// 统一管线负责释放实例，实例不会直接暴露给调用者。
/// </remarks>
internal abstract class ImageSurface : IDisposable
{
    /// <summary>
    /// 获取图像宽度。
    /// </summary>
    internal abstract int Width { get; }

    /// <summary>
    /// 获取图像高度。
    /// </summary>
    internal abstract int Height { get; }

    /// <inheritdoc />
    public abstract void Dispose();
}

/// <summary>
/// 定义图像后端适配器的操作契约。
/// </summary>
/// <remarks>
/// 仅供适配程序集调用，几何、资源限额和压缩策略由公共管线控制。
/// </remarks>
internal interface IImageBackend
{
    /// <summary>
    /// 从编码数据加载图像。
    /// </summary>
    /// <param name="bytes">编码图像数据。</param>
    /// <returns>后端拥有的图像表面。</returns>
    ImageSurface Load(byte[] bytes);

    /// <summary>
    /// 创建指定尺寸和背景色的图像。
    /// </summary>
    /// <param name="width">图像宽度。</param>
    /// <param name="height">图像高度。</param>
    /// <param name="background">背景颜色。</param>
    /// <returns>新创建的图像表面。</returns>
    ImageSurface Create(int width, int height, RgbColor background);

    /// <summary>
    /// 按指定尺寸缩放图像。
    /// </summary>
    /// <param name="image">源图像。</param>
    /// <param name="width">目标宽度。</param>
    /// <param name="height">目标高度。</param>
    /// <returns>缩放后的图像表面。</returns>
    ImageSurface Resize(ImageSurface image, int width, int height);

    /// <summary>
    /// 裁剪图像区域。
    /// </summary>
    /// <param name="image">源图像。</param>
    /// <param name="x">裁剪区域左上角横坐标。</param>
    /// <param name="y">裁剪区域左上角纵坐标。</param>
    /// <param name="width">裁剪区域宽度。</param>
    /// <param name="height">裁剪区域高度。</param>
    /// <returns>裁剪后的图像表面。</returns>
    ImageSurface Crop(ImageSurface image, int x, int y, int width, int height);

    /// <summary>
    /// 按顺时针角度旋转图像。
    /// </summary>
    /// <param name="image">源图像。</param>
    /// <param name="angle">旋转角度。</param>
    /// <returns>旋转后的图像表面。</returns>
    ImageSurface Rotate(ImageSurface image, int angle);

    /// <summary>
    /// 按指定方向翻转图像。
    /// </summary>
    /// <param name="image">源图像。</param>
    /// <param name="horizontal">是否水平翻转。</param>
    /// <param name="vertical">是否垂直翻转。</param>
    /// <returns>翻转后的图像表面。</returns>
    ImageSurface Flip(ImageSurface image, bool horizontal, bool vertical);

    /// <summary>
    /// 将图像绘制到目标表面。
    /// </summary>
    /// <param name="target">目标图像。</param>
    /// <param name="image">待绘制图像。</param>
    /// <param name="x">目标位置横坐标。</param>
    /// <param name="y">目标位置纵坐标。</param>
    /// <param name="opacity">绘制不透明度。</param>
    void DrawImage(ImageSurface target, ImageSurface image, int x, int y, float opacity);

    /// <summary>
    /// 在图像上绘制圆角和边框。
    /// </summary>
    /// <param name="image">目标图像。</param>
    /// <param name="radius">圆角半径。</param>
    /// <param name="borderWidth">边框宽度。</param>
    /// <param name="borderColor">边框颜色。</param>
    void Frame(ImageSurface image, float radius, int borderWidth, RgbColor borderColor);

    /// <summary>
    /// 测量文字所需的图像尺寸。
    /// </summary>
    /// <param name="text">文字样式。</param>
    /// <returns>文字宽高。</returns>
    (int Width, int Height) MeasureText(ImageTextOptions text);

    /// <summary>
    /// 在图像上绘制标注。
    /// </summary>
    /// <param name="image">目标图像。</param>
    /// <param name="annotation">标注配置。</param>
    /// <param name="opacity">绘制不透明度。</param>
    void Annotate(ImageSurface image, ImageAnnotation annotation, float opacity);

    /// <summary>
    /// 将图像编码为指定格式。
    /// </summary>
    /// <param name="image">待编码图像。</param>
    /// <param name="format">输出格式。</param>
    /// <param name="quality">编码质量。</param>
    /// <param name="background">透明像素使用的背景色。</param>
    /// <returns>编码后的图像数据。</returns>
    byte[] Encode(ImageSurface image, ImageOutputFormat format, int quality, RgbColor background);
}
