using System.Drawing;
using System.IO;
using Bing.Drawing;

namespace Bing.Utils.Drawing.Tests;

/// <summary>
/// 验证原生图像接口保留调用方输入和资源所有权。
/// </summary>
public class ImageOwnershipTest
{
    /// <summary>
    /// 验证无效数据抛出内容异常，尝试加载则返回 false。
    /// </summary>
    [Fact]
    public void InvalidBytes_ThrowClearError_AndTryLoadReturnsFalse()
    {
        var bytes = new byte[] { 1, 2, 3 };
        Should.Throw<InvalidDataException>(() => ImageHelper.FromBytes(bytes));
        ImageHelper.TryLoad(bytes, out var image).ShouldBeFalse();
        image.ShouldBeNull();
    }

    /// <summary>
    /// 验证灰度处理返回独立图像且不修改源像素。
    /// </summary>
    [Fact]
    public void Gray_ReturnsIndependentImage_AndKeepsSourcePixels()
    {
        using var source = new Bitmap(3, 2);
        source.SetPixel(0, 0, Color.Red);
        using var gray = ImageHelper.Gray(source);
        ReferenceEquals(source, gray).ShouldBeFalse();
        source.GetPixel(0, 0).ToArgb().ShouldBe(Color.Red.ToArgb());
        gray.GetPixel(0, 0).R.ShouldBe(gray.GetPixel(0, 0).G);
    }

    /// <summary>
    /// 验证释放输入流后已加载图像仍可使用。
    /// </summary>
    [Fact]
    public void FromStream_ResultSurvivesInputDisposal()
    {
        using var source = new Bitmap(3, 2);
        var stream = new MemoryStream(ImageHelper.ToBytes(source));
        using var loaded = ImageHelper.FromStream(stream);
        stream.CanRead.ShouldBeTrue();
        stream.Dispose();
        ImageHelper.ToBytes(loaded).Length.ShouldBeGreaterThan(0);
    }
}
