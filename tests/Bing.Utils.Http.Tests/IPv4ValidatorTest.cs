using Bing.Net.IPv4;

namespace Bing.Net.IPv4;

/// <summary>
/// IPv4地址验证器测试
/// </summary>
public class IPv4ValidatorTest
{
    /// <summary>
    /// 测试目的：验证有效的IPv4地址
    /// </summary>
    [Theory]
    [InlineData("192.168.1.1")]
    [InlineData("10.0.0.1")]
    [InlineData("172.16.0.1")]
    [InlineData("8.8.8.8")]
    [InlineData("255.255.255.255")]
    [InlineData("0.0.0.0")]
    [InlineData("127.0.0.1")]
    public void IsValid_ValidIPv4_ReturnsTrue(string ip)
    {
        // Act
        var result = IPv4Validator.IsValid(ip);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试目的：验证无效的IPv4地址（超出范围）
    /// </summary>
    [Theory]
    [InlineData("256.1.1.1")]
    [InlineData("1.256.1.1")]
    [InlineData("1.1.256.1")]
    [InlineData("1.1.1.256")]
    [InlineData("300.300.300.300")]
    public void IsValid_OutOfRange_ReturnsFalse(string ip)
    {
        // Act
        var result = IPv4Validator.IsValid(ip);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 测试目的：验证前导零的处理（应该被拒绝）
    /// </summary>
    [Theory]
    [InlineData("192.168.01.1")]
    [InlineData("192.168.001.1")]
    [InlineData("01.1.1.1")]
    [InlineData("192.168.1.01")]
    public void IsValid_LeadingZeros_ReturnsFalse(string ip)
    {
        // Act
        var result = IPv4Validator.IsValid(ip);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 测试目的：验证格式错误的IPv4地址
    /// </summary>
    [Theory]
    [InlineData("192.168.1")]        // 缺少段
    [InlineData("192.168.1.1.1")]    // 多余段
    [InlineData("192.168.1.")]       // 末尾点号
    [InlineData(".192.168.1.1")]     // 前导点号
    [InlineData("192..168.1.1")]     // 双点号
    [InlineData("192.168.-1.1")]     // 负数
    [InlineData("a.b.c.d")]          // 非数字
    public void IsValid_MalformedIPv4_ReturnsFalse(string ip)
    {
        // Act
        var result = IPv4Validator.IsValid(ip);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 测试目的：验证IPv6地址应该被拒绝
    /// </summary>
    [Theory]
    [InlineData("2001:db8::1")]
    [InlineData("::1")]
    [InlineData("fe80::1")]
    public void IsValid_IPv6Address_ReturnsFalse(string ip)
    {
        // Act
        var result = IPv4Validator.IsValid(ip);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 测试目的：验证空值或null的处理
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void IsValid_NullOrWhitespace_ReturnsFalse(string ip)
    {
        // Act
        var result = IPv4Validator.IsValid(ip);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 测试目的：验证回环地址判断
    /// </summary>
    [Theory]
    [InlineData("127.0.0.1", true)]
    [InlineData("127.0.0.0", true)]
    [InlineData("127.255.255.255", true)]
    [InlineData("127.1.2.3", true)]
    [InlineData("128.0.0.1", false)]
    [InlineData("192.168.1.1", false)]
    [InlineData("10.0.0.1", false)]
    public void IsLocalIp_VariousAddresses_ReturnsExpected(string ip, bool expected)
    {
        // Act
        var result = IPv4Validator.IsLocalIp(ip);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// 测试目的：验证内网地址判断 - A类私有地址
    /// </summary>
    [Theory]
    [InlineData("10.0.0.0", true)]
    [InlineData("10.0.0.1", true)]
    [InlineData("10.255.255.255", true)]
    [InlineData("10.128.64.32", true)]
    [InlineData("9.255.255.255", false)]
    [InlineData("11.0.0.0", false)]
    public void IsInternalIp_ClassAPrivate_ReturnsExpected(string ip, bool expected)
    {
        // Act
        var result = IPv4Validator.IsInternalIp(ip);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// 测试目的：验证内网地址判断 - B类私有地址
    /// </summary>
    [Theory]
    [InlineData("172.16.0.0", true)]
    [InlineData("172.16.0.1", true)]
    [InlineData("172.31.255.255", true)]
    [InlineData("172.20.10.5", true)]
    [InlineData("172.15.255.255", false)]
    [InlineData("172.32.0.0", false)]
    public void IsInternalIp_ClassBPrivate_ReturnsExpected(string ip, bool expected)
    {
        // Act
        var result = IPv4Validator.IsInternalIp(ip);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// 测试目的：验证内网地址判断 - C类私有地址
    /// </summary>
    [Theory]
    [InlineData("192.168.0.0", true)]
    [InlineData("192.168.1.1", true)]
    [InlineData("192.168.255.255", true)]
    [InlineData("192.168.100.50", true)]
    [InlineData("192.167.255.255", false)]
    [InlineData("192.169.0.0", false)]
    public void IsInternalIp_ClassCPrivate_ReturnsExpected(string ip, bool expected)
    {
        // Act
        var result = IPv4Validator.IsInternalIp(ip);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// 测试目的：验证内网地址判断 - 链路本地地址
    /// </summary>
    [Theory]
    [InlineData("169.254.0.0", true)]
    [InlineData("169.254.1.1", true)]
    [InlineData("169.254.255.255", true)]
    [InlineData("169.253.255.255", false)]
    [InlineData("169.255.0.0", false)]
    public void IsInternalIp_LinkLocal_ReturnsExpected(string ip, bool expected)
    {
        // Act
        var result = IPv4Validator.IsInternalIp(ip);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// 测试目的：验证内网地址判断 - 回环地址也算内网
    /// </summary>
    [Theory]
    [InlineData("127.0.0.1", true)]
    [InlineData("127.0.0.0", true)]
    [InlineData("127.255.255.255", true)]
    public void IsInternalIp_Loopback_ReturnsTrue(string ip)
    {
        // Act
        var result = IPv4Validator.IsInternalIp(ip);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试目的：验证公网地址
    /// </summary>
    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("1.1.1.1")]
    [InlineData("114.114.114.114")]
    [InlineData("220.181.38.148")]
    public void IsInternalIp_PublicAddress_ReturnsFalse(string ip)
    {
        // Act
        var result = IPv4Validator.IsInternalIp(ip);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 测试目的：验证无效地址的处理
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("256.1.1.1")]
    public void IsLocalIp_InvalidAddress_ReturnsFalse(string ip)
    {
        // Act
        var result = IPv4Validator.IsLocalIp(ip);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 测试目的：验证无效地址的内网判断
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("256.1.1.1")]
    public void IsInternalIp_InvalidAddress_ReturnsFalse(string ip)
    {
        // Act
        var result = IPv4Validator.IsInternalIp(ip);

        // Assert
        Assert.False(result);
    }
}
