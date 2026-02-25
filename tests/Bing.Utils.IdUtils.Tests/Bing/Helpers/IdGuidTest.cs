using System.Collections.Concurrent;
namespace Bing.Helpers;
/// <summary>
/// 标识生成器 - Guid 测试
/// </summary>
[Trait("Bing.Helpers", "Id.Guid")]
public class IdGuidTest : IDisposable
{
    /// <summary>
    /// 测试初始化，重置 Id 状态
    /// </summary>
    public IdGuidTest()
    {
        Id.Reset();
        Id.ResetGuid();
    }
    /// <summary>
    /// 测试清理，重置 Id 状态
    /// </summary>
    public void Dispose()
    {
        Id.Reset();
        Id.ResetGuid();
    }
    #region Configure 测试
    /// <summary>
    /// 测试 - Configure - 配置自定义 Guid 生成函数
    /// </summary>
    [Fact]
    public void Configure_ValidProvider_SetsCustomProvider()
    {
        // Arrange
        var fixedGuid = Guid.Parse("12345678-1234-1234-1234-123456789abc");
        Func<Guid> customProvider = () => fixedGuid;
        // Act
        Id.Configure(customProvider);
        var result = Id.CreateGuid();
        // Assert
        result.ShouldBe(fixedGuid);
    }
    /// <summary>
    /// 测试 - Configure - null 提供程序抛出异常
    /// </summary>
    [Fact]
    public void Configure_NullProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => Id.Configure((Func<Guid>)null));
    }
    /// <summary>
    /// 测试 - Configure - 多次配置使用最新配置
    /// </summary>
    [Fact]
    public void Configure_MultipleCalls_UsesLatestProvider()
    {
        // Arrange
        var firstGuid = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var secondGuid = Guid.Parse("22222222-2222-2222-2222-222222222222");
        // Act
        Id.Configure(() => firstGuid);
        var firstResult = Id.CreateGuid();
        Id.Configure(() => secondGuid);
        var secondResult = Id.CreateGuid();
        // Assert
        firstResult.ShouldBe(firstGuid);
        secondResult.ShouldBe(secondGuid);
    }
    /// <summary>
    /// 测试 - Configure - 并发配置的线程安全性
    /// </summary>
    [Fact]
    public void Configure_ConcurrentCalls_IsThreadSafe()
    {
        // Arrange
        var results = new ConcurrentBag<Guid>();
        var tasks = new Task[10];
        var guids = Enumerable.Range(1, 10)
            .Select(i => new Guid(i, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0))
            .ToArray();
        // Act
        for (int i = 0; i < 10; i++)
        {
            var index = i;
            tasks[i] = Task.Run(() =>
            {
                Id.Configure(() => guids[index]);
                results.Add(Id.CreateGuid());
            });
        }
        Task.WaitAll(tasks);
        // Assert
        results.Count.ShouldBe(10);
        results.All(g => guids.Contains(g)).ShouldBeTrue();
    }
    #endregion
    #region ResetGuid 测试
    /// <summary>
    /// 测试 - ResetGuid - 重置为默认生成函数
    /// </summary>
    [Fact]
    public void ResetGuid_AfterCustomConfiguration_ResetsToDefault()
    {
        // Arrange
        var customGuid = Guid.Parse("12345678-1234-1234-1234-123456789abc");
        Id.Configure(() => customGuid);
        // Act
        Id.ResetGuid();
        var result1 = Id.CreateGuid();
        var result2 = Id.CreateGuid();
        // Assert
        result1.ShouldNotBe(customGuid);
        result2.ShouldNotBe(customGuid);
        result1.ShouldNotBe(result2); // 默认生成器应该生成不同的 Guid
    }
    /// <summary>
    /// 测试 - ResetGuid - 未配置时重置不影响功能
    /// </summary>
    [Fact]
    public void ResetGuid_WithoutPriorConfiguration_WorksNormally()
    {
        // Act
        Id.ResetGuid();
        var result = Id.CreateGuid();
        // Assert
        result.ShouldNotBe(Guid.Empty);
    }
    /// <summary>
    /// 测试 - ResetGuid - 并发重置的线程安全性
    /// </summary>
    [Fact]
    public void ResetGuid_ConcurrentCalls_IsThreadSafe()
    {
        // Arrange
        var customGuid = Guid.Parse("12345678-1234-1234-1234-123456789abc");
        Id.Configure(() => customGuid);
        var tasks = new Task[10];
        var results = new ConcurrentBag<Guid>();
        // Act
        for (int i = 0; i < 10; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                Id.ResetGuid();
                results.Add(Id.CreateGuid());
            });
        }
        Task.WaitAll(tasks);
        // Assert
        results.Count.ShouldBe(10);
        results.All(g => g != customGuid).ShouldBeTrue();
    }
    #endregion
    #region CreateSimpleGuid 测试
    /// <summary>
    /// 测试 - CreateSimpleGuid - 生成32位无连字符字符串
    /// </summary>
    [Fact]
    public void CreateSimpleGuid_WithoutSetId_Returns32CharacterString()
    {
        // Act
        var result = Id.CreateSimpleGuid();
        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBe(32);
        result.ShouldNotContain("-");
        result.ShouldNotContain("{");
        result.ShouldNotContain("}");
        result.ShouldMatch(@"^[0-9a-f]{32}$"); // 验证为16进制字符串
    }
    /// <summary>
    /// 测试 - CreateSimpleGuid - 使用设置的 Id 值
    /// </summary>
    [Fact]
    public void CreateSimpleGuid_WithSetId_ReturnsSetValue()
    {
        // Arrange
        var expectedValue = "testid123";
        Id.SetId(expectedValue);
        // Act
        var result = Id.CreateSimpleGuid();
        // Assert
        result.ShouldBe(expectedValue);
    }
    /// <summary>
    /// 测试 - CreateSimpleGuid - 空白 Id 值时使用生成函数
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateSimpleGuid_WithEmptySetId_UsesGenerateFunc(string emptyValue)
    {
        // Arrange
        Id.SetId(emptyValue);
        // Act
        var result = Id.CreateSimpleGuid();
        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBe(32);
        result.ShouldMatch(@"^[0-9a-f]{32}$");
    }
    /// <summary>
    /// 测试 - CreateSimpleGuid - 使用自定义生成函数
    /// </summary>
    [Fact]
    public void CreateSimpleGuid_WithCustomProvider_UsesCustomProvider()
    {
        // Arrange
        var fixedGuid = Guid.Parse("12345678-1234-1234-1234-123456789abc");
        Id.Configure(() => fixedGuid);
        // Act
        var result = Id.CreateSimpleGuid();
        // Assert
        result.ShouldBe("12345678123412341234123456789abc");
    }
    /// <summary>
    /// 测试 - CreateSimpleGuid - 多次调用生成不同值
    /// </summary>
    [Fact]
    public void CreateSimpleGuid_MultipleCalls_GeneratesDifferentValues()
    {
        // Act
        var results = new HashSet<string>();
        for (int i = 0; i < 100; i++)
        {
            results.Add(Id.CreateSimpleGuid());
        }
        // Assert
        results.Count.ShouldBeGreaterThan(90); // 应该生成大部分不同的值
    }
    /// <summary>
    /// 测试 - CreateSimpleGuid - 并发调用的线程安全性
    /// </summary>
    [Fact]
    public void CreateSimpleGuid_ConcurrentCalls_IsThreadSafe()
    {
        // Arrange
        var results = new ConcurrentBag<string>();
        var tasks = new Task[100];
        // Act
        for (int i = 0; i < 100; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                results.Add(Id.CreateSimpleGuid());
            });
        }
        Task.WaitAll(tasks);
        // Assert
        results.Count.ShouldBe(100);
        results.All(r => r.Length == 32).ShouldBeTrue();
        results.All(r => !string.IsNullOrWhiteSpace(r)).ShouldBeTrue();
    }
    #endregion
    #region CreateGuid 测试
    /// <summary>
    /// 测试 - CreateGuid - 生成有效的 Guid
    /// </summary>
    [Fact]
    public void CreateGuid_WithoutSetId_ReturnsValidGuid()
    {
        // Act
        var result = Id.CreateGuid();
        // Assert
        result.ShouldNotBe(Guid.Empty);
    }
    /// <summary>
    /// 测试 - CreateGuid - 使用设置的有效 Id 值
    /// </summary>
    [Fact]
    public void CreateGuid_WithValidSetId_ReturnsConvertedGuid()
    {
        // Arrange
        var expectedGuid = Guid.Parse("12345678-1234-1234-1234-123456789abc");
        Id.SetId(expectedGuid.ToString());
        // Act
        var result = Id.CreateGuid();
        // Assert
        result.ShouldBe(expectedGuid);
    }
    /// <summary>
    /// 测试 - CreateGuid - 使用设置的无效 Id 值返回 Empty
    /// </summary>
    [Fact]
    public void CreateGuid_WithInvalidSetId_ReturnsEmpty()
    {
        // Arrange
        Id.SetId("invalid-guid-string");
        // Act
        var result = Id.CreateGuid();
        // Assert
        result.ShouldBe(Guid.Empty);
    }
    /// <summary>
    /// 测试 - CreateGuid - 空白 Id 值时使用生成函数
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateGuid_WithEmptySetId_UsesGenerateFunc(string emptyValue)
    {
        // Arrange
        Id.SetId(emptyValue);
        // Act
        var result = Id.CreateGuid();
        // Assert
        result.ShouldNotBe(Guid.Empty);
    }
    /// <summary>
    /// 测试 - CreateGuid - 使用自定义生成函数
    /// </summary>
    [Fact]
    public void CreateGuid_WithCustomProvider_UsesCustomProvider()
    {
        // Arrange
        var fixedGuid = Guid.Parse("12345678-1234-1234-1234-123456789abc");
        Id.Configure(() => fixedGuid);
        // Act
        var result = Id.CreateGuid();
        // Assert
        result.ShouldBe(fixedGuid);
    }
    /// <summary>
    /// 测试 - CreateGuid - 多次调用生成不同值
    /// </summary>
    [Fact]
    public void CreateGuid_MultipleCalls_GeneratesDifferentValues()
    {
        // Act
        var results = new HashSet<Guid>();
        for (int i = 0; i < 100; i++)
        {
            results.Add(Id.CreateGuid());
        }
        // Assert
        results.Count.ShouldBeGreaterThan(90); // 应该生成大部分不同的值
        results.ShouldNotContain(Guid.Empty);
    }
    /// <summary>
    /// 测试 - CreateGuid - 并发调用的线程安全性
    /// </summary>
    [Fact]
    public void CreateGuid_ConcurrentCalls_IsThreadSafe()
    {
        // Arrange
        var results = new ConcurrentBag<Guid>();
        var tasks = new Task[100];
        // Act
        for (int i = 0; i < 100; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                results.Add(Id.CreateGuid());
            });
        }
        Task.WaitAll(tasks);
        // Assert
        results.Count.ShouldBe(100);
        results.All(g => g != Guid.Empty).ShouldBeTrue();
    }
    /// <summary>
    /// 测试 - CreateGuid - 设置不同格式的 Guid 字符串
    /// </summary>
    [Theory]
    [InlineData("12345678-1234-1234-1234-123456789abc")]
    [InlineData("{12345678-1234-1234-1234-123456789abc}")]
    [InlineData("12345678123412341234123456789abc")]
    [InlineData("(12345678-1234-1234-1234-123456789abc)")]
    public void CreateGuid_WithDifferentGuidFormats_ConvertsCorrectly(string guidString)
    {
        // Arrange
        var expectedGuid = Guid.Parse("12345678-1234-1234-1234-123456789abc");
        Id.SetId(guidString);
        // Act
        var result = Id.CreateGuid();
        // Assert
        result.ShouldBe(expectedGuid);
    }
    #endregion
    #region 集成测试
    /// <summary>
    /// 测试 - 集成测试 - CreateGuid 和 CreateSimpleGuid 的一致性
    /// </summary>
    [Fact]
    public void IntegrationTest_CreateGuidAndSimpleGuid_Consistency()
    {
        // Arrange
        var fixedGuid = Guid.Parse("12345678-1234-1234-1234-123456789abc");
        Id.Configure(() => fixedGuid);
        // Act
        var guid = Id.CreateGuid();
        var simpleGuid = Id.CreateSimpleGuid();
        // Assert
        guid.ShouldBe(fixedGuid);
        simpleGuid.ShouldBe(fixedGuid.ToString("N"));
    }
    /// <summary>
    /// 测试 - 集成测试 - 重置后恢复默认行为
    /// </summary>
    [Fact]
    public void IntegrationTest_ResetAfterSetId_RestoresDefaultBehavior()
    {
        // Arrange
        Id.SetId("12345678-1234-1234-1234-123456789abc");
        var resultWithSetId = Id.CreateGuid();
        // Act
        Id.Reset();
        var resultAfterReset = Id.CreateGuid();
        // Assert
        resultWithSetId.ToString().ShouldBe("12345678-1234-1234-1234-123456789abc");
        resultAfterReset.ShouldNotBe(resultWithSetId);
        resultAfterReset.ShouldNotBe(Guid.Empty);
    }
    /// <summary>
    /// 测试 - 集成测试 - 配置和重置的完整流程
    /// </summary>
    [Fact]
    public void IntegrationTest_ConfigureAndReset_CompleteFlow()
    {
        // Arrange
        var customGuid = Guid.Parse("11111111-1111-1111-1111-111111111111");
        // Act & Assert - 配置自定义生成器
        Id.Configure(() => customGuid);
        Id.CreateGuid().ShouldBe(customGuid);
        // Act & Assert - 重置到默认生成器
        Id.ResetGuid();
        var defaultResult = Id.CreateGuid();
        defaultResult.ShouldNotBe(customGuid);
        defaultResult.ShouldNotBe(Guid.Empty);
        // Act & Assert - 再次配置
        Id.Configure(() => customGuid);
        Id.CreateGuid().ShouldBe(customGuid);
    }
    #endregion
}
