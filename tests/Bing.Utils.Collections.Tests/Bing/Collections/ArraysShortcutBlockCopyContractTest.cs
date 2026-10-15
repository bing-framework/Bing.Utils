namespace Bing.Collections;

/// <summary>
/// 数组按字节块复制的类型契约测试。
/// </summary>
[Trait("CollectionsUT", "ArraysShortcut.BlockCopy")]
public class ArraysShortcutBlockCopyContractTest
{
    /// <summary>
    /// 验证基本值类型数组按指定字节数复制。
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
    /// 验证引用类型数组不能执行字节块复制。
    /// </summary>
    [Fact]
    public void BlockCopy_ReferenceTypeArrays_ThrowsArgumentException()
    {
        Array source = new string[] { "A", "B" };
        Array destination = new string[2];

        Should.Throw<ArgumentException>(() => source.BlockCopy(0, destination, 0, 1));
    }
}
