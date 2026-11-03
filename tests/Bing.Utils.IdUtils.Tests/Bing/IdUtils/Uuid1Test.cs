namespace Bing.IdUtils;

/// <summary>
/// UUIDv1 生成测试。
/// </summary>
[Trait("IdUtilsUT", "Uuid1")]
public class Uuid1Test : IDisposable
{
    private static readonly DateTimeOffset Epoch = new(1582, 10, 15, 0, 0, 0, TimeSpan.Zero);

    public Uuid1Test() => Uuid1.ResetStateForTests();

    public void Dispose() => Uuid1.ResetStateForTests();

    [Fact]
    public void CreateDeterministic_MatchesRfc9562OfficialVector()
    {
        var timestamp = Epoch.AddTicks(138648505420000000L);
        var node = new byte[] { 0x9F, 0x6B, 0xDE, 0xCE, 0xD8, 0x46 };

        var value = Uuid1.CreateDeterministic(timestamp, 0x33C8, node);

        value.ShouldBe(Guid.Parse("C232AB00-9414-11EC-B3C8-9F6BDECED846"));
    }

    [Fact]
    public void CreateDeterministic_UsesVersionVariantAndNode()
    {
        var value = Uuid1.CreateDeterministic(Epoch.AddTicks(1), 0x1234, new byte[] { 1, 2, 3, 4, 5, 6 });
        var compact = value.ToString("N");

        compact[12].ShouldBe('1');
        new[] { '8', '9', 'a', 'b' }.ShouldContain(compact[16]);
        compact.Substring(20).ShouldBe("010203040506");
    }

    [Fact]
    public void Generate_UsesRandomNodeWithMulticastBit()
    {
        var value = Uuid1.Generate(Epoch.AddTicks(1));
        var nodeFirstByte = value.ToByteArray()[10];

        (nodeFirstByte & 0x01).ShouldBe(1);
    }

    [Fact]
    public void Generate_RepeatedTimestamp_AdvancesLogical100Nanoseconds()
    {
        var timestamp = new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero);

        var values = Enumerable.Range(0, 5).Select(_ => Uuid1.Generate(timestamp)).ToArray();

        values.Distinct().Count().ShouldBe(values.Length);
        for (var index = 0; index < values.Length; index++)
        {
            ExtractTimestamp(values[index]).ShouldBe(ExtractTimestamp(values[0]) + index);
            ExtractClockSequence(values[index]).ShouldBe(ExtractClockSequence(values[0]));
        }
    }

    [Fact]
    public void Generate_WhenClockMovesBack_IncrementsClockSequenceAndLogicalTime()
    {
        var timestamp = new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero).AddTicks(100);

        var first = Uuid1.Generate(timestamp);
        var second = Uuid1.Generate(timestamp.AddTicks(-1));

        ExtractTimestamp(second).ShouldBe(ExtractTimestamp(first) + 1);
        ExtractClockSequence(second).ShouldBe((ExtractClockSequence(first) + 1) & 0x3FFF);
    }

    [Fact]
    public async Task Generate_ConcurrentSameTimestamp_IsUnique()
    {
        var timestamp = new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var values = new System.Collections.Concurrent.ConcurrentBag<Guid>();
        var tasks = Enumerable.Range(0, 8)
            .Select(_ => Task.Run(() =>
            {
                for (var i = 0; i < 200; i++)
                    values.Add(Uuid1.Generate(timestamp));
            }))
            .ToArray();

        var completed = Task.WhenAll(tasks);
        try
        {
            (await Task.WhenAny(completed, Task.Delay(TimeSpan.FromSeconds(5)))).ShouldBeSameAs(completed);
        }
        finally
        {
            // 即使超时断言失败，也要等待所有发号任务退出后再由 Dispose 重置状态。
            await completed;
        }
        values.Count.ShouldBe(1600);
        values.Distinct().Count().ShouldBe(values.Count);
        values.Select(ExtractClockSequence).Distinct().Count().ShouldBe(1);
    }

    [Fact]
    public void Generate_PhysicalClockRecoversBelowLogicalTime_DoesNotChangeSequenceAgain()
    {
        Uuid1.ResetStateForTests(0x3FFF);
        var timestamp = Epoch.AddTicks(100);
        var first = Uuid1.Generate(timestamp);
        var rollback = Uuid1.Generate(timestamp.AddTicks(-2));
        var repeated = Uuid1.Generate(timestamp.AddTicks(-2));
        var recovering = Uuid1.Generate(timestamp.AddTicks(-1));

        ExtractClockSequence(first).ShouldBe(0x3FFF);
        ExtractClockSequence(rollback).ShouldBe(0);
        ExtractClockSequence(repeated).ShouldBe(0);
        ExtractClockSequence(recovering).ShouldBe(0);
        ExtractTimestamp(recovering).ShouldBe(103);
    }

    [Fact]
    public void Generate_EpochAndMaximumTimestamp_AreAcceptedUntilLogicalSpaceIsExhausted()
    {
        ExtractTimestamp(Uuid1.Generate(Epoch)).ShouldBe(0);
        const long maximum = 0x0FFF_FFFF_FFFF_FFFF;
        var last = Uuid1.Generate(Epoch.AddTicks(maximum));
        ExtractTimestamp(last).ShouldBe(maximum);
        Should.Throw<ArgumentOutOfRangeException>(() => Uuid1.Generate(Epoch.AddTicks(maximum)));
        Should.Throw<ArgumentOutOfRangeException>(() => Uuid1.Generate(Epoch));
    }

    [Fact]
    public void Generate_RejectsEpochBeforeAnd60BitOverflow()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => Uuid1.Generate(Epoch.AddTicks(-1)));
        Should.Throw<ArgumentOutOfRangeException>(() => Uuid1.Generate(Epoch.AddTicks(0x1000000000000000L)));
    }

    [Fact]
    public void CreateDeterministic_RejectsInvalidNodeAndClockSequence()
    {
        Should.Throw<ArgumentNullException>(() => Uuid1.CreateDeterministic(Epoch, 0, null));
        Should.Throw<ArgumentException>(() => Uuid1.CreateDeterministic(Epoch, 0, new byte[5]));
        Should.Throw<ArgumentOutOfRangeException>(() => Uuid1.CreateDeterministic(Epoch, 0xFFFF, new byte[6]));
    }

    private static long ExtractTimestamp(Guid value)
    {
        var bytes = value.ToByteArray();
        return ((long)(bytes[7] & 0x0F) << 56)
             | ((long)bytes[6] << 48)
             | ((long)bytes[5] << 40)
             | ((long)bytes[4] << 32)
             | ((long)bytes[3] << 24)
             | ((long)bytes[2] << 16)
             | ((long)bytes[1] << 8)
             | bytes[0];
    }

    private static int ExtractClockSequence(Guid value)
    {
        var bytes = value.ToByteArray();
        return ((bytes[8] & 0x3F) << 8) | bytes[9];
    }
}
