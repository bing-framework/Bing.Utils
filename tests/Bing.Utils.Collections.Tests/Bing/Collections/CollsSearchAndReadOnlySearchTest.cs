using System.Collections.Generic;
namespace Bing.Collections;
/// <summary>
/// 可写及只读列表二分查找的参数和结果契约测试。
/// </summary>
[Trait("CollectionsUT", "Colls.Search")]
public class CollsSearchAndReadOnlySearchTest
{
    /// <summary>
    /// 验证升序列表二分查找返回匹配索引。
    /// </summary>
    [Fact]
    public void BinarySearch_SortedNumbers_ReturnsMatchedIndex()
    {
        IList<int> source = new List<int> { 1, 3, 5, 7, 9 };
        var index = Colls.BinarySearch(source, 5);
        index.ShouldBe(2);
    }
    /// <summary>
    /// 验证未命中时返回插入索引的按位补数。
    /// </summary>
    [Fact]
    public void BinarySearch_ValueNotFound_ReturnsComplementOfInsertionIndex()
    {
        IList<int> source = new List<int> { 1, 3, 5, 7, 9 };
        var index = Colls.BinarySearch(source, 6);
        index.ShouldBe(~3);
    }
    /// <summary>
    /// 验证指定范围内按投影键查找返回原列表索引。
    /// </summary>
    [Fact]
    public void BinarySearch_WithMapAndRange_ReturnsMatchedIndex()
    {
        IList<SearchSample> source = new List<SearchSample>
        {
            new() { Id = 10, Name = "a" },
            new() { Id = 20, Name = "b" },
            new() { Id = 30, Name = "c" },
            new() { Id = 40, Name = "d" }
        };
        var index = Colls.BinarySearch(source, 1, 2, x => x.Id, 30);
        index.ShouldBe(2);
    }
    /// <summary>
    /// 验证二分查找扩展方法返回匹配索引。
    /// </summary>
    [Fact]
    public void BinarySearch_ExtensionMethod_WorksAsWrapper()
    {
        IList<int> source = new List<int> { 2, 4, 6, 8 };
        var index = source.BinarySearch(6);
        index.ShouldBe(2);
    }
    /// <summary>
    /// 验证当前简化重载对空源抛出空引用异常。
    /// </summary>
    [Fact]
    public void BinarySearch_WithNullSourceOnShortcutOverload_CurrentlyThrowsNullReferenceException()
    {
        IList<int> source = null;
        Should.Throw<NullReferenceException>(() => Colls.BinarySearch(source, 1));
    }
    /// <summary>
    /// 验证核心重载拒绝空源并报告参数名。
    /// </summary>
    [Fact]
    public void BinarySearch_WithNullSourceOnCoreOverload_ThrowsArgumentNullException()
    {
        IList<int> source = null;
        var ex = Should.Throw<ArgumentNullException>(() =>
            Colls.BinarySearch(source, 0, 0, x => x, 1, Comparer<int>.Default));
        ex.ParamName.ShouldBe("source");
    }
    /// <summary>
    /// 验证二分查找拒绝空映射函数并报告参数名。
    /// </summary>
    [Fact]
    public void BinarySearch_WithNullMap_ThrowsArgumentNullException()
    {
        IList<int> source = new List<int> { 1, 2, 3 };
        var ex = Should.Throw<ArgumentNullException>(() =>
            Colls.BinarySearch(source, 0, 3, (Func<int, int>)null, 2, Comparer<int>.Default));
        ex.ParamName.ShouldBe("map");
    }
    /// <summary>
    /// 验证二分查找拒绝空比较器并报告参数名。
    /// </summary>
    [Fact]
    public void BinarySearch_WithNullComparer_ThrowsArgumentNullException()
    {
        IList<int> source = new List<int> { 1, 2, 3 };
        var ex = Should.Throw<ArgumentNullException>(() =>
            Colls.BinarySearch(source, 0, 3, x => x, 2, null));
        ex.ParamName.ShouldBe("comparer");
    }
    /// <summary>
    /// 验证二分查找拒绝负起始索引或长度。
    /// </summary>
    /// <param name="index">查找起始索引。</param>
    /// <param name="length">查找范围长度。</param>
    [Theory]
    [InlineData(-1, 1)]
    [InlineData(0, -1)]
    public void BinarySearch_WithNegativeIndexOrLength_ThrowsArgumentOutOfRangeException(int index, int length)
    {
        IList<int> source = new List<int> { 1, 2, 3 };
        Should.Throw<ArgumentOutOfRangeException>(() =>
            Colls.BinarySearch(source, index, length, x => x, 2, Comparer<int>.Default));
    }
    /// <summary>
    /// 验证二分查找拒绝超出源列表的范围。
    /// </summary>
    [Fact]
    public void BinarySearch_WithIndexAndLengthBeyondSource_ThrowsInvalidOperationException()
    {
        IList<int> source = new List<int> { 1, 2, 3 };
        var ex = Should.Throw<InvalidOperationException>(() =>
            Colls.BinarySearch(source, 2, 2, x => x, 2, Comparer<int>.Default));
        ex.Message.ShouldContain("must be less than or equal");
    }

    /// <summary>
    /// 验证空范围返回起始索引的按位补数。
    /// </summary>
    [Fact]
    public void BinarySearch_LengthIsZero_ReturnsComplementOfStartIndex()
    {
        IList<int> source = new List<int> { 1, 3, 5 };

        var index = Colls.BinarySearch(source, 1, 0, x => x, 3, Comparer<int>.Default);

        index.ShouldBe(~1);
    }

    /// <summary>
    /// 验证降序列表使用对应比较器查找。
    /// </summary>
    [Fact]
    public void BinarySearch_DescendingComparer_ReturnsMatchedIndex()
    {
        IList<int> source = new List<int> { 9, 7, 5, 3, 1 };
        var comparer = Comparer<int>.Create((x, y) => y.CompareTo(x));

        var index = Colls.BinarySearch(source, 5, comparer);

        index.ShouldBe(2);
    }

    /// <summary>
    /// 验证当前扩展重载对空源抛出空引用异常。
    /// </summary>
    [Fact]
    public void BinarySearch_ExtensionWithNullSource_CurrentlyThrowsNullReferenceException()
    {
        IList<int> source = null;

        Should.Throw<NullReferenceException>(() => source.BinarySearch(1));
    }
    /// <summary>
    /// 验证只读升序列表二分查找返回匹配索引。
    /// </summary>
    [Fact]
    public void ReadOnlyBinarySearch_SortedNumbers_ReturnsMatchedIndex()
    {
        IReadOnlyList<int> source = new List<int> { 1, 3, 5, 7, 9 };
        var index = ReadOnlyColls.BinarySearch(source, 7);
        index.ShouldBe(3);
    }
    /// <summary>
    /// 验证只读列表未命中时返回插入索引的按位补数。
    /// </summary>
    [Fact]
    public void ReadOnlyBinarySearch_ValueNotFound_ReturnsComplementOfInsertionIndex()
    {
        IReadOnlyList<int> source = new List<int> { 1, 3, 5, 7, 9 };
        var index = ReadOnlyColls.BinarySearch(source, 4);
        index.ShouldBe(~2);
    }
    /// <summary>
    /// 验证只读列表核心重载拒绝空源并报告参数名。
    /// </summary>
    [Fact]
    public void ReadOnlyBinarySearch_WithNullSource_ThrowsArgumentNullException()
    {
        IReadOnlyList<int> source = null;
        var ex = Should.Throw<ArgumentNullException>(() =>
            ReadOnlyColls.BinarySearch(source, 0, 0, x => x, 1, Comparer<int>.Default));
        ex.ParamName.ShouldBe("source");
    }

    /// <summary>
    /// 验证只读列表当前简化重载对空源抛出空引用异常。
    /// </summary>
    [Fact]
    public void ReadOnlyBinarySearch_ShortcutOverloadNullSource_CurrentlyThrowsNullReferenceException()
    {
        IReadOnlyList<int> source = null;

        Should.Throw<NullReferenceException>(() => ReadOnlyColls.BinarySearch(source, 1));
    }

    /// <summary>
    /// 验证只读列表二分查找拒绝空映射函数并报告参数名。
    /// </summary>
    [Fact]
    public void ReadOnlyBinarySearch_NullMap_ThrowsArgumentNullException()
    {
        IReadOnlyList<int> source = new List<int> { 1, 2, 3 };

        var ex = Should.Throw<ArgumentNullException>(() =>
            ReadOnlyColls.BinarySearch(source, 0, 3, (Func<int, int>)null, 2, Comparer<int>.Default));

        ex.ParamName.ShouldBe("map");
    }

    /// <summary>
    /// 验证只读列表二分查找拒绝空比较器并报告参数名。
    /// </summary>
    [Fact]
    public void ReadOnlyBinarySearch_NullComparer_ThrowsArgumentNullException()
    {
        IReadOnlyList<int> source = new List<int> { 1, 2, 3 };

        var ex = Should.Throw<ArgumentNullException>(() =>
            ReadOnlyColls.BinarySearch(source, 0, 3, x => x, 2, null));

        ex.ParamName.ShouldBe("comparer");
    }

    /// <summary>
    /// 验证只读列表二分查找拒绝负起始索引或长度。
    /// </summary>
    /// <param name="index">查找起始索引。</param>
    /// <param name="length">查找范围长度。</param>
    [Theory]
    [InlineData(-1, 1)]
    [InlineData(0, -1)]
    public void ReadOnlyBinarySearch_NegativeIndexOrLength_ThrowsArgumentOutOfRangeException(int index, int length)
    {
        IReadOnlyList<int> source = new List<int> { 1, 2, 3 };

        Should.Throw<ArgumentOutOfRangeException>(() =>
            ReadOnlyColls.BinarySearch(source, index, length, x => x, 2, Comparer<int>.Default));
    }

    /// <summary>
    /// 验证只读列表二分查找拒绝超出源列表的范围。
    /// </summary>
    [Fact]
    public void ReadOnlyBinarySearch_IndexAndLengthBeyondSource_ThrowsInvalidOperationException()
    {
        IReadOnlyList<int> source = new List<int> { 1, 2, 3 };

        var ex = Should.Throw<InvalidOperationException>(() =>
            ReadOnlyColls.BinarySearch(source, 2, 2, x => x, 2, Comparer<int>.Default));

        ex.Message.ShouldContain("must be less than or equal");
    }

    /// <summary>
    /// 验证只读列表空范围返回起始索引的按位补数。
    /// </summary>
    [Fact]
    public void ReadOnlyBinarySearch_LengthIsZero_ReturnsComplementOfStartIndex()
    {
        IReadOnlyList<int> source = new List<int> { 1, 3, 5 };

        var index = ReadOnlyColls.BinarySearch(source, 1, 0, x => x, 3, Comparer<int>.Default);

        index.ShouldBe(~1);
    }
    /// <summary>
    /// 二分查找键映射使用的测试模型。
    /// </summary>
    private sealed class SearchSample
    {
        /// <summary>
        /// 获取或设置排序和查找使用的标识符。
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// 获取或设置测试模型名称。
        /// </summary>
        public string Name { get; set; }
    }
}

