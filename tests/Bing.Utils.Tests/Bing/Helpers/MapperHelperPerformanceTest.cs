using System.Diagnostics;

namespace Bing.Helpers;

/// <summary>
/// 在串行集合中测量 MapperHelper 的重复映射时间。
/// </summary>
[Collection("TimingSensitive")]
[Trait("Bing.Helpers", "MapperHelper")]
[Trait("Category", "Performance")]
public class MapperHelperPerformanceTest : TestBase
{
    /// <inheritdoc />
    public MapperHelperPerformanceTest(ITestOutputHelper output) : base(output)
    {
    }

    /// <summary>
    /// 预热后仅计时映射循环，并在计时结束后检查全部结果及代表性成员。
    /// </summary>
    [Fact]
    public void Map_LargeNumberOfProperties_ShouldPerformWell()
    {
        var source = new MapperHelperTest.ComplexTypeSource
        {
            Id = 1,
            Tags = new List<string> { "performance", "test" },
            Metadata = new Dictionary<string, object> { { "perf", true } },
            Scores = new decimal[] { 1.0m, 2.0m, 3.0m },
            CreatedAt = DateTime.Now,
            Duration = TimeSpan.FromMinutes(30),
            UniqueId = Guid.NewGuid(),
            Website = new Uri("https://performance.test")
        };

        MapperHelper.Map<MapperHelperTest.ComplexTypeSource, MapperHelperTest.ComplexTypeDestination>(source)
            .ShouldNotBeNull();

        const int count = 10000;
        var results = new MapperHelperTest.ComplexTypeDestination[count];
        var stopwatch = Stopwatch.StartNew();
        for (var index = 0; index < count; index++)
        {
            results[index] = MapperHelper.Map<MapperHelperTest.ComplexTypeSource,
                MapperHelperTest.ComplexTypeDestination>(source);
        }
        stopwatch.Stop();

        Assert.All(results, result => Assert.NotNull(result));
        var representative = results[0];
        representative.Id.ShouldBe(source.Id);
        representative.Tags.ShouldBeSameAs(source.Tags);
        representative.Metadata.ShouldBeSameAs(source.Metadata);
        representative.UniqueId.ShouldBe(source.UniqueId);
        representative.Website.ShouldBeSameAs(source.Website);

        Output.WriteLine($"映射10000次复杂对象耗时: {stopwatch.ElapsedMilliseconds}ms");
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(5000);
    }
}
