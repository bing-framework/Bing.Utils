namespace Bing.Collections;

/// <summary>
/// 字节数组 扩展 测试
/// </summary>
[Trait("CollectionsUT", "ByteArrayExtensions")]
public class ByteArrayExtensionsTest : TestBase
{
    /// <inheritdoc />
    public ByteArrayExtensionsTest(ITestOutputHelper output) : base(output)
    {
    }

    #region Copy

    /// <summary>
    /// 测试 - Copy - 正确复制二维字节数组
    /// </summary>
    [Fact]
    public void Copy_ReturnsCorrectCopy()
    {
        // 准备
        byte[,] original = new byte[3, 2] {
            { 1, 2 },
            { 3, 4 },
            { 5, 6 }
        };

        // 执行
        var copy = original.Copy();

        // 验证
        copy.ShouldNotBeSameAs(original); // 不是同一个实例
        copy.GetLength(0).ShouldBe(original.GetLength(0));
        copy.GetLength(1).ShouldBe(original.GetLength(1));

        // 验证内容相同
        for (int i = 0; i < original.GetLength(0); i++)
        {
            for (int j = 0; j < original.GetLength(1); j++)
            {
                copy[i, j].ShouldBe(original[i, j]);
            }
        }
    }

    /// <summary>
    /// 测试 - Copy - 修改副本不影响原始数组
    /// </summary>
    [Fact]
    public void Copy_ModifyingCopyDoesNotAffectOriginal()
    {
        // 准备
        byte[,] original = new byte[2, 2] {
            { 1, 2 },
            { 3, 4 }
        };

        // 执行
        var copy = original.Copy();
        copy[0, 0] = 99; // 修改副本

        // 验证 - 原数组不受影响
        original[0, 0].ShouldBe((byte)1);
    }

    /// <summary>
    /// 测试 - Copy - null输入抛出异常
    /// </summary>
    [Fact]
    public void Copy_NullInput_ThrowsException()
    {
        // 准备
        byte[,] nullArray = null;

        // 执行 & 验证
        Should.Throw<ArgumentNullException>(() => nullArray.Copy());
    }

    #endregion
}