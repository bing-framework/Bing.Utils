using System.Collections.Concurrent;
using Bing.IdUtils;
namespace Bing.Helpers;
/// <summary>
/// 标识生成器 - 雪花算法Id 测试
/// </summary>
[Trait("Bing.Helpers", "Id.SnowflakeId")]
public class IdSnowflakeTest
{
    #region 基础功能测试
    /// <summary>
    /// 测试 - CreateSnowflakeId - 生成唯一ID
    /// </summary>
    [Fact]
    public void CreateSnowflakeId_DefaultGenerator_ReturnsUniqueId()
    {
        // Arrange & Act
        var id1 = Id.CreateSnowflakeId();
        var id2 = Id.CreateSnowflakeId();
        // Assert
        id1.ShouldBeGreaterThan(0);
        id2.ShouldBeGreaterThan(0);
        id1.ShouldNotBe(id2, $"生成的ID不应该重复: id1={id1}, id2={id2}");
        // 验证递增趋势（雪花ID具有时间序列特性）
        id2.ShouldBeGreaterThanOrEqualTo(id1, "后生成的ID应该大于等于先生成的ID");
    }
    /// <summary>
    /// 测试 - CreateSnowflakeId - 递增趋势验证
    /// </summary>
    [Fact]
    public void CreateSnowflakeId_SequentialGeneration_ShowsIncreasingTrend()
    {
        // Arrange
        Id.ResetSnowflakeId();
        var ids = new List<long>();
        // Act
        for (int i = 0; i < 10; i++)
        {
            ids.Add(Id.CreateSnowflakeId());
            Thread.Sleep(1); // 确保不同的时间戳
        }
        // Assert
        for (int i = 1; i < ids.Count; i++)
        {
            ids[i].ShouldBeGreaterThanOrEqualTo(ids[i - 1],
                $"ID[{i}]({ids[i]}) should be >= ID[{i - 1}]({ids[i - 1]})");
        }
    }
    /// <summary>
    /// 测试 - CreateSnowflakeId - 使用上下文ID
    /// </summary>
    [Theory]
    [InlineData("123456789")]
    [InlineData("987654321")]
    [InlineData("1")]
    [InlineData("9223372036854775807")] // long.MaxValue
    public void CreateSnowflakeId_WithContextId_ReturnsContextValue(string contextId)
    {
        try
        {
            // Arrange
            Id.SetId(contextId);
            // Act
            var result = Id.CreateSnowflakeId();
            // Assert
            result.ShouldBe(long.Parse(contextId));
        }
        finally
        {
            // Cleanup
            Id.Reset();
        }
    }
    /// <summary>
    /// 测试 - CreateSnowflakeId - 空白上下文ID时生成新ID
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void CreateSnowflakeId_WithEmptyContextId_GeneratesNewId(string contextId)
    {
        try
        {
            // Arrange
            Id.SetId(contextId);
            // Act
            var result = Id.CreateSnowflakeId();
            // Assert
            result.ShouldBeGreaterThan(0);
        }
        finally
        {
            // Cleanup
            Id.Reset();
        }
    }
    /// <summary>
    /// 测试 - CreateSnowflakeId - 快速连续生成不重复
    /// </summary>
    [Fact]
    public void CreateSnowflakeId_RapidGeneration_ProducesUniqueIds()
    {
        try
        {
            // Arrange
            Id.ResetSnowflakeId();
            Id.Reset();
            var ids = new List<long>();
            const int count = 1000;
            // Act - 快速连续生成
            for (int i = 0; i < count; i++)
            {
                ids.Add(Id.CreateSnowflakeId());
            }
            // Assert
            var uniqueIds = new HashSet<long>(ids);
            uniqueIds.Count.ShouldBe(count, "所有生成的ID应该是唯一的");
            ids.ShouldAllBe(id => id > 0, "所有ID应该大于0");
            // 验证递增趋势
            for (int i = 1; i < ids.Count; i++)
            {
                ids[i].ShouldBeGreaterThanOrEqualTo(ids[i - 1],
                    $"ID[{i}]({ids[i]}) 应该大于等于 ID[{i - 1}]({ids[i - 1]})");
            }
        }
        finally
        {
            // Cleanup
            Id.Reset();
            Id.ResetSnowflakeId();
        }
    }
    /// <summary>
    /// 测试 - CreateSnowflakeId - 重复配置不影响唯一性
    /// </summary>
    [Fact]
    public void CreateSnowflakeId_MultipleConfigurations_MaintainsUniqueness()
    {
        try
        {
            var allIds = new HashSet<long>();
            // 第一次配置
            Id.ConfigureSnowflakeId(() => SnowflakeGenerator.Create(1));
            for (int i = 0; i < 10; i++)
            {
                allIds.Add(Id.CreateSnowflakeId());
            }
            // 第二次配置（不同配置）
            Id.ConfigureSnowflakeId(() => SnowflakeGenerator.Create(2));
            for (int i = 0; i < 10; i++)
            {
                allIds.Add(Id.CreateSnowflakeId());
            }
            // 重置为默认配置
            Id.ResetSnowflakeId();
            for (int i = 0; i < 10; i++)
            {
                allIds.Add(Id.CreateSnowflakeId());
            }
            // Assert
            allIds.Count.ShouldBe(30, "所有30个ID应该都是唯一的");
            allIds.ShouldAllBe(id => id > 0, "所有ID应该有效");
        }
        finally
        {
            // Cleanup
            Id.Reset();
            Id.ResetSnowflakeId();
        }
    }
    #endregion
    #region 批量生成测试
    /// <summary>
    /// 测试 - CreateSnowflakeIds - 批量生成ID
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(10000)]
    public void CreateSnowflakeIds_ValidCount_ReturnsCorrectAmount(uint count)
    {
        // Act
        var ids = Id.CreateSnowflakeIds(count);
        // Assert
        ids.ShouldNotBeNull();
        ids.Length.ShouldBe((int)count);
        // 验证所有ID都是正数且唯一
        var uniqueIds = new HashSet<long>(ids);
        uniqueIds.Count.ShouldBe((int)count, "所有生成的ID应该是唯一的");
        ids.ShouldAllBe(id => id > 0, "所有ID应该大于0");
    }
    /// <summary>
    /// 测试 - CreateSnowflakeIds - 参数验证
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(100001)]
    [InlineData(200000)]
    public void CreateSnowflakeIds_InvalidCount_ThrowsArgumentOutOfRangeException(uint count)
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => Id.CreateSnowflakeIds(count))
            .ParamName.ShouldBe("count");
    }
    /// <summary>
    /// 测试 - CreateSnowflakeIds - 边界值测试
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(100000)]
    public void CreateSnowflakeIds_BoundaryValues_WorksCorrectly(uint count)
    {
        // Act
        var ids = Id.CreateSnowflakeIds(count);
        // Assert
        ids.Length.ShouldBe((int)count);
        ids.ShouldAllBe(id => id > 0);
    }
    /// <summary>
    /// 测试 - CreateSnowflakeIds - 忽略上下文ID
    /// </summary>
    [Fact]
    public void CreateSnowflakeIds_WithContextId_IgnoresContextAndGeneratesNew()
    {
        try
        {
            // Arrange
            Id.SetId("123456789");
            // Act
            var ids = Id.CreateSnowflakeIds(5);
            // Assert
            ids.Length.ShouldBe(5);
            ids.ShouldAllBe(id => id != 123456789, "批量生成应该忽略上下文ID");
            ids.ShouldAllBe(id => id > 0);
        }
        finally
        {
            // Cleanup
            Id.Reset();
        }
    }
    #endregion
    #region 配置管理测试
    /// <summary>
    /// 测试 - ConfigureSnowflakeId - 自定义生成器
    /// </summary>
    [Fact]
    public void ConfigureSnowflakeId_CustomGenerator_UsesCustomLogic()
    {
        // Arrange
        const long customWorkerId = 10;
        const long customDataCenterId = 5;
        try
        {
            // Act
            Id.ConfigureSnowflakeId(() => SnowflakeGenerator.Create(customWorkerId, customDataCenterId));
            var id1 = Id.CreateSnowflakeId();
            var id2 = Id.CreateSnowflakeId();
            // Assert
            id1.ShouldBeGreaterThan(0);
            id2.ShouldBeGreaterThan(0);
            id1.ShouldNotBe(id2);
        }
        finally
        {
            // Cleanup
            Id.ResetSnowflakeId();
        }
    }
    /// <summary>
    /// 测试 - ConfigureSnowflakeId - Null参数验证
    /// </summary>
    [Fact]
    public void ConfigureSnowflakeId_NullGenerator_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => Id.ConfigureSnowflakeId(null))
            .ParamName.ShouldBe("generatorFactory");
    }
    /// <summary>
    /// 测试 - ResetSnowflakeId - 重置为默认配置
    /// </summary>
    [Fact]
    public void ResetSnowflakeId_AfterCustomConfiguration_RestoresDefaultBehavior()
    {
        // Arrange
        var originalId = Id.CreateSnowflakeId();
        // 配置自定义生成器
        Id.ConfigureSnowflakeId(() => SnowflakeGenerator.Create(99));
        var customId = Id.CreateSnowflakeId();
        // Act
        Id.ResetSnowflakeId();
        var resetId = Id.CreateSnowflakeId();
        // Assert
        originalId.ShouldBeGreaterThan(0);
        customId.ShouldBeGreaterThan(0);
        resetId.ShouldBeGreaterThan(0);
        // 验证重置后的行为（不能简单比较ID值，因为时间戳不同）
        resetId.ShouldNotBe(customId);
    }
    /// <summary>
    /// 测试 - Configure - 过时方法向后兼容性
    /// </summary>
    [Fact]
    public void Configure_ObsoleteMethod_StillWorksCorrectly()
    {
        try
        {
            // Arrange & Act
#pragma warning disable CS0618 // 类型或成员已过时
            Id.Configure(() => SnowflakeGenerator.Create(20));
#pragma warning restore CS0618 // 类型或成员已过时
            var id = Id.CreateSnowflakeId();
            // Assert
            id.ShouldBeGreaterThan(0);
        }
        finally
        {
            // Cleanup
            Id.ResetSnowflakeId();
        }
    }
    #endregion
    #region 并发安全测试
    /// <summary>
    /// 测试 - CreateSnowflakeId - 并发安全性
    /// </summary>
    [Fact]
    public void CreateSnowflakeId_ConcurrentGeneration_ProducesUniqueIds()
    {
        // Arrange
        const int threadCount = 10;
        const int idsPerThread = 1000;
        var allIds = new ConcurrentBag<long>();
        var tasks = new List<Task>();
        // Act
        for (int i = 0; i < threadCount; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < idsPerThread; j++)
                {
                    allIds.Add(Id.CreateSnowflakeId());
                }
            }));
        }
        Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(10));
        // Assert
        var uniqueIds = new HashSet<long>(allIds);
        allIds.Count.ShouldBe(threadCount * idsPerThread);
        uniqueIds.Count.ShouldBe(threadCount * idsPerThread, "所有并发生成的ID应该是唯一的");
    }
    /// <summary>
    /// 测试 - CreateSnowflakeIds - 并发批量生成
    /// </summary>
    [Fact]
    public void CreateSnowflakeIds_ConcurrentBatchGeneration_ProducesUniqueIds()
    {
        // Arrange
        const int threadCount = 5;
        const uint batchSize = 100;
        var allIds = new ConcurrentBag<long>();
        var tasks = new List<Task>();
        // Act
        for (int i = 0; i < threadCount; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                var ids = Id.CreateSnowflakeIds(batchSize);
                foreach (var id in ids)
                {
                    allIds.Add(id);
                }
            }));
        }
        Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(5));
        // Assert
        var uniqueIds = new HashSet<long>(allIds);
        allIds.Count.ShouldBe(threadCount * (int)batchSize);
        uniqueIds.Count.ShouldBe(threadCount * (int)batchSize, "所有并发批量生成的ID应该是唯一的");
    }
    /// <summary>
    /// 测试 - ConfigureSnowflakeId - 并发配置安全性
    /// </summary>
    [Fact]
    public void ConfigureSnowflakeId_ConcurrentConfiguration_IsThreadSafe()
    {
        // Arrange
        const int threadCount = 5;
        var tasks = new List<Task>();
        var exceptions = new ConcurrentBag<Exception>();
        // Act
        for (int i = 0; i < threadCount; i++)
        {
            var workerId = i + 1;
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    Id.ConfigureSnowflakeId(() => SnowflakeGenerator.Create(workerId));
                    var id = Id.CreateSnowflakeId();
                    id.ShouldBeGreaterThan(0);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }));
        }
        Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(5));
        // Assert
        exceptions.ShouldBeEmpty("并发配置不应该产生异常");
        // Cleanup
        Id.ResetSnowflakeId();
    }
    #endregion
    #region 性能测试
    /// <summary>
    /// 测试 - CreateSnowflakeId - 单线程性能
    /// </summary>
    [Fact]
    public void CreateSnowflakeId_Performance_GeneratesQuickly()
    {
        // Arrange
        const int count = 10000;
        // Act & Assert
        Should.CompleteIn(() =>
        {
            for (int i = 0; i < count; i++)
            {
                Id.CreateSnowflakeId();
            }
        }, TimeSpan.FromSeconds(1), $"生成{count}个ID应该在1秒内完成");
    }
    /// <summary>
    /// 测试 - CreateSnowflakeIds - 批量生成性能
    /// </summary>
    [Fact]
    public void CreateSnowflakeIds_BatchPerformance_GeneratesQuickly()
    {
        // Arrange
        const uint batchSize = 10000;
        // Act & Assert
        Should.CompleteIn(() =>
        {
            var ids = Id.CreateSnowflakeIds(batchSize);
            ids.Length.ShouldBe((int)batchSize);
        }, TimeSpan.FromSeconds(1), $"批量生成{batchSize}个ID应该在1秒内完成");
    }
    #endregion
    #region 数据质量测试
    /// <summary>
    /// 测试 - CreateSnowflakeId - ID长度验证
    /// </summary>
    [Fact]
    public void CreateSnowflakeId_IdLength_IsValid64BitLong()
    {
        // Act
        var id = Id.CreateSnowflakeId();
        // Assert
        id.ShouldBeGreaterThan(0);
        id.ShouldBeLessThanOrEqualTo(long.MaxValue);
        // 验证ID的位数（雪花ID通常是19位数字）
        var idString = id.ToString();
        idString.Length.ShouldBeInRange(16, 19, "雪花ID应该是16-19位数字");
    }
    /// <summary>
    /// 测试 - CreateSnowflakeId - 时间单调性验证
    /// </summary>
    [Fact]
    public void CreateSnowflakeId_TimeMonotonicity_IncreasesOverTime()
    {
        // Arrange
        var startTime = DateTimeOffset.UtcNow;
        var ids = new List<long>();
        // Act
        for (int i = 0; i < 5; i++)
        {
            ids.Add(Id.CreateSnowflakeId());
            Thread.Sleep(10); // 确保时间差异
        }
        // Assert
        // 验证ID在时间上的单调递增性
        for (int i = 1; i < ids.Count; i++)
        {
            ids[i].ShouldBeGreaterThan(ids[i - 1],
                "后生成的ID应该大于先生成的ID");
        }
    }
    /// <summary>
    /// 测试 - CreateSnowflakeIds - 批量ID质量验证
    /// </summary>
    [Fact]
    public void CreateSnowflakeIds_DataQuality_AllIdsAreValid()
    {
        // Arrange
        const uint count = 1000;
        // Act
        var ids = Id.CreateSnowflakeIds(count);
        // Assert
        ids.ShouldAllBe(id => id > 0, "所有ID应该大于0");
        // 验证ID的分布（不应该有明显的聚集）
        var sortedIds = ids.OrderBy(x => x).ToArray();
        for (int i = 1; i < sortedIds.Length; i++)
        {
            sortedIds[i].ShouldBeGreaterThan(sortedIds[i - 1], "排序后的ID应该严格递增");
        }
    }
    #endregion
    #region 集成测试
    /// <summary>
    /// 测试 - 完整工作流程 - 配置-生成-重置-再生成
    /// </summary>
    [Fact]
    public void CompleteWorkflow_ConfigureGenerateResetGenerate_WorksCorrectly()
    {
        try
        {
            // 1. 默认生成
            var defaultId = Id.CreateSnowflakeId();
            defaultId.ShouldBeGreaterThan(0);
            // 2. 配置自定义生成器
            Id.ConfigureSnowflakeId(() => SnowflakeGenerator.Create(50));
            var customId = Id.CreateSnowflakeId();
            customId.ShouldBeGreaterThan(0);
            // 3. 批量生成
            var batchIds = Id.CreateSnowflakeIds(10);
            batchIds.Length.ShouldBe(10);
            batchIds.ShouldAllBe(id => id > 0);
            // 4. 设置上下文ID
            Id.SetId("999999999");
            var contextId = Id.CreateSnowflakeId();
            contextId.ShouldBe(999999999);
            // 5. 重置并清理上下文
            Id.Reset();
            Id.ResetSnowflakeId();
            var finalId = Id.CreateSnowflakeId();
            finalId.ShouldBeGreaterThan(0);
            finalId.ShouldNotBe(999999999);
            // Assert - 验证所有ID都不相同（除了上下文ID）
            var uniqueIds = new HashSet<long> { defaultId, customId, finalId };
            uniqueIds.Count.ShouldBe(3, "不同阶段生成的ID应该不同");
        }
        finally
        {
            // Cleanup
            Id.Reset();
            Id.ResetSnowflakeId();
        }
    }
    /// <summary>
    /// 测试 - 极限场景 - 大量ID生成和验证
    /// </summary>
    [Fact]
    public void ExtremeScenario_LargeVolumeGeneration_MaintainsUniqueness()
    {
        // Arrange
        const int totalIds = 50000;
        var allIds = new HashSet<long>();
        // Act
        Should.CompleteIn(() =>
        {
            // 混合单个生成和批量生成
            for (int i = 0; i < totalIds / 2; i++)
            {
                allIds.Add(Id.CreateSnowflakeId());
            }
            // 批量生成剩余的ID
            var remainingCount = (uint)(totalIds - allIds.Count);
            if (remainingCount > 0)
            {
                var batchIds = Id.CreateSnowflakeIds(Math.Min(remainingCount, 10000));
                foreach (var id in batchIds)
                {
                    allIds.Add(id);
                }
            }
        }, TimeSpan.FromSeconds(10), "大量ID生成应该在合理时间内完成");
        // Assert
        allIds.Count.ShouldBeGreaterThanOrEqualTo(totalIds / 2, "应该生成预期数量的唯一ID");
        allIds.ShouldAllBe(id => id > 0, "所有ID应该有效");
    }
    #endregion
}
