using System.Collections.ObjectModel;
using System.Collections.Generic;
namespace Bing.Collections;
/// <summary>
/// 测试类：覆盖 `ReadOnlyCollConv` 相关行为。
/// </summary>
[Trait("CollectionsUT", "ReadOnlyCollConv")]
public class ReadOnlyCollConvTest
{
    /// <summary>
    /// 测试用例：验证 `AsReadOnly` 在 `NullSource` 场景下，结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void AsReadOnly_NullSource_ThrowsArgumentNullException()
    {
        IEnumerable<int> source = null;
        var ex = Should.Throw<ArgumentNullException>(() => source.AsReadOnly());
        ex.ParamName.ShouldBe("source");
    }
    /// <summary>
    /// 测试用例：验证 `AsReadOnly` 在 `ValidSource` 场景下，结果为 `ReturnsSnapshotCollection`。
    /// </summary>
    [Fact]
    public void AsReadOnly_ValidSource_ReturnsSnapshotCollection()
    {
        var sourceList = new List<int> { 1, 2, 3 };
        IEnumerable<int> source = sourceList;
        ReadOnlyCollection<int> readOnly = source.AsReadOnly();
        sourceList.Add(4);
        readOnly.Count.ShouldBe(3);
        readOnly.ShouldBe(new[] { 1, 2, 3 });
    }
    /// <summary>
    /// 测试用例：验证 `AsList` 在 `NullSource` 场景下，结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void AsList_NullSource_ThrowsArgumentNullException()
    {
        IReadOnlyList<int> source = null;
        var ex = Should.Throw<ArgumentNullException>(() => ReadOnlyCollConv.AsList(source));
        ex.ParamName.ShouldBe("list");
    }
    /// <summary>
    /// 测试用例：验证 `AsList` 在 `ReadOnlySource` 场景下，结果为 `ModificationApisThrowNotSupportedException`。
    /// </summary>
    [Fact]
    public void AsList_ReadOnlySource_ModificationApisThrowNotSupportedException()
    {
        IReadOnlyList<string> source = new List<string> { "A", "B" };
        var list = source.AsList();
        list.Count.ShouldBe(2);
        list[0].ShouldBe("A");
        Should.Throw<NotSupportedException>(() => list.Add("C"));
        Should.Throw<NotSupportedException>(() => list[0] = "X");
        Should.Throw<NotSupportedException>(() => list.RemoveAt(0));
    }

    /// <summary>
    /// 测试用例：验证 `AsList` 在 `BackingListMutated` 场景下，结果为 `ReflectsLatestUnderlyingData`。
    /// </summary>
    [Fact]
    public void AsList_BackingListMutated_ReflectsLatestUnderlyingData()
    {
        var backing = new List<int> { 1, 2 };
        IReadOnlyList<int> source = backing;
        var list = source.AsList();

        backing.Add(3);
        backing[0] = 9;

        list.Count.ShouldBe(3);
        list.ShouldBe(new[] { 9, 2, 3 });
    }

    /// <summary>
    /// 测试用例：验证 `AsList` 在 `MissingItemIndexOf` 场景下，结果为 `ReturnsMinusOne`。
    /// </summary>
    [Fact]
    public void AsList_MissingItemIndexOf_ReturnsMinusOne()
    {
        IReadOnlyList<string> source = new List<string> { "A", "B" };
        var list = source.AsList();

        var index = list.IndexOf("X");

        index.ShouldBe(-1);
    }

    /// <summary>
    /// 测试用例：验证 `AsList` 在 `ContainsNullItem` 场景下，结果为 `ReturnsTrue`。
    /// </summary>
    [Fact]
    public void AsList_ContainsNullItem_ReturnsTrue()
    {
        IReadOnlyList<string> source = new List<string> { "A", null, "B" };
        var list = source.AsList();

        list.Contains(null).ShouldBeTrue();
    }
}

