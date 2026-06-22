using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bing.Linq;
using Shouldly;
using Xunit;

// ReSharper disable once CheckNamespace
namespace Bing.Collections;

/// <summary>
/// 测试 <see cref="BingQueryableExtensions"/>
/// (PageBy / WhereIf for IQueryable{T})
/// </summary>
[Trait("Bing.Linq", "BingQueryableExtensions")]
public class BingQueryableExtensionsTests
{
    // ──────────────────────────────────────────
    //  PageBy<T>
    // ──────────────────────────────────────────

    [Fact]
    public void PageBy_NullQuery_ThrowsArgumentNullException()
    {
        IQueryable<int> q = null;
        Should.Throw<ArgumentNullException>(() => q.PageBy(0, 5));
    }

    [Fact]
    public void PageBy_SkipAndTake_ReturnsCorrectSubset()
    {
        new[] { 1, 2, 3, 4, 5 }.AsQueryable()
            .PageBy(1, 2).ToList()
            .ShouldBe(new[] { 2, 3 });
    }

    [Fact]
    public void PageBy_SkipZero_TakesFromBeginning()
    {
        Enumerable.Range(1, 5).AsQueryable()
            .PageBy(0, 3).ToList()
            .ShouldBe(new[] { 1, 2, 3 });
    }

    [Fact]
    public void PageBy_SkipExceedsCount_ReturnsEmpty()
    {
        new[] { 1, 2, 3 }.AsQueryable()
            .PageBy(10, 5).ToList()
            .ShouldBeEmpty();
    }

    [Fact]
    public void PageBy_TakeLargerThanRemaining_ReturnsRemainder()
    {
        new[] { 1, 2, 3, 4, 5 }.AsQueryable()
            .PageBy(3, 100).ToList()
            .ShouldBe(new[] { 4, 5 });
    }

    [Fact]
    public void PageBy_TakeZero_ReturnsEmpty()
    {
        new[] { 1, 2, 3 }.AsQueryable()
            .PageBy(0, 0).ToList()
            .ShouldBeEmpty();
    }

    // ──────────────────────────────────────────
    //  PageBy<T, TQueryable>
    // ──────────────────────────────────────────

    [Fact]
    public void PageBy_TypedQueryable_Null_ThrowsArgumentNullException()
    {
        IQueryable<int> q = null;
        Should.Throw<ArgumentNullException>(() => q.PageBy<int, IQueryable<int>>(0, 5));
    }

    [Fact]
    public void PageBy_TypedQueryable_ReturnsCorrectResult()
    {
        IQueryable<int> q = new[] { 10, 20, 30, 40, 50 }.AsQueryable();
        q.PageBy<int, IQueryable<int>>(1, 2).ToList()
            .ShouldBe(new[] { 20, 30 });
    }

    [Fact]
    public void PageBy_TypedQueryable_SecondPage_ReturnsCorrectItems()
    {
        // 每页3条，第2页 → skipCount = 3, maxResultCount = 3
        IQueryable<int> q = Enumerable.Range(1, 9).AsQueryable();
        q.PageBy<int, IQueryable<int>>(3, 3).ToList()
            .ShouldBe(new[] { 4, 5, 6 });
    }

    // ──────────────────────────────────────────
    //  WhereIf<T>(bool, Expression<Func<T,bool>>)
    // ──────────────────────────────────────────

    [Fact]
    public void WhereIf_NullQuery_ThrowsArgumentNullException()
    {
        IQueryable<int> q = null;
        Should.Throw<ArgumentNullException>(() => q.WhereIf(true, x => x > 0));
    }

    [Fact]
    public void WhereIf_ConditionTrue_AppliesPredicate()
    {
        new[] { 1, 2, 3, 4, 5 }.AsQueryable()
            .WhereIf(true, x => x > 3).ToList()
            .ShouldBe(new[] { 4, 5 });
    }

    [Fact]
    public void WhereIf_ConditionFalse_ReturnsAllElements()
    {
        new[] { 1, 2, 3, 4, 5 }.AsQueryable()
            .WhereIf(false, x => x > 3).ToList()
            .ShouldBe(new[] { 1, 2, 3, 4, 5 });
    }

    [Fact]
    public void WhereIf_ConditionFalse_ReturnsSameQueryReference()
    {
        var q = new[] { 1, 2 }.AsQueryable();
        q.WhereIf(false, x => x > 10).ShouldBeSameAs(q);
    }

    [Fact]
    public void WhereIf_ConditionTrue_EmptySource_ReturnsEmpty()
    {
        Array.Empty<int>().AsQueryable()
            .WhereIf(true, x => x > 0).ToList()
            .ShouldBeEmpty();
    }

    // ──────────────────────────────────────────
    //  WhereIf<T, TQueryable>(bool, pred)
    // ──────────────────────────────────────────

    [Fact]
    public void WhereIf_TypedQueryable_Null_ThrowsArgumentNullException()
    {
        IQueryable<int> q = null;
        Should.Throw<ArgumentNullException>(() => q.WhereIf<int, IQueryable<int>>(true, x => x > 0));
    }

    [Fact]
    public void WhereIf_TypedQueryable_ConditionTrue_AppliesPredicate()
    {
        IQueryable<int> q = new[] { 1, 2, 3, 4, 5 }.AsQueryable();
        q.WhereIf<int, IQueryable<int>>(true, x => x % 2 == 0).ToList()
            .ShouldBe(new[] { 2, 4 });
    }

    [Fact]
    public void WhereIf_TypedQueryable_ConditionFalse_ReturnsSameReference()
    {
        IQueryable<int> q = new[] { 1, 2, 3 }.AsQueryable();
        q.WhereIf<int, IQueryable<int>>(false, x => x > 10).ShouldBeSameAs(q);
    }

    // ──────────────────────────────────────────
    //  WhereIf<T>(bool, Expression<Func<T, int, bool>>)  — indexed predicate
    // ──────────────────────────────────────────

    [Fact]
    public void WhereIf_IndexedPredicate_NullQuery_ThrowsArgumentNullException()
    {
        IQueryable<int> q = null;
        Should.Throw<ArgumentNullException>(() => q.WhereIf(true, (x, i) => i < 2));
    }

    [Fact]
    public void WhereIf_IndexedPredicate_ConditionTrue_KeepsEvenIndexed()
    {
        // indices: 0→10, 1→20, 2→30, 3→40; keep i%2==0 → {10, 30}
        new[] { 10, 20, 30, 40 }.AsQueryable()
            .WhereIf(true, (x, i) => i % 2 == 0).ToList()
            .ShouldBe(new[] { 10, 30 });
    }

    [Fact]
    public void WhereIf_IndexedPredicate_ConditionFalse_ReturnsAllElements()
    {
        var q = new[] { 10, 20, 30 }.AsQueryable();
        q.WhereIf(false, (x, i) => i < 1).ToList()
            .ShouldBe(new[] { 10, 20, 30 });
    }

    [Fact]
    public void WhereIf_IndexedPredicate_ConditionFalse_ReturnsSameReference()
    {
        var q = new[] { 5, 6, 7 }.AsQueryable();
        q.WhereIf(false, (x, i) => false).ShouldBeSameAs(q);
    }

    // ──────────────────────────────────────────
    //  WhereIf<T, TQueryable>(bool, indexed pred)
    // ──────────────────────────────────────────

    [Fact]
    public void WhereIf_TypedIndexedPredicate_ConditionTrue_FiltersCorrectly()
    {
        IQueryable<string> q = new[] { "a", "b", "c", "d" }.AsQueryable();
        q.WhereIf<string, IQueryable<string>>(true, (s, i) => i >= 2).ToList()
            .ShouldBe(new[] { "c", "d" });
    }

    [Fact]
    public void WhereIf_TypedIndexedPredicate_ConditionFalse_ReturnsSameReference()
    {
        IQueryable<string> q = new[] { "a", "b" }.AsQueryable();
        q.WhereIf<string, IQueryable<string>>(false, (s, i) => false).ShouldBeSameAs(q);
    }
}

/// <summary>
/// 测试 <see cref="BingEnumerableExtensions"/>
/// (ForEach / ForEachAsync)
/// </summary>
[Trait("Bing.Collections", "BingEnumerableExtensions.ForEach")]
public class EnumerableForEachExtensionsTests
{
    // ──────────────────────────────────────────
    //  ForEach<T>(Action<T>)
    // ──────────────────────────────────────────

    [Fact]
    public void ForEach_NullSource_ThrowsArgumentNullException()
    {
        IEnumerable<int> source = null;
        Should.Throw<ArgumentNullException>(() => source.ForEach(x => { }));
    }

    [Fact]
    public void ForEach_NullAction_ThrowsArgumentNullException()
    {
        var source = new[] { 1, 2 };
        Should.Throw<ArgumentNullException>(() => source.ForEach((Action<int>)null));
    }

    [Fact]
    public void ForEach_ExecutesActionForEachElement()
    {
        var results = new List<int>();
        new[] { 1, 2, 3 }.ForEach(x => results.Add(x * 2));
        results.ShouldBe(new[] { 2, 4, 6 });
    }

    [Fact]
    public void ForEach_EmptyCollection_ActionNotCalled()
    {
        var callCount = 0;
        Array.Empty<int>().ForEach(_ => callCount++);
        callCount.ShouldBe(0);
    }

    [Fact]
    public void ForEach_SingleElement_ActionCalledOnce()
    {
        var callCount = 0;
        new[] { 42 }.ForEach(_ => callCount++);
        callCount.ShouldBe(1);
    }

    // ──────────────────────────────────────────
    //  ForEach<T>(Action<T, int>)  — indexed
    // ──────────────────────────────────────────

    [Fact]
    public void ForEach_WithIndex_NullSource_ThrowsArgumentNullException()
    {
        IEnumerable<int> source = null;
        Should.Throw<ArgumentNullException>(() => source.ForEach((x, i) => { }));
    }

    [Fact]
    public void ForEach_WithIndex_NullAction_ThrowsArgumentNullException()
    {
        var source = new[] { 1 };
        Should.Throw<ArgumentNullException>(() => source.ForEach((Action<int, int>)null));
    }

    [Fact]
    public void ForEach_WithIndex_PassesCorrectIndices()
    {
        var indices = new List<int>();
        new[] { "a", "b", "c" }.ForEach((x, i) => indices.Add(i));
        indices.ShouldBe(new[] { 0, 1, 2 });
    }

    [Fact]
    public void ForEach_WithIndex_VisitsAllElementsWithValue()
    {
        var items = new List<string>();
        new[] { "x", "y" }.ForEach((s, i) => items.Add($"{i}:{s}"));
        items.ShouldBe(new[] { "0:x", "1:y" });
    }

    [Fact]
    public void ForEach_WithIndex_EmptyCollection_ActionNotCalled()
    {
        var callCount = 0;
        Array.Empty<string>().ForEach((x, i) => callCount++);
        callCount.ShouldBe(0);
    }

    [Fact]
    public void ForEach_WithIndex_IndexStartsAtZero()
    {
        var firstIndex = -1;
        new[] { 99 }.ForEach((x, i) => firstIndex = i);
        firstIndex.ShouldBe(0);
    }

    // ──────────────────────────────────────────
    //  ForEachAsync — null guards (synchronous throw)
    // ──────────────────────────────────────────

    [Fact]
    public void ForEachAsync_NullSource_ThrowsArgumentNullException()
    {
        IEnumerable<int> source = null;
        Should.Throw<ArgumentNullException>(() => source.ForEachAsync(x => Task.CompletedTask));
    }

    [Fact]
    public void ForEachAsync_NullAction_ThrowsArgumentNullException()
    {
        var source = new[] { 1, 2 };
        Should.Throw<ArgumentNullException>(() => source.ForEachAsync(null));
    }

    // ──────────────────────────────────────────
    //  ForEachAsync — unlimited parallelism
    // ──────────────────────────────────────────

    [Fact]
    public async Task ForEachAsync_NoMaxParallelism_ExecutesAllActions()
    {
        var results = new ConcurrentBag<int>();
        await new[] { 1, 2, 3, 4, 5 }.ForEachAsync(async x =>
        {
            await Task.Yield();
            results.Add(x);
        });
        results.Count.ShouldBe(5);
    }

    [Fact]
    public async Task ForEachAsync_NoMaxParallelism_EmptySource_CompletesImmediately()
    {
        var callCount = 0;
        await Array.Empty<int>().ForEachAsync(async x =>
        {
            await Task.Yield();
            callCount++;
        });
        callCount.ShouldBe(0);
    }

    [Fact]
    public async Task ForEachAsync_WithErrorHandler_ContinuesWhenHandlerReturnsTrue()
    {
        // errorHandler 返回 true → 吞掉异常 → 其余元素照常处理
        var processed = new ConcurrentBag<int>();
        await new[] { 1, 2, 3 }.ForEachAsync(
            async x =>
            {
                await Task.Yield();
                if (x == 2) throw new InvalidOperationException("fail");
                processed.Add(x);
            },
            errorHandler: (item, ex) => true   // 吞掉异常，继续执行
        );
        processed.OrderBy(x => x).ToList().ShouldBe(new[] { 1, 3 });
    }

    [Fact]
    public async Task ForEachAsync_WithErrorHandler_ThrowsWhenHandlerReturnsFalse()
    {
        // errorHandler 返回 false → 重新抛出 → 整体失败
        await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await new[] { 1, 2, 3 }.ForEachAsync(
                async x =>
                {
                    await Task.Yield();
                    if (x == 2) throw new InvalidOperationException("fail");
                },
                errorHandler: (item, ex) => false  // 不吞异常 → 重新抛出
            );
        });
    }

    // ──────────────────────────────────────────
    //  ForEachAsync — limited parallelism
    // ──────────────────────────────────────────

    [Fact]
    public async Task ForEachAsync_WithMaxParallelism_ExecutesAllActions()
    {
        var results = new ConcurrentBag<int>();
        await Enumerable.Range(1, 8).ForEachAsync(
            async x =>
            {
                await Task.Yield();
                results.Add(x);
            },
            maxDegreeOfParallelism: 2);
        results.Count.ShouldBe(8);
    }

    [Fact]
    public async Task ForEachAsync_WithMaxParallelism_NoErrorHandler_ThrowsAggregateException()
    {
        // 无 errorHandler 且任务失败 → ParallelForEachAsync 抛出 AggregateException
        await Should.ThrowAsync<AggregateException>(async () =>
        {
            await new[] { 1, 2, 3 }.ForEachAsync(
                async x =>
                {
                    await Task.Yield();
                    throw new InvalidOperationException($"Error on {x}");
                },
                maxDegreeOfParallelism: 2
            );
        });
    }

    [Fact]
    public async Task ForEachAsync_WithMaxParallelism_ErrorHandlerReturnsTrue_CompletesNormally()
    {
        // errorHandler 返回 true → 异常不入 exceptions 集合 → 无 AggregateException
        var processed = new ConcurrentBag<int>();
        await new[] { 1, 2, 3, 4 }.ForEachAsync(
            async x =>
            {
                await Task.Yield();
                if (x == 2) throw new InvalidOperationException("fail");
                processed.Add(x);
            },
            maxDegreeOfParallelism: 2,
            errorHandler: (item, ex) => true
        );
        processed.OrderBy(x => x).ToList().ShouldBe(new[] { 1, 3, 4 });
    }
}
