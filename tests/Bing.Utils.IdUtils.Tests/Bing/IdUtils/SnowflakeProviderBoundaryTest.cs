namespace Bing.IdUtils.SnowflakeImplements.Providers;

/// <summary>
/// 测试类：覆盖 Twitter 雪花 ID 提供程序的时钟和序列边界。
/// </summary>
[Trait("IdUtilsUT", "SnowflakeProvider.Boundary")]
public class SnowflakeProviderBoundaryTest
{
    private const long SequenceMask = 4095L;

    /// <summary>
    /// 测试用例：序列从 4095 溢出后应推进到下一毫秒并保持 ID 唯一。
    /// </summary>
    [Fact]
    public void NextId_SequenceOverflow_UsesNextMillisecondAndResetsSequence()
    {
        var timestamp = TwitterSnowflakeProvider.TWEPOCH + 10;
        var timestamps = Enumerable.Repeat(timestamp, 4097)
            .Concat(new[] { timestamp, timestamp - 1, timestamp + 1 })
            .ToArray();
        var provider = new ControlledTwitterSnowflakeProvider(timestamps);

        var ids = Enumerable.Range(0, 4097)
            .Select(_ => provider.NextId())
            .ToArray();

        ids.Distinct().Count().ShouldBe(4097);
        for (var i = 1; i < ids.Length; i++)
            ids[i].ShouldBeGreaterThan(ids[i - 1]);
        (ids[4095] & SequenceMask).ShouldBe(SequenceMask);
        (ids[4096] & SequenceMask).ShouldBe(0);
        DecodeTimestamp(ids[0]).ShouldBe(timestamp);
        DecodeTimestamp(ids[4095]).ShouldBe(timestamp);
        DecodeTimestamp(ids[4096]).ShouldBe(timestamp + 1);
        provider.Sequence.ShouldBe(0);
        provider.TimeGenCalls.ShouldBe(4100);
    }

    /// <summary>
    /// 测试用例：时钟回拨抛出异常后，恢复到前进时间仍应能够生成唯一 ID。
    /// </summary>
    [Fact]
    public void NextId_WhenClockMovesBack_ThrowsAndLaterForwardCallRecovers()
    {
        var timestamp = TwitterSnowflakeProvider.TWEPOCH + 20;
        var provider = new ControlledTwitterSnowflakeProvider(timestamp, timestamp - 1, timestamp, timestamp + 1);

        var first = provider.NextId();

        Should.Throw<ApplicationException>(() => provider.NextId());
        var sameTimestamp = provider.NextId();
        var recovered = provider.NextId();

        (first & SequenceMask).ShouldBe(0);
        (sameTimestamp & SequenceMask).ShouldBe(1);
        DecodeTimestamp(sameTimestamp).ShouldBe(timestamp);
        DecodeTimestamp(recovered).ShouldBe(timestamp + 1);
        new[] { first, sameTimestamp, recovered }.Distinct().Count().ShouldBe(3);
        recovered.ShouldBeGreaterThan(sameTimestamp);
        provider.Sequence.ShouldBe(0);
        provider.TimeGenCalls.ShouldBe(4);
    }

    private static long DecodeTimestamp(long id) => (id >> 22) + TwitterSnowflakeProvider.TWEPOCH;

    private sealed class ControlledTwitterSnowflakeProvider : TwitterSnowflakeProvider
    {
        private readonly Queue<long> _timestamps;

        public ControlledTwitterSnowflakeProvider(params long[] timestamps)
            : base(1, 1)
        {
            _timestamps = new Queue<long>(timestamps);
        }

        public int TimeGenCalls { get; private set; }

        protected override long TimeGen()
        {
            TimeGenCalls++;
            if (_timestamps.Count == 0)
                throw new InvalidOperationException("受控时钟队列已耗尽。");
            return _timestamps.Dequeue();
        }
    }
}
