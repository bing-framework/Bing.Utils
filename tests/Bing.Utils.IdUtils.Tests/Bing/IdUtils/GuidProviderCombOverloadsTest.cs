using Bing.IdUtils.CombImplements;

namespace Bing.IdUtils;
/// <summary>
/// 测试类：覆盖 `GuidProviderCombOverloads` 相关行为。
/// </summary>
[Trait("IdUtilsUT", "GuidProviderCombOverloads")]
public class GuidProviderCombOverloadsTest
{
    /// <summary>
    /// 测试用例：验证 `Create` 在 `WithCombStyle` 场景下，结果为 `ReturnsNonEmptyGuid`。
    /// </summary>
    /// <param name="style">用于生成 GUID 的 COMB 样式。</param>
    [Theory]
    [InlineData(CombStyle.NormalStyle)]
    [InlineData(CombStyle.UnixStyle)]
    [InlineData(CombStyle.SqlStyle)]
    [InlineData(CombStyle.LegacySqlStyle)]
    [InlineData(CombStyle.PostgreSqlStyle)]
    public void Create_WithCombStyle_ReturnsNonEmptyGuid(CombStyle style)
    {
        var result = GuidProvider.Create(style);
        result.ShouldNotBe(Guid.Empty);
    }
    /// <summary>
    /// 测试用例：验证 `Create` 在 `WithUnknownCombStyle` 场景下，结果为 `FallsBackToNonEmptyGuid`。
    /// </summary>
    [Fact]
    public void Create_WithUnknownCombStyle_FallsBackToNonEmptyGuid()
    {
        var result = GuidProvider.Create((CombStyle)999);
        result.ShouldNotBe(Guid.Empty);
    }
    /// <summary>
    /// 测试用例：验证 `Create` 在 `WithValueAndCombStyle` 场景下，结果为 `ReturnsNonEmptyGuid`。
    /// </summary>
    /// <param name="style">用于生成 GUID 的 COMB 样式。</param>
    [Theory]
    [InlineData(CombStyle.NormalStyle)]
    [InlineData(CombStyle.UnixStyle)]
    [InlineData(CombStyle.SqlStyle)]
    [InlineData(CombStyle.LegacySqlStyle)]
    [InlineData(CombStyle.PostgreSqlStyle)]
    public void Create_WithValueAndCombStyle_ReturnsNonEmptyGuid(CombStyle style)
    {
        var result = GuidProvider.Create(Guid.NewGuid(), style, NoRepeatMode.Off);
        result.ShouldNotBe(Guid.Empty);
    }
    /// <summary>
    /// 测试用例：验证 `Create` 在 `WithTimestampAndCombStyle` 场景下，结果为 `ReturnsNonEmptyGuid`。
    /// </summary>
    /// <param name="style">用于生成 GUID 的 COMB 样式。</param>
    [Theory]
    [InlineData(CombStyle.NormalStyle)]
    [InlineData(CombStyle.UnixStyle)]
    [InlineData(CombStyle.SqlStyle)]
    [InlineData(CombStyle.LegacySqlStyle)]
    [InlineData(CombStyle.PostgreSqlStyle)]
    public void Create_WithTimestampAndCombStyle_ReturnsNonEmptyGuid(CombStyle style)
    {
        var result = GuidProvider.Create(new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc), style);
        result.ShouldNotBe(Guid.Empty);
    }
    /// <summary>
    /// 测试用例：验证 `Create` 在 `WithValueAndTimestampAndCombStyle` 场景下，结果为 `IsDeterministic`。
    /// </summary>
    /// <param name="style">用于生成 GUID 的 COMB 样式。</param>
    [Theory]
    [InlineData(CombStyle.NormalStyle)]
    [InlineData(CombStyle.UnixStyle)]
    [InlineData(CombStyle.SqlStyle)]
    [InlineData(CombStyle.LegacySqlStyle)]
    [InlineData(CombStyle.PostgreSqlStyle)]
    public void Create_WithValueAndTimestampAndCombStyle_IsDeterministic(CombStyle style)
    {
        var value = Guid.Parse("00112233-4455-6677-8899-aabbccddeeff");
        var timestamp = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var first = GuidProvider.Create(value, timestamp, style);
        var second = GuidProvider.Create(value, timestamp, style);
        first.ShouldBe(second);
    }

    /// <summary>
    /// 验证 PostgreSQL COMB 使用规范字符串字段顺序写入时间戳，并可逆解析。
    /// </summary>
    [Fact]
    public void Create_PostgreSqlStyle_UsesStringOrderAndRoundTripsTimestamp()
    {
        var value = Guid.Parse("00112233-4455-6677-8899-aabbccddeeff");
        var timestamp = new DateTime(2024, 1, 2, 3, 4, 5, 678, DateTimeKind.Utc);

        var result = GuidProvider.Create(value, timestamp, CombStyle.PostgreSqlStyle);
        var parsed = Bing.IdUtils.CombImplements.InternalCombImplementProxy.PostgreSql.GetTimeStamp(result);

        result.ToString("N").Substring(0, 12).ShouldBe("018cc820db2e");
        parsed.ShouldBe(timestamp.AddTicks(-(timestamp.Ticks % TimeSpan.TicksPerMillisecond)));
        result.ToString("N").Substring(12).ShouldBe(value.ToString("N").Substring(12));
    }

    /// <summary>
    /// 验证 PostgreSQL COMB 固定字节布局、可逆时间戳和时间排序。
    /// </summary>
    [Fact]
    public void Create_PostgreSqlStyle_MatchesFixedByteLayoutAndSortsByTimestamp()
    {
        var value = Guid.Parse("00112233-4455-6677-8899-aabbccddeeff");
        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var timestamp1 = epoch.AddMilliseconds(0x010203FFFEFFL);
        var timestamp2 = epoch.AddMilliseconds(0x010204000000L);

        var result1 = GuidProvider.Create(value, timestamp1, CombStyle.PostgreSqlStyle);
        var result2 = GuidProvider.Create(value, timestamp2, CombStyle.PostgreSqlStyle);

        result1.ShouldBe(Guid.Parse("010203FF-FEFF-6677-8899-AABBCCDDEEFF"));
        result2.ShouldBe(Guid.Parse("01020400-0000-6677-8899-AABBCCDDEEFF"));
        string.CompareOrdinal(result2.ToString(), result1.ToString()).ShouldBeGreaterThan(0);
        InternalCombImplementProxy.PostgreSql.GetTimeStamp(result1).ShouldBe(timestamp1);
        InternalCombImplementProxy.PostgreSql.GetTimeStamp(result2).ShouldBe(timestamp2);
    }

    /// <summary>
    /// 验证 PostgreSQL COMB 拒绝被截断后会编码成无效时间戳的 Unix Epoch 前时间。
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(10000)]
    public void Create_PostgreSqlStyle_BeforeUnixEpoch_ThrowsArgumentOutOfRangeException(long ticksBeforeEpoch)
    {
        var value = Guid.Parse("00112233-4455-6677-8899-aabbccddeeff");
        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        Should.Throw<ArgumentOutOfRangeException>(() =>
            GuidProvider.Create(value, epoch.AddTicks(-ticksBeforeEpoch), CombStyle.PostgreSqlStyle));
    }

    /// <summary>
    /// 验证 Unix Epoch 及 .NET 最大可表示 UTC 时间可以编码并往返解析。
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Create_PostgreSqlStyle_RepresentableTimestampBoundaries_RoundTrip(bool useMaximumValue)
    {
        var value = Guid.Parse("00112233-4455-6677-8899-aabbccddeeff");
        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var timestamp = useMaximumValue
            ? DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc)
            : epoch;

        var result = GuidProvider.Create(value, timestamp, CombStyle.PostgreSqlStyle);
        var parsed = InternalCombImplementProxy.PostgreSql.GetTimeStamp(result);
        var expected = timestamp.AddTicks(-(timestamp.Ticks % TimeSpan.TicksPerMillisecond));

        parsed.ShouldBe(expected);
    }
}

