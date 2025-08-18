using Bing.Tests;
using System.Numerics;

namespace Bing.Net.IPv6;

/// <summary>
/// IPv6地址操作器单元测试
/// </summary>
[Trait("Bing.Net", "IpOperator")]
public class IPv6OperatorTest : TestBase
{
    /// <inheritdoc />
    public IPv6OperatorTest(ITestOutputHelper output) : base(output)
    {
    }

    #region Sort 测试

    /// <summary>
    /// 测试 - Sort - 升序排列IPv6地址
    /// </summary>
    [Fact]
    public void Sort_AscendingOrder_ReturnsCorrectOrder()
    {
        // Arrange
        var addresses = new[]
        {
            "2001:db8::3",
            "2001:db8::1",
            "::1",
            "2001:db8::2",
            "::",
            "fe80::1"
        };

        var expected = new[]
        {
            "::",
            "::1",
            "2001:db8::1",
            "2001:db8::2",
            "2001:db8::3",
            "fe80::1"
        };

        // Act
        var result = IPv6Operator.Sort(addresses, ascending: true);

        // Assert
        result.Count.ShouldBe(expected.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            IPv6Converter.AreEqual(result[i], expected[i]).ShouldBeTrue($"位置 {i}: 期望 '{expected[i]}', 实际 '{result[i]}'");
        }

        Output.WriteLine("升序排列结果:");
        foreach (var addr in result)
        {
            Output.WriteLine($"  {addr}");
        }
    }

    /// <summary>
    /// 测试 - Sort - 降序排列IPv6地址
    /// </summary>
    [Fact]
    public void Sort_DescendingOrder_ReturnsCorrectOrder()
    {
        // Arrange
        var addresses = new[]
        {
            "2001:db8::1",
            "::1",
            "2001:db8::3",
            "::",
            "2001:db8::2"
        };

        var expected = new[]
        {
            "2001:db8::3",
            "2001:db8::2",
            "2001:db8::1",
            "::1",
            "::"
        };

        // Act
        var result = IPv6Operator.Sort(addresses, ascending: false);

        // Assert
        result.Count.ShouldBe(expected.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            IPv6Converter.AreEqual(result[i], expected[i]).ShouldBeTrue($"位置 {i}: 期望 '{expected[i]}', 实际 '{result[i]}'");
        }

        Output.WriteLine("降序排列结果:");
        foreach (var addr in result)
        {
            Output.WriteLine($"  {addr}");
        }
    }

    /// <summary>
    /// 测试 - Sort - 包含无效地址的列表过滤
    /// </summary>
    [Fact]
    public void Sort_WithInvalidAddresses_FiltersInvalidOnes()
    {
        // Arrange
        var addresses = new[]
        {
            "2001:db8::2",
            "invalid",
            "::1",
            "192.168.1.1",  // IPv4地址
            "2001:db8::1",
            "gggg::1",      // 无效字符
            "::"
        };

        var expectedValid = new[]
        {
            "::",
            "::1",
            "2001:db8::1",
            "2001:db8::2"
        };

        // Act
        var result = IPv6Operator.Sort(addresses);

        // Assert
        result.Count.ShouldBe(expectedValid.Length, "应该过滤掉无效地址");
        for (int i = 0; i < expectedValid.Length; i++)
        {
            IPv6Converter.AreEqual(result[i], expectedValid[i]).ShouldBeTrue();
        }

        Output.WriteLine("过滤并排序后的结果:");
        foreach (var addr in result)
        {
            Output.WriteLine($"  {addr}");
        }
    }

    /// <summary>
    /// 测试 - Sort - 空列表处理
    /// </summary>
    [Fact]
    public void Sort_EmptyList_ReturnsEmptyList()
    {
        // Arrange
        var addresses = new string[0];

        // Act
        var result = IPv6Operator.Sort(addresses);

        // Assert
        result.ShouldBeEmpty();
        Output.WriteLine("空列表排序结果: 空列表");
    }

    /// <summary>
    /// 测试 - Sort - 单个地址
    /// </summary>
    [Fact]
    public void Sort_SingleAddress_ReturnsSingleAddress()
    {
        // Arrange
        var addresses = new[] { "2001:db8::1" };

        // Act
        var result = IPv6Operator.Sort(addresses);

        // Assert
        result.Count.ShouldBe(1);
        IPv6Converter.AreEqual(result[0], "2001:db8::1").ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - Sort - 相同地址的处理
    /// </summary>
    [Fact]
    public void Sort_DuplicateAddresses_PreservesAll()
    {
        // Arrange
        var addresses = new[]
        {
            "2001:db8::1",
            "::1",
            "2001:db8::1",  // 重复
            "::1"           // 重复
        };

        // Act
        var result = IPv6Operator.Sort(addresses);

        // Assert
        result.Count.ShouldBe(4, "应该保留所有地址，包括重复的");

        Output.WriteLine("包含重复地址的排序结果:");
        foreach (var addr in result)
        {
            Output.WriteLine($"  {addr}");
        }
    }

    #endregion

    #region GetIpDistance 测试

    /// <summary>
    /// 测试 - GetIpDistance - 计算相同地址的距离
    /// </summary>
    [Fact]
    public void GetIpDistance_SameAddress_ReturnsZero()
    {
        // Arrange
        var address = "2001:db8::1";

        // Act
        var distance = IPv6Operator.GetIpDistance(address, address);

        // Assert
        distance.ShouldBe(BigInteger.Zero);
        Output.WriteLine($"相同地址距离: {distance}");
    }

    /// <summary>
    /// 测试 - GetIpDistance - 计算连续地址的距离
    /// </summary>
    [Theory]
    [InlineData("::", "::1", "1")]
    [InlineData("::1", "::2", "1")]
    [InlineData("::", "::100", "256")]  // 0x100 = 256
    [InlineData("2001:db8::", "2001:db8::1", "1")]
    public void GetIpDistance_ConsecutiveAddresses_ReturnsCorrectDistance(string addr1, string addr2, string expectedStr)
    {
        // Arrange
        var expected = BigInteger.Parse(expectedStr);

        // Act
        var distance = IPv6Operator.GetIpDistance(addr1, addr2);

        // Assert
        distance.ShouldBe(expected);
        Output.WriteLine($"距离计算: '{addr1}' <-> '{addr2}' = {distance}");
    }

    /// <summary>
    /// 测试 - GetIpDistance - 大范围地址距离
    /// </summary>
    [Fact]
    public void GetIpDistance_LargeRange_ReturnsCorrectDistance()
    {
        // Arrange
        var addr1 = "::";
        var addr2 = "ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff";

        // Act
        var distance = IPv6Operator.GetIpDistance(addr1, addr2);

        // Assert
        var maxIPv6Value = (BigInteger.One << 128) - 1;
        distance.ShouldBe(maxIPv6Value);

        Output.WriteLine($"最大范围距离: {distance}");
        Output.WriteLine($"预期最大值: {maxIPv6Value}");
    }

    /// <summary>
    /// 测试 - GetIpDistance - 地址顺序不影响距离
    /// </summary>
    [Fact]
    public void GetIpDistance_AddressOrder_DoesNotMatter()
    {
        // Arrange
        var addr1 = "2001:db8::1";
        var addr2 = "2001:db8::100";

        // Act
        var distance1 = IPv6Operator.GetIpDistance(addr1, addr2);
        var distance2 = IPv6Operator.GetIpDistance(addr2, addr1);

        // Assert
        distance1.ShouldBe(distance2, "地址顺序不应该影响距离计算");
        distance1.ShouldBeGreaterThan(0);

        Output.WriteLine($"双向距离计算: {distance1} == {distance2}");
    }

    /// <summary>
    /// 测试 - GetIpDistance - 无效地址抛出异常
    /// </summary>
    [Theory]
    [InlineData("invalid", "2001:db8::1")]
    [InlineData("2001:db8::1", "invalid")]
    [InlineData("192.168.1.1", "2001:db8::1")]
    [InlineData("", "2001:db8::1")]
    [InlineData(null, "2001:db8::1")]
    public void GetIpDistance_InvalidAddress_ThrowsArgumentException(string addr1, string addr2)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => IPv6Operator.GetIpDistance(addr1, addr2))
            .Message.ShouldContain("无效的IPv6地址");

        Output.WriteLine($"无效地址 '{addr1}', '{addr2}' 正确抛出异常");
    }

    #endregion

    #region GetNextIp 测试

    /// <summary>
    /// 测试 - GetNextIp - 获取下一个IPv6地址
    /// </summary>
    [Theory]
    [InlineData("::", "::1")]
    [InlineData("::1", "::2")]
    [InlineData("::ff", "::100")]
    [InlineData("::ffff", "::1:0")]
    [InlineData("2001:db8::", "2001:db8::1")]
    public void GetNextIp_SingleStep_ReturnsCorrectNext(string current, string expected)
    {
        // Act
        var result = IPv6Operator.GetNextIp(current);

        // Assert
        IPv6Converter.AreEqual(result, expected).ShouldBeTrue($"'{current}' 的下一个地址应该是 '{expected}', 实际为 '{result}'");
        Output.WriteLine($"下一个地址: '{current}' -> '{result}'");
    }

    /// <summary>
    /// 测试 - GetNextIp - 多步递增
    /// </summary>
    [Theory]
    [InlineData("::", 5, "::5")]
    [InlineData("::1", 10, "::b")]
    [InlineData("2001:db8::", 256, "2001:db8::100")]
    public void GetNextIp_MultipleSteps_ReturnsCorrectNext(string current, int step, string expected)
    {
        // Act
        var result = IPv6Operator.GetNextIp(current, step);

        // Assert
        IPv6Converter.AreEqual(result, expected).ShouldBeTrue($"'{current}' + {step} 应该是 '{expected}', 实际为 '{result}'");
        Output.WriteLine($"多步递增: '{current}' + {step} -> '{result}'");
    }

    /// <summary>
    /// 测试 - GetNextIp - 零步长返回相同地址
    /// </summary>
    [Fact]
    public void GetNextIp_ZeroStep_ReturnsSameAddress()
    {
        // Arrange
        var address = "2001:db8::1";

        // Act
        var result = IPv6Operator.GetNextIp(address, 0);

        // Assert
        IPv6Converter.AreEqual(result, address).ShouldBeTrue();
        Output.WriteLine($"零步长: '{address}' -> '{result}'");
    }

    /// <summary>
    /// 测试 - GetNextIp - 溢出处理
    /// </summary>
    [Fact]
    public void GetNextIp_Overflow_HandlesCorrectly()
    {
        // Arrange
        var maxAddress = "ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff";

        // Act
        var result = IPv6Operator.GetNextIp(maxAddress);

        // Assert
        IPv6Converter.AreEqual(result, "::").ShouldBeTrue("最大地址的下一个应该是全零地址（溢出）");
        Output.WriteLine($"溢出测试: '{maxAddress}' -> '{result}'");
    }

    /// <summary>
    /// 测试 - GetNextIp - 无效参数抛出异常
    /// </summary>
    [Theory]
    [InlineData("invalid")]
    [InlineData("192.168.1.1")]
    [InlineData("")]
    [InlineData(null)]
    public void GetNextIp_InvalidAddress_ThrowsArgumentException(string invalidAddress)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => IPv6Operator.GetNextIp(invalidAddress))
            .Message.ShouldContain("无效的IPv6地址");
    }

    /// <summary>
    /// 测试 - GetNextIp - 负步长抛出异常
    /// </summary>
    [Fact]
    public void GetNextIp_NegativeStep_ThrowsArgumentException()
    {
        // Arrange
        var address = "2001:db8::1";

        // Act & Assert
        Should.Throw<ArgumentException>(() => IPv6Operator.GetNextIp(address, -1))
            .Message.ShouldContain("步长不能为负数");
    }

    #endregion

    #region GetPreviousIp 测试

    /// <summary>
    /// 测试 - GetPreviousIp - 获取前一个IPv6地址
    /// </summary>
    [Theory]
    [InlineData("::1", "::")]
    [InlineData("::2", "::1")]
    [InlineData("::100", "::ff")]
    [InlineData("::1:0", "::ffff")]
    [InlineData("2001:db8::1", "2001:db8::")]
    public void GetPreviousIp_SingleStep_ReturnsCorrectPrevious(string current, string expected)
    {
        // Act
        var result = IPv6Operator.GetPreviousIp(current);

        // Assert
        IPv6Converter.AreEqual(result, expected).ShouldBeTrue($"'{current}' 的前一个地址应该是 '{expected}', 实际为 '{result}'");
        Output.WriteLine($"前一个地址: '{current}' -> '{result}'");
    }

    /// <summary>
    /// 测试 - GetPreviousIp - 多步递减
    /// </summary>
    [Theory]
    [InlineData("::5", 5, "::")]
    [InlineData("::b", 10, "::1")]
    [InlineData("2001:db8::100", 256, "2001:db8::")]
    public void GetPreviousIp_MultipleSteps_ReturnsCorrectPrevious(string current, int step, string expected)
    {
        // Act
        var result = IPv6Operator.GetPreviousIp(current, step);

        // Assert
        IPv6Converter.AreEqual(result, expected).ShouldBeTrue($"'{current}' - {step} 应该是 '{expected}', 实际为 '{result}'");
        Output.WriteLine($"多步递减: '{current}' - {step} -> '{result}'");
    }

    /// <summary>
    /// 测试 - GetPreviousIp - 零步长返回相同地址
    /// </summary>
    [Fact]
    public void GetPreviousIp_ZeroStep_ReturnsSameAddress()
    {
        // Arrange
        var address = "2001:db8::1";

        // Act
        var result = IPv6Operator.GetPreviousIp(address, 0);

        // Assert
        IPv6Converter.AreEqual(result, address).ShouldBeTrue();
        Output.WriteLine($"零步长: '{address}' -> '{result}'");
    }

    /// <summary>
    /// 测试 - GetPreviousIp - 下溢处理
    /// </summary>
    [Fact]
    public void GetPreviousIp_Underflow_HandlesCorrectly()
    {
        // Arrange
        var minAddress = "::";

        // Act
        var result = IPv6Operator.GetPreviousIp(minAddress);

        // Assert
        IPv6Converter.AreEqual(result, "ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff").ShouldBeTrue("全零地址的前一个应该是最大地址（下溢）");
        Output.WriteLine($"下溢测试: '{minAddress}' -> '{result}'");
    }

    /// <summary>
    /// 测试 - GetPreviousIp - 无效参数抛出异常
    /// </summary>
    [Theory]
    [InlineData("invalid")]
    [InlineData("192.168.1.1")]
    [InlineData("")]
    [InlineData(null)]
    public void GetPreviousIp_InvalidAddress_ThrowsArgumentException(string invalidAddress)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => IPv6Operator.GetPreviousIp(invalidAddress))
            .Message.ShouldContain("无效的IPv6地址");
    }

    /// <summary>
    /// 测试 - GetPreviousIp - 负步长抛出异常
    /// </summary>
    [Fact]
    public void GetPreviousIp_NegativeStep_ThrowsArgumentException()
    {
        // Arrange
        var address = "2001:db8::1";

        // Act & Assert
        Should.Throw<ArgumentException>(() => IPv6Operator.GetPreviousIp(address, -1))
            .Message.ShouldContain("步长不能为负数");
    }

    #endregion

    #region 集成测试

    /// <summary>
    /// 测试 - GetNext 和 GetPrevious 的往返一致性
    /// </summary>
    [Theory]
    [InlineData("2001:db8::1")]
    [InlineData("::1")]
    [InlineData("fe80::1")]
    [InlineData("fc00::1")]
    public void GetNextAndGetPrevious_RoundTrip_ReturnsOriginal(string originalAddress)
    {
        // Act
        var nextAddress = IPv6Operator.GetNextIp(originalAddress);
        var backToOriginal = IPv6Operator.GetPreviousIp(nextAddress);

        // Assert
        IPv6Converter.AreEqual(backToOriginal, originalAddress).ShouldBeTrue($"往返转换应该返回原始地址");

        Output.WriteLine($"往返测试: '{originalAddress}' -> '{nextAddress}' -> '{backToOriginal}'");
    }

    /// <summary>
    /// 测试 - 距离计算与地址递增的一致性
    /// </summary>
    [Fact]
    public void GetIpDistance_ConsistencyWithIncrement()
    {
        // Arrange
        var startAddress = "2001:db8::1";
        var step = 10;

        // Act
        var endAddress = IPv6Operator.GetNextIp(startAddress, step);
        var calculatedDistance = IPv6Operator.GetIpDistance(startAddress, endAddress);

        // Assert
        calculatedDistance.ShouldBe(new BigInteger(step), "计算的距离应该等于递增的步数");

        Output.WriteLine($"距离一致性测试: 步数={step}, 计算距离={calculatedDistance}");
    }

    /// <summary>
    /// 测试 - 排序结果与地址比较的一致性
    /// </summary>
    [Fact]
    public void Sort_ConsistencyWithAddressComparison()
    {
        // Arrange
        var addresses = new[]
        {
            "2001:db8::3",
            "2001:db8::1",
            "2001:db8::2"
        };

        // Act
        var sorted = IPv6Operator.Sort(addresses);

        // Assert
        for (int i = 0; i < sorted.Count - 1; i++)
        {
            var current = sorted[i];
            var next = sorted[i + 1];
            var distance = IPv6Operator.GetIpDistance(current, next);

            distance.ShouldBeGreaterThan(0, $"排序后的相邻地址应该有正距离: '{current}' vs '{next}'");

            Output.WriteLine($"排序一致性: '{current}' < '{next}', 距离={distance}");
        }
    }

    #endregion

    #region 性能测试

    /// <summary>
    /// 测试 - 排序性能测试
    /// </summary>
    [Fact]
    public void Sort_PerformanceTest()
    {
        // Arrange
        var random = new Random(42); // 固定种子以便复现
        var addresses = new List<string>();

        // 生成测试地址
        for (int i = 0; i < 1000; i++)
        {
            var bytes = new byte[16];
            random.NextBytes(bytes);
            addresses.Add(IPv6Converter.FromBytes(bytes));
        }

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var sortedAddresses = IPv6Operator.Sort(addresses);
        sw.Stop();

        // Assert
        sortedAddresses.Count.ShouldBe(1000);
        sw.ElapsedMilliseconds.ShouldBeLessThan(1000, "1000个地址的排序应该在1秒内完成");

        Output.WriteLine($"排序性能: {addresses.Count} 个地址, 耗时 {sw.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// 测试 - 地址递增性能测试
    /// </summary>
    [Fact]
    public void GetNextIp_PerformanceTest()
    {
        // Arrange
        var address = "2001:db8::1";
        const int iterations = 10000;

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var current = address;
        for (int i = 0; i < iterations; i++)
        {
            current = IPv6Operator.GetNextIp(current);
        }
        sw.Stop();

        // Assert
        sw.ElapsedMilliseconds.ShouldBeLessThan(1000, $"{iterations} 次地址递增应该在1秒内完成");
        IPv6Validator.IsValid(current).ShouldBeTrue("最终地址应该是有效的");

        Output.WriteLine($"递增性能: {iterations} 次递增, 耗时 {sw.ElapsedMilliseconds}ms");
        Output.WriteLine($"最终地址: {current}");
    }

    #endregion

    #region 边界情况测试

    /// <summary>
    /// 测试 - 极端值处理
    /// </summary>
    [Fact]
    public void ExtremeValues_HandledCorrectly()
    {
        // Test cases: (smaller_address, larger_address)
        var testCases = new[]
        {
            (smaller: "::", larger: "ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff"),  // 绝对最小到最大
            (smaller: "::1", larger: "ffff:ffff:ffff:ffff:ffff:ffff:ffff:fffe"), // 接近边界
            (smaller: "7fff:ffff:ffff:ffff:ffff:ffff:ffff:ffff", larger: "8000::") // 中点附近（修正）
        };

        foreach (var (smaller, larger) in testCases)
        {
            // Test distance calculation
            var distance = IPv6Operator.GetIpDistance(smaller, larger);
            distance.ShouldBeGreaterThan(0);

            // Test sorting - 验证较小的地址排在前面
            var sorted = IPv6Operator.Sort(new[] { larger, smaller }); // 故意颠倒输入顺序

            Output.WriteLine($"排序测试: 输入 [{larger}, {smaller}]");
            Output.WriteLine($"排序结果: [{sorted[0]}, {sorted[1]}]");

            IPv6Converter.AreEqual(sorted[0], smaller).ShouldBeTrue($"排序后第一个应该是较小的地址 '{smaller}', 实际为 '{sorted[0]}'");
            IPv6Converter.AreEqual(sorted[1], larger).ShouldBeTrue($"排序后第二个应该是较大的地址 '{larger}', 实际为 '{sorted[1]}'");

            Output.WriteLine($"极端值测试: '{smaller}' <-> '{larger}', 距离={distance}");
        }
    }

    #endregion
}