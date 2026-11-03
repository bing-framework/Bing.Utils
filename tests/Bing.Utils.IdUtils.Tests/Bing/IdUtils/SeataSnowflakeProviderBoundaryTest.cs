namespace Bing.IdUtils.SnowflakeImplements.Providers;

/// <summary>
/// 覆盖 Seata 雪花提供程序的节点、布局和逻辑时间边界。
/// </summary>
[Trait("IdUtilsUT", "SeataSnowflakeProvider.Boundary")]
public class SeataSnowflakeProviderBoundaryTest
{
    private const int WorkerShift = 53;
    private const long SequenceMask = 4095L;

    [Theory]
    [InlineData(0)]
    [InlineData(1023)]
    public void Constructor_ValidWorkerId_CanGenerate(long workerId)
    {
        var provider = new SeataSnowflakeProvider(workerId);
        provider.NextId().ShouldBeGreaterThanOrEqualTo(0);
        provider.WorkerId.ShouldBe(workerId << WorkerShift);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1024)]
    public void Constructor_InvalidWorkerId_Throws(long workerId)
    {
        Should.Throw<ArgumentException>(() => new SeataSnowflakeProvider(workerId));
    }

    [Fact]
    public void NextId_UsesLogicalTimestampAndSequenceLayout()
    {
        var provider = new SeataSnowflakeProvider(7);
        provider.SetTimestampAndSequenceForTests(100, 0);

        var first = provider.NextId();
        var second = provider.NextId();

        (first >> WorkerShift).ShouldBe(7L);
        (first & SequenceMask).ShouldBe(1);
        (second & SequenceMask).ShouldBe(2);
        second.ShouldBeGreaterThan(first);
    }

    [Fact]
    public void NextId_WhenSequenceCarries_AdvancesLogicalTimestamp()
    {
        var provider = new SeataSnowflakeProvider(0);
        provider.SetTimestampAndSequenceForTests(100, SequenceMask - 1);

        var id = provider.NextId();

        (id & SequenceMask).ShouldBe(SequenceMask);
        var next = provider.NextId();
        (next & SequenceMask).ShouldBe(0);
        (next >> 12).ShouldBe(101);
    }

    [Fact]
    public void NextId_WhenLogicalSpaceExhausted_Throws()
    {
        var provider = new SeataSnowflakeProvider(0);
        provider.SetTimestampAndSequenceForTests((1L << 41) - 1, SequenceMask);

        Should.Throw<InvalidOperationException>(() => provider.NextId());
    }

    [Fact]
    public void NextId_WhenPhysicalClockFreezesJumpsOrMovesBack_RemainsLogicalMonotonic()
    {
        var provider = new ControlledSeataSnowflakeProvider(100);
        provider.SetTimestampAndSequenceForTests(100, 0);

        var first = provider.NextId();
        provider.CurrentTime = 100;
        var frozen = provider.NextId();
        provider.CurrentTime = 1000;
        var jumped = provider.NextId();
        provider.CurrentTime = 99;
        var rolledBack = provider.NextId();

        (frozen & ((1L << 53) - 1)).ShouldBeGreaterThan(first & ((1L << 53) - 1));
        (jumped & ((1L << 53) - 1)).ShouldBeGreaterThan(frozen & ((1L << 53) - 1));
        (rolledBack & ((1L << 53) - 1)).ShouldBeGreaterThan(jumped & ((1L << 53) - 1));
    }

    private sealed class ControlledSeataSnowflakeProvider : SeataSnowflakeProvider
    {
        public ControlledSeataSnowflakeProvider(long currentTime)
            : base(0)
        {
            CurrentTime = currentTime;
        }

        public long CurrentTime { get; set; }

        protected override long TimeGen() => CurrentTime;
    }
}
