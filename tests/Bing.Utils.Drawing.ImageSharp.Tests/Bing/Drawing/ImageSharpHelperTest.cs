using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
namespace Bing.Drawing;
/// <summary>
/// 测试类：覆盖 `ImageSharpHelper` 相关行为。
/// </summary>
public class ImageSharpHelperTest
{
    /// <summary>
    /// 测试用例：验证 `ToBytes` 在 `And_FromBytes` 场景下，结果为 `RoundTrip`。
    /// </summary>
    [Fact]
    public void ToBytes_And_FromBytes_RoundTrip()
    {
        using var source = CreateSampleImage();
        var bytes = ImageSharpHelper.ToBytes(source, PngFormat.Instance);
        bytes.Length.ShouldBeGreaterThan(0);
        using var restored = ImageSharpHelper.FromBytes(bytes);
        restored.ShouldNotBeNull();
        restored!.Width.ShouldBe(2);
        restored.Height.ShouldBe(2);
    }
    /// <summary>
    /// 测试用例：验证 `ToBase64String` 在 `And_FromBase64String` 场景下，结果为 `RoundTrip`。
    /// </summary>
    [Fact]
    public void ToBase64String_And_FromBase64String_RoundTrip()
    {
        using var source = CreateSampleImage();
        var base64 = ImageSharpHelper.ToBase64String(source, PngFormat.Instance);
        base64.ShouldNotBeNullOrWhiteSpace();
        using var restored = ImageSharpHelper.FromBase64String(base64);
        restored.ShouldNotBeNull();
        restored!.Width.ShouldBe(2);
        restored.Height.ShouldBe(2);
    }
    /// <summary>
    /// 测试用例：验证 `ToDataUrl` 在 `And_FromDataUrl` 场景下，结果为 `RoundTrip`。
    /// </summary>
    [Fact]
    public void ToDataUrl_And_FromDataUrl_RoundTrip()
    {
        using var source = CreateSampleImage();
        var dataUrl = ImageSharpHelper.ToDataUrl(source, PngFormat.Instance);
        dataUrl.ShouldStartWith("data:image/png;base64,");
        using var restored = ImageSharpHelper.FromDataUrl(dataUrl);
        restored.ShouldNotBeNull();
        restored!.Width.ShouldBe(2);
        restored.Height.ShouldBe(2);
    }
    /// <summary>
    /// 测试用例：验证 `FromBytes` 在 `Null` 场景下，结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void FromBytes_Null_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.FromBytes(null!))
            .ParamName.ShouldBe("bytes");
    }
    /// <summary>
    /// 测试用例：验证 `ToBytes` 在 `NullImage` 场景下，结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void ToBytes_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.ToBytes(null!))
            .ParamName.ShouldBe("image");
    }
    /// <summary>
    /// 测试用例：验证 `ToBase64String` 在 `NullImage` 场景下，结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void ToBase64String_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.ToBase64String(null!))
            .ParamName.ShouldBe("image");
    }
    /// <summary>
    /// 测试用例：验证 `ToDataUrl` 在 `NullImage` 场景下，结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void ToDataUrl_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.ToDataUrl(null!))
            .ParamName.ShouldBe("image");
    }
    /// <summary>
    /// 测试用例：验证 `FromBase64String` 在 `InvalidValue` 场景下，结果为 `ReturnsNull`。
    /// </summary>
    [Fact]
    public void FromBase64String_InvalidValue_ReturnsNull()
    {
        var restored = ImageSharpHelper.FromBase64String("not-base64");
        restored.ShouldBeNull();
    }
    /// <summary>
    /// 测试用例：验证 `FromDataUrl` 在 `InvalidFormat` 场景下，结果为 `ReturnsNull`。
    /// </summary>
    [Fact]
    public void FromDataUrl_InvalidFormat_ReturnsNull()
    {
        var restored = ImageSharpHelper.FromDataUrl("invalid-data-url");
        restored.ShouldBeNull();
    }
    /// <summary>
    /// 测试用例：验证 `FromFile` 在 `And_FromFileGeneric` 场景下，结果为 `LoadExpectedImage`。
    /// </summary>
    [Fact]
    public void FromFile_And_FromFileGeneric_LoadExpectedImage()
    {
        using var source = CreateSampleImage();
        var bytes = ImageSharpHelper.ToBytes(source, PngFormat.Instance);
        var tempPath = Path.Combine(Path.GetTempPath(), $"bing-utils-imagesharp-{Guid.NewGuid():N}.png");
        try
        {
            File.WriteAllBytes(tempPath, bytes);
            using var restored = ImageSharpHelper.FromFile(tempPath);
            restored.ShouldNotBeNull();
            restored!.Width.ShouldBe(2);
            restored.Height.ShouldBe(2);
            using var restoredGeneric = ImageSharpHelper.FromFile<Rgba32>(tempPath);
            restoredGeneric.ShouldNotBeNull();
            restoredGeneric!.Width.ShouldBe(2);
            restoredGeneric.Height.ShouldBe(2);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }
    /// <summary>
    /// 测试用例：验证 `SetOpacity` 在 `OutOfRange` 场景下，结果为 `ThrowsArgumentOutOfRangeException`。
    /// </summary>
    [Fact]
    public void SetOpacity_OutOfRange_ThrowsArgumentOutOfRangeException()
    {
        using var source = CreateSampleImage();
        Should.Throw<ArgumentOutOfRangeException>(() => ImageSharpHelper.SetOpacity(source, -0.1f))
            .ParamName.ShouldBe("opacity");
        Should.Throw<ArgumentOutOfRangeException>(() => ImageSharpHelper.SetOpacity(source, 1.1f))
            .ParamName.ShouldBe("opacity");
    }
    /// <summary>
    /// 测试用例：验证 `SetOpacity` 在 `ValidOpacity` 场景下，结果为 `ReturnsNewImageWithLowerAlpha`。
    /// </summary>
    [Fact]
    public void SetOpacity_ValidOpacity_ReturnsNewImageWithLowerAlpha()
    {
        using var source = CreateSampleImage();
        using var result = ImageSharpHelper.SetOpacity(source, 0.5f);
        result.ShouldNotBeNull();
        ReferenceEquals(source, result).ShouldBeFalse();
        result.Width.ShouldBe(source.Width);
        result.Height.ShouldBe(source.Height);
        using var resultRgba = result.CloneAs<Rgba32>();
        resultRgba[0, 0].A.ShouldBeLessThan((byte)255);
    }
    /// <summary>
    /// 测试用例：验证 `SetOpacity` 在 `NullImage` 场景下，结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void SetOpacity_NullImage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ImageSharpHelper.SetOpacity(null!, 0.5f))
            .ParamName.ShouldBe("image");
    }
    private static Image<Rgba32> CreateSampleImage()
    {
        var image = new Image<Rgba32>(2, 2);
        image[0, 0] = new Rgba32(255, 0, 0, 255);
        image[1, 0] = new Rgba32(0, 255, 0, 255);
        image[0, 1] = new Rgba32(0, 0, 255, 255);
        image[1, 1] = new Rgba32(255, 255, 255, 255);
        return image;
    }
}

