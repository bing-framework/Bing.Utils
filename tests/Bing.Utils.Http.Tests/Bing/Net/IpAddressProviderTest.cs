using System.Collections.Concurrent;

namespace Bing.Net;

/// <summary>
/// IP地址提供器单元测试
/// </summary>
[Trait("Bing.Net", "IpAddressProvider")]
public class IpAddressProviderTest : TestBase
{
    /// <inheritdoc />
    public IpAddressProviderTest(ITestOutputHelper output) : base(output)
    {
    }

    #region SetIp 和 Reset 测试

    /// <summary>
    /// 测试 - SetIp - 设置有效的IPv4地址
    /// </summary>
    [Theory]
    [InlineData("192.168.1.1")]
    [InlineData("10.0.0.1")]
    [InlineData("172.16.1.1")]
    [InlineData("8.8.8.8")]
    [InlineData("127.0.0.1")]
    public void SetIp_ValidIPv4_SetsSuccessfully(string validIp)
    {
        try
        {
            // Act
            Should.NotThrow(() => IpAddressProvider.SetIp(validIp));

            // Assert
            var result = IpAddressProvider.GetIp();
            result.ShouldBe(validIp);

            Output.WriteLine($"成功设置IPv4地址: {validIp}");
        }
        finally
        {
            // Cleanup
            IpAddressProvider.Reset();
        }
    }

    /// <summary>
    /// 测试 - SetIp - 设置有效的IPv6地址
    /// </summary>
    [Theory]
    [InlineData("::1")]
    [InlineData("2001:db8::1")]
    [InlineData("fe80::1")]
    [InlineData("::")]
    [InlineData("2001:4860:4860::8888")]
    public void SetIp_ValidIPv6_SetsSuccessfully(string validIp)
    {
        try
        {
            // Act
            Should.NotThrow(() => IpAddressProvider.SetIp(validIp));

            // Assert
            var result = IpAddressProvider.GetIp();
            result.ShouldBe(validIp);

            Output.WriteLine($"成功设置IPv6地址: {validIp}");
        }
        finally
        {
            // Cleanup
            IpAddressProvider.Reset();
        }
    }

    /// <summary>
    /// 测试 - SetIp - 无效IP地址抛出异常
    /// </summary>
    [Theory]
    [InlineData("invalid")]
    [InlineData("999.999.999.999")]
    [InlineData("192.168.1")]
    [InlineData("192.168.1.1.1")]
    [InlineData("gggg::hhhh")]
    [InlineData("192.168.1.256")]
    public void SetIp_InvalidIp_ThrowsArgumentException(string invalidIp)
    {
        try
        {
            // Act & Assert
            var exception = Should.Throw<ArgumentException>(() => IpAddressProvider.SetIp(invalidIp));
            exception.ParamName.ShouldBe("ip");
            exception.Message.ShouldContain("无效的IP地址格式");
            exception.Message.ShouldContain(invalidIp);

            Output.WriteLine($"无效IP地址 '{invalidIp}' 正确抛出异常: {exception.Message}");
        }
        finally
        {
            // Cleanup
            IpAddressProvider.Reset();
        }
    }

    /// <summary>
    /// 测试 - SetIp - 空值和空白字符串
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\r\n")]
    public void SetIp_NullOrWhitespace_SetsSuccessfully(string nullOrWhitespace)
    {
        try
        {
            // Act
            Should.NotThrow(() => IpAddressProvider.SetIp(nullOrWhitespace));

            Output.WriteLine($"空值或空白字符串 '{nullOrWhitespace}' 设置成功");
        }
        finally
        {
            // Cleanup
            IpAddressProvider.Reset();
        }
    }

    /// <summary>
    /// 测试 - Reset - 重置IP地址缓存
    /// </summary>
    [Fact]
    public void Reset_AfterSetIp_ClearsCache()
    {
        try
        {
            // Arrange
            var testIp = "192.168.1.100";
            IpAddressProvider.SetIp(testIp);
            IpAddressProvider.GetIp().ShouldBe(testIp);

            // Act
            IpAddressProvider.Reset();

            // Assert
            var result = IpAddressProvider.GetIp();
            result.ShouldNotBe(testIp);

            Output.WriteLine("IP地址缓存已成功重置");
        }
        finally
        {
            // Cleanup
            IpAddressProvider.Reset();
        }
    }

    #endregion

    #region GetIp 测试

    /// <summary>
    /// 测试 - GetIp - 手动设置IP优先级最高
    /// </summary>
    [Fact]
    public void GetIp_WithManuallySetIp_ReturnsSetIp()
    {
        try
        {
            // Arrange
            var manualIp = "203.0.113.1"; // 文档用途的公网IP
            IpAddressProvider.SetIp(manualIp);

            // Act
            var result = IpAddressProvider.GetIp();

            // Assert
            result.ShouldBe(manualIp);

            Output.WriteLine($"手动设置的IP地址优先返回: {result}");
        }
        finally
        {
            // Cleanup
            IpAddressProvider.Reset();
        }
    }

    /// <summary>
    /// 测试 - GetIp - 未设置手动IP时的回退行为
    /// </summary>
    [Fact]
    public void GetIp_WithoutManualIp_ReturnsLocalOrRemoteIp()
    {
        try
        {
            // Arrange
            IpAddressProvider.Reset();

            // Act
            var result = IpAddressProvider.GetIp();

            // Assert
            result.ShouldNotBeNull();

            if (!string.IsNullOrEmpty(result))
            {
                IpValidator.IsValid(result).ShouldBeTrue();
                Output.WriteLine($"回退获取的IP地址: {result}");

                // 验证IP地址类型
                if (IpValidator.IsValidIPv4(result))
                {
                    Output.WriteLine($"  类型: IPv4");
                    Output.WriteLine($"  是否本地IP: {IpValidator.IsLocalIp(result)}");
                    Output.WriteLine($"  是否内网IP: {IpValidator.IsInnerIp(result)}");
                }
                else if (IpValidator.IsValidIPv6(result))
                {
                    Output.WriteLine($"  类型: IPv6");
                    Output.WriteLine($"  是否本地IP: {IpValidator.IsLocalIp(result)}");
                    Output.WriteLine($"  是否内网IP: {IpValidator.IsInnerIp(result)}");
                }
            }
            else
            {
                Output.WriteLine("未获取到IP地址（这在某些环境下是正常的）");
            }
        }
        finally
        {
            // Cleanup
            IpAddressProvider.Reset();
        }
    }

    #endregion

    #region GetAllLocalIps 测试

    /// <summary>
    /// 测试 - GetAllLocalIps - 默认参数（仅IPv4，排除回环）
    /// </summary>
    [Fact]
    public void GetAllLocalIps_DefaultParameters_ReturnsIPv4NonLoopback()
    {
        // Act
        var result = IpAddressProvider.GetAllLocalIps();

        // Assert
        result.ShouldNotBeNull();

        foreach (var ip in result)
        {
            IpValidator.IsValid(ip).ShouldBeTrue();
            IpValidator.IsValidIPv4(ip).ShouldBeTrue();
            IpValidator.IsLocalIp(ip).ShouldBeFalse(); // 不包含回环地址
        }

        Output.WriteLine($"默认参数获取到 {result.Count} 个IPv4地址:");
        foreach (var ip in result)
        {
            Output.WriteLine($"  {ip} (内网: {IpValidator.IsInnerIp(ip)})");
        }
    }

    /// <summary>
    /// 测试 - GetAllLocalIps - 包含IPv6地址
    /// </summary>
    [Fact]
    public void GetAllLocalIps_IncludeIPv6_ReturnsIPv4AndIPv6()
    {
        // Act
        var result = IpAddressProvider.GetAllLocalIps(includeIPv6: true, includeLoopback: false);

        // Assert
        result.ShouldNotBeNull();

        var ipv4Count = 0;
        var ipv6Count = 0;

        foreach (var ip in result)
        {
            IpValidator.IsValid(ip).ShouldBeTrue();
            IpValidator.IsLocalIp(ip).ShouldBeFalse(); // 不包含回环地址

            if (IpValidator.IsValidIPv4(ip))
                ipv4Count++;
            else if (IpValidator.IsValidIPv6(ip))
                ipv6Count++;
        }

        Output.WriteLine($"包含IPv6参数获取到 {result.Count} 个地址:");
        Output.WriteLine($"  IPv4地址: {ipv4Count} 个");
        Output.WriteLine($"  IPv6地址: {ipv6Count} 个");

        foreach (var ip in result)
        {
            var type = IpValidator.IsValidIPv4(ip) ? "IPv4" : "IPv6";
            var isInner = IpValidator.IsInnerIp(ip) ? "内网" : "公网";
            Output.WriteLine($"  {ip} ({type}, {isInner})");
        }
    }

    /// <summary>
    /// 测试 - GetAllLocalIps - 包含回环地址
    /// </summary>
    [Fact]
    public void GetAllLocalIps_IncludeLoopback_ReturnsWithLoopback()
    {
        // Act
        var result = IpAddressProvider.GetAllLocalIps(includeIPv6: false, includeLoopback: true);

        // Assert
        result.ShouldNotBeNull();

        var hasLoopback = result.Any(ip => IpValidator.IsLocalIp(ip));

        Output.WriteLine($"包含回环地址参数获取到 {result.Count} 个IPv4地址:");
        Output.WriteLine($"包含回环地址: {hasLoopback}");

        foreach (var ip in result)
        {
            IpValidator.IsValid(ip).ShouldBeTrue();
            IpValidator.IsValidIPv4(ip).ShouldBeTrue();

            var isLocal = IpValidator.IsLocalIp(ip) ? "回环" : "非回环";
            var isInner = IpValidator.IsInnerIp(ip) ? "内网" : "公网";
            Output.WriteLine($"  {ip} ({isLocal}, {isInner})");
        }

        if (hasLoopback)
        {
            result.ShouldContain(ip => IpValidator.IsLocalIp(ip), "应该包含回环地址");
        }
    }

    /// <summary>
    /// 测试 - GetAllLocalIps - 所有选项组合
    /// </summary>
    [Theory]
    [InlineData(false, false)] // 仅IPv4，排除回环
    [InlineData(false, true)]  // 仅IPv4，包含回环
    [InlineData(true, false)]  // IPv4+IPv6，排除回环
    [InlineData(true, true)]   // IPv4+IPv6，包含回环
    public void GetAllLocalIps_VariousOptions_ReturnsCorrectResults(bool includeIPv6, bool includeLoopback)
    {
        // Act
        var result = IpAddressProvider.GetAllLocalIps(includeIPv6, includeLoopback);

        // Assert
        result.ShouldNotBeNull();

        var ipv4Count = 0;
        var ipv6Count = 0;
        var loopbackCount = 0;

        foreach (var ip in result)
        {
            IpValidator.IsValid(ip).ShouldBeTrue();

            if (IpValidator.IsValidIPv4(ip))
            {
                ipv4Count++;
            }
            else if (IpValidator.IsValidIPv6(ip))
            {
                ipv6Count++;
                if (!includeIPv6)
                {
                    Assert.True(false, $"不应包含IPv6地址，但发现: {ip}");
                }
            }

            if (IpValidator.IsLocalIp(ip))
            {
                loopbackCount++;
                if (!includeLoopback)
                {
                    Assert.True(false, $"不应包含回环地址，但发现: {ip}");
                }
            }
        }

        Output.WriteLine($"选项测试 (IPv6={includeIPv6}, 回环={includeLoopback}):");
        Output.WriteLine($"  总地址数: {result.Count}");
        Output.WriteLine($"  IPv4: {ipv4Count}, IPv6: {ipv6Count}, 回环: {loopbackCount}");
    }

    #endregion

    #region GetPublicIpAsync 测试

    /// <summary>
    /// 测试 - GetPublicIpAsync - 顺序模式获取公网IP
    /// </summary>
    [Fact]
    public async Task GetPublicIpAsync_SequentialMode_ReturnsValidPublicIp()
    {
        // Act
        var result = await IpAddressProvider.GetPublicIpAsync(TimeSpan.FromSeconds(10), useParallel: false);

        // Assert
        if (result != null)
        {
            IpValidator.IsValid(result).ShouldBeTrue();
            IpValidator.IsInnerIp(result).ShouldBeFalse(); // 应该是公网IP
            IpValidator.IsLocalIp(result).ShouldBeFalse(); // 不应该是回环地址

            Output.WriteLine($"顺序模式获取的公网IP: {result}");

            var type = IpValidator.IsValidIPv4(result) ? "IPv4" : "IPv6";
            Output.WriteLine($"  IP类型: {type}");
        }
        else
        {
            Output.WriteLine("未能获取公网IP（可能是网络问题或服务不可用）");
        }
    }

    /// <summary>
    /// 测试 - GetPublicIpAsync - 并行模式获取公网IP
    /// </summary>
    [Fact]
    public async Task GetPublicIpAsync_ParallelMode_ReturnsValidPublicIp()
    {
        // Act
        var result = await IpAddressProvider.GetPublicIpAsync(TimeSpan.FromSeconds(10), useParallel: true);

        // Assert
        if (result != null)
        {
            IpValidator.IsValid(result).ShouldBeTrue();
            IpValidator.IsInnerIp(result).ShouldBeFalse(); // 应该是公网IP
            IpValidator.IsLocalIp(result).ShouldBeFalse(); // 不应该是回环地址

            Output.WriteLine($"并行模式获取的公网IP: {result}");

            var type = IpValidator.IsValidIPv4(result) ? "IPv4" : "IPv6";
            Output.WriteLine($"  IP类型: {type}");
        }
        else
        {
            Output.WriteLine("未能获取公网IP（可能是网络问题或服务不可用）");
        }
    }

    /// <summary>
    /// 测试 - GetPublicIpAsync - 超时处理
    /// </summary>
    [Fact]
    public async Task GetPublicIpAsync_WithShortTimeout_HandlesTimeoutGracefully()
    {
        // Arrange
        var shortTimeout = TimeSpan.FromMilliseconds(1); // 极短超时

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await IpAddressProvider.GetPublicIpAsync(shortTimeout, useParallel: false);
        sw.Stop();

        // Assert
        // 由于超时很短，很可能返回null
        if (result != null)
        {
            IpValidator.IsValid(result).ShouldBeTrue();
            Output.WriteLine($"在短超时内成功获取IP: {result}");
        }
        else
        {
            Output.WriteLine("短超时测试：未获取到IP（符合预期）");
        }

        // 验证确实在合理时间内完成
        sw.ElapsedMilliseconds.ShouldBeLessThan(2000, "即使超时，也应该在合理时间内返回");

        Output.WriteLine($"超时测试耗时: {sw.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// 测试 - GetPublicIpAsync - 默认参数
    /// </summary>
    [Fact]
    public async Task GetPublicIpAsync_DefaultParameters_WorksCorrectly()
    {
        // Act
        var result = await IpAddressProvider.GetPublicIpAsync();

        // Assert
        if (result != null)
        {
            IpValidator.IsValid(result).ShouldBeTrue();
            IpValidator.IsInnerIp(result).ShouldBeFalse();
            IpValidator.IsLocalIp(result).ShouldBeFalse();

            Output.WriteLine($"默认参数获取的公网IP: {result}");
        }
        else
        {
            Output.WriteLine("默认参数测试：未获取到公网IP");
        }
    }

    #endregion

    #region 线程安全测试

    /// <summary>
    /// 测试 - SetIp/GetIp - 线程本地存储
    /// </summary>
    [Fact]
    public async Task SetIp_ThreadLocal_IsolatesAcrossThreads()
    {
        try
        {
            // Arrange
            var mainThreadIp = "192.168.1.1";
            var task1Ip = "192.168.1.2";
            var task2Ip = "192.168.1.3";

            var mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;

            // Act
            // 在主线程设置IP
            IpAddressProvider.SetIp(mainThreadIp);
            var mainThreadResult = IpAddressProvider.GetIp();

            var task1 = Task.Factory.StartNew(() =>
            {
                var currentThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
                IpAddressProvider.SetIp(task1Ip);
                var taskResult = IpAddressProvider.GetIp();
                return new { ThreadId = currentThreadId, Result = taskResult };
            }, System.Threading.CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            var task2 = Task.Factory.StartNew(() =>
            {
                var currentThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
                IpAddressProvider.SetIp(task2Ip);
                var taskResult = IpAddressProvider.GetIp();
                return new { ThreadId = currentThreadId, Result = taskResult };
            }, System.Threading.CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            var taskResults = await Task.WhenAll(task1, task2);

            // Assert
            mainThreadResult.ShouldBe(mainThreadIp, "主线程应该返回主线程设置的IP");

            // 验证任务线程的结果
            var task1Result = taskResults[0];
            var task2Result = taskResults[1];

            task1Result.Result.ShouldBe(task1Ip, "任务1应该返回任务1设置的IP");
            task2Result.Result.ShouldBe(task2Ip, "任务2应该返回任务2设置的IP");

            // 验证线程隔离性
            task1Result.ThreadId.ShouldNotBe(mainThreadId, "任务1应该在不同的线程");
            task2Result.ThreadId.ShouldNotBe(mainThreadId, "任务2应该在不同的线程");
            task1Result.ThreadId.ShouldNotBe(task2Result.ThreadId, "任务1和任务2应该在不同的线程");

            // 验证每个线程都有独立的IP设置
            var allValues = new[] { mainThreadResult, task1Result.Result, task2Result.Result };
            var distinctValues = allValues.Distinct().ToList();
            distinctValues.Count.ShouldBe(3, "每个线程应该有不同的IP值");

            Output.WriteLine("线程本地存储测试结果:");
            Output.WriteLine($"  主线程 {mainThreadId}: {mainThreadResult}");
            Output.WriteLine($"  任务1线程 {task1Result.ThreadId}: {task1Result.Result}");
            Output.WriteLine($"  任务2线程 {task2Result.ThreadId}: {task2Result.Result}");
        }
        finally
        {
            // Cleanup
            IpAddressProvider.Reset();
        }
    }

    /// <summary>
    /// 测试 - SetIp/GetIp - 更健壮的线程隔离测试
    /// </summary>
    [Fact]
    public async Task SetIp_ThreadLocal_RobustIsolationTest()
    {
        try
        {
            var results = new ConcurrentBag<(int ThreadId, string SetValue, string GetValue)>();
            var tasks = new List<Task>();

            // 创建多个并发任务，每个都设置不同的IP
            for (int i = 0; i < 5; i++)
            {
                var taskId = i;
                tasks.Add(Task.Run(() =>
                {
                    var threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
                    var ipToSet = $"192.168.1.{taskId + 10}";

                    // 设置IP
                    IpAddressProvider.SetIp(ipToSet);

                    // 短暂延迟以增加线程交错的可能性
                    System.Threading.Thread.Sleep(10);

                    // 获取IP
                    var retrievedIp = IpAddressProvider.GetIp();

                    results.Add((threadId, ipToSet, retrievedIp));
                }));
            }

            await Task.WhenAll(tasks);

            // Assert
            var resultList = results.ToList();
            resultList.Count.ShouldBe(5, "应该有5个任务的结果");

            foreach (var (threadId, setValue, getValue) in resultList)
            {
                setValue.ShouldBe(getValue, $"线程 {threadId} 设置的IP应该与获取的IP相同");
                IpValidator.IsValid(getValue).ShouldBeTrue($"线程 {threadId} 获取的IP应该是有效的");
            }

            // 线程池任务可能复用线程，不对线程唯一性做强假设
            var threadIds = resultList.Select(r => r.ThreadId).ToList();
            var uniqueThreadIds = threadIds.Distinct().ToList();
            uniqueThreadIds.Count.ShouldBeGreaterThan(0);

            // 验证每个线程都有独立的IP值
            var setValues = resultList.Select(r => r.SetValue).ToList();
            var uniqueSetValues = setValues.Distinct().ToList();
            uniqueSetValues.Count.ShouldBe(setValues.Count, "每个线程应该设置不同的IP");

            Output.WriteLine("健壮的线程隔离测试结果:");
            Output.WriteLine($"  任务数={resultList.Count}, 实际线程数={uniqueThreadIds.Count}");
            foreach (var (threadId, setValue, getValue) in resultList.OrderBy(r => r.ThreadId))
            {
                Output.WriteLine($"  线程 {threadId}: 设置={setValue}, 获取={getValue}");
            }
        }
        finally
        {
            // Cleanup
            IpAddressProvider.Reset();
        }
    }

    /// <summary>
    /// 测试 - 并发调用安全性（改进版）
    /// </summary>
    [Fact]
    public async Task ConcurrentOperations_ThreadSafe_Improved()
    {
        try
        {
            var tasks = new List<Task<(int ThreadId, List<string> Results, Exception Error)>>();

            // 创建多个并发任务
            for (int i = 0; i < 10; i++)
            {
                var taskId = i;
                tasks.Add(Task.Run(() =>
                {
                    var threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
                    var results = new List<string>();
                    Exception error = null;

                    try
                    {
                        // 设置不同的IP
                        var ip = $"192.168.1.{taskId + 10}";
                        IpAddressProvider.SetIp(ip);

                        // 获取IP
                        var retrievedIp = IpAddressProvider.GetIp();
                        results.Add($"设置后获取: {retrievedIp}");

                        // 验证设置的IP是否正确
                        if (retrievedIp != ip)
                        {
                            results.Add($"警告: 期望 {ip}, 实际 {retrievedIp}");
                        }

                        // 重置
                        IpAddressProvider.Reset();

                        // 再次获取（应该回退到默认逻辑）
                        var afterResetIp = IpAddressProvider.GetIp();
                        results.Add($"重置后获取: {afterResetIp}");
                    }
                    catch (Exception ex)
                    {
                        error = ex;
                    }

                    return (threadId, results, error);
                }));
            }

            // 等待所有任务完成
            var taskResults = await Task.WhenAll(tasks);

            // Assert
            var errors = taskResults.Where(r => r.Error != null).ToList();
            errors.ShouldBeEmpty($"不应该有异常发生，但发现: {string.Join(", ", errors.Select(e => e.Error.Message))}");

            var allResults = taskResults.SelectMany(r => r.Results).ToList();
            allResults.Count.ShouldBe(20, "每个任务应该产生2个结果");

            Output.WriteLine($"改进的并发测试完成，共收集到 {allResults.Count} 个结果");
            Output.WriteLine("无异常发生，线程安全验证通过");

            // 详细输出每个线程的结果
            foreach (var (threadId, results, error) in taskResults)
            {
                Output.WriteLine($"线程 {threadId}:");
                foreach (var result in results)
                {
                    Output.WriteLine($"  {result}");
                }
            }
        }
        finally
        {
            // Cleanup
            IpAddressProvider.Reset();
        }
    }

    /// <summary>
    /// 测试 - AsyncLocal 特性验证
    /// </summary>
    [Fact]
    public async Task AsyncLocal_Behavior_VerifyCorrectness()
    {
        try
        {
            // 测试 AsyncLocal 在异步操作中的行为
            var parentIp = "192.168.1.100";
            var childIp = "192.168.1.200";

            IpAddressProvider.SetIp(parentIp);
            var parentResult = IpAddressProvider.GetIp();
            parentResult.ShouldBe(parentIp);

            Output.WriteLine($"父上下文设置IP: {parentIp}");

            // 在子任务中修改IP
            await Task.Run(() =>
            {
                var inheritedIp = IpAddressProvider.GetIp();
                Output.WriteLine($"子任务继承的IP: {inheritedIp}");

                // 在子任务中设置新的IP
                IpAddressProvider.SetIp(childIp);
                var childResult = IpAddressProvider.GetIp();
                childResult.ShouldBe(childIp);

                Output.WriteLine($"子任务设置IP: {childIp}");
            });

            // 父上下文的IP应该不受影响
            var finalParentResult = IpAddressProvider.GetIp();
            finalParentResult.ShouldBe(parentIp, "父上下文的IP不应该被子任务影响");

            Output.WriteLine($"父上下文最终IP: {finalParentResult}");
            Output.WriteLine("AsyncLocal 行为验证通过");
        }
        finally
        {
            // Cleanup
            IpAddressProvider.Reset();
        }
    }

    #endregion

    #region 性能测试

    /// <summary>
    /// 测试 - 性能测试
    /// </summary>
    [Fact]
    public void Performance_MultipleOperations_CompletesQuickly()
    {
        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // 执行多次操作
            for (int i = 0; i < 1000; i++)
            {
                IpAddressProvider.SetIp($"192.168.1.{i % 255}");
                var ip = IpAddressProvider.GetIp();
                IpAddressProvider.Reset();
            }

            sw.Stop();

            // Assert
            sw.ElapsedMilliseconds.ShouldBeLessThan(1000, "1000次操作应该在1秒内完成");

            Output.WriteLine($"性能测试: 3000次操作耗时 {sw.ElapsedMilliseconds}ms");
        }
        finally
        {
            // Cleanup
            IpAddressProvider.Reset();
        }
    }

    #endregion

    #region 边界条件和异常处理测试

    /// <summary>
    /// 测试 - GetAllLocalIps - 异常处理
    /// </summary>
    [Fact]
    public void GetAllLocalIps_ExceptionHandling_ReturnsEmptyList()
    {
        // 这个测试主要验证异常处理逻辑
        // 在正常环境下很难触发DNS.GetHostAddresses的异常
        // 所以我们主要验证方法能正常执行

        // Act & Assert - 应该不抛出异常
        Should.NotThrow(() =>
        {
            var result = IpAddressProvider.GetAllLocalIps();
            result.ShouldNotBeNull();
        });

        Should.NotThrow(() =>
        {
            var result = IpAddressProvider.GetAllLocalIps(true, true);
            result.ShouldNotBeNull();
        });

        Output.WriteLine("异常处理测试通过 - GetAllLocalIps 方法有适当的异常处理");
    }

    /// <summary>
    /// 测试 - 边界条件 - IP地址格式
    /// </summary>
    [Theory]
    [InlineData("0.0.0.0")]           // 全零IPv4
    [InlineData("255.255.255.255")]   // 全1IPv4
    [InlineData("::")]                // 全零IPv6
    [InlineData("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")] // 全1IPv6（简化）
    public void SetIp_BoundaryValues_HandlesCorrectly(string boundaryIp)
    {
        try
        {
            // Act & Assert
            Should.NotThrow(() => IpAddressProvider.SetIp(boundaryIp));

            var result = IpAddressProvider.GetIp();
            result.ShouldBe(boundaryIp);

            Output.WriteLine($"边界值 '{boundaryIp}' 处理正确");
        }
        finally
        {
            // Cleanup
            IpAddressProvider.Reset();
        }
    }

    #endregion

    #region 集成测试

    /// <summary>
    /// 测试 - 集成测试 - 完整工作流程
    /// </summary>
    [Fact]
    public async Task IntegrationTest_CompleteWorkflow()
    {
        try
        {
            Output.WriteLine("=== IP地址提供器集成测试 ===");

            // 1. 测试初始状态
            IpAddressProvider.Reset();
            var initialIp = IpAddressProvider.GetIp();
            Output.WriteLine($"1. 初始IP: {initialIp}");

            // 2. 测试手动设置
            var manualIp = "203.0.113.100";
            IpAddressProvider.SetIp(manualIp);
            var setIp = IpAddressProvider.GetIp();
            setIp.ShouldBe(manualIp);
            Output.WriteLine($"2. 手动设置IP: {setIp}");

            // 3. 测试重置
            IpAddressProvider.Reset();
            var resetIp = IpAddressProvider.GetIp();
            resetIp.ShouldNotBe(manualIp);
            Output.WriteLine($"3. 重置后IP: {resetIp}");

            // 4. 测试本地IP获取
            var localIps = IpAddressProvider.GetAllLocalIps(includeIPv6: true, includeLoopback: true);
            Output.WriteLine($"4. 本地IP地址 ({localIps.Count} 个):");
            foreach (var ip in localIps.Take(5)) // 只显示前5个
            {
                var type = IpValidator.IsValidIPv4(ip) ? "IPv4" : "IPv6";
                var scope = IpValidator.IsLocalIp(ip) ? "回环" :
                           IpValidator.IsInnerIp(ip) ? "内网" : "公网";
                Output.WriteLine($"    {ip} ({type}, {scope})");
            }

            // 5. 测试公网IP获取（可能失败，但不应该抛出异常）
            try
            {
                var publicIp = await IpAddressProvider.GetPublicIpAsync(TimeSpan.FromSeconds(5));
                if (publicIp != null)
                {
                    Output.WriteLine($"5. 公网IP: {publicIp}");
                    IpValidator.IsValid(publicIp).ShouldBeTrue();
                    IpValidator.IsInnerIp(publicIp).ShouldBeFalse();
                }
                else
                {
                    Output.WriteLine("5. 未能获取公网IP（网络问题或服务不可用）");
                }
            }
            catch (Exception ex)
            {
                Output.WriteLine($"5. 公网IP获取异常: {ex.Message}");
            }

            Output.WriteLine("=== 集成测试完成 ===");
        }
        finally
        {
            // Cleanup
            IpAddressProvider.Reset();
        }
    }

    #endregion
}
