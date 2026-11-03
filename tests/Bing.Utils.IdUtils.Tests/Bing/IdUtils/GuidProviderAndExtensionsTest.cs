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
    /// <param name="style">用于生成 GUID 的格式。</param>
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
        var name = Encoding.UTF8.GetBytes("www.widgets.com");
        var md5A = GuidProvider.Create(@namespace, name, GuidVersion.NameBasedMd5);
        var md5B = GuidProvider.Create(@namespace, name, GuidVersion.NameBasedMd5);
        var sha1A = GuidProvider.Create(@namespace, name, GuidVersion.NameBasedSha1);
        var sha1B = GuidProvider.Create(@namespace, name, GuidVersion.NameBasedSha1);
        var random = GuidProvider.Create(@namespace, name, GuidVersion.Random);
        md5A.ShouldBe(md5B);
        sha1A.ShouldBe(sha1B);
        md5A.ShouldNotBe(sha1A);
        md5A.ShouldBe(Guid.Parse("3d813cbb-47fb-32ba-91df-831e1593ac29"));
        sha1A.ShouldBe(Guid.Parse("21f7f8de-8051-5b89-8680-0195ef798b6a"));
        md5A.ToString("N")[12].ShouldBe('3');
        sha1A.ToString("N")[12].ShouldBe('5');
        new[] { '8', '9', 'a', 'b' }.ShouldContain(md5A.ToString("N")[16]);
        new[] { '8', '9', 'a', 'b' }.ShouldContain(sha1A.ToString("N")[16]);
        random.ToString("N")[12].ShouldBe('4');
        new[] { '8', '9', 'a', 'b' }.ShouldContain(random.ToString("N")[16]);
    }

    /// <summary>
    /// 验证 RFC 9562 名称型 UUID 官方固定向量。
    /// </summary>
    [Fact]
    public void Create_WithNameBasedVersions_MatchesRfc9562Vectors()
    {
        var @namespace = Guid.Parse("6ba7b810-9dad-11d1-80b4-00c04fd430c8");
        var name = Encoding.UTF8.GetBytes("www.example.com");

        GuidProvider.Create(@namespace, name, GuidVersion.NameBasedMd5)
            .ShouldBe(Guid.Parse("5df41881-3aed-3515-88a7-2f4a814cf09e"));
        GuidProvider.Create(@namespace, name, GuidVersion.NameBasedSha1)
            .ShouldBe(Guid.Parse("2ed6657d-e927-568b-95e1-2665a8aea6a2"));
    }
    /// <summary>
    /// 测试用例：验证未知 GUID 版本抛出参数范围异常。
    /// </summary>
    [Fact]
    public void Create_WithUnknownGuidVersion_ThrowsArgumentOutOfRangeException()
    {
        var exception = Should.Throw<ArgumentOutOfRangeException>(() =>
            GuidProvider.Create(Guid.NewGuid(), Encoding.UTF8.GetBytes("bing-utils"), (GuidVersion)999));
        exception.ParamName.ShouldBe("version");
    }

    /// <summary>
    /// 测试用例：验证未实现的时间型 GUID 版本明确抛出不支持异常。
    /// </summary>
    [Theory]
    [InlineData(GuidVersion.TimeBased)]
    [InlineData(GuidVersion.DceSecurity)]
    public void Create_WithUnsupportedGuidVersion_ThrowsNotSupportedException(GuidVersion version)
    {
        Should.Throw<NotSupportedException>(() => GuidProvider.Create(Guid.NewGuid(), Array.Empty<byte>(), version));
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
    /// 验证两种端序都拒绝比 GUID 字段更长的字节数组，避免静默截断。
    /// </summary>
    [Theory]
    [InlineData(GuidBytesStyle.LittleEndianByteArray, 15)]
    [InlineData(GuidBytesStyle.LittleEndianByteArray, 17)]
    [InlineData(GuidBytesStyle.BigEndianByteArray, 15)]
    [InlineData(GuidBytesStyle.BigEndianByteArray, 17)]
    public void Create_WithInvalidLengthEndianBytes_ThrowsArgumentException(GuidBytesStyle style, int length)
    {
        Should.Throw<ArgumentException>(() => GuidProvider.Create(new byte[length], style));
    }

    /// <summary>
    /// 验证两种端序的 null 输入使用一致的参数异常契约。
    /// </summary>
    [Theory]
    [InlineData(GuidBytesStyle.LittleEndianByteArray)]
    [InlineData(GuidBytesStyle.BigEndianByteArray)]
    public void Create_WithNullEndianBytes_ThrowsArgumentNullException(GuidBytesStyle style)
    {
        var exception = Should.Throw<ArgumentNullException>(() => GuidProvider.Create(null, style));
        exception.ParamName.ShouldBe("endianGuidBytes");
    }
    /// <summary>
    /// 测试用例：验证名称型 GUID 的 null 名称抛出参数为空异常。
    /// </summary>
    [Fact]
    public void Create_WithNameBasedNullName_ThrowsArgumentNullException()
    {
        var @namespace = Guid.NewGuid();
        var exception = Should.Throw<ArgumentNullException>(() => GuidProvider.Create(@namespace, null, GuidVersion.NameBasedMd5));
        exception.ParamName.ShouldBe("name");
    }

    /// <summary>
    /// 测试用例：验证名称型 GUID 对空名称和 null 名称区分处理。
    /// </summary>
    [Fact]
    public void Create_WithEmptyName_ReturnsDeterministicGuid()
    {
        var @namespace = Guid.Parse("6ba7b810-9dad-11d1-80b4-00c04fd430c8");

        var first = GuidProvider.Create(@namespace, Array.Empty<byte>(), GuidVersion.NameBasedMd5);
        var second = GuidProvider.Create(@namespace, Array.Empty<byte>(), GuidVersion.NameBasedMd5);

        first.ShouldBe(second);
        first.ToString("N")[12].ShouldBe('3');
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

