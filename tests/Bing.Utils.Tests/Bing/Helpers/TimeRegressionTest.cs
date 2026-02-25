using System.Threading;
using System.Threading.Tasks;

namespace Bing.Helpers;

/// <summary>
/// 测试类：Time 时间工具缺陷回归测试。
/// </summary>
[Trait("Category", "Regression")]
[Trait("Risk", "High")]
public class TimeRegressionTest : IDisposable
{
    /// <summary>
    /// 测试用例：并发上下文下设置模拟时间应相互隔离，避免跨线程串值（AsyncLocal 回归保护）。
    /// </summary>
    [Theory]
    [MemberData(nameof(GetParallelIsolationCases))]
    [Trait("DefectPattern", "DateTime.Time.AsyncLocalIsolation")]
    public async Task SetTime_ParallelContexts_ShouldRemainIsolated(DateTime first, DateTime second)
    {
        using var barrier = new Barrier(2);

        var task1 = Task.Run(() =>
        {
            Time.SetTime(first);
            barrier.SignalAndWait();
            return Time.Now;
        });

        var task2 = Task.Run(() =>
        {
            Time.SetTime(second);
            barrier.SignalAndWait();
            return Time.Now;
        });

        var result = await Task.WhenAll(task1, task2);

        result[0].ShouldBe(first);
        result[1].ShouldBe(second);
        result[0].Kind.ShouldBe(first.Kind);
        result[1].Kind.ShouldBe(second.Kind);
    }

    /// <summary>
    /// 测试数据：并发上下文隔离的代表时间样本（覆盖不同 DateTimeKind）。
    /// </summary>
    public static IEnumerable<object[]> GetParallelIsolationCases()
    {
        yield return new object[]
        {
            new DateTime(2024, 1, 1, 1, 2, 3, DateTimeKind.Unspecified),
            new DateTime(2025, 2, 2, 2, 3, 4, DateTimeKind.Unspecified)
        };

        yield return new object[]
        {
            new DateTime(2024, 6, 1, 8, 0, 0, DateTimeKind.Local),
            new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc)
        };

        yield return new object[]
        {
            DateTime.MinValue,
            new DateTime(2030, 12, 31, 23, 59, 59, DateTimeKind.Utc)
        };
    }

    /// <summary>
    /// 释放资源：清理 Time 的异步本地状态，避免影响后续测试。
    /// </summary>
    public void Dispose() => Time.Reset();
}