namespace Bing.IdUtils;
/// <summary>
/// 测试类：覆盖 `SnowflakeGenerator` 相关行为。
/// </summary>
[Trait("IdUtilsUT", "SnowflakeGenerator")]
public class SnowflakeGeneratorTest
{
    /// <summary>
    /// 测试用例：验证 `Create` 在 `TwitterStyleInvalidWorkerId` 场景下，结果为 `ThrowsArgumentException`。
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(32)]
    public void Create_TwitterStyleInvalidWorkerId_ThrowsArgumentException(long workerId)
    {
        Should.Throw<ArgumentException>(() => SnowflakeGenerator.Create(workerId, 1));
    }
    /// <summary>
    /// 测试用例：验证 `Create` 在 `TwitterStyleInvalidDataCenterId` 场景下，结果为 `ThrowsArgumentException`。
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(32)]
    public void Create_TwitterStyleInvalidDataCenterId_ThrowsArgumentException(long dataCenterId)
    {
        Should.Throw<ArgumentException>(() => SnowflakeGenerator.Create(1, dataCenterId));
    }
    /// <summary>
    /// 测试用例：验证 `Create` 在 `SeataStyleInvalidWorkerId` 场景下，结果为 `ThrowsArgumentException`。
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(1024)]
    public void Create_SeataStyleInvalidWorkerId_ThrowsArgumentException(long workerId)
    {
        Should.Throw<ArgumentException>(() => SnowflakeGenerator.Create(workerId));
    }
    /// <summary>
    /// 测试用例：验证 `Create` 在 `TwitterStyleValidArgs` 场景下，结果为 `CanGenerateUniqueIds`。
    /// </summary>
    [Fact]
    public void Create_TwitterStyleValidArgs_CanGenerateUniqueIds()
    {
        var generator = SnowflakeGenerator.Create(1, 1);
        var ids = generator.NextIds(128);
        ids.Length.ShouldBe(128);
        ids.All(id => id > 0).ShouldBeTrue();
        ids.Distinct().Count().ShouldBe(128);
    }
    /// <summary>
    /// 测试用例：验证 `Create` 在 `SeataStyleValidArgs` 场景下，结果为 `CanGenerateUniqueIds`。
    /// </summary>
    [Fact]
    public void Create_SeataStyleValidArgs_CanGenerateUniqueIds()
    {
        var generator = SnowflakeGenerator.Create(1);
        var ids = generator.NextIds(128);
        ids.Length.ShouldBe(128);
        ids.All(id => id > 0).ShouldBeTrue();
        ids.Distinct().Count().ShouldBe(128);
    }
    /// <summary>
    /// 测试用例：验证 `NextIds` 在 `SizeIsZero` 场景下，结果为 `ReturnsEmptyArray`。
    /// </summary>
    [Fact]
    public void NextIds_SizeIsZero_ReturnsEmptyArray()
    {
        var generator = SnowflakeGenerator.Create(1);
        var ids = generator.NextIds(0);
        ids.ShouldBeEmpty();
    }
}

