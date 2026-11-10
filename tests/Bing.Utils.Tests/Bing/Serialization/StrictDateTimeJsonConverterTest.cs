using System.Text.Json;
using Bing.Serialization.SystemTextJson;

namespace Bing.Serialization.SystemTextJson;
/// <summary>
/// 验证严格及兼容日期 JSON 转换器的输入和精度契约。
/// </summary>
public class StrictDateTimeJsonConverterTest
{
    /// <summary>
    /// 验证严格转换器拒绝无效字符串和其他 JSON 类型。
    /// </summary>
    /// <param name="json">无效的 JSON 输入。</param>
    [Theory]
    [InlineData("\"not-a-date\"")]
    [InlineData("\"2024-02-30T12:00:00Z\"")]
    [InlineData("\"2024/01/01\"")]
    [InlineData("123")]
    [InlineData("true")]
    [InlineData("{}")]
    public void StrictConverters_RejectInvalidInputs(string json)
    {
        var options = CreateStrictOptions();

        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<DateTime>(json, options));
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<DateTime?>(json, options));
    }
    /// <summary>
    /// 验证严格转换保留日期时间的 ticks 和 Kind。
    /// </summary>
    /// <param name="kind">日期时间种类。</param>
    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void StrictConverters_RoundTripTicksAndKind(DateTimeKind kind)
    {
        var options = CreateStrictOptions();
        var value = new DateTime(2024, 5, 1, 12, 30, 45, kind).AddTicks(1234567);

        var result = JsonSerializer.Deserialize<DateTime>(JsonSerializer.Serialize(value, options), options);
        var nullable = JsonSerializer.Deserialize<DateTime?>(JsonSerializer.Serialize((DateTime?)value, options), options);

        result.Ticks.ShouldBe(value.Ticks);
        result.Kind.ShouldBe(kind);
        nullable.HasValue.ShouldBeTrue();
        nullable.Value.Ticks.ShouldBe(value.Ticks);
        nullable.Value.Kind.ShouldBe(kind);
    }
    /// <summary>
    /// 验证可空日期支持 JSON null，非可空日期拒绝 null。
    /// </summary>
    [Fact]
    public void StrictNullableConverter_RoundTripsNull()
    {
        var options = CreateStrictOptions();

        JsonSerializer.Deserialize<DateTime?>("null", options).ShouldBeNull();
        JsonSerializer.Serialize((DateTime?)null, options).ShouldBe("null");
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<DateTime>("null", options));
    }
    /// <summary>
    /// 验证含偏移的日期输入保留其实际时间点。
    /// </summary>
    [Fact]
    public void StrictConverter_OffsetInput_PreservesInstant()
    {
        var result = JsonSerializer.Deserialize<DateTime>("\"2024-01-01T12:00:00+08:00\"", CreateStrictOptions());

        result.ToUniversalTime().Ticks.ShouldBe(new DateTime(2024, 1, 1, 4, 0, 0, DateTimeKind.Utc).Ticks);
    }
    /// <summary>
    /// 验证旧转换器对无效日期字符串的兼容行为。
    /// </summary>
    [Fact]
    public void LegacyConverters_InvalidString_RetainsMinValueCompatibility()
    {
        var localOptions = new JsonSerializerOptions();
        localOptions.Converters.Add(new DateTimeJsonConverter());
        var utcOptions = new JsonSerializerOptions();
        utcOptions.Converters.Add(new UtcDateTimeJsonConverter());

        JsonSerializer.Deserialize<DateTime>("\"not-a-date\"", localOptions).ShouldBe(DateTime.MinValue);
        JsonSerializer.Deserialize<DateTime>("\"not-a-date\"", utcOptions).ShouldBe(DateTime.MinValue);
    }
    /// <summary>
    /// 创建同时支持可空与非可空严格日期转换的选项。
    /// </summary>
    /// <returns>包含严格日期转换器的 JSON 选项。</returns>
    private static JsonSerializerOptions CreateStrictOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new StrictDateTimeJsonConverter());
        options.Converters.Add(new StrictNullableDateTimeJsonConverter());
        return options;
    }
}
