using System.Text;
using System.Text.Json;
namespace Bing.Helpers;
/// <summary>
/// 测试类：覆盖 `JsonSystemTextJson` 相关行为。
/// </summary>
[Trait("Bing.Helpers", "Json.SystemTextJson")]
public class JsonSystemTextJsonTest
{
    /// <summary>
    /// 测试用例：验证 `ToJson` 在 `NullValue` 场景下，结果为 `ReturnsEmptyString`。
    /// </summary>
    [Fact]
    public void ToJson_NullValue_ReturnsEmptyString()
    {
        Json.ToJson<object>(null).ShouldBe(string.Empty);
    }
    /// <summary>
    /// 测试用例：验证 `ToJson` 在 `ToSingleQuotesOption` 场景下，结果为 `ReplacesDoubleQuotes`。
    /// </summary>
    [Fact]
    public void ToJson_ToSingleQuotesOption_ReplacesDoubleQuotes()
    {
        var value = new JsonSample { Name = "alpha", Count = 2 };
        var options = new JsonOptions { ToSingleQuotes = true };
        var result = Json.ToJson(value, options);
        result.ShouldContain("'Name':'alpha'");
        result.ShouldContain("'Count':2");
        result.ShouldNotContain("\"");
    }
    /// <summary>
    /// 测试用例：验证 `ToJson` 在 `RemoveQuotationMarksOption` 场景下，结果为 `RemovesAllDoubleQuotes`。
    /// </summary>
    [Fact]
    public void ToJson_RemoveQuotationMarksOption_RemovesAllDoubleQuotes()
    {
        var value = new JsonSample { Name = "alpha", Count = 2 };
        var options = new JsonOptions { RemoveQuotationMarks = true };
        var result = Json.ToJson(value, options);
        result.ShouldContain("Name:alpha");
        result.ShouldContain("Count:2");
        result.ShouldNotContain("\"");
    }
    /// <summary>
    /// 测试用例：验证 `ToObject` 在 `GenericWithWhitespaceInput` 场景下，结果为 `ReturnsDefault`。
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void ToObject_GenericWithWhitespaceInput_ReturnsDefault(string json)
    {
        var result = Json.ToObject<JsonSample>(json);
        result.ShouldBeNull();
    }
    /// <summary>
    /// 测试用例：验证 `ToObject` 在 `NonGenericTypeOverload` 场景下，结果为 `IsCaseInsensitiveByDefault`。
    /// </summary>
    [Fact]
    public void ToObject_NonGenericTypeOverload_IsCaseInsensitiveByDefault()
    {
        const string json = "{\"name\":\"alpha\",\"count\":2}";
        var result = Json.ToObject(json, typeof(JsonSample)) as JsonSample;
        result.ShouldNotBeNull();
        result.Name.ShouldBe("alpha");
        result.Count.ShouldBe(2);
    }
    /// <summary>
    /// 测试用例：验证 `ToObject` 在 `GenericWithJsonOptions` 场景下，结果为 `RespectsIgnoreCaseFalse`。
    /// </summary>
    [Fact]
    public void ToObject_GenericWithJsonOptions_RespectsIgnoreCaseFalse()
    {
        const string json = "{\"name\":\"alpha\"}";
        var options = new JsonOptions { IgnoreCase = false };
        var result = Json.ToObject<JsonSample>(json, options);
        result.ShouldNotBeNull();
        result.Name.ShouldBeNull();
    }
    /// <summary>
    /// 测试用例：验证 `ToObject` 在 `GenericWithJsonOptions` 场景下，结果为 `RespectsIgnoreCaseTrue`。
    /// </summary>
    [Fact]
    public void ToObject_GenericWithJsonOptions_RespectsIgnoreCaseTrue()
    {
        const string json = "{\"name\":\"alpha\"}";
        var options = new JsonOptions { IgnoreCase = true };
        var result = Json.ToObject<JsonSample>(json, options);
        result.ShouldNotBeNull();
        result.Name.ShouldBe("alpha");
    }
    /// <summary>
    /// 测试用例：验证 `ToJson` 在 `InterfaceInstance` 场景下，结果为 `IgnoreInterfaceFlagControlsRuntimeProperties`。
    /// </summary>
    [Fact]
    public void ToJson_InterfaceInstance_IgnoreInterfaceFlagControlsRuntimeProperties()
    {
        IJsonContract value = new JsonDerived { Name = "alpha", Extra = 7 };
        var ignoreInterfaceJson = Json.ToJson(value, new JsonOptions { IgnoreInterface = true });
        var keepInterfaceJson = Json.ToJson(value, new JsonOptions { IgnoreInterface = false });
        ignoreInterfaceJson.ShouldContain("\"Extra\":7");
        keepInterfaceJson.ShouldNotContain("\"Extra\":7");
        keepInterfaceJson.ShouldContain("\"Name\":\"alpha\"");
    }
    /// <summary>
    /// 测试用例：验证 `ToBytes` 在 `AndToObjectByteArray` 场景下，结果为 `RoundTripPreservesData`。
    /// </summary>
    [Fact]
    public void ToBytes_AndToObjectByteArray_RoundTripPreservesData()
    {
        var value = new JsonSample { Name = "alpha", Count = 2 };
        var bytes = Json.ToBytes(value);
        var result = Json.ToObject<JsonSample>(bytes);
        bytes.ShouldNotBeNull();
        bytes.Length.ShouldBeGreaterThan(0);
        result.ShouldNotBeNull();
        result.Name.ShouldBe("alpha");
        result.Count.ShouldBe(2);
    }
#if NET6_0_OR_GREATER
    /// <summary>
    /// 测试用例：验证 `ToJsonAsync` 在 `NullValue` 场景下，结果为 `ReturnsEmptyString`。
    /// </summary>
    [Fact]
    public async Task ToJsonAsync_NullValue_ReturnsEmptyString()
    {
        var result = await Json.ToJsonAsync<object>(null);
        result.ShouldBe(string.Empty);
    }
    /// <summary>
    /// 测试用例：验证 `ToObjectAsync` 在 `StringAndBytes` 场景下，结果为 `RoundTripPreservesData`。
    /// </summary>
    [Fact]
    public async Task ToObjectAsync_StringAndBytes_RoundTripPreservesData()
    {
        var value = new JsonSample { Name = "alpha", Count = 2 };
        var json = JsonSerializer.Serialize(value);
        var bytes = Encoding.UTF8.GetBytes(json);
        var fromString = await Json.ToObjectAsync<JsonSample>(json);
        var fromBytes = await Json.ToObjectAsync<JsonSample>(bytes);
        fromString.ShouldNotBeNull();
        fromString.Name.ShouldBe("alpha");
        fromString.Count.ShouldBe(2);
        fromBytes.ShouldNotBeNull();
        fromBytes.Name.ShouldBe("alpha");
        fromBytes.Count.ShouldBe(2);
    }
    /// <summary>
    /// 测试用例：验证 `ToObject` 在 `StreamOverload_WithNullStream` 场景下，结果为 `ReturnsDefault`。
    /// </summary>
    [Fact]
    public void ToObject_StreamOverload_WithNullStream_ReturnsDefault()
    {
        Json.ToObject<JsonSample>((Stream)null).ShouldBeNull();
    }
#endif
    private interface IJsonContract
    {
        string Name { get; }
    }
    private class JsonDerived : IJsonContract
    {
        public string Name { get; set; }
        public int Extra { get; set; }
    }
    private class JsonSample
    {
        public string Name { get; set; }
        public int Count { get; set; }
    }
}

