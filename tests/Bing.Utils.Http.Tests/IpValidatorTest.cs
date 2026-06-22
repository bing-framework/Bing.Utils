using Bing.Net;

namespace Bing.Net;

/// <summary>
/// 通用IP地址验证器测试
/// </summary>
public class IpValidatorTest
{
    /// <summary>
    /// 测试目的：验证有效的IPv4地址格式
    /// </summary>
    [Theory]
    [InlineData("192.168.1.1")]
    [InlineData("10.0.0.1")]
    [InlineData("172.16.0.1")]
    [InlineData("8.8.8.8")]
    [InlineData("255.255.255.255")]
    [InlineData("0.0.0.0")]
    [InlineData("127.0.0.1")]
    [InlineData("1.1.1.1")]
    public void IsValid_ValidIPv4_ReturnsTrue(string ip)
    {
        // Act
        var result = IpValidator.IsValid(ip);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试目的：验证无效的IPv4地址格式
    /// </summary>
    [Theory]
    [InlineData("256.1.1.1")]          // 超出范围
    [InlineData("192.168.1")]          // 缺少段
    [InlineData("192.168.1.1.1")]      // 多余段
    [InlineData("192.168.01.1")]       // 前导零
    [InlineData("192.168.-1.1")]       // 负数
    [InlineData("192.168.1.")]         // 末尾点号
    [InlineData(".192.168.1.1")]       // 前导点号
    [InlineData("192..168.1.1")]       // 双点号
    [InlineData("a.b.c.d")]            // 非数字
    [InlineData("192.168.1.1a")]       // 包含字母
    public void IsValid_InvalidIPv4_ReturnsFalse(string ip)
    {
        // Act
        var result = IpValidator.IsValid(ip);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 测试目的：验证有效的IPv6地址格式
    /// </summary>
    [Theory]
    [InlineData("2001:db8::1")]
    [InlineData("::1")]
    [InlineData("fe80::1")]
    [InlineData("2001:db8::ff00:42:8329")] // 归一化形式（无前导零，使用::压缩）
    [InlineData("::")]
    [InlineData("2001:db8:85a3::8a2e:370:7334")]
    public void IsValid_ValidIPv6_ReturnsTrue(string ip)
    {
        // Act
        var result = IpValidator.IsValid(ip);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试目的：验证无效的IPv6地址格式
    /// </summary>
    [Theory]
    [InlineData("02001:0db8:0000:0000:0000:ff00:0042:8329")]  // 前导零过多
    [InlineData("2001:db8::8a2e::7334")]                        // 双重 ::
    [InlineData("gggg::1")]                                      // 非十六进制
    [InlineData("2001:db8:")]                                    // 不完整
    public void IsValid_InvalidIPv6_ReturnsFalse(string ip)
    {
        // Act
        var result = IpValidator.IsValid(ip);

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
    [InlineData("\n")]
    public void IsValid_NullOrEmpty_ReturnsFalse(string ip)
    {
        // Act
        var result = IpValidator.IsValid(ip);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 测试目的：验证各种无效输入
    /// </summary>
    [Theory]
    [InlineData("invalid")]
    [InlineData("192.168.1.1/24")]  // 包含CIDR
    [InlineData("http://192.168.1.1")]
    [InlineData("192.168.1.1:8080")]  // 包含端口
    public void IsValid_InvalidFormat_ReturnsFalse(string ip)
    {
        // Act
        var result = IpValidator.IsValid(ip);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 测试目的：验证边界值
    /// </summary>
    [Theory]
    [InlineData("0.0.0.0", true)]
    [InlineData("255.255.255.255", true)]
    [InlineData("255.255.255.256", false)]
    [InlineData("-1.0.0.0", false)]
    public void IsValid_BoundaryValues_ReturnsExpected(string ip, bool expected)
    {
        // Act
        var result = IpValidator.IsValid(ip);

        // Assert
        Assert.Equal(expected, result);
    }
}
