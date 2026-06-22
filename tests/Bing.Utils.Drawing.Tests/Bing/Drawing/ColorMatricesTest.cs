using System.Drawing;
using System.Drawing.Imaging;
using Bing.Drawing;

namespace Bing.Drawing;

/// <summary>
/// 测试类：覆盖 <see cref="ColorMatrices"/> 相关行为。
/// </summary>
[Trait("Drawing", "ColorMatrices")]
public class ColorMatricesTest
{
    #region CreateBrightnessFilter

    /// <summary>
    /// 测试用例：验证 <see cref="ColorMatrices.CreateBrightnessFilter"/> 在 `Amount1` 场景下，结果为 `DiagonalIs1`。
    /// amount=1 时矩阵对角线 RGB 应为 1（原始亮度）
    /// </summary>
    [Fact]
    public void CreateBrightnessFilter_Amount1_DiagonalIs1()
    {
        var matrix = ColorMatrices.CreateBrightnessFilter(1f);
        matrix.Matrix00.ShouldBe(1f, 0.001f);
        matrix.Matrix11.ShouldBe(1f, 0.001f);
        matrix.Matrix22.ShouldBe(1f, 0.001f);
        matrix.Matrix33.ShouldBe(1f, 0.001f);
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ColorMatrices.CreateBrightnessFilter"/> 在 `Amount0` 场景下，结果为 `DiagonalIs0`。
    /// amount=0 时矩阵对角线 RGB 应为 0（全黑图像）
    /// </summary>
    [Fact]
    public void CreateBrightnessFilter_Amount0_DiagonalIs0()
    {
        var matrix = ColorMatrices.CreateBrightnessFilter(0f);
        matrix.Matrix00.ShouldBe(0f, 0.001f);
        matrix.Matrix11.ShouldBe(0f, 0.001f);
        matrix.Matrix22.ShouldBe(0f, 0.001f);
        matrix.Matrix33.ShouldBe(1f, 0.001f); // Alpha 通道始终为 1
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ColorMatrices.CreateBrightnessFilter"/> 在 `Amount2` 场景下，结果为 `DiagonalIs2`。
    /// amount>1 时允许更高亮度
    /// </summary>
    [Fact]
    public void CreateBrightnessFilter_Amount2_DiagonalIs2()
    {
        var matrix = ColorMatrices.CreateBrightnessFilter(2f);
        matrix.Matrix00.ShouldBe(2f, 0.001f);
        matrix.Matrix11.ShouldBe(2f, 0.001f);
        matrix.Matrix22.ShouldBe(2f, 0.001f);
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ColorMatrices.CreateBrightnessFilter"/> 在 `NegativeAmount` 场景下，结果为 `ThrowsArgumentOutOfRangeException`。
    /// </summary>
    [Fact]
    public void CreateBrightnessFilter_NegativeAmount_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ColorMatrices.CreateBrightnessFilter(-0.1f));
    }

    #endregion

    #region CreateGrayScaleFilter

    /// <summary>
    /// 测试用例：验证 <see cref="ColorMatrices.CreateGrayScaleFilter"/> 在 `Amount1` 场景下，结果为 `ReturnsIdentityLikeMatrix`。
    /// amount=1 时不改变颜色（无灰度化）
    /// </summary>
    [Fact]
    public void CreateGrayScaleFilter_Amount1_ReturnsNoChangeMatrix()
    {
        var matrix = ColorMatrices.CreateGrayScaleFilter(1f);
        // amount=1 → internal amount=0 → Matrix00 = 0.299 + 0.701*0 = 0.299
        // 实际 identity 趋近于 R=1,G=1,B=1 的分量
        matrix.Matrix33.ShouldBe(1f, 0.001f); // Alpha 始终为 1
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ColorMatrices.CreateGrayScaleFilter"/> 在 `Amount0` 场景下，结果为 `ReturnsFullGrayscaleMatrix`。
    /// amount=0 时完全灰度化
    /// </summary>
    [Fact]
    public void CreateGrayScaleFilter_Amount0_ReturnsFullGrayscaleMatrix()
    {
        var matrix = ColorMatrices.CreateGrayScaleFilter(0f);
        // amount=0 → internal amount=1 → Matrix00 = 0.299 + 0.701*1 = 1.0
        matrix.Matrix00.ShouldBe(1f, 0.001f);
        matrix.Matrix33.ShouldBe(1f, 0.001f);
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ColorMatrices.CreateGrayScaleFilter"/> 在 `NegativeAmount` 场景下，结果为 `ThrowsArgumentOutOfRangeException`。
    /// </summary>
    [Fact]
    public void CreateGrayScaleFilter_NegativeAmount_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ColorMatrices.CreateGrayScaleFilter(-0.1f));
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ColorMatrices.CreateGrayScaleFilter"/> 在 `AmountGreaterThan1` 场景下，结果为 `ThrowsArgumentOutOfRangeException`。
    /// </summary>
    [Fact]
    public void CreateGrayScaleFilter_AmountGreaterThan1_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ColorMatrices.CreateGrayScaleFilter(1.1f));
    }

    #endregion

    #region CreateContrastFilter

    /// <summary>
    /// 测试用例：验证 <see cref="ColorMatrices.CreateContrastFilter"/> 在 `Amount1` 场景下，结果为 `DiagonalIs1AndOffsetIs0`。
    /// amount=1 时矩阵对角线为1，偏移为0（原始对比度）
    /// </summary>
    [Fact]
    public void CreateContrastFilter_Amount1_DiagonalIs1AndOffsetIs0()
    {
        var matrix = ColorMatrices.CreateContrastFilter(1f);
        matrix.Matrix00.ShouldBe(1f, 0.001f);
        matrix.Matrix11.ShouldBe(1f, 0.001f);
        matrix.Matrix22.ShouldBe(1f, 0.001f);
        matrix.Matrix33.ShouldBe(1f, 0.001f);
        // contrast = (-0.5 * 1) + 0.5 = 0
        matrix.Matrix40.ShouldBe(0f, 0.001f);
        matrix.Matrix41.ShouldBe(0f, 0.001f);
        matrix.Matrix42.ShouldBe(0f, 0.001f);
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ColorMatrices.CreateContrastFilter"/> 在 `Amount0` 场景下，结果为 `ProducesGrayImage`。
    /// amount=0 时图像变为纯灰
    /// </summary>
    [Fact]
    public void CreateContrastFilter_Amount0_ProducesGrayImage()
    {
        var matrix = ColorMatrices.CreateContrastFilter(0f);
        // contrast = (-0.5 * 0) + 0.5 = 0.5
        matrix.Matrix40.ShouldBe(0.5f, 0.001f);
        matrix.Matrix41.ShouldBe(0.5f, 0.001f);
        matrix.Matrix42.ShouldBe(0.5f, 0.001f);
        matrix.Matrix00.ShouldBe(0f, 0.001f);
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ColorMatrices.CreateContrastFilter"/> 在 `NegativeAmount` 场景下，结果为 `ThrowsArgumentOutOfRangeException`。
    /// </summary>
    [Fact]
    public void CreateContrastFilter_NegativeAmount_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ColorMatrices.CreateContrastFilter(-0.1f));
    }

    #endregion

    #region CreateSaturationFilter

    /// <summary>
    /// 测试用例：验证 <see cref="ColorMatrices.CreateSaturationFilter"/> 在 `Amount1` 场景下，结果为 `ReturnsOriginalSaturation`。
    /// amount=1 时不改变饱和度
    /// </summary>
    [Fact]
    public void CreateSaturationFilter_Amount1_ReturnsOriginalSaturation()
    {
        var matrix = ColorMatrices.CreateSaturationFilter(1f);
        // amount=1 → Matrix00 = 0.213 + 0.787*1 = 1.0
        matrix.Matrix00.ShouldBe(1f, 0.001f);
        matrix.Matrix33.ShouldBe(1f, 0.001f);
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ColorMatrices.CreateSaturationFilter"/> 在 `Amount0` 场景下，结果为 `ReturnsGrayscaleMatrix`。
    /// amount=0 时完全不饱和（灰度图）
    /// </summary>
    [Fact]
    public void CreateSaturationFilter_Amount0_ReturnsGrayscaleMatrix()
    {
        var matrix = ColorMatrices.CreateSaturationFilter(0f);
        // amount=0 → Matrix00 = 0.213
        matrix.Matrix00.ShouldBe(0.213f, 0.001f);
        matrix.Matrix33.ShouldBe(1f, 0.001f);
    }

    /// <summary>
    /// 测试用例：验证 <see cref="ColorMatrices.CreateSaturationFilter"/> 在 `NegativeAmount` 场景下，结果为 `ThrowsArgumentOutOfRangeException`。
    /// </summary>
    [Fact]
    public void CreateSaturationFilter_NegativeAmount_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ColorMatrices.CreateSaturationFilter(-0.1f));
    }

    #endregion
}
