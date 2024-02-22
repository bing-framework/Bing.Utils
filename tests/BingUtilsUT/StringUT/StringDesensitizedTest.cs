using Bing.Text;

namespace BingUtilsUT.StringUT;

[Trait("StringUT", "Strings.Desensitized")]
public class StringDesensitizedTest
{
    /// <summary>
    /// 测试 - 脱敏 - 中文名
    /// </summary>
    [Fact]
    public void Test_ChineseName()
    {
        DesensitizedHelper.ChineseName("段正淳").ShouldBe("段**");
    }

    /// <summary>
    /// 测试 - 脱敏 -身份证
    /// </summary>
    [Fact]
    public void Test_IdCardNum()
    {
        DesensitizedHelper.IdCardNum("51343620000320711X",1,2).ShouldBe("5***************1X");
    }

    /// <summary>
    /// 测试 - 脱敏 - 固定电话
    /// </summary>
    [Fact]
    public void Test_FixedPhone()
    {
        DesensitizedHelper.FixedPhone("09157518479").ShouldBe("0915*****79");
    }

    /// <summary>
    /// 测试 - 脱敏 - 手机号码
    /// </summary>
    [Fact]
    public void Test_MobilePhone()
    {
        DesensitizedHelper.MobilePhone("13610000000").ShouldBe("136****0000");
    }

    /// <summary>
    /// 测试 - 脱敏 - 地址
    /// </summary>
    [Fact]
    public void Test_Address()
    {
        DesensitizedHelper.Address("广东省广州市天河区猎德街道289号",5).ShouldBe("广东省广州市天河区猎德街*****");
        DesensitizedHelper.Address("广东省广州市天河区猎德街道289号",50).ShouldBe("*****************");
        DesensitizedHelper.Address("广东省广州市天河区猎德街道289号",0).ShouldBe("广东省广州市天河区猎德街道289号");
        DesensitizedHelper.Address("广东省广州市天河区猎德街道289号",-1).ShouldBe("广东省广州市天河区猎德街道289号");
    }

    /// <summary>
    /// 测试 - 脱敏 - 邮箱
    /// </summary>
    [Fact]
    public void Test_Email()
    {
        DesensitizedHelper.Email("wang@126.com").ShouldBe("w***@126.com");
        DesensitizedHelper.Email("wang@gmail.com.cn").ShouldBe("w***@gmail.com.cn");
        DesensitizedHelper.Email("wang-andy@gmail.com.cn").ShouldBe("w********@gmail.com.cn");
    }

    /// <summary>
    /// 测试 - 脱敏 - 密码
    /// </summary>
    [Fact]
    public void Test_Password()
    {
        DesensitizedHelper.Password("1234567890").ShouldBe("**********");
    }

    /// <summary>
    /// 测试 - 脱敏 - 银行卡
    /// </summary>
    [Theory]
    [InlineData(null,"")]
    [InlineData("","")]
    [InlineData(" ","")]
    [InlineData("1234 2222 3333 4444 6789 9","1234 **** **** **** **** 9")]
    [InlineData("1234 2222 3333 4444 6789 91","1234 **** **** **** **** 91")]
    [InlineData("1234 2222 3333 4444 6789","1234 **** **** **** 6789")]
    [InlineData("1234 2222 3333 4444 678","1234 **** **** **** 678")]
    [InlineData("11011111222233333256","1101 **** **** **** 3256")]
    [InlineData("6227880100100105123","6227 **** **** **** 123")]
    public void Test_BankCard(string input, string result)
    {
        DesensitizedHelper.BankCard(input).ShouldBe(result);
    }

    /// <summary>
    /// 测试 - 脱敏 - IPv4
    /// </summary>
    [Fact]
    public void Test_IPv4()
    {
        DesensitizedHelper.IPv4("192.168.1.1").ShouldBe("192.*.*.*");
    }
    
    /// <summary>
    /// 测试 - 脱敏 - IPv6
    /// </summary>
    [Fact]
    public void Test_IPv6()
    {
        DesensitizedHelper.IPv6("2001:0db8:86a3:08d3:1319:8a2e:0370:7344").ShouldBe("2001:*:*:*:*:*:*:*");
    }
}