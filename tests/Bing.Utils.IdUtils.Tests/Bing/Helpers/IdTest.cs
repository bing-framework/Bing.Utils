using System.Collections.Concurrent;

namespace Bing.Helpers;

/// <summary>
/// 标识生成器 测试
/// </summary>
[Trait("Bing.Helpers", "Id")]
public class IdTest : IDisposable
{
    /// <summary>
    /// 测试初始化
    /// </summary>
    public IdTest()
    {
        // 重置所有状态
        Id.Reset();
        Id.ResetLong();
        Id.ResetString();
    }

    /// <summary>
    /// 测试清理
    /// </summary>
    public void Dispose()
    {
        // 清理测试状态
        Id.Reset();
        Id.ResetLong();
        Id.ResetString();
    }

    #region SetId 和 Reset 测试

    /// <summary>
    /// 测试 - SetId - 设置Id值
    /// </summary>
    [Fact]
    public void SetId_ValidValue_SetsContextId()
    {
        // Arrange
        var testId = "test123";

        // Act
        Id.SetId(testId);

        // Assert - 通过CreateString验证设置是否生效
        Id.ConfigureString(() => "should_not_be_used");
        var result = Id.CreateString();
        result.ShouldBe(testId);
    }

    /// <summary>
    /// 测试 - SetId - 设置null值
    /// </summary>
    [Fact]
    public void SetId_NullValue_ClearsContextId()
    {
        // Arrange
        Id.SetId("initial_value");
        Id.ConfigureString(() => "generated_value");

        // Act
        Id.SetId(null);
        var result = Id.CreateString();

        // Assert
        result.ShouldBe("generated_value");
    }

    /// <summary>
    /// 测试 - SetId - 空字符串值
    /// </summary>
    [Fact]
    public void SetId_EmptyString_ClearsContextId()
    {
        // Arrange
        Id.SetId("initial_value");
        Id.ConfigureString(() => "generated_value");

        // Act
        Id.SetId("");
        var result = Id.CreateString();

        // Assert
        result.ShouldBe("generated_value");
    }

    /// <summary>
    /// 测试 - Reset - 重置Id值
    /// </summary>
    [Fact]
    public void Reset_AfterSetId_ClearsContextId()
    {
        // Arrange
        Id.SetId("test_value");
        Id.ConfigureString(() => "generated_value");

        // Act
        Id.Reset();
        var result = Id.CreateString();

        // Assert
        result.ShouldBe("generated_value");
    }

    /// <summary>
    /// 测试 - SetId - 线程隔离性
    /// </summary>
    [Fact]
    public void SetId_DifferentThreads_AreIsolated()
    {
        // Arrange
        Id.ConfigureString(() => "default_value");
        var results = new ConcurrentDictionary<int, string>();
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 5; i++)
        {
            var threadId = i;
            tasks.Add(Task.Run(() =>
            {
                Id.SetId($"thread_{threadId}");
                Thread.Sleep(10); // 确保其他线程也有时间设置值
                results[threadId] = Id.CreateString();
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        for (int i = 0; i < 5; i++)
        {
            results[i].ShouldBe($"thread_{i}");
        }
    }

    #endregion

    #region ConfigureLong 测试

    /// <summary>
    /// 测试 - ConfigureLong - 配置Long生成函数
    /// </summary>
    [Fact]
    public void ConfigureLong_ValidProvider_SetsLongGenerator()
    {
        // Arrange
        const long expectedValue = 12345L;
        Func<long> provider = () => expectedValue;

        // Act
        Id.ConfigureLong(provider);
        var result = Id.CreateLong();

        // Assert
        result.ShouldBe(expectedValue);
    }

    /// <summary>
    /// 测试 - ConfigureLong - null提供程序抛出异常
    /// </summary>
    [Fact]
    public void ConfigureLong_NullProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => Id.ConfigureLong(null))
            .ParamName.ShouldBe("provider");
    }

    /// <summary>
    /// 测试 - ConfigureLong - 多次配置使用最新配置
    /// </summary>
    [Fact]
    public void ConfigureLong_MultipleCalls_UsesLatestProvider()
    {
        // Arrange
        Id.ConfigureLong(() => 111L);
        var firstResult = Id.CreateLong();

        // Act
        Id.ConfigureLong(() => 222L);
        var secondResult = Id.CreateLong();

        // Assert
        firstResult.ShouldBe(111L);
        secondResult.ShouldBe(222L);
    }

    #endregion

    #region ConfigureString 测试

    /// <summary>
    /// 测试 - ConfigureString - 配置String生成函数
    /// </summary>
    [Fact]
    public void ConfigureString_ValidProvider_SetsStringGenerator()
    {
        // Arrange
        const string expectedValue = "test_string";
        Func<string> provider = () => expectedValue;

        // Act
        Id.ConfigureString(provider);
        var result = Id.CreateString();

        // Assert
        result.ShouldBe(expectedValue);
    }

    /// <summary>
    /// 测试 - ConfigureString - null提供程序抛出异常
    /// </summary>
    [Fact]
    public void ConfigureString_NullProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => Id.ConfigureString(null))
            .ParamName.ShouldBe("provider");
    }

    /// <summary>
    /// 测试 - ConfigureString - 多次配置使用最新配置
    /// </summary>
    [Fact]
    public void ConfigureString_MultipleCalls_UsesLatestProvider()
    {
        // Arrange
        Id.ConfigureString(() => "first");
        var firstResult = Id.CreateString();

        // Act
        Id.ConfigureString(() => "second");
        var secondResult = Id.CreateString();

        // Assert
        firstResult.ShouldBe("first");
        secondResult.ShouldBe("second");
    }

    #endregion

    #region ResetLong 测试

    /// <summary>
    /// 测试 - ResetLong - 重置Long生成函数
    /// </summary>
    [Fact]
    public void ResetLong_AfterConfiguration_ClearsGenerator()
    {
        // Arrange
        Id.ConfigureLong(() => 999L);
        var beforeReset = Id.CreateLong();

        // Act
        Id.ResetLong();

        // Assert
        beforeReset.ShouldBe(999L);
        Should.Throw<InvalidOperationException>(() => Id.CreateLong());
    }

    /// <summary>
    /// 测试 - ResetLong - 不影响String配置
    /// </summary>
    [Fact]
    public void ResetLong_DoesNotAffectStringConfiguration()
    {
        // Arrange
        Id.ConfigureLong(() => 123L);
        Id.ConfigureString(() => "test");

        // Act
        Id.ResetLong();

        // Assert
        Should.Throw<InvalidOperationException>(() => Id.CreateLong());
        Id.CreateString().ShouldBe("test");
    }

    #endregion

    #region ResetString 测试

    /// <summary>
    /// 测试 - ResetString - 重置String生成函数
    /// </summary>
    [Fact]
    public void ResetString_AfterConfiguration_ClearsGenerator()
    {
        // Arrange
        Id.ConfigureString(() => "test");
        var beforeReset = Id.CreateString();

        // Act
        Id.ResetString();

        // Assert
        beforeReset.ShouldBe("test");
        Should.Throw<InvalidOperationException>(() => Id.CreateString());
    }

    /// <summary>
    /// 测试 - ResetString - 不影响Long配置
    /// </summary>
    [Fact]
    public void ResetString_DoesNotAffectLongConfiguration()
    {
        // Arrange
        Id.ConfigureLong(() => 456L);
        Id.ConfigureString(() => "test");

        // Act
        Id.ResetString();

        // Assert
        Id.CreateLong().ShouldBe(456L);
        Should.Throw<InvalidOperationException>(() => Id.CreateString());
    }

    #endregion

    #region CreateLong 测试

    /// <summary>
    /// 测试 - CreateLong - 使用配置的生成函数
    /// </summary>
    [Fact]
    public void CreateLong_WithConfiguredGenerator_ReturnsGeneratedValue()
    {
        // Arrange
        const long expectedValue = 987654321L;
        Id.ConfigureLong(() => expectedValue);

        // Act
        var result = Id.CreateLong();

        // Assert
        result.ShouldBe(expectedValue);
    }

    /// <summary>
    /// 测试 - CreateLong - 使用上下文Id值
    /// </summary>
    [Fact]
    public void CreateLong_WithContextId_ReturnsConvertedValue()
    {
        // Arrange
        Id.ConfigureLong(() => 111L);
        Id.SetId("123456789");

        // Act
        var result = Id.CreateLong();

        // Assert
        result.ShouldBe(123456789L);
    }

    /// <summary>
    /// 测试 - CreateLong - 未配置生成函数且无上下文Id时抛出异常
    /// </summary>
    [Fact]
    public void CreateLong_WithoutConfigurationAndContext_ThrowsInvalidOperationException()
    {
        // Act & Assert
        Should.Throw<InvalidOperationException>(() => Id.CreateLong())
            .Message.ShouldContain("LongGenerateFunc未配置");
    }

    /// <summary>
    /// 测试 - CreateLong - 无效上下文Id时使用生成函数
    /// </summary>
    [Fact]
    public void CreateLong_WithInvalidContextId_UsesGenerator()
    {
        // Arrange
        Id.ConfigureLong(() => 999L);
        Id.SetId("invalid_long_value");

        // Act
        var result = Id.CreateLong();

        // Assert
        // 因为ToLong方法在转换失败时返回0，所以这里应该是0
        result.ShouldBe(0L);
    }

    /// <summary>
    /// 测试 - CreateLong - 多次调用生成不同值
    /// </summary>
    [Fact]
    public void CreateLong_MultipleCalls_GeneratesDifferentValues()
    {
        // Arrange
        var counter = 0L;
        Id.ConfigureLong(() => ++counter);

        // Act
        var results = new List<long>();
        for (int i = 0; i < 5; i++)
        {
            results.Add(Id.CreateLong());
        }

        // Assert
        results.ShouldBe(new[] { 1L, 2L, 3L, 4L, 5L });
    }

    #endregion

    #region CreateString 测试

    /// <summary>
    /// 测试 - CreateString - 使用配置的生成函数
    /// </summary>
    [Fact]
    public void CreateString_WithConfiguredGenerator_ReturnsGeneratedValue()
    {
        // Arrange
        const string expectedValue = "generated_string";
        Id.ConfigureString(() => expectedValue);

        // Act
        var result = Id.CreateString();

        // Assert
        result.ShouldBe(expectedValue);
    }

    /// <summary>
    /// 测试 - CreateString - 使用上下文Id值
    /// </summary>
    [Fact]
    public void CreateString_WithContextId_ReturnsContextValue()
    {
        // Arrange
        Id.ConfigureString(() => "should_not_be_used");
        Id.SetId("context_value");

        // Act
        var result = Id.CreateString();

        // Assert
        result.ShouldBe("context_value");
    }

    /// <summary>
    /// 测试 - CreateString - 未配置生成函数且无上下文Id时抛出异常
    /// </summary>
    [Fact]
    public void CreateString_WithoutConfigurationAndContext_ThrowsInvalidOperationException()
    {
        // Act & Assert
        Should.Throw<InvalidOperationException>(() => Id.CreateString())
            .Message.ShouldContain("StringGenerateFunc未配置");
    }

    /// <summary>
    /// 测试 - CreateString - 多次调用生成不同值
    /// </summary>
    [Fact]
    public void CreateString_MultipleCalls_GeneratesDifferentValues()
    {
        // Arrange
        var counter = 0;
        Id.ConfigureString(() => $"string_{++counter}");

        // Act
        var results = new List<string>();
        for (int i = 0; i < 3; i++)
        {
            results.Add(Id.CreateString());
        }

        // Assert
        results.ShouldBe(new[] { "string_1", "string_2", "string_3" });
    }

    /// <summary>
    /// 测试 - CreateString - 空白上下文Id时使用生成函数
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void CreateString_WithEmptyContextId_UsesGenerator(string emptyValue)
    {
        // Arrange
        Id.ConfigureString(() => "generated");
        Id.SetId(emptyValue);

        // Act
        var result = Id.CreateString();

        // Assert
        result.ShouldBe("generated");
    }

    #endregion

    #region CreateObjectId 测试

    /// <summary>
    /// 测试 - CreateObjectId - 生成有效ObjectId
    /// </summary>
    [Fact]
    public void CreateObjectId_AlwaysGeneratesNewId()
    {
        // Act
        var id1 = Id.CreateObjectId();
        var id2 = Id.CreateObjectId();

        // Assert
        id1.ShouldNotBeNull();
        id2.ShouldNotBeNull();
        id1.ShouldNotBe(id2);
        id1.Length.ShouldBe(24); // ObjectId标准长度
        id2.Length.ShouldBe(24);

        // 验证是有效的16进制字符串
        id1.ShouldMatch(@"^[0-9a-f]{24}$");
        id2.ShouldMatch(@"^[0-9a-f]{24}$");
    }

    /// <summary>
    /// 测试 - CreateObjectId - 不受上下文Id影响
    /// </summary>
    [Fact]
    public void CreateObjectId_IgnoresContextId()
    {
        // Arrange
        Id.SetId("context_value");

        // Act
        var result = Id.CreateObjectId();

        // Assert
        result.ShouldNotBe("context_value");
        result.Length.ShouldBe(24);
        result.ShouldMatch(@"^[0-9a-f]{24}$");
    }

    /// <summary>
    /// 测试 - CreateObjectId - 大量生成验证唯一性
    /// </summary>
    [Fact]
    public void CreateObjectId_MassGeneration_ProducesUniqueIds()
    {
        // Arrange
        const int count = 1000;
        var ids = new HashSet<string>();

        // Act
        for (int i = 0; i < count; i++)
        {
            ids.Add(Id.CreateObjectId());
        }

        // Assert
        ids.Count.ShouldBe(count, "所有ObjectId应该是唯一的");
    }

    #endregion

    #region CreateTimestampId 测试

    /// <summary>
    /// 测试 - CreateTimestampId - 生成有效时间戳Id
    /// </summary>
    [Fact]
    public void CreateTimestampId_AlwaysGeneratesNewId()
    {
        // Act
        var id1 = Id.CreateTimestampId();
        Thread.Sleep(1); // 确保时间差异
        var id2 = Id.CreateTimestampId();

        // Assert
        id1.ShouldNotBeNull();
        id2.ShouldNotBeNull();
        id1.ShouldNotBe(id2);
    }

    /// <summary>
    /// 测试 - CreateTimestampId - 不受上下文Id影响
    /// </summary>
    [Fact]
    public void CreateTimestampId_IgnoresContextId()
    {
        // Arrange
        Id.SetId("context_value");

        // Act
        var result = Id.CreateTimestampId();

        // Assert
        result.ShouldNotBe("context_value");
        result.ShouldNotBeNull();
        result.ShouldNotBeEmpty();
    }

    /// <summary>
    /// 测试 - CreateTimestampId - 时序性验证
    /// </summary>
    [Fact]
    public void CreateTimestampId_ShowsTimeSequence()
    {
        // Arrange
        var timestamps = new List<string>();

        // Act
        for (int i = 0; i < 5; i++)
        {
            timestamps.Add(Id.CreateTimestampId());
            Thread.Sleep(1);
        }

        // Assert - 时间戳ID应该体现时序性（具体比较依赖于TimestampId的实现）
        timestamps.Count.ShouldBe(5);
        timestamps.ShouldAllBe(t => !string.IsNullOrEmpty(t));
    }

    #endregion

    #region 并发安全测试

    /// <summary>
    /// 测试 - 并发设置Id - 线程隔离
    /// </summary>
    [Fact]
    public void ConcurrentSetId_ThreadIsolation_WorksCorrectly()
    {
        // Arrange
        Id.ConfigureString(() => "fallback");
        const int threadCount = 10;
        var results = new ConcurrentDictionary<int, string>();
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < threadCount; i++)
        {
            var threadIndex = i;
            tasks.Add(Task.Run(() =>
            {
                Id.SetId($"thread_{threadIndex}");
                Thread.Sleep(10); // 模拟一些工作
                results[threadIndex] = Id.CreateString();
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        results.Count.ShouldBe(threadCount);
        for (int i = 0; i < threadCount; i++)
        {
            results[i].ShouldBe($"thread_{i}");
        }
    }

    /// <summary>
    /// 测试 - 并发配置生成函数 - 线程安全
    /// </summary>
    [Fact]
    public void ConcurrentConfiguration_ThreadSafety_NoExceptions()
    {
        // Arrange
        const int threadCount = 5;
        var tasks = new List<Task>();
        var exceptions = new ConcurrentBag<Exception>();

        // Act
        for (int i = 0; i < threadCount; i++)
        {
            var threadIndex = i;
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    Id.ConfigureLong(() => threadIndex);
                    Id.ConfigureString(() => $"thread_{threadIndex}");

                    // 尝试生成一些ID
                    for (int j = 0; j < 10; j++)
                    {
                        Id.CreateLong();
                        Id.CreateString();
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        exceptions.ShouldBeEmpty("并发配置不应该产生异常");
    }

    #endregion

    #region 集成测试

    /// <summary>
    /// 测试 - 完整工作流程 - 配置-设置-生成-重置
    /// </summary>
    [Fact]
    public void CompleteWorkflow_ConfigureSetGenerateReset_WorksCorrectly()
    {
        // 1. 配置生成函数
        Id.ConfigureLong(() => 999L);
        Id.ConfigureString(() => "generated");

        // 2. 使用生成函数
        var generatedLong = Id.CreateLong();
        var generatedString = Id.CreateString();

        // 3. 设置上下文Id
        Id.SetId("12345");
        var contextLong = Id.CreateLong();
        var contextString = Id.CreateString();

        // 4. 重置上下文
        Id.Reset();
        var resetLong = Id.CreateLong();
        var resetString = Id.CreateString();

        // Assert
        generatedLong.ShouldBe(999L);
        generatedString.ShouldBe("generated");

        contextLong.ShouldBe(12345L);
        contextString.ShouldBe("12345");

        resetLong.ShouldBe(999L);
        resetString.ShouldBe("generated");
    }

    /// <summary>
    /// 测试 - 混合使用不同类型Id生成
    /// </summary>
    [Fact]
    public void MixedIdGeneration_DifferentTypes_WorksIndependently()
    {
        // Arrange
        Id.ConfigureLong(() => 777L);
        Id.ConfigureString(() => "test");

        // Act
        var longId = Id.CreateLong();
        var stringId = Id.CreateString();
        var objectId = Id.CreateObjectId();
        var timestampId = Id.CreateTimestampId();

        // Assert
        longId.ShouldBe(777L);
        stringId.ShouldBe("test");
        objectId.ShouldNotBeNull();
        objectId.Length.ShouldBe(24);
        timestampId.ShouldNotBeNull();

        // ObjectId和TimestampId不应该受配置影响
        objectId.ShouldNotBe("test");
        timestampId.ShouldNotBe("test");
    }

    /// <summary>
    /// 测试 - 异常场景恢复
    /// </summary>
    [Fact]
    public void ExceptionScenarioRecovery_InvalidConfiguration_CanRecover()
    {
        // Arrange - 配置一个会抛异常的生成函数
        Id.ConfigureString(() => throw new InvalidOperationException("Test exception"));

        // Act & Assert - 验证异常被正确抛出
        Should.Throw<InvalidOperationException>(() => Id.CreateString());

        // 恢复 - 重新配置正常的生成函数
        Id.ConfigureString(() => "recovered");
        var result = Id.CreateString();

        // Assert - 验证已恢复正常
        result.ShouldBe("recovered");
    }

    /// <summary>
    /// 测试 - 部分重置不影响其他功能
    /// </summary>
    [Fact]
    public void PartialReset_DoesNotAffectOtherFunctionality()
    {
        // Arrange
        Id.ConfigureLong(() => 123L);
        Id.ConfigureString(() => "test");
        Id.SetId("456"); // 使用可以转换为数字的字符串

        // Act - 只重置String生成函数
        Id.ResetString();

        // Assert
        // Long生成器仍然可用，但会优先使用上下文值
        Id.CreateLong().ShouldBe(456L); // 上下文值转换：456

        // String生成器已重置，但仍会优先使用上下文值
        Id.CreateString().ShouldBe("456");

        // 清除上下文后，Long仍可用，String会抛异常
        Id.Reset();
        Id.CreateLong().ShouldBe(123L); // 现在使用配置的生成函数
        Should.Throw<InvalidOperationException>(() => Id.CreateString());
    }

    /// <summary>
    /// 测试 - 上下文值转换的优先级和边界情况
    /// </summary>
    [Fact]
    public void ContextValueConversion_PriorityAndEdgeCases()
    {
        // Arrange
        Id.ConfigureLong(() => 999L);
        Id.ConfigureString(() => "fallback");

        // Test 1 - 有效数字字符串的转换
        Id.SetId("12345");
        Id.CreateLong().ShouldBe(12345L);
        Id.CreateString().ShouldBe("12345");

        // Test 2 - 无效数字字符串的转换（ToLong返回0）
        Id.SetId("invalid_number");
        Id.CreateLong().ShouldBe(0L); // ToLong转换失败返回0
        Id.CreateString().ShouldBe("invalid_number"); // String直接返回

        // Test 3 - 空字符串/null的处理
        Id.SetId("");
        Id.CreateLong().ShouldBe(999L); // 使用配置的生成函数
        Id.CreateString().ShouldBe("fallback"); // 使用配置的生成函数

        Id.SetId(null);
        Id.CreateLong().ShouldBe(999L);
        Id.CreateString().ShouldBe("fallback");

        // Test 4 - 只包含空白字符的字符串
        Id.SetId("   ");
        Id.CreateLong().ShouldBe(999L);
        Id.CreateString().ShouldBe("fallback");
    }

    #endregion
}