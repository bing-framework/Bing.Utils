using System;

namespace Bing.IdUtils;

/// <summary>
/// UUIDv7 生成与时间戳解析测试。
/// </summary>
public class Uuid7Test
{
    /// <summary>
    /// 验证指定时刻被编码到 RFC 字段位置并设置版本和变体位。
    /// </summary>
    [Fact]
    public void Generate_EncodesRfcTimestampVersionAndVariant()
    {
        var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(1645557742000);

        var value = Uuid7.Generate(timestamp);
        var compact = value.ToString("N");

        compact.Substring(0, 12).ShouldBe("017f22e279b0");
        compact[12].ShouldBe('7');
        new[] { '8', '9', 'a', 'b' }.ShouldContain(compact[16]);
        Uuid7.TryGetTimestamp(value, out var unixTimeMilliseconds).ShouldBeTrue();
        unixTimeMilliseconds.ShouldBe(1645557742000);
    }

    /// <summary>
    /// 验证 Unix Epoch 和 .NET 可表示时间边界，并拒绝 Epoch 之前的时刻。
    /// </summary>
    [Fact]
    public void Generate_HandlesDateTimeOffsetBoundaries()
    {
        var epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var epochValue = Uuid7.Generate(epoch);
        Uuid7.TryGetTimestamp(epochValue, out var epochMilliseconds).ShouldBeTrue();
        epochMilliseconds.ShouldBe(0L);

        var beforeEpoch = epoch.AddTicks(-1);
        Assert.Throws<ArgumentOutOfRangeException>(() => Uuid7.Generate(beforeEpoch));

        var maximumDate = DateTimeOffset.MaxValue;
        var maximumValue = Uuid7.Generate(maximumDate);
        Uuid7.TryGetTimestamp(maximumValue, out var maximumMilliseconds).ShouldBeTrue();
        maximumMilliseconds.ShouldBe(maximumDate.ToUnixTimeMilliseconds());
    }

    /// <summary>
    /// 验证时间戳先后顺序与 UUIDv7 的规范字符串顺序一致。
    /// </summary>
    [Fact]
    public void Generate_ProducesLexicallySortableTimestampPrefix()
    {
        var first = Uuid7.Generate(DateTimeOffset.FromUnixTimeMilliseconds(1000));
        var second = Uuid7.Generate(DateTimeOffset.FromUnixTimeMilliseconds(1001));

        StringComparer.Ordinal.Compare(first.ToString("D"), second.ToString("D")).ShouldBeLessThan(0);
    }

    /// <summary>
    /// 验证同一毫秒生成的 UUIDv7 使用独立的随机字段。
    /// </summary>
    [Fact]
    public void Generate_UsesRandomFieldsForEachValue()
    {
        var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(2000);

        var first = Uuid7.Generate(timestamp);
        var second = Uuid7.Generate(timestamp);

        first.ShouldNotBe(second);
        Uuid7.TryGetTimestamp(first, out var firstMilliseconds).ShouldBeTrue();
        Uuid7.TryGetTimestamp(second, out var secondMilliseconds).ShouldBeTrue();
        firstMilliseconds.ShouldBe(secondMilliseconds);
    }

    /// <summary>
    /// 验证 RFC 示例及完整 48 位时间戳按网络字节序解析。
    /// </summary>
    [Theory]
    [InlineData("017f22e2-79b0-7cc3-98c4-dc0c0c07398f", 1645557742000L)]
    [InlineData("ffffffff-ffff-7fff-bfff-ffffffffffff", 281474976710655L)]
    public void TryGetTimestamp_ReadsRfc9562NetworkOrder(string text, long expectedMilliseconds)
    {
        var value = Guid.Parse(text);

        Uuid7.TryGetTimestamp(value, out var actualMilliseconds).ShouldBeTrue();

        actualMilliseconds.ShouldBe(expectedMilliseconds);
    }

    /// <summary>
    /// 验证非 UUIDv7 版本或非 RFC 变体的标识不会被解析为时间戳。
    /// </summary>
    [Theory]
    [InlineData("f81d4fae-7dec-41d0-a765-00a0c91e6bf6")]
    [InlineData("017f22e2-79b0-7cc3-78c4-dc0c0c07398f")]
    public void TryGetTimestamp_RejectsOtherVersionsAndVariants(string text)
    {
        Uuid7.TryGetTimestamp(Guid.Parse(text), out var timestamp).ShouldBeFalse();

        timestamp.ShouldBe(0L);
    }
}
