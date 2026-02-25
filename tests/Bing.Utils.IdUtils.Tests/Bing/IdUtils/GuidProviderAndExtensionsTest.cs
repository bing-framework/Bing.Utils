namespace Bing.IdUtils;
/// <summary>
/// 测试类：覆盖 `GuidProviderAndExtensions` 相关行为。
/// </summary>
[Trait("IdUtilsUT", "GuidProviderAndExtensions")]
public class GuidProviderAndExtensionsTest
{
    /// <summary>
    /// 测试用例：验证 `Create` 在 `WithGuidStyle` 场景下，结果为 `ReturnsNonEmptyGuid`。
    /// </summary>
    [Theory]
    [InlineData(GuidStyle.BasicStyle)]
    [InlineData(GuidStyle.CombStyle)]
    [InlineData(GuidStyle.TimeStampStyle)]
    [InlineData(GuidStyle.UnixTimeStampStyle)]
    [InlineData(GuidStyle.LegacySqlTimeStampStyle)]
    [InlineData(GuidStyle.SqlTimeStampStyle)]
    [InlineData(GuidStyle.PostgreSqlTimeStampStyle)]
    [InlineData(GuidStyle.SequentialAsStringStyle)]
    [InlineData(GuidStyle.SequentialAsBinaryStyle)]
    [InlineData(GuidStyle.SequentialAsEndStyle)]
    [InlineData(GuidStyle.EquifaxStyle)]
    public void Create_WithGuidStyle_ReturnsNonEmptyGuid(GuidStyle style)
    {
        var result = GuidProvider.Create(style);
        result.ShouldNotBe(Guid.Empty);
    }
    /// <summary>
    /// 测试用例：验证 `Create` 在 `WithUnknownGuidStyle` 场景下，结果为 `FallsBackToRandomGuid`。
    /// </summary>
    [Fact]
    public void Create_WithUnknownGuidStyle_FallsBackToRandomGuid()
    {
        var result = GuidProvider.Create((GuidStyle)999);
        result.ShouldNotBe(Guid.Empty);
    }
    /// <summary>
    /// 测试用例：验证 `Create` 在 `WithLittleEndianBytes` 场景下，结果为 `ReconstructsOriginalGuid`。
    /// </summary>
    [Fact]
    public void Create_WithLittleEndianBytes_ReconstructsOriginalGuid()
    {
        var expected = Guid.Parse("00112233-4455-6677-8899-aabbccddeeff");
        var result = GuidProvider.Create(expected.ToByteArray(), GuidBytesStyle.LittleEndianByteArray);
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试用例：验证 `Create` 在 `WithBigEndianBytes` 场景下，结果为 `ReturnsDifferentGuidForLittleEndianBytes`。
    /// </summary>
    [Fact]
    public void Create_WithBigEndianBytes_ReturnsDifferentGuidForLittleEndianBytes()
    {
        var bytes = Guid.Parse("00112233-4455-6677-8899-aabbccddeeff").ToByteArray();
        var littleEndianResult = GuidProvider.Create(bytes, GuidBytesStyle.LittleEndianByteArray);
        var bigEndianResult = GuidProvider.Create(bytes, GuidBytesStyle.BigEndianByteArray);
        littleEndianResult.ShouldNotBe(Guid.Empty);
        bigEndianResult.ShouldNotBe(Guid.Empty);
        bigEndianResult.ShouldNotBe(littleEndianResult);
    }
    /// <summary>
    /// 测试用例：验证 `Create` 在 `WithNameBasedVersions` 场景下，结果为 `IsDeterministic`。
    /// </summary>
    [Fact]
    public void Create_WithNameBasedVersions_IsDeterministic()
    {
        var @namespace = Guid.Parse("6ba7b810-9dad-11d1-80b4-00c04fd430c8");
        var name = Encoding.UTF8.GetBytes("bing-utils");
        var md5A = GuidProvider.Create(@namespace, name, GuidVersion.NameBasedMd5);
        var md5B = GuidProvider.Create(@namespace, name, GuidVersion.NameBasedMd5);
        var sha1A = GuidProvider.Create(@namespace, name, GuidVersion.NameBasedSha1);
        var sha1B = GuidProvider.Create(@namespace, name, GuidVersion.NameBasedSha1);
        md5A.ShouldBe(md5B);
        sha1A.ShouldBe(sha1B);
        md5A.ShouldNotBe(sha1A);
    }
    /// <summary>
    /// 测试用例：验证 `Create` 在 `WithUnknownGuidVersion` 场景下，结果为 `FallsBackToRandomGuid`。
    /// </summary>
    [Fact]
    public void Create_WithUnknownGuidVersion_FallsBackToRandomGuid()
    {
        var result = GuidProvider.Create(Guid.NewGuid(), Encoding.UTF8.GetBytes("bing-utils"), (GuidVersion)999);
        result.ShouldNotBe(Guid.Empty);
    }
    /// <summary>
    /// 测试用例：验证 `Create` 在 `WithLittleEndianNullBytes` 场景下，结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void Create_WithLittleEndianNullBytes_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => GuidProvider.Create(null, GuidBytesStyle.LittleEndianByteArray));
    }
    /// <summary>
    /// 测试用例：验证 `Create` 在 `WithLittleEndianInvalidBytesLength` 场景下，结果为 `ThrowsArgumentException`。
    /// </summary>
    [Fact]
    public void Create_WithLittleEndianInvalidBytesLength_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => GuidProvider.Create(new byte[15], GuidBytesStyle.LittleEndianByteArray));
    }
    /// <summary>
    /// 测试用例：验证 `Create` 在 `WithBigEndianInvalidBytesLength` 场景下，结果为 `ThrowsArgumentException`。
    /// </summary>
    [Fact]
    public void Create_WithBigEndianInvalidBytesLength_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => GuidProvider.Create(new byte[8], GuidBytesStyle.BigEndianByteArray));
    }
    /// <summary>
    /// 测试用例：验证 `Create` 在 `WithNameBasedNullName` 场景下，结果为 `ThrowsNullReferenceException`。
    /// </summary>
    [Fact]
    public void Create_WithNameBasedNullName_ThrowsNullReferenceException()
    {
        var @namespace = Guid.NewGuid();
        Should.Throw<NullReferenceException>(() => GuidProvider.Create(@namespace, null, GuidVersion.NameBasedMd5));
    }
    /// <summary>
    /// 测试用例：验证 `Create` 在 `WithSecureTimestampAndUnknownStyle` 场景下，结果为 `ReturnsNonEmptyGuid`。
    /// </summary>
    [Fact]
    public void Create_WithSecureTimestampAndUnknownStyle_ReturnsNonEmptyGuid()
    {
        var result = GuidProvider.Create(new DateTime(2024, 1, 1), (GuidStyle)999);
        result.ShouldNotBe(Guid.Empty);
    }
    /// <summary>
    /// 测试用例：验证 `GuidExtensions` 在 `IsNullOrEmpty` 场景下，结果为 `ReturnsExpected`。
    /// </summary>
    [Fact]
    public void GuidExtensions_IsNullOrEmpty_ReturnsExpected()
    {
        Guid? nullGuid = null;
        var emptyGuid = Guid.Empty;
        var normalGuid = Guid.NewGuid();
        nullGuid.IsNullOrEmpty().ShouldBeTrue();
        emptyGuid.IsNullOrEmpty().ShouldBeTrue();
        normalGuid.IsNullOrEmpty().ShouldBeFalse();
    }
}

