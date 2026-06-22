using System.Collections.ObjectModel;
using Bing.Collections;

namespace Bing.Utils.Tests.Bing.Collections;

/// <summary>
/// <see cref="BingCollectionExtensions"/> / <see cref="BingListExtensions"/> /
/// <see cref="BingEnumerableExtensions"/> / <see cref="ArrayExtensions"/> 单元测试
/// </summary>
public class CollectionAndListExtensionsTests
{
    // ─────────────────────────────────────────────────────────────────
    // BingCollectionExtensions — AddRange
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void AddRange_AddsAllItems()
    {
        ICollection<int> col = new List<int> { 1 };
        col.AddRange(new[] { 2, 3, 4 });
        col.Count.ShouldBe(4);
        col.ShouldContain(4);
    }

    [Fact]
    public void AddRange_NullCollection_ThrowsArgumentNullException() =>
        Should.Throw<ArgumentNullException>(() =>
            ((ICollection<int>)null!).AddRange(new[] { 1 }));

    [Fact]
    public void AddRange_NullItems_ThrowsArgumentNullException() =>
        Should.Throw<ArgumentNullException>(() =>
            new List<int>().AddRange(null!));

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

    [Fact]
    public void Sort_DefaultComparer_SortsAscending()
    {
        ICollection<int> col = new List<int> { 3, 1, 4, 1, 5, 9 };
        col.Sort();
        col.ShouldBe(new[] { 1, 1, 3, 4, 5, 9 });
    }

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

    [Fact]
    public void ReplaceItems_ReplacesAllElements()
    {
        ICollection<int> col = new List<int> { 1, 2, 3 };
        col.ReplaceItems(new[] { 10, 20 });
        col.ShouldBe(new[] { 10, 20 });
    }

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

    [Fact]
    public void ToObservableCollection_CreatesNewCollection()
    {
        ICollection<int> col = new List<int> { 1, 2, 3 };
        var obs = col.ToObservableCollection();
        obs.ShouldBeOfType<ObservableCollection<int>>();
        obs.Count.ShouldBe(3);
    }

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

    [Fact]
    public void InsertRange_InsertsAtCorrectPosition()
    {
        IList<int> list = new List<int> { 1, 5 };
        list.InsertRange(1, new[] { 2, 3, 4 });
        list.ShouldBe(new[] { 1, 2, 3, 4, 5 });
    }

    [Fact]
    public void FindIndex_MatchFound_ReturnsIndex()
    {
        IList<int> list = new List<int> { 10, 20, 30 };
        list.FindIndex(x => x == 20).ShouldBe(1);
    }

    [Fact]
    public void FindIndex_NoMatch_ReturnsMinusOne()
    {
        IList<int> list = new List<int> { 10, 20, 30 };
        list.FindIndex(x => x == 99).ShouldBe(-1);
    }

    [Fact]
    public void AddFirst_InsertsAtBeginning()
    {
        IList<int> list = new List<int> { 2, 3 };
        list.AddFirst(1);
        list[0].ShouldBe(1);
    }

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

    [Fact]
    public void InsertAfter_ExistingItem_InsertsAfterIt()
    {
        IList<int> list = new List<int> { 1, 2, 4 };
        list.InsertAfter(2, 3);
        list.ShouldBe(new[] { 1, 2, 3, 4 });
    }

    [Fact]
    public void InsertAfter_NotFound_InsertsAtFront()
    {
        IList<int> list = new List<int> { 2, 3 };
        list.InsertAfter(99, 0);
        list[0].ShouldBe(0);
    }

    [Fact]
    public void InsertAfter_Predicate_InsertsAfterMatch()
    {
        IList<int> list = new List<int> { 1, 2, 4 };
        list.InsertAfter(x => x == 2, 3);
        list.ShouldBe(new[] { 1, 2, 3, 4 });
    }

    [Fact]
    public void InsertBefore_ExistingItem_InsertsBeforeIt()
    {
        IList<int> list = new List<int> { 1, 3, 4 };
        list.InsertBefore(3, 2);
        list.ShouldBe(new[] { 1, 2, 3, 4 });
    }

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

    [Fact]
    public void ReplaceWhile_Predicate_ReplacesAllMatches()
    {
        IList<int> list = new List<int> { 1, 2, 2, 3 };
        list.ReplaceWhile(x => x == 2, 99);
        list.ShouldBe(new[] { 1, 99, 99, 3 });
    }

    [Fact]
    public void ReplaceWhile_Factory_ReplacesAllMatchesWithTransform()
    {
        IList<int> list = new List<int> { 1, 2, 3 };
        list.ReplaceWhile(x => x % 2 == 0, x => x * 10);
        list.ShouldBe(new[] { 1, 20, 3 });
    }

    [Fact]
    public void ReplaceOne_Predicate_ReplacesOnlyFirstMatch()
    {
        IList<int> list = new List<int> { 2, 2, 3 };
        list.ReplaceOne(x => x == 2, 99);
        list.ShouldBe(new[] { 99, 2, 3 });
    }

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

    [Fact]
    public void MoveItem_MovesItemToTargetIndex()
    {
        var list = new List<int> { 1, 2, 3, 4, 5 };
        list.MoveItem(x => x == 5, 0);
        list[0].ShouldBe(5);
    }

    [Fact]
    public void ChunkByView_SplitsCorrectly()
    {
        var list = new List<int> { 1, 2, 3, 4, 5 };
        var chunks = list.ChunkByView(2).ToList();
        chunks.Count.ShouldBe(3);
        chunks[0].ShouldBe(new[] { 1, 2 });
        chunks[2].ShouldBe(new[] { 5 });
    }

    [Fact]
    public void ChunkByView_NullSource_ThrowsArgumentNullException() =>
        Should.Throw<ArgumentNullException>(() => ((List<int>)null!).ChunkByView(2).ToList());

    [Fact]
    public void ChunkByView_ZeroChunkSize_ThrowsArgumentOutOfRangeException() =>
        Should.Throw<ArgumentOutOfRangeException>(() => new List<int> { 1 }.ChunkByView(0).ToList());

    // ─────────────────────────────────────────────────────────────────
    // BingEnumerableExtensions — ChunkBy
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ChunkBy_SplitsEnumerableCorrectly()
    {
        IEnumerable<int> src = new[] { 1, 2, 3, 4, 5 };
        var chunks = src.ChunkBy(2).ToList();
        chunks.Count.ShouldBe(3);
        chunks[0].ShouldBe(new[] { 1, 2 });
        chunks[2].ShouldBe(new[] { 5 });
    }

    [Fact]
    public void ChunkBy_NullSource_ThrowsArgumentNullException() =>
        Should.Throw<ArgumentNullException>(() =>
            ((IEnumerable<int>)null!).ChunkBy(2).ToList());

    // ─────────────────────────────────────────────────────────────────
    // BingEnumerableExtensions — ForEach
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ForEach_VisitsAllItems()
    {
        var sum = 0;
        new[] { 1, 2, 3 }.ForEach(x => sum += x);
        sum.ShouldBe(6);
    }

    [Fact]
    public void ForEach_WithIndex_PassesCorrectIndex()
    {
        var indices = new List<int>();
        new[] { 'a', 'b', 'c' }.ForEach((_, i) => indices.Add(i));
        indices.ShouldBe(new[] { 0, 1, 2 });
    }

    [Fact]
    public void ForEach_NullSource_ThrowsArgumentNullException() =>
        Should.Throw<ArgumentNullException>(() =>
            ((IEnumerable<int>)null!).ForEach(_ => { }));

    [Fact]
    public void ForEach_NullAction_ThrowsArgumentNullException() =>
        Should.Throw<ArgumentNullException>(() =>
            new[] { 1 }.ForEach((Action<int>)null!));

    // ─────────────────────────────────────────────────────────────────
    // BingEnumerableExtensions — ForEachAsync
    // ─────────────────────────────────────────────────────────────────

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

    [Fact]
    public void WithInIndex_ValidIndex_ReturnsTrue()
    {
        var arr = new[] { 1, 2, 3 };
        arr.WithInIndex(1).ShouldBeTrue();
    }

    [Fact]
    public void WithInIndex_IndexAtBoundary_ReturnsTrue()
    {
        var arr = new[] { 1, 2, 3 };
        arr.WithInIndex(0).ShouldBeTrue();
        arr.WithInIndex(2).ShouldBeTrue();
    }

    [Fact]
    public void WithInIndex_NegativeIndex_ReturnsFalse()
    {
        var arr = new[] { 1, 2, 3 };
        arr.WithInIndex(-1).ShouldBeFalse();
    }

    [Fact]
    public void WithInIndex_OutOfBoundsIndex_ReturnsFalse()
    {
        var arr = new[] { 1, 2, 3 };
        arr.WithInIndex(3).ShouldBeFalse();
    }

    [Fact]
    public void WithInIndex_NullArray_ReturnsFalse() =>
        ((Array)null!).WithInIndex(0).ShouldBeFalse();

    [Fact]
    public void WithInIndex_2D_ValidIndex_ReturnsTrue()
    {
        var arr = new int[3, 3];
        arr.WithInIndex(1, 0).ShouldBeTrue();
        arr.WithInIndex(1, 1).ShouldBeTrue();
    }

    [Fact]
    public void WithInIndex_2D_InvalidDimension_ThrowsArgumentOutOfRangeException()
    {
        var arr = new int[3, 3];
        Should.Throw<ArgumentOutOfRangeException>(() => arr.WithInIndex(0, -1));
        Should.Throw<ArgumentOutOfRangeException>(() => arr.WithInIndex(0, 3));
    }
}
