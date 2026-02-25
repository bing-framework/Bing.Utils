using System.Collections.Concurrent;
namespace Bing.IdUtils;
/// <summary>
/// 模型ID访问器测试
/// </summary>
[Trait("Bing.IdUtils", "ModelIdAccessor")]
public class ModelIdAccessorTest
{
    #region 基础功能测试
    /// <summary>
    /// 测试 - 默认构造函数 - 正确初始化
    /// </summary>
    [Fact]
    public void Constructor_Default_InitializesCorrectly()
    {
        // Act
        var accessor = new ModelIdAccessor();
        // Assert
        accessor.GetCurrentIndex().ShouldBe(0);
        accessor.GetCurrentTimeStamp().ShouldBeGreaterThan(DateTime.MinValue);
        accessor.GetCurrentTimeStamp().ShouldBeLessThanOrEqualTo(DateTime.Now);
    }
    /// <summary>
    /// 测试 - 带初始索引的构造函数 - 正确设置初始值
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(999999)]
    public void Constructor_WithInitialIndex_SetsCorrectValue(int initialIndex)
    {
        // Act
        var accessor = new ModelIdAccessor(initialIndex);
        // Assert
        accessor.GetCurrentIndex().ShouldBe(initialIndex);
        accessor.GetCurrentTimeStamp().ShouldBeGreaterThan(DateTime.MinValue);
    }
    /// <summary>
    /// 测试 - 带初始索引的构造函数 - 负数参数验证
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void Constructor_WithNegativeInitialIndex_ThrowsArgumentOutOfRangeException(int invalidIndex)
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => new ModelIdAccessor(invalidIndex))
            .ParamName.ShouldBe("initialIndex");
    }
    #endregion
    #region 索引生成测试
    /// <summary>
    /// 测试 - GetNextIndex - 递增序列生成
    /// </summary>
    [Fact]
    public void GetNextIndex_SequentialCalls_ReturnsIncrementingValues()
    {
        // Arrange
        var accessor = new ModelIdAccessor();
        // Act & Assert
        accessor.GetNextIndex().ShouldBe(0);
        accessor.GetNextIndex().ShouldBe(1);
        accessor.GetNextIndex().ShouldBe(2);
        accessor.GetNextIndex().ShouldBe(3);
        accessor.GetNextIndex().ShouldBe(4);
        accessor.GetNextIndex().ShouldBe(5);
    }
    /// <summary>
    /// 测试 - GetNextIndex - 从自定义起始值递增
    /// </summary>
    [Fact]
    public void GetNextIndex_WithCustomInitialIndex_IncrementsFromStartValue()
    {
        // Arrange
        var accessor = new ModelIdAccessor(100);
        // Act & Assert
        accessor.GetNextIndex().ShouldBe(100);
        accessor.GetNextIndex().ShouldBe(101);
        accessor.GetNextIndex().ShouldBe(102);
    }
    /// <summary>
    /// 测试 - GetCurrentIndex - 不改变索引值
    /// </summary>
    [Fact]
    public void GetCurrentIndex_MultipleCalls_DoesNotChangeValue()
    {
        // Arrange
        var accessor = new ModelIdAccessor(50);
        // Act & Assert
        accessor.GetCurrentIndex().ShouldBe(50);
        accessor.GetCurrentIndex().ShouldBe(50);
        accessor.GetCurrentIndex().ShouldBe(50);
        // 验证GetNextIndex确实会递增
        accessor.GetNextIndex().ShouldBe(50);
        accessor.GetCurrentIndex().ShouldBe(51);
    }
    /// <summary>
    /// 测试 - ResetIndex - 重置索引值
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(500)]
    [InlineData(999999)]
    public void ResetIndex_ValidValue_UpdatesIndexCorrectly(int newIndex)
    {
        // Arrange
        var accessor = new ModelIdAccessor();
        accessor.GetNextIndex(); // 递增到1
        accessor.GetNextIndex(); // 递增到2
        // Act
        accessor.ResetIndex(newIndex);
        // Assert
        accessor.GetCurrentIndex().ShouldBe(newIndex);
        accessor.GetNextIndex().ShouldBe(newIndex);
        accessor.GetNextIndex().ShouldBe(newIndex + 1);
    }
    /// <summary>
    /// 测试 - ResetIndex - 负数参数验证
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void ResetIndex_NegativeValue_ThrowsArgumentOutOfRangeException(int invalidIndex)
    {
        // Arrange
        var accessor = new ModelIdAccessor();
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => accessor.ResetIndex(invalidIndex))
            .ParamName.ShouldBe("newIndex");
    }
    #endregion
    #region 时间戳管理测试
    /// <summary>
    /// 测试 - GetCurrentTimeStamp - 返回一致的时间戳
    /// </summary>
    [Fact]
    public void GetCurrentTimeStamp_MultipleCalls_ReturnsSameValue()
    {
        // Arrange
        var accessor = new ModelIdAccessor();
        // Act
        var timestamp1 = accessor.GetCurrentTimeStamp();
        System.Threading.Thread.Sleep(10); // 等待一段时间
        var timestamp2 = accessor.GetCurrentTimeStamp();
        // Assert
        timestamp1.ShouldBe(timestamp2, "多次调用GetCurrentTimeStamp应返回相同值");
    }
    /// <summary>
    /// 测试 - RefreshTimeStamp - 更新时间戳
    /// </summary>
    [Fact]
    public void RefreshTimeStamp_AfterDelay_ReturnsNewerTimestamp()
    {
        // Arrange
        var accessor = new ModelIdAccessor();
        var originalTimestamp = accessor.GetCurrentTimeStamp();
        // Act
        System.Threading.Thread.Sleep(10); // 确保时间流逝
        var newTimestamp = accessor.RefreshTimeStamp();
        // Assert
        newTimestamp.ShouldBeGreaterThan(originalTimestamp);
        accessor.GetCurrentTimeStamp().ShouldBe(newTimestamp);
    }
    /// <summary>
    /// 测试 - RefreshTimeStamp - 时间戳单调递增特性
    /// </summary>
    [Fact]
    public void RefreshTimeStamp_MultipleCalls_MaintainsMonotonicIncrease()
    {
        // Arrange
        var accessor = new ModelIdAccessor();
        var timestamps = new List<DateTime>();
        // Act
        for (int i = 0; i < 5; i++)
        {
            timestamps.Add(accessor.RefreshTimeStamp());
            System.Threading.Thread.Sleep(1); // 微小延迟
        }
        // Assert
        for (int i = 1; i < timestamps.Count; i++)
        {
            timestamps[i].ShouldBeGreaterThan(timestamps[i - 1],
                $"时间戳[{i}]({timestamps[i]:HH:mm:ss.fff})应该大于时间戳[{i - 1}]({timestamps[i - 1]:HH:mm:ss.fff})");
        }
    }
    #endregion
    #region 组合ID生成测试
    /// <summary>
    /// 测试 - GenerateCompositeId - 无前缀生成
    /// </summary>
    [Fact]
    public void GenerateCompositeId_WithoutPrefix_ReturnsValidFormat()
    {
        // Arrange
        var accessor = new ModelIdAccessor();
        // Act
        var compositeId = accessor.GenerateCompositeId();
        // Assert
        compositeId.ShouldNotBeNullOrEmpty();
        compositeId.ShouldMatch(@"^\d{17}_\d{6}$", "格式应为：时间戳(17位)_索引(6位)");
    }
    /// <summary>
    /// 测试 - GenerateCompositeId - 带前缀生成
    /// </summary>
    [Theory]
    [InlineData("ORDER")]
    [InlineData("BATCH")]
    [InlineData("USER")]
    [InlineData("")]
    public void GenerateCompositeId_WithPrefix_ReturnsValidFormat(string prefix)
    {
        // Arrange
        var accessor = new ModelIdAccessor();
        // Act
        var compositeId = accessor.GenerateCompositeId(prefix);
        // Assert
        compositeId.ShouldNotBeNullOrEmpty();
        if (string.IsNullOrEmpty(prefix))
        {
            compositeId.ShouldMatch(@"^\d{17}_\d{6}$");
        }
        else
        {
            compositeId.ShouldStartWith(prefix);
            compositeId.ShouldMatch($@"^{prefix}_\d{{17}}_\d{{6}}$");
        }
    }
    /// <summary>
    /// 测试 - GenerateCompositeId - 连续生成唯一性
    /// </summary>
    [Fact]
    public void GenerateCompositeId_SequentialGeneration_ProducesUniqueIds()
    {
        // Arrange
        var accessor = new ModelIdAccessor();
        var ids = new HashSet<string>();
        // Act
        for (int i = 0; i < 100; i++)
        {
            var id = accessor.GenerateCompositeId("TEST");
            ids.Add(id).ShouldBeTrue($"生成的ID应该是唯一的: {id}");
        }
        // Assert
        ids.Count.ShouldBe(100, "应该生成100个唯一的ID");
    }
    #endregion
    #region 并发安全测试
    /// <summary>
    /// 测试 - GetNextIndex - 并发安全性
    /// </summary>
    [Fact]
    public void GetNextIndex_ConcurrentAccess_ProducesUniqueValues()
    {
        // Arrange
        var accessor = new ModelIdAccessor();
        const int threadCount = 10;
        const int operationsPerThread = 100;
        var allIndices = new ConcurrentBag<int>();
        var tasks = new List<Task>();
        // Act
        for (int i = 0; i < threadCount; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < operationsPerThread; j++)
                {
                    allIndices.Add(accessor.GetNextIndex());
                }
            }));
        }
        Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(10));
        // Assert
        var uniqueIndices = new HashSet<int>(allIndices);
        allIndices.Count.ShouldBe(threadCount * operationsPerThread);
        uniqueIndices.Count.ShouldBe(threadCount * operationsPerThread, "所有并发生成的索引应该是唯一的");
    }
    /// <summary>
    /// 测试 - RefreshTimeStamp - 并发安全性
    /// </summary>
    [Fact]
    public void RefreshTimeStamp_ConcurrentAccess_IsThreadSafe()
    {
        // Arrange
        var accessor = new ModelIdAccessor();
        const int threadCount = 5;
        var timestamps = new ConcurrentBag<DateTime>();
        var tasks = new List<Task>();
        var exceptions = new ConcurrentBag<Exception>();
        // Act
        for (int i = 0; i < threadCount; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    for (int j = 0; j < 20; j++)
                    {
                        timestamps.Add(accessor.RefreshTimeStamp());
                        System.Threading.Thread.Sleep(1);
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }));
        }
        Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(10));
        // Assert
        exceptions.ShouldBeEmpty("并发刷新时间戳不应该产生异常");
        timestamps.Count.ShouldBe(threadCount * 20);
    }
    /// <summary>
    /// 测试 - 混合操作 - 并发安全性
    /// </summary>
    [Fact]
    public void MixedOperations_ConcurrentAccess_MaintainsDataIntegrity()
    {
        // Arrange
        var accessor = new ModelIdAccessor();
        const int threadCount = 8;
        var results = new ConcurrentBag<(int index, DateTime timestamp, string compositeId)>();
        var tasks = new List<Task>();
        // Act
        for (int i = 0; i < threadCount; i++)
        {
            var threadIndex = i;
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < 50; j++)
                {
                    var index = accessor.GetNextIndex();
                    var timestamp = accessor.GetCurrentTimeStamp();
                    var compositeId = accessor.GenerateCompositeId($"T{threadIndex}");
                    results.Add((index, timestamp, compositeId));
                    if (j % 10 == 0)
                    {
                        accessor.RefreshTimeStamp();
                    }
                }
            }));
        }
        Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(15));
        // Assert
        var allResults = results.ToArray();
        allResults.Length.ShouldBe(threadCount * 50);
        // 验证索引唯一性
        var indices = allResults.Select(r => r.index).ToArray();
        var uniqueIndices = new HashSet<int>(indices);
        uniqueIndices.Count.ShouldBe(indices.Length, "所有索引应该唯一");
        // 验证组合ID唯一性
        var compositeIds = allResults.Select(r => r.compositeId).ToArray();
        var uniqueCompositeIds = new HashSet<string>(compositeIds);
        uniqueCompositeIds.Count.ShouldBe(compositeIds.Length, "所有组合ID应该唯一");
    }
    #endregion
    #region 性能测试
    /// <summary>
    /// 测试 - GetNextIndex - 性能测试
    /// </summary>
    [Fact]
    public void GetNextIndex_Performance_CompletesQuickly()
    {
        // Arrange
        var accessor = new ModelIdAccessor();
        const int operationCount = 100000;
        // Act & Assert
        Should.CompleteIn(() =>
        {
            for (int i = 0; i < operationCount; i++)
            {
                accessor.GetNextIndex();
            }
        }, TimeSpan.FromSeconds(1), $"生成{operationCount}个索引应该在1秒内完成");
    }
    /// <summary>
    /// 测试 - GenerateCompositeId - 性能测试
    /// </summary>
    [Fact]
    public void GenerateCompositeId_Performance_CompletesQuickly()
    {
        // Arrange
        var accessor = new ModelIdAccessor();
        const int operationCount = 10000;
        // Act & Assert
        Should.CompleteIn(() =>
        {
            for (int i = 0; i < operationCount; i++)
            {
                accessor.GenerateCompositeId("PERF");
            }
        }, TimeSpan.FromSeconds(2), $"生成{operationCount}个组合ID应该在2秒内完成");
    }
    #endregion
    #region 边界条件测试
    /// <summary>
    /// 测试 - 索引接近整数最大值的行为
    /// </summary>
    [Fact]
    public void GetNextIndex_NearMaxValue_HandlesOverflowGracefully()
    {
        // Arrange
        var accessor = new ModelIdAccessor(int.MaxValue - 2);
        // Act & Assert
        accessor.GetNextIndex().ShouldBe(int.MaxValue - 2);
        accessor.GetNextIndex().ShouldBe(int.MaxValue - 1);
        accessor.GetNextIndex().ShouldBe(int.MaxValue);
        // 下一次调用会溢出，返回负数（这是.NET的标准行为）
        var overflowValue = accessor.GetNextIndex();
        overflowValue.ShouldBe(int.MinValue);
    }
    /// <summary>
    /// 测试 - 快速连续时间戳刷新
    /// </summary>
    [Fact]
    public void RefreshTimeStamp_RapidCalls_MaintainsStrictOrdering()
    {
        // Arrange
        var accessor = new ModelIdAccessor();
        var timestamps = new List<DateTime>();
        // Act
        for (int i = 0; i < 100; i++)
        {
            timestamps.Add(accessor.RefreshTimeStamp());
            // 不添加延迟，测试NoRepeatTimeStampFactory的处理能力
        }
        // Assert
        for (int i = 1; i < timestamps.Count; i++)
        {
            timestamps[i].ShouldBeGreaterThan(timestamps[i - 1],
                $"快速刷新时间戳[{i}]应该大于[{i - 1}]");
        }
    }
    #endregion
    #region 集成测试
    /// <summary>
    /// 测试 - 完整工作流程 - 实际使用场景模拟
    /// </summary>
    [Fact]
    public void CompleteWorkflow_RealWorldScenario_WorksCorrectly()
    {
        // Arrange - 模拟订单处理系统
        var orderAccessor = new ModelIdAccessor(1000); // 从订单号1000开始
        var batchAccessor = new ModelIdAccessor();     // 批次号从0开始
        var processedOrders = new List<(int orderId, string batchId, DateTime processTime)>();
        // Act - 模拟批量订单处理
        for (int batch = 0; batch < 3; batch++)
        {
            // 每个批次刷新时间
            var batchTime = batchAccessor.RefreshTimeStamp();
            var batchId = batchAccessor.GenerateCompositeId("BATCH");
            // 处理该批次的订单
            for (int order = 0; order < 5; order++)
            {
                var orderId = orderAccessor.GetNextIndex();
                processedOrders.Add((orderId, batchId, batchTime));
            }
        }
        // Assert
        processedOrders.Count.ShouldBe(15); // 3批次 × 5订单
        // 验证订单ID的连续性
        var orderIds = processedOrders.Select(o => o.orderId).ToArray();
        orderIds.ShouldBe(Enumerable.Range(1000, 15).ToArray());
        // 验证批次ID的唯一性
        var batchIds = processedOrders.Select(o => o.batchId).Distinct().ToArray();
        batchIds.Length.ShouldBe(3);
        // 验证每个批次内的时间一致性
        var batchGroups = processedOrders.GroupBy(o => o.batchId);
        foreach (var group in batchGroups)
        {
            var times = group.Select(o => o.processTime).Distinct().ToArray();
            times.Length.ShouldBe(1, "同一批次内的处理时间应该相同");
        }
    }
    #endregion
}
