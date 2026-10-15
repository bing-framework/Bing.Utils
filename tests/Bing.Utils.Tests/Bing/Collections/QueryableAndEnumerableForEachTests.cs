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
/// 可查询序列分页及条件筛选扩展测试。
/// </summary>
[Trait("Bing.Linq", "BingQueryableExtensions")]
public class BingQueryableExtensionsTests
{
    // ──────────────────────────────────────────
    //  PageBy<T>
    // ──────────────────────────────────────────

    /// <summary>
    /// 验证分页拒绝空查询。
    /// </summary>
    [Fact]
    public void PageBy_NullQuery_ThrowsArgumentNullException()
    {
        IQueryable<int> q = null;
        Should.Throw<ArgumentNullException>(() => q.PageBy(0, 5));
    }

    /// <summary>
    /// 验证分页跳过指定数量后取出对应子序列。
    /// </summary>
    [Fact]
    public void PageBy_SkipAndTake_ReturnsCorrectSubset()
    {
        new[] { 1, 2, 3, 4, 5 }.AsQueryable()
            .PageBy(1, 2).ToList()
            .ShouldBe(new[] { 2, 3 });
    }

    /// <summary>
    /// 验证零偏移分页从序列开头取值。
    /// </summary>
    [Fact]
    public void PageBy_SkipZero_TakesFromBeginning()
    {
        Enumerable.Range(1, 5).AsQueryable()
            .PageBy(0, 3).ToList()
            .ShouldBe(new[] { 1, 2, 3 });
    }

    /// <summary>
    /// 验证分页偏移超过元素数量时返回空序列。
    /// </summary>
    [Fact]
    public void PageBy_SkipExceedsCount_ReturnsEmpty()
    {
        new[] { 1, 2, 3 }.AsQueryable()
            .PageBy(10, 5).ToList()
            .ShouldBeEmpty();
    }

    /// <summary>
    /// 验证请求数量超过剩余元素时返回剩余序列。
    /// </summary>
    [Fact]
    public void PageBy_TakeLargerThanRemaining_ReturnsRemainder()
    {
        new[] { 1, 2, 3, 4, 5 }.AsQueryable()
            .PageBy(3, 100).ToList()
            .ShouldBe(new[] { 4, 5 });
    }

    /// <summary>
    /// 验证请求零个元素时分页结果为空。
    /// </summary>
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

    /// <summary>
    /// 验证保留查询类型的分页重载拒绝空查询。
    /// </summary>
    [Fact]
    public void PageBy_TypedQueryable_Null_ThrowsArgumentNullException()
    {
        IQueryable<int> q = null;
        Should.Throw<ArgumentNullException>(() => q.PageBy<int, IQueryable<int>>(0, 5));
    }

    /// <summary>
    /// 验证保留查询类型的分页重载返回对应子序列。
    /// </summary>
    [Fact]
    public void PageBy_TypedQueryable_ReturnsCorrectResult()
    {
        IQueryable<int> q = new[] { 10, 20, 30, 40, 50 }.AsQueryable();
        q.PageBy<int, IQueryable<int>>(1, 2).ToList()
            .ShouldBe(new[] { 20, 30 });
    }

    /// <summary>
    /// 验证保留查询类型的分页重载返回第二页元素。
    /// </summary>
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

    /// <summary>
    /// 验证条件筛选拒绝空查询。
    /// </summary>
    [Fact]
    public void WhereIf_NullQuery_ThrowsArgumentNullException()
    {
        IQueryable<int> q = null;
        Should.Throw<ArgumentNullException>(() => q.WhereIf(true, x => x > 0));
    }

    /// <summary>
    /// 验证条件为真时应用筛选谓词。
    /// </summary>
    [Fact]
    public void WhereIf_ConditionTrue_AppliesPredicate()
    {
        new[] { 1, 2, 3, 4, 5 }.AsQueryable()
            .WhereIf(true, x => x > 3).ToList()
            .ShouldBe(new[] { 4, 5 });
    }

    /// <summary>
    /// 验证条件为假时保留全部元素。
    /// </summary>
    [Fact]
    public void WhereIf_ConditionFalse_ReturnsAllElements()
    {
        new[] { 1, 2, 3, 4, 5 }.AsQueryable()
            .WhereIf(false, x => x > 3).ToList()
            .ShouldBe(new[] { 1, 2, 3, 4, 5 });
    }

    /// <summary>
    /// 验证条件为假时返回原查询实例。
    /// </summary>
    [Fact]
    public void WhereIf_ConditionFalse_ReturnsSameQueryReference()
    {
        var q = new[] { 1, 2 }.AsQueryable();
        q.WhereIf(false, x => x > 10).ShouldBeSameAs(q);
    }

    /// <summary>
    /// 验证空查询应用筛选后仍为空。
    /// </summary>
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

    /// <summary>
    /// 验证保留查询类型的筛选重载拒绝空查询。
    /// </summary>
    [Fact]
    public void WhereIf_TypedQueryable_Null_ThrowsArgumentNullException()
    {
        IQueryable<int> q = null;
        Should.Throw<ArgumentNullException>(() => q.WhereIf<int, IQueryable<int>>(true, x => x > 0));
    }

    /// <summary>
    /// 验证保留查询类型的筛选重载应用谓词。
    /// </summary>
    [Fact]
    public void WhereIf_TypedQueryable_ConditionTrue_AppliesPredicate()
    {
        IQueryable<int> q = new[] { 1, 2, 3, 4, 5 }.AsQueryable();
        q.WhereIf<int, IQueryable<int>>(true, x => x % 2 == 0).ToList()
            .ShouldBe(new[] { 2, 4 });
    }

    /// <summary>
    /// 验证保留查询类型的筛选重载在条件为假时返回原实例。
    /// </summary>
    [Fact]
    public void WhereIf_TypedQueryable_ConditionFalse_ReturnsSameReference()
    {
        IQueryable<int> q = new[] { 1, 2, 3 }.AsQueryable();
        q.WhereIf<int, IQueryable<int>>(false, x => x > 10).ShouldBeSameAs(q);
    }

    // ──────────────────────────────────────────
    //  WhereIf<T>(bool, Expression<Func<T, int, bool>>)  — indexed predicate
    // ──────────────────────────────────────────

    /// <summary>
    /// 验证带索引谓词的筛选拒绝空查询。
    /// </summary>
    [Fact]
    public void WhereIf_IndexedPredicate_NullQuery_ThrowsArgumentNullException()
    {
        IQueryable<int> q = null;
        Should.Throw<ArgumentNullException>(() => q.WhereIf(true, (x, i) => i < 2));
    }

    /// <summary>
    /// 验证带索引谓词的筛选保留偶数索引元素。
    /// </summary>
    [Fact]
    public void WhereIf_IndexedPredicate_ConditionTrue_KeepsEvenIndexed()
    {
        // indices: 0→10, 1→20, 2→30, 3→40; keep i%2==0 → {10, 30}
        new[] { 10, 20, 30, 40 }.AsQueryable()
            .WhereIf(true, (x, i) => i % 2 == 0).ToList()
            .ShouldBe(new[] { 10, 30 });
    }

    /// <summary>
    /// 验证带索引谓词的筛选在条件为假时保留全部元素。
    /// </summary>
    [Fact]
    public void WhereIf_IndexedPredicate_ConditionFalse_ReturnsAllElements()
    {
        var q = new[] { 10, 20, 30 }.AsQueryable();
        q.WhereIf(false, (x, i) => i < 1).ToList()
            .ShouldBe(new[] { 10, 20, 30 });
    }

    /// <summary>
    /// 验证带索引谓词的筛选在条件为假时返回原实例。
    /// </summary>
    [Fact]
    public void WhereIf_IndexedPredicate_ConditionFalse_ReturnsSameReference()
    {
        var q = new[] { 5, 6, 7 }.AsQueryable();
        q.WhereIf(false, (x, i) => false).ShouldBeSameAs(q);
    }

    // ──────────────────────────────────────────
    //  WhereIf<T, TQueryable>(bool, indexed pred)
    // ──────────────────────────────────────────

    /// <summary>
    /// 验证保留查询类型的索引筛选按索引下限保留元素。
    /// </summary>
    [Fact]
    public void WhereIf_TypedIndexedPredicate_ConditionTrue_FiltersCorrectly()
    {
        IQueryable<string> q = new[] { "a", "b", "c", "d" }.AsQueryable();
        q.WhereIf<string, IQueryable<string>>(true, (s, i) => i >= 2).ToList()
            .ShouldBe(new[] { "c", "d" });
    }

    /// <summary>
    /// 验证保留查询类型的索引筛选在条件为假时返回原实例。
    /// </summary>
    [Fact]
    public void WhereIf_TypedIndexedPredicate_ConditionFalse_ReturnsSameReference()
    {
        IQueryable<string> q = new[] { "a", "b" }.AsQueryable();
        q.WhereIf<string, IQueryable<string>>(false, (s, i) => false).ShouldBeSameAs(q);
    }
}

/// <summary>
/// 序列同步及异步逐项回调的参数与异常处理测试。
/// </summary>
[Trait("Bing.Collections", "BingEnumerableExtensions.ForEach")]
public class EnumerableForEachExtensionsTests
{
    // ──────────────────────────────────────────
    //  ForEach<T>(Action<T>)
    // ──────────────────────────────────────────

    /// <summary>
    /// 验证逐项回调拒绝空源序列。
    /// </summary>
    [Fact]
    public void ForEach_NullSource_ThrowsArgumentNullException()
    {
        IEnumerable<int> source = null;
        Should.Throw<ArgumentNullException>(() => source.ForEach(x => { }));
    }

    /// <summary>
    /// 验证逐项回调拒绝空回调。
    /// </summary>
    [Fact]
    public void ForEach_NullAction_ThrowsArgumentNullException()
    {
        var source = new[] { 1, 2 };
        Should.Throw<ArgumentNullException>(() => source.ForEach((Action<int>)null));
    }

    /// <summary>
    /// 验证逐项回调按元素值生成全部结果。
    /// </summary>
    [Fact]
    public void ForEach_ExecutesActionForEachElement()
    {
        var results = new List<int>();
        new[] { 1, 2, 3 }.ForEach(x => results.Add(x * 2));
        results.ShouldBe(new[] { 2, 4, 6 });
    }

    /// <summary>
    /// 验证空序列不执行逐项回调。
    /// </summary>
    [Fact]
    public void ForEach_EmptyCollection_ActionNotCalled()
    {
        var callCount = 0;
        Array.Empty<int>().ForEach(_ => callCount++);
        callCount.ShouldBe(0);
    }

    /// <summary>
    /// 验证单元素序列只执行一次回调。
    /// </summary>
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

    /// <summary>
    /// 验证带索引的逐项回调拒绝空源。
    /// </summary>
    [Fact]
    public void ForEach_WithIndex_NullSource_ThrowsArgumentNullException()
    {
        IEnumerable<int> source = null;
        Should.Throw<ArgumentNullException>(() => source.ForEach((x, i) => { }));
    }

    /// <summary>
    /// 验证带索引的逐项回调拒绝空回调。
    /// </summary>
    [Fact]
    public void ForEach_WithIndex_NullAction_ThrowsArgumentNullException()
    {
        var source = new[] { 1 };
        Should.Throw<ArgumentNullException>(() => source.ForEach((Action<int, int>)null));
    }

    /// <summary>
    /// 验证逐项回调收到连续的正确索引。
    /// </summary>
    [Fact]
    public void ForEach_WithIndex_PassesCorrectIndices()
    {
        var indices = new List<int>();
        new[] { "a", "b", "c" }.ForEach((x, i) => indices.Add(i));
        indices.ShouldBe(new[] { 0, 1, 2 });
    }

    /// <summary>
    /// 验证逐项回调同时收到正确索引和元素值。
    /// </summary>
    [Fact]
    public void ForEach_WithIndex_VisitsAllElementsWithValue()
    {
        var items = new List<string>();
        new[] { "x", "y" }.ForEach((s, i) => items.Add($"{i}:{s}"));
        items.ShouldBe(new[] { "0:x", "1:y" });
    }

    /// <summary>
    /// 验证空序列不执行带索引的逐项回调。
    /// </summary>
    [Fact]
    public void ForEach_WithIndex_EmptyCollection_ActionNotCalled()
    {
        var callCount = 0;
        Array.Empty<string>().ForEach((x, i) => callCount++);
        callCount.ShouldBe(0);
    }

    /// <summary>
    /// 验证逐项回调的索引从零开始。
    /// </summary>
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

    /// <summary>
    /// 验证异步逐项回调拒绝空源。
    /// </summary>
    [Fact]
    public void ForEachAsync_NullSource_ThrowsArgumentNullException()
    {
        IEnumerable<int> source = null;
        Should.Throw<ArgumentNullException>(() => source.ForEachAsync(x => Task.CompletedTask));
    }

    /// <summary>
    /// 验证异步逐项回调拒绝空回调。
    /// </summary>
    [Fact]
    public void ForEachAsync_NullAction_ThrowsArgumentNullException()
    {
        var source = new[] { 1, 2 };
        Should.Throw<ArgumentNullException>(() => source.ForEachAsync(null));
    }

    // ──────────────────────────────────────────
    //  ForEachAsync — unlimited parallelism
    // ──────────────────────────────────────────

    /// <summary>
    /// 验证未限制并发度时处理全部元素。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
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

    /// <summary>
    /// 验证空序列的异步遍历不执行回调。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
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

    /// <summary>
    /// 验证错误处理器允许继续时其余元素仍被处理。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
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

    /// <summary>
    /// 验证错误处理器拒绝继续时传播原异常。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
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

    /// <summary>
    /// 验证指定并发度时处理全部元素。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
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

    /// <summary>
    /// 验证指定并发度且无错误处理器时聚合回调异常。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
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

    /// <summary>
    /// 验证指定并发度的错误处理器允许继续时保留成功结果。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
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
