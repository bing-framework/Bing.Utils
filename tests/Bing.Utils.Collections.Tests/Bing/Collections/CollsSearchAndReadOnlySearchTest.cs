using System.Collections.Generic;
namespace Bing.Collections;
/// <summary>
/// 测试类：覆盖 `CollsSearchAndReadOnlySearch` 相关行为。
/// </summary>
[Trait("CollectionsUT", "Colls.Search")]
public class CollsSearchAndReadOnlySearchTest
{
    /// <summary>
    /// 测试用例：验证 `BinarySearch` 在 `SortedNumbers` 场景下，结果为 `ReturnsMatchedIndex`。
    /// </summary>
    [Fact]
    public void BinarySearch_SortedNumbers_ReturnsMatchedIndex()
    {
        IList<int> source = new List<int> { 1, 3, 5, 7, 9 };
        var index = Colls.BinarySearch(source, 5);
        index.ShouldBe(2);
    }
    /// <summary>
    /// 测试用例：验证 `BinarySearch` 在 `ValueNotFound` 场景下，结果为 `ReturnsComplementOfInsertionIndex`。
    /// </summary>
    [Fact]
    public void BinarySearch_ValueNotFound_ReturnsComplementOfInsertionIndex()
    {
        IList<int> source = new List<int> { 1, 3, 5, 7, 9 };
        var index = Colls.BinarySearch(source, 6);
        index.ShouldBe(~3);
    }
    /// <summary>
    /// 测试用例：验证 `BinarySearch` 在 `WithMapAndRange` 场景下，结果为 `ReturnsMatchedIndex`。
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
    /// 测试用例：验证 `BinarySearch` 在 `ExtensionMethod` 场景下，结果为 `WorksAsWrapper`。
    /// </summary>
    [Fact]
    public void BinarySearch_ExtensionMethod_WorksAsWrapper()
    {
        IList<int> source = new List<int> { 2, 4, 6, 8 };
        var index = source.BinarySearch(6);
        index.ShouldBe(2);
    }
    /// <summary>
    /// 测试用例：验证 `BinarySearch` 在 `WithNullSourceOnShortcutOverload` 场景下，结果为 `CurrentlyThrowsNullReferenceException`。
    /// </summary>
    [Fact]
    public void BinarySearch_WithNullSourceOnShortcutOverload_CurrentlyThrowsNullReferenceException()
    {
        IList<int> source = null;
        Should.Throw<NullReferenceException>(() => Colls.BinarySearch(source, 1));
    }
    /// <summary>
    /// 测试用例：验证 `BinarySearch` 在 `WithNullSourceOnCoreOverload` 场景下，结果为 `ThrowsArgumentNullException`。
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
    /// 测试用例：验证 `BinarySearch` 在 `WithNullMap` 场景下，结果为 `ThrowsArgumentNullException`。
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
    /// 测试用例：验证 `BinarySearch` 在 `WithNullComparer` 场景下，结果为 `ThrowsArgumentNullException`。
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
    /// 测试用例：验证 `BinarySearch` 在 `WithNegativeIndexOrLength` 场景下，结果为 `ThrowsArgumentOutOfRangeException`。
    /// </summary>
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
    /// 测试用例：验证 `BinarySearch` 在 `WithIndexAndLengthBeyondSource` 场景下，结果为 `ThrowsInvalidOperationException`。
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
    /// 测试用例：验证 `BinarySearch` 在 `LengthIsZero` 场景下，结果为 `ReturnsComplementOfStartIndex`。
    /// </summary>
    [Fact]
    public void BinarySearch_LengthIsZero_ReturnsComplementOfStartIndex()
    {
        IList<int> source = new List<int> { 1, 3, 5 };

        var index = Colls.BinarySearch(source, 1, 0, x => x, 3, Comparer<int>.Default);

        index.ShouldBe(~1);
    }

    /// <summary>
    /// 测试用例：验证 `BinarySearch` 在 `DescendingComparer` 场景下，结果为 `ReturnsMatchedIndex`。
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
    /// 测试用例：验证 `BinarySearch` 扩展重载在 `NullSource` 场景下，结果为 `CurrentlyThrowsNullReferenceException`。
    /// </summary>
    [Fact]
    public void BinarySearch_ExtensionWithNullSource_CurrentlyThrowsNullReferenceException()
    {
        IList<int> source = null;

        Should.Throw<NullReferenceException>(() => source.BinarySearch(1));
    }
    /// <summary>
    /// 测试用例：验证 `ReadOnlyBinarySearch` 在 `SortedNumbers` 场景下，结果为 `ReturnsMatchedIndex`。
    /// </summary>
    [Fact]
    public void ReadOnlyBinarySearch_SortedNumbers_ReturnsMatchedIndex()
    {
        IReadOnlyList<int> source = new List<int> { 1, 3, 5, 7, 9 };
        var index = ReadOnlyColls.BinarySearch(source, 7);
        index.ShouldBe(3);
    }
    /// <summary>
    /// 测试用例：验证 `ReadOnlyBinarySearch` 在 `ValueNotFound` 场景下，结果为 `ReturnsComplementOfInsertionIndex`。
    /// </summary>
    [Fact]
    public void ReadOnlyBinarySearch_ValueNotFound_ReturnsComplementOfInsertionIndex()
    {
        IReadOnlyList<int> source = new List<int> { 1, 3, 5, 7, 9 };
        var index = ReadOnlyColls.BinarySearch(source, 4);
        index.ShouldBe(~2);
    }
    /// <summary>
    /// 测试用例：验证 `ReadOnlyBinarySearch` 在 `WithNullSource` 场景下，结果为 `ThrowsArgumentNullException`。
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
    /// 测试用例：验证 `ReadOnlyBinarySearch` 在 `ShortcutOverloadNullSource` 场景下，结果为 `CurrentlyThrowsNullReferenceException`。
    /// </summary>
    [Fact]
    public void ReadOnlyBinarySearch_ShortcutOverloadNullSource_CurrentlyThrowsNullReferenceException()
    {
        IReadOnlyList<int> source = null;

        Should.Throw<NullReferenceException>(() => ReadOnlyColls.BinarySearch(source, 1));
    }

    /// <summary>
    /// 测试用例：验证 `ReadOnlyBinarySearch` 在 `NullMap` 场景下，结果为 `ThrowsArgumentNullException`。
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
    /// 测试用例：验证 `ReadOnlyBinarySearch` 在 `NullComparer` 场景下，结果为 `ThrowsArgumentNullException`。
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
    /// 测试用例：验证 `ReadOnlyBinarySearch` 在 `NegativeIndexOrLength` 场景下，结果为 `ThrowsArgumentOutOfRangeException`。
    /// </summary>
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
    /// 测试用例：验证 `ReadOnlyBinarySearch` 在 `IndexAndLengthBeyondSource` 场景下，结果为 `ThrowsInvalidOperationException`。
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
    /// 测试用例：验证 `ReadOnlyBinarySearch` 在 `LengthIsZero` 场景下，结果为 `ReturnsComplementOfStartIndex`。
    /// </summary>
    [Fact]
    public void ReadOnlyBinarySearch_LengthIsZero_ReturnsComplementOfStartIndex()
    {
        IReadOnlyList<int> source = new List<int> { 1, 3, 5 };

        var index = ReadOnlyColls.BinarySearch(source, 1, 0, x => x, 3, Comparer<int>.Default);

        index.ShouldBe(~1);
    }
    private sealed class SearchSample
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}

