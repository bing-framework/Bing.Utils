using System.Collections.ObjectModel;
using Bing.Collections;

namespace Bing.Utils.Tests.Bing.Collections;

/// <summary>
/// 验证集合、列表、可枚举对象和数组扩展的行为。
/// </summary>
public class CollectionAndListExtensionsTests
{
    // ─────────────────────────────────────────────────────────────────
    // BingCollectionExtensions — AddRange
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证批量追加全部元素。
    /// </summary>
    [Fact]
    public void AddRange_AddsAllItems()
    {
        ICollection<int> col = new List<int> { 1 };
        col.AddRange(new[] { 2, 3, 4 });
        col.Count.ShouldBe(4);
        col.ShouldContain(4);
    }

    /// <summary>
    /// 验证批量追加拒绝空目标集合。
    /// </summary>
    [Fact]
    public void AddRange_NullCollection_ThrowsArgumentNullException() =>
        Should.Throw<ArgumentNullException>(() =>
            ((ICollection<int>)null!).AddRange(new[] { 1 }));

    /// <summary>
    /// 验证批量追加拒绝空元素序列。
    /// </summary>
    [Fact]
    public void AddRange_NullItems_ThrowsArgumentNullException() =>
        Should.Throw<ArgumentNullException>(() =>
            new List<int>().AddRange(null!));

    /// <summary>
    /// 验证批量追加返回新增元素数量。
    /// </summary>
    [Fact]
    public void AddRange_ReturnsCountOfAddedItems()
    {
        ICollection<int> col = new List<int>();
        var added = col.AddRange(new[] { 10, 20, 30 });
        added.ShouldBe(3);
    }

    // ─────────────────────────────────────────────────────────────────
    // BingCollectionExtensions — Sort
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证默认比较器将集合升序排列。
    /// </summary>
    [Fact]
    public void Sort_DefaultComparer_SortsAscending()
    {
        ICollection<int> col = new List<int> { 3, 1, 4, 1, 5, 9 };
        col.Sort();
        col.ShouldBe(new[] { 1, 1, 3, 4, 5, 9 });
    }

    /// <summary>
    /// 验证自定义降序比较器将最大元素排在首位。
    /// </summary>
    [Fact]
    public void Sort_CustomComparer_SortsDescending()
    {
        ICollection<int> col = new List<int> { 3, 1, 4, 1, 5, 9 };
        col.Sort(Comparer<int>.Create((a, b) => b.CompareTo(a)));
        col.First().ShouldBe(9);
    }

    // ─────────────────────────────────────────────────────────────────
    // BingCollectionExtensions — ReplaceItems
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证替换操作清空原元素并写入新元素。
    /// </summary>
    [Fact]
    public void ReplaceItems_ReplacesAllElements()
    {
        ICollection<int> col = new List<int> { 1, 2, 3 };
        col.ReplaceItems(new[] { 10, 20 });
        col.ShouldBe(new[] { 10, 20 });
    }

    /// <summary>
    /// 验证转换新元素后替换原集合内容。
    /// </summary>
    [Fact]
    public void ReplaceItems_WithTransform_TransformsAndReplaces()
    {
        ICollection<string> col = new List<string> { "a", "b" };
        col.ReplaceItems(new[] { 1, 2 }, i => i.ToString());
        col.ShouldBe(new[] { "1", "2" });
    }

    // ─────────────────────────────────────────────────────────────────
    // BingCollectionExtensions — ToObservableCollection
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证创建可观察集合且保留元素数量。
    /// </summary>
    [Fact]
    public void ToObservableCollection_CreatesNewCollection()
    {
        ICollection<int> col = new List<int> { 1, 2, 3 };
        var obs = col.ToObservableCollection();
        obs.ShouldBeOfType<ObservableCollection<int>>();
        obs.Count.ShouldBe(3);
    }

    /// <summary>
    /// 验证填充已有可观察集合前清空原内容。
    /// </summary>
    [Fact]
    public void ToObservableCollection_WithTarget_ClearsAndFills()
    {
        ICollection<int> col = new List<int> { 10, 20 };
        var existing = new ObservableCollection<int> { 99 };
        col.ToObservableCollection(existing);
        existing.Count.ShouldBe(2);
        existing[0].ShouldBe(10);
    }

    // ─────────────────────────────────────────────────────────────────
    // BingListExtensions — InsertRange / FindIndex / AddFirst / AddLast
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证在指定索引按顺序插入全部元素。
    /// </summary>
    [Fact]
    public void InsertRange_InsertsAtCorrectPosition()
    {
        IList<int> list = new List<int> { 1, 5 };
        list.InsertRange(1, new[] { 2, 3, 4 });
        list.ShouldBe(new[] { 1, 2, 3, 4, 5 });
    }

    /// <summary>
    /// 验证条件匹配返回对应索引。
    /// </summary>
    [Fact]
    public void FindIndex_MatchFound_ReturnsIndex()
    {
        IList<int> list = new List<int> { 10, 20, 30 };
        list.FindIndex(x => x == 20).ShouldBe(1);
    }

    /// <summary>
    /// 验证条件未匹配时返回负一。
    /// </summary>
    [Fact]
    public void FindIndex_NoMatch_ReturnsMinusOne()
    {
        IList<int> list = new List<int> { 10, 20, 30 };
        list.FindIndex(x => x == 99).ShouldBe(-1);
    }

    /// <summary>
    /// 验证元素插入到列表首位。
    /// </summary>
    [Fact]
    public void AddFirst_InsertsAtBeginning()
    {
        IList<int> list = new List<int> { 2, 3 };
        list.AddFirst(1);
        list[0].ShouldBe(1);
    }

    /// <summary>
    /// 验证元素追加到列表末尾。
    /// </summary>
    [Fact]
    public void AddLast_InsertsAtEnd()
    {
        IList<int> list = new List<int> { 1, 2 };
        list.AddLast(3);
        list[^1].ShouldBe(3);
    }

    // ─────────────────────────────────────────────────────────────────
    // BingListExtensions — InsertAfter / InsertBefore
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证元素插入到指定现有元素之后。
    /// </summary>
    [Fact]
    public void InsertAfter_ExistingItem_InsertsAfterIt()
    {
        IList<int> list = new List<int> { 1, 2, 4 };
        list.InsertAfter(2, 3);
        list.ShouldBe(new[] { 1, 2, 3, 4 });
    }

    /// <summary>
    /// 验证定位元素不存在时插入到列表首位。
    /// </summary>
    [Fact]
    public void InsertAfter_NotFound_InsertsAtFront()
    {
        IList<int> list = new List<int> { 2, 3 };
        list.InsertAfter(99, 0);
        list[0].ShouldBe(0);
    }

    /// <summary>
    /// 验证元素插入到谓词匹配项之后。
    /// </summary>
    [Fact]
    public void InsertAfter_Predicate_InsertsAfterMatch()
    {
        IList<int> list = new List<int> { 1, 2, 4 };
        list.InsertAfter(x => x == 2, 3);
        list.ShouldBe(new[] { 1, 2, 3, 4 });
    }

    /// <summary>
    /// 验证元素插入到指定现有元素之前。
    /// </summary>
    [Fact]
    public void InsertBefore_ExistingItem_InsertsBeforeIt()
    {
        IList<int> list = new List<int> { 1, 3, 4 };
        list.InsertBefore(3, 2);
        list.ShouldBe(new[] { 1, 2, 3, 4 });
    }

    /// <summary>
    /// 验证定位元素不存在时插入到列表末尾。
    /// </summary>
    [Fact]
    public void InsertBefore_NotFound_InsertsAtEnd()
    {
        IList<int> list = new List<int> { 1, 2 };
        list.InsertBefore(99, 3);
        list[^1].ShouldBe(3);
    }

    // ─────────────────────────────────────────────────────────────────
    // BingListExtensions — ReplaceWhile / ReplaceOne
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证所有匹配项替换为给定值。
    /// </summary>
    [Fact]
    public void ReplaceWhile_Predicate_ReplacesAllMatches()
    {
        IList<int> list = new List<int> { 1, 2, 2, 3 };
        list.ReplaceWhile(x => x == 2, 99);
        list.ShouldBe(new[] { 1, 99, 99, 3 });
    }

    /// <summary>
    /// 验证工厂函数转换全部匹配项。
    /// </summary>
    [Fact]
    public void ReplaceWhile_Factory_ReplacesAllMatchesWithTransform()
    {
        IList<int> list = new List<int> { 1, 2, 3 };
        list.ReplaceWhile(x => x % 2 == 0, x => x * 10);
        list.ShouldBe(new[] { 1, 20, 3 });
    }

    /// <summary>
    /// 验证谓词替换只修改首个匹配项。
    /// </summary>
    [Fact]
    public void ReplaceOne_Predicate_ReplacesOnlyFirstMatch()
    {
        IList<int> list = new List<int> { 2, 2, 3 };
        list.ReplaceOne(x => x == 2, 99);
        list.ShouldBe(new[] { 99, 2, 3 });
    }

    /// <summary>
    /// 验证按元素替换只修改首次出现项。
    /// </summary>
    [Fact]
    public void ReplaceOne_Item_ReplacesFirstOccurrence()
    {
        IList<int> list = new List<int> { 1, 2, 2, 3 };
        list.ReplaceOne(2, 99);
        list.ShouldBe(new[] { 1, 99, 2, 3 });
    }

    // ─────────────────────────────────────────────────────────────────
    // BingListExtensions — MoveItem / ChunkByView
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证匹配元素移动到目标索引。
    /// </summary>
    [Fact]
    public void MoveItem_MovesItemToTargetIndex()
    {
        var list = new List<int> { 1, 2, 3, 4, 5 };
        list.MoveItem(x => x == 5, 0);
        list[0].ShouldBe(5);
    }

    /// <summary>
    /// 验证列表按块大小分组且保留不足整块的末组。
    /// </summary>
    [Fact]
    public void ChunkByView_SplitsCorrectly()
    {
        var list = new List<int> { 1, 2, 3, 4, 5 };
        var chunks = list.ChunkByView(2).ToList();
        chunks.Count.ShouldBe(3);
        chunks[0].ShouldBe(new[] { 1, 2 });
        chunks[2].ShouldBe(new[] { 5 });
    }

    /// <summary>
    /// 验证列表分块拒绝空源。
    /// </summary>
    [Fact]
    public void ChunkByView_NullSource_ThrowsArgumentNullException() =>
        Should.Throw<ArgumentNullException>(() => ((List<int>)null!).ChunkByView(2).ToList());

    /// <summary>
    /// 验证列表分块拒绝零块大小。
    /// </summary>
    [Fact]
    public void ChunkByView_ZeroChunkSize_ThrowsArgumentOutOfRangeException() =>
        Should.Throw<ArgumentOutOfRangeException>(() => new List<int> { 1 }.ChunkByView(0).ToList());

    // ─────────────────────────────────────────────────────────────────
    // BingEnumerableExtensions — ChunkBy
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证序列按块大小分组且保留不足整块的末组。
    /// </summary>
    [Fact]
    public void ChunkBy_SplitsEnumerableCorrectly()
    {
        IEnumerable<int> src = new[] { 1, 2, 3, 4, 5 };
        var chunks = src.ChunkBy(2).ToList();
        chunks.Count.ShouldBe(3);
        chunks[0].ShouldBe(new[] { 1, 2 });
        chunks[2].ShouldBe(new[] { 5 });
    }

    /// <summary>
    /// 验证序列分块拒绝空源。
    /// </summary>
    [Fact]
    public void ChunkBy_NullSource_ThrowsArgumentNullException() =>
        Should.Throw<ArgumentNullException>(() =>
            ((IEnumerable<int>)null!).ChunkBy(2).ToList());

    // ─────────────────────────────────────────────────────────────────
    // BingEnumerableExtensions — ForEach
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证逐项回调访问全部元素。
    /// </summary>
    [Fact]
    public void ForEach_VisitsAllItems()
    {
        var sum = 0;
        new[] { 1, 2, 3 }.ForEach(x => sum += x);
        sum.ShouldBe(6);
    }

    /// <summary>
    /// 验证逐项回调收到从零开始的连续索引。
    /// </summary>
    [Fact]
    public void ForEach_WithIndex_PassesCorrectIndex()
    {
        var indices = new List<int>();
        new[] { 'a', 'b', 'c' }.ForEach((_, i) => indices.Add(i));
        indices.ShouldBe(new[] { 0, 1, 2 });
    }

    /// <summary>
    /// 验证逐项回调拒绝空源。
    /// </summary>
    [Fact]
    public void ForEach_NullSource_ThrowsArgumentNullException() =>
        Should.Throw<ArgumentNullException>(() =>
            ((IEnumerable<int>)null!).ForEach(_ => { }));

    /// <summary>
    /// 验证逐项遍历拒绝空回调。
    /// </summary>
    [Fact]
    public void ForEach_NullAction_ThrowsArgumentNullException() =>
        Should.Throw<ArgumentNullException>(() =>
            new[] { 1 }.ForEach((Action<int>)null!));

    // ─────────────────────────────────────────────────────────────────
    // BingEnumerableExtensions — ForEachAsync
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证异步逐项回调处理全部元素。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [Fact]
    public async Task ForEachAsync_ExecutesAllItems()
    {
        var results = new System.Collections.Concurrent.ConcurrentBag<int>();
        await new[] { 1, 2, 3 }.ForEachAsync(async x =>
        {
            await Task.Yield();
            results.Add(x);
        });
        results.Count.ShouldBe(3);
    }

    /// <summary>
    /// 验证指定并发度的异步逐项回调处理全部元素。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [Fact]
    public async Task ForEachAsync_WithMaxParallelism_ExecutesAllItems()
    {
        var results = new System.Collections.Concurrent.ConcurrentBag<int>();
        await Enumerable.Range(1, 10).ForEachAsync(async x =>
        {
            await Task.Delay(1);
            results.Add(x);
        }, maxDegreeOfParallelism: 3);
        results.Count.ShouldBe(10);
    }

    // ─────────────────────────────────────────────────────────────────
    // ArrayExtensions — WithInIndex
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证数组内部索引在有效范围内。
    /// </summary>
    [Fact]
    public void WithInIndex_ValidIndex_ReturnsTrue()
    {
        var arr = new[] { 1, 2, 3 };
        arr.WithInIndex(1).ShouldBeTrue();
    }

    /// <summary>
    /// 验证数组首尾索引在有效范围内。
    /// </summary>
    [Fact]
    public void WithInIndex_IndexAtBoundary_ReturnsTrue()
    {
        var arr = new[] { 1, 2, 3 };
        arr.WithInIndex(0).ShouldBeTrue();
        arr.WithInIndex(2).ShouldBeTrue();
    }

    /// <summary>
    /// 验证负数组索引无效。
    /// </summary>
    [Fact]
    public void WithInIndex_NegativeIndex_ReturnsFalse()
    {
        var arr = new[] { 1, 2, 3 };
        arr.WithInIndex(-1).ShouldBeFalse();
    }

    /// <summary>
    /// 验证等于数组长度的索引无效。
    /// </summary>
    [Fact]
    public void WithInIndex_OutOfBoundsIndex_ReturnsFalse()
    {
        var arr = new[] { 1, 2, 3 };
        arr.WithInIndex(3).ShouldBeFalse();
    }

    /// <summary>
    /// 验证空数组引用的索引无效。
    /// </summary>
    [Fact]
    public void WithInIndex_NullArray_ReturnsFalse() =>
        ((Array)null!).WithInIndex(0).ShouldBeFalse();

    /// <summary>
    /// 验证二维数组指定维度的有效索引。
    /// </summary>
    [Fact]
    public void WithInIndex_2D_ValidIndex_ReturnsTrue()
    {
        var arr = new int[3, 3];
        arr.WithInIndex(1, 0).ShouldBeTrue();
        arr.WithInIndex(1, 1).ShouldBeTrue();
    }

    /// <summary>
    /// 验证二维数组拒绝无效维度。
    /// </summary>
    [Fact]
    public void WithInIndex_2D_InvalidDimension_ThrowsArgumentOutOfRangeException()
    {
        var arr = new int[3, 3];
        Should.Throw<ArgumentOutOfRangeException>(() => arr.WithInIndex(0, -1));
        Should.Throw<ArgumentOutOfRangeException>(() => arr.WithInIndex(0, 3));
    }
}
