using System.Drawing;
using System.Drawing.Imaging;
using Bing.Drawing;

namespace Bing.Drawing;

/// <summary>
/// 验证 <see cref="global::Bing.Drawing.Gdi.ColorMatrices" /> 生成的颜色矩阵。
/// </summary>
[Trait("Drawing", "ColorMatrices")]
public class ColorMatricesTest
{
    #region CreateBrightnessFilter

    /// <summary>
    /// 验证亮度参数为 1 时生成保持原始亮度的矩阵。
    /// </summary>
    [Fact]
    public void CreateBrightnessFilter_Amount1_DiagonalIs1()
    {
        var matrix = global::Bing.Drawing.Gdi.ColorMatrices.CreateBrightnessFilter(1f);
        matrix.Matrix00.ShouldBe(1f, 0.001f);
        matrix.Matrix11.ShouldBe(1f, 0.001f);
        matrix.Matrix22.ShouldBe(1f, 0.001f);
        matrix.Matrix33.ShouldBe(1f, 0.001f);
    }

    /// <summary>
    /// 验证亮度参数为 0 时生成黑色矩阵。
    /// </summary>
    [Fact]
    public void CreateBrightnessFilter_Amount0_DiagonalIs0()
    {
        var matrix = global::Bing.Drawing.Gdi.ColorMatrices.CreateBrightnessFilter(0f);
        matrix.Matrix00.ShouldBe(0f, 0.001f);
        matrix.Matrix11.ShouldBe(0f, 0.001f);
        matrix.Matrix22.ShouldBe(0f, 0.001f);
        matrix.Matrix33.ShouldBe(1f, 0.001f); // Alpha 通道始终为 1
    }

    /// <summary>
    /// 验证亮度参数大于 1 时生成对应的增亮矩阵。
    /// </summary>
    [Fact]
    public void CreateBrightnessFilter_Amount2_DiagonalIs2()
    {
        var matrix = global::Bing.Drawing.Gdi.ColorMatrices.CreateBrightnessFilter(2f);
        matrix.Matrix00.ShouldBe(2f, 0.001f);
        matrix.Matrix11.ShouldBe(2f, 0.001f);
        matrix.Matrix22.ShouldBe(2f, 0.001f);
    }

    /// <summary>
    /// 验证负亮度参数抛出范围异常。
    /// </summary>
    [Fact]
    public void CreateBrightnessFilter_NegativeAmount_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => global::Bing.Drawing.Gdi.ColorMatrices.CreateBrightnessFilter(-0.1f));
    }

    #endregion

    #region CreateGrayScaleFilter

    /// <summary>
    /// 验证灰度化参数为 1 时保持原始颜色。
    /// </summary>
    [Fact]
    public void CreateGrayScaleFilter_Amount1_ReturnsNoChangeMatrix()
    {
        var matrix = global::Bing.Drawing.Gdi.ColorMatrices.CreateGrayScaleFilter(1f);
        // amount=1 → internal amount=0 → Matrix00 = 0.299 + 0.701*0 = 0.299
        // 实际 identity 趋近于 R=1,G=1,B=1 的分量
        matrix.Matrix33.ShouldBe(1f, 0.001f); // Alpha 始终为 1
    }

    /// <summary>
    /// 验证灰度化参数为 0 时生成完整灰度矩阵。
    /// </summary>
    [Fact]
    public void CreateGrayScaleFilter_Amount0_ReturnsFullGrayscaleMatrix()
    {
        var matrix = global::Bing.Drawing.Gdi.ColorMatrices.CreateGrayScaleFilter(0f);
        // amount=0 → internal amount=1 → Matrix00 = 0.299 + 0.701*1 = 1.0
        matrix.Matrix00.ShouldBe(1f, 0.001f);
        matrix.Matrix33.ShouldBe(1f, 0.001f);
    }

    /// <summary>
    /// 验证负灰度化参数抛出范围异常。
    /// </summary>
    [Fact]
    public void CreateGrayScaleFilter_NegativeAmount_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => global::Bing.Drawing.Gdi.ColorMatrices.CreateGrayScaleFilter(-0.1f));
    }

    /// <summary>
    /// 验证大于 1 的灰度化参数抛出范围异常。
    /// </summary>
    [Fact]
    public void CreateGrayScaleFilter_AmountGreaterThan1_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => global::Bing.Drawing.Gdi.ColorMatrices.CreateGrayScaleFilter(1.1f));
    }

    #endregion

    #region CreateContrastFilter

    /// <summary>
    /// 验证对比度参数为 1 时保持原始对比度。
    /// </summary>
    [Fact]
    public void CreateContrastFilter_Amount1_DiagonalIs1AndOffsetIs0()
    {
        var matrix = global::Bing.Drawing.Gdi.ColorMatrices.CreateContrastFilter(1f);
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
    /// 验证对比度参数为 0 时生成纯灰变换矩阵。
    /// </summary>
    [Fact]
    public void CreateContrastFilter_Amount0_ProducesGrayImage()
    {
        var matrix = global::Bing.Drawing.Gdi.ColorMatrices.CreateContrastFilter(0f);
        // contrast = (-0.5 * 0) + 0.5 = 0.5
        matrix.Matrix40.ShouldBe(0.5f, 0.001f);
        matrix.Matrix41.ShouldBe(0.5f, 0.001f);
        matrix.Matrix42.ShouldBe(0.5f, 0.001f);
        matrix.Matrix00.ShouldBe(0f, 0.001f);
    }

    /// <summary>
    /// 验证负对比度参数抛出范围异常。
    /// </summary>
    [Fact]
    public void CreateContrastFilter_NegativeAmount_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => global::Bing.Drawing.Gdi.ColorMatrices.CreateContrastFilter(-0.1f));
    }

    #endregion

    #region CreateSaturationFilter

    /// <summary>
    /// 验证饱和度参数为 1 时保持原始饱和度。
    /// </summary>
    [Fact]
    public void CreateSaturationFilter_Amount1_ReturnsOriginalSaturation()
    {
        var matrix = global::Bing.Drawing.Gdi.ColorMatrices.CreateSaturationFilter(1f);
        // amount=1 → Matrix00 = 0.213 + 0.787*1 = 1.0
        matrix.Matrix00.ShouldBe(1f, 0.001f);
        matrix.Matrix33.ShouldBe(1f, 0.001f);
    }

    /// <summary>
    /// 验证饱和度参数为 0 时生成灰度矩阵。
    /// </summary>
    [Fact]
    public void CreateSaturationFilter_Amount0_ReturnsGrayscaleMatrix()
    {
        var matrix = global::Bing.Drawing.Gdi.ColorMatrices.CreateSaturationFilter(0f);
        // amount=0 → Matrix00 = 0.213
        matrix.Matrix00.ShouldBe(0.213f, 0.001f);
        matrix.Matrix33.ShouldBe(1f, 0.001f);
    }

    /// <summary>
    /// 验证负饱和度参数抛出范围异常。
    /// </summary>
    [Fact]
    public void CreateSaturationFilter_NegativeAmount_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => global::Bing.Drawing.Gdi.ColorMatrices.CreateSaturationFilter(-0.1f));
    }

    #endregion
}
