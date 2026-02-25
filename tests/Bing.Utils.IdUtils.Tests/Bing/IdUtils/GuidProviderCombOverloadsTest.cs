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
}

