namespace Bing.Collections;

/// <summary>
/// 测试类：覆盖 ArraysShortcutExtensions.BlockCopy 的边界与契约行为。
/// </summary>
[Trait("CollectionsUT", "ArraysShortcut.BlockCopy")]
public class ArraysShortcutBlockCopyContractTest
{
    /// <summary>
    /// 测试用例：BlockCopy 在基础值类型数组上应按字节拷贝指定长度。
    /// </summary>
    [Fact]
    public void BlockCopy_PrimitiveArrays_CopiesRequestedBytes()
    {
        short[] source = [0x0102, 0x0304];
        short[] destination = [0, 0];

        source.BlockCopy(0, destination, 0, 4);

        destination.ShouldBe(source);
    }

    /// <summary>
    /// 测试用例：BlockCopy 在引用类型数组上应抛出 ArgumentException。
    /// </summary>
    [Fact]
    public void BlockCopy_ReferenceTypeArrays_ThrowsArgumentException()
    {
        Array source = new string[] { "A", "B" };
        Array destination = new string[2];

        Should.Throw<ArgumentException>(() => source.BlockCopy(0, destination, 0, 1));
    }
}
