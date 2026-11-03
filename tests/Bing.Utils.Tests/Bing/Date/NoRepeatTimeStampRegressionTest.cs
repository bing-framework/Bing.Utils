using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Bing.Date;

/// <summary>同一 UTC 序列、输出 Kind 与失败原子性的回归。</summary>
public class NoRepeatTimeStampRegressionTest
{
    [Fact]
    public void MixedEntrypoints_UseOneUtcSequenceAndPreserveKinds()
    {
        var origin = new DateTime(2090, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var factory = new NoRepeatTimeStampFactory(0.0001);
        factory.GetTimeStamp(origin).Kind.ShouldBe(DateTimeKind.Utc);
        var local = factory.GetTimeStamp(origin.ToLocalTime());
        local.Kind.ShouldBe(DateTimeKind.Local);
        local.ToUniversalTime().Ticks.ShouldBe(origin.Ticks + 1);
        var unspecified = factory.GetTimeStamp(DateTime.SpecifyKind(origin.ToLocalTime(), DateTimeKind.Unspecified));
        unspecified.Kind.ShouldBe(DateTimeKind.Unspecified);
        unspecified.ToUniversalTime().Ticks.ShouldBe(origin.Ticks + 2);
        var preview = factory.PeekNextTimeStamp(true);
        preview.Kind.ShouldBe(DateTimeKind.Utc);
        factory.PeekNextTimeStamp(true).ShouldBe(preview);
        factory.GetUtcTimeStamp().ShouldBe(preview);
        var batch = factory.GetTimeStamps(3);
        for (var i = 0; i < batch.Length; i++)
        {
            batch[i].Kind.ShouldBe(DateTimeKind.Local);
            batch[i].ToUniversalTime().Ticks.ShouldBe(preview.Ticks + i + 1);
        }
        factory.GetUtcTimeStampObject().ToDateTime().Kind.ShouldBe(DateTimeKind.Utc);
        factory.GetTimeStampObject().ToDateTime().Kind.ShouldBe(DateTimeKind.Local);
        factory.GetUtcUnixTimeStampObject().ToDateTime().Kind.ShouldBe(DateTimeKind.Utc);
        factory.GetUnixTimeStampObject().ToDateTime().Kind.ShouldBe(DateTimeKind.Local);
        factory.GetTimeStampObjects(2, true).All(x => x.ToDateTime().Kind == DateTimeKind.Utc).ShouldBeTrue();
    }

    [Fact]
    public void BatchOverflow_DoesNotConsumeAnyTimestamp()
    {
        var factory = new NoRepeatTimeStampFactory(0.0001);
        var last = new DateTime(DateTime.MaxValue.Ticks - 1, DateTimeKind.Utc);
        factory.GetTimeStamp(last).ShouldBe(last);
        var count = factory.GeneratedCount;
        Should.Throw<ArgumentOutOfRangeException>(() => factory.GetTimeStamps(2, true));
        factory.GeneratedCount.ShouldBe(count);
        factory.GetUtcTimeStamp().Ticks.ShouldBe(DateTime.MaxValue.Ticks);
        count = factory.GeneratedCount;
        Should.Throw<ArgumentOutOfRangeException>(() => factory.GetUtcTimeStamp());
        Should.Throw<ArgumentOutOfRangeException>(() => factory.PeekNextTimeStamp(true));
        factory.GeneratedCount.ShouldBe(count);
        factory.Reset();
        factory.GeneratedCount.ShouldBe(0);
        factory.GetTimeStamp(last).ShouldBe(last);
    }

    [Fact]
    public async Task ConcurrentCalls_HaveUniqueContiguousUtcTicks()
    {
        var factory = new NoRepeatTimeStampFactory(0.0001);
        var origin = new DateTime(2090, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        factory.GetTimeStamp(origin);
        using var gate = new ManualResetEventSlim(false);
        var tasks = Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
        {
            gate.Wait();
            return Enumerable.Range(0, 250).Select(_ => factory.GetTimeStamp(origin).Ticks).ToArray();
        })).ToArray();
        gate.Set();
        var values = (await Task.WhenAll(tasks)).SelectMany(x => x).OrderBy(x => x).ToArray();
        values.ShouldBe(Enumerable.Range(1, 1000).Select(i => origin.Ticks + i).ToArray());
    }

    [Fact]
    public void LocalRepresentation_OfDstFallbackInstant_KeepsUtcSequence()
    {
        // 在 America/New_York 中覆盖重复的 01:30；其他时区仍验证同一时间点转换。
        var origin = new DateTime(2024, 11, 3, 5, 30, 0, DateTimeKind.Utc);
        var factory = new NoRepeatTimeStampFactory(0.0001);
        factory.GetTimeStamp(origin).Kind.ShouldBe(DateTimeKind.Utc);
        var next = factory.GetTimeStamp(origin.ToLocalTime());
        next.Kind.ShouldBe(DateTimeKind.Local);
        next.ToUniversalTime().Ticks.ShouldBe(origin.Ticks + 1);
    }

    [Fact]
    public void UnspecifiedOutput_RejectsAmbiguousInstantWithoutConsumingSequence()
    {
        var origin = new DateTime(2024, 11, 3, 5, 30, 0, DateTimeKind.Utc);
        var earlierLocal = origin.AddDays(-1).ToLocalTime();
        var earlier = DateTime.SpecifyKind(earlierLocal, DateTimeKind.Unspecified);
        var expected = origin.AddTicks(1);
        var wallTime = DateTime.SpecifyKind(expected.ToLocalTime(), DateTimeKind.Unspecified);
        var factory = new NoRepeatTimeStampFactory(0.0001);
        factory.GetTimeStamp(origin);
        var count = factory.GeneratedCount;
        if (wallTime.ToUniversalTime().Ticks != expected.Ticks)
        {
            // DST 回拨的第一次 01:30 无法在 Unspecified 中携带重复时间的选择信息。
            Should.Throw<ArgumentOutOfRangeException>(() => factory.GetTimeStamp(earlier)).ParamName.ShouldBe("baseTime");
            factory.GeneratedCount.ShouldBe(count);
            factory.GetTimeStamp(origin).ShouldBe(expected);
        }
        else
        {
            var result = factory.GetTimeStamp(earlier);
            result.Kind.ShouldBe(DateTimeKind.Unspecified);
            result.ToUniversalTime().Ticks.ShouldBe(expected.Ticks);
        }
    }
}
