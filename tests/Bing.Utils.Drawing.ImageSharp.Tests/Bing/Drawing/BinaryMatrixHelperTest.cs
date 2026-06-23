using Bing.Drawing;

namespace Bing.Conversions;

/// <summary>
/// 测试类：覆盖 BinaryMatrixHelper 公共矩阵兼容层。
/// </summary>
[Trait("Drawing", "BinaryMatrixHelper")]
public class BinaryMatrixHelperTest
{
    #region FromFlatArray / ToFlatArray

    [Fact]
    public void FromFlatArray_ValidInput_ReturnsCorrectShape()
    {
        var buffer = new byte[] { 1, 2, 3, 4, 5, 6 };
        var matrix = BinaryMatrixHelper.FromFlatArray(buffer, 3, 2);

        matrix.GetLength(0).ShouldBe(3);
        matrix.GetLength(1).ShouldBe(2);
        matrix[0, 0].ShouldBe((byte)1);
        matrix[1, 0].ShouldBe((byte)2);
        matrix[2, 0].ShouldBe((byte)3);
        matrix[0, 1].ShouldBe((byte)4);
        matrix[1, 1].ShouldBe((byte)5);
        matrix[2, 1].ShouldBe((byte)6);
    }

    [Fact]
    public void ToFlatArray_ValidInput_ReturnsRowMajor()
    {
        var matrix = new byte[3, 2];
        matrix[0, 0] = 1; matrix[1, 0] = 2; matrix[2, 0] = 3;
        matrix[0, 1] = 4; matrix[1, 1] = 5; matrix[2, 1] = 6;

        var flat = BinaryMatrixHelper.ToFlatArray(matrix);

        flat.ShouldBe(new byte[] { 1, 2, 3, 4, 5, 6 });
    }

    [Fact]
    public void RoundTrip_FlatToMatrixToFlat_ReturnsSame()
    {
        var original = new byte[] { 10, 20, 30, 40 };
        var matrix = BinaryMatrixHelper.FromFlatArray(original, 2, 2);
        var roundTripped = BinaryMatrixHelper.ToFlatArray(matrix);

        roundTripped.ShouldBe(original);
    }

    [Fact]
    public void FromFlatArray_WrongSize_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => BinaryMatrixHelper.FromFlatArray(new byte[5], 2, 2));
    }

    [Fact]
    public void FromFlatArray_NullBuffer_Throws()
    {
        Should.Throw<ArgumentNullException>(() => BinaryMatrixHelper.FromFlatArray(null!, 2, 2));
    }

    [Fact]
    public void FromFlatArray_ZeroWidth_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => BinaryMatrixHelper.FromFlatArray(new byte[0], 0, 1));
    }

    [Fact]
    public void ToFlatArray_NullMatrix_Throws()
    {
        Should.Throw<ArgumentNullException>(() => BinaryMatrixHelper.ToFlatArray(null!));
    }

    #endregion

    #region ClearBorder

    [Fact]
    public void ClearBorder_RemovesBorderPixels()
    {
        var matrix = new byte[4, 4];
        for (var x = 0; x < 4; x++)
        for (var y = 0; y < 4; y++)
            matrix[x, y] = 0;

        var result = BinaryMatrixHelper.ClearBorder(matrix, 1);

        // 边框像素应为 255
        result[0, 0].ShouldBe((byte)255);
        result[3, 0].ShouldBe((byte)255);
        result[0, 3].ShouldBe((byte)255);
        result[3, 3].ShouldBe((byte)255);
        // 内部像素应保持不变
        result[1, 1].ShouldBe((byte)0);
        result[2, 2].ShouldBe((byte)0);
    }

    #endregion

    #region AddBorder

    [Fact]
    public void AddBorder_IncreasesSize()
    {
        var matrix = new byte[2, 2];
        matrix[0, 0] = 1; matrix[1, 0] = 2;
        matrix[0, 1] = 3; matrix[1, 1] = 4;

        var result = BinaryMatrixHelper.AddBorder(matrix, 1);

        result.GetLength(0).ShouldBe(4);
        result.GetLength(1).ShouldBe(4);
        // 边框应为 255
        result[0, 0].ShouldBe((byte)255);
        result[3, 3].ShouldBe((byte)255);
        // 内部内容应正确偏移
        result[1, 1].ShouldBe((byte)1);
        result[2, 2].ShouldBe((byte)4);
    }

    #endregion

    #region Clone

    [Fact]
    public void Clone_ExtractsSubMatrix()
    {
        var matrix = new byte[4, 4];
        matrix[1, 1] = 10;
        matrix[2, 1] = 20;
        matrix[1, 2] = 30;
        matrix[2, 2] = 40;

        var sub = BinaryMatrixHelper.Clone(matrix, 1, 1, 2, 2);

        sub[0, 0].ShouldBe((byte)10);
        sub[1, 0].ShouldBe((byte)20);
        sub[0, 1].ShouldBe((byte)30);
        sub[1, 1].ShouldBe((byte)40);
    }

    #endregion

    #region DrawTo

    [Fact]
    public void DrawTo_CopiesSourceToTarget()
    {
        var source = new byte[2, 2];
        source[0, 0] = 1; source[1, 0] = 2;
        source[0, 1] = 3; source[1, 1] = 4;

        var target = new byte[4, 4];

        BinaryMatrixHelper.DrawTo(source, target, 1, 1);

        target[1, 1].ShouldBe((byte)1);
        target[2, 1].ShouldBe((byte)2);
        target[1, 2].ShouldBe((byte)3);
        target[2, 2].ShouldBe((byte)4);
        target[0, 0].ShouldBe((byte)0); // 未覆盖区域
    }

    [Fact]
    public void DrawTo_OutOfBounds_Throws()
    {
        var source = new byte[3, 3];
        var target = new byte[4, 4];

        Should.Throw<ArgumentException>(() => BinaryMatrixHelper.DrawTo(source, target, 2, 2));
    }

    #endregion

    #region FloodFill

    [Fact]
    public void FloodFill_FillsConnectedRegion()
    {
        var matrix = new byte[3, 3];
        matrix[0, 0] = 0; matrix[1, 0] = 0; matrix[2, 0] = 255;
        matrix[0, 1] = 0; matrix[1, 1] = 0; matrix[2, 1] = 255;
        matrix[0, 2] = 255; matrix[1, 2] = 255; matrix[2, 2] = 255;

        BinaryMatrixHelper.FloodFill(matrix, 0, 0, 128);

        matrix[0, 0].ShouldBe((byte)128);
        matrix[1, 0].ShouldBe((byte)128);
        matrix[0, 1].ShouldBe((byte)128);
        matrix[1, 1].ShouldBe((byte)128);
        matrix[2, 0].ShouldBe((byte)255); // 不连通区域不受影响
    }

    #endregion

    #region ToCodeString

    [Fact]
    public void ToCodeString_BreakLine_IncludesLineBreaks()
    {
        var matrix = new byte[2, 2];
        matrix[0, 0] = 0; matrix[1, 0] = 255;
        matrix[0, 1] = 255; matrix[1, 1] = 0;

        var code = BinaryMatrixHelper.ToCodeString(matrix, 128, true);

        code.ShouldContain("\r\n");
    }

    [Fact]
    public void ToCodeString_NoBreakLine_SingleLine()
    {
        var matrix = new byte[2, 2];
        matrix[0, 0] = 0; matrix[1, 0] = 255;
        matrix[0, 1] = 255; matrix[1, 1] = 0;

        var code = BinaryMatrixHelper.ToCodeString(matrix, 128, false);

        code.ShouldNotContain("\r\n");
        code.Length.ShouldBe(4);
    }

    [Fact]
    public void ToCodeString_ForegroundIs1_BackgroundIs0()
    {
        var matrix = new byte[3, 1];
        matrix[0, 0] = 0;     // 前景 (0 < 128)
        matrix[1, 0] = 200;   // 背景 (200 >= 128)
        matrix[2, 0] = 50;    // 前景 (50 < 128)

        var code = BinaryMatrixHelper.ToCodeString(matrix, 128, false);
        code.ShouldBe("101");
    }

    #endregion
}
