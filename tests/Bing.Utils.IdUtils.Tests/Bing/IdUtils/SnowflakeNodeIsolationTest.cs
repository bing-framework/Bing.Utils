using System.Collections.Concurrent;

namespace Bing.IdUtils;
/// <summary>
/// 验证不同 Snowflake 节点配置之间的标识隔离。
/// </summary>
[Trait("IdUtilsUT", "SnowflakeGenerator.NodeIsolation")]
public class SnowflakeNodeIsolationTest
{
    /// <summary>
    /// Twitter Snowflake worker 字段的位移量。
    /// </summary>
    private const int TwitterWorkerShift = 12;
    /// <summary>
    /// Twitter Snowflake datacenter 字段的位移量。
    /// </summary>
    private const int TwitterDatacenterShift = 17;
    /// <summary>
    /// Seata Snowflake worker 字段的位移量。
    /// </summary>
    private const int SeataWorkerShift = 53;
    /// <summary>
    /// Twitter Snowflake 五位节点字段的掩码。
    /// </summary>
    private const long FiveBitMask = 31;
    /// <summary>
    /// 验证 Twitter Snowflake 并发节点使用各自配置的 worker 和 datacenter 位。
    /// </summary>
    [Fact]
    public async Task TwitterNodes_ConcurrentGeneration_UsesDistinctConfiguredNodeBits()
    {
        const int count = 512;
        const long datacenterA = 3;
        const long workerA = 7;
        const long datacenterB = 19;
        const long workerB = 23;

        var nodeA = SnowflakeGenerator.Create(workerA, datacenterA);
        var nodeB = SnowflakeGenerator.Create(workerB, datacenterB);
        var idsA = new ConcurrentBag<long>();
        var idsB = new ConcurrentBag<long>();

        await Task.WhenAll(
            Task.Run(() => Generate(nodeA, idsA, count)),
            Task.Run(() => Generate(nodeB, idsB, count)));

        idsA.Count.ShouldBe(count);
        idsB.Count.ShouldBe(count);
        idsA.Distinct().Count().ShouldBe(count);
        idsB.Distinct().Count().ShouldBe(count);
        idsA.Intersect(idsB).ShouldBeEmpty();

        foreach (var id in idsA)
        {
            ((id >> TwitterWorkerShift) & FiveBitMask).ShouldBe(workerA);
            ((id >> TwitterDatacenterShift) & FiveBitMask).ShouldBe(datacenterA);
        }

        foreach (var id in idsB)
        {
            ((id >> TwitterWorkerShift) & FiveBitMask).ShouldBe(workerB);
            ((id >> TwitterDatacenterShift) & FiveBitMask).ShouldBe(datacenterB);
        }
    }
    /// <summary>
    /// 验证 Seata Snowflake 并发节点使用各自配置的 worker 位。
    /// </summary>
    [Fact]
    public async Task SeataNodes_ConcurrentGeneration_UsesDistinctConfiguredWorkerBits()
    {
        const int count = 512;
        const long workerA = 17;
        const long workerB = 777;

        var nodeA = SnowflakeGenerator.Create(workerA);
        var nodeB = SnowflakeGenerator.Create(workerB);
        var idsA = new ConcurrentBag<long>();
        var idsB = new ConcurrentBag<long>();

        await Task.WhenAll(
            Task.Run(() => Generate(nodeA, idsA, count)),
            Task.Run(() => Generate(nodeB, idsB, count)));

        idsA.Count.ShouldBe(count);
        idsB.Count.ShouldBe(count);
        idsA.Distinct().Count().ShouldBe(count);
        idsB.Distinct().Count().ShouldBe(count);
        idsA.Intersect(idsB).ShouldBeEmpty();

        foreach (var id in idsA)
            (id >> SeataWorkerShift).ShouldBe(workerA);

        foreach (var id in idsB)
            (id >> SeataWorkerShift).ShouldBe(workerB);
    }
    /// <summary>
    /// 将指定数量的生成结果追加到并发集合。
    /// </summary>
    /// <param name="generator">产生标识的节点生成器。</param>
    /// <param name="ids">接收标识的并发集合。</param>
    /// <param name="count">生成数量。</param>
    private static void Generate(ISnowflakeId generator, ConcurrentBag<long> ids, int count)
    {
        for (var index = 0; index < count; index++)
            ids.Add(generator.NextId());
    }
}
