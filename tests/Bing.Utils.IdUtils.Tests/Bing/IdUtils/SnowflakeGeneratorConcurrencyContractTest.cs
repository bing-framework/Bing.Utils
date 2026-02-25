using System.Collections.Concurrent;

namespace Bing.IdUtils;

/// <summary>
/// 测试类：覆盖 `SnowflakeGenerator` 的并发与顺序契约行为。
/// </summary>
[Trait("IdUtilsUT", "SnowflakeGenerator.Concurrency")]
public class SnowflakeGeneratorConcurrencyContractTest
{
    /// <summary>
    /// 测试用例：验证 `NextIds` 在 `TwitterStyleSequentialBatch` 场景下，结果为 `IsStrictlyIncreasing`。
    /// </summary>
    [Fact]
    public void NextIds_TwitterStyleSequentialBatch_IsStrictlyIncreasing()
    {
        var generator = SnowflakeGenerator.Create(1, 1);

        var ids = generator.NextIds(2048);

        ids.Length.ShouldBe(2048);
        for (var i = 1; i < ids.Length; i++)
            ids[i].ShouldBeGreaterThan(ids[i - 1]);
    }

    /// <summary>
    /// 测试用例：验证 `NextId` 在 `TwitterStyleParallelCalls` 场景下，结果为 `ReturnsUniquePositiveIds`。
    /// </summary>
    [Fact]
    public void NextId_TwitterStyleParallelCalls_ReturnsUniquePositiveIds()
    {
        var generator = SnowflakeGenerator.Create(1, 1);
        const int total = 5000;
        var bag = new ConcurrentBag<long>();

        Parallel.For(0, total, _ => bag.Add(generator.NextId()));

        bag.Count.ShouldBe(total);
        bag.All(x => x > 0).ShouldBeTrue();
        bag.Distinct().Count().ShouldBe(total);
    }

    /// <summary>
    /// 测试用例：验证 `NextId` 在 `SeataStyleParallelCalls` 场景下，结果为 `ReturnsUniquePositiveIds`。
    /// </summary>
    [Fact]
    public void NextId_SeataStyleParallelCalls_ReturnsUniquePositiveIds()
    {
        var generator = SnowflakeGenerator.Create(1);
        const int total = 5000;
        var bag = new ConcurrentBag<long>();

        Parallel.For(0, total, _ => bag.Add(generator.NextId()));

        bag.Count.ShouldBe(total);
        bag.All(x => x > 0).ShouldBeTrue();
        bag.Distinct().Count().ShouldBe(total);
    }
}
