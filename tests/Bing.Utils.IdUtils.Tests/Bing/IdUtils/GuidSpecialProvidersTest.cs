using Bing.IdUtils.GuidImplements;
using Bing.IdUtils.GuidImplements.Internals;
namespace Bing.IdUtils;
/// <summary>
/// 测试类：覆盖 `GuidSpecialProviders` 相关行为。
/// </summary>
[Trait("IdUtilsUT", "GuidSpecialProviders")]
public class GuidSpecialProvidersTest
{
    /// <summary>
    /// 测试用例：验证 `Create` 在 `DefaultTraceIdMaker` 场景下，结果为 `ReturnsAlphaNumericNonEmptyString`。
    /// </summary>
    [Fact]
    public void Create_DefaultTraceIdMaker_ReturnsAlphaNumericNonEmptyString()
    {
        var maker = new DefaultTraceIdMaker();
        var traceId = maker.Create();
        traceId.ShouldNotBeNullOrWhiteSpace();
        traceId.ShouldMatch("^[a-zA-Z0-9]+$");
    }
    /// <summary>
    /// 测试用例：验证 `Create` 在 `DefaultTraceIdMaker_MultipleCalls` 场景下，结果为 `ReturnDifferentValue`。
    /// </summary>
    [Fact]
    public void Create_DefaultTraceIdMaker_MultipleCalls_ReturnDifferentValue()
    {
        var maker = new DefaultTraceIdMaker();
        var first = maker.Create();
        var second = maker.Create();
        first.ShouldNotBe(second);
    }
    /// <summary>
    /// 测试用例：验证 `GuidNamespaces` 在 `Constants` 场景下，结果为 `AreRfcDefinedValues`。
    /// </summary>
    [Fact]
    public void GuidNamespaces_Constants_AreRfcDefinedValues()
    {
        GuidNamespaces.Dns.ShouldBe(Guid.Parse("6ba7b810-9dad-11d1-80b4-00c04fd430c8"));
        GuidNamespaces.Url.ShouldBe(Guid.Parse("6ba7b811-9dad-11d1-80b4-00c04fd430c8"));
        GuidNamespaces.Oid.ShouldBe(Guid.Parse("6ba7b812-9dad-11d1-80b4-00c04fd430c8"));
        GuidNamespaces.X500.ShouldBe(Guid.Parse("6ba7b814-9dad-11d1-80b4-00c04fd430c8"));
    }
    /// <summary>
    /// 测试用例：验证 `Create` 在 `UnixTimeStampStyleProvider_WithFixedInput` 场景下，结果为 `IsDeterministic`。
    /// </summary>
    [Fact]
    public void Create_UnixTimeStampStyleProvider_WithFixedInput_IsDeterministic()
    {
        var value = Guid.Parse("00112233-4455-6677-8899-aabbccddeeff");
        var timestamp = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var first = UnixTimeStampStyleProvider.Create(value, timestamp);
        var second = UnixTimeStampStyleProvider.Create(value, timestamp);
        first.ShouldBe(second);
        first.ShouldNotBe(Guid.Empty);
    }
    /// <summary>
    /// 测试用例：验证 `Create` 在 `UnixTimeStampStyleProvider_WithDifferentTimestamps` 场景下，结果为 `ReturnsDifferentValues`。
    /// </summary>
    [Fact]
    public void Create_UnixTimeStampStyleProvider_WithDifferentTimestamps_ReturnsDifferentValues()
    {
        var value = Guid.Parse("00112233-4455-6677-8899-aabbccddeeff");
        var firstTimestamp = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        var secondTimestamp = new DateTime(2024, 1, 3, 0, 0, 0, DateTimeKind.Utc);
        var first = UnixTimeStampStyleProvider.Create(value, firstTimestamp);
        var second = UnixTimeStampStyleProvider.Create(value, secondTimestamp);
        first.ShouldNotBe(second);
    }
    /// <summary>
    /// 测试用例：验证 `Create` 在 `UnixTimeStampStyleProvider_NoArgs` 场景下，结果为 `ReturnsNonEmptyGuid`。
    /// </summary>
    [Fact]
    public void Create_UnixTimeStampStyleProvider_NoArgs_ReturnsNonEmptyGuid()
    {
        var guid = UnixTimeStampStyleProvider.Create();
        guid.ShouldNotBe(Guid.Empty);
    }
}

