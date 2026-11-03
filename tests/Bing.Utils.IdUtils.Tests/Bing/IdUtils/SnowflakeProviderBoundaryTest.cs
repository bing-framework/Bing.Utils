namespace Bing.IdUtils.SnowflakeImplements.Providers;

/// <summary>
/// 覆盖 Twitter 雪花提供程序的配置、序列和时钟边界。
/// </summary>
[Trait("IdUtilsUT", "SnowflakeProvider.Boundary")]
public class SnowflakeProviderBoundaryTest
{
    private const long SequenceMask = 4095L;

    [Theory]
    [InlineData(-1)]
    [InlineData(32)]
    public void Constructor_InvalidWorkerId_Throws(long workerId)
    {
        Should.Throw<ArgumentException>(() => new TwitterSnowflakeProvider(workerId, 0));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(32)]
    public void Constructor_InvalidDataCenterId_Throws(long dataCenterId)
    {
        Should.Throw<ArgumentException>(() => new TwitterSnowflakeProvider(0, dataCenterId));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4096)]
    public void Constructor_InvalidSequence_Throws(long sequence)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new TwitterSnowflakeProvider(0, 0, sequence));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(4095)]
    public void NextId_FirstCall_UsesInitialSequence(long initialSequence)
    {
        var timestamp = TwitterSnowflakeProvider.TWEPOCH + 10;
        var provider = new ControlledTwitterSnowflakeProvider(new[] { timestamp, timestamp, timestamp + 1 }, initialSequence);

        var first = provider.NextId();

        (first & SequenceMask).ShouldBe(initialSequence);
    }

    [Fact]
    public void NextId_SequenceOverflow_AdvancesTimestampWithoutReusingSequence()
    {
        var timestamp = TwitterSnowflakeProvider.TWEPOCH + 10;
        var clock = Enumerable.Repeat(timestamp, 4097).Concat(new[] { timestamp + 1 }).ToArray();
        var provider = new ControlledTwitterSnowflakeProvider(clock);

        var ids = Enumerable.Range(0, 4097).Select(_ => provider.NextId()).ToArray();

        ids.Distinct().Count().ShouldBe(4097);
        (ids[0] & SequenceMask).ShouldBe(0);
        (ids[4095] & SequenceMask).ShouldBe(SequenceMask);
        (ids[4096] & SequenceMask).ShouldBe(0);
        DecodeTimestamp(ids[4096]).ShouldBe(timestamp + 1);
    }

    [Fact]
    public void NextId_WhenClockMovesBack_ThrowsAndPreservesLastSuccessfulState()
    {
        var timestamp = TwitterSnowflakeProvider.TWEPOCH + 20;
        var provider = new ControlledTwitterSnowflakeProvider(timestamp, timestamp - 1, timestamp + 1);

        var first = provider.NextId();
        Should.Throw<InvalidOperationException>(() => provider.NextId());
        var recovered = provider.NextId();

        (first & SequenceMask).ShouldBe(0);
        (recovered & SequenceMask).ShouldBe(0);
        DecodeTimestamp(recovered).ShouldBe(timestamp + 1);
        recovered.ShouldBeGreaterThan(first);
    }

    [Fact]
    public void NextId_WhenWaitDetectsRollback_ThrowsWithoutCommittingState()
    {
        var timestamp = TwitterSnowflakeProvider.TWEPOCH + 30;
        var provider = new ControlledTwitterSnowflakeProvider(new[] { timestamp, timestamp, timestamp - 1, timestamp + 1 }, SequenceMask);

        provider.NextId();
        Should.Throw<InvalidOperationException>(() => provider.NextId());
        var recovered = provider.NextId();

        DecodeTimestamp(recovered).ShouldBe(timestamp + 1);
        (recovered & SequenceMask).ShouldBe(0);
    }

    [Fact]
    public void NextId_WhenWaitTimesOut_ThrowsWithoutReusingLastSequence()
    {
        var timestamp = TwitterSnowflakeProvider.TWEPOCH + 40;
        var provider = new ControlledTwitterSnowflakeProvider(Enumerable.Repeat(timestamp, 3).Concat(new[] { timestamp + 1 }).ToArray(), SequenceMask)
        {
            MonotonicStep = 1000
        };

        provider.NextId();
        Should.Throw<TimeoutException>(() => provider.NextId());
        var recovered = provider.NextId();

        DecodeTimestamp(recovered).ShouldBe(timestamp + 1);
        (recovered & SequenceMask).ShouldBe(0);
    }

    [Fact]
    public void NextId_WhenWaitCrossesTimestampFieldLimit_ThrowsWithoutCommittingState()
    {
        var maximumTimestamp = TwitterSnowflakeProvider.TWEPOCH + ((1L << 41) - 1);
        var provider = new ControlledTwitterSnowflakeProvider(
            new[] { maximumTimestamp, maximumTimestamp, maximumTimestamp + 1 },
            SequenceMask);

        provider.NextId();

        Should.Throw<InvalidOperationException>(() => provider.NextId());
    }

    private static long DecodeTimestamp(long id) => (id >> 22) + TwitterSnowflakeProvider.TWEPOCH;

    private sealed class ControlledTwitterSnowflakeProvider : TwitterSnowflakeProvider
    {
        private readonly Queue<long> _timestamps;
        private long _monotonicMilliseconds;

        public ControlledTwitterSnowflakeProvider(IEnumerable<long> timestamps, long sequence = 0)
            : base(1, 1, sequence)
        {
            _timestamps = new Queue<long>(timestamps);
        }

        public ControlledTwitterSnowflakeProvider(params long[] timestamps)
            : this((IEnumerable<long>)timestamps)
        {
        }

        public long MonotonicStep { get; set; } = 1;

        protected override long TimeGen()
        {
            if (_timestamps.Count == 0)
                throw new InvalidOperationException("受控时钟队列已耗尽。");
            return _timestamps.Dequeue();
        }

        protected override long MonotonicMilliseconds()
        {
            var value = _monotonicMilliseconds;
            _monotonicMilliseconds += MonotonicStep;
            return value;
        }

        protected override void Delay(int milliseconds)
        {
        }
    }
}
