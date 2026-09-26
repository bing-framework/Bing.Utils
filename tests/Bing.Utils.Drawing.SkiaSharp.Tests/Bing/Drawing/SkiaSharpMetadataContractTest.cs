using SkiaSharp;

namespace Bing.Drawing;

/// <summary>
/// 验证 Skia 元数据清理 API 的流所有权契约。
/// </summary>
public sealed class SkiaSharpMetadataContractTest
{
    /// <summary>
    /// 验证启用 <c>leaveOpen</c> 时成功和失败都会保留流的打开状态。
    /// </summary>
    [Fact]
    public void DeleteCoordinate_LeaveOpenTrue_KeepsStreamsOpenOnSuccessAndFailure()
    {
        using var bitmap = new SKBitmap(2, 2, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var image = SKImage.FromBitmap(bitmap);
        var bytes = SkiaSharpHelper.ToBytes(image);
        using var input = new MemoryStream(bytes);
        using var output = new MemoryStream();

        SkiaSharpHelper.DeleteCoordinate(input, output, leaveOpen: true);

        input.CanRead.ShouldBeTrue();
        output.CanWrite.ShouldBeTrue();

        using var badInput = new MemoryStream(new byte[] { 1, 2, 3 });
        using var badOutput = new MemoryStream();
        Should.Throw<Exception>(() => SkiaSharpHelper.DeleteCoordinate(badInput, badOutput, leaveOpen: true));
        badInput.CanRead.ShouldBeTrue();
        badOutput.CanWrite.ShouldBeTrue();
    }

    /// <summary>
    /// 验证输入流与输出流相同时拒绝执行并保持输入流可读。
    /// </summary>
    [Fact]
    public void DeleteCoordinate_SameInputAndOutput_ThrowsArgumentException()
    {
        using var bitmap = new SKBitmap(2, 2, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var image = SKImage.FromBitmap(bitmap);
        using var stream = new MemoryStream(SkiaSharpHelper.ToBytes(image));

        Should.Throw<ArgumentException>(() => SkiaSharpHelper.DeleteCoordinate(stream, stream));
        stream.CanRead.ShouldBeTrue();
    }

    /// <summary>
    /// 验证禁用 <c>leaveOpen</c> 时成功和失败都会关闭输入流与输出流。
    /// </summary>
    [Fact]
    public void DeleteCoordinate_LeaveOpenFalse_ClosesStreamsOnSuccessAndFailure()
    {
        using var bitmap = new SKBitmap(2, 2, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var image = SKImage.FromBitmap(bitmap);
        var bytes = SkiaSharpHelper.ToBytes(image);
        var input = new MemoryStream(bytes);
        var output = new MemoryStream();

        SkiaSharpHelper.DeleteCoordinate(input, output, leaveOpen: false);

        Should.Throw<ObjectDisposedException>(() => input.ReadByte());
        Should.Throw<ObjectDisposedException>(() => output.WriteByte(1));

        var badInput = new MemoryStream(new byte[] { 1, 2, 3 });
        var badOutput = new MemoryStream();
        Should.Throw<Exception>(() => SkiaSharpHelper.DeleteCoordinate(badInput, badOutput, leaveOpen: false));
        Should.Throw<ObjectDisposedException>(() => badInput.ReadByte());
        Should.Throw<ObjectDisposedException>(() => badOutput.WriteByte(1));
    }
}
