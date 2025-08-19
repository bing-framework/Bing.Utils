namespace Bing.Net.IPv6;

/// <summary>
/// IPv6地址生成器单元测试
/// </summary>
[Trait("Bing.Net", "IpGenerator")]
public class IPv6GeneratorTest : TestBase
{
    /// <inheritdoc />
    public IPv6GeneratorTest(ITestOutputHelper output) : base(output)
    {
    }

    #region GenerateLinkLocal 测试

    /// <summary>
    /// 测试 - GenerateLinkLocal - 不带MAC地址生成链路本地地址
    /// </summary>
    [Fact]
    public void GenerateLinkLocal_WithoutMac_GeneratesValidLinkLocalAddress()
    {
        // Act
        var result = IPv6Generator.GenerateLinkLocal();

        // Assert
        result.ShouldNotBeNullOrEmpty();
        IPv6Validator.IsValid(result).ShouldBeTrue();

        // 验证是链路本地地址（fe80::/10）
        result.ShouldStartWith("fe8");

        // 转换为字节验证前缀
        var bytes = IPv6Converter.ToBytes(result);
        bytes[0].ShouldBe((byte)0xfe);
        (bytes[1] & 0xc0).ShouldBe(0x80); // 前2位应该是10

        Output.WriteLine($"生成的链路本地地址: {result}");
    }

    /// <summary>
    /// 测试 - GenerateLinkLocal - 带有效MAC地址生成链路本地地址
    /// </summary>
    [Theory]
    [InlineData("00:11:22:33:44:55")]
    [InlineData("00-11-22-33-44-55")]
    [InlineData("00.11.22.33.44.55")]
    [InlineData("001122334455")]
    public void GenerateLinkLocal_WithValidMac_GeneratesLinkLocalWithEUI64(string macAddress)
    {
        // Act
        var result = IPv6Generator.GenerateLinkLocal(macAddress);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        IPv6Validator.IsValid(result).ShouldBeTrue();
        result.ShouldStartWith("fe8");

        // 验证EUI-64生成是否正确
        var expectedEUI64 = Bing.Net.Mac.MacAddressHelper.GenerateEUI64(macAddress);
        var resultBytes = IPv6Converter.ToBytes(result);

        // 比较后8字节（EUI-64部分）
        for (int i = 0; i < 8; i++)
        {
            resultBytes[8 + i].ShouldBe(expectedEUI64[i], $"EUI-64字节 {i} 不匹配");
        }

        Output.WriteLine($"MAC: {macAddress} -> IPv6: {result}");
    }

    /// <summary>
    /// 测试 - GenerateLinkLocal - 基于MAC地址生成
    /// </summary>
    [Theory]
    [InlineData("00:11:22:33:44:55")]
    [InlineData("aa:bb:cc:dd:ee:ff")]
    [InlineData("12:34:56:78:9a:bc")]
    public void GenerateLinkLocal_ValidMacAddress_GeneratesCorrectAddress(string macAddress)
    {
        // Act
        var result = IPv6Generator.GenerateLinkLocal(macAddress);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        IPv6Validator.IsValid(result).ShouldBeTrue();
        IPv6Validator.IsLinkLocal(result).ShouldBeTrue();
        result.ShouldStartWith("fe80:");

        Output.WriteLine($"基于MAC {macAddress} 生成的链路本地地址: {result}");
    }

    /// <summary>
    /// 测试 - GenerateLinkLocal - 无效MAC地址回退
    /// </summary>
    [Theory]
    [InlineData("invalid")]
    [InlineData("")]
    [InlineData("12:34:56")]
    public void GenerateLinkLocal_InvalidMacAddress_FallbackToRandom(string invalidMacAddress)
    {
        // Act
        var result = IPv6Generator.GenerateLinkLocal(invalidMacAddress);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        IPv6Validator.IsValid(result).ShouldBeTrue();
        IPv6Validator.IsLinkLocal(result).ShouldBeTrue();

        Output.WriteLine($"无效MAC地址 '{invalidMacAddress}' 回退生成: {result}");
    }

    /// <summary>
    /// 测试 - GenerateLinkLocal - 多次生成确保随机性
    /// </summary>
    [Fact]
    public void GenerateLinkLocal_MultipleGenerations_ProducesDifferentResults()
    {
        // Arrange
        var results = new HashSet<string>();
        const int iterations = 10;

        // Act
        for (int i = 0; i < iterations; i++)
        {
            var result = IPv6Generator.GenerateLinkLocal();
            results.Add(result);

            // 验证每个结果都有效
            IPv6Validator.IsValid(result).ShouldBeTrue();
            result.ShouldStartWith("fe8");
        }

        // Assert - 应该生成不同的地址（随机性）
        results.Count.ShouldBeGreaterThan(1, "应该生成不同的随机地址");

        Output.WriteLine($"生成了 {results.Count} 个不同的链路本地地址（共 {iterations} 次）");
        foreach (var addr in results.Take(3))
        {
            Output.WriteLine($"  {addr}");
        }
    }

    /// <summary>
    /// 测试 - GenerateLinkLocal - 随机生成
    /// </summary>
    [Fact]
    public void GenerateLinkLocal_NoMacAddress_GeneratesValidAddress()
    {
        // Act
        var result = IPv6Generator.GenerateLinkLocal();

        // Assert
        result.ShouldNotBeNullOrEmpty();
        IPv6Validator.IsValid(result).ShouldBeTrue();
        IPv6Validator.IsLinkLocal(result).ShouldBeTrue();
        result.ShouldStartWith("fe80:");

        Output.WriteLine($"随机生成的链路本地地址: {result}");
    }

    #endregion

    #region GenerateRandom 测试

    /// <summary>
    /// 测试 - GenerateRandom - 完全随机生成
    /// </summary>
    [Fact]
    public void GenerateRandom_WithoutPrefix_GeneratesValidRandomAddress()
    {
        // Act
        var result = IPv6Generator.GenerateRandom();

        // Assert
        result.ShouldNotBeNullOrEmpty();
        IPv6Validator.IsValid(result).ShouldBeTrue();

        Output.WriteLine($"随机生成的IPv6地址: {result}");
    }

    /// <summary>
    /// 测试 - GenerateRandom - 带前缀生成
    /// </summary>
    [Theory]
    [InlineData("2001:db8::", 32)]
    [InlineData("2001:db8::", 48)]
    [InlineData("fe80::", 64)]
    [InlineData("2001:db8:abcd::", 48)]
    public void GenerateRandom_WithPrefix_GeneratesAddressWithCorrectPrefix(string prefix, int prefixLength)
    {
        // Act
        var result = IPv6Generator.GenerateRandom(prefix, prefixLength);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        IPv6Validator.IsValid(result).ShouldBeTrue();

        // 验证前缀是否保持
        var resultBytes = IPv6Converter.ToBytes(result);
        var prefixBytes = IPv6Converter.ToBytes(prefix);

        var bytesToCheck = prefixLength / 8;
        var bitsToCheck = prefixLength % 8;

        // 检查完整字节
        for (int i = 0; i < bytesToCheck; i++)
        {
            resultBytes[i].ShouldBe(prefixBytes[i], $"前缀字节 {i} 应该匹配");
        }

        // 检查部分字节（如果有）
        if (bitsToCheck > 0 && bytesToCheck < 16)
        {
            var mask = (byte)(0xFF << (8 - bitsToCheck));
            var resultMasked = (byte)(resultBytes[bytesToCheck] & mask);
            var prefixMasked = (byte)(prefixBytes[bytesToCheck] & mask);
            resultMasked.ShouldBe(prefixMasked, $"前缀位应该匹配");
        }

        Output.WriteLine($"前缀: {prefix}/{prefixLength} -> 结果: {result}");
    }

    /// <summary>
    /// 测试 - GenerateRandom - 无效前缀回退到完全随机
    /// </summary>
    [Theory]
    [InlineData("invalid", 64)]
    [InlineData("", 32)]
    [InlineData(null, 48)]
    [InlineData("192.168.1.1", 32)]  // IPv4地址
    public void GenerateRandom_WithInvalidPrefix_FallbackToFullyRandom(string invalidPrefix, int prefixLength)
    {
        // Act
        var result = IPv6Generator.GenerateRandom(invalidPrefix, prefixLength);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        IPv6Validator.IsValid(result).ShouldBeTrue();

        Output.WriteLine($"无效前缀 '{invalidPrefix}' -> 随机地址: {result}");
    }

    /// <summary>
    /// 测试 - GenerateRandom - 边界前缀长度
    /// </summary>
    [Theory]
    [InlineData(0)]   // 无前缀
    [InlineData(128)] // 完整前缀
    [InlineData(64)]  // 典型前缀
    [InlineData(1)]   // 最小前缀
    [InlineData(127)] // 最大有效前缀
    public void GenerateRandom_WithBoundaryPrefixLengths_HandlesCorrectly(int prefixLength)
    {
        // Arrange
        var prefix = "2001:db8::";

        // Act
        var result = IPv6Generator.GenerateRandom(prefix, prefixLength);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        IPv6Validator.IsValid(result).ShouldBeTrue();

        if (prefixLength == 128)
        {
            // 完整前缀应该返回相同地址
            IPv6Converter.AreEqual(result, prefix).ShouldBeTrue();
        }

        Output.WriteLine($"前缀长度 {prefixLength} -> 结果: {result}");
    }

    /// <summary>
    /// 测试 - GenerateRandom - 无效前缀长度抛出异常
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(129)]
    public void GenerateRandom_InvalidPrefixLength_ThrowsException(int invalidPrefixLength)
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() =>
            IPv6Generator.GenerateRandom("2001:db8::", invalidPrefixLength));
    }

    /// <summary>
    /// 测试 - GenerateRandom - 多次生成确保随机性
    /// </summary>
    [Fact]
    public void GenerateRandom_MultipleGenerations_ProducesDifferentResults()
    {
        // Arrange
        var results = new HashSet<string>();
        const int iterations = 10;
        var prefix = "2001:db8::";
        const int prefixLength = 32;

        // Act
        for (int i = 0; i < iterations; i++)
        {
            var result = IPv6Generator.GenerateRandom(prefix, prefixLength);
            results.Add(result);

            // 验证每个结果都有效
            IPv6Validator.IsValid(result).ShouldBeTrue();
            result.ShouldStartWith("2001:db8");
        }

        // Assert - 应该生成不同的地址
        results.Count.ShouldBeGreaterThan(1, "应该生成不同的随机地址");

        Output.WriteLine($"生成了 {results.Count} 个不同的地址（共 {iterations} 次）");
        foreach (var addr in results.Take(3))
        {
            Output.WriteLine($"  {addr}");
        }
    }

    /// <summary>
    /// 测试 - GenerateRandom - 唯一性测试
    /// </summary>
    [Fact]
    public void GenerateRandom_MultipleGenerations_ProducesUniqueAddresses()
    {
        // Arrange
        var addresses = new HashSet<string>();
        const int count = 100;

        // Act
        for (int i = 0; i < count; i++)
        {
            var address = IPv6Generator.GenerateRandom();
            addresses.Add(address);
        }

        // Assert
        addresses.Count.ShouldBeGreaterThan((int)(count * 0.95)); // 允许极少数重复
        Output.WriteLine($"生成 {count} 个随机地址，唯一地址数: {addresses.Count}");
    }

    #endregion

    #region GenerateRandomBatch 测试

    /// <summary>
    /// 测试 - GenerateRandomBatch - 批量生成
    /// </summary>
    [Fact]
    public void GenerateRandomBatch_RequestedCount_GeneratesCorrectAmount()
    {
        // Arrange
        const int count = 10;

        // Act
        var result = IPv6Generator.GenerateRandomBatch(count);

        // Assert
        result.Count.ShouldBe(count);
        result.All(IPv6Validator.IsValid).ShouldBeTrue();

        // 验证唯一性
        result.Distinct().Count().ShouldBe(count);

        Output.WriteLine($"批量生成 {count} 个地址:");
        foreach (var addr in result.Take(5))
        {
            Output.WriteLine($"  {addr}");
        }
    }

    #endregion

    #region GenerateUniqueLocal 测试

    /// <summary>
    /// 测试 - GenerateUniqueLocal - 默认生成
    /// </summary>
    [Fact]
    public void GenerateUniqueLocal_Default_GeneratesValidULA()
    {
        // Act
        var result = IPv6Generator.GenerateUniqueLocal();

        // Assert
        result.ShouldNotBeNullOrEmpty();
        IPv6Validator.IsValid(result).ShouldBeTrue();
        IPv6Validator.IsUniqueLocal(result).ShouldBeTrue();
        result.ShouldStartWith("fd");

        Output.WriteLine($"生成的ULA地址: {result}");
    }

    /// <summary>
    /// 测试 - GenerateUniqueLocal - 指定全局ID和子网ID
    /// </summary>
    [Fact]
    public void GenerateUniqueLocal_WithGlobalIdAndSubnetId_GeneratesCorrectAddress()
    {
        // Arrange
        var globalId = new byte[] { 0x12, 0x34, 0x56, 0x78, 0x9a };
        var subnetId = new byte[] { 0xbc, 0xde };

        // Act
        var result = IPv6Generator.GenerateUniqueLocal(globalId, subnetId);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        IPv6Validator.IsValid(result).ShouldBeTrue();
        IPv6Validator.IsUniqueLocal(result).ShouldBeTrue();
        result.ShouldStartWith("fd12:3456:789a:bcde:");

        Output.WriteLine($"指定ID生成的ULA地址: {result}");
    }

    /// <summary>
    /// 测试 - GenerateUniqueLocal - 带指定subnetId生成ULA地址
    /// </summary>
    [Fact]
    public void GenerateUniqueLocal_WithSubnetId_UsesProvidedSubnetId()
    {
        // Arrange
        var subnetId = new byte[] { 0xbc, 0xde };

        // Act
        var result = IPv6Generator.GenerateUniqueLocal(null, subnetId);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        IPv6Validator.IsValid(result).ShouldBeTrue();
        result.ShouldStartWith("fd");

        // 验证subnetId是否正确设置
        var bytes = IPv6Converter.ToBytes(result);
        bytes[0].ShouldBe((byte)0xfd);
        bytes[6].ShouldBe(subnetId[0], "Subnet ID第一个字节应该匹配");
        bytes[7].ShouldBe(subnetId[1], "Subnet ID第二个字节应该匹配");

        Output.WriteLine($"带Subnet ID的ULA地址: {result}");
        Output.WriteLine($"Subnet ID: {subnetId[0]:x2}:{subnetId[1]:x2}");
    }

    /// <summary>
    /// 测试 - GenerateUniqueLocal - 带完整参数生成ULA地址
    /// </summary>
    [Fact]
    public void GenerateUniqueLocal_WithAllParameters_UsesProvidedValues()
    {
        // Arrange
        var globalId = new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55 };
        var subnetId = new byte[] { 0x66, 0x77 };

        // Act
        var result = IPv6Generator.GenerateUniqueLocal(globalId, subnetId);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        IPv6Validator.IsValid(result).ShouldBeTrue();
        result.ShouldStartWith("fd");

        // 验证所有提供的参数
        var bytes = IPv6Converter.ToBytes(result);
        bytes[0].ShouldBe((byte)0xfd);

        // 验证globalId
        for (int i = 0; i < 5; i++)
        {
            bytes[1 + i].ShouldBe(globalId[i], $"Global ID字节 {i} 应该匹配");
        }

        // 验证subnetId
        bytes[6].ShouldBe(subnetId[0], "Subnet ID第一个字节应该匹配");
        bytes[7].ShouldBe(subnetId[1], "Subnet ID第二个字节应该匹配");

        Output.WriteLine($"完整参数的ULA地址: {result}");
        Output.WriteLine($"Global ID: {string.Join(":", globalId.Select(b => b.ToString("x2")))}");
        Output.WriteLine($"Subnet ID: {subnetId[0]:x2}:{subnetId[1]:x2}");
    }

    /// <summary>
    /// 测试 - GenerateUniqueLocal - 无效长度的globalId被忽略
    /// </summary>
    [Theory]
    [InlineData(new byte[] { 0x11, 0x22, 0x33, 0x44 })]        // 4字节，应该被忽略
    [InlineData(new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66 })] // 6字节，应该被忽略
    public void GenerateUniqueLocal_WithInvalidGlobalIdLength_IgnoresGlobalId(byte[] invalidGlobalId)
    {
        // Act
        var result = IPv6Generator.GenerateUniqueLocal(invalidGlobalId);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        IPv6Validator.IsValid(result).ShouldBeTrue();
        result.ShouldStartWith("fd");

        // 由于globalId无效，应该使用随机生成
        // 我们无法预测随机值，但可以确保地址格式正确
        var bytes = IPv6Converter.ToBytes(result);
        bytes[0].ShouldBe((byte)0xfd);

        Output.WriteLine($"无效Global ID长度 ({invalidGlobalId.Length}字节) -> ULA: {result}");
    }

    /// <summary>
    /// 测试 - GenerateUniqueLocal - 无效长度的subnetId被忽略
    /// </summary>
    [Theory]
    [InlineData(new byte[] { 0x11 })]              // 1字节，应该被忽略
    [InlineData(new byte[] { 0x11, 0x22, 0x33 })] // 3字节，应该被忽略
    public void GenerateUniqueLocal_WithInvalidSubnetIdLength_IgnoresSubnetId(byte[] invalidSubnetId)
    {
        // Act
        var result = IPv6Generator.GenerateUniqueLocal(null, invalidSubnetId);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        IPv6Validator.IsValid(result).ShouldBeTrue();
        result.ShouldStartWith("fd");

        // 由于subnetId无效，应该使用随机生成
        var bytes = IPv6Converter.ToBytes(result);
        bytes[0].ShouldBe((byte)0xfd);

        Output.WriteLine($"无效Subnet ID长度 ({invalidSubnetId.Length}字节) -> ULA: {result}");
    }

    /// <summary>
    /// 测试 - GenerateUniqueLocal - 多次生成确保随机性（接口ID部分）
    /// </summary>
    [Fact]
    public void GenerateUniqueLocal_MultipleGenerations_ProducesDifferentInterfaceIds()
    {
        // Arrange
        var results = new HashSet<string>();
        var globalId = new byte[] { 0x12, 0x34, 0x56, 0x78, 0x9a };
        var subnetId = new byte[] { 0xbc, 0xde };
        const int iterations = 10;

        // Act
        for (int i = 0; i < iterations; i++)
        {
            var result = IPv6Generator.GenerateUniqueLocal(globalId, subnetId);
            results.Add(result);

            // 验证每个结果都有效且前缀正确
            IPv6Validator.IsValid(result).ShouldBeTrue();
            result.ShouldStartWith("fd");

            // 验证固定部分（前8字节）是否一致
            var bytes = IPv6Converter.ToBytes(result);
            bytes[0].ShouldBe((byte)0xfd);
            for (int j = 0; j < 5; j++)
            {
                bytes[1 + j].ShouldBe(globalId[j]);
            }
            bytes[6].ShouldBe(subnetId[0]);
            bytes[7].ShouldBe(subnetId[1]);
        }

        // Assert - 接口ID部分应该是随机的，所以应该生成不同的地址
        results.Count.ShouldBeGreaterThan(1, "接口ID应该是随机的，生成不同的地址");

        Output.WriteLine($"生成了 {results.Count} 个不同的ULA地址（共 {iterations} 次）");
        foreach (var addr in results.Take(3))
        {
            Output.WriteLine($"  {addr}");
        }
    }

    /// <summary>
    /// 测试 - GenerateUniqueLocal - ULA地址结构验证
    /// </summary>
    [Fact]
    public void GenerateUniqueLocal_AddressStructure_FollowsULASpecification()
    {
        // Arrange
        var globalId = new byte[] { 0xaa, 0xbb, 0xcc, 0xdd, 0xee };
        var subnetId = new byte[] { 0x12, 0x34 };

        // Act
        var result = IPv6Generator.GenerateUniqueLocal(globalId, subnetId);

        // Assert
        var bytes = IPv6Converter.ToBytes(result);

        // RFC 4193 ULA地址结构验证：
        // | 7 bits |1|  40 bits   |  16 bits  |          64 bits           |
        // | Prefix |L| Global ID  | Subnet ID |        Interface ID        |
        // | FC00::/7 |L| Global ID  | Subnet ID |        Interface ID        |

        // 前缀：fd00::/8 (L=1表示本地分配)
        bytes[0].ShouldBe((byte)0xfd, "应该是fd前缀（本地分配ULA）");

        // Global ID（5字节，位置1-5）
        for (int i = 0; i < 5; i++)
        {
            bytes[1 + i].ShouldBe(globalId[i], $"Global ID字节 {i} 应该匹配");
        }

        // Subnet ID（2字节，位置6-7）
        bytes[6].ShouldBe(subnetId[0], "Subnet ID第一个字节应该匹配");
        bytes[7].ShouldBe(subnetId[1], "Subnet ID第二个字节应该匹配");

        // Interface ID（8字节，位置8-15）应该存在但是随机的
        var interfaceId = bytes.Skip(8).Take(8).ToArray();
        interfaceId.Length.ShouldBe(8, "接口ID应该是8字节");

        Output.WriteLine($"ULA地址结构验证通过: {result}");
        Output.WriteLine($"前缀: {bytes[0]:x2}");
        Output.WriteLine($"Global ID: {string.Join(":", globalId.Select(b => b.ToString("x2")))}");
        Output.WriteLine($"Subnet ID: {string.Join(":", subnetId.Select(b => b.ToString("x2")))}");
        Output.WriteLine($"Interface ID: {string.Join(":", interfaceId.Select(b => b.ToString("x2")))}");
    }

    /// <summary>
    /// 测试 - GenerateUniqueLocal - 与IPv6工具兼容性
    /// </summary>
    [Fact]
    public void GenerateUniqueLocal_CompatibilityWithIPv6Tools()
    {
        // Arrange & Act
        var ula = IPv6Generator.GenerateUniqueLocal();

        // Assert - 测试与其他IPv6工具的兼容性

        // 1. 地址展开和压缩
        var expanded = IPv6Converter.Expand(ula);
        var compressed = IPv6Converter.Compress(expanded);
        IPv6Converter.AreEqual(ula, compressed).ShouldBeTrue();

        // 2. 转换为其他格式
        var hex = IPv6Converter.ToHexString(ula);
        hex.Length.ShouldBe(32);
        hex.ShouldStartWith("fd");

        var binary = IPv6Converter.ToBinaryString(ula);
        binary.Length.ShouldBe(128);
        binary.ShouldStartWith("11111101"); // fd的二进制

        // 3. BigInteger转换
        var bigInt = IPv6Converter.ToBigInteger(ula);
        bigInt.ShouldBeGreaterThan(0);

        // 4. 地址验证
        IPv6Validator.IsValid(ula).ShouldBeTrue();

        Output.WriteLine($"ULA地址兼容性测试通过: {ula}");
        Output.WriteLine($"十六进制: {hex}");
        Output.WriteLine($"二进制前8位: {binary.Substring(0, 8)}");
    }

    #endregion

    #region GenerateMulticast 测试

    /// <summary>
    /// 测试 - GenerateMulticast - 组播地址生成
    /// </summary>
    [Theory]
    [InlineData(0x1)] // 接口本地
    [InlineData(0x2)] // 链路本地
    [InlineData(0x5)] // 站点本地
    [InlineData(0xE)] // 全局
    public void GenerateMulticast_VariousScopes_GeneratesValidMulticast(byte scope)
    {
        // Act
        var result = IPv6Generator.GenerateMulticast(scope);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        IPv6Validator.IsValid(result).ShouldBeTrue();
        IPv6Validator.IsMulticast(result).ShouldBeTrue();
        result.ShouldStartWith("ff");

        Output.WriteLine($"生成的组播地址 (scope={scope:X}): {result}");
    }

    #endregion

    #region GenerateFromMac 测试

    /// <summary>
    /// 测试 - GenerateFromMac - 基于MAC生成地址
    /// </summary>
    [Fact]
    public void GenerateFromMac_ValidMacAndPrefix_GeneratesCorrectAddress()
    {
        // Arrange
        var macAddress = "00:11:22:33:44:55";
        var prefix = "2001:db8::";
        var prefixLength = 64;

        // Act
        var result = IPv6Generator.GenerateFromMac(macAddress, prefix, prefixLength);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        IPv6Validator.IsValid(result).ShouldBeTrue();
        result.ShouldStartWith("2001:db8:");

        Output.WriteLine($"基于MAC {macAddress} 和前缀 {prefix}/{prefixLength} 生成: {result}");
    }

    #endregion

    #region GenerateRange 测试

    /// <summary>
    /// 测试 - GenerateRange - 小范围生成
    /// </summary>
    [Fact]
    public void GenerateRange_SmallRange_GeneratesCorrectSequence()
    {
        // Arrange
        var start = "2001:db8::1";
        var end = "2001:db8::5";

        // Act
        var result = IPv6Generator.GenerateRange(start, end);

        // Assert
        result.Count.ShouldBe(5);
        result[0].ShouldBe("2001:db8::1");
        result[4].ShouldBe("2001:db8::5");

        // 验证连续性
        for (int i = 1; i < result.Count; i++)
        {
            var prev = IPv6Converter.ToBigInteger(result[i - 1]);
            var curr = IPv6Converter.ToBigInteger(result[i]);
            (curr - prev).ShouldBe(1);
        }

        Output.WriteLine($"地址范围 {start} 到 {end}:");
        foreach (var addr in result)
        {
            Output.WriteLine($"  {addr}");
        }
    }

    /// <summary>
    /// 测试 - GenerateRange - 超出最大限制抛出异常
    /// </summary>
    [Fact]
    public void GenerateRange_ExceedsMaxCount_ThrowsException()
    {
        // Arrange
        var start = "2001:db8::1";
        var end = "2001:db8::1000"; // 范围太大

        // Act & Assert
        Should.Throw<ArgumentException>(() => IPv6Generator.GenerateRange(start, end, 100));
    }

    /// <summary>
    /// 测试 - GenerateRange - 起始地址大于结束地址抛出异常
    /// </summary>
    [Fact]
    public void GenerateRange_StartGreaterThanEnd_ThrowsException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            IPv6Generator.GenerateRange("2001:db8::10", "2001:db8::1"));
    }

    #endregion

    #region GenerateAddressPool 测试

    /// <summary>
    /// 测试 - GenerateAddressPool - 地址池生成
    /// </summary>
    [Fact]
    public void GenerateAddressPool_ValidParameters_CreatesUsablePool()
    {
        // Arrange
        const int poolSize = 10;

        // Act
        var pool = IPv6Generator.GenerateAddressPool(poolSize, IPv6AddressType.LinkLocal);

        // Assert
        pool.ShouldNotBeNull();
        pool.Addresses.Count.ShouldBe(poolSize);
        pool.AddressType.ShouldBe(IPv6AddressType.LinkLocal);
        pool.RemainingCount.ShouldBe(poolSize);
        pool.IsExhausted.ShouldBeFalse();

        // 测试地址获取
        for (int i = 0; i < poolSize; i++)
        {
            var addr = pool.GetNext();
            IPv6Validator.IsValid(addr).ShouldBeTrue();
            IPv6Validator.IsLinkLocal(addr).ShouldBeTrue();
        }

        pool.IsExhausted.ShouldBeTrue();
        Should.Throw<InvalidOperationException>(() => pool.GetNext());

        Output.WriteLine($"地址池生成测试完成，池大小: {poolSize}");
    }

    #endregion

    #region 性能测试

    /// <summary>
    /// 测试 - 生成性能测试
    /// </summary>
    [Fact]
    public void GenerationMethods_Performance_CompletesWithinReasonableTime()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // 测试各种生成方法的性能
        for (int i = 0; i < 1000; i++)
        {
            IPv6Generator.GenerateRandom();
            IPv6Generator.GenerateLinkLocal();
            IPv6Generator.GenerateUniqueLocal();
        }

        sw.Stop();

        sw.ElapsedMilliseconds.ShouldBeLessThan(5000, "3000次地址生成应该在5秒内完成");
        Output.WriteLine($"性能测试: 3000次地址生成耗时 {sw.ElapsedMilliseconds}ms");
    }

    #endregion

    #region 集成测试

    /// <summary>
    /// 测试 - 生成的地址与其他IPv6工具的兼容性
    /// </summary>
    [Fact]
    public void GeneratedAddresses_CompatibleWithIPv6Tools()
    {
        // Arrange & Act
        var linkLocal = IPv6Generator.GenerateLinkLocal("00:11:22:33:44:55");
        var random = IPv6Generator.GenerateRandom("2001:db8::", 48);

        // Assert - 测试与其他IPv6工具的兼容性

        // 1. 地址展开和压缩
        var linkLocalExpanded = IPv6Converter.Expand(linkLocal);
        var linkLocalCompressed = IPv6Converter.Compress(linkLocalExpanded);
        IPv6Converter.AreEqual(linkLocal, linkLocalCompressed).ShouldBeTrue();

        // 2. 转换为其他格式
        var linkLocalHex = IPv6Converter.ToHexString(linkLocal);
        linkLocalHex.Length.ShouldBe(32);

        var linkLocalBinary = IPv6Converter.ToBinaryString(linkLocal);
        linkLocalBinary.Length.ShouldBe(128);

        // 3. BigInteger转换
        var linkLocalBigInt = IPv6Converter.ToBigInteger(linkLocal);
        linkLocalBigInt.ShouldBeGreaterThan(0);

        Output.WriteLine($"链路本地地址兼容性测试通过: {linkLocal}");
        Output.WriteLine($"随机地址兼容性测试通过: {random}");
    }

    #endregion
}