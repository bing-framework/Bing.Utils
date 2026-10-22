using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Bing.Drawing;

/// <summary>
/// 验证元数据流操作的 <c>leaveOpen</c> 契约。
/// </summary>
public sealed class ImageSharpMetadataContractTest
{
    /// <summary>
    /// 验证输入流与输出流相同时拒绝执行并保持输入流可读。
    /// </summary>
    [Fact]
    public void DeleteCoordinate_SameInputAndOutputStream_Throws()
    {
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        Should.Throw<ArgumentException>(() => ImageSharpHelper.DeleteCoordinate(stream, stream));
        stream.CanRead.ShouldBeTrue();
    }

    /// <summary>
    /// 验证启用 <c>leaveOpen</c> 时成功和失败都会保留流的打开状态。
    /// </summary>
    [Fact]
    public void DeleteCoordinate_LeaveOpenTrue_KeepsStreamsOpenOnSuccessAndFailure()
    {
        using var image = new Image<Rgba32>(2, 2);
        var bytes = ImageSharpHelper.ToBytes(image);
        using var input = new MemoryStream(bytes);
        using var output = new MemoryStream();

        ImageSharpHelper.DeleteCoordinate(input, output, leaveOpen: true);

        input.CanRead.ShouldBeTrue();
        output.CanWrite.ShouldBeTrue();

        using var badInput = new MemoryStream(new byte[] { 1, 2, 3 });
        using var badOutput = new MemoryStream();
        Should.Throw<Exception>(() => ImageSharpHelper.DeleteCoordinate(badInput, badOutput, leaveOpen: true));
        badInput.CanRead.ShouldBeTrue();
        badOutput.CanWrite.ShouldBeTrue();
    }

    /// <summary>
    /// 验证禁用 <c>leaveOpen</c> 时成功和失败都会关闭输入流与输出流。
    /// </summary>
    [Fact]
    public void DeleteCoordinate_LeaveOpenFalse_ClosesStreamsOnSuccessAndFailure()
    {
        using var image = new Image<Rgba32>(2, 2);
        var bytes = ImageSharpHelper.ToBytes(image);
        var input = new MemoryStream(bytes);
        var output = new MemoryStream();

        ImageSharpHelper.DeleteCoordinate(input, output, leaveOpen: false);

        Should.Throw<ObjectDisposedException>(() => input.ReadByte());
        Should.Throw<ObjectDisposedException>(() => output.WriteByte(1));

        var badInput = new MemoryStream(new byte[] { 1, 2, 3 });
        var badOutput = new MemoryStream();
        Should.Throw<Exception>(() => ImageSharpHelper.DeleteCoordinate(badInput, badOutput, leaveOpen: false));
        Should.Throw<ObjectDisposedException>(() => badInput.ReadByte());
        Should.Throw<ObjectDisposedException>(() => badOutput.WriteByte(1));
    }
}
